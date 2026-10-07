using RaceFatal.Career;
using RaceFatal.Content.Career;
using RaceFatal.Content.Racing;
using RaceFatal.Content.Vehicles;
using RaceFatal.Infrastructure;
using RaceFatal.Presentation.Bootstrap;
using RaceFatal.Racing;
using RaceFatal.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RaceFatal.Presentation.Racing
{
    public class PrototypeRaceLauncher : MonoBehaviour
    {
        [Header("Prototype Player")] [SerializeField] private string teamName = "Prototype Team";
        [SerializeField] private string playerName = "Player";
        [SerializeField] private string primaryColor = "#FFFFFF";
        [SerializeField] private string secondaryColor = "#202020";
        [Header("Starter Content")] [SerializeField] private RacerDefinitionSO partner;
        [SerializeField] private BikeBuildDefinitionSO playerStarterBuild;
        [SerializeField] private BikeBuildDefinitionSO partnerStarterBuild;
        [Header("Race")] [SerializeField] private RaceDefinitionSO race;
        [SerializeField] private string raceSceneName = "10_Race";
        [Header("Launch")] [SerializeField] private bool launchOnStart = true;

        public BikeBuildDefinitionSO DefaultPlayerBuild => playerStarterBuild;

        private void Start()
        {
            RaceStartupTrace.Mark("PrototypeRaceLauncher.Start() " + $"LaunchOnStart={launchOnStart}", this);
            if (!launchOnStart)
            {
                RaceStartupTrace.Warning("Automatic prototype race " + "launch is disabled.", this);
                return;
            }

            Launch();
        }

        [ContextMenu("Launch Prototype Race")]
        public void Launch()
        {
            LaunchInternal(playerStarterBuild, false);
        }

        public void LaunchPublisherRace(BikeBuildDefinitionSO selectedPlayerBuild)
        {
            LaunchInternal(selectedPlayerBuild != null ? selectedPlayerBuild : playerStarterBuild, true);
        }

        private void LaunchInternal(BikeBuildDefinitionSO selectedPlayerBuild, bool recreateSession)
        {
            RaceStartupTrace.Mark(recreateSession ? "Publisher race launch requested." : "Prototype race launch requested.", this);
            GameContext context = BootstrapController.Context;
            if (context == null)
            {
                RaceStartupTrace.Fail("BootstrapController.Context is null.", this);
                return;
            }

            RaceStartupTrace.Mark("GameContext found.");
            if (partner == null)
            {
                RaceStartupTrace.Fail("Prototype partner is not assigned.", this);
                return;
            }

            if (selectedPlayerBuild == null)
            {
                RaceStartupTrace.Fail("Player starter build is not assigned.", this);
                return;
            }

            if (partnerStarterBuild == null)
            {
                RaceStartupTrace.Fail("Partner starter build is not assigned.", this);
                return;
            }

            if (race == null)
            {
                RaceStartupTrace.Fail("Prototype race is not assigned.", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(partner.Id))
            {
                RaceStartupTrace.Fail("Prototype partner has no ID.", partner);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedPlayerBuild.Id))
            {
                RaceStartupTrace.Fail("Player starter bike build " + "has no ID.", selectedPlayerBuild);
                return;
            }

            if (string.IsNullOrWhiteSpace(partnerStarterBuild.Id))
            {
                RaceStartupTrace.Fail("Partner starter bike build " + "has no ID.", partnerStarterBuild);
                return;
            }

            if (string.IsNullOrWhiteSpace(race.Id))
            {
                RaceStartupTrace.Fail("Prototype race has no ID.", race);
                return;
            }

            RaceStartupTrace.Mark($"Prototype content validated. " + $"Race='{race.Id}'.");
            if (recreateSession && context.Sessions.HasSession)
            {
                context.RaceLaunch.Clear();
                context.Sessions.ClearSession();
                RaceStartupTrace.Mark("Publisher session reset for selected loadout.");
            }

            if (!context.Sessions.HasSession)
            {
                RaceStartupTrace.Mark("Creating new game session...");
                var request = new NewGameRequest(teamName, primaryColor, secondaryColor, playerName, partner.Id, selectedPlayerBuild.Id, partnerStarterBuild.Id);
                Result<GameSessionState> sessionResult = context.Sessions.CreateNewSession(request);
                if (!sessionResult.IsSuccess)
                {
                    RaceStartupTrace.Fail("GameSessionManager." + "CreateNewSession() failed: " + sessionResult.ErrorMessage, this);
                    return;
                }

                GameSessionState session = sessionResult.Value;
                RaceStartupTrace.Mark("GameSessionManager." + "CreateNewSession() succeeded.");
                RaceStartupTrace.Mark($"Player team created: " + $"'{session.PlayerTeam.TeamName}'.");
                RaceStartupTrace.Mark($"Player roster contains " + $"{session.PlayerTeam.Roster.Racers.Count} " + "racers.");
                RaceStartupTrace.Mark($"Opponent WorldState created with " + $"{session.World.OpponentTeams.Count} " + "teams.");
                for (int i = 0; i < session.World.OpponentTeams.Count; i++)
                {
                    TeamState team = session.World.OpponentTeams[i];
                    RaceStartupTrace.Mark($"Opponent Team {i + 1}: " + $"'{team.TeamName}', " + $"{team.Roster.Racers.Count} racers.");
                }
            }
            else
            {
                RaceStartupTrace.Mark("Existing GameSession detected. " + "New-session creation skipped.");
            }

            RaceStartupTrace.Mark($"Preparing race '{race.Id}'...");
            Result<RaceDirector> raceResult = context.RacePreparation.PrepareDefaultRace(race.Id);
            if (!raceResult.IsSuccess)
            {
                RaceStartupTrace.Fail("RacePreparationService failed: " + raceResult.ErrorMessage, this);
                return;
            }

            RaceDirector director = raceResult.Value;
            RaceStartupTrace.Mark("RacePreparationService succeeded.");
            RaceStartupTrace.Mark($"RaceDirector created with " + $"{director.State.Participants.Count} " + "participants.");
            if (director.State.Participants.Count != 12)
            {
                RaceStartupTrace.Warning($"Expected 12 participants but " + $"received " + $"{director.State.Participants.Count}.");
            }

            context.RaceLaunch.SetPendingRace(director);
            RaceStartupTrace.Mark("RaceDirector stored in " + "RaceLaunchContext.");
            if (!context.RaceLaunch.HasPendingRace)
            {
                RaceStartupTrace.Fail("RaceLaunchContext did not " + "retain the prepared race.", this);
                return;
            }

            RaceStartupTrace.Mark($"Checking whether scene " + $"'{raceSceneName}' can be loaded...");
            if (!Application.CanStreamedLevelBeLoaded(raceSceneName))
            {
                RaceStartupTrace.Fail($"Scene '{raceSceneName}' " + "cannot be loaded. Verify its exact " + "name and that it is included in " + "Build Profiles / Build Settings.", this);
                return;
            }

            RaceStartupTrace.Mark($"Loading scene " + $"'{raceSceneName}'...");
            SceneManager.LoadScene(raceSceneName);
        }
    }
}
