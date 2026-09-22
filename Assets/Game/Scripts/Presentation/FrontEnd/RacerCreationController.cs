using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public class RacerCreationController :
        MonoBehaviour
    {
        [Header("Fields")]
        [SerializeField]
        private TMP_InputField racerNameInput;

        [Header("Actions")]
        [SerializeField]
        private Button continueButton;

        [SerializeField]
        private Button backButton;

        [Header("Feedback")]
        [SerializeField]
        private TMP_Text statusText;

        private FrontEndController frontEnd;

        public void Initialize(
            FrontEndController owner)
        {
            frontEnd =
                owner;

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    Continue);

                continueButton.onClick.AddListener(
                    Continue);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(
                    Back);

                backButton.onClick.AddListener(
                    Back);
            }
        }

        public void Present(
            NewCampaignDraft draft)
        {
            if (draft == null)
                return;

            if (racerNameInput != null)
            {
                racerNameInput.SetTextWithoutNotify(
                    draft.PlayerName ?? string.Empty);
            }

            SetStatus(
                string.Empty);
        }

        private void Continue()
        {
            if (frontEnd == null)
            {
                SetStatus(
                    "Front-end controller is unavailable.");

                return;
            }

            string racerName =
                racerNameInput != null
                    ? racerNameInput.text.Trim()
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(
                    racerName))
            {
                SetStatus(
                    "Racer name is required.");

                return;
            }

            SetStatus(
                string.Empty);

            frontEnd.AcceptRacerCreation(
                racerName);
        }

        private void Back()
        {
            frontEnd?.BackToTeamCreation();
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
