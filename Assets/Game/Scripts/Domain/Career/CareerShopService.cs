using System;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public class CareerShopService
    {
        private readonly GameDatabase database;
        private readonly VehicleFactory vehicles;
        private readonly EquipmentFactory equipment;

        public CareerShopService(
            GameDatabase database,
            VehicleFactory vehicles,
            EquipmentFactory equipment)
        {
            this.database =
                database
                ?? throw new ArgumentNullException(
                    nameof(database));

            this.vehicles =
                vehicles
                ?? throw new ArgumentNullException(
                    nameof(vehicles));

            this.equipment =
                equipment
                ?? throw new ArgumentNullException(
                    nameof(equipment));
        }

        public Result<BikeState> PurchaseBike(
            TeamState team,
            string definitionId)
        {
            if (team == null)
            {
                return Result<BikeState>.Failure(
                    "Team is required.");
            }

            BikeDefinition definition =
                database.GetBikeDefinition(
                    definitionId);

            if (definition == null)
            {
                return Result<BikeState>.Failure(
                    $"Bike definition '{definitionId}' was not found.");
            }

            Result eligibility =
                ValidatePurchase(
                    team,
                    definition.CreditCost,
                    definition.RequiredTechnologyId);

            if (!eligibility.IsSuccess)
            {
                return Result<BikeState>.Failure(
                    eligibility.ErrorMessage);
            }

            if (!team.TrySpendCredits(
                    definition.CreditCost))
            {
                return Result<BikeState>.Failure(
                    "Not enough credits.");
            }

            BikeState bike =
                vehicles.CreateBike(
                    definition,
                    team.PrimaryColor,
                    team.SecondaryColor);

            Result<BikeState> addResult =
                team.Garage.AddBike(
                    bike);

            if (!addResult.IsSuccess)
            {
                team.AddCredits(
                    definition.CreditCost);

                return Result<BikeState>.Failure(
                    addResult.ErrorMessage);
            }

            return Result<BikeState>.Success(
                bike);
        }

        public Result<EngineState> PurchaseEngine(
            TeamState team,
            string definitionId)
        {
            if (team == null)
            {
                return Result<EngineState>.Failure(
                    "Team is required.");
            }

            EngineDefinition definition =
                database.GetEngineDefinition(
                    definitionId);

            if (definition == null)
            {
                return Result<EngineState>.Failure(
                    $"Engine definition '{definitionId}' was not found.");
            }

            Result eligibility =
                ValidatePurchase(
                    team,
                    definition.CreditCost,
                    definition.RequiredTechnologyId);

            if (!eligibility.IsSuccess)
            {
                return Result<EngineState>.Failure(
                    eligibility.ErrorMessage);
            }

            if (!team.TrySpendCredits(
                    definition.CreditCost))
            {
                return Result<EngineState>.Failure(
                    "Not enough credits.");
            }

            EngineState engine =
                vehicles.CreateEngine(
                    definition);

            Result<EngineState> addResult =
                team.Garage.AddEngine(
                    engine);

            if (!addResult.IsSuccess)
            {
                team.AddCredits(
                    definition.CreditCost);

                return Result<EngineState>.Failure(
                    addResult.ErrorMessage);
            }

            return Result<EngineState>.Success(
                engine);
        }

        public Result<ChassisState> PurchaseChassis(
            TeamState team,
            string definitionId)
        {
            if (team == null)
            {
                return Result<ChassisState>.Failure(
                    "Team is required.");
            }

            ChassisDefinition definition =
                database.GetChassisDefinition(
                    definitionId);

            if (definition == null)
            {
                return Result<ChassisState>.Failure(
                    $"Chassis definition '{definitionId}' was not found.");
            }

            Result eligibility =
                ValidatePurchase(
                    team,
                    definition.CreditCost,
                    definition.RequiredTechnologyId);

            if (!eligibility.IsSuccess)
            {
                return Result<ChassisState>.Failure(
                    eligibility.ErrorMessage);
            }

            if (!team.TrySpendCredits(
                    definition.CreditCost))
            {
                return Result<ChassisState>.Failure(
                    "Not enough credits.");
            }

            ChassisState chassis =
                vehicles.CreateChassis(
                    definition);

            Result<ChassisState> addResult =
                team.Garage.AddChassis(
                    chassis);

            if (!addResult.IsSuccess)
            {
                team.AddCredits(
                    definition.CreditCost);

                return Result<ChassisState>.Failure(
                    addResult.ErrorMessage);
            }

            return Result<ChassisState>.Success(
                chassis);
        }

        public Result<EquipmentState> PurchaseEquipment(
            TeamState team,
            string definitionId)
        {
            if (team == null)
            {
                return Result<EquipmentState>.Failure(
                    "Team is required.");
            }

            EquipmentDefinition definition =
                database.GetEquipmentDefinition(
                    definitionId);

            if (definition == null)
            {
                return Result<EquipmentState>.Failure(
                    $"Equipment definition '{definitionId}' was not found.");
            }

            Result eligibility =
                ValidatePurchase(
                    team,
                    definition.CreditCost,
                    definition.RequiredTechnologyId);

            if (!eligibility.IsSuccess)
            {
                return Result<EquipmentState>.Failure(
                    eligibility.ErrorMessage);
            }

            if (!team.TrySpendCredits(
                    definition.CreditCost))
            {
                return Result<EquipmentState>.Failure(
                    "Not enough credits.");
            }

            EquipmentState item =
                equipment.Create(
                    definition);

            Result<EquipmentState> addResult =
                team.Garage.AddEquipment(
                    item);

            if (!addResult.IsSuccess)
            {
                team.AddCredits(
                    definition.CreditCost);

                return Result<EquipmentState>.Failure(
                    addResult.ErrorMessage);
            }

            return Result<EquipmentState>.Success(
                item);
        }

        private Result ValidatePurchase(
            TeamState team,
            int creditCost,
            string requiredTechnologyId)
        {
            if (!string.IsNullOrWhiteSpace(
                    requiredTechnologyId) &&
                !team.HasTechnology(
                    requiredTechnologyId))
            {
                return Result.Failure(
                    $"Required technology '{requiredTechnologyId}' has not been researched.");
            }

            if (creditCost < 0)
            {
                return Result.Failure(
                    "Item cost cannot be negative.");
            }

            if (team.Credits <
                creditCost)
            {
                return Result.Failure(
                    $"Not enough credits. Requires {creditCost:N0}.");
            }

            return Result.Success();
        }
    }
}
