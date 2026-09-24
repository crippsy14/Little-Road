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
        var risk=neutral.WithRisk(5);
        Require(risk.Risk==5&&risk.BrakeFraction==neutral.BrakeFraction&&!risk.Same(in neutral),"risk alters braking or loses identity");
        foreach(int invalid in new[]{-1,11}){
            bool rejected=false;try{_ = neutral.WithRisk(invalid);}catch(ArgumentOutOfRangeException){rejected=true;}
            Require(rejected,"invalid risk accepted");
        }
        var riskProfile=new RacingSpeedProfile(line,tuning,risk);
        for(int i=0;i<line.Count;i++)Require(original.speed[i]==riskProfile.speed[i]&&original.acceleration[i]==riskProfile.acceleration[i],"risk grants vehicle pace");
        var riskRequest=new ForecastRequest(in o,in path,centre,half,planning:risk);
        var neutralRequest=new ForecastRequest(in o,in path,centre,half,planning:neutral);
        Require(!riskRequest.Matches(in neutralRequest),"risk missing from forecast identity");
        mismatch=false;try{riskRequest.Run(new ContextPathPrediction(original));}catch(InvalidOperationException){mismatch=true;}
        Require(mismatch,"risk request accepted wrong profile");
        results.Add("Risk validates 0..10 independently of braking and physical pace, and belongs to owned forecast identity");
        d=course.regionStarts[13]+190;f=road.Forward(d);
        self=new ObservedCar(0,road.At(d)+road.Right(d)*.25f,f*24,f,road.Right(d),d,.25f,24,1.2f,2.3f);
        var others=new ObservedCar[2];
        for(int i=0;i<2;i++){
            float offset=i==0?-2.65f:3.15f;
            others[i]=new ObservedCar(i+1,road.At(d+15)+road.Right(d+15)*offset,f*10,f,road.Right(d+15),d+15,offset,10,1.2f,2.3f);
        }
        var gapObservation=new DriverObservation(10,1f/120,self,others,road,tuning,1,0,false,0);
        var cautiousMap=new ContextPreferenceMap(original);var riskMap=new ContextPreferenceMap(riskProfile);
        cautiousMap.Fill(in gapObservation,false,0,true,true);riskMap.Fill(in gapObservation,false,0,true,true);
        Require(cautiousMap.Count==ContextPreferenceMap.Slots,"risk0 adds candidates");
        bool hasGap=false;
        for(int i=ContextPreferenceMap.Slots;i<riskMap.Count;i++)hasGap|=Mathf.Abs(riskMap.Sample(i)-.25f)<.001f;
        Require(hasGap,"narrow off-grid gap not sampled");
        Require(riskMap.Danger(in gapObservation,.25f)<cautiousMap.Danger(in gapObservation,.25f),"risk5 still penalises the physically clear gap equally");
        for(int i=0;i<ContextPreferenceMap.Slots;i++)Require(riskMap.reachable[i]==cautiousMap.reachable[i],"risk weakens hard side reservation");
        results.Add("Risk5 samples an off-grid gap centre and reduces spare-space preference without weakening side reservations");
        var emptyObservation=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,0);
        cautiousMap.Fill(in emptyObservation,false,0,true,true);riskMap.Fill(in emptyObservation,false,0,true,true);
        Require(cautiousMap.Pick(out float oldTarget)==riskMap.Pick(out float newTarget)&&oldTarget==newTarget,"risk changes clear-road choice");
        Require(riskMap.Count==ContextPreferenceMap.Slots,"clear road adds candidates");
        results.Add("Risk5 leaves the unconstrained clear-road preference exactly unchanged");
        return results;
    }
}
