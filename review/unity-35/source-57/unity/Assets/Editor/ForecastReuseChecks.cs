using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class ForecastReuseChecks
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Forecast reuse: "+message);}
    public static List<string> Run()
    {
        var results=new List<string>();
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var tuning=CarTuning.Club;var profile=new RacingSpeedProfile(line,tuning);
        var centre=new Vector3(.03f,.5f,.1f);var half=new Vector3(1.2f,.6f,2.3f);
        float d=road.length*.35f;var forward=road.Forward(d);var right=road.Right(d);
        var self=new ObservedCar(0,road.At(d)+Vector3.up*.7f,forward*20,forward,right,d,0,20,half.x,half.z,.02f);
        var o=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,.1f);
        var path=RoadPathFeasibility.FromMotion(in o,1,true);
        using(var pool=new ForecastWorkerPool(2)) {
            var jobs=new ForecastWork[1];
            var serial=new RoadPathFeasibility(profile,centre,half,null);
            var candidate=new RoadPathFeasibility(profile,centre,half,null);
            jobs[0]=candidate.Prepare(in o,in path);pool.Run(jobs,1);
            bool expected=serial.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
            bool actual=candidate.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
            Require(expected==actual&&candidate.PrefetchedCalls==1&&candidate.SynchronousCalls==0,"joined same-request result not reused");
            candidate.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
            Require(candidate.PrefetchedCalls==1&&candidate.SynchronousCalls==1,"result reused twice");
            results.Add("Joined forecast preserves acceptance and can be consumed only once");

            // Stale time, changed equipment/pose/control timestep/steering and
            // candidate path must all use the current request's serial result.
            for(int change=0;change<10;change++) {
                var changedTuning=tuning;if(change==1)changedTuning.roadGrip+=.25f;
                var changedSelf=change==2?new ObservedCar(0,self.position+right*.01f,self.velocity,forward,right,d,.01f,20,half.x,half.z,.02f):self;
                var changed=new DriverObservation(change==0?11:10,change==3?1f/100:1f/120,changedSelf,Array.Empty<ObservedCar>(),road,changedTuning,1,0,false,change==4?.11f:.1f);
                var changedPath=path;if(change==5)changedPath.endOffset+=.01f;
                if(change==6)changedPath.guide=true;if(change==7)changedPath.guideOffset+=.01f;if(change==8)changedPath.guideSlope+=.01f;
                if(change==9)changedPath.coast=true;
                jobs[0]=candidate.Prepare(in o,in path);pool.Run(jobs,1);
                int before=candidate.SynchronousCalls;
                expected=serial.Check(in changed,in changedPath,float.NegativeInfinity,float.PositiveInfinity);
                actual=candidate.Check(in changed,in changedPath,float.NegativeInfinity,float.PositiveInfinity);
                Require(actual==expected&&candidate.SynchronousCalls==before+1&&candidate.PrefetchedCalls==1,"changed request reused: "+change);
            }
            results.Add("Stale time or changed equipment, pose, timestep, steering, path and guide geometry fall back to current serial prediction");

            // Prepared but never dispatched (e.g. recovery suppressed a plan)
            // cannot masquerade as a completed forecast.
            jobs[0]=candidate.Prepare(in o,in path);
            int oldCount=candidate.SynchronousCalls;
            candidate.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
            Require(candidate.SynchronousCalls==oldCount+1,"unexecuted request reused");
            jobs[0]=candidate.Prepare(in o,in path);pool.Run(jobs,1);
            expected=serial.Check(in o,in path,100,101);
            actual=candidate.Check(in o,in path,100,101);
            Require(!expected&&!actual&&candidate.PrefetchedCalls==2,"reused forecast bypassed current corridor constraint");
            results.Add("Undispatched input falls back and reused prediction still checks the current corridor");
        }
        return results;
    }
}
