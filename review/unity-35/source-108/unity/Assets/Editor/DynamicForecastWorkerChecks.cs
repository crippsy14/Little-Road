using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DynamicForecastWorkerChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Dynamic forecast workers: "+why);}
    static void Equal(ContextPathPrediction expected,ContextPathPrediction actual)
    {
        Require(expected.Count==actual.Count,"forecast sample count");
        for(int i=0;i<expected.Count;i++)Require(expected.samples[i].Equals(actual.samples[i]),"forecast sample differs: "+i);
        Require(expected.envelope.Contained==actual.envelope.Contained&&expected.envelope.MaximumBound==actual.envelope.MaximumBound&&expected.envelope.FirstUncertainTime==actual.envelope.FirstUncertainTime,"road envelope differs");
        for(int i=0;i<expected.Count-1;i++)Require(expected.envelope.intervalUpperBounds[i]==actual.envelope.intervalUpperBounds[i],"road interval differs");
    }
    public static List<string> Run()
    {
        var result=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profiles=new[]{new RacingSpeedProfile(line,CarTuning.Stock),new RacingSpeedProfile(line,CarTuning.Street,new DriverPlanningSettings(.65f,2,true)),new RacingSpeedProfile(line,CarTuning.Club,new DriverPlanningSettings(.7f,8,true))};
        var tunings=new[]{CarTuning.Stock,CarTuning.Street,CarTuning.Club};
        var jobs=new ForecastWork[24];var serial=new DynamicRejoinPlanner[24];var controls=new ContextPathPrediction[24];var requests=new ForecastRequest[24];var expected=new bool[24];
        var centre=new Vector3(.03f,.5f,.1f);var half=new Vector3(1.2f,.6f,2.3f);
        using(var pool=new ForecastWorkerPool(4)){
            for(int i=0;i<jobs.Length;i++){
                var profile=profiles[i%3];float d=road.length*i/jobs.Length+.137f,side=(i%3-1)*2,speed=18+i*.2f;
                var f=road.Forward(d);var r=road.Right(d);
                var self=new ObservedCar(i,road.At(d)+r*side,f*speed+r*.2f,f,r,d,side,speed,1.2f,2.3f);
                var rival=new ObservedCar(24+i,road.At(d+12)+road.Right(d+12)*side,road.Forward(d+12)*14,road.Forward(d+12),road.Right(d+12),d+12,side,14,1.2f,2.3f);
                var traffic=new[]{self,rival};var o=new DriverObservation(100,1f/120,self,traffic,road,tunings[i%3],1,0,false,.1f,i%2==0);
                var previous=RoadPathFeasibility.GuideFromMotion(in o,profile);previous.localSpeed=true;previous.consumed=3;
                bool hasPrevious=i%3!=0,aware=i%4!=0;
                serial[i]=new DynamicRejoinPlanner(profile,true,true);serial[i].PrepareOwned(in o,in previous,hasPrevious,aware);
                expected[i]=serial[i].Count>0&&float.IsFinite(serial[i].EstimatedTime(0));controls[i]=new ContextPathPrediction(profile);
                if(expected[i]){var path=serial[i].Path(0);requests[i]=new ForecastRequest(in o,in path,centre,half,planning:profile.planning);requests[i].Run(controls[i],traffic);}
                var planner=new DynamicRejoinPlanner(profile,true,true);var preparation=new DynamicForecastPreparation(planner,profile.planning,centre,half);
                jobs[i]=new ForecastWork(new ContextPathPrediction(profile));jobs[i].SetDynamic(preparation,in o,in previous,hasPrevious,aware);
                // Mutating the caller's buffer after capture must not affect
                // either ranking or drafting inside the queued job.
                traffic[1]=default;
            }
            var original=(ForecastWork[])jobs.Clone();
            for(int repeat=0;repeat<4;repeat++){
                pool.Run(jobs,jobs.Length);
                foreach(var job in jobs){int id=Array.IndexOf(original,job);var ranked=job.DynamicPreparation.Planner;
                    Require(job.Error==null&&job.Completed==expected[id],"completion differs from serial ranking");
                    Require(ranked.Count==serial[id].Count,"rank count differs");
                    for(int j=0;j<ranked.Count;j++)Require(ranked.Path(j).Equals(serial[id].Path(j))&&ranked.EstimatedTime(j)==serial[id].EstimatedTime(j)&&ranked.Clearance(j)==serial[id].Clearance(j),"worker ranking differs");
                    if(expected[id]){Require(job.MatchesRequest(in requests[id]),"completed request identity differs");Equal(controls[id],job.prediction);}
                    if(repeat>0)Require(job.AllocatedBytes==0,"warm worker allocation");
                }
                if(repeat>0)Require(pool.MainAllocatedBytes==0,"warm dispatch allocation");
                Array.Reverse(jobs);
            }
            result.Add("Four reordered24-job dynamic ranking/forecast batches match serial curves, physical samples and bounds across equipment/personality/drafting, with owned traffic and zero warm allocation");
            var profile0=profiles[0];var p0=new DynamicRejoinPlanner(profile0,true,true);
            var a=new ForecastWork(new ContextPathPrediction(profile0));var b=new ForecastWork(new ContextPathPrediction(profile0));
            float start=road.length*.25f;var forward=road.Forward(start);var right=road.Right(start);
            var own=new ObservedCar(0,road.At(start),forward*12,forward,right,start,0,12,1.2f,2.3f);
            var obs=new DriverObservation(200,1f/120,own,Array.Empty<ObservedCar>(),road,CarTuning.Stock,1,0,false);ContextPathState prior=default;
            a.SetDynamic(new DynamicForecastPreparation(p0,profile0.planning,centre,half),in obs,in prior,false,false);
            b.SetDynamic(new DynamicForecastPreparation(p0,profile0.planning,centre,half),in obs,in prior,false,false);
            bool rejected=false;try{pool.Run(new[]{a,b},2);}catch(ArgumentException){rejected=true;}Require(rejected,"shared ranking buffers accepted");
            result.Add("Worker pool rejects aliased ranking buffers even when prediction buffers differ");
            var prep=new DynamicForecastPreparation(p0,profile0.planning,centre,half);
            var off=new ObservedCar(0,road.At(start)+right*30,forward*12,forward,right,start,30,12,1.2f,2.3f);
            var offObs=new DriverObservation(201,1f/120,off,Array.Empty<ObservedCar>(),road,CarTuning.Stock,1,0,false);
            a.SetDynamic(prep,in offObs,in prior,false,false);pool.Run(new[]{a},1);
            Require(!a.Completed&&a.Error==null&&prep.Calls==1&&prep.Forecasts==0,"nonviable ranking published a forecast");
            a.SetDynamic(prep,in obs,in prior,false,false);pool.Run(new[]{a},1);Require(a.Completed,"valid input after nonviable ranking failed");
            result.Add("A nonviable ranking publishes no physical forecast; the same work accepts a later valid snapshot");
            var invalid=new DynamicForecastPreparation(new DynamicRejoinPlanner(profile0,true,true),new DriverPlanningSettings(.55f),centre,half);
            a.SetDynamic(invalid,in obs,in prior,false,false);bool failed=false;
            try{pool.Run(new[]{a},1);}catch(InvalidOperationException){failed=true;}
            Require(failed&&a.Error!=null&&!a.Completed,"dynamic worker exception not joined/propagated");
            a.SetDynamic(prep,in obs,in prior,false,false);pool.Run(new[]{a},1);Require(a.Completed,"valid batch after worker error failed");
            result.Add("Dynamic worker errors cannot publish completed output and a subsequent valid batch remains usable");
        }
        return result;
    }
}
