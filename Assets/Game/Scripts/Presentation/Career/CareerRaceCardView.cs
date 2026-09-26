using System;
using RaceFatal.Racing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerRaceCardView :
        MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] private TMP_Text raceNameText;
        [SerializeField] private TMP_Text trackNameText;
        [SerializeField] private TMP_Text requirementsText;
        [SerializeField] private TMP_Text availabilityText;

        [Header("Interaction")]
        [SerializeField] private Button selectButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color standardColor =
            new Color(0.12f, 0.13f, 0.16f, 1f);
        [SerializeField] private Color selectedColor =
            new Color(0.19f, 0.28f, 0.32f, 1f);

        private string raceId;
        private Action<string> onSelected;

        public string RaceId => raceId;

        public void BindEvent(string eventId, string name, string type, string requirements, string status, Action<string> selected)
        {
            raceId = eventId;
            onSelected = selected;
            SetText(raceNameText, name);
            SetText(trackNameText, type);
            SetText(requirementsText, requirements);
            SetText(availabilityText, status);
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(Select);
                selectButton.onClick.AddListener(Select);
                selectButton.interactable = true;
            }
            SetSelected(false);
        }

        public void Bind(
            RaceDefinition race,
            string trackName,
            bool available,
            Action<string> selectionCallback)
        {
            if (race == null)
                throw new ArgumentNullException(nameof(race));

            raceId = race.Id;
            onSelected = selectionCallback;

            SetText(raceNameText, race.DisplayName);
            SetText(trackNameText, "TRACK  " + trackName);
            SetText(
                requirementsText,
                $"ENGINE  {race.EngineClass}   //   " +
                $"{race.LapCount} LAPS   //   " +
                $"{race.EntrantCount} RACERS   //   " +
                $"{race.TeamSize} PER TEAM");
            SetText(
                availabilityText,
                available ? "AVAILABLE" : "INELIGIBLE");

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(Select);
                selectButton.onClick.AddListener(Select);
                selectButton.interactable = true;
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (backgroundImage != null)
                backgroundImage.color = selected
                    ? selectedColor
                    : standardColor;
        }

        private void Select()
        {
            onSelected?.Invoke(raceId);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(Select);
        }

        private static void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }
}
