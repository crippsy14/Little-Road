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
        [ThreadStatic] static SharedPreparation sharedPreparation;
        readonly bool sharePrepared;
        readonly ObservedCar[] frames=new ObservedCar[Cars*Frames];
        readonly int owner=System.Threading.Thread.CurrentThread.ManagedThreadId;
        public int SharedHits {get;private set;}
        public int SharedMisses {get;private set;}
        public int SharedProjectedCars {get;private set;}
        public ObservedTrafficRollout(bool sharePrepared=false){this.sharePrepared=sharePrepared;}
        int count,frameCount,selfId,controlSteps;double timestamp;float step;bool prepared;
        public void Prepare(in DriverObservation o,float seconds=2,int controlSteps=3)
        {
            if(System.Threading.Thread.CurrentThread.ManagedThreadId!=owner)throw new InvalidOperationException("Traffic rollout belongs to its main-thread owner.");
            if(controlSteps<1||seconds<=0)throw new ArgumentOutOfRangeException();
            var road=o.route as Circuit;if(road==null)throw new ArgumentException("Traffic rollout requires Circuit.");
            float dt=o.stepSeconds*controlSteps;frameCount=Mathf.RoundToInt(seconds/dt)+1;
            if(frameCount>Frames||o.traffic.Length>Cars)throw new ArgumentException("Traffic rollout capacity exceeded.");
            count=0;this.controlSteps=controlSteps;step=o.stepSeconds;timestamp=o.timestamp;selfId=o.self.id;
            if(sharePrepared) {
                var shared=sharedPreparation??=new SharedPreparation();
                bool hit=shared.Prepare(in o,road,seconds,controlSteps,frameCount);
                if(hit)SharedHits++;else SharedMisses++;
                count=shared.Copy(in o,frames,out int projected);SharedProjectedCars+=projected;
                prepared=true;return;
            }
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                Fill(road,in other,dt,frameCount,frames,count++);
            }
            prepared=true;
        }
        static void Fill(Circuit road,in ObservedCar other,float dt,int frameCount,ObservedCar[] output,int slot)
        {
            output[slot]=other;
            var position=other.position;var velocity=other.velocity;var forward=other.forward;
            for(int k=1;k<frameCount;k++) {
                var turn=Quaternion.AngleAxis(other.yawRate*Mathf.Exp(-(k-.5f)*dt/.5f)*dt*Mathf.Rad2Deg,Vector3.up);
                forward=turn*forward;velocity=turn*velocity;position+=velocity*dt;
                float route=road.Nearest(position,out float offset);
                output[k*Cars+slot]=new ObservedCar(other.id,position,velocity,forward,Vector3.Cross(Vector3.up,forward).normalized,
                    route,offset,Vector3.Dot(velocity,forward),other.halfWidth,other.halfLength,other.yawRate*Mathf.Exp(-k*dt/.5f),other.colliderCentre);
            }
        }
        // One bounded preparation per thread. The source and each caller's
        // output are copied; no caller retains views into this shared scratch.
        sealed class SharedPreparation
        {
            readonly ObservedCar[] source=new ObservedCar[Cars],projected=new ObservedCar[Cars*Frames];
            readonly bool[] ready=new bool[Cars];
            Circuit road;double timestamp;float step,seconds,dt;int controls,count,frameCount;bool valid;
            static bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
            static bool Same(Vector3 a,Vector3 b)=>Same(a.x,b.x)&&Same(a.y,b.y)&&Same(a.z,b.z);
            static bool Same(in ObservedCar a,in ObservedCar b)=>a.id==b.id&&Same(a.position,b.position)&&Same(a.velocity,b.velocity)
                &&Same(a.forward,b.forward)&&Same(a.right,b.right)&&Same(a.colliderCentre,b.colliderCentre)&&Same(a.route,b.route)
                &&Same(a.offset,b.offset)&&Same(a.speed,b.speed)&&Same(a.yawRate,b.yawRate)&&Same(a.halfWidth,b.halfWidth)&&Same(a.halfLength,b.halfLength);
            public bool Prepare(in DriverObservation o,Circuit route,float horizon,int controlSteps,int frames)
            {
                bool match=valid&&ReferenceEquals(road,route)&&timestamp==o.timestamp&&Same(step,o.stepSeconds)
                    &&Same(seconds,horizon)&&controls==controlSteps&&frameCount==frames&&count==o.traffic.Length;
                if(match)for(int i=0;i<count;i++)if(!Same(in source[i],in o.traffic[i])){match=false;break;}
                if(match)return true;
                road=route;timestamp=o.timestamp;step=o.stepSeconds;seconds=horizon;controls=controlSteps;
                dt=step*controls;frameCount=frames;count=o.traffic.Length;
                for(int i=0;i<count;i++){source[i]=o.traffic[i];ready[i]=false;}
                valid=true;return false;
            }
            public int Copy(in DriverObservation o,ObservedCar[] output,out int built)
            {
                int used=0;built=0;
                for(int i=0;i<count;i++) {
                    if(source[i].id==o.self.id)continue;
                    if(!ready[i]){Fill(road,in source[i],dt,frameCount,projected,i);ready[i]=true;built++;}
                    int slot=used++;
                    for(int k=0;k<frameCount;k++)output[k*Cars+slot]=projected[k*Cars+i];
                }
                return used;
            }
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
