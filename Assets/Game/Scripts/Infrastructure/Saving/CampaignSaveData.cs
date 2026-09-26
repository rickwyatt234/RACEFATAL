using System;
using System.Collections.Generic;

namespace RaceFatal.Infrastructure.Saving
{
    [Serializable]
    public class CampaignSaveData
    {
        public int saveVersion = 1;
        public string lastSavedUtc;

        public TeamSaveData playerTeam;
        public CareerRunSaveData careerRun;
        public WorldSaveData world;

        public string defaultPartnerRacerId;
        public string defaultPlayerBikeId;
        public string defaultPartnerBikeId;

        // Optional v1 additions; absent in older saves, so use defaults on load.
        public string selectedPlayerBikeId;
        public string selectedPartnerBikeId;
        public string selectedPartnerRacerId;
        public string successorStarterBuildId;
    }

    [Serializable]
    public class TeamSaveData
    {
        public RaceFatal.Career.CareerCalendarData calendar;
        public string teamId;
        public string teamName;

        public string primaryColor;
        public string secondaryColor;

        public int credits;
        public int fame;
        public int researchPoints;
        public string recoverySupportCheckpoint;

        public List<ResearchContractSaveData> researchContracts = new List<ResearchContractSaveData>();
        public List<SettledRaceSaveData> settledRaceResults = new List<SettledRaceSaveData>();
        public List<string> unlockedTechnologyIds =
            new List<string>();

        public List<string> unlockedChampionshipIds =
            new List<string>();

        public List<string> eliminatedRacerIds =
            new List<string>();

        public List<RacerSaveData> racers =
            new List<RacerSaveData>();

        public GarageSaveData garage =
            new GarageSaveData();
    }

    [Serializable]
    public class ResearchContractSaveData
    {
        public string definitionId, displayName;
        public int pointsPerRace, racesRemaining;
    }
    [Serializable]
    public class SettledRaceSaveData
    {
        public string instanceId, raceId, playerRacerId, status;
        public int position, credits, teamFame, researchPoints, characterFame, eventRP, researcherRP;
        public bool playerDied, careerEnded;
    }
    [Serializable]
    public class RacerSaveData
    {
        public string racerId;
        public string definitionId;
        public string name;
        public string teamId;

        public bool isPlayerCharacter;

        public string status;

        public int racesEntered;
        public int racesWon;
        public int podiums;
        public int racersDestroyed;

        public CharacterProgressionSaveData progression =
            new CharacterProgressionSaveData();
    }

    [Serializable]
    public class CharacterProgressionSaveData
    {
        public int fame;

        public List<string> purchasedPerkIds =
            new List<string>();
    }

    [Serializable]
    public class CareerRunSaveData
    {
        // JsonUtility can materialize a null inline run as a default object.
        // A partially populated run remains present and must pass validation.
        public static bool IsAbsent(CareerRunSaveData data) => data == null || (
            string.IsNullOrEmpty(data.runId) && string.IsNullOrEmpty(data.playerRacerId) &&
            !data.isActive && string.IsNullOrEmpty(data.activeChampionshipId) && !data.needsIntroduction &&
            string.IsNullOrEmpty(data.startingBikeSource) && string.IsNullOrEmpty(data.startingPerkId) &&
            (data.rivals == null || data.rivals.Count == 0));

        public string runId;
        public string playerRacerId;

        public bool isActive;

        public string activeChampionshipId;
        public bool needsIntroduction;
        public string startingBikeSource, startingPerkId;

        public List<EngineClassRivalsSaveData> rivals =
            new List<EngineClassRivalsSaveData>();
    }

    [Serializable]
    public class EngineClassRivalsSaveData
    {
        public string engineClass;

        public List<string> racerIds =
            new List<string>();
    }

    [Serializable]
    public class WorldSaveData
    {
        public List<TeamSaveData> opponentTeams =
            new List<TeamSaveData>();
    }

    [Serializable]
    public class GarageSaveData
    {
        public List<BikeSaveData> bikes =
            new List<BikeSaveData>();

        public List<EngineSaveData> engines =
            new List<EngineSaveData>();

        public List<ChassisSaveData> chassis =
            new List<ChassisSaveData>();

        public List<EquipmentSaveData> equipment =
            new List<EquipmentSaveData>();
    }

    [Serializable]
    public class BikeSaveData
    {
        public string bikeId;
        public string bikeDefinitionId;

        public string primaryColor;
        public string secondaryColor;

        public bool isDestroyed;

        public string installedEngineId;
        public string installedChassisId;

        public List<BikeEquipmentPlacementSaveData>
            installedEquipment =
                new List<BikeEquipmentPlacementSaveData>();
    }

    [Serializable]
    public class BikeEquipmentPlacementSaveData
    {
        public string equipmentId;

        public string nodeSize;
        public int nodeIndex;
    }

    [Serializable]
    public class EngineSaveData
    {
        public string engineId;
        public string engineDefinitionId;

        public bool isDestroyed;
    }

    [Serializable]
    public class ChassisSaveData
    {
        public string chassisId;
        public string chassisDefinitionId;

        public bool isDestroyed;
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public string equipmentId;
        public string equipmentDefinitionId;

        public bool isDestroyed;
    }
}
