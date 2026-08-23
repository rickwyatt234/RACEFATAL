/*
    LIFETIME OF ONE PLAYER CHARACTER'S CAREER. ENDS WHEN PLAYER QUITS OR DIES.
*/
using System;
using System.Collections.Generic;
using RaceFatal.Shared;

namespace RaceFatal.Career
{
    public class CareerRun
    {
        private readonly Dictionary<EngineClass, List<string>> rivalsByEngineClass = new Dictionary<EngineClass, List<string>>();
        public string RunId { get; }
        public TeamState Team { get; }
        public RacerState Player { get; }
        public bool IsActive { get; private set; } = true;
        public string ActiveChampionshipId { get; private set; }

        public CareerRun(
            string runId,
            TeamState team,
            RacerState player)
        {
            RunId = runId;
            Team = team;
            Player = player;
        }

        public IReadOnlyCollection<string> GetRivalsForEngineClass(EngineClass engineClass)
        {
            if (!rivalsByEngineClass.TryGetValue(engineClass, out List<string> rivals))
            {
                return Array.Empty<string>();
            }
            return rivals;
        }

        public bool AddRivalForEngineClass(EngineClass engineClass, string rivalId)
        {
            if (!rivalsByEngineClass.TryGetValue(engineClass, out List<string> rivals))
            {
                rivals = new List<string>();
                rivalsByEngineClass[engineClass] = rivals;
            }
            if (rivals.Contains(rivalId))
            {
                return false;
            }
            if (rivals.Count >= 2)
            {
                return false;
            }

            rivals.Add(rivalId);
            return true;
        }

        public void EnterChampionship(string championshipId)
        {
            ActiveChampionshipId = championshipId;
        }
        public void ExitChampionship()
        {
            ActiveChampionshipId = null;
        }

        public void Kill()
        {
            Player.Kill();
            Team.PermanentlyEliminateRacer(Player.RacerId);
            IsActive = false;
        }
        public void Retire()
        {
            //Player.Retire();
            IsActive = false;
        }
    }
}

