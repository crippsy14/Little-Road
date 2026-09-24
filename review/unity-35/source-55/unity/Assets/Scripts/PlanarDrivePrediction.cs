using UnityEngine;

namespace LittleRoad
{
    // Contact-free, flat-tarmac model. Calibrate against Rigidbody motion before
    // using it as a path constraint. It never mutates the actual vehicle.
    public struct PlanarDrivePrediction
    {
        public Vector3 position,velocity,forward;
        public float yawRate,steering;
        public PlanarDrivePrediction(Vector3 position,Vector3 velocity,Vector3 forward,float yawRate,float steering)
        {
            this.position=position;velocity.y=0;forward.y=0;
            this.velocity=velocity;this.forward=forward.normalized;this.yawRate=yawRate;this.steering=steering;
        }
        public void Step(CarTuning tuning,float pedal,float steeringRequest,float dt)
        {
            var right=Vector3.Cross(Vector3.up,forward);
            float speed=Vector3.Dot(velocity,forward),budget=tuning.roadGrip*1.65f;
            steering=Mathf.MoveTowards(steering,Mathf.Clamp(steeringRequest,-1,1),tuning.response*dt);
            float request=steering*tuning.yaw*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed==0?1:speed);
            DriveModel.Forces(tuning,speed,Mathf.Clamp(pedal,-1,1),request,tuning.roadGrip,0,out float acceleration,out float targetYaw);
            if(speed< -7&&acceleration<0)acceleration=0;
            velocity+=(forward*acceleration-right*Mathf.Clamp(Vector3.Dot(velocity,right)*8,-budget,budget))*dt;
            // Match the configured torque servo and explicit angular damping;
            // collision/ground-alignment impulses are deliberately not predicted.
            yawRate=Mathf.Clamp((yawRate+Mathf.Clamp((targetYaw-yawRate)*20,-25,25)*dt)*Mathf.Max(0,1-1.5f*dt),-2.3f,2.3f);
            position+=velocity*dt;
            forward=Quaternion.AngleAxis(yawRate*dt*Mathf.Rad2Deg,Vector3.up)*forward;
        }
        public float MaxBodyOffset(Circuit road,Vector3 colliderCentre,Vector3 half)
        {
            var right=Vector3.Cross(Vector3.up,forward);float maximum=0;
            var centre=position+right*colliderCentre.x+forward*colliderCentre.z+Vector3.up*(colliderCentre.y-half.y);
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2) {
                road.Nearest(centre+right*(side*half.x)+forward*(end*half.z),out float offset);
                maximum=Mathf.Max(maximum,Mathf.Abs(offset));
            }
            return maximum;
        }
    }
}
