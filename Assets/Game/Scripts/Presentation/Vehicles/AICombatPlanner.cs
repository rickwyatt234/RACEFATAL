using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(AIRacerSensor))]
    [RequireComponent(typeof(AIDriverController))]
    [RequireComponent(typeof(RacerViewController))]
    public class AICombatPlanner : MonoBehaviour
    {
        #region Targeting

        [Header("Targeting")]
        [Tooltip("Hard upper limit on AI combat acquisition distance. Weapon range may reduce this further.")]
        [Min(1f)][SerializeField] private float maximumTargetDistance = 150f;

        [Tooltip("Low-aggression racers use this fraction of Maximum Target Distance.")]
        [Range(0.1f, 1f)][SerializeField] private float lowAggressionTargetDistanceMultiplier = 0.6f;

        [Tooltip("Maximum angle from the physical weapon muzzle direction at which a target is considered shootable.")]
        [Range(0.1f, 45f)][SerializeField] private float firingHalfAngle = 6f;

        [Tooltip("Targets closer than this are ignored to avoid unstable point-blank firing.")]
        [Min(0f)][SerializeField] private float minimumTargetDistance = 3f;

        #endregion

        #region Racing Conditions

        [Header("Racing Conditions")]
        [Tooltip("Corner severity tolerated by a racer with minimum Weapon Aggression.")]
        [Range(0f, 1f)][SerializeField] private float conservativeCornerSeverity = 0.18f;

        [Tooltip("Corner severity tolerated by a racer with maximum Weapon Aggression.")]
        [Range(0f, 1f)][SerializeField] private float maximumCornerSeverity = 0.35f;

        [Tooltip("If enabled, AI will not fire weapons while boosting.")]
        [SerializeField] private bool avoidFiringWhileBoosting = true;

        #endregion

        #region Automatic Weapons

        [Header("Hold Weapons")]
        [Tooltip("Burst duration used by the least weapon-aggressive AI.")]
        [Min(0.05f)][SerializeField] private float minimumBurstDuration = 0.45f;

        [Tooltip("Burst duration used by the most weapon-aggressive AI.")]
        [Min(0.05f)][SerializeField] private float maximumBurstDuration = 1f;

        [Tooltip("Burst cooldown used by the most weapon-aggressive AI.")]
        [Min(0f)][SerializeField] private float minimumBurstCooldown = 0.35f;

        [Tooltip("Burst cooldown used by the least weapon-aggressive AI.")]
        [Min(0f)][SerializeField] private float maximumBurstCooldown = 0.8f;

        #endregion

        #region Press Weapons

        [Header("Press Weapons")]
        [Tooltip("Base delay between separate press-weapon activations.")]
        [Min(0.05f)][SerializeField] private float pressWeaponCooldown = 1.25f;

        [Tooltip("Press cooldown multiplier at minimum Weapon Aggression.")]
        [Min(1f)][SerializeField] private float lowAggressionPressCooldownMultiplier = 1.5f;

        [Tooltip("Press cooldown multiplier at maximum Weapon Aggression.")]
        [Range(0.1f, 1f)][SerializeField] private float highAggressionPressCooldownMultiplier = 0.65f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private string debugDecision = "Not Initialized";

        [SerializeField] private float debugWeaponAggression;
        [SerializeField] private float debugRuntimeTargetDistance;
        [SerializeField] private float debugRuntimeCornerSeverity;
        [SerializeField] private float debugRuntimeBurstDuration;
        [SerializeField] private float debugRuntimeBurstCooldown;
        [SerializeField] private float debugRuntimePressCooldown;

        [SerializeField] private string debugSelectedWeapon;
        [SerializeField] private string debugActivationMode;
        [SerializeField] private int debugAmmo;
        [SerializeField] private int debugMaximumAmmo;

        [SerializeField] private string debugTargetRacer;
        [SerializeField] private float debugTargetDistance;
        [SerializeField] private float debugTargetAngle;

        [SerializeField] private bool debugWeaponActive;

        [SerializeField] private float debugBurstTimer;
        [SerializeField] private float debugCooldownTimer;
        [SerializeField] private float debugChargeTimer;

        #endregion

        #region Runtime

        private AIRacerSensor sensor;
        private AIDriverController driver;
        private RacerViewController racerView;
        private AIBoostPlanner boostPlanner;

        private RaceParticipant participant;
        private RaceEquipmentSystem equipment;

        private RacerViewController currentTarget;

        private bool initialized;
        private bool weaponActive;

        private float weaponAggression;

        private float burstTimer;
        private float cooldownTimer;
        private float chargeTimer;

        private float runtimeTargetDistance;
        private float runtimeMaximumCornerSeverity;
        private float runtimeBurstDuration;
        private float runtimeBurstCooldown;
        private float runtimePressWeaponCooldown;

        #endregion

        #region Public

        public bool IsInitialized => initialized;
        public float WeaponAggression => weaponAggression;

        #endregion

        #region Unity

        private void Awake()
        {
            sensor = GetComponent<AIRacerSensor>();
            driver = GetComponent<AIDriverController>();
            racerView = GetComponent<RacerViewController>();
            boostPlanner = GetComponent<AIBoostPlanner>();
        }

        private void FixedUpdate()
        {
            if (!initialized)
                return;

            if (!CanConsiderCombat())
            {
                CancelWeapon();
                return;
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer =
                    Mathf.Max(
                        0f,
                        cooldownTimer -
                        Time.fixedDeltaTime);
            }

            /*
             * If the current weapon emptied during its previous
             * burst, end that activation before changing selection.
             */
            if (equipment.SelectedWeaponIsEmpty &&
                weaponActive)
            {
                CancelWeapon();
            }

            if (!equipment.SelectWeaponWithAmmo())
            {
                debugSelectedWeapon = "None";
                debugAmmo = 0;
                debugMaximumAmmo = 0;
                debugDecision = "Out Of Ammo";

                CancelWeapon();
                return;
            }

            WeaponDefinition weapon =
                equipment.SelectedWeaponDefinition;

            if (weapon == null)
            {
                debugSelectedWeapon = "None";
                debugDecision = "No Weapon";

                CancelWeapon();
                return;
            }

            debugSelectedWeapon = weapon.DisplayName;
            debugActivationMode = weapon.ActivationMode.ToString();
            debugAmmo = equipment.SelectedWeaponAmmo;
            debugMaximumAmmo = equipment.SelectedWeaponMaximumAmmo;

            if (driver.CurrentCornerSeverity >
                runtimeMaximumCornerSeverity)
            {
                debugDecision = "Corner / Hold Fire";
                CancelWeapon();
                return;
            }

            if (avoidFiringWhileBoosting &&
                boostPlanner != null &&
                boostPlanner.IsBoosting)
            {
                debugDecision = "Boosting / Hold Fire";
                CancelWeapon();
                return;
            }

            currentTarget =
                FindBestTarget(
                    weapon);

            if (currentTarget == null)
            {
                debugTargetRacer = "None";
                debugTargetDistance = 0f;
                debugTargetAngle = 0f;
                debugDecision = "No Target";

                CancelWeapon();
                return;
            }

            UpdateWeapon(
                weapon);
        }

        private void OnDisable()
        {
            CancelWeapon();
        }

        #endregion

        #region Initialization

        public bool Initialize(
            RaceParticipant raceParticipant,
            float aggression)
        {
            if (raceParticipant == null)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires a RaceParticipant.",
                    this);

                return false;
            }

            if (raceParticipant.Role ==
                RaceParticipantRole.Player)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} cannot initialize for the player racer.",
                    this);

                return false;
            }

            if (raceParticipant.Vehicle?.EquipmentSystem == null)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires a race vehicle with an equipment system.",
                    this);

                return false;
            }

            if (driver == null ||
                !driver.IsInitialized)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires an initialized {nameof(AIDriverController)}.",
                    this);

                return false;
            }

            if (racerView == null ||
                !racerView.IsInitialized)
            {
                Debug.LogError(
                    $"{nameof(AICombatPlanner)} requires an initialized {nameof(RacerViewController)}.",
                    this);

                return false;
            }

            participant = raceParticipant;
            equipment = participant.Vehicle.EquipmentSystem;

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

            burstTimer = 0f;
            cooldownTimer = 0f;
            chargeTimer = 0f;
            weaponActive = false;
            currentTarget = null;

            debugWeaponAggression = weaponAggression;
            debugRuntimeTargetDistance = runtimeTargetDistance;
            debugRuntimeCornerSeverity = runtimeMaximumCornerSeverity;
            debugRuntimeBurstDuration = runtimeBurstDuration;
            debugRuntimeBurstCooldown = runtimeBurstCooldown;
            debugRuntimePressCooldown = runtimePressWeaponCooldown;

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

        #region Conditions

        private bool CanConsiderCombat()
        {
            if (participant == null ||
                participant.Vehicle == null)
            {
                debugDecision = "No Participant";
                return false;
            }

            if (driver == null ||
                !driver.enabled ||
                !driver.IsInitialized)
            {
                debugDecision = "AI Driver Inactive";
                return false;
            }

            if (participant.Status !=
                RaceParticipantStatus.Racing)
            {
                debugDecision = "Not Racing";
                return false;
            }

            if (participant.Vehicle.IsDestroyed)
            {
                debugDecision = "Destroyed";
                return false;
            }

            if (!equipment.HasWeapon)
            {
                debugDecision = "No Weapon";
                return false;
            }

            if (!equipment.HasUsableWeapon)
            {
                debugDecision = "Out Of Ammo";
                return false;
            }

            return true;
        }

        #endregion

        #region Targeting

        private RacerViewController FindBestTarget(
            WeaponDefinition weapon)
        {
            if (weapon == null)
                return null;

            if (!racerView.TryGetEquipmentMount(
                    equipment.SelectedEquipmentId,
                    out BikeEquipmentMountView mount))
            {
                debugDecision = "No Weapon Mount";
                return null;
            }

            Transform origin =
                mount.EquipmentOrigin;

            float searchRange =
                Mathf.Min(
                    runtimeTargetDistance,
                    weapon.Range);

            RacerViewController bestTarget = null;
            float bestDistance = float.PositiveInfinity;
            float bestAngle = 0f;

            var racers =
                AIRacerSensor.ActiveSensors;

            for (int i = 0;
                 i < racers.Count;
                 i++)
            {
                AIRacerSensor candidateSensor =
                    racers[i];

                if (candidateSensor == null ||
                    candidateSensor == sensor ||
                    !candidateSensor.isActiveAndEnabled)
                {
                    continue;
                }

                RacerViewController candidateView =
                    candidateSensor.RacerView;

                if (candidateView == null ||
                    !candidateView.IsInitialized ||
                    candidateView.Participant == null)
                {
                    continue;
                }

                RaceParticipant candidate =
                    candidateView.Participant;

                if (candidate.Status !=
                    RaceParticipantStatus.Racing)
                {
                    continue;
                }

                if (candidate.Vehicle == null ||
                    candidate.Vehicle.IsDestroyed)
                {
                    continue;
                }

                if (string.Equals(
                        candidate.TeamId,
                        participant.TeamId,
                        System.StringComparison.Ordinal))
                {
                    continue;
                }

                Vector3 toTarget =
                    candidateView.transform.position -
                    origin.position;

                float distance =
                    toTarget.magnitude;

                if (distance <
                        minimumTargetDistance ||
                    distance >
                        searchRange)
                {
                    continue;
                }

                if (toTarget.sqrMagnitude <
                    0.001f)
                {
                    continue;
                }

                Vector3 direction =
                    toTarget.normalized;

                float forwardDot =
                    Vector3.Dot(
                        origin.forward,
                        direction);

                if (forwardDot <= 0f)
                    continue;

                float angle =
                    Vector3.Angle(
                        origin.forward,
                        direction);

                if (angle >
                    firingHalfAngle)
                {
                    continue;
                }

                if (distance >=
                    bestDistance)
                {
                    continue;
                }

                bestTarget = candidateView;
                bestDistance = distance;
                bestAngle = angle;
            }

            if (bestTarget != null)
            {
                debugTargetRacer =
                    bestTarget.RacerId;

                debugTargetDistance =
                    bestDistance;

                debugTargetAngle =
                    bestAngle;
            }

            return bestTarget;
        }

        #endregion

        #region Weapon Control

        private void UpdateWeapon(
            WeaponDefinition weapon)
        {
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
                    debugDecision = "Unsupported Activation";
                    break;
            }
        }

        private void UpdatePressWeapon()
        {
            if (cooldownTimer > 0f)
            {
                debugDecision = "Press Cooldown";
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
                    debugDecision = "Burst Cooldown";
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

                weaponActive = true;
                burstTimer = runtimeBurstDuration;

                debugWeaponActive = true;
                debugDecision = "Burst Started";
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

                weaponActive = false;
                burstTimer = 0f;

                debugWeaponActive = false;
                debugBurstTimer = 0f;
                debugDecision = "Weapon Empty";

                return;
            }

            if (burstTimer > 0f)
            {
                debugDecision = "Firing Burst";
                return;
            }

            equipment.EndSelectedActivation();

            weaponActive = false;
            cooldownTimer = runtimeBurstCooldown;

            debugWeaponActive = false;
            debugBurstTimer = 0f;
            debugDecision = "Burst Complete";
        }

        private void UpdateChargeWeapon(
            WeaponDefinition weapon)
        {
            if (!weaponActive)
            {
                if (cooldownTimer > 0f)
                {
                    debugDecision = "Charge Cooldown";
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

                weaponActive = true;
                chargeTimer = 0f;

                debugWeaponActive = true;
                debugDecision = "Charging";

                return;
            }

            chargeTimer +=
                Time.fixedDeltaTime;

            debugChargeTimer =
                chargeTimer;

            if (chargeTimer <
                weapon.ChargeDuration)
            {
                debugDecision = "Charging";
                return;
            }

            bool fired =
                equipment.EndSelectedActivation();

            weaponActive = false;
            chargeTimer = 0f;

            cooldownTimer =
                runtimePressWeaponCooldown;

            debugWeaponActive = false;
            debugChargeTimer = 0f;

            debugAmmo =
                equipment.SelectedWeaponAmmo;

            debugDecision =
                fired
                    ? "Charged Shot Fired"
                    : "Charged Shot Failed";
        }

        private void CancelWeapon()
        {
            if (!weaponActive ||
                equipment == null)
            {
                debugWeaponActive = false;
                return;
            }

            equipment.EndSelectedActivation();

            weaponActive = false;
            burstTimer = 0f;
            chargeTimer = 0f;

            debugWeaponActive = false;
            debugBurstTimer = 0f;
            debugChargeTimer = 0f;
        }

        #endregion
    }
}