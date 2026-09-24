using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    public sealed class RaceTrial : MonoBehaviour
    {
        [NonSerialized] public Circuit track;
        public readonly List<RoadCar> cars=new();
        public float elapsed=-3;
        public bool running,freeRoam;
        public bool PlayerRecoveryPending {get;private set;}
        public bool DraftingEnabled {get;private set;}
        public RaceWorldSnapshot DriverSnapshot {get;private set;}
        public DriverFrameAudit DriverAudit {get;private set;}
        public MotionPredictionAudit MotionAudit {get;private set;}
        public PathApproachAudit ApproachAudit {get;private set;}
        public RacingLine ReferenceLine {get;private set;}
        public string DriverPolicyId {get;private set;}="legacy-v1";
        AdventureUI careerUI;
        TechnicalCircuit technical;
        TechnicalCircuitReview technicalReview;
        bool overview,wasLookingBehind;
        CollisionAudit collisionAudit;
        bool career;
        public int requiredLaps=3;
        Camera chase;
        bool automation,reportWritten;
        ManualRaceStart manualStart;
        public bool WaitingForReady=>manualStart?.Waiting??false;
        float duration=240,wallStart;
        string reportPath,captureFolder,setup="stock";
        int seed=1709,field=24;
        readonly List<float> frameTimes=new();
        readonly List<RoadCar> finishOrder=new();
        readonly List<MotionSample> motion=new();
        readonly List<DraftSample> draftingSamples=new();
        [Serializable] public class DraftSample {public int id,leader;public float time,reduction;}
        readonly List<ImpactSample> impacts=new();
        readonly List<RecoverySample> assistedRecoveries=new();
        readonly List<CheckpointSample> checkpointPassages=new();
        string gridMode="pack";
        string[] opponentSetups;
        float nextSample,sampleInterval=1;
        GUIStyle title,label,small;
        [Serializable] public class CarResult {public int id,laps,nextGate,place,contacts,recoveries,missedGates,physicalRecoveries;public string equipment;public bool finished;public float time,progress,offset,bestLap,maxImpact,maxPenetration,physicalRecoverySeconds,recoveryWaitSeconds,recoveryReverseSeconds;}
        [Serializable] public class MotionSample {public int id,nextGate,laps;public float time,speed,targetSpeed,throttle,steering,offset,progress,headingError,nextGateProgress,predictedOffset,lateralSpeed,requestedYaw,actualYaw,chosenLane;}
        [Serializable] public class ImpactSample {public int id,other;public string otherName;public float time,relativeSpeed,penetration,progress,offset;}
        [Serializable] public class RecoverySample {public int id,gateBefore,gateAfter,rejectedDestinations;public string reason;public float time,progressBefore,progressAfter;public Vector3 positionBefore,positionAfter;}
        [Serializable] public class CheckpointSample {public int id,gate;public float time,requiredProgress,actualProgress;}
        [Serializable] public class Report {public string engine,stage,utc,graphics,equipment,gridMode,collisionMode,trackId,trackVersion,driverPolicy,observationMode;public int seed,carCount,finished,requiredLaps,overlapSampledSteps,overlapQuerySaturations;public float simSeconds,wallSeconds,trackLength,trackWidth,meanFrameMs,p95FrameMs,physicsStepSeconds,maxCarPoseOverlap,maxEnvironmentPoseOverlap;public bool autonomous;public CarResult[] cars;public MotionSample[] motion;public DraftSample[] draftingSamples;public ImpactSample[] impacts;public RecoverySample[] assistedRecoveries;public CheckpointSample[] checkpointPassages;public CollisionAudit.Peak[] overlapPeaks;}
        public static string Arg(string key,string fallback=null) {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
        public static bool Has(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0;
        IEnumerator Start()
        {
            if(Has("--performance-trial")){enabled=false;gameObject.AddComponent<PerformanceTrial>();yield break;}
            if(Has("--trial-gallery")){gameObject.AddComponent<CarGallery>();yield break;}
            career=AdventureSession.Racing;
            if(!career&&!Has("--trial-auto")&&!Has("--trial-race")){freeRoam=true;gameObject.AddComponent<AdventureGame>();yield break;}
            sampleInterval=Arg("--traffic-case","").StartsWith("draft-")?1f/120:1;
            automation=Has("--trial-auto")||AdventureSession.Smoke;int.TryParse(Arg("--trial-cars","24"),out field);field=Mathf.Clamp(field,1,24);
            float.TryParse(Arg("--trial-duration","240"),out duration);int.TryParse(Arg("--trial-seed","1709"),out seed);
            int.TryParse(Arg("--trial-laps","3"),out requiredLaps);requiredLaps=Mathf.Max(1,requiredLaps);
            setup=Arg("--trial-setup","stock");reportPath=Arg("--trial-report");captureFolder=Arg("--trial-captures");
            if(reportPath!=null)DriverAudit=new DriverFrameAudit();
            if(career){field=24;requiredLaps=3;setup="owned equipment";seed=1709+AdventureSession.RaceSerial*11;careerUI=gameObject.AddComponent<AdventureUI>();}
            gridMode=Arg("--trial-grid","pack");opponentSetups=Arg("--trial-opponents","").Split(',',StringSplitOptions.RemoveEmptyEntries);
            if(gridMode!="pack"&&gridMode!="line")throw new ArgumentException("Unknown trial grid: "+gridMode);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=120;Time.fixedDeltaTime=1f/120;Time.maximumDeltaTime=.15f;
            Application.runInBackground=true;
            string trackId=career?"bayview":Arg("--trial-track","bayview");
            if(trackId=="cedar") {technical=new TechnicalCircuit();track=technical.track;TechnicalCircuitWorld.Create(technical);}
            else if(trackId=="bayview"){track=new Circuit();TrialWorld.Create(track);}
            else throw new ArgumentException("Unknown trial track: "+trackId);
            string policy=career?"legacy":Arg("--driver-policy","legacy");
            if(Has("--dynamic-rejoin")&&(career||policy!="context"||(field!=1&&!Has("--dynamic-traffic"))||!Has("--road-feasibility")))throw new ArgumentException("Dynamic rejoin requires declared context fixtures and physical road checks.");
            if(Has("--dynamic-traffic")&&(!Has("--dynamic-rejoin")||!Has("--local-origin-curvature")||!Has("--traffic-path-check")||field>3))throw new ArgumentException("Dynamic traffic currently requires corrected local envelopes, moving-traffic checks and at most three cars.");
            if(Has("--local-origin-curvature")&&!Has("--dynamic-rejoin"))throw new ArgumentException("Local-origin curvature is currently part of the solo dynamic-rejoin study.");
            DraftingEnabled=Has("--light-drafting");
            if(DraftingEnabled&&(career||policy!="context"))throw new ArgumentException("Light drafting is currently supported only by context prototype fixtures.");
            if(policy=="reference"||policy=="traffic"||policy=="context") {
                bool referenceRearControl=Arg("--traffic-case")=="rear-control"&&field==2;
                if(technical==null||(policy=="reference"&&field!=1&&!Has("--recovery-case")&&!referenceRearControl)||(policy=="traffic"&&!Has("--traffic-case")))throw new ArgumentException("New drivers require their declared Cedar development fixtures.");
                if(policy=="context"&&Array.IndexOf(new[]{"draft-straight","draft-pullout","solo","standing-solo","pace-solo","rear-control","rear-control-left","rear-control-right","follow","pass","crossing-pass","brake-leader","brake-probe","queue","no-room","field","merge-left","merge-right","merge-witness-left","merge-witness-right","corner-solo-left","corner-solo-middle","corner-solo-right","corner-solo-deep","corner-pair-left","corner-pair-right","corner-three"},Arg("--traffic-case"))<0)throw new ArgumentException("Context prototype requires a declared supported development fixture.");
                ReferenceLine=new RacingLine(track,Arg("--racing-line","cedar-mincurve-v1"),technical.definition.id,technical.definition.version);DriverPolicyId=policy=="traffic"?"traffic-v1":policy=="context"?"context-v1":"reference-v1";
            }else if(policy!="legacy")throw new ArgumentException("Unknown driver policy: "+policy);
            if(!career)Time.timeScale=Mathf.Clamp(float.Parse(Arg("--trial-timescale","1"),System.Globalization.CultureInfo.InvariantCulture),.25f,8);
            for(int i=0;i<field;i++) {
                string asset=i%3==1?"coupe_car":i%3==2?"utility_car":"compact_v1";
                if(Has("--path-approach-file"))asset=PathApproachAudit.Replay().bodyAsset;
                if(career&&i==0)asset=AdventureSession.Store.Data.bodyId;
                Color color=i==0?new Color(.08f,.56f,.49f):Color.HSVToRGB(Mathf.Repeat(i*.173f,.999f),.64f,.84f);
                if(career&&i==0)color=AdventureGame.Paints[AdventureSession.Store.Data.paintIndex];
                var go=TrialWorld.Car(asset,color);int slot=RaceGrid.TrialSlot(i,field,Arg("--traffic-case"));bool line=gridMode=="line";
                float grid=RaceGrid.Distance(slot,line);
                go.transform.SetPositionAndRotation(RaceGrid.Position(track,slot,line),Quaternion.LookRotation(track.Forward(grid)));
                string equipment=i==0?setup:i-1<opponentSetups.Length?opponentSetups[i-1]:i%4==0?"club":"street";
                var tuning=EquipmentCatalog.Stats(career&&i==0?AdventureSession.Store.Data.Loadout():EquipmentCatalog.TestLoadout(equipment));
                var car=go.AddComponent<RoadCar>();car.Setup(this,i,tuning,automation||i!=0,seed+i*997,equipment);cars.Add(car);
            }
            if(Has("--recovery-case"))gameObject.AddComponent<RecoveryFixture>().Setup(this);
            if(Has("--traffic-case"))gameObject.AddComponent<TrafficFixture>().Setup(this,technical);
            if(Has("--path-probe"))gameObject.AddComponent<PathPredictionFixture>().Setup(this,technical);
            if(!career&&(Has("--shared-driver-snapshot")||ReferenceLine!=null||Has("--physical-recovery"))){DriverSnapshot=gameObject.AddComponent<RaceWorldSnapshot>();DriverSnapshot.Setup(this);}
            if(ReferenceLine!=null)gameObject.AddComponent<RacingLineOverlay>().Setup(this,cars[0].referenceProfile);
            chase=new GameObject("Chase camera").AddComponent<Camera>();chase.tag="MainCamera";chase.fieldOfView=52;chase.nearClipPlane=.12f;chase.farClipPlane=1800;
            chase.gameObject.AddComponent<AudioListener>();var cameraData=chase.GetUniversalAdditionalCameraData();cameraData.renderPostProcessing=true;
            cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            if(reportPath!=null){collisionAudit=gameObject.AddComponent<CollisionAudit>();collisionAudit.Setup(this);}
            if(technical!=null){technicalReview=gameObject.AddComponent<TechnicalCircuitReview>();technicalReview.Setup(this,technical,reportPath);}
            if(Has("--motion-audit"))MotionAudit=new MotionPredictionAudit(Arg("--motion-audit"),Time.fixedDeltaTime,cars.Count);
            if(Has("--capture-path-approach"))ApproachAudit=new PathApproachAudit(int.Parse(Arg("--capture-path-approach")),cars.Count);
            UpdateCamera(true);wallStart=Time.realtimeSinceStartup;running=true;
            manualStart=new ManualRaceStart(!career&&!automation&&Has("--trial-human"));
            if(WaitingForReady){Time.timeScale=0;Debug.Log("LITTLE_ROAD_MANUAL_WAITING countdown=-3");}
            RaceGrid.Capture(this,captureFolder);
            if(Arg("--traffic-case")=="standing-solo"&&captureFolder==null&&reportPath!=null)RaceGrid.Capture(this,Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            if(Has("--vertical-audit"))gameObject.AddComponent<VerticalMotionAudit>().Setup(this);
            Debug.Log("LITTLE_ROAD_UNITY_READY cars="+cars.Count+" width="+Circuit.Width+" length="+track.length);
            if(Has("--evening-route-review"))gameObject.AddComponent<EveningRouteReview>();
            if(captureFolder!=null) {Directory.CreateDirectory(captureFolder);yield return new WaitForSeconds(2);yield return Capture("01-grid");
                yield return new WaitForSeconds(15);yield return Capture("02-race");}
        }
        void FixedUpdate(){if(running&&!freeRoam) {
            elapsed+=Time.fixedDeltaTime;
            if(elapsed>=nextSample) {nextSample+=sampleInterval;
                foreach(var c in cars)motion.Add(new MotionSample{id=c.driverId,time=elapsed,speed=c.speed,targetSpeed=c.targetSpeed,
                    throttle=c.throttle,steering=c.steering,offset=c.offset,progress=c.progress,
                    headingError=Vector3.SignedAngle(c.transform.forward,track.Forward(c.pathDistance),Vector3.up),nextGate=c.nextGate,laps=c.laps,nextGateProgress=c.nextGateProgress,
                    predictedOffset=c.predictedOffset,lateralSpeed=c.lateralSpeed,requestedYaw=c.requestedYaw,actualYaw=c.body.angularVelocity.y,chosenLane=c.chosenLane});
                if(DraftingEnabled)foreach(var c in cars)draftingSamples.Add(new DraftSample{id=c.driverId,time=elapsed,leader=c.DraftLeader,reduction=c.DragReduction});
            }
        }}
        void Update()
        {
            if(!running||freeRoam)return;
            if(WaitingForReady)ConfirmManualStart(Keyboard.current?.enterKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true);
            DriverAudit?.EndFrame(elapsed);
            if(Has("--trial-human")&&cars[0].finished&&!reportWritten)WriteReport();
            if(technical!=null&&Keyboard.current?.cKey.wasPressedThisFrame==true)overview=!overview;
            if(elapsed>5)frameTimes.Add(Time.unscaledDeltaTime*1000);
            if(!cars[0].finished&&(Keyboard.current?.rKey.wasPressedThisFrame==true || Gamepad.current?.dpad.down.wasPressedThisFrame==true))PlayerRecoveryPending=true;
            if(PlayerRecoveryPending&&cars[0].TryRecover())PlayerRecoveryPending=false;
            if(!career&&(Keyboard.current?.tabKey.wasPressedThisFrame==true || Gamepad.current?.selectButton.wasPressedThisFrame==true))cars[0].autonomous=!cars[0].autonomous;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true || (career&&Gamepad.current?.startButton.wasPressedThisFrame==true)) {if(career){AdventureSession.Notice=cars[0].finished?"Back in Bayview. Your result and reward are saved.":"Race retired · no payout. Your equipment is unchanged.";AdventureSession.ReturnToTown();}else{WriteReport();Application.Quit();}}
            if(career) {
                int place=cars[0].finished?finishOrder.IndexOf(cars[0])+1:1+cars.Count(c=>c!=cars[0]&&(c.finished||c.RankedProgress>cars[0].RankedProgress));
                careerUI.HUD($"BAYVIEW COAST / CLASS C   ·   {place}/24   ·   Lap {Mathf.Min(cars[0].laps+1,requiredLaps)}/3   ·   {Mathf.RoundToInt(Mathf.Abs(cars[0].speed)*2.237f)} mph\n{(elapsed<0?"START IN "+Mathf.CeilToInt(-elapsed):"Race time "+elapsed.ToString("0.0")+"s")}   ·   {AdventureSession.Store.Data.credits} credits",
                    PlayerRecoveryPending?"RECOVERY REQUESTED — waiting for a clear space on the road":cars[0].CheckpointMissed?cars[0].CheckpointMessage:"WASD / arrows or stick + R2/L2: drive     Hold Q / Triangle: look behind     R / D-pad Down: recover     Esc / Options: return to town (unfinished races pay nothing)");
            }
            if(automation && !reportWritten && (elapsed>=duration || finishOrder.Count==cars.Count)) {
                WriteReport();
                if(career&&AdventureSession.Smoke){if(finishOrder.Count!=24)throw new Exception("Adventure race did not complete its field.");if(captureFolder!=null)careerUI.Capture(Path.Combine(captureFolder,"03-race-result.png"));AdventureSession.ReturnToTown();}
                else StartCoroutine(EndTrial());
            }
        }
        void LateUpdate(){if(chase!=null&&cars.Count>0)UpdateCamera(false);}
        void ConfirmManualStart(bool requested)
        {
            if(manualStart?.TryStart(Application.isFocused,requested)!=true)return;
            Time.timeScale=1;wallStart=Time.realtimeSinceStartup;
            Debug.Log("LITTLE_ROAD_MANUAL_READY countdown="+elapsed);
        }
        void OnApplicationQuit(){if(running&&!freeRoam&&!career&&cars.Count>0)WriteReport();}
        void UpdateCamera(bool snap)
        {
            bool behind=DrivingViewInput.LookingBehind;
            snap|=behind!=wasLookingBehind;wasLookingBehind=behind;
            if(technical!=null&&overview&&!behind) {
                chase.orthographic=true;chase.orthographicSize=230;
                chase.transform.SetPositionAndRotation(new Vector3(-50,700,-8),Quaternion.Euler(90,0,0));return;
            }
            chase.orthographic=false;
            var car=cars[0];var viewingForward=car.transform.forward*(behind?-1:1);
            Vector3 goal=car.transform.position-viewingForward*5.8f+Vector3.up*3.1f;
            chase.transform.position=snap?goal:Vector3.Lerp(chase.transform.position,goal,1-Mathf.Exp(-Time.deltaTime*8));
            chase.transform.LookAt(car.transform.position+viewingForward*3+Vector3.up*.8f);
        }
        IEnumerator Capture(string name)
        {
            // WaitForEndOfFrame is not resumed in Unity batch mode. Manual
            // camera rendering works after a normal frame in either launch mode.
            yield return null;
            // Render the same live camera in a standalone texture, including URP
            // post effects. This deliberately excludes the GUI overlay.
            var rt=new RenderTexture(1920,1080,24);chase.targetTexture=rt;chase.Render();var old=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();
            File.WriteAllBytes(Path.Combine(captureFolder,name+".png"),tex.EncodeToPNG());chase.targetTexture=null;RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);
        }
        IEnumerator EndTrial(){if(captureFolder!=null)yield return Capture("03-final");yield return null;Application.Quit();}
        public void Finish(RoadCar car){if(finishOrder.Contains(car))return;finishOrder.Add(car);Debug.Log("FINISH id="+car.driverId+" time="+car.finishTime+" place="+finishOrder.Count);if(career&&car.driverId==0)SettleCareer();}
        void SettleCareer()
        {
            int place=finishOrder.IndexOf(cars[0])+1;
            try {
                int reward=AdventureSession.Store.FinishRace(AdventureSession.RaceSerial,place);
                AdventureSession.Notice=$"Finished {place}/24 · +{reward} credits. Visit Mara to buy and fit parts.";
                careerUI.Show("Coast run / result",$"Place {place} of 24 · {cars[0].finishTime:0.0}s\n{reward} credits saved · Wallet {AdventureSession.Store.Data.credits}\nYour owned parts stay with your car. Build toward midfield, then the podium.",new(){new("Return to Bayview",AdventureSession.ReturnToTown),new("Race again",AdventureSession.StartRace)});
            }catch(Exception e){Debug.LogError(e);careerUI.Show("Reward not saved yet",e.Message,new(){new("Retry saving this result",SettleCareer)});}
        }
        public void RecordImpact(RoadCar car,int other,float relativeSpeed,float penetration,string otherName)
        {
            impacts.Add(new ImpactSample{id=car.driverId,other=other,otherName=otherName,time=elapsed,relativeSpeed=relativeSpeed,penetration=penetration,progress=car.progress,offset=car.offset});
        }
        public void RecordCheckpoint(RoadCar car,float requiredProgress)
        {checkpointPassages.Add(new CheckpointSample{id=car.driverId,gate=car.nextGate,time=elapsed,requiredProgress=requiredProgress,actualProgress=car.progress});}
        public void RecordRecovery(RoadCar car,Vector3 before,Vector3 after,float beforeProgress,int beforeGate,string reason,int rejected)
        {
            assistedRecoveries.Add(new RecoverySample{id=car.driverId,time=elapsed,positionBefore=before,positionAfter=after,progressBefore=beforeProgress,progressAfter=car.progress,gateBefore=beforeGate,gateAfter=car.nextGate,reason=reason,rejectedDestinations=rejected});
            Debug.Log($"ASSISTED_RECOVERY id={car.driverId} time={elapsed:0.00} reason={reason} progress={beforeProgress:0.00}->{car.progress:0.00} rejected={rejected}");
        }
        void WriteReport()
        {
            if(reportWritten)return;reportWritten=true;
            if(technicalReview!=null)technicalReview.Save();
            GetComponent<RecoveryFixture>()?.Save();
            GetComponent<TrafficFixture>()?.Save();
            GetComponent<PathPredictionFixture>()?.Save();
            ApproachAudit?.Save(reportPath);
            var sorted=frameTimes.OrderBy(x=>x).ToArray();var r=new Report {engine=Application.unityVersion,stage=career?"Connected adventure race":"Standalone race trial",utc=DateTime.UtcNow.ToString("O"),
                trackId=technical==null?"bayview":technical.definition.id,trackVersion=technical==null?"bayview-v1":technical.definition.version,
                driverPolicy=DriverPolicyId,observationMode=DriverSnapshot==null?"legacy cached order":"shared pre-controller snapshot",
                collisionMode=cars[0].body.collisionDetectionMode.ToString(),graphics=SystemInfo.graphicsDeviceName,equipment=setup,gridMode=gridMode,seed=seed,carCount=cars.Count,finished=finishOrder.Count,requiredLaps=requiredLaps,simSeconds=elapsed,
                wallSeconds=Time.realtimeSinceStartup-wallStart,trackLength=track.length,trackWidth=Circuit.Width,autonomous=automation,physicsStepSeconds=Time.fixedDeltaTime,
                meanFrameMs=sorted.Length>0?sorted.Average():0,p95FrameMs=sorted.Length>0?sorted[(int)((sorted.Length-1)*.95f)]:0,
                overlapSampledSteps=collisionAudit==null?0:collisionAudit.sampledSteps,overlapQuerySaturations=collisionAudit==null?0:collisionAudit.querySaturations,
                maxCarPoseOverlap=collisionAudit==null?0:collisionAudit.maxCarOverlap,maxEnvironmentPoseOverlap=collisionAudit==null?0:collisionAudit.maxEnvironmentOverlap,
                overlapPeaks=collisionAudit==null?Array.Empty<CollisionAudit.Peak>():collisionAudit.peaks.ToArray(),
                cars=cars.Select(c=>new CarResult {id=c.driverId,laps=c.laps,nextGate=c.nextGate,place=finishOrder.IndexOf(c)+1,equipment=c.equipment,finished=c.finished,time=c.finishTime,
                    progress=c.progress,offset=c.offset,bestLap=c.bestLap==float.MaxValue?0:c.bestLap,contacts=c.contacts,recoveries=c.recoveries,maxImpact=c.maxImpact,
                    missedGates=c.missedGates,maxPenetration=c.maxPenetration,physicalRecoveries=c.physicalRecovery?.completed??0,
                    physicalRecoverySeconds=c.physicalRecovery?.activeSeconds??0,recoveryWaitSeconds=c.physicalRecovery?.waitSeconds??0,recoveryReverseSeconds=c.physicalRecovery?.reverseSeconds??0}).ToArray(),motion=motion.ToArray(),draftingSamples=draftingSamples.ToArray(),impacts=impacts.ToArray(),assistedRecoveries=assistedRecoveries.ToArray(),checkpointPassages=checkpointPassages.ToArray()};
            if(reportPath!=null){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));File.WriteAllText(reportPath,JsonUtility.ToJson(r,true));DriverAudit?.Save(reportPath);MotionAudit?.Save(reportPath);}
            Debug.Log("LITTLE_ROAD_TRIAL_REPORT finished="+r.finished+"/"+r.carCount+" time="+r.simSeconds+" file="+reportPath);
        }
        void OnGUI()
        {
            if(cars.Count==0||freeRoam||career)return;
            using var hud=new DisplaySettings.HudScale(true);
            title??=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,normal={textColor=new Color(.96f,.95f,.84f)}};
            label??=new GUIStyle(GUI.skin.label){fontSize=20,normal={textColor=Color.white}};
            small??=new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=new Color(.79f,.88f,.88f)}};
            GUI.color=new Color(.07f,.16f,.19f,.9f);GUI.DrawTexture(new Rect(20,20,480,148),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(36,29,450,38),technical==null?"LITTLE ROAD / Coastal Circuit":"LITTLE ROAD / Cedar Technical",title);
            int place=cars[0].finished?finishOrder.IndexOf(cars[0])+1:1+cars.Count(c=>c!=cars[0]&&(c.finished||c.RankedProgress>cars[0].RankedProgress));
            GUI.Label(new Rect(36,69,450,30),$"{Mathf.RoundToInt(Mathf.Abs(cars[0].speed)*2.237f)} mph     {place}/{cars.Count}     Lap {Mathf.Min(cars[0].laps+1,requiredLaps)}/{requiredLaps}",label);
            GUI.Label(new Rect(36,103,450,24),$"{setup.ToUpperInvariant()}  ·  {(cars[0].autonomous?"AI driving":"You are driving")}  ·  {Mathf.Max(0,elapsed):0.0}s",small);
            GUI.Label(new Rect(36,130,450,24),technical==null?"Unity migration trial — prototype art":technical.Region(cars[0].pathDistance)+"  ·  C: overview",small);
            GUI.color=new Color(.07f,.16f,.19f,.9f);GUI.DrawTexture(new Rect(20,hud.height-60,hud.width-40,40),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(36,hud.height-53,hud.width-65,30),"WASD / arrows or stick + R2/L2     Hold Q / Triangle: look behind     R / D-pad Down: recover     Tab / Share: AI     F11: fullscreen     Esc: quit",small);
            if(GUI.Button(new Rect(hud.width-320,244,300,32),DisplaySettings.Fullscreen?"Switch to windowed (F11)":"Switch to fullscreen (F11)"))DisplaySettings.Toggle();
            if(WaitingForReady) {
                GUI.Box(new Rect(hud.width*.5f-290,hud.height*.3f-15,580,150),"");
                GUI.Label(new Rect(hud.width*.5f-265,hud.height*.3f,550,45),"READY TO RACE?",title);
                GUI.Label(new Rect(hud.width*.5f-265,hud.height*.3f+43,550,32),"Press Enter / Options when you are ready",label);
                if(GUI.Button(new Rect(hud.width*.5f-130,hud.height*.3f+82,260,36),"Start countdown"))ConfirmManualStart(true);
            }else if(elapsed<0)GUI.Label(new Rect(hud.width*.5f-50,hud.height*.3f,120,60),Mathf.CeilToInt(-elapsed).ToString(),title);
            if(cars[0].finished)GUI.Label(new Rect(hud.width*.5f-200,hud.height*.22f,650,60),"FINISHED · "+cars[0].finishTime.ToString("0.0")+"s"+(Has("--trial-human")?" · Esc: exit":""),title);
            else if(PlayerRecoveryPending||cars[0].CheckpointMissed)GUI.Label(new Rect(36,180,hud.width-72,50),PlayerRecoveryPending?"RECOVERY REQUESTED — waiting for a clear space":cars[0].CheckpointMessage,label);
        }
    }
}
