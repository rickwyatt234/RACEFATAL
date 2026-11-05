using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public enum UtilityEffect
    {
        Jammer, Steering, Grip, ShieldDelay, Rupture, Leech, Stability, Mass, Integrity,
        ShieldRecharge, Acceleration, Recovery, BoosterEfficiency, Anchor, Hardlight,
        Siphon, Repulsion, DamageConverter
    }

    public sealed class RaceUtilityDefinition : EquipmentDefinition
    {
        public UtilityEffect Effect { get; }
        public float Strength { get; }

        public RaceUtilityDefinition(string id, string name, NodeSize node, int price,
            UtilityEffect effect, float strength, EquipmentActivationMode activation)
            : base(id, name, EquipmentCategory.Utility, node, activation, price)
        {
            Effect = effect;
            Strength = strength;
        }
    }
}
