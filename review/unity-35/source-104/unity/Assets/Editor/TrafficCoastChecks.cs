using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TrafficCoastChecks
{
    public static List<string> Run()
    {
        var results=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Traffic coast: "+message);results.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var follower=new ContextPathFollower(profile);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);
        var self=new ObservedCar(0,road.At(d),forward*18,forward,road.Right(d),d,0,18,1.2f,2.3f);
        var o=new DriverObservation(1,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        var path=RoadPathFeasibility.FromMotion(in o,0,false);var coast=path;coast.coast=true;
        var normal=follower.Follow(in o,in path);var capped=follower.Follow(in o,in coast);
        Check(normal.pedal>0&&capped.pedal==0&&normal.steering==capped.steering,"Coast trajectory suppresses acceleration without changing steering");
        var fast=new ObservedCar(0,self.position,forward*50,forward,road.Right(d),d,0,50,1.2f,2.3f);
        var overspeed=new DriverObservation(2,1f/120,fast,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        normal=follower.Follow(in overspeed,in path);capped=follower.Follow(in overspeed,in coast);
        Check(normal.pedal<0&&normal.pedal==capped.pedal,"Coast trajectory preserves required deceleration");
        var centre=new Vector3(0,.5f,0);var half=new Vector3(1.2f,.6f,2.3f);
        var a=new ForecastRequest(in o,in path,centre,half);var b=new ForecastRequest(in o,in coast,centre,half);
        Check(!a.Matches(in b),"Coast mode belongs to owned forecast request identity");
        var accelerating=new ContextPathPrediction(profile);var coasting=new ContextPathPrediction(profile);a.Run(accelerating);b.Run(coasting);
        float accelerated=Vector3.Distance(self.position,accelerating.samples[accelerating.Count-1].state.position);
        float coasted=Vector3.Distance(self.position,coasting.samples[coasting.Count-1].state.position);
        Check(accelerated>coasted+1&&coasted>10,"Coast rollout retains forward motion without fabricated power");
        return results;
    }
}
