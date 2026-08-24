using System;

namespace RaceFatal.Equipment
{
    public class RaceShieldState
    {
        private float timeSinceDamage;
        public ShieldDefinition Definition { get; }
        public float Current  { get; private set; }
        public float Maximum => Definition.Capacity;
        public bool IsDepleted => Current <= 0f;
        public bool IsFullyCharged => Current >= Maximum;
        
        public RaceShieldState(ShieldDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Current = Maximum;
        }

        public float AbsorbDamage(float incomingDamage)
        {
            if (incomingDamage <= 0f)
            {
                return 0f;
            }

            timeSinceDamage = 0f;

            if (Current <= 0f)
            {
                return incomingDamage;
            }

            float absorbed = Math.Min(Current, incomingDamage);
            Current -= absorbed;

            return incomingDamage - absorbed;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }
            if (IsFullyCharged)
            {
                return;
            }
            timeSinceDamage += deltaTime;
            if (timeSinceDamage < Definition.RechargeDelay)
            {
                return;
            }
            Current = Math.Min(Maximum, Current + Definition.RechargePerSecond * deltaTime);
        }
    }
}