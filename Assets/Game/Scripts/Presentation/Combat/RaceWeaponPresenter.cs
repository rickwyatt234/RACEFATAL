using System;
using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public sealed class RaceWeaponPresenter : MonoBehaviour
    {
        [Serializable]
        private sealed class ProjectileBinding
        {
            [Tooltip(
                "Must match the weapon definition ID exactly.")]
            public string definitionId;

            public ProjectileView prefab;
        }

        [Header("Hitscan / Area")]

        [SerializeField]
        private LayerMask hitMask = ~0;

        [Header("Projectile Prefabs")]

        [SerializeField]
        private List<ProjectileBinding>
            projectilePrefabs =
                new List<ProjectileBinding>();

        private readonly Dictionary<
            string,
            ProjectileView>
            projectileLookup =
                new Dictionary<
                    string,
                    ProjectileView>();

        private RaceRuntimeController runtime;

        // ---------------------------------------------------------
        // UNITY
        // ---------------------------------------------------------

        private void Awake()
        {
            BuildProjectileLookup();
        }

        private void OnDestroy()
        {
            UnsubscribeFromRuntime();
        }

        // ---------------------------------------------------------
        // INITIALIZATION
        // ---------------------------------------------------------

        /// <summary>
        /// Connects this presentation component to the active
        /// race runtime.
        ///
        /// RaceSceneAssembler calls this after the RaceDirector
        /// and physical race runtime have been created.
        /// </summary>
        public void Initialize(
            RaceRuntimeController raceRuntime)
        {
            UnsubscribeFromRuntime();

            runtime = raceRuntime;

            if (runtime == null)
            {
                Debug.LogError(
                    "RaceWeaponPresenter requires a " +
                    "RaceRuntimeController.",
                    this);

                return;
            }

            if (runtime.Director == null)
            {
                Debug.LogError(
                    "RaceRuntimeController does not have an " +
                    "initialized RaceDirector.",
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

        // ---------------------------------------------------------
        // PROJECTILE LOOKUP
        // ---------------------------------------------------------

        private void BuildProjectileLookup()
        {
            projectileLookup.Clear();

            foreach (ProjectileBinding binding
                     in projectilePrefabs)
            {
                if (binding == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        binding.definitionId))
                {
                    continue;
                }

                if (binding.prefab == null)
                    continue;

                if (projectileLookup.ContainsKey(
                        binding.definitionId))
                {
                    Debug.LogWarning(
                        $"Duplicate projectile binding for " +
                        $"'{binding.definitionId}'. " +
                        "The later entry will replace the earlier one.",
                        this);
                }

                projectileLookup[
                    binding.definitionId] =
                        binding.prefab;
            }
        }

        // ---------------------------------------------------------
        // WEAPON FIRING
        // ---------------------------------------------------------

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
                    $"No RacerViewController was found for " +
                    $"racer '{fireEvent.RacerId}'.",
                    this);

                return;
            }

            if (!racer.TryGetEquipmentMount(
                    fireEvent.EquipmentId,
                    out BikeEquipmentMountView mount))
            {
                Debug.LogWarning(
                    $"Racer '{fireEvent.RacerId}' has no " +
                    $"physical mount bound to equipment " +
                    $"'{fireEvent.EquipmentId}'.",
                    racer);

                return;
            }

            Transform origin =
                mount.EquipmentOrigin;

            switch (fireEvent.DeliveryMode)
            {
                case WeaponDeliveryMode.Hitscan:
                    FireHitscan(
                        fireEvent,
                        origin);
                    break;

                case WeaponDeliveryMode.Projectile:
                    SpawnProjectile(
                        fireEvent,
                        origin);
                    break;

                case WeaponDeliveryMode.GuidedProjectile:
                    // Proper target acquisition and homing will
                    // be added with the targeting system.
                    //
                    // For now, this behaves like a normal
                    // forward projectile.
                    SpawnProjectile(
                        fireEvent,
                        origin);
                    break;

                case WeaponDeliveryMode.Dropped:
                    SpawnProjectile(
                        fireEvent,
                        origin);
                    break;

                case WeaponDeliveryMode.Area:
                    FireArea(
                        fireEvent,
                        origin);
                    break;

                default:
                    Debug.LogWarning(
                        $"Unsupported weapon delivery mode " +
                        $"'{fireEvent.DeliveryMode}'.",
                        this);
                    break;
            }
        }

        // ---------------------------------------------------------
        // HITSCAN
        // ---------------------------------------------------------

        private void FireHitscan(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            if (origin == null)
                return;

            if (!Physics.Raycast(
                    origin.position,
                    origin.forward,
                    out RaycastHit hit,
                    fireEvent.Range,
                    hitMask,
                    QueryTriggerInteraction.Ignore))
            {
                return;
            }

            RacerViewController victim =
                hit.collider
                    .GetComponentInParent<
                        RacerViewController>();

            if (victim == null ||
                !victim.IsInitialized)
            {
                return;
            }

            // Never damage the weapon's owner.
            if (victim.RacerId ==
                fireEvent.RacerId)
            {
                return;
            }

            runtime.Director.ApplyDamage(
                fireEvent.RacerId,
                victim.RacerId,
                fireEvent.Damage,
                DamageCause.Weapon);
        }

        // ---------------------------------------------------------
        // PROJECTILES
        // ---------------------------------------------------------

        private void SpawnProjectile(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            if (origin == null)
                return;

            if (!projectileLookup.TryGetValue(
                    fireEvent.DefinitionId,
                    out ProjectileView prefab))
            {
                Debug.LogWarning(
                    $"No ProjectileView prefab is registered " +
                    $"for weapon definition " +
                    $"'{fireEvent.DefinitionId}'.",
                    this);

                return;
            }

            if (prefab == null)
                return;

            ProjectileView projectile =
                Instantiate(
                    prefab,
                    origin.position,
                    origin.rotation);

            projectile.Initialize(
                runtime,
                fireEvent.RacerId,
                fireEvent.Damage,
                fireEvent.ProjectileSpeed,
                fireEvent.Range);
        }

        // ---------------------------------------------------------
        // AREA ATTACKS
        // ---------------------------------------------------------

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

            // A racer may have several colliders.
            // Ensure an area attack only damages each racer once.
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