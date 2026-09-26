using System;
using System.Collections.Generic;
using UnityEngine;

namespace HtmlToUGUI
{
    public sealed class UiTemplateCatalog : MonoBehaviour
    {
        [Serializable] public sealed class Entry { public string name; public GameObject prefab; }
        [SerializeField] private List<Entry> templates = new List<Entry>();
        public UiBindingMap CreateItem(string templateName, Transform parent)
        {
            var entry = templates.Find(e => e.name == templateName);
            if (entry == null || entry.prefab == null) throw new ArgumentException("Unknown template: " + templateName);
            var name = UiObjectNaming.UniqueUnder(parent, entry.prefab.name, true);
            var instance = Instantiate(entry.prefab, parent, false);
            instance.name = name;
            return instance.GetComponent<UiBindingMap>();
        }
#if UNITY_EDITOR
        public void EditorSetTemplates(List<Entry> value) { templates = value; }
#endif
    }
}
