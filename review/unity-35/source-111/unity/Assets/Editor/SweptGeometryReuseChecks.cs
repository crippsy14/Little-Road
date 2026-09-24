using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class SweptGeometryReuseChecks
{
    // The oracle is the frozen108 implementation, copied before this optimization.
    public static List<string> Run()
    {
        var random=new System.Random(1091709);
        float Value(float scale)=>(float)(random.NextDouble()*2-1)*scale;
        Vector3 Vector(float scale)=>new(Value(scale),Value(scale)*.05f,Value(scale));
        int hits=0,misses=0;
        for(int i=0;i<40000;i++) {
            var a=Vector(2000);var b=a+Vector(i%3==0?3:30);
            var af=Quaternion.Euler(Value(12),Value(180),0)*Vector3.forward;
            var bf=Quaternion.Euler(Value(12),Value(180),0)*Vector3.forward;
            if(i%11==0)bf=af;
            if(i%37==0)af=Vector3.zero;
            if(i%41==0)bf=Vector3.up;
            var da=i%7==0?Vector3.zero:Vector(40);var db=i%5==0?da:Vector(40);
            float aw=Mathf.Abs(Value(2)),al=Mathf.Abs(Value(4)),bw=Mathf.Abs(Value(2)),bl=Mathf.Abs(Value(4)),padding=Mathf.Abs(Value(.4f));
            if(i%17==0){aw=bw=1;al=bl=2;af=bf=Vector3.forward;b=a+Vector3.right*2;da=db=Vector3.zero;padding=0;}
            bool expected=Safety108Oracle.SweptConflict(a,af,aw,al,da,b,bf,bw,bl,db,padding,out float oldTime);
            bool actual=ImmediateTrafficSafety.SweptConflict(a,af,aw,al,da,b,bf,bw,bl,db,padding,out float newTime);
            if(actual!=expected||BitConverter.SingleToInt32Bits(oldTime)!=BitConverter.SingleToInt32Bits(newTime))
                throw new Exception("Swept geometry differs from108 at query "+i);
            if(actual)hits++;else misses++;
        }
        if(hits<1000||misses<1000)throw new Exception("Insufficient sweep outcome coverage");
        bool Query()=>ImmediateTrafficSafety.SweptConflict(Vector3.zero,Vector3.forward,1,2,Vector3.forward*10,
            Vector3.forward*8,Vector3.forward,1,2,Vector3.zero,.1f,out _);
        for(int i=0;i<100;i++)Query();
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++)if(!Query())throw new Exception("Warm sweep unexpectedly missed");
        if(GC.GetAllocatedBytesForCurrentThread()!=before)throw new Exception("Sweep allocated after warmup");
        return new List<string>{"40000 varied swept collisions match frozen108 boolean and fraction bits exactly",
            "Reused swept axes allocate zero bytes over10000 warm queries"};
    }
}
