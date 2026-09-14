using System;
using RaceFatal.Shared;

namespace RaceFatal.Vehicles
{
    public class BikeState
    {
        public string BikeId { get; }
        public string BikeDefinitionId { get; }

        public string PrimaryColor { get; private set; }
        public string SecondaryColor { get; private set; }

        public BikeLoadout Loadout { get; }

        public bool IsDestroyed { get; private set; }

        public bool IsRaceReady =>
            !IsDestroyed &&
            Loadout.Engine != null &&
            Loadout.Chassis != null;

        public EngineClass? EngineClass
        {
            get
            {
                if (Loadout.Engine != null)
                    return Loadout.Engine.EngineClass;

                return null;
            }
        }

        public BikeState(
            string bikeId,
            BikeDefinition definition,
            string primaryColor,
            string secondaryColor)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(bikeId))
            {
                throw new ArgumentException(
                    "Bike ID is required.",
                    nameof(bikeId));
            }

            BikeId = bikeId;
            BikeDefinitionId = definition.Id;

            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            Loadout =
                new BikeLoadout(
                    definition.SmallNodeCount,
                    definition.MediumNodeCount,
                    definition.LargeNodeCount);
        }

        /*
         * Temporary compatibility constructor.
         *
         * Existing bootstrap / save construction can continue
         * compiling while we migrate creation to BikeDefinition.
         *
         * New gameplay code should use the BikeDefinition
         * constructor instead.
         */
        public BikeState(
            string bikeId,
            string bikeDefinitionId,
            int smallNodes,
            int mediumNodes,
            int largeNodes,
            string primaryColor,
            string secondaryColor)
        {
            if (string.IsNullOrWhiteSpace(bikeId))
            {
                throw new ArgumentException(
                    "Bike ID is required.",
                    nameof(bikeId));
            }

            if (string.IsNullOrWhiteSpace(bikeDefinitionId))
            {
                throw new ArgumentException(
                    "Bike definition ID is required.",
                    nameof(bikeDefinitionId));
            }

            if (smallNodes < 0 ||
                mediumNodes < 0 ||
                largeNodes < 0)
            {
                throw new ArgumentException(
                    "Node counts cannot be negative.");
            }

            BikeId = bikeId;
            BikeDefinitionId = bikeDefinitionId;

            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            Loadout =
                new BikeLoadout(
                    smallNodes,
                    mediumNodes,
                    largeNodes);
        }

        public void Paint(
            string primaryColor,
            string secondaryColor)
        {
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
        }

        public void Destroy()
        {
            if (IsDestroyed)
                return;

            IsDestroyed = true;

            Loadout.DestroyInstalledEquipment();
        }
    }
}