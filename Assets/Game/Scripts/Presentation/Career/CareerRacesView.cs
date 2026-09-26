using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Racing;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public sealed class CareerRacesView : MonoBehaviour
    {
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private CareerRaceCardView cardTemplate;
        [SerializeField] private TMP_Text weekText;
        [SerializeField] private TMP_Text selectedRaceNameText;
        [SerializeField] private TMP_Text selectedTrackText;
        [SerializeField] private TMP_Text selectedRequirementsText;
        [SerializeField] private TMP_Text selectedStatusText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Image trackDiagram;
        [SerializeField] private TMP_Text diagramPlaceholder;
        [SerializeField] private Button enterRaceButton;
        [SerializeField] private Button calendarButton;
        [SerializeField] private Button unlockedButton;
        [SerializeField] private Button fameButton;
        [SerializeField] private Button standingsButton;
        [SerializeField] private Button advanceButton;
        [SerializeField] private TMP_Text advanceLabel;
        [SerializeField] private GameObject confirmationRoot;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private readonly List<CareerRaceCardView> cards = new List<CareerRaceCardView>();
        private readonly List<Selectable> blocked = new List<Selectable>();
        private CareerController owner;
        private GameContext context;
        private CareerCalendarService calendar;
        private string selectedEventId, pendingEventId;
        private bool busy, calendarSaved, pendingAdvance;
        private int mode;
        private GameSessionState Session => context?.Sessions?.Current;
        private TeamState Team => Session?.PlayerTeam;

        public void Initialize(CareerController controller, GameContext gameContext)
        {
            owner = controller; context = gameContext;
            calendar = new CareerCalendarService(context.Database);
            Bind(enterRaceButton, ConfirmEntry); Bind(calendarButton, ShowCalendar); Bind(unlockedButton, ShowUnlocked);
            Bind(fameButton, ShowFame); Bind(standingsButton, ShowStandings); Bind(advanceButton, ConfirmAdvance);
            Bind(confirmButton, Confirm); Bind(cancelButton, Cancel);
            Cancel();
        }
        private void ShowCalendar() { mode = 0; Refresh(); }
        private void ShowUnlocked() { mode = 1; Refresh(); }
        private void ShowFame() { mode = 2; Refresh(); }
        private void ShowStandings() { mode = 3; Refresh(); }
        public void Refresh()
        {
            Cancel(); ClearCards(); busy = false; calendarSaved = false;
            if (enterRaceButton != null) enterRaceButton.interactable = false;
            if (advanceButton != null) advanceButton.interactable = false;
            Set(selectedRaceNameText, "SELECT AN EVENT"); Set(selectedTrackText, ""); Set(selectedRequirementsText, ""); Set(selectedStatusText, "");
            ShowDiagram(null);
            if (Team == null || calendar == null || context.Saves?.HasActiveCampaign != true)
            { Set(weekText, "NO CAMPAIGN"); SetFeedback("LOAD A CAMPAIGN FROM 00_BOOTSTRAP."); return; }
            var beforeRefresh = Team.Calendar.Export();
            bool changed = calendar.Refresh(Team);
            var saved = changed ? context.Saves.SaveCurrentCampaign() : Result.Success();
            calendarSaved = saved.IsSuccess;
            if (!saved.IsSuccess)
            {
                Team.RestoreCalendar(beforeRefresh);
                SetFeedback("CALENDAR SAVE FAILED: " + saved.ErrorMessage + " REOPEN RACES TO RETRY.");
                return;
            }
            SetFeedback(saved.IsSuccess ? "ONE RACE ADVANCES ONE WEEK. FAME UNLOCKS ARE PERMANENT." : "CALENDAR SAVE FAILED: " + saved.ErrorMessage);
            Set(weekText, $"WEEK {Team.Calendar.Week}  //  {Team.Credits:N0} CREDITS  //  TEAM FAME {Team.Fame:N0}");
            var active = Team.Calendar.Active;
            Set(advanceLabel, active == null ? "SKIP WEEK" : "WITHDRAW EVENT");
            if (advanceButton != null) advanceButton.interactable = calendarSaved && Session.CareerRun?.IsActive == true;
            if (mode == 3) { DisplayStandings(); return; }
            IEnumerable<CareerEventDefinition> definitions = context.Database.CareerEventDefinitions.Values.Where(d => calendar.Validate(d).IsSuccess);
            if (mode == 0) definitions = definitions.Where(d => Team.Calendar.DrawIds.Contains(d.Id) && active == null);
            else if (mode == 1) definitions = definitions.Where(d => Team.Calendar.UnlockedIds.Contains(d.Id));
            foreach (var definition in definitions.OrderBy(d => d.RequiredFame).ThenBy(d => d.DisplayName))
            {
                bool unlocked = Team.Calendar.UnlockedIds.Contains(definition.Id);
                string status = !unlocked ? $"UNLOCKS AT {definition.RequiredFame:N0} FAME" :
                    active?.eventId == definition.Id ? "ENTERED" : Team.Calendar.DrawIds.Contains(definition.Id) && active == null ? "THIS WEEK" : "UNLOCKED · NOT THIS WEEK";
                AddCard(definition.Id, definition.DisplayName, definition.Kind.ToString().ToUpperInvariant(),
                    $"FEE {definition.EntryFee:N0}  //  {definition.RaceIds.Count} ROUND(S)", status);
            }
            if (mode == 0 && active != null)
                AddCard(active.eventId, active.displayName, active.kind.ToString().ToUpperInvariant(),
                    $"ROUND {active.roundIndex + 1}/{active.raceIds.Count}  //  ENTRY PAID", "CONTINUE EVENT");
            if (cards.Count == 0) SetFeedback("NO EVENTS IN THIS VIEW. CHECK FAME MILESTONES OR SKIP TO NEXT WEEK.");
            if (!cards.Any(c => c.RaceId == selectedEventId)) selectedEventId = cards.FirstOrDefault()?.RaceId;
            if (selectedEventId != null) SelectEvent(selectedEventId);
        }
        private void AddCard(string id, string name, string type, string requirements, string status)
        {
            if (cardTemplate == null || cardContainer == null) return;
            var card = Instantiate(cardTemplate, cardContainer);
            card.gameObject.SetActive(true);
            card.BindEvent(id, name, type, requirements, status, SelectEvent);
            cards.Add(card);
        }
        private void SelectEvent(string id)
        {
            if (busy) return;
            selectedEventId = id;
            foreach (var card in cards) card.SetSelected(card.RaceId == id);
            var active = Team.Calendar.Active;
            var entry = active?.eventId == id ? active : null;
            var definition = context.Database.GetCareerEventDefinition(id);
            if (entry == null && definition == null) return;
            var rounds = entry?.raceIds ?? definition.RaceIds.ToList();
            var payouts = entry?.payouts ?? definition.RacePayouts.ToList();
            var prizes = entry?.prizes ?? definition.ChampionshipPrizes.ToList();
            var points = entry?.points ?? definition.PositionPoints.ToList();
            var kind = entry?.kind ?? definition.Kind;
            var raceId = rounds[entry?.roundIndex ?? 0];
            var race = context.Database.GetRaceDefinition(raceId);
            Set(selectedRaceNameText, entry?.displayName ?? definition.DisplayName);
            Set(selectedTrackText, race == null ? "MISSING RACE CONTENT" : context.Database.GetTrackDefinition(race.TrackId)?.DisplayName ?? race.TrackId);
            var text = new StringBuilder();
            text.AppendLine(entry?.description ?? definition.Description);
            text.AppendLine($"\nFORMAT  {kind.ToString().ToUpperInvariant()}\nENTRY FEE  {entry?.entryFee ?? definition.EntryFee:N0} CREDITS{(entry != null ? " (PAID)" : "")}");
            if (race != null) text.AppendLine($"ENGINE CLASS  {race.EngineClass}\nLAPS  {race.LapCount}\nRACERS  UP TO {race.EntrantCount} / {race.TeamSize} PER TEAM\nEVENT RESEARCH BONUS  +{race.ResearchPointBonus} RP");
            text.AppendLine($"\nRACE PURSE  {payouts.Take(race?.EntrantCount ?? payouts.Count).Sum(v => (long)v):N0} CREDITS\nPLAYER PAYOUT BY FINISH");
            for (int i = 0; i < payouts.Count; i++) text.AppendLine($"{i + 1}.  {payouts[i]:N0} CREDITS");
            text.AppendLine("DNF: 40% OF POSITION PAYOUT.\nTEAM FAME, CHARACTER FAME AND BASE RP ALSO FOLLOW THE EXISTING FINISH REWARDS.");
            if (kind == CareerEventKind.Championship)
            {
                text.AppendLine("\nCHAMPIONSHIP ROUNDS");
                for (int i = 0; i < rounds.Count; i++) text.AppendLine($"{i + 1}. {context.Database.GetRaceDefinition(rounds[i])?.DisplayName ?? rounds[i]}");
                text.AppendLine("\nFINAL TEAM PRIZES");
                for (int i = 0; i < prizes.Count; i++) text.AppendLine($"RANK {i + 1}: {prizes[i]:N0} CREDITS");
                text.AppendLine("\nFINISH POINTS: " + string.Join(", ", points));
                text.AppendLine("BOTH RACERS SCORE FOR THE TEAM. DNF SCORES ZERO. TIED POINTS SHARE FINAL RANK AND PRIZE.");
            }
            if (mode == 2 && definition != null) text.AppendLine($"\nFAME MILESTONE  {definition.RequiredFame:N0}\n{Math.Max(0, definition.RequiredFame - Team.Fame):N0} MORE FAME NEEDED. FAME IS NOT SPENT.");
            Set(selectedRequirementsText, text.ToString());
            ShowDiagram(race?.TrackId);
            var eligible = Preview(id);
            Set(selectedStatusText, eligible.IsSuccess ? entry != null ? "ENTRY PAID · CONTINUE ROUND" : "READY TO ENTER" : eligible.ErrorMessage);
            if (enterRaceButton != null) enterRaceButton.interactable = eligible.IsSuccess && !busy;
        }
        private Result<RaceDirector> Preview(string id)
        {
            if (!calendarSaved || Session?.CareerRun?.IsActive != true) return Result<RaceDirector>.Failure("CREATE A NEW RACER FROM CAREER HOME TO ENTER EVENTS.");
            var allowed = calendar.CanEnter(Team, id);
            if (!allowed.IsSuccess) return Result<RaceDirector>.Failure(allowed.ErrorMessage);
            var raceId = calendar.NextRaceId(Team, id);
            var race = string.IsNullOrEmpty(raceId) ? null : context.Database.GetRaceDefinition(raceId);
            var track = race == null ? null : BootstrapController.ContentCatalog?.FindTrackContent(race.TrackId);
            if (track?.TrackPrefab == null) return Result<RaceDirector>.Failure("THIS ROUND HAS NO CONFIGURED TRACK PREFAB.");
            try { return context.RacePreparation.PrepareSelectedRace(raceId); }
            catch (Exception error) { return Result<RaceDirector>.Failure("RACE PREPARATION FAILED: " + error.Message); }
        }
        private void DisplayStandings()
        {
            var entry = Team.Calendar.Active ?? Team.Calendar.LastEvent;
            if (entry == null) { Set(selectedRequirementsText, "ENTER AN EVENT TO RECORD RESULTS."); return; }
            selectedEventId = entry.eventId;
            Set(selectedRaceNameText, entry.displayName);
            Set(selectedTrackText, entry.withdrawn ? "WITHDRAWN" : entry.completed ? "COMPLETED" : $"ROUND {entry.roundIndex + 1}/{entry.raceIds.Count}");
            var text = new StringBuilder("TEAM STANDINGS\n\n");
            foreach (var standing in entry.standings.OrderByDescending(s => s.points).ThenBy(s => s.teamName))
                text.AppendLine($"{1 + entry.standings.Count(s => s.points > standing.points)}. {standing.teamName}  ·  {standing.points} POINTS");
            text.AppendLine("\nTIES SHARE RANK. ABSENT TEAMS KEEP THEIR POINTS AND SCORE ZERO FOR THAT ROUND.");
            if (entry.completed && !entry.withdrawn) text.AppendLine($"\nFINAL RANK  {entry.finalRank}\nCHAMPIONSHIP BONUS  {entry.finalPrize:N0} CREDITS");
            Set(selectedRequirementsText, text.ToString());
            if (!entry.completed)
            {
                var available = Preview(entry.eventId);
                Set(selectedStatusText, available.IsSuccess ? "CONTINUE NEXT ROUND" : available.ErrorMessage);
                if (enterRaceButton != null) enterRaceButton.interactable = available.IsSuccess;
            }
        }
        private void ShowDiagram(string trackId)
        {
            var content = string.IsNullOrEmpty(trackId) ? null : BootstrapController.ContentCatalog?.FindTrackContent(trackId);
            var sprite = content?.TrackDiagram;
            if (trackDiagram != null) { trackDiagram.sprite = sprite; trackDiagram.preserveAspect = true; trackDiagram.enabled = sprite != null; }
            if (diagramPlaceholder != null) diagramPlaceholder.gameObject.SetActive(sprite == null);
        }
        private void ConfirmEntry()
        {
            if (busy || selectedEventId == null || !Preview(selectedEventId).IsSuccess) return;
            pendingEventId = selectedEventId; pendingAdvance = false;
            int fee = Team.Calendar.Active == null ? context.Database.GetCareerEventDefinition(selectedEventId).EntryFee : 0;
            string name = Team.Calendar.Active?.displayName ?? context.Database.GetCareerEventDefinition(selectedEventId).DisplayName;
            ShowConfirmation($"ENTER {name}?\n\nCHARGE NOW: {fee:N0} CREDITS\nREMAINING: {Team.Credits - fee:N0} CREDITS\n\nONE COMPLETED ROUND ADVANCES ONE WEEK. ENTRY FEES ARE FORFEITED IF YOU WITHDRAW.");
        }
        private void ConfirmAdvance()
        {
            if (busy || !calendarSaved) return;
            pendingAdvance = true; pendingEventId = null;
            ShowConfirmation(Team.Calendar.Active == null ? "SKIP THIS WEEK?\n\nA NEW DRAW WILL BE SAVED. RESEARCH STAFF DO NOT EARN POINTS FOR SKIPPED WEEKS."
                : "WITHDRAW FROM THIS EVENT?\n\nTHE ENTRY FEE IS NOT REFUNDED. NO COMPLETION PRIZE IS AWARDED. THE CALENDAR ADVANCES ONE WEEK.");
        }
        private void ShowConfirmation(string message)
        {
            if (confirmationRoot == null) { SetFeedback("REBUILD THE RACES SCREEN TO CONFIGURE CONFIRMATION."); return; }
            Set(confirmationText, message); confirmationRoot.SetActive(true);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) foreach (var selectable in canvas.GetComponentsInChildren<Selectable>())
                if (selectable.interactable && !selectable.transform.IsChildOf(confirmationRoot.transform))
                { blocked.Add(selectable); selectable.interactable = false; }
            confirmButton?.Select();
        }
        private void Confirm()
        {
            bool advance = pendingAdvance; string id = pendingEventId;
            Cancel();
            if (advance)
            {
                var before = Team.Calendar.Export(); string championship = Session.CareerRun?.ActiveChampionshipId;
                var result = calendar.SkipOrWithdraw(Session);
                if (!result.IsSuccess) { SetFeedback(result.ErrorMessage); return; }
                calendar.Refresh(Team);
                var saved = context.Saves.SaveCurrentCampaign();
                if (!saved.IsSuccess)
                {
                    Team.RestoreCalendar(before);
                    if (championship != null) Session.CareerRun.EnterChampionship(championship);
                    SetFeedback("CALENDAR NOT ADVANCED: " + saved.ErrorMessage); return;
                }
                selectedEventId = null; Refresh();
            }
            else if (id != null) owner?.LaunchCalendarEvent(id);
        }
        private void Cancel()
        {
            foreach (var control in blocked) if (control != null) control.interactable = true;
            blocked.Clear(); pendingAdvance = false; pendingEventId = null;
            if (confirmationRoot != null) confirmationRoot.SetActive(false);
        }
        public void SetBusy(bool value) { busy = value; if (enterRaceButton != null) enterRaceButton.interactable = !value && selectedEventId != null && Preview(selectedEventId).IsSuccess; }
        public void SetFeedback(string message) { Set(feedbackText, message); }
        private void ClearCards() { foreach (var card in cards) if (card != null) { card.gameObject.SetActive(false); Destroy(card.gameObject); } cards.Clear(); }
        private void OnDisable() { Cancel(); }
        private void OnDestroy()
        {
            Unbind(enterRaceButton, ConfirmEntry); Unbind(calendarButton, ShowCalendar); Unbind(unlockedButton, ShowUnlocked);
            Unbind(fameButton, ShowFame); Unbind(standingsButton, ShowStandings); Unbind(advanceButton, ConfirmAdvance);
            Unbind(confirmButton, Confirm); Unbind(cancelButton, Cancel);
        }
        private static void Set(TMP_Text text, string value) { if (text != null) text.text = value ?? ""; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action) { if (button == null) return; button.onClick.RemoveListener(action); button.onClick.AddListener(action); }
        private static void Unbind(Button button, UnityEngine.Events.UnityAction action) { if (button != null) button.onClick.RemoveListener(action); }
    }
}
