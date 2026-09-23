using System;
using System.Collections.Generic;
using RaceFatal.Data;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Tracks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerRacesView :
        MonoBehaviour
    {
        [Header("Race List")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private CareerRaceCardView cardTemplate;

        [Header("Selected Race")]
        [SerializeField] private TMP_Text selectedRaceNameText;
        [SerializeField] private TMP_Text selectedTrackText;
        [SerializeField] private TMP_Text selectedRequirementsText;
        [SerializeField] private TMP_Text selectedStatusText;
        [SerializeField] private Button enterRaceButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text feedbackText;

        private readonly List<CareerRaceCardView> cards =
            new List<CareerRaceCardView>();

        private CareerController owner;
        private GameContext context;
        private string selectedRaceId;
        private bool busy;

        public void Initialize(
            CareerController careerController,
            GameContext gameContext)
        {
            owner = careerController;
            context = gameContext;

            if (enterRaceButton != null)
            {
                enterRaceButton.onClick.RemoveListener(EnterSelectedRace);
                enterRaceButton.onClick.AddListener(EnterSelectedRace);
            }
        }

        public void Refresh()
        {
            ClearCards();
            selectedRaceId = null;
            busy = false;
            ShowNoSelection();
            SetFeedback(string.Empty);

            if (context == null ||
                context.Database == null ||
                context.RacePreparation == null)
            {
                SetFeedback("Race services are unavailable.");
                return;
            }

            if (cardContainer == null ||
                cardTemplate == null)
            {
                SetFeedback("Race list UI is not configured.");
                return;
            }

            var races = new List<RaceDefinition>(
                context.Database.RaceDefinitions.Values);

            races.Sort((a, b) =>
                string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase));

            if (races.Count == 0)
            {
                SetFeedback("NO RACES HAVE BEEN AUTHORED.");
                return;
            }

            foreach (RaceDefinition race in races)
            {
                if (race == null)
                    continue;

                TrackDefinition track =
                    context.Database.GetTrackDefinition(race.TrackId);

                string trackName =
                    track != null
                        ? track.DisplayName
                        : "UNKNOWN TRACK";

                Result<RaceDirector> preview =
                    PreviewRace(race.Id);

                CareerRaceCardView card =
                    Instantiate(
                        cardTemplate,
                        cardContainer);

                card.gameObject.SetActive(true);
                card.Bind(
                    race,
                    trackName,
                    preview.IsSuccess,
                    SelectRace);

                cards.Add(card);
            }

            if (context.Saves == null ||
                !context.Saves.HasActiveCampaign)
            {
                SetFeedback(
                    "LOAD A SAVED CAMPAIGN TO ENTER CAREER RACES.");
            }
            else if (context.Sessions.Current?.CareerRun == null ||
                     !context.Sessions.Current.CareerRun.IsActive)
            {
                SetFeedback(
                    "NO ACTIVE RACER. START A NEW CAREER BEFORE RACING.");
            }
        }

        public void SetBusy(bool isBusy)
        {
            busy = isBusy;

            if (enterRaceButton != null)
            {
                enterRaceButton.interactable =
                    !busy && CanEnterSelectedRace();
            }
        }

        public void SetFeedback(string message)
        {
            SetText(feedbackText, message);
        }

        private Result<RaceDirector> PreviewRace(
            string raceId)
        {
            if (context?.Saves == null ||
                !context.Saves.HasActiveCampaign)
            {
                return Result<RaceDirector>.Failure(
                    "Only a saved career can enter this race.");
            }

            RaceDefinition definition =
                context.Database?.GetRaceDefinition(raceId);

            if (definition == null)
            {
                return Result<RaceDirector>.Failure(
                    "Selected race definition does not exist.");
            }

            var trackContent =
                BootstrapController.ContentCatalog?.FindTrackContent(
                    definition.TrackId);

            if (trackContent == null ||
                trackContent.TrackPrefab == null)
            {
                return Result<RaceDirector>.Failure(
                    "This race has no configured track prefab.");
            }

            try
            {
                return context.RacePreparation.PrepareSelectedRace(
                    raceId);
            }
            catch (Exception exception)
            {
                return Result<RaceDirector>.Failure(
                    "Race preparation failed: " + exception.Message);
            }
        }

        private void SelectRace(string raceId)
        {
            if (busy || context?.Database == null)
                return;

            RaceDefinition race =
                context.Database.GetRaceDefinition(raceId);

            if (race == null)
            {
                SetFeedback("The selected race is unavailable.");
                return;
            }

            selectedRaceId = raceId;

            foreach (CareerRaceCardView card in cards)
                card.SetSelected(card.RaceId == raceId);

            TrackDefinition track =
                context.Database.GetTrackDefinition(race.TrackId);

            SetText(selectedRaceNameText, race.DisplayName);
            SetText(
                selectedTrackText,
                "TRACK  " + (track != null
                    ? track.DisplayName
                    : "NOT FOUND"));
            SetText(
                selectedRequirementsText,
                $"ENGINE CLASS  {race.EngineClass}\n" +
                $"LAPS  {race.LapCount}\n" +
                $"ENTRANTS  {race.EntrantCount}\n" +
                $"TEAM SIZE  {race.TeamSize}");

            Result<RaceDirector> preview =
                PreviewRace(race.Id);

            SetText(
                selectedStatusText,
                preview.IsSuccess
                    ? "READY TO ENTER"
                    : "INELIGIBLE: " + preview.ErrorMessage);

            if (enterRaceButton != null)
                enterRaceButton.interactable =
                    preview.IsSuccess && !busy;

            SetFeedback(string.Empty);
        }

        private void EnterSelectedRace()
        {
            if (busy || !CanEnterSelectedRace())
                return;

            owner?.LaunchRace(selectedRaceId);
        }

        private bool CanEnterSelectedRace()
        {
            if (string.IsNullOrWhiteSpace(selectedRaceId))
                return false;

            return PreviewRace(selectedRaceId).IsSuccess;
        }

        private void ShowNoSelection()
        {
            SetText(selectedRaceNameText, "SELECT A RACE");
            SetText(selectedTrackText, string.Empty);
            SetText(selectedRequirementsText, string.Empty);
            SetText(selectedStatusText, string.Empty);

            if (enterRaceButton != null)
                enterRaceButton.interactable = false;
        }

        private void ClearCards()
        {
            foreach (CareerRaceCardView card in cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            cards.Clear();
        }

        private void OnDestroy()
        {
            if (enterRaceButton != null)
                enterRaceButton.onClick.RemoveListener(EnterSelectedRace);
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
