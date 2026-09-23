using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RaceFatal.Presentation.Career
{
    // Pure presentation. Catalog/pricing/ownership rules live in ShopService.
    public class CareerShopView : MonoBehaviour
    {
        [Header("Catalog")]
        [SerializeField] private RectTransform offersRoot;
        [SerializeField] private CareerGarageOptionView optionTemplate;

        [Header("Category Filters")]
        [SerializeField] private Button allButton;
        [SerializeField] private Button bikesButton;
        [SerializeField] private Button enginesButton;
        [SerializeField] private Button chassisButton;
        [SerializeField] private Button equipmentButton;

        [Header("Details")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDetailsText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Actions")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Button saveButton;

        private readonly List<CareerGarageOptionView> spawned =
            new List<CareerGarageOptionView>();

        private GameContext context;
        private ShopItemKind? filter;
        private ShopItemKind selectedKind;
        private string selectedDefinitionId;
        private ShopOffer selectedOffer;
        private bool purchaseInProgress;

        private TeamState Team => context?.Sessions?.Current?.PlayerTeam;

        public void Initialize(GameContext gameContext)
        {
            context = gameContext;

            Bind(allButton, ShowAll);
            Bind(bikesButton, ShowBikes);
            Bind(enginesButton, ShowEngines);
            Bind(chassisButton, ShowChassis);
            Bind(equipmentButton, ShowEquipment);
            Bind(purchaseButton, PurchaseSelected);
            Bind(saveButton, SaveShop);
        }

        public void Refresh()
        {
            ClearOptions();

            if (Team == null || context?.Shop == null)
            {
                selectedOffer = null;
                SetText(feedbackText, "NO ACTIVE CAMPAIGN.");
                SetText(balanceText, "CREDITS  --");
                SetText(itemNameText, "NO SELECTION");
                SetText(itemDetailsText, string.Empty);
                SetText(requirementText, string.Empty);
                UpdatePurchaseButton();
                return;
            }

            SetText(balanceText, "CREDITS  " + Team.Credits.ToString("N0"));

            IReadOnlyList<ShopOffer> offers = context.Shop.GetOffers();
            var visible = new List<ShopOffer>();

            foreach (ShopOffer offer in offers)
            {
                if (!filter.HasValue || offer.Kind == filter.Value)
                    visible.Add(offer);
            }

            selectedOffer = null;
            foreach (ShopOffer offer in visible)
            {
                if (offer.Kind == selectedKind &&
                    offer.DefinitionId == selectedDefinitionId)
                {
                    selectedOffer = offer;
                    break;
                }
            }

            if (selectedOffer == null && visible.Count > 0)
            {
                selectedOffer = visible[0];
                selectedKind = selectedOffer.Kind;
                selectedDefinitionId = selectedOffer.DefinitionId;
            }

            foreach (ShopOffer offer in visible)
            {
                ShopOffer captured = offer;
                bool selected = selectedOffer != null &&
                    selectedOffer.Kind == offer.Kind &&
                    selectedOffer.DefinitionId == offer.DefinitionId;

                AddOption(
                    offer.DisplayName + "\n" +
                    offer.Kind.ToString().ToUpperInvariant() + "  //  " +
                    offer.CreditCost.ToString("N0") + " CR  //  " +
                    GetOfferStatus(offer),
                    () => SelectOffer(captured),
                    selected);
            }

            RenderDetails();

            if (visible.Count == 0)
                SetText(feedbackText,
                    "NO PRICED ITEMS IN THIS CATEGORY. " +
                    "SET STABLE IDS AND CREDIT COSTS IN THE CONTENT DEFINITIONS.");
        }

        private void SelectOffer(ShopOffer offer)
        {
            if (offer == null)
                return;

            selectedKind = offer.Kind;
            selectedDefinitionId = offer.DefinitionId;
            Refresh();
        }

        private void RenderDetails()
        {
            ShopOffer offer = selectedOffer;
            if (offer == null)
            {
                SetText(itemNameText, "NO ITEMS AVAILABLE");
                SetText(itemDetailsText, string.Empty);
                SetText(requirementText, string.Empty);
                UpdatePurchaseButton();
                return;
            }

            SetText(itemNameText, offer.DisplayName);

            string details =
                offer.Kind.ToString().ToUpperInvariant() + "  //  " +
                offer.CreditCost.ToString("N0") + " CREDITS" +
                "\nOWNED  " + GetOwnedCount(offer) +
                "\n\n" + GetItemStatistics(offer);

            SetText(itemDetailsText, details);

            string requirement =
                !offer.IsUnlockedFor(Team)
                    ? "RESEARCH REQUIRED  //  " + offer.RequiredTechnologyId
                    : Team.Credits < offer.CreditCost
                        ? "INSUFFICIENT CREDITS"
                        : "AVAILABLE TO PURCHASE";

            SetText(requirementText, requirement);
            UpdatePurchaseButton();
        }

        private string GetItemStatistics(ShopOffer offer)
        {
            GameDatabase database = context?.Database;
            if (database == null)
                return "CONTENT DATA UNAVAILABLE";

            switch (offer.Kind)
            {
                case ShopItemKind.Bike:
                    BikeDefinition bike = database.GetBikeDefinition(
                        offer.DefinitionId);
                    return bike == null
                        ? "BIKE DEFINITION MISSING"
                        : "EQUIPMENT NODES  //  " +
                          "S " + bike.SmallNodeCount + "  " +
                          "M " + bike.MediumNodeCount + "  " +
                          "L " + bike.LargeNodeCount +
                          "\nBASE HANDLING  " + bike.BaseHandling.ToString("F2") +
                          "\nENERGY CAPACITY  " + bike.EnergyCapacity.ToString("F0") +
                          "\nSOLD AS BARE FRAME; ENGINE AND CHASSIS SOLD SEPARATELY.";

                case ShopItemKind.Engine:
                    EngineDefinition engine = database.GetEngineDefinition(
                        offer.DefinitionId);
                    return engine == null
                        ? "ENGINE DEFINITION MISSING"
                        : "ENGINE CLASS  " + engine.EngineClass +
                          "\nTOP SPEED  " + engine.TopSpeed.ToString("F0") +
                          "\nACCELERATION  " + engine.Acceleration.ToString("F2");

                case ShopItemKind.Chassis:
                    ChassisDefinition chassis = database.GetChassisDefinition(
                        offer.DefinitionId);
                    return chassis == null
                        ? "CHASSIS DEFINITION MISSING"
                        : "MASS MODIFIER  " + chassis.MassModifier.ToString("F2") +
                          "\nHANDLING MODIFIER  " + chassis.HandlingModifier.ToString("F2");

                case ShopItemKind.Equipment:
                    EquipmentDefinition item = database.GetEquipmentDefinition(
                        offer.DefinitionId);
                    return item == null
                        ? "EQUIPMENT DEFINITION MISSING"
                        : "CATEGORY  " + item.Category +
                          "\nREQUIRED NODE SIZE  " + item.RequiredNodeSize +
                          "\nACTIVATION  " + item.ActivationMode;

                default:
                    return string.Empty;
            }
        }

        private int GetOwnedCount(ShopOffer offer)
        {
            GarageState garage = Team?.Garage;
            if (garage == null)
                return 0;

            int count = 0;
            switch (offer.Kind)
            {
                case ShopItemKind.Bike:
                    foreach (BikeState b in garage.Bikes)
                        if (b.BikeDefinitionId == offer.DefinitionId)
                            count++;
                    break;

                case ShopItemKind.Engine:
                    foreach (EngineState e in garage.Engines)
                        if (e.EngineDefinitionId == offer.DefinitionId)
                            count++;
                    break;

                case ShopItemKind.Chassis:
                    foreach (ChassisState c in garage.Chassis)
                        if (c.ChassisDefinitionId == offer.DefinitionId)
                            count++;
                    break;

                case ShopItemKind.Equipment:
                    foreach (EquipmentState e in garage.Equipment)
                        if (e.EquipmentDefinitionId == offer.DefinitionId)
                            count++;
                    break;
            }

            return count;
        }

        private string GetOfferStatus(ShopOffer offer)
        {
            if (!offer.IsUnlockedFor(Team))
                return "LOCKED";

            if (Team.Credits < offer.CreditCost)
                return "INSUFFICIENT CREDITS";

            return "AVAILABLE";
        }

        private void PurchaseSelected()
        {
            if (purchaseInProgress || selectedOffer == null ||
                Team == null || context?.Shop == null ||
                context.Saves == null || !context.Saves.HasActiveCampaign)
                return;

            purchaseInProgress = true;
            UpdatePurchaseButton();

            Result<ShopPurchaseReceipt> result =
                context.Shop.Purchase(
                    Team, selectedOffer.Kind, selectedOffer.DefinitionId);

            if (!result.IsSuccess)
            {
                purchaseInProgress = false;
                Refresh();
                SetText(feedbackText, result.ErrorMessage);
                return;
            }

            // Store each transaction immediately; no separate inventory
            // representation exists outside the domain garage.
            Result save = context.Saves.SaveCurrentCampaign();
            purchaseInProgress = false;
            Refresh();

            SetText(
                feedbackText,
                save.IsSuccess
                    ? "PURCHASED " + result.Value.DisplayName.ToUpperInvariant() +
                      ". ITEM ADDED TO GARAGE."
                    : "PURCHASED, BUT SAVE FAILED: " + save.ErrorMessage +
                      "  //  RETRY WITH SAVE SHOP.");
        }

        private void SaveShop()
        {
            if (context?.Saves == null ||
                !context.Saves.HasActiveCampaign)
            {
                SetText(feedbackText, "NO PERSISTENT CAMPAIGN TO SAVE.");
                return;
            }

            Result result = context.Saves.SaveCurrentCampaign();
            SetText(feedbackText,
                result.IsSuccess
                    ? "CAMPAIGN SAVED."
                    : "SAVE FAILED: " + result.ErrorMessage);
        }

        private void AddOption(
            string label,
            Action onClick,
            bool selected)
        {
            if (offersRoot == null || optionTemplate == null)
                return;

            CareerGarageOptionView view =
                Instantiate(optionTemplate, offersRoot);

            view.gameObject.SetActive(true);
            view.Bind(label, onClick, true, selected);
            spawned.Add(view);
        }

        private void ClearOptions()
        {
            foreach (CareerGarageOptionView view in spawned)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }

            spawned.Clear();
        }

        private void UpdatePurchaseButton()
        {
            if (purchaseButton != null)
            {
                purchaseButton.interactable =
                    !purchaseInProgress &&
                    selectedOffer != null &&
                    selectedOffer.IsUnlockedFor(Team) &&
                    Team.Credits >= selectedOffer.CreditCost &&
                    context?.Saves?.HasActiveCampaign == true;
            }
        }

        public void ShowAll() => SetFilter(null);
        public void ShowBikes() => SetFilter(ShopItemKind.Bike);
        public void ShowEngines() => SetFilter(ShopItemKind.Engine);
        public void ShowChassis() => SetFilter(ShopItemKind.Chassis);
        public void ShowEquipment() => SetFilter(ShopItemKind.Equipment);

        private void SetFilter(ShopItemKind? kind)
        {
            filter = kind;
            selectedDefinitionId = null;
            SetText(feedbackText, string.Empty);
            Refresh();
        }

        private void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void SetText(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }
}
