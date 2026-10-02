using System;
using System.Linq;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Racing;
using RaceFatal.Shared;
using RaceFatal.Vehicles;

static class Program
{
    static int checks;
    static void Check(bool value, string message) { checks++; if(!value) throw new Exception(message); }
    static T Ok<T>(Result<T> result) { if(!result.IsSuccess)throw new Exception(result.ErrorMessage);return result.Value; }
    static void Main()
    {
        var db=new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike","Bike",0,0,0));
        db.AddEngineDefinition(new EngineDefinition("engine","Engine",default,100,10,0,null));
        db.AddChassisDefinition(new ChassisDefinition("chassis","Chassis",250,1,0,null));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("build","Build","bike","engine","chassis",Array.Empty<EquipmentMountDefinition>()));
        db.AddRacerDefinition(new RacerDefinition("partner","Partner",.5f,.5f,.5f,.5f,.5f));
        db.AddRaceDefinition(new RaceDefinition("race-a","Venue A race","venue-a",default,3,2,2,7));
        db.AddRaceDefinition(new RaceDefinition("race-b","Venue B race","venue-b",default,4,2,2,11));
        CareerEventDefinition Event(string id,string race,int day,int repeat,int payout=100,int fame=0,CareerEventKind kind=CareerEventKind.Race) => new CareerEventDefinition(id,id,"Scheduled event",kind,fame,0,new[]{race},new[]{payout},Array.Empty<int>(),new[]{25,18},1,day,repeat);
        db.AddCareerEventDefinition(Event("day4","race-a",4,4,100));
        db.AddCareerEventDefinition(Event("day6","race-b",6,6,250));
        db.AddCareerEventDefinition(Event("day2","race-b",2,5));
        db.AddCareerEventDefinition(Event("locked","race-a",5,4,100,100000));
        db.AddCareerEventDefinition(Event("other","race-b",7,5,170,0,CareerEventKind.Other));
        db.AddCareerEventDefinition(new CareerEventDefinition("champ","Champ","",CareerEventKind.Championship,0,0,new[]{"race-a","race-b"},new[]{100},new[]{1000},new[]{25,18},2,2,5,3));
        var career=new CareerManager(new CharacterFactory());
        var builds=new BikeBuildFactory(db,new VehicleFactory(),new EquipmentFactory());
        var sessions=new GameSessionManager(db,career,new WorldFactory(db,builds),builds);
        var session=Ok(sessions.CreateNewSession(new NewGameRequest("Team","#fff","#000","Player","partner","build","build")));
        var team=session.PlayerTeam;
        var service=new CareerCalendarService(db);
        var mapper=new CampaignSaveMapper(db);
        var factory=new RaceFactory(new RaceGridValidator(new RaceEligibilityService()));
        var participants=new RaceParticipantFactory(db,new BikePerformanceCalculator(db));
        var prep=new RacePreparationService(db,sessions,new RaceEntryBuilder(participants,factory,career));
        var resolver=new PostRaceResolutionService(new RaceRewardPolicy());
        RaceDirector Prepare(string eventId) => Ok(prep.PrepareSelectedRace(service.NextRaceId(team,eventId)));
        RaceResult ResultFor(RaceDirector director) => new RaceResult(director.State.RaceDefinition.Id,new[]{new RaceResultEntry(session.CareerRun.Player.RacerId,team.TeamId,1,3,RaceParticipantStatus.Finished),new RaceResultEntry(session.SelectedPartnerRacerId,team.TeamId,2,3,RaceParticipantStatus.Finished)},director.InstanceId,director.State.RaceDefinition.ResearchPointBonus);
        void Finish(RaceDirector director) => resolver.Resolve(ResultFor(director),team,session.CareerRun.Player);
        Check(team.Calendar.AbsoluteDay==1 && team.Calendar.Schedules.Count==6,"New game captures entire event bank including locked events");
        var dates=service.GetOccurrences(team,1,85);
        Check(dates.Where(e=>e.EventId=="day4").Select(e=>e.Day).SequenceEqual(new[]{4,32,60}),"Four-week repeat dates");
        Check(dates.Where(e=>e.EventId=="day6").Select(e=>e.Day).SequenceEqual(new[]{6,48}),"Six-week repeat dates");
        Check(team.Calendar.AbsoluteDay==1,"Browsing calendar must not advance date");
        Check(service.CanEnter(team,"day4",4).IsSuccess,"Can select day 4 from new save");
        Check(!service.CanEnter(team,"day4",5).IsSuccess,"Reject unscheduled dates");
        Check(!service.CanEnter(team,"locked",5).IsSuccess,"Locked future entries remain locked");
        var first=Prepare("day4");
        Check(!service.Register(session,"day6",first,6).IsSuccess && team.Calendar.AbsoluteDay==1,"Wrong event race cannot advance date");
        var transaction=Ok(service.Register(session,"day4",first,4));
        Check(team.Calendar.Week==1 && team.Calendar.DayOfWeek==4,"Entry jumps to exact date");
        transaction.Rollback();transaction.Rollback();
        Check(team.Calendar.AbsoluteDay==1 && !team.Calendar.IsConsumed("day4",4),"Failed launch rollback restores time and occurrence");
        Ok(service.Register(session,"day4",first,4));
        var reload=Ok(mapper.Restore(UnityEngine.JsonUtility.FromJson<CampaignSaveData>(UnityEngine.JsonUtility.ToJson(Ok(mapper.Capture(session))))));
        Check(reload.PlayerTeam.Calendar.DayOfWeek==4 && reload.PlayerTeam.Calendar.Active.pendingInstanceId==first.InstanceId,"Interrupted entry survives roundtrip");
        int credits=team.Credits;
        Finish(first);
        Check(team.Credits==credits+100 && team.Calendar.AbsoluteDay==4,"Own payout and finish stays on event day");
        resolver.Resolve(ResultFor(first),team,session.CareerRun.Player);
        Check(team.Credits==credits+100 && team.Calendar.AbsoluteDay==4,"Result replay cannot pay or advance date");
        Check(!service.CanEnter(team,"day4",4).IsSuccess && !service.CanEnter(team,"day2",2).IsSuccess,"Consumed and past occurrences are blocked");
        Check(service.NextOccurrenceDay(team,"day4")==32,"Next repeat remains available");
        var second=Prepare("day6");Ok(service.Register(session,"day6",second,6));Finish(second);
        Check(team.Calendar.Week==1 && team.Calendar.DayOfWeek==6 && team.Credits==credits+350,"Two distinct venues/rewards in the same week");
        Check(service.NextRaceId(team,"day4")=="race-a" && service.NextRaceId(team,"day6")=="race-b","Events keep their own race");
        var other=Prepare("other");Ok(service.Register(session,"other",other,7));Finish(other);
        Check(team.Calendar.AbsoluteDay==7 && team.Credits==credits+520,"Other category supports an authored single race");
        var champ=Prepare("champ");Ok(service.Register(session,"champ",champ,9));Finish(champ);
        Check(team.Calendar.AbsoluteDay==9 && team.Calendar.Active.scheduledDay==12,"Next championship round is dated but does not auto-advance time");
        Check(!service.CanEnter(team,"day4",32).IsSuccess,"Active championship prevents switching events");
        Check(service.GetOccurrences(team,9,20).Any(e=>e.IsActiveRound && e.Day==12 && e.RoundIndex==1),"Calendar shows active round symbol");
        var next=Prepare("champ");Ok(service.Register(session,"champ",next,12));Finish(next);
        Check(team.Calendar.AbsoluteDay==12 && team.Calendar.Active==null && team.Calendar.LastEvent.finalPrize==1000,"Championship completes at last round date with final prize");
        var before=Ok(mapper.Capture(session));
        var restored=Ok(mapper.Restore(UnityEngine.JsonUtility.FromJson<CampaignSaveData>(UnityEngine.JsonUtility.ToJson(before))));
        Check(restored.PlayerTeam.Calendar.AbsoluteDay==12 && restored.PlayerTeam.Calendar.IsConsumed("day4",4),"Dates and consumed occurrences survive JSON roundtrip");
        Check(string.Join(",",service.GetOccurrences(restored.PlayerTeam,1,85).Select(e=>e.EventId+e.Day))==string.Join(",",service.GetOccurrences(team,1,85).Select(e=>e.EventId+e.Day)),"Reload preserves whole calendar");
        var changedDb=new GameDatabase(); changedDb.AddRaceDefinition(db.GetRaceDefinition("race-a"));changedDb.AddCareerEventDefinition(Event("day4","race-a",1,6));
        new CareerCalendarService(changedDb).Refresh(team);
        Check(new CareerCalendarService(changedDb).NextOccurrenceDay(team,"day4")==32,"Editing authored timing cannot reshuffle saved schedules");
        var snapshot=team.Calendar.Export(); snapshot.dayOfWeek=8;Check(!team.RestoreCalendar(snapshot).IsSuccess,"Invalid day rejected");
        snapshot=team.Calendar.Export();snapshot.schedules[0].repeatEveryWeeks=0;Check(!team.RestoreCalendar(snapshot).IsSuccess,"Invalid recurrence rejected");
        var legacy=new CareerCalendarData {week=8,dayOfWeek=0};var migrated=Ok(CareerCalendarState.Restore(legacy));
        Check(migrated.Week==8 && migrated.DayOfWeek==1,"Legacy week migrates without resetting progress");
        Console.WriteLine($"PASS: {checks} dated-calendar checks");
        CareerCalendarValidation.Run();
        DeathmatchValidation.Run();
        CareerSuccessionValidation.Run();
    }
}
