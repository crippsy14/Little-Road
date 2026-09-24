using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DriverPolicyChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Driver policy check: "+name);checks.Add(name);}
        var invalid=new DriveCommand(float.NaN,float.PositiveInfinity,0,0,0);var limits=new DriveCommand(2,-2,0,0,0);
        Check(invalid.pedal==0&&invalid.steering==0&&limits.pedal==1&&limits.steering==-1,"AI command boundary rejects nonfinite input and clamps physical controls");
        var route=new Circuit();var traffic=new ObservedCar[2];
        var a=new LegacyDriverPolicy(new System.Random(1709));var b=new LegacyDriverPolicy(new System.Random(1709));
        bool exact=true,finite=true;float braking=0,turning=0;
        for(int i=0;i<1200;i++) {
            float distance=i*.55f;var forward=route.Forward(distance);var right=route.Right(distance);
            traffic[0]=new ObservedCar(0,route.At(distance),forward*18,forward,right,distance,0,18);
            traffic[1]=new ObservedCar(1,route.At(distance+8),route.Forward(distance+8)*12,route.Forward(distance+8),route.Right(distance+8),distance+8,0,12);
            var o=new DriverObservation(i/120.0,1f/120,traffic[0],traffic,route,CarTuning.Stock,.98f,.3f,false);
            var x=a.Decide(in o);var y=b.Decide(in o);
            exact&=x.pedal==y.pedal&&x.steering==y.steering&&x.targetSpeed==y.targetSpeed&&x.chosenLane==y.chosenLane;
            finite&=!float.IsNaN(x.pedal)&&!float.IsNaN(x.steering)&&Mathf.Abs(x.pedal)<=1&&Mathf.Abs(x.steering)<=1;
            braking=Mathf.Min(braking,x.pedal);turning=Mathf.Max(turning,Mathf.Abs(x.steering));
        }
        Check(exact&&finite,"Frozen timestamped traffic observations replay exactly with the same seed");
        Check(braking<-.5f&&turning>.1f,"Policy replay exercises braking and steering in traffic, not only a stationary input");
        return checks;
    }
}
