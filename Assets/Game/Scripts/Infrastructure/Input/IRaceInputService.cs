namespace RaceFatal.Infrastructure.Input
{
    public interface IRaceInputService
    {
        float Throttle { get; }
        float Brake { get; }
        float Steering { get; }
        bool EquipmentPressed { get; }
        bool EquipmentReleased { get; }
        bool NextEquipmentPressed { get; }
        bool PreviousEquipmentPressed { get; }
    }
}