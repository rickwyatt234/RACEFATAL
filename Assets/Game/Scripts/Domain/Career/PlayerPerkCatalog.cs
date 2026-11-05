using System;
using System.Collections.Generic;
using RaceFatal.Data;

namespace RaceFatal.Career
{
    // Initial 30-perk authored catalog. Values are provisional; conditional effects require their respective race-system hooks.
    public static class PlayerPerkCatalog
    {
        public static IEnumerable<RacerPerkDefinition> CreateDefinitions()
        {
            yield return new RacerPerkDefinition("DRV_01", "Apex Reader", "Improves steering response by 5%.", 100, RacerPerkEffect.SteeringResponse, 0.05f);
            yield return new RacerPerkDefinition("DRV_02", "Corner Specialist", "Improves stability during cornering by 6%.", 150, RacerPerkEffect.CornerStability, 0.06f);
            yield return new RacerPerkDefinition("DRV_03", "Late Braker", "Improves braking by 8% but reduces hard-braking stability by 3%.", 200, RacerPerkEffect.Braking, 0.08f);
            yield return new RacerPerkDefinition("DRV_04", "Controlled Descent", "Improves steep-slope stability by 8%.", 200, RacerPerkEffect.VerticalStability, 0.08f);
            yield return new RacerPerkDefinition("DRV_05", "Slipstream Artist", "Improves slipstream acceleration by 6%.", 250, RacerPerkEffect.SlipstreamAcceleration, 0.06f);
            yield return new RacerPerkDefinition("PRP_06", "Reserve Cell", "Increases maximum energy capacity by 5%.", 100, RacerPerkEffect.EnergyCapacityPercent, 0.05f);
            yield return new RacerPerkDefinition("PRP_07", "Efficient Burn", "Reduces booster energy use by 5%.", 150, RacerPerkEffect.BoosterEfficiency, 0.05f);
            yield return new RacerPerkDefinition("PRP_08", "Overcharged", "Improves boost acceleration by 7% at 6% higher energy cost.", 200, RacerPerkEffect.BoostAcceleration, 0.07f);
            yield return new RacerPerkDefinition("PRP_09", "Rapid Recovery", "Increases energy regeneration by 7%.", 200, RacerPerkEffect.EnergyRegeneration, 0.07f);
            yield return new RacerPerkDefinition("PRP_10", "Redline Specialist", "Improves engine acceleration by 5% at 4% lower stability.", 300, RacerPerkEffect.EngineAcceleration, 0.05f);
            yield return new RacerPerkDefinition("CBT_11", "Apex Predator", "Increases integrity damage to unshielded targets by 5%.", 150, RacerPerkEffect.UnshieldedDamage, 0.05f);
            yield return new RacerPerkDefinition("CBT_12", "Ballistic Expert", "Increases ballistic machine-gun and shotgun integrity damage by 5%.", 200, RacerPerkEffect.BallisticDamage, 0.05f);
            yield return new RacerPerkDefinition("CBT_13", "Ordnance Specialist", "Increases rocket and mine damage by 6% at an ammunition or energy cost.", 250, RacerPerkEffect.OrdnanceDamage, 0.06f);
            yield return new RacerPerkDefinition("CBT_14", "Thermal Operator", "Increases burn damage by 8%.", 250, RacerPerkEffect.BurnDamage, 0.08f);
            yield return new RacerPerkDefinition("CBT_15", "Executioner", "Increases integrity damage to critical targets by 10%, reducing damage to healthier targets by 3%.", 350, RacerPerkEffect.ExecutionDamage, 0.10f);
            yield return new RacerPerkDefinition("DEF_16", "Reinforced Instincts", "Increases maximum integrity by 5%.", 100, RacerPerkEffect.MaxIntegrityPercent, 0.05f);
            yield return new RacerPerkDefinition("DEF_17", "Shield Discipline", "Reduces rechargeable shield recharge delay by 7%.", 150, RacerPerkEffect.ShieldRechargeDelay, 0.07f);
            yield return new RacerPerkDefinition("DEF_18", "Impact Specialist", "Reduces collision integrity damage by 8%.", 200, RacerPerkEffect.CollisionResistance, 0.08f);
            yield return new RacerPerkDefinition("DEF_19", "Second Wind", "Increases shield recharge by 12% when integrity is below 35%.", 250, RacerPerkEffect.CriticalShieldRecharge, 0.12f);
            yield return new RacerPerkDefinition("DEF_20", "Iron Will", "Reduces integrity damage by 8% below 25% integrity, at 5% lower acceleration.", 350, RacerPerkEffect.CriticalIntegrityResistance, 0.08f);
            yield return new RacerPerkDefinition("TAC_21", "Deadeye", "Reduces guided weapon lock time by 8%.", 150, RacerPerkEffect.GuidedLockSpeed, 0.08f);
            yield return new RacerPerkDefinition("TAC_22", "Target Analyst", "Improves eligible AI targeting lead accuracy by 5%.", 200, RacerPerkEffect.AiLeadAccuracy, 0.05f);
            yield return new RacerPerkDefinition("TAC_23", "Countermeasure Expert", "Reduces automatic countermeasure cooldown by 10%.", 250, RacerPerkEffect.CountermeasureCooldown, 0.10f);
            yield return new RacerPerkDefinition("TAC_24", "Energy Leech Operator", "Increases energy siphoning by 8%.", 300, RacerPerkEffect.SiphonStrength, 0.08f);
            yield return new RacerPerkDefinition("TAC_25", "Tactical Overdrive", "Reduces manual utility cooldowns by 8% at 5% higher energy use.", 350, RacerPerkEffect.UtilityCooldown, 0.08f);
            yield return new RacerPerkDefinition("FAM_26", "Crowd Favorite", "Increases Character Fame from race finishes by 8%.", 100, RacerPerkEffect.FinishCharacterFame, 0.08f);
            yield return new RacerPerkDefinition("FAM_27", "Headline Hunter", "Increases eligible elimination Character Fame by 10%.", 150, RacerPerkEffect.EliminationCharacterFame, 0.10f);
            yield return new RacerPerkDefinition("FAM_28", "Showboat", "Increases overtake audience gains by 10% and reduces combat audience gains by 5%.", 200, RacerPerkEffect.OvertakeAudience, 0.10f);
            yield return new RacerPerkDefinition("FAM_29", "Combat Celebrity", "Increases combat audience gains by 12% and reduces overtake audience gains by 5%.", 250, RacerPerkEffect.CombatAudience, 0.12f);
            yield return new RacerPerkDefinition("FAM_30", "Living Legend", "Increases Character Fame from prestigious events by 10%.", 350, RacerPerkEffect.PrestigeCharacterFame, 0.10f);
        }
    }
}
