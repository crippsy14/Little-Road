using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-racing-review goal 1: --pass-commitment ranks candidates on the other side
// of the committed blocker behind; ordering only.
public static class PassCommitmentChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Pass commitment: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        float d=80;
        ObservedCar Car(int id,float route,float side,float v){var f=road.Forward(route);return new ObservedCar(id,road.At(route)+road.Right(route)*side,f*v,f,road.Right(route),route,side,v,1.2f,2.3f);}
        var self=Car(0,d,.5f,25);var slow=Car(1,d+14,.5f,18);
        var o=new DriverObservation(1,1f/120,self,new[]{self,slow},road,CarTuning.Club,1,0,false);

        var planner=new DynamicRejoinPlanner(profile,true,true);
        planner.PrepareTraffic(in o,default,false);
        Check(planner.TrafficPlan&&planner.Blocker==1&&Mathf.Abs(planner.BlockerCentre-.5f)<.01f,"A traffic plan records the blocking car and its centre");
        var first=planner.Path(0);
        planner.Commit(0,100);
        Check(planner.Path(0).endOffset==first.endOffset,"No committed side leaves the ranking unchanged");
        for(int side=-1;side<=1;side+=2) {
            planner.PrepareTraffic(in o,default,false);planner.Commit(side,100);
            Check((planner.Path(0).endOffset-planner.BlockerCentre)*side>=-.5f,"Committed to side "+side+": the top candidate does not end on the other side of the blocker");
        }
        var clear=new DriverObservation(2,1f/120,self,new[]{self},road,CarTuning.Club,1,0,false);
        planner.Prepare(in clear,default,false);var clearFirst=planner.Path(0);planner.Commit(1,100);
        Check(!planner.TrafficPlan&&planner.Blocker==-1&&planner.Path(0).endOffset==clearFirst.endOffset,"Clear-road plans ignore commitment");
        return checks;
    }
}
