using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public class TeamCreationController :
        MonoBehaviour
    {
        [Header("Fields")]
        [SerializeField]
        private TMP_InputField teamNameInput;

        [SerializeField]
        private TMP_InputField primaryColorInput;

        [SerializeField]
        private TMP_InputField secondaryColorInput;

        [Header("Preview")]
        [SerializeField]
        private Image primaryColorPreview;

        [SerializeField]
        private Image secondaryColorPreview;

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

            if (primaryColorInput != null)
            {
                primaryColorInput.onValueChanged.RemoveListener(
                    HandlePrimaryColorChanged);

                primaryColorInput.onValueChanged.AddListener(
                    HandlePrimaryColorChanged);
            }

            if (secondaryColorInput != null)
            {
                secondaryColorInput.onValueChanged.RemoveListener(
                    HandleSecondaryColorChanged);

                secondaryColorInput.onValueChanged.AddListener(
                    HandleSecondaryColorChanged);
            }
        }

        public void Present(
            NewCampaignDraft draft)
        {
            if (draft == null)
                return;

            if (teamNameInput != null)
            {
                teamNameInput.SetTextWithoutNotify(
                    draft.TeamName ?? string.Empty);
            }

            if (primaryColorInput != null)
            {
                primaryColorInput.SetTextWithoutNotify(
                    string.IsNullOrWhiteSpace(
                        draft.PrimaryColor)
                        ? "#FFFFFF"
                        : draft.PrimaryColor);
            }

            if (secondaryColorInput != null)
            {
                secondaryColorInput.SetTextWithoutNotify(
                    string.IsNullOrWhiteSpace(
                        draft.SecondaryColor)
                        ? "#202020"
                        : draft.SecondaryColor);
            }

            RefreshColorPreviews();
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

            string teamName =
                teamNameInput != null
                    ? teamNameInput.text.Trim()
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(
                    teamName))
            {
                SetStatus(
                    "Team name is required.");

                return;
            }

            if (!TryNormalizeColor(
                    primaryColorInput != null
                        ? primaryColorInput.text
                        : null,
                    out string primaryColor,
                    out Color primaryPreview))
            {
                SetStatus(
                    "Primary color must be a valid HTML hex color, for example #FFFFFF.");

                return;
            }

            if (!TryNormalizeColor(
                    secondaryColorInput != null
                        ? secondaryColorInput.text
                        : null,
                    out string secondaryColor,
                    out Color secondaryPreview))
            {
                SetStatus(
                    "Secondary color must be a valid HTML hex color, for example #202020.");

                return;
            }

            SetPreviewColor(
                primaryColorPreview,
                primaryPreview);

            SetPreviewColor(
                secondaryColorPreview,
                secondaryPreview);

            SetStatus(
                string.Empty);

            frontEnd.AcceptTeamCreation(
                teamName,
                primaryColor,
                secondaryColor);
        }

        private void Back()
        {
            frontEnd?.CancelNewCampaign();
        }

        private void HandlePrimaryColorChanged(
            string value)
        {
            if (TryNormalizeColor(
                    value,
                    out _,
                    out Color color))
            {
                SetPreviewColor(
                    primaryColorPreview,
                    color);
            }
        }

        private void HandleSecondaryColorChanged(
            string value)
        {
            if (TryNormalizeColor(
                    value,
                    out _,
                    out Color color))
            {
                SetPreviewColor(
                    secondaryColorPreview,
                    color);
            }
        }

        private void RefreshColorPreviews()
        {
            HandlePrimaryColorChanged(
                primaryColorInput != null
                    ? primaryColorInput.text
                    : null);

            HandleSecondaryColorChanged(
                secondaryColorInput != null
                    ? secondaryColorInput.text
                    : null);
        }

        private bool TryNormalizeColor(
            string value,
            out string normalized,
            out Color color)
        {
            normalized =
                null;

            color =
                Color.white;

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }

            string candidate =
                value.Trim();

            if (!candidate.StartsWith("#"))
            {
                candidate =
                    "#" + candidate;
            }

            if (!ColorUtility.TryParseHtmlString(
                    candidate,
                    out color))
            {
                return false;
            }

            normalized =
                "#" +
                ColorUtility.ToHtmlStringRGB(
                    color);

            return true;
        }

        private void SetPreviewColor(
            Image image,
            Color color)
        {
            if (image != null)
            {
                image.color =
                    color;
            }
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
