namespace RaceFatal.Combat
{
    public readonly struct DamageResolution
    {
        public float IncomingDamage { get; }
        public float ShieldAbsorbed { get; }
        public float BikeDamage { get; }
        public bool CausedDestruction { get; }

        public DamageResolution(float incomingDamage, float shieldAbsorbed, float bikeDamage, bool causedDestruction)
        {
            IncomingDamage = incomingDamage;
            ShieldAbsorbed = shieldAbsorbed;
            BikeDamage = bikeDamage;
            CausedDestruction = causedDestruction;
        }
    }
}