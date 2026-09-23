using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerShopItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image background;

        [SerializeField] private Color normalColor =
            new Color(0.10f, 0.16f, 0.20f);

        [SerializeField] private Color selectedColor =
            new Color(0.14f, 0.37f, 0.34f);

        public void Bind(
            string label,
            Action clicked,
            bool selected)
        {
            if (labelText != null)
            {
                labelText.text =
                    label ?? string.Empty;
            }

            if (background != null)
            {
                background.color =
                    selected
                        ? selectedColor
                        : normalColor;
            }

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.interactable = true;

            if (clicked != null)
            {
                button.onClick.AddListener(
                    () => clicked());
            }
        }
    }
}
