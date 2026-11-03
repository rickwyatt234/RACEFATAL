using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public class CampaignReviewController : MonoBehaviour
    {
        [Header("Summary")] [SerializeField] private TMP_Text slotText;
        [SerializeField] private TMP_Text teamNameText;
        [SerializeField] private TMP_Text racerNameText;
        [SerializeField] private TMP_Text colorsText;
        [SerializeField] private Image primaryColorPreview;
        [SerializeField] private Image secondaryColorPreview;
        [Header("Actions")] [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;
        [Header("Feedback")] [SerializeField] private TMP_Text statusText;
        private FrontEndController frontEnd;
        public void Initialize(FrontEndController owner)
        {
            frontEnd = owner;
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                confirmButton.onClick.AddListener(Confirm);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Back);
                backButton.onClick.AddListener(Back);
            }
        }

        public void Present(NewCampaignDraft draft)
        {
            if (draft == null)
                return;
            var placeholder = teamNameText != null ? teamNameText.transform.parent.Find("StartingContentInfo") : null;
            if (placeholder != null) placeholder.gameObject.SetActive(false);
            if (startingInfo != null) startingInfo.text = "";
            SetText(slotText, $"SAVE {draft.SlotIndex:00}");
            SetText(teamNameText, draft.TeamName);
            SetText(racerNameText, draft.PlayerName);
            SetText(colorsText, $"PRIMARY {draft.PrimaryColor}  //  SECONDARY {draft.SecondaryColor}");
            ApplyColor(primaryColorPreview, draft.PrimaryColor);
            ApplyColor(secondaryColorPreview, draft.SecondaryColor);
            SetBusy(false);
            SetStatus(string.Empty);
        }

        [SerializeField] private TMP_Text startingInfo;
        public void PresentStartingInfo(RaceFatal.Career.GameSessionState session, RaceFatal.Data.GameDatabase database)
        {
            if (startingInfo != null)
                startingInfo.text = RaceFatal.Presentation.Career.CareerSuccessionView.BuildStartingSummary(session, database);
        }

#if UNITY_EDITOR
        public void EditorAuthor()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Editor only");
            if (startingInfo != null) return;
            foreach (var label in new[] {teamNameText, racerNameText, colorsText})
                if (label != null) { label.rectTransform.sizeDelta = new Vector2(650, 100); label.enableAutoSizing = true; label.fontSizeMin = 18; }
            if (startingInfo == null && teamNameText != null)
                startingInfo = RaceFatal.Presentation.Career.EditorUiFactory.Scroll(teamNameText.transform.parent,
                    "StartingLoadout", new Vector2(780, -205), new Vector2(1020, 620));
        }
#endif
        public void SetBusy(bool busy)
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = !busy;
            }

            if (backButton != null)
            {
                backButton.interactable = !busy;
            }
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }

        private void Confirm()
        {
            frontEnd?.ConfirmNewCampaign();
        }

        private void Back()
        {
            frontEnd?.BackToRacerCreation();
        }

        private void ApplyColor(Image image, string htmlColor)
        {
            if (image == null || string.IsNullOrWhiteSpace(htmlColor))
            {
                return;
            }

            if (ColorUtility.TryParseHtmlString(htmlColor, out Color color))
            {
                image.color = color;
            }
        }

        private void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }
    }
}
