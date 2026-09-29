using System;
using System.Linq;

namespace RaceFatal.Career
{
    public class CareerManager
    {
        private readonly CharacterFactory characterFactory;
        private readonly PostRaceResolutionService postRaceResolutionService;
        public TeamState Team { get; private set; }
        public CareerRun CurrentRun { get; private set; }
        public bool HasTeam => Team != null;
        public bool HasActiveRun => CurrentRun != null && CurrentRun.IsActive;

        public event Action<CareerRun> CurrentRunChanged;
        public CareerManager(CharacterFactory characterFactory, PostRaceResolutionService postRaceResolutionService = null)
        {
            this.characterFactory = characterFactory ?? throw new ArgumentNullException(nameof(characterFactory));
            this.postRaceResolutionService = postRaceResolutionService ?? new PostRaceResolutionService(new RaceRewardPolicy());
        }

        public void LoadTeam(TeamState team)
        {
            if (HasTeam)
            {
                throw new InvalidOperationException("Cannot load team when one is already loaded.");
            }

            Team = team ?? throw new ArgumentNullException(nameof(team));
        }

        public CareerRun StartNewRun(string playerName)
        {
            if (Team == null)
            {
                throw new InvalidOperationException("Cannot start a new run without a team.");
            }

            if (HasActiveRun)
            {
                throw new InvalidOperationException("Cannot start a new run when one is already active.");
            }

            if (string.IsNullOrWhiteSpace(playerName) || playerName.Trim().Length > 40 || playerName.Any(char.IsControl))
                throw new ArgumentException("Enter a racer name of 1–40 characters.", nameof(playerName));
            if (Team.Calendar.Active != null)
                throw new InvalidOperationException("Resolve or withdraw from the previous event before starting a new racer.");
            foreach (var former in Team.Roster.Racers.Where(r => r.IsPlayerCharacter && r.CanRace))
                former.Retire();
            RacerState player = characterFactory.CreateNewPlayerCharacter(Team.TeamId, playerName.Trim());
            Team.Roster.AddRacer(player);
            CurrentRun = new CareerRun(Guid.NewGuid().ToString("N"), Team, player);
            CurrentRunChanged?.Invoke(CurrentRun);
            return CurrentRun;
        }

        public PostRaceResult ResolvePostRace(Racing.RaceResult raceResult, RacerState player)
        {
            if (Team == null)
            {
                throw new InvalidOperationException("Cannot resolve a race without a loaded team.");
            }

            var result = postRaceResolutionService.Resolve(raceResult, Team, player);
            if (Team.Calendar.Active == null)
                CurrentRun?.ExitChampionship();
            return result;
        }

        public void KillCurrentRun()
        {
            if (!HasActiveRun)
                return;
            CurrentRun.Kill();
            CurrentRunChanged?.Invoke(CurrentRun);
        }

        public void RetireCurrentRun()
        {
            if (!HasActiveRun)
                return;
            var calendar = Team.Calendar.Data;
            if (!string.IsNullOrEmpty(calendar.active?.pendingInstanceId))
                throw new InvalidOperationException("Finish or withdraw from the paid race in Races before retiring.");
            if (calendar.active != null)
            {
                if (calendar.week >= int.MaxValue - 1)
                    throw new InvalidOperationException("Calendar week limit reached.");
                calendar.active.withdrawn = true;
                calendar.active.completed = true;
                calendar.lastEvent = calendar.active;
                calendar.active = null;
                calendar.week++;
                calendar.drawWeek = 0;
                calendar.draw.Clear();
            }

            CurrentRun.ExitChampionship();
            CurrentRun.Retire();
            CurrentRunChanged?.Invoke(CurrentRun);
        }

        public void InitializeNewGame(TeamState team, CareerRun run)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            RestoreState(team, run);
        }

        public void RestoreState(TeamState team, CareerRun run)
        {
            if (team == null)
            {
                throw new ArgumentNullException(nameof(team));
            }

            if (run != null && run.Team.TeamId != team.TeamId)
            {
                throw new InvalidOperationException("Career run does not belong to the supplied team.");
            }

            Team = team;
            CurrentRun = run;
            CurrentRunChanged?.Invoke(CurrentRun);
        }

        public void Clear()
        {
            Team = null;
            CurrentRun = null;
            CurrentRunChanged?.Invoke(null);
        }
    }
}
