using System;

namespace RaceFatal.Combat
{
    public class DamageMeter
    {
        public float MaxIntegrity { get; }
        public float AccumulatedDamage { get; private set; }
        public float CurrentIntegrity => Math.Max(0f, MaxIntegrity - AccumulatedDamage);
        public float DamageRatio => AccumulatedDamage / MaxIntegrity;
        public float Percent => DamageRatio * 100f;
        public bool IsDestroyed => CurrentIntegrity <= 0f;

        public DamageMeter(float maxIntegrity = 100f)
        {
            if (float.IsNaN(maxIntegrity) || float.IsInfinity(maxIntegrity) || maxIntegrity <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxIntegrity));
            MaxIntegrity = maxIntegrity;
        }

        internal float ApplyDamage(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f || IsDestroyed)
                return 0f;
            float previous = AccumulatedDamage;
            AccumulatedDamage = Math.Min(MaxIntegrity, AccumulatedDamage + amount);
            return AccumulatedDamage - previous;
        }
    }
}
