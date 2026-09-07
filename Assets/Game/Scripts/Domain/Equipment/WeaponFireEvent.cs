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

        public WeaponFireEvent(
            string racerId,
            string equipmentId,
            string definitionId,
            WeaponAimMode aimMode,
            WeaponDeliveryMode deliveryMode,
            float damage,
            float range,
            float projectileSpeed,
            float chargeRatio)
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
        }
    }
}