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

        // Seconds between shots for Hold weapons.
        // Ignored by Press and ChargeRelease weapons.
        public float FireInterval { get; }

        // Seconds required to fully charge a ChargeRelease weapon.
        public float ChargeDuration { get; }

        public WeaponDefinition(
            string id,
            string displayName,
            NodeSize requiredNodeSize,
            EquipmentActivationMode activationMode,
            WeaponAimMode aimMode,
            WeaponDeliveryMode deliveryMode,
            float range,
            float projectileSpeed,
            float damage,
            int startingAmmo,
            float fireInterval,
            float chargeDuration,
            int creditCost,
            string requiredTechnologyId)
            : base(
                id,
                displayName,
                EquipmentCategory.Weapon,
                requiredNodeSize,
                activationMode,
                creditCost,
                requiredTechnologyId)
        {
            if (activationMode != EquipmentActivationMode.Press &&
                activationMode != EquipmentActivationMode.Hold &&
                activationMode != EquipmentActivationMode.ChargeRelease)
            {
                throw new ArgumentException(
                    "Weapons must use Press, Hold, or ChargeRelease.");
            }

            if (startingAmmo <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingAmmo),
                    "Weapons must begin a race with at least one round of ammunition.");
            }

            AimMode = aimMode;
            DeliveryMode = deliveryMode;

            Range = range;
            ProjectileSpeed = projectileSpeed;
            Damage = damage;

            StartingAmmo = startingAmmo;

            FireInterval = fireInterval;
            ChargeDuration = chargeDuration;
        }
    }
}