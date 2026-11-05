using System;

namespace RaceFatal.Vehicles
{
    public enum BikeWeightClass { Heavy, Medium, Light }

    public class BikeDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public int SmallNodeCount { get; }
        public int MediumNodeCount { get; }
        public int LargeNodeCount { get; }

        public BikeWeightClass WeightClass { get; }
        public float SpeedPenalty { get; }
        public float AccelerationPenalty { get; }
        public float IntegrityPenalty { get; }
        public float HandlingPenalty { get; }

        public BikeDefinition(string id, string displayName, int smallNodeCount, int mediumNodeCount, int largeNodeCount,
            BikeWeightClass weightClass = BikeWeightClass.Medium, float speedPenalty = 0f,
            float accelerationPenalty = 0f, float integrityPenalty = 0f, float handlingPenalty = 0f)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Bike definition ID is required.", nameof(id));
            if (smallNodeCount < 0 || mediumNodeCount < 0 || largeNodeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(smallNodeCount), "Bike node counts cannot be negative.");
            }

            WeightClass = weightClass;
            SpeedPenalty = ValidatePenalty(speedPenalty);
            AccelerationPenalty = ValidatePenalty(accelerationPenalty);
            IntegrityPenalty = ValidatePenalty(integrityPenalty);
            HandlingPenalty = ValidatePenalty(handlingPenalty);
            Id = id;
            DisplayName = displayName;
            SmallNodeCount = smallNodeCount;
            MediumNodeCount = mediumNodeCount;
            LargeNodeCount = largeNodeCount;
        }

        private static float ValidatePenalty(float value)
        {
            if (!(value >= 0f && value < 1f))
                throw new ArgumentOutOfRangeException(nameof(value));
            return value;
        }

        public BikeState CreateBikeState(string bikeId, string primaryColor, string secondaryColor)
        {
            return new BikeState(bikeId, this, primaryColor, secondaryColor);
        }
    }
}
