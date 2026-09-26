using System;
using RaceFatal.Career;
using RaceFatal.Shared;

namespace RaceFatal.Infrastructure.Saving
{
    public class CampaignSaveService
    {
        private readonly GameSessionManager sessions;
        private readonly CampaignSaveMapper mapper;
        private readonly ICampaignSaveRepository repository;

        public int SlotCount =>
            repository.SlotCount;

        public int? ActiveSlotIndex {
            get;
            private set;
        }

        public bool HasActiveCampaign =>
            sessions.HasSession &&
            ActiveSlotIndex.HasValue;

        public CampaignSaveService(
            GameSessionManager sessions,
            CampaignSaveMapper mapper,
            ICampaignSaveRepository repository)
        {
            this.sessions =
                sessions
                ?? throw new ArgumentNullException(
                    nameof(sessions));

            this.mapper =
                mapper
                ?? throw new ArgumentNullException(
                    nameof(mapper));

            this.repository =
                repository
                ?? throw new ArgumentNullException(
                    nameof(repository));
        }

        public Result StartSuccessor(string name) => SaveCareerTransition(() => sessions.StartSuccessor(name));
        public Result AcknowledgeNewRacer() => SaveCareerTransition(sessions.AcknowledgeNewRacer);
        public Result RetirePlayer() => SaveCareerTransition(sessions.RetirePlayer);

        // Prepare a restorable snapshot before changing racer identity. A failed write
        // must not leave a successor or permanent retirement only in memory.
        private Result SaveCareerTransition(Func<Result> transition)
        {
            if (!HasActiveCampaign) return Result.Failure("A saved campaign is required.");
            var captured = mapper.Capture(sessions.Current);
            if (!captured.IsSuccess) return Result.Failure(captured.ErrorMessage);
            var before = mapper.Restore(captured.Value);
            if (!before.IsSuccess) return Result.Failure(before.ErrorMessage);
            Result result;
            try
            {
                result = transition();
                if (result.IsSuccess) result = SaveCurrentCampaign();
            }
            catch (Exception exception) { result = Result.Failure(exception.Message); }
            if (result.IsSuccess) return result;
            sessions.ClearSession();
            var restored = sessions.RestoreSession(before.Value);
            return Result.Failure(restored.IsSuccess
                ? "Change was not saved and has been undone. " + result.ErrorMessage
                : "Change failed: " + result.ErrorMessage + " Restore failed: " + restored.ErrorMessage);
        }

        public Result<SaveSlotSummary> GetSlotSummary(
            int slotIndex)
        {
            Result slotResult =
                ValidateSlot(
                    slotIndex);

            if (!slotResult.IsSuccess)
            {
                return Result<SaveSlotSummary>.Failure(
                    slotResult.ErrorMessage);
            }

            if (!repository.Exists(
                    slotIndex))
            {
                return Result<SaveSlotSummary>.Success(
                    SaveSlotSummary.Empty(
                        slotIndex));
            }

            Result<CampaignSaveData> loadResult =
                repository.Load(
                    slotIndex);

            if (!loadResult.IsSuccess)
            {
                return Result<SaveSlotSummary>.Failure(
                    loadResult.ErrorMessage);
            }

            CampaignSaveData data =
                loadResult.Value;

            if (data.playerTeam == null)
            {
                return Result<SaveSlotSummary>.Failure(
                    $"Campaign slot {slotIndex} " +
                    "contains no player team data.");
            }

            string racerName =
                ResolveSummaryRacerName(
                    data);

            bool hasCareerRun =
                data.careerRun != null;

            bool careerActive =
                hasCareerRun &&
                data.careerRun.isActive;

            var summary =
                new SaveSlotSummary(
                    slotIndex,
                    true,
                    data.saveVersion,
                    data.lastSavedUtc,
                    data.playerTeam.teamName,
                    racerName,
                    data.playerTeam.credits,
                    data.playerTeam.fame,
                    data.playerTeam.researchPoints,
                    hasCareerRun,
                    careerActive);

            return Result<SaveSlotSummary>.Success(
                summary);
        }

        public Result<GameSessionState> CreateCampaign(
            int slotIndex,
            NewGameRequest request)
        {
            Result slotResult =
                ValidateSlot(
                    slotIndex);

            if (!slotResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    slotResult.ErrorMessage);
            }

            if (request == null)
            {
                return Result<GameSessionState>.Failure(
                    "New game request is required.");
            }

            if (sessions.HasSession)
            {
                return Result<GameSessionState>.Failure(
                    "A game session is already active.");
            }

            if (repository.Exists(
                    slotIndex))
            {
                return Result<GameSessionState>.Failure(
                    $"Campaign slot {slotIndex} already contains a save.");
            }

            Result<GameSessionState> createResult =
                sessions.CreateNewSession(
                    request);

            if (!createResult.IsSuccess)
            {
                return createResult;
            }

            Result<CampaignSaveData> captureResult =
                mapper.Capture(
                    createResult.Value);

