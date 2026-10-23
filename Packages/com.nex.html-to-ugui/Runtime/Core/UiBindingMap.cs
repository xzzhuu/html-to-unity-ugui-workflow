using System;
using System.Collections.Generic;
using UnityEngine;

namespace HtmlToUGUI
{
    /// <summary>
    /// HTML -> UGUI 导入时自动写入的绑定表。
    /// </summary>
    public sealed class UiBindingMap : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class Entry
        {
            public string key;
            public Component target;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        private Dictionary<string, Component> cache;

        public IReadOnlyList<Entry> Entries { get { return entries; } }

        public T Get<T>(string key) where T : Component
        {
            EnsureCache();
            Component value;
            return cache.TryGetValue(key, out value) ? value as T : null;
        }

        public Component Get(string key)
        {
            EnsureCache();
            Component value;
            cache.TryGetValue(key, out value);
            return value;
        }

        public bool TryGet<T>(string key, out T component) where T : Component
        {
            component = Get<T>(key);
            return component != null;
        }

#if UNITY_EDITOR
        public void EditorSetEntries(List<Entry> value)
        {
            entries = value ?? new List<Entry>();
            cache = null;
        }
#endif

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { cache = null; }
        private void OnValidate() { cache = null; }

        private void EnsureCache()
        {
            if (cache != null)
                return;

            cache = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.target == null)
                    continue;
                cache[entry.key] = entry.target;
            }
        }
    }
}
