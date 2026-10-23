#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;

namespace HtmlToUGUI.Editor
{
    [Serializable] public sealed class UiProjectSource
    {
        public string packagePath, panelName, error;
    }
    [Serializable] public sealed class UiProjectScan
    {
        public string directory;
        public List<UiProjectSource> sources = new List<UiProjectSource>();
        public List<string> warnings = new List<string>();
    }
    [Serializable] public sealed class UiProjectPanelPlan
    {
        public string packagePath, packageSha256, panelName, error;
        public UiImportPlan import;
    }
    [Serializable] public sealed class UiProjectImportPlan
    {
        public string directory, projectName;
        public bool replaceGeneratedPrefabs;
        public List<UiProjectPanelPlan> panels = new List<UiProjectPanelPlan>();
        public List<string> errors = new List<string>();
        public UiProjectAssetPlan library;
        public bool valid { get { return panels.Count > 0 && errors.Count == 0 && panels.All(p => p.error == null && p.import != null); } }
    }
    [Serializable] public sealed class UiProjectPanelResult
    {
        public string packagePath, panelName, status = "skipped", prefabPath, error;
    }
    [Serializable] public sealed class UiProjectImportResult
    {
        public string projectName, status, reportPath, reportError, previewError;
        public string assetManifestHash;
        public List<string> assetWarnings = new List<string>();
        public List<UiProjectPanelResult> panels = new List<UiProjectPanelResult>();
    }

