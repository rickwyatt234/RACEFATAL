using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public enum WeaponReticlePlacement { BikeForward, FixedViewport }

    [CreateAssetMenu(fileName = "WeaponPresentation_", menuName = "RACE FATAL/Presentation/Weapon Presentation")]
    public class WeaponPresentationProfile : ScriptableObject
    {
        [Header("Identity")] [Tooltip("Must exactly match the corresponding WeaponDefinition ID.")] [SerializeField] private string weaponDefinitionId;
        [Header("Gun Reticle")]
        [Tooltip("Optional UI sprite for forward guns. Empty uses a built-in crosshair. Locking weapons keep their targeting HUD.")]
        [SerializeField] private Sprite reticleSprite;
        [SerializeField] private Vector2 reticleSize = new Vector2(36f, 36f);
        [SerializeField] private Color reticleColor = new Color(0.4f, 1f, 1f, 0.9f);
        [Tooltip("Bike Forward keeps camera framing independent of gun aim. Fixed Viewport aims at an absolute screen position.")]
        [SerializeField] private WeaponReticlePlacement reticlePlacement;
        [Tooltip("Normalized screen position for Fixed Viewport: (0.5, 0.5) is the center.")]
        [SerializeField] private Vector2 reticleViewportPosition = new Vector2(0.5f, 0.5f);
        [Tooltip("Normalized screen offset applied to either placement mode. Positive Y moves the reticle and aim upward.")]
        [SerializeField] private Vector2 reticleViewportOffset;
        [Header("Projectile")] [Tooltip("Projectile prefab used by this weapon. May be empty for hitscan or area weapons.")] [SerializeField] private
            ProjectileView projectilePrefab;
        [Header("Muzzle")] [Tooltip("VFX prefab spawned at the physical weapon origin whenever this weapon fires.")] [SerializeField] private
            GameObject muzzlePrefab;
        [Tooltip("Used if the muzzle prefab contains no ParticleSystem from which a lifetime can be determined.")] [Min(0.01f)] [SerializeField] private float muzzleFallbackLifetime =
            2f;
        [Header("Charge VFX")] [Tooltip("Looping/continuous effect attached to the muzzle while a ChargeRelease weapon is charging.")] [SerializeField] private
            GameObject chargePrefab;
        [Tooltip("One-shot effect spawned when the weapon reaches full charge.")] [SerializeField] private GameObject fullChargePrefab;
        [Min(0.01f)] [SerializeField] private float fullChargeFallbackLifetime = 2f;
        [Header("Charge Audio")] [Tooltip("Looping charging sound. Pitch and volume rise with charge percentage.")] [SerializeField] private
            AudioClip chargeLoopClip;
        [Tooltip("Optional one-shot sound when full charge is reached.")] [SerializeField] private AudioClip fullChargeClip;
        [Range(0f, 1f)] [SerializeField] private float minimumChargeVolume = 0.2f;
        [Range(0f, 1f)] [SerializeField] private float maximumChargeVolume = 0.8f;
        [Range(0.5f, 2f)] [SerializeField] private float minimumChargePitch = 0.7f;
        [Range(0.5f, 2f)] [SerializeField] private float maximumChargePitch = 1.25f;
        [Range(0f, 1f)] [SerializeField] private float fullChargeVolume = 1f;
        [Header("Fire Audio")] [Tooltip("Randomly selects from these clips. Add several variations for frequently fired weapons.")] [SerializeField] private AudioClip[]
            fireClips;
        [Range(0f, 1f)] [SerializeField] private float fireVolume = 1f;
        [Tooltip("Small random volume variation applied independently to every shot.")] [Range(0f,
            0.25f)] [SerializeField] private float fireVolumeVariation = 0.04f;
        [Range(0.5f, 2f)] [SerializeField] private float minimumFirePitch = 0.97f;
        [Range(0.5f, 2f)] [SerializeField] private float maximumFirePitch = 1.03f;
        [Header("Impact / Secondary Feedback")] [Tooltip("Optional world-space effect used by mines, ram impacts, and status applications.")] [SerializeField] private
            GameObject impactPrefab;
        [Min(0.01f)] [SerializeField] private float impactFallbackLifetime = 2f;
        [Tooltip("Optional impact/trigger sounds used by special weapons.")] [SerializeField] private AudioClip[] impactClips;
        [Range(0f, 1f)] [SerializeField] private float impactVolume = 1f;
        [Header("Fire / Charge Audio - 3D")] [Range(0f, 1f)] [SerializeField] private float fireSpatialBlend = 1f;
        [Min(0.01f)] [SerializeField] private float fireMinDistance = 3f;
        [Min(0.1f)] [SerializeField] private float fireMaxDistance = 120f;
        [SerializeField] private AudioRolloffMode fireRolloffMode = AudioRolloffMode.Logarithmic;
        [Range(0f, 1f)] [SerializeField] private float fireDopplerLevel = 0.1f;
        public Sprite ReticleSprite => reticleSprite;
        public Vector2 ReticleSize => reticleSize;
        public Color ReticleColor => reticleColor;
        public WeaponReticlePlacement ReticlePlacement => reticlePlacement;
        public Vector2 ReticleViewportPosition => reticleViewportPosition;
        public Vector2 ReticleViewportOffset => reticleViewportOffset;
        public string WeaponDefinitionId => weaponDefinitionId;
        public ProjectileView ProjectilePrefab => projectilePrefab;
        public GameObject MuzzlePrefab => muzzlePrefab;
        public float MuzzleFallbackLifetime => muzzleFallbackLifetime;
        public GameObject ChargePrefab => chargePrefab;
        public GameObject FullChargePrefab => fullChargePrefab;
        public float FullChargeFallbackLifetime => fullChargeFallbackLifetime;
        public AudioClip ChargeLoopClip => chargeLoopClip;
        public AudioClip FullChargeClip => fullChargeClip;
        public float MinimumChargeVolume => minimumChargeVolume;
        public float MaximumChargeVolume => maximumChargeVolume;
        public float MinimumChargePitch => minimumChargePitch;
        public float MaximumChargePitch => maximumChargePitch;
        public float FullChargeVolume => fullChargeVolume;
        public AudioClip[] FireClips => fireClips;
        public float FireVolume => fireVolume;
        public float FireVolumeVariation => fireVolumeVariation;
        public float MinimumFirePitch => minimumFirePitch;
        public float MaximumFirePitch => maximumFirePitch;
        public GameObject ImpactPrefab => impactPrefab;
        public float ImpactFallbackLifetime => impactFallbackLifetime;
        public AudioClip[] ImpactClips => impactClips;
        public float ImpactVolume => impactVolume;
        public float FireSpatialBlend => fireSpatialBlend;
        public float FireMinDistance => fireMinDistance;
        public float FireMaxDistance => fireMaxDistance;
        public AudioRolloffMode FireRolloffMode => fireRolloffMode;
        public float FireDopplerLevel => fireDopplerLevel;

        private void OnValidate()
        {
            reticleSize = new Vector2(Mathf.Max(1f, reticleSize.x), Mathf.Max(1f, reticleSize.y));
            reticleViewportPosition = new Vector2(Mathf.Clamp01(reticleViewportPosition.x), Mathf.Clamp01(reticleViewportPosition.y));
            maximumFirePitch = Mathf.Max(minimumFirePitch, maximumFirePitch);
            maximumChargePitch = Mathf.Max(minimumChargePitch, maximumChargePitch);
            maximumChargeVolume = Mathf.Max(minimumChargeVolume, maximumChargeVolume);
            fireMaxDistance = Mathf.Max(fireMinDistance, fireMaxDistance);
        }
    }
}
