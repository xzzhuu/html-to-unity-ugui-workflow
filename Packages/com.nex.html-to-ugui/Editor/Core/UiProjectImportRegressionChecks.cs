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

namespace HtmlToUGUI.Editor
{
    public static class UiProjectImportRegressionChecks
    {
        // Synthetic data only; never approves/imports the user's production panels.
        public static string Run(string evidenceDirectory)
        {
            var passed = new List<string>();
            Action<bool, string> check = (v, name) => { if (!v) throw new Exception("Failed: " + name); passed.Add(name); };
            var token = Guid.NewGuid().ToString("N");
            var directory = Path.Combine(Path.GetTempPath(), "HtmlUiProject-" + token);
            var group = "BatchValidation" + token;
            var groupOther = group + "Other"; var groupPartial = group + "Partial"; var groupCancel = group + "Cancel";
            var validation = UiRegressionWorkspace.Root + "/Project-" + token;
            var oneName = "ProjectOne" + token + "Panel"; var twoName = "ProjectTwo" + token + "Panel";
            var assets = new UiProjectAssetManifest();
            var original = SceneManager.GetActiveScene(); var dirty = original.isDirty; var roots = original.rootCount;
            Directory.CreateDirectory(directory); Directory.CreateDirectory(evidenceDirectory);
            Func<string, string, string> source = (folder, name) =>
            {
                var target = Path.Combine(directory, folder); Directory.CreateDirectory(Path.Combine(target, "assets"));
                var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                texture.SetPixels(Enumerable.Repeat(Color.white, 64).ToArray()); texture.Apply();
                File.WriteAllBytes(Path.Combine(target, "assets/surface.png"), texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllText(Path.Combine(target, name + ".html"), "<html><body><div data-root='true' data-panel-name='" + name + "' style='width:640px;height:360px'>" +
                    "<button data-ugui='Button' data-ui-name='Btn_Test' data-sprite='Assets/Art/UI/Sprite/" + name + "/surface.png' style='width:160px;height:48px'>Batch</button></div></body></html>");
                var path = Path.Combine(target, "panel.package.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(new UiPanelPackage { panelName = name, html = name + ".html", assetPrefix = "Assets/Art/UI/Sprite/" + name + "/" }));
                assets.assets.Add(new UiProjectAssetDefinition { assetId = "Surface" + assets.assets.Count, componentType = "Button", part = "Surface", styleId = "Primary", state = "Normal",
                    source = folder + "/assets/surface.png", sourceSha256 = HtmlUiImporter.Sha256(Path.Combine(target, "assets/surface.png")), approved = true, approvalVersion = "synthetic-fixture" });
                File.WriteAllText(Path.Combine(directory, UiProjectAssets.ManifestName), JsonConvert.SerializeObject(assets));
                return path;
            };
            try
            {
                var one = source("One/html", oneName); var two = source("Two/html", twoName);
                var ignored = Path.Combine(directory, "node_modules/ignored"); Directory.CreateDirectory(ignored);
                File.WriteAllText(Path.Combine(ignored, "panel.package.json"), "{}");
                var broken = Path.Combine(directory, "Broken"); Directory.CreateDirectory(broken);
                File.WriteAllText(Path.Combine(broken, "panel.package.json"), "{}");
                var scan = UiProjectImport.Scan(directory);
                check(scan.sources.Count == 3 && scan.sources.Count(p => p.error == null) == 2, "Scan finds valid nested panels, reports invalid JSON and skips dependency folders");
                check(!Directory.Exists(UiProjectImport.ProjectRoot(group)), "Scanning creates no project stage folder");
                var manifestPath = Path.Combine(directory, UiProjectAssets.ManifestName);
                var manifestBytes = File.ReadAllBytes(manifestPath);
                File.Move(manifestPath, manifestPath + ".saved");
                try
                {
                    var missing = UiProjectImport.Plan(directory, group, new[] { one, two });
                    check(!missing.valid && missing.errors.Count == 1 && missing.errors[0].Contains(manifestPath) && missing.panels.Count == 0 &&
                        !File.Exists(manifestPath) && !Directory.Exists(UiProjectImport.ProjectRoot(group)),
                        "Missing project manifest reports its full path once without retrying panels, creating assets or fabricating approvals");
                }
                finally { File.Move(manifestPath + ".saved", manifestPath); }
                File.WriteAllText(manifestPath, "{");
                try
                {
                    var malformed = UiProjectImport.Plan(directory, group, new[] { one, two });
                    check(!malformed.valid && malformed.errors.Count == 1 && malformed.panels.Count == 0,
                        "Invalid shared manifest stops batch planning once before panel planning");
                }
                finally { File.WriteAllBytes(manifestPath, manifestBytes); }
                var bad = UiProjectImport.Plan(directory, group, new[] { Path.Combine(broken, "panel.package.json") });
                check(!bad.valid && !Directory.Exists(UiProjectImport.ProjectRoot(group)), "Malformed selections fail read-only planning without creating assets");
                var escaped = UiProjectImport.Plan(directory, group, new[] { Path.Combine(directory, "../outside/panel.package.json") });
                check(!escaped.valid, "Package paths outside the selected project are rejected");
                foreach (var name in new[] { "..", "A/B", "C:\\Escape", "CON", " " })
                {
                    var blocked = false; try { UiProjectImport.ValidateProjectName(name); } catch (InvalidDataException) { blocked = true; }
                    check(blocked, "Invalid project folder rejected: " + name);
                }
                var plan = UiProjectImport.Plan(directory, group, new[] { one, two });
                check(plan.valid && plan.panels.All(p => p.import.assets.All(a => a.path.StartsWith(UiProjectImport.ProjectRoot(group) + "/", StringComparison.Ordinal))) &&
                    !Directory.Exists(UiProjectImport.ProjectRoot(group)), "Whole-project plan is read-only and every target is under its project folder");
                var bytes = File.ReadAllBytes(two); File.AppendAllText(two, " ");
                var stale = false;
                try { UiProjectImport.Import(plan, rebuildPreviewScene: false); } catch (InvalidDataException) { stale = true; }
                finally { File.WriteAllBytes(two, bytes); }
                check(stale && !Directory.Exists(UiProjectImport.ProjectRoot(group)), "Changing a later package invalidates the batch before the first write");
                var duplicate = source("Duplicate/html", oneName);
                var duplicatePlan = UiProjectImport.Plan(directory, group, new[] { one, duplicate });
                check(!duplicatePlan.valid && duplicatePlan.errors.Any(e => e.Contains(oneName)), "Duplicate names inside a project are blocked before execution");
                var outsideOutput = Path.Combine(Path.GetDirectoryName(one), "panel.package.json");
                var originalPackage = File.ReadAllText(outsideOutput);
                var custom = UnityEngine.JsonUtility.FromJson<UiPanelPackage>(originalPackage); custom.projectName = group;
                File.WriteAllText(outsideOutput, JsonConvert.SerializeObject(custom));
                check(HtmlUiImporter.ReadPackage(outsideOutput).options.stagingRoot == UiProjectImport.ProjectRoot(group) + "/{PanelName}", "Single-package reader supports optional projectName without an explicit stagingRoot");
                File.WriteAllText(outsideOutput, originalPackage);
                var imported = UiProjectImport.Import(UiProjectImport.Plan(directory, group, new[] { one, two }), rebuildPreviewScene: false);
                check(imported.status == "completed" && imported.panels.Count(p => p.status == "imported") == 2 && File.Exists(imported.reportPath), "Selected panels import into distinct project/panel folders with a batch report");
                check(!UiProjectImport.Plan(directory, group, new[] { one, two }).valid, "Existing generated Prefabs are protected at project preflight");
                var onePanel = UiStageWorkspace.FindPanels().Single(p => p.stageRoot == UiProjectImport.StagingRoot(group, oneName));
                check(onePanel.projectName == group && onePanel.selectionKey == group + "/" + oneName, "Stage discovery exposes project grouping and an unambiguous selection key");
                var copied = UiStageWorkspace.PlanCopy(new[] { onePanel.prefabPath }, validation + "/Formal", validation + "/Sprites");
                UiStageWorkspace.CopySelected(copied, validation + "/copy-report.json");
                check(copied.assets.All(a => File.Exists(a.destination)) && !AssetDatabase.GetDependencies(validation + "/Formal/" + oneName + "/" + oneName + ".prefab", true).Any(p => p.StartsWith(UiStageWorkspace.Root + "/")), "Grouped staged panels still copy and remap into formal resources");
                var other = UiProjectImport.Import(UiProjectImport.Plan(directory, groupOther, new[] { one }), rebuildPreviewScene: false);
                var otherPanel = UiStageWorkspace.FindPanels().Single(p => p.prefabPath == other.panels[0].prefabPath);
                var scenePath = validation + "/ProjectPreview.unity";
                var ambiguous = false;
                try { UiStageWorkspace.BuildPreviewScene(new List<UiStagedPanel> { onePanel, otherPanel }, validation + "/Ambiguous.unity", oneName); }
                catch (InvalidDataException) { ambiguous = true; }
                check(ambiguous && !File.Exists(validation + "/Ambiguous.unity") && SceneManager.GetActiveScene() == original,
                    "Ambiguous unqualified names are rejected before changing any scene");
                UiStageWorkspace.BuildPreviewScene(new List<UiStagedPanel> { onePanel, otherPanel }, scenePath, otherPanel.prefabPath);
                var preview = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    var host = preview.GetRootGameObjects().Single(g => g.name == "HtmlToUGUIStage");
                    check(host.transform.Find(onePanel.selectionKey) != null && host.transform.Find(otherPanel.selectionKey).gameObject.activeSelf, "Two projects can preview the same panel name in separate hierarchy groups");
                    UiStageWorkspace.SelectPreview(preview, onePanel.selectionKey);
                    check(host.transform.Find(onePanel.selectionKey).gameObject.activeSelf && !host.transform.Find(otherPanel.selectionKey).gameObject.activeSelf, "Grouped preview selection activates exactly one panel");
                }
                finally { EditorSceneManager.CloseScene(preview, true); }
                var partialPlan = UiProjectImport.Plan(directory, groupPartial, new[] { one, two });
                UiProjectImportResult partial;
                var secondHtml = Path.Combine(Path.GetDirectoryName(two), twoName + ".html"); var htmlBytes = File.ReadAllBytes(secondHtml);
                try
                {
                    partial = UiProjectImport.Import(partialPlan, (i, count) => { if (i == 1) File.AppendAllText(secondHtml, " "); return false; }, false);
                }
                finally { File.WriteAllBytes(secondHtml, htmlBytes); }
                check(partial.status == "partial" && partial.panels[0].status == "imported" && partial.panels[1].status == "failed" &&
                    File.Exists(partial.panels[0].prefabPath) && !Directory.Exists(UiProjectImport.StagingRoot(groupPartial, twoName)), "Mid-batch source changes retain successful panels and stop without creating the failed panel");
                var cancelled = UiProjectImport.Import(UiProjectImport.Plan(directory, groupCancel, new[] { one, two }), (i, count) => true, false);
                check(cancelled.status == "cancelled" && cancelled.panels.All(p => p.status == "skipped"), "Cancellation between panels is reported without importing the remaining selection");
                check(SceneManager.GetActiveScene() == original && original.isDirty == dirty && original.rootCount == roots, "Project regression preserves the user's active scene and dirty state");
                var result = JsonConvert.SerializeObject(new { count = passed.Count, passed }, Formatting.Indented);
                File.WriteAllText(Path.Combine(evidenceDirectory, "project-import-regression.json"), result);
                return result;
            }
            finally
            {
                foreach (var target in new[] { UiProjectImport.ProjectRoot(group), UiProjectImport.ProjectRoot(groupOther), UiProjectImport.ProjectRoot(groupPartial), UiProjectImport.ProjectRoot(groupCancel), validation })
                {
                    UiStageWorkspace.ValidateAssetDestination(target);
                    if (AssetDatabase.IsValidFolder(target)) AssetDatabase.DeleteAsset(target);
                }
                UiRegressionWorkspace.RemoveEmptyRoot();
                var resolved = Path.GetFullPath(directory);
                if (resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(resolved) == "HtmlUiProject-" + token)
                    Directory.Delete(resolved, true);
            }
        }
    }
}
#endif
