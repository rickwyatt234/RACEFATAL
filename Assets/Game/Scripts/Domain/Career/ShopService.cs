using System;
using System.Collections.Generic;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public enum ShopItemKind { Engine, Chassis, Equipment }

    public sealed class ShopOffer
    {
        public ShopItemKind Kind { get; }
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Details { get; }
        public int CreditCost { get; }
        public string RequiredTechnologyId { get; }

        internal ShopOffer(ShopItemKind kind, string id, string name,
            string category, string details, int cost, string technology)
        {
            Kind = kind;
            DefinitionId = id;
            DisplayName = name;
            Category = category;
            Details = details;
            CreditCost = cost;
            RequiredTechnologyId = technology;
        }
    }

    // Prices and unlock requirements always come from the registered content.
    // The UI cannot supply a price or bypass a technology requirement.
    public sealed class ShopService
    {
        private readonly GameDatabase database;
        private readonly VehicleFactory vehicles = new VehicleFactory();
        private readonly EquipmentFactory equipment = new EquipmentFactory();

        public ShopService(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public List<ShopOffer> GetOffers()
        {
            var offers = new List<ShopOffer>();
            foreach (var d in database.EngineDefinitions.Values)
                offers.Add(new ShopOffer(ShopItemKind.Engine, d.Id, d.DisplayName,
                    "ENGINES", $"ENGINE CLASS  {d.EngineClass}\nTOP SPEED  {d.TopSpeed:0.##}\nACCELERATION  {d.Acceleration:0.##}",
                    d.CreditCost, d.RequiredTechnologyId));
            foreach (var d in database.ChassisDefinitions.Values)
                offers.Add(new ShopOffer(ShopItemKind.Chassis, d.Id, d.DisplayName,
                    "CHASSIS", $"MASS MODIFIER  {d.MassModifier:0.##}\nHANDLING MODIFIER  {d.HandlingModifier:0.##}",
                    d.CreditCost, d.RequiredTechnologyId));
            foreach (var d in database.EquipmentDefinitions.Values)
            {
                string details = $"CATEGORY  {d.Category}\nREQUIRED NODE  {d.RequiredNodeSize}\nACTIVATION  {d.ActivationMode}";
                if (d is WeaponDefinition weapon)
                    details += $"\nDAMAGE  {weapon.Damage:0.##}    RANGE  {weapon.Range:0.##}\nSTARTING AMMO  {weapon.StartingAmmo}";
                if (d is ShieldDefinition shield)
                    details += $"\nCAPACITY  {shield.Capacity:0.##}\nRECHARGE / SEC  {shield.RechargePerSecond:0.##}";
                offers.Add(new ShopOffer(ShopItemKind.Equipment, d.Id, d.DisplayName,
                    d.Category.ToString().ToUpperInvariant(), details,
                    d.CreditCost, d.RequiredTechnologyId));
            }
            offers.Sort((a, b) =>
            {
                int category = string.CompareOrdinal(a.Category, b.Category);
                return category != 0 ? category : string.CompareOrdinal(a.DisplayName, b.DisplayName);
            });
            return offers;
        }

        public ShopOffer FindOffer(ShopItemKind kind, string id)
        {
            return GetOffers().Find(o => o.Kind == kind && o.DefinitionId == id);
        }

        public Result CanPurchase(TeamState team, ShopItemKind kind, string id)
        {
            if (team == null) return Result.Failure("NO LOADED TEAM.");
            ShopOffer offer = FindOffer(kind, id);
            if (offer == null) return Result.Failure("ITEM IS NO LONGER AVAILABLE.");
            if (offer.CreditCost < 0) return Result.Failure("ITEM HAS AN INVALID PRICE.");
            if (!string.IsNullOrWhiteSpace(offer.RequiredTechnologyId) &&
                !team.HasTechnology(offer.RequiredTechnologyId))
                return Result.Failure("REQUIRES TECHNOLOGY: " + offer.RequiredTechnologyId);
            if (team.Credits < offer.CreditCost)
                return Result.Failure($"INSUFFICIENT CREDITS. NEED {offer.CreditCost - team.Credits:N0} MORE.");
            return Result.Success();
        }

        public Result Purchase(TeamState team, ShopItemKind kind, string id)
        {
            Result allowed = CanPurchase(team, kind, id);
            if (!allowed.IsSuccess) return allowed;
            ShopOffer offer = FindOffer(kind, id);

            // Create before charging; insertion failure refunds the debit.
            Func<Result> add;
            switch (kind)
            {
                case ShopItemKind.Engine:
                    var engine = vehicles.CreateEngine(database.GetEngineDefinition(id));
                    add = () => ToResult(team.Garage.AddEngine(engine));
                    break;
                case ShopItemKind.Chassis:
                    var chassis = vehicles.CreateChassis(database.GetChassisDefinition(id));
                    add = () => ToResult(team.Garage.AddChassis(chassis));
                    break;
                case ShopItemKind.Equipment:
                    var item = equipment.Create(database.GetEquipmentDefinition(id));
                    add = () => ToResult(team.Garage.AddEquipment(item));
                    break;
                default:
                    return Result.Failure("UNKNOWN ITEM TYPE.");
            }
            if (!team.TrySpendCredits(offer.CreditCost))
                return Result.Failure("INSUFFICIENT CREDITS.");
            Result result = add();
            if (!result.IsSuccess) team.AddCredits(offer.CreditCost);
            return result;
        }

        private static Result ToResult<T>(Result<T> result)
        {
            return result.IsSuccess ? Result.Success() : Result.Failure(result.ErrorMessage);
        }
    }
}
