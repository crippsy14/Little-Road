using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class PathSpeedChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Path speed: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        var original=new ContextPathFollower(profile,false);var candidate=new ContextPathFollower(profile,true);
        int faster=0;float biggest=0;
        for(float d=20;d<road.Length;d+=40)foreach(float side in new[]{-4f,0f,4f}) {
            var f=road.Forward(d);var self=new ObservedCar(0,road.At(d)+road.Right(d)*side,f*18,f,road.Right(d),d,side,18,1.2f,2.3f);
            var o=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
            var path=RoadPathFeasibility.FromMotion(in o,side,false);
            var a=original.Follow(in o,in path);var b=candidate.Follow(in o,in path);
            Require(!float.IsNaN(b.targetSpeed)&&b.targetSpeed>=0&&b.targetSpeed<=CarTuning.Club.AttainableSpeed+.001f,"invalid equipment speed bound");
            Require(a.steering==b.steering,"speed experiment changes steering geometry");
            if(b.targetSpeed>a.targetSpeed+.1f)faster++;biggest=Mathf.Max(biggest,b.targetSpeed-a.targetSpeed);
            var guide=RoadPathFeasibility.GuideFromMotion(in o,profile);
            a=original.Follow(in o,in guide);b=candidate.Follow(in o,in guide);
            Require(a.pedal==b.pedal&&a.targetSpeed==b.targetSpeed&&a.steering==b.steering,"guide command parity lost");
            path.coast=true;b=candidate.Follow(in o,in path);Require(b.pedal<=0,"coast trajectory gains drive torque");
        }
        Require(faster>0,"real offset paths never use the local speed envelope");
        results.Add("Actual Cedar offset paths can use local pace without changing steering or exceeding equipment; faster samples="+faster+", largest target difference="+biggest);
        results.Add("Guide paths preserve exact commands throughout Cedar; coast paths retain the torque cap");
        float route=course.regionStarts[13]+190;var forward=road.Forward(route);
        var own=new ObservedCar(0,road.At(route),forward*18,forward,road.Right(route),route,0,18,1.2f,2.3f);
        var club=new DriverObservation(10,1f/120,own,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        var stock=new DriverObservation(10,1f/120,own,Array.Empty<ObservedCar>(),road,CarTuning.Stock,1,0,false);
        var clear=RoadPathFeasibility.FromMotion(in club,0,false);
        float clubTarget=candidate.Follow(in club,in clear).targetSpeed;
        float stockTarget=candidate.Follow(in stock,in clear).targetSpeed;
        Require(clubTarget>stockTarget&&stockTarget<=CarTuning.Stock.AttainableSpeed+.001f,"equipment cache ignores changed power/gearing");
        Require(candidate.Follow(in club,in clear).targetSpeed==clubTarget,"equipment cache does not restore the same command");
        results.Add("Owned equipment-speed cache invalidates for changed power/gearing and restores identical commands");
        candidate.Follow(in club,in clear);long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<50;i++)candidate.Follow(in club,in clear);
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm local speed planning allocates");
        results.Add("Local speed envelope and equipment cache allocate zero memory when warm");
        var prediction=new ContextPathPrediction(profile,true);
        prediction.Predict(in club,clear,Vector3.zero,new Vector3(1.2f,.5f,2.3f),1);
        var first=prediction.samples[0].command;var command=candidate.Follow(in club,in clear);
        Require(first.pedal==command.pedal&&first.steering==command.steering&&first.targetSpeed==command.targetSpeed,"live and prospective local speed differ");
        results.Add("Prospective physical rollout starts with the identical local-speed live command");
        return results;
    }
}
