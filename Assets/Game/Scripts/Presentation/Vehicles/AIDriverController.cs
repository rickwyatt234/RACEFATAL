using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(BikeMotor))]
    [RequireComponent(typeof(TrackSurfaceProbe))]
    [RequireComponent(typeof(AIRacerSensor))]
    [RequireComponent(typeof(AIOvertakePlanner))]
    [RequireComponent(typeof(AIBoostPlanner))]
    [RequireComponent(typeof(AIEnergyStripPlanner))]
    public class AIDriverController : MonoBehaviour
    {
        #region Path Following

        [Header("Path Following")]
        [Min(1f)][SerializeField] private float baseLookAheadDistance = 18f;
        [Min(0f)][SerializeField] private float speedLookAheadMultiplier = 0.4f;
        [Min(1f)][SerializeField] private float maximumLookAheadDistance = 55f;
        [Min(1)][SerializeField] private int pathSearchRadius = 20;

        #endregion

        #region Track Width

        [Header("Track Width")]
        [Min(0.5f)][SerializeField] private float usableTrackHalfWidth = 6f;
        [Min(0f)][SerializeField] private float wallSafetyMargin = 1.25f;
        [Min(0f)][SerializeField] private float boundaryRecoveryMargin = 0.5f;
        [Range(0f, 1f)][SerializeField] private float boundaryRecoveryTargetAmount = 0.4f;
        [Min(0.1f)][SerializeField] private float boundaryRecoveryShiftSpeed = 4f;

        #endregion

        #region Racing Line

        [Header("Racing Line")]
        [Min(5f)][SerializeField] private float cornerPreviewDistance = 90f;
        [Min(1f)][SerializeField] private float activeCornerProbeDistance = 25f;
        [Range(0.1f, 30f)][SerializeField] private float cornerStartAngle = 5f;
        [Range(1f, 90f)][SerializeField] private float fullCornerAngle = 35f;
        [Range(0f, 1f)][SerializeField] private float outsideEntryAmount = 0.65f;
        [Range(0f, 1f)][SerializeField] private float insideApexAmount = 0.55f;
        [Min(0.1f)][SerializeField] private float maximumLateralShiftSpeed = 2f;
        [Min(0.1f)][SerializeField] private float cornerSignalResponse = 2.5f;
        [Min(0f)][SerializeField] private float straightCenteringSpeed = 0f;

        #endregion

        #region Overtaking

        [Header("Overtaking")]
        [Range(0f, 1f)][SerializeField] private float maximumCornerSeverityForNewPass = 0.3f;

        #endregion

        #region Pace Variation

        [Header("Prototype Pace Variation")]
        [Range(0.8f, 1f)][SerializeField] private float minimumPaceMultiplier = 0.97f;
        [Range(0.8f, 1f)][SerializeField] private float maximumPaceMultiplier = 1f;

        #endregion

        #region Steering

        [Header("Steering")]
        [Min(1f)][SerializeField] private float fullSteeringAngle = 30f;
        [Range(0f, 10f)][SerializeField] private float steeringDeadZone = 0.75f;
        [Min(0.01f)][SerializeField] private float steeringSmoothTime = 0.15f;
        [Min(0.1f)][SerializeField] private float maximumSteeringChangeSpeed = 6f;

        #endregion

        #region Corner Speed Planning

        [Header("Corner Speed Planning")]
        [Min(10f)][SerializeField] private float cornerScanDistance = 140f;
        [Min(1f)][SerializeField] private float cornerSampleSpacing = 10f;
        [Min(1f)][SerializeField] private float maximumCornerAcceleration = 25f;
        [Range(0.1f, 1f)][SerializeField] private float cornerSpeedSafetyFactor = 0.95f;
        [Range(0f, 15f)][SerializeField] private float minimumCurveAngle = 1.5f;
        [Min(1f)][SerializeField] private float minimumCornerSpeedKph = 80f;

        #endregion

        #region Braking

        [Header("Brake Planning")]
        [Min(0.1f)][SerializeField] private float plannedBrakeDeceleration = 25f;
        [Min(0f)][SerializeField] private float brakingSafetyDistance = 5f;
        [Min(0f)][SerializeField] private float brakeSpeedTolerance = 2f;

        #endregion

        #region Speed Control

        [Header("Speed Control")]
        [Min(0.1f)][SerializeField] private float fullThrottleSpeedDifference = 8f;
        [Min(0.1f)][SerializeField] private float fullBrakeSpeedDifference = 12f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private float currentProgress;
        [SerializeField] private int currentSegment = -1;

        [SerializeField] private float currentThrottle;
        [SerializeField] private float currentBrake;
        [SerializeField] private float currentSteering;

        [SerializeField] private float debugCrossTrackError;
        [SerializeField] private float debugBaseLateralOffset;
        [SerializeField] private float debugOvertakeOffset;
        [SerializeField] private float debugEnergyStripOffset;
        [SerializeField] private float debugFinalLateralOffset;
        [SerializeField] private float debugRawLateralTarget;

        [SerializeField] private float debugLookAheadDistance;
        [SerializeField] private float debugSteeringAngle;
        [SerializeField] private float debugDesiredSteering;

        [SerializeField] private float debugUpcomingCornerAngle;
        [SerializeField] private float debugActiveCornerAngle;
        [SerializeField] private float debugSmoothedUpcomingAngle;
        [SerializeField] private float debugSmoothedActiveAngle;
        [SerializeField] private float debugCornerSeverity;
        [SerializeField] private string debugRacingState;

        [SerializeField] private float debugPaceMultiplier;
        [SerializeField] private string debugOvertakeState;
        [SerializeField] private string debugTacticalState;
        [SerializeField] private float debugTrafficSpeedLimitKph;

        [SerializeField] private bool debugBoostActive;
        [SerializeField] private float debugSpeedMultiplier;
        [SerializeField] private float debugAccelerationMultiplier;

        [SerializeField] private float debugTargetSpeedKph;
        [SerializeField] private float debugLimitingCurveSpeedKph;
        [SerializeField] private float debugLimitingCurveDistance;
        [SerializeField] private float debugTightestRadius;
        [SerializeField] private float debugMaximumCurveAngle;

        #endregion

        #region Debug Drawing

        [Header("Debug Drawing")]
        [SerializeField] private bool drawDebugTarget = true;

        #endregion

        #region Runtime

        private BikeMotor motor;
        private TrackSurfaceProbe surfaceProbe;
        private AIRacerSensor racerSensor;
        private AIOvertakePlanner overtakePlanner;
        private AIBoostPlanner boostPlanner;
        private AIEnergyStripPlanner energyStripPlanner;

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;
        private TrackProgressPath progressPath;

        private Vector3 nearestPathPoint;
        private Vector3 currentPathForward;
        private Vector3 pursuitTarget;

        private float desiredLateralOffset;
        private float finalLateralOffset;
        private float smoothedUpcomingAngle;
        private float smoothedActiveAngle;
        private float steeringSmoothVelocity;
        private float paceMultiplier = 1f;

        private bool laneInitialized;
        private bool initialized;

        #endregion

        #region Public

        public bool IsInitialized => initialized;
        public float CurrentProgress => currentProgress;
        public float CurrentThrottle => currentThrottle;
        public float CurrentBrake => currentBrake;
        public float CurrentSteering => currentSteering;
        public float CrossTrackError => debugCrossTrackError;
        public float CurrentCornerSeverity => debugCornerSeverity;
        public float TargetSpeedKph => debugTargetSpeedKph;

        public bool IsOvertaking =>
            overtakePlanner != null &&
            (overtakePlanner.CurrentState == AIOvertakePlanner.OvertakeState.PassingLeft ||
             overtakePlanner.CurrentState == AIOvertakePlanner.OvertakeState.PassingRight);

        #endregion

        #region Unity

        private void Awake()
        {
            motor = GetComponent<BikeMotor>();
            surfaceProbe = GetComponent<TrackSurfaceProbe>();
            racerSensor = GetComponent<AIRacerSensor>();
            overtakePlanner = GetComponent<AIOvertakePlanner>();
            boostPlanner = GetComponent<AIBoostPlanner>();
            energyStripPlanner = GetComponent<AIEnergyStripPlanner>();
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;

            if (raceRuntime == null ||
                !raceRuntime.HasStarted)
            {
                boostPlanner.StopBoost();
                ApplyEquipmentModifiers();
                motor.SetControls(0f, 0f, 0f);
                return;
            }

            if (progressPath == null ||
                progressPath.TotalLength <= 0f)
            {
                boostPlanner.StopBoost();
                ApplyEquipmentModifiers();
                motor.SetControls(0f, 1f, 0f);
                return;
            }

            UpdatePathState();
            UpdateRacingLine();
            UpdateTacticalLine();
            UpdatePursuitTarget();
            CalculateSteering();
            CalculateSpeedControl();

            bool trafficLimited =
                !float.IsPositiveInfinity(
                    overtakePlanner
                        .TrafficSpeedLimitMetersPerSecond);

            bool boostChanged =
                boostPlanner.UpdatePlan(
                    debugCornerSeverity,
                    currentBrake,
                    motor.SpeedMetersPerSecond,
                    trafficLimited,
                    IsOvertaking,
                    energyStripPlanner.IsTargetingStrip);

            ApplyEquipmentModifiers();

            if (boostChanged)
                CalculateSpeedControl();

            motor.SetControls(
                currentThrottle,
                currentBrake,
                currentSteering);
        }

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
                    $"{nameof(AIDriverController)} requires a RaceParticipant.",
                    this);

                return false;
            }

            if (runtime == null)
            {
                Debug.LogError(
                    $"{nameof(AIDriverController)} requires a RaceRuntimeController.",
                    this);

                return false;
            }

            if (path == null)
            {
                Debug.LogError(
                    $"{nameof(AIDriverController)} requires a TrackProgressPath.",
                    this);

                return false;
            }

            if (motor == null ||
                surfaceProbe == null ||
                racerSensor == null ||
                overtakePlanner == null ||
                boostPlanner == null ||
                energyStripPlanner == null)
            {
                Debug.LogError(
                    $"{nameof(AIDriverController)} is missing required bike components.",
                    this);

                return false;
            }

            participant = raceParticipant;
            raceRuntime = runtime;
            progressPath = path;

            currentSegment = -1;
            currentProgress = 0f;

            desiredLateralOffset = 0f;
            finalLateralOffset = 0f;
            smoothedUpcomingAngle = 0f;
            smoothedActiveAngle = 0f;
            steeringSmoothVelocity = 0f;

            laneInitialized = false;

            if (participant.Vehicle == null ||
                participant.Vehicle.Performance == null)
            {
                Debug.LogError(
                    $"AI racer '{participant.RacerId}' does not have vehicle performance.",
                    this);

                return false;
            }

            racerSensor.SetIdentity(
                participant.RacerId);

            paceMultiplier =
                CalculateDeterministicPace(
                    participant.RacerId);

            debugPaceMultiplier =
                paceMultiplier;

            motor.SetPerformance(
                participant.Vehicle.Performance);

            if (!boostPlanner.Initialize(
                    participant,
                    raceRuntime))
            {
                Debug.LogError(
                    $"Boost planner initialization failed for AI racer '{participant.RacerId}'.",
                    this);

                return false;
            }

            if (!energyStripPlanner.Initialize(
                    participant,
                    raceRuntime,
                    progressPath))
            {
                Debug.LogError(
                    $"Energy Strip planner initialization failed for AI racer '{participant.RacerId}'.",
                    this);

                return false;
            }

            ApplyEquipmentModifiers();

            motor.SetControls(
                0f,
                0f,
                0f);

            initialized = true;
            return true;
        }

        #endregion

        #region Path State

        private void UpdatePathState()
        {
            currentProgress =
                progressPath.GetProgress(
                    transform.position,
                    currentSegment,
                    pathSearchRadius,
                    out currentSegment);

            nearestPathPoint =
                progressPath.GetPositionAtProgress(
                    currentProgress);

            currentPathForward =
                progressPath.GetForwardAtProgress(
                    currentProgress);

            Vector3 surfaceNormal =
                GetSurfaceNormal();

            Vector3 pathForward =
                ProjectDirectionToSurface(
                    currentPathForward,
                    surfaceNormal,
                    transform.forward);

            Vector3 pathRight =
                Vector3.Cross(
                    surfaceNormal,
                    pathForward).normalized;

            Vector3 centerToBike =
                Vector3.ProjectOnPlane(
                    transform.position -
                    nearestPathPoint,
                    surfaceNormal);

            debugCrossTrackError =
                Vector3.Dot(
                    centerToBike,
                    pathRight);

            if (!laneInitialized)
            {
                float availableHalfWidth =
                    Mathf.Max(
                        0f,
                        usableTrackHalfWidth -
                        wallSafetyMargin);

                desiredLateralOffset =
                    Mathf.Clamp(
                        debugCrossTrackError,
                        -availableHalfWidth,
                        availableHalfWidth);

                finalLateralOffset =
                    desiredLateralOffset;

                laneInitialized = true;
            }

            debugLookAheadDistance =
                baseLookAheadDistance +
                motor.SpeedMetersPerSecond *
                speedLookAheadMultiplier;

            debugLookAheadDistance =
                Mathf.Clamp(
                    debugLookAheadDistance,
                    baseLookAheadDistance,
                    maximumLookAheadDistance);
        }

        #endregion

        #region Racing Line

        private void UpdateRacingLine()
        {
            Vector3 surfaceNormal =
                GetSurfaceNormal();

            float availableHalfWidth =
                Mathf.Max(
                    0f,
                    usableTrackHalfWidth -
                    wallSafetyMargin);

            float boundaryLimit =
                availableHalfWidth +
                boundaryRecoveryMargin;

            float upcomingProgress =
                progressPath.AdvanceProgressByDistance(
                    currentProgress,
                    cornerPreviewDistance);

            float activeProgress =
                progressPath.AdvanceProgressByDistance(
                    currentProgress,
                    activeCornerProbeDistance);

            Vector3 upcomingForward =
                progressPath.GetForwardAtProgress(
                    upcomingProgress);

            Vector3 activeForward =
                progressPath.GetForwardAtProgress(
                    activeProgress);

            debugUpcomingCornerAngle =
                GetLateralTurnAngle(
                    currentPathForward,
                    upcomingForward,
                    surfaceNormal);

            debugActiveCornerAngle =
                GetLateralTurnAngle(
                    currentPathForward,
                    activeForward,
                    surfaceNormal);

            float signalSmoothing =
                1f -
                Mathf.Exp(
                    -cornerSignalResponse *
                    Time.fixedDeltaTime);

            smoothedUpcomingAngle =
                Mathf.Lerp(
                    smoothedUpcomingAngle,
                    debugUpcomingCornerAngle,
                    signalSmoothing);

            smoothedActiveAngle =
                Mathf.Lerp(
                    smoothedActiveAngle,
                    debugActiveCornerAngle,
                    signalSmoothing);

            debugSmoothedUpcomingAngle =
                smoothedUpcomingAngle;

            debugSmoothedActiveAngle =
                smoothedActiveAngle;

            float upcomingSeverity =
                GetCornerSeverity(
                    smoothedUpcomingAngle);

            float activeSeverity =
                GetCornerSeverity(
                    smoothedActiveAngle);

            debugCornerSeverity =
                Mathf.Max(
                    upcomingSeverity,
                    activeSeverity);

            float rawTarget =
                desiredLateralOffset;

            float shiftSpeed =
                maximumLateralShiftSpeed;

            if (Mathf.Abs(
                    debugCrossTrackError) >
                boundaryLimit)
            {
                float side =
                    Mathf.Sign(
                        debugCrossTrackError);

                rawTarget =
                    side *
                    availableHalfWidth *
                    boundaryRecoveryTargetAmount;

                shiftSpeed =
                    boundaryRecoveryShiftSpeed;

                debugRacingState =
                    "Boundary Recovery";
            }
            else if (activeSeverity > 0.05f)
            {
                float direction =
                    Mathf.Sign(
                        smoothedActiveAngle);

                rawTarget =
                    direction *
                    availableHalfWidth *
                    insideApexAmount *
                    activeSeverity;

                debugRacingState =
                    "Corner / Apex";
            }
            else if (upcomingSeverity > 0.05f)
            {
                float direction =
                    Mathf.Sign(
                        smoothedUpcomingAngle);

                rawTarget =
                    -direction *
                    availableHalfWidth *
                    outsideEntryAmount *
                    upcomingSeverity;

                debugRacingState =
                    "Corner Setup";
            }
            else
            {
                debugRacingState =
                    "Straight / Hold Lane";

                if (straightCenteringSpeed > 0f)
                {
                    rawTarget =
                        Mathf.MoveTowards(
                            desiredLateralOffset,
                            0f,
                            straightCenteringSpeed *
                            Time.fixedDeltaTime);
                }
            }

            rawTarget =
                Mathf.Clamp(
                    rawTarget,
                    -availableHalfWidth,
                    availableHalfWidth);

            debugRawLateralTarget =
                rawTarget;

            desiredLateralOffset =
                Mathf.MoveTowards(
                    desiredLateralOffset,
                    rawTarget,
                    shiftSpeed *
                    Time.fixedDeltaTime);

            debugBaseLateralOffset =
                desiredLateralOffset;
        }

        private float GetCornerSeverity(
            float angle)
        {
            return Mathf.InverseLerp(
                cornerStartAngle,
                fullCornerAngle,
                Mathf.Abs(angle));
        }

        #endregion

        #region Tactical Racing

        private void UpdateTacticalLine()
        {
            float availableHalfWidth =
                Mathf.Max(
                    0f,
                    usableTrackHalfWidth -
                    wallSafetyMargin);

            /*
             * If already committed to an Energy Strip, don't
             * initiate a fresh overtake until the strip objective
             * has completed.
             */
            bool allowNewPass =
                debugCornerSeverity <=
                    maximumCornerSeverityForNewPass &&
                !energyStripPlanner.IsTargetingStrip;

            float overtakeOffset =
                overtakePlanner.UpdatePlan(
                    desiredLateralOffset,
                    availableHalfWidth,
                    motor.SpeedMetersPerSecond,
                    allowNewPass);

            bool passing =
                IsOvertaking;

            float energyOffset =
                energyStripPlanner.UpdatePlan(
                    currentProgress,
                    desiredLateralOffset,
                    availableHalfWidth,
                    debugCornerSeverity,
                    passing);

            float tacticalOffset;

            if (energyStripPlanner.IsTargetingStrip &&
                !passing)
            {
                tacticalOffset =
                    energyOffset;

                debugTacticalState =
                    "Energy Strip";
            }
            else
            {
                tacticalOffset =
                    overtakeOffset;

                debugTacticalState =
                    passing
                        ? "Overtake"
                        : "Normal";
            }

            finalLateralOffset =
                Mathf.Clamp(
                    desiredLateralOffset +
                    tacticalOffset,
                    -availableHalfWidth,
                    availableHalfWidth);

            debugOvertakeOffset =
                overtakeOffset;

            debugEnergyStripOffset =
                energyOffset;

            debugFinalLateralOffset =
                finalLateralOffset;

            debugOvertakeState =
                overtakePlanner
                    .CurrentState
                    .ToString();

            debugTrafficSpeedLimitKph =
                float.IsPositiveInfinity(
                    overtakePlanner
                        .TrafficSpeedLimitMetersPerSecond)
                    ? 0f
                    : overtakePlanner
                        .TrafficSpeedLimitMetersPerSecond *
                      3.6f;
        }

        #endregion

        #region Pursuit Target

        private void UpdatePursuitTarget()
        {
            Vector3 surfaceNormal =
                GetSurfaceNormal();

            float targetProgress =
                progressPath.AdvanceProgressByDistance(
                    currentProgress,
                    debugLookAheadDistance);

            Vector3 targetCenter =
                progressPath.GetPositionAtProgress(
                    targetProgress);

            Vector3 targetForward =
                progressPath.GetForwardAtProgress(
                    targetProgress);

            targetForward =
                ProjectDirectionToSurface(
                    targetForward,
                    surfaceNormal,
                    currentPathForward);

            Vector3 targetRight =
                Vector3.Cross(
                    surfaceNormal,
                    targetForward);

            if (targetRight.sqrMagnitude <
                0.001f)
            {
                targetRight =
                    Vector3.Cross(
                        surfaceNormal,
                        transform.forward);
            }

            targetRight.Normalize();

            pursuitTarget =
                targetCenter +
                targetRight *
                finalLateralOffset;
        }

        #endregion

        #region Steering

        private void CalculateSteering()
        {
            Vector3 surfaceNormal =
                GetSurfaceNormal();

            Vector3 bikeForward =
                ProjectDirectionToSurface(
                    transform.forward,
                    surfaceNormal,
                    currentPathForward);

            Vector3 targetDirection =
                Vector3.ProjectOnPlane(
                    pursuitTarget -
                    transform.position,
                    surfaceNormal);

            if (targetDirection.sqrMagnitude <
                0.001f)
            {
                debugDesiredSteering = 0f;
            }
            else
            {
                targetDirection.Normalize();

                debugSteeringAngle =
                    Vector3.SignedAngle(
                        bikeForward,
                        targetDirection,
                        surfaceNormal);

                if (Mathf.Abs(
                        debugSteeringAngle) <=
                    steeringDeadZone)
                {
                    debugDesiredSteering = 0f;
                }
                else
                {
                    debugDesiredSteering =
                        Mathf.Clamp(
                            debugSteeringAngle /
                            Mathf.Max(
                                1f,
                                fullSteeringAngle),
                            -1f,
                            1f);
                }
            }

            currentSteering =
                Mathf.SmoothDamp(
                    currentSteering,
                    debugDesiredSteering,
                    ref steeringSmoothVelocity,
                    steeringSmoothTime,
                    maximumSteeringChangeSpeed,
                    Time.fixedDeltaTime);
        }

        #endregion

        #region Speed Control

        private void CalculateSpeedControl()
        {
            float physicalMaximumSpeed =
                participant.Vehicle
                    .Performance
                    .TopSpeedMetersPerSecond;

            float equipmentSpeedMultiplier =
                participant.Vehicle
                    .EquipmentSystem
                    .SpeedMultiplier;

            float desiredMaximumSpeed =
                physicalMaximumSpeed *
                paceMultiplier *
                equipmentSpeedMultiplier;

            float targetSpeed =
                CalculateAllowedSpeedNow(
                    desiredMaximumSpeed,
                    GetSurfaceNormal());

            if (!float.IsPositiveInfinity(
                    overtakePlanner
                        .TrafficSpeedLimitMetersPerSecond))
            {
                targetSpeed =
                    Mathf.Min(
                        targetSpeed,
                        overtakePlanner
                            .TrafficSpeedLimitMetersPerSecond);
            }

            targetSpeed =
                Mathf.Clamp(
                    targetSpeed,
                    minimumCornerSpeedKph /
                    3.6f,
                    desiredMaximumSpeed);

            debugTargetSpeedKph =
                targetSpeed *
                3.6f;

            float speedDifference =
                targetSpeed -
                motor.SpeedMetersPerSecond;

            if (speedDifference >=
                -brakeSpeedTolerance)
            {
                currentBrake = 0f;

                currentThrottle =
                    Mathf.Clamp01(
                        Mathf.Max(
                            0f,
                            speedDifference) /
                        Mathf.Max(
                            0.1f,
                            fullThrottleSpeedDifference));
            }
            else
            {
                currentThrottle = 0f;

                float excessSpeed =
                    -speedDifference -
                    brakeSpeedTolerance;

                currentBrake =
                    Mathf.Clamp01(
                        excessSpeed /
                        Mathf.Max(
                            0.1f,
                            fullBrakeSpeedDifference));
            }
        }

        private float CalculateAllowedSpeedNow(
            float maximumSpeed,
            Vector3 surfaceNormal)
        {
            float allowedSpeedNow =
                maximumSpeed;

            debugTightestRadius =
                float.PositiveInfinity;

            debugMaximumCurveAngle = 0f;

            debugLimitingCurveSpeedKph =
                maximumSpeed * 3.6f;

            debugLimitingCurveDistance = 0f;

            Vector3 previousForward =
                progressPath.GetForwardAtProgress(
                    currentProgress);

            float previousDistance = 0f;

            for (float distance = cornerSampleSpacing;
                 distance <= cornerScanDistance;
                 distance += cornerSampleSpacing)
            {
                float sampleProgress =
                    progressPath.AdvanceProgressByDistance(
                        currentProgress,
                        distance);

                Vector3 nextForward =
                    progressPath.GetForwardAtProgress(
                        sampleProgress);

                float travelledDistance =
                    distance -
                    previousDistance;

                float signedAngle =
                    GetLateralTurnAngle(
                        previousForward,
                        nextForward,
                        surfaceNormal);

                float angleDegrees =
                    Mathf.Abs(
                        signedAngle);

                debugMaximumCurveAngle =
                    Mathf.Max(
                        debugMaximumCurveAngle,
                        angleDegrees);

                if (angleDegrees >=
                    minimumCurveAngle)
                {
                    float angleRadians =
                        angleDegrees *
                        Mathf.Deg2Rad;

                    float radius =
                        travelledDistance /
                        Mathf.Max(
                            0.001f,
                            angleRadians);

                    debugTightestRadius =
                        Mathf.Min(
                            debugTightestRadius,
                            radius);

                    float curveSpeed =
                        Mathf.Sqrt(
                            maximumCornerAcceleration *
                            radius);

                    curveSpeed *=
                        cornerSpeedSafetyFactor;

                    curveSpeed =
                        Mathf.Max(
                            curveSpeed,
                            minimumCornerSpeedKph /
                            3.6f);

                    float availableDistance =
                        Mathf.Max(
                            0f,
                            distance -
                            brakingSafetyDistance);

                    float allowedForCurve =
                        Mathf.Sqrt(
                            curveSpeed *
                            curveSpeed +
                            2f *
                            plannedBrakeDeceleration *
                            availableDistance);

                    if (allowedForCurve <
                        allowedSpeedNow)
                    {
                        allowedSpeedNow =
                            allowedForCurve;

                        debugLimitingCurveSpeedKph =
                            curveSpeed *
                            3.6f;

                        debugLimitingCurveDistance =
                            distance;
                    }
                }

                previousForward =
                    nextForward;

                previousDistance =
                    distance;
            }

            if (float.IsPositiveInfinity(
                    debugTightestRadius))
            {
                debugTightestRadius = 0f;
            }

            return Mathf.Min(
                allowedSpeedNow,
                maximumSpeed);
        }

        #endregion

        #region Equipment

        private void ApplyEquipmentModifiers()
        {
            if (participant?.Vehicle?.EquipmentSystem ==
                null)
            {
                return;
            }

            float speedMultiplier =
                participant.Vehicle
                    .EquipmentSystem
                    .SpeedMultiplier;

            float accelerationMultiplier =
                participant.Vehicle
                    .EquipmentSystem
                    .AccelerationMultiplier;

            float handlingMultiplier =
                participant.Vehicle
                    .EquipmentSystem
                    .HandlingMultiplier;

            motor.SetRuntimeModifiers(
                speedMultiplier,
                accelerationMultiplier,
                handlingMultiplier);

            debugBoostActive =
                participant.Vehicle
                    .EquipmentSystem
                    .IsBoosterActive;

            debugSpeedMultiplier =
                speedMultiplier;

            debugAccelerationMultiplier =
                accelerationMultiplier;
        }

        #endregion

        #region Pace

        private float CalculateDeterministicPace(
            string id)
        {
            float minimum =
                Mathf.Min(
                    minimumPaceMultiplier,
                    maximumPaceMultiplier);

            float maximum =
                Mathf.Max(
                    minimumPaceMultiplier,
                    maximumPaceMultiplier);

            uint hash =
                2166136261u;

            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0;
                     i < id.Length;
                     i++)
                {
                    hash ^= id[i];
                    hash *= 16777619u;
                }
            }

            float normalized =
                (hash & 0x00FFFFFFu) /
                16777215f;

            return Mathf.Lerp(
                minimum,
                maximum,
                normalized);
        }

        #endregion

        #region Direction Helpers

        private float GetLateralTurnAngle(
            Vector3 from,
            Vector3 to,
            Vector3 surfaceNormal)
        {
            Vector3 planarFrom =
                Vector3.ProjectOnPlane(
                    from,
                    surfaceNormal);

            Vector3 planarTo =
                Vector3.ProjectOnPlane(
                    to,
                    surfaceNormal);

            if (planarFrom.sqrMagnitude <
                    0.001f ||
                planarTo.sqrMagnitude <
                    0.001f)
            {
                return 0f;
            }

            planarFrom.Normalize();
            planarTo.Normalize();

            return Vector3.SignedAngle(
                planarFrom,
                planarTo,
                surfaceNormal);
        }

        private Vector3 ProjectDirectionToSurface(
            Vector3 direction,
            Vector3 surfaceNormal,
            Vector3 fallback)
        {
            Vector3 projected =
                Vector3.ProjectOnPlane(
                    direction,
                    surfaceNormal);

            if (projected.sqrMagnitude <
                0.001f)
            {
                projected =
                    Vector3.ProjectOnPlane(
                        fallback,
                        surfaceNormal);
            }

            if (projected.sqrMagnitude <
                0.001f)
            {
                projected =
                    transform.forward;
            }

            return projected.normalized;
        }

        private Vector3 GetSurfaceNormal()
        {
            Vector3 normal =
                surfaceProbe.HasSurface
                    ? surfaceProbe.SurfaceNormal
                    : surfaceProbe.LastSurfaceNormal;

            if (normal.sqrMagnitude <
                0.001f)
            {
                normal =
                    transform.up;
            }

            return normal.normalized;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugTarget ||
                !initialized)
            {
                return;
            }

            Gizmos.DrawLine(
                transform.position,
                nearestPathPoint);

            Gizmos.DrawWireSphere(
                nearestPathPoint,
                0.5f);

            Gizmos.DrawLine(
                transform.position,
                pursuitTarget);

            Gizmos.DrawWireSphere(
                pursuitTarget,
                0.8f);
        }

        #endregion
    }
}