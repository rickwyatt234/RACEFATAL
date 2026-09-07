using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;
using System.Collections.Generic;

namespace RaceFatal.Presentation.Vehicles
{
    public class AIEnergyStripPlanner : MonoBehaviour
    {
        #region Energy Strategy

        [Header("Energy Strategy")]
        [Tooltip("AI begins considering Energy Strips below this Energy percentage.")]
        [Range(0f, 1f)][SerializeField] private float beginSeekingEnergyPercent = 0.65f;

        [Tooltip("Energy level considered urgent.")]
        [Range(0f, 1f)][SerializeField] private float urgentEnergyPercent = 0.30f;

        #endregion

        #region Search

        [Header("Strip Search")]
        [Tooltip("Maximum strip search distance when Energy has only recently become low.")]
        [Min(1f)][SerializeField] private float normalSearchDistance = 120f;

        [Tooltip("Maximum strip search distance when Energy is critically low.")]
        [Min(1f)][SerializeField] private float urgentSearchDistance = 240f;

        [Tooltip("A strip closer than this is normally considered too late to deliberately move toward.")]
        [Min(0f)][SerializeField] private float minimumApproachDistance = 15f;

        [Tooltip("At urgent Energy levels the AI may attempt strips this close.")]
        [Min(0f)][SerializeField] private float urgentMinimumApproachDistance = 5f;

        [Tooltip("New strip targets are not chosen during corners more severe than this.")]
        [Range(0f, 1f)][SerializeField] private float maximumCornerSeverityForNewTarget = 0.35f;

        #endregion

        #region Lateral Movement

        [Header("Lateral Movement")]
        [Tooltip("How quickly the tactical Energy Strip offset may change.")]
        [Min(0.1f)][SerializeField] private float lateralShiftSpeed = 2.5f;

        [Tooltip("Distance beyond the strip center before the target is considered completed.")]
        [Min(0f)][SerializeField] private float completionDistance = 12f;

        [Tooltip("Penalty applied to strips that require a large lateral movement.")]
        [Min(0f)][SerializeField] private float lateralTravelPenalty = 10f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugTargetingStrip;
        [SerializeField] private string debugTargetName;
        [SerializeField] private string debugDecision = "Not Initialized";

        [SerializeField] private float debugEnergyPercent;
        [SerializeField] private float debugUrgency;
        [SerializeField] private float debugSearchDistance;
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugTargetLateralOffset;
        [SerializeField] private float debugTacticalOffset;

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private TrackProgressPath progressPath;

        private EnergyStripAIAnchor targetStrip;

        private float targetProgress;
        private float targetLateralOffset;
        private float currentTacticalOffset;

        private bool initialized;

        #endregion

        #region Public

        public bool IsInitialized => initialized;
        public bool IsTargetingStrip => targetStrip != null;
        public float TacticalOffset => currentTacticalOffset;

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime,
            TrackProgressPath path)
        {
            if (raceParticipant == null)
            {
                Debug.LogError(
                    $"{nameof(AIEnergyStripPlanner)} requires a RaceParticipant.",
                    this);

                return false;
            }

            if (runtime == null)
            {
                Debug.LogError(
                    $"{nameof(AIEnergyStripPlanner)} requires a RaceRuntimeController.",
                    this);

                return false;
            }

            if (path == null)
            {
                Debug.LogError(
                    $"{nameof(AIEnergyStripPlanner)} requires a TrackProgressPath.",
                    this);

                return false;
            }

            if (raceParticipant.Vehicle?.EnergyPool == null)
            {
                Debug.LogError(
                    $"{nameof(AIEnergyStripPlanner)} requires a race vehicle with Energy.",
                    this);

                return false;
            }

            participant = raceParticipant;
            raceRuntime = runtime;
            progressPath = path;

            targetStrip = null;
            targetProgress = 0f;
            targetLateralOffset = 0f;
            currentTacticalOffset = 0f;

            initialized = true;
            debugInitialized = true;

            return true;
        }

