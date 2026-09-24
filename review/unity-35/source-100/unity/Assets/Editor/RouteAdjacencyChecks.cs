using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RouteAdjacencyChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Route adjacency: "+why);}
    static ObservedCar Car(Circuit road,int id,float route,float offset,float speed)
    {
        var f=road.Forward(route);var r=road.Right(route);
        return new ObservedCar(id,road.At(route)+r*offset,f*speed,f,r,route,offset,speed,1.16f,1.59f);
    }
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        // Route/offset witnesses from the two distant-section failures in 78.
        // Positions/headings are reconstructed from track geometry, not claimed
        // to be a replay of captured physical poses.
        foreach(var v in new[]{new Vector4(1118.969f,-6.293213f,958.75885f,-4.139169f),new Vector4(1171.6519f,3.665558f,1004.5402f,3.028772f)}) {
            var self=Car(road,0,v.x,v.y,30);var other=Car(road,1,v.z,v.w,25);
            var o=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Club,1,0,false);
            Require(RoadPathFeasibility.LongitudinallyAdjacent(in o,in other),"witness no longer demonstrates projection aliasing");
            Require(!RoadPathFeasibility.LongitudinallyAdjacent(in o,in other,true),"separate section still treated as alongside");
            var map=new ContextPreferenceMap(profile,false,true,false,true,true,false,true);map.Fill(in o,false,0,true,true);
            Require(map.RelevantCars==0&&map.ReachableLeft==-map.Edge&&map.ReachableRight==map.Edge,"distant-section car still reserves a corridor");
            results.Add("Separate Cedar section cannot reserve alongside space at route "+v.x);
        }
        foreach(float route in new[]{267f,road.Length-1}) {
            var self=Car(road,0,route,-2,20);var other=Car(road,1,Mathf.Repeat(route+1,road.Length),2,20);
            var o=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Club,1,0,false);
            Require(RoadPathFeasibility.LongitudinallyAdjacent(in o,in other,true),"actual alongside car lost, including wrapped route");
            results.Add("Actual alongside body remains classified near route "+route);
        }
        float d=course.regionStarts[13]+190;
        var front=Car(road,0,d,0,18);var rear=Car(road,1,d-12,4,26);
        var merge=new DriverObservation(10,1f/120,front,new[]{front,rear},road,CarTuning.Club,1,0,false);
        var before=new ContextPreferenceMap(profile,false,true,false,true,true);before.Fill(in merge,false,0,true,true);
        var after=new ContextPreferenceMap(profile,false,true,false,true,true,false,true);after.Fill(in merge,false,0,true,true);
        Require(after.RearApproaches==1&&after.ReachableLeft==before.ReachableLeft&&after.ReachableRight==before.ReachableRight,"legitimate rear merge reservation lost");
        results.Add("A nearby closing rear car still reserves its actual merge corridor");
        return results;
    }
}
