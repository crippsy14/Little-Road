using System;
using System.Collections.Generic;
using LittleRoad;

public static class RaceTimingChecks
{
    static void Require(bool value,string reason){if(!value)throw new Exception("Race timing: "+reason);}
    static void Seconds(RaceTiming.Gap gap,float expected){Require(gap.Kind==RaceTiming.GapKind.Seconds&&Math.Abs(gap.Value-expected)<.001f,"incorrect common-position passage gap: "+gap.Value+" expected "+expected);}
    public static List<string> Run()
    {
        var checks=new List<string>();var t=new RaceTiming(100,new[]{0,1});
        t.Observe(0,0,-10,false,0,0);t.Observe(1,0,-20,false,0,0);
        Require(t.RelativeTo(0,1).Kind==RaceTiming.GapKind.Waiting,"invented a grid time gap");
        t.Observe(0,2,30,false,0,0);t.Observe(1,2,20,false,0,0);Seconds(t.RelativeTo(0,1),-.5f);Seconds(t.RelativeTo(1,0),.5f);
        t.Observe(0,4,70,false,0,0);t.Observe(1,4,20,false,0,0);Seconds(t.RelativeTo(0,1),-2.5f);
        checks.Add("Recorded common-position gaps have correct sign and grow when a trailing car stops, without distance/current-speed division");

        var stopped=new RaceTiming(100,new[]{0,1});stopped.Observe(0,0,0,false,0,0);stopped.Observe(1,0,-5,false,0,0);
        stopped.Observe(0,1,10,false,0,0);stopped.Observe(0,20,10,false,0,0);stopped.Observe(1,20,9,false,0,0);Seconds(stopped.RelativeTo(0,1),-19.1f);
        stopped.Observe(0,21,20,false,0,0);stopped.Observe(1,21,9,false,0,0);Seconds(stopped.RelativeTo(0,1),-20.1f);
        stopped.Observe(1,22,15,false,0,0);Seconds(stopped.RelativeTo(0,1),-1.5f);
        checks.Add("A leading car's stop preserves arrival and departure separately; moving again cannot smear stationary time across an earlier passage");

        var close=new RaceTiming(100,new[]{0,1});close.Observe(0,0,0,false,0,0);close.Observe(1,0,0,false,0,0);
        close.Observe(0,1,3,false,0,0);close.Observe(1,1,1.5f,false,0,0);Seconds(close.RelativeTo(0,1),-.5f);
        close.Observe(0,2,6,false,0,0);close.Observe(1,2,8,false,0,0);close.Sort();Require(close.Rank(1)==1&&close.RelativeTo(1,0).Value<0,"overtake did not change order/gap sign");
        checks.Add("Close cars within one timing interval interpolate the observed leading path; overtakes update legal order and gap sign");

        t.Observe(0,5,20,false,0,1);t.Observe(1,5,16,false,0,0);Require(t.RelativeTo(0,1).Kind==RaceTiming.GapKind.Waiting,"recovery reused stale passage timestamps");
        t.Observe(0,6,30,false,0,1);t.Observe(1,6,26,false,0,0);Seconds(t.RelativeTo(0,1),-.4f);
        checks.Add("Recovery clears historical timing instead of granting a false time gain; new physical passages restore the gap");

        var laps=new RaceTiming(100,new[]{0,1});laps.Observe(0,0,0,false,0,0);laps.Observe(1,0,0,false,0,0);
        laps.Observe(0,10,230,false,0,0);laps.Observe(1,10,110,false,0,0);var g=laps.RelativeTo(0,1);Require(g.Kind==RaceTiming.GapKind.Laps&&g.Value==-1,"lap-down state absent");
        laps.Observe(0,12,300,true,12,0);laps.Observe(1,15.25f,300,true,15.25f,0);Seconds(laps.RelativeTo(1,0),3.25f);laps.Sort();Require(laps.Rank(0)==1,"finish order lost");
        laps.Observe(0,20,900,true,12,0);Seconds(laps.RelativeTo(1,0),3.25f);
        checks.Add("Lap differences are explicit; completed races use finish-time differences and cannot drift after the finish");

        var slots=new int[32];for(int i=0;i<32;i++)slots[i]=31-i;var field=new RaceTiming(100,slots);
        for(int i=0;i<32;i++)field.Observe(i,-1,-i,false,0,0);field.Sort();
        Require(field.WindowStart(0)==0&&field.WindowStart(31)==21&&field.WindowStart(16)==11,"eleven-row boundary window wrong");
        var small=new RaceTiming(100,new[]{1,0});small.Observe(0,-1,-10,false,0,0);small.Observe(1,-1,-10,false,0,0);small.Sort();Require(small.Rank(1)==1&&small.WindowStart(0)==0,"stable grid tie/small field wrong");
        checks.Add("Relative-order windows retain the player at both field ends and midfield, including32-car and small fields; ties use actual grid slot");

        Require(RaceHud.DriveIndicator(10,-1)=="D"&&RaceHud.DriveIndicator(0,-1)=="R"&&RaceHud.DriveIndicator(-2,0)=="R"&&RaceHud.DriveIndicator(0,0)=="D","automatic drive/reverse display is misleading");
        checks.Add("Dashboard selector represents current automatic forward/reverse behaviour; braking forward does not fabricate a reverse gear");
        return checks;
    }
}
