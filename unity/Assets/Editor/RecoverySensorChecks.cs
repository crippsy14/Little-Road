using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RecoverySensorChecks
{
    public static void Verify(){foreach(var check in Run())Debug.Log("RECOVERY_CHECK_OK "+check);}
    sealed class Straight:IDriverRoute
    {
        public float Length=>1000;
        public Vector3 At(float metres)=>new Vector3(10000,0,metres);
        public Vector3 Forward(float metres)=>Vector3.forward;
        public Vector3 Right(float metres)=>Vector3.right;
    }
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Recovery sensor check: "+name);checks.Add(name);}
        var route=new Straight();var root=new GameObject("Recovery sensor test");var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
        RecoverySensor sensor=null;
        try {
            var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
            var shape=root.AddComponent<BoxCollider>();shape.size=new Vector3(2.32f,1.64f,3.18f);shape.center=Vector3.up*.82f;
            sensor=new RecoverySensor(body,shape);obstacle.SetActive(false);
            var rotation=Quaternion.Euler(0,-45,0);var position=route.At(0)+Vector3.right*10.5f;
            root.transform.SetPositionAndRotation(position,rotation);Physics.SyncTransforms();
            var self=new ObservedCar(0,position,Vector3.zero,rotation*Vector3.forward,rotation*Vector3.right,0,10.5f,0);
            var observed=new[]{self};var o=new DriverObservation(0,1f/120,self,observed,route,CarTuning.Stock,1,0,false);
            int inward=0;foreach(var option in sensor.Sample(in o,0))if(option.clear&&option.end.x<position.x-1)inward++;
            Check(inward>0,"Diagonal shoulder start retains physically clear inward recovery paths");

            position=route.At(1.4f);root.transform.SetPositionAndRotation(position,Quaternion.identity);
            obstacle.transform.SetPositionAndRotation(route.At(3.2f)+Vector3.up,Quaternion.identity);
            obstacle.transform.localScale=new Vector3(5,2,.3f);obstacle.tag="Respawn";obstacle.SetActive(true);Physics.SyncTransforms();
            self=new ObservedCar(0,position,Vector3.zero,Vector3.forward,Vector3.right,1.4f,0,0);observed[0]=self;
            o=new DriverObservation(0,1f/120,self,observed,route,CarTuning.Stock,1,0,false);
            Check(!Physics.ComputePenetration(shape,position,Quaternion.identity,obstacle.GetComponent<Collider>(),obstacle.transform.position,Quaternion.identity,out _,out _),"Wall-margin fixture begins with no physical body penetration");
            Check(sensor.FrontBlocked(in o,false,null),"Wall inside the forward safety margin remains detected");
            int reverse=0,forward=0;foreach(var option in sensor.Sample(in o,0))if(option.clear){if(option.speed<0)reverse++;else forward++;}
            Check(reverse>0&&forward==0,$"Safety-margin overlap permits escape and rejects driving deeper into the wall (reverse={reverse}, forward={forward})");
            Check(!sensor.DestinationClear(in o,position,Vector3.forward)&&sensor.DestinationClear(in o,route.At(-5),Vector3.forward),"Assisted destination rejects scenery and accepts a clear point behind it");
            obstacle.SetActive(false);Physics.SyncTransforms();
            observed=new[]{self,new ObservedCar(1,route.At(10),Vector3.back*20,Vector3.back,Vector3.left,10,0,20)};
            o=new DriverObservation(0,1f/120,self,observed,route,CarTuning.Stock,1,0,false);
            Check(!sensor.DestinationClear(in o,route.At(0),Vector3.forward),"Assisted destination rejects an approaching car before it reaches the spawn point");

            // The recovery unlock (study 228): two cars stopped nose to tail, touching. Without it the front car finds no clear
            // path (every one overlaps the car behind from its first step); with it, driving away from that car is clear.
            self=new ObservedCar(0,route.At(0),Vector3.zero,Vector3.forward,Vector3.right,0,0,0);
            observed=new[]{self,new ObservedCar(1,route.At(-3.9f),Vector3.zero,Vector3.forward,Vector3.right,-3.9f,0,0)};
            o=new DriverObservation(0,1f/120,self,observed,route,CarTuning.Stock,1,0,false);
            bool was=RecoverySensor.Unlock;int locked=0,unlocked=0;
            try{RecoverySensor.Unlock=false;foreach(var option in sensor.Sample(in o,0))if(option.clear&&option.speed>0)locked++;RecoverySensor.Unlock=true;foreach(var option in sensor.Sample(in o,0))if(option.clear&&option.speed>0)unlocked++;}
            finally{RecoverySensor.Unlock=was;}
            Check(locked==0&&unlocked>0,$"Touching cars: a move away is clear only with the recovery unlock (without {locked}, with {unlocked})");

            var waiting=new PhysicalRecovery();var blocked=new RecoveryOption[1];
            self=new ObservedCar(0,route.At(0),Vector3.zero,Vector3.forward,Vector3.right,0,0,0);observed=new[]{self};
            void WaitFor(int start,int ticks,bool moving){blocked[0]=new RecoveryOption(false,false,0,0,self.position,self.forward,moving);
                for(int i=start;i<start+ticks;i++){var observation=new DriverObservation(i*.1,.1f,self,observed,route,CarTuning.Stock,1,0,false);waiting.Observe(in observation,0,100,true);if(waiting.NeedsPlan(i*.1))waiting.Plan(in observation,0,100,blocked);}}
            WaitFor(0,400,false);Check(!waiting.AssistedNeeded,"Forty-second temporary blockage does not trigger assisted recovery");
            WaitFor(400,250,false);Check(waiting.AssistedNeeded,"Permanent static blockage reaches the separate assisted-recovery deadline");
            waiting=new PhysicalRecovery();WaitFor(0,900,true);Check(!waiting.AssistedNeeded,"Actual moving-traffic blockage can justify waiting beyond sixty seconds");
            WaitFor(900,270,false);Check(waiting.AssistedNeeded,"Moving traffic that stops clearing cannot leave a deadlock waiting forever");
            self=new ObservedCar(0,route.At(0),Vector3.forward*2,Vector3.forward,Vector3.right,0,0,2);observed[0]=self;
            var rejoined=new DriverObservation(118,.1f,self,observed,route,CarTuning.Stock,1,0,false);waiting.Observe(in rejoined,0,100,false);
            Check(!waiting.Active&&!waiting.AssistedNeeded&&waiting.AssistedReason==null,"A physical rejoin cancels a pending fallback instead of resetting a recovered car");
        } finally {sensor?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(obstacle);}
        return checks;
    }
}
