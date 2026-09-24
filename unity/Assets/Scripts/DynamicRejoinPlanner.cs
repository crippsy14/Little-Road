using UnityEngine;

namespace LittleRoad
{
    // Bounded local curves ranked to a shared gate. Solo and opt-in traffic
    // experiments share ordinary control and at most two physical checks.
    public sealed class DynamicRejoinPlanner
    {
        [System.Serializable] public struct Summary {public int id,evaluated,geometricRejects,selections,trafficPlans,trafficSelections;public float minimumBlend,maximumBlend,lastEstimatedTime,minimumHorizon,maximumHorizon;}
        public const int Capacity=9,PhysicalShortlist=2,GapCandidates=3;
        const float RetentionAllowance=.05f;
        const float ScanStep=6,CornerThreshold=.004f,ExitStraight=24;
        readonly RacingSpeedProfile profile;
        readonly bool routeLocalAdjacency,blockingTarget,gapSeeking;
        readonly ContextPathFollower evaluator;
        readonly ContextPathState[] paths=new ContextPathState[Capacity];
        readonly float[] times=new float[Capacity];
        readonly float[] clearance=new float[Capacity];
        readonly int[] order=new int[Capacity];
        ForecastRequest preparedIdentity;
        readonly ObservedCar[] preparedTraffic=new ObservedCar[24];
        int preparedTrafficCount;bool preparedValid,preparedHasPrevious,preparedTrafficMode,preparedFinished;
        float preparedDrag,preparedPace,preparedPreferred;
        public int Count {get;private set;}
        public int Evaluated {get;private set;}
        public int GeometricRejects {get;private set;}
        public int Selections {get;private set;}
        int trafficPlans,trafficSelections,freshHoldIndex=-1,gapStart=int.MaxValue;bool trafficPlan,retained;
        // Traffic plans only: the car the passing sides were built around.
        public bool TrafficPlan=>trafficPlan;
        public int Blocker {get;private set;}=-1;
        public float BlockerCentre {get;private set;}
        public float LastBlend {get;private set;}
        public float LastEstimatedTime {get;private set;}
        float minimumBlend=float.PositiveInfinity,maximumBlend,minimumHorizon=float.PositiveInfinity,maximumHorizon;
        public float Horizon {get;private set;}
        public float CornerExit {get;private set;}
        public Summary Export(int id)=>new Summary{id=id,evaluated=Evaluated,geometricRejects=GeometricRejects,selections=Selections,trafficPlans=trafficPlans,trafficSelections=trafficSelections,minimumBlend=Selections>0?minimumBlend:0,maximumBlend=maximumBlend,lastEstimatedTime=LastEstimatedTime,minimumHorizon=Selections>0?minimumHorizon:0,maximumHorizon=maximumHorizon};
        // gapSeeking null reads --gap-seeking (ai-racing-review goal 4 experiment).
        public DynamicRejoinPlanner(RacingSpeedProfile profile,bool routeLocalAdjacency=false,bool blockingTarget=false,bool? gapSeeking=null){this.profile=profile;this.routeLocalAdjacency=routeLocalAdjacency;this.blockingTarget=blockingTarget;this.gapSeeking=gapSeeking??RaceTrial.Has("--gap-seeking");evaluator=new ContextPathFollower(profile,false);}
        public ContextPathState Path(int rank)=>paths[order[rank]];
        public float EstimatedTime(int rank)=>times[order[rank]];
        public float Clearance(int rank)=>clearance[order[rank]];
        public int NextUntriedRank(int triedMask)
        {
            for(int rank=0;rank<Count;rank++)
                if((triedMask&(1<<rank))==0&&float.IsFinite(EstimatedTime(rank)))return rank;
            return -1;
        }
        public void InvalidatePrepared()=>preparedValid=false;
        // Speculative ranking owns its inputs and buffers until the synchronous
        // snapshot/forecast boundary ends. It cannot advance the live counters.
        public void PrepareOwned(in DriverObservation o,in ContextPathState previous,bool hasPrevious,bool traffic)
        {
            preparedValid=false;
            if(o.traffic.Length>preparedTraffic.Length)throw new System.ArgumentException("Prepared ranking traffic capacity exceeded.");
            if(traffic)PrepareTraffic(in o,in previous,hasPrevious);else Prepare(in o,in previous,hasPrevious);
            preparedIdentity=new ForecastRequest(in o,in previous,Vector3.zero,Vector3.zero,planning:profile.planning);
            preparedHasPrevious=hasPrevious;preparedTrafficMode=traffic;preparedFinished=o.finished;
            preparedDrag=o.dragReduction;preparedPace=o.pace;preparedPreferred=o.preferredOffset;
            preparedTrafficCount=o.traffic.Length;o.traffic.CopyTo(preparedTraffic);preparedValid=true;
        }
        public bool TryUsePrepared(DynamicRejoinPlanner source,in DriverObservation o,in ContextPathState previous,bool hasPrevious,bool traffic)
        {
            if(source==null||ReferenceEquals(source,this)||!source.preparedValid)return false;
            source.preparedValid=false; // Single-use even on mismatch; never keep a stale proposal.
            if(!ReferenceEquals(profile,source.profile)||routeLocalAdjacency!=source.routeLocalAdjacency||blockingTarget!=source.blockingTarget||gapSeeking!=source.gapSeeking
                ||hasPrevious!=source.preparedHasPrevious||traffic!=source.preparedTrafficMode||o.finished!=source.preparedFinished
                ||o.dragReduction!=source.preparedDrag||o.pace!=source.preparedPace||o.preferredOffset!=source.preparedPreferred
                ||o.traffic.Length!=source.preparedTrafficCount)return false;
            var identity=new ForecastRequest(in o,in previous,Vector3.zero,Vector3.zero,planning:profile.planning);
            if(!source.preparedIdentity.Matches(in identity))return false;
            for(int i=0;i<o.traffic.Length;i++)if(!DraftingModel.Same(in o.traffic[i],in source.preparedTraffic[i]))return false;
            Count=source.Count;trafficPlan=source.trafficPlan;freshHoldIndex=source.freshHoldIndex;
            retained=source.retained;Blocker=source.Blocker;BlockerCentre=source.BlockerCentre;gapStart=source.gapStart;if(gapStart<Count)GapCandidatesAdded+=Count-gapStart;
            Horizon=source.Horizon;CornerExit=source.CornerExit;
            System.Array.Copy(source.paths,paths,Count);System.Array.Copy(source.times,times,Count);
            System.Array.Copy(source.clearance,clearance,Count);System.Array.Copy(source.order,order,Count);
            Evaluated+=Count;if(trafficPlan)trafficPlans++;
            for(int i=0;i<Count;i++)if(float.IsPositiveInfinity(times[i]))GeometricRejects++;
            return true;
        }
        // Only used after the fastest candidate fails its physical check.
        // Spend the second rollout on a route with geometric room to track,
        // rather than another near-identical boundary-grazing trajectory.
        public int FallbackRank(bool sceneryBlocked=false,bool corridorBlocked=false)
        {
            // A blocked passing route calls for the guide through the corridor,
            // not maximum distance from the road edge (which ignores scenery).
            // This only chooses the second check; it never bypasses that check.
            if(trafficPlan&&sceneryBlocked)for(int rank=1;rank<Count;rank++)
                if(Path(rank).guide&&float.IsFinite(EstimatedTime(rank)))return rank;
            // Alongside/merge occupancy invalidates a lateral manoeuvre, not
            // necessarily continued travel in the current corridor. Test the
            // fresh hold before another movement toward the same obstruction.
            if(trafficPlan&&corridorBlocked)for(int rank=1;rank<Count;rank++)
                if(order[rank]==freshHoldIndex&&float.IsFinite(EstimatedTime(rank)))return rank;
            int best=-1;
            for(int rank=1;rank<Count;rank++) {
                if(!float.IsFinite(EstimatedTime(rank)))continue;
                if(best<0||Clearance(rank)>Clearance(best)+.001f)best=rank;
            }
            return best;
        }
        void Add(in DriverObservation o,ContextPathState path,bool trafficAware=false)
        {
            paths[Count]=path;times[Count]=evaluator.EstimateGateTime(in o,in path,Horizon,trafficAware);order[Count]=Count;
            clearance[Count]=evaluator.MinimumClearance;
            if(float.IsPositiveInfinity(times[Count]))GeometricRejects++;
            Count++;Evaluated++;
        }
        public void Prepare(in DriverObservation o,in ContextPathState previous,bool hasPrevious)
        {
            Count=0;trafficPlan=false;freshHoldIndex=-1;Blocker=-1;gapStart=int.MaxValue;
            SetHorizon(in o);float exit=CornerExit;
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
            Sort(retain);
        }
        void SetHorizon(in DriverObservation o)
        {
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
        }
        // Opponent-relative targets are continuous metre offsets, not lanes.
        // Always include a fresh hold as well as the retained curve. Spend the
        // remaining budget on both passing sides and a slower lateral approach.
        // The next fresh snapshot
        // can choose a different duration/side without changing vehicle power.
        public void PrepareTraffic(in DriverObservation o,in ContextPathState previous,bool hasPrevious)
        {
            float nearest=float.PositiveInfinity,centre=o.self.offset,width=o.self.halfWidth*2+profile.planning.PassingClearance;
            int blocker=-1,nearestId=-1;
            if(blockingTarget) {
                // Generate passing sides around the car limiting this corridor's
                // attainable pace. A nearer car alongside remains an occupancy
                // constraint, but must not hide the route around the actual leader.
                var potential=new DriveCommand(1,0,o.tuning.AttainableSpeed,o.self.offset,o.self.offset);
                MovingLeaderControl.Apply(in o,potential,out int leader,out _,out bool limited);
                if(limited)blocker=leader;
            }
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                if(blocker>=0&&other.id!=blocker)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap<0&&!RoadPathFeasibility.LongitudinallyAdjacent(in o,in other,routeLocalAdjacency))continue;
                if(Mathf.Abs(gap)>=nearest)continue;
                nearest=Mathf.Abs(gap);nearestId=other.id;
                float lateral=Vector3.Dot(other.velocity,o.route.Right(other.route));
                centre=other.offset+Mathf.Clamp(lateral*.5f,-1,1);
                width=o.self.halfWidth+other.halfWidth+profile.planning.PassingClearance;
            }
            if(float.IsPositiveInfinity(nearest)){Prepare(in o,in previous,hasPrevious);return;}
            Count=0;trafficPlan=true;trafficPlans++;SetHorizon(in o);Blocker=nearestId;BlockerCentre=centre;
            bool retain=hasPrevious&&previous.localSpeed&&!previous.coast;
            if(retain)Add(in o,previous,true);
            var hold=RoadPathFeasibility.FromMotion(in o,o.self.offset,false);
            freshHoldIndex=Count;
            hold.localSpeed=true;Add(in o,hold,true);
            var guide=RoadPathFeasibility.GuideFromMotion(in o,profile);
            guide.blend=Mathf.Clamp(Mathf.Max(18+Mathf.Max(0,o.self.speed)*.9f,CornerExit),22,336);
            guide.localSpeed=true;guide.selectedOffset=guide.endOffset=profile.line.Offset(guide.anchor+guide.blend);
            Add(in o,guide,true);
            float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
            int firstPass=Count;
            for(int side=-1;side<=1;side+=2) {
                var path=RoadPathFeasibility.FromMotion(in o,Mathf.Clamp(centre+side*width,-edge,edge),false);
                path.blend=Mathf.Clamp(path.blend*.8f,16,100);path.localSpeed=true;
                Add(in o,path,true);
            }
            // Without a prior curve there is room for both longer approaches.
            // Otherwise keep the longer approach on the faster estimated side.
            int preferred=times[firstPass]<=times[firstPass+1]?firstPass:firstPass+1;
            for(int i=0;i<(retain?1:2);i++) {
                var path=paths[retain?preferred:firstPass+i];
                path.blend=Mathf.Clamp(Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60)*1.5f,16,100);
                Add(in o,path,true);
            }
            gapStart=int.MaxValue;if(gapSeeking){gapStart=Count;AddGapCandidates(in o);}
            Sort(retain);
        }
        // Lateral gaps between every car ahead or alongside (within 60 m) and to
        // both road edges, not only either side of the one blocking car. The
        // widest gaps not already covered become extra ranked candidates; each
        // still needs the ordinary physical check before it can be followed.
        readonly float[] gapLeft=new float[25],gapRight=new float[25];
        void AddGapCandidates(in DriverObservation o)
        {
            float edge=Circuit.Width*.5f-RoadPathFeasibility.Allowance-o.self.halfWidth-.1f;int blocked=0;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float gap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(gap>60||gap<=-(o.self.halfLength+other.halfLength))continue;
                float centre=other.offset+Mathf.Clamp(Vector3.Dot(other.velocity,o.route.Right(other.route))*.5f,-1,1);
                float width=o.self.halfWidth+other.halfWidth+profile.planning.PassingClearance;
                int k=blocked++;
                while(k>0&&gapLeft[k-1]>centre-width){gapLeft[k]=gapLeft[k-1];gapRight[k]=gapRight[k-1];k--;}
                gapLeft[k]=centre-width;gapRight[k]=centre+width;
            }
            if(blocked==0)return;
            // Free intervals, widest first, skipping targets within 0.75 m of an existing candidate.
            int added=0;float cursor=-edge;
            System.Span<float> centres=stackalloc float[26];System.Span<float> widths=stackalloc float[26];int free=0;
            for(int j=0;j<=blocked;j++) {
                float next=j<blocked?Mathf.Min(edge,gapLeft[j]):edge;
                if(next-cursor>0){centres[free]=(cursor+next)*.5f;widths[free]=next-cursor;free++;}
                if(j<blocked)cursor=Mathf.Max(cursor,gapRight[j]);
            }
            while(added<GapCandidates&&Count<Capacity) {
                int best=-1;for(int j=0;j<free;j++)if(widths[j]>0&&(best<0||widths[j]>widths[best]))best=j;
                if(best<0)break;
                float target=centres[best];widths[best]=0;bool covered=false;
                for(int i=0;i<Count;i++)covered|=Mathf.Abs(paths[i].endOffset-target)<.75f;
                if(covered)continue;
                var path=RoadPathFeasibility.FromMotion(in o,target,false);
                path.blend=Mathf.Clamp(path.blend*.8f,16,100);path.localSpeed=true;
                Add(in o,path,true);added++;GapCandidatesAdded++;
            }
        }
        public int GapCandidatesAdded {get;private set;}
        public int GapSelections {get;private set;}
        void Sort(bool retain)
        {
            retained=retain;biasSide=0;
            Resort();
        }
        // Stable insertion sort. Preference for a viable current curve is
        // a small time tolerance, never a reward for reaching the guide.
        void Resort()
        {
            for(int i=1;i<Count;i++) {
                int key=order[i],j=i-1;float cost=Cost(key);
                while(j>=0&&cost<Cost(order[j])){order[j+1]=order[j];j--;}
                order[j+1]=key;
            }
        }
        int biasSide;float biasPenalty,biasCentre;
        float Cost(int key)=>times[key]-(retained&&key==0?RetentionAllowance:0)+(biasSide!=0&&(paths[key].endOffset-biasCentre)*biasSide< -.5f?biasPenalty:0);
        public bool OppositeSide(int key,int side)=>(paths[key].endOffset-BlockerCentre)*side< -.5f;
        // Experiment --pass-commitment: a committed pass ranks candidates that
        // end clearly on the other side of the same blocker behind by a time
        // penalty. Ordering only; every candidate keeps its physical check.
        public void Commit(int side,float penalty)=>Commit(side,penalty,BlockerCentre);
        // Centre of the committed target car, which may differ from this plan's blocker.
        public void Commit(int side,float penalty,float centre)
        {
            if(!trafficPlan||side==0)return;
            biasSide=side;biasPenalty=penalty;biasCentre=centre;Resort();
        }
        public void Selected(int rank){Selections++;if(order[rank]>=gapStart)GapSelections++;if(trafficPlan)trafficSelections++;LastBlend=Path(rank).blend;LastEstimatedTime=EstimatedTime(rank);minimumBlend=Mathf.Min(minimumBlend,LastBlend);maximumBlend=Mathf.Max(maximumBlend,LastBlend);minimumHorizon=Mathf.Min(minimumHorizon,Horizon);maximumHorizon=Mathf.Max(maximumHorizon,Horizon);}
    }
}
