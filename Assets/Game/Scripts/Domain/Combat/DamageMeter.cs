using System;

namespace RaceFatal.Combat
{
    public class DamageMeter
    {
        public const float MaxDamage = 100f;
        public float Percent { get; private set; }

        public bool IsDestroyed => Percent >= MaxDamage;

        internal float ApplyDamage(float amount)
        {
            if (amount < 0 || IsDestroyed)
            {
                return 0f;
            }

            float previous = Percent;
            Percent = Math.Min(MaxDamage, Percent + amount);
            return Percent - previous;
        }
    }
}
