using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class TechnologyDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public ResearchField Field { get; }
        public int ResearchCost { get; }
        public IReadOnlyList<string> PrerequisiteTechnologyIds { get; }
        public int DisplayOrder { get; }

        public TechnologyDefinition(string id, string displayName, string description,
            ResearchField field, int researchCost, IEnumerable<string> prerequisites = null, int displayOrder = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Technology ID is required.", nameof(id));
            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Description = description ?? string.Empty;
            Field = field;
            ResearchCost = researchCost;
            PrerequisiteTechnologyIds = new List<string>(prerequisites ?? Array.Empty<string>()).AsReadOnly();
            DisplayOrder = displayOrder;
        }
    }
}
