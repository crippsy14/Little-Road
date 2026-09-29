using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    [DefaultExecutionOrder(-100)]
    public sealed class TrafficFixture:MonoBehaviour
    {
        [Serializable] public sealed class IntentEvent {public int id,leader,rejected;public float time,progress,offset,clearance;public string intent,reason;}
        [Serializable] public sealed class PassEvidence {public int follower,leader;public float firstClear=-1,sustainedAt=-1;public bool onStraight;[NonSerialized] public float clearSeconds;}
        [Serializable] public sealed class Evidence
        {
            public string scenario,scope="Declared mid-lap start and velocity, then ordinary vehicle physics. Partial course completion is not a full lap benchmark.";
            public float start,straightEnd,corridorEnd,startingGap,startingSpeed,rearStartingSpeed,witnessRelease=-1,firstClearPass=-1,sustainedPassAt=-1,minimumGap=999;
            public bool passBeforeCorridorExit,passOnStraight;public IntentEvent[] decisions;public PassEvidence[] passes;
            public TrafficDriverPolicy.PathDecision[] pathDecisions;public int droppedPathDecisions;
        }
        RaceTrial race;Evidence evidence;bool released,merge,rearControl,witness,witnessReleased,crossing;float passTime;
        readonly List<IntentEvent> decisions=new();readonly string[] lastIntent=new string[24];
        public void Setup(RaceTrial owner,TechnicalCircuit course)
        {
            race=owner;string scenario=RaceTrial.Arg("--traffic-case");
            crossing=scenario=="crossing-pass";
            merge=scenario.StartsWith("merge-");witness=scenario.StartsWith("merge-witness-");
            rearControl=scenario.StartsWith("rear-control");
            bool lane=scenario=="lane-left"||scenario=="lane-middle"||scenario=="lane-right";
            bool rejoin=scenario=="rejoin-left"||scenario=="rejoin-right";
            bool corner=Array.IndexOf(new[]{"corner-solo-left","corner-solo-middle","corner-solo-right","corner-solo-deep","corner-pair-left","corner-pair-right","corner-three"},scenario)>=0;
            int required=scenario=="field"?24:scenario=="class-sort"?RaceGrid.ClassSortCars:scenario=="solo"||scenario=="standing-solo"||scenario=="pace-solo"||lane||rejoin||scenario.StartsWith("corner-solo-")?1:scenario=="queue"||scenario=="corner-three"?3:2;
            if(course==null||race.cars.Count!=required||(!lane&&!corner&&Array.IndexOf(new[]{"draft-straight","draft-pullout","solo","standing-solo","pace-solo","follow","pass","no-room","queue","field","class-sort","rear-control","rear-control-left","rear-control-right","rejoin-left","rejoin-right","crossing-pass","brake-leader","brake-probe","merge-left","merge-right","merge-witness-left","merge-witness-right"},scenario)<0))throw new ArgumentException("Invalid declared traffic fixture.");
            if(lane) {
                if(race.cars[0].trafficPolicy==null)throw new ArgumentException("Lane fixture requires the local traffic controller.");
                float side=scenario=="lane-left"?-4:scenario=="lane-right"?4:0;
                released=true;race.cars[0].PlaceLaneFixture(side);race.cars[0].trafficPolicy.SetLaneFixture(side);
                evidence=new Evidence{scenario=scenario,scope="Full standing lap, fixed road corridor, ordinary local driver/physics. Tests off-reference tracking independently of traffic choice.",passes=Array.Empty<PassEvidence>()};
                Physics.SyncTransforms();return;
            }
            if(scenario=="field"||scenario=="class-sort"||scenario=="standing-solo") {
                // Experimental full-field preview keeps the normal stationary
                // starting grid and lap/checkpoint accounting. No mid-lap placing.
                released=true;
                evidence=new Evidence{scenario=scenario,scope=scenario=="class-sort"?"15-car overtaking fixture from a class-mixed standing grid (3/2/3/1/empty/2 rows, 1 fastest); ordinary AI/physics.":scenario=="standing-solo"?"One car from the exact24-car player rear-grid slot, full standing lap, ordinary AI/physics; traffic removed without moving the player start.":"Experimental 24-car full race from the normal standing grid. Traffic behaviour is not yet accepted for production.",passes=Array.Empty<PassEvidence>()};
                return;
            }
            float start=course.regionStarts[13]+40;
            // The reference is near the road centre here, so +/-4 m genuinely
            // tests both directions. Earlier on the straight it already sits at +4 m.
            if(rejoin||merge||rearControl||scenario=="pace-solo")start=course.regionStarts[13]+190;
            if(corner)start=course.regionStarts[1]-35;
            if(scenario=="corner-solo-deep")start=course.regionStarts[1]+15;
            float gap=float.Parse(RaceTrial.Arg("--traffic-gap","24"),System.Globalization.CultureInfo.InvariantCulture);
            float speed=float.Parse(RaceTrial.Arg("--traffic-speed","18"),System.Globalization.CultureInfo.InvariantCulture);
            if(!float.IsFinite(gap)||!float.IsFinite(speed)||gap<12||gap>60||speed<5||speed>30)throw new ArgumentException("Traffic fixture gap must be 12–60 m and speed 5–30 m/s.");
            if(crossing){start=190;gap=12;speed=31;}
            evidence=new Evidence{scenario=scenario,start=start,straightEnd=course.regionStarts[14],corridorEnd=start+220,startingGap=gap,startingSpeed=speed};
            if(crossing){evidence.rearStartingSpeed=17;evidence.corridorEnd=start+180;evidence.scope="Declared outside club car at route190 offset5.9 speed31, street car12m ahead at offset2.8 speed17. Inspired by the44 incident, not an exact snapshot replay. Normal shared physics and autonomous decisions after release.";}
            if(merge||rearControl) {
                evidence.rearStartingSpeed=float.Parse(RaceTrial.Arg("--traffic-rear-speed",rearControl?"18":"26"),System.Globalization.CultureInfo.InvariantCulture);
                if(!float.IsFinite(evidence.rearStartingSpeed)||evidence.rearStartingSpeed<5||evidence.rearStartingSpeed>35)throw new ArgumentException("Rear merge-fixture speed must be 5–35 m/s.");
                evidence.scope="Street car begins +/-4 m off the road centre; club car approaches from behind on the reference. Declared initial velocities only, then normal physics. Witness holds straight full throttle until the rear car is physically body-clear, then rejoins normally. No prescribed position or hidden opponent information.";
                if(rearControl)evidence.scope="Front car starts on the normal reference, rear car on reference or +/-4 m. Explicit starting speeds only, then ordinary physics and AI. This keeps the front car's initial off-line rejoin from creating an undeclared closing encounter.";
            }
            if(corner){evidence.corridorEnd=course.regionStarts[3]+25;evidence.startingGap=0;evidence.scope="Simultaneous declared first-corner entry, then ordinary racing physics. Corner exit is scored separately from the remaining course.";}
            var pairs=new List<PassEvidence>();for(int follower=0;follower<race.cars.Count;follower++)for(int leader=follower+1;leader<race.cars.Count;leader++)pairs.Add(new PassEvidence{follower=follower,leader=leader});evidence.passes=pairs.ToArray();
            if(merge)evidence.passes=new[]{new PassEvidence{follower=1,leader=0}};
            for(int i=0;i<race.cars.Count;i++) {
                if(i>0&&gap<race.cars[i-1].CollisionHalf.z+race.cars[i].CollisionHalf.z+2)throw new ArgumentException("Traffic fixture starts with insufficient body clearance.");
                float side=scenario.EndsWith("left")?-4:scenario.EndsWith("right")?4:0;
                if(scenario=="corner-solo-deep")side=4;
                if(corner&&required==2)side=(i==0?1:-1)*side;
                if(corner&&required==3)side=(i-1)*4;
                race.cars[i].PlaceTrafficFixture(corner?start:start+(rearControl||merge?-i:i)*gap,corner?side:crossing?(i==0?5.9f:2.8f):rearControl&&i==1&&scenario!="rear-control"?side:merge&&i==0?side:scenario=="brake-probe"?0:rejoin?(scenario=="rejoin-left"?-4f:4f):(float?)null);
            }
            if(scenario.StartsWith("draft-")) {
                foreach(var car in race.cars)car.PlaceTrafficFixture(start+car.driverId*gap,0);
                evidence.scope="Declared two-car straight acceleration experiment. Identical full-throttle commands for three seconds; pullout applies a steering input from 1 to 2 seconds. All speed/position changes use ordinary Rigidbody physics. Then ordinary AI resumes. Compare the initial three seconds, not whole-lap time.";
            }
            if(scenario=="no-room") {
                for(float d=start-16;d<evidence.corridorEnd;d+=4)foreach(int side in new[]{-1,1}) {
                    var line=race.ReferenceLine;var forward=(line.At(d+1)-line.At(d-1)).normalized;
                    var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Narrow passing control";wall.tag="Respawn";
                    wall.transform.SetPositionAndRotation(line.At(d)+Vector3.Cross(Vector3.up,forward)*side*2.15f+Vector3.up,Quaternion.LookRotation(forward));
                    wall.transform.localScale=new Vector3(.25f,2,4.1f);wall.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("No-room fixture barrier",new Color(.9f,.45f,.12f));
                }
            }
            Physics.SyncTransforms();
        }
        void FixedUpdate()
        {
            if(race==null||!race.running)return;
            // Set the declared initial speed before the common snapshot and
            // first live decision, rather than after controllers saw zero speed.
            if(!released&&race.elapsed>=-Time.fixedDeltaTime*1.01f){released=true;foreach(var car in race.cars)car.body.linearVelocity=car.transform.forward*((merge||rearControl||crossing)&&car.driverId==1?evidence.rearStartingSpeed:evidence.startingSpeed);}
            if(race.elapsed<0)return;
            if(evidence.scenario.StartsWith("draft-"))foreach(var car in race.cars)car.commandSource=race.elapsed<3?DraftCommand:null;
            if(witness&&!witnessReleased) {
                var slow=race.cars[0];var fast=race.cars[1];
                witnessReleased=fast.progress-slow.progress>slow.CollisionHalf.z+fast.CollisionHalf.z+2;
                slow.commandSource=witnessReleased?null:AccelerateLeader;
                if(witnessReleased)evidence.witnessRelease=race.elapsed;
            }
            // Declared disturbance, invisible to the following policy. The
            // leader returns to ordinary driving after one second of hard braking.
            if(evidence.scenario=="brake-leader"||evidence.scenario=="brake-probe")race.cars[1].commandSource=race.elapsed>=1&&race.elapsed<2?BrakeLeader:null;
            if(evidence.scenario=="brake-probe") {
                race.cars[0].trafficPolicy?.SetCommandProbe(race.elapsed<3);
                race.cars[0].contextPolicy?.SetCommandProbe(race.elapsed<3);
                // Keep the leader straight too, so an initial reference-line
                // rejoin cannot introduce an undeclared braking event before 1 s.
                race.cars[1].commandSource=race.elapsed<3?(race.elapsed>=1&&race.elapsed<2?BrakeLeader:AccelerateLeader):null;
            }
            foreach(var car in race.cars)if(car.trafficPolicy!=null&&lastIntent[car.driverId]!=car.trafficPolicy.Intent) {
                var policy=car.trafficPolicy;decisions.Add(new IntentEvent{id=car.driverId,time=race.elapsed,progress=car.progress,offset=policy.PlannedOffset,intent=policy.Intent,reason=policy.Reason,leader=policy.Leader,rejected=policy.Rejected,clearance=policy.Clearance});
                lastIntent[car.driverId]=policy.Intent;
                if((evidence.scenario!="field"&&evidence.scenario!="class-sort")||car.driverId==0)Debug.Log($"TRAFFIC_INTENT id={car.driverId} t={race.elapsed:0.00} {policy.Intent} offset={policy.PlannedOffset:0.00} reason={policy.Reason}");
            }
            if(evidence.scenario=="field"||evidence.scenario=="class-sort")return;
            foreach(var pass in evidence.passes) {
                var a=race.cars[pass.follower];var b=race.cars[pass.leader];if(a.finished||b.finished)continue;
                if(b.progress-a.progress<-(a.CollisionHalf.z+b.CollisionHalf.z+2)) {
                    if(pass.firstClear<0)pass.firstClear=race.elapsed;
                    pass.clearSeconds+=Time.fixedDeltaTime;
                    if(pass.clearSeconds>2&&pass.sustainedAt<0){pass.sustainedAt=race.elapsed;pass.onStraight=a.progress<evidence.straightEnd;}
                }else pass.clearSeconds=0;
            }
            if(race.cars.Count<2||race.cars[0].finished||race.cars[1].finished)return;
            var follower=race.cars[0];var leader=race.cars[1];float gap=leader.progress-follower.progress;
            evidence.minimumGap=Mathf.Min(evidence.minimumGap,gap);
            float clear=follower.CollisionHalf.z+leader.CollisionHalf.z+2;
            if(gap<-clear) {
                if(evidence.firstClearPass<0)evidence.firstClearPass=race.elapsed;
                passTime+=Time.fixedDeltaTime;
                if(passTime>2&&evidence.sustainedPassAt<0){evidence.sustainedPassAt=race.elapsed;evidence.passOnStraight=follower.progress<evidence.straightEnd;}
            }else passTime=0;
            if(gap<0&&follower.progress<evidence.corridorEnd)evidence.passBeforeCorridorExit=true;
        }
        public void Save()
        {
            var report=RaceTrial.Arg("--trial-report");if(report==null)return;
            var traces=new List<TrafficDriverPolicy.PathDecision>();
            foreach(var car in race.cars)if(car.trafficPolicy!=null){traces.AddRange(car.trafficPolicy.PathDecisions);evidence.droppedPathDecisions+=car.trafficPolicy.DroppedPathDecisions;}
            evidence.pathDecisions=traces.ToArray();
            evidence.decisions=decisions.ToArray();File.WriteAllText(Path.ChangeExtension(report,"traffic.json"),JsonUtility.ToJson(evidence,true));
        }
        Vector2 DraftCommand(RoadCar car)=>new Vector2(1,evidence.scenario=="draft-pullout"&&car.driverId==0&&race.elapsed>=1&&race.elapsed<2?.35f:0);
        static Vector2 BrakeLeader(RoadCar car)=>new Vector2(-1,0);
        static Vector2 AccelerateLeader(RoadCar car)=>new Vector2(1,0);
        void OnGUI()
        {
            if(race==null||race.cars.Count==0)return;using var hud=new DisplaySettings.HudScale(true);var p=race.cars[0].trafficPolicy;
            GUI.color=new Color(.07f,.16f,.19f,.92f);GUI.DrawTexture(new Rect(20,235,650,68),Texture2D.whiteTexture);GUI.color=Color.white;
            var context=race.cars[0].contextPolicy;
            GUI.Label(new Rect(34,241,620,28),$"TRAFFIC TEST · {evidence.scenario} · {context?.Intent??p?.Intent??"Reference control"}",new GUIStyle(GUI.skin.label){fontSize=19});
            GUI.Label(new Rect(34,271,620,24),context!=null?$"{context.Reason} · continuous target {context.SelectedOffset:0.00}m":$"{p?.Reason} · passing offset {p?.PlannedOffset:0.0}m · rejected paths {p?.Rejected}");
        }
    }
}
