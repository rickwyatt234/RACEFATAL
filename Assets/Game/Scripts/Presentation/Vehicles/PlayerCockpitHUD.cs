using System.Collections.Generic;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RaceFatal.Presentation.Vehicles
{
    [RequireComponent(typeof(PlayerCockpitView))]
    public class PlayerCockpitHUD : MonoBehaviour
    {
#region References
        [Header("Energy")]
        [SerializeField]
        private Image energyFill;
        [SerializeField] private TextMeshProUGUI energyText;
        [Header("Damage")] [SerializeField] private Image damageFill;
        [SerializeField] private TextMeshProUGUI damageText;
        [Header("Integrity Display")] [SerializeField] private bool showIntegrity = true;
        [SerializeField] private RectTransform integrityRoot;
        [SerializeField] private Vector2 integrityAnchor = new Vector2(0.5f, 1f);
        [SerializeField] private Vector2 integrityPosition = new Vector2(0f, -260f);
        [SerializeField] private Vector2 integritySize = new Vector2(220f, 70f);
        private Image integrityBar;
        private TextMeshProUGUI integrityLabel;
        [Header("Shield")] [SerializeField] private GameObject shieldGroup;
        [SerializeField] private Image shieldFill;
        [SerializeField] private TextMeshProUGUI shieldText;
        [Header("Speed")] [SerializeField] private TextMeshProUGUI speedText;
        [Header("Weapon")] [SerializeField] private TextMeshProUGUI weaponText;
        [SerializeField] private TextMeshProUGUI ammoText;
        [Header("Race")] [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI lapText;
        [Header("Damage Feedback")] [Tooltip("Optional full-screen image flashed when the player takes shield or bike damage.")] [SerializeField] private
            Image damageFlash;
        [Header("Audience")] [SerializeField] private bool showAudience = true;
        [SerializeField] private RectTransform audienceRoot;
        [SerializeField] private Vector2 audienceAnchor = new Vector2(0.5f, 0f);
        [SerializeField] private Vector2 audiencePosition = new Vector2(0f, 100f);
        [SerializeField] private Vector2 audienceSize = new Vector2(260f, 96f);
        [SerializeField] private AudioClip crowdCheeringLoop;
        [SerializeField] private AudioClip crowdBooingLoop;
        [Range(0f, 1f)] [SerializeField] private float crowdVolume = 0.35f;
        private CockpitAudienceHUD audienceHud;
        [Header("Root")] [Tooltip("Root GameObject containing the normal player cockpit HUD.")] [SerializeField] private GameObject hudRoot;
#endregion
#region Feedback
        [Header("Damage Feedback Settings")]
        [Min(0.01f)]
        [SerializeField]
        private float damageFlashDuration = 0.18f;
        [Range(0f, 1f)] [SerializeField] private float damageFlashMaximumAlpha = 0.3f;
#endregion
#region Debug
        [Header("Runtime Debug")]
        [SerializeField]
        private bool debugResolved;
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
#region Lap and Weapon Panel Views
        [System.Serializable]
        private class LapBoxView
        {
            public GameObject root;
            public Image centerImage;
        }

        [System.Serializable]
        private class WeaponPanelSlotView
        {
            public TextMeshProUGUI weaponName;
            public TextMeshProUGUI ammoText;
        }

        [Header("Lap Boxes")] [SerializeField] private List<LapBoxView> lapBoxes = new List<LapBoxView>();
        [SerializeField] private Color incompleteLapColor = Color.black;
        [SerializeField] private Color completeLapColor = Color.white;
        [Header("Weapon Panel")] [SerializeField] private List<WeaponPanelSlotView> weaponPanelSlots = new List<WeaponPanelSlotView>();
        [SerializeField] private Color selectedWeaponColor = Color.white;
        [SerializeField] private Color unselectedWeaponColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private float selectedWeaponFontSize = 30f;
        [SerializeField] private float unselectedWeaponFontSize = 21f;
#endregion
#region Runtime
        private RacerViewController racerView;
        private PlayerCockpitView cockpitView;
        private RaceRuntimeController raceRuntime;
        private RaceParticipant participant;
        private BikeMotor motor;
        private float previousDamage;
        private float previousShield;
        private float previousShieldMaximum;
        private float damageFlashTimer;
        private bool resolved;
#endregion
#region Unity
        private void Awake()
        {
            racerView = GetComponentInParent<RacerViewController>();
            cockpitView = GetComponent<PlayerCockpitView>();
            SetDamageFlashAlpha(0f);
            motor = GetComponentInParent<BikeMotor>();
        }

        private void Update()
        {
            if (!resolved && !TryResolve())
                return;
            if (participant?.Vehicle == null)
                return;
            if (showAudience && audienceHud == null)
                TryCreateAudienceHUD();
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
        private void TryCreateAudienceHUD()
        {
            if (raceRuntime?.Director == null)
                return;
            if (audienceRoot == null)
            {
                Transform parent = hudRoot != null ? hudRoot.transform : speedText != null ? speedText.canvas?.transform : null;
                if (parent == null)
                    return;
                audienceRoot = new GameObject("Audience HUD", typeof(RectTransform)).GetComponent<RectTransform>();
                audienceRoot.SetParent(parent, false);
                audienceRoot.gameObject.layer = parent.gameObject.layer;
                audienceRoot.anchorMin = audienceRoot.anchorMax = audienceAnchor;
                audienceRoot.pivot = new Vector2(0.5f, 0f);
                audienceRoot.anchoredPosition = audiencePosition;
                audienceRoot.sizeDelta = audienceSize;
            }

            audienceHud = audienceRoot.GetComponent<CockpitAudienceHUD>() ?? audienceRoot.gameObject.AddComponent<CockpitAudienceHUD>();
            audienceHud.Initialize(raceRuntime.Director, participant, speedText != null ? speedText.font : null, crowdCheeringLoop, crowdBooingLoop, crowdVolume);
        }

        private void UpdateVehicleHUD()
        {
            RaceVehicleState vehicle = participant.Vehicle;
            UpdateSpeed();
            UpdateEnergy(vehicle);
            UpdateDamage(vehicle);
            UpdateShield(vehicle);
            UpdateWeapon(vehicle);
        }

        private void UpdateSpeed()
        {
            if (motor == null || speedText == null)
            {
                return;
            }

            int speedKph = Mathf.RoundToInt(motor.SpeedMetersPerSecond * 3.6f);
            speedText.text = speedKph.ToString("000");
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
                energyText.text = $"{normalized * 100f:0}%";
            debugEnergyPercent = normalized * 100f;
        }

        private void UpdateDamage(RaceVehicleState vehicle)
        {
            float damage = vehicle.Damage.Percent;
            float normalized = vehicle.Damage.DamageRatio;
            if (damageFill != null)
                damageFill.fillAmount = normalized;
            if (damageText != null)
                damageText.text = $"{damage:0}%";
            debugDamagePercent = damage;
            if (showIntegrity && integrityBar == null)
                TryCreateIntegrityHUD();
            if (integrityRoot != null)
                integrityRoot.gameObject.SetActive(showIntegrity);
            if (showIntegrity && integrityBar != null)
            {
                float remaining = Mathf.Clamp01(1f - normalized);
                integrityBar.rectTransform.anchorMax = new Vector2(remaining, 1f);
                integrityBar.color = Color.Lerp(new Color(1f, 0.18f, 0.25f), new Color(0.2f, 0.9f, 1f), remaining);
                integrityLabel.text = $"INTEGRITY // {vehicle.Damage.CurrentIntegrity:0}/{vehicle.Damage.MaxIntegrity:0}  {remaining * 100f:0}%";
            }
        }

        private void TryCreateIntegrityHUD()
        {
            if (integrityRoot == null)
            {
                Transform parent = hudRoot != null ? hudRoot.transform : speedText != null ? speedText.canvas?.transform : null;
                if (parent == null) return;
                integrityRoot = CreateIntegrityRect("HUD_Integrity", parent, integrityAnchor, integrityAnchor);
                integrityRoot.pivot = new Vector2(0.5f, 0.5f);
                integrityRoot.anchoredPosition = integrityPosition;
                integrityRoot.sizeDelta = integritySize;
            }
            var background = CreateIntegrityRect("Integrity Bar Background", integrityRoot,
                new Vector2(0f, 0.1f), new Vector2(1f, 0.38f));
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.25f, 0.05f, 0.08f, 0.8f);
            backgroundImage.raycastTarget = false;
            integrityBar = CreateIntegrityRect("Integrity Remaining", background, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            integrityBar.raycastTarget = false;
            integrityLabel = CreateIntegrityRect("Integrity Label", integrityRoot,
                new Vector2(0f, 0.45f), Vector2.one).gameObject.AddComponent<TextMeshProUGUI>();
            if (speedText != null && speedText.font != null) integrityLabel.font = speedText.font;
            integrityLabel.fontSize = 15f;
            integrityLabel.enableAutoSizing = true;
            integrityLabel.fontSizeMin = 10f;
            integrityLabel.fontSizeMax = 15f;
            integrityLabel.color = new Color(0.65f, 0.95f, 1f);
            integrityLabel.alignment = TextAlignmentOptions.Center;
            integrityLabel.raycastTarget = false;
        }

        private RectTransform CreateIntegrityRect(string name, Transform parent, Vector2 minimum, Vector2 maximum)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.gameObject.layer = parent.gameObject.layer;
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
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

            float normalized = shield.BaseMaximum > 0f ? shield.Current / shield.BaseMaximum : 0f;
            normalized = Mathf.Clamp01(normalized);
            if (shieldFill != null)
                shieldFill.fillAmount = normalized;
            if (shieldText != null)
            {
                shieldText.text = $"{shield.Current:0}";
            }

            debugShieldCurrent = shield.Current;
            debugShieldMaximum = shield.Maximum;
            debugShieldBaseMaximum = shield.BaseMaximum;
        }

        private void UpdateWeapon(RaceVehicleState vehicle)
        {
            RaceEquipmentSystem equipment = vehicle.EquipmentSystem;
            UpdateWeaponPanel(equipment);
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

        private void UpdateWeaponPanel(RaceEquipmentSystem equipment)
        {
            for (int i = 0; i < weaponPanelSlots.Count; i++)
            {
                WeaponPanelSlotView slot = weaponPanelSlots[i];
                if (slot == null)
                    continue;
                if (i >= 4 || !equipment.TryGetWeaponSnapshot(i, out RaceWeaponSnapshot snapshot))
                {
                    if (slot.weaponName != null)
                        slot.weaponName.text = string.Empty;
                    if (slot.ammoText != null)
                        slot.ammoText.text = string.Empty;
                    continue;
                }

                bool selected = snapshot.IsSelected;
                Color color = selected ? selectedWeaponColor : unselectedWeaponColor;
                if (slot.weaponName != null)
                {
                    slot.weaponName.text = snapshot.Definition.DisplayName.ToUpperInvariant();
                    slot.weaponName.color = color;
                    slot.weaponName.fontSize = selected ? selectedWeaponFontSize : unselectedWeaponFontSize;
                }

                if (slot.ammoText != null)
                {
                    slot.ammoText.text = $"{snapshot.CurrentAmmo}";
                    slot.ammoText.color = color;
                }
            }
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
            IReadOnlyList<RaceParticipant> order = raceRuntime.Director.State.GetCurrentOrder();
            int position = 0;
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i].RacerId != participant.RacerId)
                    continue;
                position = raceRuntime.Director.State.Deathmatch != null ? raceRuntime.Director.State.DeathmatchRank(participant) : i + 1;
                break;
            }

            if (positionText != null)
            {
                positionText.text = position > 0 ? $"{position}" : "--";
            }

            debugPosition = position;
        }

        private void UpdateLap()
        {
            var director = raceRuntime.Director;
            var rules = director.State.Deathmatch;
            if (rules != null)
            {
                UpdateLapBoxes(0);
                if (lapText != null)
                {
                    lapText.enableAutoSizing = true;
                    lapText.fontSizeMin = 10;
                    float remaining = Mathf.Max(0, rules.TimeLimitSeconds - director.ElapsedRaceTime);
                    string speed = director.ElapsedRaceTime < rules.StartGraceSeconds ? $"START GRACE {rules.StartGraceSeconds - director.ElapsedRaceTime:0}s" : participant.BelowSpeedSeconds > 0 ? $"SPEED UP! DQ IN {Mathf.Max(0, rules.BelowSpeedGraceSeconds - participant.BelowSpeedSeconds):0.0}s" : $"MIN {rules.MinimumSpeedKph:0} KM/H";
                    lapText.text = $"ALIVE {director.State.SurvivingContenders} / WINNERS {rules.AllowedWinners}  {remaining:0}s\n{speed}";
                }

                return;
            }

            int totalLaps = raceRuntime.Director.State.RaceDefinition.LapCount;
            int currentLap = Mathf.Clamp(participant.CompletedLaps + 1, 1, Mathf.Max(1, totalLaps));
            if (lapText != null)
            {
                lapText.text = $"LAP  {currentLap}/{totalLaps}";
            }

            UpdateLapBoxes(totalLaps);
            debugCurrentLap = currentLap;
        }

        private void UpdateLapBoxes(int totalLaps)
        {
            for (int i = 0; i < lapBoxes.Count; i++)
            {
                LapBoxView box = lapBoxes[i];
                if (box == null)
                    continue;
                bool used = i < totalLaps;
                if (box.root != null)
                {
                    box.root.SetActive(used);
                }

                if (!used || box.centerImage == null)
                {
                    continue;
                }

                bool completed = participant.CompletedLaps > i;
                box.centerImage.color = completed ? completeLapColor : incompleteLapColor;
            }
        }

        public void SetHudVisible(bool visible)
        {
            if (hudRoot == null)
            {
                Debug.LogWarning($"{nameof(PlayerCockpitHUD)} has no HUD Root assigned.", this);
                return;
            }

            hudRoot.SetActive(visible);
        }

#endregion
#region Damage Feedback
        private void UpdateDamageFeedback()
        {
            float currentDamage = participant.Vehicle.Damage.Percent;
            RaceShieldState shield = participant.Vehicle.EquipmentSystem.Shield;
            float currentShield = shield != null ? shield.Current : 0f;
            float currentShieldMaximum = shield != null ? shield.Maximum : 0f;
            bool bikeWasHit = currentDamage > previousDamage + 0.001f;
            float shieldLoss = Mathf.Max(0f, previousShield - currentShield);
            float capacityLoss = Mathf.Max(0f, previousShieldMaximum - currentShieldMaximum);
            float damageRelatedShieldLoss = Mathf.Max(0f, shieldLoss - capacityLoss);
            bool shieldWasHit = shield != null && damageRelatedShieldLoss > 0.001f;
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

            damageFlashTimer = Mathf.Max(0f, damageFlashTimer - Time.deltaTime);
            float normalized = damageFlashDuration > 0f ? damageFlashTimer / damageFlashDuration : 0f;
            SetDamageFlashAlpha(normalized * damageFlashMaximumAlpha);
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
