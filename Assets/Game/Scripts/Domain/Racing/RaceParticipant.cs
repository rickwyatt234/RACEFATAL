using System;
using RaceFatal.Career;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RaceParticipant
    {
        public RacerState Racer { get; }
        public RacerPerkBonuses Perks { get; }
        public RaceVehicleState Vehicle { get; }
        public BikeState Bike => Vehicle.Bike;
        public RaceParticipantRole Role { get; }
        public string RacerId => Racer.RacerId;
        public string TeamId => Racer.TeamId;
        public string TeamName { get; }

        private bool hasCourseSample;
        private float lastAudienceCourseSample;
        internal bool HasCourseSample => hasCourseSample;
        internal double AudienceCourseProgress { get; private set; }

        private float lapTraversalProgress;
        public float LapTraversalProgress => lapTraversalProgress;
        public RaceParticipantStatus Status { get; private set; }
        public int CompletedLaps { get; private set; }
        public int Eliminations { get; internal set; }
        public float EliminationTime { get; internal set; }
        public float BelowSpeedSeconds { get; internal set; }
        public float SpeedKph { get; internal set; }
        public string EliminationReason { get; internal set; }
        public bool DeathmatchWinner { get; internal set; }
        public int DeathmatchPosition { get; internal set; }
        public float CourseProgress { get; private set; }
        public int FinishPosition { get; private set; }
        public float? FinishTimeSeconds { get; private set; }
        public bool WasFastResolved { get; private set; }

        public RaceParticipant(RacerState racer, RaceVehicleState vehicle, RaceParticipantRole role, string teamName = null, RacerPerkBonuses perks = null)
        {
            Racer = racer ?? throw new ArgumentNullException(nameof(racer));
            Vehicle = vehicle ?? throw new ArgumentNullException(nameof(vehicle));
            Role = role;
            Perks = perks ?? new RacerPerkBonuses();
            TeamName = string.IsNullOrWhiteSpace(teamName) ? racer.TeamId : teamName;
            Status = RaceParticipantStatus.Ready;
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
            if (float.IsNaN(progress) || float.IsInfinity(progress))
                return;
            if (progress < 0f)
                progress = 0f;
            if (progress > 1f)
                progress = 1f;
            if (!hasCourseSample)
            {
                CourseProgress = progress;
                AudienceCourseProgress = progress >= 0.5f ? progress - 1f : progress;
                lastAudienceCourseSample = progress;
                hasCourseSample = true;
                return;
            }

            float delta = progress - CourseProgress;
            if (delta < -0.5f)
                delta += 1f;
            else if (delta > 0.5f)
                delta -= 1f;
            const float maximumAcceptedDelta = 0.15f;
            float audienceDelta = progress - lastAudienceCourseSample;
            if (audienceDelta < -0.5f)
                audienceDelta += 1f;
            else if (audienceDelta > 0.5f)
                audienceDelta -= 1f;
            if (Math.Abs(audienceDelta) <= maximumAcceptedDelta)
                AudienceCourseProgress += audienceDelta;
            lastAudienceCourseSample = progress;
            if (Math.Abs(delta) <= maximumAcceptedDelta)
            {
                lapTraversalProgress += delta;
                if (lapTraversalProgress < 0f)
                    lapTraversalProgress = 0f;
                if (lapTraversalProgress > 1.25f)
                    lapTraversalProgress = 1.25f;
            }

            CourseProgress = progress;
        }

        internal void ConfirmLapTraversal()
        {
            lapTraversalProgress -= 1f;
            if (lapTraversalProgress < 0f)
                lapTraversalProgress = 0f;
        }

        internal bool HasCompletedLapTraversal(float requiredProgress)
        {
            return lapTraversalProgress >= requiredProgress;
        }

        internal void CompleteLap()
        {
            if (Status != RaceParticipantStatus.Racing)
                return;
            CompletedLaps++;
            CourseProgress = 0f;
        }

        internal void Finish(int position, float? finishTimeSeconds, bool wasFastResolved)
        {
            if (Status != RaceParticipantStatus.Racing)
                return;
            FinishPosition = position;
            FinishTimeSeconds = finishTimeSeconds.HasValue ? Math.Max(0f, finishTimeSeconds.Value) : null;
            WasFastResolved = wasFastResolved;
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
