using System;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;
using RaceFatal.Presentation.Combat;

namespace RaceFatal.Presentation.Vehicles
{
    [Serializable]
    public class AICombatPlanner
    {
        #region Targeting

        [Header("Targeting")]
        [Min(1f)][SerializeField] private float maximumTargetDistance = 150f;

        [Tooltip("Low-aggression racers use this fraction of Maximum Target Distance.")]
        [Range(0.1f, 1f)][SerializeField] private float lowAggressionTargetDistanceMultiplier = 0.6f;

        [Tooltip("Base firing cone used by ordinary forward weapons.")]
        [Range(0.1f, 45f)][SerializeField] private float firingHalfAngle = 6f;

        [Tooltip("Guided weapons are allowed a somewhat wider acquisition angle.")]
        [Range(1f, 4f)][SerializeField] private float guidedAngleMultiplier = 1.75f;

        [Tooltip("Charge weapons demand better alignment before the AI commits.")]
        [Range(0.1f, 1f)][SerializeField] private float chargeAngleMultiplier = 0.8f;

        [Min(0f)][SerializeField] private float minimumTargetDistance = 3f;

        #endregion

        #region Tactical Decisions

        [Header("Tactical Decisions")]
        [Tooltip("How often a racer reconsider its current weapon/target pairing.")]
        [Min(0.05f)][SerializeField] private float decisionInterval = 0.2f;

        [Tooltip("Minimum time the AI normally commits to a newly selected tactical decision.")]
        [Min(0f)][SerializeField] private float minimumDecisionCommitTime = 0.6f;

        [Tooltip("How much better another weapon must score before replacing the current weapon.")]
        [Range(0f, 1f)][SerializeField] private float weaponSwitchScoreAdvantage = 0.12f;

        [Tooltip("How much better another target must score before replacing the current target.")]
        [Range(0f, 1f)][SerializeField] private float targetSwitchScoreAdvantage = 0.08f;

        [Tooltip("Minimum tactical score required before the AI attacks.")]
        [Range(0f, 1f)][SerializeField] private float conservativeMinimumAttackScore = 0.48f;

        [Range(0f, 1f)][SerializeField] private float aggressiveMinimumAttackScore = 0.3f;

        #endregion

        #region Tactical Scoring

        [Header("Tactical Scoring")]
        [Min(0f)][SerializeField] private float distanceWeight = 0.34f;
        [Min(0f)][SerializeField] private float alignmentWeight = 0.34f;
        [Min(0f)][SerializeField] private float vulnerabilityWeight = 0.14f;
        [Min(0f)][SerializeField] private float finisherWeight = 0.18f;

        [Tooltip("Damage value treated as a very high-value finishing shot.")]
        [Min(1f)][SerializeField] private float damageForFullFinisherValue = 30f;

        #endregion

        #region Ammo Tactics

        [Header("Ammo Tactics")]
        [Tooltip("Weapons with this many rounds or more are treated as plentiful.")]
        [Min(1)][SerializeField] private int plentifulAmmoReference = 30;

        [Tooltip("Maximum scarcity penalty for cautious racers.")]
        [Range(0f, 1f)][SerializeField] private float conservativeScarcityPenalty = 0.45f;

        [Tooltip("Maximum scarcity penalty for highly aggressive racers.")]
        [Range(0f, 1f)][SerializeField] private float aggressiveScarcityPenalty = 0.08f;

        [Tooltip("Additional penalty as a scarce weapon approaches empty.")]
        [Range(0f, 1f)][SerializeField] private float lowAmmoPenalty = 0.25f;
        [Tooltip("Weapons at or below this starting ammo count are treated as scarce ordnance.")]
        [Min(1)][SerializeField] private int scarceAmmoThreshold = 8;

        [Tooltip("Base score multiplier applied to scarce ordnance against a healthy target.")]
        [Range(0.1f, 1f)][SerializeField] private float conservativeScarceWeaponFactor = 0.48f;

        [Range(0.1f, 1f)][SerializeField] private float aggressiveScarceWeaponFactor = 0.72f;

        [Tooltip("As a target becomes vulnerable, scarce weapons recover their full tactical value.")]
        [Range(0f, 1f)][SerializeField] private float vulnerabilityForFullScarceWeaponValue = 0.65f;

        #endregion

        #region Projectile Tactics

        [Header("Projectile Tactics")]
        [Tooltip("Penalty applied to long projectile travel times. Guided weapons receive a reduced version.")]
        [Min(0f)][SerializeField] private float projectileTravelTimePenalty = 0.12f;

        [Tooltip("How strongly long charge times reduce willingness to begin a charge attack.")]
        [Min(0f)][SerializeField] private float chargeDurationPenalty = 0.12f;

        [Tooltip("Minimum alignment score required before beginning a charge weapon.")]
        [Range(0f, 1f)][SerializeField] private float minimumChargeAlignment = 0.45f;

        #endregion

        #region Racing Conditions

        [Header("Racing Conditions")]
        [Range(0f, 1f)][SerializeField] private float conservativeCornerSeverity = 0.18f;
        [Range(0f, 1f)][SerializeField] private float maximumCornerSeverity = 0.35f;

        [SerializeField] private bool avoidFiringWhileBoosting = true;

