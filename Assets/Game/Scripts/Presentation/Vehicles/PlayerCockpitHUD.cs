using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(RacerViewController))]
    [RequireComponent(typeof(PlayerCockpitView))]
    public class PlayerCockpitHUD : MonoBehaviour
    {
        #region References

        [Header("Energy")]
        [SerializeField] private Image energyFill;
        [SerializeField] private TextMeshProUGUI energyText;

        [Header("Damage")]
        [SerializeField] private Image damageFill;
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("Shield")]
        [SerializeField] private GameObject shieldGroup;
        [SerializeField] private Image shieldFill;
        [SerializeField] private TextMeshProUGUI shieldText;

        [Header("Weapon")]
        [SerializeField] private TextMeshProUGUI weaponText;
        [SerializeField] private TextMeshProUGUI ammoText;

        [Header("Race")]
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI lapText;

        [Header("Damage Feedback")]
        [Tooltip("Optional full-screen image flashed when the player takes shield or bike damage.")]
        [SerializeField] private Image damageFlash;

        #endregion

        #region Feedback

        [Header("Damage Feedback Settings")]
        [Min(0.01f)][SerializeField] private float damageFlashDuration = 0.18f;
        [Range(0f, 1f)][SerializeField] private float damageFlashMaximumAlpha = 0.3f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugResolved;
        [SerializeField] private float debugEnergyPercent;
        [SerializeField] private float debugDamagePercent;

        [SerializeField] private float debugShieldCurrent;
        [SerializeField] private float debugShieldMaximum;
        [SerializeField] private float debugShieldBaseMaximum;

        [SerializeField] private string debugWeapon = "None";
        [SerializeField] private int debugWeaponAmmo;
        [SerializeField] private int debugWeaponMaximumAmmo;
        [SerializeField] private bool debugWeaponEmpty;

        [SerializeField] private int debugPosition;
        [SerializeField] private int debugCurrentLap;

        #endregion

        #region Runtime

        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private RaceRuntimeController raceRuntime;
        private RaceParticipant participant;

        private float previousDamage;
        private float previousShield;
        private float previousShieldMaximum;
        private float damageFlashTimer;

        private bool resolved;

        #endregion

        #region Unity

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();
            cockpitView = GetComponent<PlayerCockpitView>();
            SetDamageFlashAlpha(0f);
        }

        private void Update()
        {
            if (!resolved && !TryResolve())
                return;

            if (participant?.Vehicle == null)
                return;

            UpdateVehicleHUD();
            UpdateRaceHUD();
            UpdateDamageFeedback();
        }

        #endregion

        #region Initialization

        private bool TryResolve()
        {
            if (racerView == null || !racerView.IsInitialized || racerView.Participant == null)
                return false;

            if (racerView.Participant.Role != RaceParticipantRole.Player)
            {
                enabled = false;
                return false;
            }

            if (cockpitView == null || !cockpitView.IsActivePlayerView)
                return false;

            participant = racerView.Participant;
            raceRuntime = FindFirstObjectByType<RaceRuntimeController>();

            previousDamage = participant.Vehicle.Damage.Percent;

            RaceShieldState shield = participant.Vehicle.EquipmentSystem.Shield;

            previousShield = shield != null ? shield.Current : 0f;
            previousShieldMaximum = shield != null ? shield.Maximum : 0f;

            resolved = true;
            debugResolved = true;

            return true;
        }

        #endregion

        #region Vehicle HUD

        private void UpdateVehicleHUD()
        {
            RaceVehicleState vehicle = participant.Vehicle;

            UpdateEnergy(vehicle);
            UpdateDamage(vehicle);
            UpdateShield(vehicle);
            UpdateWeapon(vehicle);
        }

        private void UpdateEnergy(RaceVehicleState vehicle)
        {
            float maximum = vehicle.EnergyPool.MaxEnergy;
            float current = vehicle.EnergyPool.CurrentEnergy;
            float normalized = maximum > 0f ? current / maximum : 0f;

            normalized = Mathf.Clamp01(normalized);

            if (energyFill != null)
                energyFill.fillAmount = normalized;

            if (energyText != null)
                energyText.text = $"ENERGY  {normalized * 100f:0}%";

            debugEnergyPercent = normalized * 100f;
        }

        private void UpdateDamage(RaceVehicleState vehicle)
        {
            float damage = vehicle.Damage.Percent;
            float normalized = Mathf.Clamp01(
                damage / RaceFatal.Combat.DamageMeter.MaxDamage);

            if (damageFill != null)
                damageFill.fillAmount = normalized;

            if (damageText != null)
                damageText.text = $"DAMAGE  {damage:0}%";

            debugDamagePercent = damage;
        }

        private void UpdateShield(RaceVehicleState vehicle)
        {
            RaceShieldState shield = vehicle.EquipmentSystem.Shield;
            bool hasShield = shield != null;

            if (shieldGroup != null && shieldGroup.activeSelf != hasShield)
                shieldGroup.SetActive(hasShield);

            if (!hasShield)
            {
                debugShieldCurrent = 0f;
                debugShieldMaximum = 0f;
                debugShieldBaseMaximum = 0f;
                return;
            }

            /*
             * Fill is based on the shield's original capacity rather
             * than its current energy-limited maximum.
             *
             * This means boosting visibly shortens the available
             * shield bar instead of showing 100% of a smaller pool.
             */
            float normalized = shield.BaseMaximum > 0f
                ? shield.Current / shield.BaseMaximum
                : 0f;

            normalized = Mathf.Clamp01(normalized);

            if (shieldFill != null)
                shieldFill.fillAmount = normalized;

            if (shieldText != null)
            {
                shieldText.text =
                    $"SHIELD  {shield.Current:0} / {shield.Maximum:0}";
            }

            debugShieldCurrent = shield.Current;
            debugShieldMaximum = shield.Maximum;
            debugShieldBaseMaximum = shield.BaseMaximum;
        }

        private void UpdateWeapon(RaceVehicleState vehicle)
        {
            RaceEquipmentSystem equipment = vehicle.EquipmentSystem;
            WeaponDefinition weapon = equipment.SelectedWeaponDefinition;

            if (weapon == null)
            {
                if (weaponText != null)
                    weaponText.text = "WEAPON  NO WEAPON";

                if (ammoText != null)
                    ammoText.text = "AMMO  --";

                debugWeapon = "None";
                debugWeaponAmmo = 0;
                debugWeaponMaximumAmmo = 0;
                debugWeaponEmpty = false;
                return;
            }

            int currentAmmo = equipment.SelectedWeaponAmmo;
            int maximumAmmo = equipment.SelectedWeaponMaximumAmmo;

            if (weaponText != null)
                weaponText.text = $"WEAPON  {weapon.DisplayName.ToUpperInvariant()}";

            if (ammoText != null)
                ammoText.text = $"AMMO  {currentAmmo} / {maximumAmmo}";

            debugWeapon = weapon.DisplayName;
            debugWeaponAmmo = currentAmmo;
            debugWeaponMaximumAmmo = maximumAmmo;
            debugWeaponEmpty = equipment.SelectedWeaponIsEmpty;
        }

        #endregion

        #region Race HUD

        private void UpdateRaceHUD()
        {
            if (raceRuntime == null || raceRuntime.Director == null)
                return;

            UpdatePosition();
            UpdateLap();
        }

        private void UpdatePosition()
        {
            IReadOnlyList<RaceParticipant> order =
                raceRuntime.Director.State.GetCurrentOrder();

            int position = 0;

            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].RacerId != participant.RacerId)
                    continue;

                position = i + 1;
                break;
            }

            if (positionText != null)
            {
                positionText.text = position > 0
                    ? $"POS  {position}/{order.Count}"
                    : $"POS  --/{order.Count}";
            }

            debugPosition = position;
        }

        private void UpdateLap()
        {
            int totalLaps =
                raceRuntime.Director.State.RaceDefinition.LapCount;

            int currentLap = Mathf.Clamp(
                participant.CompletedLaps + 1,
                1,
                Mathf.Max(1, totalLaps));

            if (lapText != null)
                lapText.text = $"LAP  {currentLap}/{totalLaps}";

            debugCurrentLap = currentLap;
        }

        #endregion

        #region Damage Feedback

        private void UpdateDamageFeedback()
        {
            float currentDamage = participant.Vehicle.Damage.Percent;

            RaceShieldState shield =
                participant.Vehicle.EquipmentSystem.Shield;

            float currentShield = shield != null ? shield.Current : 0f;
            float currentShieldMaximum = shield != null ? shield.Maximum : 0f;

            bool bikeWasHit =
                currentDamage > previousDamage + 0.001f;

            /*
             * Boost can lower both Current and Maximum because Current
             * is clamped to the available shield capacity.
             *
             * That capacity loss must not look like incoming damage.
             */
            float shieldLoss =
                Mathf.Max(0f, previousShield - currentShield);

            float capacityLoss =
                Mathf.Max(0f, previousShieldMaximum - currentShieldMaximum);

            float damageRelatedShieldLoss =
                Mathf.Max(0f, shieldLoss - capacityLoss);

            bool shieldWasHit =
                shield != null &&
                damageRelatedShieldLoss > 0.001f;

            if (bikeWasHit || shieldWasHit)
                damageFlashTimer = damageFlashDuration;

            previousDamage = currentDamage;
            previousShield = currentShield;
            previousShieldMaximum = currentShieldMaximum;

            if (damageFlashTimer <= 0f)
            {
                SetDamageFlashAlpha(0f);
                return;
            }

            damageFlashTimer =
                Mathf.Max(0f, damageFlashTimer - Time.deltaTime);

            float normalized = damageFlashDuration > 0f
                ? damageFlashTimer / damageFlashDuration
                : 0f;

            SetDamageFlashAlpha(
                normalized * damageFlashMaximumAlpha);
        }

        private void SetDamageFlashAlpha(float alpha)
        {
            if (damageFlash == null)
                return;

            Color color = damageFlash.color;
            color.a = Mathf.Clamp01(alpha);
            damageFlash.color = color;
        }

        #endregion
    }
}