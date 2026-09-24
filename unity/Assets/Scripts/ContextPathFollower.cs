using UnityEngine;

namespace LittleRoad
{
    [System.Serializable] public struct ContextPathState
    {
        public float anchor,consumed,startOffset,startSlope,endOffset,blend,selectedOffset;
        public bool rejoin,guide,coast,localSpeed;
        public float guideOffset,guideSlope;
    }
    // One follower for actual inputs and prospective physical rollouts. Buffers
    // belong to each instance so a forecast cannot overwrite the live path.
    public sealed class ContextPathFollower
    {
        const int Points=41,MaximumPoints=121;const float Step=3;
        readonly RacingSpeedProfile profile;
        readonly bool pathSpeedEnvelope,localOriginCurvature;
        public float MinimumClearance {get;private set;}
        bool equipmentCached;float cachedPower,cachedGear,cachedEquipmentSpeed;
        readonly bool coastWithinEnvelope=RaceTrial.Has("--coast-within-envelope");
        readonly Vector3[] points=new Vector3[MaximumPoints];
        readonly float[] speeds=new float[MaximumPoints],distances=new float[MaximumPoints],curvatures=new float[MaximumPoints];
        struct RankedTraffic {public float initial,longitudinal,sideVelocity,offset,halfWidth,halfLength;}
        readonly RankedTraffic[] rankedTraffic=new RankedTraffic[24];
        public ContextPathFollower(RacingSpeedProfile profile,bool? pathSpeedEnvelope=null,bool? localOriginCurvature=null){this.profile=profile;this.pathSpeedEnvelope=pathSpeedEnvelope??false;this.localOriginCurvature=localOriginCurvature??RaceTrial.Has("--local-origin-curvature");}
        public static float Smooth(float start,float slope,float end,float distance,float length)
        {
            float t=Mathf.Clamp01(distance/length),a=slope*length,d=end-start;
            return start+a*t+(10*d-6*a)*t*t*t+(-15*d+8*a)*t*t*t*t+(6*d-3*a)*t*t*t*t*t;
        }
        public static float Lateral(in ContextPathState path,float along,float guideOffset)
        {
            if(path.guide)return guideOffset+Smooth(path.startOffset-path.guideOffset,path.startSlope-path.guideSlope,0,along,path.blend);
            return Smooth(path.startOffset,path.startSlope,path.endOffset,along,path.blend);
        }
        void BuildEnvelope(in DriverObservation o,in ContextPathState path,ForecastPathCache cache,int count=Points,float step=Step)
        {
            float budget=o.tuning.roadGrip*1.65f;
            bool ownEnvelope=path.localSpeed||(pathSpeedEnvelope&&!path.guide);
            if(ownEnvelope&&(!equipmentCached||cachedPower!=o.tuning.power||cachedGear!=o.tuning.gearCeiling)) {
                cachedPower=o.tuning.power;cachedGear=o.tuning.gearCeiling;cachedEquipmentSpeed=o.tuning.AttainableSpeed;equipmentCached=true;
            }
            float equipmentSpeed=cachedEquipmentSpeed;
            for(int i=0;i<count;i++) {
                float along=path.consumed+i*step,d=path.anchor+along;
                if(cache==null||!cache.TryPoint(along,i*step,out points[i])) {
                    float side=Lateral(in path,along,path.guide?profile.line.Offset(d):0);
                    // When traffic has cleared, progressively reconnect to the guide.
                    if(path.rejoin)side=Mathf.Lerp(side,profile.line.Offset(d),Mathf.SmoothStep(0,1,i*step/65));
                    points[i]=o.route.At(d)+o.route.Right(d)*side;
                }
                distances[i]=i==0?0:distances[i-1]+Vector3.Distance(points[i],points[i-1]);
                // A different traffic path has different corner entry/exit
                // geometry. Do not inherit an already acceleration/braking-
                // limited guide speed at every point on that alternate path.
                // Anchor the far end to the known beyond-horizon guide profile;
                // local curvature and the backward brake pass remain below.
                speeds[i]=ownEnvelope&&i<count-1?equipmentSpeed:profile.Speed(d);
            }
            for(int i=1;i<count-1;i++) {
                var a=points[i]-points[i-1];var b=points[i+1]-points[i];
                float curvature=2*Mathf.Abs(Vector3.Cross(a,b).y)/Mathf.Max(.001f,a.magnitude*b.magnitude*(a+b).magnitude);
                curvatures[i]=curvature;
                speeds[i]=Mathf.Min(speeds[i],Mathf.Sqrt(budget*.78f/Mathf.Max(.00001f,curvature)));
            }
            // The first local point has no preceding sample. Use the first
            // measured segment curvature as its conservative one-sided limit.
            // Without it, backward propagation inflates speed[0] to exactly
            // one maximum-braking step above speed[1], producing false brake
            // feed-forward on every update even far below the corner limit.
            if(ownEnvelope&&localOriginCurvature) {
                curvatures[0]=curvatures[1];
                speeds[0]=Mathf.Min(speeds[0],Mathf.Sqrt(budget*.78f/Mathf.Max(.00001f,curvatures[0])));
            }
            for(int i=count-2;i>=0;i--)speeds[i]=Mathf.Min(speeds[i],Mathf.Sqrt(speeds[i+1]*speeds[i+1]+2*Mathf.Min(o.tuning.braking,budget)*profile.planning.BrakeFraction*(distances[i+1]-distances[i])));
        }
        // Cheap common downstream-gate estimate (120-360m). This is a geometry/capability ranking,
        // not a promise of feasible actual motion or a substitute for rollouts.
        public float EstimateGateTime(in DriverObservation o,in ContextPathState path,float horizon=120,bool trafficAware=false)
        {
            MinimumClearance=float.PositiveInfinity;
            horizon=Mathf.Clamp(horizon,120,360);
            int count=Mathf.Clamp(Mathf.CeilToInt(horizon/Step)+1,Points,MaximumPoints);float step=horizon/(count-1);
            BuildEnvelope(in o,in path,null,count,step);
            // These values depend only on this immutable observation, not on
            // the downstream sample or estimated arrival time. Preserve order.
            int trafficCount=0;
            if(trafficAware)foreach(var other in o.traffic){
                if(other.id==o.self.id)continue;
                float initial=Mathf.Repeat(other.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
                if(initial<=0||initial>120)continue;
                if(trafficCount==rankedTraffic.Length)throw new System.InvalidOperationException("Ranking traffic capacity exceeded.");
                rankedTraffic[trafficCount++]=new RankedTraffic{initial=initial,
                    longitudinal=Mathf.Max(0,Vector3.Dot(other.velocity,o.route.Forward(other.route))),
                    sideVelocity=Vector3.Dot(other.velocity,o.route.Right(other.route)),offset=other.offset,halfWidth=other.halfWidth,halfLength=other.halfLength};
            }
            float speed=Mathf.Min(Mathf.Max(0,o.self.speed),speeds[0]),time=0,budget=o.tuning.roadGrip*1.65f;
            for(int i=0;i<count-1;i++) {
                float d=path.anchor+path.consumed+i*step;var right=o.route.Right(d);
                var forward=(points[i+1]-points[i]).normalized;
                float offset=Vector3.Dot(points[i]-o.route.At(d),right);
                float extent=o.self.halfWidth*Mathf.Abs(Vector3.Dot(Vector3.Cross(Vector3.up,forward),right))
                    +o.self.halfLength*Mathf.Abs(Vector3.Dot(forward,right));
                MinimumClearance=Mathf.Min(MinimumClearance,Circuit.Width*.5f-.1f-Mathf.Abs(offset)-extent);
                if(MinimumClearance<0)return float.PositiveInfinity;
                float lateral=speed*speed*curvatures[Mathf.Max(1,i)];
                float traction=Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral));
                float drive=speed>=o.tuning.gearCeiling?0:Mathf.Min(DriveModel.EngineForce(o.tuning,speed),traction);
                float net=drive-DraftingModel.Resistance(speed,o.tuning.mass,o.dragReduction);
                float distance=distances[i+1]-distances[i];
                float next=Mathf.Min(speeds[i+1],Mathf.Sqrt(Mathf.Max(0,speed*speed+2*net*distance)));
                // Short observed-motion window only. This ranks delay on the
                // actual candidate curve; it is not collision certification or
                // a prediction that rivals keep their inputs for a whole bend.
                if(trafficAware&&time<3) {
                    float ahead=i*step;
                    for(int t=0;t<trafficCount;t++) {
                        var other=rankedTraffic[t];float initial=other.initial,longitudinal=other.longitudinal,sideVelocity=other.sideVelocity;
                        float side=other.offset+Mathf.Clamp(sideVelocity*.75f*(1-Mathf.Exp(-time/.75f)),-1.5f,1.5f);
                        if(Mathf.Abs(side-offset)>o.self.halfWidth+other.halfWidth+.2f)continue;
                        float gap=initial+longitudinal*time-ahead;
                        if(gap<=-(o.self.halfLength+other.halfLength))continue;
                        float available=Mathf.Max(0,gap-o.self.halfLength-other.halfLength-1.5f);
                        next=Mathf.Min(next,Mathf.Sqrt(longitudinal*longitudinal+2*Mathf.Min(o.tuning.braking,budget)*.7f*available));
                    }
                }
                time+=2*distance/Mathf.Max(.1f,speed+next);speed=next;
            }
            return time;
        }
        // Lateral offset of the committed path "ahead" metres beyond the car,
        // using the same geometry as BuildEnvelope's uncached points.
        public float PlannedOffset(in ContextPathState path,float ahead)
        {
            ahead=Mathf.Max(0,ahead);float along=path.consumed+ahead,d=path.anchor+along;
            float side=Lateral(in path,along,path.guide?profile.line.Offset(d):0);
            if(path.rejoin)side=Mathf.Lerp(side,profile.line.Offset(d),Mathf.SmoothStep(0,1,ahead/65));
            return side;
        }
        public DriveCommand Follow(in DriverObservation o,in ContextPathState path,ForecastPathCache cache=null)
        {
            BuildEnvelope(in o,in path,cache);
            float budget=o.tuning.roadGrip*1.65f;
            float look=Mathf.Clamp(5+Mathf.Max(0,o.self.speed)*.4f,6,19);int aim=0;
            while(aim<Points-2&&distances[aim+1]<look)aim++;
            var delta=Vector3.Lerp(points[aim],points[aim+1],Mathf.InverseLerp(distances[aim],distances[aim+1],look))-o.self.position;
            // The tyre model permits measurable lateral velocity. Pure pursuit
            // must turn the direction of travel toward the path, rather than
            // assuming that body heading and actual travel are identical.
            var travel=o.self.velocity;travel.y=0;
            var heading=travel.sqrMagnitude>9?travel.normalized:o.self.forward;
            float angle=Vector3.SignedAngle(heading,delta,Vector3.up)*Mathf.Deg2Rad;
            float curve=2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude);
            float steer=Mathf.Clamp(curve*Mathf.Max(3,o.self.speed)*1.09f/o.tuning.yaw,-1,1);
            float target=Mathf.Min(speeds[1],Mathf.Sqrt(budget*.85f/Mathf.Max(.00001f,Mathf.Abs(curve))));
            if(o.finished)target=Mathf.Min(target,18);
            float feed=(speeds[1]*speeds[1]-speeds[0]*speeds[0])/(2*Mathf.Max(.01f,distances[1]));
            float force=feed+(target-o.self.speed)*3+DraftingModel.Resistance(o.self.speed,o.tuning.mass,o.dragReduction);
            float pedal=force>=0?force/Mathf.Max(.01f,DriveModel.EngineForce(o.tuning,o.self.speed)):force/o.tuning.braking;
            // Shared by live following and prospective rollouts. Only the
            // follower's braking can coast; leader/scenery/emergency limits
            // are applied afterwards by the policy and retain their authority.
            float steeringLimit=Mathf.Sqrt(budget*.85f/Mathf.Max(.00001f,Mathf.Abs(curve)));
            if(coastWithinEnvelope&&pedal<0&&!o.finished&&CoastingEnvelope.Allows(o.self.speed,o.tuning.mass,steeringLimit,distances,speeds,Points,o.drafting?DraftingModel.MaximumReduction:0))pedal=0;
            // A coast-limited trajectory suppresses drive torque, never required
            // braking. The identical cap is used by live and forecast followers.
            if(path.coast)pedal=Mathf.Min(pedal,0);
            return new DriveCommand(pedal,steer,target,o.self.offset,path.selectedOffset);
        }
    }
}
