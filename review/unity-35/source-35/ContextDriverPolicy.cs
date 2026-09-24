using UnityEngine;

namespace LittleRoad
{
    // Experimental F1-inspired track-width context selection. This class issues
    // ordinary inputs only; it never invokes the legacy traffic path planner.
    public sealed class ContextDriverPolicy : IDriverPolicy
    {
        const int Slots=ContextPreferenceMap.Slots;
        readonly RacingSpeedProfile profile;
        readonly ReferenceDriverPolicy reference;
        readonly RecoverySensor scenery;
        readonly ContextPreferenceMap preferences,prospectivePreferences;
        readonly ContextPathFollower follower;
        readonly RoadPathFeasibility roadCheck;
        public bool RoadFeasibilityEnabled=>roadCheck!=null;
        public bool RoadUnresolved {get;private set;}
        public int RoadRejected {get;private set;}
        public int RoadCalls=>roadCheck?.Calls??0;
        public RoadPathFeasibility RoadDiagnostics=>roadCheck;
        public readonly ImmediateTrafficSafety Safety=new();
        readonly bool safetyEnabled=!RaceTrial.Has("--no-immediate-safety");
        // Component comparison only; ordinary races always use anticipation.
        readonly bool rearAnticipationEnabled=!RaceTrial.Has("--no-rear-anticipation");
        double nextPlan;
        bool pathActive,commandProbe;
        bool useCheckedReference,referenceClear;
        float staticStopDistance=999,staticCheckedRoute;
        public int StaticRejected {get;private set;}
        public bool CheckedReference=>useCheckedReference;
        public float StaticLimit {get;private set;}=999;
        internal void SetCommandProbe(bool active)=>commandProbe=active;
        float anchor,startOffset,startSlope,endOffset,blend,consumed,lastRoute;
        public string Intent {get;private set;}="Reference";
        public string Reason {get;private set;}="Clear track";
        public float SelectedOffset {get;private set;}
        public float FollowingOffset=>endOffset;
        public bool TryGetPath(out ContextPathState path)
        {
            path=new ContextPathState{anchor=anchor,consumed=consumed,startOffset=startOffset,startSlope=startSlope,
                endOffset=endOffset,blend=blend,selectedOffset=SelectedOffset,rejoin=RelevantCars==0};
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

        public ContextDriverPolicy(RacingSpeedProfile profile,RecoverySensor scenery=null,Vector3? colliderCentre=null,Vector3? colliderHalf=null)
        {
            this.profile=profile;this.scenery=scenery;preferences=new ContextPreferenceMap(profile);prospectivePreferences=new ContextPreferenceMap(profile);reference=new ReferenceDriverPolicy(profile);follower=new ContextPathFollower(profile);
            if(RaceTrial.Has("--road-feasibility")) {
                if(!colliderCentre.HasValue||!colliderHalf.HasValue)throw new System.ArgumentException("Road feasibility needs actual collider geometry.");
                roadCheck=new RoadPathFeasibility(profile,colliderCentre.Value,colliderHalf.Value,scenery);
            }
        }
        public ForecastWork PrepareForecast(in DriverObservation o)
        {
            if(roadCheck==null||commandProbe||(PlanCount>0&&o.timestamp<nextPlan))return null;
            // Never advance the path/plan counters here: recovery may suppress
            // the subsequent decision. Use independent scratch preferences.
            prospectivePreferences.Fill(in o,pathActive,endOffset,rearAnticipationEnabled,true);
            if(prospectivePreferences.Pick(out float selected)<0)return null;
            var path=RoadPathFeasibility.FromMotion(in o,selected,prospectivePreferences.RelevantCars==0);
            return roadCheck.Prepare(in o,in path);
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
            preferences.Fill(in o,pathActive,endOffset,rearAnticipationEnabled,roadCheck!=null);
            float edge=preferences.Edge,spacing=preferences.Spacing,guide=preferences.Guide;
            StaticRejected=0;useCheckedReference=false;staticStopDistance=999;staticCheckedRoute=o.self.route;
            referenceClear=SceneryClear(in o,guide,true,out float referenceDistance);
            int best=-1;float selected=Mathf.Clamp(o.self.offset,-edge,edge);
            RoadRejected=0;RoadUnresolved=false;ContextPathState roadPath=default;bool roadAccepted=false;
            // Check in preference order. Usually only the winning continuous
            // target needs a sweep; blocked choices cannot win on score alone.
            for(int attempt=0;attempt<(roadCheck==null?Slots:6);attempt++) {
                best=preferences.Pick(out float picked);
                if(best<0)break;
                float sample=-edge+best*spacing;selected=picked;
                if(roadCheck!=null||SceneryClear(in o,selected,false,out _)) {
                    if(roadCheck==null)break;
                    roadPath=RoadPathFeasibility.FromMotion(in o,selected,RelevantCars==0);
                    float corridorLeft=ReachableLeft<=-edge?float.NegativeInfinity:ReachableLeft;
                    float corridorRight=ReachableRight>=edge?float.PositiveInfinity:ReachableRight;
                    if(roadCheck.Check(in o,in roadPath,corridorLeft,corridorRight)){roadAccepted=true;break;}
                    preferences.reachable[best]=false;RoadRejected++;best=-1;continue;
                }
                if(selected!=sample&&SceneryClear(in o,sample,false,out _)){selected=sample;break;}
                preferences.reachable[best]=false;StaticRejected++;best=-1;
            }
            // If walls deny a pass, use the checked guide and let the existing
            // moving-leader speed constraint do the following. Do not weave
            // among tiny residual offsets inside a one-car corridor.
            // An empty interval caused by adjacent cars is not evidence of a
            // scenery blockage. Rejoining the guide then can steer into them.
            if(roadCheck!=null) {
                RoadUnresolved=!roadAccepted;
                if(roadAccepted) {
                    anchor=roadPath.anchor;consumed=0;startOffset=roadPath.startOffset;startSlope=roadPath.startSlope;
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
            if(needsPath&&(!pathActive||consumed>blend+20)) {
                if(!pathActive)endOffset=selected;
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
            if(RoadUnresolved){Intent="Unresolved road path";Reason="No checked path among bounded candidates; prior path retained without safety claim";}
            nextPlan=o.timestamp+.1;PlanCount++;
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            PlanTotalMs+=ms;PlanMaxMs=System.Math.Max(PlanMaxMs,ms);
        }
        DriveCommand Follow(in DriverObservation o)
        {
            if(!pathActive)return reference.Decide(in o);
            var path=new ContextPathState{anchor=anchor,consumed=consumed,startOffset=startOffset,startSlope=startSlope,
                endOffset=endOffset,blend=blend,selectedOffset=SelectedOffset,rejoin=RelevantCars==0};
            return follower.Follow(in o,in path);
        }
        public DriveCommand Decide(in DriverObservation o)
        {
            // Includes selection, follower, moving-leader constraint and guard.
            // Per-call CPU cost does not establish a rendered-frame AI budget.
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            var command=PlanCommand(in o);
            var applied=safetyEnabled?Safety.Apply(in o,in command):command;
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            DecisionCount++;DecisionTotalMs+=ms;DecisionMaxMs=System.Math.Max(DecisionMaxMs,ms);
            return applied;
        }
        DriveCommand PlanCommand(in DriverObservation o)
        {
            // Explicit safety-component fixture only; never activated by races.
            if(commandProbe){Intent="Command probe";Reason="Declared full-throttle disturbance";return new DriveCommand(1,0,o.tuning.AttainableSpeed,o.self.offset,o.self.offset);}
            if(pathActive){consumed+=Mathf.Max(0,Gap(lastRoute,o.self.route,o.route.Length));lastRoute=o.self.route;}
            if(PlanCount==0||o.timestamp>=nextPlan)Select(in o);
            // Preference updates are continuous. The old 0.75 m retarget gate
            // quantised actual approaches even though selection was continuous.
            if(pathActive)endOffset=Mathf.MoveTowards(endOffset,SelectedOffset,2.5f*o.stepSeconds);
            var command=Follow(in o);Leader=-1;LeaderLimit=999;WaitingForTraffic=false;
            foreach(var other in o.traffic) {
                if(other.id==o.self.id)continue;
                float gap=Gap(o.self.route,other.route,o.route.Length);
                if(gap<=0||gap>100)continue;
                float width=o.self.halfWidth+other.halfWidth+.35f;
                if(Mathf.Abs(other.offset-o.self.offset)>width)continue;
                float clearance=gap-o.self.halfLength-other.halfLength-1.5f;
                float brake=Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f)*.7f;
                float limit=Mathf.Sqrt(Mathf.Max(0,other.speed*other.speed+2*brake*Mathf.Max(0,clearance-o.self.speed*.25f)));
                if(limit<LeaderLimit){LeaderLimit=limit;Leader=other.id;}
            }
            if(LeaderLimit<command.targetSpeed) {
                float force=(LeaderLimit-o.self.speed)*3+DriveModel.Resistance(o.self.speed,o.tuning.mass);
                float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
                command=new DriveCommand(Mathf.Min(command.pedal,pedal),command.steering,LeaderLimit,command.predictedOffset,command.chosenLane);
                Reason="Moving leader stopping-distance limit";WaitingForTraffic=o.self.speed<2;
            }
            StaticLimit=999;
            if(staticStopDistance<999) {
                float remaining=Mathf.Max(0,staticStopDistance-Mathf.Max(0,Gap(staticCheckedRoute,o.self.route,o.route.Length))-2);
                StaticLimit=Mathf.Sqrt(2*Mathf.Min(o.tuning.braking,o.tuning.roadGrip*1.65f)*.7f*remaining);
                float force=(StaticLimit-o.self.speed)*3+DriveModel.Resistance(o.self.speed,o.tuning.mass);
                float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
                command=new DriveCommand(Mathf.Min(command.pedal,pedal),command.steering,Mathf.Min(command.targetSpeed,StaticLimit),command.predictedOffset,command.chosenLane);
                Reason="Stopping-distance limit before blocked scenery";
            }
            return command;
        }
    }
}
