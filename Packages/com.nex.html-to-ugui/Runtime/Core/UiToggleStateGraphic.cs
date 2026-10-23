using UnityEngine;
using UnityEngine.UI;

namespace HtmlToUGUI
{
    /// <summary>Three full surfaces. Disabled preserves the Toggle's logical value.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Toggle))]
    public sealed class UiToggleStateGraphic : MonoBehaviour
    {
        public Image surface;
        public Sprite offSprite, onSprite, disabledSprite;
        private Toggle control;

        private void OnEnable()
        {
            control = GetComponent<Toggle>();
            control.onValueChanged.AddListener(OnValueChanged);
            Refresh();
        }
        private void OnDisable()
        {
            if (control != null) control.onValueChanged.RemoveListener(OnValueChanged);
        }
        private void OnValueChanged(bool value) { Refresh(); }
        // Also covers SetIsOnWithoutNotify, CanvasGroup changes, and interactable changes.
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            if (control == null) control = GetComponent<Toggle>();
            if (surface == null || control == null) return;
            var desired = !control.IsInteractable() ? disabledSprite : control.isOn ? onSprite : offSprite;
            if (surface.sprite != desired) surface.sprite = desired;
        }
    }
}
