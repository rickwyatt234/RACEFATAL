using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
static class Program
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Main()
    {
        var database=new GameDatabase();
        RacerPerkCatalog.Ensure(database);
        Check(database.RacerPerkDefinitions.Count==30,"Exactly 30 approved Player Perks");
        Check(RacerPerkCatalog.Definitions.Select(p=>p.Id).Distinct().Count()==30,"Unique Player Perk IDs");
        Check(RacerPerkCatalog.IsStarter("DRV-01") && !RacerPerkCatalog.IsStarter("CBT-05"),"Starting-perk eligibility");
        Check(TeamPerkCatalog.All.Count==28,"Exactly 28 permanent manufacturer Team Perks");
        Check(TeamPerkCatalog.All.Select(p=>p.Id).Distinct().Count()==28,"Team Perk uniqueness");
        Check(TeamPerkCatalog.All.GroupBy(p=>p.Manufacturer).Count()==14,"14 manufacturers");
        Check(TeamPerkCatalog.All.GroupBy(p=>p.Manufacturer).All(g=>g.Count()==2),"Two Team Perks per manufacturer");
        Check(ApprovedCareerBlueprint.Teams.Count==21,"21 rival teams");
        Check(ApprovedCareerBlueprint.Tracks.Count==7,"Seven forward/reverse tracks");
        Check(ApprovedCareerBlueprint.Utilities.Count==19,"19 utility blueprints");
        Check(ApprovedCareerBlueprint.Exclusives.Count==8,"Eight exclusive variants");
        Check(CareerIncidentCatalog.All.Count==50,"50 placeholder career incidents");
        var team=new TeamState("test","Test","#fff","#000");
        Check(team.GrantTeamPerk("TEAM-01"),"Grant a Team Perk");
        Check(!team.GrantTeamPerk("TEAM-01"),"No duplicate Team Perks");
        Check(Math.Abs(TeamPerkCatalog.Bonus(team,"PurchaseDiscount","Pilion")-.02f)<.001f,"Correct brand-specific bonus");
        Check(TeamPerkCatalog.Bonus(team,"PurchaseDiscount","Pace Industries")==0,"Wrong manufacturer does not benefit");
        team.RestoreTeamPerks(new[]{"TEAM-01","TEAM-01"});
        Check(team.OwnedTeamPerkIds.Count==1,"Perk reload deduplicates");
        Check(CareerIncidentService.Queue(team,"EVENT_001"),"Queue debug incident");
        Check(!CareerIncidentService.Queue(team,"EVENT_002"),"Cannot replace an unresolved incident");
        var outcome=CareerIncidentService.Resolve(team,0);
        Check(outcome!=null && team.Credits==outcome.CreditDelta && team.PendingIncidentId==null,"Resolve once");
        Check(CareerIncidentService.Resolve(team,0)==null,"Incident cannot resolve twice");
        Console.WriteLine("PASS: approved content catalogs and perk/incident state");
    }
}
