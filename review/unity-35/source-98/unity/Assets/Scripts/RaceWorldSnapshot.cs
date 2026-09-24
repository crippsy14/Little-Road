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
        ForecastWorkerPool forecasts;
        readonly ForecastWork[] jobs=new ForecastWork[48];
        public double Timestamp {get;private set;}
        public ReadOnlySpan<ObservedCar> Entries=>new ReadOnlySpan<ObservedCar>(entries,0,count);
        public void Setup(RaceTrial owner)
        {
            race=owner;var seen=new bool[entries.Length];
            foreach(var car in race.cars){if(car.driverId<0||car.driverId>=race.cars.Count||car.driverId>=entries.Length||seen[car.driverId])throw new InvalidOperationException("Snapshot needs unique contiguous driver IDs.");seen[car.driverId]=true;}
            if(RaceTrial.Has("--parallel-road-forecasts")) {
                if(race.DriverPolicyId!="context-v1"||!RaceTrial.Has("--road-feasibility"))throw new InvalidOperationException("Parallel road forecasts require context road feasibility.");
                int workers=int.Parse(RaceTrial.Arg("--parallel-forecast-workers","8"));
                forecasts=new ForecastWorkerPool(workers);
            }
            Capture();
        }
        void OnDestroy(){forecasts?.Dispose();}
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
            // Same observation and physics step as the original decisions.
            // Join before any RoadCar update; no scene objects enter a worker.
            if(forecasts!=null&&race.running) {
                int jobCount=0;
                foreach(var car in race.cars) {
                    var job=car.PrepareRoadForecast(this);
                    if(job!=null){jobs[jobCount++]=job;var continuation=car.PreparedRoadContinuation;if(continuation!=null)jobs[jobCount++]=continuation;}
                }
                if(jobCount>0) {
                    forecasts.Run(jobs,jobCount);
                    race.DriverAudit?.RecordForecastBatch(forecasts,jobs,jobCount);
                    Array.Clear(jobs,0,jobCount);
                }
            }
            race.DriverAudit?.Record(stamp,true);
        }
    }
}
