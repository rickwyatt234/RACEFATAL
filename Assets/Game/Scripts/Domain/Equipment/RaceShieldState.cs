using System;
using RaceFatal.Energy;

namespace RaceFatal.Equipment
{
    public class RaceShieldState
    {
        private float timeSinceDamage;
        private readonly float capacityMultiplier, rechargeMultiplier, delayMultiplier;
        public float EmergencyRechargeMultiplier { get; set; } = 1f;
        public ShieldDefinition Definition { get; }
        public float Current { get; private set; }
        public float BaseMaximum => Definition.Capacity;
        public float Maximum => Definition.Capacity * capacityMultiplier;
        public bool IsDepleted => Current <= 0f;
        public bool IsFullyCharged => Current >= Maximum;

        public RaceShieldState(ShieldDefinition definition, EnergyPool energy, float capacityBonus = 0f, float rechargeBonus = 0f, float delayReduction = 0f)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            capacityMultiplier = 1f + Math.Min(.15f, Math.Max(0f,capacityBonus));
            rechargeMultiplier = 1f + Math.Min(.2f, Math.Max(0f,rechargeBonus));
            delayMultiplier = 1f - Math.Min(.2f, Math.Max(0f,delayReduction));
            if (energy == null) throw new ArgumentNullException(nameof(energy));
            Current = Maximum;
            timeSinceDamage = Definition.RechargeDelay * delayMultiplier;
        }

        public float AbsorbDamage(float incomingDamage)
        {
            if (incomingDamage <= 0f)
                return 0f;
            ClampToMaximum();
            timeSinceDamage = 0f;
            if (Current <= 0f)
                return incomingDamage;
            float absorbed = Math.Min(Current, incomingDamage);
            Current -= absorbed;
            return incomingDamage - absorbed;
        }

        public void Tick(float deltaTime)
        {
            ClampToMaximum();
            if (deltaTime <= 0f)
                return;
            if (IsFullyCharged)
                return;
            float previousTime = timeSinceDamage;
            timeSinceDamage += deltaTime;
            float rechargeTime = Math.Max(0f,timeSinceDamage - Math.Max(previousTime,Definition.RechargeDelay * delayMultiplier));
            Current = Math.Min(Maximum, Current + Definition.RechargePerSecond * rechargeMultiplier * Math.Max(0f,EmergencyRechargeMultiplier) * rechargeTime);
        }

        private void ClampToMaximum()
        {
            if (Current > Maximum)
                Current = Maximum;
            if (Current < 0f)
                Current = 0f;
        }
    }
}
