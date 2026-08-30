namespace RaceFatal.Shared
{
    public enum Difficulty
    {
        Normal,
        Hard
    }
    public enum EngineClass
    {
        Class1,
        Class2,
        Class3,
        Class4,
        Class5
    }
    public enum NodeSize
    {
        Small,
        Medium,
        Large
    }
    public enum EquipmentCategory
    {
        Engine,
        Chassis,
        Utility,
        Weapon,
        Shield,
    }
    public enum WeaponAimMode
    {
        Forward,
        RearDrop,
        Targeted,
        AreaOfEffect,
        NotApplicable
    }
    public enum WeaponDeliveryMode
    {
        Hitscan,
        Projectile,
        GuidedProjectile,
        Dropped,
        Area
    }
    public enum EquipmentActivationMode
    {
        Passive,
        Press,
        Hold,
        ChargeRelease,
        Reactive
    }
    public enum CountermeasureType
    {
        None,
        Flare,
        Chaff
    }
    public enum TeamPhilosophy
    {
        Aggressive,
        Teamwork,
        Risk,
        Balanced,
        Opportunistic,
        Defensive,
        Speed
    }
    public enum ResearchField
    {
        ConventionalWeapons,
        LaserTechnology,
        RailgunTechnology,
        MissileTechnology,
        ShieldTechnology,
        EnergyTechnology,
        EngineTechnology,
        ChassisTechnology,
    }
    public enum RacerCareerStatus
    {
        Active,
        Retired,
        Dead
    }
}
