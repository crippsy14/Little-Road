using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class ContinuationForecastChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Continuation forecast: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var tuning=CarTuning.Club;var profile=new RacingSpeedProfile(line,tuning);
        float d=road.length*.35f;var f=road.Forward(d);var r=road.Right(d);
        var self=new ObservedCar(0,road.At(d),f*20,f,r,d,0,20,1.2f,2.3f,.02f);
        var o=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,.1f);
        var first=RoadPathFeasibility.FromMotion(in o,2,false);
        var second=RoadPathFeasibility.FromMotion(in o,-2,false);
        var centre=new Vector3(.03f,.5f,.1f);var half=new Vector3(1.2f,.6f,2.3f);
        var serial=new RoadPathFeasibility(profile,centre,half,null);
        var candidate=new RoadPathFeasibility(profile,centre,half,null,true);
        var jobs=new ForecastWork[2];
        using(var pool=new ForecastWorkerPool(2)) {
            void Prepare(in DriverObservation current){jobs[0]=candidate.Prepare(in current,in first);jobs[1]=candidate.PrepareContinuation(in current,in second);pool.Run(jobs,2);}
            bool Check(RoadPathFeasibility target,DriverObservation obs,ContextPathState path,float left=float.NegativeInfinity,float right=float.PositiveInfinity)
                =>target.Check(in obs,in path,left,right);
            Prepare(in o);
            bool expected=Check(serial,o,first);float gain=serial.PredictedRouteGain;
            Require(Check(candidate,o,first)==expected&&candidate.PredictedRouteGain==gain,"first prediction changed");
            expected=Check(serial,o,second);gain=serial.PredictedRouteGain;
            Require(Check(candidate,o,second)==expected&&candidate.PredictedRouteGain==gain&&candidate.ContinuationReused==1&&candidate.SynchronousCalls==0,"second prediction not exactly reused");
            results.Add("Two joined owned forecasts preserve serial acceptance and progress in request order");
            Check(candidate,o,second);Require(candidate.SynchronousCalls==1&&candidate.ContinuationReused==1,"secondary reused twice");
            results.Add("Continuation result is consumed only once");
            for(int change=0;change<5;change++) {
                Prepare(in o);int sync=candidate.SynchronousCalls,reuse=candidate.ContinuationReused;
                var changed=second;if(change==1)changed.endOffset+=.01f;if(change==2)changed.coast=true;
                var obs=new DriverObservation(change==0?11:10,change==3?1f/100:1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false,change==4?.11f:.1f);
                expected=Check(serial,obs,changed);gain=serial.PredictedRouteGain;
                Require(Check(candidate,obs,changed)==expected&&candidate.PredictedRouteGain==gain&&candidate.SynchronousCalls==sync+1&&candidate.ContinuationReused==reuse,"changed secondary request reused: "+change);
            }
            results.Add("Changed time, path, coast, timestep or steering rejects secondary reuse");
            Prepare(in o);int before=candidate.ContinuationReused;
            Require(!Check(candidate,o,second,100,101)&&candidate.ContinuationReused==before+1,"secondary bypassed current corridor");
            results.Add("Joined continuation still checks the current physical corridor");
            jobs[0]=candidate.Prepare(in o,in first);jobs[1]=candidate.PrepareContinuation(in o,in second);
            int oldSync=candidate.SynchronousCalls;Check(candidate,o,second);
            Require(candidate.SynchronousCalls==oldSync+1,"undispatched secondary reused");
            Prepare(in o);candidate.Prepare(in o,in first);oldSync=candidate.SynchronousCalls;Check(candidate,o,second);
            Require(candidate.SynchronousCalls==oldSync+1,"new primary retained an old secondary");
            results.Add("Undispatched or superseded secondary work cannot satisfy a check");
            Prepare(in o);var traffic=new ObservedTrafficRollout();traffic.Prepare(in o);oldSync=candidate.SynchronousCalls;before=candidate.ContinuationReused;
            expected=serial.Check(in o,in second,float.NegativeInfinity,float.PositiveInfinity,following:traffic);gain=serial.PredictedRouteGain;
            Require(candidate.Check(in o,in second,float.NegativeInfinity,float.PositiveInfinity,following:traffic)==expected&&candidate.PredictedRouteGain==gain
                &&candidate.SynchronousCalls==oldSync+1&&candidate.ContinuationReused==before,"following model reused nominal secondary");
            Check(candidate,o,second);Require(candidate.SynchronousCalls==oldSync+2,"following model did not invalidate secondary");
            results.Add("Traffic-constrained forecasting bypasses and invalidates nominal continuation work");
            Prepare(in o);Check(candidate,o,first);Check(candidate,o,second);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<3;i++){Prepare(in o);Check(candidate,o,first);Check(candidate,o,second);}
            Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm main-thread allocation");
            Require(jobs[0].AllocatedBytes==0&&jobs[1].AllocatedBytes==0,"warm worker allocation");
            results.Add("Repeated paired preparation and consumption has zero warm managed allocation");
        }
        return results;
    }
}
