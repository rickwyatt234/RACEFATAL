using System;
using System.Collections.Generic;
using RaceFatal.Data;
using RaceFatal.Energy;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Equipment
{
    public class RaceEquipmentSystem
    {
        private readonly string racerId;
        private readonly EnergyPool energy;

        private readonly List<WeaponState> weapons = new List<WeaponState>();
        private readonly List<BoosterState> boosters = new List<BoosterState>();
        private readonly List<CountermeasureState> countermeasures = new List<CountermeasureState>();

        private int selectedIndex;
        private float passiveHandlingMultiplier = 1f;

        public RaceShieldState Shield { get; private set; }

        public string SelectedEquipmentId
        {
            get
            {
                WeaponState weapon = GetSelectedWeapon();
                return weapon?.Equipment.EquipmentId;
            }
        }

        public bool HasBooster => boosters.Count > 0;

        public bool IsBoosterActive
        {
            get
            {
                foreach (BoosterState booster in boosters)
                {
                    if (booster.IsActive)
                        return true;
                }

                return false;
            }
        }

        public float HandlingMultiplier => passiveHandlingMultiplier;

        public float SpeedMultiplier
        {
            get
            {
                float multiplier = 1f;

                foreach (BoosterState booster in boosters)
                {
                    if (booster.IsActive)
                        multiplier *= booster.Definition.SpeedMultiplier;
                }

                return multiplier;
            }
        }

        public float AccelerationMultiplier
        {
            get
            {
                float multiplier = 1f;

                foreach (BoosterState booster in boosters)
                {
                    if (booster.IsActive)
                        multiplier *= booster.Definition.AccelerationMultiplier;
                }

                return multiplier;
            }
        }

        public event Action<WeaponFireEvent> WeaponFired;

        public RaceEquipmentSystem(string racerId, EnergyPool energy)
        {
            this.racerId = racerId
                ?? throw new ArgumentNullException(nameof(racerId));

            this.energy = energy
                ?? throw new ArgumentNullException(nameof(energy));
        }

        public static Result<RaceEquipmentSystem> Create(
            string racerId,
            BikeLoadout loadout,
            GameDatabase database,
            EnergyPool energy)
        {
            if (loadout == null)
                return Result<RaceEquipmentSystem>.Failure("Bike loadout is required.");

            if (database == null)
                return Result<RaceEquipmentSystem>.Failure("Game database is required.");

            var system = new RaceEquipmentSystem(racerId, energy);

            foreach (BikeNode node in loadout.Nodes)
            {
                if (!node.IsOccupied)
                    continue;

                EquipmentState equipment = node.InstalledEquipment;

                EquipmentDefinition definition =
                    database.GetEquipmentDefinition(
                        equipment.EquipmentDefinitionId);

                if (definition == null)
                {
                    return Result<RaceEquipmentSystem>.Failure(
                        $"Equipment definition '{equipment.EquipmentDefinitionId}' was not found.");
                }

                Result<RaceEquipmentSystem> result =
                    system.Register(equipment, definition);

                if (!result.IsSuccess)
                {
                    return Result<RaceEquipmentSystem>.Failure(
                        $"Failed to register equipment '{equipment.EquipmentId}': " +
                        $"{result.ErrorMessage}");
                }
            }

            return Result<RaceEquipmentSystem>.Success(system);
        }

        private Result<RaceEquipmentSystem> Register(
            EquipmentState equipment,
            EquipmentDefinition definition)
        {
            switch (definition)
            {
                case WeaponDefinition weapon:
                    weapons.Add(
                        new WeaponState(
                            equipment,
                            weapon));

                    return Result<RaceEquipmentSystem>.Success(this);

                case BoosterDefinition booster:
                    boosters.Add(
                        new BoosterState(
                            equipment,
                            booster));

                    return Result<RaceEquipmentSystem>.Success(this);

                case ShieldDefinition shield:
                    if (Shield != null)
                    {
                        return Result<RaceEquipmentSystem>.Failure(
                            "Only one shield can be installed.");
                    }

                    Shield =
                        new RaceShieldState(
                            shield);

                    return Result<RaceEquipmentSystem>.Success(this);

                case HandlingUtilityDefinition handling:
                    passiveHandlingMultiplier *=
                        handling.HandlingMultiplier;

                    return Result<RaceEquipmentSystem>.Success(this);

                case CountermeasureDefinition countermeasure:
                    countermeasures.Add(
                        new CountermeasureState(
                            equipment,
                            countermeasure));

                    return Result<RaceEquipmentSystem>.Success(this);

                default:
                    return Result<RaceEquipmentSystem>.Failure(
                        $"Unsupported equipment definition '{definition.Id}'.");
            }
        }

        // -----------------------------------------------------
        // WEAPON SELECTION
        // -----------------------------------------------------

        public string SelectNext()
        {
            StopCurrentWeaponActivation();

            if (weapons.Count == 0)
                return null;

            selectedIndex++;

            if (selectedIndex >= weapons.Count)
                selectedIndex = 0;

            return SelectedEquipmentId;
        }

        public string SelectPrevious()
        {
            StopCurrentWeaponActivation();

            if (weapons.Count == 0)
                return null;

            selectedIndex--;

            if (selectedIndex < 0)
                selectedIndex = weapons.Count - 1;

            return SelectedEquipmentId;
        }

        // -----------------------------------------------------
        // SELECTED WEAPON ACTIVATION
        // -----------------------------------------------------

        public bool BeginSelectedActivation()
        {
            WeaponState weapon = GetSelectedWeapon();

            if (weapon == null)
                return false;

            return BeginWeapon(weapon);
        }

        public bool EndSelectedActivation()
        {
            WeaponState weapon = GetSelectedWeapon();

            if (weapon == null)
                return false;

            return EndWeapon(weapon);
        }

        private bool BeginWeapon(WeaponState weapon)
        {
            switch (weapon.Definition.ActivationMode)
            {
                case EquipmentActivationMode.Press:
                    return TryFire(
                        weapon,
                        1f);

                case EquipmentActivationMode.Hold:
                    weapon.IsHeld = true;
                    weapon.FireTimer = 0f;
                    return true;

                case EquipmentActivationMode.ChargeRelease:
                    weapon.IsCharging = true;
                    weapon.ChargeTime = 0f;
                    return true;

                default:
                    return false;
            }
        }

        private bool EndWeapon(WeaponState weapon)
        {
            switch (weapon.Definition.ActivationMode)
            {
                case EquipmentActivationMode.Press:
                    return true;

                case EquipmentActivationMode.Hold:
                    weapon.IsHeld = false;
                    return true;

                case EquipmentActivationMode.ChargeRelease:
                {
                    bool fullyCharged =
                        weapon.ChargeTime >=
                        weapon.Definition.ChargeDuration;

                    weapon.IsCharging = false;

                    float chargeRatio =
                        weapon.Definition.ChargeDuration <= 0f
                            ? 1f
                            : Math.Min(
                                1f,
                                weapon.ChargeTime /
                                weapon.Definition.ChargeDuration);

                    weapon.ChargeTime = 0f;

                    if (!fullyCharged)
                        return false;

                    return TryFire(
                        weapon,
                        chargeRatio);
                }

                default:
                    return false;
            }
        }

        // -----------------------------------------------------
        // BOOST
        // -----------------------------------------------------

        public bool SetBoostActive(bool active)
        {
            if (boosters.Count == 0)
                return false;

            if (active && energy.IsEmpty)
            {
                SetAllBoostersActive(false);
                return false;
            }

            SetAllBoostersActive(active);
            return true;
        }

        private void SetAllBoostersActive(bool active)
        {
            foreach (BoosterState booster in boosters)
                booster.IsActive = active;
        }

        // -----------------------------------------------------
        // UPDATE
        // -----------------------------------------------------

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Shield?.Tick(deltaTime);

            foreach (CountermeasureState countermeasure in countermeasures)
            {
                if (countermeasure.CooldownRemaining <= 0f)
                    continue;

                countermeasure.CooldownRemaining =
                    Math.Max(
                        0f,
                        countermeasure.CooldownRemaining -
                        deltaTime);
            }

            foreach (WeaponState weapon in weapons)
                TickWeapon(weapon, deltaTime);

            TickBoosters(deltaTime);
        }

        private void TickWeapon(
            WeaponState weapon,
            float deltaTime)
        {
            if (weapon.IsCharging)
            {
                weapon.ChargeTime += deltaTime;

                if (weapon.ChargeTime >
                    weapon.Definition.ChargeDuration)
                {
                    weapon.ChargeTime =
                        weapon.Definition.ChargeDuration;
                }
            }

            if (!weapon.IsHeld)
                return;

            weapon.FireTimer -= deltaTime;

            while (weapon.FireTimer <= 0f)
            {
                bool fired =
                    TryFire(
                        weapon,
                        1f);

                weapon.FireTimer +=
                    Math.Max(
                        0.01f,
                        weapon.Definition.FireInterval);

                if (!fired)
                    break;
            }
        }

        private void TickBoosters(float deltaTime)
        {
            float totalCostPerSecond = 0f;
            bool anyActive = false;

            foreach (BoosterState booster in boosters)
            {
                if (!booster.IsActive)
                    continue;

                anyActive = true;

                totalCostPerSecond +=
                    booster.Definition.EnergyPerSecond;
            }

            if (!anyActive)
                return;

            float cost =
                totalCostPerSecond *
                deltaTime;

            if (!energy.TrySpend(cost))
                SetAllBoostersActive(false);
        }

        private bool TryFire(
            WeaponState weapon,
            float chargeRatio)
        {
            if (!energy.TrySpend(
                    weapon.Definition.EnergyCostPerShot))
            {
                return false;
            }

            WeaponFired?.Invoke(
                new WeaponFireEvent(
                    racerId,
                    weapon.Equipment.EquipmentId,
                    weapon.Definition.Id,
                    weapon.Definition.AimMode,
                    weapon.Definition.DeliveryMode,
                    weapon.Definition.Damage,
                    weapon.Definition.Range,
                    weapon.Definition.ProjectileSpeed,
                    chargeRatio));

            return true;
        }

        // -----------------------------------------------------
        // COUNTERMEASURES
        // -----------------------------------------------------

        public bool TryTriggerCountermeasure(
            CountermeasureType type)
        {
            foreach (CountermeasureState state in countermeasures)
            {
                if (state.Definition.CountermeasureType != type)
                    continue;

                if (state.CooldownRemaining > 0f)
                    continue;

                state.CooldownRemaining =
                    state.Definition.Cooldown;

                return true;
            }

            return false;
        }

        // -----------------------------------------------------
        // SHIELD
        // -----------------------------------------------------

        public float AbsorbDamage(float incomingDamage)
        {
            if (Shield == null)
                return incomingDamage;

            return Shield.AbsorbDamage(
                incomingDamage);
        }

        // -----------------------------------------------------
        // HELPERS
        // -----------------------------------------------------

        private WeaponState GetSelectedWeapon()
        {
            if (weapons.Count == 0)
                return null;

            if (selectedIndex < 0 ||
                selectedIndex >= weapons.Count)
            {
                selectedIndex = 0;
            }

            return weapons[selectedIndex];
        }

        private void StopCurrentWeaponActivation()
        {
            WeaponState weapon =
                GetSelectedWeapon();

            if (weapon == null)
                return;

            weapon.IsHeld = false;
            weapon.IsCharging = false;
            weapon.ChargeTime = 0f;
        }

        // -----------------------------------------------------
        // INTERNAL STATE TYPES
        // -----------------------------------------------------

        private abstract class ActivatableState
        {
            public EquipmentState Equipment { get; }

            protected ActivatableState(
                EquipmentState equipment)
            {
                Equipment = equipment;
            }
        }

        private class WeaponState : ActivatableState
        {
            public WeaponDefinition Definition { get; }

            public bool IsHeld;
            public bool IsCharging;

            public float FireTimer;
            public float ChargeTime;

            public WeaponState(
                EquipmentState equipment,
                WeaponDefinition definition)
                : base(equipment)
            {
                Definition = definition;
            }
        }

        private class BoosterState : ActivatableState
        {
            public BoosterDefinition Definition { get; }
            public bool IsActive;

            public BoosterState(
                EquipmentState equipment,
                BoosterDefinition definition)
                : base(equipment)
            {
                Definition = definition;
            }
        }

        private class CountermeasureState
        {
            public EquipmentState Equipment { get; }
            public CountermeasureDefinition Definition { get; }
            public float CooldownRemaining;

            public CountermeasureState(
                EquipmentState equipment,
                CountermeasureDefinition definition)
            {
                Equipment = equipment;
                Definition = definition;
            }
        }
    }
}