using System;
using RaceFatal.Data;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public sealed class BikePerformanceCalculator
    {
        private readonly GameDatabase database;

        public BikePerformanceCalculator(
            GameDatabase database)
        {
            this.database = database
                ?? throw new ArgumentNullException(
                    nameof(database));
        }

        public Result<BikePerformance> Calculate(
            BikeState bike)
        {
            if (bike == null)
            {
                return Result<BikePerformance>.Failure(
                    "Bike is required.");
            }

            if (bike.IsDestroyed)
            {
                return Result<BikePerformance>.Failure(
                    "Cannot calculate performance for a destroyed bike.");
            }

            BikeDefinition bikeDefinition =
                database.GetBikeDefinition(
                    bike.BikeDefinitionId);

            if (bikeDefinition == null)
            {
                return Result<BikePerformance>.Failure(
                    $"Bike definition '{bike.BikeDefinitionId}' was not found.");
            }

            EngineState engineState =
                bike.Loadout.Engine;

            if (engineState == null)
            {
                return Result<BikePerformance>.Failure(
                    "Bike does not have an engine.");
            }

            EngineDefinition engineDefinition =
                database.GetEngineDefinition(
                    engineState.EngineDefinitionId);

            if (engineDefinition == null)
            {
                return Result<BikePerformance>.Failure(
                    $"Engine definition '{engineState.EngineDefinitionId}' was not found.");
            }

            ChassisState chassisState =
                bike.Loadout.Chassis;

            if (chassisState == null)
            {
                return Result<BikePerformance>.Failure(
                    "Bike does not have a chassis.");
            }

            ChassisDefinition chassisDefinition =
                database.GetChassisDefinition(
                    chassisState.ChassisDefinitionId);

            if (chassisDefinition == null)
            {
                return Result<BikePerformance>.Failure(
                    $"Chassis definition '{chassisState.ChassisDefinitionId}' was not found.");
            }

            float mass = Math.Max(
                1f,
                bikeDefinition.BaseWeight +
                chassisDefinition.MassModifier);

            float handling = Math.Max(
                0.1f,
                bikeDefinition.BaseHandling +
                chassisDefinition.HandlingModifier);

            var performance =
                new BikePerformance(
                    engineDefinition.TopSpeed,
                    engineDefinition.Acceleration,
                    handling,
                    mass);

            return Result<BikePerformance>.Success(
                performance);
        }
    }
}