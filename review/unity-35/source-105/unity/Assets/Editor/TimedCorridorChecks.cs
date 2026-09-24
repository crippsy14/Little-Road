using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TimedCorridorChecks
{
    sealed class Straight:IDriverRoute
    {
        public float Length=>1000;
        public Vector3 At(float d)=>new Vector3(0,0,d);
        public Vector3 Forward(float d)=>Vector3.forward;
        public Vector3 Right(float d)=>Vector3.right;
    }
    static ObservedCar Car(int id,float route,float side,float speed)=>new ObservedCar(id,new Vector3(side,.7f,route),Vector3.forward*speed,
        Vector3.forward,Vector3.right,route,side,speed,1.2f,2.3f);
    static void Require(bool ok,string message){if(!ok)throw new Exception("Timed corridor: "+message);}
    public static List<string> Run()
    {
        var results=new List<string>();var route=new Straight();
        var course=new TechnicalCircuit();var line=new RacingLine(course.track,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        var original=new ContextPreferenceMap(profile);var timed=new ContextPreferenceMap(profile);
        var self=Car(0,100,0,20);var rear=Car(1,88,4,28);var traffic=new[]{self,rear};
        var o=new DriverObservation(10,1f/120,self,traffic,route,CarTuning.Club,1,0,false);
        original.Fill(in o,false,0,true,true);timed.Fill(in o,false,0,true,true,true);
        Require(original.Pick(out float a)==timed.Pick(out float b)&&a==b&&original.ReachableRight==timed.ReachableRight,"destination reservation changed");
        float start=ContextPreferenceMap.RearReservationStart(in o,in rear,-12);
        original.BoundsAt(0,out _,out float oldRight);timed.BoundsAt(start-.001f,out _,out float before);timed.BoundsAt(start+.001f,out _,out float after);
        Require(start>0&&float.IsPositiveInfinity(before)&&after==oldRight,"approach-time activation failed");
        float longitudinalClear=12-(28-20)*start-self.halfLength-rear.halfLength;
        Require(longitudinalClear>=3,"reserved later than original 3m longitudinal space");
        results.Add("Rear destination remains reserved; approach corridor activates before the measured constant-closing bodies reach the original 3m space");

        var adjacent=Car(2,99,-4,20);traffic=new[]{self,rear,adjacent};
        var mixed=new DriverObservation(10,1f/120,self,traffic,route,CarTuning.Club,1,0,false);
        timed.Fill(in mixed,false,0,true,true,true);timed.BoundsAt(0,out float left,out float right);
        Require(float.IsFinite(left)&&float.IsPositiveInfinity(right),"rear delay removed an already adjacent constraint");
        timed.BoundsAt(start+.001f,out float laterLeft,out float laterRight);
        Require(laterLeft==left&&float.IsFinite(laterRight),"independent sides were conflated");
        results.Add("Adjacent car reserves space immediately while a separate rear constraint activates independently");

        rear=Car(1,88,0,28);traffic=new[]{self,rear};
        var following=new DriverObservation(10,1f/120,self,traffic,route,CarTuning.Club,1,0,false);
        timed.Fill(in following,false,0,true,true,true);timed.BoundsAt(2,out left,out right);
        Require(float.IsNegativeInfinity(left)&&float.IsPositiveInfinity(right),"same-corridor rear follower acquired a merge reservation");
        results.Add("Rear follower already in the same corridor does not force the leading car aside");
        return results;
    }
}
