#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    [Serializable]
    public sealed class UiStagedPanel
    {
        public string panelName, prefabPath, stageRoot, reportPath;
    }

    // Persistent review assets belong here; synthetic regression assets do not.
    public static partial class UiStageWorkspace
    {
        public const string Root = "Assets/HtmlToUGUIStage";
        public const string ScenePath = Root + "/StageScene.unity";
        public const string DefaultStagingRoot = Root + "/{PanelName}";

        public static List<UiStagedPanel> FindPanels()
        {
            if (!AssetDatabase.IsValidFolder(Root)) return new List<UiStagedPanel>();
            return AssetDatabase.FindAssets("t:Prefab", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileName(Path.GetDirectoryName(p)) == "Prefabs" &&
                    Path.GetFileNameWithoutExtension(p).EndsWith("Panel", StringComparison.Ordinal) &&
                    File.Exists(Path.ChangeExtension(p, ".import-report.json")))
                .Select(p => new UiStagedPanel { panelName = Path.GetFileNameWithoutExtension(p), prefabPath = p,
                    stageRoot = Path.GetDirectoryName(Path.GetDirectoryName(p)).Replace('\\', '/'),
                    reportPath = Path.ChangeExtension(p, ".import-report.json") })
                .OrderBy(p => p.panelName, StringComparer.Ordinal).ToList();
        }

        internal static void ValidateAssetDestination(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal) || path.Contains('\\') ||
                path.Split('/').Any(p => p == "." || p == ".." || p.Length == 0))
                throw new InvalidDataException("Expected a project-relative Assets path: " + path);
            var assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Destination escapes Assets: " + path);
        }

        internal static void EnsureFolder(string folder)
        {
            ValidateAssetDestination(folder);
            var segments = folder.Split('/');
            var current = "Assets";
            foreach (var part in segments.Skip(1))
            {
                var next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next) && string.IsNullOrEmpty(AssetDatabase.CreateFolder(current, part)))
                    throw new IOException("Could not create folder: " + next);
                current = next;
            }
        }

        public static void RebuildScene(string selectedPanel = null)
        {
            BuildPreviewScene(FindPanels(), ScenePath, selectedPanel);
        }

        internal static void BuildPreviewScene(List<UiStagedPanel> panels, string scenePath, string selectedPanel)
        {
            UiPanelScenePlacement.ValidateContext();
            ValidateAssetDestination(scenePath);
            var loaded = SceneManager.GetSceneByPath(scenePath);
            if (loaded.IsValid() && loaded.isLoaded && loaded.isDirty)
                throw new InvalidOperationException("StageScene has unsaved changes. Save or discard them before rebuilding it.");
            if (panels.Select(p => p.panelName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != panels.Count)
                throw new InvalidDataException("Duplicate staged panel names must be resolved before creating the preview scene.");
            foreach (var panel in panels)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(panel.prefabPath) == null)
                    throw new InvalidDataException("Missing staged prefab: " + panel.prefabPath);
            EnsureFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'));
            var active = SceneManager.GetActiveScene();
            var keepOpen = loaded.IsValid() && loaded.isLoaded;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                // Open the replacement before closing a loaded StageScene, including
                // the case where StageScene was the only open scene.
                if (keepOpen) EditorSceneManager.CloseScene(loaded, true);
                SceneManager.SetActiveScene(scene);
                var camera = new GameObject("StageCamera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.18f, .20f, .23f, 1);
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                var host = new GameObject("HtmlToUGUIStage");
                var selected = panels.FirstOrDefault(p => p.panelName == selectedPanel) ?? panels.FirstOrDefault();
                foreach (var panel in panels)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(panel.prefabPath);
                    var size = prefab.GetComponent<RectTransform>().sizeDelta;
                    var canvasGo = new GameObject(panel.panelName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvasGo.transform.SetParent(host.transform, false);
                    canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    var scaler = canvasGo.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = size;
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasGo.transform);
                    var rect = instance.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition3D = Vector3.zero;
                    rect.localScale = Vector3.one;
                    rect.sizeDelta = size;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                    canvasGo.SetActive(selected == panel);
                }
                var events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (type != null) events.AddComponent(type);
#else
                events.AddComponent<StandaloneInputModule>();
#endif
                if (!EditorSceneManager.SaveScene(scene, scenePath)) throw new IOException("Could not save preview scene: " + scenePath);
            }
            finally
            {
                if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
                if (!keepOpen && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void Preview(string panelName)
        {
            UiPanelScenePlacement.ValidateContext();
            if (!FindPanels().Any(p => p.panelName == panelName)) throw new InvalidDataException("No staged panel: " + panelName);
            var current = SceneManager.GetActiveScene();
            if (current.path == ScenePath)
            {
                FocusPreview(SelectPreview(current, panelName));
                return;
            }
            // The normal Unity save prompt only applies when the user explicitly
            // opens a scene. Automatic imports never close the user's scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            RebuildScene(panelName);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            FocusPreview(SelectPreview(scene, panelName));
        }

        internal static GameObject SelectPreview(Scene scene, string panelName)
        {
            var host = scene.GetRootGameObjects().Single(g => g.name == "HtmlToUGUIStage");
            var target = host.transform.Find(panelName);
            if (target == null) throw new InvalidDataException("Panel is not in StageScene. Rebuild the scene after saving pending scene edits.");
            var wasDirty = scene.isDirty;
            foreach (Transform child in host.transform) child.gameObject.SetActive(child == target);
            EditorSceneManager.MarkSceneDirty(scene);
            // Preserve pending user edits. Switching a preview in an already open
            // stage scene never opens a save dialog or silently saves those edits.
            if (!wasDirty && !EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save StageScene selection.");
            return target.GetChild(0).gameObject;
        }

        private static void FocusPreview(GameObject target)
        {
            Selection.activeGameObject = target;
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }
}
#endif
