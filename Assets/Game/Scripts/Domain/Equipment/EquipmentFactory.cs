using System;

namespace RaceFatal.Equipment
{
    public class EquipmentFactory
    {
        public EquipmentState Create(EquipmentDefinition definition)
        {
            return new EquipmentState(
                Guid.NewGuid().ToString("N"),
                definition.Id,
                definition.Category,
                definition.RequiredNodeSize);
        }
    }
}
