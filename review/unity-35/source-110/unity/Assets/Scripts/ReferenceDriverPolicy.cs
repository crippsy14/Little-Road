using UnityEngine;

namespace LittleRoad
{
    // Solo engineering reference. No form noise, pace handicap or traffic
    // decisions. Commands still go through the same physical car controller.
    public sealed class ReferenceDriverPolicy:IDriverPolicy
    {
        readonly RacingSpeedProfile profile;
        public ReferenceDriverPolicy(RacingSpeedProfile profile){this.profile=profile;}
        public DriveCommand Decide(in DriverObservation observation)
        {
            var car=observation.self;var tuning=observation.tuning;var line=profile.line;
            float distance=line.Project(car.position,car.route,out float error);
            float look=Mathf.Clamp(5+Mathf.Max(car.speed,0)*.4f,6,19);
            var aim=line.At(distance+look);var delta=aim-car.position;
            float angle=Vector3.SignedAngle(car.forward,delta,Vector3.up)*Mathf.Deg2Rad;
            float curve=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steering=Mathf.Clamp(curve*Mathf.Max(car.speed,3)*1.09f/tuning.yaw,-1,1);
            float preview=distance+2+Mathf.Max(car.speed,0)*.12f;
            float target=profile.Speed(preview);
            target=Mathf.Min(target,Mathf.Sqrt(tuning.roadGrip*1.65f*.85f/Mathf.Max(.00001f,Mathf.Abs(curve))));
            if(Mathf.Abs(error)>3)target=Mathf.Min(target,12);
            if(Mathf.Abs(angle)>.9f){steering=Mathf.Sign(angle);target=Mathf.Min(target,8);}
            if(observation.finished)target=Mathf.Min(target,18);
            float desiredAcceleration=profile.Acceleration(preview)+(target-car.speed)*3;
            float wheelForce=desiredAcceleration+DriveModel.Resistance(car.speed,tuning.mass);
            float pedal=wheelForce>=0?wheelForce/Mathf.Max(.01f,DriveModel.EngineForce(tuning,car.speed)):wheelForce/tuning.braking;
            if(target>=tuning.AttainableSpeed-.15f&&car.speed<target)pedal=1;
            float predicted=Vector3.Dot(car.position+car.velocity*.55f-observation.route.At(car.route+Mathf.Max(car.speed,0)*.55f),observation.route.Right(car.route+Mathf.Max(car.speed,0)*.55f));
            return new DriveCommand(pedal,steering,target,predicted,line.Offset(distance));
        }
    }
}
