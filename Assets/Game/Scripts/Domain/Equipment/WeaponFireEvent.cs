using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public readonly struct WeaponFireEvent
    {
        public string RacerId { get; }
        public string EquipmentId { get; }
        public string DefinitionId { get; }
        public WeaponAimMode AimMode { get; }
        public WeaponDeliveryMode DeliveryMode { get; }
        public float Damage { get; }
        public float Range { get; }
        public float ProjectileSpeed { get; }
        public float ChargeRatio { get; }
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
        public float FireInterval { get; }

        public WeaponFireEvent(string racerId, string equipmentId, string definitionId, WeaponAimMode aimMode, WeaponDeliveryMode deliveryMode, float damage, float range, float projectileSpeed, float chargeRatio, int projectileCount, float spreadAngle, float explosionRadius, float armingDelay, float lifetime, float exposureDuration, float effectDuration, float statusDamagePerSecond, float lateralDistance, float dashDuration, float impactPush, float targetingHalfAngle, float fireInterval)
        {
            RacerId = racerId;
            EquipmentId = equipmentId;
            DefinitionId = definitionId;
            AimMode = aimMode;
            DeliveryMode = deliveryMode;
            Damage = damage;
            Range = range;
            ProjectileSpeed = projectileSpeed;
            ChargeRatio = chargeRatio;
            ProjectileCount = projectileCount;
            SpreadAngle = spreadAngle;
            ExplosionRadius = explosionRadius;
            ArmingDelay = armingDelay;
            Lifetime = lifetime;
            ExposureDuration = exposureDuration;
            EffectDuration = effectDuration;
            StatusDamagePerSecond = statusDamagePerSecond;
            LateralDistance = lateralDistance;
            DashDuration = dashDuration;
            ImpactPush = impactPush;
            TargetingHalfAngle = targetingHalfAngle;
            FireInterval = fireInterval;
        }
    }
}
