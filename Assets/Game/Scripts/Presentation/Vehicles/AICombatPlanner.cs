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

        [Tooltip("Maximum angle from the physical weapon muzzle direction at which a target is considered shootable.")]
        [Range(0.1f, 45f)][SerializeField] private float firingHalfAngle = 6f;

        [Tooltip("Targets closer than this are ignored to avoid unstable point-blank firing.")]
        [Min(0f)][SerializeField] private float minimumTargetDistance = 3f;

        #endregion

        #region Racing Conditions

        [Header("Racing Conditions")]
        [Tooltip("AI does not start or continue attacks through corners more severe than this.")]
        [Range(0f, 1f)][SerializeField] private float maximumCornerSeverity = 0.35f;

        [Tooltip("If enabled, AI will not fire weapons while boosting.")]
        [SerializeField] private bool avoidFiringWhileBoosting = true;

        #endregion

        #region Automatic Weapons

        [Header("Hold Weapons")]
        [Tooltip("Minimum duration of an automatic-fire burst.")]
        [Min(0.05f)][SerializeField] private float minimumBurstDuration = 0.45f;

        [Tooltip("Maximum duration of an automatic-fire burst.")]
        [Min(0.05f)][SerializeField] private float maximumBurstDuration = 1f;

        [Tooltip("Minimum pause between automatic-fire bursts.")]
        [Min(0f)][SerializeField] private float minimumBurstCooldown = 0.35f;

        [Tooltip("Maximum pause between automatic-fire bursts.")]
        [Min(0f)][SerializeField] private float maximumBurstCooldown = 0.8f;

        #endregion

        #region Press Weapons

        [Header("Press Weapons")]
        [Tooltip("Minimum delay between separate press-weapon activations.")]
        [Min(0.05f)][SerializeField] private float pressWeaponCooldown = 1.25f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private string debugDecision = "Not Initialized";

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

        private float burstTimer;
        private float cooldownTimer;
        private float chargeTimer;

        private float runtimeBurstDuration;
        private float runtimeBurstCooldown;

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
            if (!initialized && !TryInitialize())
                return;

            if (!CanConsiderCombat())
            {
                CancelWeapon();
                return;
            }

            if (cooldownTimer > 0f)
            {
                cooldownTimer =
                    Mathf.Max(0f, cooldownTimer - Time.fixedDeltaTime);
            }

            /*
             * If the current weapon emptied during its previous burst,
             * end that activation before changing selection.
             */
            if (equipment.SelectedWeaponIsEmpty && weaponActive)
                CancelWeapon();

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

            if (driver.CurrentCornerSeverity > maximumCornerSeverity)
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

            currentTarget = FindBestTarget(weapon);

            if (currentTarget == null)
            {
                debugTargetRacer = "None";
                debugTargetDistance = 0f;
                debugTargetAngle = 0f;
                debugDecision = "No Target";

                CancelWeapon();
                return;
            }

            UpdateWeapon(weapon);
        }

        private void OnDisable()
        {
            CancelWeapon();
        }

        #endregion

        #region Initialization

        private bool TryInitialize()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return false;
            }

            if (driver == null ||
                !driver.IsInitialized ||
                !driver.enabled)
            {
                return false;
            }

            participant = racerView.Participant;

            if (participant.Role == RaceParticipantRole.Player)
            {
                enabled = false;
                return false;
            }

            if (participant.Vehicle?.EquipmentSystem == null)
            {
                enabled = false;
                return false;
            }

            equipment = participant.Vehicle.EquipmentSystem;

            float personality =
                CalculateDeterministicValue(participant.RacerId);

            runtimeBurstDuration = Mathf.Lerp(
                minimumBurstDuration,
                maximumBurstDuration,
                personality);

            runtimeBurstCooldown = Mathf.Lerp(
                maximumBurstCooldown,
                minimumBurstCooldown,
                personality);

            initialized = true;
            debugInitialized = true;

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
            if (participant == null || participant.Vehicle == null)
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

            if (participant.Status != RaceParticipantStatus.Racing)
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

        private RacerViewController FindBestTarget(WeaponDefinition weapon)
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

            Transform origin = mount.EquipmentOrigin;

            float searchRange =
                Mathf.Min(maximumTargetDistance, weapon.Range);

            RacerViewController bestTarget = null;
            float bestDistance = float.PositiveInfinity;
            float bestAngle = 0f;

            var racers = AIRacerSensor.ActiveSensors;

            for (int i = 0; i < racers.Count; i++)
            {
                AIRacerSensor candidateSensor = racers[i];

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

                if (candidate.Status != RaceParticipantStatus.Racing)
                    continue;

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
                    candidateView.transform.position - origin.position;

                float distance = toTarget.magnitude;

                if (distance < minimumTargetDistance ||
                    distance > searchRange)
                {
                    continue;
                }

                if (toTarget.sqrMagnitude < 0.001f)
                    continue;

                Vector3 direction = toTarget.normalized;

                float forwardDot =
                    Vector3.Dot(origin.forward, direction);

                if (forwardDot <= 0f)
                    continue;

                float angle =
                    Vector3.Angle(origin.forward, direction);

                if (angle > firingHalfAngle)
                    continue;

                if (distance >= bestDistance)
                    continue;

                bestTarget = candidateView;
                bestDistance = distance;
                bestAngle = angle;
            }

            if (bestTarget != null)
            {
                debugTargetRacer = bestTarget.RacerId;
                debugTargetDistance = bestDistance;
                debugTargetAngle = bestAngle;
            }

            return bestTarget;
        }

        #endregion

        #region Weapon Control

        private void UpdateWeapon(WeaponDefinition weapon)
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
                    UpdateChargeWeapon(weapon);
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

            cooldownTimer = pressWeaponCooldown;

            debugAmmo = equipment.SelectedWeaponAmmo;
            debugDecision = fired ? "Press Fired" : "Press Failed";
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
                    debugDecision = equipment.SelectedWeaponIsEmpty
                        ? "Out Of Ammo"
                        : "Burst Failed";

                    return;
                }

                weaponActive = true;
                burstTimer = runtimeBurstDuration;

                debugWeaponActive = true;
                debugDecision = "Burst Started";
            }

            burstTimer -= Time.fixedDeltaTime;
            debugBurstTimer = burstTimer;
            debugAmmo = equipment.SelectedWeaponAmmo;

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

        private void UpdateChargeWeapon(WeaponDefinition weapon)
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
                    debugDecision = equipment.SelectedWeaponIsEmpty
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

            chargeTimer += Time.fixedDeltaTime;
            debugChargeTimer = chargeTimer;

            if (chargeTimer < weapon.ChargeDuration)
            {
                debugDecision = "Charging";
                return;
            }

            bool fired =
                equipment.EndSelectedActivation();

            weaponActive = false;
            chargeTimer = 0f;
            cooldownTimer = pressWeaponCooldown;

            debugWeaponActive = false;
            debugChargeTimer = 0f;
            debugAmmo = equipment.SelectedWeaponAmmo;

            debugDecision =
                fired
                    ? "Charged Shot Fired"
                    : "Charged Shot Failed";
        }

        private void CancelWeapon()
        {
            if (!weaponActive || equipment == null)
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

        #region Personality

        private float CalculateDeterministicValue(string id)
        {
            uint hash = 2166136261u;

            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < id.Length; i++)
                {
                    hash ^= id[i];
                    hash *= 16777619u;
                }
            }

            return (hash & 0x00FFFFFFu) / 16777215f;
        }

        #endregion
    }
}