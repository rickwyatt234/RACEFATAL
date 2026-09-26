using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    internal static class CareerRuntimeUi
    {
        internal static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        internal static TMP_Text Text(Transform parent, string name, string value, Vector2 position, Vector2 size, float fontSize = 23)
        {
            var label = Rect(parent, name, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = value; label.fontSize = fontSize; label.color = new Color(.84f, .95f, .97f);
            label.richText = false; label.raycastTarget = false; return label;
        }
        internal static Button Button(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, label, position, size);
            var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.11f, .26f, .29f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Text(rect, "Label", label, Vector2.zero, size, 21).alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(action); return button;
        }
        internal static TMP_Text Scroll(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size);
            rect.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .085f);
            rect.gameObject.AddComponent<RectMask2D>();
            var text = Text(rect, "Content", "", Vector2.zero, new Vector2(size.x - 18, 0));
            text.rectTransform.anchorMax = new Vector2(1, 1); text.rectTransform.sizeDelta = new Vector2(-18, 0);
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.viewport = rect; scroll.content = text.rectTransform;
            scroll.horizontal = false; scroll.scrollSensitivity = 35; scroll.movementType = ScrollRect.MovementType.Clamped;
            return text;
        }
        internal static RectTransform Modal(Transform canvas, string name, Vector2 size, out GameObject overlay)
        {
            var root = Rect(canvas, name, Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .94f);
            overlay = root.gameObject;
            var panel = Rect(root, "Panel", Vector2.zero, size);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .085f);
            return panel;
        }
    }
}
