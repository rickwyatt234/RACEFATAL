using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    public class AIDefensiveDrivingPlanner : MonoBehaviour
    {
        #region Reaction

        [Header("Reaction")]
        [Tooltip("Reaction time for a racer with Defensive Skill = 0.")]
        [Min(0f)][SerializeField] private float lowSkillReactionDelay = 0.55f;

        [Tooltip("Reaction time for a racer with Defensive Skill = 1.")]
        [Min(0f)][SerializeField] private float highSkillReactionDelay = 0.08f;

        [Tooltip("How long a low-skill racer continues evasive driving after the most recent hit.")]
        [Min(0.1f)][SerializeField] private float lowSkillThreatMemory = 0.55f;

        [Tooltip("How long a high-skill racer continues evasive driving after the most recent hit.")]
        [Min(0.1f)][SerializeField] private float highSkillThreatMemory = 1.25f;

        [Tooltip("Hits closer together than this are considered sustained fire.")]
        [Min(0.01f)][SerializeField] private float sustainedFireGap = 0.22f;

        #endregion

        #region Evasion

        [Header("Evasion")]
        [Tooltip("Maximum lateral dodge distance for Defensive Skill = 0.")]
        [Min(0f)][SerializeField] private float lowSkillDodgeDistance = 0.7f;

        [Tooltip("Maximum lateral dodge distance for Defensive Skill = 1.")]
        [Min(0f)][SerializeField] private float highSkillDodgeDistance = 2.8f;

        [Tooltip("Lateral movement speed for Defensive Skill = 0.")]
        [Min(0.1f)][SerializeField] private float lowSkillShiftSpeed = 1.5f;

        [Tooltip("Lateral movement speed for Defensive Skill = 1.")]
        [Min(0.1f)][SerializeField] private float highSkillShiftSpeed = 5.5f;

        [Header("Jinking")]
        [Tooltip("Time between evasive direction changes for Defensive Skill = 0.")]
        [Min(0.1f)][SerializeField] private float lowSkillJinkInterval = 1.2f;

        [Tooltip("Time between evasive direction changes for Defensive Skill = 1.")]
        [Min(0.1f)][SerializeField] private float highSkillJinkInterval = 0.4f;

        [Tooltip("Minimum Defensive Skill required before sustained fire can cause repeated left/right jinking.")]
        [Range(0f, 1f)][SerializeField] private float jinkSkillThreshold = 0.4f;

        [Tooltip("Number of rapid hits required before jinking can begin.")]
        [Min(2)][SerializeField] private int hitsRequiredForJinking = 2;

        #endregion

        #region Corner Safety

        [Header("Corner Safety")]
        [Tooltip("Corner severity where defensive movement begins being reduced.")]
        [Range(0f, 1f)][SerializeField] private float defenseReductionCornerSeverity = 0.3f;

        [Tooltip("Corner severity where defensive movement reaches its minimum.")]
        [Range(0f, 1f)][SerializeField] private float maximumDefenseCornerSeverity = 0.75f;

        [Tooltip("Minimum defensive movement retained through very severe corners.")]
        [Range(0f, 1f)][SerializeField] private float minimumCornerDefenseScale = 0.15f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugUnderFire;

        [Range(0f, 1f)]
        [SerializeField] private float debugDefensiveSkill;

        [SerializeField] private float debugReactionTimer;
        [SerializeField] private float debugThreatTimer;

        [SerializeField] private float debugCurrentOffset;
        [SerializeField] private float debugTargetOffset;

        [SerializeField] private int debugHitStreak;
        [SerializeField] private int debugDodgeSide;

        [SerializeField] private string debugLastAttacker = "None";
        [SerializeField] private string debugLastImpactSide = "Unknown";

        #endregion

        #region Runtime

        private RaceParticipant participant;
        private RaceRuntimeController raceRuntime;

        private float defensiveSkill;

        private float reactionDelay;
        private float threatMemory;
        private float dodgeDistance;
        private float shiftSpeed;
        private float jinkInterval;

        private float reactionTimer;
        private float threatTimer;
        private float jinkTimer;

        private float timeSinceLastHit =
            float.PositiveInfinity;

        private float currentOffset;

        private int dodgeSide = 1;
        private int hitStreak;

        private bool initialized;

        #endregion

        #region Public State

        public bool IsUnderFire =>
            initialized &&
            threatTimer > 0f;

        public float CurrentDefensiveOffset =>
            currentOffset;

        public float DefensiveSkill =>
            defensiveSkill;

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            RaceRuntimeController runtime,
            float skill)
        {
            if (raceParticipant == null ||
                runtime == null ||
                runtime.Director == null)
            {
                return false;
            }

            Unsubscribe();

            participant = raceParticipant;
            raceRuntime = runtime;

            defensiveSkill =
                Mathf.Clamp01(skill);

            CalculateSkillValues();
            ResetRuntimeState();

            raceRuntime.Director.DamageApplied +=
                OnDamageApplied;

            initialized = true;

            debugInitialized = true;
            debugDefensiveSkill = defensiveSkill;

            return true;
        }

        private void CalculateSkillValues()
        {
            reactionDelay =
                Mathf.Lerp(
                    lowSkillReactionDelay,
                    highSkillReactionDelay,
                    defensiveSkill);

            threatMemory =
                Mathf.Lerp(
                    lowSkillThreatMemory,
                    highSkillThreatMemory,
                    defensiveSkill);

            dodgeDistance =
                Mathf.Lerp(
                    lowSkillDodgeDistance,
                    highSkillDodgeDistance,
                    defensiveSkill);

            shiftSpeed =
                Mathf.Lerp(
                    lowSkillShiftSpeed,
                    highSkillShiftSpeed,
                    defensiveSkill);

            jinkInterval =
                Mathf.Lerp(
                    lowSkillJinkInterval,
                    highSkillJinkInterval,
                    defensiveSkill);
        }

        private void ResetRuntimeState()
        {
            reactionTimer = 0f;
            threatTimer = 0f;
            jinkTimer = 0f;

            timeSinceLastHit =
                float.PositiveInfinity;

            currentOffset = 0f;

            hitStreak = 0;
            dodgeSide =
                GetStableStartingSide(
                    participant.RacerId);

            debugUnderFire = false;
            debugReactionTimer = 0f;
            debugThreatTimer = 0f;

            debugCurrentOffset = 0f;
            debugTargetOffset = 0f;

            debugHitStreak = 0;
            debugDodgeSide = dodgeSide;

            debugLastAttacker = "None";
            debugLastImpactSide = "Unknown";
        }

        #endregion

        #region Planning

        public float UpdatePlan(
            float baseLateralOffset,
            float maximumAbsoluteOffset,
            float cornerSeverity)
        {
            if (!initialized)
                return 0f;

            float deltaTime =
                Time.fixedDeltaTime;

            UpdateTimers(deltaTime);

            float targetOffset = 0f;

            if (threatTimer > 0f &&
                reactionTimer <= 0f)
            {
                UpdateJinking();

                ChooseTrackSafeSide(
                    baseLateralOffset,
                    maximumAbsoluteOffset);

                float cornerScale =
                    CalculateCornerScale(
                        cornerSeverity);

                float desiredMagnitude =
                    dodgeDistance *
                    cornerScale;

                float desiredAbsoluteOffset =
                    Mathf.Clamp(
                        baseLateralOffset +
                        dodgeSide *
                        desiredMagnitude,
                        -maximumAbsoluteOffset,
                        maximumAbsoluteOffset);

                targetOffset =
                    desiredAbsoluteOffset -
                    baseLateralOffset;
            }

            currentOffset =
                Mathf.MoveTowards(
                    currentOffset,
                    targetOffset,
                    shiftSpeed *
                    deltaTime);

            UpdateDebug(
                targetOffset);

            return currentOffset;
        }

        private void UpdateTimers(
            float deltaTime)
        {
            timeSinceLastHit +=
                deltaTime;

            if (reactionTimer > 0f)
            {
                reactionTimer =
                    Mathf.Max(
                        0f,
                        reactionTimer -
                        deltaTime);
            }

            if (threatTimer > 0f)
            {
                threatTimer =
                    Mathf.Max(
                        0f,
                        threatTimer -
                        deltaTime);
            }

            if (jinkTimer > 0f)
            {
                jinkTimer =
                    Mathf.Max(
                        0f,
                        jinkTimer -
                        deltaTime);
            }

            if (threatTimer <= 0f)
                hitStreak = 0;
        }

        private void UpdateJinking()
        {
            if (defensiveSkill <
                jinkSkillThreshold)
            {
                return;
            }

            if (hitStreak <
                hitsRequiredForJinking)
            {
                return;
            }

            if (jinkTimer > 0f)
                return;

            dodgeSide *= -1;
            jinkTimer = jinkInterval;
        }

        #endregion

        #region Damage Response

        private void OnDamageApplied(
            DamageEvent damageEvent)
        {
            if (!initialized)
                return;

            if (damageEvent.VictimRacerId !=
                participant.RacerId)
            {
                return;
            }

            if (damageEvent.Cause !=
                DamageCause.Weapon)
            {
                return;
            }

            bool wasUnderFire =
                threatTimer > 0f;

            bool sustainedFire =
                timeSinceLastHit <=
                sustainedFireGap;

            hitStreak =
                sustainedFire
                    ? hitStreak + 1
                    : 1;

            timeSinceLastHit = 0f;
            threatTimer = threatMemory;

            /*
             * Reaction delay only begins when entering the
             * under-fire state. Subsequent rapid hits refresh
             * threat memory without repeatedly delaying response.
             */
            if (!wasUnderFire)
                reactionTimer = reactionDelay;

            SetInitialDodgeSide(
                damageEvent.ImpactSide);

            debugLastAttacker =
                string.IsNullOrWhiteSpace(
                    damageEvent.AttackerRacerId)
                    ? "Unknown"
                    : damageEvent.AttackerRacerId;

            debugLastImpactSide =
                damageEvent.ImpactSide.ToString();
        }

        private void SetInitialDodgeSide(
            DamageImpactSide impactSide)
        {
            /*
             * If the left side is being hit, favor moving right.
             * If the right side is being hit, favor moving left.
             *
             * Front/rear fire does not provide a useful lateral
             * preference, so preserve the existing dodge side.
             */
            switch (impactSide)
            {
                case DamageImpactSide.Left:
                    dodgeSide = 1;
                    break;

                case DamageImpactSide.Right:
                    dodgeSide = -1;
                    break;

                case DamageImpactSide.Front:
                case DamageImpactSide.Rear:
                case DamageImpactSide.Unknown:
                    break;
            }
        }

        #endregion

        #region Track Safety

        private void ChooseTrackSafeSide(
            float baseOffset,
            float maximumAbsoluteOffset)
        {
            maximumAbsoluteOffset =
                Mathf.Max(
                    0f,
                    maximumAbsoluteOffset);

            float leftRoom =
                baseOffset -
                (-maximumAbsoluteOffset);

            float rightRoom =
                maximumAbsoluteOffset -
                baseOffset;

            float requiredRoom =
                dodgeDistance *
                0.75f;

            if (dodgeSide < 0 &&
                leftRoom < requiredRoom &&
                rightRoom > leftRoom)
            {
                dodgeSide = 1;
            }
            else if (dodgeSide > 0 &&
                     rightRoom < requiredRoom &&
                     leftRoom > rightRoom)
            {
                dodgeSide = -1;
            }
        }

        private float CalculateCornerScale(
            float cornerSeverity)
        {
            cornerSeverity =
                Mathf.Clamp01(
                    cornerSeverity);

            if (cornerSeverity <=
                defenseReductionCornerSeverity)
            {
                return 1f;
            }

            float t =
                Mathf.InverseLerp(
                    defenseReductionCornerSeverity,
                    Mathf.Max(
                        defenseReductionCornerSeverity +
                        0.001f,
                        maximumDefenseCornerSeverity),
                    cornerSeverity);

            return Mathf.Lerp(
                1f,
                minimumCornerDefenseScale,
                t);
        }

        #endregion

        #region Helpers

        private int GetStableStartingSide(
            string racerId)
        {
            if (string.IsNullOrEmpty(racerId))
                return 1;

            unchecked
            {
                int hash = 17;

                for (int i = 0;
                     i < racerId.Length;
                     i++)
                {
                    hash =
                        hash * 31 +
                        racerId[i];
                }

                return (hash & 1) == 0
                    ? -1
                    : 1;
            }
        }

        private void UpdateDebug(
            float targetOffset)
        {
            debugUnderFire =
                threatTimer > 0f;

            debugReactionTimer =
                reactionTimer;

            debugThreatTimer =
                threatTimer;

            debugCurrentOffset =
                currentOffset;

            debugTargetOffset =
                targetOffset;

            debugHitStreak =
                hitStreak;

            debugDodgeSide =
                dodgeSide;
        }

        private void Unsubscribe()
        {
            if (raceRuntime != null &&
                raceRuntime.Director != null)
            {
                raceRuntime.Director.DamageApplied -=
                    OnDamageApplied;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        #endregion
    }
}