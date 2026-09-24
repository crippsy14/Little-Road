using UnityEngine;

namespace LittleRoad
{
    // Experimental F1-inspired track-width context selection. This class issues
    // ordinary inputs only; it never invokes the legacy traffic path planner.
    public sealed class ContextDriverPolicy : IDriverPolicy
    {
        const int Slots=ContextPreferenceMap.Slots;
        readonly RacingSpeedProfile profile;
        readonly DynamicRejoinPlanner dynamicRejoin;
        readonly bool dynamicTraffic=RaceTrial.Has("--dynamic-traffic");
        readonly bool dynamicTrafficFallback=RaceTrial.Has("--dynamic-traffic-fallback");
        public DynamicRejoinPlanner DynamicRejoin=>dynamicRejoin;
        readonly ReferenceDriverPolicy reference;
        readonly RecoverySensor scenery;
        readonly ContextPreferenceMap preferences,prospectivePreferences;
        readonly ContextPathFollower follower;
        readonly RoadPathFeasibility roadCheck;
        public readonly ContextPlanAudit PlanAudit=RaceTrial.Has("--context-plan-audit")?new ContextPlanAudit():null;
        public readonly ContextPlanAudit SwitchAudit=RaceTrial.Has("--context-switch-audit")?new ContextPlanAudit():null;
        public readonly ContextPlanAudit ContinuationAudit=RaceTrial.Has("--continuation-audit")?new ContextPlanAudit():null;
        public ContextPlanAudit OpeningAudit {get;private set;}
        readonly OpeningCaptureWindow openingWindow=RaceTrial.Has("--opening-decision-audit")?new OpeningCaptureWindow():null;
        public ForecastWork PreparedContinuation {get;private set;}
        readonly bool parallelContinuation=RaceTrial.Has("--parallel-continuation-forecast");
        readonly bool compareContinuation=RaceTrial.Has("--compare-continuation");
        public readonly ContextPlanAudit FollowingAudit=RaceTrial.Has("--following-fallback-audit")?new ContextPlanAudit():null;
        readonly FollowingFallbackScope followingScope=FollowingFallbackSelection.Parse(RaceTrial.Arg("--following-fallback-scope","all"));
        readonly ObservedTrafficRollout followingFallback=RaceTrial.Has("--following-feasibility-fallback")?new ObservedTrafficRollout(RaceTrial.Has("--shared-observed-traffic")):null;
        public int FollowingSharedHits=>followingFallback?.SharedHits??0;
        public int FollowingSharedMisses=>followingFallback?.SharedMisses??0;
        public int FollowingSharedProjectedCars=>followingFallback?.SharedProjectedCars??0;
        public int FollowingFallbackAttempts {get;private set;}
        public int FollowingFallbackAccepted {get;private set;}
        readonly bool comparePassCompletion=RaceTrial.Has("--compare-pass-completion");
        public int PassCompletionComparisons {get;private set;}
        readonly bool retainPassTrajectory=RaceTrial.Has("--retain-pass-trajectory");
        public int TrajectoryAttempts {get;private set;}
        public int TrajectoryAccepted {get;private set;}
        public int TrajectoryRejected {get;private set;}
        readonly TrafficProgressComparison trafficProgress;
        public TrafficProgressComparison ProgressDiagnostics=>trafficProgress;
        public int ContinuationAttempts {get;private set;}
        public int ContinuationAccepted {get;private set;}
        public bool RoadFeasibilityEnabled=>roadCheck!=null;
        public bool RoadUnresolved {get;private set;}
        public bool TrafficUnresolved {get;private set;}
        readonly bool coastContinuation=RaceTrial.Has("--traffic-coast-continuation");
        bool coastPath;
        public bool CoastContinuationActive=>coastPath&&pathActive;
        public int CoastAttempts {get;private set;}
        public int CoastAccepted {get;private set;}
        public int RoadRejected {get;private set;}
        public int RoadCalls=>roadCheck?.Calls??0;
        public RoadPathFeasibility RoadDiagnostics=>roadCheck;
        public readonly ImmediateTrafficSafety Safety=new();
        readonly bool safetyEnabled=!RaceTrial.Has("--no-immediate-safety");
        // Component comparison only; ordinary races always use anticipation.
        readonly bool rearAnticipationEnabled=!RaceTrial.Has("--no-rear-anticipation");
        double nextPlan;
        bool pathActive,commandProbe,pathRejoin,guidePath,localSpeedPath;
        readonly bool guideFallbackEnabled=RaceTrial.Has("--guide-path-fallback");
        readonly bool timedCorridors=RaceTrial.Has("--timed-rear-corridors");
        bool PathRejoin=>guidePath||localSpeedPath?pathRejoin:RelevantCars==0;
        public int GuideAttempts {get;private set;}
        public int GuideAccepted {get;private set;}
        bool useCheckedReference,referenceClear;
        float staticStopDistance=999,staticCheckedRoute;
        public int StaticRejected {get;private set;}
        public bool CheckedReference=>useCheckedReference;
        public float StaticLimit {get;private set;}=999;
        internal void SetCommandProbe(bool active)=>commandProbe=active;
        float anchor,startOffset,startSlope,endOffset,blend,consumed,lastRoute,guideOffset,guideSlope;
        public string Intent {get;private set;}="Reference";
        public string Reason {get;private set;}="Clear track";
        public float SelectedOffset {get;private set;}
        public float FollowingOffset=>endOffset;
        public bool TryGetPath(out ContextPathState path)
        {
            path=new ContextPathState{anchor=anchor,consumed=consumed,startOffset=startOffset,startSlope=startSlope,
                endOffset=endOffset,blend=blend,selectedOffset=SelectedOffset,rejoin=PathRejoin,guide=guidePath,guideOffset=guideOffset,guideSlope=guideSlope,coast=coastPath,localSpeed=localSpeedPath};
            return pathActive;
        }
        public float ReachableLeft=>preferences.ReachableLeft;
        public float ReachableRight=>preferences.ReachableRight;
        public float LeaderLimit {get;private set;}=999;
        public int Leader {get;private set;}=-1;
        public int RelevantCars=>preferences.RelevantCars;
        public int RearApproaches=>preferences.RearApproaches;
        public bool WaitingForTraffic {get;private set;}
        public int PlanCount {get;private set;}
        public double PlanTotalMs {get;private set;}
        public double PlanMaxMs {get;private set;}
        public int DecisionCount {get;private set;}
        public double DecisionTotalMs {get;private set;}
        public double DecisionMaxMs {get;private set;}
        // Actual command stages, for attribution only. Values never feed back
        // into selection/control. Timestamp lets diagnostics reject stale data.
        public double PedalTimestamp {get;private set;}
        public float FollowerPedal {get;private set;}
        public float FollowerTarget {get;private set;}
        public float AfterLeaderPedal {get;private set;}
        public float BeforeSafetyPedal {get;private set;}
        public float AppliedPedal {get;private set;}

