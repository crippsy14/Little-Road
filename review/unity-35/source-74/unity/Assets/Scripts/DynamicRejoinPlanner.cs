using UnityEngine;

namespace LittleRoad
{
    // First dynamic-line slice: solo rejoin location and speed, with up to six
    // continuous curves. Traffic planning remains out of scope for this option.
    public sealed class DynamicRejoinPlanner
    {
        [System.Serializable] public struct Summary {public int id,evaluated,geometricRejects,selections;public float minimumBlend,maximumBlend,lastEstimatedTime;}
        public const int Capacity=6,PhysicalShortlist=2;
        const float RetentionAllowance=.05f;
        static readonly float[] scales={.65f,1f,1.5f,2f,2.75f};
        readonly RacingSpeedProfile profile;
        readonly ContextPathFollower evaluator;
        readonly ContextPathState[] paths=new ContextPathState[Capacity];
        readonly float[] times=new float[Capacity];
        readonly int[] order=new int[Capacity];
        public int Count {get;private set;}
        public int Evaluated {get;private set;}
        public int GeometricRejects {get;private set;}
        public int Selections {get;private set;}
        public float LastBlend {get;private set;}
        public float LastEstimatedTime {get;private set;}
        float minimumBlend=float.PositiveInfinity,maximumBlend;
        public Summary Export(int id)=>new Summary{id=id,evaluated=Evaluated,geometricRejects=GeometricRejects,selections=Selections,minimumBlend=Selections>0?minimumBlend:0,maximumBlend=maximumBlend,lastEstimatedTime=LastEstimatedTime};
        public DynamicRejoinPlanner(RacingSpeedProfile profile){this.profile=profile;evaluator=new ContextPathFollower(profile,false);}
        public ContextPathState Path(int rank)=>paths[order[rank]];
        public float EstimatedTime(int rank)=>times[order[rank]];
        void Add(in DriverObservation o,ContextPathState path)
        {
            paths[Count]=path;times[Count]=evaluator.EstimateGateTime(in o,in path);order[Count]=Count;
            if(float.IsPositiveInfinity(times[Count]))GeometricRejects++;
            Count++;Evaluated++;
        }
        public void Prepare(in DriverObservation o,in ContextPathState previous,bool hasPrevious)
        {
            Count=0;
            bool retain=hasPrevious&&previous.guide&&previous.localSpeed&&!previous.coast;
            if(retain)Add(in o,previous);
            float basis=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60);
            foreach(float scale in scales) {
                var path=RoadPathFeasibility.GuideFromMotion(in o,profile);
                path.blend=Mathf.Clamp(basis*scale,16,110);path.localSpeed=true;
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
        public void Selected(int rank){Selections++;LastBlend=Path(rank).blend;LastEstimatedTime=EstimatedTime(rank);minimumBlend=Mathf.Min(minimumBlend,LastBlend);maximumBlend=Mathf.Max(maximumBlend,LastBlend);}
    }
}
