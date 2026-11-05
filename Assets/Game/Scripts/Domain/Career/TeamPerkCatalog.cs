using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    // Team perks are permanent, unique team-owned rewards, never racer equipment.
    public enum TeamPerkEffect { EquipmentPrice, RaceCredits, MaxIntegrity, CollisionResistance, Acceleration, EnergyEfficiency, Handling, EnergyCapacity, BallisticDamage, ProjectileAccuracy, OrdnanceEfficiency, OrdnanceDamage, ShieldCapacity, ShieldRechargeDelay, Steering, CornerStability, LockSpeed, ExperimentalDamage, SiphonStrength, EwCooldown, BurnDamage, BurnDuration, ImpactResistance, EmergencyDefense }
    public sealed class TeamPerkDefinition
    {
        public string Id { get; }
        public string Manufacturer { get; }
        public string Name { get; }
        public string Description { get; }
        public string Tier { get; }
        public TeamPerkEffect Effect { get; }
        public float Strength { get; }
        public TeamPerkDefinition(string id, string manufacturer, string name, string description, string tier, TeamPerkEffect effect, float strength)
        { Id = id; Manufacturer = manufacturer; Name = name; Description = description; Tier = tier; Effect = effect; Strength = strength; }
    }
    public static class TeamPerkCatalog
    {
        public static readonly IReadOnlyList<TeamPerkDefinition> All = new TeamPerkDefinition[]
        {
            new TeamPerkDefinition("RF_TEAM_PILION_TRUSTED", "Pilion", "Production Standard", "Reduces the credit price of eligible Pilion equipment by 2%.", "Trusted", TeamPerkEffect.EquipmentPrice, 0.02f),
            new TeamPerkDefinition("RF_TEAM_PILION_PARTNER", "Pilion", "Warranty Network", "Increases eligible race credit payouts by 2%.", "Partner", TeamPerkEffect.RaceCredits, 0.03f),
            new TeamPerkDefinition("RF_TEAM_PACE_TRUSTED", "Pace Industries", "Heavy-Duty Fabrication", "Increases maximum integrity by 2% when using Pace chassis.", "Trusted", TeamPerkEffect.MaxIntegrity, 0.02f),
            new TeamPerkDefinition("RF_TEAM_PACE_PARTNER", "Pace Industries", "Load-Bearing Legacy", "Reduces collision integrity damage by 3% when using Pace chassis.", "Partner", TeamPerkEffect.CollisionResistance, 0.03f),
            new TeamPerkDefinition("RF_TEAM_CATALYX_TRUSTED", "Catalyx", "Catalytic Ignition", "Increases acceleration by 2% with Catalyx engines.", "Trusted", TeamPerkEffect.Acceleration, 0.02f),
            new TeamPerkDefinition("RF_TEAM_CATALYX_PARTNER", "Catalyx", "Prototype Catalyst", "Reduces energy consumption by 3% with Catalyx propulsion.", "Partner", TeamPerkEffect.EnergyEfficiency, 0.03f),
            new TeamPerkDefinition("RF_TEAM_KAPPA_TRUSTED", "Kappa Engineering", "Race Calibration", "Increases engine acceleration by 2% with Kappa engines.", "Trusted", TeamPerkEffect.Acceleration, 0.02f),
            new TeamPerkDefinition("RF_TEAM_KAPPA_PARTNER", "Kappa Engineering", "Pole Position", "Improves handling by 2% with Kappa propulsion.", "Partner", TeamPerkEffect.Handling, 0.03f),
            new TeamPerkDefinition("RF_TEAM_MONOCHROME_TRUSTED", "Monochrome", "Energy Discipline", "Increases booster energy capacity by 2% with Monochrome equipment.", "Trusted", TeamPerkEffect.EnergyCapacity, 0.02f),
            new TeamPerkDefinition("RF_TEAM_MONOCHROME_PARTNER", "Monochrome", "Closed Circuit", "Reduces booster energy consumption by 3% with Monochrome boosters.", "Partner", TeamPerkEffect.EnergyEfficiency, 0.03f),
            new TeamPerkDefinition("RF_TEAM_ONE_KINETICS_TRUSTED", "One Kinetics", "Ballistic Calibration", "Increases ballistic weapon integrity damage by 2%.", "Trusted", TeamPerkEffect.BallisticDamage, 0.02f),
            new TeamPerkDefinition("RF_TEAM_ONE_KINETICS_PARTNER", "One Kinetics", "Precision Manufacture", "Increases ballistic projectile accuracy by 3%.", "Partner", TeamPerkEffect.ProjectileAccuracy, 0.03f),
            new TeamPerkDefinition("RF_TEAM_LEISTUNG_TRUSTED", "Leistung", "Ordnance Handling", "Reduces rocket and mine energy consumption by 2%.", "Trusted", TeamPerkEffect.OrdnanceEfficiency, 0.02f),
            new TeamPerkDefinition("RF_TEAM_LEISTUNG_PARTNER", "Leistung", "High-Yield Division", "Increases rocket and mine integrity damage by 3%.", "Partner", TeamPerkEffect.OrdnanceDamage, 0.03f),
            new TeamPerkDefinition("RF_TEAM_SCUDO_TRUSTED", "Scudo Migliore", "Protective Standard", "Increases shield capacity by 2%.", "Trusted", TeamPerkEffect.ShieldCapacity, 0.02f),
            new TeamPerkDefinition("RF_TEAM_SCUDO_PARTNER", "Scudo Migliore", "Last Line", "Reduces shield recharge delay by 3% for rechargeable shields.", "Partner", TeamPerkEffect.ShieldRechargeDelay, 0.03f),
            new TeamPerkDefinition("RF_TEAM_HIVEMIND_TRUSTED", "Hivemind Corp", "Motion Mapping", "Improves steering response by 2%.", "Trusted", TeamPerkEffect.Steering, 0.02f),
            new TeamPerkDefinition("RF_TEAM_HIVEMIND_PARTNER", "Hivemind Corp", "Distributed Reflexes", "Improves cornering stability by 3%.", "Partner", TeamPerkEffect.CornerStability, 0.03f),
            new TeamPerkDefinition("RF_TEAM_OSAKI_TRUSTED", "Osaki Labs", "Experimental Targeting", "Reduces guided weapon lock time by 2%.", "Trusted", TeamPerkEffect.LockSpeed, 0.02f),
            new TeamPerkDefinition("RF_TEAM_OSAKI_PARTNER", "Osaki Labs", "Prototype Feedback", "Increases eligible experimental weapon integrity damage by 3%.", "Partner", TeamPerkEffect.ExperimentalDamage, 0.03f),
            new TeamPerkDefinition("RF_TEAM_LANCE_REBA_TRUSTED", "Lance Reba Group", "Signal Intrusion", "Improves energy siphon strength by 2%.", "Trusted", TeamPerkEffect.SiphonStrength, 0.02f),
            new TeamPerkDefinition("RF_TEAM_LANCE_REBA_PARTNER", "Lance Reba Group", "Blackout Protocol", "Reduces electronic warfare utility cooldowns by 3%.", "Partner", TeamPerkEffect.EwCooldown, 0.03f),
            new TeamPerkDefinition("RF_TEAM_TRIDENT_TRUSTED", "Trident", "Thermal Chemistry", "Increases flamethrower burn damage by 2%.", "Trusted", TeamPerkEffect.BurnDamage, 0.02f),
            new TeamPerkDefinition("RF_TEAM_TRIDENT_PARTNER", "Trident", "Residual Heat", "Increases incendiary damage-over-time duration by 3%.", "Partner", TeamPerkEffect.BurnDuration, 0.03f),
            new TeamPerkDefinition("RF_TEAM_SNAPDRAGON_TRUSTED", "Snapdragon Incorporated", "Structural Geometry", "Increases chassis impact resistance by 2%.", "Trusted", TeamPerkEffect.ImpactResistance, 0.02f),
            new TeamPerkDefinition("RF_TEAM_SNAPDRAGON_PARTNER", "Snapdragon Incorporated", "Adaptive Frame", "Improves handling by 3% with Snapdragon chassis.", "Partner", TeamPerkEffect.Handling, 0.03f),
            new TeamPerkDefinition("RF_TEAM_ALION_TRUSTED", "Alion Limited", "Hardlight Envelope", "Increases shield capacity by 2%.", "Trusted", TeamPerkEffect.ShieldCapacity, 0.02f),
            new TeamPerkDefinition("RF_TEAM_ALION_PARTNER", "Alion Limited", "Contingency Architecture", "Improves emergency defensive utility strength by 3%.", "Partner", TeamPerkEffect.EmergencyDefense, 0.03f),
        };
        public static TeamPerkDefinition Find(string id) => All.FirstOrDefault(x => x.Id == id);
    }
}
