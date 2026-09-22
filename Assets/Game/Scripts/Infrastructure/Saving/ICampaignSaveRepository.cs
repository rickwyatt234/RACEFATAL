using RaceFatal.Shared;

namespace RaceFatal.Infrastructure.Saving
{
    public interface ICampaignSaveRepository
    {
        int SlotCount {
            get;
        }

        bool Exists(
            int slotIndex);

        Result Save(
            int slotIndex,
            CampaignSaveData data);

        Result<CampaignSaveData> Load(
            int slotIndex);

        Result Delete(
            int slotIndex);
    }
}