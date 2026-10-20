using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// --rejoin-at-speed (study 126): a car just past the 7.2 m edge trigger at speed steers back
// towards the tarmac while slowing; a growing excursion still falls back to the full stop.
public static class RecoveryRejoinChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Recovery rejoin check failed: "+name);checks.Add(name);}
        Check(!PhysicalRecovery.RejoinAtSpeed,"Rejoin at speed is off unless --rejoin-at-speed");
        var road=new Circuit();var tuning=CarTuning.Stock;var traffic=new ObservedCar[1];
        DriverObservation At(float route,float offset,float speed,float yaw)
        {
            var forward=Quaternion.AngleAxis(yaw,Vector3.up)*road.Forward(route);var right=Vector3.Cross(Vector3.up,forward).normalized;
            var p=road.At(route)+road.Right(route)*offset;traffic[0]=new ObservedCar(0,p,forward*speed,forward,right,route,offset,speed);
            return new DriverObservation(1,1f/120,traffic[0],traffic,road,tuning,1,0,false);
        }
        // (a straight to test on: the new Bayview's loop, the default since 20 Oct, bends at 100 m where the old one ran straight)
        float S=100;for(float s=20;s<road.length-20;s+=2)if(Vector3.Dot(road.Forward(s-12),road.Forward(s+12))>.9995f&&Vector3.Dot(road.Forward(s),road.Forward(s+6))>.9995f){S=s;break;}
        PhysicalRecovery.RejoinAtSpeed=true;
        try {
            var recovery=new PhysicalRecovery();
            var wide=At(S,7.6f,30,0);recovery.Observe(in wide,S,120,false);
            Check(recovery.State=="Steer back"&&!recovery.NeedsPlan(2),"A car 7.6 m out at 30 m/s steers back instead of stopping");
            var command=recovery.Command(in wide);
            Check(command.steering<0&&command.pedal<0&&command.targetSpeed>=12,"Steer back turns towards the road (left from the right edge) and slows, not stops");
            var left=new PhysicalRecovery();var mirrored=At(S,-7.6f,30,0);left.Observe(in mirrored,S,120,false);
            Check(left.State=="Steer back"&&left.Command(in mirrored).steering>0,"Mirrored excursion steers right");
            var worse=At(S+1,10.8f,28,0);recovery.Observe(in worse,S+1,120,false);
            Check(recovery.State=="Stabilise","A growing excursion falls back to the full stop");
            var back=new PhysicalRecovery();var o=At(S,7.6f,30,0);back.Observe(in o,S,120,false);o=At(S+4,4.8f,22,0);back.Observe(in o,S+4,120,false);
            Check(back.State=="Racing"&&back.completed==1,"Back inside 5.5 m and pointing up the road resumes racing");
            var slow=new PhysicalRecovery();var crawl=At(S,7.6f,4,0);slow.Observe(in crawl,S,120,false);
            Check(slow.State=="Stabilise","Slow or blocked cars keep the existing stop-and-plan recovery");
        } finally {PhysicalRecovery.RejoinAtSpeed=false;}
        return checks;
    }
}
