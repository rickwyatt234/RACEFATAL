using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public sealed class OpponentTeamDefinition
    {
        public string Id { get; }

        public string DisplayName { get; }

        public string PrimaryColor { get; }

        public string SecondaryColor { get; }

        public TeamPhilosophy Philosophy { get; }

        public IReadOnlyList<string>
            RacerDefinitionIds { get; }

        public IReadOnlyList<string>
            StartingBikeBuildIds { get; }

        public OpponentTeamDefinition(
            string id,
            string displayName,
            string primaryColor,
            string secondaryColor,
            TeamPhilosophy philosophy,
            IReadOnlyList<string> racerDefinitionIds,
            IReadOnlyList<string> startingBikeBuildIds)
        {
            Id = id
                ?? throw new ArgumentNullException(
                    nameof(id));

            DisplayName = displayName
                ?? throw new ArgumentNullException(
                    nameof(displayName));

            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;

            Philosophy = philosophy;

            RacerDefinitionIds =
                racerDefinitionIds
                ?? Array.Empty<string>();

            StartingBikeBuildIds =
                startingBikeBuildIds
                ?? Array.Empty<string>();
        }
    }
}