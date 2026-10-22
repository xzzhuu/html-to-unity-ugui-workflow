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
            if (full.StartsWith(Path.GetFullPath(Application.dataPath), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Release evidence must be outside Assets.");
            if (TMP_Settings.defaultFontAsset == null) throw new InvalidOperationException("Configure an existing TMP default font before release validation.");
            Directory.CreateDirectory(full);
            File.WriteAllText(Path.Combine(full, "existing-regression.json"), HtmlUiRegressionChecks.Run());
            var contract = UiContractRegressionChecks.Run(Path.Combine(full, "contract"));
            var stage = UiStageRegressionChecks.Run(Path.Combine(full, "stage"));
            var result = JsonConvert.SerializeObject(new { status = "passed", unityVersion = Application.unityVersion,
                converterVersion = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(HtmlUiImporter).Assembly).version,
                defaultFont = AssetDatabase.GetAssetPath(TMP_Settings.defaultFontAsset), contract = JsonConvert.DeserializeObject(contract), stage = JsonConvert.DeserializeObject(stage)
            }, Formatting.Indented);
            File.WriteAllText(Path.Combine(full, "release-validation.json"), result);
            return result;
        }

        public static void BatchRun()
        {
            var directory = Environment.GetEnvironmentVariable("HTML_UI_EVIDENCE_DIRECTORY");
            if (string.IsNullOrEmpty(directory)) throw new InvalidOperationException("Set HTML_UI_EVIDENCE_DIRECTORY to an external directory.");
            try { Run(directory); if (Application.isBatchMode) EditorApplication.Exit(0); }
            catch (Exception error)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "release-failure.json"), error.ToString());
                Debug.LogException(error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
#endif
