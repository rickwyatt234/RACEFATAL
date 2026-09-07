using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class TeamRosterState
    {
        private readonly List<RacerState>
            racers =
                new List<RacerState>();

        public IReadOnlyList<RacerState> Racers =>
            racers;

        public Result<RacerState> AddRacer(
            RacerState racer)
        {
            if (racer == null)
            {
                return Result<RacerState>.Failure(
                    "Racer is required.");
            }

            if (FindRacer(
                    racer.RacerId) != null)
            {
                return Result<RacerState>.Failure(
                    "Racer is already in this roster.");
            }

            racers.Add(racer);

            return Result<RacerState>.Success(racer);
        }

        public RacerState FindRacer(
            string racerId)
        {
            foreach (RacerState racer
                     in racers)
            {
                if (racer.RacerId ==
                    racerId)
                {
                    return racer;
                }
            }

            return null;
        }

        public IReadOnlyList<RacerState>
            GetRaceEligibleRacers()
        {
            var result =
                new List<RacerState>();

            foreach (RacerState racer
                     in racers)
            {
                if (racer.CanRace)
                {
                    result.Add(racer);
                }
            }

            return result;
        }
    }
}