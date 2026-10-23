#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    [Serializable] public sealed class UiStageCopyAsset
    {
        public string source, destination, action, sourceSha256, sourceMetaSha256, destinationSha256, destinationMetaSha256;
        public List<string> consumers = new List<string>();
    }
    [Serializable] public sealed class UiStageCopyPlan
    {
        public List<UiStagedPanel> panels = new List<UiStagedPanel>();
        public List<UiStageCopyAsset> assets = new List<UiStageCopyAsset>();
        public string prefabRoot, spriteRoot, signature;
        public bool replaceExisting;
        public bool keepPanelNameSubdirectory = true;
        public bool reuseIdenticalImages = true;
        public List<string> approvedOverwrites;
        public List<string> dependencyChecks = new List<string>();
    }

    public static partial class UiStageWorkspace
    {
        private static string Fingerprint(string path) { return File.Exists(path) ? HtmlUiImporter.Sha256(path) : null; }

        public static UiStageCopyPlan PlanCopy(IEnumerable<string> selectedPrefabs, string prefabRoot = "Assets/Art/UIPrefab",
            string spriteRoot = "Assets/Art/UI/Sprites", bool replaceExisting = false, bool reuseIdenticalImages = true,
            IEnumerable<string> approvedOverwrites = null, bool keepPanelNameSubdirectory = true)
        {
            ValidateAssetDestination(prefabRoot + "/dummy.prefab");
            ValidateAssetDestination(spriteRoot + "/dummy.png");
            foreach (var path in new[] { prefabRoot, spriteRoot })
                if (path.Equals(Root, StringComparison.OrdinalIgnoreCase) || path.StartsWith(Root + "/", StringComparison.OrdinalIgnoreCase) ||
                    Root.StartsWith(path.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Formal destinations must be separate from HtmlToUGUIStage.");
            var selected = selectedPrefabs.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            if (selected.Length == 0) throw new InvalidDataException("Select at least one panel to copy.");
            var found = FindPanels();
            var plan = new UiStageCopyPlan { prefabRoot = prefabRoot, spriteRoot = spriteRoot, replaceExisting = replaceExisting,
                keepPanelNameSubdirectory = keepPanelNameSubdirectory,
                reuseIdenticalImages = reuseIdenticalImages, approvedOverwrites = approvedOverwrites == null ? null :
                    approvedOverwrites.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList() };
            var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prefabPath in selected)
            {
                var panel = found.SingleOrDefault(p => p.prefabPath == prefabPath);
                if (panel == null) throw new InvalidDataException("Selected prefab is not a registered staged panel: " + prefabPath);
                plan.panels.Add(panel);
                foreach (var source in AssetDatabase.GetDependencies(prefabPath, true).Where(p => p.StartsWith(Root + "/", StringComparison.Ordinal)))
                {
                    var projectRoot = string.IsNullOrEmpty(panel.projectName) ? null : UiProjectImport.ProjectRoot(panel.projectName);
                    var sharedImage = projectRoot != null && source.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                        (source.StartsWith(projectRoot + "/Sprites/", StringComparison.Ordinal) || source.StartsWith(projectRoot + "/Textures/", StringComparison.Ordinal));
                    if (!sharedImage && !source.StartsWith(panel.stageRoot + "/", StringComparison.Ordinal))
                        throw new InvalidDataException("Panel references another staged panel. Resolve that shared dependency first: " + source);
                    if (plan.assets.Any(a => a.source == source)) continue;
                    string target;
                    if (source.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        // The registered root is the panel; other generated Prefab dependencies are Items.
                        // Classify by identity so Item names need no particular prefix.
                        var folder = source == panel.prefabPath
                            ? (keepPanelNameSubdirectory ? prefabRoot + "/" + panel.panelName : prefabRoot)
                            : prefabRoot + "/Item";
                        target = folder + "/" + Path.GetFileName(source);
                    }
                    else if (source.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        var prefix = panel.stageRoot + "/Sprites/";
                        if (sharedImage)
                        {
                            // spriteRoot already identifies the Sprite collection. Retain the
                            // game and component hierarchy without repeating its staging bucket.
                            var relative = source.Substring(projectRoot.Length + 1);
                            if (relative.StartsWith("Sprites/", StringComparison.Ordinal)) relative = relative.Substring("Sprites/".Length);
                            target = spriteRoot + "/" + panel.projectName + "/" + relative;
                        }
                        else
                        {
                            if (!source.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidDataException("Unexpected staged image location: " + source);
                            target = spriteRoot + "/" + panel.panelName + "/" + source.Substring(prefix.Length);
                        }
                    }
                    else throw new InvalidDataException("Only generated Prefabs and PNGs are copied. Keep fonts and other shared project assets outside staging: " + source);
                    ValidateAssetDestination(target);
                    if (!destinations.Add(target)) throw new InvalidDataException("Two staged assets map to the same formal path: " + target);
                    if (!File.Exists(source + ".meta")) throw new InvalidDataException("Missing staged asset meta: " + source);
                    if (Directory.Exists(target) || File.Exists(target) != File.Exists(target + ".meta"))
                        throw new InvalidDataException("Formal target is a directory or has an incomplete asset/meta pair. Resolve before copying: " + target);
                    plan.assets.Add(new UiStageCopyAsset { source = source, destination = target,
                        action = File.Exists(target) ? (reuseIdenticalImages && source.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                            Fingerprint(source) == Fingerprint(target) && File.Exists(target + ".meta") &&
                            ComparableImageMeta(source) == ComparableImageMeta(target) ? "reuse" : "overwrite") : "create", sourceSha256 = Fingerprint(source), sourceMetaSha256 = Fingerprint(source + ".meta"),
                        destinationSha256 = Fingerprint(target), destinationMetaSha256 = Fingerprint(target + ".meta") });
                }
            }
            plan.assets = plan.assets.OrderBy(a => a.destination, StringComparer.Ordinal).ToList();
            InspectCopyDependencies(plan);
            SignCopyPlan(plan);
            return plan;
        }

        private static void SignCopyPlan(UiStageCopyPlan plan)
        {
            plan.signature = null;
            using (var hash = System.Security.Cryptography.SHA256.Create())
                plan.signature = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(plan)))).Replace("-", "").ToLowerInvariant();
        }

        internal static void SetCopyOverwriteApproval(UiStageCopyPlan plan, IEnumerable<string> approvedPaths)
        {
            // Change permissions on the reviewed snapshot, not its file fingerprints.
            // Execution must still reject any inputs changed since the Dry Run.
            plan.approvedOverwrites = approvedPaths.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
            SignCopyPlan(plan);
        }

        public static string CopySelected(UiStageCopyPlan reviewedPlan, string reportPath = null)
        {
            UiPanelScenePlacement.ValidateContext();
            if (reviewedPlan == null) throw new InvalidDataException("Inspect the copy plan first.");
            var plan = PlanCopy(reviewedPlan.panels.Select(p => p.prefabPath), reviewedPlan.prefabRoot, reviewedPlan.spriteRoot,
                reviewedPlan.replaceExisting, reviewedPlan.reuseIdenticalImages, reviewedPlan.approvedOverwrites, reviewedPlan.keepPanelNameSubdirectory);
            if (plan.signature != reviewedPlan.signature) throw new InvalidDataException("Copy plan is stale. Inspect the current plan before copying.");
            if (plan.assets.Any(a => a.action == "overwrite" && (!plan.replaceExisting ||
                (plan.approvedOverwrites != null && !plan.approvedOverwrites.Contains(a.destination)))))
                throw new InvalidDataException("Formal resources already exist. Enable replacement only for disposable generated resources; integrated business Prefabs need project-specific adaptation.");
            reportPath = reportPath ?? Root + "/CopyReports/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json";
            ValidateAssetDestination(reportPath);
            if (!reportPath.StartsWith(Root + "/", StringComparison.Ordinal) && !reportPath.StartsWith("Assets/HtmlUiValidation/", StringComparison.Ordinal))
                throw new InvalidDataException("Copy evidence must stay in staging or an isolated validation directory.");
            if (File.Exists(reportPath)) throw new InvalidDataException("Copy evidence path already exists.");
            var transactionPlan = new UiImportPlan();
            transactionPlan.assets.AddRange(plan.assets.Where(a => a.action != "reuse").Select(a => new UiPlannedAsset { path = a.destination }));
            transactionPlan.assets.Add(new UiPlannedAsset { path = reportPath });
            using (var transaction = new UiImportTransaction(transactionPlan))
            {
                try
                {
                    var mapping = plan.assets.ToDictionary(a => a.source, a => a.destination, StringComparer.Ordinal);
                    foreach (var asset in plan.assets.Where(a => a.action != "reuse" && a.source.EndsWith(".png", StringComparison.OrdinalIgnoreCase))) CopyImage(asset);
                    var saved = new HashSet<string>(StringComparer.Ordinal);
                    var visiting = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var asset in plan.assets.Where(a => a.source.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                        CopyPrefab(asset.source, mapping, saved, visiting);
                    foreach (var panel in plan.panels)
                    {
                        var target = mapping[panel.prefabPath];
                        if (AssetDatabase.GetDependencies(target, true).Any(p => p.StartsWith(Root + "/", StringComparison.Ordinal)))
                            throw new InvalidDataException("Formal prefab still depends on staging: " + target);
                    }
                    foreach (var asset in plan.assets)
                        if (Fingerprint(asset.source) != asset.sourceSha256 || Fingerprint(asset.source + ".meta") != asset.sourceMetaSha256)
                            throw new InvalidDataException("Staged resources changed during copying.");
                    EnsureFolder(Path.GetDirectoryName(reportPath).Replace('\\', '/'));
                    File.WriteAllText(reportPath, JsonConvert.SerializeObject(new { status = "copied", mode = "copy", sourceRetained = true,
                        plan.signature, plan.keepPanelNameSubdirectory, plan.assets, plan.dependencyChecks,
                        panels = plan.panels.Select(p => new { p.panelName, p.prefabPath, sourceImportReportSha256 = Fingerprint(p.reportPath) }),
                        finalArtifacts = plan.assets.Select(a => new { path = a.destination, guid = AssetDatabase.AssetPathToGUID(a.destination),
                            sha256 = Fingerprint(a.destination), metaSha256 = Fingerprint(a.destination + ".meta") })
                    }, Formatting.Indented));
                    AssetDatabase.ImportAsset(reportPath);
                    transaction.Commit();
                    return reportPath;
                }
                catch (Exception original)
                {
                    var recovery = transaction.Rollback();
                    if (recovery.Count > 0) throw new AggregateException("Copy failed and recovery was incomplete.", new[] { original }.Concat(recovery));
                    throw;
                }
            }
        }

        // Keep all import/subasset identity settings in the comparison. Only the owning GUID differs.
        private static string ComparableImageMeta(string path)
        {
            return System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(path + ".meta").Replace("\r\n", "\n"),
                @"(?m)^guid: [0-9a-f]+[ \t]*$", "guid: <owner>");
        }

        private static void InspectCopyDependencies(UiStageCopyPlan plan)
        {
            var overwritten = plan.assets.Where(a => a.action == "overwrite").ToDictionary(a => a.destination, StringComparer.Ordinal);
            var candidates = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) &&
                !AssetDatabase.IsValidFolder(p)).OrderBy(p => p, StringComparer.Ordinal);
            foreach (var path in candidates)
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (overwritten.Count > 0 && new[] { ".prefab", ".unity", ".asset", ".mat", ".anim", ".controller", ".spriteatlas" }.Contains(extension))
                    foreach (var dependency in AssetDatabase.GetDependencies(path, true).Distinct())
                        if (dependency != path && overwritten.TryGetValue(dependency, out var asset)) asset.consumers.Add(path);
                if (extension != ".spriteatlas") continue;
                var atlas = AssetDatabase.LoadAssetAtPath<UnityEngine.U2D.SpriteAtlas>(path);
                if (atlas == null) continue;
                var packables = UnityEditor.U2D.SpriteAtlasExtensions.GetPackables(atlas).Where(p => p != null)
                    .Select(AssetDatabase.GetAssetPath).ToArray();
                if (packables.Any(p => p == Root || p.StartsWith(Root + "/", StringComparison.Ordinal) ||
                    plan.assets.Any(a => a.destination == p || a.destination.StartsWith(p + "/", StringComparison.Ordinal))))
                    plan.dependencyChecks.Add("SpriteAtlas review (packables/build/platform settings): " + path + " sha256=" + Fingerprint(path) + " metaSha256=" + Fingerprint(path + ".meta") +
                        "; packables=" + string.Join(", ", packables));
            }
            foreach (var asset in plan.assets.Where(a => a.source.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            {
                var importer = AssetImporter.GetAtPath(asset.source);
                if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                    plan.dependencyChecks.Add("Source AssetBundle mark will be copied; review formal ownership: " + asset.source + " -> " + importer.assetBundleName);
                if (File.Exists(asset.destination))
                {
                    importer = AssetImporter.GetAtPath(asset.destination);
                    if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                        plan.dependencyChecks.Add("Existing target AssetBundle mark: " + asset.destination + " -> " + importer.assetBundleName);
                }
            }
            plan.dependencyChecks.Add("Manual: verify SpriteAtlas packing/loading, material/animation references and Sprite subasset IDs.");
            plan.dependencyChecks.Add("Manual: verify AssetBundle rules, Resources and string-based resource configuration; these are not automatically modified.");
            plan.dependencyChecks.Add("Manual: validate final scenes, business bindings/events and runtime loading; exclude staging scenes/resources from build collection.");
        }

        private static void CopyImage(UiStageCopyAsset asset)
        {
            EnsureFolder(Path.GetDirectoryName(asset.destination).Replace('\\', '/'));
            // Preserve the formal GUID while copying the reviewed Sprite import
            // settings. Source and target images are independently owned assets.
            var guid = AssetDatabase.AssetPathToGUID(asset.destination);
            if (string.IsNullOrEmpty(guid)) guid = Guid.NewGuid().ToString("N");
            File.Copy(asset.source, asset.destination, true);
            var meta = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(asset.source + ".meta"), @"(?m)^guid: [0-9a-f]+\s*$", "guid: " + guid);
            File.WriteAllText(asset.destination + ".meta", meta);
            AssetDatabase.ImportAsset(asset.destination, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void CopyPrefab(string source, Dictionary<string, string> mapping, HashSet<string> saved, HashSet<string> visiting)
        {
            if (saved.Contains(source)) return;
            if (!visiting.Add(source)) throw new InvalidDataException("Cyclic staged Prefab dependency: " + source);
            foreach (var dependency in AssetDatabase.GetDependencies(source, false).Where(p => p != source && mapping.ContainsKey(p) && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)))
                CopyPrefab(dependency, mapping, saved, visiting);
            var destination = mapping[source];
            EnsureFolder(Path.GetDirectoryName(destination).Replace('\\', '/'));
            var contents = PrefabUtility.LoadPrefabContents(source);
            try
            {
                foreach (var component in contents.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidDataException("Missing script in staged Prefab: " + source);
                    var serialized = new SerializedObject(component);
                    var iterator = serialized.GetIterator();
                    while (iterator.Next(true))
                    {
                        if (iterator.propertyType != SerializedPropertyType.ObjectReference || iterator.objectReferenceValue == null) continue;
                        var reference = iterator.objectReferenceValue;
                        string target;
                        if (mapping.TryGetValue(AssetDatabase.GetAssetPath(reference), out target))
                            iterator.objectReferenceValue = RemapReference(reference, target);
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.SaveAsPrefabAsset(contents, destination) == null) throw new IOException("Could not save formal Prefab: " + destination);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            visiting.Remove(source);
            saved.Add(source);
        }

        private static UnityEngine.Object RemapReference(UnityEngine.Object source, string target)
        {
            var sourceObject = source as GameObject;
            var sourceComponent = source as Component;
            if (sourceObject != null || sourceComponent != null)
            {
                var transform = sourceObject != null ? sourceObject.transform : sourceComponent.transform;
                var segments = new List<string>();
                while (transform.parent != null) { segments.Insert(0, transform.name); transform = transform.parent; }
                var destination = AssetDatabase.LoadAssetAtPath<GameObject>(target);
                var child = segments.Count == 0 ? destination.transform : destination.transform.Find(string.Join("/", segments));
                if (child == null) throw new InvalidDataException("Could not remap staged object: " + source.name);
                if (sourceObject != null) return child.gameObject;
                var siblings = sourceComponent.GetComponents(sourceComponent.GetType());
                return child.GetComponents(sourceComponent.GetType())[Array.IndexOf(siblings, sourceComponent)];
            }
            string guid; long localId;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out guid, out localId);
            foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(target))
            {
                string candidateGuid; long candidateId;
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out candidateGuid, out candidateId) && candidateId == localId && candidate.GetType() == source.GetType()) return candidate;
            }
            throw new InvalidDataException("Could not remap staged asset: " + source.name);
        }
    }
}
#endif
