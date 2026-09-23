using System;
using System.Collections.Generic;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    // Reads prices and technology requirements from authored definitions.
    // Owned physical instances are added to the team's existing GarageState.
    public class ShopService
    {
        private readonly GameDatabase database;
        private readonly VehicleFactory vehicles;
        private readonly EquipmentFactory equipment;

        public ShopService(
            GameDatabase database,
            VehicleFactory vehicles,
            EquipmentFactory equipment)
        {
            this.database = database ??
                throw new ArgumentNullException(nameof(database));
            this.vehicles = vehicles ??
                throw new ArgumentNullException(nameof(vehicles));
            this.equipment = equipment ??
                throw new ArgumentNullException(nameof(equipment));
        }

        public IReadOnlyList<ShopOffer> GetOffers()
        {
            var offers = new List<ShopOffer>();

            foreach (BikeDefinition definition in database.BikeDefinitions.Values)
            {
                if (!IsSellable(definition?.Id, definition?.CreditCost ?? 0))
                    continue;

                offers.Add(new ShopOffer(
                    ShopItemKind.Bike,
                    definition.Id,
                    definition.DisplayName,
                    definition.CreditCost,
                    definition.RequiredTechnologyId));
            }

            foreach (EngineDefinition definition in database.EngineDefinitions.Values)
            {
                if (!IsSellable(definition?.Id, definition?.CreditCost ?? 0))
                    continue;

                offers.Add(new ShopOffer(
                    ShopItemKind.Engine,
                    definition.Id,
                    definition.DisplayName,
                    definition.CreditCost,
                    definition.RequiredTechnologyId));
            }

            foreach (ChassisDefinition definition in database.ChassisDefinitions.Values)
            {
                if (!IsSellable(definition?.Id, definition?.CreditCost ?? 0))
                    continue;

                offers.Add(new ShopOffer(
                    ShopItemKind.Chassis,
                    definition.Id,
                    definition.DisplayName,
                    definition.CreditCost,
                    definition.RequiredTechnologyId));
            }

            foreach (EquipmentDefinition definition in database.EquipmentDefinitions.Values)
            {
                if (!IsSellable(definition?.Id, definition?.CreditCost ?? 0))
                    continue;

                offers.Add(new ShopOffer(
                    ShopItemKind.Equipment,
                    definition.Id,
                    definition.DisplayName,
                    definition.CreditCost,
                    definition.RequiredTechnologyId));
            }

            offers.Sort((a, b) =>
            {
                int typeOrder = a.Kind.CompareTo(b.Kind);
                return typeOrder != 0
                    ? typeOrder
                    : string.Compare(
                        a.DisplayName,
                        b.DisplayName,
                        StringComparison.OrdinalIgnoreCase);
            });

            return offers;
        }

        public Result<ShopPurchaseReceipt> Purchase(
            TeamState team,
            ShopItemKind kind,
            string definitionId)
        {
            if (team == null)
                return Result<ShopPurchaseReceipt>.Failure(
                    "A team is required.");

            ShopOffer offer = null;
            foreach (ShopOffer candidate in GetOffers())
            {
                if (candidate.Kind == kind &&
                    candidate.DefinitionId == definitionId)
                {
                    offer = candidate;
                    break;
                }
            }

            // The caller supplies only a kind and stable ID. The current
            // catalog remains authoritative for prices and requirements.
            if (offer == null)
                return Result<ShopPurchaseReceipt>.Failure(
                    "This item is not currently offered for sale.");

            if (!offer.IsUnlockedFor(team))
                return Result<ShopPurchaseReceipt>.Failure(
                    "Research required: " + offer.RequiredTechnologyId);

            if (team.Credits < offer.CreditCost)
                return Result<ShopPurchaseReceipt>.Failure(
                    "Insufficient credits.");

            string instanceId;
            Result ownership;

            try
            {
                switch (kind)
                {
                    case ShopItemKind.Bike:
                        BikeDefinition bike = database.GetBikeDefinition(definitionId);
                        if (bike == null)
                            return MissingContent();
                        BikeState ownedBike = vehicles.CreateBike(
                            bike, team.PrimaryColor, team.SecondaryColor);
                        instanceId = ownedBike.BikeId;
                        Result<BikeState> addBike = team.Garage.AddBike(ownedBike);
                        ownership = addBike.IsSuccess
                            ? Result.Success()
                            : Result.Failure(addBike.ErrorMessage);
                        break;

                    case ShopItemKind.Engine:
                        EngineDefinition engine = database.GetEngineDefinition(definitionId);
                        if (engine == null)
                            return MissingContent();
                        EngineState ownedEngine = vehicles.CreateEngine(engine);
                        instanceId = ownedEngine.EngineId;
                        Result<EngineState> addEngine = team.Garage.AddEngine(ownedEngine);
                        ownership = addEngine.IsSuccess
                            ? Result.Success()
                            : Result.Failure(addEngine.ErrorMessage);
                        break;

                    case ShopItemKind.Chassis:
                        ChassisDefinition chassis = database.GetChassisDefinition(definitionId);
                        if (chassis == null)
                            return MissingContent();
                        ChassisState ownedChassis = vehicles.CreateChassis(chassis);
                        instanceId = ownedChassis.ChassisId;
                        Result<ChassisState> addChassis = team.Garage.AddChassis(ownedChassis);
                        ownership = addChassis.IsSuccess
                            ? Result.Success()
                            : Result.Failure(addChassis.ErrorMessage);
                        break;

                    case ShopItemKind.Equipment:
                        EquipmentDefinition item = database.GetEquipmentDefinition(definitionId);
                        if (item == null)
                            return MissingContent();
                        EquipmentState ownedEquipment = equipment.Create(item);
                        instanceId = ownedEquipment.EquipmentId;
                        Result<EquipmentState> addEquipment =
                            team.Garage.AddEquipment(ownedEquipment);
                        ownership = addEquipment.IsSuccess
                            ? Result.Success()
                            : Result.Failure(addEquipment.ErrorMessage);
                        break;

                    default:
                        return Result<ShopPurchaseReceipt>.Failure(
                            "Unsupported shop item category.");
                }
            }
            catch (Exception exception)
            {
                return Result<ShopPurchaseReceipt>.Failure(
                    "Could not create purchased item: " + exception.Message);
            }

            if (!ownership.IsSuccess)
                return Result<ShopPurchaseReceipt>.Failure(
                    ownership.ErrorMessage);

            // The wallet was checked before creating the physical asset.
            // No other code can spend credits concurrently on this thread.
            team.TrySpendCredits(offer.CreditCost);

            return Result<ShopPurchaseReceipt>.Success(
                new ShopPurchaseReceipt(offer, instanceId));
        }

        private static bool IsSellable(string id, int price)
        {
            // Zero-priced prototype assets and definitions with blank IDs
            // remain outside the live shop until they are authored.
            return !string.IsNullOrWhiteSpace(id) && price > 0;
        }

        private static Result<ShopPurchaseReceipt> MissingContent()
        {
            return Result<ShopPurchaseReceipt>.Failure(
                "The selected item is no longer in the content database.");
        }
    }
}
