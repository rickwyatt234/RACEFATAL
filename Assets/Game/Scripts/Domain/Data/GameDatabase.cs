using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Tracks;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using RaceFatal.Career;

namespace RaceFatal.Data
{
    public class GameDatabase
    {
        private readonly Dictionary<string, BikeDefinition> bikeDefinitions = new Dictionary<string, BikeDefinition>();
        private readonly Dictionary<string, EngineDefinition> engineDefinitions = new Dictionary<string, EngineDefinition>();
        private readonly Dictionary<string, ChassisDefinition> chassisDefinitions = new Dictionary<string, ChassisDefinition>();
        private readonly Dictionary<string, EquipmentDefinition> equipmentDefinitions = new Dictionary<string, EquipmentDefinition>();
        private readonly Dictionary<string, BikeBuildDefinition> bikeBuildDefinitions = new Dictionary<string, BikeBuildDefinition>();
        private readonly Dictionary<string, RacerDefinition> racerDefinitions = new Dictionary<string, RacerDefinition>();
        private readonly Dictionary<string, OpponentTeamDefinition> opponentTeamDefinitions = new Dictionary<string, OpponentTeamDefinition>();
        private readonly Dictionary<string, TrackDefinition> trackDefinitions = new Dictionary<string, TrackDefinition>();
        private readonly Dictionary<string, RaceDefinition> raceDefinitions = new Dictionary<string, RaceDefinition>();

        private readonly List<OpponentTeamDefinition> opponentTeamDefinitionList = new();

        public IReadOnlyDictionary<string, BikeDefinition> BikeDefinitions => bikeDefinitions;
        public IReadOnlyDictionary<string, EngineDefinition> EngineDefinitions => engineDefinitions;
        public IReadOnlyDictionary<string, ChassisDefinition> ChassisDefinitions => chassisDefinitions;
        public IReadOnlyDictionary<string, EquipmentDefinition> EquipmentDefinitions => equipmentDefinitions;
        public IReadOnlyDictionary<string, TrackDefinition> TrackDefinitions => trackDefinitions;
        public IReadOnlyDictionary<string, RaceDefinition> RaceDefinitions => raceDefinitions;
        
        public IReadOnlyList<OpponentTeamDefinition> OpponentTeamDefinitions => opponentTeamDefinitionList;

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
        
        public void AddBikeBuildDefinition(BikeBuildDefinition definition)
        {
            bikeBuildDefinitions.Add(definition.Id, definition);
        }

        public void AddOpponentTeamDefinition(OpponentTeamDefinition definition)
        {
            opponentTeamDefinitions.Add(definition.Id, definition);
            opponentTeamDefinitionList.Add(definition);
        }

        public void AddRacerDefinition(RacerDefinition definition)
        {
            racerDefinitions.Add(definition.Id, definition);
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

        public BikeBuildDefinition GetBikeBuildDefinition(string id)
        {
            return bikeBuildDefinitions.TryGetValue(id, out BikeBuildDefinition value) ? value : null;
        }

        public OpponentTeamDefinition GetOpponentTeamDefinition(string id)
        {
            return opponentTeamDefinitions.TryGetValue(id, out OpponentTeamDefinition value) ? value : null;
        }

        public RacerDefinition GetRacerDefinition(string id)
        {
            return racerDefinitions.TryGetValue(id, out RacerDefinition value) ? value : null;
        }
#endregion
    }
}
