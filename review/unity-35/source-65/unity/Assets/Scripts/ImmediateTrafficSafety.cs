using UnityEngine;

namespace LittleRoad
{
    // A bounded braking constraint on the planned command, using actual motion.
    // It does not choose a lane, read opponent inputs, or change physical state.
    // This first slice handles moving cars ahead; scenery/merge aborts remain
    // responsibilities of the planner and future safety fixtures.
    public sealed class ImmediateTrafficSafety
    {
        const int NearSteps=12,Steps=32,Capacity=24;
        const float Dt=.0625f,Margin=.10f;
        readonly ObservedMotionPredictor history=new();
        readonly float[] lastSpeed=new float[64];readonly double[] lastSeen=new double[64];readonly bool[] known=new bool[64];
        readonly ObservedCar[] others=new ObservedCar[Capacity];
        readonly int[] horizons=new int[Capacity];int rolloutSteps;
        readonly Vector3[,] positions=new Vector3[Capacity,Steps+1],headings=new Vector3[Capacity,Steps+1];
        int count;
        public bool Active {get;private set;}
        public bool Unresolved {get;private set;}
        public int Threat {get;private set;}=-1;
        public float ConflictAfter {get;private set;}=-1;
        public float PedalLimit {get;private set;}=1;

        static Vector3 Right(Vector3 f)=>Vector3.Cross(Vector3.up,f).normalized;
        static float Radius(Vector3 axis,Vector3 f,float width,float length)=>Mathf.Abs(Vector3.Dot(axis,f))*length+Mathf.Abs(Vector3.Dot(axis,Right(f)))*width;
        // Continuous translation between rollout samples. Angular padding bounds
        // corner movement during that small interval, preventing point-sample gaps.
        public static bool SweptConflict(Vector3 a,Vector3 af,float aw,float al,Vector3 da,
            Vector3 b,Vector3 bf,float bw,float bl,Vector3 db,float padding,out float when)
        {
            float enter=0,leave=1;var delta=b-a;var velocity=db-da;
            for(int i=0;i<4;i++) {
                var axis=i==0?af:i==1?Right(af):i==2?bf:Right(bf);
                float span=Radius(axis,af,aw,al)+Radius(axis,bf,bw,bl)+padding;
                float p=Vector3.Dot(delta,axis),v=Vector3.Dot(velocity,axis);
                if(Mathf.Abs(v)<.000001f){if(Mathf.Abs(p)>span){when=-1;return false;}continue;}
                float t0=(-span-p)/v,t1=(span-p)/v;
                enter=Mathf.Max(enter,Mathf.Min(t0,t1));leave=Mathf.Min(leave,Mathf.Max(t0,t1));
                if(enter>leave){when=-1;return false;}
            }
            when=enter;return true;
        }
        public static bool OwnsFollowingResponse(float routeGap)=>routeGap>=0;

