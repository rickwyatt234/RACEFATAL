using System;

namespace RaceFatal.Vehicles
{
    public class BikeDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public int SmallNodeCount { get; }
        public int MediumNodeCount { get; }
        public int LargeNodeCount { get; }

        public float BaseWeight { get; }
        public float BaseHandling { get; }
        public float EnergyCapacity { get; }

        public int CreditCost { get; }

        public string RequiredTechnologyId { get; }

        public BikeDefinition(
            string id,
            string displayName,
            int smallNodeCount,
            int mediumNodeCount,
            int largeNodeCount,
            float baseWeight,
            float baseHandling,
            float energyCapacity,
            int creditCost,
            string requiredTechnologyId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Bike definition ID is required.", nameof(id));

            if (smallNodeCount < 0 ||
                mediumNodeCount < 0 ||
                largeNodeCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(smallNodeCount),
                    "Bike node counts cannot be negative.");
            }

            Id = id;
            DisplayName = displayName;

            SmallNodeCount = smallNodeCount;
            MediumNodeCount = mediumNodeCount;
            LargeNodeCount = largeNodeCount;

            BaseWeight = Math.Max(0f, baseWeight);
            BaseHandling = Math.Max(0f, baseHandling);
            EnergyCapacity = Math.Max(1f, energyCapacity);

            CreditCost =
                Math.Max(
                    0,
                    creditCost);

            RequiredTechnologyId =
                requiredTechnologyId;
        }

        public BikeState CreateBikeState(
            string bikeId,
            string primaryColor,
            string secondaryColor)
        {
            return new BikeState(
                bikeId,
                this,
                primaryColor,
                secondaryColor);
        }
    }
}