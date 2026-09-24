using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Read-only diagnostics of the previous completed physics step. Contact
    // manifold separation is retained separately in RoadCar's collision data.
    public sealed class CollisionAudit : MonoBehaviour
    {
        [Serializable] public class Peak
        {
            public int car,other;
            public string objectName;
            public float time,depth;
            public Vector3 position,otherPosition;
        }
        public int sampledSteps,querySaturations;
        public float maxCarOverlap,maxEnvironmentOverlap;
        public readonly List<Peak> peaks=new();
        readonly Collider[] nearby=new Collider[64];
        readonly Dictionary<int,float> carPeaks=new();
        RaceTrial race;
        public void Setup(RaceTrial owner){race=owner;}

        void FixedUpdate()
        {
            if(race==null||!race.running||race.elapsed<0)return;
            sampledSteps++;
            foreach(var car in race.cars) {
                var shape=car.GetComponent<BoxCollider>();
                Vector3 position=car.body.position;Quaternion rotation=car.body.rotation;
                Vector3 centre=position+rotation*shape.center;
                int count=Physics.OverlapBoxNonAlloc(centre,shape.size*.5f+Vector3.one*.05f,nearby,rotation,~0,QueryTriggerInteraction.Ignore);
                if(count==nearby.Length)querySaturations++;
                for(int i=0;i<count;i++) {
                    var obstacle=nearby[i];
                    if(obstacle.attachedRigidbody==car.body)continue;
                    var other=obstacle.GetComponent<RoadCar>();
                    if(other==null&&!obstacle.CompareTag("Respawn"))continue;
                    if(other!=null&&other.driverId<car.driverId)continue;
                    Vector3 otherPosition=other==null?obstacle.transform.position:other.body.position;
                    Quaternion otherRotation=other==null?obstacle.transform.rotation:other.body.rotation;
                    if(!Physics.ComputePenetration(shape,position,rotation,obstacle,otherPosition,otherRotation,out _,out float depth))continue;
                    if(other==null)maxEnvironmentOverlap=Mathf.Max(maxEnvironmentOverlap,depth);
                    else maxCarOverlap=Mathf.Max(maxCarOverlap,depth);
                    int key=car.driverId*2+(other==null?1:0);
                    carPeaks.TryGetValue(key,out float previous);
                    if(depth>previous+.001f) {
                        carPeaks[key]=depth;
                        peaks.Add(new Peak{car=car.driverId,other=other==null?-1:other.driverId,objectName=obstacle.name,
                            time=race.elapsed,depth=depth,position=position,otherPosition=otherPosition});
                    }
                }
            }
        }
    }
}
