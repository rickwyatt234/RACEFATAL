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
        SteeringResponse, CorneringStability, LateBraking, DescentStability, SlipstreamAcceleration, EnergyCapacityPercent, BoosterEfficiency, Overcharged, EnergyRegeneration, Redline, UnshieldedDamage, BallisticDamage, OrdnanceDamage, BurnDamage, Executioner, MaxIntegrityPercent, ShieldDelay, ImpactDefense, LowIntegrityShieldRecharge, IronWill, LockOnSpeed, AiLeadAccuracy, CountermeasureCooldown, SiphonStrength, TacticalOverdrive, FinishCharacterFame, EliminationCharacterFame, Showboat, CombatCelebrity, PrestigeCharacterFame
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
        private readonly System.Collections.Generic.Dictionary<RacerPerkEffect,float> modifiers = new System.Collections.Generic.Dictionary<RacerPerkEffect,float>();
        public float Bonus(RacerPerkEffect effect) => modifiers.TryGetValue(effect, out var value) ? value : 0f;
        public bool Has(RacerPerkEffect effect) => Bonus(effect) > 0f;

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
                if (!bonuses.modifiers.ContainsKey(perk.Effect)) bonuses.modifiers[perk.Effect] = 0f;
                bonuses.modifiers[perk.Effect] = Math.Min(0.25f, bonuses.modifiers[perk.Effect] + perk.Strength);
                switch (perk.Effect)
                {
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
