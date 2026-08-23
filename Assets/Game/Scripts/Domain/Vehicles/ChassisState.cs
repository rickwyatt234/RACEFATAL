namespace RaceFatal.Vehicles
{
    public class ChassisState
    {
        public string ChassisId { get; }
        public string ChassisDefinitionId { get; }
        public bool IsDestroyed { get; private set; }

        public ChassisState(string chassisId, string chassisDefinitionId)
        {
            ChassisId = chassisId;
            ChassisDefinitionId = chassisDefinitionId;
        }

        public void Destroy()
        {
            IsDestroyed = true;
        }
    }
}
