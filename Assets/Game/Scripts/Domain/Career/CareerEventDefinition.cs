using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    public enum CareerEventKind { Race, Championship, Deathmatch }

    public sealed class CareerEventDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public CareerEventKind Kind { get; }
        public int RequiredFame { get; }
        public int EntryFee { get; }
        public IReadOnlyList<string> RaceIds { get; }
        public IReadOnlyList<int> RacePayouts { get; }
        public IReadOnlyList<int> ChampionshipPrizes { get; }
        public IReadOnlyList<int> PositionPoints { get; }
        public bool Supported => Kind == CareerEventKind.Race || Kind == CareerEventKind.Championship;

        public CareerEventDefinition(string id, string name, string description, CareerEventKind kind,
            int fame, int fee, IEnumerable<string> raceIds, IEnumerable<int> payouts,
            IEnumerable<int> prizes, IEnumerable<int> points)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Event ID is required.");
            Id = id; DisplayName = string.IsNullOrWhiteSpace(name) ? id : name; Description = description ?? "";
            Kind = kind; RequiredFame = fame; EntryFee = fee;
            RaceIds = Array.AsReadOnly((raceIds ?? Array.Empty<string>()).ToArray());
            RacePayouts = Array.AsReadOnly((payouts ?? Array.Empty<int>()).ToArray());
            ChampionshipPrizes = Array.AsReadOnly((prizes ?? Array.Empty<int>()).ToArray());
            PositionPoints = Array.AsReadOnly((points ?? Array.Empty<int>()).ToArray());
        }
    }
}
