using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    public sealed class TeamPerkDefinition
    {
        public string Id { get; }
        public string Manufacturer { get; }
        public string Name { get; }
        public string Description { get; }
        public string Effect { get; }
        public float Strength { get; }
        public int RequiredStanding { get; }
        public TeamPerkDefinition(string id, string manufacturer, string name, string description, string effect, float strength, int requiredStanding)
        {
            Id=id; Manufacturer=manufacturer; Name=name; Description=description; Effect=effect; Strength=strength; RequiredStanding=requiredStanding;
        }
    }
    /// <summary>28 nonrepeatable permanent team perks; none are equipment or temporary modifiers.</summary>
    public static class TeamPerkCatalog
    {
        public static readonly IReadOnlyList<TeamPerkDefinition> All = new []
        {
            new TeamPerkDefinition("TEAM-01", "Pilion", "Procurement Network", "Basic Pilion equipment costs 2% less", "PurchaseDiscount", 0.02f, 6),
            new TeamPerkDefinition("TEAM-02", "Pilion", "Fleet Standardization", "Pilion equipment upkeep-free reliability: +2% energy capacity", "EnergyCapacityPercent", 0.02f, 15),
            new TeamPerkDefinition("TEAM-03", "Pace Industries", "Heavy Metal", "Pace chassis +2% maximum integrity", "MaxIntegrityPercent", 0.02f, 6),
            new TeamPerkDefinition("TEAM-04", "Pace Industries", "Impact Warranty", "Pace chassis +2% collision protection", "ImpactDefense", 0.02f, 15),
            new TeamPerkDefinition("TEAM-05", "Catalyx", "Reactive Core", "Catalyx propulsion +2% acceleration", "Acceleration", 0.02f, 6),
            new TeamPerkDefinition("TEAM-06", "Catalyx", "Experimental Threshold", "Catalyx boosters +2% energy capacity", "EnergyCapacityPercent", 0.02f, 15),
            new TeamPerkDefinition("TEAM-07", "Kappa Engineering", "Precision Output", "Kappa engines +2% acceleration", "Acceleration", 0.02f, 6),
            new TeamPerkDefinition("TEAM-08", "Kappa Engineering", "Perfect Transfer", "Kappa boosters +2% acceleration", "BoostAcceleration", 0.02f, 15),
            new TeamPerkDefinition("TEAM-09", "Monochrome", "Power Economy", "Monochrome boosters consume 2% less energy", "BoosterEfficiency", 0.02f, 6),
            new TeamPerkDefinition("TEAM-10", "Monochrome", "Closed Circuit", "Monochrome energy recovery +2%", "EnergyRegeneration", 0.02f, 15),
            new TeamPerkDefinition("TEAM-11", "One Kinetics", "Ballistic Discipline", "One Kinetics ballistic weapon damage +2%", "BallisticDamage", 0.02f, 6),
            new TeamPerkDefinition("TEAM-12", "One Kinetics", "Terminal Velocity", "One Kinetics railgun damage +2%", "RailgunDamage", 0.02f, 15),
            new TeamPerkDefinition("TEAM-13", "Leistung", "Controlled Detonation", "Leistung rockets deal +2% damage", "RocketDamage", 0.02f, 6),
            new TeamPerkDefinition("TEAM-14", "Leistung", "Chain Reaction", "Leistung mines deal +2% damage", "MineDamage", 0.02f, 15),
            new TeamPerkDefinition("TEAM-15", "Scudo Migliore", "Safety Margin", "Scudo shields gain +2% capacity", "ShieldCapacity", 0.02f, 6),
            new TeamPerkDefinition("TEAM-16", "Scudo Migliore", "Shield Covenant", "Scudo rechargeable shields recover +2% faster", "ShieldRecharge", 0.02f, 15),
            new TeamPerkDefinition("TEAM-17", "Hivemind Corp", "Fine Control", "Hivemind chassis +2% steering response", "SteeringResponse", 0.02f, 6),
            new TeamPerkDefinition("TEAM-18", "Hivemind Corp", "Motion Consensus", "Hivemind chassis +2% stability", "Stability", 0.02f, 15),
            new TeamPerkDefinition("TEAM-19", "Osaki Labs", "Rare Precision", "Osaki precision weapons +2% integrity damage", "PrecisionDamage", 0.02f, 6),
            new TeamPerkDefinition("TEAM-20", "Osaki Labs", "Prototype Advantage", "Osaki experimental weapons +2% damage", "ExperimentalDamage", 0.02f, 15),
            new TeamPerkDefinition("TEAM-21", "Lance Reba Group", "Signal Breach", "Lance Reba utilities drain +2% energy", "SiphonStrength", 0.02f, 6),
            new TeamPerkDefinition("TEAM-22", "Lance Reba Group", "Silent Protocol", "Lance Reba disruption duration +2%", "DisruptionDuration", 0.02f, 15),
            new TeamPerkDefinition("TEAM-23", "Trident", "Hot Zone", "Trident burn damage +2%", "BurnDamage", 0.02f, 6),
            new TeamPerkDefinition("TEAM-24", "Trident", "Chemical Persistence", "Trident corrosion and incendiary duration +2%", "DotDuration", 0.02f, 15),
            new TeamPerkDefinition("TEAM-25", "Snapdragon Incorporated", "Structural Memory", "Snapdragon chassis integrity +2%", "MaxIntegrityPercent", 0.02f, 6),
            new TeamPerkDefinition("TEAM-26", "Snapdragon Incorporated", "Adaptive Response", "Snapdragon chassis stability +2%", "Stability", 0.02f, 15),
            new TeamPerkDefinition("TEAM-27", "Alion Limited", "Defense Prototype", "Alion shields capacity +2%", "ShieldCapacity", 0.02f, 6),
            new TeamPerkDefinition("TEAM-28", "Alion Limited", "Experimental Containment", "Alion defensive utilities cooldown -2%", "DefensiveCooldown", 0.02f, 15)
        };
        public static TeamPerkDefinition Find(string id) => All.FirstOrDefault(p=>p.Id==id);
        public static IEnumerable<TeamPerkDefinition> ForBrand(string name) => All.Where(p=>string.Equals(p.Manufacturer,name,StringComparison.OrdinalIgnoreCase));
        public static float Bonus(TeamState team, string effect, string manufacturer)
        {
            if (team == null || string.IsNullOrWhiteSpace(manufacturer)) return 0f;
            float sum=0f;
            foreach (var perk in ForBrand(manufacturer))
                if(perk.Effect==effect && team.HasTeamPerk(perk.Id)) sum+=perk.Strength;
            return Math.Min(0.10f,sum);
        }
        public static BrandDefinition AddContractRewards(BrandDefinition brand)
        {
            var contracts=brand.Contracts.ToList();
            foreach(var perk in ForBrand(brand.DisplayName))
            {
                string id="teamperk_"+perk.Id;
                if(contracts.Any(c=>c.Id==id))continue;
                contracts.Add(new BrandContractTemplate { Id=id, Title="Permanent Perk: "+perk.Name, Objective=BrandObjective.WinRaces,
                    Target=perk.RequiredStanding>=1100?5:3, RequiredStanding=perk.RequiredStanding, MinimumLifetimeFame=perk.RequiredStanding>=1100?1100:650,
                    StandingReward=2, RewardKind=BrandRewardKind.TeamPerk, ItemId=perk.Id, Credits=0 });
            }
            return new BrandDefinition(brand.Id,brand.DisplayName,brand.Tagline,brand.ColorHex,brand.Category,brand.Philosophy,brand.Products,contracts);
        }
    }
}
