using System;

namespace RaceFatal.Career
{
    [Serializable]
    public class RacerDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public RacerDefinition(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }
}