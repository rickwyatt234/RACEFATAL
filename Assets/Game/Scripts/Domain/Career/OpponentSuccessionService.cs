using System;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    /// <summary>
    /// Opposing teams retain all deceased racers in their archive but receive a new
    /// individual before their next race. No personal perk or rivalry is inherited.
    /// Opponent replacements use real authored bike builds; they never invent illegal loadouts.
    /// </summary>
    public sealed class OpponentSuccessionService
    {
        private readonly GameDatabase database;
        private readonly BikeBuildFactory builds;
        public OpponentSuccessionService(GameDatabase database)
        {
            this.database=database;
            builds=new BikeBuildFactory(database,new VehicleFactory(),new EquipmentFactory());
        }
        public Result Ensure(TeamState team,OpponentTeamDefinition definition,EngineClass raceClass)
        {
            if(team==null || definition==null)return Result.Failure("OPPONENT TEAM CONTENT IS MISSING.");
            int required=2;
            while(team.Roster.GetRaceEligibleRacers().Count<required)
            {
                var fallbackId=definition.RacerDefinitionIds.FirstOrDefault();
                var authored=database.GetRacerDefinition(fallbackId);
                if(authored==null)return Result.Failure("OPPONENT REPLACEMENT RACER IS NOT AUTHORED.");
                int sequence=team.Roster.Racers.Count+1;
                string name=authored.DisplayName+" / "+sequence;
                var racer=new RacerState(Guid.NewGuid().ToString("N"),name,team.TeamId,false,authored.Id);
                var added=team.Roster.AddRacer(racer);
                if(!added.IsSuccess)return Result.Failure(added.ErrorMessage);
            }
            // Fill the two distinct seats. Higher tiers keep the frame and mounts, and
            // select a legal engine from a preferred manufacturer when available.
            var ready = team.Garage.GetRaceReadyBikesFor(raceClass).ToList();
            foreach (string id in definition.StartingBikeBuildIds.Take(required))
            {
                var build = database.GetBikeBuildDefinition(id);
                if (build == null) return Result.Failure("Opponent build is missing: " + id);
                var existing = ready.FirstOrDefault(b => b.BikeDefinitionId == build.BikeDefinitionId);
                if (existing != null) { ready.Remove(existing); continue; }
                var engine = database.GetEngineDefinition(build.EngineDefinitionId);
                if (engine == null || !GarageState.IsClassEligible(engine.EngineClass, raceClass))
                {
                    var upgrade = database.EngineDefinitions.Values.Where(e => GarageState.IsClassEligible(e.EngineClass, raceClass))
                        .OrderByDescending(e => team.Brands.preferredBrandIds.Contains(BrandService.OwnerOf(database, ShopItemKind.Engine, e.Id)?.Id))
                        .ThenBy(e => e.CreditCost).ThenBy(e => e.Id, StringComparer.Ordinal).FirstOrDefault();
                    if (upgrade == null) continue;
                    build = new BikeBuildDefinition(build.Id, build.DisplayName, build.BikeDefinitionId,
                        upgrade.Id, build.ChassisDefinitionId, build.Equipment);
                }
                var staged = new GarageState();
                var made = builds.CreateInGarage(build, staged, team.PrimaryColor, team.SecondaryColor);
                if (!made.IsSuccess) return Result.Failure(made.ErrorMessage);
                var imported = team.Garage.ImportKit(staged);
                if (!imported.IsSuccess) return imported;
            }
            return Result.Success();
        }
    }
}
