using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RearPressureChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Rear pressure: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Street,default(DriverPlanningSettings).WithRisk(5));
        float d=course.regionStarts[13]+190;var f=road.Forward(d);var right=road.Right(d);
        var self=new ObservedCar(0,line.At(d),f*18,f,right,d,line.Offset(d),18,1.2f,2.3f);
        var empty=new DriverObservation(10,1f/120,self,new[]{self},road,CarTuning.Street,1,0,false);
        var clear=new ContextPreferenceMap(profile,false,true,false,true);clear.Fill(in empty,false,0,true,true);clear.Pick(out float expected);
        foreach(float gap in new[]{-5f,-6f,-7f})foreach(float speed in new[]{12f,18f,26f}) {
            var other=new ObservedCar(1,road.At(d+gap)+road.Right(d+gap)*self.offset,f*speed,f,road.Right(d+gap),d+gap,self.offset,speed,1.2f,2.3f);
            var o=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Street,1,0,false);
            Require(!RoadPathFeasibility.LongitudinallyAdjacent(in o,in other),"test bodies overlap");
            var map=new ContextPreferenceMap(profile,false,true,false,true);map.Fill(in o,false,0,true,true);map.Pick(out float selected);
            Require(map.RelevantCars==0&&selected==expected,"trailing car changed the clear target");
            for(int i=0;i<map.Count;i++)Require(map.value[i]==clear.value[i]&&map.reachable[i]==clear.reachable[i],"trailing car changed preference or corridor");
            results.Add("Clear leader ignores non-overlapping same-corridor rear pressure at gap="+gap+", speed="+speed);
        }
        foreach(int side in new[]{-1,1}) {
            float offset=self.offset+side*4,gap=-12;
            var other=new ObservedCar(1,road.At(d+gap)+road.Right(d+gap)*offset,f*26,f,road.Right(d+gap),d+gap,offset,26,1.2f,2.3f);
            var o=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Street,1,0,false);
            var old=new ContextPreferenceMap(profile,false,true);old.Fill(in o,false,0,true,true);
            var map=new ContextPreferenceMap(profile,false,true,false,true);map.Fill(in o,false,0,true,true);map.Pick(out float selected);
            Require(map.RearApproaches==1&&map.ReachableLeft==old.ReachableLeft&&map.ReachableRight==old.ReachableRight,"imminent merge reservation lost");
            Require(selected==expected,"noncrossing leader target changed for side rear approach");
            results.Add("Side rear approach reserves crossing space without displacing a clear leader: "+side);
        }
        foreach(float gap in new[]{-.5f,12f}) {
            float offset=self.offset+3;
            var other=new ObservedCar(1,road.At(d+gap)+road.Right(d+gap)*offset,f*12,f,road.Right(d+gap),d+gap,offset,12,1.2f,2.3f);
            var o=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Street,1,0,false);
            var old=new ContextPreferenceMap(profile,false,true);old.Fill(in o,false,0,true,true);
            var map=new ContextPreferenceMap(profile,false,true,false,true);map.Fill(in o,false,0,true,true);
            for(int i=0;i<map.Count;i++)Require(map.value[i]==old.value[i]&&map.reachable[i]==old.reachable[i],"ahead/alongside handling changed");
            results.Add("Ahead or physically alongside opponent retains original preferences and corridor: "+gap);
        }
        return results;
    }
}
