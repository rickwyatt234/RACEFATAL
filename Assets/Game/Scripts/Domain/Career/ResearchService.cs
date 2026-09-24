using System;
using System.Collections.Generic;
using RaceFatal.Data;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class ResearchService
    {
        private readonly GameDatabase database;
        public ResearchService(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public Result CanResearch(TeamState team, string technologyId)
        {
            if (team == null) return Result.Failure("NO LOADED TEAM.");
            TechnologyDefinition technology = database.GetTechnologyDefinition(technologyId);
            if (technology == null) return Result.Failure("UNKNOWN TECHNOLOGY.");
            if (team.HasTechnology(technologyId)) return Result.Failure("ALREADY RESEARCHED.");
            Result content = ValidateGraph(technology, new HashSet<string>(), new HashSet<string>());
            if (!content.IsSuccess) return content;
            foreach (string prerequisite in technology.PrerequisiteTechnologyIds)
                if (!team.HasTechnology(prerequisite))
                    return Result.Failure("REQUIRES: " + database.GetTechnologyDefinition(prerequisite).DisplayName);
            if (team.ResearchPoints < technology.ResearchCost)
                return Result.Failure($"INSUFFICIENT RP. NEED {technology.ResearchCost - team.ResearchPoints:N0} MORE.");
            return Result.Success();
        }

        public Result Research(TeamState team, string technologyId)
        {
            Result allowed = CanResearch(team, technologyId);
            if (!allowed.IsSuccess) return allowed;
            int cost = database.GetTechnologyDefinition(technologyId).ResearchCost;
            if (!team.TrySpendResearchPoints(cost)) return Result.Failure("INSUFFICIENT RP.");
            if (!team.UnlockTechnology(technologyId))
            {
                team.AddResearchPoints(cost);
                return Result.Failure("TECHNOLOGY COULD NOT BE UNLOCKED.");
            }
            return Result.Success();
        }

        // Reject broken authoring before any currency changes, including indirect cycles.
        private Result ValidateGraph(TechnologyDefinition technology, HashSet<string> visiting, HashSet<string> visited)
        {
            if (visited.Contains(technology.Id)) return Result.Success();
            if (!visiting.Add(technology.Id)) return Result.Failure("INVALID TECHNOLOGY: CYCLIC PREREQUISITES.");
            if (technology.ResearchCost < 0) return Result.Failure("INVALID TECHNOLOGY: NEGATIVE RP COST.");
            foreach (string id in technology.PrerequisiteTechnologyIds)
            {
                TechnologyDefinition prerequisite = database.GetTechnologyDefinition(id);
                if (prerequisite == null) return Result.Failure("INVALID TECHNOLOGY: MISSING PREREQUISITE " + id);
                Result result = ValidateGraph(prerequisite, visiting, visited);
                if (!result.IsSuccess) return result;
            }
            visiting.Remove(technology.Id);
            visited.Add(technology.Id);
            return Result.Success();
        }
    }
}
