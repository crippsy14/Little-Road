using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class FollowingFeasibilityChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new Exception("Following feasibility: "+text);checks.Add(text);}
        var course=new TechnicalCircuit();var road=course.track;float d=course.regionStarts[13]+190;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var half=new Vector3(1.2f,.5f,2.3f);
        var self=new ObservedCar(0,road.At(d),road.Forward(d)*18,road.Forward(d),road.Right(d),d,0,18,half.x,half.z);
        var lead=new ObservedCar(1,road.At(d+10),road.Forward(d+10)*10,road.Forward(d+10),road.Right(d+10),d+10,0,10,half.x,half.z);
        var o=new DriverObservation(10,1f/120,self,new[]{lead},road,CarTuning.Club,1,0,false);
        var path=RoadPathFeasibility.FromMotion(in o,0,false);var traffic=new ObservedTrafficRollout();traffic.Prepare(in o);
        var serial=new RoadPathFeasibility(profile,Vector3.zero,half,null);
        serial.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
        float free=serial.PredictedRouteGain;
        bool expected=serial.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity,following:traffic);
        float constrained=serial.PredictedRouteGain;
        Check(constrained<free-2&&constrained>10,"Following-constrained feasibility forecasts slower but positive physical progress");
        var candidate=new RoadPathFeasibility(profile,Vector3.zero,half,null);
        using(var pool=new ForecastWorkerPool(1)) {
            var jobs=new[]{candidate.Prepare(in o,in path)};pool.Run(jobs,1);
            bool actual=candidate.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity,following:traffic);
            Check(actual==expected&&candidate.PredictedRouteGain==constrained&&candidate.PrefetchedCalls==0&&candidate.SynchronousCalls==1,
                "Traffic-constrained request cannot reuse a completed nominal worker result");
            candidate.Check(in o,in path,float.NegativeInfinity,float.PositiveInfinity);
            Check(candidate.PredictedRouteGain==free&&candidate.PrefetchedCalls==0&&candidate.SynchronousCalls==2,
                "Consumed nominal result cannot leak into the following or subsequent request");
        }
        return checks;
    }
}
