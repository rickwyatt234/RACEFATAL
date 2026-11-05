using System;
using RaceFatal.Data;
using System.Linq;
using RaceFatal.Career;
using UnityEngine;

namespace RaceFatal.Content
{
    public static class GameDatabaseFactory
    {
        public static GameDatabase CreateGameDatabase(GameContentCatalogSO catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
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
                if (track == null)
                {
                    Debug.LogWarning($"Content catalog '{catalog.name}' contains a missing Track asset. Remove the missing entry from Track Definitions.", catalog);
                    continue;
                }
                database.AddTrackDefinition(track.CreateTrackDefinition());
            }

            foreach (var race in catalog.RaceDefinitions)
            {
                if (race == null)
                {
                    Debug.LogWarning($"Content catalog '{catalog.name}' contains a missing Race asset. Remove the missing entry from Race Definitions.", catalog);
                    continue;
                }
                if (!race.TryCreateRaceDefinition(out var definition, out var error))
                {
                    Debug.LogError($"{error} This race is excluded from the database until corrected.", race);
                    continue;
                }
                if (database.GetTrackDefinition(definition.TrackId) == null)
                {
                    Debug.LogError($"Race asset '{race.name}' (ID '{race.Id}') references track '{definition.TrackId}', which is not registered in catalog '{catalog.name}'. Add it to Track Definitions. This race is excluded until corrected.", race);
                    continue;
                }
                database.AddRaceDefinition(definition);
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
                if (perk != null)
                    database.AddRacerPerkDefinition(perk.CreateDefinition());
            RacerPerkCatalog.Ensure(database);
            foreach (var opponentTeam in catalog.OpponentTeamDefinitions)
            {
                var definition = opponentTeam.CreateDefinition();
                database.AddOpponentTeamDefinition(definition);
            }

            foreach (var entry in catalog.CareerEventDefinitions)
                if (entry != null)
                    database.AddCareerEventDefinition(entry.CreateDefinition());
            if (database.CareerEventDefinitions.Count == 0)
                foreach (var race in database.RaceDefinitions.Values)
                    database.AddCareerEventDefinition(new RaceFatal.Career.CareerEventDefinition("EVENT_" + race.Id, race.DisplayName, "Open race", race.Deathmatch != null ? RaceFatal.Career.CareerEventKind.Deathmatch : RaceFatal.Career.CareerEventKind.Race, 0, 0, new[] { race.Id }, new[] { 10000, 8000, 6500, 5000, 5000, 5000, 3500, 3500, 3500, 2500, 2500, 2500 }, System.Array.Empty<int>(), new[] { 25, 18, 15, 12, 10, 8, 6, 5, 4, 3, 2, 1 }));
            var brands = catalog.BrandDefinitions.Where(b => b != null).Select(b => b.CreateDefinition()).ToList();
            if (brands.Count == 0 || brands.Any(b => b.Id.StartsWith("RF_BRAND_", StringComparison.Ordinal) && b.Products.Count == 0))
                BrandDefaults.Ensure(database);
            foreach (var brand in brands)
            {
                var definition = brand;
                if (database.BrandDefinitions.TryGetValue(brand.Id, out var standard) && brand.Products.Count == 0)
                    definition = new BrandDefinition(brand.Id, brand.DisplayName, brand.Tagline, brand.ColorHex, brand.Category,
                        brand.Philosophy, standard.Products, standard.Contracts.Concat(brand.Contracts).GroupBy(c => c.Id).Select(g => g.Last()).ToList());
                database.ReplaceBrandDefinition(TeamPerkCatalog.AddContractRewards(definition));
            }
            // Keep generated/default brands on the same permanent reward contract path.
            foreach (var item in database.BrandDefinitions.Values.ToArray())
                if (TeamPerkCatalog.ForBrand(item.DisplayName).Any() && !item.Contracts.Any(c => c.RewardKind == BrandRewardKind.TeamPerk))
                    database.ReplaceBrandDefinition(TeamPerkCatalog.AddContractRewards(item));
            return database;
        }
    }
}
