using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Spotlight.UI3
{
    [RequireComponent(typeof(Button), typeof(CanvasGroup))]
    public sealed class UI3ButtonFeedback : MonoBehaviour, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform visual;
        public TMP_Text label;
        public Image background;
        public Sprite idleSprite, pressedSprite;
        public Color disabledTextColor = new Color32(183, 148, 113, 255);
        private Color normalTextColor;
        private Button button;
        private bool pressed;
        private void Awake() { button = GetComponent<Button>(); if (label) normalTextColor = label.color; }
        private void OnEnable()
        {
            pressed = false;
            if (visual) { visual.localScale = Vector3.one; visual.localRotation = Quaternion.identity; }
        }
        private void Update() { ApplyState(); }
        private void ApplyState()
        {
            if (!button || !visual) return;
            bool usable = button.IsInteractable();
            if (label) label.color = usable ? normalTextColor : disabledTextColor;
            if (background && idleSprite) background.sprite = usable && pressed && pressedSprite ? pressedSprite : idleSprite;
        }
        private void OnDisable()
        {
            if (visual) { visual.localScale = Vector3.one; visual.localRotation = Quaternion.identity; }
            if (background && idleSprite) background.sprite = idleSprite;
            if (label && button) label.color = normalTextColor;
        }
        public void OnPointerExit(PointerEventData e) { pressed = false; ApplyState(); }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left && button.IsInteractable()) pressed = true; ApplyState(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; ApplyState(); }
    }
}
