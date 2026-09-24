using System;
using UnityEngine;

namespace LittleRoad
{
    // Short-horizon side-entry check, not a general prediction of opponent intent.
    // Own path uses the shared physical model. Opponents use observed velocity
    // and fading observed yaw; no future inputs or performance bonuses are read.
    public sealed class TrafficPathFeasibility
    {
        public const int Stride=6;
        public const float Horizon=1.25f;
        const int Capacity=24,Steps=61;
        readonly ObservedCar[] cars=new ObservedCar[Capacity];
        readonly Vector3[,] positions=new Vector3[Capacity,Steps],headings=new Vector3[Capacity,Steps];
        readonly float[,] angularPadding=new float[Capacity,Steps];
        int count,steps;double stamp=double.NaN;
        static Vector3 Right(Vector3 forward)=>Vector3.Cross(Vector3.up,forward).normalized;
        static Vector3 Centre(Vector3 position,Vector3 forward,Vector3 local)=>position+Right(forward)*local.x+forward*local.z;
        static float Radius(in ObservedCar car,Vector3 axis)=>car.halfWidth*Mathf.Abs(Vector3.Dot(car.right,axis))+car.halfLength*Mathf.Abs(Vector3.Dot(car.forward,axis));
        void Prepare(in DriverObservation o)
        {
            if(stamp==o.timestamp)return;
            stamp=o.timestamp;count=0;
            float dt=o.stepSeconds*Stride;steps=Mathf.CeilToInt(Horizon/dt)+1;
            if(steps>Steps)throw new ArgumentException("Traffic forecast exceeds fixed capacity");
            var ownCentre=Centre(o.self.position,o.self.forward,o.self.colliderCentre);
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                var otherCentre=Centre(other.position,other.forward,other.colliderCentre);
                float reach=(o.self.velocity.magnitude+other.velocity.magnitude)*Horizon+o.self.halfLength+other.halfLength+2;
                if((otherCentre-ownCentre).sqrMagnitude>reach*reach)continue;
                // Same-corridor following is handled by the leader envelope and
                // immediate guard. Do not reject every following path because
                // this nominal own-car forecast omits the leader's speed limit.
                float side=Mathf.Abs(Vector3.Dot(otherCentre-ownCentre,o.self.right));
                if(side<=o.self.halfWidth+Radius(in other,o.self.right)+.1f)continue;
                int j=count++;cars[j]=other;positions[j,0]=otherCentre;headings[j,0]=other.forward;
                var position=other.position;var velocity=other.velocity;var forward=other.forward;
                float radius=Mathf.Sqrt(other.halfWidth*other.halfWidth+other.halfLength*other.halfLength);
                for(int k=1;k<steps;k++) {
                    var turn=Quaternion.AngleAxis(other.yawRate*Mathf.Exp(-(k-.5f)*dt/.5f)*dt*Mathf.Rad2Deg,Vector3.up);
                    forward=turn*forward;velocity=turn*velocity;position+=velocity*dt;
                    positions[j,k]=Centre(position,forward,other.colliderCentre);headings[j,k]=forward;
                    angularPadding[j,k]=radius*(forward-headings[j,k-1]).magnitude;
                }
            }
        }
        public bool Conflict(in DriverObservation o,ContextPathPrediction own,Vector3 centre,Vector3 half,out int otherId,out float after)
        {
            Prepare(in o);otherId=-1;after=-1;
            float radius=Mathf.Sqrt(half.x*half.x+half.z*half.z);
            for(int k=1;k<steps&&k*Stride<own.Count;k++) {
                var a=own.samples[(k-1)*Stride];var b=own.samples[k*Stride];
                var start=Centre(a.state.position,a.state.forward,centre);var end=Centre(b.state.position,b.state.forward,centre);
                float angular=radius*(b.state.forward-a.state.forward).magnitude;
                for(int j=0;j<count;j++) {
                    var other=cars[j];
                    if(ImmediateTrafficSafety.SweptConflict(start,a.state.forward,half.x,half.z,end-start,
                        positions[j,k-1],headings[j,k-1],other.halfWidth,other.halfLength,positions[j,k]-positions[j,k-1],
                        .1f+angular+angularPadding[j,k],out float when)) {
                        otherId=other.id;after=Mathf.Lerp(a.time,b.time,when);return true;
                    }
                }
            }
            return false;
        }
    }
}
