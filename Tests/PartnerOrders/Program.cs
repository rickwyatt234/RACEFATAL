using System;
using RaceFatal.Career;
using RaceFatal.Energy;
using RaceFatal.Equipment;
using RaceFatal.Presentation.Racing;
using RaceFatal.Presentation.Tactics;
using RaceFatal.Presentation.Tracks;
using RaceFatal.Racing;
using RaceFatal.Vehicles;
using UnityEngine;
class Program
{
    static int checks;
    static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
    static RacerViewController Racer(string id,string team,RaceParticipantRole role,float z)
    {
        var bike=new BikeState(id,new BikeDefinition("bike","Bike",0,0,0),"white","black");
        var energy=new EnergyPool(100);var equipment=new RaceEquipmentSystem(id,energy);
        var p=new RaceParticipant(new RacerState(id,id,team,role==RaceParticipantRole.Player),new RaceVehicleState(bike,new BikePerformance(100,10,1,100),energy,equipment),role);
        p.Start();p.SpeedKph=180;
        return new RacerViewController{Participant=p,transform=new Transform{position=new Vector3(0,0,z)}};
    }
    static void Main()
    {
        var self=Racer("partner","team",RaceParticipantRole.PlayerPartner,93);
        var player=Racer("player","team",RaceParticipantRole.Player,100);
        var enemy=Racer("enemy","other",RaceParticipantRole.Opponent,120);
        var orders=new PartnerOrderController();orders.Initialize(self,player,new TrackProgressPath());
        Check(orders.Order==PartnerOrder.FormUp&&!orders.AllowAttack&&!orders.AllowBoost,"passive formation default");
        Check(Math.Abs(orders.LimitSpeed(.093f,100)-50)<.01,"formation matches player speed at slot");
        orders.SetFormationSide(-1);orders.TryGetLane(.093f,new Vector3(0,1,0),5,out float lane);Check(lane<0,"formation left");
        orders.Issue(PartnerOrder.Tail,enemy);Check(!orders.AllowAttack,"tail holds fire");
        orders.Issue(PartnerOrder.Attack,enemy);Check(orders.AllowAttack&&orders.CombatTargetId==enemy.RacerId,"attack has exact target");
        orders.SetConservation(true);Check(!orders.AllowAttack&&!orders.AllowBoost,"conservation suppresses optional spending");
        orders.Issue(PartnerOrder.PressureRight,enemy);orders.TryGetLane(.093f,new Vector3(0,1,0),1,out lane);Check(lane==1,"pressure lane respects track width");
        enemy.Participant.Vehicle.ApplyDamage(100);orders.UpdateIntent();Check(orders.Order==PartnerOrder.FormUp&&orders.Target==null,"destroyed target returns to formation");
        var another=Racer("rival","other",RaceParticipantRole.Opponent,120);orders.Issue(PartnerOrder.Attack,another);
        self.Participant.Vehicle.ApplyDamage(90);orders.UpdateIntent();Check(!orders.AllowAttack&&orders.Acknowledgment.Contains("critical"),"survival overrides order");
        Console.WriteLine($"PASS: {checks} partner order checks with presentation doubles.");
    }
}
