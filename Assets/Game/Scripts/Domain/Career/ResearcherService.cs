using System;
using System.Linq;
using RaceFatal.Data;
using RaceFatal.Shared;
namespace RaceFatal.Career
{
    public sealed class ResearcherService
    {
        private readonly GameDatabase database;
        public ResearcherService(GameDatabase database) { this.database = database ?? throw new ArgumentNullException(nameof(database)); }
        public Result CanHire(TeamState team, string id)
        {
            if (team == null) return Result.Failure("NO LOADED TEAM.");
            var offer = database.GetResearcherDefinition(id);
            if (offer == null) return Result.Failure("UNKNOWN RESEARCHER.");
            if (offer.CreditCost < 0 || offer.PointsPerRace <= 0 || offer.Duration <= 0)
                return Result.Failure("INVALID CONTRACT TERMS.");
            if (team.ResearchContracts.Any(c => c.DefinitionId == id && c.RacesRemaining > 0))
                return Result.Failure("THIS RESEARCHER ALREADY HAS AN ACTIVE CONTRACT.");
            if ((long)team.ResearchOutputPerRace + offer.PointsPerRace > int.MaxValue)
                return Result.Failure("RESEARCH OUTPUT LIMIT REACHED.");
            if (team.Credits < offer.CreditCost) return Result.Failure("INSUFFICIENT CREDITS.");
            return Result.Success();
        }
        public Result Hire(TeamState team, string id)
        {
            var check = CanHire(team, id);
            if (!check.IsSuccess) return check;
            var offer = database.GetResearcherDefinition(id);
            if (!team.TrySpendCredits(offer.CreditCost)) return Result.Failure("INSUFFICIENT CREDITS.");
            team.ReplaceResearchContract(new ResearchContract(id, offer.DisplayName, offer.PointsPerRace, offer.Duration));
            return Result.Success();
        }
    }
}
