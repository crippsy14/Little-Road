using UnityEngine;

namespace LittleRoad
{
    // Shared ordinary following control. Keep the existing arithmetic/order so
    // extracting this from the live driver does not change its behaviour.
    public static class MovingLeaderControl
    {
        // Experiment (ai-pace-experiments, point 2): judge a car ahead against the
        // committed path, not only the current lateral offset. Kept in 114 with
        // --conditional-hold (default on); --no-path-aware-leader restores the old rule.
        public static readonly bool PathAware=!RaceTrial.Has("--no-path-aware-leader");
        const float MaximumCatchSeconds=4,MinimumClosing=.5f,MaximumDrift=1.5f;

        public static DriveCommand Apply(in DriverObservation o,DriveCommand command,out int leader,out float limit,out bool limited)
        {
            ContextPathState none=default;
            return Apply(in o,command,null,in none,out leader,out limit,out limited);
        }
        // planned==null reproduces the original current-offset rule exactly.
        public static DriveCommand Apply(in DriverObservation o,DriveCommand command,ContextPathFollower planned,in ContextPathState path,out int leader,out float limit,out bool limited)
        {
            leader=-1;limit=999;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap<=0||gap>100)continue;
                float width=o.self.halfWidth+other.halfWidth+.35f;
                if(Mathf.Abs(other.offset-o.self.offset)>width)continue;
                if(planned!=null&&ClearsAtCatch(in o,in other,gap,width,planned,in path))continue;
                float clearance=gap-o.self.halfLength-other.halfLength-1.5f;
                float brake=Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f)*.7f;
                float candidate=Mathf.Sqrt(Mathf.Max(0,other.speed*other.speed+2*brake*Mathf.Max(0,clearance-o.self.speed*.25f)));
                if(candidate<limit){limit=candidate;leader=other.id;}
            }
            limited=limit<command.targetSpeed;
            if(!limited)return command;
            float force=(limit-o.self.speed)*3+DraftingModel.Resistance(o.self.speed,o.tuning.mass,o.dragReduction,o.tuning.DragFactor);
            float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
            return new DriveCommand(Mathf.Min(command.pedal,pedal),command.steering,limit,command.predictedOffset,command.chosenLane);
        }
        // True only when we are closing on the car and the committed path has
        // moved us laterally clear of it (current and drifted position) by the
        // time our nose reaches its tail, and stays clear for a body length.
        // Not a collision proof: the planner's traffic checks and the immediate
        // guard retain authority. Slow/no closing keeps ordinary following.
        public static bool ClearsAtCatch(in DriverObservation o,in ObservedCar other,float gap,float width,ContextPathFollower planned,in ContextPathState path)
        {
            float own=Mathf.Max(0,Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route)));
            float theirs=Mathf.Max(0,Vector3.Dot(other.velocity,o.route.Forward(other.route)));
            float closing=own-theirs;
            if(closing<MinimumClosing)return false;
            float bumper=Mathf.Max(0,gap-o.self.halfLength-other.halfLength);
            float t=bumper/closing;
            if(t>MaximumCatchSeconds)return false;
            float drifted=other.offset+Mathf.Clamp(Vector3.Dot(other.velocity,o.route.Right(other.route))*t,-MaximumDrift,MaximumDrift);
            float reach=own*t;
            for(int k=0;k<3;k++) {
                float mine=planned.PlannedOffset(in path,reach+k*o.self.halfLength);
                if(Mathf.Abs(mine-other.offset)<=width||Mathf.Abs(mine-drifted)<=width)return false;
            }
            return true;
        }
    }
}
