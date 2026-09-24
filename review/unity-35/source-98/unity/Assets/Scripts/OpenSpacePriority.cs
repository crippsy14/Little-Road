using UnityEngine;

namespace LittleRoad
{
    // Cheap tactical progress estimate, not collision clearance. The selected
    // route still goes through the existing physical road/traffic forecasts.
    public sealed class OpenSpacePriority
    {
        public const float Horizon=2,Weight=2;
        readonly float[] distances=new float[8],widths=new float[24];
        readonly float[,] sides=new float[24,8],spaces=new float[24,8];
        float offset,slope,blend;int count;
        readonly PassingProgress passing;
        public OpenSpacePriority(bool integratePassing=false){if(integratePassing)passing=new PassingProgress();}
        public float FreeProgress=>passing!=null?passing.FreeProgress:distances[7];
        public float HoldProgress {get;private set;}
        public void Prepare(in DriverObservation o,float desiredSpeed)
        {
            if(passing!=null){passing.Prepare(in o,desiredSpeed);HoldProgress=passing.HoldProgress;return;}
            float speed=Mathf.Max(0,o.self.speed),distance=0;
            float forward=Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route));
            slope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,forward),-.35f,.35f);
            blend=Mathf.Clamp(18+speed*.9f,22,60);offset=o.self.offset;count=0;
            for(int step=1;step<=8;step++) {
                const float dt=.25f;
                // Equipment defines reachable longitudinal progress. No bonus
                // force or promised future rival input enters this estimate.
                float acceleration=DriveModel.EngineForce(o.tuning,speed)-DraftingModel.Resistance(speed,o.tuning.mass,o.dragReduction);
                speed=Mathf.Min(Mathf.Max(speed,desiredSpeed),Mathf.Max(0,speed+acceleration*dt));
                distance+=speed*dt;
                distances[step-1]=distance;
            }
            foreach(var other in o.traffic) {
                    if(other.id==o.self.id)continue;
                    float initialGap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                    // Rear approaches cannot create a reward for yielding.
                    // Their actual crossing reservations remain in the planner.
                    if(initialGap<=0||initialGap>120)continue;
                    if(count==widths.Length)throw new System.ArgumentException("Open-space traffic capacity exceeded.");
                    float otherForward=Mathf.Max(0,Vector3.Dot(other.velocity,o.route.Forward(other.route)));
                    float sideways=Vector3.Dot(other.velocity,o.route.Right(other.route));
                    widths[count]=o.self.halfWidth+other.halfWidth+.35f;
                    for(int step=0;step<8;step++) {
                        float time=(step+1)*.25f;
                        sides[count,step]=other.offset+Mathf.Clamp(sideways*.75f*(1-Mathf.Exp(-time/.75f)),-1.5f,1.5f);
                        spaces[count,step]=Mathf.Max(0,initialGap+otherForward*time-o.self.halfLength-other.halfLength-1.5f)*Horizon/time;
                    }
                    count++;
            }
            HoldProgress=Progress(offset);
        }
        public float Progress(float destination)
        {
            if(passing!=null)return passing.Progress(destination);
            float available=FreeProgress;
            for(int step=0;step<8;step++) {
                float side=ContextPathFollower.Smooth(offset,slope,destination,distances[step],blend);
                for(int other=0;other<count;other++)if(Mathf.Abs(sides[other,step]-side)<=widths[other])available=Mathf.Min(available,spaces[other,step]);
            }
            return available;
        }
        public static float Preference(float progress,float holdProgress,float freeProgress)
            =>Weight*Mathf.Clamp((progress-holdProgress)/Mathf.Max(5,freeProgress*.25f),-1,1);
    }
}
