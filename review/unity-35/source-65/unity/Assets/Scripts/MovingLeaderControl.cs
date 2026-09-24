using UnityEngine;

namespace LittleRoad
{
    // Shared ordinary following control. Keep the existing arithmetic/order so
    // extracting this from the live driver does not change its behaviour.
    public static class MovingLeaderControl
    {
        public static DriveCommand Apply(in DriverObservation o,DriveCommand command,out int leader,out float limit,out bool limited)
        {
            leader=-1;limit=999;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap<=0||gap>100)continue;
                float width=o.self.halfWidth+other.halfWidth+.35f;
                if(Mathf.Abs(other.offset-o.self.offset)>width)continue;
                float clearance=gap-o.self.halfLength-other.halfLength-1.5f;
                float brake=Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f)*.7f;
                float candidate=Mathf.Sqrt(Mathf.Max(0,other.speed*other.speed+2*brake*Mathf.Max(0,clearance-o.self.speed*.25f)));
                if(candidate<limit){limit=candidate;leader=other.id;}
            }
            limited=limit<command.targetSpeed;
            if(!limited)return command;
            float force=(limit-o.self.speed)*3+DriveModel.Resistance(o.self.speed,o.tuning.mass);
            float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
            return new DriveCommand(Mathf.Min(command.pedal,pedal),command.steering,limit,command.predictedOffset,command.chosenLane);
        }
    }
}
