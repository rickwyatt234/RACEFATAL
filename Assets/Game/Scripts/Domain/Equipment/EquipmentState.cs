using RaceFatal.Shared;

namespace RaceFatal.Equipment
{
    public class EquipmentState
    {
        public string EquipmentId { get; }
        public string EquipmentDefinitionId { get;  } 
        public EquipmentCategory Category { get; }
        public NodeSize RequiredNodeSize { get; }
        public bool IsDestroyed { get; private set; }

        public EquipmentState(
            string equipmentId, 
            string equipmentDefinitionId, 
            EquipmentCategory category, 
            NodeSize requiredNodeSize)
        {
            EquipmentId = equipmentId;
            EquipmentDefinitionId = equipmentDefinitionId;
            Category = category;
            RequiredNodeSize = requiredNodeSize;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
