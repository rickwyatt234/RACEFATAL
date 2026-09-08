using System;
using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class RaceWeaponPresenter : MonoBehaviour
    {
        [Serializable]
        private class ProjectileBinding
        {
            [Tooltip("Must match the weapon definition ID exactly.")]
            public string definitionId;

            public ProjectileView prefab;
        }

        [Header("Hitscan / Area")]
        [SerializeField] private LayerMask hitMask = ~0;

        [Tooltip("Maximum number of hits inspected by a single hitscan shot.")]
        [Min(4)][SerializeField] private int hitscanBufferSize = 32;

        [Header("Projectile Prefabs")]
        [SerializeField] private List<ProjectileBinding> projectilePrefabs =
            new List<ProjectileBinding>();

        [Header("Runtime Debug")]
        [SerializeField] private string debugLastShooter;
        [SerializeField] private string debugLastWeapon;
        [SerializeField] private bool debugUsedPlayerAim;
        [SerializeField] private bool debugLastShotHit;
        [SerializeField] private string debugLastVictim = "None";
        [SerializeField] private Vector3 debugLastFireDirection;
        [SerializeField] private Vector3 debugLastHitPoint;

        private readonly Dictionary<string, ProjectileView> projectileLookup =
            new Dictionary<string, ProjectileView>();

        private RaceRuntimeController runtime;
        private RaycastHit[] hitscanBuffer;

        private void Awake()
        {
            hitscanBuffer =
                new RaycastHit[
                    Mathf.Max(
                        4,
                        hitscanBufferSize)];

            BuildProjectileLookup();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuntime();
        }

        public void Initialize(
            RaceRuntimeController raceRuntime)
        {
            UnsubscribeFromRuntime();

            runtime =
                raceRuntime;

            if (runtime == null)
            {
                Debug.LogError(
                    "RaceWeaponPresenter requires a RaceRuntimeController.",
                    this);

                return;
            }

            if (runtime.Director == null)
            {
                Debug.LogError(
                    "RaceRuntimeController does not have an initialized RaceDirector.",
                    this);

                return;
            }

            runtime.Director.WeaponFired +=
                OnWeaponFired;
        }

        private void UnsubscribeFromRuntime()
        {
            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.WeaponFired -=
                OnWeaponFired;
        }

        private void BuildProjectileLookup()
        {
            projectileLookup.Clear();

            foreach (ProjectileBinding binding in projectilePrefabs)
            {
                if (binding == null ||
                    string.IsNullOrWhiteSpace(
                        binding.definitionId) ||
                    binding.prefab == null)
                {
                    continue;
                }

                if (projectileLookup.ContainsKey(
                        binding.definitionId))
                {
                    Debug.LogWarning(
                        $"Duplicate projectile binding for '{binding.definitionId}'. " +
                        "The later entry will replace the earlier one.",
                        this);
                }

                projectileLookup[
                    binding.definitionId] =
                        binding.prefab;
            }
        }

        private void OnWeaponFired(
            WeaponFireEvent fireEvent)
        {
            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            if (!runtime.TryGetRacerView(
                    fireEvent.RacerId,
                    out RacerViewController racer))
            {
                Debug.LogWarning(
                    $"No RacerViewController was found for racer '{fireEvent.RacerId}'.",
                    this);

                return;
            }

            if (!racer.TryGetEquipmentMount(
                    fireEvent.EquipmentId,
                    out BikeEquipmentMountView mount))
            {
                Debug.LogWarning(
                    $"Racer '{fireEvent.RacerId}' has no physical mount bound " +
                    $"to equipment '{fireEvent.EquipmentId}'.",
                    racer);

                return;
            }

            Transform origin =
                mount.EquipmentOrigin;

            Vector3 fireDirection =
                ResolveFireDirection(
                    racer,
                    origin,
                    fireEvent.Range);

            PlayMuzzleFeedback(
                mount);

            debugLastShooter =
                fireEvent.RacerId;

            debugLastWeapon =
                fireEvent.DefinitionId;

            debugLastFireDirection =
                fireDirection;

            debugLastShotHit = false;
            debugLastVictim = "None";

            switch (fireEvent.DeliveryMode)
            {
                case WeaponDeliveryMode.Hitscan:
                    FireHitscan(
                        fireEvent,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.Projectile:
                    SpawnProjectile(
                        fireEvent,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.GuidedProjectile:
                    /*
                     * Actual homing/locking comes later.
                     * Initial launch still uses the resolved aim direction.
                     */
                    SpawnProjectile(
                        fireEvent,
                        origin,
                        fireDirection);
                    break;

                case WeaponDeliveryMode.Dropped:
                    /*
                     * Dropped weapons should retain their physical
                     * mount direction rather than cockpit convergence.
                     */
                    SpawnProjectile(
                        fireEvent,
                        origin,
                        origin.forward);
                    break;

                case WeaponDeliveryMode.Area:
                    FireArea(
                        fireEvent,
                        origin);
                    break;

                default:
                    Debug.LogWarning(
                        $"Unsupported weapon delivery mode '{fireEvent.DeliveryMode}'.",
                        this);
                    break;
            }
        }

        private void PlayMuzzleFeedback(
            BikeEquipmentMountView mount)
        {
            if (mount == null)
                return;

            WeaponMountFeedbackView feedback =
                mount.GetComponentInChildren<
                    WeaponMountFeedbackView>(true);

            if (feedback != null)
                feedback.PlayFire();
        }

        private Vector3 ResolveFireDirection(
            RacerViewController racer,
            Transform origin,
            float range)
        {
            debugUsedPlayerAim = false;

            if (origin == null)
                return transform.forward;

            PlayerWeaponAim playerAim =
                racer.GetComponent<
                    PlayerWeaponAim>();

            if (playerAim != null &&
                playerAim.TryGetAimDirection(
                    origin,
                    range,
                    hitMask,
                    out Vector3 aimDirection))
            {
                debugUsedPlayerAim = true;

                return aimDirection;
            }

            return origin.forward;
        }

        private void FireHitscan(
            WeaponFireEvent fireEvent,
            Transform origin,
            Vector3 direction)
        {
            if (origin == null ||
                direction.sqrMagnitude <
                    0.001f)
            {
                return;
            }

            direction.Normalize();

            if (!TryGetFirstHitIgnoringOwner(
                    origin.position,
                    direction,
                    fireEvent.Range,
                    fireEvent.RacerId,
                    out RaycastHit hit))
            {
                return;
            }

            debugLastShotHit =
                true;

            debugLastHitPoint =
                hit.point;

            RacerViewController victim =
                hit.collider
                    .GetComponentInParent<
                        RacerViewController>();

            if (victim == null ||
                !victim.IsInitialized)
            {
                return;
            }

            if (victim.RacerId ==
                fireEvent.RacerId)
            {
                return;
            }

            debugLastVictim =
                victim.RacerId;

            runtime.Director.ApplyDamage(
                fireEvent.RacerId,
                victim.RacerId,
                fireEvent.Damage,
                DamageCause.Weapon);
        }

        private bool TryGetFirstHitIgnoringOwner(
            Vector3 origin,
            Vector3 direction,
            float range,
            string ownerRacerId,
            out RaycastHit nearestHit)
        {
            nearestHit = default;

            int hitCount =
                Physics.RaycastNonAlloc(
                    origin,
                    direction,
                    hitscanBuffer,
                    range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            if (hitCount <= 0)
                return false;

            bool found =
                false;

            float nearestDistance =
                float.PositiveInfinity;

            for (int i = 0;
                 i < hitCount;
                 i++)
            {
                RaycastHit hit =
                    hitscanBuffer[i];

                if (hit.collider == null)
                    continue;

                RacerViewController hitRacer =
                    hit.collider
                        .GetComponentInParent<
                            RacerViewController>();

                if (hitRacer != null &&
                    hitRacer.IsInitialized &&
                    hitRacer.RacerId ==
                        ownerRacerId)
                {
                    continue;
                }

                if (hit.distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance =
                    hit.distance;

                nearestHit =
                    hit;

                found =
                    true;
            }

            return found;
        }

        private void SpawnProjectile(
            WeaponFireEvent fireEvent,
            Transform origin,
            Vector3 direction)
        {
            if (origin == null)
                return;

            if (!projectileLookup.TryGetValue(
                    fireEvent.DefinitionId,
                    out ProjectileView prefab))
            {
                Debug.LogWarning(
                    $"No ProjectileView prefab is registered for weapon " +
                    $"definition '{fireEvent.DefinitionId}'.",
                    this);

                return;
            }

            if (prefab == null)
                return;

            Quaternion rotation =
                CalculateProjectileRotation(
                    origin,
                    direction);

            ProjectileView projectile =
                Instantiate(
                    prefab,
                    origin.position,
                    rotation);

            projectile.Initialize(
                runtime,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ProjectileSpeed,
                fireEvent.Range,
                direction);
                    }

        private Quaternion CalculateProjectileRotation(
            Transform origin,
            Vector3 direction)
        {
            if (direction.sqrMagnitude <
                0.001f)
            {
                return origin.rotation;
            }

            direction.Normalize();

            Vector3 up =
                origin.up;

            if (Mathf.Abs(
                    Vector3.Dot(
                        direction,
                        up)) >
                0.98f)
            {
                up =
                    origin.right;
            }

            return Quaternion.LookRotation(
                direction,
                up);
        }

        private void FireArea(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            if (origin == null)
                return;

            Collider[] hits =
                Physics.OverlapSphere(
                    origin.position,
                    fireEvent.Range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            var damagedRacerIds =
                new HashSet<string>();

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                RacerViewController victim =
                    hit.GetComponentInParent<
                        RacerViewController>();

                if (victim == null ||
                    !victim.IsInitialized)
                {
                    continue;
                }

                if (victim.RacerId ==
                    fireEvent.RacerId)
                {
                    continue;
                }

                if (!damagedRacerIds.Add(
                        victim.RacerId))
                {
                    continue;
                }

                runtime.Director.ApplyDamage(
                    fireEvent.RacerId,
                    victim.RacerId,
                    fireEvent.Damage,
                    DamageCause.Weapon);
            }
        }
    }
}