        [Header("Race Pressure")]
        [Range(0f, 0.4f)][SerializeField] private float fullPressureAttackThresholdReduction = 0.12f;
        [Range(0f, 0.5f)][SerializeField] private float fullPressurePreferredTargetBonus = 0.22f;
        [Range(0f, 0.5f)][SerializeField] private float fullPressureTargetRangeBonus = 0.12f;
        [Range(0f, 0.25f)][SerializeField] private float fullPressureCornerToleranceBonus = 0.06f;

        #endregion

        #region Hold Weapons

        [Header("Hold Weapons")]
        [Min(0.05f)][SerializeField] private float minimumBurstDuration = 0.45f;
        [Min(0.05f)][SerializeField] private float maximumBurstDuration = 1f;

        [Min(0f)][SerializeField] private float minimumBurstCooldown = 0.35f;
        [Min(0f)][SerializeField] private float maximumBurstCooldown = 0.8f;

        #endregion

        #region Press Weapons

        [Header("Press Weapons")]
        [Min(0.05f)][SerializeField] private float pressWeaponCooldown = 1.25f;

        [Min(1f)][SerializeField] private float lowAggressionPressCooldownMultiplier = 1.5f;

        [Range(0.1f, 1f)]
        [SerializeField] private float highAggressionPressCooldownMultiplier = 0.65f;

        #endregion

        #region Weapon Role Bias
        [Header("Weapon Role Bias")]

        [Tooltip("Small bonus for Hold weapons when attacking relatively healthy racers.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float holdWeaponPressureBonus = 0.12f;

        [Tooltip("Press weapons become increasingly attractive as the target becomes vulnerable.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pressWeaponFinisherBonus = 0.12f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private string debugDecision = "Not Initialized";

        [SerializeField] private float debugWeaponAggression;
        [SerializeField] private float debugRuntimeTargetDistance;
        [SerializeField] private float debugRuntimeCornerSeverity;
        [SerializeField] private float debugRuntimeBurstDuration;
        [SerializeField] private float debugRuntimeBurstCooldown;
        [SerializeField] private float debugRuntimePressCooldown;
        [SerializeField] private float debugMinimumAttackScore;
        [SerializeField] private float debugRacePressure;
        [SerializeField] private string debugPreferredTarget = "None";

        [SerializeField] private string debugSelectedWeapon = "None";
        [SerializeField] private string debugActivationMode = "None";
        [SerializeField] private string debugDeliveryMode = "None";

        [SerializeField] private int debugAmmo;
        [SerializeField] private int debugMaximumAmmo;

        [SerializeField] private string debugTargetRacer = "None";
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugTargetAngle;

        [SerializeField] private float debugCurrentScore;
        [SerializeField] private float debugBestScore;
        [SerializeField] private float debugDistanceScore;
        [SerializeField] private float debugAlignmentScore;
        [SerializeField] private float debugVulnerabilityScore;
        [SerializeField] private float debugAmmoFactor;

        [SerializeField] private float debugDecisionTimer;
        [SerializeField] private float debugCommitTimer;

        [SerializeField] private bool debugWeaponActive;
        [SerializeField] private float debugBurstTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private float debugChargeTimer;

        #endregion

        #region Runtime
        
        private GuidedTargetLockState guidedLockState;

        private AIRacerSensor sensor;
        private RacerViewController racerView;

        private RaceParticipant participant;
        private RaceEquipmentSystem equipment;

        private RacerViewController currentTarget;
        private string currentEquipmentId;

        private bool initialized;
        private bool weaponActive;

        private float weaponAggression;

        private float burstTimer;
        private float cooldownTimer;
        private float chargeTimer;

        private float decisionTimer;
        private float commitTimer;

        private float runtimeTargetDistance;
        private float runtimeMaximumCornerSeverity;
        private float runtimeBurstDuration;
        private float runtimeBurstCooldown;
        private float runtimePressWeaponCooldown;
        private float runtimeMinimumAttackScore;

        private float currentRacePressure;
        private string preferredTargetRacerId;

        #endregion

        #region Tactical Opportunity

        private struct TacticalOpportunity
        {
            public bool Valid;

            public RaceWeaponSnapshot Weapon;
            public RacerViewController Target;

            public float Score;
            public float Distance;
            public float Angle;

            public float DistanceScore;
            public float AlignmentScore;
            public float VulnerabilityScore;
            public float AmmoFactor;
        }

        #endregion

        public bool IsInitialized =>
            initialized;

        public float WeaponAggression =>
            weaponAggression;

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            AIRacerSensor racerSensor,
            RacerViewController view,
            float aggression)
        {
            if (raceParticipant == null ||
                racerSensor == null ||
                view == null)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} is missing required runtime state.");

                return false;
            }

            if (raceParticipant.Role ==
                RaceParticipantRole.Player)
            {
                return false;
            }

            if (raceParticipant.Vehicle?.EquipmentSystem == null)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires a race vehicle with an equipment system.");

                return false;
            }

            if (!view.IsInitialized)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires an initialized " +
                    $"{nameof(RacerViewController)}.");

                return false;
            }

            participant =
                raceParticipant;

            sensor =
                racerSensor;

            racerView =
                view;
            
            guidedLockState = racerView.GetComponent<GuidedTargetLockState>();

