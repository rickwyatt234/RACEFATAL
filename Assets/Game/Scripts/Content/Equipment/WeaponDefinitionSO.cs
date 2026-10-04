using RaceFatal.Equipment;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Equipment
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "RaceFatal/Equipment/Weapon")]
    public class WeaponDefinitionSO : EquipmentDefinitionSO
    {
        [Header("Activation")] [SerializeField] private EquipmentActivationMode activationMode;
        [Header("Weapon")] [SerializeField] private WeaponAimMode aimMode;
        [SerializeField] private WeaponDeliveryMode deliveryMode;
        [SerializeField] private float range = 100f;
        [SerializeField] private float projectileSpeed = 100f;
        [Min(0f)] [SerializeField] private float damage;
        [Header("Ammunition")] [Tooltip("Amount of ammunition this weapon receives at the beginning of each race.")] [Min(1)] [SerializeField] private int startingAmmo =
            100;
        [Header("Timing")] [Tooltip("Minimum seconds between shots/activations. Applies to press, hold, charged and passive weapons; releasing or switching weapons does not reset it.")] [Min(0f)] [SerializeField] private float fireInterval;
        [Min(0f)] [SerializeField] private float chargeDuration;
        [Header("Target Lock")] [Tooltip("Seconds a targeted guided weapon must maintain a valid target before achieving lock.")] [Min(0f)] [SerializeField] private
            float targetLockDuration;
        [Header("Multi Projectile / Cone")] [Tooltip("Projectiles emitted per activation. Used by cone weapons such as shotguns.")] [Min(1)] [SerializeField] private int projectileCount =
            1;
        [Tooltip("Full cone angle in degrees. Used by shotgun and flame-cone delivery.")] [Min(0f)] [SerializeField] private float spreadAngle;
        [Header("Mine")] [Tooltip("Explosion / proximity radius for dropped mine weapons.")] [Min(0f)] [SerializeField] private float explosionRadius;
        [Tooltip("Seconds after deployment before a mine can trigger.")] [Min(0f)] [SerializeField] private float armingDelay;
        [Tooltip("Maximum deployed lifetime in seconds. Zero means the presentation chooses a safe default.")] [Min(0f)] [SerializeField] private
            float lifetime;
        [Header("Status Effect")] [Tooltip("Continuous flame exposure required before the target catches fire.")] [Min(0f)] [SerializeField] private
            float exposureDuration;
        [Tooltip("Duration of the applied status effect.")] [Min(0f)] [SerializeField] private float effectDuration;
        [Tooltip("Shield / integrity damage per second while the status effect is active.")] [Min(0f)] [SerializeField] private float statusDamagePerSecond;
        [Header("Ram")] [Tooltip("Approximate lateral displacement requested by a ram activation.")] [Min(0f)] [SerializeField] private float lateralDistance;
        [Tooltip("Time over which the lateral ram impulse is intended to cover the configured distance.")] [Min(0f)] [SerializeField] private
            float dashDuration;
        [Tooltip("Velocity-change impulse applied to a racer struck by an active ram.")] [Min(0f)] [SerializeField] private float impactPush;
        [Header("Automatic Targeting")] [Tooltip("Half-angle of an automatic or special targeting cone. Zero uses the runtime default.")] [Range(0f,
            180f)] [SerializeField] private float targetingHalfAngle;
        public override EquipmentDefinition CreateEquipmentDefinition()
        {
            return new WeaponDefinition(id, displayName, requiredNodeSize, activationMode, aimMode, deliveryMode, range, projectileSpeed, damage, startingAmmo, fireInterval, chargeDuration, targetLockDuration, creditCost, requiredTechnologyId, projectileCount, spreadAngle, explosionRadius, armingDelay, lifetime, exposureDuration, effectDuration, statusDamagePerSecond, lateralDistance, dashDuration, impactPush, targetingHalfAngle);
        }
    }
}
