using UnityEngine;

namespace LittleRoad
{
    // Initial, inspectable traffic planner. Physics and checkpoint ownership stay
    // in RoadCar. No opponent policy, future input, or extra grip is consulted.
    public sealed class TrafficDriverPolicy:IDriverPolicy
    {
        [System.Serializable] public sealed class PathDecision
        {
            public int id,candidate,rejection,blocker;public float time,offset,conflictAfter,routeGain,travelled,cost,observedAcceleration;public bool reference,limited,clear;
            public Vector3 predictedSelf,predictedOther,observedOther;
        }
        public readonly System.Collections.Generic.List<PathDecision> PathDecisions=new();
        public int DroppedPathDecisions {get;private set;}
        readonly bool capturePaths=RaceTrial.Has("--traffic-diagnostics");
        const int Count=41;
        sealed class Path
        {
            public readonly Vector3[] point=new Vector3[Count],forward=new Vector3[Count];
            public readonly float[] speed=new float[Count],distance=new float[Count],curvature=new float[Count];
            public float offset,cost,clearance,origin,start,blend,consumed,routeGain,travelled,exitRouteSpeed;public bool clear,reference,limited;public int rejection;
            public int blocker;public float conflictAfter;public Vector3 predictedSelf,predictedOther,observedOther;
        }
        readonly RacingSpeedProfile profile;
        readonly ReferenceDriverPolicy reference;
        readonly RecoverySensor space;
        readonly ObservedMotionPredictor motionPrediction=new();
        public readonly ImmediateTrafficSafety Safety=new();
        readonly bool safetyEnabled=!RaceTrial.Has("--no-immediate-safety");
        readonly Path[] candidates={new(),new(),new(),new(),new(),new(),new(),new(),new()};
        const int PredictionSteps=12;const float PredictionStep=.25f;
        readonly ObservedCar[] observed=new ObservedCar[64];int neighbours;
        readonly Vector3[,] predictedPosition=new Vector3[64,PredictionSteps+1],predictedForward=new Vector3[64,PredictionSteps+1];
        readonly Vector3[,] roadPoint=new Vector3[Count,3],roadRight=new Vector3[Count,3];
        readonly float[,] referenceOffset=new float[Count,3];
        float pathStep,pathBlend,initialSlope;
        bool anchorValid;float anchorRoute,anchorOffset,anchorBlend,anchorConsumed,anchorLastRoute;
        public int PlanCount {get;private set;}
        public double PlanTotalMs {get;private set;}
        public double PlanMaxMs {get;private set;}
        Path chosen;double nextPlan;bool planned,selectedReference=true,emergencyBraking,referenceActive;
        float selectedOffset;
        public string Intent {get;private set;}="RaceLine";
        public string Reason {get;private set;}="Clear track";
        public int Leader {get;private set;}=-1;
        public int Rejected {get;private set;}
        public float PlannedOffset=>selectedOffset;
        public float Clearance=>chosen?.clearance??999;
        public float PredictedRouteGain=>chosen?.routeGain??0;
        public float PredictedTravel=>chosen?.travelled??0;
        public float PredictedExitRouteSpeed=>chosen?.exitRouteSpeed??0;
        public int RelevantCars=>neighbours;
        public int SelectedCandidate=>System.Array.IndexOf(candidates,chosen);
        public int ReferenceRejection {get;private set;}
        public bool WaitingForTraffic {get;private set;}
        float? fixtureLane;
        bool commandProbe;
        internal void SetCommandProbe(bool active)=>commandProbe=active;
        internal void SetLaneFixture(float offset)
        {if(!float.IsFinite(offset)||Mathf.Abs(offset)>5)throw new System.ArgumentOutOfRangeException(nameof(offset));fixtureLane=offset;}
        public TrafficDriverPolicy(RacingSpeedProfile profile,RecoverySensor space)
        {this.profile=profile;reference=new ReferenceDriverPolicy(profile);this.space=space;}