            equipment =
                participant.Vehicle.EquipmentSystem;

            weaponAggression =
                Mathf.Clamp01(
                    aggression);

            runtimeTargetDistance =
                maximumTargetDistance *
                Mathf.Lerp(
                    lowAggressionTargetDistanceMultiplier,
                    1f,
                    weaponAggression);

            runtimeMaximumCornerSeverity =
                Mathf.Lerp(
                    conservativeCornerSeverity,
                    maximumCornerSeverity,
                    weaponAggression);

            runtimeBurstDuration =
                Mathf.Lerp(
                    minimumBurstDuration,
                    maximumBurstDuration,
                    weaponAggression);

            runtimeBurstCooldown =
                Mathf.Lerp(
                    maximumBurstCooldown,
                    minimumBurstCooldown,
                    weaponAggression);

            runtimePressWeaponCooldown =
                pressWeaponCooldown *
                Mathf.Lerp(
                    lowAggressionPressCooldownMultiplier,
                    highAggressionPressCooldownMultiplier,
                    weaponAggression);

            runtimeMinimumAttackScore =
                Mathf.Lerp(
                    conservativeMinimumAttackScore,
                    aggressiveMinimumAttackScore,
                    weaponAggression);

            burstTimer = 0f;
            cooldownTimer = 0f;
            chargeTimer = 0f;

            decisionTimer = 0f;
            commitTimer = 0f;

            weaponActive = false;
            currentTarget = null;
            currentEquipmentId = null;

            debugWeaponAggression =
                weaponAggression;

            debugRuntimeTargetDistance =
                runtimeTargetDistance;

            debugRuntimeCornerSeverity =
                runtimeMaximumCornerSeverity;

            debugRuntimeBurstDuration =
                runtimeBurstDuration;

            debugRuntimeBurstCooldown =
                runtimeBurstCooldown;

            debugRuntimePressCooldown =
                runtimePressWeaponCooldown;

            debugMinimumAttackScore =
                runtimeMinimumAttackScore;

            debugInitialized = true;
            initialized = true;

            debugDecision =
                equipment.HasUsableWeapon
                    ? "Ready"
                    : equipment.HasWeapon
                        ? "Out Of Ammo"
                        : "No Weapon";

