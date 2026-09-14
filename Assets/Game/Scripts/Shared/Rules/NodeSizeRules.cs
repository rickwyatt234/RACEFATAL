namespace RaceFatal.Shared
{
    public static class NodeSizeRules
    {
        public static bool CanFit(
            NodeSize requiredSize,
            NodeSize availableSize)
        {
            switch (availableSize)
            {
                case NodeSize.Small:
                    return requiredSize == NodeSize.Small;

                case NodeSize.Medium:
                    return requiredSize == NodeSize.Small ||
                           requiredSize == NodeSize.Medium;

                case NodeSize.Large:
                    return requiredSize == NodeSize.Small ||
                           requiredSize == NodeSize.Medium ||
                           requiredSize == NodeSize.Large;

                default:
                    return false;
            }
        }
    }
}