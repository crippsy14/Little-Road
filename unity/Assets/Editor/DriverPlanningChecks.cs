using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DriverPlanningChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Driver planning: "+message);}
    public static List<string> Run()
    {
        var results=new List<string>();
        var neutral=default(DriverPlanningSettings);var explicitNeutral=new DriverPlanningSettings(.75f);
        Require(neutral.Same(in explicitNeutral)&&neutral.ReferenceBrakeScale==1,"default is not exact neutral");
        foreach(float invalid in new[]{0f,.54f,.76f,float.NaN,float.PositiveInfinity}){
            bool rejected=false;try{_ = new DriverPlanningSettings(invalid);}catch(ArgumentOutOfRangeException){rejected=true;}
            Require(rejected,"invalid judgement accepted");
        }
        results.Add("Driver judgement defaults to exact neutral and rejects non-finite/out-of-domain values");
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var tuning=CarTuning.Club;
        var original=new RacingSpeedProfile(line,tuning);var same=new RacingSpeedProfile(line,tuning,explicitNeutral);
        for(int i=0;i<line.Count;i++)Require(original.speed[i]==same.speed[i]&&original.acceleration[i]==same.acceleration[i],"explicit neutral profile changed");
        results.Add("Explicit neutral speed/acceleration arrays exactly match default planning");
        var early=DriverPlanningSettings.Preset("early");var earlier=DriverPlanningSettings.Preset("earlier");
        var profiles=new[]{new RacingSpeedProfile(line,tuning,early),new RacingSpeedProfile(line,tuning,earlier),original};
        Require(profiles[0].predictedLapTime>profiles[1].predictedLapTime&&profiles[1].predictedLapTime>profiles[2].predictedLapTime,"planned braking ladder has no expected time separation");
        results.Add("Earlier braking estimates have ordered planned lap times on the fixed car");
        var risk=neutral.WithRisk(5);
        Require(risk.Risk==5&&risk.BrakeFraction==neutral.BrakeFraction&&!risk.Same(in neutral),"risk alters braking or loses identity");
        foreach(int invalid in new[]{-1,11}){
            bool rejected=false;try{_ = neutral.WithRisk(invalid);}catch(ArgumentOutOfRangeException){rejected=true;}
            Require(rejected,"invalid risk accepted");
        }
        var riskProfile=new RacingSpeedProfile(line,tuning,risk);
        for(int i=0;i<line.Count;i++)Require(original.speed[i]==riskProfile.speed[i]&&original.acceleration[i]==riskProfile.acceleration[i],"risk grants vehicle pace");
        results.Add("Risk validates 0..10 independently of braking and physical pace");
        return results;
    }
}
