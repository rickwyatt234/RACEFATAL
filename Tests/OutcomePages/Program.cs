using System;
using System.Reflection;
using RaceFatal.Career;
using RaceFatal.Presentation.Racing;
using RaceFatal.Racing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

static class Program
{
    static void Main()
    {
        var controller=new RaceOutcomeController();
        var standings=new RaceStandingsView();var payout=new GameObject();var button=new Button();var label=new TextMeshProUGUI();
        var director=new RaceDirector();
        void Set(string field,object value)=>typeof(RaceOutcomeController).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(controller,value);
        void Call(string method,params object[] args)=>typeof(RaceOutcomeController).GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controller,args);
        void Check(bool value,string message){if(!value)throw new Exception(message);}
        Set("standingsView",standings);Set("payoutPanel",payout);Set("continueButton",button);Set("continueButtonText",label);Set("outcomePanel",new GameObject());Set("outcomeTitle",new TextMeshProUGUI());Set("outcomeSubtitle",new TextMeshProUGUI());Set("director",director);Set("postRaceSceneName","Career");
        Call("Awake");Call("ShowLiveOutcome","FINISHED","POSITION 1");
        Check(standings.gameObject.activeSelf&&!payout.activeSelf,"Live standings must not show payout");
        Call("OnPostRaceResolved",new PostRaceResult());
        Check(!payout.activeSelf,"Early reward callback must not overlay live standings");
        Call("ShowFinalResults",new RaceResult());
        Check(standings.gameObject.activeSelf&&!payout.activeSelf&&label.text=="VIEW PAYOUT","Final classification must be a separate page");
        Call("OnPostRaceResolved",new PostRaceResult());
        Check(!payout.activeSelf,"Reward callback must not overlay final standings");
        controller.ConfirmOutcome();
        Check(!standings.gameObject.activeSelf&&payout.activeSelf&&label.text=="RETURN TO CAREER","Continue must replace standings with payout");
        Call("OnRaceCompleted",new RaceResult());Call("OnPostRaceResolved",new PostRaceResult());
        Check(!standings.gameObject.activeSelf&&payout.activeSelf,"Late callbacks must not restore standings over payout");
        director.PostRaceResult=new PostRaceResult();controller.ConfirmOutcome();
        Check(UnityEngine.SceneManagement.SceneManager.Loaded=="Career","Continue from payout returns to career");
        Console.WriteLine("PASS: 7 outcome-page transition checks (presentation doubles)");
    }
}
