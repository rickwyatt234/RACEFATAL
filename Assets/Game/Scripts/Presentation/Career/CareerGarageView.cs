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
    public class CareerGarageView : MonoBehaviour
    {
        [Header("Lists")]
        [SerializeField] private RectTransform bikeList;
        [SerializeField] private RectTransform slotList;
        [SerializeField] private RectTransform inventoryList;
        [SerializeField] private CareerGarageOptionView optionTemplate;

        [Header("Details")]
        [SerializeField] private TMP_Text bikeDetailsText;
        [SerializeField] private TMP_Text slotDetailsText;
        [SerializeField] private TMP_Text assignmentText;
        [SerializeField] private TMP_Text inventoryHeadingText;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Actions")]
        [SerializeField] private Button assignPlayerButton;
        [SerializeField] private Button assignPartnerButton;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button saveButton;

        private readonly List<CareerGarageOptionView> spawned =
            new List<CareerGarageOptionView>();

        private enum SlotKind
        {
            Engine,
            Chassis,
            Equipment
        }

        private GameContext context;
        private string selectedBikeId;
        private SlotKind selectedSlotKind = SlotKind.Engine;
        private NodeSize selectedNodeSize;
        private int selectedNodeIndex;

        private GameSessionState Session => context?.Sessions?.Current;
        private GarageState Garage => Session?.PlayerTeam?.Garage;

        private BikeState SelectedBike =>
            Garage?.FindBike(selectedBikeId);

        public void Initialize(GameContext gameContext)
        {
            context = gameContext;

            Bind(assignPlayerButton, AssignPlayer);
            Bind(assignPartnerButton, AssignPartner);
            Bind(removeButton, RemoveSelected);
            Bind(saveButton, SaveGarage);
        }

        public void Refresh()
        {
            ClearOptions();

            if (Garage == null || context?.Database == null)
            {
                SetText(feedbackText, "NO LOADED GARAGE.");
                SetText(bikeDetailsText, string.Empty);
                SetText(slotDetailsText, string.Empty);
                SetText(assignmentText, string.Empty);
                return;
            }

            if (SelectedBike == null && Garage.Bikes.Count > 0)
            {
                selectedBikeId = Garage.Bikes[0].BikeId;
                selectedSlotKind = SlotKind.Engine;
            }

            RenderBikes();
            RenderSlots();
            RenderInventory();
            RenderDetails();
            UpdateActions();
        }

        private void SelectBike(string bikeId)
        {
            selectedBikeId = bikeId;
            selectedSlotKind = SlotKind.Engine;
            Refresh();
        }

        private void SelectSlot(
            SlotKind kind,
            NodeSize nodeSize = default,
            int nodeIndex = 0)
        {
            selectedSlotKind = kind;
            selectedNodeSize = nodeSize;
            selectedNodeIndex = nodeIndex;
            Refresh();
        }

        private void RenderBikes()
        {
            if (Garage == null)
                return;

            foreach (BikeState bike in Garage.Bikes)
            {
                BikeDefinition definition =
                    context.Database.GetBikeDefinition(bike.BikeDefinitionId);

                string name = definition?.DisplayName ?? bike.BikeDefinitionId;
                string assignment =
                    bike.BikeId == Session.SelectedPlayerBikeId
                        ? "  [PLAYER]"
                        : bike.BikeId == Session.SelectedPartnerBikeId
                            ? "  [PARTNER]"
                            : string.Empty;

                string condition = bike.IsDestroyed
                    ? "DESTROYED"
                    : bike.IsRaceReady ? "RACE READY" : "INCOMPLETE";

                string bikeId = bike.BikeId;
                AddOption(
                    bikeList,
                    name + assignment + "\n" + condition +
                    "  //  " + (bike.EngineClass?.ToString() ?? "NO ENGINE"),
                    () => SelectBike(bikeId),
                    true,
                    bikeId == selectedBikeId);
            }
        }

        private void RenderSlots()
        {
            BikeState bike = SelectedBike;
            if (bike == null)
                return;

            AddOption(
                slotList,
                "ENGINE  //  " + EngineName(bike.Loadout.Engine),
                () => SelectSlot(SlotKind.Engine),
                true,
                selectedSlotKind == SlotKind.Engine);

            AddOption(
                slotList,
                "CHASSIS  //  " + ChassisName(bike.Loadout.Chassis),
                () => SelectSlot(SlotKind.Chassis),
                true,
                selectedSlotKind == SlotKind.Chassis);

            foreach (BikeNode node in bike.Loadout.Nodes)
            {
                NodeSize size = node.NodeSize;
                int index = node.Index;
                bool selected =
                    selectedSlotKind == SlotKind.Equipment &&
                    selectedNodeSize == size &&
                    selectedNodeIndex == index;

                AddOption(
                    slotList,
                    size.ToString().ToUpperInvariant() + " NODE " +
                    (index + 1).ToString("00") + "  //  " +
                    EquipmentName(node.InstalledEquipment),
                    () => SelectSlot(SlotKind.Equipment, size, index),
                    true,
                    selected);
            }
        }

        private void RenderInventory()
        {
            BikeState bike = SelectedBike;
            if (bike == null)
                return;

            bool canEdit = !bike.IsDestroyed;

            switch (selectedSlotKind)
            {
                case SlotKind.Engine:
                    SetText(inventoryHeadingText, "OWNED ENGINES");
                    foreach (EngineState engine in Garage.Engines)
                    {
                        string installedOn =
                            Garage.FindBikeUsingEngine(engine.EngineId)?.BikeId;
                        string reason = engine.IsDestroyed
                            ? "  [DESTROYED]"
                            : installedOn != null
                                ? "  [INSTALLED]"
                                : string.Empty;

                        string engineId = engine.EngineId;
                        bool available = canEdit &&
                            bike.Loadout.Engine == null &&
                            !engine.IsDestroyed &&
                            installedOn == null;

                        AddOption(
                            inventoryList,
                            EngineName(engine) + reason,
                            () => InstallEngine(engineId),
                            available,
                            false);
                    }
                    break;

                case SlotKind.Chassis:
                    SetText(inventoryHeadingText, "OWNED CHASSIS");
                    foreach (ChassisState chassis in Garage.Chassis)
                    {
                        string installedOn =
                            Garage.FindBikeUsingChassis(chassis.ChassisId)?.BikeId;
                        string reason = chassis.IsDestroyed
                            ? "  [DESTROYED]"
                            : installedOn != null
                                ? "  [INSTALLED]"
                                : string.Empty;

                        string chassisId = chassis.ChassisId;
                        bool available = canEdit &&
                            bike.Loadout.Chassis == null &&
                            !chassis.IsDestroyed &&
                            installedOn == null;

                        AddOption(
                            inventoryList,
                            ChassisName(chassis) + reason,
                            () => InstallChassis(chassisId),
                            available,
                            false);
                    }
                    break;

                case SlotKind.Equipment:
                    SetText(
                        inventoryHeadingText,
                        "OWNED EQUIPMENT  //  " +
                        selectedNodeSize.ToString().ToUpperInvariant() +
                        " NODE " + (selectedNodeIndex + 1));

                    BikeNode node = bike.Loadout.FindNode(
                        selectedNodeSize, selectedNodeIndex);

                    foreach (EquipmentState equipment in Garage.Equipment)
                    {
                        BikeState installed =
                            Garage.FindBikeUsingEquipment(equipment.EquipmentId);

                        bool fits = NodeSizeRules.CanFit(
                            equipment.RequiredNodeSize, selectedNodeSize);

                        bool shieldConflict =
                            equipment.Category == EquipmentCategory.Shield &&
                            bike.Loadout.ContainsCategory(EquipmentCategory.Shield);

                        string reason = equipment.IsDestroyed
                            ? "  [DESTROYED]"
                            : installed != null
                                ? "  [INSTALLED]"
                                : !fits
                                    ? "  [NEEDS " + equipment.RequiredNodeSize + "]"
                                    : shieldConflict
                                        ? "  [SHIELD ALREADY INSTALLED]"
                                        : string.Empty;

                        bool available = canEdit &&
                            node != null && !node.IsOccupied &&
                            !equipment.IsDestroyed && installed == null &&
                            fits && !shieldConflict;

                        string equipmentId = equipment.EquipmentId;
                        AddOption(
                            inventoryList,
                            EquipmentName(equipment) + reason,
                            () => InstallEquipment(equipmentId),
                            available,
                            false);
                    }
                    break;
            }
        }

        private void RenderDetails()
        {
            BikeState bike = SelectedBike;
            if (bike == null)
            {
                SetText(bikeDetailsText, "NO OWNED BIKES.");
                SetText(slotDetailsText, string.Empty);
                SetText(assignmentText, string.Empty);
                return;
            }

            BikeDefinition definition =
                context.Database.GetBikeDefinition(bike.BikeDefinitionId);

            string status = bike.IsDestroyed
                ? "DESTROYED"
                : bike.IsRaceReady ? "RACE READY" : "INCOMPLETE";

            SetText(
                bikeDetailsText,
                (definition?.DisplayName ?? bike.BikeDefinitionId) +
                "   //   " + status + "\n" +
                "ENGINE CLASS  " + (bike.EngineClass?.ToString() ?? "NONE") +
                "     ENGINE  " + EngineName(bike.Loadout.Engine) +
                "     CHASSIS  " + ChassisName(bike.Loadout.Chassis));

            SetText(
                assignmentText,
                "PLAYER  " + AssignedName(Session.SelectedPlayerBikeId) +
                "\nPARTNER  " + AssignedName(Session.SelectedPartnerBikeId));

            string slotName =
                selectedSlotKind == SlotKind.Engine ? "ENGINE"
                : selectedSlotKind == SlotKind.Chassis ? "CHASSIS"
                : selectedNodeSize + " NODE " + (selectedNodeIndex + 1);

            SetText(slotDetailsText, "SELECTED SLOT  " + slotName);
        }

        private string AssignedName(string bikeId)
        {
            BikeState bike = Garage.FindBike(bikeId);
            if (bike == null)
                return "NONE";

            return context.Database
                .GetBikeDefinition(bike.BikeDefinitionId)?.DisplayName ??
                bike.BikeDefinitionId;
        }

        private string EngineName(EngineState engine)
        {
            if (engine == null)
                return "EMPTY";

            return context.Database
                .GetEngineDefinition(engine.EngineDefinitionId)?.DisplayName ??
                engine.EngineDefinitionId;
        }

        private string ChassisName(ChassisState chassis)
        {
            if (chassis == null)
                return "EMPTY";

            return context.Database
                .GetChassisDefinition(chassis.ChassisDefinitionId)?.DisplayName ??
                chassis.ChassisDefinitionId;
        }

        private string EquipmentName(EquipmentState equipment)
        {
            if (equipment == null)
                return "EMPTY";

            string name = context.Database
                .GetEquipmentDefinition(equipment.EquipmentDefinitionId)?.DisplayName ??
                equipment.EquipmentDefinitionId;

            return name + "  [" + equipment.Category + "]";
        }

        private void InstallEngine(string engineId)
        {
            BikeState bike = SelectedBike;
            if (bike == null || bike.IsDestroyed)
                return;

            ShowMutation(Garage.InstallEngine(bike.BikeId, engineId));
        }

        private void InstallChassis(string chassisId)
        {
            BikeState bike = SelectedBike;
            if (bike == null || bike.IsDestroyed)
                return;

            ShowMutation(Garage.InstallChassis(bike.BikeId, chassisId));
        }

        private void InstallEquipment(string equipmentId)
        {
            BikeState bike = SelectedBike;
            if (bike == null || bike.IsDestroyed)
                return;

            Result<EquipmentState> result = Garage.InstallEquipment(
                bike.BikeId, equipmentId, selectedNodeSize, selectedNodeIndex);

            ShowMutation(result.IsSuccess, result.ErrorMessage);
        }

        private void RemoveSelected()
        {
            BikeState bike = SelectedBike;
            if (bike == null || bike.IsDestroyed)
                return;

            switch (selectedSlotKind)
            {
                case SlotKind.Engine:
                    Result<EngineState> engine =
                        Garage.RemoveEngine(bike.BikeId);
                    ShowMutation(engine.IsSuccess, engine.ErrorMessage);
                    break;

                case SlotKind.Chassis:
                    Result<ChassisState> chassis =
                        Garage.RemoveChassis(bike.BikeId);
                    ShowMutation(chassis.IsSuccess, chassis.ErrorMessage);
                    break;

                case SlotKind.Equipment:
                    Result<EquipmentState> equipment =
                        Garage.RemoveEquipment(
                            bike.BikeId, selectedNodeSize, selectedNodeIndex);
                    ShowMutation(equipment.IsSuccess, equipment.ErrorMessage);
                    break;
            }
        }

        private void AssignPlayer()
        {
            if (SelectedBike == null || Session == null)
                return;

            Result result = Session.SelectPlayerBike(selectedBikeId);
            ShowMutation(result);
        }

        private void AssignPartner()
        {
            if (SelectedBike == null || Session == null)
                return;

            Result result = Session.SelectPartnerBike(selectedBikeId);
            ShowMutation(result);
        }

        private void SaveGarage()
        {
            if (context?.Saves == null ||
                !context.Saves.HasActiveCampaign)
            {
                SetText(feedbackText, "NO PERSISTENT CAMPAIGN TO SAVE.");
                return;
            }

            Result result = context.Saves.SaveCurrentCampaign();
            SetText(
                feedbackText,
                result.IsSuccess
                    ? "GARAGE SAVED."
                    : "SAVE FAILED: " + result.ErrorMessage);
        }

        private void ShowMutation(Result result)
        {
            ShowMutation(result.IsSuccess, result.ErrorMessage);
        }

        private void ShowMutation(bool success, string error)
        {
            if (success)
                Refresh();

            SetText(
                feedbackText,
                success
                    ? "GARAGE UPDATED. SAVE TO KEEP CHANGES."
                    : (error ?? "GARAGE UPDATE FAILED."));
        }

        private void UpdateActions()
        {
            BikeState bike = SelectedBike;
            bool editable = bike != null && !bike.IsDestroyed;

            if (assignPlayerButton != null)
                assignPlayerButton.interactable = editable &&
                    bike.BikeId != Session.SelectedPlayerBikeId;

            if (assignPartnerButton != null)
                assignPartnerButton.interactable = editable &&
                    bike.BikeId != Session.SelectedPartnerBikeId;

            bool occupied = false;
            if (bike != null)
            {
                occupied = selectedSlotKind == SlotKind.Engine
                    ? bike.Loadout.Engine != null
                    : selectedSlotKind == SlotKind.Chassis
                        ? bike.Loadout.Chassis != null
                        : bike.Loadout.FindNode(
                            selectedNodeSize, selectedNodeIndex)?.IsOccupied ?? false;
            }

            if (removeButton != null)
                removeButton.interactable = editable && occupied;

            if (saveButton != null)
                saveButton.interactable =
                    context?.Saves?.HasActiveCampaign == true;
        }

        private void AddOption(
            RectTransform container,
            string label,
            Action clicked,
            bool interactable,
            bool selected)
        {
            if (container == null || optionTemplate == null)
                return;

            CareerGarageOptionView option =
                Instantiate(optionTemplate, container);

            option.gameObject.SetActive(true);
            option.Bind(label, clicked, interactable, selected);
            spawned.Add(option);
        }

        private void ClearOptions()
        {
            foreach (CareerGarageOptionView option in spawned)
            {
                if (option != null)
                    Destroy(option.gameObject);
            }
            spawned.Clear();
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private void OnDestroy()
        {
            if (assignPlayerButton != null)
                assignPlayerButton.onClick.RemoveListener(AssignPlayer);
            if (assignPartnerButton != null)
                assignPartnerButton.onClick.RemoveListener(AssignPartner);
            if (removeButton != null)
                removeButton.onClick.RemoveListener(RemoveSelected);
            if (saveButton != null)
                saveButton.onClick.RemoveListener(SaveGarage);
        }
    }
}