        public ContextDriverPolicy(RacingSpeedProfile profile,RecoverySensor scenery=null,Vector3? colliderCentre=null,Vector3? colliderHalf=null)
        {
            if(RaceTrial.Has("--dynamic-rejoin"))dynamicRejoin=new DynamicRejoinPlanner(profile,dynamicTraffic,RaceTrial.Has("--dynamic-blocking-target"));
            if(RaceTrial.Has("--traffic-aware-progress"))trafficProgress=new TrafficProgressComparison(profile);
            if(followingFallback==null&&followingScope!=FollowingFallbackScope.All)throw new System.ArgumentException("Component scope requires following fallback enabled.");
            if(followingFallback!=null&&!RaceTrial.Has("--road-feasibility"))throw new System.ArgumentException("Following fallback requires current physical road checks.");
            if(comparePassCompletion&&(!compareContinuation||trafficProgress==null))throw new System.ArgumentException("Pass completion comparison requires traffic-aware continuation.");
            if(retainPassTrajectory&&!RaceTrial.Has("--road-feasibility"))throw new System.ArgumentException("Retaining a trajectory requires current physical road checks.");
            if(parallelContinuation&&(!compareContinuation||!RaceTrial.Has("--parallel-road-forecasts")))throw new System.ArgumentException("Parallel continuation requires joined road workers and continuation comparison.");
            this.profile=profile;this.scenery=scenery;bool gapCentres=!RaceTrial.Has("--no-gap-centres"),blockingOnly=RaceTrial.Has("--anticipate-blocking-car"),anticipateSpace=RaceTrial.Has("--anticipate-passing-space")||blockingOnly;preferences=new ContextPreferenceMap(profile,gapCentres,anticipateSpace,blockingOnly,RaceTrial.Has("--rear-pressure-filter"),RaceTrial.Has("--open-space-priority"),RaceTrial.Has("--integrated-passing-progress"),dynamicTraffic);prospectivePreferences=new ContextPreferenceMap(profile,gapCentres,anticipateSpace,blockingOnly,RaceTrial.Has("--rear-pressure-filter"),RaceTrial.Has("--open-space-priority"),RaceTrial.Has("--integrated-passing-progress"),dynamicTraffic);reference=new ReferenceDriverPolicy(profile);follower=new ContextPathFollower(profile);
            if(RaceTrial.Has("--road-feasibility")) {
                if(!colliderCentre.HasValue||!colliderHalf.HasValue)throw new System.ArgumentException("Road feasibility needs actual collider geometry.");
                roadCheck=new RoadPathFeasibility(profile,colliderCentre.Value,colliderHalf.Value,scenery,parallelContinuation);
            }
        }
        public ForecastWork PrepareForecast(in DriverObservation o)
        {
            PreparedContinuation=null;
            // The initial solo study ranks a bounded shortlist on the main thread.
            // Avoid preparing an unrelated legacy candidate for this mode.
            if(dynamicRejoin!=null&&!o.finished)return null;
            if(roadCheck==null||commandProbe||(PlanCount>0&&o.timestamp<nextPlan))return null;
            // Never advance the path/plan counters here: recovery may suppress
            // the subsequent decision. Use independent scratch preferences.
            prospectivePreferences.Fill(in o,pathActive,endOffset,rearAnticipationEnabled,true,timedCorridors);
            if(prospectivePreferences.Pick(out float selected)<0)return null;
            ContextPathState previous=default;bool hadPrevious=(retainPassTrajectory||parallelContinuation)&&TryGetPath(out previous);
            var path=CandidatePath(in o,selected,prospectivePreferences.RelevantCars,in previous,hadPrevious,out _);
            var primary=roadCheck.Prepare(in o,in path);
            // Speculate only the existing fresh continuation on a large proposed
            // switch. A different winning candidate still uses serial fallback.
            // This neither advances planning nor promises acceptance.
            if(parallelContinuation&&compareContinuation&&hadPrevious&&prospectivePreferences.RelevantCars>0&&!previous.guide&&!path.guide
                &&Mathf.Abs(path.selectedOffset-previous.selectedOffset)>=o.self.halfWidth*2+.4f) {
                var continuation=RoadPathFeasibility.FromMotion(in o,previous.selectedOffset,false);
                PreparedContinuation=roadCheck.PrepareContinuation(in o,in continuation);
            }
            return primary;
        }
        ContextPathState CandidatePath(in DriverObservation o,float selected,int relevant,in ContextPathState previous,bool hadPrevious,out bool retained)
        {
            ContextPathState candidate=default;
            retained=retainPassTrajectory&&hadPrevious&&PassTrajectory.TryRetain(in o,in previous,lastRoute,selected,relevant,out candidate);
            if(retained)return candidate;
            return RoadPathFeasibility.FromMotion(in o,selected,relevant==0);
        }
        static float Gap(float a,float b,float length)=>Mathf.Repeat(b-a+length*.5f,length)-length*.5f;
        // A sampled preference is interpolated, not used as a lane destination.
        public static float PeakOffset(float left,float centre,float right,float spacing)
        {
            float curvature=left-2*centre+right;
            return curvature<-.00001f?Mathf.Clamp(.5f*(left-right)/curvature,-.5f,.5f)*spacing:0;
        }
        // Reserve an occupied passing corridor during the time needed for our
        // lateral transition. This is a relative-motion constraint, not a lane
        // assignment or a reason for the leading car to slow down. Cars already
        // behind in our own corridor retain responsibility for their following.
        public static bool RearApproachDuringMerge(in DriverObservation o,in ObservedCar other,float gap)
        {
            if(gap>=0||other.id==o.self.id)return false;
            float width=o.self.halfWidth+other.halfWidth+.4f;
            if(Mathf.Abs(other.offset-o.self.offset)<width)return false;
            var along=o.route.Forward(o.self.route);
            float ownSpeed=Mathf.Max(0,Vector3.Dot(o.self.velocity,along));
            float closing=Vector3.Dot(other.velocity,along)-ownSpeed;
            if(closing<=.8f)return false;
            float transition=Mathf.Clamp(18+ownSpeed*.9f,22,60);
            float horizon=Mathf.Min(3,transition/Mathf.Max(3,ownSpeed));
            float clear=-gap-o.self.halfLength-other.halfLength-3;
            return clear<closing*horizon;
        }
        bool SceneryClear(in DriverObservation o,float destination,bool useReference,out float clearDistance)
        {
            clearDistance=999;if(scenery==null)return true;
            float transition=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60);
            float slope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route))),-.35f,.35f);
            var previous=o.self.position;var heading=o.self.forward;float travelled=0;
            for(float ahead=3;ahead<=transition+18;ahead+=3) {
                float route=o.self.route+ahead;
                float offset=pathActive?ContextPathFollower.Smooth(startOffset,startSlope,destination,consumed+ahead,blend):ContextPathFollower.Smooth(o.self.offset,slope,destination,ahead,transition);
                var point=useReference?profile.line.At(route):o.route.At(route)+o.route.Right(route)*offset;
                // Reference fallback also checks the actual approach from the
                // observed pose, not a disconnected line through the obstacle.
                if(useReference)point+=(o.self.position-profile.line.At(o.self.route))*Mathf.Clamp01(1-ahead/transition);
                var direction=(point-previous).normalized;
                if(!scenery.StaticSweepClear(previous,point,heading,direction)){clearDistance=travelled;return false;}
                travelled+=Vector3.Distance(previous,point);previous=point;heading=direction;
            }
            return true;
        }
        void Select(in DriverObservation o)
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            bool captureOpening=openingWindow!=null&&openingWindow.Capture(o.self.id,o.timestamp,o.self.speed,o.finished);
            if(captureOpening&&OpeningAudit==null)OpeningAudit=new ContextPlanAudit(192);
            OpeningAudit?.Begin();FollowingAudit?.Begin();
            PlanAudit?.Begin();
            ContinuationAudit?.Begin();
            SwitchAudit?.Begin();ContextPathState priorPath=default;bool hadPriorPath=(SwitchAudit!=null||compareContinuation||retainPassTrajectory||dynamicRejoin!=null||openingWindow!=null||FollowingAudit!=null)&&TryGetPath(out priorPath);
            preferences.Fill(in o,pathActive,endOffset,rearAnticipationEnabled,roadCheck!=null,timedCorridors);
            SwitchAudit?.CapturePreferences(preferences);
            ContinuationAudit?.CapturePreferences(preferences);
            OpeningAudit?.CapturePreferences(preferences);FollowingAudit?.CapturePreferences(preferences);
            float edge=preferences.Edge,spacing=preferences.Spacing,guide=preferences.Guide;
            StaticRejected=0;useCheckedReference=false;staticStopDistance=999;staticCheckedRoute=o.self.route;
            referenceClear=SceneryClear(in o,guide,true,out float referenceDistance);
            int best=-1;float selected=Mathf.Clamp(o.self.offset,-edge,edge);
            int sceneryFailuresBefore=roadCheck?.SceneryFailures??0;
            int corridorFailuresBefore=roadCheck?.CorridorFailures??0;
            int trafficFailuresBefore=roadCheck?.TrafficFailures??0;
            TrafficUnresolved=false;
            RoadRejected=0;RoadUnresolved=false;ContextPathState roadPath=default,firstPath=default;bool roadAccepted=false,acceptedGuide=false,hasFirstPath=false,acceptedFollowing=false;
            bool useDynamic=dynamicRejoin!=null&&!o.finished&&(RelevantCars==0||dynamicTraffic)&&roadCheck!=null;
            if(useDynamic) {
                if(dynamicTraffic&&RelevantCars>0)dynamicRejoin.PrepareTraffic(in o,in priorPath,hadPriorPath);
                else dynamicRejoin.Prepare(in o,in priorPath,hadPriorPath);
                for(int attempt=0;attempt<Mathf.Min(DynamicRejoinPlanner.PhysicalShortlist,dynamicRejoin.Count);attempt++) {
                    int rank=attempt==0?0:dynamicRejoin.FallbackRank(roadCheck.SceneryFailures>sceneryFailuresBefore,roadCheck.CorridorFailures>corridorFailuresBefore);
                    if(attempt>0&&dynamicTrafficFallback&&roadCheck.TrafficFailures>trafficFailuresBefore) {
                        int trafficRank=roadCheck.TrafficFallbackRank(in o,dynamicRejoin);
                        if(trafficRank>=0)rank=trafficRank;
                    }
                    if(rank<0)break;
                    if(float.IsPositiveInfinity(dynamicRejoin.EstimatedTime(rank)))continue;
                    var candidate=dynamicRejoin.Path(rank);
                    roadPath=candidate;
                    if(!hasFirstPath){firstPath=candidate;hasFirstPath=true;}
                    float left=dynamicTraffic&&ReachableLeft>-edge?ReachableLeft:float.NegativeInfinity;
                    float right=dynamicTraffic&&ReachableRight<edge?ReachableRight:float.PositiveInfinity;
                    bool accepted=roadCheck.Check(in o,in candidate,left,right,dynamicTraffic&&timedCorridors?preferences:null);
                    if(PlanAudit!=null||OpeningAudit!=null){var audit=roadCheck.LastCandidate;PlanAudit?.Add(in audit);OpeningAudit?.Add(in audit);}
                    if(!accepted){RoadRejected++;continue;}
                    roadPath=candidate;roadAccepted=true;acceptedGuide=candidate.guide;selected=candidate.selectedOffset;best=Slots;
                    dynamicRejoin.Selected(rank);break;
                }
            }
            // Check in preference order. Usually only the winning continuous
            // target needs a sweep; blocked choices cannot win on score alone.
            for(int attempt=0;!useDynamic&&attempt<(roadCheck==null?preferences.Count:6);attempt++) {
                // A wall-blocked passing preference may need checked guide following.
                // Road-edge/traffic failures alone do not justify a different
                // tactical choice; keep their original sixth candidate.
                bool sceneryBlocked=guideFallbackEnabled&&roadCheck!=null&&roadCheck.SceneryFailures>sceneryFailuresBefore;
                bool tryGuide=sceneryBlocked&&attempt==5;
                bool tryHold=!tryGuide&&roadCheck!=null&&roadCheck.TrafficFailures>trafficFailuresBefore&&attempt==5;
                float picked=selected;best=tryGuide||tryHold?Slots:preferences.Pick(out picked);
                if(best>=0&&!tryGuide&&!tryHold)selected=picked;
                if(best<0) {
                    if(sceneryBlocked)tryGuide=true;
                    else if(roadCheck!=null&&roadCheck.TrafficFailures>trafficFailuresBefore){tryHold=true;best=Slots;}
                    else break;
                }
                if(tryGuide) {
                    float transition=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60);
                    selected=profile.line.Offset(o.self.route+transition);best=Slots;GuideAttempts++;
                }
                if(tryHold)selected=o.self.offset;
                float sample=tryGuide||tryHold?selected:preferences.Sample(best);
                if(roadCheck!=null||SceneryClear(in o,selected,false,out _)) {
                    if(roadCheck==null)break;
                    bool retained=false;
                    roadPath=tryGuide?RoadPathFeasibility.GuideFromMotion(in o,profile):tryHold?RoadPathFeasibility.FromMotion(in o,selected,RelevantCars==0):CandidatePath(in o,selected,RelevantCars,in priorPath,hadPriorPath,out retained);
                    if(tryGuide){roadPath.endOffset=selected;roadPath.selectedOffset=selected;}
                    if(!hasFirstPath&&!tryGuide&&!tryHold){firstPath=roadPath;hasFirstPath=true;}
                    float corridorLeft=ReachableLeft<=-edge?float.NegativeInfinity:ReachableLeft;
                    float corridorRight=ReachableRight>=edge?float.PositiveInfinity:ReachableRight;
                    bool accepted=roadCheck.Check(in o,in roadPath,corridorLeft,corridorRight,timedCorridors?preferences:null);
                    if(retained) {
                        TrajectoryAttempts++;
                        if(accepted){TrajectoryAccepted++;selected=roadPath.selectedOffset;}
                        else {
                            TrajectoryRejected++;RoadRejected++;retained=false;
                            if(PlanAudit!=null||SwitchAudit!=null||ContinuationAudit!=null||OpeningAudit!=null){var rejected=roadCheck.LastCandidate;PlanAudit?.Add(in rejected);SwitchAudit?.Add(in rejected);ContinuationAudit?.Add(in rejected);OpeningAudit?.Add(in rejected);}
                            // The old shape may now be unsafe while a fresh route
                            // to the same destination remains physically viable.
                            roadPath=RoadPathFeasibility.FromMotion(in o,selected,RelevantCars==0);
                            accepted=roadCheck.Check(in o,in roadPath,corridorLeft,corridorRight,timedCorridors?preferences:null);
                        }
                    }
                    if(PlanAudit!=null||SwitchAudit!=null||ContinuationAudit!=null||OpeningAudit!=null){var candidate=roadCheck.LastCandidate;PlanAudit?.Add(in candidate);SwitchAudit?.Add(in candidate);ContinuationAudit?.Add(in candidate);OpeningAudit?.Add(in candidate);}
                    if(retained&&accepted)ContinuationAudit?.Record(in o,ReachableLeft,ReachableRight,false,priorPath,roadPath,"retained-trajectory");
                    if(accepted){roadAccepted=true;acceptedGuide=tryGuide;break;}
                    RoadRejected++;
                    if(tryGuide||tryHold){best=-1;break;}
                    preferences.reachable[best]=false;best=-1;continue;
                }
                if(selected!=sample&&SceneryClear(in o,sample,false,out _)){selected=sample;break;}
                preferences.reachable[best]=false;StaticRejected++;best=-1;
            }
            if(coastContinuation&&roadCheck!=null&&!roadAccepted&&roadCheck.TrafficFailures>trafficFailuresBefore) {
                // Try the last attempted continuation with a torque cap before
                // resorting to emergency braking. Recheck the full own-body
                // road/scenery/corridor and moving-traffic constraints.
                // One extra bounded candidate; no unchecked steering override.
                var capped=roadPath;capped.coast=true;CoastAttempts++;
                float left=ReachableLeft<=-edge?float.NegativeInfinity:ReachableLeft;
                float right=ReachableRight>=edge?float.PositiveInfinity:ReachableRight;
                bool accepted=roadCheck.Check(in o,in capped,left,right,timedCorridors?preferences:null);
                if(PlanAudit!=null||SwitchAudit!=null||ContinuationAudit!=null||OpeningAudit!=null){var candidate=roadCheck.LastCandidate;PlanAudit?.Add(in candidate);SwitchAudit?.Add(in candidate);ContinuationAudit?.Add(in candidate);OpeningAudit?.Add(in candidate);}
                if(accepted){roadPath=capped;roadAccepted=true;acceptedGuide=capped.guide;selected=capped.selectedOffset;best=Slots;CoastAccepted++;}
                else RoadRejected++;
            }
            if(followingFallback!=null&&FollowingFallbackSelection.Applies(followingScope,o.self.id)&&roadCheck!=null&&!roadAccepted&&hasFirstPath&&roadCheck.TrafficFailures>trafficFailuresBefore) {
                FollowingFallbackAttempts++;followingFallback.Prepare(in o);
                float left=ReachableLeft<=-edge?float.NegativeInfinity:ReachableLeft;
                float right=ReachableRight>=edge?float.PositiveInfinity:ReachableRight;
                bool accepted=roadCheck.Check(in o,in firstPath,left,right,timedCorridors?preferences:null,followingFallback);
                // Only the model-tagged opening audit receives this alternative;
                // legacy nominal-only replay buffers must not mislabel it.
                if(OpeningAudit!=null){var candidate=roadCheck.LastCandidate;OpeningAudit.Add(in candidate);}
                if(accepted){roadPath=firstPath;roadAccepted=true;acceptedGuide=useDynamic&&firstPath.guide;selected=firstPath.selectedOffset;best=Slots;FollowingFallbackAccepted++;acceptedFollowing=true;
                    if(FollowingAudit!=null){var candidate=roadCheck.LastCandidate;FollowingAudit.Add(in candidate);FollowingAudit.Record(in o,ReachableLeft,ReachableRight,false,priorPath,firstPath,"following-fallback-accepted");}
                }
                else RoadRejected++;
            }
            bool smallPassReturn=comparePassCompletion&&roadAccepted&&hadPriorPath
                &&PassingTargetContinuation.ShouldCompare(in o,roadPath.selectedOffset,priorPath.selectedOffset,profile.Speed(o.self.route+10));
            if(!useDynamic&&compareContinuation&&roadAccepted&&!acceptedFollowing&&hadPriorPath&&RelevantCars>0&&!priorPath.guide&&!roadPath.guide
                &&(Mathf.Abs(roadPath.selectedOffset-priorPath.selectedOffset)>=o.self.halfWidth*2+.4f||smallPassReturn)) {
                if(smallPassReturn)PassCompletionComparisons++;
                // A preferred destination is not necessarily a faster manoeuvre.
                // Compare one freshly anchored continuation before a large switch.
                // It must pass the same road/scenery/corridor/traffic checks and
                // gain over a whole body length in the nominal two-second forecast.
                // Live leader and emergency controls still apply; no pace guarantee.
                float chosenGain=roadCheck.PredictedRouteGain;var proposalPath=roadPath;ContinuationAttempts++;
                var continuation=RoadPathFeasibility.FromMotion(in o,priorPath.selectedOffset,RelevantCars==0);
                float left=ReachableLeft<=-edge?float.NegativeInfinity:ReachableLeft;
                float right=ReachableRight>=edge?float.PositiveInfinity:ReachableRight;
                bool accepted=roadCheck.Check(in o,in continuation,left,right,timedCorridors?preferences:null);
                if(PlanAudit!=null||SwitchAudit!=null||ContinuationAudit!=null||OpeningAudit!=null){var candidate=roadCheck.LastCandidate;PlanAudit?.Add(in candidate);SwitchAudit?.Add(in candidate);ContinuationAudit?.Add(in candidate);OpeningAudit?.Add(in candidate);}
                bool prefer=accepted&&(trafficProgress!=null?trafficProgress.PreferContinuation(in o,in proposalPath,in continuation):roadCheck.PredictedRouteGain>chosenGain+o.self.halfLength*2);
                if(prefer) {
                    roadPath=continuation;selected=continuation.selectedOffset;acceptedGuide=false;best=Slots;ContinuationAccepted++;
                    ContinuationAudit?.Record(in o,ReachableLeft,ReachableRight,false,priorPath,continuation,"chosen-continuation",proposalPath);
                }
            }
            // If walls deny a pass, use the checked guide and let the existing
            // moving-leader speed constraint do the following. Do not weave
            // among tiny residual offsets inside a one-car corridor.
            // An empty interval caused by adjacent cars is not evidence of a
            // scenery blockage. Rejoining the guide then can steer into them.
            if(roadCheck!=null) {
                RoadUnresolved=!roadAccepted;
                TrafficUnresolved=RoadUnresolved&&roadCheck.TrafficFailures>trafficFailuresBefore;
                if(RoadUnresolved) {
                    if(useDynamic)PlanAudit?.Record(in o,ReachableLeft,ReachableRight,false,priorPath,default,"failed-dynamic-selection");
                    else PlanAudit?.Record(in o,ReachableLeft,ReachableRight);
                }
                if(roadAccepted&&!acceptedFollowing&&hadPriorPath&&!o.finished&&Mathf.Abs(roadPath.selectedOffset-priorPath.selectedOffset)>=o.self.halfWidth*2+.4f)
                    SwitchAudit?.Record(in o,ReachableLeft,ReachableRight,true,priorPath,roadPath);
                if(roadAccepted) {
                    coastPath=roadPath.coast;localSpeedPath=roadPath.localSpeed;
                    guidePath=acceptedGuide;guideOffset=roadPath.guideOffset;guideSlope=roadPath.guideSlope;pathRejoin=roadPath.rejoin;if(acceptedGuide)GuideAccepted++;
                    anchor=roadPath.anchor;consumed=roadPath.consumed;startOffset=roadPath.startOffset;startSlope=roadPath.startSlope;
                    endOffset=roadPath.endOffset;blend=roadPath.blend;lastRoute=o.self.route;pathActive=true;
                } else {
                    // Explicit unresolved experiment state: retain the prior
                    // path rather than mislabel an unchecked reference as safe.
                    selected=pathActive?endOffset:o.self.offset;
                }
            }
            useCheckedReference=roadCheck==null&&referenceClear&&StaticRejected>0&&(best<0||preferences.Danger(in o,selected)>.1f);
            if(useCheckedReference){selected=guide;pathActive=false;}
            if(best<0&&!referenceClear){staticStopDistance=referenceDistance;if(roadCheck==null){useCheckedReference=true;pathActive=false;}}
            SelectedOffset=selected;
            bool needsPath=!useCheckedReference&&(RelevantCars>0||pathActive||!referenceClear);
            if(needsPath&&!localSpeedPath&&(!pathActive||consumed>blend+20)) {
                if(!pathActive){endOffset=selected;pathRejoin=RelevantCars==0;guidePath=false;}
                anchor=o.self.route;startOffset=o.self.offset;consumed=0;lastRoute=o.self.route;
                float forward=Vector3.Dot(o.self.velocity,o.route.Forward(o.self.route));
                startSlope=Mathf.Clamp(Vector3.Dot(o.self.velocity,o.route.Right(o.self.route))/Mathf.Max(3,forward),-.35f,.35f);
                blend=Mathf.Clamp(18+Mathf.Max(0,o.self.speed)*.9f,22,60);pathActive=true;
            }
            if(roadCheck==null&&referenceClear&&RelevantCars==0&&Mathf.Abs(o.self.offset-profile.line.Offset(o.self.route))<.6f)pathActive=false;
            Intent=RelevantCars==0?(pathActive?"Rejoin":"Reference"):Mathf.Abs(selected-o.self.offset)>.6f?"Pass":"Follow";
            Reason=RelevantCars==0?"Clear space; use reference guidance":"Reachable continuous track-width preference";
            if(RearApproaches>0){Intent="Hold passing space";Reason="Rear closing motion occupies the proposed merge corridor";}
            if(useCheckedReference){Intent=staticStopDistance<999?"Blocked route":"Follow";Reason=staticStopDistance<999?"Brake before blocked scenery":"Scenery blocks passing space; follow checked reference";}
            if(guidePath&&!RoadUnresolved){Intent="Follow checked guide";Reason="Preferred passing paths blocked; physical guide path accepted";}
            if(RoadUnresolved){Intent="Unresolved road path";Reason="No checked path among bounded candidates; prior path retained without safety claim";}
            if(useDynamic&&!RoadUnresolved){Intent=dynamicTraffic&&RelevantCars>0?"Dynamic traffic path":"Dynamic rejoin";Reason="Checked local curve and speed to shared downstream gate";}
            if(captureOpening) {
                TryGetPath(out var committed);
                OpeningAudit.Record(in o,ReachableLeft,ReachableRight,false,priorPath,committed,RoadUnresolved?"opening-unresolved":acceptedFollowing?"opening-following-accepted":"opening-accepted");
            }
            nextPlan=o.timestamp+.1;PlanCount++;
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            PlanTotalMs+=ms;PlanMaxMs=System.Math.Max(PlanMaxMs,ms);
        }
        DriveCommand Follow(in DriverObservation o)
        {
            if(!pathActive)return reference.Decide(in o);
            var path=new ContextPathState{anchor=anchor,consumed=consumed,startOffset=startOffset,startSlope=startSlope,
                endOffset=endOffset,blend=blend,selectedOffset=SelectedOffset,rejoin=PathRejoin,guide=guidePath,guideOffset=guideOffset,guideSlope=guideSlope,coast=coastPath,localSpeed=localSpeedPath};
            return follower.Follow(in o,in path);
        }
        public DriveCommand Decide(in DriverObservation o)
        {
            // Includes selection, follower, moving-leader constraint and guard.
            // Per-call CPU cost does not establish a rendered-frame AI budget.
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            var command=PlanCommand(in o);
            BeforeSafetyPedal=command.pedal;
            var applied=safetyEnabled?Safety.Apply(in o,in command):command;
            PedalTimestamp=o.timestamp;AppliedPedal=applied.pedal;
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            DecisionCount++;DecisionTotalMs+=ms;DecisionMaxMs=System.Math.Max(DecisionMaxMs,ms);
            return applied;
        }
        DriveCommand PlanCommand(in DriverObservation o)
        {
            // Explicit safety-component fixture only; never activated by races.
            if(commandProbe){Intent="Command probe";Reason="Declared full-throttle disturbance";FollowerPedal=AfterLeaderPedal=1;FollowerTarget=o.tuning.AttainableSpeed;return new DriveCommand(1,0,o.tuning.AttainableSpeed,o.self.offset,o.self.offset);}
            if(pathActive){consumed+=Mathf.Max(0,Gap(lastRoute,o.self.route,o.route.Length));lastRoute=o.self.route;}
            if(PlanCount==0||o.timestamp>=nextPlan)Select(in o);
            // Preference updates are continuous. The old 0.75 m retarget gate
            // quantised actual approaches even though selection was continuous.
            if(pathActive)endOffset=Mathf.MoveTowards(endOffset,SelectedOffset,2.5f*o.stepSeconds);
            var command=Follow(in o);Leader=-1;LeaderLimit=999;WaitingForTraffic=false;
            if(TrafficUnresolved) {
                // No checked continuation: stop commanding the old crossing.
                // Ordinary steering response/braking physics apply. This is an
                // explicit emergency outcome, never labelled a safe accepted path.
                command=new DriveCommand(-1,0,0,command.predictedOffset,command.chosenLane);
                Reason="No viable crossing continuation; straighten and brake";
            }
            FollowerPedal=command.pedal;FollowerTarget=command.targetSpeed;
            command=MovingLeaderControl.Apply(in o,command,out int leader,out float leaderLimit,out bool limited);
            Leader=leader;LeaderLimit=leaderLimit;
            if(limited){Reason="Moving leader stopping-distance limit";WaitingForTraffic=o.self.speed<2;}
            AfterLeaderPedal=command.pedal;StaticLimit=999;
            if(staticStopDistance<999) {
                float remaining=Mathf.Max(0,staticStopDistance-Mathf.Max(0,Gap(staticCheckedRoute,o.self.route,o.route.Length))-2);
                StaticLimit=Mathf.Sqrt(2*Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f)*.7f*remaining);
                float force=(StaticLimit-o.self.speed)*3+DraftingModel.Resistance(o.self.speed,o.tuning.mass,o.dragReduction);
                float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
                command=new DriveCommand(Mathf.Min(command.pedal,pedal),command.steering,Mathf.Min(command.targetSpeed,StaticLimit),command.predictedOffset,command.chosenLane);
                Reason="Stopping-distance limit before blocked scenery";
            }
            return command;
        }
    }
}
