#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    [Serializable] public sealed class UiProjectAssetDefinition
    {
        public string assetId, componentType, part, styleId, state = "Default", source, sourceSha256, approvalVersion;
        public bool approved;
        public bool tintable;
        public float[] border = { 0, 0, 0, 0 };
        public float[] pivot = { .5f, .5f };
        public float pixelsPerUnit = 100;
        public bool sRGB = true;
        public string filter = "Bilinear";
    }
    [Serializable] public sealed class UiProjectAssetManifest
    {
        public int schemaVersion = 1;
        public List<UiProjectAssetDefinition> assets = new List<UiProjectAssetDefinition>();
    }
    [Serializable] public sealed class UiProjectAssetEntry
    {
        public UiProjectAssetDefinition definition;
        public string sourcePath, sourceHash, pixelHash, profileHash, canonicalAssetId, unityPath, guid;
        public int width, height;
        public List<string> consumers = new List<string>();
    }
    [Serializable] public sealed class UiProjectAssetPlan
    {
        public string directory, projectName, manifestHash, signature, registryPath, registryHash;
        public List<UiProjectAssetEntry> entries = new List<UiProjectAssetEntry>();
        public List<UiPlannedAsset> writes = new List<UiPlannedAsset>();
        public Dictionary<string, string> targets = new Dictionary<string, string>();
        public List<string> warnings = new List<string>();
        public Dictionary<string, string> migrations = new Dictionary<string, string>();
        public List<string> retiredPaths = new List<string>();
    }

    // Shared by both the single-panel and project import entry points. Planning is read-only.
    public static class UiProjectAssets
    {
        public const string ManifestName = "project.assets.json";
        private static readonly HashSet<string> Writing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> Prefixes = new Dictionary<string, string> {
            { "Button", "Btn" }, { "Toggle", "Tgl" }, { "Slider", "Sld" }, { "Scrollbar", "Sbr" },
            { "Dropdown", "Dpd" }, { "TMP_Dropdown", "Dpd" }, { "InputField", "Inp" }, { "TMP_InputField", "Inp" },
            { "ScrollRect", "Scr" }, { "Image", "Img" }, { "RawImage", "RawImg" }, { "Mask", "Msk" }
        };
        public static string FindDirectory(string htmlPath)
        {
            for (var dir = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(htmlPath))); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, ManifestName))) return dir.FullName;
            throw new InvalidDataException("Project imports require project.assets.json in the source project root.");
        }
        internal static string Hash(object value)
        {
            using (var hash = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)))).Replace("-", "").ToLowerInvariant();
        }
        private static string FileHash(string path) { return File.Exists(path) ? HtmlUiImporter.Sha256(path) : null; }
        private static void Token(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^[A-Za-z][A-Za-z0-9_]*$"))
                throw new InvalidDataException(label + " must be an English semantic identifier: " + value);
        }
        private static string Source(string directory, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Replace('\\', '/').Split('/').Any(s => s == ".." || s == "."))
                throw new InvalidDataException("Asset source must stay inside the source project: " + relative);
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var full = Path.GetFullPath(Path.Combine(root, relative));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(full)) throw new FileNotFoundException("Missing project asset: " + relative);
            for (var current = full; current.Length >= root.TrimEnd(Path.DirectorySeparatorChar).Length; current = Path.GetDirectoryName(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Asset sources cannot traverse links: " + relative);
                if (current == root.TrimEnd(Path.DirectorySeparatorChar)) break;
            }
            return full;
        }
        public static UiProjectAssetPlan Plan(string directory, string projectName)
        {
            var root = UiProjectImport.ProjectRoot(projectName);
            directory = Path.GetFullPath(directory);
            var expectedManifest = Path.Combine(directory, ManifestName);
            if (!File.Exists(expectedManifest))
                throw new FileNotFoundException("缺少项目素材清单：" + expectedManifest +
                    "\n项目模式需要在源根目录登记已确认素材（类型、部件、状态、源文件 SHA256 与批准版本）。旧工作区只有 sprites.json 时，请先迁移项目素材清单；不要用空清单或自动批准绕过检查。", expectedManifest);
            var manifestPath = Source(directory, ManifestName);
            var manifest = JsonConvert.DeserializeObject<UiProjectAssetManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.schemaVersion != 1 || manifest.assets == null) throw new InvalidDataException("Unsupported project asset manifest.");
            var result = new UiProjectAssetPlan { directory = directory, projectName = projectName, manifestHash = FileHash(manifestPath),
                registryPath = root + "/Reports/asset-registry.json" };
            result.registryHash = FileHash(result.registryPath);
            var old = File.Exists(result.registryPath) ? JsonConvert.DeserializeObject<UiProjectAssetPlan>(File.ReadAllText(result.registryPath)) : null;
            if (old != null)
                foreach (var path in old.entries.Select(e => e.unityPath).Concat(old.retiredPaths))
                {
                    UiStageWorkspace.ValidateAssetDestination(path);
                    if (!path.StartsWith(root + "/Sprites/", StringComparison.Ordinal) && !path.StartsWith(root + "/Textures/", StringComparison.Ordinal))
                        throw new InvalidDataException("Registry contains an asset outside this game: " + path);
                }
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in manifest.assets.OrderBy(a => a.assetId, StringComparer.Ordinal))
            {
                Token(item.assetId, "assetId");
                if (!ids.Add(item.assetId)) throw new InvalidDataException("Duplicate assetId: " + item.assetId);
                if (!item.approved) continue;
                Token(item.styleId, "styleId"); Token(item.part, "part"); Token(item.state, "state");
                if (!Prefixes.ContainsKey(item.componentType ?? "")) throw new InvalidDataException("Unsupported asset componentType: " + item.componentType);
                if (string.IsNullOrWhiteSpace(item.approvalVersion) || !Regex.IsMatch(item.sourceSha256 ?? "", "^[a-f0-9]{64}$"))
                    throw new InvalidDataException("Approved assets require approvalVersion and sourceSha256: " + item.assetId);
                var source = Source(directory, item.source);
                var priorIdentity = old == null ? null : old.entries.FirstOrDefault(e => e.definition.assetId == item.assetId);
                if (priorIdentity != null && (priorIdentity.definition.componentType != item.componentType || priorIdentity.definition.part != item.part || priorIdentity.definition.state != item.state))
                    throw new InvalidDataException("Changing an asset's component, part or state requires a new assetId: " + item.assetId);
                if (!source.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Use PNG for project elements: " + source);
                var entry = new UiProjectAssetEntry { definition = item, sourcePath = source, sourceHash = FileHash(source) };
                if (entry.sourceHash != item.sourceSha256) throw new InvalidDataException("Approved asset changed: " + item.assetId);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(source))) throw new InvalidDataException("Invalid PNG: " + source);
                    entry.width = texture.width; entry.height = texture.height;
                    if (!Mathf.IsPowerOfTwo(entry.width) || !Mathf.IsPowerOfTwo(entry.height) || Math.Max(entry.width, entry.height) > 16384)
                        throw new InvalidDataException("Project element dimensions must be POT and <=16384: " + item.assetId);
                    using (var hash = System.Security.Cryptography.SHA256.Create())
                    {
                        var colors = texture.GetPixels32(); var bytes = new byte[colors.Length * 4];
                        if (item.tintable && colors.Any(c => c.a != 0 && (c.r != c.g || c.g != c.b)))
                            throw new InvalidDataException("Tintable project elements must use a neutral RGB mask; export one approved neutral source: " + item.assetId);
                        for (int i = 0; i < colors.Length; i++) { bytes[i * 4] = colors[i].r; bytes[i * 4 + 1] = colors[i].g; bytes[i * 4 + 2] = colors[i].b; bytes[i * 4 + 3] = colors[i].a; }
                        entry.pixelHash = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
                if (item.border == null || item.border.Length != 4 || item.border.Any(v => v < 0 || float.IsNaN(v) || float.IsInfinity(v)) ||
                    item.border[0] + item.border[2] >= entry.width || item.border[1] + item.border[3] >= entry.height ||
                    item.pivot == null || item.pivot.Length != 2 || item.pivot.Any(v => v < 0 || v > 1 || float.IsNaN(v)) ||
                    !(item.pixelsPerUnit > 0) || float.IsInfinity(item.pixelsPerUnit) || !new[] { "Point", "Bilinear" }.Contains(item.filter))
                    throw new InvalidDataException("Invalid Sprite profile: " + item.assetId);
                entry.profileHash = Hash(new { item.border, item.pivot, item.pixelsPerUnit, item.sRGB, item.filter, entry.width, entry.height,
                    compression = "Uncompressed", mesh = "FullRect", wrap = "Clamp", mipmaps = false });
                result.entries.Add(entry);
            }
            foreach (var style in result.entries.Where(e => e.definition.componentType == "Toggle" && e.definition.part == "Surface").GroupBy(e => e.definition.styleId))
            {
                if (!new[] { "Off", "On", "Disabled" }.All(state => style.Any(e => e.definition.state == state)) || style.Any(e => !new[] { "Off", "On", "Disabled" }.Contains(e.definition.state)))
                    throw new InvalidDataException("Toggle Surface style requires Off, On and Disabled: " + style.Key);
                if (style.Select(e => Hash(new { e.width, e.height, e.definition.border, e.definition.pivot, e.definition.pixelsPerUnit })).Distinct().Count() != 1)
                    throw new InvalidDataException("Toggle state surfaces must share canvas, border, pivot and PPU: " + style.Key);
            }
            // A state's identity is part of the key: Toggle Off/On/Disabled can never collapse.
            foreach (var group in result.entries.GroupBy(e => Hash(new { e.definition.componentType, e.definition.part, e.definition.state, e.profileHash, e.pixelHash })))
            {
                var canonical = group.OrderBy(e => e.definition.assetId, StringComparer.Ordinal).First();
                if (old != null)
                {
                    var existing = group.Where(e => old.entries.Any(o => o.definition.assetId == e.definition.assetId &&
                        o.canonicalAssetId == e.definition.assetId && o.profileHash == e.profileHash && File.Exists(o.unityPath)))
                        .OrderBy(e => e.definition.assetId, StringComparer.Ordinal).FirstOrDefault();
                    if (existing != null) canonical = existing;
                }
                var definition = canonical.definition;
                var previous = old == null ? null : old.entries.FirstOrDefault(e => e.definition.assetId == definition.assetId && e.canonicalAssetId == definition.assetId);
                var path = previous == null ? root + (definition.componentType == "RawImage" ? "/Textures/" : "/Sprites/") + definition.componentType + "/" + definition.styleId + "/" +
                    Prefixes[definition.componentType] + "_" + definition.styleId + "_" + definition.part + "_" + definition.state + "_" + definition.assetId + ".png" : previous.unityPath;
                // Split a formerly shared identity without changing an unselected panel's old appearance.
                if (previous != null && previous.sourceHash != canonical.sourceHash && old.entries.Count(e => e.unityPath == previous.unityPath) > 1 &&
                    old.entries.Where(e => e.unityPath == previous.unityPath).Any(e => result.entries.Any(n => n.definition.assetId == e.definition.assetId && n.pixelHash != canonical.pixelHash)))
                    path = Path.ChangeExtension(path, null) + "_" + canonical.sourceHash.Substring(0, 12) + ".png";
                UiStageWorkspace.ValidateAssetDestination(path);
                if (!path.StartsWith(root + "/Sprites/", StringComparison.Ordinal) && !path.StartsWith(root + "/Textures/", StringComparison.Ordinal))
                    throw new InvalidDataException("Registry path outside project library: " + path);
                foreach (var entry in group) { entry.canonicalAssetId = definition.assetId; entry.unityPath = path; entry.guid = AssetDatabase.AssetPathToGUID(path); }
                if (File.Exists(path) && (old == null || !old.entries.Any(e => e.unityPath == path)))
                    throw new InvalidDataException("Destination exists outside the managed registry; do not overwrite it: " + path);
                result.writes.Add(new UiPlannedAsset { path = path, source = canonical.sourcePath,
                    action = !File.Exists(path) ? "create" : FileHash(path) == canonical.sourceHash ? "reuse/check-import-settings" : "overwrite" });
            }
            if (result.writes.Select(a => a.path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.writes.Count)
                throw new InvalidDataException("Different assets resolve to the same project path.");
            var dependencyMap = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p))
                .OrderBy(p => p, StringComparer.Ordinal).ToDictionary(p => p, p => AssetDatabase.GetDependencies(p, true));
            foreach (var entry in result.entries)
                entry.consumers = dependencyMap.Where(p => p.Key != entry.unityPath && p.Value.Contains(entry.unityPath)).Select(p => p.Key).ToList();
            if (old != null)
                foreach (var entry in result.entries)
                {
                    var prior = old.entries.FirstOrDefault(e => e.definition.assetId == entry.definition.assetId);
                    if (prior != null && prior.unityPath != entry.unityPath && result.entries.Any(e => e.unityPath == prior.unityPath))
                        result.warnings.Add("Shared identity split; regenerate consumers of asset " + entry.definition.assetId + " to adopt " + entry.unityPath + "; old shared path retained: " + prior.unityPath);
                }
            if (old != null)
                foreach (var path in old.entries.Select(e => e.unityPath).Concat(old.retiredPaths).Distinct().Where(p => !result.entries.Any(e => e.unityPath == p) && File.Exists(p)))
                {
                    result.retiredPaths.Add(path);
                    var identities = old.entries.Where(e => e.unityPath == path).Select(e => e.definition.assetId).ToArray();
                    var replacements = result.entries.Where(e => identities.Contains(e.definition.assetId)).ToArray();
                    if (identities.Length > 0 && replacements.Length == identities.Length && replacements.Select(e => e.unityPath).Distinct().Count() == 1)
                    {
                        result.migrations[path] = replacements[0].unityPath;
                        result.writes.Add(new UiPlannedAsset { path = path, action = "retarget/delete-if-unreferenced" });
                        foreach (var consumer in dependencyMap.Where(p => p.Key.StartsWith(root + "/", StringComparison.Ordinal) && p.Key.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && p.Value.Contains(path)).Select(p => p.Key))
                            if (!result.writes.Any(w => w.path == consumer)) result.writes.Add(new UiPlannedAsset { path = consumer, action = "retarget" });
                    }
                    else
                    {
                        result.writes.Add(new UiPlannedAsset { path = path, action = "delete-if-unreferenced" });
                        result.warnings.Add("Retained split/retired asset with ambiguous ownership; regenerate its consumer panels before physical cleanup: " + path);
                    }
                }
            result.writes.Add(new UiPlannedAsset { path = result.registryPath, action = File.Exists(result.registryPath) ? "overwrite" : "create" });
            foreach (var path in result.writes.Select(a => a.path)) { result.targets[path] = FileHash(path); result.targets[path + ".meta"] = FileHash(path + ".meta"); }
            result.signature = Hash(new { result.directory, result.projectName, result.manifestHash, result.registryHash, result.entries, result.writes, result.targets, result.migrations, result.retiredPaths });
            return result;
        }

        internal static string ComponentType(XElement element)
        {
            var type = (string)element.Attribute("data-ugui") ?? "Container";
            if (type.Equals("Container", StringComparison.OrdinalIgnoreCase) && (string)element.Attribute("data-root") == "true") return "Image";
            if (type.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                var owner = element.Ancestors().FirstOrDefault(e => new[] { "Button", "Toggle", "Slider", "InputField", "Dropdown", "Scrollbar" }.Contains((string)e.Attribute("data-ugui"), StringComparer.OrdinalIgnoreCase));
                if (owner != null) type = (string)owner.Attribute("data-ugui");
            }
            if (type.Equals("InputField", StringComparison.OrdinalIgnoreCase)) return "TMP_InputField";
            if (type.Equals("ScrollView", StringComparison.OrdinalIgnoreCase)) return "ScrollRect";
            return Prefixes.Keys.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase)) ?? type;
        }
        internal static UiProjectAssetEntry Resolve(UiProjectAssetPlan plan, string value, string htmlDir, string sourcePrefix)
        {
            var entries = value.StartsWith("asset:", StringComparison.Ordinal) ? plan.entries.Where(e => e.definition.assetId == value.Substring(6)) :
                plan.entries.Where(e => value.StartsWith(sourcePrefix, StringComparison.Ordinal) && e.sourcePath ==
                    Path.GetFullPath(Path.Combine(htmlDir, "assets", value.Substring(sourcePrefix.Length))));
            var found = entries.ToArray();
            if (found.Length != 1) throw new InvalidDataException("Asset reference must resolve to exactly one approved project asset; use asset:<assetId>: " + value);
            return found[0];
        }
        internal static void Remap(XDocument document, UiProjectAssetPlan plan, string htmlDir, string sourcePrefix)
        {
            foreach (var attr in document.Descendants().Attributes().Where(a => HtmlUiImporter.AssetAttributes.Contains(a.Name.LocalName)))
            {
                var entry = Resolve(plan, attr.Value, htmlDir, sourcePrefix);
                var type = ComponentType(attr.Parent);
                if (type != entry.definition.componentType) throw new InvalidDataException("Cross-component reuse forbidden: " + entry.definition.assetId + " belongs to " + entry.definition.componentType + ", used by " + type);
                var attribute = attr.Name.LocalName;
                var expectedState = attribute == "data-toggle-off-sprite" ? "Off" : attribute == "data-toggle-on-sprite" ? "On" : attribute == "data-toggle-disabled-sprite" ? "Disabled" : null;
                var expectedPart = attribute == "data-fill-sprite" ? "Fill" : attribute == "data-handle-sprite" ? "Handle" : attribute == "data-checkmark-sprite" ? "Checkmark" :
                    (string)attr.Parent.Attribute("data-asset-part");
                if (expectedPart == null && !string.Equals((string)attr.Parent.Attribute("data-ugui"), "Image", StringComparison.OrdinalIgnoreCase) && type != "Image" && type != "RawImage")
                    expectedPart = type == "Slider" ? (attribute == "data-sprite" ? "Track" : "Handle") : "Surface";
                if (expectedPart == null && string.Equals((string)attr.Parent.Attribute("data-ugui"), "Image", StringComparison.OrdinalIgnoreCase) && type != "Image")
                    throw new InvalidDataException("Control-owned Image needs explicit data-asset-part: " + entry.definition.assetId);
                if (expectedPart != null && entry.definition.part != expectedPart) throw new InvalidDataException("Asset part mismatch: " + entry.definition.assetId + " / " + expectedPart);
                if (expectedState != null && (entry.definition.part != "Surface" || entry.definition.state != expectedState))
                    throw new InvalidDataException("Toggle full-surface state mismatch: " + entry.definition.assetId);
                var border = (string)attr.Parent.Attribute("data-border") ?? "0,0,0,0";
                if ((string)attr.Parent.Attribute("data-nine-slice") != "true") border = "0,0,0,0";
                if (attribute != "data-texture" && !border.Split(',').Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture)).SequenceEqual(entry.definition.border))
                    throw new InvalidDataException("HTML and project Sprite borders disagree: " + entry.definition.assetId);
                if (attribute == "data-texture" && type != "RawImage") throw new InvalidDataException("Texture requires RawImage.");
                attr.Value = entry.unityPath;
            }
        }
        public static void Sync(UiProjectAssetPlan reviewed)
        {
            if (reviewed == null) throw new ArgumentNullException("reviewed");
            if (!Writing.Add(reviewed.projectName)) throw new InvalidOperationException("This game's asset library is already synchronizing.");
            try
            {
                var current = Plan(reviewed.directory, reviewed.projectName);
                if (current.signature != reviewed.signature) throw new InvalidDataException("Project asset plan is stale; inspect it again.");
                var transactionPlan = new UiImportPlan { assets = current.writes };
                using (var transaction = new UiImportTransaction(transactionPlan))
                {
                    try
                    {
                        foreach (var write in current.writes.Where(w => w.source != null))
                        {
                            UiStageWorkspace.EnsureFolder(Path.GetDirectoryName(write.path).Replace('\\', '/'));
                            if (FileHash(write.path) != FileHash(write.source)) File.Copy(write.source, write.path, true);
                            AssetDatabase.ImportAsset(write.path, ImportAssetOptions.ForceSynchronousImport);
                            Configure(current.entries.First(e => e.unityPath == write.path), write.path);
                        }
                        foreach (var entry in current.entries) entry.guid = AssetDatabase.AssetPathToGUID(entry.unityPath);
                        foreach (var write in current.writes.Where(w => w.action == "retarget")) RetargetPrefab(write.path, current.migrations);
                        foreach (var retired in current.retiredPaths.ToArray())
                        {
                            var consumers = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p) && p != retired &&
                                AssetDatabase.GetDependencies(p, true).Contains(retired)).Concat(LiveConsumers(retired)).ToArray();
                            if (consumers.Length == 0) { if (!AssetDatabase.DeleteAsset(retired)) throw new IOException("Could not delete duplicate: " + retired); current.retiredPaths.Remove(retired); }
                            else current.warnings.Add("Retained duplicate with consumers: " + retired + " -> " + string.Join(", ", consumers));
                        }
                        UiStageWorkspace.EnsureFolder(Path.GetDirectoryName(current.registryPath).Replace('\\', '/'));
                        // The registry records identities and approvals, not transient plan target fingerprints.
                        var registry = JsonConvert.SerializeObject(new UiProjectAssetPlan { directory = current.directory, projectName = current.projectName,
                            manifestHash = current.manifestHash, entries = current.entries, warnings = current.warnings, retiredPaths = current.retiredPaths }, Formatting.Indented);
                        if (!File.Exists(current.registryPath) || File.ReadAllText(current.registryPath) != registry) File.WriteAllText(current.registryPath, registry);
                        AssetDatabase.ImportAsset(current.registryPath, ImportAssetOptions.ForceSynchronousImport);
                        if (FileHash(Path.Combine(current.directory, ManifestName)) != current.manifestHash || current.entries.Any(e => FileHash(e.sourcePath) != e.sourceHash) ||
                            current.entries.Where(e => e.definition.assetId == e.canonicalAssetId).Any(e => FileHash(e.unityPath) != e.sourceHash))
                            throw new InvalidDataException("Approved project inputs changed during asset synchronization.");
                        transaction.Commit();
                    }
                    catch (Exception original)
                    {
                        var recovery = transaction.Rollback();
                        if (recovery.Count > 0) throw new AggregateException("Asset synchronization failed and recovery was incomplete.", new[] { original }.Concat(recovery));
                        throw;
                    }
                }
            }
            finally { Writing.Remove(reviewed.projectName); }
        }
        internal static Dictionary<string, string> Freeze(UiProjectAssetPlan plan)
        {
            var paths = plan.entries.Select(e => e.sourcePath).Concat(plan.entries.Select(e => e.unityPath)).Concat(plan.entries.Select(e => e.unityPath + ".meta"))
                .Concat(new[] { Path.Combine(plan.directory, ManifestName), plan.registryPath });
            return paths.Distinct(StringComparer.Ordinal).ToDictionary(p => p, FileHash, StringComparer.Ordinal);
        }
        internal static void RequireFrozen(Dictionary<string, string> snapshot)
        {
            if (snapshot != null && snapshot.Any(p => FileHash(p.Key) != p.Value))
                throw new InvalidDataException("Project asset snapshot changed during generation; inspect and synchronize again.");
        }
        private static void RetargetPrefab(string path, Dictionary<string, string> mapping)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var component in contents.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidDataException("Missing script while retargeting: " + path);
                    var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) continue;
                        string target;
                        if (!mapping.TryGetValue(AssetDatabase.GetAssetPath(property.objectReferenceValue), out target)) continue;
                        var original = property.objectReferenceValue;
                        var replacement = AssetDatabase.LoadAllAssetsAtPath(target).SingleOrDefault(o => o.GetType() == original.GetType());
                        if (replacement == null) throw new InvalidDataException("Cannot resolve duplicate reference: " + path + " / " + property.propertyPath);
                        property.objectReferenceValue = replacement;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.SaveAsPrefabAsset(contents, path) == null) throw new IOException("Failed to retarget duplicate references: " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        private static IEnumerable<string> LiveConsumers(string path)
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<Component>())
            {
                if (component == null || EditorUtility.IsPersistent(component) || !component.gameObject.scene.IsValid() || !component.gameObject.scene.isLoaded) continue;
                var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null && AssetDatabase.GetAssetPath(property.objectReferenceValue) == path)
                    { yield return "Loaded scene: " + component.gameObject.scene.name + "/" + component.name + "/" + property.propertyPath; break; }
            }
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>().Where(m => !EditorUtility.IsPersistent(m)))
                if (material.GetTexturePropertyNames().Any(n => material.GetTexture(n) != null && AssetDatabase.GetAssetPath(material.GetTexture(n)) == path))
                    yield return "Unsaved material: " + material.name;
        }
        private static void Configure(UiProjectAssetEntry entry, string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Missing texture importer: " + path);
            var before = EditorJsonUtility.ToJson(importer);
            var item = entry.definition;
            importer.textureType = item.componentType == "RawImage" ? TextureImporterType.Default : TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(item.border[0], item.border[1], item.border[2], item.border[3]);
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(item.pivot[0], item.pivot[1]);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = item.pixelsPerUnit; importer.sRGBTexture = item.sRGB;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = item.filter == "Point" ? FilterMode.Point : FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Mathf.NextPowerOfTwo(Math.Max(entry.width, entry.height));
            foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL", "Windows Store Apps", "PS4", "PS5", "XboxOne", "Nintendo Switch" })
                if (importer.GetPlatformTextureSettings(platform).overridden)
                    throw new InvalidDataException("Shared library forbids unregistered platform overrides; remove the override before synchronization: " + path + " / " + platform);
            if (before != EditorJsonUtility.ToJson(importer)) importer.SaveAndReimport();
        }
    }
}
#endif
