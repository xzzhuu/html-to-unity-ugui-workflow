#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace HtmlToUGUI.Editor
{
    // A release gate for a prepared validation project with its own licensed Editor
    // and existing TMP default font. Evidence always stays outside Assets.
    public static class UiReleaseValidation
    {
        public static string Run(string evidenceDirectory)
        {
            var full = Path.GetFullPath(evidenceDirectory);
            var assetsDirectory = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (full.Equals(assetsDirectory, StringComparison.OrdinalIgnoreCase) || full.StartsWith(assetsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release evidence must be outside Assets.");
            Directory.CreateDirectory(full);
            var successPath = Path.Combine(full, "release-validation.json");
            var failurePath = Path.Combine(full, "release-failure.json");
            // Interactive callers must not mistake a previous success for this run.
            if (File.Exists(successPath)) File.Delete(successPath);
            if (File.Exists(failurePath)) File.Delete(failurePath);
            try
            {
            if (TMP_Settings.defaultFontAsset == null) throw new InvalidOperationException("Configure an existing TMP default font before release validation.");
            File.WriteAllText(Path.Combine(full, "existing-regression.json"), HtmlUiRegressionChecks.Run());
            var contract = UiContractRegressionChecks.Run(Path.Combine(full, "contract"));
            var stage = UiStageRegressionChecks.Run(Path.Combine(full, "stage"));
            var project = UiProjectImportRegressionChecks.Run(Path.Combine(full, "project"));
            var assets = UiProjectAssetRegressionChecks.Run(Path.Combine(full, "assets"));
            var result = JsonConvert.SerializeObject(new { status = "passed", unityVersion = Application.unityVersion,
                converterVersion = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HtmlUiImporter).Assembly).version,
                defaultFont = AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset), contract = JsonConvert.DeserializeObject(contract), stage = JsonConvert.DeserializeObject(stage), project = JsonConvert.DeserializeObject(project), assets = JsonConvert.DeserializeObject(assets)
            }, Formatting.Indented);
            File.WriteAllText(successPath, result);
            return result;
            }
            catch (Exception error)
            {
                File.WriteAllText(failurePath, error.ToString());
                throw;
            }
        }

        public static void BatchRun()
        {
            var directory = Environment.GetEnvironmentVariable("HTML_UI_EVIDENCE_DIRECTORY");
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Set HTML_UI_EVIDENCE_DIRECTORY to an external directory.");
            string baseline = null;
            var failed = false;
            try
            {
                // A fresh batch Editor starts in an untitled scene. Unity rejects the
                // additive preview checks until that scene has a saved identity.
                if (Application.isBatchMode)
                {
                    UiStageWorkspace.EnsureFolder(UiRegressionWorkspace.Root);
                    baseline = UiRegressionWorkspace.Root + "/BatchBaseline-" + Guid.NewGuid().ToString("N") + ".unity";
                    var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                        UnityEditor.SceneManagement.NewSceneMode.Single);
                    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, baseline))
                        throw new IOException("Could not save batch validation baseline scene.");
                }
                Run(directory);
            }
            catch (Exception error)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "release-failure.json"), error.ToString());
                Debug.LogException(error);
                if (!Application.isBatchMode) throw;
                failed = true;
            }
            finally
            {
                if (baseline != null)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                        UnityEditor.SceneManagement.NewSceneMode.Single);
                    AssetDatabase.DeleteAsset(baseline);
                    UiRegressionWorkspace.RemoveEmptyRoot();
                }
            }
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        }
    }
}
#endif
