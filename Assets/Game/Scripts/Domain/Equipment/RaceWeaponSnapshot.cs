namespace RaceFatal.Equipment
{
    public readonly struct RaceWeaponSnapshot
    {
        public string EquipmentId {
            get;
        }

        public WeaponDefinition Definition {
            get;
        }

        public int CurrentAmmo {
            get;
        }

        public int MaximumAmmo {
            get;
        }

        public bool IsSelected {
            get;
        }

        public bool HasAmmo =>
            CurrentAmmo > 0;

        public float AmmoRatio =>
            MaximumAmmo > 0
                ? (float)CurrentAmmo /
                  MaximumAmmo
                : 0f;

        public RaceWeaponSnapshot(
            string equipmentId,
            WeaponDefinition definition,
            int currentAmmo,
            int maximumAmmo,
            bool isSelected)
        {
            EquipmentId =
                equipmentId;

            Definition =
                definition;

            CurrentAmmo =
                currentAmmo;

            MaximumAmmo =
                maximumAmmo;

            IsSelected =
                isSelected;
        }
    }
}