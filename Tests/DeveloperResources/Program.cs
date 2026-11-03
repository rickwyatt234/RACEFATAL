using System;
using RaceFatal.Career;
class Program
{
    static void Main()
    {
        var team = new TeamState("test", "Test", "#fff", "#000");
        DeveloperResourceGrant.Apply(team,10000,1000);
        if(team.Credits!=10000||team.Fame!=1000||team.LifetimeFame!=1000)throw new Exception("Grant mismatch");
        team.TrySpendFame(150);
        DeveloperResourceGrant.Apply(team,0,150);
        if(team.Fame!=1000||team.LifetimeFame!=1150)throw new Exception("Spent fame history lost");
        foreach(var pair in new[]{(-1,100),(100,-1),(0,0),(int.MaxValue,1),(1,int.MaxValue)})
        {
            int credits=team.Credits,fame=team.Fame,lifetime=team.LifetimeFame;
            bool rejected=false;
            try{DeveloperResourceGrant.Apply(team,pair.Item1,pair.Item2);}
            catch(Exception e)when(e is ArgumentException||e is OverflowException){rejected=true;}
            if(!rejected||team.Credits!=credits||team.Fame!=fame||team.LifetimeFame!=lifetime)throw new Exception("Invalid grant changed balances");
        }
        // Lifetime overflow can occur before available-fame overflow after spending.
        team.RestoreFame(0,int.MaxValue);
        try{DeveloperResourceGrant.Apply(team,1,1);throw new Exception("Expected lifetime overflow");}
        catch(OverflowException){}
        if(team.Credits!=10000||team.Fame!=0||team.LifetimeFame!=int.MaxValue)throw new Exception("Partial grant on lifetime overflow");
        Console.WriteLine("PASS: resource grants, lifetime fame, negative/empty rejection and atomic overflow checks");
    }
}
