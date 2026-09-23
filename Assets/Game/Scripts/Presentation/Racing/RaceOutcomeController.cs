using RaceFatal.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Presentation.Vehicles;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace RaceFatal.Presentation.Racing
{
    public class RaceOutcomeController : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private RaceRuntimeController raceRuntime;

        [Header("Outcome UI")]
        [SerializeField] private GameObject outcomePanel;
        [SerializeField] private TextMeshProUGUI outcomeTitle;
        [SerializeField] private TextMeshProUGUI outcomeSubtitle;
        [SerializeField] private RaceStandingsView standingsView;
        [SerializeField] private Button continueButton;
        [SerializeField] private TextMeshProUGUI continueButtonText;
        [SerializeField] private CanvasGroup outcomeCanvasGroup;

        [Header("Payout")]
        [SerializeField] private GameObject payoutPanel;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private TextMeshProUGUI teamFameText;
        [SerializeField] private TextMeshProUGUI researchPointsText;
        [SerializeField] private TextMeshProUGUI characterFameText;
        [SerializeField] private TextMeshProUGUI careerStatusText;

        [Header("Post-Race Scene")]
        [Tooltip("Scene loaded after leaving the final results screen.")]
        [SerializeField] private string postRaceSceneName;

        [Header("Vehicle")]
        [Range(0f, 1f)][SerializeField] private float finishBrakeInput = 0.35f;

        [Header("Cursor")]
        [SerializeField] private bool releaseCursorOnOutcome = true;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugBound;
        [SerializeField] private bool debugPlayerResolved;
        [SerializeField] private bool debugPlayerPresentationResolved;
        [SerializeField] private bool debugWaitingForContinue;
        [SerializeField] private bool debugFinalResults;
        [SerializeField] private bool debugRaceCompleted;
        [SerializeField] private bool debugPostRaceResolved;
        [SerializeField] private bool debugLeavingRace;
        [SerializeField] private string debugOutcome = "None";
        [SerializeField] private int debugFinishPosition;

        private RaceDirector director;
        private PostRaceResult cachedPostRaceResult;

        private PlayerCockpitHUD playerHud;
        private PlayerCockpitView playerCockpitView;

        private bool playerResolved;
        private bool waitingForContinue;
        private bool finalResults;
        private bool outcomeActive;
        private bool leavingRace;
        private bool bound;

        private void Awake()
        {
            if (outcomePanel != null)
                outcomePanel.SetActive(false);

            if (payoutPanel != null)
                payoutPanel.SetActive(false);

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(
                    ConfirmOutcome);
            }
        }

        private void Update()
        {
            if (!bound)
                TryBind();

            if (outcomeActive &&
                releaseCursorOnOutcome)
            {
                ReleaseCursor();
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    ConfirmOutcome);
            }

            Unbind();
        }

        #region Binding

        private void TryBind()
        {
            if (raceRuntime == null)
            {
                raceRuntime =
                    FindFirstObjectByType<
                        RaceRuntimeController>();
            }

            if (raceRuntime == null ||
                !raceRuntime.IsInitialized ||
                raceRuntime.Director == null)
            {
                return;
            }

            director =
                raceRuntime.Director;

            director.RacerFinished +=
                OnRacerFinished;

            director.RacerDestroyed +=
                OnRacerDestroyed;

            director.PostRaceResolved +=
                OnPostRaceResolved;

            director.RaceCompleted +=
                OnRaceCompleted;

            if (standingsView != null)
            {
                standingsView.Initialize(
                    raceRuntime);
            }

            ResolvePlayerPresentation();

            bound = true;
            debugBound = true;
        }

        private void Unbind()
        {
            if (!bound ||
                director == null)
            {
                return;
            }

            director.RacerFinished -=
                OnRacerFinished;

            director.RacerDestroyed -=
                OnRacerDestroyed;

            director.PostRaceResolved -=
                OnPostRaceResolved;

            director.RaceCompleted -=
                OnRaceCompleted;

            bound = false;
            debugBound = false;
        }

        private void ResolvePlayerPresentation()
        {
            if (raceRuntime == null ||
                director?.State == null)
            {
                return;
            }

            RaceParticipant player =
                FindPlayerParticipant();

            if (player == null)
                return;

            if (!raceRuntime.TryGetRacerView(
                    player.RacerId,
                    out RacerViewController playerView))
            {
                return;
            }

            if (playerView == null)
                return;

            playerHud =
                playerView.GetComponentInChildren<
                    PlayerCockpitHUD>(true);

            playerCockpitView =
                playerView.GetComponentInChildren<
                    PlayerCockpitView>(true);

            debugPlayerPresentationResolved =
                playerHud != null ||
                playerCockpitView != null;
        }

        private RaceParticipant FindPlayerParticipant()
        {
            if (director?.State == null)
                return null;

            foreach (RaceParticipant participant
                     in director.State.Participants)
            {
                if (participant != null &&
                    participant.Role ==
                        RaceParticipantRole.Player)
                {
                    return participant;
                }
            }

            return null;
        }

        #endregion

        #region Race Events

        private void OnRacerFinished(
            RaceParticipant participant)
        {
            if (!IsPlayer(participant) ||
                playerResolved)
            {
                return;
            }

            playerResolved = true;
            waitingForContinue = true;
            outcomeActive = true;

            debugPlayerResolved = true;
            debugWaitingForContinue = true;
            debugOutcome = "Finished";

            debugFinishPosition =
                participant.FinishPosition;

            raceRuntime.StopRacerControl(
                participant.RacerId,
                finishBrakeInput);

            ShowLiveOutcome(
                "FINISHED",
                $"POSITION {FormatOrdinal(participant.FinishPosition)}");
        }

        private void OnRacerDestroyed(
            RaceParticipant participant)
        {
            if (!IsPlayer(participant) ||
                playerResolved)
            {
                return;
            }

            playerResolved = true;
            waitingForContinue = true;
            outcomeActive = true;

            debugPlayerResolved = true;
            debugWaitingForContinue = true;
            debugOutcome = "Destroyed";

            raceRuntime.StopRacerControl(
                participant.RacerId,
                1f);

            ShowLiveOutcome(
                "RACER DESTROYED",
                "CAREER TERMINATED");
        }

        private void OnPostRaceResolved(
            PostRaceResult result)
        {
            cachedPostRaceResult =
                result;

            debugPostRaceResolved =
                result != null;

            if (finalResults)
            {
                RenderPayout(
                    result);
            }
        }

        private void OnRaceCompleted(
            RaceResult result)
        {
            debugRaceCompleted =
                true;

            ShowFinalResults(
                result);

            if (outcomeCanvasGroup != null)
                outcomeCanvasGroup.alpha = 1f;
        }

        private bool IsPlayer(
            RaceParticipant participant)
        {
            return participant != null &&
                   participant.Role ==
                       RaceParticipantRole.Player;
        }

        #endregion

        #region Outcome

        private void ShowLiveOutcome(
            string title,
            string subtitle)
        {
            ResolvePlayerPresentation();

            if (outcomePanel != null)
                outcomePanel.SetActive(true);

            if (outcomeCanvasGroup != null)
                {
                    outcomeCanvasGroup.alpha = 1f;
                    outcomeCanvasGroup.interactable = true;
                    outcomeCanvasGroup.blocksRaycasts = true;
                }

            if (outcomeTitle != null)
                outcomeTitle.text = title;

            if (outcomeSubtitle != null)
                outcomeSubtitle.text = subtitle;

            if (payoutPanel != null)
                payoutPanel.SetActive(false);

            playerHud?.SetHudVisible(
                false);

            playerCockpitView?.SetReticleVisible(
                false);

            if (standingsView != null)
            {
                standingsView.ShowLive();
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(
                    true);

                continueButton.interactable =
                    true;
            }

            if (continueButtonText != null)
            {
                continueButtonText.text =
                    "CONTINUE";
            }

            if (releaseCursorOnOutcome)
                ReleaseCursor();
        }

        public void ConfirmOutcome()
        {
            if (leavingRace)
                return;

            /*
             * SECOND CONTINUE:
             * final classification has already been resolved,
             * so leave the race scene.
             */
            if (finalResults)
            {
                ReturnFromRace();
                return;
            }

            /*
             * FIRST CONTINUE:
             * resolve everyone still physically racing.
             */
            if (!waitingForContinue ||
                director == null)
            {
                return;
            }

            waitingForContinue = false;
            debugWaitingForContinue = false;

            if (continueButton != null)
            {
                continueButton.interactable =
                    false;
            }

            if (continueButtonText != null)
            {
                continueButtonText.text =
                    "RESOLVING...";
            }

            RaceResult result =
                director.ResolveRemainingRace();

            raceRuntime.StopAllVehicleControl(
                1f);

            if (!finalResults &&
                result != null)
            {
                ShowFinalResults(
                    result);
            }
        }

        private void ShowFinalResults(
            RaceResult result)
        {
            finalResults = true;
            outcomeActive = true;

            debugFinalResults = true;

            if (outcomePanel != null)
                outcomePanel.SetActive(false);
            if (outcomeCanvasGroup != null)
                {
                    outcomeCanvasGroup.alpha = 1f;
                    outcomeCanvasGroup.interactable = true;
                    outcomeCanvasGroup.blocksRaycasts = true;
                }

            if (outcomeTitle != null)
                outcomeTitle.text = "RACE COMPLETE";

            if (outcomeSubtitle != null)
            {
                outcomeSubtitle.text =
                    "FINAL CLASSIFICATION";
            }

            if (standingsView != null)
            {
                standingsView.ShowFinal(
                    result);
            }

            PostRaceResult payout =
                cachedPostRaceResult ??
                director?.PostRaceResult;

            RenderPayout(
                payout);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(
                    true);

                continueButton.interactable =
                    true;
            }

            if (continueButtonText != null)
            {
                continueButtonText.text =
                    "RETURN TO CAREER";
            }

            if (releaseCursorOnOutcome)
                ReleaseCursor();
        }

        #endregion

        #region Payout

        private void RenderPayout(
            PostRaceResult result)
        {
            if (result == null)
            {
                if (payoutPanel != null)
                    payoutPanel.SetActive(false);

                return;
            }

            if (payoutPanel != null)
                payoutPanel.SetActive(true);

            RaceReward reward =
                result.Reward;

            if (creditsText != null)
            {
                creditsText.text =
                    $"+{reward.Credits:N0}";
            }

            if (teamFameText != null)
            {
                teamFameText.text =
                    $"+{reward.TeamFame:N0}";
            }

            if (researchPointsText != null)
            {
                researchPointsText.text =
                    $"+{reward.ResearchPoints:N0}";
            }

            if (characterFameText != null)
            {
                characterFameText.text =
                    $"+{reward.CharacterFame:N0}";
            }

            if (careerStatusText != null)
            {
                careerStatusText.text =
                    result.PlayerDied
                        ? "RACER DECEASED"
                        : result.CareerEnded
                            ? "CAREER ENDED"
                            : "CAREER ACTIVE";
            }
        }

        #endregion

        #region Scene Transition

        private void ReturnFromRace()
        {
            if (leavingRace)
                return;

            if (string.IsNullOrWhiteSpace(
                    postRaceSceneName))
            {
                Debug.LogError(
                    "RaceOutcomeController has no Post Race Scene Name assigned.",
                    this);

                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(
                    postRaceSceneName))
            {
                Debug.LogError(
                    $"Post-race scene '{postRaceSceneName}' " +
                    "cannot be loaded. Make sure it is included " +
                    "in the build profile.",
                    this);

                return;
            }

            GameContext context =
                BootstrapController.Context;

            if (context == null)
            {
                Debug.LogError(
                    "GameContext is not available. " +
                    "Cannot return from race.",
                    this);

                return;
            }

            // A persistent career is saved only after the director
            // has resolved the final results and applied the reward.
            // Prototype races deliberately remain transient.
            if (context.Saves != null &&
                context.Saves.HasActiveCampaign)
            {
                if (!finalResults ||
                    director == null ||
                    director.PostRaceResult == null)
                {
                    ReportReturnError(
                        "Final career results have not been resolved.");
                    return;
                }

                Result saveResult =
                    context.Saves.SaveCurrentCampaign();

                if (!saveResult.IsSuccess)
                {
                    ReportReturnError(
                        "Could not save career results: " +
                        saveResult.ErrorMessage);
                    return;
                }
            }

            context.RaceLaunch.Clear();

            leavingRace = true;
            debugLeavingRace = true;

            if (continueButton != null)
            {
                continueButton.interactable =
                    false;
            }

            if (continueButtonText != null)
            {
                continueButtonText.text =
                    "LOADING...";
            }

            SceneManager.LoadScene(
                postRaceSceneName);
        }

        private void ReportReturnError(string message)
        {
            Debug.LogError(
                "[RaceOutcome] " + message,
                this);

            if (outcomeSubtitle != null)
                outcomeSubtitle.text = message;

            if (continueButtonText != null)
                continueButtonText.text = "RETRY RETURN";

            if (continueButton != null)
                continueButton.interactable = true;
        }

        #endregion

        #region Cursor

        private void ReleaseCursor()
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }

        #endregion

        #region Formatting

        private string FormatOrdinal(
            int position)
        {
            if (position <= 0)
                return "--";

            int lastTwo =
                position % 100;

            if (lastTwo >= 11 &&
                lastTwo <= 13)
            {
                return $"{position}TH";
            }

            switch (position % 10)
            {
                case 1:
                    return $"{position}ST";

                case 2:
                    return $"{position}ND";

                case 3:
                    return $"{position}RD";

                default:
                    return $"{position}TH";
            }
        }

        #endregion
    }
}