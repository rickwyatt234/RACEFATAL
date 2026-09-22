using System;
using System.Globalization;
using RaceFatal.Infrastructure.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public class SaveSlotView : MonoBehaviour
    {
        [Min(1)][SerializeField]
        private int slotIndex = 1;

        [Header("Interaction")]
        [SerializeField] private Button selectButton;

        [Header("Text")]
        [SerializeField] private TMP_Text slotLabel;
        [SerializeField] private TMP_Text teamNameText;
        [SerializeField] private TMP_Text racerNameText;
        [SerializeField] private TMP_Text resourcesText;
        [SerializeField] private TMP_Text lastSavedText;
        [SerializeField] private TMP_Text statusText;

        private Action<int> onSelected;

        public int SlotIndex =>
            slotIndex;

        public void Initialize(
            Action<int> selectionCallback)
        {
            onSelected =
                selectionCallback;

            if (selectButton == null)
                return;

            selectButton.onClick.RemoveListener(
                HandleSelected);

            selectButton.onClick.AddListener(
                HandleSelected);
        }

        public void Bind(
            SaveSlotSummary summary)
        {
            if (summary == null)
            {
                BindError(
                    "Save summary is missing.");

                return;
            }

            slotIndex =
                summary.SlotIndex;

            SetText(
                slotLabel,
                $"SAVE {slotIndex:00}");

            if (!summary.Exists)
            {
                SetText(
                    teamNameText,
                    "EMPTY SLOT");

                SetText(
                    racerNameText,
                    "CREATE NEW CAMPAIGN");

                SetText(
                    resourcesText,
                    string.Empty);

                SetText(
                    lastSavedText,
                    string.Empty);

                SetText(
                    statusText,
                    "AVAILABLE");

                if (selectButton != null)
                {
                    selectButton.interactable =
                        true;
                }

                return;
            }

            SetText(
                teamNameText,
                string.IsNullOrWhiteSpace(
                    summary.TeamName)
                    ? "UNNAMED TEAM"
                    : summary.TeamName);

            SetText(
                racerNameText,
                string.IsNullOrWhiteSpace(
                    summary.RacerName)
                    ? "NO ACTIVE RACER"
                    : summary.RacerName);

            SetText(
                resourcesText,
                $"CREDITS {summary.Credits:N0}  //  " +
                $"FAME {summary.TeamFame:N0}  //  " +
                $"RP {summary.ResearchPoints:N0}");

            SetText(
                lastSavedText,
                FormatLastSaved(
                    summary.LastSavedUtc));

            string status =
                summary.CareerActive
                    ? "CAREER ACTIVE"
                    : summary.HasCareerRun
                        ? "CAREER INACTIVE"
                        : "NO ACTIVE RACER";

            SetText(
                statusText,
                status);

            if (selectButton != null)
            {
                selectButton.interactable =
                    true;
            }
        }

        public void BindError(
            string message)
        {
            SetText(
                slotLabel,
                $"SAVE {slotIndex:00}");

            SetText(
                teamNameText,
                "UNREADABLE SAVE");

            SetText(
                racerNameText,
                string.Empty);

            SetText(
                resourcesText,
                string.Empty);

            SetText(
                lastSavedText,
                string.Empty);

            SetText(
                statusText,
                message);

            if (selectButton != null)
            {
                selectButton.interactable =
                    false;
            }
        }

        private void HandleSelected()
        {
            onSelected?.Invoke(
                slotIndex);
        }

        private string FormatLastSaved(
            string lastSavedUtc)
        {
            if (string.IsNullOrWhiteSpace(
                    lastSavedUtc))
            {
                return string.Empty;
            }

            if (!DateTimeOffset.TryParse(
                    lastSavedUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset savedAt))
            {
                return lastSavedUtc;
            }

            return
                "LAST SAVED  " +
                savedAt
                    .ToLocalTime()
                    .ToString(
                        "yyyy-MM-dd  HH:mm");
        }

        private void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
            {
                target.text =
                    value ?? string.Empty;
            }
        }
    }
}
