using System;
using UnityEngine;

namespace LittleRoad
{
    // A recovery-only coast/brake return to the inside of the road. It neither
    // supplies extra power nor replaces the racing planner's path constraints.
    public sealed class RollingRecovery
    {
        public struct Sample {public PlanarDrivePrediction state;public float time;}
        readonly Sample[] samples=new Sample[361];
        readonly ImmediateTrafficSafety safety=new();
        float anchor,startOffset,startSlope,blend,destination;
        public string Rejection {get;private set;}
        public float PredictedEndOffset {get;private set;}
        public int Attempts {get;private set;}
        public int Accepted {get;private set;}
        public int Commands {get;private set;}
        static float Gap(float a,float b,float length)=>Mathf.Repeat(b-a+length*.5f,length)-length*.5f;
        Vector3 Point(IDriverRoute road,float along)
        {
            float d=anchor+along;
            float side=ContextPathFollower.Smooth(startOffset,startSlope,destination,along,blend);
            return road.At(d)+road.Right(d)*side;
        }
        DriveCommand Control(in DriverObservation o)
        {
            float along=Mathf.Max(0,Gap(anchor,o.self.route,o.route.Length));
            float look=Mathf.Clamp(o.self.speed*.7f,8,20);
            var delta=Point(o.route,along+look)-o.self.position;
            var travel=o.self.velocity;travel.y=0;
            var heading=travel.sqrMagnitude>9?travel.normalized:o.self.forward;
            float angle=Vector3.SignedAngle(heading,delta,Vector3.up)*Mathf.Deg2Rad;
            float curvature=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steer=Mathf.Clamp(curvature*Mathf.Max(3,o.self.speed)*1.09f/o.tuning.yaw,-1,1);
            float grip=Mathf.Abs(o.self.offset)<Circuit.Width*.5f?o.tuning.roadGrip:o.tuning.dirtGrip;
            float target=Mathf.Min(18,Mathf.Sqrt(grip*1.65f*.6f/Mathf.Max(.00001f,Mathf.Abs(curvature))));
            float force=(target-o.self.speed)*2+DraftingModel.Resistance(o.self.speed,o.tuning.mass,o.dragReduction);
            return new DriveCommand(Mathf.Clamp(force/o.tuning.braking,-1,0),steer,target,o.self.offset,destination);
        }
        public bool TryPlan(in DriverObservation o,float progress,float gate,RecoverySensor sensor)
        {
            Attempts++;Rejection=null;
            if(o.finished||o.route is not Circuit road||!road.hasConstantHeight||progress>gate+.5f||o.self.speed<=5.5f
                ||Mathf.Abs(o.self.offset)>9.5f||Mathf.Abs(o.self.offset)<4
                ||Vector3.Angle(o.self.forward,o.route.Forward(o.self.route))>35
                ||Vector3.Angle(o.self.velocity,o.route.Forward(o.self.route))>40) {Rejection="ineligible pose or gate";return false;}
            anchor=o.self.route;startOffset=o.self.offset;destination=Mathf.Sign(startOffset)*4;
            startSlope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(anchor))/Mathf.Max(3,Vector3.Dot(o.self.velocity,o.route.Forward(anchor))),-.35f,.35f);
            blend=Mathf.Clamp(o.self.speed*1.5f,25,65);
            var model=new PlanarDrivePrediction(o.self.position,o.self.velocity,o.self.forward,o.self.yawRate,o.ownSteeringState);
            int steps=Mathf.CeilToInt(2/o.stepSeconds);if(steps<1||steps>=samples.Length){Rejection="unsupported timestep";return false;}
            float initialBody=model.MaxBodyOffset(road,o.self.colliderCentre,new Vector3(o.self.halfWidth,.5f,o.self.halfLength));
            for(int i=0;i<=steps;i++) {
                float route=road.Nearest(model.position,out float offset);
                float bound=model.MaxBodyOffset(road,o.self.colliderCentre,new Vector3(o.self.halfWidth,.5f,o.self.halfLength));
                if(bound>11.5f||bound>initialBody+.5f){Rejection="leaving supported shoulder";return false;}
                samples[i]=new Sample{state=model,time=i*o.stepSeconds};
                if(i==steps){PredictedEndOffset=offset;break;}
                var self=new ObservedCar(o.self.id,model.position,model.velocity,model.forward,Vector3.Cross(Vector3.up,model.forward),route,offset,
                    Vector3.Dot(model.velocity,model.forward),o.self.halfWidth,o.self.halfLength,model.yawRate,o.self.colliderCentre);
                float drag=o.drafting?DraftingModel.Reduction(in self,o.traffic,out _,road.Length,i*o.stepSeconds):0;
                var predicted=new DriverObservation(o.timestamp+i*o.stepSeconds,o.stepSeconds,self,ReadOnlySpan<ObservedCar>.Empty,road,o.tuning,1,0,false,model.steering,o.drafting,drag);
                var command=Control(in predicted);model.Step(o.tuning,command.pedal,command.steering,o.stepSeconds,drag,Mathf.Abs(offset)<Circuit.Width*.5f?o.tuning.roadGrip:o.tuning.dirtGrip);
            }
            if(Mathf.Abs(PredictedEndOffset)>Mathf.Abs(startOffset)-1||Vector3.Dot(model.velocity,model.forward)<3) {Rejection="no useful moving return";return false;}
            if(sensor==null||!sensor.RollingClear(in o,new ReadOnlySpan<Sample>(samples,0,steps+1))) {Rejection="support scenery or traffic";return false;}
            Accepted++;return true;
        }
        public DriveCommand Command(in DriverObservation o)
        {
            Commands++;var command=Control(in o);
            return safety.Apply(in o,in command);
        }
    }
}
