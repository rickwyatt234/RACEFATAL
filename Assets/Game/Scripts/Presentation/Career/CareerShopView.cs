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
    public class CareerShopView : MonoBehaviour
    {
        [Header("Lists")]
        [SerializeField] private RectTransform categoryList;
        [SerializeField] private RectTransform itemList;
        [SerializeField] private CareerGarageOptionView optionTemplate;
        [Header("Details")]
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text feedbackText;
        [Header("Actions")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button garageButton;
        [Header("Confirmation")]
        [SerializeField] private GameObject confirmationRoot;
        [SerializeField] private TMP_Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private readonly List<CareerGarageOptionView> spawned = new List<CareerGarageOptionView>();
        private readonly List<Selectable> modalBlocked = new List<Selectable>();
        private GameContext context;
        private CareerController owner;
        private ShopService shop;
        private ShopOffer selected;
        private ShopOffer pending;
        private TeamState pendingTeam;
        private string category = "ALL";
        private TeamState Team => context?.Sessions?.Current?.PlayerTeam;

        public void Initialize(CareerController controller, GameContext gameContext)
        {
            owner = controller;
            context = gameContext;
            shop = context?.Database != null ? new ShopService(context.Database) : null;
            Bind(purchaseButton, RequestPurchase);
            Bind(saveButton, SaveShop);
            Bind(garageButton, OpenGarage);
            Bind(confirmButton, ConfirmPurchase);
            Bind(cancelButton, DismissPurchase);
            CancelPurchase();
        }

        public void Refresh()
        {
            CancelPurchase();
            ClearOptions();
            SetText(creditsText, Team != null ? $"CREDITS  {Team.Credits:N0}" : "CREDITS  --");
            if (saveButton != null) saveButton.interactable = context?.Saves?.HasActiveCampaign == true;
            if (garageButton != null) garageButton.interactable = Team != null;
            if (shop == null || Team == null)
            {
                selected = null;
                SetText(detailsText, "NO LOADED CAMPAIGN.");
                SetText(feedbackText, "START FROM 00_Bootstrap AND LOAD A CAMPAIGN.");
                if (purchaseButton != null) purchaseButton.interactable = false;
                return;
            }

            List<ShopOffer> offers = shop.GetOffers();
            var categories = new List<string> { "ALL" };
            categories.AddRange(offers.Select(o => o.Category).Distinct());
            if (!categories.Contains(category)) category = "ALL";
            foreach (string value in categories)
                AddOption(categoryList, value, () => { category = value; selected = null; Refresh(); }, value == category);

            List<ShopOffer> visible = offers.Where(o => category == "ALL" || o.Category == category).ToList();
            selected = selected == null ? null : visible.Find(o => o.Kind == selected.Kind && o.DefinitionId == selected.DefinitionId);
            if (selected == null && visible.Count > 0) selected = visible[0];
            foreach (ShopOffer offer in visible)
            {
                bool locked = !string.IsNullOrWhiteSpace(offer.RequiredTechnologyId) && !Team.HasTechnology(offer.RequiredTechnologyId);
                string state = offer.CreditCost < 0 ? "  [UNAVAILABLE]" : locked ? "  [LOCKED]" : Team.Credits < offer.CreditCost ? "  [LOW CREDITS]" : "";
                AddOption(itemList, $"{offer.DisplayName}{state}\n{offer.Category}  //  {offer.CreditCost:N0} CR",
                    () => { selected = offer; Refresh(); }, offer == selected);
            }
            RenderDetails();
        }

        private void RenderDetails()
        {
            if (selected == null)
            {
                SetText(detailsText, "NO ITEMS AVAILABLE IN THIS CATEGORY.");
                if (purchaseButton != null) purchaseButton.interactable = false;
                return;
            }
            Result allowed = shop.CanPurchase(Team, selected.Kind, selected.DefinitionId);
            int owned = selected.Kind == ShopItemKind.Engine
                ? Team.Garage.Engines.Count(e => e.EngineDefinitionId == selected.DefinitionId)
                : selected.Kind == ShopItemKind.Chassis
                    ? Team.Garage.Chassis.Count(c => c.ChassisDefinitionId == selected.DefinitionId)
                    : Team.Garage.Equipment.Count(e => e.EquipmentDefinitionId == selected.DefinitionId);
            SetText(detailsText, $"{selected.DisplayName}\n\n{selected.Details}\n\nPRICE  {selected.CreditCost:N0} CR\nOWNED  {owned}\n\n" +
                (allowed.IsSuccess ? "AVAILABLE // ADDED TO GARAGE INVENTORY" : allowed.ErrorMessage) +
                "\n\nINSTALL PURCHASED COMPONENTS IN GARAGE.");
            if (purchaseButton != null) purchaseButton.interactable = allowed.IsSuccess;
        }

        private void RequestPurchase()
        {
            if (selected == null || shop == null || confirmationRoot == null) return;
            Result allowed = shop.CanPurchase(Team, selected.Kind, selected.DefinitionId);
            if (!allowed.IsSuccess) { SetText(feedbackText, allowed.ErrorMessage); return; }
            pending = selected;
            pendingTeam = Team;
            SetText(confirmationText, $"BUY {pending.DisplayName}?\n\nCOST  {pending.CreditCost:N0} CR\nREMAINING  {Team.Credits - pending.CreditCost:N0} CR\n\nONE NEW COMPONENT WILL BE ADDED TO YOUR GARAGE.");
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

        private void ConfirmPurchase()
        {
            // Consume the pending confirmation before mutating: repeated clicks cannot buy twice.
            ShopOffer offer = pending;
            TeamState team = pendingTeam;
            CancelPurchase();
            if (offer == null || shop == null || team == null || team != Team) return;
            ShopOffer current = shop.FindOffer(offer.Kind, offer.DefinitionId);
            if (current == null || current.CreditCost != offer.CreditCost)
            {
                Refresh();
                SetText(feedbackText, "OFFER CHANGED. REVIEW THE PRICE AND TRY AGAIN.");
                return;
            }
            Result result = shop.Purchase(team, offer.Kind, offer.DefinitionId);
            Refresh();
            SetText(feedbackText, result.IsSuccess
                ? $"PURCHASED {offer.DisplayName}. SAVE TO KEEP CHANGES; INSTALL IN GARAGE."
                : result.ErrorMessage);
            owner?.RefreshHome();
            if (purchaseButton != null && purchaseButton.interactable) purchaseButton.Select();
        }

        private void CancelPurchase()
        {
            foreach (Selectable control in modalBlocked)
                if (control != null) control.interactable = true;
            modalBlocked.Clear();
            pending = null;
            pendingTeam = null;
            if (confirmationRoot != null) confirmationRoot.SetActive(false);
        }

        private void DismissPurchase()
        {
            CancelPurchase();
            if (purchaseButton != null && purchaseButton.interactable) purchaseButton.Select();
        }

        private void SaveShop()
        {
            if (context?.Saves?.HasActiveCampaign != true)
            {
                SetText(feedbackText, "NO PERSISTENT CAMPAIGN TO SAVE.");
                return;
            }
            Result result = context.Saves.SaveCurrentCampaign();
            SetText(feedbackText, result.IsSuccess ? "CAMPAIGN SAVED." : "SAVE FAILED: " + result.ErrorMessage);
        }

        private void OpenGarage() { owner?.ShowGarage(); }
        private void OnDisable() { CancelPurchase(); }

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

        private static void SetText(TMP_Text target, string value) { if (target != null) target.text = value; }
        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void OnDestroy()
        {
            if (purchaseButton != null) purchaseButton.onClick.RemoveListener(RequestPurchase);
            if (saveButton != null) saveButton.onClick.RemoveListener(SaveShop);
            if (garageButton != null) garageButton.onClick.RemoveListener(OpenGarage);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmPurchase);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(DismissPurchase);
        }
    }
}
