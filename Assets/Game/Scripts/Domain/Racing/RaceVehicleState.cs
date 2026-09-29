using System;
using RaceFatal.Combat;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RaceVehicleState
    {
        public BikeState Bike { get; }
        public BikePerformance Performance { get; }
        public DamageMeter Damage { get; }
        public EnergyPool EnergyPool { get; }
        public RaceEquipmentSystem EquipmentSystem { get; }

        private float temporaryHandlingMultiplier = 1f;
        public float TemporaryHandlingMultiplier
        {
            get => temporaryHandlingMultiplier;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                    throw new ArgumentOutOfRangeException(nameof(value));
                temporaryHandlingMultiplier = value;
            }
        }

        public float RuntimeHandling => Performance.ConfiguredHandling * TemporaryHandlingMultiplier;
        public bool IsDestroyed => Damage.IsDestroyed || Bike.IsDestroyed;

        public RaceVehicleState(BikeState bike, BikePerformance performance, EnergyPool energyPool, RaceEquipmentSystem equipmentSystem)
        {
            Bike = bike ?? throw new ArgumentNullException(nameof(bike));
            Performance = performance ?? throw new ArgumentNullException(nameof(performance));
            EnergyPool = energyPool ?? throw new ArgumentNullException(nameof(energyPool));
            EquipmentSystem = equipmentSystem ?? throw new ArgumentNullException(nameof(equipmentSystem));
            Damage = new DamageMeter(performance.MaxIntegrity);
        }

        internal DamageResolution ApplyDamage(float incomingDamage, DamageCause cause = DamageCause.Other)
        {
            if (incomingDamage <= 0f)
            {
                return new DamageResolution(incomingDamage: 0f, shieldAbsorbed: 0f, bikeDamage: 0f, causedDestruction: false);
            }

            float remainingDamage = EquipmentSystem.AbsorbDamage(incomingDamage);
            float shieldAbsorbed = incomingDamage - remainingDamage;
            if (cause == DamageCause.Collision)
                remainingDamage *= 1f - Performance.ImpactResistance;
            bool wasDestroyed = Damage.IsDestroyed;
            float bikeDamage = Damage.ApplyDamage(remainingDamage);
            bool destroyedNow = !wasDestroyed && Damage.IsDestroyed;
            return new DamageResolution(incomingDamage: incomingDamage, shieldAbsorbed: shieldAbsorbed, bikeDamage: bikeDamage, causedDestruction: destroyedNow);
        }

        internal void Tick(float deltaTime)
        {
            EquipmentSystem.Tick(deltaTime);
        }

        internal bool TrySpendEnergy(float amount)
        {
            return EnergyPool.TrySpend(amount);
        }

        internal float RechargeEnergy(float amount)
        {
            return EnergyPool.Recharge(amount);
        }
    }
}
