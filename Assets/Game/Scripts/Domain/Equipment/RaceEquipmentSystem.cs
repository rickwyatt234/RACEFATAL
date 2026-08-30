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

        private readonly List<ActivatableState>
            activatables =
                new List<ActivatableState>();

        private readonly List<CountermeasureState>
            countermeasures =
                new List<CountermeasureState>();

        private int selectedIndex;

        private float passiveHandlingMultiplier =
            1f;

        public RaceShieldState Shield {
            get;
            private set;
        }

        public string SelectedEquipmentId
        {
            get
            {
                if (activatables.Count == 0)
                    return null;

                return activatables[
                    selectedIndex]
                    .Equipment.EquipmentId;
            }
        }

        public float HandlingMultiplier =>
            passiveHandlingMultiplier;

        public float SpeedMultiplier
        {
            get
            {
                float multiplier = 1f;

                foreach (ActivatableState state
                         in activatables)
                {
                    if (state is BoosterState booster &&
                        booster.IsActive)
                    {
                        multiplier *=
                            booster.Definition
                                .SpeedMultiplier;
                    }
                }

                return multiplier;
            }
        }

        public float AccelerationMultiplier
        {
            get
            {
                float multiplier = 1f;

                foreach (ActivatableState state
                         in activatables)
                {
                    if (state is BoosterState booster &&
                        booster.IsActive)
                    {
                        multiplier *=
                            booster.Definition
                                .AccelerationMultiplier;
                    }
                }

                return multiplier;
            }
        }

        public event Action<WeaponFireEvent>
            WeaponFired;

        public RaceEquipmentSystem(
            string racerId,
            EnergyPool energy)
        {
            this.racerId = racerId
                ?? throw new ArgumentNullException(
                    nameof(racerId));

            this.energy = energy
                ?? throw new ArgumentNullException(
                    nameof(energy));
        }

        public static Result<RaceEquipmentSystem> Create(
            string racerId,
            BikeLoadout loadout,
            GameDatabase database,
            EnergyPool energy)
        {
            if (loadout == null)
            {
                return Result<RaceEquipmentSystem>.Failure(
                    "Bike loadout is required.");
            }

            if (database == null)
            {
                return Result<RaceEquipmentSystem>.Failure(
                    "Game database is required.");
            }

            var system =
                new RaceEquipmentSystem(
                    racerId,
                    energy);

            foreach (BikeNode node in loadout.Nodes)
            {
                if (!node.IsOccupied)
                    continue;

                EquipmentState equipment =
                    node.InstalledEquipment;

                EquipmentDefinition definition =
                    database.GetEquipmentDefinition(
                        equipment.EquipmentDefinitionId);

                if (definition == null)
                {
                    return Result<RaceEquipmentSystem>.Failure(
                        $"Equipment definition " +
                        $"'{equipment.EquipmentDefinitionId}' " +
                        $"was not found.");
                }

                Result<RaceEquipmentSystem> result =
                    system.Register(
                        equipment,
                        definition);

                if (!result.IsSuccess)
                {
                    return Result<RaceEquipmentSystem>.Failure(
                        $"Failed to register equipment " +
                        $"'{equipment.EquipmentId}': " +
                        $"{result.ErrorMessage}");
                }
            }

            return Result<RaceEquipmentSystem>.Success(
                system);
        }

        private Result<RaceEquipmentSystem> Register(
            EquipmentState equipment,
            EquipmentDefinition definition)
        {
            switch (definition)
            {
                case WeaponDefinition weapon:
                    activatables.Add(
                        new WeaponState(
                            equipment,
                            weapon));
                    return Result<RaceEquipmentSystem>.Success(this);

                case BoosterDefinition booster:
                    activatables.Add(
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
                        $"Unsupported equipment definition " +
                        $"'{definition.Id}'.");
            }
        }

        // ----------------------------
        // SELECTION
        // ----------------------------

        public string SelectNext()
        {
            StopCurrentActivation();

            if (activatables.Count == 0)
                return null;

            selectedIndex++;

            if (selectedIndex >=
                activatables.Count)
            {
                selectedIndex = 0;
            }

            return SelectedEquipmentId;
        }

        public string SelectPrevious()
        {
            StopCurrentActivation();

            if (activatables.Count == 0)
                return null;

            selectedIndex--;

            if (selectedIndex < 0)
            {
                selectedIndex =
                    activatables.Count - 1;
            }

            return SelectedEquipmentId;
        }

        // ----------------------------
        // PLAYER INPUT
        // ----------------------------

        public bool BeginSelectedActivation()
        {
            ActivatableState state =
                GetSelected();

            if (state == null)
                return false;

            switch (state)
            {
                case WeaponState weapon:
                    return BeginWeapon(
                        weapon);

                case BoosterState booster:
                    booster.IsActive = true;
                    return true;

                default:
                    return false;
            }
        }

        public bool EndSelectedActivation()
        {
            ActivatableState state =
                GetSelected();

            if (state == null)
                return false;

            switch (state)
            {
                case WeaponState weapon:
                    return EndWeapon(
                        weapon);

                case BoosterState booster:
                    booster.IsActive = false;
                    return true;

                default:
                    return false;
            }
        }

        private bool BeginWeapon(
            WeaponState weapon)
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

        private bool EndWeapon(
            WeaponState weapon)
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

        // ----------------------------
        // UPDATE
        // ----------------------------

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            Shield?.Tick(deltaTime);

            foreach (CountermeasureState countermeasure
                     in countermeasures)
            {
                if (countermeasure.CooldownRemaining > 0f)
                {
                    countermeasure.CooldownRemaining =
                        Math.Max(
                            0f,
                            countermeasure.CooldownRemaining -
                            deltaTime);
                }
            }

            foreach (ActivatableState state
                     in activatables)
            {
                switch (state)
                {
                    case WeaponState weapon:
                        TickWeapon(
                            weapon,
                            deltaTime);
                        break;

                    case BoosterState booster:
                        TickBooster(
                            booster,
                            deltaTime);
                        break;
                }
            }
        }

        private void TickWeapon(
            WeaponState weapon,
            float deltaTime)
        {
            if (weapon.IsCharging)
            {
                weapon.ChargeTime +=
                    deltaTime;

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

        private void TickBooster(
            BoosterState booster,
            float deltaTime)
        {
            if (!booster.IsActive)
                return;

            float cost =
                booster.Definition
                    .EnergyPerSecond *
                deltaTime;

            if (!energy.TrySpend(cost))
            {
                booster.IsActive = false;
            }
        }

        private bool TryFire(
            WeaponState weapon,
            float chargeRatio)
        {
            if (!energy.TrySpend(
                    weapon.Definition
                        .EnergyCostPerShot))
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

        // ----------------------------
        // COUNTERMEASURES
        // ----------------------------

        public bool TryTriggerCountermeasure(
            CountermeasureType type)
        {
            foreach (CountermeasureState state
                     in countermeasures)
            {
                if (state.Definition
                        .CountermeasureType != type)
                {
                    continue;
                }

                if (state.CooldownRemaining > 0f)
                    continue;

                state.CooldownRemaining =
                    state.Definition.Cooldown;

                return true;
            }

            return false;
        }

        // ----------------------------
        // SHIELD
        // ----------------------------

        public float AbsorbDamage(
            float incomingDamage)
        {
            if (Shield == null)
                return incomingDamage;

            return Shield.AbsorbDamage(
                incomingDamage);
        }

        private ActivatableState GetSelected()
        {
            if (activatables.Count == 0)
                return null;

            return activatables[selectedIndex];
        }

        private void StopCurrentActivation()
        {
            ActivatableState current =
                GetSelected();

            switch (current)
            {
                case WeaponState weapon:
                    weapon.IsHeld = false;
                    weapon.IsCharging = false;
                    weapon.ChargeTime = 0f;
                    break;

                case BoosterState booster:
                    booster.IsActive = false;
                    break;
            }
        }

        // ----------------------------
        // INTERNAL STATE TYPES
        // ----------------------------

        private abstract class ActivatableState
        {
            public EquipmentState Equipment { get; }

            protected ActivatableState(
                EquipmentState equipment)
            {
                Equipment = equipment;
            }
        }

        private sealed class WeaponState :
            ActivatableState
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

        private sealed class BoosterState :
            ActivatableState
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

        private sealed class CountermeasureState
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