            if (!captureResult.IsSuccess)
            {
                sessions.ClearSession();

                return Result<GameSessionState>.Failure(
                    captureResult.ErrorMessage);
            }

            Result saveResult =
                repository.Save(
                    slotIndex,
                    captureResult.Value);

            if (!saveResult.IsSuccess)
            {
                sessions.ClearSession();

                return Result<GameSessionState>.Failure(
                    saveResult.ErrorMessage);
            }

            ActiveSlotIndex =
                slotIndex;

            return Result<GameSessionState>.Success(
                createResult.Value);
        }

        public Result<GameSessionState> LoadCampaign(
            int slotIndex)
        {
            Result slotResult =
                ValidateSlot(
                    slotIndex);

            if (!slotResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    slotResult.ErrorMessage);
            }

            if (sessions.HasSession)
            {
                return Result<GameSessionState>.Failure(
                    "A game session is already active. " +
                    "Close it before loading another campaign.");
            }

            Result<CampaignSaveData> loadResult =
                repository.Load(
                    slotIndex);

            if (!loadResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    loadResult.ErrorMessage);
            }

            Result<GameSessionState> restoreResult =
                mapper.Restore(
                    loadResult.Value);

            if (!restoreResult.IsSuccess)
            {
                return restoreResult;
            }

            Result sessionResult =
                sessions.RestoreSession(
                    restoreResult.Value);

            if (!sessionResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    sessionResult.ErrorMessage);
            }

            ActiveSlotIndex =
                slotIndex;

            return Result<GameSessionState>.Success(
                restoreResult.Value);
        }

        public Result SaveCurrentCampaign()
        {
            if (!sessions.HasSession)
            {
                return Result.Failure(
                    "There is no active game session to save.");
            }

            if (!ActiveSlotIndex.HasValue)
            {
                return Result.Failure(
                    "The active game session is transient and is not " +
                    "associated with a campaign save slot.");
            }

            Result<CampaignSaveData> captureResult =
                mapper.Capture(
                    sessions.Current);

            if (!captureResult.IsSuccess)
            {
                return Result.Failure(
                    captureResult.ErrorMessage);
            }

            return repository.Save(
                ActiveSlotIndex.Value,
                captureResult.Value);
        }

        public Result CloseCurrentCampaign(
            bool saveChanges)
        {
            if (!sessions.HasSession)
            {
                ActiveSlotIndex = null;
                return Result.Success();
            }

            if (!ActiveSlotIndex.HasValue)
            {
                return Result.Failure(
                    "The active game session is transient and is not " +
                    "managed by the campaign save service.");
            }

            if (saveChanges)
            {
                Result saveResult =
                    SaveCurrentCampaign();

                if (!saveResult.IsSuccess)
                {
                    return saveResult;
                }
            }

            sessions.ClearSession();
            ActiveSlotIndex = null;

            return Result.Success();
        }

        public Result DeleteCampaign(
            int slotIndex)
        {
            Result slotResult =
                ValidateSlot(
                    slotIndex);

            if (!slotResult.IsSuccess)
            {
                return slotResult;
            }

            if (sessions.HasSession &&
                ActiveSlotIndex.HasValue &&
                ActiveSlotIndex.Value == slotIndex)
            {
                return Result.Failure(
                    "Cannot delete the currently loaded campaign. " +
                    "Close it first.");
            }

            Result deleteResult =
                repository.Delete(
                    slotIndex);

            if (deleteResult.IsSuccess &&
                ActiveSlotIndex.HasValue &&
                ActiveSlotIndex.Value == slotIndex)
            {
                ActiveSlotIndex = null;
            }

            return deleteResult;
        }

        private Result ValidateSlot(
            int slotIndex)
        {
            if (slotIndex < 1 ||
                slotIndex > repository.SlotCount)
            {
                return Result.Failure(
                    $"Campaign slot {slotIndex} is invalid. " +
                    $"Valid slots are 1 through {repository.SlotCount}.");
            }

            return Result.Success();
        }

        private string ResolveSummaryRacerName(
            CampaignSaveData data)
        {
            if (data.playerTeam?.racers == null ||
                data.playerTeam.racers.Count == 0)
            {
                return null;
            }

            string currentRacerId =
                data.careerRun?.playerRacerId;

            if (!string.IsNullOrWhiteSpace(
                    currentRacerId))
            {
                foreach (RacerSaveData racer
                         in data.playerTeam.racers)
                {
                    if (racer != null &&
                        racer.racerId == currentRacerId)
                    {
                        return racer.name;
                    }
                }
            }

            for (int i =
                     data.playerTeam.racers.Count - 1;
                 i >= 0;
                 i--)
            {
                RacerSaveData racer =
                    data.playerTeam.racers[i];

                if (racer != null &&
                    racer.isPlayerCharacter)
                {
                    return racer.name;
                }
            }

            return null;
        }
    }
}
