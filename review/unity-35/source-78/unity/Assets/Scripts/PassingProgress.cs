using UnityEngine;

namespace LittleRoad
{
    // Tactical ranking only: integrate the acceleration/following phase before
    // the payoff of clearing a rival. The physical candidate checks remain the
    // authority. Own corner motion and rival future inputs are not simulated here.
    public sealed class PassingProgress
    {
        const int Steps=16,Cars=24;
        const float Dt=.25f;
        readonly float[,] sides=new float[Cars,Steps],ahead=new float[Cars,Steps];
        readonly float[] widths=new float[Cars],speeds=new float[Cars],lengths=new float[Cars];
        CarTuning tuning;
        float initialSpeed,offset,slope,blend,desired,dragReduction,brake;
        int count;
        public float FreeProgress {get;private set;}
        public float HoldProgress {get;private set;}
        public void Prepare(in DriverObservation o,float desiredSpeed)
        {
            tuning=o.tuning;initialSpeed=Mathf.Max(0,o.self.speed);offset=o.self.offset;
            desired=Mathf.Max(0,desiredSpeed);dragReduction=o.dragReduction;
            float forward=Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route));
            slope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,forward),-.35f,.35f);
            blend=Mathf.Clamp(18+initialSpeed*.9f,22,60);
            brake=Mathf.Min(tuning.braking,tuning.roadGrip*1.65f)*.7f;
            count=0;FreeProgress=Integrate(offset,false);
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap<=0||gap>160)continue;
                if(count==Cars)throw new System.ArgumentException("Passing-progress capacity exceeded.");
                widths[count]=o.self.halfWidth+other.halfWidth+.35f;
                lengths[count]=o.self.halfLength+other.halfLength;
                speeds[count]=Mathf.Max(0,Vector3.Dot(other.velocity,o.route.Forward(other.route)));
                float sideways=Vector3.Dot(other.velocity,o.route.Right(other.route));
                for(int step=0;step<Steps;step++) {
                    float time=step*Dt;
                    sides[count,step]=other.offset+Mathf.Clamp(sideways*.75f*(1-Mathf.Exp(-time/.75f)),-1.5f,1.5f);
                    ahead[count,step]=gap+speeds[count]*time-o.self.halfLength-other.halfLength-1.5f;
                }
                count++;
            }
            HoldProgress=Progress(offset);
        }
        public float Progress(float destination)=>Integrate(destination,true);
        float Integrate(float destination,bool traffic)
        {
            float speed=initialSpeed,distance=0;
            for(int step=0;step<Steps;step++) {
                float target=desired;
                if(traffic) {
                    float side=ContextPathFollower.Smooth(offset,slope,destination,distance,blend);
                    for(int j=0;j<count;j++) {
                        // Once longitudinally past a rival, it cannot limit the
                        // leader. Rear merging is still checked by the planner.
                        if(ahead[j,step]+lengths[j]+1.5f<distance||Mathf.Abs(side-sides[j,step])>widths[j])continue;
                        float clearance=ahead[j,step]-distance;
                        float limit=Mathf.Sqrt(Mathf.Max(0,speeds[j]*speeds[j]+2*brake*Mathf.Max(0,clearance-speed*.25f)));
                        target=Mathf.Min(target,limit);
                    }
                }
                float drag=DraftingModel.Resistance(speed,tuning.mass,dragReduction);
                float force=(target-speed)*3+drag;
                float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(tuning,speed)):force/tuning.braking;
                DriveModel.Forces(tuning,speed,Mathf.Clamp(pedal,-1,1),0,tuning.roadGrip,0,out float acceleration,out _,dragReduction);
                float next=Mathf.Max(0,speed+acceleration*Dt);
                distance+=(speed+next)*.5f*Dt;speed=next;
            }
            return distance;
        }
    }
}
