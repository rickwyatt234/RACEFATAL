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

        public BikeDefinition(string id, string displayName, int smallNodeCount, int mediumNodeCount, int largeNodeCount)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Bike definition ID is required.", nameof(id));
            if (smallNodeCount < 0 || mediumNodeCount < 0 || largeNodeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(smallNodeCount), "Bike node counts cannot be negative.");
            }

            Id = id;
            DisplayName = displayName;
            SmallNodeCount = smallNodeCount;
            MediumNodeCount = mediumNodeCount;
            LargeNodeCount = largeNodeCount;
        }

        public BikeState CreateBikeState(string bikeId, string primaryColor, string secondaryColor)
        {
            return new BikeState(bikeId, this, primaryColor, secondaryColor);
        }
    }
}
