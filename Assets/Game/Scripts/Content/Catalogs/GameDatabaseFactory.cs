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
            foreach (var perk in catalog.RacerPerkDefinitions)
                if (perk != null) database.AddRacerPerkDefinition(perk.CreateDefinition());
            foreach (var opponentTeam in catalog.OpponentTeamDefinitions)
            {
                var definition = opponentTeam.CreateDefinition();
                database.AddOpponentTeamDefinition(definition);
            }

            foreach (var technology in catalog.TechnologyDefinitions)
            {
                if (technology != null) database.AddTechnologyDefinition(technology.CreateDefinition(catalog.ResearchProgression));
            }

            foreach (var researcher in catalog.ResearcherDefinitions)
                if (researcher != null) database.AddResearcherDefinition(researcher.CreateDefinition());
            foreach (var entry in catalog.CareerEventDefinitions)
                if (entry != null) database.AddCareerEventDefinition(entry.CreateDefinition());
            // Existing projects remain raceable before any calendar assets have been authored.
            if (database.CareerEventDefinitions.Count == 0)
                foreach (var race in database.RaceDefinitions.Values)
                    database.AddCareerEventDefinition(new RaceFatal.Career.CareerEventDefinition(
                        "EVENT_" + race.Id, race.DisplayName, "Open race", RaceFatal.Career.CareerEventKind.Race,
                        0, 0, new[] { race.Id }, new[] {10000,8000,6500,5000,5000,5000,3500,3500,3500,2500,2500,2500},
                        System.Array.Empty<int>(), new[] {25,18,15,12,10,8,6,5,4,3,2,1}));
            return database;
        }
    }
}
