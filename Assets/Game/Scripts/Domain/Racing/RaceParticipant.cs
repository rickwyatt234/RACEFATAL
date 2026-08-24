using System;
using RaceFatal.Career;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RaceParticipant
    {
        public RacerState Racer { get; }
        public RaceVehicleState Vehicle { get; }

        public BikeState Bike => Vehicle.Bike;

        public RaceParticipantRole Role { get; }

        public RaceParticipantStatus Status {
            get;
            private set;
        }

        public int CompletedLaps {
            get;
            private set;
        }

        // Approximate progress through the current lap.
        // Expected range: 0 to 1.

        public float CourseProgress {
            get;
            private set;
        }


        // Assigned when the racer crosses the finish line
        // after completing all required laps.
        // Zero means the racer has not finished.
        public int FinishPosition {
            get;
            private set;
        }

        public string RacerId => Racer.RacerId;

        public string TeamId => Racer.TeamId;

        public RaceParticipant(
            RacerState racer,
            RaceVehicleState vehicle,
            RaceParticipantRole role)
        {
            Racer = racer
                ?? throw new ArgumentNullException(
                    nameof(racer));

            Vehicle = vehicle
                ?? throw new ArgumentNullException(
                    nameof(vehicle));

            Role = role;

            Status =
                RaceParticipantStatus.Ready;
        }

        internal void Start()
        {
            if (Status == RaceParticipantStatus.Ready)
                Status = RaceParticipantStatus.Racing;
        }

        internal void SetCourseProgress(float progress)
        {
            if (Status != RaceParticipantStatus.Racing)
                return;

            if (progress < 0f)
                progress = 0f;

            if (progress > 1f)
                progress = 1f;

            CourseProgress = progress;
        }

        internal void CompleteLap()
        {
            if (Status != RaceParticipantStatus.Racing)
                return;

            CompletedLaps++;
            CourseProgress = 0f;
        }

        internal void Finish(int position)
        {
            if (Status != RaceParticipantStatus.Racing)
                return;

            FinishPosition = position;
            Status = RaceParticipantStatus.Finished;
        }

        internal void Retire()
        {
            if (Status == RaceParticipantStatus.Racing)
                Status = RaceParticipantStatus.Retired;
        }

        internal void Destroy()
        {
            if (Status == RaceParticipantStatus.Finished)
                return;

            Status = RaceParticipantStatus.Destroyed;
        }
    }
}