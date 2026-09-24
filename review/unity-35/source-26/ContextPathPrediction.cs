using System;
using UnityEngine;

namespace LittleRoad
{
    // Forecast a declared path using generated follower commands, never later
    // recorded inputs. Observation-only until path/road acceptance is measured.
    public sealed class ContextPathPrediction
    {
        [Serializable] public struct Cost {public double totalMs,projectionMs,followerMs,bodyMs,physicsMs;}
        public Cost LastCost {get;private set;}
        public struct Sample
        {
            public PlanarDrivePrediction state;public DriveCommand command;
            public float time,bodyOffset,route;
        }
        public readonly Sample[] samples=new Sample[361];
        public int Count {get;private set;}
        readonly ContextPathFollower follower;
        public readonly ForecastRoadEnvelope envelope=new ForecastRoadEnvelope();
        public ContextPathPrediction(RacingSpeedProfile profile){follower=new ContextPathFollower(profile);}
        static long Stamp()=>System.Diagnostics.Stopwatch.GetTimestamp();
        static double Ms(long start)=>(Stamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        public void Predict(in DriverObservation initial,ContextPathState path,Vector3 centre,Vector3 half,float seconds,bool measure=false,int controlSteps=1,bool adaptiveEnvelope=false,float roadAllowance=0)
        {
            if(controlSteps<1||controlSteps>6)throw new ArgumentOutOfRangeException(nameof(controlSteps));
            if(roadAllowance<0||roadAllowance>=Circuit.Width*.5f)throw new ArgumentOutOfRangeException(nameof(roadAllowance));
            long total=measure?Stamp():0;var cost=new Cost();
            var road=initial.route as Circuit;if(road==null)throw new ArgumentException("Physical path forecast requires Circuit projection.");
            float dt=initial.stepSeconds;int steps=Mathf.RoundToInt(seconds/dt);
            if(steps+1>samples.Length)throw new ArgumentException("Path forecast capacity exceeded.");
            var model=new PlanarDrivePrediction(initial.self.position,initial.self.velocity,initial.self.forward,initial.self.yawRate,initial.ownSteeringState);
            float lastRoute=initial.self.route;Count=steps+1;DriveCommand command=default;
            for(int i=0;i<=steps;i++) {
                long stamp=measure?Stamp():0;
                float route=road.Nearest(model.position,out float offset);
                if(measure)cost.projectionMs+=Ms(stamp);
                path.consumed+=Mathf.Max(0,Mathf.Repeat(route-lastRoute+road.Length*.5f,road.Length)-road.Length*.5f);lastRoute=route;
                // Only forecast controls are held. Projection, body checks and physical
                // integration retain the native step; the real follower is unchanged.
                if(i%controlSteps==0) {
                var self=new ObservedCar(initial.self.id,model.position,model.velocity,model.forward,Vector3.Cross(Vector3.up,model.forward),route,offset,
                    Vector3.Dot(model.velocity,model.forward),half.x,half.z,model.yawRate);
                var o=new DriverObservation(initial.timestamp+i*dt,dt,self,Array.Empty<ObservedCar>(),road,initial.tuning,1,0,false,model.steering);
                if(measure)stamp=Stamp();command=follower.Follow(in o,in path);
                if(measure)cost.followerMs+=Ms(stamp);
                }
                if(measure)stamp=Stamp();
                // Adaptive mode publishes interval bounds separately. Fail closed
                // if a caller mistakenly consumes an unavailable dense offset.
                float bodyOffset=adaptiveEnvelope?float.PositiveInfinity:model.MaxBodyOffset(road,centre,half);
                if(measure)cost.bodyMs+=Ms(stamp);
                samples[i]=new Sample{state=model,command=command,time=i*dt,route=route,bodyOffset=bodyOffset};
                if(i<steps){if(measure)stamp=Stamp();model.Step(initial.tuning,command.pedal,command.steering,dt);if(measure)cost.physicsMs+=Ms(stamp);}
            }
            if(adaptiveEnvelope) {
                long stamp=measure?Stamp():0;
                envelope.Evaluate(road,samples,Count,centre,half,Circuit.Width*.5f-roadAllowance);
                if(measure)cost.bodyMs+=Ms(stamp);
            }
            if(measure)cost.totalMs=Ms(total);LastCost=cost;
        }
    }
}
