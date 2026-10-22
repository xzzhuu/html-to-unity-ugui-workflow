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
            var output = "Assets/HtmlUiValidation/Stage-" + token;
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
                    var prefix = "Assets/Art/UI/Sprite/" + name + "/";
                    var html = Path.Combine(directory, name + ".html");
                    File.WriteAllText(html, "<html><body><div data-root='true' data-panel-name='" + name + "' style='width:640px;height:360px'>" +
                        "<button data-ugui='Button' data-ui-name='Btn_Main' data-sprite='" + prefix + "surface.png' style='left:0px;top:0px;width:160px;height:48px'>Review</button>" +
                        "<template data-prefab='Row'><div data-ugui='Image' data-ui-name='Item_Row' data-sprite='" + prefix + "surface.png' style='width:120px;height:40px'><span data-ugui='Text' data-ui-name='Txt_Row' style='width:100px;height:40px'>Row</span></div></template></div></body></html>");
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
                UiStageWorkspace.CopySelected(single, output + "/single-copy.json");
                var copied = formal + "/" + firstName + "/" + firstName + ".prefab";
                var copiedItem = formal + "/" + firstName + "/Item_Row.prefab";
                check(File.Exists(copied) && File.Exists(copiedItem) && !Directory.Exists(formal + "/" + secondName), "Copying one panel leaves the unselected panel out");
                check(!Directory.Exists(formal + "/" + firstName + "/Items") && !Directory.GetFiles(formal, "*.json", SearchOption.AllDirectories).Any(), "Formal Prefabs are flat and omit review reports");
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
                var stale = UiStageWorkspace.PlanCopy(new[] { first.prefabPath }, output + "/Stale", output + "/StaleSprites");
                var image = firstRoot + "/Sprites/surface.png"; var bytes = File.ReadAllBytes(image);
                File.AppendAllText(image, "changed");
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
                        try { UiStageWorkspace.CopySelected(UiStageWorkspace.PlanCopy(new[] { first.prefabPath, second.prefabPath }, output + "/Multi", output + "/MultiSprites", true), output + "/failed.json"); }
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
                Directory.Delete(directory, true);
            }
        }
    }
}
#endif
