using System;
using System.Collections.Generic;
using System.Linq;

namespace RaceFatal.Career
{
    public enum CareerIncidentCategory { Corporate, Financial, Media, Mechanical, Personnel, Rivalry, Extraordinary }
    public enum CareerIncidentOutcome { CreditGain, CreditLoss, FameGain, NoEffect }

    public sealed class CareerIncidentDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public CareerIncidentCategory Category { get; }
        public IReadOnlyList<string> Choices { get; }
        public CareerIncidentDefinition(int index, CareerIncidentCategory category)
        {
            Id="EVENT_"+index.ToString("D3");
            Title="Event "+index;
            Description="Placeholder career encounter "+index+". Replace with authored narrative.";
            Category=category;
            Choices=new[]{"Placeholder Choice A","Placeholder Choice B","Placeholder Choice C"};
        }
    }

    /// <summary>50 numbered, inspector-ready debug events. No fictional narrative content is shipped.</summary>
    public static class CareerIncidentCatalog
    {
        public static readonly IReadOnlyList<CareerIncidentDefinition> All = Enumerable.Range(1,50)
            .Select(i=>new CareerIncidentDefinition(i, i<=8?CareerIncidentCategory.Corporate:
                i<=16?CareerIncidentCategory.Financial:i<=23?CareerIncidentCategory.Media:
                i<=30?CareerIncidentCategory.Mechanical:i<=37?CareerIncidentCategory.Personnel:
                i<=44?CareerIncidentCategory.Rivalry:CareerIncidentCategory.Extraordinary)).ToArray();
        public static CareerIncidentDefinition Find(string id) => All.FirstOrDefault(d=>d.Id==id);
    }

    public sealed class CareerIncidentResult
    {
        public string IncidentId { get; }
        public int ChoiceIndex { get; }
        public CareerIncidentOutcome Outcome { get; }
        public int CreditDelta { get; }
        public int FameDelta { get; }
        public CareerIncidentResult(string id,int choice,CareerIncidentOutcome outcome,int creditDelta,int fameDelta)
        { IncidentId=id; ChoiceIndex=choice; Outcome=outcome; CreditDelta=creditDelta; FameDelta=fameDelta; }
    }

    /// <summary>Side-effect-free choice lookup with exactly-once state settlement at TeamState.</summary>
    public static class CareerIncidentService
    {
        public static bool Queue(TeamState team,string incidentId)
        {
            if(team==null || team.PendingIncidentId!=null || CareerIncidentCatalog.Find(incidentId)==null)return false;
            return team.QueueIncident(incidentId);
        }
        public static void ConsiderAfterRace(TeamState team, string instanceId)
        {
            if (team.PendingIncidentId != null || team.Calendar.Active != null) return;
            uint roll = Roll(instanceId, team.Calendar.Data.seed);
            if (roll % 7 == 0) Queue(team, CareerIncidentCatalog.All[(int)((roll / 7) % (uint)CareerIncidentCatalog.All.Count)].Id);
        }

        private static uint Roll(string key, int seed)
        {
            uint value = unchecked((uint)seed + 2166136261u);
            foreach (char c in key) value = unchecked((value ^ c) * 16777619u);
            return value;
        }

        public static CareerIncidentResult Resolve(TeamState team,int choiceIndex)
        {
            if(team==null || team.PendingIncidentId==null || choiceIndex<0 || choiceIndex>2)return null;
            string id=team.PendingIncidentId;
            uint roll = Roll(id + "/" + choiceIndex, team.Calendar.Data.seed ^ team.SettledRaceResults.Count);
            int chance = (int)(roll % 100);
            int credits = choiceIndex == 0 ? (chance < 55 ? 1000 : -500)
                : choiceIndex == 1 ? (chance < 75 ? 400 : -150) : 0;
            int fame = choiceIndex == 2 && chance < 60 ? 25 : 0;
            credits = Math.Max(-team.Credits, Math.Min(int.MaxValue - team.Credits, credits));
            fame = Math.Min(int.MaxValue - team.LifetimeFame, fame);
            if (credits > 0) team.AddCredits(credits);
            else if (credits < 0) team.TrySpendCredits(-credits);
            if (fame > 0) team.AddFame(fame);
            var outcome = credits > 0 ? CareerIncidentOutcome.CreditGain : credits < 0 ? CareerIncidentOutcome.CreditLoss :
                fame > 0 ? CareerIncidentOutcome.FameGain : CareerIncidentOutcome.NoEffect;
            var result = new CareerIncidentResult(id, choiceIndex, outcome, credits, fame);
            team.ResolveIncident(id);
            return result;
        }
    }
}
