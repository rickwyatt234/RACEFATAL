/*
    OWNED CURRENTLY ACTIVE CAREER RUN
    DOES NOT OWN PERSISTENT SAVE DATA
    STARTS NEW RUNS, EITHER BY CREATING NEW TEAM IF SLOT IS EMPTY OR BY
    CREATING A NEW PLAYER CHARACTER FOR AN EXISTING TEAM.
*/

using System;

namespace RaceFatal.Career
{
    public class CareerManager
    {
        private readonly CharacterFactory characterFactory;
        private readonly PostRaceResolutionService postRaceResolutionService;

        public TeamState Team { get; private set; }
        public CareerRun CurrentRun { get; private set; }

        public bool HasTeam => Team != null;

        public bool HasActiveRun =>
            CurrentRun != null &&
            CurrentRun.IsActive;

        public event Action<CareerRun> CurrentRunChanged;

        public CareerManager(
            CharacterFactory characterFactory,
            PostRaceResolutionService postRaceResolutionService = null)
        {
            this.characterFactory =
                characterFactory
                ?? throw new ArgumentNullException(
                    nameof(characterFactory));

            this.postRaceResolutionService =
                postRaceResolutionService
                ?? new PostRaceResolutionService(
                    new RaceRewardPolicy());
        }

        public void LoadTeam(
            TeamState team)
        {
            if (HasTeam)
            {
                throw new InvalidOperationException(
                    "Cannot load team when one is already loaded.");
            }

            Team = team
                ?? throw new ArgumentNullException(
                    nameof(team));
        }

        public CareerRun StartNewRun(
            string playerName)
        {
            if (Team == null)
            {
                throw new InvalidOperationException(
                    "Cannot start a new run without a team.");
            }

            if (HasActiveRun)
            {
                throw new InvalidOperationException(
                    "Cannot start a new run when one is already active.");
            }

            RacerState player =
                characterFactory.CreateNewPlayerCharacter(
                    Team.TeamId,
                    playerName);

            Team.Roster.AddRacer(
                player);

            CurrentRun =
                new CareerRun(
                    Guid.NewGuid().ToString("N"),
                    Team,
                    player);

            CurrentRunChanged?.Invoke(
                CurrentRun);

            return CurrentRun;
        }

        public PostRaceResult ResolvePostRace(
            Racing.RaceResult raceResult,
            RacerState player)
        {
            if (Team == null)
            {
                throw new InvalidOperationException(
                    "Cannot resolve a race without a loaded team.");
            }

            return postRaceResolutionService.Resolve(
                raceResult,
                Team,
                player);
        }

        public void KillCurrentRun()
        {
            if (CurrentRun == null)
                return;

            CurrentRun.Kill();

            CurrentRun = null;

            CurrentRunChanged?.Invoke(
                null);
        }

        public void RetireCurrentRun()
        {
            if (CurrentRun == null)
                return;

            CurrentRun.Retire();

            CurrentRun = null;

            CurrentRunChanged?.Invoke(
                null);
        }

        public void InitializeNewGame(
            TeamState team,
            CareerRun run)
        {
            if (team == null)
            {
                throw new ArgumentNullException(
                    nameof(team));
            }

            if (run == null)
            {
                throw new ArgumentNullException(
                    nameof(run));
            }

            Team = team;
            CurrentRun = run;

            CurrentRunChanged?.Invoke(
                CurrentRun);
        }
    }
}