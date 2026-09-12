using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(Collider))]
    public class ProjectileView : MonoBehaviour
    {
        #region Lifetime

        [Header("Lifetime")]
        [Min(0.1f)][SerializeField] private float maximumLifetime = 10f;

        #endregion

        #region Flight Feedback

        [Header("Flight Audio")]
        [Tooltip("Optional AudioSource located on the projectile prefab.")]
        [SerializeField] private AudioSource flightAudioSource;

        [Tooltip("Optional looping sound played while the projectile is flying.")]
        [SerializeField] private AudioClip flightLoopClip;

        [Range(0f, 1f)][SerializeField] private float flightVolume = 1f;

        #endregion

        #region Impact Feedback

        [Header("Impact VFX")]
        [Tooltip("Optional particle effect spawned when this projectile strikes a solid collider.")]
        [SerializeField] private ParticleSystem impactPrefab;

        [Tooltip("Small offset preventing impact particles from clipping into surfaces.")]
        [Min(0f)][SerializeField] private float impactOffset = 0.01f;

        [Header("Impact Audio")]
        [SerializeField] private AudioClip impactClip;

        [Range(0f, 1f)][SerializeField] private float impactVolume = 1f;

        [Tooltip("Random pitch variation applied to impact sounds.")]
        [Range(0f, 0.5f)][SerializeField] private float impactPitchVariation = 0.05f;

        #endregion

        #region Runtime Debug

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugImpactResolved;
        [SerializeField] private string debugAttackerRacerId;
        [SerializeField] private string debugVictimRacerId = "None";
        [SerializeField] private float debugRemainingRange;
        [SerializeField] private float debugLifetime;
        [SerializeField] private Vector3 debugTravelDirection;
        [SerializeField] private float debugSpeed;

        #endregion

        private RaceRuntimeController runtime;
        private Collider projectileCollider;

        private string attackerRacerId;
        private Vector3 travelDirection;

        private float damage;
        private float speed;
        private float remainingRange;
        private float lifetime;

        private bool initialized;
        private bool impactResolved;

        private void Awake()
        {
            projectileCollider =
                GetComponent<Collider>();
        }

        public void Initialize(
            RaceRuntimeController raceRuntime,
            string attackerId,
            float projectileDamage,
            float projectileSpeed,
            float maximumRange,
            Vector3 worldDirection)
        {
            runtime = raceRuntime;
            attackerRacerId = attackerId;

            damage = projectileDamage;
            speed = Mathf.Max(0f, projectileSpeed);
            remainingRange = Mathf.Max(0f, maximumRange);

            travelDirection =
                worldDirection.sqrMagnitude > 0.001f
                    ? worldDirection.normalized
                    : transform.forward;

            transform.rotation =
                Quaternion.LookRotation(
                    travelDirection,
                    transform.up);

            lifetime = 0f;
            impactResolved = false;

            debugAttackerRacerId = attackerRacerId;
            debugRemainingRange = remainingRange;
            debugLifetime = 0f;
            debugImpactResolved = false;
            debugVictimRacerId = "None";
            debugTravelDirection = travelDirection;
            debugSpeed = speed;

            IgnoreOwnerCollisions();
            StartFlightAudio();

            initialized = true;
            debugInitialized = true;
        }

        private void Update()
        {
            if (!initialized ||
                impactResolved)
            {
                return;
            }

            float movement =
                speed *
                Time.deltaTime;

            transform.position +=
                travelDirection *
                movement;

            remainingRange -= movement;
            lifetime += Time.deltaTime;

            debugRemainingRange = remainingRange;
            debugLifetime = lifetime;

            if (remainingRange <= 0f ||
                lifetime >= maximumLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(
            Collider other)
        {
            if (!initialized ||
                impactResolved ||
                other == null ||
                other.isTrigger)
            {
                return;
            }

            RacerViewController victim =
                other.GetComponentInParent<RacerViewController>();

            if (victim != null &&
                victim.RacerId == attackerRacerId)
            {
                return;
            }

            impactResolved = true;
            debugImpactResolved = true;

            Vector3 impactPoint =
                other.ClosestPoint(
                    transform.position);

            Vector3 impactNormal =
                -travelDirection;

            SpawnImpactVFX(
                impactPoint,
                impactNormal);

            PlayImpactAudio(
                impactPoint);

            if (victim != null &&
                victim.IsInitialized)
            {
                debugVictimRacerId =
                    victim.RacerId;

                runtime?.Director?.ApplyDamage(
                    attackerRacerId,
                    victim.RacerId,
                    damage,
                    DamageCause.Weapon);
            }

            Destroy(gameObject);
        }

        #region Flight Audio

        private void StartFlightAudio()
        {
            if (flightAudioSource == null ||
                flightLoopClip == null)
            {
                return;
            }

            flightAudioSource.clip =
                flightLoopClip;

            flightAudioSource.loop =
                true;

            flightAudioSource.volume =
                Mathf.Clamp01(
                    flightVolume);

            flightAudioSource.Play();
        }

        #endregion

        #region Impact Feedback

        private void SpawnImpactVFX(
            Vector3 impactPoint,
            Vector3 impactNormal)
        {
            if (impactPrefab == null)
                return;

            Quaternion rotation =
                Quaternion.LookRotation(
                    impactNormal);

            ParticleSystem impact =
                Instantiate(
                    impactPrefab,
                    impactPoint +
                        impactNormal *
                        impactOffset,
                    rotation);

            impact.Play(true);

            Destroy(
                impact.gameObject,
                GetParticleLifetime(impact));
        }

        private void PlayImpactAudio(
            Vector3 impactPoint)
        {
            if (impactClip == null)
                return;

            GameObject audioObject =
                new GameObject(
                    "ProjectileImpactAudio");

            audioObject.transform.position =
                impactPoint;

            AudioSource source =
                audioObject.AddComponent<AudioSource>();

            source.clip =
                impactClip;

            source.volume =
                Mathf.Clamp01(
                    impactVolume);

            source.spatialBlend = 1f;

            source.pitch =
                Random.Range(
                    1f - impactPitchVariation,
                    1f + impactPitchVariation);

            source.Play();

            Destroy(
                audioObject,
                impactClip.length /
                Mathf.Max(
                    0.01f,
                    Mathf.Abs(source.pitch)) +
                0.1f);
        }

        private float GetParticleLifetime(
            ParticleSystem particles)
        {
            ParticleSystem.MainModule main =
                particles.main;

            return main.duration +
                   main.startLifetime.constantMax +
                   1f;
        }

        #endregion

        #region Collision Ignore

        private void IgnoreOwnerCollisions()
        {
            if (runtime == null ||
                projectileCollider == null ||
                string.IsNullOrEmpty(
                    attackerRacerId))
            {
                return;
            }

            if (!runtime.TryGetRacerView(
                    attackerRacerId,
                    out RacerViewController owner))
            {
                return;
            }

            Collider[] ownerColliders =
                owner.GetComponentsInChildren<Collider>(
                    true);

            for (int i = 0;
                 i < ownerColliders.Length;
                 i++)
            {
                Collider ownerCollider =
                    ownerColliders[i];

                if (ownerCollider == null ||
                    ownerCollider ==
                        projectileCollider)
                {
                    continue;
                }

                Physics.IgnoreCollision(
                    projectileCollider,
                    ownerCollider,
                    true);
            }
        }

        #endregion
    }
}