using System;

namespace RaceFatal.Energy
{
    public class EnergyPool
    {
        public float CurrentEnergy { get; private set; }
        public float MaxEnergy { get; }

        public bool IsEmpty => CurrentEnergy <= 0f;

        public float EnergyRatio =>
            MaxEnergy > 0f
                ? CurrentEnergy / MaxEnergy
                : 0f;

        public event Action<float, float> OnEnergyChanged;

        public EnergyPool(float maxEnergy)
        {
            if (maxEnergy <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxEnergy),
                    "Max energy must be greater than zero.");
            }

            MaxEnergy = maxEnergy;
            CurrentEnergy = maxEnergy;
        }

        public bool CanSpend(float amount)
        {
            return amount >= 0f &&
                   CurrentEnergy >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount))
                return false;

            CurrentEnergy -= amount;

            OnEnergyChanged?.Invoke(
                CurrentEnergy,
                MaxEnergy);

            return true;
        }

        public float SpendUpTo(float amount)
        {
            if (amount <= 0f ||
                CurrentEnergy <= 0f)
            {
                return 0f;
            }

            float spent =
                Math.Min(
                    CurrentEnergy,
                    amount);

            CurrentEnergy -= spent;

            OnEnergyChanged?.Invoke(
                CurrentEnergy,
                MaxEnergy);

            return spent;
        }

        public float Recharge(float amount)
        {
            if (amount <= 0f)
                return 0f;

            float previous =
                CurrentEnergy;

            CurrentEnergy =
                Math.Min(
                    MaxEnergy,
                    CurrentEnergy + amount);

            float restored =
                CurrentEnergy - previous;

            if (restored > 0f)
            {
                OnEnergyChanged?.Invoke(
                    CurrentEnergy,
                    MaxEnergy);
            }

            return restored;
        }
    }
}