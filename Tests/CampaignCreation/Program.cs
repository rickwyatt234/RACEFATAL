using System;
using System.Collections.Generic;
using RaceFatal.Career;
using RaceFatal.Data;
using RaceFatal.Equipment;
using RaceFatal.Vehicles;
using RaceFatal.Shared;
using RaceFatal.Infrastructure.Saving;
using RaceFatal.Presentation.FrontEnd;

static class Program
{
    static int checks;
    static void Check(bool condition, string message) { checks++; if(!condition) throw new Exception(message); }
    static T Ok<T>(Result<T> r) { if(!r.IsSuccess) throw new Exception(r.ErrorMessage); return r.Value; }
    static void Main()
    {
        var db=new GameDatabase();
        db.AddBikeDefinition(new BikeDefinition("bike","Starter",1,0,0));
        db.AddEngineDefinition(new EngineDefinition("engine","Engine",default,100,10,0,null));
        db.AddChassisDefinition(new ChassisDefinition("chassis","Chassis",350,1,0,null));
        db.AddEquipmentDefinition(new ShieldDefinition("shield","Shield",NodeSize.Small,50,10,1,0,null));
        db.AddBikeBuildDefinition(new BikeBuildDefinition("build","Build","bike","engine","chassis",new[]{new EquipmentMountDefinition("shield",NodeSize.Small,0)}));
        db.AddRacerDefinition(new RacerDefinition("partner","Partner",.5f,.5f,.5f,.5f,.5f));
        db.AddRacerPerkDefinition(new RacerPerkDefinition("perk1","Perk One","Test",0,RacerPerkEffect.EnergyCapacity,.1f));
        db.AddRacerPerkDefinition(new RacerPerkDefinition("perk2","Perk Two","Test",0,RacerPerkEffect.EnergyCapacity,.2f));
        var builds=new BikeBuildFactory(db,new VehicleFactory(),new EquipmentFactory());
        var career=new CareerManager(new CharacterFactory());
        var sessions=new GameSessionManager(db,career,new WorldFactory(db,builds),builds);
        var repository=new MemoryRepository();
        var saves=new CampaignSaveService(sessions,new CampaignSaveMapper(db),repository);
        var request=new NewGameRequest("Team","#FF1122","#33AABB","Player","partner","build","build");
        var preview=Ok(sessions.PrepareNewSession(request));
        Check(!sessions.HasSession && !career.HasTeam && !repository.Exists(1),"Preview must not activate or save campaign");
        string perk=preview.CareerRun.StartingPerkId;
        string bike=preview.SelectedPlayerBikeId;
        Check(perk!=null && preview.PlayerTeam.Garage.FindBike(bike).Loadout.Nodes[0].IsOccupied,"Preview contains actual perk and equipment");
        var draft=new NewCampaignDraft(1); draft.SetTeam("Team","#FF1122","#33AABB"); draft.SetPlayer("Player"); draft.PreparedSession=preview;
        draft.SetPlayer("Player"); draft.SetTeam("Team","#FF1122","#33AABB");
        Check(ReferenceEquals(draft.PreparedSession,preview),"Back/forward without edits must retain the roll");
        draft.SetPlayer("Changed"); Check(draft.PreparedSession==null,"Identity edits invalidate the prepared session");
        Check(!saves.CommitPreparedCampaign(0,preview).IsSuccess && !sessions.HasSession,"Invalid slot must not activate preview");
        repository.Fail=true;
        Check(!saves.CommitPreparedCampaign(1,preview).IsSuccess,"Write failure reported");
        Check(!sessions.HasSession && !career.HasTeam && !saves.ActiveSlotIndex.HasValue,"Write failure must clear active state");
        repository.Fail=false;
        var saved=Ok(saves.CommitPreparedCampaign(1,preview));
        Check(saved.CareerRun.StartingPerkId==perk && saved.SelectedPlayerBikeId==bike,"Confirmation must retain preview identities and roll");
        Check(!saved.CareerRun.NeedsIntroduction,"Reviewed campaign must not show home introduction popup");
        Check(!saves.CommitPreparedCampaign(1,preview).IsSuccess,"Cannot overwrite active slot");
        Check(saves.CloseCurrentCampaign(false).IsSuccess,"Close created campaign");
        var loaded=Ok(saves.LoadCampaign(1));
        Check(loaded.CareerRun.StartingPerkId==perk && loaded.SelectedPlayerBikeId==bike,"Roundtrip preserves reviewed starter setup");
        Check(!loaded.CareerRun.NeedsIntroduction && loaded.PlayerTeam.PrimaryColor=="#FF1122","Roundtrip preserves review acknowledgement and paint");
        Check(saves.CloseCurrentCampaign(false).IsSuccess,"Close loaded campaign");
        Check(!saves.CommitPreparedCampaign(1,preview).IsSuccess && !sessions.HasSession,"Occupied slot cannot be overwritten");
        var direct=Ok(sessions.CreateNewSession(request));
        Check(ReferenceEquals(sessions.Current,direct) && direct.CareerRun.NeedsIntroduction,"Existing direct session path retains behavior");
        Console.WriteLine($"PASS: {checks} campaign creation checks");
    }
    sealed class MemoryRepository : ICampaignSaveRepository
    {
        readonly Dictionary<int,CampaignSaveData> data=new();
        public bool Fail; public int SlotCount=>3;
        public bool Exists(int slot)=>data.ContainsKey(slot);
        public Result Save(int slot,CampaignSaveData value) { if(Fail)return Result.Failure("Simulated disk failure");data[slot]=value;return Result.Success(); }
        public Result<CampaignSaveData> Load(int slot)=>data.TryGetValue(slot,out var value)?Result<CampaignSaveData>.Success(value):Result<CampaignSaveData>.Failure("Missing");
        public Result Delete(int slot) {data.Remove(slot);return Result.Success();}
    }
}
