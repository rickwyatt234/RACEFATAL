using RaceFatal.Equipment;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(
        fileName = "WeaponDefinition",
        menuName = "RaceFatal/Equipment/Weapon")]
    public class WeaponDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Activation")]
        [SerializeField]
        private EquipmentActivationMode activationMode;

        [Header("Weapon")]
        [SerializeField]
        private WeaponAimMode aimMode;

        [SerializeField]
        private WeaponDeliveryMode deliveryMode;

        [SerializeField]
        private float range = 100f;

        [SerializeField]
        private float projectileSpeed = 100f;

        [Min(0f)]
        [SerializeField]
        private float damage;

        [Min(0f)]
        [SerializeField]
        private float energyCostPerShot;

        [Min(0f)]
        [SerializeField]
        private float fireInterval;

        [Min(0f)]
        [SerializeField]
        private float chargeDuration;

        public override EquipmentDefinition
            CreateEquipmentDefinition()
        {
            return new WeaponDefinition(
                id,
                displayName,
                requiredNodeSize,
                activationMode,
                aimMode,
                deliveryMode,
                range,
                projectileSpeed,
                damage,
                energyCostPerShot,
                fireInterval,
                chargeDuration,
                creditCost,
                requiredTechnologyId);
        }
    }
}