        static float Gap(float a,float b,float length)=>Mathf.Repeat(b-a+length*.5f,length)-length*.5f;
        // Route progress is common to all candidates. Path arc length is not:
        // the longer outside of a bend must not earn progress merely for distance.
        public static float RouteGain(float[] distances,float step,float travelled,out float routeScale)
        {
            int segment=0;while(segment<distances.Length-2&&distances[segment+1]<travelled)segment++;
            routeScale=step/Mathf.Max(.001f,distances[segment+1]-distances[segment]);
            return (segment+Mathf.InverseLerp(distances[segment],distances[segment+1],travelled))*step;
        }
        public static float AdvanceAnchor(float consumed,float previous,float current,float length,float blend)
        {return consumed>=blend?blend:Mathf.Clamp(consumed+Gap(previous,current,length),0,blend);}
        public static bool RelevantTraffic(float gap,float ownSpeed,float otherSpeed,float halfLengths,float braking)
        {
            if(gap<0)return -gap<halfLengths+4+Mathf.Max(0,otherSpeed-ownSpeed)*3;
            return gap<halfLengths+Mathf.Max(35,ownSpeed*3+ownSpeed*ownSpeed/(2*Mathf.Max(1,braking*.7f)));
        }
        public static float LateralRoadMotion(Vector3 velocity,Vector3 roadRight)=>Vector3.Dot(velocity,roadRight);
        bool Relevant(in DriverObservation o,in ObservedCar other)=>other.id!=o.self.id&&RelevantTraffic(Gap(o.self.route,other.route,o.route.Length),Mathf.Max(0,o.self.speed),Mathf.Max(0,other.speed),o.self.halfLength+other.halfLength,o.tuning.braking);
        static Vector3 Right(Vector3 forward)=>Vector3.Cross(Vector3.up,forward).normalized;
        Vector3 LineForward(float d)=>(profile.line.At(d+.5f)-profile.line.At(d-.5f)).normalized;
        public static float Blend(float start,float derivative,float end,float metres,float length)
        {
            float u=Mathf.Clamp01(metres/length),a=derivative*length,delta=end-start;
            return start+a*u+(10*delta-6*a)*u*u*u+(-15*delta+8*a)*u*u*u*u+(6*delta-3*a)*u*u*u*u*u;
        }
        Vector3 Point(float d,float s,float start,float slope,float end,float blend)
        {return profile.line.At(d+s)+Right(LineForward(d+s))*Blend(start,slope,end,s,blend);}
        public static float Separation(Vector3 a,Vector3 af,float aw,float al,Vector3 b,Vector3 bf,float bw,float bl)
        {return Separation(a,af,aw,al,b,bf,bw,bl,out _);}
        static float Separation(Vector3 a,Vector3 af,float aw,float al,Vector3 b,Vector3 bf,float bw,float bl,out Vector3 separatingAxis)
        {
            var ar=Right(af);var br=Right(bf);var delta=b-a;float result=float.NegativeInfinity;separatingAxis=af;
            for(int i=0;i<4;i++) {
                var axis=i==0?af:i==1?ar:i==2?bf:br;
                float span=al*Mathf.Abs(Vector3.Dot(af,axis))+aw*Mathf.Abs(Vector3.Dot(ar,axis))+bl*Mathf.Abs(Vector3.Dot(bf,axis))+bw*Mathf.Abs(Vector3.Dot(br,axis));
                float separation=Mathf.Abs(Vector3.Dot(delta,axis))-span;
                if(separation>result){result=separation;separatingAxis=axis;}
            }
            return result;
        }
        // Geometry is planned in road coordinates. A constant lane therefore
        // stays beside another car instead of copying the reference line's sweep.
        Vector3 RoadPoint(Path path,in DriverObservation o,int i,int sample,float end,bool referencePath)
        {
            float s=i*pathStep+sample-1;
            float destination=referencePath?referenceOffset[i,sample]:end;
            var p=roadPoint[i,sample]+roadRight[i,sample]*Blend(path.start,initialSlope,destination,s+path.consumed,path.blend);
            p.y=o.self.position.y;return p;
        }
        void BrakeEnvelope(Path path,in DriverObservation o)
        {
            float budget=o.tuning.roadGrip*1.65f;
            for(int i=Count-2;i>=0;i--) {
                float lateral=path.speed[i+1]*path.speed[i+1]*Mathf.Max(path.curvature[i],path.curvature[i+1]);
                float braking=Mathf.Min(o.tuning.braking,Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral)))*.8f;
                path.speed[i]=Mathf.Min(path.speed[i],Mathf.Sqrt(path.speed[i+1]*path.speed[i+1]+2*braking*(path.distance[i+1]-path.distance[i])));
            }
        }
        void Evaluate(Path path,in DriverObservation o,float end,bool referencePath)
        {
            path.clear=false;path.offset=end;path.reference=referencePath;path.clearance=999;path.rejection=0;path.limited=false;
            path.blocker=-1;path.conflictAfter=-1;path.cost=0;path.predictedSelf=path.predictedOther=path.observedOther=Vector3.zero;
            // Retain the manoeuvre's anchor while tracking it. Replacing the
            // desired path with the current tracking error at each update makes
            // a receding target which never corrects that error fully.
            bool retain=anchorValid&&selectedReference==referencePath&&Mathf.Abs(end-selectedOffset)<.2f;
            path.origin=retain?anchorRoute:o.self.route;path.start=retain?anchorOffset:o.self.offset;path.blend=retain?anchorBlend:pathBlend;
            path.consumed=retain?anchorConsumed:0;path.routeGain=0;path.travelled=0;path.exitRouteSpeed=0;
            float v=Mathf.Max(0,o.self.speed),budget=o.tuning.roadGrip*1.65f;
            path.distance[0]=0;
            for(int i=0;i<Count;i++) {
                float s=i*pathStep;var p=RoadPoint(path,in o,i,1,end,referencePath);
                var before=RoadPoint(path,in o,i,0,end,referencePath);var after=RoadPoint(path,in o,i,2,end,referencePath);
                var forward=(after-before).normalized;forward.y=0;forward.Normalize();
                path.point[i]=p;path.forward[i]=forward;
                var a=p-before;a.y=0;var b=after-p;b.y=0;
                float curvature=2*Mathf.Abs(Vector3.Cross(a,b).y)/Mathf.Max(.0001f,a.magnitude*b.magnitude*(a+b).magnitude);
                path.curvature[i]=curvature;
                path.speed[i]=Mathf.Min(profile.Speed(o.self.route+s),Mathf.Sqrt(budget*.72f/Mathf.Max(.00001f,curvature)));
                var right=roadRight[i,1];float offset=Mathf.Abs(Vector3.Dot(p-roadPoint[i,1],right));
                float footprint=o.self.halfWidth*Mathf.Abs(Vector3.Dot(Right(forward),right))+o.self.halfLength*Mathf.Abs(Vector3.Dot(forward,right));
                if(offset+footprint+.25f>Circuit.Width*.5f){path.rejection=1;return;}
                if(i>0) {
                    if(!space.StaticSweepClear(path.point[i-1],p,forward)){path.rejection=2;return;}
                    path.distance[i]=path.distance[i-1]+Vector3.Distance(path.point[i-1],p);
                }
            }
            BrakeEnvelope(path,in o);
            // A blocked future point becomes a braking constraint, not a reason
            // to discard all the usable road before it. Recheck the changed
            // trajectory against the same observations after repairing speed.
            for(int repair=0;repair<3;repair++) {
                float travelled=0,speed=v;int segment=0;bool conflict=false;
                for(int tick=1;tick<=PredictionSteps;tick++) {
                    while(segment<Count-2&&path.distance[segment+1]<travelled)segment++;
                    float target=path.speed[segment+1];
                    float force=DriveModel.EngineForce(o.tuning,speed)-DriveModel.Resistance(speed,o.tuning.mass);
                    float acceleration=Mathf.Clamp((target-speed)*3,-o.tuning.braking*.8f,force);
                    float next=Mathf.Max(0,speed+acceleration*PredictionStep);
                    travelled+=(speed+next)*.5f*PredictionStep;speed=next;
                    while(segment<Count-2&&path.distance[segment+1]<travelled)segment++;
                    float u=Mathf.InverseLerp(path.distance[segment],path.distance[segment+1],travelled);
                    var point=Vector3.Lerp(path.point[segment],path.point[segment+1],u);
                    var forward=Vector3.Slerp(path.forward[segment],path.forward[segment+1],u);
                    for(int j=0;j<neighbours;j++) {
                        var other=observed[j];
                        // A following car owns its own braking. Do not slow a
                        // car remaining in lane because a rear car might hit it.
                        float initialGap=Gap(o.self.route,other.route,o.route.Length);
                        bool continuingCorridor=referencePath
                            ?selectedReference&&Mathf.Abs(o.self.offset-profile.line.Offset(o.self.route))<.7f
                            :Mathf.Abs(end-o.self.offset)<.5f;
                        if(tick>2&&initialGap<-(o.self.halfLength+other.halfLength+2)&&continuingCorridor)continue;
                        var position=predictedPosition[j,tick];var heading=predictedForward[j,tick];
                        float separation=Separation(point,forward,o.self.halfWidth,o.self.halfLength,position,heading,other.halfWidth,other.halfLength,out var axis);
                        float closing=Mathf.Abs(Vector3.Dot(forward*speed-other.velocity,axis));
                        float margin=.3f+closing*PredictionStep*.5f;
                        path.clearance=Mathf.Min(path.clearance,separation-margin);
                        if(separation>=margin)continue;
                        path.blocker=other.id;path.conflictAfter=tick*PredictionStep;path.predictedSelf=point;path.predictedOther=position;path.observedOther=other.position;
                        // Never repair a side collision by stopping alongside it.
                        float ahead=Vector3.Dot(position-point,forward);
                        if(ahead<o.self.halfLength){path.rejection=3;return;}
                        int stop=Mathf.Max(0,segment-1);
                        // A moving leader is a speed constraint, not a parked
                        // obstacle. Match its observed predicted forward pace,
                        // giving back the clearance deficit over the approach.
                        // Keep a stop response for crossing/stationary traffic.
                        // The repaired trajectory must still pass every SAT
                        // check; an infeasible moving response is rejected.
                        float leaderSpeed=Mathf.Max(0,Vector3.Dot((predictedPosition[j,tick]-predictedPosition[j,tick-1])/PredictionStep,forward));
                        float giveBack=Mathf.Max(.5f,margin-separation)/Mathf.Max(.25f,tick*PredictionStep*.5f);
                        float followSpeed=Vector3.Dot(heading,forward)>.7f?Mathf.Max(0,leaderSpeed-giveBack):0;
                        for(int k=stop;k<Count;k++)path.speed[k]=Mathf.Min(path.speed[k],followSpeed);
                        BrakeEnvelope(path,in o);path.limited=true;conflict=true;break;
                    }
                    if(conflict)break;
                }
                if(!conflict) {
                    path.clear=true;
                    path.routeGain=RouteGain(path.distance,pathStep,travelled,out float routeScale);
                    path.travelled=travelled;path.exitRouteSpeed=speed*routeScale;
                    float switching=(referencePath!=selectedReference||Mathf.Abs(end-selectedOffset)>.6f)?3f:0;
                    float exitPreference=referencePath?0:Mathf.Abs(end-profile.line.Offset(o.self.route+path.routeGain))*.12f;
                    path.cost=-path.routeGain-path.exitRouteSpeed*.35f+switching+exitPreference;
                    return;
                }
            }
            path.rejection=3;
        }
        void Plan(in DriverObservation o)
        {
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            // Distribute expensive planning over the twelve 120 Hz physics
            // ticks in each 100 ms interval. Steering still updates every tick.
            nextPlan=(System.Math.Floor(o.timestamp*10)+1)*.1+(o.self.id%12)*(.1/12);
            if(nextPlan-o.timestamp>.105)nextPlan-=.1;
            planned=true;Leader=-1;neighbours=0;float nearest=float.MaxValue;
            if(anchorValid)anchorConsumed=AdvanceAnchor(anchorConsumed,anchorLastRoute,o.self.route,o.route.Length,anchorBlend);
            anchorLastRoute=o.self.route;ReferenceRejection=0;
            float v=Mathf.Max(0,o.self.speed);
            pathStep=Mathf.Max(65,v*4,v*v/(2*Mathf.Max(1,o.tuning.braking*.7f))+25)/(Count-1);pathBlend=Mathf.Max(25,v*1.5f);
            // Match the road tangent for the tracking target. Re-anchoring its
            // tangent to a drifting chassis every 100 ms otherwise perpetuates
            // that drift, moving the intended lane outwards on each replan.
            initialSlope=0;
            for(int i=0;i<Count;i++)for(int sample=0;sample<3;sample++) {
                float d=o.self.route+i*pathStep+sample-1;
                roadPoint[i,sample]=o.route.At(d);roadRight[i,sample]=o.route.Right(d);referenceOffset[i,sample]=profile.line.Offset(d);
            }
            foreach(var other in o.traffic)if(other.id!=o.self.id)motionPrediction.Observe(other.id,o.timestamp,Mathf.Max(0,other.speed));
            foreach(var other in o.traffic) {
                if(!Relevant(in o,in other))continue;
                if(neighbours>=observed.Length)break;
                int j=neighbours++;observed[j]=other;
                for(int tick=0;tick<=PredictionSteps;tick++) {
                    float t=tick*PredictionStep,travel=ObservedMotionPredictor.Travel(other.speed,motionPrediction.Acceleration(other.id),t),route=other.route+travel;
                    var heading=o.route.Forward(route);
                    // Offsets are road coordinates. Car-right measures tyre slip,
                    // and misses a lane change driven with little or no sideslip.
                    float lateral=LateralRoadMotion(other.velocity,o.route.Right(other.route))*Mathf.Min(t,.5f);
                    var roadPosition=o.route.At(route)+o.route.Right(route)*(other.offset+lateral);roadPosition.y=other.position.y;
                    float follow=Mathf.Clamp01(t/.75f);
                    var immediate=other.position+other.velocity*t+other.forward*(travel-Mathf.Max(0,other.speed)*t);
                    predictedPosition[j,tick]=Vector3.Lerp(immediate,roadPosition,follow);
                    predictedForward[j,tick]=Vector3.Slerp(other.forward,heading,follow);
                }
                float gap=Gap(o.self.route,other.route,o.route.Length);
                if(gap>0&&gap<nearest&&Mathf.Abs(other.offset-o.self.offset)<o.self.halfWidth+other.halfWidth+.4f){nearest=gap;Leader=other.id;}
            }
            WaitingForTraffic=Leader>=0&&nearest<20&&o.self.speed<2;
            float best=float.MaxValue;chosen=null;Path brakingPath=null;float brakingCost=float.MaxValue;emergencyBraking=false;Rejected=0;int roadRejected=0,staticRejected=0,trafficRejected=0;
            for(int i=0;i<candidates.Length;i++) {
                if(fixtureLane.HasValue&&i!=1)continue;
                bool referencePath=i==0;
                float end=i==0?0:i==1?Mathf.Clamp(selectedReference?o.self.offset:selectedOffset,-5,5):i==2?-5:i==3?5:i==4?0:i==5?Mathf.Clamp(o.self.offset-3.6f,-5,5):i==6?Mathf.Clamp(o.self.offset+3.6f,-5,5):i==7?selectedOffset:profile.line.Offset(o.self.route+45);
                if(fixtureLane.HasValue)end=fixtureLane.Value;
                if(i==7&&selectedReference)continue;
                Evaluate(candidates[i],in o,end,referencePath);
                if(capturePaths){var p=candidates[i];if(PathDecisions.Count<30000)PathDecisions.Add(new PathDecision{id=o.self.id,candidate=i,time=(float)o.timestamp,offset=p.offset,reference=p.reference,limited=p.limited,clear=p.clear,rejection=p.rejection,blocker=p.blocker,conflictAfter=p.conflictAfter,routeGain=p.routeGain,travelled=p.travelled,cost=p.cost,predictedSelf=p.predictedSelf,predictedOther=p.predictedOther,observedOther=p.observedOther,observedAcceleration=motionPrediction.Acceleration(p.blocker)});else DroppedPathDecisions++;}
                if(referencePath)ReferenceRejection=candidates[i].rejection;
                if(candidates[i].rejection==3) {
                    float cost=referencePath?1:Mathf.Abs(end-o.self.offset);
                    if(cost<brakingCost){brakingCost=cost;brakingPath=candidates[i];}
                }
                if(!candidates[i].clear){Rejected++;if(candidates[i].rejection==1)roadRejected++;else if(candidates[i].rejection==2)staticRejected++;else trafficRejected++;continue;}
                if(candidates[i].cost<best){best=candidates[i].cost;chosen=candidates[i];}
                // With no interacting traffic, recover a useful corner approach.
                // Validate this transition rather than snapping the car onto it.
                if(referencePath&&neighbours==0)break;
            }
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            PlanCount++;PlanTotalMs+=ms;PlanMaxMs=System.Math.Max(PlanMaxMs,ms);
            if(chosen==null){chosen=brakingPath;emergencyBraking=true;WaitingForTraffic=neighbours>0&&o.self.speed<2;Intent="Brake / hold";Reason=$"No reachable path: road {roadRejected}, scenery {staticRejected}, traffic {trafficRejected}";return;}
            selectedOffset=chosen.offset;selectedReference=chosen.reference;
            anchorValid=true;anchorRoute=chosen.origin;anchorOffset=chosen.start;anchorBlend=chosen.blend;anchorConsumed=chosen.consumed;
            if(chosen.limited){WaitingForTraffic=neighbours>0&&o.self.speed<2;Intent="Follow";Reason="Match moving traffic within checked clearance";}
            else if(!selectedReference&&Mathf.Abs(selectedOffset-o.self.offset)>.6f&&Leader>=0){Intent=selectedOffset<o.self.offset?"Pass left":"Pass right";Reason="Faster feasible path through observed space";}
            else {Intent=selectedReference?(neighbours==0?"Rejoin line":"RaceLine"):"Hold lane";Reason=selectedReference?"Use feasible reference corner approach":"Maintain useful road corridor";}
        }
        public DriveCommand Decide(in DriverObservation o)
        {
            var plannedCommand=PlanCommand(in o);
            return safetyEnabled?Safety.Apply(in o,in plannedCommand):plannedCommand;
        }
        DriveCommand PlanCommand(in DriverObservation o)
        {
            // Explicit straight-line safety-component test, never used in races.
            if(commandProbe)return new DriveCommand(1,0,o.tuning.AttainableSpeed,o.self.offset,o.self.offset);
            float d=profile.line.Project(o.self.position,o.self.route,out float error);
            bool nearby=false;
            foreach(var other in o.traffic)if(Relevant(in o,in other)){nearby=true;break;}
            // Ordinary tracking error is handled by the reference controller.
            // Once joined, do not keep switching controllers at a 0.7 m boundary.
            if(!fixtureLane.HasValue&&!nearby&&(referenceActive||Mathf.Abs(error)<.7f)){referenceActive=true;Intent="RaceLine";Reason="Clear track; no interacting traffic";chosen=null;planned=false;anchorValid=false;selectedReference=true;selectedOffset=0;WaitingForTraffic=false;neighbours=0;Leader=-1;ReferenceRejection=0;return reference.Decide(in o);}
            referenceActive=false;
            if(!planned||o.timestamp>=nextPlan)Plan(in o);
            if(chosen==null)return new DriveCommand(o.self.speed>.1f?-1:0,0,0,o.self.offset,selectedOffset);
            int nearest=0;float minimum=float.MaxValue,coordinate=0;
            for(int i=0;i<Count-1;i++) {
                var edge=chosen.point[i+1]-chosen.point[i];float u=Mathf.Clamp01(Vector3.Dot(o.self.position-chosen.point[i],edge)/Mathf.Max(.001f,edge.sqrMagnitude));
                float distance=(chosen.point[i]+edge*u-o.self.position).sqrMagnitude;
                if(distance<minimum){minimum=distance;nearest=i;coordinate=Mathf.Lerp(chosen.distance[i],chosen.distance[i+1],u);}
            }
            float look=Mathf.Clamp(5+Mathf.Max(o.self.speed,0)*.4f,6,19);float wanted=coordinate+look;int aim=nearest;
            while(aim<Count-2&&chosen.distance[aim+1]<wanted)aim++;
            var targetPoint=Vector3.Lerp(chosen.point[aim],chosen.point[aim+1],Mathf.InverseLerp(chosen.distance[aim],chosen.distance[aim+1],wanted));
            var delta=targetPoint-o.self.position;float angle=Vector3.SignedAngle(o.self.forward,delta,Vector3.up)*Mathf.Deg2Rad;
            float curve=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steer=Mathf.Clamp(curve*Mathf.Max(o.self.speed,3)*1.09f/o.tuning.yaw,-1,1);
            float target=chosen.speed[Mathf.Min(Count-1,nearest+1)];target=Mathf.Min(target,Mathf.Sqrt(o.tuning.roadGrip*1.65f*.85f/Mathf.Max(.00001f,Mathf.Abs(curve))));
            if(o.finished)target=Mathf.Min(target,18);
            int next=Mathf.Min(Count-1,nearest+1);
            float feedforward=(chosen.speed[next]*chosen.speed[next]-chosen.speed[nearest]*chosen.speed[nearest])/(2*Mathf.Max(.01f,chosen.distance[next]-chosen.distance[nearest]));
            if(emergencyBraking){target=0;feedforward=0;}
            float force=feedforward+(target-o.self.speed)*3+DriveModel.Resistance(o.self.speed,o.tuning.mass);
            float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
            return new DriveCommand(pedal,steer,target,o.self.offset,selectedReference?profile.line.Offset(d):selectedOffset);
        }
    }
}