        #endregion

        #region Planning

        public float UpdatePlan(
            float currentProgress,
            float baseLateralOffset,
            float availableHalfWidth,
            float cornerSeverity,
            bool passing)
        {
            if (!initialized)
                return 0f;

            UpdateEnergyDebug();

            if (!RaceIsActive() ||
                participant.Vehicle.IsDestroyed)
            {
                ClearTarget("Race Inactive");
                return ReturnOffsetToZero();
            }

            if (targetStrip != null)
            {
                if (!targetStrip.isActiveAndEnabled ||
                    !targetStrip.TryGetTrackData(
                        progressPath,
                        out targetProgress,
                        out targetLateralOffset))
                {
                    ClearTarget("Target Lost");
                    return ReturnOffsetToZero();
                }

                float signedDistance =
                    GetSignedDistance(
                        currentProgress,
                        targetProgress);

                debugTargetDistance =
                    signedDistance;

                if (signedDistance <
                    -completionDistance)
                {
                    ClearTarget("Strip Passed");
                    return ReturnOffsetToZero();
                }

                if (passing)
                {
                    ClearTarget("Overtake Priority");
                    return ReturnOffsetToZero();
                }

                return UpdateTargetOffset(
                    baseLateralOffset,
                    availableHalfWidth);
            }

            if (passing)
            {
                debugDecision = "Passing";
                return ReturnOffsetToZero();
            }

            if (debugEnergyPercent >
                beginSeekingEnergyPercent)
            {
                debugDecision = "Energy Sufficient";
                return ReturnOffsetToZero();
            }

            if (cornerSeverity >
                maximumCornerSeverityForNewTarget)
            {
                debugDecision = "Corner / Wait";
                return ReturnOffsetToZero();
            }

            float energyRange =
                Mathf.Max(
                    0.001f,
                    beginSeekingEnergyPercent -
                    urgentEnergyPercent);

            debugUrgency =
                Mathf.Clamp01(
                    (beginSeekingEnergyPercent -
                     debugEnergyPercent) /
                    energyRange);

            debugSearchDistance =
                Mathf.Lerp(
                    normalSearchDistance,
                    urgentSearchDistance,
                    debugUrgency);

            float minimumDistance =
                Mathf.Lerp(
                    minimumApproachDistance,
                    urgentMinimumApproachDistance,
                    debugUrgency);

            FindBestStrip(
                currentProgress,
                baseLateralOffset,
                availableHalfWidth,
                minimumDistance,
                debugSearchDistance);

            if (targetStrip == null)
            {
                debugDecision = "No Strip Ahead";
                return ReturnOffsetToZero();
            }

            debugDecision = "Strip Target Acquired";

            return UpdateTargetOffset(
                baseLateralOffset,
                availableHalfWidth);
        }

        private void FindBestStrip(
            float currentProgress,
            float baseLateralOffset,
            float availableHalfWidth,
            float minimumDistance,
            float maximumDistance)
        {
            EnergyStripAIAnchor bestStrip = null;

            float bestProgress = 0f;
            float bestLateralOffset = 0f;
            float bestScore = float.PositiveInfinity;

            IReadOnlyList<EnergyStripAIAnchor> strips =
                EnergyStripAIAnchor.ActiveAnchors;

            for (int i = 0;
                 i < strips.Count;
                 i++)
            {
                EnergyStripAIAnchor strip =
                    strips[i];

                if (strip == null ||
                    !strip.isActiveAndEnabled)
                {
                    continue;
                }

                if (!strip.TryGetTrackData(
                        progressPath,
                        out float stripProgress,
                        out float stripLateralOffset))
                {
                    continue;
                }

                float forwardDistance =
                    GetForwardDistance(
                        currentProgress,
                        stripProgress);

                if (forwardDistance <
                        minimumDistance ||
                    forwardDistance >
                        maximumDistance)
                {
                    continue;
                }

                float clampedLateral =
                    Mathf.Clamp(
                        stripLateralOffset,
                        -availableHalfWidth,
                        availableHalfWidth);

                float lateralTravel =
                    Mathf.Abs(
                        clampedLateral -
                        baseLateralOffset);

                float score =
                    forwardDistance +
                    lateralTravel *
                    lateralTravelPenalty;

                if (score >= bestScore)
                    continue;

                bestScore = score;

                bestStrip = strip;
                bestProgress = stripProgress;
                bestLateralOffset = clampedLateral;
            }

            if (bestStrip == null)
                return;

            targetStrip = bestStrip;
            targetProgress = bestProgress;
            targetLateralOffset = bestLateralOffset;

            debugTargetingStrip = true;
            debugTargetName = bestStrip.name;
        }

