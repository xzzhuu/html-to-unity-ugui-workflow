#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class UiProjectAssetRegressionChecks
    {
        public static string Run(string evidenceDirectory)
        {
            var passed = new List<string>();
            Action<bool, string> check = (value, name) => { if (!value) throw new Exception("Failed: " + name); passed.Add(name); };
            var token = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), "HtmlUiAssets-" + token);
            var game = "AssetValidation" + token;
            var root = UiProjectImport.ProjectRoot(game);
            var formal = UiRegressionWorkspace.Root + "/Assets-" + token;
            var manifest = new UiProjectAssetManifest();
            Directory.CreateDirectory(directory); Directory.CreateDirectory(evidenceDirectory);
            Action<string, string, string, string> asset = (id, type, part, state) =>
            {
                var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                texture.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); texture.Apply();
                File.WriteAllBytes(Path.Combine(directory, id + ".png"), texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                manifest.assets.Add(new UiProjectAssetDefinition { assetId = id, componentType = type, part = part, state = state, styleId = "Shared",
                    source = id + ".png", sourceSha256 = HtmlUiImporter.Sha256(Path.Combine(directory, id + ".png")), approved = true, approvalVersion = "synthetic-fixture" });
            };
            Action save = () => File.WriteAllText(Path.Combine(directory, UiProjectAssets.ManifestName), JsonConvert.SerializeObject(manifest));
            Func<string, string, string> panel = (name, controls) =>
            {
                var dir = Path.Combine(directory, name); Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "panel.html"), "<html><body><div data-root='true' data-panel-name='" + name + "' style='width:640px;height:360px'>" + controls + "</div></body></html>");
                var package = Path.Combine(dir, "panel.package.json");
                File.WriteAllText(package, JsonConvert.SerializeObject(new UiPanelPackage { panelName = name, projectName = game, html = "panel.html" }));
                return package;
            };
            Func<Action, bool> rejects = action => { try { action(); return false; } catch (InvalidDataException) { return true; } };
            try
            {
                asset("ButtonA", "Button", "Surface", "Normal"); asset("ButtonB", "Button", "Surface", "Normal");
                asset("Off", "Toggle", "Surface", "Off"); asset("On", "Toggle", "Surface", "On"); asset("Disabled", "Toggle", "Surface", "Disabled");
                asset("Unused", "Slider", "Track", "Default"); asset("Fill", "Slider", "Fill", "Default"); asset("Handle", "Slider", "Handle", "Default");
                foreach (var entry in manifest.assets.Where(e => e.componentType == "Toggle")) entry.border = new float[] { 1, 1, 1, 1 };
                save();
                var plan = UiProjectAssets.Plan(directory, game);
                check(!Directory.Exists(root), "Asset planning creates no files");
                check(plan.entries.Single(e => e.definition.assetId == "ButtonA").unityPath == plan.entries.Single(e => e.definition.assetId == "ButtonB").unityPath,
                    "Equivalent parts of the same component type deduplicate across style instances");
                check(plan.entries.Where(e => e.definition.componentType == "Toggle").Select(e => e.unityPath).Distinct().Count() == 3,
                    "Three Toggle states remain three physical images even with identical pixels");
                check(plan.entries.Select(e => e.unityPath).Distinct().Count() == 7, "Cross-component or different-part identical pixels never merge");
                var one = panel("AssetOnePanel", "<button data-ugui='Button' data-ui-name='Btn_One' data-sprite='asset:ButtonA' style='width:100px;height:40px;background-color:#FF0000'/>" +
                    "<input data-ugui='Toggle' data-ui-name='Toggle_Option' data-binding='Option' data-toggle-off-sprite='asset:Off' data-toggle-on-sprite='asset:On' data-toggle-disabled-sprite='asset:Disabled' data-nine-slice='true' data-border='1,1,1,1' style='left:120px;width:100px;height:40px;background-color:#000000'/>");
                var two = panel("AssetTwoPanel", "<button data-ugui='Button' data-ui-name='Btn_Two' data-sprite='asset:ButtonB' style='width:100px;height:40px;background-color:#00FF00'/>" +
                    "<input data-ugui='Slider' data-ui-name='Slider_Value' data-sprite='asset:Unused' data-fill-sprite='asset:Fill' data-handle-sprite='asset:Handle' style='left:120px;width:200px;height:24px'/>");
                var request = HtmlUiImporter.ReadPackage(one);
                var singlePlan = HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options);
                check(singlePlan.library.entries.Count == 8 && !Directory.Exists(root), "Single-panel plan inventories the entire game including unused elements");
                var prefabPath = HtmlUiImporter.Import(request.htmlPath, request.outputFolder, request.options, singlePlan);
                check(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponent<UiBindingMap>().Get<Toggle>("Option").GetComponent<Image>().color == Color.white,
                    "Full Toggle surfaces keep their authored colors instead of multiplying the browser fallback background");
                check(Directory.GetFiles(root + "/Sprites", "*.png", SearchOption.AllDirectories).Length == 7 &&
                    !Directory.Exists(UiProjectImport.StagingRoot(game, "AssetTwoPanel")), "Single-panel import synchronizes all approved project elements and generates only its panel");
                var frozen = UiProjectAssets.Plan(directory, game);
                var guids = frozen.entries.ToDictionary(e => e.definition.assetId, e => AssetDatabase.AssetPathToGUID(e.unityPath));
                var hashes = frozen.writes.Where(w => w.source != null).ToDictionary(w => w.path, w => HtmlUiImporter.Sha256(w.path + ".meta"));
                UiProjectAssets.Sync(frozen);
                check(hashes.All(h => HtmlUiImporter.Sha256(h.Key + ".meta") == h.Value) && UiProjectAssets.Plan(directory, game).entries.All(e => AssetDatabase.AssetPathToGUID(e.unityPath) == guids[e.definition.assetId]),
                    "Repeated synchronization preserves GUIDs and texture import settings");
                var originalHtml = File.ReadAllText(request.htmlPath);
                File.WriteAllText(request.htmlPath, originalHtml.Replace("asset:ButtonA", "asset:Off"));
                check(rejects(() => HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options)), "Cross-component use is blocked before writes even though the renderer is Image");
                File.WriteAllText(request.htmlPath, originalHtml.Replace("data-toggle-on-sprite='asset:On'", ""));
                check(rejects(() => HtmlUiImporter.PlanImport(request.htmlPath, request.outputFolder, request.options)), "Incomplete Toggle state coverage is rejected");
                File.WriteAllText(request.htmlPath, originalHtml);
                using (var scope = new UiTemporarySceneScope())
                {
                    var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
                    try
                    {
                        var toggle = instance.GetComponentInChildren<Toggle>(); var visuals = toggle.GetComponent<HtmlToUGUI.UiToggleStateGraphic>();
                        check(toggle.graphic == null && toggle.transform.Find("Img_Checkmark") == null && visuals.surface.sprite == visuals.offSprite,
                            "Full Toggle surfaces have no extra generated Checkmark or geometric fragments");
                        check(visuals.surface.color == Color.white && visuals.surface.type == Image.Type.Sliced && toggle.transition == Selectable.Transition.None,
                            "Toggle full surfaces preserve authored color and nine-slice geometry without default tinting");
                        toggle.isOn = true;
                        check(visuals.surface.sprite == visuals.onSprite, "Toggle value change immediately selects the On surface");
                        toggle.interactable = false; visuals.SendMessage("LateUpdate");
                        check(visuals.surface.sprite == visuals.disabledSprite && toggle.isOn, "Disabled surface takes priority without altering the logical value");
                        toggle.interactable = true; visuals.SendMessage("LateUpdate");
                        check(visuals.surface.sprite == visuals.onSprite, "Restoring interaction restores the retained On value");
                        toggle.SetIsOnWithoutNotify(false); visuals.SendMessage("LateUpdate");
                        check(visuals.surface.sprite == visuals.offSprite, "Programmatic no-notify assignment refreshes on the next frame");
                        var group = instance.AddComponent<CanvasGroup>(); group.interactable = false; visuals.SendMessage("LateUpdate");
                        check(visuals.surface.sprite == visuals.disabledSprite, "Parent CanvasGroup interaction changes refresh the state");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(instance); }
                }
                var batch = UiProjectImport.Plan(directory, game, new[] { one, two }, true);
                var result = UiProjectImport.Import(batch, rebuildPreviewScene: false);
                check(result.status == "completed" && result.panels.All(p => p.status == "imported"), "All-panel generation uses the same project library");
                var prefabs = result.panels.Select(p => p.prefabPath).ToArray();
                var buttonSprites = prefabs.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponentInChildren<Button>().GetComponent<Image>().sprite).ToArray();
                var slider = AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[1]).GetComponentInChildren<Slider>();
                check(slider.fillRect.GetComponent<Image>().sprite != null && slider.handleRect.GetComponent<Image>().sprite != null &&
                    slider.fillRect.GetComponent<Image>().color == Color.white && slider.fillRect.GetComponent<Image>().sprite != slider.handleRect.GetComponent<Image>().sprite,
                    "Slider Track, Fill and Handle are separate assigned sprites with no unintended cyan tint");
                check(buttonSprites[0] == buttonSprites[1], "Separately generated panels reference the same canonical Sprite");
                var copy = UiStageWorkspace.PlanCopy(prefabs, formal + "/Prefabs", formal + "/Images");
                check(copy.assets.Count(a => a.source.EndsWith(".png")) == 7, "Formal batch copy schedules a shared image exactly once");
                check(copy.assets.Where(a => a.source.EndsWith(".png")).All(a =>
                    a.destination.StartsWith(formal + "/Images/" + game + "/", StringComparison.Ordinal) &&
                    !a.destination.StartsWith(formal + "/Images/" + game + "/Sprites/", StringComparison.Ordinal)),
                    "Formal shared images keep the game hierarchy without a repeated Sprites directory");
                UiStageWorkspace.CopySelected(copy, formal + "/copy.json");
                check(copy.panels.All(p => !AssetDatabase.GetDependencies(copy.assets.Single(a => a.source == p.prefabPath).destination, true).Any(d => d.StartsWith(UiStageWorkspace.Root + "/"))),
                    "Formal Prefabs including Toggle state images have no staging dependencies");
                var buttonB = manifest.assets.Single(a => a.assetId == "ButtonB");
                var oldBytes = File.ReadAllBytes(Path.Combine(directory, buttonB.source));
                var changed = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                changed.SetPixels(Enumerable.Repeat(Color.gray, 64).ToArray()); changed.Apply();
                File.WriteAllBytes(Path.Combine(directory, buttonB.source), changed.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(changed);
                buttonB.sourceSha256 = HtmlUiImporter.Sha256(Path.Combine(directory, buttonB.source)); save();
                var split = UiProjectAssets.Plan(directory, game);
                var splitPath = split.entries.Single(e => e.definition.assetId == "ButtonB").unityPath;
                check(splitPath != split.entries.Single(e => e.definition.assetId == "ButtonA").unityPath && split.warnings.Any(w => w.Contains("split")),
                    "Diverging shared styles allocate separate assets and report unselected consumers");
                UiProjectAssets.Sync(split);
                check(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[0]).GetComponentInChildren<Button>().GetComponent<Image>().sprite == buttonSprites[0],
                    "Splitting a shared style preserves the other panel's Sprite identity");
                var secondRequest = HtmlUiImporter.ReadPackage(two); secondRequest.options.allowPrefabReplacement = true;
                HtmlUiImporter.Import(secondRequest.htmlPath, secondRequest.outputFolder, secondRequest.options);
                check(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[1]).GetComponentInChildren<Button>().GetComponent<Image>().sprite) == splitPath,
                    "Regenerating the changed panel adopts its split identity");
                File.WriteAllBytes(Path.Combine(directory, buttonB.source), oldBytes); buttonB.sourceSha256 = HtmlUiImporter.Sha256(Path.Combine(directory, buttonB.source)); save();
                using (var scope = new UiTemporarySceneScope())
                {
                    var live = UiTemporarySceneScope.CreateGameObject("UnsavedConsumer", typeof(RectTransform), typeof(Image));
                    live.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(splitPath);
                    try
                    {
                        UiProjectAssets.Sync(UiProjectAssets.Plan(directory, game));
                        check(File.Exists(splitPath) && live.GetComponent<Image>().sprite != null,
                            "Duplicate cleanup preserves references in loaded unsaved scenes");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(live); }
                }
                UiProjectAssets.Sync(UiProjectAssets.Plan(directory, game));
                check(!File.Exists(splitPath) && AssetDatabase.LoadAssetAtPath<GameObject>(prefabs[1]).GetComponentInChildren<Button>().GetComponent<Image>().sprite == buttonSprites[0],
                    "Merging duplicates retargets existing managed Prefab references before deleting the unreferenced copy");
                var stale = UiProjectAssets.Plan(directory, game); File.AppendAllText(Path.Combine(directory, UiProjectAssets.ManifestName), " ");
                check(rejects(() => UiProjectAssets.Sync(stale)), "Changed manifest invalidates the asset plan before synchronization"); save();
                var bytes = File.ReadAllBytes(Path.Combine(directory, "Unused.png")); File.WriteAllBytes(Path.Combine(directory, "Unused.png"), new byte[] { 1, 2, 3 });
                check(rejects(() => UiProjectAssets.Plan(directory, game)), "Changed unrelated approved asset blocks single and batch project synchronization");
                File.WriteAllBytes(Path.Combine(directory, "Unused.png"), bytes);
                var profile = new UiProjectAssetDefinition { assetId = "DifferentBorder", componentType = "Button", part = "Surface", state = "Normal", styleId = "Shared", source = "ButtonA.png",
                    sourceSha256 = manifest.assets[0].sourceSha256, approved = true, approvalVersion = "synthetic-fixture", border = new float[] { 1, 1, 1, 1 } };
                manifest.assets.Add(profile); save();
                var different = UiProjectAssets.Plan(directory, game);
                check(different.entries.Single(e => e.definition.assetId == "DifferentBorder").unityPath != different.entries.Single(e => e.definition.assetId == "ButtonA").unityPath,
                    "Identical pixels with different nine-slice metadata cannot merge");
                var firstPath = different.entries.Single(e => e.definition.assetId == "ButtonA").unityPath;
                var importer = (TextureImporter)AssetImporter.GetAtPath(firstPath);
                var platform = importer.GetPlatformTextureSettings("Android"); platform.overridden = true; platform.maxTextureSize = 32;
                importer.SetPlatformTextureSettings(platform); importer.SaveAndReimport();
                var beforeFailure = File.ReadAllBytes(firstPath + ".meta");
                var rejectedPlan = UiProjectAssets.Plan(directory, game);
                check(rejects(() => UiProjectAssets.Sync(rejectedPlan)) && beforeFailure.SequenceEqual(File.ReadAllBytes(firstPath + ".meta")),
                    "A failed shared importer configuration rolls back file and meta mutations");
                var report = JsonConvert.SerializeObject(new { count = passed.Count, passed }, Formatting.Indented);
                File.WriteAllText(Path.Combine(evidenceDirectory, "project-assets-regression.json"), report); return report;
            }
            finally
            {
                foreach (var path in new[] { root, formal }) { UiStageWorkspace.ValidateAssetDestination(path); if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path); }
                UiRegressionWorkspace.RemoveEmptyRoot();
                if (Path.GetFileName(directory) == "HtmlUiAssets-" + token && Path.GetFullPath(directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) Directory.Delete(directory, true);
            }
        }
    }
}
#endif
