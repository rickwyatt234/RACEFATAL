using System;
using System.Collections.Generic;
using RaceFatal.Data;

namespace RaceFatal.Career
{
    /// <summary>Approved 30-perk roster. Existing ScriptableObject definitions take priority by ID.</summary>
    public static class RacerPerkCatalog
    {
        public static IReadOnlyList<RacerPerkDefinition> Definitions { get; } = new[]
        {
            new RacerPerkDefinition("DRV-01", "Apex Reader", "Steering response +5%.", 100, RacerPerkEffect.SteeringResponse, 0.05f),
            new RacerPerkDefinition("DRV-02", "Corner Specialist", "Cornering stability +6%.", 150, RacerPerkEffect.CorneringStability, 0.06f),
            new RacerPerkDefinition("DRV-03", "Late Braker", "Braking +8%; hard-braking stability -3%.", 200, RacerPerkEffect.LateBraking, 0.08f),
            new RacerPerkDefinition("DRV-04", "Controlled Descent", "Steep-section stability +8%.", 200, RacerPerkEffect.DescentStability, 0.08f),
            new RacerPerkDefinition("DRV-05", "Slipstream Artist", "Acceleration while slipstreaming +6%.", 250, RacerPerkEffect.SlipstreamAcceleration, 0.06f),
            new RacerPerkDefinition("PRP-01", "Reserve Cell", "Maximum booster energy +5%.", 100, RacerPerkEffect.EnergyCapacityPercent, 0.05f),
            new RacerPerkDefinition("PRP-02", "Efficient Burn", "Booster energy use -5%.", 150, RacerPerkEffect.BoosterEfficiency, 0.05f),
            new RacerPerkDefinition("PRP-03", "Overcharged", "Boost acceleration +7%; booster drain +6%.", 200, RacerPerkEffect.Overcharged, 0.07f),
            new RacerPerkDefinition("PRP-04", "Rapid Recovery", "Energy regeneration +7%.", 200, RacerPerkEffect.EnergyRegeneration, 0.07f),
            new RacerPerkDefinition("PRP-05", "Redline Specialist", "Engine acceleration +5%; stability -4%.", 300, RacerPerkEffect.Redline, 0.05f),
            new RacerPerkDefinition("CBT-01", "Apex Predator", "Integrity damage to unshielded targets +5%.", 150, RacerPerkEffect.UnshieldedDamage, 0.05f),
            new RacerPerkDefinition("CBT-02", "Ballistic Expert", "Ballistic machine-gun and shotgun integrity damage +5%.", 200, RacerPerkEffect.BallisticDamage, 0.05f),
            new RacerPerkDefinition("CBT-03", "Ordnance Specialist", "Rocket and mine damage +6%; ammunition or energy drawback.", 250, RacerPerkEffect.OrdnanceDamage, 0.06f),
            new RacerPerkDefinition("CBT-04", "Thermal Operator", "Burn damage +8%.", 250, RacerPerkEffect.BurnDamage, 0.08f),
            new RacerPerkDefinition("CBT-05", "Executioner", "Integrity damage +10% below 25% target integrity; otherwise -3%.", 350, RacerPerkEffect.Executioner, 0.1f),
            new RacerPerkDefinition("DEF-01", "Reinforced Instincts", "Maximum integrity +5%.", 100, RacerPerkEffect.MaxIntegrityPercent, 0.05f),
            new RacerPerkDefinition("DEF-02", "Shield Discipline", "Shield recharge delay -7%; not compatible with Ablative shields.", 150, RacerPerkEffect.ShieldDelay, 0.07f),
            new RacerPerkDefinition("DEF-03", "Impact Specialist", "Incoming collision integrity damage -8%.", 200, RacerPerkEffect.ImpactDefense, 0.08f),
            new RacerPerkDefinition("DEF-04", "Second Wind", "Shield recharge +12% below 35% integrity; incompatible with Ablative shields.", 250, RacerPerkEffect.LowIntegrityShieldRecharge, 0.12f),
            new RacerPerkDefinition("DEF-05", "Iron Will", "Integrity damage -8% below 25% integrity; acceleration -5% while active.", 350, RacerPerkEffect.IronWill, 0.08f),
            new RacerPerkDefinition("TAC-01", "Deadeye", "Guided lock-on time -8%.", 150, RacerPerkEffect.LockOnSpeed, 0.08f),
            new RacerPerkDefinition("TAC-02", "Target Analyst", "Eligible AI projectile lead accuracy +5%.", 200, RacerPerkEffect.AiLeadAccuracy, 0.05f),
            new RacerPerkDefinition("TAC-03", "Countermeasure Expert", "Automatic countermeasure cooldown -10%.", 250, RacerPerkEffect.CountermeasureCooldown, 0.1f),
            new RacerPerkDefinition("TAC-04", "Energy Leech Operator", "Energy siphon amount +8%.", 300, RacerPerkEffect.SiphonStrength, 0.08f),
            new RacerPerkDefinition("TAC-05", "Tactical Overdrive", "Manual utility cooldown -8%; activation energy +5%.", 350, RacerPerkEffect.TacticalOverdrive, 0.08f),
            new RacerPerkDefinition("FAM-01", "Crowd Favorite", "Character Fame from finishes +8%.", 100, RacerPerkEffect.FinishCharacterFame, 0.08f),
            new RacerPerkDefinition("FAM-02", "Headline Hunter", "Character Fame from eliminations +10% where supported.", 150, RacerPerkEffect.EliminationCharacterFame, 0.1f),
            new RacerPerkDefinition("FAM-03", "Showboat", "Audience overtake gains +10%; combat gains -5%.", 200, RacerPerkEffect.Showboat, 0.1f),
            new RacerPerkDefinition("FAM-04", "Combat Celebrity", "Audience combat gains +12%; overtake gains -5%.", 250, RacerPerkEffect.CombatCelebrity, 0.12f),
            new RacerPerkDefinition("FAM-05", "Living Legend", "Character Fame in prestigious events +10%.", 350, RacerPerkEffect.PrestigeCharacterFame, 0.1f)
        };
        public static void Ensure(GameDatabase database)
        {
            foreach (var perk in Definitions)
                if (database.GetRacerPerkDefinition(perk.Id) == null)
                    database.AddRacerPerkDefinition(perk);
        }
        public static bool IsStarter(RacerPerkDefinition perk) =>
            perk != null && (IsStarter(perk.Id) || (perk.Effect == RacerPerkEffect.EnergyCapacity && perk.FameCost <= 150 && perk.Strength > 0));

        public static bool IsStarter(string id)
        {
            var definition = Array.Find((RacerPerkDefinition[])Definitions, p => p.Id == id);
            return definition != null && definition.FameCost <= 150;
        }
    }
}
