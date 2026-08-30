using System;
using System.Collections.Generic;
using RaceFatal.Combat;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Presentation.Combat
{
    public class RaceWeaponPresenter :
        MonoBehaviour
    {
        [Serializable]
        private sealed class ProjectileBinding
        {
            public string definitionId;

            public ProjectileView prefab;
        }

        [SerializeField]
        private RaceRuntimeController runtime;

        [SerializeField]
        private LayerMask hitMask = ~0;

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

        private void Awake()
        {
            foreach (ProjectileBinding binding
                     in projectilePrefabs)
            {
                if (binding.prefab == null ||
                    string.IsNullOrWhiteSpace(
                        binding.definitionId))
                {
                    continue;
                }

                projectileLookup[
                    binding.definitionId] =
                        binding.prefab;
            }
        }

        private void Start()
        {
            if (runtime == null ||
                runtime.Director == null)
            {
                return;
            }

            runtime.Director.WeaponFired +=
                OnWeaponFired;
        }

        private void OnWeaponFired(
            WeaponFireEvent fireEvent)
        {
            if (!runtime.TryGetRacerView(
                    fireEvent.RacerId,
                    out RacerViewController racer))
            {
                return;
            }

            if (!racer.TryGetEquipmentMount(
                    fireEvent.EquipmentId,
                    out BikeEquipmentMountView mount))
            {
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

                case WeaponDeliveryMode.GuidedProjectile:

                    // Target-lock behavior will be added in
                    // the targeting phase.
                    SpawnProjectile(
                        fireEvent,
                        origin);

                    break;
            }
        }

        private void FireHitscan(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
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
                hit.collider.GetComponentInParent<
                    RacerViewController>();

            if (victim == null)
                return;

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

        private void SpawnProjectile(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            if (!projectileLookup.TryGetValue(
                    fireEvent.DefinitionId,
                    out ProjectileView prefab))
            {
                Debug.LogWarning(
                    $"No projectile prefab registered " +
                    $"for weapon '{fireEvent.DefinitionId}'.",
                    this);

                return;
            }

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

        private void FireArea(
            WeaponFireEvent fireEvent,
            Transform origin)
        {
            Collider[] hits =
                Physics.OverlapSphere(
                    origin.position,
                    fireEvent.Range,
                    hitMask,
                    QueryTriggerInteraction.Ignore);

            var damaged =
                new HashSet<string>();

            foreach (Collider hit in hits)
            {
                RacerViewController victim =
                    hit.GetComponentInParent<
                        RacerViewController>();

                if (victim == null)
                    continue;

                if (victim.RacerId ==
                    fireEvent.RacerId)
                {
                    continue;
                }

                if (!damaged.Add(
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

        private void OnDestroy()
        {
            if (runtime != null &&
                runtime.Director != null)
            {
                runtime.Director.WeaponFired -=
                    OnWeaponFired;
            }
        }
    }
}