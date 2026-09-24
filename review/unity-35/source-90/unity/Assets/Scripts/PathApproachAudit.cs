using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Read-only capture of real racing approaches. Replays are separate declared
    // single-car fixtures; no capture mutates the field or supplies future inputs.
    public sealed class PathApproachAudit
    {
        [Serializable] public sealed class Capture
        {
            public int id;public string bodyAsset,equipment;
            public float time,route,offset,speed,steeringState,pedal,steering;
            public Vector3 position,velocity,angularVelocity,colliderCentre,colliderHalf;
            public Quaternion rotation;public CarTuning tuning;public ContextPathState path;
            public bool active;public float leaderLimit,staticLimit;public bool safetyActive;
        }
        [Serializable] public sealed class Evidence
        {
            public string scope="Actual field states before physics, after current command selection. Frozen-path replays are counterfactual single-car witnesses, not continuations of traffic decisions.";
            public List<Capture> captures=new();
        }
        readonly int id;float next=51;readonly Evidence evidence=new();
        public PathApproachAudit(int id,int count){if(id<0||id>=count)throw new ArgumentException("Approach car must be in field.");this.id=id;}
        public void Observe(RoadCar car,float time)
        {
            if(car.driverId!=id||time<next||evidence.captures.Count>=5)return;
            if(car.contextPolicy==null)throw new InvalidOperationException("Approach capture requires context policy.");
            var shape=car.GetComponent<BoxCollider>();bool active=car.contextPolicy.TryGetPath(out var path);
            evidence.captures.Add(new Capture{id=id,bodyAsset=car.gameObject.name,equipment=car.equipment,time=time,
                route=car.pathDistance,offset=car.offset,speed=car.speed,steeringState=car.SteeringState,pedal=car.throttle,steering=car.steering,
                position=car.body.position,rotation=car.body.rotation,velocity=car.body.linearVelocity,angularVelocity=car.body.angularVelocity,
                colliderCentre=shape.center,colliderHalf=shape.size*.5f,tuning=car.tuning,path=path,active=active,
                leaderLimit=car.contextPolicy.LeaderLimit,staticLimit=car.contextPolicy.StaticLimit,safetyActive=car.contextPolicy.Safety.Active});
            next+=.5f;
        }
        public void Save(string report)=>File.WriteAllText(Path.ChangeExtension(report,"approaches.json"),JsonUtility.ToJson(evidence,true));
        static Capture replay;
        public static Capture Replay()
        {
            if(replay!=null)return replay;
            var data=JsonUtility.FromJson<Evidence>(File.ReadAllText(RaceTrial.Arg("--path-approach-file")));
            int index=int.Parse(RaceTrial.Arg("--path-approach-index","2"));
            if(index<0||index>=data.captures.Count||!data.captures[index].active)throw new ArgumentException("Replay requires a captured active path.");
            return replay=data.captures[index];
        }
    }
}
