using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Racing
{
    public class RaceState
    {
        private readonly List<RaceParticipant> participants = new List<RaceParticipant>();
        public RaceDefinition RaceDefinition { get; }
        public IReadOnlyList<RaceParticipant> Participants => participants;
        public bool IsStarted { get; private set; }
        public bool IsFinished { get; private set; }

        public RaceState(RaceDefinition raceDefinition, IEnumerable<RaceParticipant> participants)
        {
            RaceDefinition = raceDefinition ?? throw new ArgumentNullException(nameof(raceDefinition));

            this.participants = participants.ToList() ?? throw new ArgumentNullException(nameof(participants));
        }

        internal void StartRace()
        {
            if (IsStarted)
            {
                throw new InvalidOperationException("Race has already started.");
            }
            foreach (var participant in participants)
            {
                participant.Start();
            }
            IsStarted = true;
        }

        internal void FinishRace()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException("Race has not started yet.");
            }
            IsFinished = true;
        }

        public RaceParticipant FindParticipant(string racerId)
        {
            return participants.FirstOrDefault(p => p.RacerId == racerId);
        }

        public IReadOnlyList<RaceParticipant> GetCurrentOrder()
        {
            return participants
                .OrderBy(GetStatusPriority)
                .ThenBy(p =>
                    p.Status ==
                    RaceParticipantStatus.Finished
                        ? p.FinishPosition
                        : int.MaxValue)
                .ThenByDescending(
                    p => p.CompletedLaps)
                .ThenByDescending(
                    p => p.CourseProgress)
                .ToList();
        }

        public int GetCurrentPosition(
            string racerId)
        {
            IReadOnlyList<RaceParticipant> order =
                GetCurrentOrder();

            for (int i = 0;
                 i < order.Count;
                 i++)
            {
                if (order[i].RacerId == racerId)
                    return i + 1;
            }

            return 0;
        }

        private static int GetStatusPriority(
            RaceParticipant participant)
        {
            return participant.Status switch
            {
                RaceParticipantStatus.Finished => 0,
                RaceParticipantStatus.Racing => 1,
                RaceParticipantStatus.Ready => 2,
                RaceParticipantStatus.Retired => 3,
                RaceParticipantStatus.Destroyed => 4,
                _ => 5
            };
        }
    }
}

