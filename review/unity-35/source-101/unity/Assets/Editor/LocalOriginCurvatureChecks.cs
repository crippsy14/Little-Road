using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class LocalOriginCurvatureChecks
{
    static void Require(bool condition,string reason){if(!condition)throw new Exception("Local envelope origin: "+reason);}
    public static List<string> Run()
    {
        var course=new TechnicalCircuit();var line=new RacingLine(course.track,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var tuning=CarTuning.Street;var profile=new RacingSpeedProfile(line,tuning);
        var points=new Vector3[720];for(int i=0;i<points.Length;i++){float a=2*Mathf.PI*i/points.Length;points[i]=new Vector3(40*Mathf.Cos(a),0,40*Mathf.Sin(a));}
        var road=new Circuit(points);var forward=road.Forward(0);var right=road.Right(0);
        var self=new ObservedCar(0,road.At(0),forward*19,forward,right,0,0,19,1.2f,2.3f);
        var o=new DriverObservation(0,1f/120,self,Array.Empty<ObservedCar>(),road,tuning,1,0,false);
        var path=new ContextPathState{anchor=0,blend=40,localSpeed=true};
        var previous=new ContextPathFollower(profile,false,false);var corrected=new ContextPathFollower(profile,false,true);
        var before=previous.Follow(in o,in path);var after=corrected.Follow(in o,in path);
        Require(after.targetSpeed==before.targetSpeed&&after.steering==before.steering,"origin correction changed the first forward limit or steering");
        Require(after.targetSpeed>self.speed+1&&after.pedal>.95f&&after.pedal>before.pedal+.5f,"steady corner still cancels required acceleration with phantom braking");
        var results=new List<string>{"On a steady curved path below its unchanged speed limit, corrected origin commands acceleration instead of phantom maximum-braking feed-forward"};
        var tightening=path;tightening.endOffset=6;tightening.blend=16;
        var brake=corrected.Follow(in o,in tightening);
        Require(brake.pedal<0,"correction suppresses braking for a genuinely tightening path");
        results.Add("A genuinely tightening upcoming path still commands braking with the corrected origin");
        var legacy=path;legacy.localSpeed=false;
        before=previous.Follow(in o,in legacy);after=corrected.Follow(in o,in legacy);
        Require(before.pedal==after.pedal&&before.steering==after.steering&&before.targetSpeed==after.targetSpeed,"reference envelope behaviour changed");
        results.Add("Reference-profile commands remain exactly unchanged by the local-origin option");
        after=corrected.Follow(in o,in path);var prediction=new ContextPathPrediction(profile,false,true);
        prediction.Predict(in o,path,Vector3.zero,new Vector3(1.2f,.5f,2.3f),1);
        var predicted=prediction.samples[0].command;
        Require(after.pedal==predicted.pedal&&after.steering==predicted.steering&&after.targetSpeed==predicted.targetSpeed,"live and forecast origin correction diverge");
        results.Add("The corrected local-origin speed envelope produces identical live and physical-forecast commands");
        corrected.Follow(in o,in path);long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;i++)corrected.Follow(in o,in path);
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm corrected follower allocates");
        results.Add("Corrected local-origin following allocates zero managed bytes when warm");
        return results;
    }
}
