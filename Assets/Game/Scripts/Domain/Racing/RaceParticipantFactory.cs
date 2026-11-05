using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public sealed class RaceParticipantFactory
    {
        private readonly GameDatabase database;
        private readonly BikePerformanceCalculator performanceCalculator;
        public RaceParticipantFactory(GameDatabase database, BikePerformanceCalculator performanceCalculator)
        {
            this.database = database;
            this.performanceCalculator = performanceCalculator;
        }

        public Result<RaceParticipant> Create(RacerState racer, BikeState bike, RaceParticipantRole role, TeamState team = null)
        {
            if (racer == null)
            {
                return Result<RaceParticipant>.Failure("Racer is required.");
            }

            if (bike == null)
            {
                return Result<RaceParticipant>.Failure("Bike is required.");
            }

            if (bike.IsDestroyed)
            {
                return Result<RaceParticipant>.Failure("Destroyed bikes cannot enter races.");
            }

            BikeDefinition bikeDefinition = database.GetBikeDefinition(bike.BikeDefinitionId);
            if (bikeDefinition == null)
            {
                return Result<RaceParticipant>.Failure($"Bike definition '{bike.BikeDefinitionId}' was not found.");
            }

            Result<BikePerformance> performanceResult = performanceCalculator.Calculate(bike);
            if (!performanceResult.IsSuccess)
                return Result<RaceParticipant>.Failure(performanceResult.ErrorMessage);
            var perks = RacerPerkBonuses.For(racer, database);
            var engineBrand = bike.Loadout.Engine == null ? null : BrandService.OwnerOf(database, ShopItemKind.Engine, bike.Loadout.Engine.EngineDefinitionId);
            var chassisBrand = bike.Loadout.Chassis == null ? null : BrandService.OwnerOf(database, ShopItemKind.Chassis, bike.Loadout.Chassis.ChassisDefinitionId);
            float EngineBonus(string effect) => engineBrand == null ? 0f : TeamPerkCatalog.Bonus(team,effect,engineBrand.DisplayName);
            float ChassisBonus(string effect) => chassisBrand == null ? 0f : TeamPerkCatalog.Bonus(team,effect,chassisBrand.DisplayName);
            var basePerformance = performanceResult.Value;
            float capacity = basePerformance.EnergyCapacity * (1f + System.Math.Min(.20f, perks.Bonus(RacerPerkEffect.EnergyCapacityPercent) + EngineBonus("EnergyCapacityPercent"))) + perks.EnergyCapacity;
            var energy = new EnergyPool(System.Math.Max(0f, capacity));
            Result<RaceEquipmentSystem> equipmentResult = RaceEquipmentSystem.Create(racer.RacerId, bike.Loadout, database, energy);
            if (!equipmentResult.IsSuccess)
                return Result<RaceParticipant>.Failure(equipmentResult.ErrorMessage);
            // Personal race-performance modifiers are applied once on entry, never to persistent equipment definitions.
            var tuned = new BikePerformance(
                basePerformance.TopSpeedMPH,
                basePerformance.Acceleration * (1f + System.Math.Min(.15f, perks.Bonus(RacerPerkEffect.Redline) + EngineBonus("Acceleration"))),
                basePerformance.BaseHandling, basePerformance.Mass,
                basePerformance.MaxIntegrity * (1f + System.Math.Min(.15f, perks.Bonus(RacerPerkEffect.MaxIntegrityPercent) + ChassisBonus("MaxIntegrityPercent"))),
                basePerformance.SteeringResponseMultiplier * (1f + System.Math.Min(.20f, perks.Bonus(RacerPerkEffect.SteeringResponse) + ChassisBonus("SteeringResponse"))),
                basePerformance.LeanResponseMultiplier,
                basePerformance.StabilityMultiplier * (1f + ChassisBonus("Stability") - .04f * (perks.Has(RacerPerkEffect.Redline) ? 1f : 0f)),
                System.Math.Min(.9f, basePerformance.ImpactResistance + ChassisBonus("ImpactDefense")),
                basePerformance.ConfiguredHandling / System.Math.Max(.01f, basePerformance.BaseHandling),
                basePerformance.EnergyCapacity);
            RaceVehicleState vehicle = new RaceVehicleState(bike, tuned, energy, equipmentResult.Value, perks);
            return Result<RaceParticipant>.Success(new RaceParticipant(racer, vehicle, role, perks: perks));
        }
    }
}
