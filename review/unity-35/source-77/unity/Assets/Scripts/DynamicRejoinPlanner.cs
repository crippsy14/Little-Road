using UnityEngine;

namespace LittleRoad
{
    // First dynamic-line slice: solo rejoin location and speed, with up to six
    // continuous curves. Traffic planning remains out of scope for this option.
    public sealed class DynamicRejoinPlanner
    {
        [System.Serializable] public struct Summary {public int id,evaluated,geometricRejects,selections;public float minimumBlend,maximumBlend,lastEstimatedTime,minimumHorizon,maximumHorizon;}
        public const int Capacity=6,PhysicalShortlist=2;
        const float RetentionAllowance=.05f;
        const float ScanStep=6,CornerThreshold=.004f,ExitStraight=24;
        readonly RacingSpeedProfile profile;
        readonly ContextPathFollower evaluator;
        readonly ContextPathState[] paths=new ContextPathState[Capacity];
        readonly float[] times=new float[Capacity];
        readonly float[] clearance=new float[Capacity];
        readonly int[] order=new int[Capacity];
        public int Count {get;private set;}
        public int Evaluated {get;private set;}
        public int GeometricRejects {get;private set;}
        public int Selections {get;private set;}
        public float LastBlend {get;private set;}
        public float LastEstimatedTime {get;private set;}
        float minimumBlend=float.PositiveInfinity,maximumBlend,minimumHorizon=float.PositiveInfinity,maximumHorizon;
        public float Horizon {get;private set;}
        public float CornerExit {get;private set;}
        public Summary Export(int id)=>new Summary{id=id,evaluated=Evaluated,geometricRejects=GeometricRejects,selections=Selections,minimumBlend=Selections>0?minimumBlend:0,maximumBlend=maximumBlend,lastEstimatedTime=LastEstimatedTime,minimumHorizon=Selections>0?minimumHorizon:0,maximumHorizon=maximumHorizon};
        public DynamicRejoinPlanner(RacingSpeedProfile profile){this.profile=profile;evaluator=new ContextPathFollower(profile,false);}
        public ContextPathState Path(int rank)=>paths[order[rank]];
        public float EstimatedTime(int rank)=>times[order[rank]];
        public float Clearance(int rank)=>clearance[order[rank]];
        // Only used after the fastest candidate fails its physical check.
        // Spend the second rollout on a route with geometric room to track,
        // rather than another near-identical boundary-grazing trajectory.
        public int FallbackRank()
        {
            int best=-1;
            for(int rank=1;rank<Count;rank++) {
                if(!float.IsFinite(EstimatedTime(rank)))continue;
                if(best<0||Clearance(rank)>Clearance(best)+.001f)best=rank;
            }
            return best;
        }
        void Add(in DriverObservation o,ContextPathState path)
        {
            paths[Count]=path;times[Count]=evaluator.EstimateGateTime(in o,in path,Horizon);order[Count]=Count;
            clearance[Count]=evaluator.MinimumClearance;
            if(float.IsPositiveInfinity(times[Count]))GeometricRejects++;
            Count++;Evaluated++;
        }
        public void Prepare(in DriverObservation o,in ContextPathState previous,bool hasPrevious)
        {
            Count=0;
            // Scan shared guide geometry, not track-specific corner names. A
            // sustained straight closes the upcoming corner/complex. Bounded
            // scan remains explicit when no exit is found inside the horizon.
            bool entered=false;float straight=0,exit=72;
            for(float ahead=0;ahead<=312;ahead+=ScanStep) {
                float d=o.self.route+ahead;
                var before=profile.line.At(d)-profile.line.At(d-6);
                var after=profile.line.At(d+6)-profile.line.At(d);
                float curvature=2*Mathf.Abs(Vector3.Cross(before,after).y)/Mathf.Max(.001f,before.magnitude*after.magnitude*(before+after).magnitude);
                if(curvature>=CornerThreshold){entered=true;straight=0;exit=ahead;}
                else if(entered){straight+=ScanStep;if(straight>=ExitStraight)break;}
                // On a clear straight, retain a local plan until a corner is
                // within a useful approach horizon. No full-track car search.
                else if(ahead>=120)break;
            }
            CornerExit=exit;Horizon=Mathf.Clamp(exit+48,120,360);
            bool retain=hasPrevious&&previous.guide&&previous.localSpeed&&!previous.coast;
            if(retain)Add(in o,previous);
            float basis=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60);
            for(int i=0;i<5;i++) {
                var path=RoadPathFeasibility.GuideFromMotion(in o,profile);
                path.blend=i==0?basis:i==1?Mathf.Max(basis,exit*.4f):i==2?Mathf.Max(basis,exit*.7f):i==3?Mathf.Max(basis,exit):Mathf.Max(basis,exit+24);
                path.blend=Mathf.Clamp(path.blend,16,336);path.localSpeed=true;
                path.selectedOffset=path.endOffset=profile.line.Offset(path.anchor+path.blend);
                Add(in o,path);
            }
            // Stable insertion sort. Preference for a viable current curve is
            // a small time tolerance, never a reward for reaching the guide.
            for(int i=1;i<Count;i++) {
                int key=order[i],j=i-1;float cost=times[key]-(retain&&key==0?RetentionAllowance:0);
                while(j>=0&&cost<times[order[j]]-(retain&&order[j]==0?RetentionAllowance:0)){order[j+1]=order[j];j--;}
                order[j+1]=key;
            }
        }
        public void Selected(int rank){Selections++;LastBlend=Path(rank).blend;LastEstimatedTime=EstimatedTime(rank);minimumBlend=Mathf.Min(minimumBlend,LastBlend);maximumBlend=Mathf.Max(maximumBlend,LastBlend);minimumHorizon=Mathf.Min(minimumHorizon,Horizon);maximumHorizon=Mathf.Max(maximumHorizon,Horizon);}
    }
}
