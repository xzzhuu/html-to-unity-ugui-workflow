#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class UiPanelScenePlacement
    {
        public static void ValidateContext()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Import panels in a normal scene in Edit Mode.");
        }

        internal static void CleanupTransientMeshesForPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var map in Resources.FindObjectsOfTypeAll<UiBindingMap>())
                if (!EditorUtility.IsPersistent(map) && map.gameObject.scene.IsValid() &&
                    PrefabUtility.GetCorrespondingObjectFromSource(map.gameObject) == prefab)
                    CleanupTransientMeshes(map.gameObject);
        }

        private static void CleanupTransientMeshes(GameObject instance)
        {
            // Prefab root/child renames can leave TMP DontSave meshes detached from their text.
            // Remove only generated, disconnected orphan meshes; TMP recreates valid glyph meshes.
            foreach (var mesh in instance.GetComponentsInChildren<TMPro.TMP_SubMeshUI>(true))
                if (mesh.transform.parent != null && mesh.transform.parent.GetComponent<TMPro.TMP_Text>() == null &&
                    PrefabUtility.GetCorrespondingObjectFromSource(mesh) == null &&
                    (mesh.gameObject.hideFlags & HideFlags.DontSave) != 0)
                    UnityEngine.Object.DestroyImmediate(mesh.gameObject);
        }

        public static GameObject Place(string prefabPath)
        {
            ValidateContext();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || prefab.GetComponent<RectTransform>() == null)
                throw new ArgumentException("A saved UI panel prefab is required.");
            var scene = SceneManager.GetActiveScene();
            var selected = Selection.activeGameObject;
            var canvas = selected != null && selected.scene == scene ? selected.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Canvas>(false))
                    .OrderByDescending(c => c.name == "Canvas").FirstOrDefault(c => c.isRootCanvas && c.isActiveAndEnabled);
            var size = prefab.GetComponent<RectTransform>().sizeDelta;
            if (canvas == null)
            {
                var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create UI Canvas");
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = size;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            var instance = canvas.transform.Cast<Transform>().Select(t => t.gameObject)
                .FirstOrDefault(g => PrefabUtility.GetCorrespondingObjectFromSource(g) == prefab);
            if (instance == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Import UI Panel");
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition3D = Vector3.zero;
                rect.localScale = Vector3.one;
                rect.sizeDelta = size;
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
            if (!scene.GetRootGameObjects().Any(g => g.GetComponentInChildren<EventSystem>(true) != null))
            {
                var go = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create UI EventSystem");
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                var type = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (type != null) go.AddComponent(type);
#else
                go.AddComponent<StandaloneInputModule>();
#endif
            }
            CleanupTransientMeshes(instance);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = instance;
            return instance;
        }
    }
}
#endif
