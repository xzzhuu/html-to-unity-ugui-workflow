#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HtmlToUGUI.Editor
{
    public static class UiVisualAudit
    {
        [Serializable] public sealed class Node
        {
            public string key, component, text;
            public float x, y, width, height;
        }
        [Serializable] public sealed class Snapshot
        {
            public string schemaVersion = "html-ugui/1", status = "pending-visual-review";
            public int width, height;
            public List<Node> bindings = new List<Node>();
            public List<string> issues = new List<string>();
        }
        // Uses a disposable preview scene so the user's open scene is preserved.
        public static Snapshot Capture(GameObject prefab, string outputFolder, Action<GameObject> populate = null)
        {
            Directory.CreateDirectory(outputFolder);
            var scene = EditorSceneManager.NewPreviewScene();
            RenderTexture rt = null;
            Texture2D pixels = null;
            Camera auditCamera = null;
            var previous = RenderTexture.active;
            try
            {
                var resolution = prefab.GetComponent<RectTransform>().sizeDelta;
                var result = new Snapshot { width = Mathf.RoundToInt(resolution.x), height = Mathf.RoundToInt(resolution.y) };
                var cameraGo = new GameObject("AuditCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraGo, scene);
                var cam = auditCamera = cameraGo.GetComponent<Camera>();
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.transform.position = new Vector3(0, 0, -100);
                cam.orthographic = true; cam.orthographicSize = resolution.y / 2;
                cam.nearClipPlane = .1f; cam.farClipPlane = 1000;
                rt = new RenderTexture(result.width, result.height, 24);
                cam.targetTexture = rt;
                var canvasGo = new GameObject("AuditCanvas", typeof(Canvas));
                SceneManager.MoveGameObjectToScene(canvasGo, scene);
                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 100;
                var instance = UnityEngine.Object.Instantiate(prefab, canvasGo.transform, false);
                var root = instance.GetComponent<RectTransform>();
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
                root.anchoredPosition = Vector2.zero; root.sizeDelta = resolution;
                if (populate != null) populate(instance);
                Canvas.ForceUpdateCanvases();
                foreach (var text in instance.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate(true);
                    if (text.font == null) result.issues.Add(text.name + ": no TMP font");
                    else
                    {
                        uint[] missing;
                        if (!text.font.HasCharacters(text.text, out missing, true, true)) result.issues.Add(text.name + ": missing glyphs");
                    }
                    if (text.isTextOverflowing) result.issues.Add(text.name + ": text overflow");
                }
                var map = instance.GetComponent<UiBindingMap>();
                if (map != null) foreach (var entry in map.Entries)
                {
                    var rect = entry.target.transform as RectTransform;
                    if (rect == null) continue;
                    var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    var min = root.InverseTransformPoint(corners[0]); var max = root.InverseTransformPoint(corners[2]);
                    var tmp = entry.target as TMP_Text;
                    result.bindings.Add(new Node { key = entry.key, component = entry.target.GetType().Name,
                        text = tmp == null ? null : tmp.text, x = min.x + resolution.x / 2, y = resolution.y / 2 - max.y,
                        width = max.x - min.x, height = max.y - min.y });
                    if (min.x < -resolution.x / 2 - 1 || max.x > resolution.x / 2 + 1 || min.y < -resolution.y / 2 - 1 || max.y > resolution.y / 2 + 1)
                        result.issues.Add(entry.key + ": outside design canvas");
                }
                cam.Render(); RenderTexture.active = rt;
                pixels = new Texture2D(result.width, result.height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Path.Combine(outputFolder, prefab.name + ".unity.png"), pixels.EncodeToPNG());
                File.WriteAllText(Path.Combine(outputFolder, prefab.name + ".unity.json"), JsonUtility.ToJson(result, true));
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                if (auditCamera != null) auditCamera.targetTexture = null;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
#endif