            return true;
        }

        #endregion

        #region Tick

        public void Tick(
            float cornerSeverity,
            bool boosting,
            float racePressure,
            string preferredTargetId)
        {
            if (!initialized)
                return;

            currentRacePressure =
                Mathf.Clamp01(
                    racePressure);

            preferredTargetRacerId =
                preferredTargetId;

            debugRacePressure =
                currentRacePressure;

            debugPreferredTarget =
                string.IsNullOrWhiteSpace(
                    preferredTargetRacerId)
                    ? "None"
                    : preferredTargetRacerId;

            TickTimers();

            if (!CanConsiderCombat())
            {
                ClearDecision();
                CancelWeapon();
                return;
            }

            float effectiveCornerLimit =
                Mathf.Clamp01(
                    runtimeMaximumCornerSeverity +
                    fullPressureCornerToleranceBonus *
                    currentRacePressure);

            if (cornerSeverity >
                effectiveCornerLimit)
            {
                debugDecision =
                    "Corner / Hold Fire";

                CancelWeapon();
                return;
            }

            if (avoidFiringWhileBoosting &&
                boosting)
            {
                debugDecision =
                    "Boosting / Hold Fire";

                CancelWeapon();
                return;
            }

            if (equipment.SelectedWeaponIsEmpty &&
                weaponActive)
            {
                CancelWeapon();
            }

            /*
             * While actively firing a burst or charging, don't
             * casually switch weapon/target. Validate the current
             * attack first and let it continue if still viable.
             */
            if (weaponActive)
            {
                if (!TryGetCurrentOpportunity(
                        cornerSeverity,
                        out TacticalOpportunity activeOpportunity))
                {
                    debugDecision =
                        "Attack Opportunity Lost";

                    CancelWeapon();
                    ForceDecisionRefresh();

                    return;
                }

                ApplyDebugOpportunity(
                    activeOpportunity);

                UpdateWeapon(
                    activeOpportunity.Weapon.Definition);

                return;
            }

            bool currentValid =
                TryGetCurrentOpportunity(
                    cornerSeverity,
                    out TacticalOpportunity currentOpportunity);

            if (!currentValid)
            {
                ForceDecisionRefresh();
            }

            if (decisionTimer <= 0f)
            {
                TacticalOpportunity best =
                    FindBestOpportunity(
                        cornerSeverity);

                SelectDecision(
                    currentOpportunity,
                    currentValid,
                    best);

                decisionTimer =
                    decisionInterval;
            }

            if (!TryGetCurrentOpportunity(
                    cornerSeverity,
                    out TacticalOpportunity opportunity))
            {
                debugTargetRacer =
                    "None";

                debugTargetDistance =
                    0f;

                debugTargetAngle =
                    0f;

                debugCurrentScore =
                    0f;

                debugDecision =
                    "No Tactical Opportunity";

                return;
            }

            float effectiveAttackScore =
                Mathf.Max(
                    0f,
                    runtimeMinimumAttackScore -
                    fullPressureAttackThresholdReduction *
                    currentRacePressure);

            if (opportunity.Score <
                effectiveAttackScore)
            {
                ApplyDebugOpportunity(
                    opportunity);

                debugDecision =
                    "Opportunity Too Weak";

                return;
            }

            ApplyDebugOpportunity(
                opportunity);

            UpdateWeapon(
                opportunity.Weapon.Definition);
        }

        private void TickTimers()
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer =
                    Mathf.Max(
                        0f,
                        cooldownTimer -
                        Time.fixedDeltaTime);
            }

            if (decisionTimer > 0f)
            {
                decisionTimer =
                    Mathf.Max(
                        0f,
                        decisionTimer -
                        Time.fixedDeltaTime);
            }

            if (commitTimer > 0f)
            {
                commitTimer =
                    Mathf.Max(
                        0f,
                        commitTimer -
                        Time.fixedDeltaTime);
            }

            debugCooldownTimer =
                cooldownTimer;

            debugDecisionTimer =
                decisionTimer;

            debugCommitTimer =
                commitTimer;
        }

        #endregion

        #region Tactical Selection

        private TacticalOpportunity FindBestOpportunity(
            float cornerSeverity)
        {
            TacticalOpportunity best =
                default;

            float bestScore =
                float.NegativeInfinity;

            for (int weaponIndex = 0;
                 weaponIndex < equipment.WeaponCount;
                 weaponIndex++)
            {
                if (!equipment.TryGetWeaponSnapshot(
                        weaponIndex,
                        out RaceWeaponSnapshot weapon))
                {
                    continue;
                }

                if (!weapon.HasAmmo ||
                    weapon.Definition == null)
                {
                    continue;
                }

                if (!racerView.TryGetEquipmentMount(
                        weapon.EquipmentId,
                        out BikeEquipmentMountBinding mount))
                {
                    continue;
                }

                Transform origin =
                    mount.EquipmentOrigin != null
                        ? mount.EquipmentOrigin
                        : mount.MountRoot;

                if (origin == null)
                    continue;

                var racers =
                    AIRacerSensor.ActiveSensors;

                for (int racerIndex = 0;
                     racerIndex < racers.Count;
                     racerIndex++)
                {
                    AIRacerSensor candidateSensor =
                        racers[racerIndex];

                    if (!TryGetValidEnemy(
                            candidateSensor,
                            out RacerViewController candidate))
                    {
                        continue;
                    }

                    if (!TryScoreOpportunity(
                            weapon,
                            origin,
                            candidate,
                            cornerSeverity,
                            out TacticalOpportunity opportunity))
                    {
                        continue;
                    }

                    if (opportunity.Score <=
                        bestScore)
                    {
                        continue;
                    }

                    best =
                        opportunity;

                    bestScore =
                        opportunity.Score;
                }
            }

            debugBestScore =
                best.Valid
                    ? best.Score
                    : 0f;

            return best;
        }

        private void SelectDecision(
            TacticalOpportunity current,
            bool currentValid,
            TacticalOpportunity best)
        {
            if (!best.Valid)
            {
                if (!currentValid)
                    ClearDecision();

                return;
            }

            if (!currentValid)
            {
                ApplyDecision(
                    best);

                return;
            }

            bool sameWeapon =
                string.Equals(
                    current.Weapon.EquipmentId,
                    best.Weapon.EquipmentId,
                    StringComparison.Ordinal);

            bool sameTarget =
                current.Target ==
                best.Target;

            if (sameWeapon &&
                sameTarget)
            {
                return;
            }

            /*
             * During the initial commitment window we keep the
             * existing valid tactical plan. Invalid plans are
             * already discarded before reaching here.
             */
            if (commitTimer > 0f)
                return;

            float requiredAdvantage =
                0f;

            if (!sameWeapon)
            {
                requiredAdvantage =
                    Mathf.Max(
                        requiredAdvantage,
                        weaponSwitchScoreAdvantage);
            }

            if (!sameTarget)
            {
                requiredAdvantage =
                    Mathf.Max(
                        requiredAdvantage,
                        targetSwitchScoreAdvantage);
            }

            if (best.Score <
                current.Score +
                requiredAdvantage)
            {
                return;
            }

            ApplyDecision(
                best);
        }

        private void ApplyDecision(
            TacticalOpportunity opportunity)
        {
            if (!opportunity.Valid)
                return;

            if (!equipment.SelectWeapon(
                    opportunity.Weapon.EquipmentId))
            {
                debugDecision =
                    "Weapon Selection Failed";

                return;
            }

            currentEquipmentId =
                opportunity.Weapon.EquipmentId;

            currentTarget =
                opportunity.Target;

            commitTimer =
                minimumDecisionCommitTime;

            ApplyDebugOpportunity(
                opportunity);

            debugDecision =
                "Tactical Decision";
        }

        private void ClearDecision()
        {
            currentTarget =
                null;

            currentEquipmentId =
                null;

            commitTimer =
                0f;

            debugTargetRacer =
                "None";

            debugCurrentScore =
                0f;
            
            guidedLockState?.ClearTarget();
        }

        private void ForceDecisionRefresh()
        {
            decisionTimer =
                0f;

            commitTimer =
                0f;
        }

        #endregion

        #region Opportunity Scoring

        private bool TryGetCurrentOpportunity(
            float cornerSeverity,
            out TacticalOpportunity opportunity)
        {
            opportunity =
                default;

            if (currentTarget == null ||
                string.IsNullOrWhiteSpace(
                    currentEquipmentId))
            {
                return false;
            }

            if (!equipment.TryGetWeaponSnapshot(
                    currentEquipmentId,
                    out RaceWeaponSnapshot weapon))
            {
                return false;
            }

            if (!weapon.HasAmmo ||
                weapon.Definition == null)
            {
                return false;
            }

            if (!racerView.TryGetEquipmentMount(
                    weapon.EquipmentId,
                    out BikeEquipmentMountBinding mount))
            {
                return false;
            }

            Transform origin =
                mount.EquipmentOrigin != null
                    ? mount.EquipmentOrigin
                    : mount.MountRoot;

            if (origin == null)
                return false;

            if (!IsValidEnemy(
                    currentTarget))
            {
                return false;
            }

            return TryScoreOpportunity(
                weapon,
                origin,
                currentTarget,
                cornerSeverity,
                out opportunity);
        }

        private bool TryScoreOpportunity(
            RaceWeaponSnapshot weapon,
            Transform origin,
            RacerViewController target,
            float cornerSeverity,
            out TacticalOpportunity opportunity)
        {
            opportunity =
                default;

            WeaponDefinition definition =
                weapon.Definition;

            if (definition == null ||
                origin == null ||
                target == null)
            {
                return false;
            }

            /*
             * Dropped weapons require a different tactical model
             * because this planner currently evaluates forward
             * firing opportunities.
             */
            if (definition.DeliveryMode ==
                WeaponDeliveryMode.Dropped)
            {
                return false;
            }

            Vector3 toTarget =
                target.transform.position -
                origin.position;

            if (toTarget.sqrMagnitude <
                0.001f)
            {
                return false;
            }

            float distance =
                toTarget.magnitude;

            float pressureRange =
                runtimeTargetDistance *
                (1f +
                 fullPressureTargetRangeBonus *
                 currentRacePressure);

            float searchRange =
                Mathf.Min(
                    pressureRange,
                    definition.Range);

            if (distance <
                    minimumTargetDistance ||
                distance >
                    searchRange)
            {
                return false;
            }

            Vector3 direction =
                toTarget /
                distance;

            if (Vector3.Dot(
                    origin.forward,
                    direction) <= 0f)
            {
                return false;
            }

            float angle =
                Vector3.Angle(
                    origin.forward,
                    direction);

            float allowedAngle =
                GetAllowedFiringAngle(
                    definition);

            if (angle >
                allowedAngle)
            {
                return false;
            }

            float alignmentScore =
                1f -
                Mathf.Clamp01(
                    angle /
                    Mathf.Max(
                        0.01f,
                        allowedAngle));

            if (definition.ActivationMode ==
                    EquipmentActivationMode.ChargeRelease &&
                alignmentScore <
                    minimumChargeAlignment)
            {
                return false;
            }

            float normalizedDistance =
                Mathf.Clamp01(
                    distance /
                    Mathf.Max(
                        minimumTargetDistance + 0.01f,
                        searchRange));

            float idealRange =
                GetIdealRange(
                    definition);

            float distanceScore =
                CalculateRangeScore(
                    normalizedDistance,
                    idealRange);

            float vulnerability =
                CalculateTargetVulnerability(
                    target);

            float damageValue =
                Mathf.Clamp01(
                    definition.Damage /
                    Mathf.Max(
                        1f,
                        damageForFullFinisherValue));

            float finisherScore =
                vulnerability *
                damageValue;

            float baseScore =
                distanceScore *
                    distanceWeight +
                alignmentScore *
                    alignmentWeight +
                vulnerability *
                    vulnerabilityWeight +
                finisherScore *
                    finisherWeight;
            
            float roleBonus = 
                CalculateWeaponRoleBonus(
                    definition,
                    vulnerability);

            float ammoFactor =
                CalculateAmmoFactor(
                    weapon,
                    vulnerability);

            float travelFactor =
                CalculateTravelFactor(
                    definition,
                    distance);

            float chargeFactor =
                CalculateChargeFactor(
                    definition,
                    cornerSeverity);

            float score =
                baseScore *
                ammoFactor *
                travelFactor *
                chargeFactor;

            if (!string.IsNullOrWhiteSpace(
                    preferredTargetRacerId) &&
                target.Participant != null &&
                string.Equals(
                    target.Participant.RacerId,
                    preferredTargetRacerId,
                    StringComparison.Ordinal))
            {
                score +=
                    fullPressurePreferredTargetBonus *
                    currentRacePressure;
            }

            opportunity.Valid =
                true;

            opportunity.Weapon =
                weapon;

            opportunity.Target =
                target;

            opportunity.Score =
                Mathf.Clamp01(
                    score);

            opportunity.Distance =
                distance;

            opportunity.Angle =
                angle;

            opportunity.DistanceScore =
                distanceScore;

            opportunity.AlignmentScore =
                alignmentScore;

            opportunity.VulnerabilityScore =
                vulnerability;

            opportunity.AmmoFactor =
                ammoFactor;

            return true;
        }

        private float GetAllowedFiringAngle(
            WeaponDefinition weapon)
        {
            float multiplier =
                1f;

            if (weapon.DeliveryMode ==
                WeaponDeliveryMode.GuidedProjectile)
            {
                multiplier *=
                    guidedAngleMultiplier;
            }

            if (weapon.ActivationMode ==
                EquipmentActivationMode.ChargeRelease)
            {
                multiplier *=
                    chargeAngleMultiplier;
            }

            return Mathf.Clamp(
                firingHalfAngle *
                multiplier,
                0.1f,
                45f);
        }

        private float GetIdealRange(
            WeaponDefinition weapon)
        {
            if (weapon.DeliveryMode ==
                WeaponDeliveryMode.Area)
            {
                return 0.15f;
            }

            if (weapon.ActivationMode ==
                EquipmentActivationMode.ChargeRelease)
            {
                return 0.72f;
            }

            if (weapon.DeliveryMode ==
                WeaponDeliveryMode.GuidedProjectile)
            {
                return 0.58f;
            }

            if (weapon.ActivationMode ==
                EquipmentActivationMode.Hold)
            {
                return 0.38f;
            }

            return 0.52f;
        }

        private float CalculateRangeScore(
            float normalizedDistance,
            float idealRange)
        {
            idealRange =
                Mathf.Clamp(
                    idealRange,
                    0.05f,
                    0.95f);

            float difference =
                Mathf.Abs(
                    normalizedDistance -
                    idealRange);

            float maximumDifference =
                Mathf.Max(
                    idealRange,
                    1f - idealRange);

            float fit =
                1f -
                Mathf.Clamp01(
                    difference /
                    maximumDifference);

            /*
             * Any target inside the valid range retains some
             * usefulness. Ideal-range positioning raises it
             * toward one.
             */
            return Mathf.Lerp(
                0.35f,
                1f,
                fit);
        }

        private float CalculateTargetVulnerability(
            RacerViewController target)
        {
            if (target?.Participant?.Vehicle == null)
                return 0f;

            RaceVehicleState vehicle =
                target.Participant.Vehicle;

            float damageRatio =
                Mathf.Clamp01(
                    vehicle.Damage.Percent /
                    100f);

            RaceShieldState shield =
                vehicle.EquipmentSystem?
                    .Shield;

            float shieldWeakness =
                0f;

            if (shield != null &&
                shield.BaseMaximum > 0f)
            {
                float shieldRatio =
                    Mathf.Clamp01(
                        shield.Current /
                        shield.BaseMaximum);

                shieldWeakness =
                    1f -
                    shieldRatio;
            }

            /*
            * Hull damage is the main indicator that a racer
            * is vulnerable.
            *
            * Shield weakness is only a secondary opportunity
            * signal. Having no shield does not automatically
            * mean the racer is nearly dead.
            */
            float vulnerability =
                damageRatio *
                    0.85f +
                shieldWeakness *
                    0.15f;

            return Mathf.Clamp01(
                vulnerability);
        }

        private float CalculateAmmoFactor(
            RaceWeaponSnapshot weapon,
            float targetVulnerability)
        {
            if (weapon.MaximumAmmo <= 0)
                return 0f;

            float ammoRatio =
                Mathf.Clamp01(
                    weapon.AmmoRatio);

            /*
            * High-capacity weapons such as machine guns are
            * naturally expendable.
            */
            float capacityFactor =
                Mathf.Clamp01(
                    weapon.MaximumAmmo /
                    (float)Mathf.Max(
                        1,
                        plentifulAmmoReference));

            float conservationPenalty =
                Mathf.Lerp(
                    conservativeScarcityPenalty,
                    aggressiveScarcityPenalty,
                    weaponAggression);

            float normalFactor =
                1f -
                (1f - capacityFactor) *
                conservationPenalty *
                Mathf.Lerp(
                    0.5f,
                    1f,
                    1f - ammoRatio);

            /*
            * Very low-capacity weapons receive a separate
            * ordnance-conservation rule.
            *
            * Against a healthy target, rockets / other scarce
            * weapons are deliberately unattractive. Their value
            * rises as the target becomes a legitimate finishing
            * opportunity.
            */
            if (weapon.MaximumAmmo <=
                scarceAmmoThreshold)
            {
                float baseScarceFactor =
                    Mathf.Lerp(
                        conservativeScarceWeaponFactor,
                        aggressiveScarceWeaponFactor,
                        weaponAggression);

                float vulnerabilityFactor =
                    Mathf.InverseLerp(
                        0f,
                        Mathf.Max(
                            0.01f,
                            vulnerabilityForFullScarceWeaponValue),
                        targetVulnerability);

                float scarceFactor =
                    Mathf.Lerp(
                        baseScarceFactor,
                        1f,
                        vulnerabilityFactor);

                /*
                * Become even more conservative as the remaining
                * stock falls.
                */
                float remainingFactor =
                    Mathf.Lerp(
                        0.7f,
                        1f,
                        ammoRatio);

                normalFactor *=
                    scarceFactor *
                    remainingFactor;
            }

            return Mathf.Clamp01(
                normalFactor);
        }

        private float CalculateWeaponRoleBonus(
            WeaponDefinition weapon,
            float targetVulnerability)
        {
            if (weapon == null)
                return 0f;

            switch (weapon.ActivationMode)
            {
                case EquipmentActivationMode.Hold:
                    return
                        holdWeaponPressureBonus *
                        (1f - targetVulnerability);

                case EquipmentActivationMode.Press:
                    return
                        pressWeaponFinisherBonus *
                        targetVulnerability;

                default:
                    return 0f;
            }
        }

        private float CalculateTravelFactor(
            WeaponDefinition weapon,
            float distance)
        {
            if (weapon.DeliveryMode ==
                    WeaponDeliveryMode.Hitscan ||
                weapon.ProjectileSpeed <= 0.01f)
            {
                return 1f;
            }

            float travelTime =
                distance /
                weapon.ProjectileSpeed;

            float penalty =
                projectileTravelTimePenalty;

            if (weapon.DeliveryMode ==
                WeaponDeliveryMode.GuidedProjectile)
            {
                penalty *=
                    0.5f;
            }

            return
                1f /
                (1f +
                 travelTime *
                 penalty);
        }

        private float CalculateChargeFactor(
            WeaponDefinition weapon,
            float cornerSeverity)
        {
            if (weapon.ActivationMode !=
                EquipmentActivationMode.ChargeRelease)
            {
                return 1f;
            }

            float durationFactor =
                1f /
                (1f +
                 Mathf.Max(
                     0f,
                     weapon.ChargeDuration) *
                 chargeDurationPenalty);

            float cornerFactor =
                1f -
                Mathf.Clamp01(
                    cornerSeverity /
                    Mathf.Max(
                        0.01f,
                        runtimeMaximumCornerSeverity));

            /*
             * Don't completely eliminate a charge opportunity
             * simply because the road has some curvature.
             */
            cornerFactor =
                Mathf.Lerp(
                    0.55f,
                    1f,
                    cornerFactor);

            return durationFactor *
                   cornerFactor;
        }

        #endregion

        #region Enemy Validation

        private bool TryGetValidEnemy(
            AIRacerSensor candidateSensor,
            out RacerViewController candidateView)
        {
            candidateView =
                null;

            if (candidateSensor == null ||
                candidateSensor == sensor ||
                !candidateSensor.isActiveAndEnabled)
            {
                return false;
            }

            candidateView =
                candidateSensor.RacerView;

            return IsValidEnemy(
                candidateView);
        }

        private bool IsValidEnemy(
            RacerViewController candidateView)
        {
            if (candidateView == null ||
                !candidateView.IsInitialized ||
                candidateView.Participant == null)
            {
                return false;
            }

            RaceParticipant candidate =
                candidateView.Participant;

            if (candidate == participant)
                return false;

            if (candidate.Status !=
                RaceParticipantStatus.Racing)
            {
                return false;
            }

            if (candidate.Vehicle == null ||
                candidate.Vehicle.IsDestroyed)
            {
                return false;
            }

            if (string.Equals(
                    candidate.TeamId,
                    participant.TeamId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Debug Opportunity

        private void ApplyDebugOpportunity(
            TacticalOpportunity opportunity)
        {
            if (!opportunity.Valid)
                return;

            debugSelectedWeapon =
                opportunity.Weapon.Definition
                    .DisplayName;

            debugActivationMode =
                opportunity.Weapon.Definition
                    .ActivationMode
                    .ToString();

            debugDeliveryMode =
                opportunity.Weapon.Definition
                    .DeliveryMode
                    .ToString();

            debugAmmo =
                opportunity.Weapon.CurrentAmmo;

            debugMaximumAmmo =
                opportunity.Weapon.MaximumAmmo;

            debugTargetRacer =
                opportunity.Target != null
                    ? opportunity.Target.RacerId
                    : "None";

            debugTargetDistance =
                opportunity.Distance;

            debugTargetAngle =
                opportunity.Angle;

            debugCurrentScore =
                opportunity.Score;

            debugDistanceScore =
                opportunity.DistanceScore;

            debugAlignmentScore =
                opportunity.AlignmentScore;

            debugVulnerabilityScore =
                opportunity.VulnerabilityScore;

            debugAmmoFactor =
                opportunity.AmmoFactor;
        }

        #endregion

        #region Weapon Execution

        private void UpdateWeapon(
            WeaponDefinition weapon)
        {
            if (weapon == null)
            {
                CancelWeapon();
                return;
            }

            if (weapon.DeliveryMode ==
                WeaponDeliveryMode.GuidedProjectile)
            {
                if (guidedLockState == null ||
                    currentTarget == null)
                {
                    debugDecision =
                        "No Guided Target";

                    return;
                }

                guidedLockState.TrackCandidate(
                    currentTarget,
                    Time.fixedDeltaTime,
                    weapon.TargetLockDuration);

                if (!guidedLockState.IsLocked)
                {
                    debugDecision =
                        "Acquiring Guided Lock";

                    return;
                }
            }
            else
            {
                guidedLockState?.ClearTarget();
            }

            switch (weapon.ActivationMode)
            {
                case EquipmentActivationMode.Press:
                    UpdatePressWeapon();
                    break;

                case EquipmentActivationMode.Hold:
                    UpdateHoldWeapon();
                    break;

                case EquipmentActivationMode.ChargeRelease:
                    UpdateChargeWeapon(
                        weapon);
                    break;

                default:
                    CancelWeapon();

                    debugDecision =
                        "Unsupported Activation";
                    break;
            }
        }

        private void UpdatePressWeapon()
        {
            if (cooldownTimer > 0f)
            {
                debugDecision =
                    "Press Cooldown";

                return;
            }

            bool fired =
                equipment.BeginSelectedActivation();

            equipment.EndSelectedActivation();

            cooldownTimer =
                runtimePressWeaponCooldown;

            debugAmmo =
                equipment.SelectedWeaponAmmo;

            debugDecision =
                fired
                    ? "Press Fired"
                    : "Press Failed";
        }

        private void UpdateHoldWeapon()
        {
            if (!weaponActive)
            {
                if (cooldownTimer > 0f)
                {
                    debugDecision =
                        "Burst Cooldown";

                    return;
                }

                bool started =
                    equipment.BeginSelectedActivation();

                if (!started)
                {
                    debugDecision =
                        equipment.SelectedWeaponIsEmpty
                            ? "Out Of Ammo"
                            : "Burst Failed";

                    return;
                }

                weaponActive =
                    true;

                burstTimer =
                    runtimeBurstDuration;

                debugWeaponActive =
                    true;

                debugDecision =
                    "Burst Started";
            }

            burstTimer -=
                Time.fixedDeltaTime;

            debugBurstTimer =
                burstTimer;

            debugAmmo =
                equipment.SelectedWeaponAmmo;

            if (equipment.SelectedWeaponIsEmpty)
            {
                equipment.EndSelectedActivation();

                weaponActive =
                    false;

                burstTimer =
                    0f;

                debugWeaponActive =
                    false;

                debugBurstTimer =
                    0f;

                debugDecision =
                    "Weapon Empty";

                ForceDecisionRefresh();

                return;
            }

            if (burstTimer > 0f)
            {
                debugDecision =
                    "Firing Burst";

                return;
            }

            equipment.EndSelectedActivation();

            weaponActive =
                false;

            cooldownTimer =
                runtimeBurstCooldown;

            debugWeaponActive =
                false;

            debugBurstTimer =
                0f;

            debugDecision =
                "Burst Complete";
        }

        private void UpdateChargeWeapon(
            WeaponDefinition weapon)
        {
            if (!weaponActive)
            {
                if (cooldownTimer > 0f)
                {
                    debugDecision =
                        "Charge Cooldown";

                    return;
                }

                bool started =
                    equipment.BeginSelectedActivation();

                if (!started)
                {
                    debugDecision =
                        equipment.SelectedWeaponIsEmpty
                            ? "Out Of Ammo"
                            : "Charge Failed";

                    return;
                }

                weaponActive =
                    true;

                chargeTimer =
                    0f;

                debugWeaponActive =
                    true;

                debugDecision =
                    "Charging";

                return;
            }

            chargeTimer +=
                Time.fixedDeltaTime;

            debugChargeTimer =
                chargeTimer;

            if (chargeTimer <
                weapon.ChargeDuration)
            {
                debugDecision =
                    "Charging";

                return;
            }

            bool fired =
                equipment.EndSelectedActivation();

            weaponActive =
                false;

            chargeTimer =
                0f;

            cooldownTimer =
                runtimePressWeaponCooldown;

            debugWeaponActive =
                false;

            debugChargeTimer =
                0f;

            debugAmmo =
                equipment.SelectedWeaponAmmo;

            debugDecision =
                fired
                    ? "Charged Shot Fired"
                    : "Charged Shot Failed";

            if (equipment.SelectedWeaponIsEmpty)
                ForceDecisionRefresh();
        }

        #endregion

        #region Combat State

        private bool CanConsiderCombat()
        {
            if (participant == null ||
                participant.Vehicle == null)
            {
                debugDecision =
                    "No Participant";

                return false;
            }

            if (participant.Status !=
                RaceParticipantStatus.Racing)
            {
                debugDecision =
                    "Not Racing";

                return false;
            }

            if (participant.Vehicle.IsDestroyed)
            {
                debugDecision =
                    "Destroyed";

                return false;
            }

            if (!equipment.HasWeapon)
            {
                debugDecision =
                    "No Weapon";

                return false;
            }

            if (!equipment.HasUsableWeapon)
            {
                debugDecision =
                    "Out Of Ammo";

                return false;
            }

            return true;
        }

        public void Stop()
        {
            CancelWeapon();
        }

        private void CancelWeapon()
        {
            guidedLockState?.ClearTarget();

            if (!weaponActive ||
                equipment == null)
            {
                debugWeaponActive =
                    false;

                return;
            }

            equipment.EndSelectedActivation();

            weaponActive =
                false;

            burstTimer =
                0f;

            chargeTimer =
                0f;

            debugWeaponActive =
                false;

            debugBurstTimer =
                0f;

            debugChargeTimer =
                0f;
        }

        public void Dispose()
        {
            CancelWeapon();
            guidedLockState?.ClearTarget();

            participant =
                null;

            equipment =
                null;

            sensor =
                null;

            racerView =
                null;

            currentTarget =
                null;

            currentEquipmentId =
                null;

            initialized =
                false;

            debugInitialized =
                false;
        }

        #endregion
    }
}