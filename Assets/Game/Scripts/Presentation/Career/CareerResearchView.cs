using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Infrastructure;
using RaceFatal.Shared;
using RaceFatal.Presentation.Bootstrap;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    public class CareerResearchView : MonoBehaviour
    {
        [SerializeField] private CareerResearchTreeView treeView;
        [SerializeField] private GameObject staffListRoot;
        private string selectedResearcherId;
        private ResearcherDefinition pendingResearcher;
        private ResearcherService researchers;
        [Header("Lists")]
        [SerializeField] private RectTransform categoryList;
        [SerializeField] private RectTransform itemList;
        [SerializeField] private CareerGarageOptionView optionTemplate;
        [Header("Details")]
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text feedbackText;
        [Header("Actions")]
        [SerializeField] private Button researchButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button shopButton;
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationRoot;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private readonly List<CareerGarageOptionView> spawned = new List<CareerGarageOptionView>();
        private readonly List<Selectable> modalBlocked = new List<Selectable>();
        private GameContext context;
        private CareerController owner;
        private ResearchService research;
        private TechnologyDefinition selected;
        private TechnologyDefinition pending;
        private TeamState pendingTeam;
        private string category = "ALL";
        private TeamState Team => context?.Sessions?.Current?.PlayerTeam;

        public void Initialize(CareerController controller, GameContext gameContext)
        {
            owner = controller;
            context = gameContext;
            research = context?.Database != null ? new ResearchService(context.Database) : null;
            researchers = context?.Database != null ? new ResearcherService(context.Database) : null;
            Bind(researchButton, RequestResearch);
            Bind(saveButton, SaveResearch);
            Bind(shopButton, OpenShop);
            Bind(confirmButton, ConfirmResearch);
            Bind(cancelButton, DismissResearch);
            CancelResearch();
        }

        public void Refresh()
        {
            CancelResearch();
            ClearOptions();
            SetText(pointsText, Team != null ? $"RP {Team.ResearchPoints:N0}  //  STAFF +{Team.ResearchOutputPerRace:N0}/RACE" : "RESEARCH POINTS  --");
            if (saveButton != null) saveButton.interactable = context?.Saves?.HasActiveCampaign == true;
            if (shopButton != null) shopButton.interactable = Team != null;
            if (research == null || Team == null)
            {
                selected = null;
                SetText(detailsText, "NO LOADED CAMPAIGN.");
                SetText(feedbackText, "START FROM 00_Bootstrap AND LOAD A CAMPAIGN.");
                if (researchButton != null) researchButton.interactable = false;
                return;
            }

            List<TechnologyDefinition> technologies = context.Database.TechnologyDefinitions.Values
                .OrderBy(t => t.DisplayOrder).ThenBy(t => t.DisplayName).ThenBy(t => t.Id).ToList();
            var categories = new List<string> { "ALL", "STAFF" };
            categories.AddRange(technologies.Select(t => t.Field.ToString()).Distinct());
            if (!categories.Contains(category)) category = "ALL";
            foreach (string value in categories)
                AddOption(categoryList, FieldLabel(value), () => { category = value; selected = null; Refresh(); }, value == category);
            if (treeView != null) treeView.gameObject.SetActive(category != "STAFF");
            if (staffListRoot != null) staffListRoot.SetActive(category == "STAFF");
            var actionLabel = researchButton != null ? researchButton.GetComponentInChildren<TMP_Text>() : null;
            SetText(actionLabel, category == "STAFF" ? "HIRE RESEARCHER" : "RESEARCH TECHNOLOGY");
            if (category == "STAFF") { RenderStaff(); return; }
            var visible = technologies.Where(t => category == "ALL" || t.Field.ToString() == category).ToList();
            selected = selected == null ? null : visible.Find(t => t.Id == selected.Id);
            if (selected == null && visible.Count > 0) selected = visible[0];
            treeView?.Render(context.Database, Team, BootstrapController.ContentCatalog?.ResearchTreeLayout,
                category, selected?.Id, SelectTreeTechnology);
            RenderDetails();
        }

        private void SelectTreeTechnology(string id)
        {
            var technology = context?.Database?.GetTechnologyDefinition(id);
            if (technology == null) return;
            selected = technology;
            if (category != "ALL") category = technology.Field.ToString();
            Refresh();
        }

        public void FocusTechnology(string id)
        {
            var technology = context?.Database?.GetTechnologyDefinition(id);
            if (technology == null) { SetText(feedbackText, "TECHNOLOGY IS NOT REGISTERED: " + id); return; }
            selected = technology;
            category = technology.Field.ToString();
            Refresh();
            treeView?.Focus(id);
        }

        private void RenderStaff()
        {
            var offers = context.Database.ResearcherDefinitions.Values.OrderBy(o => o.DisplayName).ToList();
            if (selectedResearcherId == null && offers.Count > 0) selectedResearcherId = offers[0].Id;
            foreach (var offer in offers)
                AddOption(itemList, $"{offer.DisplayName}\n{offer.CreditCost:N0} CR • +{offer.PointsPerRace} RP × {offer.Duration} RACES",
                    () => { selectedResearcherId = offer.Id; Refresh(); }, selectedResearcherId == offer.Id);
            var chosen = context.Database.GetResearcherDefinition(selectedResearcherId);
            var text = new System.Text.StringBuilder("RESEARCH STAFF\n\nACTIVE / COMPLETED CONTRACTS\n");
            foreach (var contract in Team.ResearchContracts)
                text.AppendLine($"{contract.DisplayName}: +{contract.PointsPerRace} RP / race; {contract.RacesRemaining} races left");
            if (Team.ResearchContracts.Count == 0) text.AppendLine("No contracts.");
            if (chosen != null)
            {
                text.AppendLine($"\n{chosen.DisplayName}\nPREPAID COST {chosen.CreditCost:N0} CR\n+{chosen.PointsPerRace} RP AFTER EACH RACE\nDURATION {chosen.Duration} RACES");
                text.AppendLine("\nResolved DNFs count. Abandoned races do not. No automatic renewal. One active contract per researcher.");
                var allowed = researchers.CanHire(Team, chosen.Id);
                text.AppendLine(allowed.IsSuccess ? "AVAILABLE TO HIRE" : allowed.ErrorMessage);
                if (researchButton != null) researchButton.interactable = allowed.IsSuccess && context?.Saves?.HasActiveCampaign == true;
            }
            else
            {
                text.AppendLine("\nNo researcher offers. Add contracts to the content catalog.");
                if (researchButton != null) researchButton.interactable = false;
            }
            SetText(detailsText, text.ToString());
        }

        private void RenderDetails()
        {
            if (selected == null)
            {
                SetText(detailsText, "NO TECHNOLOGIES IN THIS FIELD. ADD TECHNOLOGIES TO THE CONTENT CATALOG.");
                if (researchButton != null) researchButton.interactable = false;
                return;
            }
            Result allowed = research.CanResearch(Team, selected.Id);
            var text = new System.Text.StringBuilder();
            text.AppendLine(selected.DisplayName).AppendLine().AppendLine(selected.Description);
            text.AppendLine($"\nCOST  {selected.ResearchCost:N0} RP");
            text.AppendLine("\nPREREQUISITES");
            if (selected.PrerequisiteTechnologyIds.Count == 0) text.AppendLine("None");
            foreach (string id in selected.PrerequisiteTechnologyIds)
            {
                var prerequisite = context.Database.GetTechnologyDefinition(id);
                text.AppendLine((Team.HasTechnology(id) ? "[DONE] " : "[NEEDED] ") + (prerequisite?.DisplayName ?? id));
            }
            text.AppendLine("\nUNLOCKS IN SHOP");
            var offers = new ShopService(context.Database).GetOffers().Where(o => o.RequiredTechnologyId == selected.Id).ToList();
            foreach (var offer in offers) text.AppendLine(offer.DisplayName);
            if (offers.Count == 0) text.AppendLine("No direct component unlocks.");
            text.AppendLine("\n" + (allowed.IsSuccess ? "AVAILABLE TO RESEARCH" : allowed.ErrorMessage));
            SetText(detailsText, text.ToString());
            if (researchButton != null) researchButton.interactable = allowed.IsSuccess && context?.Saves?.HasActiveCampaign == true;
        }

        private void RequestResearch()
        {
            if (research == null || confirmationRoot == null || (selected == null && category != "STAFF")) return;
            if (context?.Saves?.HasActiveCampaign != true)
            {
                SetText(feedbackText, "LOAD A SAVED CAMPAIGN BEFORE RESEARCHING.");
                return;
            }
            if (category == "STAFF")
            {
                var allowedHire = researchers.CanHire(Team, selectedResearcherId);
                if (!allowedHire.IsSuccess) { SetText(feedbackText, allowedHire.ErrorMessage); return; }
                pendingResearcher = context.Database.GetResearcherDefinition(selectedResearcherId);
                pendingTeam = Team;
                SetText(confirmationText, $"HIRE {pendingResearcher.DisplayName}?\n\nPREPAY {pendingResearcher.CreditCost:N0} CR\n+{pendingResearcher.PointsPerRace} RP / RACE FOR {pendingResearcher.Duration} RACES\nREMAINING {Team.Credits - pendingResearcher.CreditCost:N0} CR");
                ShowConfirmation();
                return;
            }
            Result allowed = research.CanResearch(Team, selected.Id);
            if (!allowed.IsSuccess) { SetText(feedbackText, allowed.ErrorMessage); return; }
            pending = selected;
            pendingTeam = Team;
            SetText(confirmationText, $"RESEARCH {pending.DisplayName}?\n\nCOST  {pending.ResearchCost:N0} RP\nREMAINING  {Team.ResearchPoints - pending.ResearchCost:N0} RP\n\nTHIS UNLOCK IS PERMANENT. THE CAMPAIGN WILL BE SAVED.");
            ShowConfirmation();
        }

        private void ShowConfirmation()
        {
            confirmationRoot.SetActive(true);
            // Block keyboard/controller navigation as well as pointer clicks behind the dialog.
            Canvas canvas = GetComponentInParent<Canvas>();
            foreach (Selectable control in canvas != null
                ? canvas.GetComponentsInChildren<Selectable>() : new Selectable[0])
            {
                if (!control.interactable || control.transform.IsChildOf(confirmationRoot.transform)) continue;
                modalBlocked.Add(control);
                control.interactable = false;
            }
            if (confirmButton != null) confirmButton.Select();
        }

        private void ConfirmResearch()
        {
            // Consume confirmation first so a repeated click cannot spend twice.
            TechnologyDefinition technology = pending;
            ResearcherDefinition hire = pendingResearcher;
            TeamState team = pendingTeam;
            CancelResearch();
            if ((technology == null && hire == null) || research == null || team == null || team != Team) return;
            if (context?.Saves?.HasActiveCampaign != true)
            {
                SetText(feedbackText, "LOAD A SAVED CAMPAIGN BEFORE RESEARCHING.");
                return;
            }
            if (hire != null)
            {
                if (context.Database.GetResearcherDefinition(hire.Id) != hire) { SetText(feedbackText, "CONTRACT CHANGED. REVIEW AGAIN."); return; }
                var hired = researchers.Hire(team, hire.Id);
                Refresh();
                if (hired.IsSuccess)
                {
                    var savedHire = context.Saves.SaveCurrentCampaign();
                    SetText(feedbackText, savedHire.IsSuccess ? "RESEARCHER HIRED. CAMPAIGN SAVED." : "HIRED BUT NOT SAVED: " + savedHire.ErrorMessage + " USE SAVE CAMPAIGN TO RETRY.");
                    owner?.RefreshHome();
                }
                else SetText(feedbackText, hired.ErrorMessage);
                return;
            }
            var current = context.Database.GetTechnologyDefinition(technology.Id);
            if (current != technology)
            {
                Refresh();
                SetText(feedbackText, "TECHNOLOGY CHANGED. REVIEW AND TRY AGAIN.");
                return;
            }
            Result result = research.Research(team, technology.Id);
            Refresh();
            if (result.IsSuccess)
            {
                Result saved = context.Saves.SaveCurrentCampaign();
                SetText(feedbackText, saved.IsSuccess
                    ? $"RESEARCHED {technology.DisplayName}. CAMPAIGN SAVED."
                    : "RESEARCH APPLIED BUT NOT SAVED: " + saved.ErrorMessage + " USE SAVE CAMPAIGN TO RETRY.");
                owner?.RefreshHome();
            }
            else SetText(feedbackText, result.ErrorMessage);
            if (researchButton != null && researchButton.interactable) researchButton.Select();
        }

        private void CancelResearch()
        {
            foreach (Selectable control in modalBlocked)
                if (control != null) control.interactable = true;
            modalBlocked.Clear();
            pending = null;
            pendingResearcher = null;
            pendingTeam = null;
            if (confirmationRoot != null) confirmationRoot.SetActive(false);
        }

        private void DismissResearch()
        {
            CancelResearch();
            if (researchButton != null && researchButton.interactable) researchButton.Select();
        }

        private void SaveResearch()
        {
            if (context?.Saves?.HasActiveCampaign != true)
            {
                SetText(feedbackText, "NO PERSISTENT CAMPAIGN TO SAVE.");
                return;
            }
            Result result = context.Saves.SaveCurrentCampaign();
            SetText(feedbackText, result.IsSuccess ? "CAMPAIGN SAVED." : "SAVE FAILED: " + result.ErrorMessage);
        }

        private void OpenShop() { owner?.ShowShop(); }
        private void OnDisable() { CancelResearch(); }

        private void AddOption(RectTransform list, string label, System.Action click, bool isSelected)
        {
            if (list == null || optionTemplate == null) return;
            var option = Instantiate(optionTemplate, list);
            option.gameObject.SetActive(true);
            option.Bind(label, click, true, isSelected);
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

        private static string FieldLabel(string value)
        {
            return System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2").ToUpperInvariant();
        }

        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void OnDestroy()
        {
            if (researchButton != null) researchButton.onClick.RemoveListener(RequestResearch);
            if (saveButton != null) saveButton.onClick.RemoveListener(SaveResearch);
            if (shopButton != null) shopButton.onClick.RemoveListener(OpenShop);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmResearch);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(DismissResearch);
        }
    }
}
