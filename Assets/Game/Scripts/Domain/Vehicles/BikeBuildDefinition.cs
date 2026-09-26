using System;
using System.Collections.Generic;

namespace RaceFatal.Vehicles
{
    public sealed class BikeBuildDefinition
    {
        public bool AvailableInShop { get; }
        public int CreditCost { get; }
        public string RequiredTechnologyId { get; }
        public string Id { get; }

        public string DisplayName { get; }

        public string BikeDefinitionId { get; }

        public string EngineDefinitionId { get; }

        public string ChassisDefinitionId { get; }

        public IReadOnlyList<EquipmentMountDefinition>
            Equipment { get; }

        public BikeBuildDefinition(
            string id,
            string displayName,
            string bikeDefinitionId,
            string engineDefinitionId,
            string chassisDefinitionId,
            IReadOnlyList<EquipmentMountDefinition> equipment,
            bool availableInShop = false,
            int creditCost = 12000,
            string requiredTechnologyId = null)
        {
            AvailableInShop = availableInShop;
            CreditCost = creditCost;
            RequiredTechnologyId = requiredTechnologyId;
            Id = id
                ?? throw new ArgumentNullException(
                    nameof(id));

            DisplayName = displayName
                ?? throw new ArgumentNullException(
                    nameof(displayName));

            BikeDefinitionId =
                bikeDefinitionId
                ?? throw new ArgumentNullException(
                    nameof(bikeDefinitionId));

            EngineDefinitionId =
                engineDefinitionId
                ?? throw new ArgumentNullException(
                    nameof(engineDefinitionId));

            ChassisDefinitionId =
                chassisDefinitionId
                ?? throw new ArgumentNullException(
                    nameof(chassisDefinitionId));

            Equipment =
                equipment
                ?? Array.Empty<
                    EquipmentMountDefinition>();
        }
    }
}