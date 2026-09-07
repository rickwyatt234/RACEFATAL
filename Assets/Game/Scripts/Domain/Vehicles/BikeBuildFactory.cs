using System;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class BikeBuildFactory
    {
        private readonly GameDatabase database;
        private readonly VehicleFactory vehicleFactory;
        private readonly EquipmentFactory equipmentFactory;

        public BikeBuildFactory(
            GameDatabase database,
            VehicleFactory vehicleFactory,
            EquipmentFactory equipmentFactory)
        {
            this.database =
                database ??
                throw new ArgumentNullException(
                    nameof(database));

            this.vehicleFactory =
                vehicleFactory ??
                throw new ArgumentNullException(
                    nameof(vehicleFactory));

            this.equipmentFactory =
                equipmentFactory ??
                throw new ArgumentNullException(
                    nameof(equipmentFactory));
        }

        public Result<BikeState> CreateInGarage(
            BikeBuildDefinition build,
            GarageState garage,
            string primaryColor,
            string secondaryColor)
        {
            if (build == null)
            {
                return Result<BikeState>.Failure(
                    "Bike build is required.");
            }

            if (garage == null)
            {
                return Result<BikeState>.Failure(
                    "Garage is required.");
            }

            BikeDefinition bikeDefinition =
                database.GetBikeDefinition(
                    build.BikeDefinitionId);

            if (bikeDefinition == null)
            {
                return Result<BikeState>.Failure(
                    $"Bike definition " +
                    $"'{build.BikeDefinitionId}' " +
                    $"was not found for build " +
                    $"'{build.Id}'.");
            }

            EngineDefinition engineDefinition =
                database.GetEngineDefinition(
                    build.EngineDefinitionId);

            if (engineDefinition == null)
            {
                return Result<BikeState>.Failure(
                    $"Engine definition " +
                    $"'{build.EngineDefinitionId}' " +
                    $"was not found for build " +
                    $"'{build.Id}'.");
            }

            ChassisDefinition chassisDefinition =
                database.GetChassisDefinition(
                    build.ChassisDefinitionId);

            if (chassisDefinition == null)
            {
                return Result<BikeState>.Failure(
                    $"Chassis definition " +
                    $"'{build.ChassisDefinitionId}' " +
                    $"was not found for build " +
                    $"'{build.Id}'.");
            }

            // -------------------------------------------------
            // BIKE
            // -------------------------------------------------

            BikeState bike =
                vehicleFactory.CreateBike(
                    bikeDefinition,
                    primaryColor,
                    secondaryColor);

            Result<BikeState> addBike =
                garage.AddBike(
                    bike);

            if (!addBike.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    addBike.ErrorMessage);
            }

            // -------------------------------------------------
            // ENGINE
            // -------------------------------------------------

            EngineState engine =
                vehicleFactory.CreateEngine(
                    engineDefinition);

            Result<EngineState> addEngine =
                garage.AddEngine(
                    engine);

            if (!addEngine.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    addEngine.ErrorMessage);
            }

            Result installEngine =
                garage.InstallEngine(
                    bike.BikeId,
                    engine.EngineId);

            if (!installEngine.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    installEngine.ErrorMessage);
            }

            // -------------------------------------------------
            // CHASSIS
            // -------------------------------------------------

            ChassisState chassis =
                vehicleFactory.CreateChassis(
                    chassisDefinition);

            Result<ChassisState> addChassis =
                garage.AddChassis(
                    chassis);

            if (!addChassis.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    addChassis.ErrorMessage);
            }

            Result installChassis =
                garage.InstallChassis(
                    bike.BikeId,
                    chassis.ChassisId);

            if (!installChassis.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    installChassis.ErrorMessage);
            }

            // -------------------------------------------------
            // EQUIPMENT
            // -------------------------------------------------

            foreach (EquipmentMountDefinition mount
                     in build.Equipment)
            {
                EquipmentDefinition definition =
                    database.GetEquipmentDefinition(
                        mount.EquipmentDefinitionId);

                if (definition == null)
                {
                    return Result<BikeState>.Failure(
                        $"Equipment definition " +
                        $"'{mount.EquipmentDefinitionId}' " +
                        $"was not found for build " +
                        $"'{build.Id}'.");
                }

                EquipmentState equipment =
                    equipmentFactory.Create(
                        definition);

                Result<EquipmentState> addEquipment =
                    garage.AddEquipment(
                        equipment);

                if (!addEquipment.IsSuccess)
                {
                    return Result<BikeState>.Failure(
                        addEquipment.ErrorMessage);
                }

                Result<EquipmentState> installEquipment =
                    garage.InstallEquipment(
                        bike.BikeId,
                        equipment.EquipmentId,
                        mount.NodeSize,
                        mount.NodeIndex);

                if (!installEquipment.IsSuccess)
                {
                    return Result<BikeState>.Failure(
                        $"Could not install " +
                        $"'{mount.EquipmentDefinitionId}' " +
                        $"on build '{build.Id}': " +
                        installEquipment.ErrorMessage);
                }
            }

            return Result<BikeState>.Success(
                bike);
        }
    }
}