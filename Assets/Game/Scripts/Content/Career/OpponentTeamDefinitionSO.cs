using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Content.Vehicles;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Content.Career
{
    [CreateAssetMenu(
        fileName = "OpponentTeamDefinition",
        menuName = "RaceFatal/Career/Opponent Team")]
    public class OpponentTeamDefinitionSO :
        ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [Header("Colors")]

        [SerializeField]
        private string primaryColor =
            "#FFFFFF";

        [SerializeField]
        private string secondaryColor =
            "#000000";

        [Header("Behavior")]

        [SerializeField]
        private TeamPhilosophy philosophy;

        [Header("Racers")]

        [SerializeField]
        private List<RacerDefinitionSO>
            racers =
                new List<RacerDefinitionSO>();

        [Header("Starting Garage")]

        [SerializeField]
        private List<BikeBuildDefinitionSO>
            bikeBuilds =
                new List<BikeBuildDefinitionSO>();

        public string Id =>
            id;

        public OpponentTeamDefinition
            CreateDefinition()
        {
            var racerIds =
                new List<string>();

            foreach (RacerDefinitionSO racer
                     in racers)
            {
                if (racer != null)
                {
                    racerIds.Add(
                        racer.Id);
                }
            }

            var bikeBuildIds =
                new List<string>();

            foreach (BikeBuildDefinitionSO build
                     in bikeBuilds)
            {
                if (build != null)
                {
                    bikeBuildIds.Add(
                        build.Id);
                }
            }

            return new OpponentTeamDefinition(
                id,
                displayName,
                primaryColor,
                secondaryColor,
                philosophy,
                racerIds,
                bikeBuildIds);
        }
    }
}