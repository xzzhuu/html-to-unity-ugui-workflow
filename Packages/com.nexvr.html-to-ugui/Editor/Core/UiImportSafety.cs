#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    [Serializable] public sealed class UiPlannedAsset
    {
        public string path, action, source;
        public List<string> consumers = new List<string>();
    }
    [Serializable] public sealed class UiImportPlan
    {
        public string html, outputFolder, assetPrefix;
        public List<UiPlannedAsset> assets = new List<UiPlannedAsset>();
        public List<string> warnings = new List<string>();
        public Dictionary<string, string> inputsSha256 = new Dictionary<string, string>();
        public object configuration;
        public string configurationSha256;
        public string assetDependencyHash;
        public double planningMilliseconds;
    }
    public static partial class HtmlUiImporter
    {
        internal static readonly string[] StateSpriteAttributes = { "data-normal-sprite", "data-highlighted-sprite", "data-pressed-sprite", "data-selected-sprite", "data-disabled-sprite" };
        internal static readonly string[] StateColorAttributes = { "data-normal-color", "data-highlighted-color", "data-pressed-color", "data-selected-color", "data-disabled-color" };
        private static bool importing;
        private static string consumersCacheKey;
        private static Dictionary<string, List<string>> consumersCache;

        private static ImportOptions EffectiveOptions(ImportOptions source, ref string outputFolder)
        {
            source = source ?? new ImportOptions();
            var settings = new ImportOptions { panelName = source.panelName, font = source.font, fontMaterial = source.fontMaterial,
                boldWeight = source.boldWeight, configureRoot = source.configureRoot, configureTemplate = source.configureTemplate,
                allowPrefabReplacement = source.allowPrefabReplacement, requireSkillApproval = source.requireSkillApproval, stagingRoot = source.stagingRoot,
                assetPrefix = (source.assetPrefix ?? "Assets/Art/UI/Sprite/{PanelName}/").Replace("{PanelName}", source.panelName).TrimEnd('/') + "/" };
            settings.sourceAssetPrefix = settings.assetPrefix;
            if (settings.font == null) settings.font = TMP_Settings.defaultFontAsset;
            if (settings.fontMaterial == null && settings.font != null) settings.fontMaterial = settings.font.material;
            outputFolder = (outputFolder ?? "Assets/Art/UIPrefab/{PanelName}").Replace("{PanelName}", source.panelName).TrimEnd('/');
            if (!string.IsNullOrEmpty(settings.stagingRoot))
            {
                settings.stagingRoot = settings.stagingRoot.Replace("{PanelName}", settings.panelName);
                ValidateAssetPath(settings.stagingRoot.TrimEnd('/') + "/dummy.png");
                outputFolder = settings.stagingRoot.TrimEnd('/') + "/Prefabs";
                settings.assetPrefix = settings.stagingRoot.TrimEnd('/') + "/Sprites/";
            }
            return settings;
        }

        private static void RemapStagedAssets(XDocument document, ImportOptions settings)
        {
            if (settings.sourceAssetPrefix == settings.assetPrefix) return;
            foreach (var attribute in document.Descendants().Attributes().Where(a =>
                new[] { "data-sprite", "data-texture" }.Concat(StateSpriteAttributes).Contains(a.Name.LocalName)))
            {
                if (!attribute.Value.StartsWith(settings.sourceAssetPrefix, StringComparison.Ordinal))
                    throw new InvalidDataException("Staging requires every image to use the source asset prefix.");
                attribute.Value = settings.assetPrefix + attribute.Value.Substring(settings.sourceAssetPrefix.Length);
            }
        }

        public static UiImportPlan PlanImport(string htmlPath, string outputFolder, ImportOptions settings = null)
        {
            var timer = Stopwatch.StartNew();
            settings = EffectiveOptions(settings, ref outputFolder);
            if (settings.requireSkillApproval) ValidateSkillApproval(htmlPath);
            if (!System.Text.RegularExpressions.Regex.IsMatch(settings.panelName, @"^(?:Panel|[A-Z][A-Za-z0-9]*Panel)$"))
                throw new InvalidDataException("panelName must use PascalCase and end with Panel.");
            ValidateAssetPath(outputFolder + "/dummy.prefab");
            ValidateAssetPath(settings.assetPrefix + "dummy.png");
            var doc = XDocument.Load(htmlPath);
            RemapStagedAssets(doc, settings);
            CssRules.Clear(); CssVariables.Clear(); OrderedRules.Clear(); CssWarnings.Clear();
            var dir = Path.GetDirectoryName(Path.GetFullPath(htmlPath));
            LoadLinkedCss(doc, dir);
            var report = UiPackageValidator.Validate(doc, dir, settings.assetPrefix, ResolveStyle);
            if (report.errors.Count > 0) throw new InvalidDataException(string.Join("\n", report.errors));
            ValidateContentRects(dir);
            var plan = new UiImportPlan { html = Path.GetFullPath(htmlPath), outputFolder = outputFolder, assetPrefix = settings.assetPrefix };
            plan.configuration = new { settings.panelName, settings.assetPrefix, outputFolder, settings.stagingRoot,
                settings.allowPrefabReplacement, settings.requireSkillApproval, settings.boldWeight,
                font = AssetDatabase.GetAssetPath(settings.font != null ? settings.font : TMP_Settings.defaultFontAsset),
                fontMaterial = AssetDatabase.GetAssetPath(settings.fontMaterial) };
            plan.configurationSha256 = ConfigurationDigest(plan.configuration);
            plan.assetDependencyHash = AssetDatabase.GlobalArtifactDependencyVersion.ToString();
            plan.inputsSha256 = CollectInputFingerprints(dir);
            plan.warnings.AddRange(report.warnings.Concat(CssWarnings));
            Action<string, string> add = (path, source) =>
            {
                ValidateAssetPath(path);
                if (plan.assets.Any(a => a.path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;
                var exists = File.Exists(path);
                var identical = source != null && exists && File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(source));
                plan.assets.Add(new UiPlannedAsset { path = path, source = source, action = !exists ? "create" : identical ? "reuse/check-import-settings" : "overwrite" });
            };
            add(outputFolder + "/" + settings.panelName + ".prefab", null);
            add(outputFolder + "/" + settings.panelName + ".import-report.json", null);
            foreach (var template in doc.Descendants().Where(e => e.Name.LocalName == "template"))
            {
                var child = template.Elements().Single();
                var name = (string)child.Attribute("data-ui-name") ?? "Item_" + HtmlToUGUI.UiObjectNaming.Pascal((string)template.Attribute("data-prefab"));
                add(outputFolder + "/Items/" + name + ".prefab", null);
            }
            foreach (var attr in doc.Descendants().Attributes().Where(a => new[] { "data-sprite", "data-texture" }.Concat(StateSpriteAttributes).Contains(a.Name.LocalName)))
            {
                var source = Path.Combine(dir, "assets", attr.Value.Substring(settings.assetPrefix.Length));
                add(attr.Value, File.Exists(source) ? source : null);
            }
            var targets = new HashSet<string>(plan.assets.Where(a => a.action != "create").Select(a => a.path));
            if (targets.Count > 0)
            {
                var cacheKey = plan.assetDependencyHash + "|" + string.Join("|", targets.OrderBy(p => p, StringComparer.Ordinal));
                if (consumersCacheKey != cacheKey)
                {
                    consumersCache = targets.ToDictionary(p => p, p => new List<string>());
                    foreach (var candidate in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") && !AssetDatabase.IsValidFolder(p)))
                        foreach (var dependency in AssetDatabase.GetDependencies(candidate, true).Where(p => p != candidate && targets.Contains(p)))
                            consumersCache[dependency].Add(candidate);
                    consumersCacheKey = cacheKey;
                }
                foreach (var asset in plan.assets.Where(a => targets.Contains(a.path))) asset.consumers.AddRange(consumersCache[asset.path]);
            }
            if (plan.assets.Any(a => a.path.EndsWith(".prefab") && a.action != "create"))
                plan.warnings.Add("Existing prefabs are regenerated, not patched. GUID retention does not preserve internal references or UnityEvents; use a staging root for integrated panels.");
            plan.planningMilliseconds = timer.Elapsed.TotalMilliseconds;
            return plan;
        }

        private static string ConfigurationDigest(object value)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)))).Replace("-", "").ToLowerInvariant();
        }

        public static bool PlanMatches(UiImportPlan plan, UiImportPlan current)
        {
            return plan != null && plan.html == current.html && plan.configurationSha256 == current.configurationSha256 &&
                plan.inputsSha256.Count == current.inputsSha256.Count && plan.inputsSha256.All(p => current.inputsSha256.ContainsKey(p.Key) && current.inputsSha256[p.Key] == p.Value) &&
                plan.assets.Count == current.assets.Count && plan.assets.Zip(current.assets, (a, b) => a.path == b.path && a.action == b.action && a.consumers.SequenceEqual(b.consumers)).All(v => v);
        }

        public static string Import(string htmlPath, string outputFolder, ImportOptions settings = null, UiImportPlan reviewedPlan = null)
        {
            if (importing) throw new InvalidOperationException("Nested imports are unsupported.");
            var effective = EffectiveOptions(settings, ref outputFolder);
            var plan = PlanImport(htmlPath, outputFolder, settings);
            if (reviewedPlan != null && !PlanMatches(reviewedPlan, plan))
                throw new InvalidDataException("Reviewed plan is stale. Inspect the current plan before importing.");
            if (!effective.allowPrefabReplacement && plan.assets.Any(a => a.path.EndsWith(".prefab") && a.action != "create"))
                throw new InvalidDataException("Existing Prefab replacement is disabled. Use stagingRoot or explicitly enable allowPrefabReplacement for disposable generated prefabs.");
            using (var transaction = new UiImportTransaction(plan))
            {
                importing = true;
                try
                {
                    string path;
                    using (var sceneScope = new UiTemporarySceneScope())
                        path = ImportCore(htmlPath, outputFolder, effective);
                    if (effective.requireSkillApproval) ValidateSkillApproval(htmlPath);
                    WriteImportEvidence(path, plan, effective);
                    transaction.Commit();
                    return path;
                }
                catch (Exception original)
                {
                    var recovery = transaction.Rollback();
                    try { WriteFailureEvidence(plan, original, recovery); }
                    catch (Exception reporting) { recovery.Add(new IOException("Could not save import failure evidence.", reporting)); }
                    if (recovery.Count > 0) throw new AggregateException("Import failed and recovery was incomplete; inspect each path.", new[] { original }.Concat(recovery));
                    throw;
                }
                finally { importing = false; }
            }
        }

        private static void ApplySelectableState(Selectable control, XElement element, Dictionary<string, string> style)
        {
            Func<string, Sprite> sprite = key => element.Attribute(key) == null ? null : AssetDatabase.LoadAssetAtPath<Sprite>((string)element.Attribute(key));
            if (StateSpriteAttributes.Any(key => element.Attribute(key) != null))
            {
                var target = control.targetGraphic as Image;
                if (target == null) throw new InvalidDataException("SpriteSwap requires an Image targetGraphic.");
                target.type = (string)element.Attribute("data-nine-slice") == "true" ? Image.Type.Sliced : Image.Type.Simple;
                if (element.Attribute("data-normal-sprite") != null)
                { target.sprite = sprite("data-normal-sprite"); target.color = ReadBackgroundColor(style, Color.white); }
                control.transition = Selectable.Transition.SpriteSwap;
                control.spriteState = new SpriteState { highlightedSprite = sprite("data-highlighted-sprite"), pressedSprite = sprite("data-pressed-sprite"),
                    selectedSprite = sprite("data-selected-sprite"), disabledSprite = sprite("data-disabled-sprite") };
            }
            else if (StateColorAttributes.Any(key => element.Attribute(key) != null))
            {
                var colors = control.colors;
                Func<string, Color, Color> color = (key, fallback) => element.Attribute(key) == null ? fallback : ParseColor((string)element.Attribute(key), fallback);
                colors.normalColor = color("data-normal-color", colors.normalColor);
                colors.highlightedColor = color("data-highlighted-color", colors.highlightedColor);
                colors.pressedColor = color("data-pressed-color", colors.pressedColor);
                colors.selectedColor = color("data-selected-color", colors.selectedColor);
                colors.disabledColor = color("data-disabled-color", colors.disabledColor);
                control.colors = colors;
                control.transition = Selectable.Transition.ColorTint;
            }
        }

        private static void ValidatePlatformOverrides(TextureImporter importer, int requiredMax)
        {
            foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL", "Windows Store Apps", "PS4", "PS5", "XboxOne", "Nintendo Switch" })
            {
                var setting = importer.GetPlatformTextureSettings(platform);
                if (setting.overridden && (setting.maxTextureSize < requiredMax || setting.textureCompression != TextureImporterCompression.Uncompressed ||
                    (setting.format != TextureImporterFormat.Automatic && setting.format != TextureImporterFormat.RGBA32 && setting.format != TextureImporterFormat.RGB24)))
                    throw new InvalidDataException(importer.assetPath + ": platform override may reduce quality (" + platform + "); review or disable that override before import.");
            }
        }
    }

    // HideAndDontSave prevents transient creation from dirtying the active scene.
    // Move into a preview scene before clearing flags and saving generated prefabs.
    internal sealed class UiTemporarySceneScope : IDisposable
    {
        private static UnityEngine.SceneManagement.Scene current;
        private readonly UnityEngine.SceneManagement.Scene previous;
        private readonly UnityEngine.SceneManagement.Scene scratch;
        internal UiTemporarySceneScope()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Import/audit requires Edit Mode.");
            previous = current;
            scratch = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            current = scratch;
        }
        internal static GameObject CreateGameObject(string name, params Type[] components)
        {
            if (!current.IsValid()) throw new InvalidOperationException("Transient UI objects require a preview scene scope.");
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, components);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, current);
            go.hideFlags = HideFlags.None;
            foreach (var component in go.GetComponents<Component>()) component.hideFlags = HideFlags.None;
            return go;
        }
        public void Dispose()
        {
            current = previous;
            if (scratch.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scratch);
        }
    }

    // Captures every planned resource and its meta before the first write. The importer never
    // touches a shared font material. Callbacks must limit mutation to the generated objects.
    internal sealed class UiImportTransaction : IDisposable
    {
        private readonly Dictionary<string, byte[]> snapshots = new Dictionary<string, byte[]>();
        private readonly List<string> newFolders = new List<string>();
        private bool committed;
        private bool restored;
        internal UiImportTransaction(UiImportPlan plan)
        {
            foreach (var asset in plan.assets)
            {
                foreach (var path in new[] { asset.path, asset.path + ".meta" })
                    snapshots[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
                var dir = Path.GetDirectoryName(asset.path).Replace('\\', '/');
                while (dir != "Assets" && !Directory.Exists(dir))
                {
                    if (!newFolders.Contains(dir)) newFolders.Add(dir);
                    dir = Path.GetDirectoryName(dir).Replace('\\', '/');
                }
            }
        }
        internal void Commit() { committed = true; }
        internal List<Exception> Rollback()
        {
            var errors = new List<Exception>();
            if (committed || restored) return errors;
            restored = true;
            foreach (var entry in snapshots)
            {
                try
                {
                    if (entry.Value != null) { Directory.CreateDirectory(Path.GetDirectoryName(entry.Key)); File.WriteAllBytes(entry.Key, entry.Value); }
                    else if (File.Exists(entry.Key)) File.Delete(entry.Key);
                }
                catch (Exception error) { errors.Add(new IOException("Recovery failed: " + entry.Key, error)); }
            }
            foreach (var dir in newFolders.OrderByDescending(d => d.Length))
            {
                try
                {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                { Directory.Delete(dir); if (File.Exists(dir + ".meta")) File.Delete(dir + ".meta"); }
                }
                catch (Exception error) { errors.Add(new IOException("Recovery failed: " + dir, error)); }
            }
            try { AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate); }
            catch (Exception error) { errors.Add(new IOException("Recovery refresh failed", error)); }
            return errors;
        }
        public void Dispose() { Rollback(); }
    }
}
#endif