        void Observe(in DriverObservation o,in DriveCommand command)
        {
            count=0;rolloutSteps=NearSteps;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float acceleration=history.Observe(other.id,o.timestamp,Mathf.Max(0,other.speed));
                if(other.id>=0&&other.id<known.Length) {
                    double elapsed=o.timestamp-lastSeen[other.id];
                    if(known[other.id]&&elapsed>0&&elapsed<.3)acceleration=Mathf.Min(acceleration,Mathf.Clamp((other.speed-lastSpeed[other.id])/(float)elapsed,-25,15));
                    known[other.id]=true;lastSpeed[other.id]=other.speed;lastSeen[other.id]=o.timestamp;
                }
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                // A follower owns its braking; don't make the leader brake in
                // response to a rear approach. Planned merges still check rear cars.
                if(!OwnsFollowingResponse(gap))continue;
                int horizon=NearSteps;
                // Credible braking in the same corridor needs enough time for
                // our own stopping response, even before a close-range collision.
                // Do not extend all side-by-side/turning predictions indiscriminately.
                float lateral=Mathf.Abs(Vector3.Dot(other.position-o.self.position,o.self.right));
                bool straightResponse=Mathf.Abs(command.steering*o.tuning.yaw)<.15f&&Mathf.Abs(o.self.yawRate)<.15f;
                if(straightResponse&&acceleration< -2&&o.self.speed>other.speed&&Vector3.Dot(o.self.forward,other.forward)>.98f&&lateral<o.self.halfWidth+other.halfWidth+.3f)
                    horizon=Mathf.Clamp(Mathf.CeilToInt((o.self.speed/Mathf.Max(1,Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f))+.15f)/Dt),NearSteps,Steps);
                float reach=(o.self.velocity.magnitude+other.velocity.magnitude)*horizon*Dt+o.self.halfLength+other.halfLength+2;
                if((other.position-o.self.position).sqrMagnitude>reach*reach||count==Capacity)continue;
                int j=count++;others[j]=other;horizons[j]=horizon;rolloutSteps=Mathf.Max(rolloutSteps,horizon);positions[j,0]=other.position;headings[j,0]=other.forward;
                var velocity=other.velocity;float previousSpeed=Mathf.Max(0,other.speed);
                for(int tick=1;tick<=horizon;tick++) {
                    float t=(tick-.5f)*Dt;
                    var turn=Quaternion.Euler(0,other.yawRate*Mathf.Exp(-t/.5f)*Dt*Mathf.Rad2Deg,0);
                    headings[j,tick]=turn*headings[j,tick-1];velocity=turn*velocity;
                    // Retain observed deceleration until stopping or a fresh
                    // observation releases it; fading it assumed early brake release.
                    float nextSpeed=Mathf.Max(0,previousSpeed+acceleration*(acceleration<0?1:Mathf.Exp(-t/.75f))*Dt);
                    velocity+=headings[j,tick]*(nextSpeed-previousSpeed);previousSpeed=nextSpeed;
                    positions[j,tick]=positions[j,tick-1]+velocity*Dt;
                }
            }
        }
        float Conflict(in DriverObservation o,in DriveCommand command,float pedal,out int threat)
        {
            threat=-1;var p=o.self.position;var velocity=o.self.velocity;var f=o.self.forward;
            float yaw=o.self.yawRate,steer=o.ownSteeringState,budget=o.tuning.roadGrip*1.65f;
            for(int tick=1;tick<=rolloutSteps;tick++) {
                float speed=Vector3.Dot(velocity,f);var right=Right(f);
                steer=Mathf.MoveTowards(steer,command.steering,o.tuning.response*Dt);
                float request=steer*o.tuning.yaw*Mathf.Clamp01(Mathf.Abs(speed)/3)*Mathf.Sign(speed==0?1:speed);
                DriveModel.Forces(o.tuning,speed,pedal,request,o.tuning.roadGrip,0,out float acceleration,out float targetYaw);
                velocity+=(f*acceleration-right*Mathf.Clamp(Vector3.Dot(velocity,right)*8,-budget,budget))*Dt;
                yaw=(yaw+Mathf.Clamp((targetYaw-yaw)*20,-25,25)*Dt)/(1+1.5f*Dt);
                var nextForward=Quaternion.Euler(0,yaw*Dt*Mathf.Rad2Deg,0)*f;
                var movement=velocity*Dt;
                for(int j=0;j<count;j++) {
                    if(tick>horizons[j])continue;
                    var other=others[j];var heading=headings[j,tick-1];
                    float rotationPadding=(o.self.halfWidth+o.self.halfLength)*Vector3.Angle(f,nextForward)*Mathf.Deg2Rad
                        +(other.halfWidth+other.halfLength)*Vector3.Angle(heading,headings[j,tick])*Mathf.Deg2Rad;
                    if(SweptConflict(p,f,o.self.halfWidth,o.self.halfLength,movement,positions[j,tick-1],heading,other.halfWidth,other.halfLength,
                        positions[j,tick]-positions[j,tick-1],Margin+rotationPadding,out float fraction)) {
                        threat=other.id;return (tick-1+fraction)*Dt;
                    }
                }
                p+=movement;f=nextForward;
            }
            return float.PositiveInfinity;
        }
        public DriveCommand Apply(in DriverObservation o,in DriveCommand planned)
        {
            Active=Unresolved=false;Threat=-1;ConflictAfter=-1;PedalLimit=1;
            if(o.self.speed<.1f||o.finished)return planned;
            Observe(in o,in planned);if(count==0)return planned;
            float original=Conflict(in o,in planned,planned.pedal,out int threat);
            if(float.IsPositiveInfinity(original))return planned;
            Threat=threat;ConflictAfter=original;
            float best=original,limit=planned.pedal;bool clear=false;
            // Least restrictive feasible braking first. The nominal command is
            // retained if every tested brake would bring a conflict forward.
            for(int i=0;i<4;i++) {
                float pedal=i==0?0:i==1?-.35f:i==2?-.7f:-1;
                if(pedal>=planned.pedal)continue;
                float time=Conflict(in o,in planned,pedal,out _);
                if(time>best+.00001f){best=time;limit=pedal;}
                if(float.IsPositiveInfinity(time)){clear=true;break;}
            }
            Unresolved=!clear;Active=limit<planned.pedal;PedalLimit=limit;
            // Re-evaluated from fresh observations every physics tick. No stale
            // stop target persists after the credible collision forecast clears.
            return Active?new DriveCommand(limit,planned.steering,planned.targetSpeed,planned.predictedOffset,planned.chosenLane):planned;
        }
    }
}
