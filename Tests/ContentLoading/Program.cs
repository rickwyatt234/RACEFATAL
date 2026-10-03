using System;
using System.Reflection;
using RaceFatal.Content;
using RaceFatal.Content.Racing;
using RaceFatal.Content.Tracks;
using RaceFatal.Racing;
using UnityEngine;

static class Program
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target,value);
    static void Main()
    {
        var track = new TrackDefinitionSO {name="Venue"}; Set(track,"id","venue");Set(track,"displayName","Venue");
        var valid = new RaceDefinitionSO {name="Valid Race"};Set(valid,"id","valid");Set(valid,"displayName","Valid");Set(valid,"track",track);
        var missing = new RaceDefinitionSO {name="Broken Track Race"};Set(missing,"id","missing");Set(missing,"displayName","Missing");
        Check(!missing.TryCreateRaceDefinition(out var absent, out string error) && absent == null && error.Contains("Broken Track Race") && error.Contains("Track"), "Missing track reports asset identity");
        bool namedException=false;
        try { missing.CreateRaceDefinition(); } catch (InvalidOperationException e) { namedException=e.Message.Contains("Broken Track Race"); }
        Check(namedException,"Direct creation raises actionable content error rather than null reference");
        Check(valid.TryCreateRaceDefinition(out var race,out error) && race.TrackId=="venue" && race.LapCount==3 && race.Deathmatch==null,"Valid race preserves authored settings");
        Set(valid,"deathmatch",true);Set(valid,"allowedWinners",1);
        Check(valid.CreateRaceDefinition().Deathmatch.Mode==DeathmatchVictoryMode.Team,"Deathmatch settings preserved");
        Set(valid,"lapCount",0);
        Check(!valid.TryCreateRaceDefinition(out absent,out error) && error.Contains("lap"),"Invalid rule reports content error");
        Set(valid,"lapCount",3);
        var catalog = new GameContentCatalogSO {name="Test Catalog"};
        catalog.TrackDefinitions.Add(null);catalog.TrackDefinitions.Add(track);
        catalog.RaceDefinitions.Add(null);catalog.RaceDefinitions.Add(missing);catalog.RaceDefinitions.Add(valid);
        var database=GameDatabaseFactory.CreateGameDatabase(catalog);
        Check(database.GetRaceDefinition("valid")!=null && database.GetRaceDefinition("missing")==null,"Broken race does not stop valid catalog loading");
        Check(Debug.Errors.Count==1 && Debug.Errors[0].Contains("Broken Track Race"),"Factory logs asset-specific error");
        Check(Debug.Warnings.Count==2,"Stale race and track entries warn without crashing");
        var noTrackCatalog=new GameContentCatalogSO {name="Unregistered Venue"};noTrackCatalog.RaceDefinitions.Add(valid);
        Check(GameDatabaseFactory.CreateGameDatabase(noTrackCatalog).RaceDefinitions.Count==0 && Debug.Errors[1].Contains("Track Definitions"),"Unregistered track excluded with repair instruction");
        Console.WriteLine($"PASS: {checks} content-loading checks (Unity content doubles)");
    }
}