        private float UpdateTargetOffset(
            float baseLateralOffset,
            float availableHalfWidth)
        {
            float desiredStripOffset =
                Mathf.Clamp(
                    targetLateralOffset,
                    -availableHalfWidth,
                    availableHalfWidth);

            float desiredTacticalOffset =
                desiredStripOffset -
                baseLateralOffset;

            currentTacticalOffset =
                Mathf.MoveTowards(
                    currentTacticalOffset,
                    desiredTacticalOffset,
                    lateralShiftSpeed *
                    Time.fixedDeltaTime);

            debugTargetingStrip = true;
            debugTargetName =
                targetStrip != null
                    ? targetStrip.name
                    : string.Empty;

            debugTargetLateralOffset =
                desiredStripOffset;

            debugTacticalOffset =
                currentTacticalOffset;

            debugDecision = "Seeking Strip";

            return currentTacticalOffset;
        }

        private float ReturnOffsetToZero()
        {
            currentTacticalOffset =
                Mathf.MoveTowards(
                    currentTacticalOffset,
                    0f,
                    lateralShiftSpeed *
                    Time.fixedDeltaTime);

            debugTacticalOffset =
                currentTacticalOffset;

            return currentTacticalOffset;
        }

        private void ClearTarget(
            string reason)
        {
            targetStrip = null;
            targetProgress = 0f;
            targetLateralOffset = 0f;

            debugTargetingStrip = false;
            debugTargetName = string.Empty;
            debugTargetDistance = 0f;
            debugTargetLateralOffset = 0f;
            debugDecision = reason;
        }

        #endregion

        #region Distance

        private float GetForwardDistance(
            float fromProgress,
            float toProgress)
        {
            float delta =
                toProgress -
                fromProgress;

            if (delta < 0f)
                delta += 1f;

            return delta *
                progressPath.TotalLength;
        }

        private float GetSignedDistance(
            float fromProgress,
            float toProgress)
        {
            float delta =
                toProgress -
                fromProgress;

            if (delta > 0.5f)
                delta -= 1f;
            else if (delta < -0.5f)
                delta += 1f;

            return delta *
                progressPath.TotalLength;
        }

        #endregion

        #region Helpers

        private void UpdateEnergyDebug()
        {
            float maximum =
                participant.Vehicle
                    .EnergyPool
                    .MaxEnergy;

            debugEnergyPercent =
                maximum > 0f
                    ? participant.Vehicle
                        .EnergyPool
                        .CurrentEnergy /
                      maximum
                    : 0f;
        }

        private bool RaceIsActive()
        {
            if (raceRuntime == null ||
                !raceRuntime.HasStarted)
            {
                return false;
            }

            if (raceRuntime.Director?.State == null)
                return false;

            return !raceRuntime
                .Director
                .State
                .IsFinished;
        }

        #endregion

        #region Unity

        private void OnDisable()
        {
            ClearTarget("Disabled");
            currentTacticalOffset = 0f;
        }

        #endregion
    }
}