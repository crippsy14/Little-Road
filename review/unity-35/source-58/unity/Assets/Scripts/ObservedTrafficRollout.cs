using System;
using UnityEngine;

namespace LittleRoad
{
    // Owned, bounded traffic observations for main-thread progress comparisons.
    // Constant observed velocity with fading observed yaw, never rival inputs.
    // Both compared paths consume the same prediction; no later live reads.
    public sealed class ObservedTrafficRollout
    {
        const int Cars=24,Frames=81;
        readonly ObservedCar[] frames=new ObservedCar[Cars*Frames];
        readonly int owner=System.Threading.Thread.CurrentThread.ManagedThreadId;
        int count,frameCount,selfId,controlSteps;double timestamp;float step;bool prepared;
        public void Prepare(in DriverObservation o,float seconds=2,int controlSteps=3)
        {
            if(System.Threading.Thread.CurrentThread.ManagedThreadId!=owner)throw new InvalidOperationException("Traffic rollout belongs to its main-thread owner.");
            if(controlSteps<1||seconds<=0)throw new ArgumentOutOfRangeException();
            var road=o.route as Circuit;if(road==null)throw new ArgumentException("Traffic rollout requires Circuit.");
            float dt=o.stepSeconds*controlSteps;frameCount=Mathf.RoundToInt(seconds/dt)+1;
            if(frameCount>Frames||o.traffic.Length>Cars)throw new ArgumentException("Traffic rollout capacity exceeded.");
            count=0;this.controlSteps=controlSteps;step=o.stepSeconds;timestamp=o.timestamp;selfId=o.self.id;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                int j=count++;frames[j]=other;
                var position=other.position;var velocity=other.velocity;var forward=other.forward;
                for(int k=1;k<frameCount;k++) {
                    var turn=Quaternion.AngleAxis(other.yawRate*Mathf.Exp(-(k-.5f)*dt/.5f)*dt*Mathf.Rad2Deg,Vector3.up);
                    forward=turn*forward;velocity=turn*velocity;position+=velocity*dt;
                    float route=road.Nearest(position,out float offset);
                    frames[k*Cars+j]=new ObservedCar(other.id,position,velocity,forward,Vector3.Cross(Vector3.up,forward).normalized,
                        route,offset,Vector3.Dot(velocity,forward),other.halfWidth,other.halfLength,other.yawRate*Mathf.Exp(-k*dt/.5f),other.colliderCentre);
                }
            }
            prepared=true;
        }
        public void Validate(in DriverObservation o,int requestedControlSteps,int physicsSteps)
        {
            if(!prepared||System.Threading.Thread.CurrentThread.ManagedThreadId!=owner||o.timestamp!=timestamp||o.self.id!=selfId||o.stepSeconds!=step||requestedControlSteps!=controlSteps||physicsSteps/controlSteps>=frameCount)
                throw new InvalidOperationException("Stale, mismatched or foreign traffic rollout.");
        }
        public ReadOnlySpan<ObservedCar> At(int controlFrame)
        {
            if(!prepared||controlFrame<0||controlFrame>=frameCount)throw new ArgumentOutOfRangeException(nameof(controlFrame));
            return new ReadOnlySpan<ObservedCar>(frames,controlFrame*Cars,count);
        }
    }
}
