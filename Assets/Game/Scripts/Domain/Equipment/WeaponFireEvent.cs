using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public readonly struct WeaponFireEvent
    {
        public string RacerId { get; }
        public string EquipmentId { get; }
        public string DefinitionId { get; }
        public WeaponAimMode AimMode { get; }
        public float Damage { get; }
        public float ChargeRatio { get; }

        public WeaponFireEvent(
            string racerId,
            string equipmentId,
            string definitionId,
            WeaponAimMode aimMode,
            float damage,
            float chargeRatio)
        {
            RacerId = racerId;
            EquipmentId = equipmentId;
            DefinitionId = definitionId;
            AimMode = aimMode;
            Damage = damage;
            ChargeRatio = chargeRatio;
        }
    }
}