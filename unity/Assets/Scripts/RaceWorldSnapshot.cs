using System;
using UnityEngine;

namespace LittleRoad
{
    // Before RaceTrial/RoadCar FixedUpdate, after fixture setup. Stable ID order;
    // no policy sees another car's decisions or mixed-age controller caches.
    [DefaultExecutionOrder(-50)]
    public sealed class RaceWorldSnapshot:MonoBehaviour
    {
        readonly ObservedCar[] entries=new ObservedCar[24];
        RaceTrial race;int count;
        public double Timestamp {get;private set;}
        public ReadOnlySpan<ObservedCar> Entries=>new ReadOnlySpan<ObservedCar>(entries,0,count);
        public void Setup(RaceTrial owner)
        {
            race=owner;var seen=new bool[entries.Length];
            foreach(var car in race.cars){if(car.driverId<0||car.driverId>=race.cars.Count||car.driverId>=entries.Length||seen[car.driverId])throw new InvalidOperationException("Snapshot needs unique contiguous driver IDs.");seen[car.driverId]=true;}
            Capture();
        }
        void FixedUpdate(){if(race!=null&&race.running)Capture();}
        void Capture()
        {
            var stamp=race.DriverAudit!=null?DriverFrameAudit.Begin():default;
            count=race.cars.Count;if(count>entries.Length)throw new InvalidOperationException("Snapshot capacity exceeded.");
            Timestamp=Time.fixedTimeAsDouble;
            foreach(var car in race.cars) {
                float distance=race.track.Nearest(car.body.position,out float offset);
                entries[car.driverId]=new ObservedCar(car.driverId,car.body.position,car.body.linearVelocity,car.transform.forward,car.transform.right,
                    distance,offset,Vector3.Dot(car.body.linearVelocity,car.DriveForward),car.CollisionHalf.x,car.CollisionHalf.z,car.body.angularVelocity.y,car.CollisionCentre);
            }
            race.DriverAudit?.Record(stamp,true);
        }
    }
}
