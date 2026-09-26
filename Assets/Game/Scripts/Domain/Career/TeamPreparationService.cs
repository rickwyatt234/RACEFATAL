using System.Collections.Generic;
using RaceFatal.Racing;
using RaceFatal.Vehicles;

namespace RaceFatal.Career
{
    public sealed class PreparationCheck
    {
        public bool Ready { get; }
        public string Message { get; }
        public PreparationCheck(bool ready, string message) { Ready = ready; Message = message; }
        public override string ToString() => (Ready ? "[READY] " : "[ACTION] ") + Message;
    }

    public static class TeamPreparationService
    {
        public static IReadOnlyList<PreparationCheck> Inspect(GameSessionState session, RaceDefinition race = null)
        {
            var checks = new List<PreparationCheck>();
            if (session == null) { checks.Add(new PreparationCheck(false, "Load a campaign from Main Menu.")); return checks; }
            checks.Add(new PreparationCheck(session.CareerRun?.IsActive == true, session.CareerRun?.IsActive == true
                ? "Player racer is active." : "Create a new player racer in Career Home."));
            var team = session.PlayerTeam;
            var partner = team.Roster.FindRacer(session.SelectedPartnerRacerId);
            bool partnerReady = partner?.CanRace == true && !partner.IsPlayerCharacter;
            checks.Add(new PreparationCheck(partnerReady, partnerReady ? "Partner: " + partner.Name
                : "Choose an active partner in Roster, recruit a replacement, or use Team Recovery."));
            var playerBike = team.Garage.FindBike(session.SelectedPlayerBikeId);
            var partnerBike = team.Garage.FindBike(session.SelectedPartnerBikeId);
            CheckBike(checks, "Player", playerBike, race);
            CheckBike(checks, "Partner", partnerBike, race);
            checks.Add(new PreparationCheck(playerBike != null && partnerBike != null && playerBike.BikeId != partnerBike.BikeId,
                "Player and partner must have separate owned bikes. Assign them in Garage."));
            if (race == null && playerBike?.IsRaceReady == true && partnerBike?.IsRaceReady == true)
                checks.Add(new PreparationCheck(playerBike.EngineClass == partnerBike.EngineClass,
                    "Player and partner engine classes must match. Change engines or assignments in Garage."));
            return checks;
        }
        private static void CheckBike(List<PreparationCheck> checks, string role, BikeState bike, RaceDefinition race)
        {
            bool ready = bike?.IsRaceReady == true;
            checks.Add(new PreparationCheck(ready, ready ? role + " bike is ready."
                : role + (bike == null ? " has no assigned bike." : bike.IsDestroyed ? " bike is destroyed." : " bike needs an intact engine and chassis.") +
                    " Use Garage, buy a complete bike in Shop, or review Team Recovery."));
            if (ready && race != null)
                checks.Add(new PreparationCheck(bike.EngineClass == race.EngineClass,
                    $"{role} engine: {bike.EngineClass}; event requires {race.EngineClass}. Change loadout in Garage or buy a matching bike in Shop."));
        }
    }
}
