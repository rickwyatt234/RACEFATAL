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
                damage,
                energyCostPerShot,
                fireInterval,
                chargeDuration,
                creditCost,
                requiredTechnologyId);
        }
    }
}