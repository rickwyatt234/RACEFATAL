using RaceFatal.Career;
using RaceFatal.Content.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RaceFatal.Presentation.FrontEnd
{
    public class FrontEndController :
        MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject bootRoot;
        [SerializeField] private GameObject pressAnyInputRoot;
        [SerializeField] private GameObject mainMenuRoot;
        [SerializeField] private GameObject campaignSelectRoot;
        [SerializeField] private GameObject teamCreationRoot;
        [SerializeField] private GameObject racerCreationRoot;
        [SerializeField] private GameObject campaignReviewRoot;
        [SerializeField] private GameObject optionsRoot;
        [SerializeField] private GameObject creditsRoot;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button raceFatalButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button disconnectButton;

        [Header("Back Buttons")]
        [SerializeField] private Button campaignBackButton;
        [SerializeField] private Button teamCreationBackButton;
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Button creditsBackButton;

        [Header("Controllers")]
        [SerializeField]
        private CampaignSelectController
            campaignSelectController;

        [SerializeField]
        private TeamCreationController
            teamCreationController;

        [SerializeField]
        private RacerCreationController
            racerCreationController;

        [SerializeField]
        private CampaignReviewController
            campaignReviewController;

        [Header("New Campaign")]
        [SerializeField]
        private NewCampaignDefaultsSO
            newCampaignDefaults;

        [Header("Feedback")]
        [SerializeField]
        private TMP_Text errorText;

        [Header("Startup")]
        [Min(0f)][SerializeField]
        private float bootDuration = 0.75f;

        [SerializeField]
        private string careerSceneName =
            "01_Career";

        private CampaignSaveService saves;
        private NewCampaignDraft pendingCampaign;
        private float bootElapsed;

        public FrontEndScreen CurrentScreen {
            get;
            private set;
        }

        public int? PendingCampaignSlotIndex {
            get;
            private set;
        }

        public NewCampaignDraft PendingCampaign =>
            pendingCampaign;

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            GameContext context =
                BootstrapController.Context;

            if (context == null)
            {
                ShowError(
                    "GameContext is unavailable. Start the game from 00_Bootstrap.");

                return;
            }

            saves =
                context.Saves;

            campaignSelectController?.Initialize(
                this,
                saves);

            teamCreationController?.Initialize(
                this);

            racerCreationController?.Initialize(
                this);

            campaignReviewController?.Initialize(
                this);

            bootElapsed = 0f;

            ShowScreen(
                bootRoot != null
                    ? FrontEndScreen.Boot
                    : FrontEndScreen.PressAnyInput);
        }

        private void Update()
        {
            if (CurrentScreen ==
                FrontEndScreen.Boot)
            {
                bootElapsed +=
                    Time.unscaledDeltaTime;

                if (bootElapsed >=
                    bootDuration)
                {
                    ShowScreen(
                        FrontEndScreen.PressAnyInput);
                }

                return;
            }

            if (CurrentScreen !=
                FrontEndScreen.PressAnyInput)
            {
                return;
            }

            if (Input.anyKeyDown ||
                Input.GetMouseButtonDown(0) ||
                Input.GetMouseButtonDown(1) ||
                Input.GetMouseButtonDown(2))
            {
                ShowScreen(
                    FrontEndScreen.MainMenu);
            }
        }

        public void OpenCampaignSelect()
        {
            ClearPendingCampaign();

            ShowScreen(
                FrontEndScreen.CampaignSelect);
        }

        public void OpenOptions()
        {
            ShowScreen(
                FrontEndScreen.Options);
        }

        public void OpenCredits()
        {
            ShowScreen(
                FrontEndScreen.Credits);
        }

        public void BackToMainMenu()
        {
            ClearPendingCampaign();

            ShowScreen(
                FrontEndScreen.MainMenu);
        }

        public void CancelNewCampaign()
        {
            ClearPendingCampaign();

            ShowScreen(
                FrontEndScreen.CampaignSelect);
        }

        public void AcceptTeamCreation(
            string teamName,
            string primaryColor,
            string secondaryColor)
        {
            if (!TryGetPendingCampaign(
                    out NewCampaignDraft draft))
            {
                return;
            }

            draft.SetTeam(
                teamName,
                primaryColor,
                secondaryColor);

            ShowScreen(
                FrontEndScreen.RacerCreation);
        }

        public void BackToTeamCreation()
        {
            if (!TryGetPendingCampaign(
                    out _))
            {
                return;
            }

            ShowScreen(
                FrontEndScreen.TeamCreation);
        }

        public void AcceptRacerCreation(
            string racerName)
        {
            if (!TryGetPendingCampaign(
                    out NewCampaignDraft draft))
            {
                return;
            }

            draft.SetPlayer(
                racerName);

            ShowScreen(
                FrontEndScreen.CampaignReview);
        }

        public void BackToRacerCreation()
        {
            if (!TryGetPendingCampaign(
                    out _))
            {
                return;
            }

            ShowScreen(
                FrontEndScreen.RacerCreation);
        }

        public void ConfirmNewCampaign()
        {
            if (!TryGetPendingCampaign(
                    out NewCampaignDraft draft))
            {
                return;
            }

            if (!draft.IsComplete)
            {
                ShowCampaignReviewError(
                    "Campaign creation data is incomplete.");

                return;
            }

            if (saves == null)
            {
                ShowCampaignReviewError(
                    "Save service is unavailable.");

                return;
            }

            if (newCampaignDefaults == null)
            {
                ShowCampaignReviewError(
                    "New campaign defaults are not assigned.");

                return;
            }

            if (!newCampaignDefaults.TryGetDefinitionIds(
                    out string partnerDefinitionId,
                    out string playerStarterBuildId,
                    out string partnerStarterBuildId,
                    out string defaultsError))
            {
                ShowCampaignReviewError(
                    defaultsError);

                return;
            }

            if (!CanLoadCareerScene())
            {
                return;
            }

            var request =
                new NewGameRequest(
                    draft.TeamName,
                    draft.PrimaryColor,
                    draft.SecondaryColor,
                    draft.PlayerName,
                    partnerDefinitionId,
                    playerStarterBuildId,
                    partnerStarterBuildId);

            campaignReviewController?.SetBusy(
                true);

            campaignReviewController?.SetStatus(
                "CREATING CAMPAIGN...");

            Result<GameSessionState> result =
                saves.CreateCampaign(
                    draft.SlotIndex,
                    request);

            if (!result.IsSuccess)
            {
                campaignReviewController?.SetBusy(
                    false);

                ShowCampaignReviewError(
                    result.ErrorMessage);

                return;
            }

            ClearPendingCampaign();

            SceneManager.LoadScene(
                careerSceneName);
        }

        public void SelectCampaignSlot(
            int slotIndex)
        {
            if (saves == null)
            {
                ShowError(
                    "Save service is unavailable.");

                return;
            }

            Result<SaveSlotSummary> summaryResult =
                saves.GetSlotSummary(
                    slotIndex);

            if (!summaryResult.IsSuccess)
            {
                ShowError(
                    summaryResult.ErrorMessage);

                return;
            }

            SaveSlotSummary summary =
                summaryResult.Value;

            if (!summary.Exists)
            {
                pendingCampaign =
                    new NewCampaignDraft(
                        slotIndex);

                PendingCampaignSlotIndex =
                    slotIndex;

                ShowScreen(
                    FrontEndScreen.TeamCreation);

                return;
            }

            if (!CanLoadCareerScene())
            {
                return;
            }

            Result<GameSessionState> loadResult =
                saves.LoadCampaign(
                    slotIndex);

            if (!loadResult.IsSuccess)
            {
                ShowError(
                    loadResult.ErrorMessage);

                return;
            }

            SceneManager.LoadScene(
                careerSceneName);
        }

        public void Disconnect()
        {
            if (saves != null &&
                saves.HasActiveCampaign)
            {
                Result saveResult =
                    saves.SaveCurrentCampaign();

                if (!saveResult.IsSuccess)
                {
                    ShowError(
                        saveResult.ErrorMessage);

                    return;
                }
            }

            Application.Quit();
        }

        public void ShowScreen(
            FrontEndScreen screen)
        {
            CurrentScreen =
                screen;

            SetActive(
                bootRoot,
                screen == FrontEndScreen.Boot);

            SetActive(
                pressAnyInputRoot,
                screen == FrontEndScreen.PressAnyInput);

            SetActive(
                mainMenuRoot,
                screen == FrontEndScreen.MainMenu);

            SetActive(
                campaignSelectRoot,
                screen == FrontEndScreen.CampaignSelect);

            SetActive(
                teamCreationRoot,
                screen == FrontEndScreen.TeamCreation);

            SetActive(
                racerCreationRoot,
                screen == FrontEndScreen.RacerCreation);

            SetActive(
                campaignReviewRoot,
                screen == FrontEndScreen.CampaignReview);

            SetActive(
                optionsRoot,
                screen == FrontEndScreen.Options);

            SetActive(
                creditsRoot,
                screen == FrontEndScreen.Credits);

            ClearError();

            switch (screen)
            {
                case FrontEndScreen.CampaignSelect:
                    campaignSelectController?.Refresh();
                    break;

                case FrontEndScreen.TeamCreation:
                    teamCreationController?.Present(
                        pendingCampaign);
                    break;

                case FrontEndScreen.RacerCreation:
                    racerCreationController?.Present(
                        pendingCampaign);
                    break;

                case FrontEndScreen.CampaignReview:
                    campaignReviewController?.Present(
                        pendingCampaign);
                    break;
            }
        }

        public void ShowError(
            string message)
        {
            if (errorText != null)
            {
                errorText.text =
                    message ?? string.Empty;
            }

            Debug.LogError(
                $"[FrontEnd] {message}",
                this);
        }

        private bool TryGetPendingCampaign(
            out NewCampaignDraft draft)
        {
            draft =
                pendingCampaign;

            if (draft != null)
            {
                return true;
            }

            ShowError(
                "There is no pending new campaign.");

            ShowScreen(
                FrontEndScreen.CampaignSelect);

            return false;
        }

        private bool CanLoadCareerScene()
        {
            if (!string.IsNullOrWhiteSpace(
                    careerSceneName) &&
                Application.CanStreamedLevelBeLoaded(
                    careerSceneName))
            {
                return true;
            }

            ShowError(
                $"Career scene '{careerSceneName}' cannot be loaded.");

            return false;
        }

        private void ShowCampaignReviewError(
            string message)
        {
            campaignReviewController?.SetStatus(
                message);

            ShowError(
                message);
        }

        private void ClearPendingCampaign()
        {
            pendingCampaign =
                null;

            PendingCampaignSlotIndex =
                null;
        }

        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text =
                    string.Empty;
            }
        }

        private void SetActive(
            GameObject root,
            bool active)
        {
            if (root != null)
            {
                root.SetActive(
                    active);
            }
        }

        private void BindButtons()
        {
            Bind(
                raceFatalButton,
                OpenCampaignSelect);

            Bind(
                optionsButton,
                OpenOptions);

            Bind(
                creditsButton,
                OpenCredits);

            Bind(
                disconnectButton,
                Disconnect);

            Bind(
                campaignBackButton,
                BackToMainMenu);

            if (teamCreationController == null)
            {
                Bind(
                    teamCreationBackButton,
                    CancelNewCampaign);
            }

            Bind(
                optionsBackButton,
                BackToMainMenu);

            Bind(
                creditsBackButton,
                BackToMainMenu);
        }

        private void Bind(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(
                action);

            button.onClick.AddListener(
                action);
        }
    }
}
