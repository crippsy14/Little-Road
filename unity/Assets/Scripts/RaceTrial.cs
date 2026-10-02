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
        // The race in progress (not free roam), for settings applied live (race pacing, shadows, blur).
        public static bool Racing=>current!=null&&!current.freeRoam&&current.isActiveAndEnabled;static RaceTrial current;
        public bool PlayerRecoveryPending {get;private set;}
        public bool DraftingEnabled {get;private set;}
        public RaceWorldSnapshot DriverSnapshot {get;private set;}
        public DriverFrameAudit DriverAudit {get;private set;}
        public MotionPredictionAudit MotionAudit {get;private set;}
        public PathApproachAudit ApproachAudit {get;private set;}
        public RacingLine ReferenceLine {get;private set;}
        public RaceRoster Roster {get;private set;}
        RacePreviewUI racePreview;
        public bool PreviewOpen=>racePreview?.IsOpen==true;
        public RaceHud Hud {get;private set;}
        public bool ResultsOpen=>careerUI?.IsOpen==true;
        public int FinishPlace(RoadCar car)=>finishOrder.IndexOf(car)+1;
        public string DriverPolicyId {get;private set;}="legacy-v1";
        AdventureUI careerUI;
        int careerReward;
        TechnicalCircuit technical;
        TechnicalCircuitReview technicalReview;
        bool overview,wasLookingBehind;Vector3 chaseLag;
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
        readonly List<Vector2> playerTrace=new(); // (race distance in laps, place) at each checkpoint
        public int LivePlace(RoadCar car)=>car.finished?finishOrder.IndexOf(car)+1:1+cars.Count(c=>c!=car&&(c.finished||c.RankedProgress>car.RankedProgress));
        readonly List<MotionSample> motion=new();
        readonly List<DraftSample> draftingSamples=new();
        [Serializable] public class DraftSample {public int id,leader;public float time,reduction;}
        readonly List<ImpactSample> impacts=new();
        readonly List<RecoverySample> assistedRecoveries=new();
        readonly List<CheckpointSample> checkpointPassages=new();
        string gridMode="pack";
        string[] opponentSetups;
        float nextSample,sampleInterval=1;
        GUIStyle title,label,small;string reportTrack="bayview";
        public string TrackId=>reportTrack;
        [Serializable] public class DriverRosterEvidence {public int schema=1;public string mode;public DriverIdentity.Record[] drivers;}
        [Serializable] public class CarResult {public int id,laps,nextGate,place,contacts,recoveries,missedGates,physicalRecoveries;public string equipment;public bool finished;public float time,progress,offset,bestLap,maxImpact,maxPenetration,unsupportedSeconds,physicalRecoverySeconds,recoveryWaitSeconds,recoveryReverseSeconds;public int planReversals,passCommits,passCompleted,passAborted,abortTarget,abortTimeout,abortUnaccepted,lateBrakeTicks,gapCandidates,gapSelections,defendMoves;public float brakeSeconds,liftSeconds,stoppedSeconds,trafficSeconds,trafficBrakeSeconds,guardSeconds;}
        [Serializable] public class MotionSample {public int id,nextGate,laps;public float time,speed,targetSpeed,throttle,steering,offset,progress,headingError,nextGateProgress,predictedOffset,lateralSpeed,requestedYaw,actualYaw,chosenLane;public int reason=-1,intent=-1;}
        [Serializable] public class ImpactSample {public int id,other;public string otherName;public float time,relativeSpeed,penetration,progress,offset;}
        [Serializable] public class RecoverySample {public int id,gateBefore,gateAfter,rejectedDestinations;public string reason;public float time,progressBefore,progressAfter;public Vector3 positionBefore,positionAfter;}
        [Serializable] public class CheckpointSample {public int id,gate;public float time,requiredProgress,actualProgress;}
        [Serializable] public class Report {public string engine,stage,utc,graphics,equipment,gridMode,collisionMode,trackId,trackVersion,driverPolicy,observationMode;public int seed,carCount,finished,requiredLaps,overlapSampledSteps,overlapQuerySaturations;public float simSeconds,wallSeconds,trackLength,trackWidth,meanFrameMs,p95FrameMs,physicsStepSeconds,maxCarPoseOverlap,maxEnvironmentPoseOverlap;public bool autonomous;public CarResult[] cars;public MotionSample[] motion;public DraftSample[] draftingSamples;public ImpactSample[] impacts;public RecoverySample[] assistedRecoveries;public CheckpointSample[] checkpointPassages;public CollisionAudit.Peak[] overlapPeaks;}
        public static string Arg(string key,string fallback=null) {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:RaceAiPreset.Value(key,fallback);}
        public static bool Has(string key)=>Array.IndexOf(Environment.GetCommandLineArgs(),key)>=0||RaceAiPreset.Enabled(key);
        // Load timing: every race entry and exit reloads this scene and rebuilds the world at runtime.
        public static float SceneStart;
        public static string LoadTime=>$"load={Time.realtimeSinceStartup-SceneStart:0.00}s sinceLaunch={Time.realtimeSinceStartup:0.00}s";
        void Awake()=>SceneStart=Time.realtimeSinceStartup;
        IEnumerator Start()
        {
            current=this;Controls.InteractionReady=false;
            if(Has("--performance-trial")){enabled=false;gameObject.AddComponent<PerformanceTrial>();yield break;}
            if(Has("--trial-gallery")){gameObject.AddComponent<CarGallery>();yield break;}
            career=AdventureSession.Racing;
            if(!career&&!Has("--trial-auto")&&!Has("--trial-race")){freeRoam=true;gameObject.AddComponent<AdventureGame>();yield break;}
            sampleInterval=Arg("--traffic-case","").StartsWith("draft-")?1f/120:1;
            automation=Has("--trial-auto")||AdventureSession.Smoke||(career&&(Has("--valley-race-review")||Has("--duke-race-review")));int.TryParse(Arg("--trial-cars","24"),out field);field=Mathf.Clamp(field,1,24);
            float.TryParse(Arg("--trial-duration","240"),out duration);int.TryParse(Arg("--trial-seed","1709"),out seed);
            int.TryParse(Arg("--trial-laps","3"),out requiredLaps);requiredLaps=Mathf.Max(1,requiredLaps);
            setup=Arg("--trial-setup","stock");reportPath=Arg("--trial-report");captureFolder=Arg("--trial-captures");
            if(career&&AdventureSession.Smoke&&AdventureSession.RaceSerial>1) {
                if(reportPath!=null)reportPath=Path.Combine(Path.GetDirectoryName(reportPath),Path.GetFileNameWithoutExtension(reportPath)+"-race-"+AdventureSession.RaceSerial+Path.GetExtension(reportPath));
                if(captureFolder!=null)captureFolder=Path.Combine(captureFolder,"race-"+AdventureSession.RaceSerial);
            }
            if(reportPath!=null)DriverAudit=new DriverFrameAudit();
            if(career){field=AdventureSession.EnteredRoster?.Entries.Count??24;requiredLaps=AdventureSession.EnteredRoster?.Laps??3;setup="owned equipment";seed=1709+AdventureSession.RaceSerial*11;careerUI=gameObject.AddComponent<AdventureUI>();}
            gridMode=Arg("--trial-grid","pack");opponentSetups=Arg("--trial-opponents","").Split(',',StringSplitOptions.RemoveEmptyEntries);
            if(gridMode!="pack"&&gridMode!="line")throw new ArgumentException("Unknown trial grid: "+gridMode);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=120;Time.fixedDeltaTime=1f/120;Time.maximumDeltaTime=.15f;
            // A player's race follows their Frame rate setting (VSync by default): the uncapped 120 fps target
            // presented unevenly on 60/144 Hz displays (judder; frame-cost-127). The drivers step at a fixed
            // 120 Hz either way. Automated trials keep the old timing.
            if(career&&!automation&&!Application.isBatchMode)GraphicsOptions.ApplyRacePacing();
            // Races keep the 120 fps target they were tuned at: the drivers plan per rendered frame
            // cadence, and a vSync cap (60 Hz, or a throttled hidden window) jammed the smoke race's start.
            Application.runInBackground=true;
            string trackId=career?AdventureSession.EnteredRoster?.Track??"bayview":Arg("--trial-track","bayview");
            if(trackId=="cedar") {technical=new TechnicalCircuit();track=technical.track;TechnicalCircuitWorld.Create(technical);}
            // Cedar Hills: Cedar's layout with 23 m of elevation (the grade-aware driver test circuit).
            else if(trackId=="cedar-hills") {technical=new TechnicalCircuit("cedar-hills");track=technical.track;TechnicalCircuitWorld.Create(technical);}
            // The test loop: two 1-mile straights and two wide turns (2.5 miles) for gearing, top speed and sound
            // tests up to class E (Richard, 1 Oct). Run it with --trial-duration: a slow car takes a while.
            else if(trackId=="test-loop") {technical=new TechnicalCircuit("test-loop");track=technical.track;TechnicalCircuitWorld.Create(technical);}
            else if(trackId=="bayview"){track=new Circuit();yield return TrialWorld.CreateRoutine(track);}
            // Hillcrest Raceway: the valley circuit (built with the mainland); the island loop is scenery.
            else if(trackId=="hillcrest"){yield return TrialWorld.CreateRoutine(new Circuit(),false);track=new Circuit(HillcrestRoute.Circuit);RenderSettings.fogDensity=.00026f;}
            else throw new ArgumentException("Unknown trial track: "+trackId);
            reportTrack=trackId;
            // The elevated Hillcrest Raceway always races grade-aware (its context drivers could not race it
            // otherwise; the model is exactly neutral on level circuits, study 124). Elsewhere it is opt-in.
            GradeModel.Enabled=Has("--grade-aware")||(trackId=="hillcrest"&&track.hasElevation);GradeModel.Physics=GradeModel.Enabled&&!Has("--grade-no-physics");
            RoadCar.GateFirst=Has("--gate-first");PhysicalRecovery.RejoinAtSpeed=Has("--rejoin-at-speed");
            if(track.hasElevation&&Arg("--driver-policy","legacy")!="legacy"&&!GradeModel.Enabled&&!career)Debug.LogWarning("Elevated circuit without --grade-aware: context forecasts will refuse it.");
            bool connectedModern=career&&AdventureSession.EnteredRoster?.Modern==true;
            string policy=career?(connectedModern?"context":"legacy"):Arg("--driver-policy","legacy");
            // Racing AI v2 (docs/46) takes the place of the context driver on the same fixtures (--first-ai: the first AI).
            if(RaceAiPreset.RacerV2&&policy=="context")policy="racer";
            if(Has("--dynamic-rejoin")&&((career&&!connectedModern)||policy!="context"||(field!=1&&!Has("--dynamic-traffic"))||!Has("--road-feasibility")))throw new ArgumentException("Dynamic rejoin requires declared context fixtures and physical road checks.");
            bool bayviewStudy=Has("--bayview-transfer-study")||connectedModern;
            if(bayviewStudy&&((career&&!connectedModern)||(trackId!="bayview"&&trackId!="hillcrest")||(policy!="legacy"&&policy!="context"&&policy!="racer")||!((field==24&&Arg("--traffic-case")=="field")||(field==RaceGrid.ClassSortCars&&Arg("--traffic-case")=="class-sort")||(field==1&&Arg("--traffic-case")=="standing-solo")||(career&&connectedModern&&field==AdventureSession.EnteredRoster.Entries.Count))))throw new ArgumentException("Bayview transfer requires explicit standalone legacy/context standing-solo or24-car field.");
            bool dynamicFieldStudy=Has("--dynamic-field-study");
            if(Has("--dynamic-blocking-target")&&!Has("--dynamic-traffic"))throw new ArgumentException("Dynamic blocking targets require the dynamic traffic study.");
            if(Has("--dynamic-traffic-fallback")&&!Has("--dynamic-traffic"))throw new ArgumentException("Dynamic traffic fallback requires the dynamic traffic study.");
            if(dynamicFieldStudy&&((career&&!connectedModern)||(trackId!="cedar"&&trackId!="cedar-hills"&&!bayviewStudy)||policy!="context"||(Arg("--traffic-case")!="field"&&Arg("--traffic-case")!="class-sort")||!Has("--dynamic-traffic")))throw new ArgumentException("Dynamic field study requires the explicit Cedar context field fixture and dynamic traffic.");
            if(Has("--dynamic-traffic")&&(!Has("--dynamic-rejoin")||!Has("--local-origin-curvature")||!Has("--traffic-path-check")||(field>3&&!dynamicFieldStudy)))throw new ArgumentException("Dynamic traffic requires corrected local envelopes, moving-traffic checks and an explicit field-study opt-in above three cars.");
            if(Has("--local-origin-curvature")&&!Has("--dynamic-rejoin"))throw new ArgumentException("Local-origin curvature is currently part of the solo dynamic-rejoin study.");
            DraftingEnabled=Has("--light-drafting");
            if(DraftingEnabled&&((career&&!connectedModern)||(policy!="context"&&policy!="racer")))throw new ArgumentException("Light drafting is currently supported only by context prototype fixtures.");
            if(policy=="reference"||policy=="traffic"||policy=="context"||policy=="racer") {
                bool referenceRearControl=Arg("--traffic-case")=="rear-control"&&field==2;
                if((technical==null&&!bayviewStudy)||(policy=="reference"&&field!=1&&!Has("--recovery-case")&&!referenceRearControl)||(policy=="traffic"&&!Has("--traffic-case")))throw new ArgumentException("New drivers require their declared Cedar development fixtures.");
                if((policy=="context"||policy=="racer")&&Array.IndexOf(new[]{"draft-straight","draft-pullout","solo","standing-solo","pace-solo","rear-control","rear-control-left","rear-control-right","follow","pass","crossing-pass","brake-leader","brake-probe","queue","no-room","field","class-sort","merge-left","merge-right","merge-witness-left","merge-witness-right","corner-solo-left","corner-solo-middle","corner-solo-right","corner-solo-deep","corner-pair-left","corner-pair-right","corner-three"},Arg("--traffic-case"))<0)throw new ArgumentException("Context prototype requires a declared supported development fixture.");
                // Hillcrest's line is baked from the live circuit (same method as the Bayview/Cedar seeds).
                // Cedar Hills reuses Cedar's authored line (same layout), so only the elevation differs.
                ReferenceLine=trackId=="cedar-hills"?new RacingLine(track,RacingLine.Transfer(track,"cedar-smooth-v2","cedar-hills")):
                    trackId=="hillcrest"||trackId=="test-loop"?new RacingLine(track,RacingLine.BakeSmooth(track,trackId+"-smooth-v1",trackId)):
                    new RacingLine(track,Arg("--racing-line","cedar-mincurve-v1"),technical==null?"bayview":technical.definition.id,technical==null?"bayview-v1":technical.definition.version);DriverPolicyId=policy=="traffic"?"traffic-v1":policy=="context"?"context-v1":policy=="racer"?"racer-v2":"reference-v1";
            }else if(policy!="legacy")throw new ArgumentException("Unknown driver policy: "+policy);
            if(Has("--driver-roster")&&((career&&!connectedModern)||(policy!="context"&&policy!="racer")))throw new ArgumentException("Driver roster currently requires standalone context comparisons.");
            if(Has("--driver-form")&&!Has("--driver-roster"))throw new ArgumentException("Driver form needs declared identities.");
            if(!career||AdventureSession.Smoke)Time.timeScale=Mathf.Clamp(float.Parse(Arg("--trial-timescale","1"),System.Globalization.CultureInfo.InvariantCulture),.25f,8);
            Roster=career?AdventureSession.EnteredRoster:RaceRoster.Trial(field,requiredLaps,seed,trackId,setup,opponentSetups,Arg("--traffic-case"),Has("--driver-roster")?Arg("--driver-roster"):null,Has("--driver-form"),Has("--trial-body")?Arg("--trial-body"):Has("--path-approach-file")?PathApproachAudit.Replay().bodyAsset:null);
            if(Roster==null||Roster.Entries.Count!=field||Roster.Seed!=seed)throw new InvalidOperationException("Entered roster does not match this race.");
            foreach(var entrant in Roster.Entries) {
                int i=entrant.Id;var go=TrialWorld.Car(entrant.Body,entrant.Paint);bool line=gridMode=="line";float grid=RaceGrid.Distance(entrant.Slot,line);
                go.transform.SetPositionAndRotation(RaceGrid.Position(track,entrant.Slot,line),Quaternion.LookRotation(track.Forward(grid)));
                var car=go.AddComponent<RoadCar>();car.Setup(this,i,entrant.Tuning,automation||i!=0,seed+i*997,entrant.EquipmentLabel,entrant.Driver);cars.Add(car);
                // The human's car in adventure races sweeps its contacts like free roam: speculative
                // contacts snag Bayview road-mesh edges mid-corner. AI cars keep their validated mode.
                if(career&&!car.autonomous&&!Has("--free-roam-speculative"))car.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
                // The Duke's car too: at 60-75 m/s speculative contacts snagged the road mesh edges violently (docs/54).
                if(Roster.Event==RaceRoster.DukeEvent)car.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            }
            if(reportPath!=null)Roster.Save(Path.ChangeExtension(reportPath,"roster.json"));
            if(Has("--driver-roster")&&reportPath!=null){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));File.WriteAllText(Path.ChangeExtension(reportPath,"drivers.json"),JsonUtility.ToJson(new DriverRosterEvidence{mode=Arg("--driver-roster"),drivers=cars.Select(c=>c.Driver.Export(c.driverId)).ToArray()},true));}
            if(Has("--recovery-case"))gameObject.AddComponent<RecoveryFixture>().Setup(this);
            if(Has("--traffic-case")&&!bayviewStudy&&!career)gameObject.AddComponent<TrafficFixture>().Setup(this,technical);
            if(Has("--path-probe"))gameObject.AddComponent<PathPredictionFixture>().Setup(this,technical);
            if((!career||connectedModern)&&(Has("--shared-driver-snapshot")||ReferenceLine!=null||Has("--physical-recovery"))){DriverSnapshot=gameObject.AddComponent<RaceWorldSnapshot>();DriverSnapshot.Setup(this);}
            if(ReferenceLine!=null&&!career&&!RaceHud.Requested&&!Has("--clean-view"))gameObject.AddComponent<RacingLineOverlay>().Setup(this,cars[0].referenceProfile);
            chase=new GameObject("Chase camera").AddComponent<Camera>();chase.tag="MainCamera";chase.fieldOfView=52;chase.nearClipPlane=.12f;chase.farClipPlane=trackId=="hillcrest"?6000:1800;
            chase.gameObject.AddComponent<AudioListener>();WorldLook.Camera(chase);
            if(GrassField.Wanted)gameObject.AddComponent<GrassField>().Setup(chase);
            if(!Application.isBatchMode)WorldLook.RaceBlur();
            // Dust, tyre smoke and tyre marks for every car in a rendered race (visual only).
            if(!Application.isBatchMode&&!freeRoam)foreach(var raceCar in cars)raceCar.gameObject.AddComponent<DustTrail>().Setup(raceCar);
            if(reportPath!=null){collisionAudit=gameObject.AddComponent<CollisionAudit>();collisionAudit.Setup(this);}
            if(technical!=null||bayviewStudy){technicalReview=gameObject.AddComponent<TechnicalCircuitReview>();technicalReview.Setup(this,technical??new TechnicalCircuit(track,trackId),reportPath);}
            if(Has("--motion-audit"))MotionAudit=new MotionPredictionAudit(Arg("--motion-audit"),Time.fixedDeltaTime,cars.Count);
            if(Has("--capture-path-approach"))ApproachAudit=new PathApproachAudit(int.Parse(Arg("--capture-path-approach")),cars.Count);
            // Rendered races warm up behind the loading card, like free roam (AdventureGame.WarmUp): look along
            // the lap and force LOD cross-fades both ways so shaders and their fade variants compile now.
            if(!Application.isBatchMode&&!Has("--no-warm-up"))yield return WarmUpLap();
            UpdateCamera(true);CameraShake.Reset();if(cars.Count>0){CameraShake.Watch(cars[0]);if(!freeRoam)FrameProbe.Attach(cars[0].transform);}wallStart=Time.realtimeSinceStartup;running=true;
            manualStart=new ManualRaceStart(!career&&!automation&&Has("--trial-human")&&!Has("--race-preview")&&!Has("--race-preview-review"));
            if(WaitingForReady){Time.timeScale=0;Debug.Log("LITTLE_ROAD_MANUAL_WAITING countdown=-3");}
            RaceGrid.Capture(this,captureFolder);
            if(bayviewStudy&&captureFolder==null&&reportPath!=null)RaceGrid.Capture(this,Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            if((Arg("--traffic-case")=="standing-solo"||Arg("--traffic-case")=="class-sort")&&captureFolder==null&&reportPath!=null)RaceGrid.Capture(this,Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            if(Has("--vertical-audit"))gameObject.AddComponent<VerticalMotionAudit>().Setup(this);
            if(career)CarContact.SeatAll(); // the world's standing cars on their wheels (docs/57, 2.5); race cars aren't standing
            Debug.Log("LITTLE_ROAD_UNITY_READY cars="+cars.Count+" width="+Circuit.Width+" length="+track.length+" "+LoadTime);LoadingScreen.Ready();
            if((career&&!Has("--no-race-hud"))||RaceHud.Requested){Hud=gameObject.AddComponent<RaceHud>();Hud.Setup(this);}
            if(!career&&(Has("--race-preview")||Has("--race-preview-review"))) {
                float resume=Time.timeScale;Time.timeScale=0;racePreview=gameObject.AddComponent<RacePreviewUI>();
                racePreview.Open(Roster,()=>Time.timeScale=resume,()=>Application.Quit());
                if(Has("--race-preview-review"))gameObject.AddComponent<RacePreviewReview>().Setup(racePreview,this);
            }
            if(Has("--evening-route-review"))gameObject.AddComponent<EveningRouteReview>();
            if(captureFolder!=null) {Directory.CreateDirectory(captureFolder);yield return new WaitForSeconds(2);yield return Capture("01-grid");
                yield return new WaitForSeconds(15);yield return Capture("02-race");
                if(career&&Has("--race-pause-review"))yield return ReviewPause();}
        }
        void FixedUpdate(){long start=System.Diagnostics.Stopwatch.GetTimestamp();FrameCost.Steps++;try{RaceStep();}finally{FrameCost.Race+=System.Diagnostics.Stopwatch.GetTimestamp()-start;}}
        void RaceStep(){if(running&&!freeRoam) {
            elapsed+=Time.fixedDeltaTime;
            if(elapsed>=nextSample) {nextSample+=sampleInterval;
                foreach(var c in cars)motion.Add(new MotionSample{id=c.driverId,time=elapsed,speed=c.speed,targetSpeed=c.targetSpeed,
                    throttle=c.throttle,steering=c.steering,offset=c.offset,progress=c.progress,
                    headingError=Vector3.SignedAngle(c.transform.forward,track.Forward(c.pathDistance),Vector3.up),nextGate=c.nextGate,laps=c.laps,nextGateProgress=c.nextGateProgress,
                    predictedOffset=c.predictedOffset,lateralSpeed=c.lateralSpeed,requestedYaw=c.requestedYaw,actualYaw=c.body.angularVelocity.y,chosenLane=c.chosenLane,reason=c.racerPolicy==null?-1:(int)c.racerPolicy.Binding,intent=c.racerPolicy==null?-1:(int)c.racerPolicy.State});
                if(DraftingEnabled)foreach(var c in cars)draftingSamples.Add(new DraftSample{id=c.driverId,time=elapsed,leader=c.DraftLeader,reduction=c.DragReduction});
            }
        }}
        void Update()
        {
            if(!running||freeRoam||racePreview?.IsOpen==true||racePreview?.LastCloseFrame==Time.frameCount)return;
            bool waiting=WaitingForReady;
            // The in-race pause menu (Richard, 2 Oct: pause used to quit the race). Esc / Options pauses a career race;
            // Esc / B (or Options again) resumes. Once you've finished, it goes back to town as before.
            bool pausePressed=Keyboard.current?.escapeKey.wasPressedThisFrame==true||(career&&!waiting&&Gamepad.all.Any(g=>g.startButton.wasPressedThisFrame));
            if(career&&!cars[0].finished&&!(careerUI.IsOpen&&!Paused)) {
                if(Paused&&(OptionsScreen.IsOpen||OptionsScreen.LastCloseFrame==Time.frameCount))return;
                if(Paused){if(pausePressed||Gamepad.all.Any(g=>g.buttonEast.wasPressedThisFrame))ResumeRace();return;}
                if(pausePressed){OpenPause();return;}
            }
            if(WaitingForReady)ConfirmManualStart(Keyboard.current?.enterKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true);
            DriverAudit?.EndFrame(elapsed);
            if(Has("--trial-human")&&cars[0].finished&&!reportWritten)WriteReport();
            if(technical!=null&&Keyboard.current?.cKey.wasPressedThisFrame==true)overview=!overview;
            if(elapsed>5)frameTimes.Add(Time.unscaledDeltaTime*1000);
            if(!cars[0].finished&&Controls.Pressed(Controls.Act.Recover))PlayerRecoveryPending=true;
            if(PlayerRecoveryPending&&cars[0].TryRecover())PlayerRecoveryPending=false;
            if(!career&&(Keyboard.current?.tabKey.wasPressedThisFrame==true || Gamepad.current?.selectButton.wasPressedThisFrame==true))cars[0].autonomous=!cars[0].autonomous;
            if(pausePressed) {if(career){AdventureSession.Notice=cars[0].finished?"Back in Bayview. Your result and reward are saved.":"Race retired · no payout. Your equipment is unchanged.";AdventureSession.ReturnToTown();}else{WriteReport();Application.Quit();}}
            if(career) {
                careerUI.SetHudVisible(Hud==null&&!careerUI.IsOpen);
                // Only while it shows (the race HUD normally replaces it): rebuilding it every frame cost time.
                if(Hud==null){int place=LivePlace(cars[0]);
                careerUI.HUD($"BAYVIEW COAST / CLASS C   ·   {place}/{cars.Count}   ·   Lap {Mathf.Min(cars[0].laps+1,requiredLaps)}/3   ·   {Units.Show(Mathf.Abs(cars[0].speed))}\n{(elapsed<0?"START IN "+Mathf.CeilToInt(-elapsed):"Race time "+(cars[0].finished?cars[0].finishTime:elapsed).ToString("0.0")+"s")}   ·   {AdventureSession.Store.Data.credits} credits",
                    PlayerRecoveryPending?"RECOVERY REQUESTED — waiting for a clear space on the road":cars[0].CheckpointMissed?cars[0].CheckpointMessage:InputGlyphs.Race);}
            }
            if(automation && !reportWritten && (elapsed>=duration || finishOrder.Count==cars.Count)) {
                WriteReport();
                if(career&&AdventureSession.Smoke){
                    if(finishOrder.Count!=cars.Count)throw new Exception("Adventure race did not complete its field.");
                    StartCoroutine(FinishCareerSmoke());
                }
                else StartCoroutine(EndTrial());
            }
        }
        // Let the results screen settle (stamp, confetti, podium renders) before capturing,
        // then continue the smoke sequence.
        IEnumerator FinishCareerSmoke()
        {
            if(captureFolder!=null){
                float until=Time.realtimeSinceStartup+1.5f;
                while(Time.realtimeSinceStartup<until||careerUI.Result!=null&&!careerUI.Result.PodiumReady)yield return null;
                careerUI.Capture(Path.Combine(captureFolder,"03-race-result.png"));
                // A visible run also grabs the real back buffer, proving the menu camera stack draws on screen.
                if(!Application.isBatchMode){yield return new WaitForEndOfFrame();var grab=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);grab.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);File.WriteAllBytes(Path.Combine(captureFolder,"03-race-result-screen.png"),grab.EncodeToPNG());Destroy(grab);}
                if(careerUI.Result!=null){careerUI.Result.OpenStory();yield return null;careerUI.Capture(Path.Combine(captureFolder,"03c-race-story.png"));careerUI.Result.CloseStory();}
            }
            if(Has("--adventure-retry-review")&&AdventureSession.RaceSerial==1)StartCoroutine(ReviewCareerRetry());
            else AdventureSession.ReturnToTown();
        }
        void LateUpdate(){if(chase!=null&&cars.Count>0)UpdateCamera(false);}
        // ---- the in-race pause menu
        // --race-pause-review (the adventure smoke): pause mid-race, view the field, back, resume; the race must hold still
        // while paused and carry on afterwards.
        IEnumerator ReviewPause()
        {
            // Cars on the road (docs/57, 2.5): every racing car's wheel gaps, measured mid-race at the pause.
            float before=elapsed;OpenPause();yield return new WaitForSecondsRealtime(.6f);
            Debug.Log("LITTLE_ROAD_RACE_CAR_CONTACT "+CarContact.Audit(out int raceCars).Replace("\n"," | "));
            if(!Paused||Time.timeScale!=0)throw new Exception("Pause did not stop the race.");
            careerUI.Capture(Path.Combine(captureFolder,"02c-pause.png"));
            if(!careerUI.InvokeChoice("View the field"))throw new Exception("Pause menu has no View the field.");
            yield return new WaitForSecondsRealtime(.8f);
            if(racePreview==null||!racePreview.IsOpen||!racePreview.ViewOnly)throw new Exception("The field did not open view-only.");
            racePreview.Capture(Path.Combine(captureFolder,"02d-field.png"));racePreview.Cancel();yield return new WaitForSecondsRealtime(.3f);
            if(!Paused||!careerUI.IsOpen)throw new Exception("Back from the field did not return to the pause menu.");
            if(!careerUI.InvokeChoice("Options"))throw new Exception("Pause menu has no Options.");
            yield return new WaitForSecondsRealtime(.6f);
            if(!OptionsScreen.IsOpen)throw new Exception("Options did not open from the race pause menu.");
            OptionsScreen.Instance.Capture(Path.Combine(captureFolder,"02e-options-graphics.png"));
            OptionsScreen.Instance.ShowTab(3);yield return new WaitForSecondsRealtime(.3f);OptionsScreen.Instance.Capture(Path.Combine(captureFolder,"02f-options-driving.png"));
            OptionsScreen.Instance.Close();yield return new WaitForSecondsRealtime(.3f);
            if(!Paused||!careerUI.IsOpen)throw new Exception("Closing Options did not return to the pause menu.");
            if(Mathf.Abs(elapsed-before)>.001f)throw new Exception("The race moved while paused.");
            if(!careerUI.InvokeChoice("Back to the race"))throw new Exception("Pause menu has no Back to the race.");
            yield return new WaitForSecondsRealtime(.5f);
            if(Paused||Time.timeScale<=0||elapsed<=before)throw new Exception("The race did not resume.");
            Debug.Log("LITTLE_ROAD_RACE_PAUSE_REVIEW_PASSED held="+before.ToString("0.0")+" resumed="+elapsed.ToString("0.0"));
        }
        public bool Paused{get;private set;}
        float pausedScale=1;
        public void OpenPause()
        {
            if(!Paused){Paused=true;pausedScale=Time.timeScale;Time.timeScale=0;AudioListener.pause=true;}
            careerUI.SetVisible(true);
            int place=LivePlace(cars[0]);string where=(Roster?.Title??"Race").Replace("/","·");
            string status=elapsed<0?"On the grid · the countdown is held":$"{Toy.Ordinal(place)} of {cars.Count}  ·  lap {Mathf.Min(cars[0].laps+1,requiredLaps)} of {requiredLaps}  ·  {AdventureUI.FinishTimeLabel(elapsed)}";
            // The running order round you: the leader, the car ahead and the car behind.
            if(elapsed>=0) {
                var order=LiveStandings();int me=order.FindIndex(o=>o.player);
                string Who(int i)=>i>=0&&i<order.Count?$"{Toy.Ordinal(order[i].place)}  {order[i].name}":null;
                var lines=new List<string>();
                if(me!=0)lines.Add("Leading:  "+Who(0));
                if(me>1)lines.Add("Ahead of you:  "+Who(me-1));
                if(me<order.Count-1)lines.Add("Behind you:  "+Who(me+1));
                if(lines.Count>0)status+="\n\n"+string.Join("\n",lines);
            }
            var choices=new List<AdventureUI.Choice>{new("Back to the race",ResumeRace),new("Restart race",RestartRace)};
            if(Roster!=null&&Roster.Entries.Count==24)choices.Add(new("View the field",ViewField));
            choices.Add(new("Options",()=>{careerUI.Close();careerUI.SetVisible(false);OptionsScreen.Open(OpenPause);}));
            choices.Add(new("Retire to town · no payout",RetireRace));
            careerUI.Show("PAUSED / "+where,status,choices);
            Debug.Log("LITTLE_ROAD_RACE_PAUSED elapsed="+elapsed.ToString("0.0"));
        }
        public void ResumeRace()
        {
            if(!Paused)return;
            careerUI.Close();Paused=false;Time.timeScale=pausedScale;AudioListener.pause=false;
        }
        void RestartRace(){AudioListener.pause=false;Paused=false;AdventureSession.RestartRace();}
        void RetireRace()
        {
            AudioListener.pause=false;Paused=false;
            AdventureSession.Notice="Race retired · no payout. Your equipment is unchanged.";AdventureSession.ReturnToTown();
        }
        void ViewField()
        {
            careerUI.Close();careerUI.SetVisible(false);
            if(racePreview==null)racePreview=gameObject.AddComponent<RacePreviewUI>();
            racePreview.Open(Roster,OpenPause,OpenPause,viewOnly:true);
        }
        void ConfirmManualStart(bool requested)
        {
            if(manualStart?.TryStart(Application.isFocused,requested)!=true)return;
            Time.timeScale=1;wallStart=Time.realtimeSinceStartup;
            Debug.Log("LITTLE_ROAD_MANUAL_READY countdown="+elapsed);
        }
        IEnumerator WarmUpLap()
        {
            float bias=QualitySettings.lodBias;
            for(int i=0;i<10;i++) {
                float d=track.length*i/10;var f=track.Forward(d);
                chase.transform.position=track.At(d)-f*6+Vector3.up*(i%2==0?4:25);chase.transform.LookAt(track.At(d)+f*30);WorldLook.Cut(chase);
                yield return null;QualitySettings.lodBias=bias*.2f;for(int k=0;k<3;k++)yield return null;QualitySettings.lodBias=bias;for(int k=0;k<3;k++)yield return null;
            }
            QualitySettings.lodBias=bias;
        }
        void OnApplicationQuit(){if(running&&!freeRoam&&!career&&cars.Count>0)WriteReport();}
        void UpdateCamera(bool snap)
        {
            bool behind=DrivingViewInput.LookingBehind;
            bool flip=behind!=wasLookingBehind;snap|=flip;wasLookingBehind=behind;if(snap)WorldLook.Cut(chase);
            if(technical!=null&&overview&&!behind) {
                chase.orthographic=true;chase.orthographicSize=230;
                chase.transform.SetPositionAndRotation(new Vector3(-50,700,-8),Quaternion.Euler(90,0,0));return;
            }
            chase.orthographic=false;
            var car=cars[0];var viewingForward=car.transform.forward*(behind?-1:1);
            Vector3 goal=car.transform.position-viewingForward*5.8f+Vector3.up*3.1f;
            // Looking back the camera is locked 5.8 m ahead of the car; flipping back restores the forward
            // view where it was trailing (no snap-then-drift zoom). The cut also skips motion blur and TAA history.
            if(behind)chase.transform.position=goal;
            else if(flip)chase.transform.position=goal+car.transform.TransformVector(chaseLag);
            else if(snap){chase.transform.position=goal;chaseLag=Vector3.zero;}
            else{chase.transform.position=Vector3.Lerp(chase.transform.position,goal,1-Mathf.Exp(-Time.deltaTime*8));chaseLag=car.transform.InverseTransformVector(chase.transform.position-goal);}
            chase.transform.LookAt(car.transform.position+viewingForward*3+Vector3.up*.8f);
            float fov=WorldLook.SpeedFov(52+GraphicsOptions.FovOffset,car.speed);chase.fieldOfView=snap?fov:Mathf.Lerp(chase.fieldOfView,fov,1-Mathf.Exp(-Time.deltaTime*3));
            if(!snap)CameraShake.Apply(chase);
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
            if(Roster?.Event==RaceRoster.DukeEvent){SettleDuke(place);return;}
            try {
                bool valley=Roster?.Track=="hillcrest";var data=AdventureSession.Store.Data;string openBefore=RaceCalendar.Open(data);bool doneBefore=RaceCalendar.Complete(data,openBefore);
                int reward=AdventureSession.Store.FinishRace(AdventureSession.RaceSerial,place,valley?"hillcrest":"bayview");
                careerReward=reward;string openNow=RaceCalendar.Open(AdventureSession.Store.Data);
                AdventureSession.Notice=openNow!=openBefore?$"Finished {place}/24 · +{reward} credits · Class {openBefore} complete! Class {openNow} is open.":
                    !doneBefore&&RaceCalendar.Complete(AdventureSession.Store.Data,openBefore)?$"Finished {place}/24 · +{reward} credits · Class {openBefore} complete! New races and parts are on the way.":
                    $"Finished {place}/24 · +{reward} credits. "+(valley?"Gio at Hillcrest Motors has valley parts.":"Visit Mara to buy and fit parts.");
                ShowCareerResult();
            }catch(Exception e){Debug.LogError(e);careerUI.Show("Reward not saved yet",e.Message,new(){new("Retry saving this result",SettleCareer)});}
        }
        // The Duke's Challenge (docs/54): the Regale and the prize on your first win.
        void SettleDuke(int place)
        {
            try {
                int paid=AdventureSession.Store.FinishDukeRace(AdventureSession.RaceSerial,place);careerReward=paid;
                var duke=cars.Count>1?cars[1]:null;float gap=duke!=null&&duke.finished&&cars[0].finished?Mathf.Abs(cars[0].finishTime-duke.finishTime):0;
                AdventureSession.Notice=place==1?(paid>0?$"You beat Duke Rolo! +{paid:N0} credits, and the Regale is yours: swap to it at any Pit Stop garage.":
                        $"You beat Duke Rolo again, by {gap:0.0} s. \"Splendid! Same time next week?\""):
                    $"Duke Rolo wins{(gap>0?$" by {gap:0.0} s":"")}. \"Come back when your car can keep up with the Regale!\"";
                Debug.Log($"LITTLE_ROAD_DUKE_RACE place={place} paid={paid} gap={gap:0.0}");
                ShowCareerResult();
            }catch(Exception e){Debug.LogError(e);careerUI.Show("Result not saved yet",e.Message,new(){new("Retry saving this result",()=>SettleDuke(place))});}
        }
        void ShowCareerResult()
        {
            var me=cars[0];var entrant=Roster?.Entries[me.driverId];
            var summary=new RaceResultSummary{place=finishOrder.IndexOf(me)+1,entrants=cars.Count,grid=entrant!=null?entrant.Slot+1:cars.Count,reward=careerReward,
                wallet=AdventureSession.Store.Data.credits,laps=requiredLaps,finishTime=me.finishTime,bestLap=me.bestLap,standings=LiveStandings};
            if(Roster!=null)summary.venue=Roster.Event==RaceRoster.DukeEvent?"The Duke's Challenge":Roster.Title.Split('/')[0].Trim()+" Cup";
            summary.trace.Add(new Vector2(0,summary.grid));summary.trace.AddRange(playerTrace);summary.trace.Add(new Vector2(requiredLaps,summary.place));
            careerUI.ShowRaceResult(summary,AdventureSession.ReturnToTown,PreviewNextCareerRace);
        }
        // Finishers in order, then cars still racing by progress. Read live by the results board.
        List<RaceStanding> LiveStandings()
        {
            var ordered=finishOrder.Concat(cars.Where(c=>!finishOrder.Contains(c)).OrderByDescending(c=>c.RankedProgress)).ToList();
            return ordered.Select((c,i)=>{var e=Roster!=null&&c.driverId<Roster.Entries.Count?Roster.Entries[c.driverId]:null;
                return new RaceStanding{place=i+1,name=e?.Driver?.Name??"Driver "+c.driverId,paint=e?.Paint??Color.white,player=c==cars[0],finished=finishOrder.Contains(c),time=c.finishTime,entrant=e};}).ToList();
        }
        void PreviewNextCareerRace()
        {
            var data=AdventureSession.Store.Data;
            // A rematch with the Duke starts straight away (the race preview is the 24-car grid).
            if(Roster?.Event==RaceRoster.DukeEvent){AdventureSession.StartRace(RaceRoster.DukeChallenge(data));return;}
            var roster=Roster?.Event=="hillcrest"?RaceRoster.AdventureHillcrest(data):RaceRoster.Adventure(data);
            float resume=Time.timeScale;Time.timeScale=0;careerUI.Close();careerUI.SetVisible(false);
            if(racePreview==null)racePreview=gameObject.AddComponent<RacePreviewUI>();
            racePreview.Open(roster,()=>AdventureSession.StartRace(roster),()=>{Time.timeScale=resume;careerUI.SetVisible(true);ShowCareerResult();});
        }
        IEnumerator ReviewCareerRetry()
        {
            if(careerUI.DisplayedFinishTime!=AdventureUI.FinishTimeLabel(cars[0].finishTime))throw new Exception("Result time drifted after the player's finish.");
            Debug.Log("LITTLE_ROAD_RESULT_PRESENTATION_PASSED displayed="+careerUI.DisplayedFinishTime+" elapsed="+elapsed+" playerFinish="+cars[0].finishTime);
            int serial=AdventureSession.RaceSerial,wallet=AdventureSession.Store.Data.credits;
            if(!careerUI.InvokeChoice("Race again"))throw new Exception("Race again action is missing.");
            yield return null;
            if(!PreviewOpen||Time.timeScale!=0||AdventureSession.RaceSerial!=serial)throw new Exception("Retry must preview before committing another entry.");
            var proposed=racePreview.Roster;racePreview.Cancel();yield return null;
            if(PreviewOpen||Time.timeScale<=0||!careerUI.IsOpen||AdventureSession.Store.Data.credits!=wallet||AdventureSession.RaceSerial!=serial)throw new Exception("Cancelling retry changed the saved result or failed to restore it.");
            if(!careerUI.InvokeChoice("Race again"))throw new Exception("Retry action lost after cancel.");
            yield return null;
            for(int i=0;i<24;i++)if(!proposed.Entries[i].Driver.Planning.Same(in racePreview.Roster.Entries[i].Driver.Planning))throw new Exception("Cancelling retry rerolled opponents.");
            if(captureFolder!=null)racePreview.Capture(Path.Combine(captureFolder,"03b-retry-preview.png"));
            racePreview.Confirm();
            if(AdventureSession.RaceSerial!=serial+1||AdventureSession.Store.Data.credits!=wallet)throw new Exception("Retry did not commit exactly one new entry.");
            Debug.Log("LITTLE_ROAD_ADVENTURE_RETRY_PASSED cancel preserves reward/form; confirmation advances exactly one serial");
        }
        public void RecordImpact(RoadCar car,int other,float relativeSpeed,float penetration,string otherName)
        {
            impacts.Add(new ImpactSample{id=car.driverId,other=other,otherName=otherName,time=elapsed,relativeSpeed=relativeSpeed,penetration=penetration,progress=car.progress,offset=car.offset});
        }
        public void RecordCheckpoint(RoadCar car,float requiredProgress)
        {
            checkpointPassages.Add(new CheckpointSample{id=car.driverId,gate=car.nextGate,time=elapsed,requiredProgress=requiredProgress,actualProgress=car.progress});
            if(cars.Count>0&&car==cars[0]&&track!=null)playerTrace.Add(new Vector2(Mathf.Max(0,car.progress)/track.length,LivePlace(car)));
        }
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
                trackId=technical==null?reportTrack:technical.definition.id,trackVersion=technical==null?reportTrack+"-v1":technical.definition.version,
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
                    unsupportedSeconds=c.UnsupportedSeconds,physicalRecoverySeconds=c.physicalRecovery?.activeSeconds??0,recoveryWaitSeconds=c.physicalRecovery?.waitSeconds??0,recoveryReverseSeconds=c.physicalRecovery?.reverseSeconds??0,planReversals=c.contextPolicy?.PlanReversals??c.racerPolicy?.Reversals??0,passCommits=c.contextPolicy?.PassCommits??c.racerPolicy?.Attacks??0,passCompleted=c.contextPolicy?.PassCompleted??c.racerPolicy?.Passes??0,passAborted=c.contextPolicy?.PassAborted??c.racerPolicy?.Aborts??0,defendMoves=c.racerPolicy?.DefendMoves??0,brakeSeconds=c.BrakeSeconds,liftSeconds=c.LiftSeconds,stoppedSeconds=c.StoppedSeconds,trafficSeconds=c.racerPolicy?.TrafficSeconds??0,trafficBrakeSeconds=c.racerPolicy?.TrafficBrakeSeconds??0,guardSeconds=c.racerPolicy?.GuardSeconds??0,abortTarget=c.contextPolicy?.AbortTarget??c.racerPolicy?.AbortLost??0,abortTimeout=c.contextPolicy?.AbortTimeout??c.racerPolicy?.AbortTimeout??0,abortUnaccepted=c.contextPolicy?.AbortUnaccepted??c.racerPolicy?.AbortSideClosed??0,lateBrakeTicks=c.contextPolicy?.LateBrakeTicks??c.racerPolicy?.AbortNoOverlap??0,gapCandidates=c.contextPolicy?.DynamicRejoin?.GapCandidatesAdded??0,gapSelections=c.contextPolicy?.DynamicRejoin?.GapSelections??0}).ToArray(),motion=motion.ToArray(),draftingSamples=draftingSamples.ToArray(),impacts=impacts.ToArray(),assistedRecoveries=assistedRecoveries.ToArray(),checkpointPassages=checkpointPassages.ToArray()};
            if(reportPath!=null){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));File.WriteAllText(reportPath,JsonUtility.ToJson(r,true));DriverAudit?.Save(reportPath);MotionAudit?.Save(reportPath);}
            Debug.Log("LITTLE_ROAD_TRIAL_REPORT finished="+r.finished+"/"+r.carCount+" time="+r.simSeconds+" file="+reportPath);
        }
        void OnGUI()
        {
            if(cars.Count==0||freeRoam||career||racePreview?.IsOpen==true||Hud!=null||Has("--clean-view"))return;
            using var hud=new DisplaySettings.HudScale(true);
            title??=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold,normal={textColor=new Color(.96f,.95f,.84f)}};
            label??=new GUIStyle(GUI.skin.label){fontSize=20,normal={textColor=Color.white}};
            small??=new GUIStyle(GUI.skin.label){fontSize=15,normal={textColor=new Color(.79f,.88f,.88f)}};
            GUI.color=new Color(.07f,.16f,.19f,.9f);GUI.DrawTexture(new Rect(20,20,480,148),Texture2D.whiteTexture);GUI.color=Color.white;
            GUI.Label(new Rect(36,29,450,38),"LITTLE ROAD / "+(Roster?.Title??"COASTAL CIRCUIT"),title);
            int place=LivePlace(cars[0]);
            GUI.Label(new Rect(36,69,450,30),$"{Units.Show(Mathf.Abs(cars[0].speed))}     {place}/{cars.Count}     Lap {Mathf.Min(cars[0].laps+1,requiredLaps)}/{requiredLaps}",label);
            GUI.Label(new Rect(36,103,450,24),$"{setup.ToUpperInvariant()}  ·  {(cars[0].autonomous?"AI driving":"You are driving")}  ·  {Mathf.Max(0,elapsed):0.0}s",small);
            // Lap times (the max build hot laps, docs/54: how fast is the fastest car?).
            string Lap(float t)=>t>0&&t<float.MaxValue?$"{(int)(t/60)}:{t%60:00.00}":"--";
            GUI.Label(new Rect(36,130,450,24),$"This lap {Lap(cars[0].CurrentLapTime)}  ·  Last {Lap(cars[0].LastLapTime)}  ·  Best {Lap(cars[0].bestLap)}"+(technical!=null?"  ·  "+technical.Region(cars[0].pathDistance)+"  ·  C: overview":""),small);
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
