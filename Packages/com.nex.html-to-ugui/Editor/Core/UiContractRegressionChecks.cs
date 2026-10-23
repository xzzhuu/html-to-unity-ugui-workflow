#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    // Synthetic conversion fixtures, never user design approvals. Runs in the real Editor.
    public static class UiContractRegressionChecks
    {
        public static string Run(string evidenceDirectory)
        {
            var passed = new List<string>();
            Action<bool, string> check = (condition, label) => { if (!condition) throw new Exception("Failed: " + label); passed.Add(label); };
            var token = Guid.NewGuid().ToString("N");
            var dir = Path.Combine(Path.GetTempPath(), "HtmlUiContract-" + token);
            var root = UiRegressionWorkspace.Root + "/" + token;
            var stage = root + "/Stage";
            Directory.CreateDirectory(Path.Combine(dir, "assets"));
            Directory.CreateDirectory(evidenceDirectory);
            var html = Path.Combine(dir, "ValidationPanel.html");
            var prefix = root + "/FormalSprites/";
            var formalOutput = root + "/FormalPrefabs";
            var scene = SceneManager.GetActiveScene();
            var dirty = scene.isDirty; var roots = scene.rootCount;
            var settings = new HtmlUiImporter.ImportOptions { panelName = "ValidationPanel", assetPrefix = prefix,
                stagingRoot = stage, font = TMP_Settings.defaultFontAsset };
            Action<Color> png = color =>
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                texture.SetPixels(Enumerable.Repeat(color, 64 * 64).ToArray()); texture.Apply();
                File.WriteAllBytes(Path.Combine(dir, "assets/surface.png"), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            };
            Func<string, string> button = content => "<button data-ugui='Button' " + content + " />";
            var content = button("data-binding='Red' data-sprite='" + prefix + "surface.png' data-selected-color='#FFFF00' style='left:20px;top:20px;width:180px;height:60px;background-color:#FF0000'") +
                button("data-binding='Green' data-sprite='" + prefix + "surface.png' style='left:220px;top:20px;width:180px;height:60px;background-color:#00FF00'") +
                button("data-binding='Untinted' data-sprite='" + prefix + "surface.png' style='left:420px;top:20px;width:180px;height:60px'") +
                button("data-binding='Swap' data-normal-sprite='" + prefix + "surface.png' data-selected-sprite='" + prefix + "selected.png' data-highlighted-sprite='" + prefix + "selected.png' data-pressed-sprite='" + prefix + "selected.png' data-disabled-sprite='" + prefix + "selected.png' style='left:20px;top:100px;width:180px;height:60px'") +
                "<template data-prefab='Row'><div data-ugui='Button' data-ui-name='Item_Row' data-sprite='" + prefix + "surface.png' style='width:100px;height:30px;background-color:#0000FF'/></template>";
            var markup = "<html><body><div id='ValidationPanel' data-panel-name='ValidationPanel' data-root='true' style='width:640px;height:240px;background-color:#404040'>" + content + "</div></body></html>";
            png(Color.blue); File.Copy(Path.Combine(dir, "assets/surface.png"), Path.Combine(dir, "assets/selected.png"));
            png(Color.white); File.WriteAllText(html, markup);
            try
            {
                var standalonePackagePath = Path.Combine(dir, "panel.package.json");
                var standalone = new UiPanelPackage { panelName = settings.panelName, html = Path.GetFileName(html),
                    outputFolder = formalOutput, assetPrefix = prefix, stagingRoot = stage };
                File.WriteAllText(standalonePackagePath, JsonConvert.SerializeObject(standalone));
                var standaloneWindow = ScriptableObject.CreateInstance<HtmlUiImportWindow>();
                try
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    typeof(HtmlUiImportWindow).GetField("packagePath", flags).SetValue(standaloneWindow, standalonePackagePath);
                    var request = (UiImportRequest)typeof(HtmlUiImportWindow).GetMethod("ResolveRequest", flags).Invoke(standaloneWindow, null);
                    check(string.IsNullOrEmpty(request.options.projectName) && request.options.stagingRoot == stage,
                        "Single-panel window accepts standalone technical packages and preserves their staging root");
                }
                finally { UnityEngine.Object.DestroyImmediate(standaloneWindow); }
                check(!HtmlUiImporter.PlanMatches(new UiImportPlan(), null), "Missing current plan is rejected without throwing");
                var plan = HtmlUiImporter.PlanImport(html, formalOutput, settings);
                check(!AssetDatabase.IsValidFolder(root) && !Directory.Exists(root), "Read-only plan creates no assets");
                check(plan.assets.All(a => a.path.StartsWith(stage + "/")), "Staging covers sprites, material, Item, report and main Prefab");
                File.AppendAllText(html, " ");
                var staleBlocked = false;
                try { HtmlUiImporter.Import(html, formalOutput, settings, plan); } catch (InvalidDataException) { staleBlocked = true; }
                check(staleBlocked && !Directory.Exists(root), "Reviewed plan rejects changed inputs before any write");
                File.WriteAllText(html, markup);
                File.WriteAllText(Path.Combine(evidenceDirectory, "import-plan.json"), JsonConvert.SerializeObject(plan, Formatting.Indented));
                var path = HtmlUiImporter.Import(html, formalOutput, settings);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var map = prefab.GetComponent<UiBindingMap>();
                var red = map.Get<Button>("Red"); var green = map.Get<Button>("Green");
                check(red.GetComponent<Image>().color == Color.red && green.GetComponent<Image>().color == Color.green, "Shared white Sprite preserves two independent base tints");
                check(red.GetComponent<Image>().sprite == green.GetComponent<Image>().sprite, "Color variants reuse one Sprite");
                check(map.Get<Button>("Untinted").GetComponent<Image>().color == Color.white, "Sprite without explicit background remains white");
                check(red.transition == Selectable.Transition.ColorTint && red.colors.selectedColor == new Color(1, 1, 0, 1), "Native ColorTint selected state is mapped independently");
                var swap = map.Get<Button>("Swap");
                check(swap.transition == Selectable.Transition.SpriteSwap && swap.GetComponent<Image>().sprite != null && swap.spriteState.selectedSprite != null &&
                    swap.spriteState.highlightedSprite != null && swap.spriteState.pressedSprite != null && swap.spriteState.disabledSprite != null, "All native SpriteSwap states are assigned");
                using (var sceneScope = new UiTemporarySceneScope())
                {
                    var live = UnityEngine.Object.Instantiate(prefab, UiTemporarySceneScope.CreateGameObject("TransientHost").transform, false);
                    var liveSwap = live.GetComponent<UiBindingMap>().Get<Button>("Swap");
                    liveSwap.OnSelect(new UnityEngine.EventSystems.BaseEventData(null));
                    check(liveSwap.GetComponent<Image>().overrideSprite == liveSwap.spriteState.selectedSprite, "Native selection actually switches the rendered Sprite");
                    liveSwap.interactable = false;
                    check(liveSwap.GetComponent<Image>().overrideSprite == liveSwap.spriteState.disabledSprite, "Disabling a control actually applies its disabled Sprite");
                }
                var texturePath = stage + "/Sprites/surface.png";
                var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                check(importer.npotScale == TextureImporterNPOTScale.None && !importer.mipmapEnabled && importer.maxTextureSize >= 64 && importer.textureCompression == TextureImporterCompression.Uncompressed, "Texture import preserves resolution and disables NPOT scaling");
                check(!Directory.Exists(formalOutput) && !Directory.Exists(prefix), "Staging leaves formal asset locations untouched");
                var reportPath = Path.ChangeExtension(path, ".import-report.json");
                var report = JObject.Parse(File.ReadAllText(reportPath));
                check((string)report["converterVersion"] == "0.3.0" && report["configurationSha256"] != null && report["inputsSha256"]["ValidationPanel.html"] != null, "Report records converter, configuration and input fingerprints");
                check(report["artifacts"].All(a => File.Exists((string)a["path"]) && (string)a["sha256"] == HtmlUiImporter.Sha256((string)a["path"]) && !string.IsNullOrEmpty((string)a["guid"])), "Output fingerprints match actual Unity resources and GUIDs");
                var blocked = false;
                try { HtmlUiImporter.Import(html, formalOutput, settings); } catch (InvalidDataException) { blocked = true; }
                check(blocked, "Existing Prefab replacement is blocked by default");

                settings.allowPrefabReplacement = true;
                foreach (var target in new[] { reportPath, reportPath + ".meta" })
                {
                    var reviewed = HtmlUiImporter.PlanImport(html, formalOutput, settings);
                    var before = File.ReadAllBytes(target);
                    File.AppendAllText(target, "\n");
                    var changed = File.ReadAllBytes(target);
                    var targetBlocked = false;
                    try { HtmlUiImporter.Import(html, formalOutput, settings, reviewed); }
                    catch (InvalidDataException error) { targetBlocked = error.Message.Contains("stale"); }
                    finally { File.WriteAllBytes(target, before); }
                    check(targetBlocked && !before.SequenceEqual(changed), "Reviewed plan rejects changed destination bytes before overwrite: " + Path.GetExtension(target));
                }

                // Fail after textures, material and Items have been written, then compare every byte/meta.
                var baseline = Directory.GetFiles(stage, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes);
                png(Color.gray);
                settings.allowPrefabReplacement = true;
                settings.configureRoot = (go, bindings, templates) => { throw new InvalidOperationException("Synthetic failure after Item save"); };
                var failed = false;
                try { HtmlUiImporter.Import(html, formalOutput, settings); } catch (InvalidOperationException) { failed = true; }
                check(failed && baseline.Keys.OrderBy(p => p).SequenceEqual(Directory.GetFiles(stage, "*", SearchOption.AllDirectories).OrderBy(p => p)) &&
                    baseline.All(pair => pair.Value.SequenceEqual(File.ReadAllBytes(pair.Key))), "Failure restores all existing Sprite/Item/material/Prefab/report bytes and metas");
                settings.stagingRoot = root + "/FailedNewStage";
                try { HtmlUiImporter.Import(html, formalOutput, settings); } catch (InvalidOperationException) { }
                check(!Directory.Exists(settings.stagingRoot) && !File.Exists(settings.stagingRoot + ".meta"), "Failure removes all newly created resources and folders");
                settings.stagingRoot = stage; settings.configureRoot = null;

                // A restrictive mobile override must be surfaced, not silently downsampled.
                importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                var android = importer.GetPlatformTextureSettings("Android"); android.name = "Android"; android.overridden = true; android.maxTextureSize = 32;
                android.format = TextureImporterFormat.RGBA32; android.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
                var platformBlocked = false;
                try { HtmlUiImporter.Import(html, formalOutput, settings); } catch (InvalidDataException ex) { platformBlocked = ex.Message.Contains("platform override"); }
                check(platformBlocked, "Restrictive platform texture override is explicitly rejected");
                importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); importer.ClearPlatformTextureSettings("Android"); importer.SaveAndReimport();
                png(Color.white);
                importer.maxTextureSize = 32; importer.npotScale = TextureImporterNPOTScale.ToNearest; importer.SaveAndReimport();
                HtmlUiImporter.Import(html, formalOutput, settings);
                importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                check(importer.maxTextureSize >= 64 && importer.npotScale == TextureImporterNPOTScale.None && importedTexture.width == 64 && importedTexture.height == 64,
                    "Restrictive default Max Size is raised and actual imported dimensions match source");

                File.WriteAllText(Path.Combine(dir, "sprites.json"), "{\"sprites\":[{\"pixelSize\":[64,64],\"contentRect\":[-10,999,999,0]}]}");
                var rectBlocked = false;
                try { HtmlUiImporter.PlanImport(html, formalOutput, settings); } catch (InvalidDataException) { rectBlocked = true; }
                check(rectBlocked, "Invalid contentRect rejected before resource writes");
                File.WriteAllText(Path.Combine(dir, "sprites.json"), "{\"sprites\":[{\"pixelSize\":[64,64],\"contentRect\":[1,1,60,60]}]}");
                rectBlocked = false;
                try { HtmlUiImporter.PlanImport(html, formalOutput, settings); } catch (InvalidDataException ex) { rectBlocked = ex.Message.Contains("unsupported"); }
                check(rectBlocked, "Unsupported transparent padding explicitly rejected");
                File.Delete(Path.Combine(dir, "sprites.json"));
                var mixed = markup.Replace("data-selected-color='#FFFF00'", "data-selected-color='#FFFF00' data-selected-sprite='" + prefix + "selected.png'");
                File.WriteAllText(html, mixed);
                var mixedBlocked = false;
                try { HtmlUiImporter.PlanImport(html, formalOutput, settings); } catch (InvalidDataException) { mixedBlocked = true; }
                check(mixedBlocked, "Conflicting ColorTint and SpriteSwap states rejected");
                File.WriteAllText(html, markup);

                // Prepared and package entry points must preserve the active scene as well.
                var prepared = markup.Replace("data-root='true'", "data-root='true' data-asset-prefix='" + prefix + "' data-output-folder='" + root + "/Prepared' data-staging-root='" + root + "/PreparedStage'");
                File.WriteAllText(html, prepared);
                var preparedPath = HtmlUiImporter.ImportPreparedHtml(html, TMP_Settings.defaultFontAsset);
                File.WriteAllText(Path.Combine(dir, "panel.package.json"), JsonConvert.SerializeObject(new UiPanelPackage {
                    panelName = "ValidationPanel", html = "ValidationPanel.html", assetPrefix = prefix,
                    outputFolder = root + "/Package", stagingRoot = root + "/PackageStage", fontAsset = AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset) }));
                var packagePath = HtmlUiImporter.ImportPackage(Path.Combine(dir, "panel.package.json"));
                check(File.Exists(preparedPath) && File.Exists(packagePath) && scene.isDirty == dirty && scene.rootCount == roots,
                    "All import entry points preserve the active scene and its clean state");
                var strict = JObject.Parse(File.ReadAllText(Path.Combine(dir, "panel.package.json"))); strict["requireSkillApproval"] = true;
                File.WriteAllText(Path.Combine(dir, "panel.package.json"), strict.ToString());
                var approvalBlocked = false;
                try { HtmlUiImporter.ImportPackage(Path.Combine(dir, "panel.package.json")); } catch (InvalidDataException) { approvalBlocked = true; }
                check(approvalBlocked, "Skill package entry rejects missing approval evidence");
                var window = ScriptableObject.CreateInstance<HtmlUiImportWindow>();
                try
                {
                    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                    typeof(HtmlUiImportWindow).GetField("packagePath", flags).SetValue(window, Path.Combine(dir, "panel.package.json"));
                    var identityBlocked = false;
                    try { typeof(HtmlUiImportWindow).GetMethod("ResolveRequest", flags).Invoke(window, null); }
                    catch (System.Reflection.TargetInvocationException error) { identityBlocked = error.InnerException is InvalidDataException; }
                    check(identityBlocked, "Single-panel window requires a game identity before generation");
                    typeof(HtmlUiImportWindow).GetField("projectName", flags).SetValue(window, "ContractValidation");
                    var resolved = (UiImportRequest)typeof(HtmlUiImportWindow).GetMethod("ResolveRequest", flags).Invoke(window, null);
                    check(resolved.options.stagingRoot == root + "/PackageStage" && resolved.options.requireSkillApproval && !resolved.options.allowPrefabReplacement,
                        "Window inherits package safety configuration by default");
                    var preparedDocument = System.Xml.Linq.XDocument.Load(html);
                    var preparedRoot = preparedDocument.Descendants().Single(n => (string)n.Attribute("data-root") == "true");
                    preparedRoot.SetAttributeValue("data-project-name", "ContractValidation");
                    preparedRoot.SetAttributeValue("data-project-directory", ".");
                    preparedRoot.SetAttributeValue("data-staging-root", root + "/PreparedStage");
                    preparedDocument.Save(html);
                    var modeField = typeof(HtmlUiImportWindow).GetField("sourceMode", flags);
                    modeField.SetValue(window, Enum.Parse(modeField.FieldType, "PreparedHtml"));
                    typeof(HtmlUiImportWindow).GetField("html", flags).SetValue(window, html);
                    typeof(HtmlUiImportWindow).GetField("requireApproval", flags).SetValue(window, false);
                    var preparedRequest = (UiImportRequest)typeof(HtmlUiImportWindow).GetMethod("ResolveRequest", flags).Invoke(window, null);
                    check(preparedRequest.options.stagingRoot == root + "/PreparedStage" && preparedRequest.options.projectDirectory == Path.GetFullPath(dir) && preparedRequest.options.requireSkillApproval,
                        "Prepared HTML window preserves source project/staging settings and requires approval");
                    typeof(HtmlUiImportWindow).GetField("overridePackageSettings", flags).SetValue(window, true);
                    typeof(HtmlUiImportWindow).GetField("stagingRoot", flags).SetValue(window, root + "/ExplicitOverride");
                    preparedRequest = (UiImportRequest)typeof(HtmlUiImportWindow).GetMethod("ResolveRequest", flags).Invoke(window, null);
                    check(preparedRequest.options.stagingRoot == root + "/ExplicitOverride", "Prepared HTML settings change only with explicit override");
                    File.WriteAllText(html, markup);
                }
                finally { UnityEngine.Object.DestroyImmediate(window); }
                var shared = TMP_Settings.defaultFontAsset.material;
                check(!plan.assets.Any(a => a.path.EndsWith(".mat")) && prefab.GetComponentsInChildren<TMP_Text>(true).All(t => t.fontSharedMaterial == shared), "Default font material is reused without generated font variants");
                var sharedBytes = File.ReadAllBytes(AssetDatabase.GetAssetPath(shared));
                var sharedName = shared.name; var sharedWasDirty = EditorUtility.IsDirty(shared);
                shared.name = "Unsaved shared material test"; EditorUtility.SetDirty(shared);
                settings.stagingRoot = root + "/SharedFontStage"; settings.fontMaterial = shared;
                HtmlUiImporter.Import(html, formalOutput, settings);
                check(!File.Exists(settings.stagingRoot + "/Prefabs/ValidationPanel.Font.mat") && sharedBytes.SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(shared))) && EditorUtility.IsDirty(shared),
                    "Shared TMP material is reused without cloning or saving unrelated pending edits (clone=" + File.Exists(settings.stagingRoot + "/Prefabs/ValidationPanel.Font.mat") + ", bytes=" + sharedBytes.SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(shared))) + ", dirty=" + EditorUtility.IsDirty(shared) + ")");
                shared.name = sharedName;
                if (!sharedWasDirty) EditorUtility.ClearDirty(shared);

                settings.stagingRoot = stage; settings.fontMaterial = null;
                HtmlUiImporter.Import(html, formalOutput, settings);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var audit = UiVisualAudit.Capture(prefab, evidenceDirectory);
                check(audit.issues.Count == 0 && scene.isDirty == dirty && scene.rootCount == roots, "Isolated visual audit captures without modifying the active scene");
                var recoveryFolder = root + "/Recovery"; Directory.CreateDirectory(recoveryFolder);
                var lockedPath = recoveryFolder + "/Locked.txt"; var otherPath = recoveryFolder + "/Other.txt";
                File.WriteAllText(lockedPath, "original locked"); File.WriteAllText(otherPath, "original other");
                var recoveryPlan = new UiImportPlan(); recoveryPlan.assets.Add(new UiPlannedAsset { path = lockedPath }); recoveryPlan.assets.Add(new UiPlannedAsset { path = otherPath });
                using (var transaction = new UiImportTransaction(recoveryPlan))
                {
                    File.WriteAllText(lockedPath, "changed locked"); File.WriteAllText(otherPath, "changed other");
                    using (var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var errors = transaction.Rollback();
                        check(errors.Count == 1 && errors[0].Message.Contains(lockedPath) && File.ReadAllText(otherPath) == "original other",
                            "Rollback reports a locked file and continues restoring other resources");
                    }
                }
                var result = JsonConvert.SerializeObject(new { count = passed.Count, passed, prefab = path, evidenceDirectory, project = Application.dataPath,
                    scene = scene.path, sceneDirty = scene.isDirty, deviceVerification = "not-applicable: synthetic desktop converter regression" }, Formatting.Indented);
                File.WriteAllText(Path.Combine(evidenceDirectory, "contract-regression.json"), result);
                return result;
            }
            finally
            {
                // Durable evidence is outside Assets; synthetic resources are always temporary.
                if (AssetDatabase.IsValidFolder(root)) AssetDatabase.DeleteAsset(root);
                UiRegressionWorkspace.RemoveEmptyRoot();
                Directory.Delete(dir, true);
            }
        }
    }
}
#endif
