using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(Collider))]
    public class ProjectileView : MonoBehaviour
    {
        [Header("Lifetime")]
        [Min(0.1f)][SerializeField] private float maximumLifetime = 10f;

        [Header("Impact")]
        [Tooltip("Optional particle effect spawned when this projectile strikes a solid collider.")]
        [SerializeField] private ParticleSystem impactPrefab;

        [Tooltip("Small offset preventing impact particles from clipping into surfaces.")]
        [Min(0f)][SerializeField] private float impactOffset = 0.01f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugImpactResolved;
        [SerializeField] private string debugAttackerRacerId;
        [SerializeField] private string debugVictimRacerId = "None";
        [SerializeField] private float debugRemainingRange;
        [SerializeField] private float debugLifetime;
        [SerializeField] private Vector3 debugTravelDirection;
        [SerializeField] private float debugSpeed;

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
            projectileCollider = GetComponent<Collider>();
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

            /*
             * The projectile's orientation and its movement direction
             * are established once here. Nothing about the bike's
             * later orientation can alter its trajectory.
             */
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

            /*
             * IMPORTANT:
             * Move using the launch direction, not transform.forward.
             */
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
                other == null)
            {
                return;
            }

            if (other.isTrigger)
                return;

            RacerViewController victim =
                other.GetComponentInParent<
                    RacerViewController>();

            if (victim != null &&
                victim.RacerId ==
                    attackerRacerId)
            {
                return;
            }

            impactResolved = true;
            debugImpactResolved = true;

            SpawnImpact(other);

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
                owner.GetComponentsInChildren<
                    Collider>(true);

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

        private void SpawnImpact(
            Collider hitCollider)
        {
            if (impactPrefab == null ||
                hitCollider == null)
            {
                return;
            }

            Vector3 impactPoint =
                hitCollider.ClosestPoint(
                    transform.position);

            Vector3 impactNormal =
                -travelDirection;

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
        }
    }
}