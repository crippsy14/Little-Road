using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class PassingTargetChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new Exception("Pass completion: "+text);checks.Add(text);}
        var road=new TechnicalCircuit().track;float d=200;
        ObservedCar Car(int id,float route,float offset,float speed)=>new ObservedCar(id,road.At(route)+road.Right(route)*offset,
            road.Forward(route)*speed,road.Forward(route),road.Right(route),route,offset,speed,1.16f,1.6f);
        var self=Car(0,d,5.5f,27);var other=Car(1,d+15,3.3f,24);
        var o=new DriverObservation(1,1f/120,self,new[]{other},road,CarTuning.Club,1,0,false);
        Check(PassingTargetContinuation.ShouldCompare(in o,5.5f,6.29f,34),"Sub-body-width return into slower car corridor requests a comparison");
        Check(!PassingTargetContinuation.ShouldCompare(in o,6.1f,6.29f,34),"Two targets still clear of leader do not trigger extra work");
        Check(!PassingTargetContinuation.ShouldCompare(in o,5.5f,5.7f,34),"Two blocked targets do not qualify as completing a pass");
        Check(!PassingTargetContinuation.ShouldCompare(in o,6.29f,5.5f,34),"Moving out to pass does not preserve a blocked previous target");
        Check(!PassingTargetContinuation.ShouldCompare(in o,5.5f,6.29f,24),"No equipment pace advantage does not trigger this comparison");
        other=Car(1,d-10,3.3f,24);var rear=new DriverObservation(2,1f/120,self,new[]{other},road,CarTuning.Club,1,0,false);
        Check(!PassingTargetContinuation.ShouldCompare(in rear,5.5f,6.29f,34),"Already-passed rear car cannot pin the passing target");
        other=Car(1,d+101,3.3f,24);var distant=new DriverObservation(3,1f/120,self,new[]{other},road,CarTuning.Club,1,0,false);
        Check(!PassingTargetContinuation.ShouldCompare(in distant,5.5f,6.29f,34),"Distant car outside following envelope does not trigger work");
        return checks;
    }
}
