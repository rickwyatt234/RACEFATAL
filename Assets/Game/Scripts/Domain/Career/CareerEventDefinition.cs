using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    public enum CareerEventKind
    {
        Race,
        Championship,
        Deathmatch,
        Other
    }

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
        public int StarterOrder { get; }
        public int FirstWeek { get; }
        public int FirstDayOfWeek { get; }
        public int RepeatEveryWeeks { get; }
        public int RoundSpacingDays { get; }
        public int FirstAbsoluteDay => checked((FirstWeek - 1) * 7 + FirstDayOfWeek);
        public bool Supported => Kind == CareerEventKind.Race || Kind == CareerEventKind.Championship || Kind == CareerEventKind.Deathmatch || Kind == CareerEventKind.Other;

        public CareerEventDefinition(string id, string name, string description, CareerEventKind kind, int fame, int fee, IEnumerable<string> raceIds, IEnumerable<int> payouts, IEnumerable<int> prizes, IEnumerable<int> points, int firstWeek = 1, int firstDayOfWeek = 1, int repeatEveryWeeks = 5, int roundSpacingDays = 7, int starterOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Event ID is required.");
            StarterOrder = starterOrder;
            FirstWeek = firstWeek;
            FirstDayOfWeek = firstDayOfWeek;
            RepeatEveryWeeks = repeatEveryWeeks;
            RoundSpacingDays = roundSpacingDays;
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(name) ? id : name;
            Description = description ?? "";
            Kind = kind;
            RequiredFame = fame;
            EntryFee = fee;
            RaceIds = Array.AsReadOnly((raceIds ?? Array.Empty<string>()).ToArray());
            RacePayouts = Array.AsReadOnly((payouts ?? Array.Empty<int>()).ToArray());
            ChampionshipPrizes = Array.AsReadOnly((prizes ?? Array.Empty<int>()).ToArray());
            PositionPoints = Array.AsReadOnly((points ?? Array.Empty<int>()).ToArray());
        }
    }
}
