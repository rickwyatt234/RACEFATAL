using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(RacerViewController))]
    public class BikeCombatFeedback : MonoBehaviour
    {
        #region Origins

        [Header("Effect Origins")]
        [Tooltip("Origin used for shield hit/break effects. Defaults to the bike root.")]
        [SerializeField] private Transform shieldEffectOrigin;

        [Tooltip("Origin used for hull hit effects. Defaults to the bike root.")]
        [SerializeField] private Transform hullEffectOrigin;

        #endregion

        #region Shield

        [Header("Shield Audio")]
        [SerializeField] private AudioClip shieldHitClip;
        [SerializeField] private AudioClip shieldDepletedClip;

        [Header("Shield VFX")]
        [SerializeField] private ParticleSystem shieldHitPrefab;
        [SerializeField] private ParticleSystem shieldDepletedPrefab;

        #endregion

        #region Hull

        [Header("Hull Audio")]
        [SerializeField] private AudioClip hullHitClip;

        [Header("Hull VFX")]
        [SerializeField] private ParticleSystem hullHitPrefab;

        #endregion

        #region Collision Audio

        [Header("Collision Audio")]
        [Tooltip("Additional impact sound used when another racer causes the damage.")]
        [SerializeField] private AudioClip racerCollisionClip;

        [Tooltip("Additional impact sound used for track-wall/environmental damage.")]
        [SerializeField] private AudioClip wallCollisionClip;

        [Min(0f)][SerializeField] private float collisionAudioCooldown = 0.08f;

        #endregion

        #region Audio

        [Header("Audio Source")]
        [Tooltip("3D AudioSource used for one-shot combat sounds.")]
        [SerializeField] private AudioSource oneShotAudioSource;

        [Range(0f, 1f)][SerializeField] private float shieldVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float hullVolume = 1f;
        [Range(0f, 1f)][SerializeField] private float collisionVolume = 1f;

        #endregion

        #region Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugBound;
        [SerializeField] private string debugLastDamageCause = "None";
        [SerializeField] private float debugLastShieldDamage;
        [SerializeField] private float debugLastHullDamage;
        [SerializeField] private bool debugLastShieldDepleted;

        #endregion

        private RacerViewController racerView;
        private RaceRuntimeController runtime;

        private bool bound;
        private float lastCollisionAudioTime = -1000f;

        private void Awake()
        {
            racerView = GetComponent<RacerViewController>();

            if (shieldEffectOrigin == null)
                shieldEffectOrigin = transform;

            if (hullEffectOrigin == null)
                hullEffectOrigin = transform;
        }

        private void Update()
        {
            if (!bound)
                TryBind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void TryBind()
        {
            if (racerView == null ||
                !racerView.IsInitialized ||
                racerView.Participant == null)
            {
                return;
            }

            if (runtime == null)
                runtime = FindFirstObjectByType<RaceRuntimeController>();

            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.DamageApplied += OnDamageApplied;

            bound = true;
            debugBound = true;
        }

        private void Unbind()
        {
            if (!bound ||
                runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.DamageApplied -= OnDamageApplied;

            bound = false;
        }

        private void OnDamageApplied(DamageEvent damageEvent)
        {
            if (racerView == null ||
                racerView.Participant == null ||
                damageEvent.VictimRacerId != racerView.RacerId)
            {
                return;
            }

            debugLastDamageCause = damageEvent.Cause.ToString();
            debugLastShieldDamage = damageEvent.ShieldAbsorbed;
            debugLastHullDamage = damageEvent.BikeDamage;

            PlayCollisionFeedback(damageEvent);

            if (damageEvent.ShieldAbsorbed > 0f)
                PlayShieldFeedback();

            if (damageEvent.BikeDamage > 0f)
                PlayHullFeedback();
        }

        private void PlayShieldFeedback()
        {
            RaceShieldState shield =
                racerView.Participant.Vehicle
                    .EquipmentSystem.Shield;

            bool depleted =
                shield != null &&
                shield.IsDepleted;

            debugLastShieldDepleted = depleted;

            if (depleted)
            {
                PlayOneShot(
                    shieldDepletedClip,
                    shieldVolume);

                SpawnParticle(
                    shieldDepletedPrefab,
                    shieldEffectOrigin);

                return;
            }

            PlayOneShot(
                shieldHitClip,
                shieldVolume);

            SpawnParticle(
                shieldHitPrefab,
                shieldEffectOrigin);
        }

        private void PlayHullFeedback()
        {
            PlayOneShot(
                hullHitClip,
                hullVolume);

            SpawnParticle(
                hullHitPrefab,
                hullEffectOrigin);
        }

        private void PlayCollisionFeedback(
            DamageEvent damageEvent)
        {
            if (Time.time - lastCollisionAudioTime <
                collisionAudioCooldown)
            {
                return;
            }

            AudioClip clip = null;

            if (damageEvent.Cause == DamageCause.Collision)
                clip = racerCollisionClip;

            if (damageEvent.Cause == DamageCause.Environmental)
                clip = wallCollisionClip;

            if (clip == null)
                return;

            lastCollisionAudioTime = Time.time;

            PlayOneShot(
                clip,
                collisionVolume);
        }

        private void PlayOneShot(
            AudioClip clip,
            float volume)
        {
            if (oneShotAudioSource == null ||
                clip == null)
            {
                return;
            }

            oneShotAudioSource.PlayOneShot(
                clip,
                Mathf.Clamp01(volume));
        }

        private void SpawnParticle(
            ParticleSystem prefab,
            Transform origin)
        {
            if (prefab == null)
                return;

            Transform spawnOrigin =
                origin != null
                    ? origin
                    : transform;

            ParticleSystem instance =
                Instantiate(
                    prefab,
                    spawnOrigin.position,
                    spawnOrigin.rotation);

            instance.Play(true);

            Destroy(
                instance.gameObject,
                GetParticleLifetime(instance));
        }

        private float GetParticleLifetime(
            ParticleSystem particles)
        {
            if (particles == null)
                return 5f;

            ParticleSystem.MainModule main =
                particles.main;

            return main.duration +
                   main.startLifetime.constantMax +
                   1f;
        }
    }
}