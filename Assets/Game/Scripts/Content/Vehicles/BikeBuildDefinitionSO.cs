using System;
using System.Collections.Generic;
using RaceFatal.Content.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;
using UnityEngine;

namespace RaceFatal.Content.Vehicles
{
    [CreateAssetMenu(
        fileName = "BikeBuildDefinition",
        menuName = "RaceFatal/Vehicles/Bike Build")]
    public class BikeBuildDefinitionSO :
        ScriptableObject
    {
        [Serializable]
        private class EquipmentMount
        {
            public EquipmentDefinitionSO equipment;

            public NodeSize nodeSize;

            [Min(0)]
            public int nodeIndex;
        }

        [Header("Identity")]

        [SerializeField]
        private string id;

        [SerializeField]
        private string displayName;

        [Header("Bike")]

        [SerializeField]
        private BikeDefinitionSO bike;

        [SerializeField]
        private EngineDefinitionSO engine;

        [SerializeField]
        private ChassisDefinitionSO chassis;

        [Header("Equipment")]

        [SerializeField]
        private List<EquipmentMount>
            equipment =
                new List<EquipmentMount>();

        public string Id =>
            id;

        public BikeBuildDefinition
            CreateDefinition()
        {
            var mounts =
                new List<
                    EquipmentMountDefinition>();

            foreach (EquipmentMount mount
                     in equipment)
            {
                if (mount == null ||
                    mount.equipment == null)
                {
                    continue;
                }

                mounts.Add(
                    new EquipmentMountDefinition(
                        mount.equipment.Id,
                        mount.nodeSize,
                        mount.nodeIndex));
            }

            return new BikeBuildDefinition(
                id,
                displayName,
                bike != null
                    ? bike.Id
                    : null,
                engine != null
                    ? engine.Id
                    : null,
                chassis != null
                    ? chassis.Id
                    : null,
                mounts);
        }
    }
}