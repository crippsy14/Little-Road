using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Observation only. No audit metric changes a vehicle command or position.
    [DefaultExecutionOrder(100)]
    public sealed class TechnicalCircuitReview:MonoBehaviour
    {
        [Serializable] public sealed class Sample {public int id,gate,trafficLeader,trafficRejected,trafficRelevant,referenceRejection,trafficCandidate,safetyThreat;public bool safetyActive,safetyUnresolved;public double clock;public float time,route,progress,speed,target,pedal,steer,lineError,roadOffset,maxBodyOffset,trafficOffset,trafficClearance,predictedRouteGain,predictedTravel,predictedExitRouteSpeed,steeringState,requestedYaw,lateralSpeed,contactDeltaV,safetyConflictAfter,safetyPedalLimit;public string region,inputOwner,recoveryState,trafficIntent,trafficReason,contactSurface;public Vector3 position,velocity,forward,angularVelocity,contactImpulse;}
        [Serializable] public sealed class ContextSample {public int id,leader,relevant,staticRejected,rearApproaches,roadRejected,roadCalls;public bool roadEnabled,roadUnresolved;public float time,offset,followingOffset,left,right,leaderLimit;public string intent,reason;public bool safetyActive,checkedReference;public float safetyLimit,staticLimit;}
        [Serializable] public sealed class Result {public int id,samples,outsideBodySamples;public float withinOneMetrePercent,distanceWithinOneMetrePercent,eligibleSeconds,meanAbsError,p95AbsError,maxAbsError,maxBodyOffset;}
        [Serializable] public sealed class PedalSample {public bool coastContinuation,trafficUnresolved;public int id;public float time,follower,followerTarget,leader,scenery,applied;public double decisionTimestamp,sampleTimestamp;}
        [Serializable] public sealed class DriverPlanningRecord {public int id,risk;public float localBrakeFraction,referenceBrakeScale;}
        [Serializable] public sealed class SurveyPoint {public float distance,curvature,referenceSpeed,referenceAcceleration;public Vector3 position,referencePosition;}
        [Serializable] public sealed class PlannerCost {public int id,calls;public double totalMs,meanMs,maxMs;}
        [Serializable] public sealed class RoadCost {public int id,calls,roadFailures,corridorFailures,sceneryFailures,trafficFailures,coastAttempts,coastAccepted,continuationAttempts,continuationAccepted,preparedRequests,prefetchedCalls,synchronousCalls,guideAttempts,guideAccepted;public double forecastMs,corridorMs,sceneryMs,trafficMs;}
        [Serializable] public sealed class Evidence
        {
            public DriverPlanningRecord[] driverPlanning;
            public ContextPlanAudit.Evidence[] switchPlanAudits;
            public string track,version,reference="centreline-v1 (geometry baseline, not an accepted ideal racing line)",referenceStatus,driverPolicy,sessionType,sampling="10 Hz simulated time; adherence weighted by time/distance between samples after crossing the start line. Boundary counts include grid travel. Sub-sample incidents can be missed.";
            public string interpretation="Line adherence is diagnostic, not an AI pass score. Compare clean solo laps, lap/exit speeds and traffic intent before judging deviations.";
            public float length,width=16,tolerance=1,predictedLapTime;public CarTuning tuning;public Result[] results;public Sample[] samples;public ContextSample[] contextSamples;public PedalSample[] pedalSamples;public SurveyPoint[] survey;public PlannerCost[] plannerCosts,contextDecisionCosts;public RoadCost[] roadCosts;public ContextPlanAudit.Evidence[] failedPlanAudits;
        }
        RaceTrial race;TechnicalCircuit course;string path;float nextSample;
        readonly List<Sample> samples=new();Texture2D map;Vector2 minimum,maximum;GUIStyle label;
        readonly List<ContextSample> contextSamples=new();
        readonly List<PedalSample> pedalSamples=new();
        readonly bool pedalAudit=RaceTrial.Has("--pedal-stage-audit");
        public void Setup(RaceTrial owner,TechnicalCircuit circuit,string report)
        {
            race=owner;course=circuit;path=string.IsNullOrEmpty(report)?null:Path.ChangeExtension(report,"track.json");
            minimum=new Vector2(float.MaxValue,float.MaxValue);maximum=new Vector2(float.MinValue,float.MinValue);
            foreach(var p in course.track.points){minimum=Vector2.Min(minimum,new Vector2(p.x,p.z));maximum=Vector2.Max(maximum,new Vector2(p.x,p.z));}
            minimum-=Vector2.one*30;maximum+=Vector2.one*30;
            map=new Texture2D(300,180);var pixels=Enumerable.Repeat(new Color(.055f,.11f,.12f,.94f),300*180).ToArray();
            for(float d=0;d<course.track.length;d+=.8f){var p=Map(course.track.At(d));int x=Mathf.RoundToInt(p.x*299),y=Mathf.RoundToInt(p.y*179);for(int a=-2;a<=2;a++)for(int b=-2;b<=2;b++)if(x+a>=0&&x+a<300&&y+b>=0&&y+b<180)pixels[(y+b)*300+x+a]=new Color(.65f,.74f,.72f);}
            if(race.ReferenceLine!=null)for(float d=0;d<course.track.length;d+=.5f){var p=Map(race.ReferenceLine.At(d));int x=Mathf.RoundToInt(p.x*299),y=Mathf.RoundToInt(p.y*179);pixels[y*300+x]=Color.cyan;}
            map.SetPixels(pixels);map.Apply();
        }
        Vector2 Map(Vector3 p)=>new Vector2(Mathf.InverseLerp(minimum.x,maximum.x,p.x),Mathf.InverseLerp(minimum.y,maximum.y,p.z));
        void FixedUpdate()
        {
            if(race==null||!race.running||race.elapsed<nextSample)return;nextSample+=.1f;
            foreach(var car in race.cars) {
                if(car.finished)continue;
                var collider=car.GetComponent<BoxCollider>();float maximumOffset=0;
                // Project all four body corners individually; centre position alone misses road departures.
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) {
                    var p=car.transform.TransformPoint(collider.center+Vector3.Scale(collider.size*.5f,new Vector3(x,-1,z)));
                    course.track.Nearest(p,out float offset);maximumOffset=Mathf.Max(maximumOffset,Mathf.Abs(offset));
                }
                float error=car.offset;if(race.ReferenceLine!=null)race.ReferenceLine.Project(car.body.position,car.pathDistance,out error);
                var safety=car.contextPolicy?.Safety??car.trafficPolicy?.Safety;
                if(pedalAudit&&car.contextPolicy!=null){var p=car.contextPolicy;pedalSamples.Add(new PedalSample{id=car.driverId,time=race.elapsed,decisionTimestamp=p.PedalTimestamp,sampleTimestamp=Time.fixedTimeAsDouble,follower=p.FollowerPedal,followerTarget=p.FollowerTarget,leader=p.AfterLeaderPedal,scenery=p.BeforeSafetyPedal,applied=p.AppliedPedal,coastContinuation=p.CoastContinuationActive,trafficUnresolved=p.TrafficUnresolved});}
                if(car.contextPolicy!=null){var p=car.contextPolicy;contextSamples.Add(new ContextSample{id=car.driverId,time=race.elapsed,offset=p.SelectedOffset,followingOffset=p.FollowingOffset,roadEnabled=p.RoadFeasibilityEnabled,roadUnresolved=p.RoadUnresolved,roadRejected=p.RoadRejected,roadCalls=p.RoadCalls,left=p.ReachableLeft,right=p.ReachableRight,leaderLimit=p.LeaderLimit,leader=p.Leader,relevant=p.RelevantCars,rearApproaches=p.RearApproaches,staticRejected=p.StaticRejected,checkedReference=p.CheckedReference,staticLimit=p.StaticLimit,intent=p.Intent,reason=p.Reason,safetyActive=p.Safety.Active,safetyLimit=p.Safety.PedalLimit});}
                samples.Add(new Sample{id=car.driverId,time=race.elapsed,clock=Time.fixedTimeAsDouble,trafficCandidate=car.trafficPolicy?.SelectedCandidate??-1,gate=car.nextGate,route=car.pathDistance,progress=car.progress,speed=car.speed,target=car.targetSpeed,
                    pedal=car.throttle,steer=car.steering,lineError=error,roadOffset=car.offset,maxBodyOffset=maximumOffset,region=course.Region(car.pathDistance),position=car.body.position,inputOwner=car.autonomous||car.finished?"AI":"human",recoveryState=car.physicalRecovery?.State??"Disabled",
                    velocity=car.body.linearVelocity,forward=car.transform.forward,angularVelocity=car.body.angularVelocity,steeringState=car.SteeringState,requestedYaw=car.requestedYaw,lateralSpeed=car.lateralSpeed,contactDeltaV=car.observedContactDeltaV,contactImpulse=car.observedContactImpulse,contactSurface=car.observedContactSurface,
                    safetyActive=safety?.Active??false,safetyUnresolved=safety?.Unresolved??false,safetyThreat=safety?.Threat??-1,safetyConflictAfter=safety?.ConflictAfter??-1,safetyPedalLimit=safety?.PedalLimit??1,
                    trafficIntent=car.trafficPolicy?.Intent,trafficReason=car.trafficPolicy?.Reason,trafficLeader=car.trafficPolicy?.Leader??-1,trafficRejected=car.trafficPolicy?.Rejected??0,trafficOffset=car.trafficPolicy?.PlannedOffset??0,trafficClearance=car.trafficPolicy?.Clearance??999,
                    trafficRelevant=car.trafficPolicy?.RelevantCars??0,referenceRejection=car.trafficPolicy?.ReferenceRejection??0,predictedRouteGain=car.trafficPolicy?.PredictedRouteGain??0,predictedTravel=car.trafficPolicy?.PredictedTravel??0,predictedExitRouteSpeed=car.trafficPolicy?.PredictedExitRouteSpeed??0});
                car.observedContactDeltaV=0;car.observedContactImpulse=Vector3.zero;car.observedContactSurface=null;
            }
        }
        public void Save()
        {
            if(path==null)return;
            var results=new List<Result>();
            foreach(var car in race.cars) {
                var rows=samples.Where(s=>s.id==car.driverId).ToArray();if(rows.Length==0)continue;
                var errors=rows.Select(s=>Mathf.Abs(s.lineError)).OrderBy(x=>x).ToArray();
                float seconds=0,inside=0,travelled=0,insideDistance=0,integralError=0;
                for(int i=1;i<rows.Length;i++) {
                    var a=rows[i-1];var b=rows[i];if(a.progress<0||b.progress<0)continue;
                    float dt=b.time-a.time,ds=Vector3.Distance(a.position,b.position),fraction=WithinFraction(a.lineError,b.lineError);
                    seconds+=dt;inside+=dt*fraction;travelled+=ds;insideDistance+=ds*fraction;
                    float ea=a.lineError,eb=b.lineError;
                    integralError+=dt*(ea*eb<0?.5f*(ea*ea+eb*eb)/Mathf.Abs(eb-ea):.5f*(Mathf.Abs(ea)+Mathf.Abs(eb)));
                }
                results.Add(new Result{id=car.driverId,samples=rows.Length,outsideBodySamples=rows.Count(s=>s.maxBodyOffset>8),
                    withinOneMetrePercent=100*inside/Mathf.Max(.001f,seconds),distanceWithinOneMetrePercent=100*insideDistance/Mathf.Max(.001f,travelled),eligibleSeconds=seconds,meanAbsError=integralError/Mathf.Max(.001f,seconds),p95AbsError=errors[Mathf.Min(errors.Length-1,Mathf.FloorToInt(errors.Length*.95f))],
                    maxAbsError=errors[^1],maxBodyOffset=rows.Max(s=>s.maxBodyOffset)});
            }
            var profile=race.cars[0].referenceProfile;
            var survey=new List<SurveyPoint>();for(float d=0;d<course.track.length;d+=2)survey.Add(new SurveyPoint{distance=d,position=course.track.At(d),curvature=course.track.Curvature(d),
                referencePosition=race.ReferenceLine==null?course.track.At(d):race.ReferenceLine.At(d),referenceSpeed=profile==null?0:profile.Speed(d),referenceAcceleration=profile==null?0:profile.Acceleration(d)});
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path,JsonUtility.ToJson(new Evidence{track=course.definition.id,version=course.definition.version,length=course.track.length,
                reference=race.ReferenceLine==null?"centreline-v1":race.ReferenceLine.data.id,referenceStatus=race.ReferenceLine==null?"geometry baseline only":race.ReferenceLine.data.status,
                driverPolicy=race.DriverPolicyId,sessionType=RaceTrial.Has("--trial-human")?"human comparison; never optimiser input":"AI candidate evaluation",tuning=race.cars[0].tuning,predictedLapTime=profile==null?0:profile.predictedLapTime,results=results.ToArray(),samples=samples.ToArray(),contextSamples=contextSamples.ToArray(),pedalSamples=pedalSamples.ToArray(),survey=survey.ToArray(),driverPlanning=race.cars.Where(c=>c.referenceProfile!=null).Select(c=>new DriverPlanningRecord{id=c.driverId,risk=c.referenceProfile.planning.Risk,localBrakeFraction=c.referenceProfile.planning.BrakeFraction,referenceBrakeScale=c.referenceProfile.planning.ReferenceBrakeScale}).ToArray(),
                contextDecisionCosts=race.cars.Where(c=>c.contextPolicy!=null).Select(c=>new PlannerCost{id=c.driverId,calls=c.contextPolicy.DecisionCount,totalMs=c.contextPolicy.DecisionTotalMs,meanMs=c.contextPolicy.DecisionTotalMs/Math.Max(1,c.contextPolicy.DecisionCount),maxMs=c.contextPolicy.DecisionMaxMs}).ToArray(),
                failedPlanAudits=race.cars.Where(c=>c.contextPolicy?.PlanAudit!=null).Select(c=>c.contextPolicy.PlanAudit.Export()).ToArray(),
                switchPlanAudits=race.cars.Where(c=>c.contextPolicy?.SwitchAudit!=null).Select(c=>c.contextPolicy.SwitchAudit.Export("Accepted target changes of at least one own body width plus0.4m, while racing with a previous path. Exact owned observation, prior path, initial preference map and attempted candidates;64 captures per car. Diagnostic allocations exclude performance acceptance; a target switch is not inherently a fault.")).ToArray(),
                roadCosts=race.cars.Where(c=>c.contextPolicy?.RoadDiagnostics!=null).Select(c=>{var p=c.contextPolicy.RoadDiagnostics;return new RoadCost{id=c.driverId,calls=p.Calls,guideAttempts=c.contextPolicy.GuideAttempts,guideAccepted=c.contextPolicy.GuideAccepted,preparedRequests=p.PreparedRequests,prefetchedCalls=p.PrefetchedCalls,synchronousCalls=p.SynchronousCalls,forecastMs=p.ForecastMs,corridorMs=p.CorridorMs,sceneryMs=p.SceneryMs,roadFailures=p.RoadFailures,corridorFailures=p.CorridorFailures,sceneryFailures=p.SceneryFailures,trafficFailures=p.TrafficFailures,trafficMs=p.TrafficMs,coastAttempts=c.contextPolicy.CoastAttempts,coastAccepted=c.contextPolicy.CoastAccepted,continuationAttempts=c.contextPolicy.ContinuationAttempts,continuationAccepted=c.contextPolicy.ContinuationAccepted};}).ToArray(),
                plannerCosts=race.cars.Where(c=>c.trafficPolicy!=null).Select(c=>new PlannerCost{id=c.driverId,calls=c.trafficPolicy.PlanCount,totalMs=c.trafficPolicy.PlanTotalMs,meanMs=c.trafficPolicy.PlanTotalMs/Math.Max(1,c.trafficPolicy.PlanCount),maxMs=c.trafficPolicy.PlanMaxMs}).ToArray()},true));
        }
        public static float WithinFraction(float a,float b)
        {
            if(Mathf.Abs(b-a)<.00001f)return Mathf.Abs(a)<=1?1:0;
            float x=(-1-a)/(b-a),y=(1-a)/(b-a);return Mathf.Max(0,Mathf.Min(1,Mathf.Max(x,y))-Mathf.Max(0,Mathf.Min(x,y)));
        }
        void OnGUI()
        {
            if(map==null)return;using var hud=new DisplaySettings.HudScale(true);float left=hud.width-320,top=20;GUI.DrawTexture(new Rect(left,top,300,180),map);
            for(int i=0;i<12;i++){var p=Map(course.track.At(course.track.length*i/12));GUI.color=i==0?Color.white:new Color(1,.7f,.2f);GUI.DrawTexture(new Rect(left+p.x*300-2,top+(1-p.y)*180-2,4,4),Texture2D.whiteTexture);}
            foreach(var car in race.cars){var p=Map(car.body.position);GUI.color=car.driverId==0?Color.cyan:Color.white;GUI.DrawTexture(new Rect(left+p.x*300-3,top+(1-p.y)*180-3,6,6),Texture2D.whiteTexture);}
            GUI.color=Color.white;label??=new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=Color.white}};
            GUI.Label(new Rect(left+8,top+184,300,24),$"{course.track.length/1000:0.00} km · 16 m wide · 12 gates",label);
        }
        void OnDestroy(){if(map!=null)Destroy(map);}
    }
}
