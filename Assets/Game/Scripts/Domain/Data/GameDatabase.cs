using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Tracks;
using RaceFatal.Racing;
using RaceFatal.Vehicles;

namespace RaceFatal.Data
{
    public class GameDatabase
    {
        private readonly Dictionary<string, BikeDefinition> bikeDefinitions = new Dictionary<string, BikeDefinition>();
        private readonly Dictionary<string, EngineDefinition> engineDefinitions = new Dictionary<string, EngineDefinition>();
        private readonly Dictionary<string, ChassisDefinition> chassisDefinitions = new Dictionary<string, ChassisDefinition>();
        private readonly Dictionary<string, EquipmentDefinition> equipmentDefinitions = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<string, TrackDefinition> trackDefinitions = new Dictionary<string, TrackDefinition>();
        private readonly Dictionary<string, RaceDefinition> raceDefinitions = new Dictionary<string, RaceDefinition>();


        public IReadOnlyDictionary<string, BikeDefinition> BikeDefinitions => bikeDefinitions;
        public IReadOnlyDictionary<string, EngineDefinition> EngineDefinitions => engineDefinitions;
        public IReadOnlyDictionary<string, ChassisDefinition> ChassisDefinitions => chassisDefinitions;
        public IReadOnlyDictionary<string, EquipmentDefinition> EquipmentDefinitions => equipmentDefinitions;
        public IReadOnlyDictionary<string, TrackDefinition> TrackDefinitions => trackDefinitions;
        public IReadOnlyDictionary<string, RaceDefinition> RaceDefinitions => raceDefinitions;

#region Setters
        public void AddBikeDefinition(BikeDefinition definition)
        {
            bikeDefinitions.Add(definition.Id, definition);
        }

        public void AddEngineDefinition(EngineDefinition definition)
        {
            engineDefinitions.Add(definition.Id, definition);
        }

        public void AddChassisDefinition(ChassisDefinition definition)
        {
            chassisDefinitions.Add(definition.Id, definition);
        }

        public void AddEquipmentDefinition(EquipmentDefinition definition)
        {
            equipmentDefinitions.Add(definition.Id, definition);
        }

        public void AddTrackDefinition(TrackDefinition definition)
        {
            trackDefinitions.Add(definition.Id, definition);
        }

        public void AddRaceDefinition(RaceDefinition definition)
        {
            raceDefinitions.Add(definition.Id, definition);
        }
#endregion

#region Getters
        public BikeDefinition GetBikeDefinition(string id)
        {
            return bikeDefinitions.TryGetValue(id, out BikeDefinition value) ? value : null;
        }

        public EngineDefinition GetEngineDefinition(string id)
        {
            return engineDefinitions.TryGetValue(id, out EngineDefinition value) ? value : null;
        }

        public ChassisDefinition GetChassisDefinition(string id)
        {
            return chassisDefinitions.TryGetValue(id, out ChassisDefinition value) ? value : null;
        }

        public EquipmentDefinition GetEquipmentDefinition(string id)
        {
            return equipmentDefinitions.TryGetValue(id, out EquipmentDefinition value) ? value : null;
        }

        public TrackDefinition GetTrackDefinition(string id)
        {
            return trackDefinitions.TryGetValue(id, out TrackDefinition value) ? value : null;
        }

        public RaceDefinition GetRaceDefinition(string id)
        {
            return raceDefinitions.TryGetValue(id, out RaceDefinition value) ? value : null;
        }
#endregion
    }
}
