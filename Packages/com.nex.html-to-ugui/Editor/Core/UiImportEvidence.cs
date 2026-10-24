#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    public static partial class HtmlUiImporter
    {
        internal static string Sha256(string path)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        private static string WorkspaceFile(string root, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("Expected a workspace-relative file.");
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidDataException("Missing/outside workspace evidence: " + relative);
            return path;
        }
        private static JObject JsonFile(string path) { return JObject.Parse(File.ReadAllText(path)); }
        private static void Require(bool passed, string message) { if (!passed) throw new InvalidDataException(message); }
        private static Dictionary<string, string> CollectInputFingerprints(string htmlDirectory,string projectDirectory=null)
        {
            var files = new HashSet<string>(Directory.GetFiles(htmlDirectory, "*", SearchOption.AllDirectories).Select(Path.GetFullPath));
            var root = Directory.GetParent(htmlDirectory).FullName;
            var packagePath = Path.Combine(htmlDirectory, "panel.package.json");
            if (string.IsNullOrEmpty(projectDirectory) && File.Exists(packagePath))
            {
                var package = JsonFile(packagePath);
                if (!string.IsNullOrEmpty((string)package["projectName"]))
                {
                    projectDirectory = string.IsNullOrEmpty((string)package["projectDirectory"])?UiProjectAssets.FindDirectory(Path.Combine(htmlDirectory,(string)package["html"])):
                        Path.GetFullPath(Path.Combine(htmlDirectory,(string)package["projectDirectory"]));
                }
            }
            if(!string.IsNullOrEmpty(projectDirectory))foreach(var contract in UiNativeAssembly.ContractFiles(projectDirectory))files.Add(contract);
            foreach (var source in new[] { "design/element-groups.json", "design/review/approval.json", "design/review/element-groups-approval.json", "design/review/html-acceptance.json", "design/review/layout-approved.png" })
            {
                var path = Path.GetFullPath(Path.Combine(root, source));
                if (File.Exists(path)) files.Add(path);
            }
            var definitions = Path.Combine(root, "design/element-groups.json");
            if (File.Exists(definitions))
            {
                var manifest = JsonFile(definitions);
                foreach (var source in (manifest["groups"] ?? new JArray()).Select(g => (string)g["sheet"]).Concat((manifest["panelPreviews"] ?? new JArray()).Select(p => (string)p)))
                    files.Add(WorkspaceFile(root, source));
            }
            return files.OrderBy(p => p, StringComparer.Ordinal).ToDictionary(p => p, Sha256);
        }
        private static JToken CanonicalOrder(JToken token)
        {
            var obj = token as JObject;
            if (obj != null) return new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, CanonicalOrder(p.Value))));
            var array = token as JArray;
            return array != null ? new JArray(array.Select(CanonicalOrder)) : token.DeepClone();
        }
        private static string DefinitionSha(JToken group, JToken sprites)
        {
            var selected = group["assets"].Select(path => sprites.Single(sprite => (string)sprite["source"] == (string)path)).OrderBy(sprite => (string)sprite["source"], StringComparer.Ordinal);
            var json = CanonicalOrder(new JObject { ["group"] = group.DeepClone(), ["sprites"] = new JArray(selected) }).ToString(Formatting.None);
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
        }

        internal static void ValidateContentRects(string htmlDirectory)
        {
            foreach (var path in new[] { Path.Combine(htmlDirectory, "sprites.json"), Path.Combine(htmlDirectory, "../design/element-groups.json") })
            {
                if (!File.Exists(path)) continue;
                foreach (var item in JsonFile(path)["sprites"] ?? new JArray())
                {
                    var rect = item["contentRect"];
                    if (rect == null) continue;
                    var size = item["pixelSize"];
                    Require(rect is JArray && rect.Count() == 4 && rect.All(v => v.Type == JTokenType.Integer) && size is JArray && size.Count() == 2,
                        "contentRect requires integer pixel x,y,width,height and pixelSize.");
                    var x = (long)rect[0]; var y = (long)rect[1]; var w = (long)rect[2]; var h = (long)rect[3];
                    Require(x >= 0 && y >= 0 && w > 0 && h > 0 && x <= (long)size[0] - w && y <= (long)size[1] - h, "contentRect outside texture bounds.");
                    Require(x == 0 && y == 0 && w == (long)size[0] && h == (long)size[1],
                        "Padded contentRect is unsupported: re-export a full-texture asset or adapt manually before conversion.");
                }
            }
        }

        public static void ValidateSkillApproval(string htmlPath)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
            var root = Directory.GetParent(dir).FullName;
            var state = JsonFile(WorkspaceFile(root, ".html-directory.json"));
            Require(((string)state["mode"] ?? "full") == "full" && (string)state["status"] == "ready", "Skill workspace must be full and ready.");
            Require((string)state["panelName"] + ".html" == Path.GetFileName(htmlPath), "Skill panel identity mismatch.");
            var package = JsonFile(Path.Combine(dir, "panel.package.json"));
            Require((bool?)package["requireSkillApproval"] == true, "Skill ready package requires requireSkillApproval=true.");
            var staging = (string)package["stagingRoot"];
            Require(!string.IsNullOrEmpty(staging), "Skill ready package requires stagingRoot.");
            ValidateAssetPath(staging.Replace("{PanelName}", (string)state["panelName"]).TrimEnd('/') + "/dummy.prefab");
            var approval = JsonFile(WorkspaceFile(root, "design/review/approval.json"));
            var groupApprovalPath = WorkspaceFile(root, "design/review/element-groups-approval.json");
            var groups = JsonFile(groupApprovalPath);
            var acceptance = JsonFile(WorkspaceFile(root, "design/review/html-acceptance.json"));
            Require((string)approval["status"] == "approved" && !string.IsNullOrWhiteSpace((string)approval["userApproval"]) &&
                (string)approval["imageSha256"] == Sha256(WorkspaceFile(root, "design/review/layout-approved.png")), "Missing/stale panel approval.");
            Require((string)groups["status"] == "approved" && (string)acceptance["status"] == "passed" &&
                (string)acceptance["approvedLayoutSha256"] == (string)approval["imageSha256"] &&
                (string)acceptance["approvedElementGroupsSha256"] == Sha256(groupApprovalPath), "Missing/stale element/HTML acceptance.");
            foreach (var check in new[] { "elements", "naming", "sprites", "layout", "visual", "interactions" })
                Require((string)acceptance["checks"]?[check] == "passed", "HTML acceptance check incomplete: " + check);
            var fingerprints = acceptance["filesSha256"] as JObject;
            Require(fingerprints != null, "Missing HTML file fingerprints.");
            var actual = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Select(p => "html/" + p.Substring(dir.Length + 1).Replace('\\', '/')).ToArray();
            Require(new HashSet<string>(actual).SetEquals(fingerprints.Properties().Select(p => p.Name).Where(p => p.StartsWith("html/"))), "HTML acceptance file set changed.");
            foreach (var name in actual.Concat(new[] { "design/element-groups.json" }))
                Require((string)fingerprints[name] == Sha256(WorkspaceFile(root, name)), "HTML acceptance stale: " + name);
            var definitions = JsonFile(WorkspaceFile(root, "design/element-groups.json"));
            var previews = definitions["panelPreviews"] as JArray ?? new JArray("design/review/layout-approved.png");
            Require(previews.Count > 0 && previews.Select(p => (string)p).Distinct().Count() == previews.Count, "Invalid panel preview list.");
            var previewApprovals = approval["previews"] as JArray ?? new JArray();
            Require(previewApprovals.Select(p => (string)p["source"]).Distinct().Count() == previewApprovals.Count, "Duplicate preview approval.");
            foreach (var source in previews.Select(p => (string)p).Where(p => p != "design/review/layout-approved.png"))
            {
                var matches = previewApprovals.Where(p => (string)p["source"] == source).ToArray();
                Require(matches.Length == 1 && !string.IsNullOrWhiteSpace((string)matches[0]["userApproval"]) &&
                    (string)matches[0]["imageSha256"] == Sha256(WorkspaceFile(root, source)), "Missing/stale individual preview approval: " + source);
            }
            var entries = groups["groups"] as JArray;
            Require(entries != null && definitions["groups"] is JArray && entries.Count == definitions["groups"].Count(), "Element approval coverage mismatch.");
            foreach (var group in definitions["groups"])
            {
                var matches = entries.Where(g => (string)g["groupId"] == (string)group["groupId"]).ToArray();
                Require(matches.Length == 1, "Missing/duplicate element approval.");
                var entry = matches[0];
                Require((string)entry["definitionSha256"] == DefinitionSha(group, definitions["sprites"]), "Element definition approval stale (use integer pixel metadata for portable canonical hashing).");
                Require(!string.IsNullOrWhiteSpace((string)entry["userApproval"]) && (string)entry["imageSha256"] == Sha256(WorkspaceFile(root, (string)group["sheet"])), "Element sheet approval stale.");
                foreach (var asset in group["assets"] ?? new JArray())
                    Require((string)entry["filesSha256"]?[(string)asset] == Sha256(WorkspaceFile(root, (string)asset)), "Element material approval stale.");
            }
            ValidateContentRects(dir);
        }

        private static void WriteImportEvidence(string prefabPath, UiImportPlan plan, ImportOptions settings)
        {
            var currentInputs = CollectInputFingerprints(Path.GetDirectoryName(plan.html),settings.projectDirectory);
            Require(plan.inputsSha256.Count == currentInputs.Count && plan.inputsSha256.All(p => currentInputs.ContainsKey(p.Key) && currentInputs[p.Key] == p.Value),
                "Inputs changed during conversion; outputs have been rolled back.");
            var reportPath = Path.ChangeExtension(prefabPath, ".import-report.json");
            var report = JsonFile(reportPath);
            report["converterVersion"] = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HtmlUiImporter).Assembly).version;
            report["sourceHtml"] = plan.html;
            report["skillApprovalVerified"] = settings.requireSkillApproval;
            report["configuration"] = JObject.FromObject(new { settings.panelName, settings.assetPrefix, plan.outputFolder,
                settings.stagingRoot, settings.allowPrefabReplacement, settings.requireSkillApproval, settings.boldWeight,
                nativeAssembly = settings.nativeAssembly,
                font = AssetDatabase.GetAssetPath(settings.font), fontMaterial = AssetDatabase.GetAssetPath(settings.fontMaterial) });
            using (var hash = SHA256.Create()) report["configurationSha256"] = BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(report["configuration"].ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
            var inputs = new JObject();
            var dir = Path.GetDirectoryName(plan.html);
            foreach (var pair in plan.inputsSha256)
                inputs[Uri.UnescapeDataString(new Uri(dir + Path.DirectorySeparatorChar).MakeRelativeUri(new Uri(pair.Key)).ToString())] = pair.Value;
            report["inputsSha256"] = inputs;
            var artifacts = new JArray();
            foreach (var asset in plan.assets.Where(a => a.path != reportPath && File.Exists(a.path)))
                artifacts.Add(JObject.FromObject(new { asset.path, asset.action, asset.consumers, guid = AssetDatabase.AssetPathToGUID(asset.path),
                    sha256 = Sha256(asset.path), metaSha256 = File.Exists(asset.path + ".meta") ? Sha256(asset.path + ".meta") : null }));
            report["artifacts"] = artifacts;
            if (plan.library != null)
            {
                report["projectAssets"] = JObject.FromObject(new { plan.library.projectName, plan.library.manifestHash, plan.library.registryPath,
                    artifacts = plan.library.entries.Select(e => new { e.definition.assetId, e.canonicalAssetId, e.definition.componentType, e.definition.part, e.definition.state,
                        e.unityPath, guid = AssetDatabase.AssetPathToGUID(e.unityPath), sourceSha256 = e.sourceHash,
                        sha256 = Sha256(e.unityPath), metaSha256 = Sha256(e.unityPath + ".meta") }), warnings = plan.library.warnings });
            }
            report["planWarnings"] = JArray.FromObject(plan.warnings);
            report["planningMilliseconds"] = plan.planningMilliseconds;
            report["rollbackScope"] = "planned resources and meta files; callbacks must only mutate generated objects; editor crash recovery is not provided";
            File.WriteAllText(reportPath, report.ToString(Formatting.Indented));
            AssetDatabase.ImportAsset(reportPath);
        }

        private static void WriteFailureEvidence(UiImportPlan plan, Exception error, List<Exception> recovery)
        {
            var directory = Path.Combine(Path.GetTempPath(), "HtmlUiImportFailures");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(new { status = "failed", source = plan.html,
                originalError = error.ToString(), recoveryStatus = recovery.Count == 0 ? "restored" : "incomplete",
                recoveryErrors = recovery.Select(e => e.ToString()).ToArray(), plannedResources = plan.assets.Select(a => a.path).ToArray()
            }, Formatting.Indented));
            UnityEngine.Debug.LogWarning("[HtmlUI] Import failure evidence: " + path);
        }
    }
}
#endif
