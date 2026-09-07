using System.Collections.Generic;
using RaceFatal.Content.Career;
using RaceFatal.Content.Equipment;
using RaceFatal.Content.Racing;
using RaceFatal.Content.Tracks;
using RaceFatal.Content.Vehicles;
using UnityEngine;

namespace RaceFatal.Content
{
    [CreateAssetMenu(
        fileName = "GameContentCatalog",
        menuName = "RaceFatal/Game Content Catalog")]
    public class GameContentCatalogSO :
        ScriptableObject
    {
        [Header("Vehicles")]

        [SerializeField]
        private List<BikeDefinitionSO>
            bikeDefinitions =
                new List<BikeDefinitionSO>();

        [SerializeField]
        private List<EngineDefinitionSO>
            engineDefinitions =
                new List<EngineDefinitionSO>();

        [SerializeField]
        private List<ChassisDefinitionSO>
            chassisDefinitions =
                new List<ChassisDefinitionSO>();

        [SerializeField]
        private List<BikeBuildDefinitionSO>
            bikeBuildDefinitions =
                new List<BikeBuildDefinitionSO>();

        [Header("Equipment")]

        [SerializeField]
        private List<EquipmentDefinitionSO>
            equipmentDefinitions =
                new List<EquipmentDefinitionSO>();

        [Header("Career")]

        [SerializeField]
        private List<RacerDefinitionSO>
            racerDefinitions =
                new List<RacerDefinitionSO>();

        [SerializeField]
        private List<OpponentTeamDefinitionSO>
            opponentTeamDefinitions =
                new List<OpponentTeamDefinitionSO>();

        [Header("Tracks")]

        [SerializeField]
        private List<TrackDefinitionSO>
            trackDefinitions =
                new List<TrackDefinitionSO>();

        [Header("Races")]

        [SerializeField]
        private List<RaceDefinitionSO>
            raceDefinitions =
                new List<RaceDefinitionSO>();



        public IReadOnlyList<BikeDefinitionSO>
            BikeDefinitions => bikeDefinitions;

        public IReadOnlyList<EngineDefinitionSO>
            EngineDefinitions => engineDefinitions;

        public IReadOnlyList<ChassisDefinitionSO>
            ChassisDefinitions => chassisDefinitions;

        public IReadOnlyList<BikeBuildDefinitionSO>
            BikeBuildDefinitions => bikeBuildDefinitions;

        public IReadOnlyList<EquipmentDefinitionSO>
            EquipmentDefinitions => equipmentDefinitions;

        public IReadOnlyList<RacerDefinitionSO>
            RacerDefinitions => racerDefinitions;

        public IReadOnlyList<OpponentTeamDefinitionSO>
            OpponentTeamDefinitions => opponentTeamDefinitions;

        public IReadOnlyList<TrackDefinitionSO>
            TrackDefinitions => trackDefinitions;

        public IReadOnlyList<RaceDefinitionSO>
            RaceDefinitions => raceDefinitions;

        // -----------------------------------------------------
        // UNITY CONTENT LOOKUPS
        // -----------------------------------------------------

        public TrackDefinitionSO FindTrackContent(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (TrackDefinitionSO track
                     in trackDefinitions)
            {
                if (track != null &&
                    track.Id == id)
                {
                    return track;
                }
            }

            return null;
        }

        public BikeDefinitionSO FindBikeContent(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (BikeDefinitionSO bike
                     in bikeDefinitions)
            {
                if (bike != null &&
                    bike.Id == id)
                {
                    return bike;
                }
            }

            return null;
        }
    }
}