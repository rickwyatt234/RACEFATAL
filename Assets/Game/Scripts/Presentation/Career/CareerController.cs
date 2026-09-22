using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerController :
        MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject homeRoot;
        [SerializeField] private GameObject racesRoot;
        [SerializeField] private GameObject garageRoot;
        [SerializeField] private GameObject researchRoot;
        [SerializeField] private GameObject shopRoot;
        [SerializeField] private GameObject rosterRoot;
        [SerializeField] private GameObject teamRoot;

        [Header("Navigation")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button racesButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button researchButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button rosterButton;
        [SerializeField] private Button teamButton;
        [SerializeField] private Button saveAndReturnButton;

        [Header("Views")]
        [SerializeField] private CareerHomeView homeView;

        [Header("Feedback")]
        [SerializeField] private TMP_Text errorText;

        [Header("Scenes")]
        [SerializeField]
        private string mainMenuSceneName =
            "03_MainMenu";

        private GameContext context;

        public CareerScreen CurrentScreen {
            get;
            private set;
        }

        private void Awake()
        {
            BindButtons();
        }

        private void Start()
        {
            context =
                BootstrapController.Context;

            if (context == null)
            {
                ShowError(
                    "GameContext is unavailable. Start the game from 00_Bootstrap.");

                return;
            }

            if (!context.Sessions.HasSession ||
                context.Sessions.Current == null)
            {
                ShowError(
                    "No campaign session is loaded.");

                return;
            }

            ShowScreen(
                CareerScreen.Home);
        }

        public void ShowHome()
        {
            ShowScreen(
                CareerScreen.Home);
        }

        public void ShowRaces()
        {
            ShowScreen(
                CareerScreen.Races);
        }

        public void ShowGarage()
        {
            ShowScreen(
                CareerScreen.Garage);
        }

        public void ShowResearch()
        {
            ShowScreen(
                CareerScreen.Research);
        }

        public void ShowShop()
        {
            ShowScreen(
                CareerScreen.Shop);
        }

        public void ShowRoster()
        {
            ShowScreen(
                CareerScreen.Roster);
        }

        public void ShowTeam()
        {
            ShowScreen(
                CareerScreen.Team);
        }

        public void ShowScreen(
            CareerScreen screen)
        {
            CurrentScreen =
                screen;

            SetActive(
                homeRoot,
                screen == CareerScreen.Home);

            SetActive(
                racesRoot,
                screen == CareerScreen.Races);

            SetActive(
                garageRoot,
                screen == CareerScreen.Garage);

            SetActive(
                researchRoot,
                screen == CareerScreen.Research);

            SetActive(
                shopRoot,
                screen == CareerScreen.Shop);

            SetActive(
                rosterRoot,
                screen == CareerScreen.Roster);

            SetActive(
                teamRoot,
                screen == CareerScreen.Team);

            ClearError();

            if (screen ==
                CareerScreen.Home)
            {
                RefreshHome();
            }
        }

        public void SaveAndReturnToMainMenu()
        {
            if (context == null ||
                context.Saves == null)
            {
                ShowError(
                    "Campaign save service is unavailable.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    mainMenuSceneName) ||
                !Application.CanStreamedLevelBeLoaded(
                    mainMenuSceneName))
            {
                ShowError(
                    $"Main-menu scene '{mainMenuSceneName}' cannot be loaded.");

                return;
            }

            Result saveResult =
                context.Saves.SaveCurrentCampaign();

            if (!saveResult.IsSuccess)
            {
                ShowError(
                    saveResult.ErrorMessage);

                return;
            }

            Result closeResult =
                context.Saves.CloseCurrentCampaign(
                    false);

            if (!closeResult.IsSuccess)
            {
                ShowError(
                    closeResult.ErrorMessage);

                return;
            }

            SceneManager.LoadScene(
                mainMenuSceneName);
        }

        public void RefreshHome()
        {
            if (context == null ||
                context.Sessions.Current == null)
            {
                return;
            }

            homeView?.Bind(
                context.Sessions.Current,
                context.Database);
        }

        private void ShowError(
            string message)
        {
            if (errorText != null)
            {
                errorText.text =
                    message ?? string.Empty;
            }

            Debug.LogError(
                $"[Career] {message}",
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
                homeButton,
                ShowHome);

            Bind(
                racesButton,
                ShowRaces);

            Bind(
                garageButton,
                ShowGarage);

            Bind(
                researchButton,
                ShowResearch);

            Bind(
                shopButton,
                ShowShop);

            Bind(
                rosterButton,
                ShowRoster);

            Bind(
                teamButton,
                ShowTeam);

            Bind(
                saveAndReturnButton,
                SaveAndReturnToMainMenu);
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
