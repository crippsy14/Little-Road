using UnityEngine;

namespace LittleRoad
{
    // Opt-in integration experiment. Nominal road feasibility only; moving
    // traffic constraints and emergency braking remain in ContextDriverPolicy.
    public sealed class RoadPathFeasibility
    {
        public const float Allowance=.10f;
        ContextPathPrediction forecast;readonly Vector3 centre,half;readonly RecoverySensor scenery;
        readonly DriverPlanningSettings planning;
        readonly RacingSpeedProfile profile;
        readonly TrafficPathFeasibility traffic=RaceTrial.Has("--traffic-path-check")?new TrafficPathFeasibility():null;
        public int TrafficFailures {get;private set;}
        public double TrafficMs {get;private set;}
        readonly bool preparedPath=RaceTrial.Has("--prepared-path");
        readonly ForecastWork work,continuationWork;
        ForecastRequest continuationRequest;bool continuationPending;
        public int ContinuationPrepared {get;private set;}
        public int ContinuationReused {get;private set;}
        ForecastRequest prefetchedRequest;
        bool pending,followingForecast;
        public int PrefetchedCalls {get;private set;}
        public int SynchronousCalls {get;private set;}
        public int PreparedRequests {get;private set;}
        readonly bool auditEnabled=RaceTrial.Has("--context-plan-audit")||RaceTrial.Has("--context-switch-audit")||RaceTrial.Has("--continuation-audit")||RaceTrial.Has("--opening-decision-audit")||RaceTrial.Has("--following-fallback-audit");
        public ContextPlanAudit.Candidate LastCandidate {get;private set;}
        public int Calls {get;private set;}
        public float PredictedRouteGain {get;private set;}
        public double ForecastMs {get;private set;}
        public double CorridorMs {get;private set;}
        public double SceneryMs {get;private set;}
        public int CurveFallbackCalls {get;private set;}
        public int CurveFallbackCandidates {get;private set;}
        public int CurveFallbackChoices {get;private set;}
        public double CurveFallbackMs {get;private set;}
        public int RoadFailures {get;private set;}
        public int CorridorFailures {get;private set;}
        public int SceneryFailures {get;private set;}
        static long Stamp()=>System.Diagnostics.Stopwatch.GetTimestamp();
        static double Ms(long started)=>(Stamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        public RoadPathFeasibility(RacingSpeedProfile profile,Vector3 centre,Vector3 half,RecoverySensor scenery,bool prepareContinuation=false)
        {this.profile=profile;planning=profile.planning;forecast=new ContextPathPrediction(profile);this.centre=centre;this.half=half;this.scenery=scenery;work=new ForecastWork(forecast);if(prepareContinuation)continuationWork=new ForecastWork(new ContextPathPrediction(profile));}
        public bool CurveTrafficConflict(in DriverObservation o,in ContextPathState path,out float after)
        {
            after=-1;
            if(traffic==null||forecast.Count==0)return true;
            return traffic.CurveConflict(in o,in path,profile.line,forecast,centre,half,out after);
        }
        public int TrafficFallbackRank(in DriverObservation o,DynamicRejoinPlanner planner)
        {
            long started=Stamp();CurveFallbackCalls++;int selected=-1;
            for(int rank=1;rank<planner.Count;rank++) {
                if(!float.IsFinite(planner.EstimatedTime(rank)))continue;
                var path=planner.Path(rank);CurveFallbackCandidates++;
                if(!CurveTrafficConflict(in o,in path,out _)){selected=rank;CurveFallbackChoices++;break;}
            }
            CurveFallbackMs+=Ms(started);return selected;
        }
        public ForecastWork Prepare(in DriverObservation o,in ContextPathState path)
        {
            continuationPending=false;
            prefetchedRequest=new ForecastRequest(in o,in path,centre,half,allowance:Allowance,prepared:preparedPath,planning:planning);
            work.Set(in prefetchedRequest,o.traffic);pending=true;PreparedRequests++;return work;
        }
        public ForecastWork PrepareContinuation(in DriverObservation o,in ContextPathState path)
        {
            if(continuationWork==null)throw new System.InvalidOperationException("Continuation forecast was not enabled at construction.");
            continuationRequest=new ForecastRequest(in o,in path,centre,half,allowance:Allowance,prepared:preparedPath,planning:planning);
            if(pending&&prefetchedRequest.Matches(in continuationRequest))throw new System.ArgumentException("Continuation must differ from the primary forecast.");
            continuationWork.Set(in continuationRequest,o.traffic);continuationPending=true;ContinuationPrepared++;return continuationWork;
        }
        public static ContextPathState FromMotion(in DriverObservation o,float destination,bool rejoin)
        {
            float forward=Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route));
            return new ContextPathState{anchor=o.self.route,startOffset=o.self.offset,
                startSlope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,forward),-.35f,.35f),
                endOffset=destination,selectedOffset=destination,blend=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60),rejoin=rejoin};
        }
        public static ContextPathState GuideFromMotion(in DriverObservation o,RacingSpeedProfile profile)
        {
            var path=FromMotion(in o,profile.line.Offset(o.self.route),false);
            path.guide=true;path.guideOffset=profile.line.Offset(path.anchor);
            path.guideSlope=(profile.line.Offset(path.anchor+1)-profile.line.Offset(path.anchor-1))*.5f;
            return path;
        }
        public static bool LongitudinallyAdjacent(in DriverObservation o,in ObservedCar other,bool routeLocal=false)
        {
            // World projection alone confuses separate parallel/hairpin track
            // sections. This broad route-distance neighbourhood exceeds a road
            // width plus both longitudinal half-extents; overlap is checked below.
            // Swept world-space collision checks remain independent of this
            // tactical classification, including at physically crossing roads.
            if(routeLocal) {
                float routeGap=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(Mathf.Abs(routeGap)>Circuit.Width+o.self.halfLength+other.halfLength)return false;
            }
            var along=o.route.Forward(o.self.route);
            float own=ProjectedRadius(in o.self,along),theirs=ProjectedRadius(in other,along);
            var a=o.self.position+o.self.right*o.self.colliderCentre.x+o.self.forward*o.self.colliderCentre.z;
            var b=other.position+other.right*other.colliderCentre.x+other.forward*other.colliderCentre.z;
            return Mathf.Abs(Vector3.Dot(b-a,along))<own+theirs+.10f;
        }
        static float ProjectedRadius(in ObservedCar car,Vector3 axis)
        {return car.halfLength*Mathf.Abs(Vector3.Dot(car.forward,axis))+car.halfWidth*Mathf.Abs(Vector3.Dot(car.right,axis));}
        void Audit(in DriverObservation o,in ContextPathState path,string reason,int sample,float offset)
        {
            if(!auditEnabled)return;
            var item=forecast.samples[Mathf.Clamp(sample,0,forecast.Count-1)];
            var last=forecast.samples[forecast.Count-1];
            LastCandidate=new ContextPlanAudit.Candidate{path=path,reason=reason,forecastModel=followingForecast?"moving-leader":"nominal",afterSeconds=item.time,offset=offset,roadBound=forecast.envelope.MaximumBound,
                predictedRouteGain=Mathf.Repeat(last.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f,
                predictedEndSpeed=Vector3.Dot(last.state.velocity,last.state.forward),
                position=item.state.position,velocity=item.state.velocity,forward=item.state.forward};
        }
        public bool Check(in DriverObservation o,in ContextPathState path,float left,float right,ContextPreferenceMap timedCorridor=null,ObservedTrafficRollout following=null)
        {
            Calls++;followingForecast=following!=null;long started=Stamp();
            var request=new ForecastRequest(in o,in path,centre,half,allowance:Allowance,prepared:preparedPath,planning:planning);
            bool reuse=following==null&&pending&&work.Completed&&prefetchedRequest.Matches(in request)&&work.MatchesTraffic(o.traffic);
            // A nominal worker result has no traffic rollout in its identity.
            // Traffic-constrained checks must use this owned synchronous model.
            bool reuseContinuation=following==null&&!reuse&&continuationPending&&continuationWork.Completed&&continuationRequest.Matches(in request)&&continuationWork.MatchesTraffic(o.traffic);
            pending=false; // Preserve primary one-check consumption semantics.
            forecast=reuseContinuation?continuationWork.prediction:work.prediction;
            if(following!=null)continuationPending=false; // A nominal result cannot serve a following model.
            if(reuseContinuation){continuationPending=false;ContinuationReused++;PrefetchedCalls++;}
            else if(reuse)PrefetchedCalls++;
            else {forecast.Predict(in o,path,centre,half,2,false,3,true,Allowance,preparedPath,following);SynchronousCalls++;}
            // Main-thread lookup/serial time only. Worker time and batch wait
            // are recorded separately in the shared snapshot frame audit.
            ForecastMs+=Ms(started);
            var final=forecast.samples[forecast.Count-1];
            PredictedRouteGain=Mathf.Repeat(final.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
            if(!forecast.envelope.Contained){RoadFailures++;Audit(in o,in path,"road",Mathf.RoundToInt(forecast.envelope.FirstUncertainTime/o.stepSeconds),0);return false;}
            if(traffic!=null) {
                started=Stamp();bool conflict=traffic.Conflict(in o,forecast,centre,half,out int other,out float after);TrafficMs+=Ms(started);
                if(conflict){TrafficFailures++;Audit(in o,in path,"moving side entry: "+other,Mathf.RoundToInt(after/o.stepSeconds),0);return false;}
            }
            // Preserve the currently reachable centre corridor throughout the
            // nominal path, not only at its destination. This is not a forecast
            // of future opponent steering and is not collision certification.
            var road=(Circuit)o.route;
            for(int i=0;i<forecast.Count;i+=6) {
                started=Stamp();road.Nearest(forecast.samples[i].state.position,out float offset);CorridorMs+=Ms(started);
                float activeLeft=left,activeRight=right;
                timedCorridor?.BoundsAt(forecast.samples[i].time,out activeLeft,out activeRight);
                if(offset<activeLeft||offset>activeRight){CorridorFailures++;Audit(in o,in path,"corridor",i,offset);return false;}
                if(i>0&&scenery!=null) {
                    var a=forecast.samples[i-6].state;var b=forecast.samples[i].state;
                    started=Stamp();bool clear=scenery.StaticSweepClear(a.position,b.position,a.forward,b.forward);SceneryMs+=Ms(started);
                    if(!clear){SceneryFailures++;Audit(in o,in path,"scenery",i,offset);return false;}
                }
            }
            Audit(in o,in path,"accepted",0,o.self.offset);return true;
        }
    }
}
