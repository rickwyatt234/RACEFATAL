using System;
using RaceFatal.Data;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class RosterService
    {
        public const int RecruitmentCost = 5000;
        private readonly GameDatabase database;
        public RosterService(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public Result CanRecruit(TeamState team, string definitionId)
        {
            if (team == null) return Result.Failure("NO LOADED TEAM.");
            if (string.IsNullOrWhiteSpace(definitionId)) return Result.Failure("SELECT A RACER TO RECRUIT.");
            var definition = database.GetRacerDefinition(definitionId);
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.DisplayName))
                return Result.Failure("RACER IS NOT AVAILABLE.");
            if (team.Roster.FindRacer(definition.Id) != null)
                return Result.Failure("RACER ALREADY BELONGS TO YOUR TEAM.");
            if (team.Credits < RecruitmentCost)
                return Result.Failure($"REQUIRES {RecruitmentCost - team.Credits:N0} MORE CREDITS.");
            return Result.Success();
        }

        public Result Recruit(TeamState team, string definitionId)
        {
            Result allowed = CanRecruit(team, definitionId);
            if (!allowed.IsSuccess) return allowed;
            var definition = database.GetRacerDefinition(definitionId);
            // Authored racer ID doubles as the runtime AI content lookup key.
            var recruit = new RacerState(definition.Id, definition.DisplayName, team.TeamId, false);
            if (!team.TrySpendCredits(RecruitmentCost)) return Result.Failure("INSUFFICIENT CREDITS.");
            Result<RacerState> added = team.Roster.AddRacer(recruit);
            if (!added.IsSuccess)
            {
                team.AddCredits(RecruitmentCost);
                return Result.Failure(added.ErrorMessage);
            }
            return Result.Success();
        }

        public Result CanPurchasePerk(TeamState team, string racerId, string perkId)
        {
            if (team == null) return Result.Failure("NO LOADED TEAM.");
            RacerState racer = team.Roster.FindRacer(racerId);
            if (racer == null || !racer.CanRace || racer.TeamId != team.TeamId)
                return Result.Failure("SELECT AN ACTIVE TEAM RACER.");
            var perk = database.GetRacerPerkDefinition(perkId);
            if (perk == null) return Result.Failure("UNKNOWN PERK.");
            if (racer.IsPlayerCharacter && perk.Effect != RacerPerkEffect.EnergyCapacity)
                return Result.Failure("AI SKILL PERKS REQUIRE A TEAMMATE.");
            if (perk.FameCost < 0 || perk.Strength <= 0 || float.IsNaN(perk.Strength) || float.IsInfinity(perk.Strength)
                || !Enum.IsDefined(typeof(RacerPerkEffect), perk.Effect))
                return Result.Failure("INVALID PERK CONFIGURATION.");
            if (racer.Progression.HasPurchasedPerk(perkId)) return Result.Failure("ALREADY OWNED.");
            if (racer.Progression.Fame < perk.FameCost) return Result.Failure("NOT ENOUGH CHARACTER FAME.");
            return Result.Success();
        }

        public Result PurchasePerk(TeamState team, string racerId, string perkId)
        {
            var allowed = CanPurchasePerk(team, racerId, perkId);
            if (!allowed.IsSuccess) return allowed;
            var racer = team.Roster.FindRacer(racerId);
            return racer.Progression.TryPurchasePerk(perkId, database.GetRacerPerkDefinition(perkId).FameCost)
                ? Result.Success() : Result.Failure("PERK PURCHASE COULD NOT BE COMPLETED.");
        }
    }
}
