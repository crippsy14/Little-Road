using System;
using System.Collections.Generic;
using System.Threading;
using LittleRoad;
using UnityEngine;

public static class SharedTrafficPreparationChecks
{
    static bool Equal(ObservedTrafficRollout a,ObservedTrafficRollout b,int frames)
    {
        for(int k=0;k<frames;k++) {
            var x=a.At(k);var y=b.At(k);if(x.Length!=y.Length)return false;
            for(int j=0;j<x.Length;j++)if(!x[j].Equals(y[j]))return false;
        }
        return true;
    }
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string text){if(!ok)throw new Exception("Shared traffic: "+text);checks.Add(text);}
        var road=new TechnicalCircuit().track;float d=200;
        ObservedCar Car(int id,float route,float speed)=>new ObservedCar(id,road.At(route),road.Forward(route)*speed,
            road.Forward(route),road.Right(route),route,0,speed,1.2f,2.3f,.01f,new Vector3(.02f,.5f,.03f));
        var cars=new[]{Car(0,d,18),Car(1,d+12,12)};
        var o0=new DriverObservation(10,1f/120,cars[0],cars,road,CarTuning.Club,1,0,false);
        var o1=new DriverObservation(10,1f/120,cars[1],cars,road,CarTuning.Club,1,0,false);
        var a=new ObservedTrafficRollout(true);var b=new ObservedTrafficRollout(true);
        var serialA=new ObservedTrafficRollout();var serialB=new ObservedTrafficRollout();
        a.Prepare(in o0);serialA.Prepare(in o0);b.Prepare(in o1);serialB.Prepare(in o1);
        Check(a.SharedMisses==1&&b.SharedHits==1&&Equal(a,serialA,81)&&Equal(b,serialB,81),"Different owners reuse one exact snapshot and reproduce every serial projected state");
        Check(a.At(0).Length==1&&a.At(0)[0].id==1&&b.At(0)[0].id==0,"Each owned output excludes its own driver and preserves source ordering");
        var retained=a.At(80)[0];var saved=cars[1];
        cars[1]=Car(1,d+12,13);b.Prepare(in o1);
        Check(b.SharedMisses==1&&a.At(80)[0].Equals(retained),"Mutated source invalidates shared preparation without overwriting another caller's owned output");
        a.Validate(in o0,3,240);
        cars[1]=saved;
        // Each input dimension is changed at the same logical decision so
        // timestamp-only cache keys cannot pass this test.
        for(int change=0;change<19;change++) {
            a.Prepare(in o0);
            var original=cars[1];var altered=new ObservedCar(change==7?2:original.id,
                original.position+(change==8?Vector3.right*.01f:Vector3.zero),
                original.velocity+(change==9?Vector3.forward*.01f:Vector3.zero),
                original.forward+(change==10?Vector3.right*.01f:Vector3.zero),
                original.right+(change==11?Vector3.forward*.01f:Vector3.zero),
                original.route+(change==13?.01f:0),original.offset+(change==14?.01f:0),original.speed+(change==15?.01f:0),
                original.halfWidth+(change==17?.01f:0),original.halfLength+(change==18?.01f:0),original.yawRate+(change==16?.01f:0),
                original.colliderCentre+(change==12?Vector3.right*.01f:Vector3.zero));
            var input=change==5?new[]{altered,cars[0]}:change==6?new[]{altered}:new[]{cars[0],altered};
            var route=change==4?new TechnicalCircuit().track:road;
            float step=change==1?.01f:1f/120,seconds=change==3?1.5f:2;int controls=change==2?4:3;
            var changed=new DriverObservation(change==0?11:10,step,cars[0],input,route,CarTuning.Club,1,0,false);
            var shared=new ObservedTrafficRollout(true);var serial=new ObservedTrafficRollout();
            shared.Prepare(in changed,seconds,controls);serial.Prepare(in changed,seconds,controls);
            int count=Mathf.RoundToInt(seconds/(step*controls))+1;
            Check(shared.SharedMisses==1&&Equal(shared,serial,count),"Changed snapshot dimension "+change+" misses cache and matches serial output");
        }
        a.Prepare(in o0);b.Prepare(in o1);
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<4;i++){a.Prepare(in o0);b.Prepare(in o1);}
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"Warm exact preparation hits allocate no managed bytes");
        bool rejected=false;
        var thread=new Thread(()=>{
            var foreign=new DriverObservation(10,1f/120,cars[0],cars,road,CarTuning.Club,1,0,false);
            try{a.Prepare(in foreign);}catch(InvalidOperationException){rejected=true;}
        });thread.Start();thread.Join();
        Check(rejected,"Another thread cannot prepare a caller-owned rollout");
        return checks;
    }
}
