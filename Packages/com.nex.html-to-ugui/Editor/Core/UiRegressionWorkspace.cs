#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;

namespace HtmlToUGUI.Editor
{
    internal static class UiRegressionWorkspace
    {
        public const string Root = "Assets/HtmlUiValidation";

        // A different fixture or batch baseline may still own this shared directory.
        public static void RemoveEmptyRoot()
        {
            if (Directory.Exists(Root) && !Directory.EnumerateFileSystemEntries(Root).Any() &&
                !AssetDatabase.DeleteAsset(Root))
                throw new IOException("Could not remove the empty regression workspace: " + Root);
        }
    }
}
#endif
