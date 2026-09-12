using System;
using RaceFatal.Energy;

namespace RaceFatal.Equipment
{
    public class RaceShieldState
    {
        private readonly EnergyPool energy;

        private float timeSinceDamage;

        public ShieldDefinition Definition { get; }

        public float Current { get; private set; }

        public float BaseMaximum =>
            Definition.Capacity;

        public float Maximum =>
            Definition.Capacity *
            Math.Clamp(
                energy.EnergyRatio,
                0f,
                1f);

        public bool IsDepleted =>
            Current <= 0f;

        public bool IsFullyCharged =>
            Current >= Maximum;

        public RaceShieldState(
            ShieldDefinition definition,
            EnergyPool energy)
        {
            Definition = definition
                ?? throw new ArgumentNullException(
                    nameof(definition));

            this.energy = energy
                ?? throw new ArgumentNullException(
                    nameof(energy));

            Current = Maximum;

            /*
             * Recharge delay is caused by taking damage,
             * not simply by Energy capacity changing.
             */
            timeSinceDamage =
                Definition.RechargeDelay;
        }

        public float AbsorbDamage(
            float incomingDamage)
        {
            if (incomingDamage <= 0f)
                return 0f;

            ClampToMaximum();

            timeSinceDamage = 0f;

            if (Current <= 0f)
                return incomingDamage;

            float absorbed =
                Math.Min(
                    Current,
                    incomingDamage);

            Current -= absorbed;

            return incomingDamage - absorbed;
        }

        public void Tick(
            float deltaTime)
        {
            ClampToMaximum();

            if (deltaTime <= 0f)
                return;

            if (IsFullyCharged)
                return;

            timeSinceDamage +=
                deltaTime;

            if (timeSinceDamage <
                Definition.RechargeDelay)
            {
                return;
            }

            Current =
                Math.Min(
                    Maximum,
                    Current +
                    Definition.RechargePerSecond *
                    deltaTime);
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