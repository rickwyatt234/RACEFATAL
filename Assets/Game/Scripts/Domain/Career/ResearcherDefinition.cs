using System;
namespace RaceFatal.Career
{
    public sealed class ResearcherDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int CreditCost { get; }
        public int PointsPerRace { get; }
        public int Duration { get; }
        public ResearcherDefinition(string id, string name, int cost, int points, int duration)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Researcher ID is required.");
            Id = id; DisplayName = name; CreditCost = cost; PointsPerRace = points; Duration = duration;
        }
    }
    public sealed class ResearchContract
    {
        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int PointsPerRace { get; }
        public int RacesRemaining { get; private set; }
        public ResearchContract(string id, string name, int points, int remaining)
        {
            if (string.IsNullOrWhiteSpace(id) || points <= 0 || remaining < 0)
                throw new ArgumentException("Invalid research contract.");
            DefinitionId = id; DisplayName = name; PointsPerRace = points; RacesRemaining = remaining;
        }
        internal int Advance()
        {
            if (RacesRemaining == 0) return 0;
            RacesRemaining--;
            return PointsPerRace;
        }
    }
}
