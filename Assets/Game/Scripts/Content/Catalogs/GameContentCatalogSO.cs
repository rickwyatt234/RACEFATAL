using UnityEngine;
using System.Collections.Generic;
using RaceFatal.Content.Vehicles;
using RaceFatal.Content.Racing;
using RaceFatal.Content.Tracks;
using RaceFatal.Content.Equipment;

namespace RaceFatal.Content
{
    [CreateAssetMenu(fileName = "GameContentCatalog", menuName = "RaceFatal/Game Content Catalog")]
    public class GameContentCatalogSO : ScriptableObject
    {
        [Header("Bike Definitions")]
        [SerializeField] private List<BikeDefinitionSO> bikeDefinitions = new List<BikeDefinitionSO>();

        [Header("Engine Definitions")]
        [SerializeField] private List<EngineDefinitionSO> engineDefinitions = new List<EngineDefinitionSO>();

        [Header("Chassis Definitions")]
        [SerializeField] private List<ChassisDefinitionSO> chassisDefinitions = new List<ChassisDefinitionSO>();

        [Header("Equipment Definitions")]
        [SerializeField] private List<EquipmentDefinitionSO> equipmentDefinitions = new List<EquipmentDefinitionSO>();

        [Header("Track Definitions")]
        [SerializeField] private List<TrackDefinitionSO> trackDefinitions = new List<TrackDefinitionSO>();

        [Header("Race Definitions")]
        [SerializeField] private List<RaceDefinitionSO> raceDefinitions = new List<RaceDefinitionSO>();

        public IReadOnlyList<BikeDefinitionSO> BikeDefinitions => bikeDefinitions;
        public IReadOnlyList<EngineDefinitionSO> EngineDefinitions => engineDefinitions;
        public IReadOnlyList<ChassisDefinitionSO> ChassisDefinitions => chassisDefinitions;
        public IReadOnlyList<EquipmentDefinitionSO> EquipmentDefinitions => equipmentDefinitions;
        public IReadOnlyList<TrackDefinitionSO> TrackDefinitions => trackDefinitions;
        public IReadOnlyList<RaceDefinitionSO> RaceDefinitions => raceDefinitions;
    }    
}
