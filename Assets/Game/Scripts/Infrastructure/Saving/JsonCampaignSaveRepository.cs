using System;
using System.IO;
using RaceFatal.Shared;
using UnityEngine;

namespace RaceFatal.Infrastructure.Saving
{
    public class JsonCampaignSaveRepository :
        ICampaignSaveRepository
    {
        private readonly string rootDirectory;
        private readonly int slotCount;

        public int SlotCount =>
            slotCount;

        public JsonCampaignSaveRepository(
            string rootDirectory,
            int slotCount = 3)
        {
            if (string.IsNullOrWhiteSpace(
                    rootDirectory))
            {
                throw new ArgumentException(
                    "Save root directory is required.",
                    nameof(rootDirectory));
            }

            if (slotCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotCount),
                    "Save repository must contain at least one slot.");
            }

            this.rootDirectory =
                rootDirectory;

            this.slotCount =
                slotCount;
        }

        public bool Exists(
            int slotIndex)
        {
            if (!IsValidSlot(
                    slotIndex))
            {
                return false;
            }

            return File.Exists(
                GetSlotPath(
                    slotIndex));
        }

        public Result Save(
            int slotIndex,
            CampaignSaveData data)
        {
            if (!IsValidSlot(
                    slotIndex))
            {
                return Result.Failure(
                    GetInvalidSlotMessage(
                        slotIndex));
            }

            if (data == null)
            {
                return Result.Failure(
                    "Campaign save data is required.");
            }

            try
            {
                Directory.CreateDirectory(
                    rootDirectory);

                data.lastSavedUtc =
                    DateTime.UtcNow
                        .ToString("O");

                string json =
                    JsonUtility.ToJson(
                        data,
                        true);

                string slotPath =
                    GetSlotPath(
                        slotIndex);

                string temporaryPath =
                    slotPath + ".tmp";

                string backupPath =
                    slotPath + ".bak";

                File.WriteAllText(
                    temporaryPath,
                    json);

                if (File.Exists(
                        slotPath))
                {
                    File.Copy(
                        slotPath,
                        backupPath,
                        true);
                }

                File.Copy(
                    temporaryPath,
                    slotPath,
                    true);

                File.Delete(
                    temporaryPath);

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    $"Failed to save campaign slot " +
                    $"{slotIndex}: {exception.Message}");
            }
        }

        public Result<CampaignSaveData> Load(
            int slotIndex)
        {
            if (!IsValidSlot(
                    slotIndex))
            {
                return Result<CampaignSaveData>.Failure(
                    GetInvalidSlotMessage(
                        slotIndex));
            }

            string slotPath =
                GetSlotPath(
                    slotIndex);

            if (!File.Exists(
                    slotPath))
            {
                return Result<CampaignSaveData>.Failure(
                    $"Campaign slot {slotIndex} is empty.");
            }

            try
            {
                string json =
                    File.ReadAllText(
                        slotPath);

                if (string.IsNullOrWhiteSpace(
                        json))
                {
                    return Result<CampaignSaveData>.Failure(
                        $"Campaign slot {slotIndex} " +
                        "contains no save data.");
                }

                CampaignSaveData data =
                    JsonUtility.FromJson<
                        CampaignSaveData>(
                            json);

                if (data == null)
                {
                    return Result<CampaignSaveData>.Failure(
                        $"Campaign slot {slotIndex} " +
                        "could not be deserialized.");
                }

                if (data.saveVersion <= 0)
                {
                    return Result<CampaignSaveData>.Failure(
                        $"Campaign slot {slotIndex} " +
                        "has an invalid save version.");
                }

                return Result<CampaignSaveData>.Success(
                    data);
            }
            catch (Exception exception)
            {
                return Result<CampaignSaveData>.Failure(
                    $"Failed to load campaign slot " +
                    $"{slotIndex}: {exception.Message}");
            }
        }

        public Result Delete(
            int slotIndex)
        {
            if (!IsValidSlot(
                    slotIndex))
            {
                return Result.Failure(
                    GetInvalidSlotMessage(
                        slotIndex));
            }

            try
            {
                string slotPath =
                    GetSlotPath(
                        slotIndex);

                string temporaryPath =
                    slotPath + ".tmp";

                string backupPath =
                    slotPath + ".bak";

                if (File.Exists(
                        slotPath))
                {
                    File.Delete(
                        slotPath);
                }

                if (File.Exists(
                        temporaryPath))
                {
                    File.Delete(
                        temporaryPath);
                }

                if (File.Exists(
                        backupPath))
                {
                    File.Delete(
                        backupPath);
                }

                return Result.Success();
            }
            catch (Exception exception)
            {
                return Result.Failure(
                    $"Failed to delete campaign slot " +
                    $"{slotIndex}: {exception.Message}");
            }
        }

        private string GetSlotPath(
            int slotIndex)
        {
            return Path.Combine(
                rootDirectory,
                $"slot_{slotIndex}.json");
        }

        private bool IsValidSlot(
            int slotIndex)
        {
            return slotIndex >= 1 &&
                   slotIndex <= slotCount;
        }

        private string GetInvalidSlotMessage(
            int slotIndex)
        {
            return
                $"Campaign slot {slotIndex} is invalid. " +
                $"Valid slots are 1 through {slotCount}.";
        }
    }
}