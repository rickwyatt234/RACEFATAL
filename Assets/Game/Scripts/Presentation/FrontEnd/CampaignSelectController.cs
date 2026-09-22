using System;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;

namespace RaceFatal.Presentation.FrontEnd
{
    public class CampaignSelectController :
        MonoBehaviour
    {
        [SerializeField]
        private SaveSlotView[] slotViews =
            Array.Empty<SaveSlotView>();

        [SerializeField]
        private TMP_Text statusText;

        private FrontEndController frontEnd;
        private CampaignSaveService saves;

        public void Initialize(
            FrontEndController owner,
            CampaignSaveService saveService)
        {
            frontEnd =
                owner;

            saves =
                saveService;

            if (slotViews == null)
                return;

            foreach (SaveSlotView slotView
                     in slotViews)
            {
                if (slotView == null)
                    continue;

                slotView.Initialize(
                    HandleSlotSelected);
            }
        }

        public void Refresh()
        {
            if (saves == null)
            {
                SetStatus(
                    "Save service is unavailable.");

                return;
            }

            SetStatus(
                string.Empty);

            if (slotViews == null)
                return;

            foreach (SaveSlotView slotView
                     in slotViews)
            {
                if (slotView == null)
                    continue;

                Result<SaveSlotSummary> result =
                    saves.GetSlotSummary(
                        slotView.SlotIndex);

                if (!result.IsSuccess)
                {
                    slotView.BindError(
                        result.ErrorMessage);

                    SetStatus(
                        result.ErrorMessage);

                    continue;
                }

                slotView.Bind(
                    result.Value);
            }
        }

        private void HandleSlotSelected(
            int slotIndex)
        {
            if (frontEnd == null)
            {
                SetStatus(
                    "Front-end controller is unavailable.");

                return;
            }

            frontEnd.SelectCampaignSlot(
                slotIndex);
        }

        private void SetStatus(
            string message)
        {
            if (statusText != null)
            {
                statusText.text =
                    message ?? string.Empty;
            }
        }
    }
}
