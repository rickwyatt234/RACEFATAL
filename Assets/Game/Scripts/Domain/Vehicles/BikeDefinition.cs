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

        public BikeDefinition(
            string id,
            string displayName,
            int smallNodeCount,
            int mediumNodeCount,
            int largeNodeCount,
            float baseWeight,
            float baseHandling)
        {
            Id = id;
            DisplayName = displayName;
            SmallNodeCount = smallNodeCount;
            MediumNodeCount = mediumNodeCount;
            LargeNodeCount = largeNodeCount;
            BaseWeight = baseWeight;
            BaseHandling = baseHandling;
        }
    }
}