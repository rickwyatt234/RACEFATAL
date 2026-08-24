using System;

namespace RaceFatal.Tracks
{
    public class TrackDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }

        public TrackDefinition(string id, string displayName)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        }
    }
}