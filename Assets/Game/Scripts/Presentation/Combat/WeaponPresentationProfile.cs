using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [CreateAssetMenu(
        fileName = "WeaponPresentation_",
        menuName = "RACE FATAL/Presentation/Weapon Presentation")]
    public class WeaponPresentationProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Must exactly match the corresponding WeaponDefinition ID.")]
        [SerializeField] private string weaponDefinitionId;

        [Header("Projectile")]
        [Tooltip("Projectile prefab used by this weapon. May be empty for hitscan or area weapons.")]
        [SerializeField] private ProjectileView projectilePrefab;

        [Header("Muzzle")]
        [Tooltip("VFX prefab spawned at the physical weapon origin whenever this weapon fires.")]
        [SerializeField] private GameObject muzzlePrefab;

        [Tooltip("Used if the muzzle prefab contains no ParticleSystem from which a lifetime can be determined.")]
        [Min(0.01f)][SerializeField] private float muzzleFallbackLifetime = 2f;

        [Header("Fire Audio")]
        [Tooltip("Randomly selects from these clips. Add several variations for frequently fired weapons.")]
        [SerializeField] private AudioClip[] fireClips;

        [Range(0f, 1f)][SerializeField] private float fireVolume = 1f;

        [Tooltip("Small random volume variation applied independently to every shot.")]
        [Range(0f, 0.25f)][SerializeField] private float fireVolumeVariation = 0.04f;

        [Tooltip("Minimum random pitch applied to each shot.")]
        [Range(0.5f, 2f)][SerializeField] private float minimumFirePitch = 0.97f;

        [Tooltip("Maximum random pitch applied to each shot.")]
        [Range(0.5f, 2f)][SerializeField] private float maximumFirePitch = 1.03f;

        [Header("Fire Audio - 3D")]
        [Tooltip("0 = completely 2D. 1 = completely positional 3D audio.")]
        [Range(0f, 1f)][SerializeField] private float fireSpatialBlend = 1f;

        [Tooltip("Inside this distance, the weapon remains at essentially full volume.")]
        [Min(0.01f)][SerializeField] private float fireMinDistance = 3f;

        [Tooltip("Maximum distance at which the weapon can still be heard.")]
        [Min(0.1f)][SerializeField] private float fireMaxDistance = 120f;

        [SerializeField] private AudioRolloffMode fireRolloffMode =
            AudioRolloffMode.Logarithmic;

        [Tooltip("Doppler strength for moving racers. Keep fairly low for very fast bikes.")]
        [Range(0f, 1f)][SerializeField] private float fireDopplerLevel = 0.1f;

        public string WeaponDefinitionId => weaponDefinitionId;
        public ProjectileView ProjectilePrefab => projectilePrefab;
        public GameObject MuzzlePrefab => muzzlePrefab;
        public float MuzzleFallbackLifetime => muzzleFallbackLifetime;

        public AudioClip[] FireClips => fireClips;
        public float FireVolume => fireVolume;
        public float FireVolumeVariation => fireVolumeVariation;
        public float MinimumFirePitch => minimumFirePitch;
        public float MaximumFirePitch => maximumFirePitch;

        public float FireSpatialBlend => fireSpatialBlend;
        public float FireMinDistance => fireMinDistance;
        public float FireMaxDistance => fireMaxDistance;
        public AudioRolloffMode FireRolloffMode => fireRolloffMode;
        public float FireDopplerLevel => fireDopplerLevel;

        private void OnValidate()
        {
            maximumFirePitch = Mathf.Max(
                minimumFirePitch,
                maximumFirePitch);

            fireMaxDistance = Mathf.Max(
                fireMinDistance,
                fireMaxDistance);
        }
    }
}