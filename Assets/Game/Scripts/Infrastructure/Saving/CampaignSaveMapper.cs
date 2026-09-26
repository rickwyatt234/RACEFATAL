using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Infrastructure.Saving
{
    public class CampaignSaveMapper
    {
        public const int SupportedSaveVersion = 1;

        private readonly GameDatabase database;

        public CampaignSaveMapper(
            GameDatabase database)
        {
            this.database =
                database
                ?? throw new ArgumentNullException(
                    nameof(database));
        }

        public Result<CampaignSaveData> Capture(
            GameSessionState session)
        {
            if (session == null)
            {
                return Result<CampaignSaveData>.Failure(
                    "Game session is required.");
            }

            var data =
                new CampaignSaveData
                {
                    saveVersion =
                        SupportedSaveVersion,

                    playerTeam =
                        CaptureTeam(
                            session.PlayerTeam),

                    careerRun =
                        session.CareerRun != null
                            ? CaptureCareerRun(
                                session.CareerRun)
                            : null,

                    world =
                        CaptureWorld(
                            session.World),

                    defaultPartnerRacerId =
                        session.DefaultPartnerRacerId,

                    defaultPlayerBikeId =
                        session.DefaultPlayerBikeId,

                    defaultPartnerBikeId =
                        session.DefaultPartnerBikeId,

                    selectedPlayerBikeId =
                        session.SelectedPlayerBikeId,

                    selectedPartnerBikeId =
                        session.SelectedPartnerBikeId,
                    selectedPartnerRacerId =
                        session.SelectedPartnerRacerId
                };

            return Result<CampaignSaveData>.Success(
                data);
        }

        public Result<GameSessionState> Restore(
            CampaignSaveData data)
        {
            if (data == null)
            {
                return Result<GameSessionState>.Failure(
                    "Campaign save data is required.");
            }

            if (data.saveVersion !=
                SupportedSaveVersion)
            {
                return Result<GameSessionState>.Failure(
                    $"Save version {data.saveVersion} is not supported. " +
                    $"Expected version {SupportedSaveVersion}.");
            }

            Result<TeamState> playerTeamResult =
                RestoreTeam(
                    data.playerTeam);

            if (!playerTeamResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    "Failed to restore player team: " +
                    playerTeamResult.ErrorMessage);
            }

            TeamState playerTeam =
                playerTeamResult.Value;

            Result<WorldState> worldResult =
                RestoreWorld(
                    data.world,
                    playerTeam.TeamId);

            if (!worldResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    "Failed to restore world: " +
                    worldResult.ErrorMessage);
            }

            CareerRun careerRun =
                null;

            if (data.careerRun != null)
            {
                Result<CareerRun> runResult =
                    RestoreCareerRun(
                        data.careerRun,
                        playerTeam);

                if (!runResult.IsSuccess)
                {
                    return Result<GameSessionState>.Failure(
                        "Failed to restore career run: " +
                        runResult.ErrorMessage);
                }

                careerRun =
                    runResult.Value;
            }

            // Existing v1 saves predate selected-bike assignments.
            string selectedPlayerBikeId =
                string.IsNullOrWhiteSpace(data.selectedPlayerBikeId)
                    ? data.defaultPlayerBikeId
                    : data.selectedPlayerBikeId;

            string selectedPartnerBikeId =
                string.IsNullOrWhiteSpace(data.selectedPartnerBikeId)
                    ? data.defaultPartnerBikeId
                    : data.selectedPartnerBikeId;

            string selectedPartnerRacerId = string.IsNullOrWhiteSpace(data.selectedPartnerRacerId)
                ? data.defaultPartnerRacerId : data.selectedPartnerRacerId;

            Result referenceResult =
                ValidateSessionReferences(
                    playerTeam,
                    data.defaultPartnerRacerId,
                    data.defaultPlayerBikeId,
                    data.defaultPartnerBikeId,
                    selectedPlayerBikeId,
                    selectedPartnerBikeId,
                    selectedPartnerRacerId);

            if (!referenceResult.IsSuccess)
            {
                return Result<GameSessionState>.Failure(
                    referenceResult.ErrorMessage);
            }

            var session =
                new GameSessionState(
                    playerTeam,
                    careerRun,
                    worldResult.Value,
                    data.defaultPartnerRacerId,
                    data.defaultPlayerBikeId,
                    data.defaultPartnerBikeId,
                    selectedPlayerBikeId,
                    selectedPartnerBikeId,
                    selectedPartnerRacerId);

            return Result<GameSessionState>.Success(
                session);
        }

        private TeamSaveData CaptureTeam(
            TeamState team)
        {
            var data =
                new TeamSaveData
                {
                    teamId =
                        team.TeamId,
                    calendar = team.Calendar.Export(),

                    teamName =
                        team.TeamName,

                    primaryColor =
                        team.PrimaryColor,

                    secondaryColor =
                        team.SecondaryColor,

                    credits =
                        team.Credits,

                    fame =
                        team.Fame,

                    researchPoints =
                        team.ResearchPoints,

                    garage =
                        CaptureGarage(
                            team.Garage)
                };

            foreach (var contract in team.ResearchContracts)
                data.researchContracts.Add(new ResearchContractSaveData {
                    definitionId = contract.DefinitionId, displayName = contract.DisplayName,
                    pointsPerRace = contract.PointsPerRace, racesRemaining = contract.RacesRemaining });
            foreach (var receipt in team.SettledRaceResults)
            {
                var result = receipt.Value;
                data.settledRaceResults.Add(new SettledRaceSaveData {
                    instanceId = receipt.Key, raceId = result.RaceId, playerRacerId = result.PlayerRacerId,
                    status = result.PlayerRaceStatus.ToString(), position = result.PlayerPosition,
                    credits = result.Reward.Credits, teamFame = result.Reward.TeamFame,
                    researchPoints = result.Reward.ResearchPoints, characterFame = result.Reward.CharacterFame,
                    eventRP = result.EventResearchPoints, researcherRP = result.ResearcherPoints,
                    playerDied = result.PlayerDied, careerEnded = result.CareerEnded });
            }
            foreach (string technologyId
                     in team.UnlockedTechnologyIds)
            {
                data.unlockedTechnologyIds.Add(
                    technologyId);
            }

            foreach (string championshipId
                     in team.UnlockedChampionshipIds)
            {
                data.unlockedChampionshipIds.Add(
                    championshipId);
            }

            foreach (string racerId
                     in team.EliminatedRacerIds)
            {
                data.eliminatedRacerIds.Add(
                    racerId);
            }

            foreach (RacerState racer
                     in team.Roster.Racers)
            {
                data.racers.Add(
                    CaptureRacer(
                        racer));
            }

            return data;
        }

        private RacerSaveData CaptureRacer(
            RacerState racer)
        {
            var data =
                new RacerSaveData
                {
                    racerId =
                        racer.RacerId,

                    name =
                        racer.Name,

                    teamId =
                        racer.TeamId,

                    isPlayerCharacter =
                        racer.IsPlayerCharacter,

                    status =
                        racer.Status.ToString(),

                    racesEntered =
                        racer.RacesEntered,

                    racesWon =
                        racer.RacesWon,

                    podiums =
                        racer.Podiums,

                    racersDestroyed =
                        racer.RacersDestroyed,

                    progression =
                        new CharacterProgressionSaveData
                        {
                            fame =
                                racer.Progression.Fame
                        }
                };

            foreach (string perkId
                     in racer.Progression.PurchasedPerkIds)
            {
                data.progression.purchasedPerkIds.Add(
                    perkId);
            }

            return data;
        }

        private CareerRunSaveData CaptureCareerRun(
            CareerRun run)
        {
            var data =
                new CareerRunSaveData
                {
                    runId =
                        run.RunId,

                    playerRacerId =
                        run.Player.RacerId,

                    isActive =
                        run.IsActive,

                    activeChampionshipId =
                        run.ActiveChampionshipId
                };

            Array engineClasses =
                Enum.GetValues(
                    typeof(EngineClass));

            foreach (EngineClass engineClass
                     in engineClasses)
            {
                IReadOnlyCollection<string> rivals =
                    run.GetRivalsForEngineClass(
                        engineClass);

                if (rivals.Count == 0)
                    continue;

                var rivalData =
                    new EngineClassRivalsSaveData
                    {
                        engineClass =
                            engineClass.ToString()
                    };

                foreach (string racerId
                         in rivals)
                {
                    rivalData.racerIds.Add(
                        racerId);
                }

                data.rivals.Add(
                    rivalData);
            }

            return data;
        }

        private WorldSaveData CaptureWorld(
            WorldState world)
        {
            var data =
                new WorldSaveData();

            foreach (TeamState team
                     in world.OpponentTeams)
            {
                data.opponentTeams.Add(
                    CaptureTeam(
                        team));
            }

            return data;
        }

        private GarageSaveData CaptureGarage(
            GarageState garage)
        {
            var data =
                new GarageSaveData();

            foreach (BikeState bike
                     in garage.Bikes)
            {
                var bikeData =
                    new BikeSaveData
                    {
                        bikeId =
                            bike.BikeId,

                        bikeDefinitionId =
                            bike.BikeDefinitionId,

                        primaryColor =
                            bike.PrimaryColor,

                        secondaryColor =
                            bike.SecondaryColor,

                        isDestroyed =
                            bike.IsDestroyed,

                        installedEngineId =
                            bike.Loadout.Engine?.EngineId,

                        installedChassisId =
                            bike.Loadout.Chassis?.ChassisId
                    };

                foreach (BikeNode node
                         in bike.Loadout.Nodes)
                {
                    if (!node.IsOccupied)
                        continue;

                    bikeData.installedEquipment.Add(
                        new BikeEquipmentPlacementSaveData
                        {
                            equipmentId =
                                node.InstalledEquipment.EquipmentId,

                            nodeSize =
                                node.NodeSize.ToString(),

                            nodeIndex =
                                node.Index
                        });
                }

                data.bikes.Add(
                    bikeData);
            }

            foreach (EngineState engine
                     in garage.Engines)
            {
                data.engines.Add(
                    new EngineSaveData
                    {
                        engineId =
                            engine.EngineId,

                        engineDefinitionId =
                            engine.EngineDefinitionId,

                        isDestroyed =
                            engine.IsDestroyed
                    });
            }

            foreach (ChassisState chassis
                     in garage.Chassis)
            {
                data.chassis.Add(
                    new ChassisSaveData
                    {
                        chassisId =
                            chassis.ChassisId,

                        chassisDefinitionId =
                            chassis.ChassisDefinitionId,

                        isDestroyed =
                            chassis.IsDestroyed
                    });
            }

            foreach (EquipmentState equipment
                     in garage.Equipment)
            {
                data.equipment.Add(
                    new EquipmentSaveData
                    {
                        equipmentId =
                            equipment.EquipmentId,

                        equipmentDefinitionId =
                            equipment.EquipmentDefinitionId,

                        isDestroyed =
                            equipment.IsDestroyed
                    });
            }

            return data;
        }

        private Result<TeamState> RestoreTeam(
            TeamSaveData data)
        {
            if (data == null)
            {
                return Result<TeamState>.Failure(
                    "Team save data is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    data.teamId))
            {
                return Result<TeamState>.Failure(
                    "Team ID is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    data.teamName))
            {
                return Result<TeamState>.Failure(
                    "Team name is required.");
            }

            if (data.credits < 0 ||
                data.fame < 0 ||
                data.researchPoints < 0)
            {
                return Result<TeamState>.Failure(
                    "Team currencies cannot be negative.");
            }

            var team =
                new TeamState(
                    data.teamId,
                    data.teamName,
                    data.primaryColor,
                    data.secondaryColor);

            team.AddCredits(
                data.credits);
            var calendarRestore = team.RestoreCalendar(data.calendar);
            if (!calendarRestore.IsSuccess) return Result<TeamState>.Failure(calendarRestore.ErrorMessage);

            team.AddFame(
                data.fame);

            team.AddResearchPoints(
                data.researchPoints);

            try
            {
                if (data.researchContracts != null)
                    foreach (var contract in data.researchContracts)
                    {
                        if (contract == null) return Result<TeamState>.Failure("Null researcher contract.");
                        team.RestoreResearchContract(new ResearchContract(contract.definitionId, contract.displayName,
                            contract.pointsPerRace, contract.racesRemaining));
                    }
                if (data.settledRaceResults != null)
                    foreach (var receipt in data.settledRaceResults)
                    {
                        if (receipt == null || !Enum.TryParse(receipt.status, out RaceFatal.Racing.RaceParticipantStatus status)
                            || (status != RaceFatal.Racing.RaceParticipantStatus.Finished
                                && status != RaceFatal.Racing.RaceParticipantStatus.Destroyed
                                && status != RaceFatal.Racing.RaceParticipantStatus.Retired)
                            || string.IsNullOrWhiteSpace(receipt.raceId) || string.IsNullOrWhiteSpace(receipt.playerRacerId)
                            || receipt.position < 1 || receipt.credits < 0 || receipt.teamFame < 0 || receipt.characterFame < 0
                            || receipt.researchPoints < 0 || receipt.eventRP < 0 || receipt.researcherRP < 0
                            || (long)receipt.eventRP + receipt.researcherRP > receipt.researchPoints)
                            return Result<TeamState>.Failure("Invalid research race receipt.");
                        team.RestoreSettledRace(receipt.instanceId, new PostRaceResult(receipt.raceId, receipt.playerRacerId,
                            receipt.position, status, new RaceReward(receipt.credits, receipt.teamFame, receipt.researchPoints,
                            receipt.characterFame), receipt.playerDied, receipt.careerEnded, receipt.eventRP, receipt.researcherRP));
                    }
                _ = team.ResearchOutputPerRace;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is OverflowException)
            {
                return Result<TeamState>.Failure("Invalid research save data: " + exception.Message);
            }
            if (data.unlockedTechnologyIds != null)
            {
                foreach (string technologyId
                         in data.unlockedTechnologyIds)
                {
                    if (string.IsNullOrWhiteSpace(
                            technologyId))
                    {
                        continue;
                    }

                    team.UnlockTechnology(
                        technologyId);
                }
            }

            if (data.unlockedChampionshipIds != null)
            {
                foreach (string championshipId
                         in data.unlockedChampionshipIds)
                {
                    if (string.IsNullOrWhiteSpace(
                            championshipId))
                    {
                        continue;
                    }

                    team.UnlockChampionship(
                        championshipId);
                }
            }

            if (data.racers != null)
            {
                foreach (RacerSaveData racerData
                         in data.racers)
                {
                    if (racerData == null)
                    {
                        return Result<TeamState>.Failure(
                            "Team roster contains null racer data.");
                    }

                    if (racerData.teamId !=
                        team.TeamId)
                    {
                        return Result<TeamState>.Failure(
                            $"Racer '{racerData.racerId}' " +
                            "does not belong to the saved team.");
                    }

                    Result<RacerState> racerResult =
                        RestoreRacer(
                            racerData);

                    if (!racerResult.IsSuccess)
                    {
                        return Result<TeamState>.Failure(
                            racerResult.ErrorMessage);
                    }

                    Result<RacerState> addResult =
                        team.Roster.AddRacer(
                            racerResult.Value);

                    if (!addResult.IsSuccess)
                    {
                        return Result<TeamState>.Failure(
                            addResult.ErrorMessage);
                    }
                }
            }

            Result garageResult =
                RestoreGarage(
                    team.Garage,
                    data.garage);

            if (!garageResult.IsSuccess)
            {
                return Result<TeamState>.Failure(
                    garageResult.ErrorMessage);
            }

            if (data.eliminatedRacerIds != null)
            {
                foreach (string racerId
                         in data.eliminatedRacerIds)
                {
                    if (string.IsNullOrWhiteSpace(
                            racerId))
                    {
                        continue;
                    }

                    team.PermanentlyEliminateRacer(
                        racerId);
                }
            }

            var activeEvent = team.Calendar.Active;
            if (activeEvent != null && (!activeEvent.standings.Exists(s => s.teamId == team.TeamId) ||
                (!string.IsNullOrEmpty(activeEvent.pendingInstanceId) && team.SettledRaceResults.ContainsKey(activeEvent.pendingInstanceId))))
                return Result<TeamState>.Failure("Active event has an invalid team or an already settled attempt.");
            return Result<TeamState>.Success(
                team);
        }

        private Result<RacerState> RestoreRacer(
            RacerSaveData data)
        {
            if (data == null)
            {
                return Result<RacerState>.Failure(
                    "Racer save data is required.");
            }

            if (!Enum.TryParse(
                    data.status,
                    true,
                    out RacerCareerStatus status))
            {
                return Result<RacerState>.Failure(
                    $"Racer '{data.racerId}' has invalid status " +
                    $"'{data.status}'.");
            }

            CharacterProgressionSaveData progression =
                data.progression
                ?? new CharacterProgressionSaveData();

            return RacerState.Restore(
                data.racerId,
                data.name,
                data.teamId,
                data.isPlayerCharacter,
                status,
                data.racesEntered,
                data.racesWon,
                data.podiums,
                data.racersDestroyed,
                progression.fame,
                progression.purchasedPerkIds);
        }

        private Result<CareerRun> RestoreCareerRun(
            CareerRunSaveData data,
            TeamState team)
        {
            if (data == null)
            {
                return Result<CareerRun>.Failure(
                    "Career run save data is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    data.playerRacerId))
            {
                return Result<CareerRun>.Failure(
                    "Career player racer ID is required.");
            }

            RacerState player =
                team.Roster.FindRacer(
                    data.playerRacerId);

            if (player == null)
            {
                return Result<CareerRun>.Failure(
                    $"Career player '{data.playerRacerId}' " +
                    "was not found in the player roster.");
            }

            Result<CareerRun> runResult =
                CareerRun.Restore(
                    data.runId,
                    team,
                    player,
                    data.isActive,
                    data.activeChampionshipId);

            if (!runResult.IsSuccess)
            {
                return runResult;
            }

            CareerRun run =
                runResult.Value;

            if (data.rivals != null)
            {
                foreach (EngineClassRivalsSaveData rivalData
                         in data.rivals)
                {
                    if (rivalData == null)
                    {
                        return Result<CareerRun>.Failure(
                            "Career rival data contains a null entry.");
                    }

                    if (!Enum.TryParse(
                            rivalData.engineClass,
                            true,
                            out EngineClass engineClass))
                    {
                        return Result<CareerRun>.Failure(
                            $"Invalid rival engine class " +
                            $"'{rivalData.engineClass}'.");
                    }

                    if (rivalData.racerIds == null)
                        continue;

                    foreach (string racerId
                             in rivalData.racerIds)
                    {
                        if (!run.AddRivalForEngineClass(
                                engineClass,
                                racerId))
                        {
                            return Result<CareerRun>.Failure(
                                $"Could not restore rival '{racerId}' " +
                                $"for engine class {engineClass}.");
                        }
                    }
                }
            }

            return Result<CareerRun>.Success(
                run);
        }

        private Result<WorldState> RestoreWorld(
            WorldSaveData data,
            string playerTeamId)
        {
            if (data == null)
            {
                return Result<WorldState>.Failure(
                    "World save data is required.");
            }

            var teams =
                new List<TeamState>();

            if (data.opponentTeams != null)
            {
                foreach (TeamSaveData teamData
                         in data.opponentTeams)
                {
                    Result<TeamState> teamResult =
                        RestoreTeam(
                            teamData);

                    if (!teamResult.IsSuccess)
                    {
                        return Result<WorldState>.Failure(
                            teamResult.ErrorMessage);
                    }

                    if (teamResult.Value.TeamId ==
                        playerTeamId)
                    {
                        return Result<WorldState>.Failure(
                            "Opponent world contains the player team ID.");
                    }

                    teams.Add(
                        teamResult.Value);
                }
            }

            return WorldState.Restore(
                teams);
        }

        private Result RestoreGarage(
            GarageState garage,
            GarageSaveData data)
        {
            if (garage == null)
            {
                return Result.Failure(
                    "Garage is required.");
            }

            if (data == null)
            {
                return Result.Failure(
                    "Garage save data is required.");
            }

            var bikes =
                new Dictionary<string, BikeState>();

            var engines =
                new Dictionary<string, EngineState>();

            var chassis =
                new Dictionary<string, ChassisState>();

            var equipment =
                new Dictionary<string, EquipmentState>();

            if (data.bikes != null)
            {
                foreach (BikeSaveData bikeData
                         in data.bikes)
                {
                    if (bikeData == null)
                    {
                        return Result.Failure(
                            "Garage contains null bike data.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            bikeData.bikeId))
                    {
                        return Result.Failure(
                            "Saved bike ID is required.");
                    }

                    if (bikes.ContainsKey(
                            bikeData.bikeId))
                    {
                        return Result.Failure(
                            $"Bike '{bikeData.bikeId}' appears more than once.");
                    }

                    BikeDefinition definition =
                        database.GetBikeDefinition(
                            bikeData.bikeDefinitionId);

                    if (definition == null)
                    {
                        return Result.Failure(
                            $"Bike definition '{bikeData.bikeDefinitionId}' " +
                            "was not found.");
                    }

                    var bike =
                        new BikeState(
                            bikeData.bikeId,
                            definition,
                            bikeData.primaryColor,
                            bikeData.secondaryColor);

                    Result<BikeState> addResult =
                        garage.AddBike(
                            bike);

                    if (!addResult.IsSuccess)
                    {
                        return Result.Failure(
                            addResult.ErrorMessage);
                    }

                    bikes.Add(
                        bike.BikeId,
                        bike);
                }
            }

            if (data.engines != null)
            {
                foreach (EngineSaveData engineData
                         in data.engines)
                {
                    if (engineData == null)
                    {
                        return Result.Failure(
                            "Garage contains null engine data.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            engineData.engineId))
                    {
                        return Result.Failure(
                            "Saved engine ID is required.");
                    }

                    if (engines.ContainsKey(
                            engineData.engineId))
                    {
                        return Result.Failure(
                            $"Engine '{engineData.engineId}' appears more than once.");
                    }

                    EngineDefinition definition =
                        database.GetEngineDefinition(
                            engineData.engineDefinitionId);

                    if (definition == null)
                    {
                        return Result.Failure(
                            $"Engine definition '{engineData.engineDefinitionId}' " +
                            "was not found.");
                    }

                    var engine =
                        new EngineState(
                            engineData.engineId,
                            definition.Id,
                            definition.EngineClass);

                    Result<EngineState> addResult =
                        garage.AddEngine(
                            engine);

                    if (!addResult.IsSuccess)
                    {
                        return Result.Failure(
                            addResult.ErrorMessage);
                    }

                    engines.Add(
                        engine.EngineId,
                        engine);
                }
            }

            if (data.chassis != null)
            {
                foreach (ChassisSaveData chassisData
                         in data.chassis)
                {
                    if (chassisData == null)
                    {
                        return Result.Failure(
                            "Garage contains null chassis data.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            chassisData.chassisId))
                    {
                        return Result.Failure(
                            "Saved chassis ID is required.");
                    }

                    if (chassis.ContainsKey(
                            chassisData.chassisId))
                    {
                        return Result.Failure(
                            $"Chassis '{chassisData.chassisId}' appears more than once.");
                    }

                    ChassisDefinition definition =
                        database.GetChassisDefinition(
                            chassisData.chassisDefinitionId);

                    if (definition == null)
                    {
                        return Result.Failure(
                            $"Chassis definition '{chassisData.chassisDefinitionId}' " +
                            "was not found.");
                    }

                    var chassisState =
                        new ChassisState(
                            chassisData.chassisId,
                            definition.Id);

                    Result<ChassisState> addResult =
                        garage.AddChassis(
                            chassisState);

                    if (!addResult.IsSuccess)
                    {
                        return Result.Failure(
                            addResult.ErrorMessage);
                    }

                    chassis.Add(
                        chassisState.ChassisId,
                        chassisState);
                }
            }

            if (data.equipment != null)
            {
                foreach (EquipmentSaveData equipmentData
                         in data.equipment)
                {
                    if (equipmentData == null)
                    {
                        return Result.Failure(
                            "Garage contains null equipment data.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            equipmentData.equipmentId))
                    {
                        return Result.Failure(
                            "Saved equipment ID is required.");
                    }

                    if (equipment.ContainsKey(
                            equipmentData.equipmentId))
                    {
                        return Result.Failure(
                            $"Equipment '{equipmentData.equipmentId}' " +
                            "appears more than once.");
                    }

                    EquipmentDefinition definition =
                        database.GetEquipmentDefinition(
                            equipmentData.equipmentDefinitionId);

                    if (definition == null)
                    {
                        return Result.Failure(
                            $"Equipment definition " +
                            $"'{equipmentData.equipmentDefinitionId}' " +
                            "was not found.");
                    }

                    var equipmentState =
                        new EquipmentState(
                            equipmentData.equipmentId,
                            definition.Id,
                            definition.Category,
                            definition.RequiredNodeSize);

                    Result<EquipmentState> addResult =
                        garage.AddEquipment(
                            equipmentState);

                    if (!addResult.IsSuccess)
                    {
                        return Result.Failure(
                            addResult.ErrorMessage);
                    }

                    equipment.Add(
                        equipmentState.EquipmentId,
                        equipmentState);
                }
            }

            if (data.bikes != null)
            {
                foreach (BikeSaveData bikeData
                         in data.bikes)
                {
                    if (!string.IsNullOrWhiteSpace(
                            bikeData.installedEngineId))
                    {
                        Result installResult =
                            garage.InstallEngine(
                                bikeData.bikeId,
                                bikeData.installedEngineId);

                        if (!installResult.IsSuccess)
                        {
                            return Result.Failure(
                                $"Could not restore engine on bike " +
                                $"'{bikeData.bikeId}': " +
                                installResult.ErrorMessage);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(
                            bikeData.installedChassisId))
                    {
                        Result installResult =
                            garage.InstallChassis(
                                bikeData.bikeId,
                                bikeData.installedChassisId);

                        if (!installResult.IsSuccess)
                        {
                            return Result.Failure(
                                $"Could not restore chassis on bike " +
                                $"'{bikeData.bikeId}': " +
                                installResult.ErrorMessage);
                        }
                    }

                    if (bikeData.installedEquipment == null)
                        continue;

                    foreach (BikeEquipmentPlacementSaveData placement
                             in bikeData.installedEquipment)
                    {
                        if (placement == null)
                        {
                            return Result.Failure(
                                $"Bike '{bikeData.bikeId}' contains " +
                                "null equipment placement data.");
                        }

                        if (!Enum.TryParse(
                                placement.nodeSize,
                                true,
                                out NodeSize nodeSize))
                        {
                            return Result.Failure(
                                $"Bike '{bikeData.bikeId}' has invalid " +
                                $"node size '{placement.nodeSize}'.");
                        }

                        Result<EquipmentState> installResult =
                            garage.InstallEquipment(
                                bikeData.bikeId,
                                placement.equipmentId,
                                nodeSize,
                                placement.nodeIndex);

                        if (!installResult.IsSuccess)
                        {
                            return Result.Failure(
                                $"Could not restore equipment on bike " +
                                $"'{bikeData.bikeId}': " +
                                installResult.ErrorMessage);
                        }
                    }
                }
            }

            if (data.bikes != null)
            {
                foreach (BikeSaveData bikeData
                         in data.bikes)
                {
                    if (bikeData.isDestroyed)
                    {
                        bikes[
                            bikeData.bikeId]
                            .Destroy();
                    }
                }
            }

            if (data.engines != null)
            {
                foreach (EngineSaveData engineData
                         in data.engines)
                {
                    if (engineData.isDestroyed)
                    {
                        engines[
                            engineData.engineId]
                            .Destroy();
                    }
                }
            }

            if (data.chassis != null)
            {
                foreach (ChassisSaveData chassisData
                         in data.chassis)
                {
                    if (chassisData.isDestroyed)
                    {
                        chassis[
                            chassisData.chassisId]
                            .Destroy();
                    }
                }
            }

            if (data.equipment != null)
            {
                foreach (EquipmentSaveData equipmentData
                         in data.equipment)
                {
                    if (equipmentData.isDestroyed)
                    {
                        equipment[
                            equipmentData.equipmentId]
                            .Destroy();
                    }
                }
            }

            return Result.Success();
        }

        private Result ValidateSessionReferences(
            TeamState team,
            string partnerRacerId,
            string playerBikeId,
            string partnerBikeId,
            string selectedPlayerBikeId,
            string selectedPartnerBikeId,
            string selectedPartnerRacerId)
        {
            if (!string.IsNullOrWhiteSpace(
                    partnerRacerId) &&
                team.Roster.FindRacer(
                    partnerRacerId) == null)
            {
                return Result.Failure(
                    $"Default partner racer '{partnerRacerId}' " +
                    "was not found in the player roster.");
            }

            if (!string.IsNullOrWhiteSpace(
                    playerBikeId) &&
                team.Garage.FindBike(
                    playerBikeId) == null)
            {
                return Result.Failure(
                    $"Default player bike '{playerBikeId}' " +
                    "was not found in the player garage.");
            }

            if (!string.IsNullOrWhiteSpace(
                    partnerBikeId) &&
                team.Garage.FindBike(
                    partnerBikeId) == null)
            {
                return Result.Failure(
                    $"Default partner bike '{partnerBikeId}' " +
                    "was not found in the player garage.");
            }

            if (!string.IsNullOrWhiteSpace(selectedPlayerBikeId) &&
                team.Garage.FindBike(selectedPlayerBikeId) == null)
                return Result.Failure(
                    "Selected player bike is missing from the saved garage.");

            if (!string.IsNullOrWhiteSpace(selectedPartnerBikeId) &&
                team.Garage.FindBike(selectedPartnerBikeId) == null)
                return Result.Failure(
                    "Selected partner bike is missing from the saved garage.");

            if (!string.IsNullOrWhiteSpace(selectedPartnerRacerId))
            {
                RacerState selectedPartner = team.Roster.FindRacer(selectedPartnerRacerId);
                if (selectedPartner == null || selectedPartner.IsPlayerCharacter || selectedPartner.TeamId != team.TeamId)
                    return Result.Failure("Selected partner racer is not a teammate.");
            }
            if (!string.IsNullOrWhiteSpace(selectedPlayerBikeId) &&
                selectedPlayerBikeId == selectedPartnerBikeId)
                return Result.Failure(
                    "Player and partner cannot use the same saved bike.");

            return Result.Success();
        }
    }
}
