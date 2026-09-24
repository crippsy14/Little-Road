using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TrafficPathChecks
{
    public static List<string> Run()
    {
        var results=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Traffic path: "+message);results.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);var right=road.Right(d);
        var self=new ObservedCar(0,road.At(d)+right*4,forward*28,forward,right,d,4,28,1.2f,2.3f);
        var other=new ObservedCar(1,road.At(d+10),forward*16,forward,right,d+10,0,16,1.2f,2.3f);
        var o=new DriverObservation(1,1f/120,self,new[]{self,other},road,CarTuning.Club,1,0,false);
        var centre=new Vector3(0,.5f,0);var half=new Vector3(1.2f,.6f,2.3f);
        var crossing=RoadPathFeasibility.FromMotion(in o,-4,false);
        var prediction=new ContextPathPrediction(profile);prediction.Predict(in o,crossing,centre,half,2,controlSteps:3);
        var check=new TrafficPathFeasibility();
        Check(check.Conflict(in o,prediction,centre,half,out int id,out float time)&&id==1&&time>0&&time<=TrafficPathFeasibility.Horizon+.05f,
            "Physical outside-to-inside approach predicts the slower opponent crossing conflict");
        var hold=RoadPathFeasibility.FromMotion(in o,4,false);prediction.Predict(in o,hold,centre,half,2,controlSteps:3);
        Check(!check.Conflict(in o,prediction,centre,half,out _,out _),"Maintaining the open outside corridor permits forward passing");
        var distant=new ObservedCar(1,other.position+forward*80,other.velocity,forward,right,d+90,0,16,1.2f,2.3f);
        var far=new DriverObservation(2,1f/120,self,new[]{self,distant},road,CarTuning.Club,1,0,false);
        prediction.Predict(in far,crossing,centre,half,2,controlSteps:3);
        Check(!check.Conflict(in far,prediction,centre,half,out _,out _),"A distant opponent does not prohibit the same lateral route");
        var same=new ObservedCar(1,other.position+right*4,other.velocity,forward,right,d+10,4,16,1.2f,2.3f);
        var following=new DriverObservation(3,1f/120,self,new[]{self,same},road,CarTuning.Club,1,0,false);
        prediction.Predict(in following,hold,centre,half,2,controlSteps:3);
        Check(!check.Conflict(in following,prediction,centre,half,out _,out _),"Same-corridor following remains owned by the existing longitudinal response");
        return results;
    }
}
