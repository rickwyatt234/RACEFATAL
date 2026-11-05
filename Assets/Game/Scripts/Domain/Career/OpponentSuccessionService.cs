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
            int ready=team.Garage.GetRaceReadyBikesFor(raceClass).Count;
            foreach(string buildId in definition.StartingBikeBuildIds)
            {
                if(ready>=required)break;
                var build=database.GetBikeBuildDefinition(buildId);
                var engine=build==null?null:database.GetEngineDefinition(build.EngineDefinitionId);
                if(engine==null || !GarageState.IsClassEligible(engine.EngineClass,raceClass))continue;
                var newBike=builds.CreateInGarage(build,team.Garage,team.PrimaryColor,team.SecondaryColor);
                if(!newBike.IsSuccess)return Result.Failure(newBike.ErrorMessage);
                ready++;
            }
            // If the team has two legitimate presets that were both destroyed, a further
            // fresh copy may be needed, using the same signature model, not arbitrary cheats.
            if(ready<required)
            {
                var build=definition.StartingBikeBuildIds.Select(database.GetBikeBuildDefinition)
                    .FirstOrDefault(b=>b!=null && database.GetEngineDefinition(b.EngineDefinitionId)!=null &&
                        GarageState.IsClassEligible(database.GetEngineDefinition(b.EngineDefinitionId).EngineClass,raceClass));
                while(ready<required && build!=null)
                {
                    var newBike=builds.CreateInGarage(build,team.Garage,team.PrimaryColor,team.SecondaryColor);
                    if(!newBike.IsSuccess)return Result.Failure(newBike.ErrorMessage);
                    ready++;
                }
            }
            return Result.Success();
        }
    }
}
