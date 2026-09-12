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
        [SerializeField] private EquipmentActivationMode activationMode;

        [Header("Weapon")]
        [SerializeField] private WeaponAimMode aimMode;
        [SerializeField] private WeaponDeliveryMode deliveryMode;

        [SerializeField] private float range = 100f;
        [SerializeField] private float projectileSpeed = 100f;

        [Min(0f)][SerializeField] private float damage;

        [Header("Ammunition")]
        [Tooltip("Amount of ammunition this weapon receives at the beginning of each race.")]
        [Min(1)][SerializeField] private int startingAmmo = 100;

        [Header("Timing")]
        [Min(0f)][SerializeField] private float fireInterval;
        [Min(0f)][SerializeField] private float chargeDuration;

        public override EquipmentDefinition CreateEquipmentDefinition()
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
                startingAmmo,
                fireInterval,
                chargeDuration,
                creditCost,
                requiredTechnologyId);
        }
    }
}