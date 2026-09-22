using RaceFatal.Career;
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
        private float bootElapsed;

        public FrontEndScreen CurrentScreen {
            get;
            private set;
        }

        public int? PendingCampaignSlotIndex {
            get;
            private set;
        }

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

            if (campaignSelectController != null)
            {
                campaignSelectController.Initialize(
                    this,
                    saves);
            }

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
            PendingCampaignSlotIndex =
                null;

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
            PendingCampaignSlotIndex =
                null;

            ShowScreen(
                FrontEndScreen.MainMenu);
        }

        public void CancelNewCampaign()
        {
            PendingCampaignSlotIndex =
                null;

            ShowScreen(
                FrontEndScreen.CampaignSelect);
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
                PendingCampaignSlotIndex =
                    slotIndex;

                ShowScreen(
                    FrontEndScreen.TeamCreation);

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    careerSceneName) ||
                !Application.CanStreamedLevelBeLoaded(
                    careerSceneName))
            {
                ShowError(
                    $"Career scene '{careerSceneName}' cannot be loaded.");

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

            if (screen ==
                FrontEndScreen.CampaignSelect)
            {
                campaignSelectController?.Refresh();
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

            Bind(
                teamCreationBackButton,
                CancelNewCampaign);

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
