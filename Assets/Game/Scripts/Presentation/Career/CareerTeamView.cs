using System.Linq;
using System.Text;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public sealed class CareerTeamView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_InputField primaryInput;
        [SerializeField] private TMP_InputField secondaryInput;
        [SerializeField] private Image primaryPreview;
        [SerializeField] private Image secondaryPreview;
        [SerializeField] private Toggle repaintToggle;
        [SerializeField] private TMP_Text overviewText;
        [SerializeField] private TMP_Text progressionText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button revertButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button rosterButton;
        [SerializeField] private Button garageButton;
        [SerializeField] private Button researchButton;

        private CareerController owner;
        private GameContext context;
        private readonly TeamManagementService service = new TeamManagementService();
        private TeamState Team => context?.Sessions?.Current?.PlayerTeam;

        public void Initialize(CareerController controller, GameContext gameContext)
        {
            owner = controller;
            context = gameContext;
            Bind(applyButton, Apply);
            Bind(revertButton, Refresh);
            Bind(saveButton, Save);
            Bind(rosterButton, OpenRoster);
            Bind(garageButton, OpenGarage);
            Bind(researchButton, OpenResearch);
            if (primaryInput != null)
            {
                primaryInput.onValueChanged.RemoveListener(UpdatePrimaryPreview);
                primaryInput.onValueChanged.AddListener(UpdatePrimaryPreview);
            }
            if (secondaryInput != null)
            {
                secondaryInput.onValueChanged.RemoveListener(UpdateSecondaryPreview);
                secondaryInput.onValueChanged.AddListener(UpdateSecondaryPreview);
            }
        }

        public void Refresh()
        {
            bool loaded = Team != null && context.Database != null;
            bool editable = loaded && context.Saves?.HasActiveCampaign == true;
            if (nameInput != null) { nameInput.interactable = editable; nameInput.SetTextWithoutNotify(Team?.TeamName ?? ""); }
            if (primaryInput != null) { primaryInput.interactable = editable; primaryInput.SetTextWithoutNotify(Team?.PrimaryColor ?? ""); }
            if (secondaryInput != null) { secondaryInput.interactable = editable; secondaryInput.SetTextWithoutNotify(Team?.SecondaryColor ?? ""); }
            if (repaintToggle != null) { repaintToggle.interactable = editable; repaintToggle.SetIsOnWithoutNotify(false); }
            if (applyButton != null) applyButton.interactable = editable;
            if (revertButton != null) revertButton.interactable = loaded;
            if (saveButton != null) saveButton.interactable = editable;
            UpdatePrimaryPreview(Team?.PrimaryColor);
            UpdateSecondaryPreview(Team?.SecondaryColor);
            Set(feedbackText, loaded ? "EDIT TEAM IDENTITY, THEN APPLY & SAVE." : "LOAD A CAMPAIGN FROM 00_BOOTSTRAP.");
            if (!loaded)
            {
                Set(overviewText, "NO TEAM LOADED.");
                Set(progressionText, "NO CAMPAIGN PROGRESSION.");
                return;
            }
            var session = context.Sessions.Current;
            var team = Team;
            var racers = team.Roster.Racers;
            var garage = team.Garage;
            var overview = new StringBuilder();
            overview.AppendLine($"{team.TeamName}\n");
            overview.AppendLine($"CREDITS  {team.Credits:N0}");
            overview.AppendLine($"TEAM FAME  {team.Fame:N0}");
            overview.AppendLine($"RESEARCH POINTS  {team.ResearchPoints:N0}");
            overview.AppendLine($"\nCAREER  {(session.CareerRun == null ? "NONE" : session.CareerRun.IsActive ? "ACTIVE" : "ENDED")}");
            overview.AppendLine($"PLAYER  {session.CareerRun?.Player?.Name ?? "NONE"}");
            var partner = team.Roster.FindRacer(session.SelectedPartnerRacerId);
            overview.AppendLine($"PARTNER  {partner?.Name ?? "NONE"}{(partner != null && !partner.CanRace ? " (UNAVAILABLE)" : "")}");
            overview.AppendLine($"\nROSTER  {racers.Count} RACERS / {racers.Count(r => r.CanRace)} ACTIVE");
            overview.AppendLine($"RACER STARTS  {racers.Sum(r => (long)r.RacesEntered):N0}");
            overview.AppendLine($"RACER WINS  {racers.Sum(r => (long)r.RacesWon):N0}");
            overview.AppendLine($"RACER PODIUMS  {racers.Sum(r => (long)r.Podiums):N0}");
            overview.AppendLine($"RACERS DESTROYED  {racers.Sum(r => (long)r.RacersDestroyed):N0}");
            overview.AppendLine($"\nGARAGE  {garage.Bikes.Count} OWNED BIKES");
            overview.AppendLine($"READY  {garage.Bikes.Count(b => b.IsRaceReady)} / DESTROYED  {garage.Bikes.Count(b => b.IsDestroyed)}");
            overview.AppendLine($"ENGINES  {garage.Engines.Count} / CHASSIS  {garage.Chassis.Count}");
            overview.AppendLine($"EQUIPMENT  {garage.Equipment.Count}");
            Set(overviewText, overview.ToString());

            var progression = new StringBuilder();
            progression.AppendLine($"RESEARCH STAFF\n{team.ResearchOutputPerRace:N0} RP AFTER EACH RACE\n");
            if (team.ResearchContracts.Count == 0) progression.AppendLine("No researchers hired. Visit Research to hire staff.");
            foreach (var contract in team.ResearchContracts.OrderBy(c => c.DisplayName))
                progression.AppendLine(contract.RacesRemaining > 0
                    ? $"{contract.DisplayName}\n{contract.PointsPerRace:N0} RP / RACE · {contract.RacesRemaining} RACES LEFT\n"
                    : $"{contract.DisplayName}\nCONTRACT EXPIRED\n");
            progression.AppendLine($"\nTECHNOLOGIES  {team.UnlockedTechnologyIds.Count}\n");
            if (team.UnlockedTechnologyIds.Count == 0) progression.AppendLine("No technologies researched yet.");
            foreach (string id in team.UnlockedTechnologyIds.OrderBy(id => id))
                progression.AppendLine(context.Database.GetTechnologyDefinition(id)?.DisplayName ?? id);
            progression.AppendLine($"\nCHAMPIONSHIP UNLOCKS  {team.UnlockedChampionshipIds.Count}\n");
            if (team.UnlockedChampionshipIds.Count == 0) progression.AppendLine("No championships unlocked yet.");
            foreach (string id in team.UnlockedChampionshipIds.OrderBy(id => id)) progression.AppendLine(id);
            Set(progressionText, progression.ToString());
        }

        private void Apply()
        {
            if (context?.Saves?.HasActiveCampaign != true) return;
            var result = service.UpdateIdentity(Team, nameInput?.text, primaryInput?.text, secondaryInput?.text,
                repaintToggle != null && repaintToggle.isOn);
            if (!result.IsSuccess) { Set(feedbackText, result.ErrorMessage); return; }
            var saved = context.Saves.SaveCurrentCampaign();
            Refresh();
            owner?.RefreshHome();
            Set(feedbackText, saved.IsSuccess ? "TEAM UPDATED. CAMPAIGN SAVED."
                : "TEAM UPDATED BUT SAVE FAILED: " + saved.ErrorMessage + " USE SAVE CAMPAIGN TO RETRY.");
        }

        private void Save()
        {
            if (context?.Saves?.HasActiveCampaign != true) return;
            var result = context.Saves.SaveCurrentCampaign();
            Set(feedbackText, result.IsSuccess ? "CAMPAIGN SAVED. APPLY ANY PENDING IDENTITY EDITS TO SAVE THEM."
                : "SAVE FAILED: " + result.ErrorMessage);
        }

        private void OpenRoster() { owner?.ShowRoster(); }
        private void OpenGarage() { owner?.ShowGarage(); }
        private void OpenResearch() { owner?.ShowResearch(); }
        private void UpdatePrimaryPreview(string value) { Preview(primaryPreview, value); }
        private void UpdateSecondaryPreview(string value) { Preview(secondaryPreview, value); }
        private static void Preview(Image image, string value)
        {
            if (image == null) return;
            image.color = TeamManagementService.TryNormalizeColor(value, out string normalized) &&
                ColorUtility.TryParseHtmlString(normalized, out Color color) ? color : Color.gray;
        }
        private static void Set(TMP_Text text, string value) { if (text != null) text.text = value; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
        private void OnDestroy()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(Apply);
            if (revertButton != null) revertButton.onClick.RemoveListener(Refresh);
            if (saveButton != null) saveButton.onClick.RemoveListener(Save);
            if (rosterButton != null) rosterButton.onClick.RemoveListener(OpenRoster);
            if (garageButton != null) garageButton.onClick.RemoveListener(OpenGarage);
            if (researchButton != null) researchButton.onClick.RemoveListener(OpenResearch);
            if (primaryInput != null) primaryInput.onValueChanged.RemoveListener(UpdatePrimaryPreview);
            if (secondaryInput != null) secondaryInput.onValueChanged.RemoveListener(UpdateSecondaryPreview);
        }
    }
}