    public static class UiProjectImport
    {
        private static readonly HashSet<string> Excluded = new HashSet<string>(new[] {
            ".git", ".svn", "Library", "Temp", "obj", "bin", "node_modules"
        }, StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ActiveProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static string ValidateProjectName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name != name.Trim() ||
                !Regex.IsMatch(name, @"^[\p{L}\p{N}_-]+$") ||
                Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])$", RegexOptions.IgnoreCase))
                throw new InvalidDataException("项目目录名只能包含文字、数字、下划线或短横线，且不能是系统保留名。");
            return name;
        }

        public static string ProjectRoot(string projectName)
        {
            return UiStageWorkspace.Root + "/" + ValidateProjectName(projectName);
        }

        public static string StagingRoot(string projectName, string panelName)
        {
            return string.IsNullOrWhiteSpace(projectName) ? UiStageWorkspace.Root + "/" + panelName : ProjectRoot(projectName) + "/" + panelName;
        }

        private static string SourceDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("请选择存在的项目源目录。");
            return Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        private static void ValidateSource(string directory, string file)
        {
            var full = Path.GetFullPath(file);
            if (!full.StartsWith(directory, StringComparison.OrdinalIgnoreCase) || Path.GetFileName(full) != "panel.package.json")
                throw new InvalidDataException("面板准备包必须位于所选项目目录内：" + file);
            for (var parent = new DirectoryInfo(Path.GetDirectoryName(full)); parent != null && parent.FullName.Length >= directory.TrimEnd(Path.DirectorySeparatorChar).Length; parent = parent.Parent)
                if ((parent.Attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("项目扫描不跟随目录链接：" + parent.FullName);
            if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("准备包不能是文件链接：" + file);
        }

        // Scan does not require a configured font and never creates Unity assets.
        public static UiProjectScan Scan(string directory)
        {
            directory = SourceDirectory(directory);
            var result = new UiProjectScan { directory = directory };
            var pending = new Stack<string>(); pending.Push(directory);
            while (pending.Count > 0)
            {
                var folder = pending.Pop();
                try
                {
                    foreach (var child in Directory.GetDirectories(folder).OrderBy(p => p, StringComparer.Ordinal))
                    {
                        if (Excluded.Contains(Path.GetFileName(child))) continue;
                        if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) { result.warnings.Add("跳过目录链接：" + child); continue; }
                        pending.Push(child);
                    }
                    foreach (var file in Directory.GetFiles(folder, "panel.package.json", SearchOption.TopDirectoryOnly))
                    {
                        var source = new UiProjectSource { packagePath = Path.GetFullPath(file) };
                        try
                        {
                            ValidateSource(directory, file);
                            var package = UnityEngine.JsonUtility.FromJson<UiPanelPackage>(File.ReadAllText(file));
                            if (package == null || package.schemaVersion != 1 || string.IsNullOrWhiteSpace(package.panelName))
                                throw new InvalidDataException("缺少有效的面板名或schemaVersion。");
                            source.panelName = package.panelName;
                            if (string.IsNullOrWhiteSpace(package.html) || Path.IsPathRooted(package.html) || package.html.Replace('\\', '/').Split('/').Contains(".."))
                                throw new InvalidDataException("html必须是准备包目录内的相对路径。");
                            if (!File.Exists(Path.Combine(folder, package.html))) throw new FileNotFoundException("找不到HTML：" + package.html);
                        }
                        catch (Exception error) { source.error = error.Message; }
                        result.sources.Add(source);
                    }
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                { result.warnings.Add(folder + ": " + error.Message); }
            }
            result.sources = result.sources.OrderBy(p => p.packagePath, StringComparer.Ordinal).ToList();
            return result;
        }

        private static UiImportRequest Request(string packagePath, string projectName, bool replace, string directory)
        {
            var request = HtmlUiImporter.ReadPackage(packagePath);
            request.options.stagingRoot = StagingRoot(projectName, request.options.panelName);
            request.options.allowPrefabReplacement = replace;
            request.options.projectName = projectName;
            request.options.projectDirectory = directory;
            return request;
        }

        public static UiProjectImportPlan Plan(string directory, string projectName, IEnumerable<string> packages, bool replaceGeneratedPrefabs = false)
        {
            directory = SourceDirectory(directory); ValidateProjectName(projectName);
            var result = new UiProjectImportPlan { directory = directory, projectName = projectName, replaceGeneratedPrefabs = replaceGeneratedPrefabs };
            try { result.library = UiProjectAssets.Plan(directory, projectName); }
            // A shared-library failure blocks the entire batch. Do not retry it for every panel.
            catch (Exception error) { result.errors.Add(error.Message); return result; }
            foreach (var path in (packages ?? Enumerable.Empty<string>()).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.Ordinal))
            {
                var panel = new UiProjectPanelPlan { packagePath = path }; result.panels.Add(panel);
                try
                {
                    ValidateSource(directory, path);
                    panel.packageSha256 = HtmlUiImporter.Sha256(path);
                    var request = Request(path, projectName, replaceGeneratedPrefabs, directory);
                    request.options.library = result.library;
                    panel.panelName = request.options.panelName;
                    panel.import = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                    if (!replaceGeneratedPrefabs && panel.import.assets.Any(a => a.path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && a.action != "create"))
                        throw new InvalidDataException("已有暂存Prefab；如需重建，请明确勾选“替换已有暂存Prefab”后重新查看计划。");
                }
                catch (Exception error) { panel.error = error.Message; }
            }
            if (result.panels.Count == 0) result.errors.Add("请至少勾选一个面板。");
            foreach (var group in result.panels.Where(p => p.panelName != null).GroupBy(p => p.panelName, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
                result.errors.Add("同一项目内存在重复面板名：" + group.Key + "\n" + string.Join("\n", group.Select(p => p.packagePath)));
            var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in result.panels.Where(p => p.import != null).SelectMany(p => p.import.assets))
                if (!targets.Add(asset.path)) result.errors.Add("多个面板写入同一路径：" + asset.path);
            return result;
        }

        public static UiProjectImportResult Import(UiProjectImportPlan reviewedPlan, Func<int, int, bool> cancel = null, bool rebuildPreviewScene = true)
        {
            if (reviewedPlan == null) throw new ArgumentNullException("reviewedPlan");
            if (!ActiveProjects.Add(reviewedPlan.projectName)) throw new InvalidOperationException("This game's panel batch is already generating.");
            try { return ImportBatch(reviewedPlan, cancel, rebuildPreviewScene); }
            finally { ActiveProjects.Remove(reviewedPlan.projectName); }
        }

        private static UiProjectImportResult ImportBatch(UiProjectImportPlan reviewedPlan, Func<int, int, bool> cancel, bool rebuildPreviewScene)
        {
            UiPanelScenePlacement.ValidateContext();
            if (reviewedPlan == null || !reviewedPlan.valid) throw new InvalidDataException("先查看并解决项目导入计划中的错误。");
            // Preflight the whole selection before the first asset mutation.
            var current = Plan(reviewedPlan.directory, reviewedPlan.projectName, reviewedPlan.panels.Select(p => p.packagePath), reviewedPlan.replaceGeneratedPrefabs);
            if (!current.valid || current.panels.Count != reviewedPlan.panels.Count || current.panels.Zip(reviewedPlan.panels,
                (a, b) => a.packagePath == b.packagePath && a.packageSha256 == b.packageSha256 && HtmlUiImporter.PlanMatches(b.import, a.import)).Any(v => !v))
                throw new InvalidDataException("项目导入计划已过期；来源、配置或目标资源发生变化，请重新查看计划。");
            if (current.library.signature != reviewedPlan.library.signature) throw new InvalidDataException("Project asset plan changed. Inspect the plan again.");
            var result = new UiProjectImportResult { projectName = reviewedPlan.projectName, status = "completed" };
            result.panels.AddRange(reviewedPlan.panels.Select(p => new UiProjectPanelResult { packagePath = p.packagePath, panelName = p.panelName }));
            if (cancel != null && cancel(0, reviewedPlan.panels.Count)) { result.status = "cancelled"; return result; }
            UiProjectAssets.Sync(current.library);
            var assetSnapshot = UiProjectAssets.Freeze(current.library);
            result.assetManifestHash = current.library.manifestHash;
            result.assetWarnings = current.library.warnings;
            for (var index = 0; index < reviewedPlan.panels.Count; index++)
            {
                var panel = reviewedPlan.panels[index]; var entry = result.panels[index];
                try
                {
                    if (cancel != null && cancel(index, reviewedPlan.panels.Count)) { result.status = "cancelled"; break; }
                    UiProjectAssets.RequireFrozen(assetSnapshot);
                    if (HtmlUiImporter.Sha256(panel.packagePath) != panel.packageSha256) throw new InvalidDataException("准备包在批次执行期间发生变化。");
                    var request = Request(panel.packagePath, reviewedPlan.projectName, reviewedPlan.replaceGeneratedPrefabs, reviewedPlan.directory);
                    request.options.libraryAlreadySynced = true;
                    request.options.library = current.library;
                    var panelPlan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                    if (panelPlan.configurationSha256 != panel.import.configurationSha256 || panelPlan.inputsSha256.Count != panel.import.inputsSha256.Count ||
                        panel.import.inputsSha256.Any(p => !panelPlan.inputsSha256.ContainsKey(p.Key) || panelPlan.inputsSha256[p.Key] != p.Value) ||
                        panelPlan.library.manifestHash != current.library.manifestHash)
                        throw new InvalidDataException("Panel or asset inputs changed during project generation.");
                    entry.prefabPath = HtmlUiImporter.Import(request.htmlPath, request.outputFolder, request.options, panelPlan);
                    entry.status = "imported";
                }
                catch (Exception error)
                {
                    entry.status = "failed"; entry.error = error.ToString();
                    result.status = result.panels.Any(p => p.status == "imported") ? "partial" : "failed";
                    break;
                }
            }
            var last = result.panels.LastOrDefault(p => p.status == "imported");
            if (rebuildPreviewScene && last != null)
                try { UiStageWorkspace.RebuildScene(last.prefabPath); }
                catch (Exception error) { result.previewError = error.Message; }
            try
            {
                var folder = ProjectRoot(reviewedPlan.projectName) + "/Reports";
                UiStageWorkspace.EnsureFolder(folder);
                result.reportPath = folder + "/project-import-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json";
                File.WriteAllText(result.reportPath, JsonConvert.SerializeObject(result, Formatting.Indented));
                AssetDatabase.ImportAsset(result.reportPath);
            }
            catch (Exception error) { result.reportError = error.Message; }
            return result;
        }
    }
}
#endif
