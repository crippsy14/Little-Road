using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class PassTrajectoryChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();var road=new TechnicalCircuit().track;
        void Check(bool ok,string name){if(!ok)throw new Exception("Pass trajectory: "+name);checks.Add(name);}
        DriverObservation Observe(float route,float offset=1,bool finished=false) {
            var f=road.Forward(route);var r=road.Right(route);
            var car=new ObservedCar(0,road.At(route)+r*offset,f*5,f,r,route,offset,5,1.16f,1.6f);
            return new DriverObservation(10,1f/120,car,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,finished,0);
        }
        var previous=new ContextPathState{anchor=90,consumed=10,startOffset=0,startSlope=.04f,endOffset=4,selectedOffset=4,blend=30};
        var o=Observe(101);
        Check(PassTrajectory.TryRetain(in o,in previous,100,4,1,out var candidate)&&candidate.anchor==90&&candidate.consumed==11
            &&candidate.startOffset==previous.startOffset&&candidate.startSlope==previous.startSlope&&candidate.blend==30,"Unchanged pass advances consumed distance without rebuilding its original geometry");
        var advanced=previous;advanced.consumed=11;
        Check(PassTrajectory.TryRetain(in o,in advanced,101,4,1,out var selected)&&selected.consumed==candidate.consumed,
            "Prospective preparation and already-advanced selection produce the same consumed distance");
        Check(!PassTrajectory.TryRetain(in o,in previous,100,3.9f,1,out _),"A changed preferred destination immediately replans");
        Check(!PassTrajectory.TryRetain(in o,in previous,100,4,0,out _),"Clear road releases the passing trajectory");
        var finished=Observe(101,1,true);var near=Observe(101,3.6f);
        Check(!PassTrajectory.TryRetain(in finished,in previous,100,4,1,out _)&&!PassTrajectory.TryRetain(in near,in previous,100,4,1,out _),"Finished or nearly-completed lateral manoeuvre is not retained");
        var ended=previous;ended.consumed=30;
        Check(!PassTrajectory.TryRetain(in o,in ended,101,4,1,out _),"A completed trajectory cannot be restarted as retained");
        foreach(int type in new[]{0,1,2}){var special=previous;special.guide=type==0;special.rejoin=type==1;special.coast=type==2;
            Check(!PassTrajectory.TryRetain(in o,in special,100,4,1,out _),"Guide, rejoin or coast path excluded from pass commitment: "+type);}
        var wrapped=Observe(2);var seam=previous;seam.anchor=road.length-6;seam.consumed=4;
        Check(PassTrajectory.TryRetain(in wrapped,in seam,road.length-2,4,1,out var across)&&Mathf.Abs(across.consumed-8)<.001f,
            "Retained progress crosses the circuit seam without resetting");
        return checks;
    }
}
