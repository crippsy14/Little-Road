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
        float d=course.regionStarts[1]-20;var f=road.Forward(d);
        var self=new ObservedCar(0,road.At(d),f*24,f,road.Right(d),d,0,24,1.2f,2.3f);
        var o=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,0);
        var path=RoadPathFeasibility.FromMotion(in o,0,true);var centre=new Vector3(0,.5f,0);var half=new Vector3(1.2f,.6f,2.3f);
        var a=new ForecastRequest(in o,in path,centre,half,planning:early);
        var b=new ForecastRequest(in o,in path,centre,half,planning:earlier);
        Require(!a.Matches(in b),"changed judgement reused stale request");
        bool mismatch=false;try{a.Run(new ContextPathPrediction(original));}catch(InvalidOperationException){mismatch=true;}
        Require(mismatch,"request applied to wrong driver's profile");
        using(var pool=new ForecastWorkerPool(3)){
            var jobs=new ForecastWork[3];var serial=new ContextPathPrediction[3];
            for(int i=0;i<3;i++){
                var request=new ForecastRequest(in o,in path,centre,half,planning:profiles[i].planning);
                serial[i]=new ContextPathPrediction(profiles[i]);request.Run(serial[i]);
                jobs[i]=new ForecastWork(new ContextPathPrediction(profiles[i]));jobs[i].Set(in request);
            }
            pool.Run(jobs,3);
            for(int i=0;i<3;i++)for(int j=0;j<serial[i].Count;j++)Require(serial[i].samples[j].Equals(jobs[i].prediction.samples[j]),"driver-specific worker forecast differs");
        }
        results.Add("Owned forecasts distinguish driver judgement, reject mismatched profiles and match serial motion for every preset");
        return results;
    }
}
