#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class UiStageRegressionChecks
    {
        public static string Run(string evidenceDirectory)
        {
            var passed = new List<string>();
            Action<bool, string> check = (value, label) => { if (!value) throw new Exception("Failed: " + label); passed.Add(label); };
            var token = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), "HtmlUiStage-" + token);
            var output = UiRegressionWorkspace.Root + "/Stage-" + token;
            var firstName = "StageOne" + token + "Panel"; var secondName = "StageTwo" + token + "Panel";
            var firstRoot = UiStageWorkspace.Root + "/" + firstName; var secondRoot = UiStageWorkspace.Root + "/" + secondName;
            var scene = SceneManager.GetActiveScene(); var dirty = scene.isDirty; var roots = scene.rootCount;
            Directory.CreateDirectory(Path.Combine(directory, "assets")); Directory.CreateDirectory(evidenceDirectory);
            var png = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            png.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); png.Apply();
            File.WriteAllBytes(Path.Combine(directory, "assets/surface.png"), png.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(png);
            try
            {
                foreach (var name in new[] { firstName, secondName })
                {
                    var itemName = name == firstName ? "Item_Row" : "Item_SecondRow";
                    var prefix = "Assets/Art/UI/Sprite/" + name + "/";
                    var html = Path.Combine(directory, name + ".html");
                    File.WriteAllText(html, "<html><body><div data-root='true' data-panel-name='" + name + "' style='width:640px;height:360px'>" +
                        "<button data-ugui='Button' data-ui-name='Btn_Main' data-sprite='" + prefix + "surface.png' style='left:0px;top:0px;width:160px;height:48px'>Review</button>" +
                        "<template data-prefab='Row'><div data-ugui='Button' data-ui-name='" + itemName + "' data-sprite='" + prefix + "surface.png' style='width:120px;height:40px'><span data-ugui='Text' data-ui-name='Txt_Row' style='width:100px;height:40px'>Row</span></div></template></div></body></html>");
                    var options = new HtmlUiImporter.ImportOptions { panelName = name, font = TMP_Settings.defaultFontAsset,
                        assetPrefix = prefix, stagingRoot = UiStageWorkspace.Root + "/" + name };
                    HtmlUiImporter.Import(html, output + "/UnusedFormal", options);
                }
                var first = UiStageWorkspace.FindPanels().Single(p => p.panelName == firstName);
                var second = UiStageWorkspace.FindPanels().Single(p => p.panelName == secondName);
                check(!Directory.Exists(output), "Staged generation does not create formal output folders");
                check(HtmlUiImporter.ReadPreparedHtml(Path.Combine(directory, firstName + ".html")).options.stagingRoot == UiStageWorkspace.DefaultStagingRoot,
                    "Prepared HTML defaults to HtmlToUGUIStage");
                File.WriteAllText(Path.Combine(directory, "panel.package.json"), JsonConvert.SerializeObject(new UiPanelPackage { panelName = firstName, html = firstName + ".html" }));
                check(HtmlUiImporter.ReadPackage(Path.Combine(directory, "panel.package.json")).options.stagingRoot == UiStageWorkspace.DefaultStagingRoot,
                    "Legacy packages without stagingRoot default to HtmlToUGUIStage");
                var formal = output + "/Formal"; var sprites = output + "/Sprites";
                var single = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites);
                check(single.assets.Count == 3 && !Directory.Exists(output), "Read-only copy plan includes only the selected panel, Item and PNG");
                check(single.keepPanelNameSubdirectory && single.assets.Single(a => a.source == first.prefabPath).destination == formal + "/" + firstName + "/" + firstName + ".prefab",
                    "Default copy mode preserves the panel-name subdirectory");
                UiStageWorkspace.CopySelected(single, output + "/single-copy.json");
                var copied = formal + "/" + firstName + "/" + firstName + ".prefab";
                var copiedItem = formal + "/Item/Item_Row.prefab";
                check(File.Exists(copied) && File.Exists(copiedItem) && !Directory.Exists(formal + "/" + secondName), "Copying one panel leaves the unselected panel out");
                check(!Directory.Exists(formal + "/" + firstName + "/Items") && !Directory.Exists(formal + "/" + firstName + "/Item") &&
                    !Directory.GetFiles(formal, "*.json", SearchOption.AllDirectories).Any(), "Items use the common Item folder and formal Prefabs omit review reports");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(copied);
                check(!AssetDatabase.GetDependencies(copied, true).Any(p => p.StartsWith(UiStageWorkspace.Root + "/")), "Copied root, Item and Sprite have no staged dependencies");
                check(prefab.GetComponentsInChildren<TMP_Text>(true).All(t => t.font == TMP_Settings.defaultFontAsset && t.fontSharedMaterial == TMP_Settings.defaultFontAsset.material) &&
                    !Directory.GetFiles(output, "*.mat", SearchOption.AllDirectories).Any(), "Copying reuses existing fonts and materials without copies");
                var copiedRow = AssetDatabase.LoadAssetAtPath<GameObject>(copiedItem);
                check(AssetDatabase.GetAssetPath(copiedRow.GetComponent<Image>().sprite).StartsWith(sprites + "/" + firstName + "/"), "Item Sprite points at the formal image copy");
                using (var scope = new UiTemporarySceneScope())
                {
                    var live = UnityEngine.Object.Instantiate(prefab, UiTemporarySceneScope.CreateGameObject("TestHost").transform, false);
                    var row = live.GetComponent<UiTemplateCatalog>().CreateItem("Row", live.transform);
                    check(row.GetComponent<Image>().sprite != null && row.GetComponentInChildren<TMP_Text>().text == "Row", "Copied template catalog instantiates the formal Item");
                }
                var blocked = false;
                try { UiStageWorkspace.CopySelected(UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites), output + "/blocked.json"); }
                catch (InvalidDataException) { blocked = true; }
                check(blocked, "Existing formal resources are protected by default");
                var guid = AssetDatabase.AssetPathToGUID(copied);
                UiStageWorkspace.CopySelected(UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites, true), output + "/replaced.json");
                check(AssetDatabase.AssetPathToGUID(copied) == guid, "Explicit replacement preserves the formal Prefab GUID");
                var formalImage = single.assets.Single(a => a.source.EndsWith(".png"));
                var imageGuid = AssetDatabase.AssetPathToGUID(formalImage.destination);
                var imageMeta = File.ReadAllBytes(formalImage.destination + ".meta");
                var reuse = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites, true);
                check(reuse.assets.Single(a => a.source.EndsWith(".png")).action == "reuse", "Identical PNG and import/subasset metadata reuse the existing formal image");
                using (var readOnlyImage = new FileStream(formalImage.destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var readOnlyMeta = new FileStream(formalImage.destination + ".meta", FileMode.Open, FileAccess.Read, FileShare.Read))
                    UiStageWorkspace.CopySelected(reuse, output + "/reuse.json");
                check(File.ReadAllBytes(formalImage.destination + ".meta").SequenceEqual(imageMeta) &&
                    AssetDatabase.AssetPathToGUID(formalImage.destination) == imageGuid, "Reuse does not rewrite the formal image meta or GUID");
                var importer = (TextureImporter)AssetImporter.GetAtPath(formalImage.destination);
                var oldPpu = importer.spritePixelsPerUnit;
                importer.spritePixelsPerUnit = oldPpu + 1; importer.SaveAndReimport();
                var settingsConflict = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites, true);
                check(settingsConflict.assets.Single(a => a.source.EndsWith(".png")).action == "overwrite", "Same pixels with different Sprite import settings require explicit overwrite");
                check(settingsConflict.assets.Single(a => a.source.EndsWith(".png")).consumers.Contains(copied), "Overwrite plan lists dependent formal Prefabs");
                var permissionBlocked = false;
                try { UiStageWorkspace.CopySelected(UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites, true, true, new string[0]), output + "/unauthorized.json"); }
                catch (InvalidDataException) { permissionBlocked = true; }
                check(permissionBlocked && !File.Exists(output + "/unauthorized.json"), "Per-resource overwrite approval is required before writes");
                var authorized = settingsConflict;
                UiStageWorkspace.SetCopyOverwriteApproval(authorized, settingsConflict.assets.Where(a => a.action == "overwrite").Select(a => a.destination));
                UiStageWorkspace.CopySelected(authorized, output + "/authorized.json");
                check(AssetDatabase.AssetPathToGUID(formalImage.destination) == imageGuid &&
                    ((TextureImporter)AssetImporter.GetAtPath(formalImage.destination)).spritePixelsPerUnit == oldPpu,
                    "Authorized replacement retains formal GUID and adopts reviewed import settings");
                check(single.assets.All(a => File.Exists(a.source) && File.Exists(a.source + ".meta")), "Promotion preserves all staged source assets and metadata");
                var noReuse = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites, true, false);
                check(noReuse.assets.All(a => a.action == "overwrite"), "Disabling reuse explicitly plans replacement even for identical images");
                var beforeMissingMeta = File.ReadAllBytes(formalImage.destination + ".meta");
                File.Delete(formalImage.destination + ".meta");
                var incompleteBlocked = false;
                try { UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites); }
                catch (InvalidDataException) { incompleteBlocked = true; }
                finally { File.WriteAllBytes(formalImage.destination + ".meta", beforeMissingMeta); }
                check(incompleteBlocked, "Incomplete formal asset/meta pairs are rejected without changing the asset");
                var copyEvidence = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(output + "/authorized.json"));
                check((bool)copyEvidence["sourceRetained"] && copyEvidence["assets"].Any() && copyEvidence["dependencyChecks"].Any(),
                    "Copy evidence includes source retention, actions, consumers and outstanding dependency checks");
                check((bool)copyEvidence["keepPanelNameSubdirectory"], "Copy evidence records the panel directory mode");
                var flat = UiStageWorkspace.PlanCopy(new[] { first.prefabPath, second.prefabPath }, output + "/Flat", output + "/FlatSprites", keepPanelNameSubdirectory: false);
                check(!flat.keepPanelNameSubdirectory && !Directory.Exists(output + "/Flat") && flat.panels.All(p =>
                    flat.assets.Single(a => a.source == p.prefabPath).destination == output + "/Flat/" + p.panelName + ".prefab"),
                    "Flat mode plans all main Prefabs directly under the formal root without writes");
                UiStageWorkspace.CopySelected(flat, output + "/flat.json");
                check(flat.panels.All(p => !Directory.Exists(output + "/Flat/" + p.panelName)) &&
                    File.Exists(output + "/Flat/Item/Item_Row.prefab") && File.Exists(output + "/Flat/Item/Item_SecondRow.prefab") &&
                    flat.panels.All(p => !AssetDatabase.GetDependencies(output + "/Flat/" + p.panelName + ".prefab", true).Any(d => d.StartsWith(UiStageWorkspace.Root + "/"))),
                    "Flat mode copies Items to the common folder and remaps all staging references");
                using (var scope = new UiTemporarySceneScope())
                {
                    foreach (var panel in flat.panels)
                    {
                        var live = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(output + "/Flat/" + panel.panelName + ".prefab"),
                            UiTemporarySceneScope.CreateGameObject("FlatHost").transform, false);
                        var row = live.GetComponent<UiTemplateCatalog>().CreateItem("Row", live.transform);
                        check(row.GetComponent<Image>().sprite != null && row.GetComponentInChildren<TMP_Text>().text == "Row",
                            "Flat copied template catalog instantiates its formal Item: " + panel.panelName);
                    }
                }
                check(!(bool)Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(output + "/flat.json"))["keepPanelNameSubdirectory"],
                    "Flat copy evidence records the disabled panel directory mode");
                var changedMode = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, output + "/ChangedMode", output + "/ChangedModeSprites");
                changedMode.keepPanelNameSubdirectory = false;
                var modeRejected = false;
                try { UiStageWorkspace.CopySelected(changedMode, output + "/changed-mode.json"); } catch (InvalidDataException) { modeRejected = true; }
                check(modeRejected && !Directory.Exists(output + "/ChangedMode"), "Changing the directory mode invalidates the reviewed plan before writes");
                var secondItem = secondRoot + "/Prefabs/Items/Item_SecondRow.prefab";
                check(string.IsNullOrEmpty(AssetDatabase.RenameAsset(secondItem, "item_row")), "Prepare case-insensitive Item filename conflict");
                try
                {
                    foreach (var keepDirectory in new[] { true, false })
                    {
                        var conflict = false;
                        try { UiStageWorkspace.PlanCopy(new[] { first.prefabPath, second.prefabPath }, output + "/Conflict", output + "/ConflictSprites", keepPanelNameSubdirectory: keepDirectory); }
                        catch (InvalidDataException error) { conflict = error.Message.Contains("same formal path"); }
                        check(conflict && !Directory.Exists(output + "/Conflict"), "Duplicate Item names are rejected in read-only planning; keepPanelNameSubdirectory=" + keepDirectory);
                    }
                }
                finally { AssetDatabase.RenameAsset(secondRoot + "/Prefabs/Items/item_row.prefab", "Item_SecondRow"); }
                var atlasPath = output + "/FormalReview.spriteatlas";
                var atlas = new UnityEngine.U2D.SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
                UnityEditor.U2D.SpriteAtlasExtensions.Add(atlas, new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(firstRoot + "/Sprites") });
                EditorUtility.SetDirty(atlas); AssetDatabase.SaveAssets();
                check(UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, formal, sprites).dependencyChecks.Any(w => w.Contains(atlasPath)),
                    "Copy plan flags SpriteAtlases that collect staged folders");
                var stale = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, output + "/Stale", output + "/StaleSprites");
                var image = firstRoot + "/Sprites/surface.png"; var bytes = File.ReadAllBytes(image);
                File.AppendAllText(image, "changed");
                UiStageWorkspace.SetCopyOverwriteApproval(stale, new string[0]);
                var rejected = false;
                try { UiStageWorkspace.CopySelected(stale, output + "/stale.json"); } catch (InvalidDataException) { rejected = true; }
                finally { File.WriteAllBytes(image, bytes); }
                check(rejected && !Directory.Exists(output + "/Stale"), "Changed staging inputs invalidate the copy plan before any target writes");
                var multi = UiStageWorkspace.PlanCopy(new[] { first.prefabPath, second.prefabPath }, output + "/Multi", output + "/MultiSprites");
                UiStageWorkspace.CopySelected(multi, output + "/multi.json");
                check(multi.panels.Count == 2 && multi.assets.All(a => File.Exists(a.destination)), "Multiple checked panels copy in one transaction");
                var before = multi.assets.ToDictionary(a => a.destination, a => File.ReadAllBytes(a.destination));
                var images = multi.assets.Where(a => a.source.EndsWith(".png")).OrderBy(a => a.destination, StringComparer.Ordinal).ToArray();
                var changedImage = images[0]; var originalImage = File.ReadAllBytes(changedImage.source);
                File.AppendAllText(changedImage.source, "changed bytes");
                var failed = false;
                try
                {
                    using (var locked = new FileStream(images[1].destination, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        try { UiStageWorkspace.CopySelected(UiStageWorkspace.PlanCopy(new[] { first.prefabPath, second.prefabPath }, output + "/Multi", output + "/MultiSprites", true, false), output + "/failed.json"); }
                        catch (Exception) { failed = true; }
                    }
                }
                finally { File.WriteAllBytes(changedImage.source, originalImage); }
                check(failed && before.All(p => File.ReadAllBytes(p.Key).SequenceEqual(p.Value)) && !File.Exists(output + "/failed.json"), "A failed batch restores all earlier copied resource bytes");
                UiStageWorkspace.BuildPreviewScene(new List<UiStagedPanel> { first, second }, output + "/StageScene.unity", secondName);
                var preview = EditorSceneManager.OpenScene(output + "/StageScene.unity", OpenSceneMode.Additive);
                try
                {
                    var host = preview.GetRootGameObjects().Single(g => g.name == "HtmlToUGUIStage");
                    check(host.transform.childCount == 2 && !host.transform.Find(firstName).gameObject.activeSelf && host.transform.Find(secondName).gameObject.activeSelf,
                        "StageScene contains all panels and shows exactly the selected panel");
                    check(host.transform.Find(secondName).GetComponent<CanvasScaler>().referenceResolution == new Vector2(640, 360), "StageScene preserves each panel's design resolution");
                    UiStageWorkspace.SelectPreview(preview, firstName);
                    check(host.transform.Find(firstName).gameObject.activeSelf && !host.transform.Find(secondName).gameObject.activeSelf && !preview.isDirty,
                        "Switching a loaded preview changes only the active panel without reopening the scene");
                    EditorSceneManager.MarkSceneDirty(preview);
                    UiStageWorkspace.SelectPreview(preview, secondName);
                    check(preview.isDirty && host.transform.Find(secondName).gameObject.activeSelf, "Preview switching preserves unsaved scene edits without a save prompt");
                }
                finally { EditorSceneManager.CloseScene(preview, true); }
                check(scene.isDirty == dirty && scene.rootCount == roots && SceneManager.GetActiveScene() == scene, "Generating preview scenes preserves the user's active scene and its dirty state");
                var result = JsonConvert.SerializeObject(new { count = passed.Count, passed, scene = scene.path, sceneDirty = scene.isDirty }, Formatting.Indented);
                File.WriteAllText(Path.Combine(evidenceDirectory, "stage-regression.json"), result);
                return result;
            }
            finally
            {
                foreach (var target in new[] { firstRoot, secondRoot, output })
                {
                    UiStageWorkspace.ValidateAssetDestination(target);
                    if (AssetDatabase.IsValidFolder(target)) AssetDatabase.DeleteAsset(target);
                }
                UiRegressionWorkspace.RemoveEmptyRoot();
                Directory.Delete(directory, true);
            }
        }
    }
}
#endif
