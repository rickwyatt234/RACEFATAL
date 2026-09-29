using System;

namespace RaceFatal.Racing
{
    public enum DeathmatchVictoryMode
    {
        Individual,
        Team
    }

    public sealed class DeathmatchRules
    {
        public DeathmatchVictoryMode Mode { get; }
        public int AllowedWinners { get; }
        public bool ShareTimeoutTies { get; }
        public float TimeLimitSeconds { get; }
        public float MinimumSpeedKph { get; }
        public float StartGraceSeconds { get; }
        public float BelowSpeedGraceSeconds { get; }

        public DeathmatchRules(DeathmatchVictoryMode mode, int allowedWinners = 1, float timeLimitSeconds = 300, float minimumSpeedKph = 30, float startGraceSeconds = 15, float belowSpeedGraceSeconds = 8, bool shareTimeoutTies = false)
        {
            if (!Enum.IsDefined(typeof(DeathmatchVictoryMode), mode) || allowedWinners < 1 || !Finite(timeLimitSeconds) || timeLimitSeconds <= 0 || !Finite(minimumSpeedKph) || minimumSpeedKph < 0 || !Finite(startGraceSeconds) || startGraceSeconds < 0 || !Finite(belowSpeedGraceSeconds) || belowSpeedGraceSeconds <= 0)
                throw new ArgumentException("Invalid deathmatch rules.");
            ShareTimeoutTies = shareTimeoutTies;
            Mode = mode;
            AllowedWinners = allowedWinners;
            TimeLimitSeconds = timeLimitSeconds;
            MinimumSpeedKph = minimumSpeedKph;
            StartGraceSeconds = startGraceSeconds;
            BelowSpeedGraceSeconds = belowSpeedGraceSeconds;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public string Summary => $"{(Mode == DeathmatchVictoryMode.Team ? "TEAM SURVIVAL" : AllowedWinners == 1 ? "LAST RACER STANDING" : "INDIVIDUAL SURVIVAL")}\n" + $"WINNER SLOTS  {AllowedWinners} {(Mode == DeathmatchVictoryMode.Team ? "TEAMS" : "RACERS")}\n" + $"TIME LIMIT  {TimeLimitSeconds:0} SEC\nMINIMUM SPEED  {MinimumSpeedKph:0.#} KM/H\n" + $"START GRACE  {StartGraceSeconds:0.#} SEC / BELOW SPEED GRACE  {BelowSpeedGraceSeconds:0.#} SEC\n" + "Speed disqualification preserves racer and bike. Timeout: survivors, then eliminations. " + (ShareTimeoutTies ? "Ties share rank and can exceed winner slots." : "Ties resolve by course distance, then grid order; winner count stays capped.");
    }
}
