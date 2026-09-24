using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in, read-only observation of every completed physics pose. This
    // circuit's road datum is flat; no grounding forces or teleports are added.
    public sealed class VerticalMotionAudit:MonoBehaviour
    {
        [Serializable] public class Sample {public int id;public float time,progress,offset,bottom,verticalSpeed;public Vector3 position;public string[] contacts;}
        [Serializable] public class Car {public int id,samples,airborneSteps;public float maxBottom,maxUpwardSpeed;public Sample peak;}
        [Serializable] public class Evidence {public string scope="120 Hz observed collider-bottom height above the flat Bayview datum, including ground contacts; not all-track or manual acceptance";public bool passed;public List<Car> cars=new();}
        RaceTrial world;readonly Evidence data=new();readonly List<BoxCollider> shapes=new();readonly List<VerticalContactProbe> probes=new();
        public void Setup(RaceTrial owner)
        {
            world=owner;
            foreach(var car in world.cars){data.cars.Add(new Car{id=car.driverId});shapes.Add(car.GetComponent<BoxCollider>());probes.Add(car.gameObject.AddComponent<VerticalContactProbe>());}
        }
        void FixedUpdate()
        {
            if(world==null||!world.running||(!world.freeRoam&&world.elapsed<1))return;
            for(int i=0;i<world.cars.Count;i++) {
                var car=world.cars[i];if(car.controlsBlocked)continue;
                var result=data.cars[i];float bottom=shapes[i].bounds.min.y,vy=car.body.linearVelocity.y;
                result.samples++;result.maxUpwardSpeed=Mathf.Max(result.maxUpwardSpeed,vy);
                if(bottom>.5f)result.airborneSteps++;
                if(result.peak==null||bottom>result.maxBottom+.005f){result.maxBottom=Mathf.Max(result.maxBottom,bottom);
                    result.peak=new Sample{id=car.driverId,time=world.elapsed,progress=car.progress,offset=car.offset,bottom=bottom,verticalSpeed=vy,position=car.body.position,contacts=probes[i].recent.ToArray()};}
                if(vy>2&&probes[i].launch==null)probes[i].launch=new Sample{id=car.driverId,time=world.elapsed,progress=car.progress,offset=car.offset,bottom=bottom,verticalSpeed=vy,position=car.body.position,contacts=probes[i].recent.ToArray()};
            }
        }
        public void Save()
        {
            if(world==null)return;string folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder))return;
            Directory.CreateDirectory(folder);data.passed=data.cars.Count>0&&data.cars.TrueForAll(c=>c.samples>0&&c.airborneSteps==0&&c.maxUpwardSpeed<2);
            string mode=world.freeRoam?"free":"race";
            File.WriteAllText(Path.Combine(folder,"vertical-"+mode+".json"),JsonUtility.ToJson(data,true));
            for(int i=0;i<probes.Count;i++)if(probes[i]!=null&&probes[i].launch!=null)File.WriteAllText(Path.Combine(folder,"launch-"+mode+"-"+i+".json"),JsonUtility.ToJson(probes[i].launch,true));
        }
        void OnApplicationQuit()=>Save();
        void OnDestroy()=>Save();
    }
    public sealed class VerticalContactProbe:MonoBehaviour
    {
        public readonly Queue<string> recent=new();public VerticalMotionAudit.Sample launch;
        void OnCollisionEnter(Collision collision)=>Record(collision);
        void OnCollisionStay(Collision collision)=>Record(collision);
        void Record(Collision collision)
        {
            if(collision.contactCount==0)return;var p=collision.GetContact(0);
            recent.Enqueue(collision.collider.name+" normal="+p.normal.ToString("F3")+" point="+p.point.ToString("F3")+" impulse="+collision.impulse.ToString("F2"));
            while(recent.Count>8)recent.Dequeue();
        }
    }
}
