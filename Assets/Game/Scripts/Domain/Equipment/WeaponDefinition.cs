using System;
using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class WeaponDefinition : EquipmentDefinition
    {
        public WeaponAimMode AimMode { get; }
        public WeaponDeliveryMode DeliveryMode { get; }
        public float Range { get; }
        public float ProjectileSpeed { get; }
        public float Damage { get; }
        public int StartingAmmo { get; }
        public float FireInterval { get; }
        public float ChargeDuration { get; }
        public float TargetLockDuration { get; }
        public int ProjectileCount { get; }
        public float SpreadAngle { get; }
        public float ExplosionRadius { get; }
        public float ArmingDelay { get; }
        public float Lifetime { get; }
        public float ExposureDuration { get; }
        public float EffectDuration { get; }
        public float StatusDamagePerSecond { get; }
        public float LateralDistance { get; }
        public float DashDuration { get; }
        public float ImpactPush { get; }
        public float TargetingHalfAngle { get; }

        public WeaponDefinition(string id, string displayName, NodeSize requiredNodeSize, EquipmentActivationMode activationMode, WeaponAimMode aimMode, WeaponDeliveryMode deliveryMode, float range, float projectileSpeed, float damage, int startingAmmo, float fireInterval, float chargeDuration, float targetLockDuration, int creditCost, string requiredTechnologyId, int projectileCount = 1, float spreadAngle = 0f, float explosionRadius = 0f, float armingDelay = 0f, float lifetime = 0f, float exposureDuration = 0f, float effectDuration = 0f, float statusDamagePerSecond = 0f, float lateralDistance = 0f, float dashDuration = 0f, float impactPush = 0f, float targetingHalfAngle = 0f) : base(id, displayName, EquipmentCategory.Weapon, requiredNodeSize, activationMode, creditCost, requiredTechnologyId)
        {
            if (activationMode != EquipmentActivationMode.Passive && activationMode != EquipmentActivationMode.Press && activationMode != EquipmentActivationMode.Hold && activationMode != EquipmentActivationMode.ChargeRelease)
            {
                throw new ArgumentException("Weapons must use Passive, Press, Hold, or ChargeRelease.");
            }

            if (startingAmmo <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingAmmo), "Weapons must begin a race with at least one round of ammunition.");
            }

            AimMode = aimMode;
            DeliveryMode = deliveryMode;
            Range = Math.Max(0f, range);
            ProjectileSpeed = Math.Max(0f, projectileSpeed);
            Damage = Math.Max(0f, damage);
            StartingAmmo = startingAmmo;
            FireInterval = Math.Max(0f, fireInterval);
            ChargeDuration = Math.Max(0f, chargeDuration);
            TargetLockDuration = Math.Max(0f, targetLockDuration);
            ProjectileCount = Math.Max(1, projectileCount);
            SpreadAngle = Math.Max(0f, spreadAngle);
            ExplosionRadius = Math.Max(0f, explosionRadius);
            ArmingDelay = Math.Max(0f, armingDelay);
            Lifetime = Math.Max(0f, lifetime);
            ExposureDuration = Math.Max(0f, exposureDuration);
            EffectDuration = Math.Max(0f, effectDuration);
            StatusDamagePerSecond = Math.Max(0f, statusDamagePerSecond);
            LateralDistance = Math.Max(0f, lateralDistance);
            DashDuration = Math.Max(0f, dashDuration);
            ImpactPush = Math.Max(0f, impactPush);
            TargetingHalfAngle = Math.Max(0f, targetingHalfAngle);
        }
    }
}
