using System;
using RaceFatal.Data;

namespace RaceFatal.Career
{
    public enum RacerPerkEffect
    {
        EnergyCapacity,
        Pace,
        Overtaking,
        Defense,
        WeaponAggression,
        SteeringResponse, CornerStability, Braking, VerticalStability, SlipstreamAcceleration, EnergyCapacityPercent, BoosterEfficiency, BoostAcceleration, EnergyRegeneration, EngineAcceleration, UnshieldedDamage, BallisticDamage, OrdnanceDamage, BurnDamage, ExecutionDamage, MaxIntegrityPercent, ShieldRechargeDelay, CollisionResistance, CriticalShieldRecharge, CriticalIntegrityResistance, GuidedLockSpeed, AiLeadAccuracy, CountermeasureCooldown, SiphonStrength, UtilityCooldown, FinishCharacterFame, EliminationCharacterFame, OvertakeAudience, CombatAudience, PrestigeCharacterFame
    }

    public sealed class RacerPerkDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int FameCost { get; }
        public RacerPerkEffect Effect { get; }
        public float Strength { get; }

        public RacerPerkDefinition(string id, string name, string description, int cost, RacerPerkEffect effect, float strength)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Perk ID is required.", nameof(id));
            Id = id;
            DisplayName = name;
            Description = description ?? string.Empty;
            FameCost = cost;
            Effect = effect;
            Strength = strength;
        }
    }

    public sealed class RacerPerkBonuses
    {
        public float EnergyCapacity { get; private set; }
        public float Pace { get; private set; }
        public float Overtaking { get; private set; }
        public float Defense { get; private set; }
        public float WeaponAggression { get; private set; }
        public float EnergyCapacityPercent { get; private set; }
        public float MaxIntegrityPercent { get; private set; }
        public float CollisionResistance { get; private set; }
        public float FinishCharacterFame { get; private set; }

        public static RacerPerkBonuses For(RacerState racer, GameDatabase database)
        {
            var bonuses = new RacerPerkBonuses();
            if (racer == null || database == null)
                return bonuses;
            foreach (string id in racer.Progression.PurchasedPerkIds)
            {
                var perk = database.GetRacerPerkDefinition(id);
                if (perk == null || float.IsNaN(perk.Strength) || float.IsInfinity(perk.Strength) || perk.Strength < 0)
                    continue;
                switch (perk.Effect)
                {
                    case RacerPerkEffect.EnergyCapacityPercent:
                        bonuses.EnergyCapacityPercent += perk.Strength;
                        break;
                    case RacerPerkEffect.MaxIntegrityPercent:
                        bonuses.MaxIntegrityPercent += perk.Strength;
                        break;
                    case RacerPerkEffect.CollisionResistance:
                        bonuses.CollisionResistance += perk.Strength;
                        break;
                    case RacerPerkEffect.FinishCharacterFame:
                        bonuses.FinishCharacterFame += perk.Strength;
                        break;
                    case RacerPerkEffect.EnergyCapacity:
                        bonuses.EnergyCapacity += perk.Strength;
                        break;
                    case RacerPerkEffect.Pace:
                        bonuses.Pace += perk.Strength;
                        break;
                    case RacerPerkEffect.Overtaking:
                        bonuses.Overtaking += perk.Strength;
                        break;
                    case RacerPerkEffect.Defense:
                        bonuses.Defense += perk.Strength;
                        break;
                    case RacerPerkEffect.WeaponAggression:
                        bonuses.WeaponAggression += perk.Strength;
                        break;
                }
            }

            return bonuses;
        }
    }
}
