using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    // An editor-authored template used by all three dynamic garage lists.
    public class CareerGarageOptionView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image background;

        [SerializeField] private Color normalColor = new Color(0.10f, 0.16f, 0.20f);
        [SerializeField] private Color selectedColor = new Color(0.14f, 0.37f, 0.34f);
        [SerializeField] private Color disabledColor = new Color(0.08f, 0.10f, 0.12f);

        public void Bind(
            string label,
            Action clicked,
            bool interactable,
            bool selected)
        {
            if (labelText != null)
                labelText.text = label ?? string.Empty;

            if (background != null)
                background.color = !interactable
                    ? disabledColor
                    : selected
                        ? selectedColor
                        : normalColor;

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.interactable = interactable;

            if (clicked != null && interactable)
                button.onClick.AddListener(() => clicked());
        }
    }
}