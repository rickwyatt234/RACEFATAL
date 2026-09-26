using System;
using System.Collections.Generic;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

namespace RaceFatal.Racing
{
    public class RacePreparationService
    {
        private readonly GameDatabase database;

        private readonly GameSessionManager
            sessionManager;

        private readonly RaceEntryBuilder
            raceEntryBuilder;

        public RacePreparationService(
            GameDatabase database,
            GameSessionManager sessionManager,
            RaceEntryBuilder raceEntryBuilder)
        {
            this.database =
                database ??
                throw new ArgumentNullException(
                    nameof(database));

            this.sessionManager =
                sessionManager ??
                throw new ArgumentNullException(
                    nameof(sessionManager));

            this.raceEntryBuilder =
                raceEntryBuilder ??
                throw new ArgumentNullException(
                    nameof(raceEntryBuilder));
        }

        public Result<RaceDirector>
            PrepareDefaultRace(
                string raceId)
        {
            if (!sessionManager.HasSession)
            {
                return Result<RaceDirector>.Failure(
                    "No active game session exists.");
            }

            GameSessionState session =
                sessionManager.Current;

            return PrepareRace(
                raceId,
                session.DefaultPlayerBikeId,
                session.DefaultPartnerRacerId,
                session.DefaultPartnerBikeId);
        }

        // Career entries use the player-configured bike assignments.
        // Prototype launch continues to use PrepareDefaultRace.
        public Result<RaceDirector> PrepareSelectedRace(string raceId)
        {
            if (!sessionManager.HasSession)
                return Result<RaceDirector>.Failure(
                    "No active game session exists.");

            GameSessionState session = sessionManager.Current;
            var active = session.PlayerTeam.Calendar.Active;
            if (active != null && active.raceIds[active.roundIndex] != raceId)
                return Result<RaceDirector>.Failure("Finish or withdraw from the active calendar event first.");
            return PrepareRace(
                raceId,
                session.SelectedPlayerBikeId,
                session.SelectedPartnerRacerId,
                session.SelectedPartnerBikeId,
                active?.standings.Select(s => s.teamId).Where(id => id != session.PlayerTeam.TeamId).ToList());
        }

        public Result<RaceDirector>
            PrepareRace(
                string raceId,
                string playerBikeId,
                string partnerRacerId,
                string partnerBikeId,
                IReadOnlyList<string> calendarTeamIds = null)
        {
            if (!sessionManager.HasSession)
            {
                return Result<RaceDirector>.Failure(
                    "No active game session exists.");
            }

            GameSessionState session =
                sessionManager.Current;

            RaceDefinition race =
                database.GetRaceDefinition(
                    raceId);

            if (race == null)
            {
                return Result<RaceDirector>.Failure(
                    $"Race '{raceId}' was not found.");
            }

            BikeState playerBike =
                session.PlayerTeam.Garage.FindBike(
                    playerBikeId);

            if (playerBike == null)
            {
                return Result<RaceDirector>.Failure(
                    $"Player bike '{playerBikeId}' " +
                    "was not found.");
            }

            RacerState partner =
                session.PlayerTeam.Roster.FindRacer(
                    partnerRacerId);

            if (partner == null)
            {
                return Result<RaceDirector>.Failure(
                    $"Partner racer '{partnerRacerId}' " +
                    "was not found.");
            }

            BikeState partnerBike =
                session.PlayerTeam.Garage.FindBike(
                    partnerBikeId);

            if (partnerBike == null)
            {
                return Result<RaceDirector>.Failure(
                    $"Partner bike '{partnerBikeId}' " +
                    "was not found.");
            }

            if (calendarTeamIds != null)
            {
                // Championship field stays fixed. Teams unable to field two active racers and
                // compatible bikes miss the round; their accumulated standings remain intact.
                var eligible = session.World.OpponentTeams.Where(t => calendarTeamIds.Contains(t.TeamId) &&
                    t.Roster.GetRaceEligibleRacers().Count >= race.TeamSize &&
                    t.Garage.GetRaceReadyBikesFor(race.EngineClass).Count >= race.TeamSize).ToList();
                var round = new RaceDefinition(race.Id, race.DisplayName, race.TrackId, race.EngineClass,
                    race.LapCount, (eligible.Count + 1) * race.TeamSize, race.TeamSize, race.ResearchPointBonus);
                return raceEntryBuilder.Build(round, session.CareerRun, playerBike, partner, partnerBike, eligible);
            }

            Result<List<TeamState>>
                opponentsResult =
                    SelectOpponentTeams(
                        race,
                        session.World);

            if (!opponentsResult.IsSuccess)
            {
                return Result<RaceDirector>.Failure(
                    opponentsResult.ErrorMessage);
            }

            return raceEntryBuilder.Build(
                race,
                session.CareerRun,
                playerBike,
                partner,
                partnerBike,
                opponentsResult.Value);
        }

        private Result<List<TeamState>>
            SelectOpponentTeams(
                RaceDefinition race,
                WorldState world)
        {
            if (race.TeamSize <= 0)
            {
                return Result<List<TeamState>>.Failure(
                    "Race TeamSize must be greater than zero.");
            }

            if (race.EntrantCount %
                race.TeamSize != 0)
            {
                return Result<List<TeamState>>.Failure(
                    "Race entrant count must divide evenly " +
                    "into the configured team size.");
            }

            int totalTeams =
                race.EntrantCount /
                race.TeamSize;

            int requiredOpponents =
                totalTeams - 1;

            var selected =
                new List<TeamState>(
                    requiredOpponents);
            if (requiredOpponents == 0) return Result<List<TeamState>>.Success(selected);

            foreach (TeamState team
                     in world.OpponentTeams)
            {
                IReadOnlyList<RacerState>
                    racers =
                        team.Roster
                            .GetRaceEligibleRacers();

                if (racers.Count <
                    race.TeamSize)
                {
                    continue;
                }

                IReadOnlyList<BikeState>
                    bikes =
                        team.Garage
                            .GetRaceReadyBikesFor(
                                race.EngineClass);

                if (bikes.Count <
                    race.TeamSize)
                {
                    continue;
                }

                selected.Add(
                    team);

                if (selected.Count >=
                    requiredOpponents)
                {
                    break;
                }
            }

            if (selected.Count <
                requiredOpponents)
            {
                return Result<List<TeamState>>.Failure(
                    $"Race requires {requiredOpponents} " +
                    $"opponent teams, but only " +
                    $"{selected.Count} eligible teams exist.");
            }

            return Result<List<TeamState>>.Success(
                selected);
        }
    }
}
