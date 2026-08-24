using System;
using RaceFatal.Shared;

namespace RaceFatal.Racing
{
    public class RaceDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string TrackId { get; set; }
        public EngineClass EngineClass { get; }
        public int LapCount { get; }
        public int EntrantCount { get; }
        public int TeamSize { get; }

        public RaceDefinition(
            string id, 
            string displayName, 
            string trackId,
            EngineClass engineClass, 
            int lapCount, 
            int entrantCount, 
            int teamSize)
        {
            if (lapCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lapCount), "Lap count must be greater than zero.");
            }
            if (entrantCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entrantCount), "Entrant count must be greater than zero.");
            }
            if (teamSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(teamSize), "Team size must be greater than zero.");
            }
            if (entrantCount % teamSize != 0)
            {
                throw new ArgumentException("Entrant count must be divisible by team size.", nameof(entrantCount));
            }

            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            TrackId = trackId ?? throw new ArgumentNullException(nameof(trackId));

            EngineClass = engineClass;
            LapCount = lapCount;
            EntrantCount = entrantCount;
            TeamSize = teamSize;
        }
    }
}