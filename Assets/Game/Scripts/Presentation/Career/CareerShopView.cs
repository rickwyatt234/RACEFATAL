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
    public class CareerShopView : MonoBehaviour
    {
        private enum ShopCategory
        {
            Bikes,
            Engines,
            Chassis,
            Equipment
        }

        [Header("Categories")]
        [SerializeField] private Button bikesButton;
        [SerializeField] private Button enginesButton;
        [SerializeField] private Button chassisButton;
        [SerializeField] private Button equipmentButton;

        [Header("Catalog")]
        [SerializeField] private RectTransform itemList;
        [SerializeField] private CareerShopItemView itemTemplate;

        [Header("Details")]
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private TMP_Text ownershipText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Actions")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private TMP_Text purchaseButtonText;

        private readonly List<CareerShopItemView> spawned =
            new List<CareerShopItemView>();

        private GameContext context;
        private ShopCategory category =
            ShopCategory.Engines;

        private string selectedDefinitionId;

        private GameSessionState Session =>
            context?.Sessions?.Current;

        private TeamState Team =>
            Session?.PlayerTeam;

        public void Initialize(
            GameContext gameContext)
        {
            context =
                gameContext;

            Bind(
                bikesButton,
                () => SelectCategory(
                    ShopCategory.Bikes));

            Bind(
                enginesButton,
                () => SelectCategory(
                    ShopCategory.Engines));

            Bind(
                chassisButton,
                () => SelectCategory(
                    ShopCategory.Chassis));

            Bind(
                equipmentButton,
                () => SelectCategory(
                    ShopCategory.Equipment));

            Bind(
                purchaseButton,
                PurchaseSelected);
        }

        public void Refresh()
        {
            ClearItems();

            if (context?.Database == null ||
                context.Shop == null ||
                Team == null)
            {
                SetText(
                    feedbackText,
                    "SHOP IS UNAVAILABLE.");

                SetText(
                    detailsText,
                    string.Empty);

                SetText(
                    ownershipText,
                    string.Empty);

                if (purchaseButton != null)
                    purchaseButton.interactable = false;

                return;
            }

            SetText(
                creditsText,
                $"CREDITS  {Team.Credits:N0}");

            EnsureSelection();
            RenderCatalog();
            RenderDetails();
            UpdatePurchaseButton();
        }

        private void SelectCategory(
            ShopCategory nextCategory)
        {
            category =
                nextCategory;

            selectedDefinitionId =
                null;

            Refresh();
        }

        private void SelectDefinition(
            string definitionId)
        {
            selectedDefinitionId =
                definitionId;

            Refresh();
        }

        private void EnsureSelection()
        {
            if (HasSelection(
                    selectedDefinitionId))
            {
                return;
            }

            selectedDefinitionId =
                FindFirstDefinitionId();
        }

        private bool HasSelection(
            string definitionId)
        {
            if (string.IsNullOrWhiteSpace(
                    definitionId))
            {
                return false;
            }

            GameDatabase database =
                context.Database;

            switch (category)
            {
                case ShopCategory.Bikes:
                    return database.GetBikeDefinition(
                        definitionId) != null;

                case ShopCategory.Engines:
                    return database.GetEngineDefinition(
                        definitionId) != null;

                case ShopCategory.Chassis:
                    return database.GetChassisDefinition(
                        definitionId) != null;

                case ShopCategory.Equipment:
                    return database.GetEquipmentDefinition(
                        definitionId) != null;

                default:
                    return false;
            }
        }

        private string FindFirstDefinitionId()
        {
            List<string> ids =
                GetSortedDefinitionIds();

            return ids.Count > 0
                ? ids[0]
                : null;
        }

        private List<string>
            GetSortedDefinitionIds()
        {
            var ids =
                new List<string>();

            GameDatabase database =
                context.Database;

            switch (category)
            {
                case ShopCategory.Bikes:
                    ids.AddRange(
                        database.BikeDefinitions.Keys);
                    break;

                case ShopCategory.Engines:
                    ids.AddRange(
                        database.EngineDefinitions.Keys);
                    break;

                case ShopCategory.Chassis:
                    ids.AddRange(
                        database.ChassisDefinitions.Keys);
                    break;

                case ShopCategory.Equipment:
                    ids.AddRange(
                        database.EquipmentDefinitions.Keys);
                    break;
            }

            ids.Sort(
                (left, right) =>
                    string.Compare(
                        GetDisplayName(left),
                        GetDisplayName(right),
                        StringComparison.OrdinalIgnoreCase));

            return ids;
        }

        private void RenderCatalog()
        {
            SetText(
                categoryText,
                category.ToString().ToUpperInvariant());

            foreach (string id
                     in GetSortedDefinitionIds())
            {
                string label =
                    BuildCatalogLabel(
                        id);

                AddItem(
                    label,
                    id,
                    id ==
                    selectedDefinitionId);
            }

            if (spawned.Count == 0)
            {
                SetText(
                    feedbackText,
                    "NO ITEMS ARE AUTHORED FOR THIS CATEGORY.");
            }
        }

        private string BuildCatalogLabel(
            string definitionId)
        {
            int cost =
                GetCreditCost(
                    definitionId);

            string requiredTechnology =
                GetRequiredTechnologyId(
                    definitionId);

            bool unlocked =
                string.IsNullOrWhiteSpace(
                    requiredTechnology) ||
                Team.HasTechnology(
                    requiredTechnology);

            int owned =
                GetOwnedCount(
                    definitionId);

            return
                $"{GetDisplayName(definitionId)}\n" +
                $"{cost:N0} CR  //  OWNED {owned}" +
                (unlocked
                    ? string.Empty
                    : "  //  LOCKED");
        }

        private void RenderDetails()
        {
            if (string.IsNullOrWhiteSpace(
                    selectedDefinitionId))
            {
                SetText(
                    detailsText,
                    "NO ITEM SELECTED.");

                SetText(
                    ownershipText,
                    string.Empty);

                return;
            }

            string requirement =
                GetRequiredTechnologyId(
                    selectedDefinitionId);

            string requirementText =
                string.IsNullOrWhiteSpace(
                    requirement)
                    ? "NONE"
                    : requirement;

            string details =
                BuildDetailText(
                    selectedDefinitionId);

            SetText(
                detailsText,
                details +
                "\n\nCOST  " +
                GetCreditCost(
                    selectedDefinitionId)
                    .ToString("N0") +
                " CR" +
                "\nREQUIRED TECHNOLOGY  " +
                requirementText);

            SetText(
                ownershipText,
                "OWNED INSTANCES  " +
                GetOwnedCount(
                    selectedDefinitionId)
                    .ToString("N0"));
        }

        private string BuildDetailText(
            string definitionId)
        {
            switch (category)
            {
                case ShopCategory.Bikes:
                {
                    BikeDefinition definition =
                        context.Database
                            .GetBikeDefinition(
                                definitionId);

                    if (definition == null)
                        return "BIKE DATA UNAVAILABLE.";

                    return
                        definition.DisplayName +
                        "\nSMALL NODES  " +
                        definition.SmallNodeCount +
                        "     MEDIUM NODES  " +
                        definition.MediumNodeCount +
                        "     LARGE NODES  " +
                        definition.LargeNodeCount +
                        "\nBASE MASS  " +
                        definition.BaseWeight.ToString("N0") +
                        "     HANDLING  " +
                        definition.BaseHandling.ToString("0.00") +
                        "     ENERGY  " +
                        definition.EnergyCapacity.ToString("N0");
                }

                case ShopCategory.Engines:
                {
                    EngineDefinition definition =
                        context.Database
                            .GetEngineDefinition(
                                definitionId);

                    if (definition == null)
                        return "ENGINE DATA UNAVAILABLE.";

                    return
                        definition.DisplayName +
                        "\nENGINE CLASS  " +
                        definition.EngineClass +
                        "     TOP SPEED  " +
                        definition.TopSpeed.ToString("0.0") +
                        "     ACCELERATION  " +
                        definition.Acceleration.ToString("0.0");
                }

                case ShopCategory.Chassis:
                {
                    ChassisDefinition definition =
                        context.Database
                            .GetChassisDefinition(
                                definitionId);

                    if (definition == null)
                        return "CHASSIS DATA UNAVAILABLE.";

                    return
                        definition.DisplayName +
                        "\nMASS MODIFIER  " +
                        definition.MassModifier.ToString("0.00") +
                        "     HANDLING MODIFIER  " +
                        definition.HandlingModifier.ToString("0.00");
                }

                case ShopCategory.Equipment:
                {
                    EquipmentDefinition definition =
                        context.Database
                            .GetEquipmentDefinition(
                                definitionId);

                    if (definition == null)
                        return "EQUIPMENT DATA UNAVAILABLE.";

                    return
                        definition.DisplayName +
                        "\nCATEGORY  " +
                        definition.Category +
                        "     NODE SIZE  " +
                        definition.RequiredNodeSize +
                        "     ACTIVATION  " +
                        definition.ActivationMode;
                }

                default:
                    return string.Empty;
            }
        }

        private void UpdatePurchaseButton()
        {
            bool canPurchase =
                CanPurchaseSelected(
                    out string reason);

            if (purchaseButton != null)
            {
                purchaseButton.interactable =
                    canPurchase;
            }

            if (purchaseButtonText != null)
            {
                purchaseButtonText.text =
                    canPurchase
                        ? "PURCHASE"
                        : "UNAVAILABLE";
            }

            if (!canPurchase &&
                !string.IsNullOrWhiteSpace(
                    selectedDefinitionId))
            {
                SetText(
                    feedbackText,
                    reason);
            }
            else if (string.IsNullOrWhiteSpace(
                         selectedDefinitionId))
            {
                SetText(
                    feedbackText,
                    string.Empty);
            }
            else
            {
                SetText(
                    feedbackText,
                    "READY TO PURCHASE.");
            }
        }

        private bool CanPurchaseSelected(
            out string reason)
        {
            reason =
                string.Empty;

            if (Team == null ||
                string.IsNullOrWhiteSpace(
                    selectedDefinitionId))
            {
                reason =
                    "SELECT AN ITEM.";

                return false;
            }

            string technologyId =
                GetRequiredTechnologyId(
                    selectedDefinitionId);

            if (!string.IsNullOrWhiteSpace(
                    technologyId) &&
                !Team.HasTechnology(
                    technologyId))
            {
                reason =
                    "RESEARCH REQUIRED  //  " +
                    technologyId;

                return false;
            }

            int cost =
                GetCreditCost(
                    selectedDefinitionId);

            if (Team.Credits < cost)
            {
                reason =
                    $"INSUFFICIENT CREDITS  //  NEED {cost:N0}";

                return false;
            }

            return true;
        }

        private void PurchaseSelected()
        {
            if (!CanPurchaseSelected(
                    out string reason))
            {
                SetText(
                    feedbackText,
                    reason);

                return;
            }

            Result result;

            switch (category)
            {
                case ShopCategory.Bikes:
                {
                    Result<BikeState> purchase =
                        context.Shop.PurchaseBike(
                            Team,
                            selectedDefinitionId);

                    result =
                        purchase.IsSuccess
                            ? Result.Success()
                            : Result.Failure(
                                purchase.ErrorMessage);
                    break;
                }

                case ShopCategory.Engines:
                {
                    Result<EngineState> purchase =
                        context.Shop.PurchaseEngine(
                            Team,
                            selectedDefinitionId);

                    result =
                        purchase.IsSuccess
                            ? Result.Success()
                            : Result.Failure(
                                purchase.ErrorMessage);
                    break;
                }

                case ShopCategory.Chassis:
                {
                    Result<ChassisState> purchase =
                        context.Shop.PurchaseChassis(
                            Team,
                            selectedDefinitionId);

                    result =
                        purchase.IsSuccess
                            ? Result.Success()
                            : Result.Failure(
                                purchase.ErrorMessage);
                    break;
                }

                case ShopCategory.Equipment:
                {
                    Result<EquipmentState> purchase =
                        context.Shop.PurchaseEquipment(
                            Team,
                            selectedDefinitionId);

                    result =
                        purchase.IsSuccess
                            ? Result.Success()
                            : Result.Failure(
                                purchase.ErrorMessage);
                    break;
                }

                default:
                    result =
                        Result.Failure(
                            "Unknown shop category.");
                    break;
            }

            if (!result.IsSuccess)
            {
                SetText(
                    feedbackText,
                    result.ErrorMessage);

                return;
            }

            Result saveResult =
                context.Saves.SaveCurrentCampaign();

            if (!saveResult.IsSuccess)
            {
                SetText(
                    feedbackText,
                    "PURCHASE COMPLETED, BUT SAVE FAILED: " +
                    saveResult.ErrorMessage);

                Refresh();
                return;
            }

            string purchasedName =
                GetDisplayName(
                    selectedDefinitionId);

            Refresh();

            SetText(
                feedbackText,
                "PURCHASED  //  " +
                purchasedName);
        }

        private string GetDisplayName(
            string definitionId)
        {
            switch (category)
            {
                case ShopCategory.Bikes:
                    return context.Database
                        .GetBikeDefinition(
                            definitionId)
                        ?.DisplayName
                        ?? definitionId;

                case ShopCategory.Engines:
                    return context.Database
                        .GetEngineDefinition(
                            definitionId)
                        ?.DisplayName
                        ?? definitionId;

                case ShopCategory.Chassis:
                    return context.Database
                        .GetChassisDefinition(
                            definitionId)
                        ?.DisplayName
                        ?? definitionId;

                case ShopCategory.Equipment:
                    return context.Database
                        .GetEquipmentDefinition(
                            definitionId)
                        ?.DisplayName
                        ?? definitionId;

                default:
                    return definitionId;
            }
        }

        private int GetCreditCost(
            string definitionId)
        {
            switch (category)
            {
                case ShopCategory.Bikes:
                    return context.Database
                        .GetBikeDefinition(
                            definitionId)
                        ?.CreditCost
                        ?? 0;

                case ShopCategory.Engines:
                    return context.Database
                        .GetEngineDefinition(
                            definitionId)
                        ?.CreditCost
                        ?? 0;

                case ShopCategory.Chassis:
                    return context.Database
                        .GetChassisDefinition(
                            definitionId)
                        ?.CreditCost
                        ?? 0;

                case ShopCategory.Equipment:
                    return context.Database
                        .GetEquipmentDefinition(
                            definitionId)
                        ?.CreditCost
                        ?? 0;

                default:
                    return 0;
            }
        }

        private string GetRequiredTechnologyId(
            string definitionId)
        {
            switch (category)
            {
                case ShopCategory.Bikes:
                    return context.Database
                        .GetBikeDefinition(
                            definitionId)
                        ?.RequiredTechnologyId;

                case ShopCategory.Engines:
                    return context.Database
                        .GetEngineDefinition(
                            definitionId)
                        ?.RequiredTechnologyId;

                case ShopCategory.Chassis:
                    return context.Database
                        .GetChassisDefinition(
                            definitionId)
                        ?.RequiredTechnologyId;

                case ShopCategory.Equipment:
                    return context.Database
                        .GetEquipmentDefinition(
                            definitionId)
                        ?.RequiredTechnologyId;

                default:
                    return null;
            }
        }

        private int GetOwnedCount(
            string definitionId)
        {
            GarageState garage =
                Team?.Garage;

            if (garage == null)
                return 0;

            int count =
                0;

            switch (category)
            {
                case ShopCategory.Bikes:
                    foreach (BikeState bike
                             in garage.Bikes)
                    {
                        if (bike.BikeDefinitionId ==
                            definitionId)
                        {
                            count++;
                        }
                    }
                    break;

                case ShopCategory.Engines:
                    foreach (EngineState engine
                             in garage.Engines)
                    {
                        if (engine.EngineDefinitionId ==
                            definitionId)
                        {
                            count++;
                        }
                    }
                    break;

                case ShopCategory.Chassis:
                    foreach (ChassisState chassis
                             in garage.Chassis)
                    {
                        if (chassis.ChassisDefinitionId ==
                            definitionId)
                        {
                            count++;
                        }
                    }
                    break;

                case ShopCategory.Equipment:
                    foreach (EquipmentState item
                             in garage.Equipment)
                    {
                        if (item.EquipmentDefinitionId ==
                            definitionId)
                        {
                            count++;
                        }
                    }
                    break;
            }

            return count;
        }

        private void AddItem(
            string label,
            string definitionId,
            bool selected)
        {
            if (itemTemplate == null ||
                itemList == null)
            {
                return;
            }

            CareerShopItemView item =
                Instantiate(
                    itemTemplate,
                    itemList);

            item.gameObject.SetActive(
                true);

            item.Bind(
                label,
                () => SelectDefinition(
                    definitionId),
                selected);

            spawned.Add(
                item);
        }

        private void ClearItems()
        {
            foreach (CareerShopItemView item
                     in spawned)
            {
                if (item != null)
                {
                    Destroy(
                        item.gameObject);
                }
            }

            spawned.Clear();
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

        private void SetText(
            TMP_Text target,
            string value)
        {
            if (target != null)
            {
                target.text =
                    value ?? string.Empty;
            }
        }
    }
}
