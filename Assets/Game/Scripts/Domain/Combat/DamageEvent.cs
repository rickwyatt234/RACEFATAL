namespace RaceFatal.Combat
{
    public readonly struct DamageEvent
    {
        public string AttackerRacerId { get; }
        public string VictimRacerId { get; }
        public float IncomingDamage { get; }
        public float ShieldAbsorbed { get; }
        public float BikeDamage { get; }
        public DamageCause Cause { get; }
        public bool CausedDestruction { get; }

        public DamageEvent(
            string attackerRacerId,
            string victimRacerId,
            float incomingDamage,
            float shieldAbsorbed,
            float bikeDamage,
            DamageCause cause,
            bool causedDestruction)
        {
            AttackerRacerId = attackerRacerId;
            VictimRacerId = victimRacerId;
            IncomingDamage = incomingDamage;
            ShieldAbsorbed = shieldAbsorbed;
            BikeDamage = bikeDamage;
            Cause = cause;
            CausedDestruction = causedDestruction;
        }
    }

}