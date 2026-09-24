using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RollingRecoveryChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool yes,string text){if(!yes)throw new Exception("Rolling recovery: "+text);checks.Add(text);}
        var road=new Circuit(new[]{new Vector3(10000,0,-200),new Vector3(10000,0,200),new Vector3(10500,0,200),new Vector3(10500,0,-200)});
        var root=new GameObject("Rolling test car");var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
        RecoverySensor sensor=null;
        try {
            var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
            var shape=root.AddComponent<BoxCollider>();shape.size=new Vector3(2.4f,1.6f,4);shape.center=new Vector3(0,.8f,0);
            sensor=new RecoverySensor(body,shape);
            floor.name="Authored rolling test support";floor.transform.position=new Vector3(10000,-.5f,0);floor.transform.localScale=new Vector3(100,1,200);floor.AddComponent<RoadSupport>();
            obstacle.SetActive(false);Physics.SyncTransforms();
            ObservedCar Car(float side,float speed=26,float angle=0,int id=0,float route=200){var f=Quaternion.Euler(0,angle,0)*road.Forward(route);return new ObservedCar(id,road.At(route)+road.Right(route)*side+Vector3.up*.15f,f*speed,f,Vector3.Cross(Vector3.up,f),route,side,speed,1.2f,2,0,shape.center);}
            var rolling=new RollingRecovery();var traffic=Array.Empty<ObservedCar>();var tuning=CarTuning.Club;
            foreach(float side in new[]{-7.4f,7.4f,8.5f}) {
                var self=Car(side);var o=new DriverObservation(10,1f/120,self,traffic,road,tuning,1,0,false);
                root.transform.position=self.position;Physics.SyncTransforms();
                Check(rolling.TryPlan(in o,200,500,sensor),"Supported shoulder return is feasible from offset "+side+" ("+rolling.Rejection+")");
                var command=rolling.Command(in o);
                Check(command.pedal<=0&&command.steering*side<0&&Mathf.Abs(rolling.PredictedEndOffset)<Mathf.Abs(side)-1,"Rolling return uses coast/brake and steers inward from "+side);
            }
            var own=Car(7.4f);var observation=new DriverObservation(20,1f/120,own,traffic,road,tuning,1,0,false);
            Check(!rolling.TryPlan(in observation,501,500,sensor),"A missed checkpoint cannot be bypassed by rolling recovery");
            foreach(var self in new[]{Car(7.4f,26,90),Car(7.4f,0),Car(10.5f)}) {
                var o=new DriverObservation(20,1f/120,self,traffic,road,tuning,1,0,false);
                Check(!rolling.TryPlan(in o,200,500,sensor),"Wrong-way stationary or deep-departure pose keeps stopped recovery");
            }
            floor.GetComponent<Collider>().enabled=false;Physics.SyncTransforms();
            Check(!rolling.TryPlan(in observation,200,500,sensor),"Missing physical support rejects a rolling return");
            floor.GetComponent<Collider>().enabled=true;
            obstacle.transform.position=road.At(230)+road.Right(230)*5.5f+Vector3.up;obstacle.transform.localScale=new Vector3(6,2,2);obstacle.tag="Respawn";obstacle.SetActive(true);Physics.SyncTransforms();
            Check(!rolling.TryPlan(in observation,200,500,sensor),"Swept scenery blocks the moving return");
            obstacle.SetActive(false);Physics.SyncTransforms();
            foreach(var rival in new[]{Car(5.5f,0,0,1,228),Car(5.5f,36,0,1,190)}) {
                var o=new DriverObservation(20,1f/120,own,new[]{rival},road,tuning,1,0,false);
                Check(!rolling.TryPlan(in o,200,500,sensor),"Stationary or fast approaching rear traffic blocks the merging return");
            }
            var recovery=new PhysicalRecovery();recovery.Observe(in observation,200,500,false);
            Check(recovery.CanRoll,"An earned-gate shoulder episode can consider a rolling plan");recovery.PlanRolling(20);
            Check(recovery.State=="Rolling rejoin"&&!recovery.NeedsPlan(20.05),"Accepted rolling plan uses the bounded recovery planning clock");
            recovery=new PhysicalRecovery();recovery.Observe(in observation,501,500,false);
            Check(!recovery.CanRoll,"Missed-gate recovery retains its return-behind-gate requirement");
            for(int i=0;i<3;i++)rolling.TryPlan(in observation,200,500,sensor);
            long before=GC.GetAllocatedBytesForCurrentThread();bool accepted=true;
            for(int i=0;i<5;i++)accepted&=rolling.TryPlan(in observation,200,500,sensor);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Check(accepted&&allocated==0,"Warm rolling prediction and physical scene queries allocate zero managed bytes");
        } finally {sensor?.Dispose();UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(floor);UnityEngine.Object.DestroyImmediate(obstacle);}
        return checks;
    }
}
