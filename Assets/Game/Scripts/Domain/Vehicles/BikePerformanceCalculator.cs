using System;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public sealed class BikePerformanceCalculator
    {
        private readonly GameDatabase database;
        public BikePerformanceCalculator(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public Result<BikePerformance> Calculate(BikeState bike)
        {
            if (bike == null)
            {
                return Result<BikePerformance>.Failure("Bike is required.");
            }

            if (bike.IsDestroyed)
            {
                return Result<BikePerformance>.Failure("Cannot calculate performance for a destroyed bike.");
            }

            BikeDefinition bikeDefinition = database.GetBikeDefinition(bike.BikeDefinitionId);
            if (bikeDefinition == null)
            {
                return Result<BikePerformance>.Failure($"Bike definition '{bike.BikeDefinitionId}' was not found.");
            }

            EngineState engineState = bike.Loadout.Engine;
            if (engineState == null)
            {
                return Result<BikePerformance>.Failure("Bike does not have an engine.");
            }

            EngineDefinition engineDefinition = database.GetEngineDefinition(engineState.EngineDefinitionId);
            if (engineDefinition == null)
            {
                return Result<BikePerformance>.Failure($"Engine definition '{engineState.EngineDefinitionId}' was not found.");
            }

            ChassisState chassisState = bike.Loadout.Chassis;
            if (chassisState == null)
            {
                return Result<BikePerformance>.Failure("Bike does not have a chassis.");
            }

            ChassisDefinition chassisDefinition = database.GetChassisDefinition(chassisState.ChassisDefinitionId);
            if (chassisDefinition == null)
            {
                return Result<BikePerformance>.Failure($"Chassis definition '{chassisState.ChassisDefinitionId}' was not found.");
            }

            var utilities = new System.Collections.Generic.Dictionary<UtilityEffect, float>();
            float Bonus(UtilityEffect effect) => utilities.TryGetValue(effect, out var bonus) ? bonus : 0f;
            float passiveHandling = 1f;
            float energyCapacity = 0f;
            foreach (var node in bike.Loadout.Nodes)
            {
                if (!node.IsOccupied)
                    continue;
                var definition = database.GetEquipmentDefinition(node.InstalledEquipment.EquipmentDefinitionId);
                if (definition == null)
                    return Result<BikePerformance>.Failure("Installed equipment definition was not found.");
                if (definition is RaceUtilityDefinition utility)
                    utilities[utility.Effect] = Math.Max(Bonus(utility.Effect), utility.Strength);
                if (definition is HandlingUtilityDefinition handling)
                    passiveHandling *= handling.HandlingMultiplier;
                if (definition is BoosterDefinition booster)
                    energyCapacity += booster.EnergyCapacity;
            }

            var performance = new BikePerformance(
                engineDefinition.TopSpeed * (1f - bikeDefinition.SpeedPenalty),
                engineDefinition.Acceleration * (1f - bikeDefinition.AccelerationPenalty) * (1f + Bonus(UtilityEffect.Acceleration)),
                chassisDefinition.HandlingMultiplier * (1f - bikeDefinition.HandlingPenalty),
                chassisDefinition.Mass * (1f - Bonus(UtilityEffect.Mass)), chassisDefinition.MaxIntegrity * (1f - bikeDefinition.IntegrityPenalty) * (1f + Bonus(UtilityEffect.Integrity)), chassisDefinition.SteeringResponseMultiplier * (1f + Bonus(UtilityEffect.Steering)), chassisDefinition.LeanResponseMultiplier, chassisDefinition.StabilityMultiplier * (1f + Bonus(UtilityEffect.Stability)), chassisDefinition.ImpactResistance, passiveHandling * (1f + Bonus(UtilityEffect.Grip)), energyCapacity);
            return Result<BikePerformance>.Success(performance);
        }
    }
}
