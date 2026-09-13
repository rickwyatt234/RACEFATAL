using RaceFatal.Combat;
using RaceFatal.Presentation.Racing;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    [RequireComponent(typeof(Collider))]
    public class ProjectileView : MonoBehaviour
    {
        [Header("Swept Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [Min(0.001f)][SerializeField] private float sweepRadius = 0.04f;
        [Min(4)][SerializeField] private int hitBufferSize = 16;

        [Header("Orientation")]
        [SerializeField] private bool alignToTravelDirection = true;

        [Header("Impact")]
        [SerializeField] private ParticleSystem impactPrefab;
        [SerializeField] private bool alignImpactToNormal = true;
        [Min(0.1f)][SerializeField] private float impactLifetimeFallback = 3f;

        [Header("Trails")]
        [SerializeField] private TrailRenderer[] trails;
        [SerializeField] private bool detachTrails = true;
        [Min(0f)][SerializeField] private float trailCleanupPadding = 0.1f;

        [Header("Runtime Debug")]
        [SerializeField] private bool debugInitialized;
        [SerializeField] private bool debugResolved;
        [SerializeField] private float debugDistanceTravelled;
        [SerializeField] private float debugCurrentSpeed;
        [SerializeField] private string debugLastHit = "None";
        [SerializeField] private string debugLastVictim = "None";
        [SerializeField] private string debugImpactSide = "Unknown";

        private Collider projectileCollider;
        private RaycastHit[] hitBuffer;

        private RaceRuntimeController runtime;
        private string attackerRacerId;

        private float damage;
        private float speed;
        private float currentMovementSpeed;
        private float maximumRange;
        private float distanceTravelled;

        private Vector3 direction;

        private bool initialized;
        private bool resolved;

        protected Vector3 CurrentDirection => direction;
        protected float BaseSpeed => speed;
        protected float CurrentSpeed => currentMovementSpeed;
        protected float MaximumRange => maximumRange;
        protected float DistanceTravelled => distanceTravelled;
        protected string AttackerRacerId => attackerRacerId;
        protected RaceRuntimeController Runtime => runtime;
        protected bool IsResolved => resolved;

        protected virtual void Awake()
        {
            projectileCollider = GetComponent<Collider>();

            if (projectileCollider != null)
                projectileCollider.isTrigger = true;

            hitBuffer = new RaycastHit[Mathf.Max(4, hitBufferSize)];

            if (trails == null || trails.Length == 0)
                trails = GetComponentsInChildren<TrailRenderer>(true);
        }

        protected virtual void Update()
        {
            if (!initialized || resolved)
                return;

            MoveProjectile(Time.deltaTime);
        }

        public void Initialize(
            RaceRuntimeController raceRuntime,
            string attackerId,
            float projectileDamage,
            float projectileSpeed,
            float range,
            Vector3 launchDirection)
        {
            runtime = raceRuntime;
            attackerRacerId = attackerId;

            damage = Mathf.Max(0f, projectileDamage);
            speed = Mathf.Max(0f, projectileSpeed);
            currentMovementSpeed = speed;
            maximumRange = Mathf.Max(0f, range);

            direction =
                launchDirection.sqrMagnitude > 0.001f
                    ? launchDirection.normalized
                    : transform.forward;

            distanceTravelled = 0f;
            resolved = false;
            initialized = true;

            debugInitialized = true;
            debugResolved = false;
            debugDistanceTravelled = 0f;
            debugCurrentSpeed = currentMovementSpeed;
            debugLastHit = "None";
            debugLastVictim = "None";
            debugImpactSide = "Unknown";

            UpdateProjectileRotation();
        }

        protected virtual float ResolveMovementSpeed(float deltaTime)
        {
            return speed;
        }

        protected virtual Vector3 ResolveMovementDirection(float deltaTime)
        {
            return direction;
        }

        private void MoveProjectile(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            currentMovementSpeed =
                Mathf.Max(
                    0f,
                    ResolveMovementSpeed(deltaTime));

            debugCurrentSpeed =
                currentMovementSpeed;

            if (currentMovementSpeed <= 0f)
                return;

            Vector3 resolvedDirection =
                ResolveMovementDirection(deltaTime);

            if (resolvedDirection.sqrMagnitude > 0.001f)
                direction = resolvedDirection.normalized;

            UpdateProjectileRotation();

            float rangeRemaining =
                maximumRange - distanceTravelled;

            if (rangeRemaining <= 0f)
            {
                ExpireProjectile();
                return;
            }

            float stepDistance =
                Mathf.Min(
                    currentMovementSpeed * deltaTime,
                    rangeRemaining);

            if (stepDistance <= 0f)
                return;

            Vector3 start =
                transform.position;

            if (TryGetFirstValidHit(
                    start,
                    direction,
                    stepDistance,
                    out RaycastHit hit))
            {
                distanceTravelled += hit.distance;
                debugDistanceTravelled = distanceTravelled;

                transform.position = hit.point;

                ResolveImpact(
                    hit.collider,
                    hit.point,
                    hit.normal);

                return;
            }

            transform.position =
                start +
                direction * stepDistance;

            distanceTravelled += stepDistance;
            debugDistanceTravelled = distanceTravelled;

            if (distanceTravelled >= maximumRange)
                ExpireProjectile();
        }

        private void UpdateProjectileRotation()
        {
            if (!alignToTravelDirection ||
                direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 up =
                transform.up;

            if (Mathf.Abs(
                    Vector3.Dot(
                        direction,
                        up)) > 0.98f)
            {
                up = transform.right;
            }

            transform.rotation =
                Quaternion.LookRotation(
                    direction,
                    up);
        }

        private bool TryGetFirstValidHit(
            Vector3 origin,
            Vector3 castDirection,
            float distance,
            out RaycastHit nearestHit)
        {
            nearestHit = default;

            int hitCount =
                Physics.SphereCastNonAlloc(
                    origin,
                    sweepRadius,
                    castDirection,
                    hitBuffer,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Collide);

            if (hitCount <= 0)
                return false;

            bool found = false;
            float nearestDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hitBuffer[i];

                if (hit.collider == null)
                    continue;

                if (!IsValidHitCollider(hit.collider))
                    continue;

                if (hit.distance >= nearestDistance)
                    continue;

                nearestDistance = hit.distance;
                nearestHit = hit;
                found = true;
            }

            return found;
        }

        private bool IsValidHitCollider(Collider hitCollider)
        {
            if (hitCollider == null)
                return false;

            if (IsProjectileCollider(hitCollider))
                return false;

            if (IsOwnerCollider(hitCollider))
                return false;

            if (!hitCollider.isTrigger)
                return true;

            return hitCollider.GetComponentInParent<RacerViewController>() != null;
        }

        private bool IsProjectileCollider(Collider hitCollider)
        {
            if (hitCollider == projectileCollider)
                return true;

            return hitCollider.transform == transform ||
                   hitCollider.transform.IsChildOf(transform);
        }

        private bool IsOwnerCollider(Collider hitCollider)
        {
            if (hitCollider == null ||
                string.IsNullOrWhiteSpace(attackerRacerId))
            {
                return false;
            }

            RacerViewController racer =
                hitCollider.GetComponentInParent<RacerViewController>();

            return racer != null &&
                   racer.IsInitialized &&
                   racer.RacerId == attackerRacerId;
        }

        private void ResolveImpact(
            Collider hitCollider,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            if (resolved)
                return;

            resolved = true;
            debugResolved = true;

            if (hitCollider != null)
                debugLastHit = hitCollider.name;

            RacerViewController victim =
                hitCollider != null
                    ? hitCollider.GetComponentInParent<RacerViewController>()
                    : null;

            if (victim != null &&
                victim.IsInitialized &&
                victim.RacerId != attackerRacerId)
            {
                DamageImpactSide impactSide =
                    ResolveImpactSide(
                        victim.transform,
                        hitPoint,
                        hitNormal);

                debugLastVictim = victim.RacerId;
                debugImpactSide = impactSide.ToString();

                if (runtime != null &&
                    runtime.Director != null)
                {
                    runtime.Director.ApplyDamage(
                        attackerRacerId,
                        victim.RacerId,
                        damage,
                        DamageCause.Weapon,
                        impactSide);
                }
            }

            SpawnImpact(hitPoint, hitNormal);
            FinishProjectile();
        }

        private DamageImpactSide ResolveImpactSide(
            Transform victimTransform,
            Vector3 hitPoint,
            Vector3 hitNormal)
        {
            if (victimTransform == null)
                return DamageImpactSide.Unknown;

            if (hitNormal.sqrMagnitude > 0.001f)
            {
                Vector3 localNormal =
                    victimTransform.InverseTransformDirection(
                        hitNormal.normalized);

                localNormal.y = 0f;

                if (localNormal.sqrMagnitude > 0.001f)
                    return ClassifyLocalDirection(localNormal);
            }

            Vector3 localPoint =
                victimTransform.InverseTransformPoint(hitPoint);

            localPoint.y = 0f;

            if (localPoint.sqrMagnitude > 0.001f)
                return ClassifyLocalDirection(localPoint);

            Vector3 localApproach =
                victimTransform.InverseTransformDirection(
                    -direction);

            localApproach.y = 0f;

            if (localApproach.sqrMagnitude > 0.001f)
                return ClassifyLocalDirection(localApproach);

            return DamageImpactSide.Unknown;
        }

        private DamageImpactSide ClassifyLocalDirection(Vector3 localDirection)
        {
            localDirection.Normalize();

            float forwardAmount =
                Mathf.Abs(localDirection.z);

            float sideAmount =
                Mathf.Abs(localDirection.x);

            if (forwardAmount >= sideAmount)
            {
                return localDirection.z >= 0f
                    ? DamageImpactSide.Front
                    : DamageImpactSide.Rear;
            }

            return localDirection.x >= 0f
                ? DamageImpactSide.Right
                : DamageImpactSide.Left;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!initialized ||
                resolved ||
                other == null)
            {
                return;
            }

            if (!IsValidHitCollider(other))
                return;

            Vector3 hitPoint =
                other.ClosestPoint(
                    transform.position);

            Vector3 hitNormal =
                -direction;

            ResolveImpact(
                other,
                hitPoint,
                hitNormal);
        }

        private void SpawnImpact(
            Vector3 position,
            Vector3 normal)
        {
            if (impactPrefab == null)
                return;

            if (normal.sqrMagnitude < 0.001f)
                normal = -direction;

            Quaternion rotation =
                alignImpactToNormal
                    ? Quaternion.LookRotation(normal.normalized)
                    : Quaternion.identity;

            ParticleSystem effect =
                Instantiate(
                    impactPrefab,
                    position,
                    rotation);

            effect.Play(true);

            Destroy(
                effect.gameObject,
                GetParticleLifetime(effect));
        }

        private float GetParticleLifetime(ParticleSystem root)
        {
            if (root == null)
                return impactLifetimeFallback;

            ParticleSystem[] systems =
                root.GetComponentsInChildren<ParticleSystem>(true);

            float longest = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem system = systems[i];

                if (system == null)
                    continue;

                ParticleSystem.MainModule main =
                    system.main;

                float lifetime =
                    main.duration +
                    main.startDelay.constantMax +
                    main.startLifetime.constantMax;

                longest =
                    Mathf.Max(
                        longest,
                        lifetime);
            }

            return longest > 0f
                ? longest + 0.25f
                : impactLifetimeFallback;
        }

        private void ExpireProjectile()
        {
            if (resolved)
                return;

            resolved = true;
            debugResolved = true;

            FinishProjectile();
        }

        private void FinishProjectile()
        {
            if (projectileCollider != null)
                projectileCollider.enabled = false;

            PreserveTrails();
            Destroy(gameObject);
        }

        private void PreserveTrails()
        {
            if (trails == null)
                return;

            for (int i = 0; i < trails.Length; i++)
            {
                TrailRenderer trail = trails[i];

                if (trail == null)
                    continue;

                trail.emitting = false;

                if (!detachTrails ||
                    trail.transform == transform ||
                    !trail.transform.IsChildOf(transform))
                {
                    continue;
                }

                trail.transform.SetParent(null, true);

                Destroy(
                    trail.gameObject,
                    Mathf.Max(
                        0.05f,
                        trail.time +
                        trailCleanupPadding));
            }
        }
    }
}