using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class GarageState
    {
        private readonly List<BikeState> bikes =
            new List<BikeState>();

        private readonly List<EngineState> engines =
            new List<EngineState>();

        private readonly List<ChassisState> chassis =
            new List<ChassisState>();

        private readonly List<EquipmentState> equipment =
            new List<EquipmentState>();

        public IReadOnlyList<BikeState> Bikes => bikes;
        public IReadOnlyList<EngineState> Engines => engines;
        public IReadOnlyList<ChassisState> Chassis => chassis;
        public IReadOnlyList<EquipmentState> Equipment => equipment;

        #region Add Ownership

        public Result<BikeState> AddBike(
            BikeState bike)
        {
            if (bike == null)
            {
                return Result<BikeState>.Failure(
                    "Bike is required.");
            }

            if (bikes.Exists(
                    candidate =>
                        candidate.BikeId ==
                        bike.BikeId))
            {
                return Result<BikeState>.Failure(
                    $"Bike with ID '{bike.BikeId}' " +
                    "already exists in the garage.");
            }

            bikes.Add(
                bike);

            return Result<BikeState>.Success(
                bike);
        }

        public Result<BikeState> AddBike(
            string bikeId,
            BikeDefinition definition,
            string primaryColor,
            string secondaryColor)
        {
            if (definition == null)
            {
                return Result<BikeState>.Failure(
                    "Bike definition is required.");
            }

            BikeState bike =
                definition.CreateBikeState(
                    bikeId,
                    primaryColor,
                    secondaryColor);

            return AddBike(
                bike);
        }

        public Result<EngineState> AddEngine(
            EngineState engine)
        {
            if (engine == null)
            {
                return Result<EngineState>.Failure(
                    "Engine is required.");
            }

            if (engines.Exists(
                    candidate =>
                        candidate.EngineId ==
                        engine.EngineId))
            {
                return Result<EngineState>.Failure(
                    $"Engine with ID '{engine.EngineId}' " +
                    "already exists in the garage.");
            }

            engines.Add(
                engine);

            return Result<EngineState>.Success(
                engine);
        }

        public Result<ChassisState> AddChassis(
            ChassisState chassisState)
        {
            if (chassisState == null)
            {
                return Result<ChassisState>.Failure(
                    "Chassis is required.");
            }

            if (chassis.Exists(
                    candidate =>
                        candidate.ChassisId ==
                        chassisState.ChassisId))
            {
                return Result<ChassisState>.Failure(
                    $"Chassis with ID '{chassisState.ChassisId}' " +
                    "already exists in the garage.");
            }

            chassis.Add(
                chassisState);

            return Result<ChassisState>.Success(
                chassisState);
        }

        public Result<EquipmentState> AddEquipment(
            EquipmentState equipmentState)
        {
            if (equipmentState == null)
            {
                return Result<EquipmentState>.Failure(
                    "Equipment is required.");
            }

            if (equipment.Exists(
                    candidate =>
                        candidate.EquipmentId ==
                        equipmentState.EquipmentId))
            {
                return Result<EquipmentState>.Failure(
                    $"Equipment with ID " +
                    $"'{equipmentState.EquipmentId}' " +
                    "already exists in the garage.");
            }

            equipment.Add(
                equipmentState);

            return Result<EquipmentState>.Success(
                equipmentState);
        }

        #endregion

        #region Engine Installation

        public Result InstallEngine(
            string bikeId,
            string engineId)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            EngineState engine =
                engines.Find(
                    candidate =>
                        candidate.EngineId ==
                        engineId);

            if (engine == null)
            {
                return Result.Failure(
                    $"Engine with ID '{engineId}' not found.");
            }

            BikeState installedBike =
                FindBikeUsingEngine(
                    engineId);

            if (installedBike != null)
            {
                return Result.Failure(
                    $"Engine '{engineId}' is already installed " +
                    $"on bike '{installedBike.BikeId}'.");
            }

            return bike.Loadout.InstallEngine(
                engine);
        }

        public Result<EngineState> RemoveEngine(
            string bikeId)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result<EngineState>.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            return bike.Loadout.RemoveEngine();
        }

        #endregion

        #region Chassis Installation

        public Result InstallChassis(
            string bikeId,
            string chassisId)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            ChassisState chassisState =
                chassis.Find(
                    candidate =>
                        candidate.ChassisId ==
                        chassisId);

            if (chassisState == null)
            {
                return Result.Failure(
                    $"Chassis with ID '{chassisId}' not found.");
            }

            BikeState installedBike =
                FindBikeUsingChassis(
                    chassisId);

            if (installedBike != null)
            {
                return Result.Failure(
                    $"Chassis '{chassisId}' is already installed " +
                    $"on bike '{installedBike.BikeId}'.");
            }

            return bike.Loadout.InstallChassis(
                chassisState);
        }

        public Result<ChassisState> RemoveChassis(
            string bikeId)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result<ChassisState>.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            return bike.Loadout.RemoveChassis();
        }

        #endregion

        #region Equipment Installation

        public Result<EquipmentState> InstallEquipment(
            string bikeId,
            string equipmentId,
            NodeSize nodeSize,
            int index)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result<EquipmentState>.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            EquipmentState equipmentState =
                equipment.Find(
                    candidate =>
                        candidate.EquipmentId ==
                        equipmentId);

            if (equipmentState == null)
            {
                return Result<EquipmentState>.Failure(
                    $"Equipment with ID '{equipmentId}' not found.");
            }

            BikeState installedBike =
                FindBikeUsingEquipment(
                    equipmentId);

            if (installedBike != null)
            {
                return Result<EquipmentState>.Failure(
                    $"Equipment '{equipmentId}' is already installed " +
                    $"on bike '{installedBike.BikeId}'.");
            }

            return bike.Loadout.InstallEquipment(
                equipmentState,
                nodeSize,
                index);
        }

        public Result<EquipmentState> RemoveEquipment(
            string bikeId,
            NodeSize nodeSize,
            int index)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result<EquipmentState>.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            return bike.Loadout.RemoveEquipment(
                nodeSize,
                index);
        }

        #endregion

        #region Queries

        public BikeState FindBike(
            string bikeId)
        {
            return bikes.Find(
                bike =>
                    bike.BikeId ==
                    bikeId);
        }

        public BikeState FindBikeUsingEquipment(
            string equipmentId)
        {
            if (string.IsNullOrWhiteSpace(
                    equipmentId))
            {
                return null;
            }

            foreach (BikeState bike in bikes)
            {
                if (bike.Loadout.HasEquipment(
                        equipmentId))
                {
                    return bike;
                }
            }

            return null;
        }

        public BikeState FindBikeUsingEngine(
            string engineId)
        {
            if (string.IsNullOrWhiteSpace(
                    engineId))
            {
                return null;
            }

            foreach (BikeState bike in bikes)
            {
                if (bike.Loadout.Engine != null &&
                    bike.Loadout.Engine.EngineId ==
                    engineId)
                {
                    return bike;
                }
            }

            return null;
        }

        public BikeState FindBikeUsingChassis(
            string chassisId)
        {
            if (string.IsNullOrWhiteSpace(
                    chassisId))
            {
                return null;
            }

            foreach (BikeState bike in bikes)
            {
                if (bike.Loadout.Chassis != null &&
                    bike.Loadout.Chassis.ChassisId ==
                    chassisId)
                {
                    return bike;
                }
            }

            return null;
        }

        public bool IsEquipmentInstalled(
            string equipmentId)
        {
            return FindBikeUsingEquipment(
                equipmentId) != null;
        }

        public bool IsEngineInstalled(
            string engineId)
        {
            return FindBikeUsingEngine(
                engineId) != null;
        }

        public bool IsChassisInstalled(
            string chassisId)
        {
            return FindBikeUsingChassis(
                chassisId) != null;
        }

        public bool HasRaceReadyBikeFor(
            EngineClass engineClass)
        {
            foreach (BikeState bike in bikes)
            {
                if (!bike.IsDestroyed &&
                    bike.EngineClass ==
                        engineClass &&
                    bike.IsRaceReady)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<BikeState>
            GetRaceReadyBikesFor(
                EngineClass engineClass)
        {
            List<BikeState> result =
                new List<BikeState>();

            foreach (BikeState bike in bikes)
            {
                if (bike.IsDestroyed)
                    continue;

                if (!bike.IsRaceReady)
                    continue;

                if (!bike.EngineClass.HasValue)
                    continue;

                if (bike.EngineClass.Value !=
                    engineClass)
                {
                    continue;
                }

                result.Add(
                    bike);
            }

            return result;
        }

        #endregion

        public Result<BikeState> DestroyBike(
            string bikeId)
        {
            BikeState bike =
                FindBike(
                    bikeId);

            if (bike == null)
            {
                return Result<BikeState>.Failure(
                    $"Bike with ID '{bikeId}' not found.");
            }

            bike.Destroy();

            return Result<BikeState>.Success(
                bike);
        }
    }
}