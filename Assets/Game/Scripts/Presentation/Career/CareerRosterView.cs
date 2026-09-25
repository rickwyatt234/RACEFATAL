using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerRosterView : MonoBehaviour
    {
        [SerializeField] private RectTransform racerList;
        [SerializeField] private RectTransform perkList;
        [SerializeField] private RectTransform recruitList;
        [SerializeField] private CareerGarageOptionView optionTemplate;
        [SerializeField] private TMP_Text profileText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text fundsText;
        [SerializeField] private Button assignButton;
        [SerializeField] private Button buyPerkButton;
        [SerializeField] private Button recruitButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private GameObject confirmationRoot;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private readonly List<CareerGarageOptionView> spawned = new List<CareerGarageOptionView>();
        private readonly List<Selectable> blocked = new List<Selectable>();
        private CareerController owner;
        private GameContext context;
        private RosterService service;
        private string selectedRacerId;
        private string selectedPerkId;
        private string selectedRecruitId;
        private string pendingId;
        private string pendingRacerId;
        private TeamState pendingTeam;
        private bool pendingRecruit;
        private TeamState Team => context?.Sessions?.Current?.PlayerTeam;
        private GameSessionState Session => context?.Sessions?.Current;

        public void Initialize(CareerController controller, GameContext gameContext)
        {
            owner = controller;
            context = gameContext;
            service = context?.Database == null ? null : new RosterService(context.Database);
            Bind(assignButton, AssignPartner);
            Bind(buyPerkButton, BuyPerk);
            Bind(recruitButton, Recruit);
            Bind(saveButton, Save);
            Bind(confirmButton, Confirm);
            Bind(cancelButton, Cancel);
            Cancel();
        }

        public void Refresh()
        {
            Cancel();
            ClearOptions();
            if (Team == null || service == null)
            {
                Set(profileText, "LOAD A CAMPAIGN TO MANAGE YOUR ROSTER.");
                Set(feedbackText, "START FROM 00_BOOTSTRAP.");
                Set(fundsText, string.Empty);
                SetActions(false, false, false);
                if (saveButton != null) saveButton.interactable = false;
                return;
            }
            var player = Session.CareerRun?.Player;
            if (saveButton != null) saveButton.interactable = context.Saves?.HasActiveCampaign == true;
            var racers = Team.Roster.Racers;
            if (selectedRacerId == null || Team.Roster.FindRacer(selectedRacerId) == null)
                selectedRacerId = player?.RacerId ?? racers.FirstOrDefault()?.RacerId;
            var selected = Team.Roster.FindRacer(selectedRacerId);
            var perks = context.Database.RacerPerkDefinitions.Values.OrderBy(p => p.DisplayName).ToList();
            if (selectedPerkId == null || !perks.Any(p => p.Id == selectedPerkId))
                selectedPerkId = perks.FirstOrDefault()?.Id;
            var recruits = context.Database.RacerDefinitions.Values
                .Where(d => d != null && Team.Roster.FindRacer(d.Id) == null)
                .OrderBy(d => d.DisplayName).ToList();
            if (selectedRecruitId == null || !recruits.Any(d => d.Id == selectedRecruitId))
                selectedRecruitId = recruits.FirstOrDefault()?.Id;
            Set(fundsText, $"TEAM CREDITS {Team.Credits:N0}  //  SELECTED RACER FAME {selected?.Progression.Fame ?? 0:N0}");
            foreach (var racer in racers)
            {
                string role = racer.IsPlayerCharacter ? "PLAYER" : racer.RacerId == Session.SelectedPartnerRacerId ? "PARTNER" : "RESERVE";
                AddOption(racerList, $"{racer.Name}  [{role}]\n{racer.Status}  //  FAME {racer.Progression.Fame:N0}",
                    () => { selectedRacerId = racer.RacerId; Refresh(); }, racer.RacerId == selectedRacerId);
            }
            foreach (var perk in perks)
            {
                bool owned = selected?.Progression.HasPurchasedPerk(perk.Id) == true;
                bool available = service.CanPurchasePerk(Team, selectedRacerId, perk.Id).IsSuccess;
                AddOption(perkList, $"{perk.DisplayName}  [{(owned ? "OWNED" : available ? "AVAILABLE" : "LOCKED")}]\n{perk.FameCost:N0} FAME  //  {perk.Effect} +{perk.Strength:0.##}",
                    () => { selectedPerkId = perk.Id; Refresh(); }, perk.Id == selectedPerkId);
            }
            foreach (var recruit in recruits)
                AddOption(recruitList, $"{recruit.DisplayName}\n{RosterService.RecruitmentCost:N0} CREDITS",
                    () => { selectedRecruitId = recruit.Id; Refresh(); }, recruit.Id == selectedRecruitId);
            var choice = context.Database.GetRacerPerkDefinition(selectedPerkId);
            var profile = new System.Text.StringBuilder();
            if (selected != null)
            {
                profile.AppendLine($"{selected.Name}  //  {selected.Status}  //  {(selected.IsPlayerCharacter ? "PLAYER" : "TEAM RACER")}");
                profile.AppendLine($"RACES {selected.RacesEntered}  WINS {selected.RacesWon}  PODIUMS {selected.Podiums}  DESTROYED {selected.RacersDestroyed}");
                profile.AppendLine($"FAME {selected.Progression.Fame:N0}  //  PERKS {selected.Progression.PurchasedPerkIds.Count}");
                if (!selected.CanRace) profile.AppendLine("UNAVAILABLE FOR RACE AND PERK PURCHASES.");
            }
            if (choice != null) profile.AppendLine($"\nSELECTED PERK: {choice.DisplayName}  //  {choice.Description}");
            Set(profileText, profile.ToString());
            Result canPerk = service.CanPurchasePerk(Team, selectedRacerId, selectedPerkId);
            Result canHire = service.CanRecruit(Team, selectedRecruitId);
            bool persistent = context.Saves?.HasActiveCampaign == true;
            SetActions(persistent && selected != null && !selected.IsPlayerCharacter && selected.CanRace &&
                       selected.RacerId != Session.SelectedPartnerRacerId,
                       persistent && canPerk.IsSuccess, persistent && canHire.IsSuccess);
        }

        private void BuyPerk() { RequestConfirmation(false); }
        private void Recruit() { RequestConfirmation(true); }

        private void AssignPartner()
        {
            if (context?.Saves?.HasActiveCampaign != true) return;
            Result result = Session.SelectPartnerRacer(selectedRacerId);
            if (result.IsSuccess)
            {
                var saved = context.Saves.SaveCurrentCampaign();
                Refresh();
                Set(feedbackText, saved.IsSuccess ? "PARTNER ASSIGNED. CAMPAIGN SAVED."
                    : "ASSIGNMENT NOT SAVED: " + saved.ErrorMessage + " USE SAVE CAMPAIGN TO RETRY.");
                owner?.RefreshHome();
            }
            else Set(feedbackText, result.ErrorMessage);
        }

        private void RequestConfirmation(bool recruit)
        {
            if (context?.Saves?.HasActiveCampaign != true || confirmationRoot == null) return;
            Result allowed = recruit ? service.CanRecruit(Team, selectedRecruitId)
                : service.CanPurchasePerk(Team, selectedRacerId, selectedPerkId);
            if (!allowed.IsSuccess) { Set(feedbackText, allowed.ErrorMessage); return; }
            pendingRecruit = recruit;
            pendingId = recruit ? selectedRecruitId : selectedPerkId;
            pendingRacerId = selectedRacerId;
            pendingTeam = Team;
            if (recruit)
            {
                var definition = context.Database.GetRacerDefinition(pendingId);
                Set(confirmationText, $"RECRUIT {definition.DisplayName}?\n\nCOST {RosterService.RecruitmentCost:N0} CREDITS\nREMAINING {Team.Credits - RosterService.RecruitmentCost:N0} CREDITS\n\nASSIGN THEM AS PARTNER AFTER RECRUITMENT.");
            }
            else
            {
                var perk = context.Database.GetRacerPerkDefinition(pendingId);
                var racer = Team.Roster.FindRacer(pendingRacerId);
                Set(confirmationText, $"BUY {perk.DisplayName} FOR {racer.Name}?\n\nCOST {perk.FameCost:N0} CHARACTER FAME\nREMAINING {racer.Progression.Fame - perk.FameCost:N0} FAME\n\n{perk.Description}");
            }
            confirmationRoot.SetActive(true);
            Canvas canvas = GetComponentInParent<Canvas>();
            foreach (var control in canvas != null ? canvas.GetComponentsInChildren<Selectable>() : new Selectable[0])
            {
                if (!control.interactable || control.transform.IsChildOf(confirmationRoot.transform)) continue;
                blocked.Add(control);
                control.interactable = false;
            }
            confirmButton?.Select();
        }

        private void Confirm()
        {
            // Consume the pending action before any mutation; a double-click cannot charge twice.
            string id = pendingId, racer = pendingRacerId;
            TeamState team = pendingTeam;
            bool recruit = pendingRecruit;
            Cancel();
            if (team == null || id == null || team != Team || context?.Saves?.HasActiveCampaign != true) return;
            Result result = recruit ? service.Recruit(team, id) : service.PurchasePerk(team, racer, id);
            Refresh();
            if (!result.IsSuccess) { Set(feedbackText, result.ErrorMessage); return; }
            var saved = context.Saves.SaveCurrentCampaign();
            Set(feedbackText, saved.IsSuccess ? recruit ? "RACER RECRUITED. CAMPAIGN SAVED."
                    : "PERK PURCHASED. CAMPAIGN SAVED."
                    : "CHANGE NOT SAVED: " + saved.ErrorMessage + " USE SAVE CAMPAIGN TO RETRY.");
            owner?.RefreshHome();
        }

        private void Save()
        {
            if (context?.Saves?.HasActiveCampaign != true) return;
            Result saved = context.Saves.SaveCurrentCampaign();
            Set(feedbackText, saved.IsSuccess ? "CAMPAIGN SAVED." : "SAVE FAILED: " + saved.ErrorMessage);
        }

        private void Cancel()
        {
            foreach (var control in blocked) if (control != null) control.interactable = true;
            blocked.Clear();
            pendingId = null;
            pendingRacerId = null;
            pendingTeam = null;
            pendingRecruit = false;
            if (confirmationRoot != null) confirmationRoot.SetActive(false);
        }
        private void OnDisable() { Cancel(); }
        private void OnDestroy()
        {
            if (assignButton != null) assignButton.onClick.RemoveListener(AssignPartner);
            if (buyPerkButton != null) buyPerkButton.onClick.RemoveListener(BuyPerk);
            if (recruitButton != null) recruitButton.onClick.RemoveListener(Recruit);
            if (saveButton != null) saveButton.onClick.RemoveListener(Save);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
        }
        private void SetActions(bool assign, bool perk, bool recruit)
        {
            if (assignButton != null) assignButton.interactable = assign;
            if (buyPerkButton != null) buyPerkButton.interactable = perk;
            if (recruitButton != null) recruitButton.interactable = recruit;
        }
        private void AddOption(RectTransform list, string label, System.Action action, bool selected)
        {
            if (list == null || optionTemplate == null) return;
            var option = Instantiate(optionTemplate, list);
            option.gameObject.SetActive(true);
            option.Bind(label, action, true, selected);
            spawned.Add(option);
        }
        private void ClearOptions()
        {
            foreach (var option in spawned)
            {
                if (option == null) continue;
                option.gameObject.SetActive(false);
                Destroy(option.gameObject);
            }
            spawned.Clear();
        }
        private static void Set(TMP_Text target, string text) { if (target != null) target.text = text; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
