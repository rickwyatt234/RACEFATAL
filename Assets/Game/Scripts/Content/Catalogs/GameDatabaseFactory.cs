using RaceFatal.Data;

namespace RaceFatal.Content
{
    public static class GameDatabaseFactory
    {
        public static GameDatabase CreateGameDatabase(GameContentCatalogSO catalog)
        {
            GameDatabase database = new GameDatabase();

            foreach (var bike in catalog.BikeDefinitions)
            {
                database.AddBikeDefinition(bike.CreateBikeDefinition());
            }

            foreach (var engine in catalog.EngineDefinitions)
            {
                database.AddEngineDefinition(engine.CreateEngineDefinition());
            }

            foreach (var chassis in catalog.ChassisDefinitions)
            {
                database.AddChassisDefinition(chassis.CreateChassisDefinition());
            }

            foreach (var equipment in catalog.EquipmentDefinitions)
            {
                database.AddEquipmentDefinition(equipment.CreateEquipmentDefinition());
            }
            foreach (var track in catalog.TrackDefinitions)
            {
                database.AddTrackDefinition(track.CreateTrackDefinition());
            }
            foreach (var race in catalog.RaceDefinitions)
            {
                database.AddRaceDefinition(race.CreateRaceDefinition());
            }
            foreach (var bikeBuild in catalog.BikeBuildDefinitions)
            {
                database.AddBikeBuildDefinition(bikeBuild.CreateDefinition());
            }
            foreach (var racer in catalog.RacerDefinitions)
            {
                database.AddRacerDefinition(racer.CreateDefinition());
            }
            foreach (var opponentTeam in catalog.OpponentTeamDefinitions)
            {
                var definition = opponentTeam.CreateDefinition();
                database.AddOpponentTeamDefinition(definition);
            }

            return database;
        }
    }
}
