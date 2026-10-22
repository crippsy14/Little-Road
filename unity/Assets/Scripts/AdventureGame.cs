using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    public sealed class AdventureGame:MonoBehaviour
    {
        RaceTrial world;RoadCar player;Camera cameraView;AdventureUI ui;PartsBrowser browser;RacePreviewUI racePreview;TuningScreen tuning;TestDrive testDrive;
        readonly Dictionary<string,Vector3> residents=new();readonly Dictionary<string,float> residentYaw=new();string shop="bayview";
        readonly Dictionary<string,GameObject> finds=new();
        Minimap minimap;readonly List<Minimap.Mark> mapMarks=new();PauseMenu pause;bool wasPaused;
        // Free-roam HUD timing (docs/ui-design.md): the objective shows for 8 s after it changes and while
        // stopped; the controls hint for the first 15 s and after 3 s stopped.
        string lastGoal;float goalUntil,controlsUntil=-1,stoppedSince=-1;
        // Fog of war (ExploredMap): reveal around the car a few times a second, save now and then.
        float nextReveal,nextExploreSave;readonly HashSet<string> viewpointsSeen=new();const float RimReveal=1500;
        string nearest;float interactionReadyTime;ValleyLife.Folk nearFolk;NpcChatter nearChat;ZipLine.Station nearZip;ZipLine nearLine;bool riding;
        // Interaction prompts: a badge over each resident (discover), the bottom card when in range (act).
        readonly Dictionary<string,TalkBadge> badges=new();TalkBadge roamBadge;GameObject roamOwner;string objectiveResident;
        void TalkChatter(NpcChatter c)
        {
            c.LastSaid=Time.time;var pool=c.TalkLines;var picks=new List<string>();
            foreach(int i in new[]{UnityEngine.Random.Range(0,pool.Length),UnityEngine.Random.Range(0,pool.Length)})if(!picks.Contains(pool[i]))picks.Add(pool[i]);
            Show(c.speaker+" / "+(string.IsNullOrEmpty(c.title)?"Local":c.title),string.Join("\n\n",picks),new(){new("Back to the road",Close)});
        }
        void TalkFolk(ValleyLife.Folk f){var c=f.car!=null?f.car.GetComponent<NpcChatter>():null;if(c!=null)c.LastSaid=Time.time;ShowFolk(f);}
        void ShowFolk(ValleyLife.Folk f)=>Show(f.name+" / "+f.title,string.Join("\n\n",f.lines.Distinct()),new(){new("Back to the road",Close)});
        public static readonly Color[] Paints=PaintCatalog.Colors();
        PaintShop paintShop;BodyShop bodyShop;SpeechBubbles bubbles;
        AdventureStore Store=>AdventureSession.Store;
        bool reviewing,wasLookingBehind;Vector3 chaseLag; // forward camera trail, car-local
        IEnumerator Start()
        {
            ui=gameObject.AddComponent<AdventureUI>();browser=gameObject.AddComponent<PartsBrowser>();racePreview=gameObject.AddComponent<RacePreviewUI>();tuning=gameObject.AddComponent<TuningScreen>();testDrive=gameObject.AddComponent<TestDrive>();
            paintShop=gameObject.AddComponent<PaintShop>();bodyShop=gameObject.AddComponent<BodyShop>();bubbles=gameObject.AddComponent<SpeechBubbles>();
            // The title screen (save slots) once per launch, before the world builds.
            TitleScreen title=null;if(TitleScreen.Wanted){title=gameObject.AddComponent<TitleScreen>();yield return title.Run();}
            try{AdventureSession.Load();}catch(Exception e){ui.Show("Your save needs attention",e.Message+"\nThe existing save has been left intact.",new List<AdventureUI.Choice>{new("Quit",()=>Application.Quit())});yield break;}
            WorldLook.PlayPacing();Time.fixedDeltaTime=1f/120;
            LoadingScreen.Begin("Little Road");Mark("begin");
            // World tiles (W1): the woods near where the car will start are built now, the rest stream in around you.
            WorldTiles.Plan(AdventureSession.ReturnSpot??(!Testing&&Store.Data.spot!=null&&Store.Data.spot.Length==4?new Vector3(Store.Data.spot[0],Store.Data.spot[1],Store.Data.spot[2]):NewBayview.On?NewBayview.Spawn:new Vector3(-63,0,-94)));
            RaceArea.Clear();world=GetComponent<RaceTrial>();world.freeRoam=true;world.track=new Circuit();world.elapsed=1;yield return TrialWorld.CreateRoutine(world.track,false);Mark("world");
            // (--prop-forge: the streets dressed once everything is built, the village and the towns included; docs/78)
            if(PropForge.Enabled&&CoastalLandscape.WorldRoot!=null){Physics.SyncTransforms();PropForgeDressing.Build(CoastalLandscape.WorldRoot);Mark("dressing");BankBarriers.Build(CoastalLandscape.WorldRoot,world.track);Mark("bank-barriers");Debug.Log("LITTLE_ROAD_GROUND_FIT "+GroundFit.Summary);}
            // (--world-fingerprint: the built world, every streamed tile included, before the town's life and cars are added.)
            if(RaceTrial.Has("--world-fingerprint")){WorldTiles.Complete();yield return null;Debug.Log("LITTLE_ROAD_WORLD_FINGERPRINT "+CoastalLandscape.Fingerprint());}   // (a frame: deferred destroys land)
            // (Tests and audits see the whole world at once; --world-tiles-test watches it stream.)
            if(Testing&&!RaceTrial.Has("--world-tiles-test"))WorldTiles.Complete();
            Debug.Log($"LITTLE_ROAD_WORLD_TILES ready deferred={WorldTiles.Deferred} pending={WorldTiles.Pending} spawned={BayviewKit.Spawned}");
            // (--list-near x,z,r: every kit piece within r metres, by name and place: to identify what a snap shows)
            if(RaceTrial.Arg("--list-near") is string near&&near.Contains(",")){var a=near.Split(',');float F(int i)=>float.Parse(a[i],System.Globalization.CultureInfo.InvariantCulture);var c=new Vector2(F(0),F(1));
                foreach(var go in BayviewKit.Placed)if(go!=null){var p=go.transform.position;if(Vector2.Distance(new(p.x,p.z),c)<F(2))Debug.Log($"LITTLE_ROAD_NEAR {go.name} {p} yaw {go.transform.eulerAngles.y:0} parent {(go.transform.parent!=null?go.transform.parent.name:"-")}");}}
            // (--list-renderers-near x,z,r: every renderer whose bounds come within r metres, kit or not, with its collider and top:
            // to identify an unknown surface in a snap)
            if(RaceTrial.Arg("--list-renderers-near") is string rn&&rn.Contains(",")){var a=rn.Split(',');float G(int i)=>float.Parse(a[i],System.Globalization.CultureInfo.InvariantCulture);var c=new Vector3(G(0),0,G(1));
                foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){var b=r.bounds;var q=b.ClosestPoint(new Vector3(c.x,b.center.y,c.z));if(Vector2.Distance(new(q.x,q.z),new(c.x,c.z))<G(2)&&b.size.magnitude<400)
                    Debug.Log($"LITTLE_ROAD_RNEAR {r.name} parent {(r.transform.parent!=null?r.transform.parent.name:"-")} bounds {b.min}..{b.max} collider {(r.GetComponent<Collider>()!=null?r.GetComponent<Collider>().GetType().Name:"none")} mat {(r.sharedMaterial!=null?r.sharedMaterial.name:"-")}");}}
            residents["garage"]=new Vector3(-62,0,-100);
            // Pit Stop bay hosts (Bayview: garage + parts). Hillcrest's arrive through HillcrestTown.Spots.
            foreach(var (id,key) in new[]{("garage","bayview/garage"),("parts","bayview/parts")})if(PitStop.Hosts.TryGetValue(key,out var host)){residents[id]=host.at;residentYaw[id]=host.yaw;}
            residents["cafe"]=NewBayview.On?NewBayview.Cafe:new Vector3(-40.5f,0,-101.8f);residents["paint"]=NewBayview.On?NewBayview.Paint:new Vector3(3.5f,0,-102.2f);
            if(NewBayview.On){residentYaw["cafe"]=NewBayview.CafeYaw;residentYaw["paint"]=NewBayview.PaintYaw;}
            if(HillcrestTown.Spots.Count>0){residents["bayhalt"]=CoastalLandscape.HaltResident;residentYaw["bayhalt"]=270;}
            foreach(var id in new[]{"motors","hcparts","hcbody","hillcafe","raceway","station","castle","vista","lookout","farm"})if(HillcrestTown.Spots.TryGetValue(id,out var spot)){residents[id]=spot.at;residentYaw[id]=spot.yaw;}
            // Bramblewick (--bramblewick, docs/82): Jed the tuner, Maggie at The Plough, the vicar on the green, Peg at the dairy.
            foreach(var (id,at) in new[]{("bwtuner","tuner"),("bwpub","pub"),("bwvicar","church"),("bwdairy","dairy"),("bwmarshal","marshal")})if(BramblewickVillage.Spots.TryGetValue(at,out var bw)){residents[id]=bw.at;residentYaw[id]=bw.yaw;}
            // Vinelands (--vinelands, docs/86): the priest at the Matriz, the clerk at the town hall, the cafe, the baker, the washing tanks.
            foreach(var (id,at) in new[]{("vlpriest","priest"),("vlclerk","clerk"),("vlcafe","cafe"),("vlbaker","baker"),("vlfonte","fonte"),("vlgarage","garage"),("vlparts","parts"),("vlbody","body"),("vlbus","bus"),("vlfruta","fruta"),("vlbarco","barqueiro"),("vllodge","lodge"),("vltasca","tasca"),("vlpombal","pombal"),("vllajes","lajes"),("vlfojo","fojo"),("vlcasal","casal"),("vlmirante","mirante"),("vlbelvedere","belvedere"),("vlveiga","veiga"),("vlbombeiros","bombeiros"),("vlescola","escola"),("vladega","adega"),("vlclube","clube"),("vlaldeia","aldeia"),("vlcorvo","corvo")})if(VinelandsVila.Spots.TryGetValue(at,out var vl)){residents[id]=vl.at;residentYaw[id]=vl.yaw;}
            // Quarry Hills built in the background (Districts): what needs it is set up when it arrives.
            quarryLater=Districts.Waiting("quarry-hills");if(quarryLater)Districts.WhenBuiltSteps("quarry-hills",QuarryHillsArrived);
            if(!quarryLater)QuarryHillsResidents();
            residents["post"]=NewBayview.On?NewBayview.PostSpot(world.track):world.track.At(world.track.length*.46f)-world.track.Right(world.track.length*.46f)*16;
            CreatePlaces();Mark("places");BuildWayfinding();Mark("wayfinding");
            if(AdventureSession.MaxBuildTest&&!AdventureSession.TestPlaced){AdventureSession.TestPlaced=true;FaceDuke();}
            var car=TrialWorld.Car(Store.Data.bodyId,Paints[Store.Data.paintIndex]);car.transform.SetPositionAndRotation(NewBayview.On?NewBayview.Spawn:new Vector3(-63,.15f,-94),Quaternion.Euler(0,90,0));
            if(AdventureSession.ReturnSpot is Vector3 back)car.transform.SetPositionAndRotation(back+Vector3.up*.15f,Quaternion.Euler(0,AdventureSession.ReturnYaw,0));
            // Otherwise carry on from where you were (Richard, 3 Oct): the saved spot, if it's still clear ground in this world.
            else if(!Testing&&Store.Data.spot!=null&&Store.Data.spot.Length==4){var s=Store.Data.spot;var at=new Vector3(s[0],s[1],s[2]);
                if(ClearSpot(ref at)){car.transform.SetPositionAndRotation(at+Vector3.up*.2f,Quaternion.Euler(0,s[3],0));resumed=true;}else Debug.Log("LITTLE_ROAD_RESUME refused "+at);}
            player=car.AddComponent<RoadCar>();player.Setup(world,0,EquipmentCatalog.Stats(Store.Data.Loadout(),Store.Data.Tune()),false,1709,"owned equipment");world.cars.Add(player);world.running=true;FrameProbe.Attach(player.transform);Grazer.Car=player.transform;
            if(QuarryHills.Enabled&&QuarryHillsTown.Spots.Count>0&&!quarryLater)QuarryFestival.Build(transform,Store.Data,player);CameraShake.Reset();CameraShake.Watch(player);
            playerHorn=Horn.Attach(car,Horn.PitchFor(Store.Data.bodyId),player:true);
            player.Bumped+=(other,speed)=>{if(speed>1f&&!ui.IsOpen&&!paintShop.IsOpen&&!bodyShop.IsOpen)other.GetComponentInParent<NpcChatter>()?.TrySay(bubbles);};
            // (Bayview's loop traffic: on in the game since 20 Oct, Richard; not in test runs, which stay as they were; --no-ambient-traffic)
            if(RaceTrial.Has("--ambient-traffic")||!Testing&&!RaceTrial.Has("--no-ambient-traffic"))AmbientTraffic();
            Mark("player");if(!RaceTrial.Has("--no-valley-life"))gameObject.AddComponent<ValleyLife>().Setup(player);Mark("valley-life");
            // Fireworks over Bayview harbour at night (docs/51).
            var fireworks=gameObject.AddComponent<Fireworks>();fireworks.Setup();fireworks.Announce=Notify;
            // Speed traps on seven roads (docs/51).
            activityHud=gameObject.AddComponent<ActivityHud>();activityHud.visible=()=>ui.Visible&&!reviewing;
            gameObject.AddComponent<ToyCountdown>().Setup(()=>activity!=null?activity.Elapsed:9f,()=>activity==null||(pause!=null&&pause.IsOpen)||!ui.Visible);
            farm=gameObject.AddComponent<FarmChallenges>();farm.Finished+=FarmFinished;
            if(TipLeap.Ready&&!quarryLater){leap=gameObject.AddComponent<TipLeap>();leap.Setup(player,()=>Store,Notify,(title,detail)=>ui.Card(title,detail,Toy.Orange,"TIP LEAP"));}
            if(BlastRun.Ready&&!quarryLater){blast=gameObject.AddComponent<BlastRun>();blast.Crossed+=BlastCrossed;blast.Shot+=BlastShot;}
            trainRace=gameObject.AddComponent<TrainRace>();trainRace.Setup(player,()=>Store,Notify,(title,detail)=>ui.Card(title,detail,Toy.Red,"BEAT THE TRAIN"));
            // (After the player's car exists.) The Duke's drive (DukeDrive, docs/51) is parked behind --duke-drive (Richard, 2 Oct:
            // the town traffic isn't built for a car at pace; it returns with a traffic overhaul). He stays at the castle.
            var duke=GameObject.Find("Resident castle");
            if(duke!=null&&player!=null&&(RaceTrial.Has("--duke-drive")||RaceTrial.Has("--duke-drive-review")))duke.AddComponent<DukeDrive>().Setup(player,()=>climbStart<0&&(trainRace==null||!trainRace.Active)&&!reviewing,Notify,()=>Store,(title,detail)=>ui.Card(title,detail,Toy.Pink,"THE DUKE'S DRIVE"));
            gameObject.AddComponent<SpeedTraps>().Setup(player,world.track,()=>Store,(title,detail)=>ui.Card(title,detail,Toy.Orange,"SPEED TRAP"));
            if(RaceTrial.Has("--drive-trace"))gameObject.AddComponent<DriveTrace>().Setup(player,RaceTrial.Arg("--drive-trace"));
            if(RaceTrial.Has("--drive-replay"))gameObject.AddComponent<DriveReplay>().Setup(player,RaceTrial.Arg("--drive-replay"));
            // Speculative contacts snag internal road-mesh edges while the car yaws: a one-step
            // sideways knock and ~5 m/s speed loss mid-corner. Free roam sweeps instead (races
            // keep their validated mode). --free-roam-speculative restores it for comparison.
            player.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            cameraView=new GameObject("Adventure camera").AddComponent<Camera>();cameraView.tag="MainCamera";cameraView.fieldOfView=55;cameraView.nearClipPlane=.15f;cameraView.farClipPlane=6000;
            if(title!=null)title.Dismiss();cameraView.gameObject.AddComponent<AudioListener>();WorldLook.Camera(cameraView);
            photo=gameObject.AddComponent<PhotoMode>();photo.Setup(cameraView,player.transform,ui,()=>AreaBanner.Area(player.transform.position),Notify,show=>ui.SetHudVisible(show));
            // Pip's photo requests (docs/51): each sight pays once, the first photo that shows it.
            photo.Photographed=cam=>{foreach(var r in PhotoRequests.Seen(cam))Attempt(()=>{if(Store.ClaimTrial(r.id,PhotoRequests.Prize)){int got=0;foreach(var q in PhotoRequests.All)if(Store.Data.collected.Contains(q.id))got++;
                ui.Card($"PHOTO FOR PIP: {r.name.ToUpperInvariant()}",$"{got} of {PhotoRequests.All.Count} requests  ·  +{PhotoRequests.Prize} credits",Toy.Pink,"POSTCARD PHOTO");}});};
            if(GrassField.Wanted)gameObject.AddComponent<GrassField>().Setup(cameraView);
            CameraPose(true);if(!string.IsNullOrEmpty(AdventureSession.Notice)){Notify(AdventureSession.Notice);AdventureSession.Notice=null;}
            else if(Store.Data.questStage==0)Notify("Welcome to Bayview · Mara has a parcel for the lighthouse");
            Mark("camera");yield return WarmUp();CameraPose(true);Mark("warm-up");
            // Free-roam map: the world baked from above once, behind the loading card.
            ExploredMap.Load(Store.Data.explored);
            // Only where you've been (Richard: no free reveals; viewpoints will fill empty country later).
            ExploredMap.Reveal(player.transform.position,ExploredMap.DriveRadius);ExploredMap.Flush();
            var banner=GetComponent<AreaBanner>();
            if(banner!=null){if(!Store.Data.discovered.Contains(banner.Current))Attempt(()=>Store.Discover(banner.Current));   // where you start is known already
                banner.known=place=>Store.Data.discovered.Contains(place);
                banner.discovered=(place,subtitle)=>Attempt(()=>{if(Store.Discover(place))ui.Discovery(place,subtitle);});
}
            Mark("explored");RenderTexture mapTexture=null;
            if(!Application.isBatchMode&&!RaceTrial.Has("--no-minimap")){mapTexture=WorldMap.Bake();minimap=gameObject.AddComponent<Minimap>();minimap.Setup(player,mapTexture,MapMarks,()=>WorldMap.Labels(ResidentDirectory(),world.track));}
            GraphicsOptions.Changed+=OnOptionChanged;
            pause=gameObject.AddComponent<PauseMenu>();pause.Setup(new PauseMenu.Context{save=()=>Store.Data,area=()=>AreaBanner.Area(player.transform.position),objective=()=>GoalText(false),car=player.transform,
                minimap=minimap,marks=MapMarks,map=mapTexture,settings=SettingsItems,quit=Application.Quit,car_=CarSummary,warpLabel=WarpLabel,warp=WarpToGarage,
                activity=()=>activity==null?null:(activity.title,()=>{pause.Close();RestartActivity();},()=>{pause.Close();ExitActivity();})});
            Mark("map-pause");Districts.Start();   // (a district left for later starts building now: Districts)
            Debug.Log($"LITTLE_ROAD_SCENE_SWITCH ready t={Time.realtimeSinceStartup:0.00} mode=town world={(PersistentWorld.Current!=null&&PersistentWorld.Current.Reuses>0?"reused":"built")}");CarContact.SeatAll(); // every standing car onto its four wheels (docs/57, 2.5)
            Debug.Log("LITTLE_ROAD_ADVENTURE_READY save="+Store.path+" quest="+Store.Data.questStage+" credits="+Store.Data.credits+" "+RaceTrial.LoadTime);LoadingScreen.Ready();
            if(RaceTrial.Has("--town-print")){yield return TownPrint();yield break;}
            // --load-timing: measure the load (LITTLE_ROAD_LOAD_STEP lines) and quit (W1 world tiles).
            if(RaceTrial.Has("--load-timing")){yield return new WaitForSeconds(2);Application.Quit(0);yield break;}
            if(RaceTrial.Has("--minimap-review")){reviewing=true;yield return MinimapReview();yield break;}
            if(RaceTrial.Has("--pause-review")){yield return PauseReview();yield break;}
            if(RaceTrial.Has("--zip-ride-review")){yield return ZipRideReview();yield break;}
            if(RaceTrial.Has("--stop-look-review")){yield return StopLookReview();yield break;}
            // --photo-review: photo mode's five looks (docs/51) to --trial-captures, framed and unframed.
            if(RaceTrial.Has("--photo-review")){reviewing=true;var main=HillcrestRoute.Mainline;float s=main.Nearest(new Vector3(95,0,-300),out _);player.body.rotation=Quaternion.LookRotation(-main.Forward(s));Place(main.At(s));
                yield return new WaitForSeconds(1.5f);CameraPose(true);cameraView.transform.position=player.transform.position+player.transform.right*5.5f+player.transform.forward*4+Vector3.up*1.6f;yield return null;photo.Open();string folder=RaceTrial.Arg("--trial-captures");
                for(int l=0;l<5;l++)yield return photo.Review(Path.Combine(folder,$"photo-{l}.png"),l,l!=1);photo.Close();Application.Quit(0);yield break;}
            if(RaceTrial.Has("--contact-review")){reviewing=true;yield return ContactReview.Run(RaceTrial.Arg("--trial-captures"));Application.Quit(0);yield break;}
            if(RaceTrial.Has("--climb-review")){yield return ClimbReview();yield break;}
            if(RaceTrial.Has("--duke-drive-review")){yield return DukeDriveReview();yield break;}
            if(RaceTrial.Has("--world-review")){RefreshHUD();reviewing=true;player.controlsBlocked=true;gameObject.AddComponent<VillageReview>();yield break;}
            if(RaceTrial.Has("--vehicle-lights-review")){reviewing=true;gameObject.AddComponent<VehicleLightReview>();yield break;}
            if(RaceTrial.Has("--turn-review")){gameObject.AddComponent<TurnReview>();yield break;}
            if(RaceTrial.Has("--valley-tour")){reviewing=true;gameObject.AddComponent<ValleyTour>().Setup(player,cameraView,ui);yield break;}
            if(RaceTrial.Has("--paint-shop-review")){reviewing=true;yield return PaintShopReview();yield break;}
            if(RaceTrial.Has("--body-shop-review")){reviewing=true;yield return BodyShopReview();yield break;}
            if(RaceTrial.Has("--tuner-garage-review")){reviewing=true;yield return TunerGarageReview();yield break;}
            // Opt-in: enter the Valley Cup straight away (with --trial-auto the player's car drives itself).
            if(RaceTrial.Has("--duke-race-review")&&!AdventureSession.Racing&&Store.Data.settledSerial==Store.Data.raceSerial){yield return new WaitForSeconds(.5f);StartDukeRace(RaceRoster.DukeChallenge(Store.Data));yield break;}
            if(RaceTrial.Has("--quarry-race-review")&&!AdventureSession.Racing&&Store.Data.settledSerial==Store.Data.raceSerial&&QuarryHills.Enabled){yield return new WaitForSeconds(.5f);PreviewRace("quarry");yield return new WaitForSeconds(.5f);racePreview.Confirm();yield break;}
            // Any calendar event by id (docs/67: the class B races), entered from its preview with the player's car on autopilot.
            // (--review-retire: a retired race never settles its serial, so round trips that retire don't wait for it.)
            if(RaceTrial.Has("--event-race-review")&&!AdventureSession.Racing&&(Store.Data.settledSerial==Store.Data.raceSerial||RaceTrial.Has("--review-retire"))){string ev=RaceTrial.Arg("--event-race-review","bayview");
                // --review-races N: N round trips (town, race, town...), then quit (docs/72: scene-switch timing).
                if(RaceTrial.Has("--review-races")){reviewRaces++;if(reviewRaces>int.Parse(RaceTrial.Arg("--review-races","1"))){Debug.Log("LITTLE_ROAD_REVIEW_RACES done");Application.Quit();yield break;}}
                // --claim-tuner <id>: race in the free Tuner (docs/67), the ground truth for class B balance.
                string tuner=RaceTrial.Arg("--claim-tuner");
                if(tuner!=null)Store.Change(p=>{foreach(var e in RaceCalendar.All)if(e.raceClass=="C"&&!p.podiums.Contains(e.id))p.podiums.Add(e.id);return true;});
                if(tuner!=null&&Store.ClaimTunerStarter(tuner))player.ReplaceBody(tuner,Paints[Store.Data.paintIndex]);
                yield return new WaitForSeconds(.5f);PreviewRace(ev);yield return new WaitForSeconds(.5f);racePreview.Confirm();yield break;}
            if(RaceTrial.Has("--valley-race-review")&&!AdventureSession.Racing&&Store.Data.settledSerial==Store.Data.raceSerial){yield return new WaitForSeconds(.5f);PreviewRace("hillcrest");yield return new WaitForSeconds(.5f);racePreview.Confirm();yield break;}
            if(RaceTrial.Has("--parts-browser-review")){OpenParts("bayview",PartsBrowser.Mode.Shop,Sid);gameObject.AddComponent<PartsBrowserReview>().Setup(browser,Store,player,ui,m=>OpenParts("bayview",m,m==PartsBrowser.Mode.Shop?Sid:Mara));yield break;}
            if(RaceTrial.Has("--sound-bench"))EngineBench.Show(RaceTrial.Arg("--sound-bench"));
            // Review: the tuning screen, captured (docs/48).
            if(RaceTrial.Has("--tuning-review")){reviewing=true;OpenTuning("bayview",Mara);yield return new WaitForSeconds(1.5f);
                string shot=RaceTrial.Arg("--tuning-review");ui.Capture(shot);yield return new WaitForSeconds(1);Debug.Log("LITTLE_ROAD_TUNING_REVIEW "+shot);Application.Quit();yield break;}
            // Test (--quarry-hills --sprint-test): talk to Gwen, start the Quarry Sprint, carry the car round the loop at 22 m/s and
            // check the lap is timed and the prize claimed (the result goes to the log, then the game quits).
            if(RaceTrial.Has("--sprint-test")&&residents.ContainsKey("rallyschool")){StartCoroutine(SprintTest());yield break;}
            // Test (--quarry-hills --blast-test): talk to Dai, start the Blast Run, carry the car up the course a fifth faster
            // than Dai, then wait for the shot. Passes if it cleared, beat Dai, paid once and the shot fired.
            if(RaceTrial.Has("--blast-test")&&residents.ContainsKey("blast")){StartCoroutine(BlastTest());yield break;}
            // Test (--quarry-hills --leap-test): the car at the top of the run-up at 32 m/s (about 72 mph), physics from
            // there; passes if it takes off and lands past the first star.
            if(RaceTrial.Has("--leap-test")&&leap!=null){StartCoroutine(LeapTest());yield break;}
            // Tests (doc 64): --harvest-test and --apples-test talk to Giles, start the challenge and drive it with a
            // velocity autopilot (rows across the wheat; down each orchard row); --activity-hud-review photographs the
            // intro card, the corner tracker and the pause menu's challenge controls, then exits the challenge.
            if(RaceTrial.Has("--harvest-test")&&residents.ContainsKey("farm")){StartCoroutine(FarmTest(FarmChallenges.Kind.Harvest));yield break;}
            if(RaceTrial.Has("--crop-test")){StartCoroutine(CropTest());yield break;}
            if(RaceTrial.Has("--apples-test")&&residents.ContainsKey("farm")){StartCoroutine(FarmTest(FarmChallenges.Kind.Apples));yield break;}
            if(RaceTrial.Has("--activity-hud-review")&&residents.ContainsKey("farm")){StartCoroutine(ActivityHudReview());yield break;}
            if(RaceTrial.Has("--carving-test")&&QuarryHillsTown.Spots.Count>0){StartCoroutine(CarvingTest());yield break;}
            if(RaceTrial.Has("--horseshoe-test")&&BramblewickVillage.Horseshoes.Count>0){StartCoroutine(KeepsakeTest("HORSESHOE",Collectibles.Horseshoes,Collectibles.Horseshoe,MapRegions.HorseshoePlace,Collectibles.IsHorseshoe));yield break;}
            if(RaceTrial.Has("--wayfinding-test")){reviewing=true;StartCoroutine(WayfindingTest());yield break;}
            if(RaceTrial.Has("--world-tiles-test")){StartCoroutine(WorldTilesTest());yield break;}
            if(RaceTrial.Has("--overtake-test")){StartCoroutine(OvertakeTest());yield break;}
            if(RaceTrial.Has("--festival-test")&&QuarryHillsTown.Spots.Count>0){reviewing=true;StartCoroutine(FestivalTest());yield break;}
            if(RaceTrial.Has("--blondin-test")){StartCoroutine(BlondinTest());yield break;}
            if(RaceTrial.Has("--climb-test")){StartCoroutine(ClimbTest());yield break;}
            if(RaceTrial.Has("--lesson-test")&&residents.ContainsKey("rallyschool")){StartCoroutine(LessonTest());yield break;}
            yield return null;if(AdventureSession.Smoke)StartCoroutine(Smoke());
        }
        // ---------------------------------------------------------------- notice boards, pins and fingerposts (Wayfinding, docs/60)
        Wayfinding.Board nearBoard;Vector3? pin;string pinLabel;
        void BuildWayfinding()
        {
            Wayfinding.Clear();var holder=new GameObject("Wayfinding").transform;
            // (Bayview's loop isn't tagged as road: measured from its centreline, 8 m to the edge.)
            Wayfinding.PlaceBoard(holder,"Bayview",residents["cafe"],p=>{float d=world.track.Nearest(new Vector3(p.x,0,p.z),out _);var on=world.track.At(d);var way=new Vector3(on.x-p.x,0,on.z-p.z);
                return (way.magnitude-8,way.normalized);});
            if(HillcrestTown.Spots.Count==0)return;
            Wayfinding.PlaceBoard(holder,"Hillcrest",HillcrestRoute.Square);
            var places=new List<(string,Vector3)>{("Bayview",HillcrestRoute.Mainline.line[0]),("Hillcrest",HillcrestRoute.Square),("Hillcrest Castle",HillcrestRoute.CastleCourt),("Raceway",HillcrestRoute.Paddock),
                ("Silver Falls",HillcrestRoute.Lookout),("Golden Acre Farm",HillcrestRoute.Farm(0,0)),("Valley Viewpoint",HillcrestRoute.Vista)};
            if(residents.TryGetValue("station",out var station))places.Add(("Station",station));
            wayHolder=holder;wayPlaces=places;
            if(QuarryHills.Enabled) {
                var square=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);if(!quarryLater)Wayfinding.PlaceBoard(holder,"Quarry Hills",square);places.Add(("Quarry Hills",square));
                // (Quarry Hills still building: the quay and the school by where they'll be, for the signs elsewhere.)
                if(residents.TryGetValue("slatequay",out var quay))places.Add(("Slate Quay",quay));else if(quarryLater)places.Add(("Slate Quay",new Vector3(QuarryHills.QuayCentre.x,0,QuarryHills.QuayCentre.y)));
                if(residents.TryGetValue("rallyschool",out var school))places.Add(("Rally School",school));else if(quarryLater)places.Add(("Rally School",QuarryFloor.School));
                places.Add(("Kettle Falls",HillcrestRoute.Find(QuarryHills.KettleRoadName).line[^1]));
            }
            // (Quarry Hills' own junctions get their fingerposts once its buildings are there: QuarryHillsWayfinding.)
            Wayfinding.Fingerposts(holder,CoastalLandscape.Network,places,quarryLater?p=>!CoastalLandscape.NearQuarryHills(p):null);
        }
        Transform wayHolder;List<(string,Vector3)> wayPlaces;
        void QuarryHillsWayfinding()
        {
            if(wayHolder==null||!QuarryHills.Enabled)return;
            var square=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);Wayfinding.PlaceBoard(wayHolder,"Quarry Hills",square);
            var places=new List<(string,Vector3)>();
            foreach(var (name,at) in wayPlaces) {
                if(name=="Slate Quay"&&residents.TryGetValue("slatequay",out var q))places.Add((name,q));
                else if(name=="Rally School"&&residents.TryGetValue("rallyschool",out var s))places.Add((name,s));
                else places.Add((name,at));
            }
            Wayfinding.Fingerposts(wayHolder,CoastalLandscape.Network,places,CoastalLandscape.NearQuarryHills);
        }
        static Wayfinding.Board NearestBoard(Vector3 p){Wayfinding.Board best=null;float d=float.MaxValue;foreach(var b in Wayfinding.Boards){float e=Vector3.Distance(p,b.at);if(e<d){d=e;best=b;}}return best;}
        // What's on in the board's town: each line pins its place (a null label can't be pinned).
        List<(string text,Vector3 at,string label)> BoardEntries(Wayfinding.Board b)
        {
            var e=new List<(string text,Vector3 at,string label)>();var d=Store.Data;
            string Podium(string id)=>d.podiums!=null&&d.podiums.Contains(id)?"podium won":"no podium yet";
            void Resident(string id,string text,string label){if(residents.TryGetValue(id,out var at))e.Add((text,at,label));}
            void Keepsakes(Func<string,bool> kind,int total,string plural,string one)
            {
                int got=0;foreach(string c in d.collected)if(kind(c))got++;
                string best=null;float bd=float.MaxValue;foreach(var f in finds)if(kind(f.Key)&&f.Value!=null){float dd=Vector3.Distance(f.Value.transform.position,b.at);if(dd<bd){bd=dd;best=f.Key;}}
                if(best==null){e.Add(($"{plural}: all {total} found",b.at,null));return;}
                string hint=MapRegions.Keepsakes.TryGetValue(best,out var k)?k.place:b.town;
                e.Add(($"{plural}: {got} of {total} · someone saw one near {hint}",finds[best].transform.position,$"a {one} near {hint}"));
            }
            void Traps()
            {
                int n=0,three=0;SpeedTraps.Trap next=null;float nd=float.MaxValue;
                foreach(var t in SpeedTraps.All){if(NearestBoard(t.at)!=b)continue;n++;if((SpeedTraps.Record(d,t.id)?.stars??0)>=3)three++;else{float dd=Vector3.Distance(t.at,b.at);if(dd<nd){nd=dd;next=t;}}}
                if(n>0)e.Add(next==null?($"Speed traps: all {n} at three stars",b.at,null):($"Speed traps: {three} of {n} at three stars · next: {next.name}",next.at,$"the {next.name} speed trap"));
            }
            void Zip(){foreach(var line in ZipLine.All)if(NearestBoard(line.bottom.bay)==b)e.Add(($"{line.title} · ride it from either station",line.bottom.bay,line.title));}
            if(b.town=="Bayview") {
                if(d.questStage<3)Resident(d.questStage==1?"post":"garage",d.questStage==1?"Mara's parcel · deliver it to Pip at the lighthouse":"Mara at the Pit Stop has a job for you",d.questStage==1?"Pip at the lighthouse":"Mara at the Pit Stop");
                Resident("cafe",$"Coast Run race with Rowan · {Podium("bayview")}","Rowan at the café");
                Resident("bayhalt","Bayview Halt · trains to the valley","Bayview Halt");
                Resident("paint","Dot's paints · a new colour for your car","Dot's paints");
                Keepsakes(Collectibles.IsPostcard,Collectibles.Postcards,"Postcards","postcard");
            }
            else if(b.town=="Hillcrest") {
                Resident("raceway",$"Valley Cup at the raceway with Vito · {Podium("hillcrest")}","Vito at the raceway");
                Resident("castle","Castle Climb with the Duke · "+(d.collected.Contains(Collectibles.CastleTrial)?"par beaten":$"par {NormalPar:0} s"),"the Duke at the castle");
                Resident("station","Beat the Train · ask Stella at the station","Stella at the station");
                Resident("vista","Wren's photo requests at the viewpoint","Wren at the viewpoint");
                Keepsakes(Collectibles.IsStamp,Collectibles.Stamps,"Hillcrest stamps","stamp");
            }
            else {
                if(QuarryFestival.Festival)e.Add(("Rally Day! The parade's on the High Street",QuarryFestival.ParadeAt,"the Rally Day parade"));
                if(QuarryFestival.ChoirHours&&QuarryHillsTown.Spots.TryGetValue("chapel",out var chapel))e.Add(("Choir night at Capel Bethel · all welcome",chapel.at,"Capel Bethel"));
                Resident("rallyschool",$"Quarry Hills Rallycross with Gwen · {Podium("quarry")}","Gwen at the rally school");
                int golds=0;for(int i=0;i<RallySchool.Lessons.Length;i++){var r=RallySchool.Record(d,i);if(r!=null&&r.stars==3)golds++;}
                Resident("rallyschool",$"Rally lessons · gold in {golds} of {RallySchool.Lessons.Length}","the rally school");
                Resident("rallyschool","Quarry Sprint · "+(d.collected.Contains(Collectibles.QuarrySprint)?"par beaten":$"par {SprintPar:0} s"),"the Quarry Sprint");
                var climb=d.traps?.Find(x=>x.id==GalleryClimb.Id);
                e.Add(("Gallery Climb · "+(climb!=null?$"best {climb.best:0.0} s":"not tried yet")+$" · par {GalleryClimb.Par:0} s",GalleryClimb.Start,"the Gallery Climb start"));
                Resident("slatequay","Slate Quay · trains to the valley","Slate Quay");
                if(TipLeap.Ready){var tl=SpeedTraps.Record(d,TipLeap.Id);e.Add(("Tip Leap · "+(tl!=null?$"best {tl.best:0} m · {tl.stars}/3 stars":"take the Blondin up the big tip, then jump"),TipLeap.RunUp,"the Tip Leap"));}
                if(residents.ContainsKey("blast")){var br=d.traps?.Find(x=>x.id==BlastRun.Id);Resident("blast","Blast Run with Dai · "+(d.collected.Contains(Collectibles.BlastRunWin)?$"Dai beaten · best {br?.best:0.0} s":br!=null?$"best {br.best:0.0} s · Dai's {BlastRun.DaiTime:0} s":"beat the shot, and Dai"),"Dai on the quarry floor");}
                Keepsakes(Collectibles.IsCarving,Collectibles.Carvings,"Slate carvings","slate carving");
            }
            Traps();Zip();return e;
        }
        void ReadBoard(Wayfinding.Board b)
        {
            var choices=new List<AdventureUI.Choice>();
            foreach(var (text,at,label) in BoardEntries(b)){var where=at;var name=label;choices.Add(new(text,()=>{pin=where;pinLabel=name;Close();Notify("Pinned on your map: "+name);},label!=null));}
            if(pin!=null)choices.Add(new("Clear the pin · "+pinLabel,()=>{pin=null;Close();}));
            choices.Add(new("Back to the road",Close));
            Show($"Notice board / {b.town}",$"What's on in {b.town}. Pick one to pin it on your map: the objective line follows it, and the pin clears when you get there.",choices);
        }
        // Test (--quarry-hills --festival --festival-test): Rally Day in the square and on the High Street by day, then choir night at
        // the chapel at half past seven. Pass: flags, the banner's parade and the choir all there, the parade moving.
        IEnumerator FestivalTest()
        {
            yield return new WaitForSeconds(1.5f);var sq=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);
            Place(sq+new Vector3(30,0,-30));
            var parade=new List<Transform>();foreach(var c in RoadCruiser.All)if(c.name.StartsWith("Rally Day parade"))parade.Add(c.transform);
            var before=parade.ConvertAll(t=>t.position);
            cameraView.transform.position=sq+new Vector3(46,34,-46);cameraView.transform.LookAt(sq+Vector3.up*2);WorldLook.Cut(cameraView);yield return new WaitForSeconds(1.2f);Capture("festival-square");
            var high=HillcrestRoute.Find(QuarryHills.HighStreetName);var at=high.At(Mathf.Min(45,high.Length*.3f));
            cameraView.transform.position=at-high.Right(Mathf.Min(45,high.Length*.3f))*12+Vector3.up*5+(at-sq).normalized*-14;cameraView.transform.LookAt(at+Vector3.up*3);WorldLook.Cut(cameraView);yield return new WaitForSeconds(2f);Capture("festival-banner");
            float moved=0;for(int i=0;i<parade.Count;i++)moved=Mathf.Max(moved,Vector3.Distance(parade[i].position,before[i]));
            TimeOfDay.Hour=19.5f;TimeOfDay.Apply();yield return new WaitForSeconds(.5f);
            int choir=0;foreach(var c in NpcChatter.All)if(c!=null&&c.gameObject.activeInHierarchy&&c.title=="Capel Bethel choir")choir++;
            if(QuarryHillsTown.Spots.TryGetValue("chapel",out var chapel)){var face=(sq-chapel.at);face.y=0;cameraView.transform.position=sq+face.normalized*-4+Vector3.up*7;cameraView.transform.LookAt(chapel.at+Vector3.up*3);WorldLook.Cut(cameraView);}
            yield return new WaitForSeconds(1f);Capture("festival-choir");
            bool pass=QuarryFestival.Flags>=6&&QuarryFestival.Parade==6&&moved>5&&choir>=4;
            Debug.Log($"LITTLE_ROAD_FESTIVAL_TEST flags={QuarryFestival.Flags} parade={QuarryFestival.Parade} moved={moved:0.0}m choir={choir} pass={pass}");
            Application.Quit(pass?0:1);
        }
        // Test (--world-tiles-test): the woods away from the start stream in on their own within a minute, the car standing
        // still; compare `spawned` with a --no-world-tiles --load-timing run (the same world, piece for piece).
        // Test (--overtake-test, with or without --traffic-yield): the car 35 m behind a Quay road cruiser, in its lane, driven
        // down that lane at up to 16 m/s. Logs whether it got by (10 m ahead within 25 s), how long it took and the bumps.
        IEnumerator OvertakeTest()
        {
            RoadCruiser cruiser=null;foreach(var c in RoadCruiser.All)if(c.OnRoad(HillcrestRoute.Quay)){cruiser=c;break;}
            if(cruiser==null){Debug.Log("LITTLE_ROAD_OVERTAKE_TEST no cruiser");Application.Quit(1);yield break;}
            var road=cruiser.Road;float s0=road.Length*.25f;cruiser.PlaceAt(s0,1);
            Vector3 Lane(float s)=>road.At(s)+road.Right(s)*cruiser.Lane;Vector3 Centre(float s)=>road.At(s);
            var start=Lane(s0-35);player.body.isKinematic=true;player.body.position=start+Vector3.up*.6f;player.transform.SetPositionAndRotation(start+Vector3.up*.6f,Quaternion.LookRotation(road.At(s0-33)-road.At(s0-35)));
            yield return new WaitForFixedUpdate();player.body.isKinematic=false;player.body.linearVelocity=Vector3.zero;
            int bumps=0;player.Bumped+=(other,speed)=>{if(other.GetComponentInParent<RoadCruiser>()==cruiser)bumps++;};
            // (In its lane up to it, then out to the crown of the road to pass, as a driver would.)
            var points=new List<Vector3>();for(float s=s0-30;s<Mathf.Min(road.Length-20,s0+260);s+=8)points.Add(s<s0-14?Lane(s):Centre(s));
            float t0=Time.time,passedAt=-1,slowest=99;

            // (Ahead along the road's own direction at the cruiser, which it keeps: dir 1.)
            bool Passed(){var d=player.body.position-cruiser.transform.position;var f=road.At(cruiser.Along+2)-road.At(cruiser.Along-2);f.y=0;float along=Vector3.Dot(d,f.normalized);if(along>10&&passedAt<0)passedAt=Time.time-t0;
                if(Time.time-t0>3)slowest=Mathf.Min(slowest,player.speed);return passedAt>=0||Time.time-t0>25;}
            yield return DriveThrough(points,16,Passed);
            bool pass=passedAt>=0;
            Debug.Log($"LITTLE_ROAD_OVERTAKE_TEST yield={RoadCruiser.Yields} passed={pass} seconds={(pass?passedAt:25):0.0} bumps={bumps} slowest={slowest:0.0}m/s");
            Application.Quit(pass?0:1);
        }
        IEnumerator WorldTilesTest()
        {
            int deferred=WorldTiles.Deferred;float start=Time.realtimeSinceStartup;
            while(WorldTiles.Pending>0&&Time.realtimeSinceStartup-start<90)yield return null;
            bool pass=deferred>0&&WorldTiles.Pending==0;
            Debug.Log($"LITTLE_ROAD_WORLD_TILES_TEST deferred={deferred} pending={WorldTiles.Pending} seconds={Time.realtimeSinceStartup-start:0.0} spawned={BayviewKit.Spawned} pass={pass}");
            Application.Quit(pass?0:1);
        }
        // Test (--wayfinding-test, with or without --quarry-hills): read every board (pin the first line, check the objective
        // follows it), and picture each board and fingerpost. Pass: a board in every town, fingerposts at the junctions.
        IEnumerator WayfindingTest()
        {
            yield return new WaitForSeconds(1);int pinned=0;int expected=1+(HillcrestTown.Spots.Count>0?1:0)+(QuarryHills.Enabled?1:0);
            foreach(var b in Wayfinding.Boards) {
                player.body.rotation=Quaternion.LookRotation(-b.front);Place(b.at+b.front*4.5f);yield return new WaitForSeconds(.5f);
                ReadBoard(b);yield return new WaitForSeconds(.3f);Capture("board-"+b.town.Replace(' ','-').ToLowerInvariant()+"-menu");
                var entries=BoardEntries(b);int first=entries.FindIndex(x=>x.label!=null);
                if(first>=0&&ui.InvokeChoice(entries[first].text)&&GoalText(true).StartsWith("Pinned: "+entries[first].label))pinned++;
                Debug.Log($"LITTLE_ROAD_BOARD {b.town} at=({b.at.x:0},{b.at.y:0.0},{b.at.z:0}) entries={entries.Count} goal=\"{GoalText(true)}\" : "+string.Join(" | ",entries.ConvertAll(x=>x.text)));
                Close();pin=null;
                cameraView.transform.position=b.at+b.front*5.5f+Vector3.Cross(Vector3.up,b.front)*1.5f+Vector3.up*1.9f;cameraView.transform.LookAt(b.at+Vector3.up*1.6f);WorldLook.Cut(cameraView);
                yield return new WaitForSeconds(.4f);Capture("board-"+b.town.Replace(' ','-').ToLowerInvariant());
            }
            int shot=0;
            foreach(var (at,text) in Wayfinding.Posts) {
                Place(at+new Vector3(9,0,9));yield return new WaitForSeconds(.3f);
                var look=at+Vector3.up*2.6f;var from=at+new Vector3(4.5f,2.4f,3.5f);
                cameraView.transform.position=from;cameraView.transform.LookAt(look);WorldLook.Cut(cameraView);yield return new WaitForSeconds(.4f);Capture($"fingerpost-{shot++}");
            }
            bool pass=Wayfinding.Boards.Count==expected&&pinned==expected&&(HillcrestTown.Spots.Count==0||Wayfinding.Posts.Count>=3);
            Debug.Log($"LITTLE_ROAD_WAYFINDING_TEST boards={Wayfinding.Boards.Count}/{expected} pinned={pinned} fingerposts={Wayfinding.Posts.Count} pass={pass}");
            Application.Quit(pass?0:1);
        }
        // Test (--quarry-hills --carving-test): every carving is clear of buildings and props, and collecting all twelve pays.
        // (--bramblewick --horseshoe-test: the same for Bramblewick's six horseshoes.)
        IEnumerator CarvingTest()=>KeepsakeTest("CARVING",Collectibles.Carvings,Collectibles.Carving,MapRegions.CarvingPlace,Collectibles.IsCarving);
        IEnumerator KeepsakeTest(string tag,int count,Func<int,string> idOf,Func<int,string> place,Func<string,bool> kind)
        {
            yield return new WaitForSeconds(1);int blocked=0;float credits=Store.Data.credits;
            for(int i=0;i<count;i++) {
                string id=idOf(i);if(!finds.TryGetValue(id,out var go))continue;var at=go.transform.position;
                int hits=0;string what="";foreach(var c in Physics.OverlapSphere(at,2.2f))if(c.CompareTag("Respawn")&&c.GetComponentInParent<RoadCar>()==null){hits++;what+=c.name+";";}if(hits>0)blocked++;
                Debug.Log($"LITTLE_ROAD_{tag} {i} {place(i)} at=({at.x:0},{at.y:0.0},{at.z:0}) ground={ValleyField.Height(at.x,at.z):0.0} road={ValleyField.RoadDistance(at.x,at.z):0} blocked={hits} {what}");
                player.body.isKinematic=true;player.body.position=at+Vector3.down*.8f;yield return new WaitForFixedUpdate();yield return null;yield return null;
            }
            player.body.isKinematic=false;int got=0;foreach(var c in Store.Data.collected)if(kind(c))got++;
            Debug.Log($"LITTLE_ROAD_{tag}_TEST collected={got}/{count} blocked={blocked} credits={credits}->{Store.Data.credits}");
            Application.Quit(got==count&&blocked==0?0:1);
        }
        // Test (--quarry-hills --blondin-test): ride the Blondin up from the floor and back down, with a picture mid-ride
        // (camera off to the side); the car must end each ride in the far bay.
        IEnumerator BlondinTest()
        {
            yield return new WaitForSeconds(1);var line=ZipLine.All.Find(z=>z.title.Contains("Blondin"));
            if(line==null){Debug.Log("LITTLE_ROAD_BLONDIN_TEST missing");Application.Quit(1);yield break;}
            Place(line.bottom.bay);yield return new WaitForSeconds(.6f);float t0=Time.time;
            var up=StartCoroutine(RideZip(line,false));
            yield return new WaitForSeconds(12);var p=player.body.position;
            cameraView.transform.position=p+new Vector3(-30,8,25);cameraView.transform.LookAt(p);yield return null;Capture("blondin-ride");
            while(riding)yield return null;float upTime=Time.time-t0;float upEnd=Vector3.Distance(player.body.position,line.top.bay);
            yield return new WaitForSeconds(1);t0=Time.time;StartCoroutine(RideZip(line,true));yield return null;while(riding)yield return null;
            float downTime=Time.time-t0,downEnd=Vector3.Distance(player.body.position,line.bottom.bay);
            Debug.Log($"LITTLE_ROAD_BLONDIN_TEST up={upTime:0.0}s end={upEnd:0.0}m down={downTime:0.0}s end={downEnd:0.0}m");
            Application.Quit(upEnd<4&&downEnd<4?0:1);
        }
        // Test (--quarry-hills --lesson-test): each rally lesson run along a clean line (carried, 14 m/s; the mini-stage at
        // 20 m/s) must finish with a medal; six golds must release the rally parts at Grit & Gravel.
        IEnumerator LessonTest()
        {
            yield return new WaitForSeconds(1);float credits=Store.Data.credits;var report=new List<string>();
            // Through the licence screen (LicenceScreen): Gwen → Rally licence → test 1; each attempt must end on its result
            // card, from which Next test starts the next; after the last, Back to licences then Exit puts the car by Gwen.
            string folder=RaceTrial.Arg("--trial-captures");if(folder!=null)Directory.CreateDirectory(folder);
            Place(residents["rallyschool"]+Vector3.forward*5);yield return new WaitForSeconds(.5f);
            bool opened=Interact("rallyschool")&&ui.InvokeChoice("Rally licence");yield return new WaitForSeconds(.5f);
            opened&=ui.Licence!=null&&!ui.Licence.IsResult;if(folder!=null)ui.Capture(Path.Combine(folder,"licence-1-list.png"));
            if(opened){EventSystem.current.SetSelectedGameObject(ui.Licence.Choices[2].gameObject);yield return new WaitForSeconds(.3f);opened&=ui.Licence.Focused==2;
                if(folder!=null)ui.Capture(Path.Combine(folder,"licence-2-focus-3.png"));}
            bool started=opened&&ui.InvokeChoice("Test 1 ");int cards=0;
            for(int i=0;i<RallySchool.Lessons.Length;i++) {
                if(i>0){bool next=ui.Licence!=null&&ui.Licence.IsResult&&ui.InvokeChoice("Next test");if(!next)StartLesson(i);}
                yield return new WaitForSeconds(3.2f);var path=RallySchool.TestPath(i);float speed=i==5?20:i==3?15:14;   // (the flick at 14 m/s landed on gold's 10.5 s exactly; under load it read silver)
                player.body.isKinematic=true;
                for(int k=0;k+1<path.Count&&school.Active;k++){var a=path[k];var b=path[k+1];float len=Vector3.Distance(a,b);
                    for(float d=0;d<len&&school.Active;d+=speed*Time.fixedDeltaTime){var p=Vector3.Lerp(a,b,d/len);p.y=ValleyField.Height(p.x,p.z)+.6f;player.body.MovePosition(p);
                        var f=b-a;f.y=0;if(f.sqrMagnitude>.01f)player.body.MoveRotation(Quaternion.LookRotation(f));yield return new WaitForFixedUpdate();}}
                for(int k=0;k<30&&school.Active;k++)yield return new WaitForFixedUpdate();
                player.body.isKinematic=false;var r=RallySchool.Record(Store.Data,i);report.Add($"{i}:{(r==null?"none":RallySchool.MedalName(r.stars)+" "+r.best)}");
                yield return new WaitForSeconds(.4f);if(ui.Licence!=null&&ui.Licence.IsResult)cards++;
                if(folder!=null&&(i==0||i==5))ui.Capture(Path.Combine(folder,$"licence-3-result-{i+1}.png"));
            }
            bool back=ui.InvokeChoice("Back to licences");yield return new WaitForSeconds(.4f);back&=ui.Licence!=null&&!ui.Licence.IsResult&&ui.Licence.Focused==5;
            if(folder!=null)ui.Capture(Path.Combine(folder,"licence-4-after.png"));
            bool exited=ui.InvokeChoice("Back Exit licences");yield return new WaitForSeconds(.4f);
            exited&=!ui.IsOpen&&Vector3.Distance(player.body.position,residents["rallyschool"])<14;
            bool unlocked=PartDiscovery.Met(Store.Data,RallySchool.Unlock);
            Debug.Log($"LITTLE_ROAD_LESSON_TEST {string.Join(" ",report)} allGold={RallySchool.AllGold(Store.Data)} partsUnlocked={unlocked} credits={credits}->{Store.Data.credits} screen={opened} started={started} resultCards={cards} back={back} exitToGwen={exited}");
            Application.Quit(unlocked&&opened&&started&&cards==6&&back&&exited?0:1);
        }
        // Test (--quarry-hills --climb-test): carried up the Gallery Climb's middle line at 15 m/s from the bottom flags, the
        // run must start itself and finish at the top; a still from part-way up, looking across the galleries.
        IEnumerator ClimbTest()
        {
            yield return new WaitForSeconds(1);var path=GalleryClimb.Path;if(path.Count<10){Debug.Log("LITTLE_ROAD_CLIMB_TEST no path");Application.Quit(1);yield break;}
            var back=GalleryClimb.Start-GalleryClimb.StartDir*20;back.y=ValleyField.Height(back.x,back.z);var all=new List<Vector3>{back};all.AddRange(path);all.Add(GalleryClimb.Finish);
            player.body.isKinematic=true;bool started=false,shot=false;float t0=Time.time;
            for(int k=0;k+1<all.Count;k++){var a=all[k];var b=all[k+1];float len=Vector3.Distance(a,b);
                for(float d=0;d<len;d+=15*Time.fixedDeltaTime){var p=Vector3.Lerp(a,b,d/len)+Vector3.up*.6f;var f=b-a;f.y=0;
                    player.body.MovePosition(p);if(f.sqrMagnitude>.01f)player.body.MoveRotation(Quaternion.LookRotation(f));
                    yield return new WaitForFixedUpdate();if(galleryStart>=0)started=true;
                    if(!shot&&k==all.Count/2){shot=true;cameraView.transform.position=p+new Vector3(0,40,0)-GalleryClimb.StartDir*60;cameraView.transform.LookAt(p);yield return null;Capture("gallery-climb");}}}
            for(int k=0;k<20;k++)yield return new WaitForFixedUpdate();
            player.body.isKinematic=false;var r=Store.Data.traps?.Find(x=>x.id==GalleryClimb.Id);
            Debug.Log($"LITTLE_ROAD_CLIMB_TEST started={started} finished={(r!=null)} best={(r!=null?r.best:0)} rise={GalleryClimb.Finish.y-GalleryClimb.Start.y:0} points={path.Count}");
            Application.Quit(started&&r!=null?0:1);
        }
        // A test autopilot through the points (the train test's way: the car's own throttle and steering), slowing for the
        // turns between rows. Gives up on a point after 12 s.
        IEnumerator DriveThrough(List<Vector3> points,float speed,Func<bool> stop)
        {
            int i=0;float since=Time.time;
            player.commandSource=c=>{
                if(i>=points.Count)return Vector2.zero;var to=points[i]-c.body.position;to.y=0;float angle=Vector3.SignedAngle(c.transform.forward,to,Vector3.up);
                float want=Mathf.Lerp(speed,5,Mathf.InverseLerp(20,80,Mathf.Abs(angle)));
                return new Vector2(DriveModel.PedalForSpeed(c.tuning,c.speed,want),Mathf.Clamp(angle/20,-1,1));};   // (behind: a full-lock turn, never reverse)
            while(i<points.Count&&!stop()) {
                var d=points[i]-player.body.position;d.y=0;if(d.magnitude<4||Time.time-since>12){i++;since=Time.time;}
                yield return new WaitForFixedUpdate();
            }
            player.commandSource=null;
        }
        IEnumerator FarmTest(FarmChallenges.Kind k)
        {
            yield return new WaitForSeconds(1);var giles=residents["farm"];Place(giles+Quaternion.Euler(0,residentYaw["farm"],0)*Vector3.forward*5);yield return new WaitForSeconds(.5f);
            bool talked=Interact("farm");bool started=talked&&ui.InvokeChoice(k==FarmChallenges.Kind.Harvest?"The Harvest":"Apple Shake");bool hud=activityHud.Busy;float credits=Store.Data.credits;
            yield return new WaitForSeconds(FarmChallenges.Countdown+.1f);
            Vector3 F(float x,float z){var q=HillcrestRoute.Farm(x,z);q.y=ValleyField.Height(q.x,q.z);return q;}
            var route=new List<Vector3>();
            if(k==FarmChallenges.Kind.Harvest){var w=HillcrestTown.WheatLocal;var size=HillcrestTown.WheatSize;bool east=true;
                for(float z=w.y+size.y*.5f-3;z>w.y-size.y*.5f;z-=5.4f){route.Add(F(east?w.x-size.x*.5f+2:w.x+size.x*.5f-2,z));route.Add(F(east?w.x+size.x*.5f-2:w.x-size.x*.5f+2,z));east=!east;}}
            else{const int cols=13;for(int r=0;r*cols<Orchard.All.Count;r++)for(int c=0;c<cols;c++){int n=r*cols+(r%2==0?c:cols-1-c);if(n<Orchard.All.Count)route.Add(Orchard.All[n].transform.position);}}   // (tree by tree, a row at a time)
            if(Orchard.All.Count>0)Debug.Log($"LITTLE_ROAD_FARM_TEST route first=({route[0].x:0},{route[0].z:0}) tree0=({Orchard.All[0].transform.position.x:0},{Orchard.All[0].transform.position.z:0}) car=({player.body.position.x:0},{player.body.position.z:0})");
            StartCoroutine(TraceFarm());
            yield return DriveThrough(route,14,()=>!farm.Active);
            float until=Time.time+FarmChallenges.Limit(k)+2;while(farm.Active&&Time.time<until)yield return null;
            var r_=SpeedTraps.Record(Store.Data,FarmChallenges.Ids[(int)k]);
            Debug.Log($"LITTLE_ROAD_FARM_TEST kind={k} talked={talked} started={started} hud={hud} result={farm.LastResult*100:0.0}% stars={farm.LastStars} best={r_?.best??0} credits={credits}->{Store.Data.credits} trees={Orchard.All.Count}");
            Application.Quit(talked&&started&&hud&&farm.LastStars>=1?0:1);
        }
        // --crop-test (bug inbox #20): the car drives straight across a valley field; the crop should lie down in its tracks
        // (CropTrample). Logs the cells pressed and photographs the field from above.
        IEnumerator CropTest()
        {
            yield return new WaitForSeconds(1);var f=HillcrestTown.SampleField;if(f==null){Debug.Log("LITTLE_ROAD_CROP_TEST no field");Application.Quit(1);yield break;}
            var (c,yaw,w,d)=f.Value;var along=Quaternion.Euler(0,yaw,0)*Vector3.forward;var a=c-along*(d*.5f+8);var b=c+along*(d*.5f+8);   // (across the rows)a.y=ValleyField.Height(a.x,a.z);b.y=ValleyField.Height(b.x,b.z);
            Place(a);player.body.rotation=Quaternion.LookRotation(along);yield return new WaitForSeconds(.5f);int before=CropTrample.Pressed;
            yield return DriveThrough(new List<Vector3>{a,b},10,()=>false);yield return new WaitForSeconds(.3f);
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            // (its own camera, drawn over the chase camera, which keeps following the car)
            var cam=new GameObject("Crop test camera").AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.depth=Camera.main.depth+10;cam.transform.position=c+Vector3.up*14-Quaternion.Euler(0,yaw,0)*Vector3.right*16+along*6;cam.transform.LookAt(c);Camera.main.tag="Untagged";cam.tag="MainCamera";   // (Toy.Capture renders Camera.main)
            yield return new WaitForSecondsRealtime(.5f);Toy.Capture(Path.Combine(folder,"crop-track.png"));
            Debug.Log($"LITTLE_ROAD_CROP_TEST field=({c.x:0},{c.z:0}) pressed={CropTrample.Pressed-before} pass={(CropTrample.Pressed-before>40)}");
            Application.Quit(CropTrample.Pressed-before>40?0:1);
        }
        IEnumerator TraceFarm(){while(farm.Active){Debug.Log($"LITTLE_ROAD_FARM_TRACE t={Time.time-farm.GoAt:0} car=({player.body.position.x:0},{player.body.position.z:0}) speed={player.speed:0.0} progress={farm.Progress*100:0}%");yield return new WaitForSeconds(4);}}
        IEnumerator ActivityHudReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            yield return new WaitForSeconds(1);var giles=residents["farm"];Place(giles+Quaternion.Euler(0,residentYaw["farm"],0)*Vector3.forward*5);yield return new WaitForSeconds(.5f);
            Interact("farm");yield return new WaitForSeconds(.4f);Toy.Capture(Path.Combine(folder,"h0-giles.png"));
            ui.InvokeChoice("The Harvest");yield return new WaitForSeconds(.9f);Toy.Capture(Path.Combine(folder,"h1-intro.png"));
            yield return new WaitForSeconds(1.6f);Toy.Capture(Path.Combine(folder,"h2-flying.png"));
            yield return new WaitForSeconds(1.5f);
            var w=HillcrestTown.WheatLocal;var size=HillcrestTown.WheatSize;Vector3 F(float x,float z){var q=HillcrestRoute.Farm(x,z);q.y=ValleyField.Height(q.x,q.z);return q;}
            yield return DriveThrough(new List<Vector3>{F(w.x-size.x*.5f+2,w.y+size.y*.5f-3),F(w.x+size.x*.5f-2,w.y+size.y*.5f-3),F(w.x+size.x*.5f-2,w.y+size.y*.5f-8.4f),F(w.x-20,w.y+size.y*.5f-8.4f)},14,()=>false);
            Toy.Capture(Path.Combine(folder,"h3-tracker.png"));
            pause.Open(0);yield return new WaitForSecondsRealtime(.6f);Toy.Capture(Path.Combine(folder,"h4-pause.png"));
            bool exitShown=GameObject.Find("Choice Exit challenge")!=null;pause.Close();yield return null;ExitActivity();yield return new WaitForSeconds(1.2f);
            Toy.Capture(Path.Combine(folder,"h5-after-exit.png"));
            float fromGiles=Vector3.Distance(player.body.position,residents["farm"]);
            Debug.Log($"LITTLE_ROAD_ACTIVITY_HUD_REVIEW exitShown={exitShown} backAtGiles={fromGiles:0.0}m active={activity!=null} farm={farm.Active}");
            Application.Quit(exitShown&&fromGiles<12&&activity==null&&!farm.Active?0:1);
        }
        IEnumerator LeapTest()
        {
            yield return new WaitForSeconds(1);string folder=RaceTrial.Arg("--trial-captures");
            var start=TipLeap.Lip-TipLeap.Dir*70;start.y=ValleyField.Height(start.x,start.z);player.body.rotation=Quaternion.LookRotation(TipLeap.Dir);Place(start);yield return new WaitForSeconds(.6f);
            player.body.linearVelocity=TipLeap.Dir*32;float until=Time.time+16;bool shot=false;
            while(leap.Last<=0&&Time.time<until) {
                if(folder!=null&&!shot&&Vector3.Dot(player.body.position-TipLeap.Lip,TipLeap.Dir)>28){shot=true;reviewing=true;ui.SetVisible(false);var side=Vector3.Cross(Vector3.up,TipLeap.Dir);
                    cameraView.transform.position=TipLeap.Lip+TipLeap.Dir*30+side*55+Vector3.up*2;cameraView.transform.LookAt(player.body.position);yield return null;Directory.CreateDirectory(folder);ui.Capture(Path.Combine(folder,"leap-1-air.png"));reviewing=false;ui.SetVisible(true);}
                else if(player.speed<26&&Vector3.Dot(player.body.position-TipLeap.Lip,TipLeap.Dir)<0)player.body.linearVelocity=TipLeap.Dir*32+Vector3.up*player.body.linearVelocity.y;   // (hold the run-up speed: a test throttle)
                yield return new WaitForFixedUpdate();
            }
            yield return new WaitForSeconds(2.5f);var r=SpeedTraps.Record(Store.Data,TipLeap.Id);
            Debug.Log($"LITTLE_ROAD_LEAP_TEST distance={leap.Last:0.0} stars={r?.stars??0} best={r?.best??0} upright={Vector3.Dot(player.transform.up,Vector3.up):0.00} credits={Store.Data.credits}");
            Application.Quit(leap.Last>=TipLeap.Marks[0]?0:1);
        }
        IEnumerator BlastTest()
        {
            yield return new WaitForSeconds(1);
            Place(residents["blast"]+Quaternion.Euler(0,residentYaw["blast"],0)*Vector3.forward*5);yield return new WaitForSeconds(.5f);
            bool talked=Interact("blast");bool started=talked&&ui.InvokeChoice("Blast Run");float credits=Store.Data.credits;
            yield return new WaitForSeconds(BlastRun.Countdown+.2f);
            var path=BlastRun.Path;float speed=BlastRun.Length/(BlastRun.DaiTime*.8f);player.body.isKinematic=true;float d=0;int i=0;
            while(i<path.Count-1) {
                d+=speed*Time.fixedDeltaTime;while(i<path.Count-1&&d>Vector3.Distance(path[i],path[i+1])){d-=Vector3.Distance(path[i],path[i+1]);i++;}
                var p=i<path.Count-1?Vector3.Lerp(path[i],path[i+1],d/Vector3.Distance(path[i],path[i+1])):path[^1];p.y=ValleyField.Height(p.x,p.z)+.6f;player.body.position=p;
                yield return new WaitForFixedUpdate();
            }
            var end=path[^1]+(path[^1]-path[^2]).normalized*4;end.y=ValleyField.Height(end.x,end.z)+.6f;player.body.position=end;
            float deadline=Time.time+BlastRun.ShotTime+5;while(!blast.LastShot&&Time.time<deadline)yield return null;
            // Photos of the shot from the crest, looking back down at the floor (with --trial-captures).
            string folder=RaceTrial.Arg("--trial-captures");
            if(folder!=null){Directory.CreateDirectory(folder);player.controlsBlocked=true;reviewing=true;ui.SetVisible(false);
                foreach(var (name,wait) in new[]{("blast-1-shot",1.2f),("blast-2-cloud",3f),("blast-3-settling",6f)}) {
                    yield return new WaitForSeconds(wait);var top=path[path.Count/2];cameraView.transform.position=top+Vector3.up*45;cameraView.transform.LookAt(Vector3.Lerp(path[0],BlastRun.BlastAt,.45f));cameraView.fieldOfView=60;
                    yield return null;ui.Capture(Path.Combine(folder,name+".png"));}}
            player.body.isKinematic=false;yield return new WaitForSeconds(1);
            bool claimed=Store.Data.collected.Contains(Collectibles.BlastRunWin);
            Debug.Log($"LITTLE_ROAD_BLAST_TEST talked={talked} started={started} cleared={blast.LastCleared} time={blast.LastTime:0.0} beatDai={blast.LastBeatDai} shot={blast.LastShot} claimed={claimed} credits={credits}->{Store.Data.credits} course={BlastRun.Length:0}m dai={BlastRun.DaiTime:0}s shotAt={BlastRun.ShotTime:0}s");
            Application.Quit(talked&&started&&blast.LastCleared&&blast.LastBeatDai&&blast.LastShot&&claimed?0:1);
        }
        IEnumerator SprintTest()
        {
            yield return new WaitForSeconds(1);
            Place(residents["rallyschool"]+Vector3.forward*5);yield return new WaitForSeconds(.5f);
            bool talked=Interact("rallyschool");bool started=talked&&ui.InvokeChoice("Quarry Sprint");
            float credits=Store.Data.credits;yield return new WaitForSeconds(3.2f);
            var loop=QuarryFloor.Loop;int n=loop.Length,s0=HillcrestRoute.Index(loop,new Vector3(-2335,0,-2935));
            player.body.isKinematic=true;
            for(float d=0;d<n*2+40;d+=22*Time.fixedDeltaTime/2) {
                int i=(s0-12+(int)d+n)%n;var p=loop[i];p.y=ValleyField.Height(p.x,p.z)+.6f;player.body.position=p;
                yield return new WaitForFixedUpdate();if(sprintStart<0)break;
            }
            player.body.isKinematic=false;
            bool claimed=Store.Data.collected.Contains(Collectibles.QuarrySprint);
            Debug.Log($"LITTLE_ROAD_SPRINT_TEST talked={talked} started={started} gates={sprintGate} finished={sprintStart<0} claimed={claimed} credits={credits}->{Store.Data.credits}");
            Application.Quit(talked&&started&&claimed?0:1);
        }
        GameObject Block(string name,Vector3 p,Vector3 scale,Color color,bool solid=false,PrimitiveType shape=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.position=p;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material(name,color);
            if(!solid)Destroy(go.GetComponent<Collider>());
            else {
                go.tag="Respawn";
                // Unity's default cylinder collider is a capsule. Flattening a
                // footing otherwise creates a tall invisible collision dome.
                if(shape==PrimitiveType.Cylinder){go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;}
            }
            return go;
        }
        void Sign(string name,string text,Vector3 p)
        {
            var go=new GameObject(name);go.transform.position=p;go.transform.rotation=Quaternion.Euler(0,180,0);
            var label=go.AddComponent<TextMesh>();label.text=text;label.fontSize=60;label.characterSize=.07f;label.anchor=TextAnchor.MiddleCenter;
            label.color=new Color(1,.94f,.75f);var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.font=font;go.GetComponent<Renderer>().sharedMaterial=font.material;
            go.AddComponent<ResidentLabel>();
            go.AddComponent<WorldText>();
        }
        static readonly Color PitTeal=new(.08f,.72f,.66f),PitYellow=new(1,.74f,.06f),PitPink=new(1,.34f,.64f);
        static readonly Dictionary<string,(string body,Color colour,string label,string speaker,Color bubble,string[] lines)> People=new(){
            // (Pit Stop hosts drive the Pit Stop's own vehicles in its colours in every town: CarBodies.PitStop.)
            ["garage"]=(CarBodies.PitGarage,PitTeal,"MARA / PIT STOP GARAGE","Mara",Toy.Orange,new[]{"Careful! I only just fixed that dent.","Bring it round the garage and I'll take a look!","Ha! You drive like I did at your age."}),
            ["parts"]=(CarBodies.PitParts,PitYellow,"SID / PIT STOP PARTS","Sid",Toy.Sun,new[]{"Mind the shelves! That's a whole engine up there.","Tyres, turbos, brakes... just not bumpers.","Need parts? Try buying them, not headbutting them!"}),
            ["hcparts"]=(CarBodies.PitParts,PitYellow,"ROSA / PIT STOP PARTS","Rosa",Toy.Sun,new[]{"Careful! The Valley V6 bruises easily.","Hill grip tyres would have stopped that.","Parts are for fitting, not for dents!"}),
            ["hcbody"]=(CarBodies.PitBody,PitPink,"LINA / PIT STOP BODY SHOP","Lina",Toy.Pink,new[]{"Easy on the show car's cousin!","That dent? I could sell you a whole new car instead.","Ooh, a bump. Fancy a new look while you're here?"}),
            ["post"]=("pebble",new Color(.9f,.7f,.23f),"PIP / LIGHTHOUSE","Pip",Toy.Sun,new[]{"Whoa! The lighthouse is that way, not through me!","Letters, parcels... and now bumps. Busy day!"}),
            ["paint"]=("picnic",Toy.Hex(0xff7ac8),"DOT / PAINTS","Dot",Toy.Pink,new[]{"Ooh, that scrape would look lovely in candy pink.","Fancy a fresh coat after that?","Mind my wet paint!"}),
            ["cafe"]=("otter",new Color(.38f,.48f,.68f),"ROWAN / COAST RUN","Rowan",Toy.Sky,new[]{"Save it for the race!","Rubbing's racing... but not by the café!","Is that a challenge? See you on the grid!"}),
            ["motors"]=(CarBodies.PitGarage,PitTeal,"GIO / PIT STOP GARAGE","Gio",Toy.Sun,new[]{"Easy! Hill brakes are a work of art.","That knock? Sounds like you need better tyres.","Welcome to Hillcrest, speedy!"}),
            ["hillcafe"]=("tallboy",Toy.Hex(0x3ee6a4),"FERN / BELLA VISTA","Fern",Toy.Mint,new[]{"Careful, the espresso's still hot!","You drove up that hill in THAT?","Save the bumping for the raceway!"}),
            ["raceway"]=("rally",Toy.Hex(0xff4d5e),"VITO / RACEWAY","Vito",Toy.Red,new[]{"Whoa! Save that for the Valley Cup!","You call that a bump? Wait till turn one!","Sign up, then we'll see who's bumping who!"}),
            ["station"]=("heron",Toy.Hex(0x2e9e5b),"STELLA / STATION","Stella",Toy.Mint,new[]{"Mind the gap! And my bonnet!","The 3:15 is never late. Unlike you!","Tickets, please! Oh, you're a car."}),
            ["castle"]=(CarBodies.Prototype,Toy.Hex(0x9b6bd9),"DUKE ROLO / CASTLE","Duke Rolo",Toy.Pink,new[]{"How dare you! I am a DUKE!","Guards! Oh... I don't have guards.","Mind the ancient cobbles, young racer!"}),
            ["vista"]=("doodle",Toy.Hex(0x5ec8ff),"WREN / VIEWPOINT","Wren",Toy.Sky,new[]{"Hey! You're in my photo!","Isn't it the BEST view?","Careful, don't knock me over the edge!"}),
            ["bayhalt"]=("minibus",Toy.Hex(0x1d6fa8),"STANLEY / BAYVIEW HALT","Stanley",Toy.Sky,new[]{"Mind the platform edge! And my wing mirror!","All aboard! Well... not you, you're a car.","The 4:10 to Hillcrest is running early!"}),
            ["lookout"]=("bramble",Toy.Hex(0x3ee6a4),"MOSS / FALLS","Moss",Toy.Mint,new[]{"Shh! You'll scare the fish!","Oof. That's the third bump today.","Mind the drop, it's a long way down!"}),
            ["rallyschool"]=("rally",Toy.Hex(0x229954),"GWEN / RALLY SCHOOL","Gwen",Toy.Mint,new[]{"Oi! Save the contact for the rallycross!","Lift and turn, cariad, lift and turn.","That's a sump guard's job, that is!"}),
            ["qgarage"]=(CarBodies.PitGarage,PitTeal,"HUW / PIT STOP GARAGE","Huw",Toy.Red,new[]{"Easy, butt! I only just swept the bay.","Gravel in the arches again? Bring her in.","Duw, that was a clatter!"}),
            ["qparts"]=(CarBodies.PitParts,PitYellow,"BETHAN / GRIT & GRAVEL","Bethan",Toy.Sun,new[]{"Mind the shelves! Mud flaps everywhere now.","Parts are for fitting, not for bumping, cariad.","Ooh, that'll want a sump guard."}),
            ["qbody"]=(CarBodies.PitBody,PitPink,"NIA / PIT STOP BODY SHOP","Nia",Toy.Pink,new[]{"Slate grey would hide that scratch, you know.","Careful! That's my show car's nan.","New look? I've got just the thing."}),
            ["caban"]=("otter",Toy.Hex(0xe9e3d3),"MEGAN / CABAN","Megan",Toy.Sky,new[]{"Careful! The cawl's still hot!","Tea's brewing, no need to knock!","Duw, you'll have the cakes off the shelf!"}),
            ["farm"]=("beetle",Toy.Hex(0x2e9e5b),"GILES / GOLDEN ACRE FARM","Farmer Giles",Toy.Sun,new[]{"Mind the sunflowers, they bruise easy.","Steady! That's a working farm, that is.","Save it for the wheat, go on."}),
            ["blast"]=("hauler",Toy.Hex(0xf1c40f),"DAI / QUARRY FLOOR","Dai",Toy.Sun,new[]{"Oi! Twelve tonnes of dumper says you lose that one, butt.","Bump me again and I'll tip slate on your bonnet!","Save it for the haul road, go on."}),
            ["slatequay"]=("heron",Toy.Hex(0x1f3a93),"MARI / SLATE QUAY","Mari",Toy.Mint,new[]{"Mind the wagons! They're loaded!","Engine's warming up, keep clear of the shed!","Whoa! The quay's for slate, not stunts!"}),
            ["bwtuner"]=("rally",Toy.Hex(0xc0392b),"JED / BACKYARD TUNER","Jed",Toy.Red,new[]{"Steady on! That's a fresh coil-over you just rattled.","Proper job, that knock. Bring 'er round the barn.","Mud tyres would've shrugged that off, my 'ansum."}),
            ["bwpub"]=("otter",Toy.Hex(0x2f5d46),"MAGGIE / THE PLOUGH","Maggie",Toy.Mint,new[]{"Mind the hanging baskets, my lover!","Cider's for after the drive, not before!","The Plough's stood four hundred year. Don't you dent it!"}),
            ["bwvicar"]=("tallboy",Toy.Hex(0x2c3e50),"REVEREND CLEM / ST MICHAEL'S","Reverend Clem",Toy.Sky,new[]{"Bless you! ...and bless my bumper.","Gently now, the lychgate's older than both of us.","Evensong at six. Do park considerately."}),
            ["bwmarshal"]=("beetle",Toy.Hex(0xff8c1a),"TED / LANES MARSHAL","Ted",Toy.Sun,new[]{"Yellow flag! Yellow flag! Oh, it's you.","Lift over the crest, then commit.","Thirty years marshalling these lanes, never lost a sheep."}),
            ["vlgarage"]=(CarBodies.PitGarage,PitTeal,"SR. ARMANDO / PIT STOP GARAGE","Sr. Armando",Toy.Red,new[]{"Calma! The ramp's only just been greased.","Bring her in, I'll have a look. Gently!","Forty years of engines and still they bump me."}),
            ["vlparts"]=(CarBodies.PitParts,PitYellow,"INES / PIT STOP PARTS","Ines",Toy.Sun,new[]{"Cuidado! The shelves!","Parts are for fitting, not for bumping, querido.","That'll want new springs, that will."}),
            ["vlbody"]=(CarBodies.PitBody,PitPink,"CATIA / PIT STOP BODY SHOP","Catia",Toy.Pink,new[]{"Mind the paintwork, it's still wet!","Azulejo blue would hide that dent.","New look? Come in, come in."}),
            ["vlbus"]=("minibus",Toy.Hex(0x1f6f8b),"SR. VITOR / CARREIRA","Sr. Vitor",Toy.Sky,new[]{"Next bus at a quarter past. Don't block my stop!","Ei! Thirty years on this route without a scratch.","Bilhetes, please. Oh, you're a car."}),
            ["vlfruta"]=("postvan",Toy.Hex(0x3f7f6a),"DONA AMELIA / FRUTARIA","Dona Amelia",Toy.Mint,new[]{"My oranges! Mind my oranges!","Laranjas, uvas, couves! Fresh this morning!","Ai, you'll bruise the figs."}),
            ["vlbarco"]=("kipper",Toy.Hex(0x1f3a5f),"MESTRE FERNANDO / BARCOS RABELOS","Mestre Fernando",Toy.Sky,new[]{"Ei! Mind the mooring lines!","Forty years on this river and never sank. Don't make me start now.","The boats leave at eleven. Bring a hat."}),
            ["vllodge"]=("dover",Toy.Hex(0x7a1f2b),"DONA GRACA / LODGE SAMPAIO","Dona Graca",Toy.Pink,new[]{"Cuidado! The pipas are full!","Tasting at four. Not for drivers, querido.","That bump was a 1977 vintage."}),
            ["vltasca"]=("otter",Toy.Hex(0x2e5e3e),"SR. ALBERTO / TASCA DO RIO","Sr. Alberto",Toy.Sun,new[]{"The grill's lit, mind the smoke!","Sardinhas at one, fado at nine!","Ai, the terrace chairs!"}),
            ["vlpombal"]=("dover",Toy.Hex(0x2e5e3e),"DONA TERESA / QUINTA DO POMBAL","Dona Teresa",Toy.Mint,new[]{"Mind the cypresses, they're older than the road!","Gently! The harvest is in the yard.","Ai, my poor doves."}),
            ["vllajes"]=("tallboy",Toy.Hex(0x7a1f2b),"ENGENHEIRO RUI / QUINTA DAS LAJES","Engenheiro Rui",Toy.Sky,new[]{"Careful, those vines are ninety years old!","The lagar is full. Please don't make waves.","Bump the crates and you're treading grapes."}),
            ["vlfojo"]=("kipper",Toy.Hex(0x5a3a24),"SR. ANTERO / QUINTA DO FOJO","Sr. Antero",Toy.Sun,new[]{"Ei! The house is falling down without your help.","Mind the fig, it holds up the wall.","We live in the stable now. It's warmer."}),
            ["vlcasal"]=("pebble",Toy.Hex(0x8a6a3a),"TIO MANEL / CASAL DO MEIO","Tio Manel",Toy.Sun,new[]{"Off the eira! The grain's drying!","Half way up, half way down. That's the Casal.","Ai, the hens!"}),
            ["vlmirante"]=("ritz",Toy.Hex(0x3a6aa6),"DONA ERMELINDA / CASA DO MIRANTE","Dona Ermelinda",Toy.Pink,new[]{"Cuidado! The azulejos came from Lisbon!","My grandfather built this with coffee money.","Ai, the palm! It's older than me."}),
            ["vlbelvedere"]=("tallboy",Toy.Hex(0x2a5490),"SR. BELMIRO / TORRE DO BELVEDERE","Sr. Belmiro",Toy.Sky,new[]{"Easy! The bell's heavy and so is my temper.","Best view in the valley, and you're looking at my bumper.","Mind the terrace, people come up here to be quiet."}),
            ["vlveiga"]=("rally",Toy.Hex(0xd35400),"SR. FAUSTO / SUBIDA DAS SEIS VOLTAS","Sr. Fausto",Toy.Red,new[]{"Oi! Save it for the climb!","Six voltas, two hundred and twenty metres. Every one of them is mine.","Bales at the voltas, not on my car!"}),
            ["vlbombeiros"]=("tallboy",Toy.Hex(0xc0392b),"COMANDANTE NUNES / BOMBEIROS","Comandante Nunes",Toy.Red,new[]{"Easy! The engine's polished for Sunday!","We're volunteers. Every one of us. Don't make us work.","Siren's at noon. Mind your ears."}),
            ["vlescola"]=("pebble",Toy.Hex(0x2a5490),"PROFESSORA LUISA / ESCOLA BASICA","Professora Luisa",Toy.Sky,new[]{"Slowly! Children crossing!","Twenty-two pupils and not one of them drives like that.","Bump me again and you're staying after class."}),
            ["vladega"]=("hauler",Toy.Hex(0x7a1f2b),"ENGENHEIRA SOFIA / ADEGA COOPERATIVA","Engenheira Sofia",Toy.Pink,new[]{"The weighbridge is for grapes, not for stunts!","Four hundred growers, one co-op. Mind my tanks.","Harvest queue starts at dawn. You're early."}),
            ["vlclube"]=("roadster",Toy.Hex(0x7a1f2b),"SR. ROGERIO / CLUBE AUTOMOVEL DA CUMEADA","Sr. Rogerio",Toy.Red,new[]{"Easy! The paddock's a walking pace, rapaz!","Save it for the Gancho!","Our marshals are volunteers. Mind them."}),
            ["vlaldeia"]=("pebble",Toy.Hex(0xd9a441),"DONA ILDA / ALDEIA DA OUTRA BANDA","Dona Ilda",Toy.Sun,new[]{"Ai! There are only eight of us, mind who you bump!","The festa's in August. Come back then!","Mind the bunting, it took all spring."}),
            ["vlcorvo"]=("kipper",Toy.Hex(0x3f3a35),"SR. JOAQUIM CORVO / QUINTA DO CORVO","Sr. Joaquim Corvo",Toy.Sky,new[]{"Careful, that wall's been falling since 1962.","The fig's older than the house. It'll outlast us both.","Young vines, old name."}),
            ["vlpriest"]=("tallboy",Toy.Hex(0x1c1c22),"PADRE AMERICO / IGREJA MATRIZ","Padre Americo",Toy.Sky,new[]{"Easy, my child! The bells are watching.","Mass at ten. Park by the fountain, not on it.","Deus me livre! My poor wing."}),
            ["vlclerk"]=("marlow",Toy.Hex(0x2e5e3e),"DONA FATIMA / PACOS DO CONCELHO","Dona Fatima",Toy.Mint,new[]{"That bump will need a form. In triplicate.","The clock is never wrong. You, however...","Carimbo! That's a stamp. On your bumper."}),
            ["vlcafe"]=("otter",Toy.Hex(0xe9dcc0),"SR. JOAQUIM / CAFE CENTRAL","Sr. Joaquim",Toy.Sun,new[]{"Cuidado! The bica is still hot!","Mind the terrace, the chairs are older than you.","Um cafe? After you stop bumping my van."}),
            ["vlbaker"]=("postvan",Toy.Hex(0xd9a441),"DONA ROSA / O FORNO","Dona Rosa",Toy.Sun,new[]{"Ai! The broa! Forty loaves in the back!","Gently, querido, the hens are nervous.","The oven's been warm since five. So am I, now."}),
            ["vlfonte"]=("doodle",Toy.Hex(0x3a6aa6),"TIA LURDES / LAVADOURO","Tia Lurdes",Toy.Pink,new[]{"Splash me again and you're scrubbing sheets!","The spring never stops, and nor do I.","Ai, these young drivers!"}),
            ["bwdairy"]=("pebble",Toy.Hex(0xf6f1e6),"PEG / BRAMBLEWICK DAIRY","Peg",Toy.Sun,new[]{"Oi! Forty years on the round and never a dent!","Think you're quick? Try me down Dairy Lane.","Mind the crates! Them's full!"}),
        };
        // Quarry Hills (--quarry-hills): Gwen at the rally school, the Pit Stop, the caban, the quay and Dai.
        void QuarryHillsResidents()
        {
            foreach(var id in new[]{"rallyschool","qgarage","qparts","qbody","caban","slatequay"})if(QuarryHillsTown.Spots.TryGetValue(id,out var spot)){residents[id]=spot.at;residentYaw[id]=spot.yaw;}
            // Dai the Dumper on the quarry floor, at the foot of the haul road: the Blast Run (BlastRun, doc 63).
            if(QuarryHillsTown.Spots.Count>0){BlastRun.Plan();if(BlastRun.Ready){residents["blast"]=BlastRun.DaiSpot.at;residentYaw["blast"]=BlastRun.DaiSpot.yaw;}}
        }
        bool quarryLater;
        // Quarry Hills has been built in the background (Districts): its residents, signs, festival and activities, as the
        // town set up the rest at load.
        IEnumerator QuarryHillsArrived()
        {
            var before=new HashSet<string>(residents.Keys);QuarryHillsResidents();var added=residents.Keys.Where(k=>!before.Contains(k)).ToList();
            PlaceResidents(added);yield return null;QuarryHillsWayfinding();yield return null;
            if(QuarryHills.Enabled&&QuarryHillsTown.Spots.Count>0&&player!=null)QuarryFestival.Build(transform,Store.Data,player);yield return null;
            if(TipLeap.Ready&&leap==null&&player!=null){leap=gameObject.AddComponent<TipLeap>();leap.Setup(player,()=>Store,Notify,(title,detail)=>ui.Card(title,detail,Toy.Orange,"TIP LEAP"));}
            if(BlastRun.Ready&&blast==null){blast=gameObject.AddComponent<BlastRun>();blast.Crossed+=BlastCrossed;blast.Shot+=BlastShot;}
            yield return null;CarContact.SeatAll();quarryLater=false;
            Debug.Log($"LITTLE_ROAD_DISTRICT arrived quarry-hills residents={added.Count} tipleap={TipLeap.Ready} blast={BlastRun.Ready}");
        }
        void CreatePlaces()
        {
            PlaceResidents(new List<string>(residents.Keys));
            CreatePlacesAfterResidents();
        }
        // Each resident's car where they stand, moved out of any building, prop or wall they were placed in.
        // A parked resident clears the ground under all four corners of its body, not just its middle: where the paving rises
        // under one end, that end sank into it (Duke Rolo's long-nosed Regale in the castle court, bug inbox #8).
        static void SeatOnCorners(GameObject car,BoxCollider box)
        {
            Physics.SyncTransforms();var t=car.transform;float lift=0;var c=box.center;var e=box.size*.5f;
            foreach(var (sx,sz) in new[]{(-1,-1),(-1,1),(1,-1),(1,1),(0,1),(0,-1)}) {
                var corner=t.TransformPoint(new Vector3(c.x+sx*e.x*.9f,c.y-e.y,c.z+sz*e.z*.9f));
                foreach(var hit in Physics.RaycastAll(corner+Vector3.up*2,Vector3.down,4))
                    if(!hit.collider.isTrigger&&!hit.collider.transform.IsChildOf(t))lift=Mathf.Max(lift,hit.point.y+.018f-corner.y);
            }
            if(lift>0&&lift<1)t.position+=Vector3.up*lift;
        }
        void PlaceResidents(List<string> keys)
        {
            var moved=new Dictionary<string,Vector3>();
            foreach(var key in keys) {var pair=new KeyValuePair<string,Vector3>(key,residents[key]);
                var who=People[pair.Key];Color color=who.colour;
                var npc=TrialWorld.Car(who.body,color);npc.name="Resident "+pair.Key;npc.transform.position=pair.Value+Vector3.up*.05f;
                var renderers=npc.GetComponentsInChildren<Renderer>();Bounds visualBounds=renderers[0].bounds;foreach(var renderer in renderers)visualBounds.Encapsulate(renderer.bounds);
                var bounds=npc.AddComponent<BoxCollider>();bounds.center=visualBounds.center-npc.transform.position;bounds.size=visualBounds.size+Vector3.one*.02f;npc.tag="Respawn";
                // Generated bodies stand on their model origin. Their wheel meshes are
                // spun about the axle, so renderer bounds reach well below the tyres.
                // Lift relative to the car's own origin: subtracting the world height buried every
                // mainland resident (ground 6-30 m up) at y=0, leaving only their floating labels.
                float lowest=npc.GetComponent<StandardEnvelope>()!=null?0:visualBounds.min.y-npc.transform.position.y;
                npc.transform.position+=Vector3.up*(.018f-lowest);
                if(pair.Key=="cafe"||pair.Key=="paint")npc.transform.rotation=Quaternion.Euler(0,90,0); // parked along the kerb, clear of the race shoulder
                if(residentYaw.TryGetValue(pair.Key,out float yaw))npc.transform.rotation=Quaternion.Euler(0,yaw,0);
                SeatOnCorners(npc,bounds);
                var chatter=npc.AddComponent<NpcChatter>();(chatter.speaker,chatter.colour,chatter.lines)=(who.speaker,who.bubble,who.lines);chatter.talkable=false;
                badges[pair.Key]=TalkBadge.Add(npc,who.bubble);
                // Out of any building, prop or wall they were placed in (Vito sat in the raceway's control tower).
                if(StationarySpot.Settle(npc)>0)moved[pair.Key]=npc.transform.position;
                CarContact.Seat(npc);   // (on its four wheels where it ends up: docs/80, the Quarry Hills garage host kept a wheel 0.25 m up)
            }
            foreach(var m in moved)residents[m.Key]=m.Value;
        }
        void CreatePlacesAfterResidents()
        {
            var post=residents["post"];var stone=new Color(.91f,.83f,.65f);var red=new Color(.71f,.26f,.18f);
            // Keep the landmark behind its resident, away from the nearest
            // carriageway; a world-Z offset can move it into a curved road.
            float road=world.track.Nearest(post,out _);Vector3 a=world.track.At(road);
            Vector3 tower=post+(post-a).normalized*9;PhotoRequests.Setup(tower);
            if(BayviewKit.Has("lighthouse")) {
                var facing=Quaternion.LookRotation(Vector3.ProjectOnPlane(post-tower,Vector3.up)).eulerAngles.y;
                    BayviewKit.Spawn("lighthouse",tower,facing,null,new BayviewKit.Tint(Color.white,Toy.Hex(0x2bb5a8),Toy.Hex(0xff4d5e),Toy.Hex(0x1d6fa8)));
            } else {
            Block("Lighthouse footing",tower+Vector3.up*.3f,new Vector3(9,.6f,9),stone,true,PrimitiveType.Cylinder);
            Block("Lighthouse tower",tower+Vector3.up*5,new Vector3(4,5,4),stone,true,PrimitiveType.Cylinder);
            Block("Lighthouse red band",tower+Vector3.up*7.4f,new Vector3(4.08f,.55f,4.08f),red,false,PrimitiveType.Cylinder);
            Block("Lantern balcony",tower+Vector3.up*10,new Vector3(6,.25f,6),stone,false,PrimitiveType.Cylinder);
            Block("Lantern housing",tower+Vector3.up*11,new Vector3(3.4f,1,3.4f),new Color(.34f,.65f,.68f),false,PrimitiveType.Cylinder);
            Block("Lantern cap",tower+Vector3.up*12.3f,new Vector3(5,.35f,5),red,false,PrimitiveType.Cylinder);
            }
            var lamp=new GameObject("Lighthouse beacon").AddComponent<Light>();lamp.transform.position=tower+Vector3.up*(BayviewKit.Has("lighthouse")?14.1f:11);lamp.type=LightType.Point;lamp.color=new Color(1,.81f,.44f);lamp.range=20;lamp.intensity=4;
            Vector3 outward=(post-a).normalized;
            Vector3 laneStart=a+outward*12,laneEnd=post+outward*2.5f,delta=laneEnd-laneStart;
            var spur=Block("Lighthouse lane",(laneStart+laneEnd)*.5f+Vector3.up*.043f,new Vector3(6,.04f,delta.magnitude),new Color(.66f,.62f,.46f));spur.transform.rotation=Quaternion.LookRotation(delta);
            if(HillcrestTown.Spots.Count>0) {
                var stampSpots=new[]{HillcrestRoute.Vista+new Vector3(-9,0,4),HillcrestRoute.Lookout+new Vector3(-8,0,-8),HillcrestRoute.CastleCourt+new Vector3(-14,0,4),HillcrestRoute.Paddock+new Vector3(-24,0,6),
                    HillcrestTown.Spots["station"].at+new Vector3(-6,0,-12),new Vector3(HillcrestRoute.BalloonField.x,0,HillcrestRoute.BalloonField.y),new Vector3(214,0,-1398),new Vector3(-30,0,-1222)};
                if(stampSpots.Length!=Collectibles.Stamps)throw new InvalidOperationException("Stamp spots and Collectibles.Stamps disagree.");
                for(int i=0;i<stampSpots.Length;i++) {
                    string id=Collectibles.Stamp(i);var p=stampSpots[i];p.y=Mathf.Max(p.y,ValleyField.Height(p.x,p.z))+1.2f;MapRegions.Keepsakes[id]=(p,MapRegions.StampPlace(i));
                    if(Store.Data.collected.Contains(id))continue;
                    finds[id]=Keepsake(id,p,1,i);
                }
            }
            // Quarry Hills' slate carvings (--quarry-hills).
            if(QuarryHillsTown.Spots.Count>0||quarryLater) {
                var spots=QuarryHills.CarvingSpots();if(spots.Length!=Collectibles.Carvings)throw new InvalidOperationException("Carving spots and Collectibles.Carvings disagree.");
                for(int i=0;i<spots.Length;i++){string id=Collectibles.Carving(i);var p=spots[i]+Vector3.up*1.3f;MapRegions.Keepsakes[id]=(p,MapRegions.CarvingPlace(i));if(Store.Data.collected.Contains(id))continue;finds[id]=Keepsake(id,p,2,i);}
            }
            // Bramblewick's lucky horseshoes (--bramblewick, docs/82).
            if(BramblewickVillage.Horseshoes.Count==Collectibles.Horseshoes)
                for(int i=0;i<Collectibles.Horseshoes;i++){string id=Collectibles.Horseshoe(i);var p=BramblewickVillage.Horseshoes[i]+Vector3.up*1.2f;MapRegions.Keepsakes[id]=(p,MapRegions.HorseshoePlace(i));if(Store.Data.collected.Contains(id))continue;finds[id]=Keepsake(id,p,3,i);}
            for(int i=0;i<Collectibles.Postcards;i++) {
                string id=Collectibles.Postcard(i);
                float d=world.track.length*(.12f+i*.17f);Vector3 p=world.track.At(d)+world.track.Right(d)*(i%2==0?13:-13)+Vector3.up*1.1f;MapRegions.Keepsakes[id]=(p,MapRegions.PostcardPlace(i));
                if(Store.Data.collected.Contains(id))continue;
                finds[id]=Keepsake(id,p,0,i);
            }
        }
        // Postcards and stamps: paper cards with a picture on both faces (they turn) on a bobbing
        // pivot, so they read as keepsakes from the road instead of flat coloured tiles.
        readonly Dictionary<Color,Material> keepsakeMaterials=new();
        // kind: 0 Bayview postcard, 1 Hillcrest stamp, 2 Quarry Hills slate carving, 3 Bramblewick horseshoe.
        GameObject Keepsake(string id,Vector3 at,int kind,int index)
        {
            var root=new GameObject(id).transform;root.position=at;
            var pivot=new GameObject("Bob").transform;pivot.SetParent(root,false);
            Color paper=new(.97f,.94f,.86f);Color[] scenes={Toy.Hex(0x5ec8ff),Toy.Hex(0xffb400),Toy.Hex(0x3ee6a4),Toy.Hex(0xff7ac8),Toy.Hex(0xff4d5e),Toy.Hex(0x9b6bd9),Toy.Hex(0x2bb5a8),Toy.Hex(0xff9f43)};
            Color scene=scenes[index%scenes.Length];
            void Part(Vector3 p,Vector3 size,Color c)
            {
                var g=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(g.GetComponent<Collider>());g.name=id+" part";
                g.transform.SetParent(pivot,false);g.transform.localPosition=p;g.transform.localScale=size;
                if(!keepsakeMaterials.TryGetValue(c,out var m))keepsakeMaterials[c]=m=TrialWorld.Material("Keepsake",c,0,.35f);
                var r=g.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if(kind==3) {
                // Lucky horseshoe: a steel U, arms up to hold the luck, nail holes punched along it, a red ribbon at the top.
                Color steel=Toy.Hex(0xaab4bf),hole=Toy.Hex(0x4a525c),ribbon=Toy.Hex(0xd92b3a);
                foreach(int side in new[]{-1,1}) {
                    Part(new Vector3(side*.26f,.08f,0),new Vector3(.14f,.56f,.1f),steel);Part(new Vector3(side*.19f,-.26f,0),new Vector3(.18f,.16f,.1f),steel);
                    Part(new Vector3(side*.26f,.34f,0),new Vector3(.18f,.06f,.11f),steel*.85f);   // (the heels)
                    foreach(float y in new[]{.18f,.0f,-.14f})foreach(int face in new[]{-1,1})Part(new Vector3(side*.26f,y,face*.051f),new Vector3(.05f,.05f,.01f),hole);
                }
                Part(new Vector3(0,-.31f,0),new Vector3(.24f,.14f,.1f),steel);
                Part(new Vector3(0,.42f,0),new Vector3(.6f,.07f,.04f),ribbon);Part(new Vector3(-.08f,.48f,0),new Vector3(.12f,.12f,.05f),ribbon);Part(new Vector3(.08f,.48f,0),new Vector3(.12f,.12f,.05f),ribbon);
            } else if(kind==2) {
                // Slate carving: a dark tablet with a pale carved border, a carved panel and a red dragon mark.
                Color slate=Toy.Hex(0x3f4650),carved=Toy.Hex(0x9aa3ad),dragon=Toy.Hex(0xc0392b);
                Part(Vector3.zero,new Vector3(.78f,.95f,.09f),slate);
                foreach(int side in new[]{-1,1}) {
                    Part(new Vector3(0,.41f,side*.05f),new Vector3(.66f,.04f,.01f),carved);Part(new Vector3(0,-.41f,side*.05f),new Vector3(.66f,.04f,.01f),carved);
                    Part(new Vector3(.31f,0,side*.05f),new Vector3(.04f,.82f,.01f),carved);Part(new Vector3(-.31f,0,side*.05f),new Vector3(.04f,.82f,.01f),carved);
                    Part(new Vector3(0,.12f,side*.05f),new Vector3(.38f,.3f,.01f),carved*.85f);Part(new Vector3(0,.12f,side*.053f),new Vector3(.22f,.14f,.01f),dragon);
                    Part(new Vector3(0,-.2f,side*.05f),new Vector3(.4f,.05f,.01f),carved);
                }
            } else if(kind==1) {
                // Hillcrest stamp: paper square, coloured scene, white emblem and a darker frame.
                Part(Vector3.zero,new Vector3(.8f,.8f,.04f),paper);
                foreach(int side in new[]{-1,1}) {
                    Part(new Vector3(0,0,side*.023f),new Vector3(.62f,.62f,.01f),scene*.8f);
                    Part(new Vector3(0,0,side*.026f),new Vector3(.54f,.54f,.01f),scene);
                    Part(new Vector3(0,.04f,side*.029f),new Vector3(.2f,.26f,.01f),paper);
                }
            } else {
                // Bayview postcard: sky over sea with a red stamp in the corner.
                Part(Vector3.zero,new Vector3(1f,.68f,.04f),paper);
                foreach(int side in new[]{-1,1}) {
                    Part(new Vector3(-.08f,.09f,side*.023f),new Vector3(.72f,.36f,.01f),Toy.Hex(0x9fd8ff));
                    Part(new Vector3(-.08f,-.15f,side*.023f),new Vector3(.72f,.16f,.01f),scene);
                    Part(new Vector3(.37f,.2f,side*.023f),new Vector3(.15f,.17f,.01f),Toy.Hex(0xff4d5e));
                }
            }
            return root.gameObject;
        }
        // On in the game (20 Oct; --no-ambient-traffic, not in tests): a few family cars cruising the loop on stock kit, using
        // the finished-car planner (gentle cruise, no gates). The player stays cars[0].
        void AmbientTraffic()
        {
            string[] bodies={"pebble","tallboy","picnic","dover"};Color[] paints={Toy.Hex(0xff7ac8),Toy.Hex(0x5ec8ff),Toy.Hex(0xffd23f),Toy.Hex(0x3ee6a4)};
            for(int i=0;i<bodies.Length;i++) {
                float d=world.track.length*(.18f+i*.21f);
                var go=TrialWorld.Car(bodies[i],paints[i]);go.name="Traffic "+bodies[i];
                go.transform.SetPositionAndRotation(world.track.At(d)+world.track.Right(d)*2.5f+Vector3.up*.15f,Quaternion.LookRotation(world.track.Forward(d)));
                var car=go.AddComponent<RoadCar>();car.Setup(world,world.cars.Count,CarTuning.Stock,true,1709+i*31,"traffic");car.finished=true;
                var chatter=go.AddComponent<NpcChatter>();chatter.speaker=new[]{"Nell","Hugo","Bea","Otis"}[i];chatter.colour=paints[i];chatter.title="Bayview driver";
                world.cars.Add(car);
            }
        }
        // Opt-in capture pass: paint shop open, live repaint on a new swatch, purchase, then a
        // speech bubble on Rowan's car. Captures go to --trial-captures.
        // --body-shop-review (own throwaway save): Lina's body shop, a purchase, then Gio's garage
        // bay swapping the live car to it. Captures each step; exits 0 only if the swap happened.
        IEnumerator BodyShopReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            Store.Change(p=>{p.credits=Math.Max(p.credits,3000);return true;});
            var lina=residents["hcbody"];Place(lina+Vector3.forward*6);player.body.rotation=Quaternion.Euler(0,residentYaw["hcbody"]+180,0);yield return new WaitForSeconds(1.2f);
            ui.Capture(Path.Combine(folder,"b0-forecourt.png"));
            Interact("hcbody");yield return new WaitForSeconds(.4f);ui.Capture(Path.Combine(folder,"b1-lina.png"));
            OpenBodyShop();yield return new WaitForSeconds(.9f);ui.Capture(Path.Combine(folder,"b2-shop.png"));
            var target=GameObject.Find("BODY comet");EventSystem.current.SetSelectedGameObject(target);yield return new WaitForSeconds(1.0f);
            ui.Capture(Path.Combine(folder,"b3-comet.png"));
            target.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSeconds(.5f);
            ui.Capture(Path.Combine(folder,"b4-bought.png"));
            bodyShop.Leave();Close();yield return new WaitForSeconds(.3f);
            Place(residents["motors"]+Vector3.forward*6);yield return new WaitForSeconds(.5f);
            Interact("motors");yield return new WaitForSeconds(.3f);ui.InvokeChoice("Swap body");yield return new WaitForSeconds(.3f);
            ui.Capture(Path.Combine(folder,"b5-swap-menu.png"));ui.InvokeChoice("Swap to · Comet");yield return new WaitForSeconds(.4f);Close();
            cameraView.transform.position=player.transform.position+new Vector3(-4,2.2f,6);cameraView.transform.LookAt(player.transform.position+Vector3.up*.8f);yield return new WaitForSeconds(.6f);
            ui.Capture(Path.Combine(folder,"b6-swapped.png"));
            bool swapped=Store.Data.bodyId=="comet"&&player.name=="comet";
            Debug.Log("BODY_SHOP_REVIEW swapped="+swapped+" body="+Store.Data.bodyId+" owned="+string.Join(",",Store.Data.bodies)+" credits="+Store.Data.credits);Application.Quit(swapped?0:2);
        }
        // --tuner-garage-review (own throwaway save, docs/67): class B opened (the class C podiums), then at Gio's garage bay:
        // the free-Tuner choice, the garage after taking the Sakura, the Tuner body shop, the car, the Tuner races, the parts
        // counter on the class B tab and a B race preview with its stars. Captures g0-g8.
        IEnumerator TunerGarageReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            Store.Change(p=>{foreach(var e in RaceCalendar.All)if(e.raceClass=="C"&&!p.podiums.Contains(e.id))p.podiums.Add(e.id);p.credits=Math.Max(p.credits,30000);return true;});
            var gio=residents["motors"];Place(gio+Vector3.forward*6);player.body.rotation=Quaternion.Euler(0,residentYaw["motors"]+180,0);yield return new WaitForSeconds(1.2f);
            Interact("motors");yield return new WaitForSeconds(.4f);ui.Capture(Path.Combine(folder,"g0-gio.png"));
            ui.InvokeChoice("Tuner Garage");yield return new WaitForSeconds(.4f);ui.Capture(Path.Combine(folder,"g1-free-tuner.png"));
            ui.InvokeChoice("Take the Sakura");yield return new WaitForSeconds(.6f);ui.Capture(Path.Combine(folder,"g2-garage.png"));
            ui.InvokeChoice("Browse Tuner bodies");yield return new WaitForSeconds(.9f);ui.Capture(Path.Combine(folder,"g3-tuner-bodies.png"));
            var hayate=GameObject.Find("BODY hayate");if(hayate!=null){EventSystem.current.SetSelectedGameObject(hayate);yield return new WaitForSeconds(1.0f);ui.Capture(Path.Combine(folder,"g4-hayate.png"));}
            bodyShop.Leave();Close();yield return new WaitForSeconds(.3f);
            cameraView.transform.position=player.transform.position+new Vector3(-4,2.2f,6);cameraView.transform.LookAt(player.transform.position+Vector3.up*.8f);yield return new WaitForSeconds(.6f);
            ui.Capture(Path.Combine(folder,"g5-sakura.png"));
            Interact("motors");yield return new WaitForSeconds(.3f);ui.InvokeChoice("Tuner races");yield return new WaitForSeconds(.4f);ui.Capture(Path.Combine(folder,"g6-tuner-races.png"));Close();
            OpenParts("hillcrest",PartsBrowser.Mode.Shop,Rosa);yield return new WaitForSeconds(.9f);ui.Capture(Path.Combine(folder,"g7-parts.png"));browser.Hide();yield return new WaitForSeconds(.3f);
            PreviewRace("b_valley");yield return new WaitForSeconds(1.2f);ui.Capture(Path.Combine(folder,"g8-preview.png"));
            bool ok=Store.Data.tunerStarter=="sakura"&&Store.Data.bodyId=="sakura"&&Store.Data.Loadout()["engine"]=="engine_b_twincam";
            Debug.Log("TUNER_GARAGE_REVIEW ok="+ok+" body="+Store.Data.bodyId+" open="+RaceCalendar.Open(Store.Data));Application.Quit(ok?0:2);
        }
        IEnumerator PaintShopReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            Store.Change(p=>{p.credits=Math.Max(p.credits,600);return true;});
            yield return new WaitForSeconds(.5f);OpenPaintShop(Dot);yield return new WaitForSeconds(.8f);
            ui.Capture(Path.Combine(folder,"p1-open.png"));
            var target=GameObject.Find("SWATCH Candy pink");EventSystem.current.SetSelectedGameObject(target);yield return new WaitForSeconds(.5f);
            ui.Capture(Path.Combine(folder,"p2-preview.png"));
            target.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();yield return new WaitForSeconds(.4f);
            ui.Capture(Path.Combine(folder,"p3-bought.png"));
            bool bought=Store.Data.paintIndex==5;paintShop.Leave();Close();yield return new WaitForSeconds(.3f);
            var rowan=GameObject.Find("Resident cafe");Place(Front("cafe",6));cameraView.transform.position=residents["cafe"]+new Vector3(-3,4,9);cameraView.transform.LookAt(residents["cafe"]+Vector3.up);
            rowan.GetComponent<NpcChatter>().TrySay(bubbles);yield return new WaitForSeconds(.6f);
            ui.Capture(Path.Combine(folder,"p4-bubble.png"));
            Debug.Log("PAINT_SHOP_REVIEW bought="+bought+" credits="+Store.Data.credits);Application.Quit(bought?0:2);
        }
        void Update()
        {
            if(minimap!=null&&(minimap.FullOpen||minimap.LastCloseFrame==Time.frameCount))return;
            if(photo!=null&&(photo.Active||photo.LastCloseFrame==Time.frameCount))return;
            if(pause!=null&&(pause.IsOpen||pause.LastCloseFrame==Time.frameCount)){if(player!=null)player.controlsBlocked=pause.IsOpen||riding||ui.IsOpen;return;}
            if(tuning.IsOpen||tuning.LastCloseFrame==Time.frameCount||testDrive.Active)return;
            if(player==null||reviewing||browser.IsOpen||racePreview.IsOpen||paintShop.IsOpen||bodyShop.IsOpen||racePreview.LastCloseFrame==Time.frameCount||browser.LastCloseFrame==Time.frameCount||paintShop.LastCloseFrame==Time.frameCount||bodyShop.LastCloseFrame==Time.frameCount)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            // Esc / Start pause; in a conversation Esc / B go back one level (the menu's own Back choice).
            bool esc=k?.escapeKey.wasPressedThisFrame==true,start=pad?.startButton.wasPressedThisFrame==true,east=pad?.buttonEast.wasPressedThisFrame==true;
            if(ui.IsOpen&&(esc||east)){if(!ui.Back())Close();}
            else if(!ui.IsOpen&&!riding&&(esc||start)){Pause();return;}
            if(ui.IsOpen||riding){RefreshHUD();return;}
            // Nearest resident within 7 m (a plain loop: this runs every frame).
            // Residents within 7 m count at any speed (so the prompt can ask you to slow down); you can
            // talk once stopped. Otherwise the nearest valley local or chatty parked car, once stopped.
            bool stopped=Mathf.Abs(player.speed)<=1.2f;string inRange=null;float nearestDistance=7;
            foreach(var p in residents){if(p.Key=="castle"&&DukeDrive.Away)continue;float d=Vector3.Distance(player.body.position,p.Value);if(d<=nearestDistance){nearestDistance=d;inRange=p.Key;}}
            nearest=stopped?inRange:null;
            nearFolk=null;nearChat=null;
            if(inRange==null&&stopped){
                float best=6.5f;
                foreach(var f in ValleyLife.People){if(f.car==null)continue;float d=Vector3.Distance(player.body.position,f.car.position);if(d<best){best=d;nearFolk=f;}}
                // Any other standing car (parked, forecourts, farm, campsite, stopped traffic).
                foreach(var c in NpcChatter.All){if(c==null||!c.CanTalk||c.transform==player.transform)continue;float d=Vector3.Distance(player.body.position,c.transform.position);if(d<best){best=d;nearChat=c;nearFolk=null;}}
            }
            // Zipline bays (castle Zip Tower and the lower-town station): stopped in the bay.
            // (Every line: the castle zipline and Quarry Hills' Blondin.)
            nearZip=null;nearLine=null;
            if(inRange==null&&stopped&&climbStart<0&&sprintStart<0&&(school==null||!school.Active)&&(blast==null||!blast.Active))foreach(var line in ZipLine.All) {
                if(line.Busy)continue;
                if(Vector3.Distance(player.body.position,line.top.bay)<6)nearZip=line.top;else if(Vector3.Distance(player.body.position,line.bottom.bay)<6)nearZip=line.bottom;
                if(nearZip!=null){nearLine=line;nearFolk=null;nearChat=null;break;}
            }
            // A town's notice board: stopped beside it.
            nearBoard=null;
            if(inRange==null&&stopped&&nearZip==null&&climbStart<0&&sprintStart<0&&galleryStart<0&&(school==null||!school.Active)&&(blast==null||!blast.Active))
                foreach(var b in Wayfinding.Boards)if(Vector3.Distance(player.body.position,b.at)<7.5f){nearBoard=b;nearFolk=null;nearChat=null;break;}
            // Cross / A (and E) talks while someone (or the zipline) is in reach; then it doesn't also shift up (Controls).
            Controls.InteractionReady=Time.unscaledTime>interactionReadyTime&&(nearest!=null||nearZip!=null||nearBoard!=null||nearFolk!=null||nearChat!=null);
            if(Time.unscaledTime>interactionReadyTime&&(k?.eKey.wasPressedThisFrame==true||pad?.buttonSouth.wasPressedThisFrame==true)){if(nearest!=null)Interact(nearest);else if(nearZip!=null)StartCoroutine(RideZip(nearLine,nearZip==nearLine.top));else if(nearBoard!=null)ReadBoard(nearBoard);else if(nearFolk!=null)TalkFolk(nearFolk);else if(nearChat!=null)TalkChatter(nearChat);}
            if(Controls.Pressed(Controls.Act.Recover))player.TryRecover();
            // The horn (H / L3, docs/51): folk nearby answer, some traffic beeps back, the train whistles.
            bool honk=Controls.Held(Controls.Act.Horn);playerHorn?.Hold(honk&&!player.controlsBlocked);
            if(honk&&!honking&&!player.controlsBlocked)HornHeard();honking=honk;
            // Photo mode (P / D-pad Up, docs/51).
            if(!player.controlsBlocked&&(k?.pKey.wasPressedThisFrame==true||pad?.dpad.up.wasPressedThisFrame==true)){photo.Open();return;}
            foreach(var pair in finds.ToArray()) {
                pair.Value.transform.Rotate(0,35*Time.deltaTime,0,Space.World);
                if(pair.Value.transform.childCount>0)pair.Value.transform.GetChild(0).localPosition=Vector3.up*(.12f*Mathf.Sin(Time.time*2.2f+pair.Key.Length*1.7f+pair.Key[^1]));
                if(Vector3.Distance(player.body.position,pair.Value.transform.position)<2.7f)Attempt(()=>{
                    var at=pair.Value.transform.position;
                    if(Store.Collect(pair.Key)){Destroy(pair.Value);finds.Remove(pair.Key);Confetti.Burst(at);
                        Func<string,bool> kind=Collectibles.IsHorseshoe(pair.Key)?Collectibles.IsHorseshoe:Collectibles.IsCarving(pair.Key)?Collectibles.IsCarving:Collectibles.IsStamp(pair.Key)?Collectibles.IsStamp:Collectibles.IsPostcard;
                        int got=0;foreach(string c in Store.Data.collected)if(kind(c))got++;
                        int reward=PaintCatalog.RewardFor(pair.Key);
                        if(reward>=0&&got==PaintCatalog.Reward(reward).count)Notify($"Full set! {PaintCatalog.All[reward].name} paint is yours, free at any paint counter · +50 credits");else
                        Notify(Collectibles.IsHorseshoe(pair.Key)?$"Lucky horseshoe {got} of {Collectibles.Horseshoes} found · +50 credits":Collectibles.IsCarving(pair.Key)?$"Slate carving {got} of {Collectibles.Carvings} found · +50 credits":Collectibles.IsStamp(pair.Key)?$"Hillcrest stamp {got} of {Collectibles.Stamps} found · +50 credits":$"Bayview postcard {got} of {Collectibles.Postcards} found · +50 credits");}});
            }
            // Countdowns hold the car still but let it rev (Richard, 1 Oct).
            player.holdStill=climbStart>=0&&Time.time<climbStart||trainRace!=null&&trainRace.Holding||school!=null&&school.Holding||activity!=null&&Time.time<activity.go||StopLook.Injected!=null;   // (--stop-look-review: braked on the hill)
            if(activity!=null&&activity.running!=null&&!activity.running())EndActivity();

            if(pin is Vector3 pinned&&Vector3.Distance(player.body.position,pinned)<20){pin=null;Notify("You're there: "+pinLabel+".");}
            UpdateClimb();UpdateSprint();UpdateGalleryClimb();UpdateVisit();RefreshHUD();SaveSpot();
        }
        // Runs every frame (from LateUpdate), reviews included: they capture the prompt and badges.
        void PromptFrame()
        {
            if(player==null||ui==null)return;
            bool stopped=Mathf.Abs(player.speed)<=1.2f;string inRange=null;float best=7;
            foreach(var p in residents){if(p.Key=="castle"&&DukeDrive.Away)continue;float d=Vector3.Distance(player.body.position,p.Value);if(d<=best){best=d;inRange=p.Key;}}
            UpdatePrompt(inRange,stopped);
        }
        void UpdatePrompt(string inRange,bool stopped)
        {
            string title=null,role=null;Color colour=Toy.Sun;GameObject roam=null;
            if(inRange!=null){var who=People[inRange];title=(stopped?"Talk to ":"Slow down to talk to ")+who.speaker;role=RoleOf(who.label);colour=who.bubble;}
            else if(nearZip!=null&&nearLine!=null&&!riding){title=nearZip.ride;role=nearLine.title;colour=Toy.Sun;}
            else if(nearBoard!=null){title="Read the notice board";role=nearBoard.town+" notices";colour=Toy.Sun;}
            else if(nearFolk!=null){title="Talk to "+nearFolk.name;role=nearFolk.title;roam=nearFolk.car.gameObject;colour=Toy.Sky;}
            else if(nearChat!=null){title="Talk to "+nearChat.speaker;role=string.IsNullOrEmpty(nearChat.title)?"Local":nearChat.title;roam=nearChat.gameObject;colour=nearChat.colour;}
            ui.Interaction(title,role,colour,stopped);
            foreach(var pair in badges){pair.Value.targeted=pair.Key==inRange;pair.Value.objective=pair.Key==objectiveResident&&climbStart<0;}
            // Locals and parked cars get a badge only while they are the one you can talk to.
            if(roam!=roamOwner){if(roamBadge!=null)Destroy(roamBadge);roamBadge=null;roamOwner=roam;if(roam!=null){roamBadge=TalkBadge.Add(roam,colour);roamBadge.onlyWhenTargeted=true;}}
            if(roamBadge!=null)roamBadge.targeted=true;
        }
        // "MARA / PIT STOP GARAGE" -> "Pit Stop Garage".
        static string RoleOf(string label)
        {
            int slash=label.IndexOf('/');string role=slash<0?label:label[(slash+1)..].Trim();
            var words=role.ToLowerInvariant().Split(' ');for(int i=0;i<words.Length;i++)if(words[i].Length>0)words[i]=char.ToUpperInvariant(words[i][0])+words[i][1..];
            return string.Join(" ",words);
        }
        // The current objective, with the distance to it when asked.
        string GoalText(bool distance)
        {
            bool mainland=player.transform.position.z<-452;
            string[] objectives={"Visit Mara at the Pit Stop garage","Deliver Mara's parcel to Pip at the lighthouse","Return to Mara for 350 credits",mainland?"Explore Hillcrest Valley · Vito runs the Valley Cup at the raceway":"Race Rowan, or cross the causeway to Hillcrest Valley"};
            string target=Store.Data.questStage==1?"post":Store.Data.questStage<3?"garage":mainland&&residents.ContainsKey("raceway")?"raceway":"cafe";objectiveResident=target;
            if(trainRace!=null&&trainRace.Active)return trainRace.Goal_Text(player.transform.position,distance);
            if(school!=null&&school.Active)return school.GoalText();
            if(galleryStart>=0)return $"Gallery Climb · {Time.time-galleryStart+galleryPenalty:0.0} s · par {GalleryClimb.Par:0} s · checkpoint {galleryCheckpoint+1}/{GalleryClimb.Checkpoints.Count-1}"+(distance?$"  ·  {Mathf.Max(0,GalleryClimb.Finish.y-player.transform.position.y):0} m to climb":"");
            if(blast!=null&&blast.Active)return blast.Elapsed<=0?"Blast Run · siren's going · get ready!":$"Blast Run · {blast.Elapsed:0.0} s · shot in {Mathf.Max(0,BlastRun.ShotTime-blast.Elapsed):0} s · up the haul road past Chapel Street";
            if(sprintStart>=0)return Time.time<sprintStart?"Quarry Sprint · get ready!":$"Quarry Sprint · {Time.time-sprintStart:0.0} s · par {SprintPar:0} s";
            if(climbStart>=0)return (Time.time<climbStart?"Castle Climb · get ready!":$"Castle Climb · {Time.time-climbStart:0.0} s · par {ClimbPar:0} s · to the castle")+(distance?$"  ·  {Vector3.Distance(player.transform.position,HillcrestRoute.CastleCourt):0} m":"");
            if(pin is Vector3 pinned)return "Pinned: "+pinLabel+(distance?$"  ·  {Vector3.Distance(player.transform.position,pinned):0} m":"");
            return objectives[Store.Data.questStage]+(distance?$"  ·  {Vector3.Distance(player.transform.position,residents[target]):0} m":"");
        }
        void RefreshHUD()
        {
            float now=Time.unscaledTime;if(controlsUntil<0)controlsUntil=now+15;
            string goal=GoalText(false);if(goal!=lastGoal&&climbStart<0&&sprintStart<0&&(school==null||!school.Active)&&(blast==null||!blast.Active)){lastGoal=goal;goalUntil=now+8;}
            if(Mathf.Abs(player.speed)<=1.2f){if(stoppedSince<0)stoppedSince=now;}else stoppedSince=-1;
            float stopped=stoppedSince<0?0:now-stoppedSince;
            bool timed=climbStart>=0||sprintStart>=0||galleryStart>=0||school!=null&&school.Active||blast!=null&&blast.Active;
            ui.Roam(GoalText(true),false,null,false,player);   // (the objective now lives on ActivityHud's quest card, doc 64)
            activityHud.Quest(activity!=null?null:GoalText(true),!ui.IsOpen&&(timed||now<goalUntil||stopped>1.5f));
        }
        // Pause hub > Settings: the Options screen (every setting, in tabs), then Save and Quit (added by the hub).
        List<(string label,Action act)> SettingsItems()=>new(){
            ("Options",()=>OptionsScreen.Open(()=>pause.ShowTab(PauseMenu.SettingsTab),Store.Data.Loadout().TryGetValue("engine",out var e)?e:null))};
        // Options applied live in free roam (the rest apply themselves in GraphicsOptions.Step).
        void OnOptionChanged(GraphicsOptions.Option o){if(o==GraphicsOptions.Grass)ApplyGraphics(o);}
        void OnDestroy(){GraphicsOptions.Changed-=OnOptionChanged;}
        // Pause hub > Car: the car as fitted.
        (string,Color,string,List<string>,int,string) CarSummary()
        {
            var fit=Store.Data.Loadout();var parts=new List<string>();
            foreach(string slot in EquipmentCatalog.All.slots)if(fit.TryGetValue(slot,out var id)){var part=EquipmentCatalog.Get(id);parts.Add($"{char.ToUpperInvariant(slot[0])+slot[1..]}:  {part.name}");}
            int paint=Mathf.Clamp(Store.Data.paintIndex,0,PaintCatalog.All.Length-1);
            return (Store.Data.bodyId,Paints[paint],PaintCatalog.All[paint].name,parts,CarPerformanceRating.Calculate(EquipmentCatalog.Stats(fit)),RaceCalendar.Progress(Store.Data));
        }
        // Marks for the minimap: residents in their colours, Pit Stops, both zipline stations, the objective.
        static readonly HashSet<string> PitHosts=new(){"garage","parts","motors","hcparts","hcbody","qgarage","qparts","qbody","vlgarage","vlparts","vlbody"};
        List<Minimap.Mark> MapMarks()
        {
            mapMarks.Clear();
            // Only what you've found shows (the objective always does).
            foreach(var r in residents)if(!PitHosts.Contains(r.Key)&&People.TryGetValue(r.Key,out var who)&&ExploredMap.Seen(r.Value))mapMarks.Add(new(r.Value,who.bubble,Minimap.Shape.Dot));
            foreach(var site in PitStop.Sites.Values)if(ExploredMap.Seen(site))mapMarks.Add(new(site,PitStop.Brand,Minimap.Shape.Service));
            foreach(var line in ZipLine.All)foreach(var bay in new[]{line.top.bay,line.bottom.bay})if(ExploredMap.Seen(bay))mapMarks.Add(new(bay,Toy.Sun,Minimap.Shape.Zip));
            foreach(var trap in SpeedTraps.All)if(ExploredMap.Seen(trap.at))mapMarks.Add(new(trap.at,(SpeedTraps.Record(Store.Data,trap.id)?.stars??0)>=3?Toy.Mint:Toy.Orange,Minimap.Shape.Trap));
            if(trainRace!=null&&trainRace.Active)mapMarks.Add(new(trainRace.Goal,Toy.Red,Minimap.Shape.Objective));
            else if(climbStart>=0)mapMarks.Add(new(HillcrestRoute.CastleCourt,Toy.Sun,Minimap.Shape.Objective));
            else if(pin is Vector3 pinned)mapMarks.Add(new(pinned,Toy.Sky,Minimap.Shape.Objective));
            else if(objectiveResident!=null&&residents.TryGetValue(objectiveResident,out var goal))mapMarks.Add(new(goal,Toy.Sun,Minimap.Shape.Objective));
            return mapMarks;
        }
        // --minimap-review: the corner map in three places, then the whole map; screenshots to --trial-captures.
        IEnumerator MinimapReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);RefreshHUD();
            var spots=new List<(string name,Vector3 at,float yaw)>{("m1-bayview",new Vector3(-40,0,-95),90f)};
            if(HillcrestTown.Spots.Count>0){var sq=HillcrestRoute.Square;spots.Add(("m2-hillcrest",new Vector3(sq.x-20,ValleyField.Height(sq.x-20,sq.z),sq.z),30f));
                var pad=HillcrestRoute.Paddock;spots.Add(("m3-raceway",new Vector3(pad.x,ValleyField.Height(pad.x,pad.z),pad.z),200f));}
            reviewing=false;
            foreach(var (name,at,yaw) in spots){player.body.rotation=Quaternion.Euler(0,yaw,0);Place(at);CameraPose(true);yield return new WaitForSeconds(1.2f);Toy.Capture(Path.Combine(folder,name+".png"));}
            minimap.OpenFull(true);yield return new WaitForSeconds(.6f);Toy.Capture(Path.Combine(folder,"m4-full-map.png"));minimap.OpenFull(false);
            Debug.Log("LITTLE_ROAD_MINIMAP_REVIEW done folder="+folder);Application.Quit(0);
        }
        // Fog of war: the minimap's standstill view around the car; a viewpoint reveals the country you can
        // see from it. The mask saves every 20 s when it has changed, and before races and quitting.
        static readonly (Vector3 at,float radius)[] Viewpoints={(HillcrestRoute.Vista,ExploredMap.ViewpointRadius),(HillcrestRoute.Lookout,ExploredMap.ViewpointRadius),(HillcrestRoute.CastleCourt,ExploredMap.CastleRadius)};
        void Explore()
        {
            if(player==null||!ExploredMap.Loaded)return;float now=Time.unscaledTime;
            if(now>nextReveal) {
                nextReveal=now+.25f;ExploredMap.Reveal(player.transform.position,ExploredMap.DriveRadius);
                foreach(var (at,radius) in Viewpoints)if(Vector3.Distance(player.transform.position,at)<30&&viewpointsSeen.Add(at.ToString()))ExploredMap.Reveal(at,radius);
                // The cairn on the quarry rim (the top of the Gallery Climb) shows you all of Quarry Hills (docs/60).
                if(QuarryHills.Enabled&&GalleryClimb.Checkpoints.Count>0&&Vector3.Distance(player.transform.position,GalleryClimb.Finish)<30&&viewpointsSeen.Add("rim")){ExploredMap.Reveal(GalleryClimb.Finish,RimReveal);Notify("The view from the rim: all of Quarry Hills is on your map");}
            }
            ExploredMap.Flush();
            if(ExploredMap.Dirty&&now>nextExploreSave){nextExploreSave=now+20;SaveExplored();}
        }
        void SaveExplored(){if(ExploredMap.Loaded&&ExploredMap.Dirty)Attempt(()=>Store.SaveExplored(ExploredMap.Save()));}
        void OnApplicationQuit(){SaveExplored();SaveSpot(true);}
        // ---------------------------------------------------------------- where you are, saved
        // Test and review runs neither restore nor save the spot (their saves are fixtures).
        bool resumed;float nextSpotSave;Vector3 lastSpot=new(1e6f,0,0);
        static bool Testing=>AdventureSession.Smoke||RaceTrial.Has("--world-review")||System.Environment.GetCommandLineArgs().Any(a=>a.StartsWith("--")&&(a.EndsWith("-test")||a.EndsWith("-review")||a.EndsWith("-audit")));
        // A spot is clear when there's solid ground (land, a road, a quay) within 3 m of it and nothing built where the car would stand.
        static bool ClearSpot(ref Vector3 at)
        {
            Physics.SyncTransforms();float best=float.MinValue;
            foreach(var h in Physics.RaycastAll(at+Vector3.up*4,Vector3.down,12))if(!h.collider.isTrigger&&h.collider.GetComponentInParent<RoadCar>()==null&&!h.collider.CompareTag("Respawn"))best=Mathf.Max(best,h.point.y);
            if(best==float.MinValue||Mathf.Abs(best-at.y)>3||best<CoastalLandscape.SeaLevel+.3f)return false;
            at.y=best;
            foreach(var c in Physics.OverlapBox(at+Vector3.up*1.1f,new Vector3(1.2f,.8f,2.3f)))if(!c.isTrigger&&c.CompareTag("Respawn")&&c.GetComponentInParent<RoadCar>()==null)return false;
            return true;
        }
        // Every 15 s on the move (and on quitting): the car's spot, when it's on the ground and not on a ride or in an event.
        void SaveSpot(bool now=false)
        {
            if(Testing||player==null||riding||AdventureSession.Racing)return;
            if(!now&&Time.unscaledTime<nextSpotSave)return;nextSpotSave=Time.unscaledTime+15;
            if(climbStart>=0||sprintStart>=0||galleryStart>=0||school!=null&&school.Active||trainRace!=null&&trainRace.Active||blast!=null&&blast.Active)return;
            var p=player.body.position;if(Vector3.Distance(p,lastSpot)<20&&!now)return;
            if(!Physics.Raycast(p+Vector3.up*.5f,Vector3.down,2.5f,~0,QueryTriggerInteraction.Ignore))return;   // airborne
            lastSpot=p;Attempt(()=>Store.SaveSpot(p,player.transform.eulerAngles.y));
        }
        // ---------------------------------------------------------------- back to a garage (pause menu)
        // The nearest Pit Stop garage you've been to (seen on the map); Bayview's is always open.
        (string name,Vector3 at,float yaw)? NearestGarage()
        {
            (string,Vector3,float)? best=null;float bd=float.MaxValue;
            foreach(var (key,name) in new[]{("bayview/garage","Bayview"),("hillcrest/garage","Hillcrest"),("quarry/garage","Quarry Hills"),("vinelands/garage","Vinelands")}) {
                if(!PitStop.Hosts.TryGetValue(key,out var host))continue;var at=GarageForecourt(host);
                if(key!="bayview/garage"&&!ExploredMap.Seen(at))continue;
                float d=Vector3.Distance(player.body.position,at);if(d<bd){bd=d;best=(name,at,host.yaw);}
            }
            return best;
        }
        string WarpLabel(){var g=NearestGarage();return g==null?null:$"Back to the {g.Value.name} Pit Stop";}
        void WarpToGarage()
        {
            var g=NearestGarage();if(g==null)return;
            if(climbStart>=0||sprintStart>=0||galleryStart>=0||school!=null&&school.Active||trainRace!=null&&trainRace.Active||blast!=null&&blast.Active||riding){Notify("Finish what you're doing first!");return;}
            pause.Close();StartCoroutine(Warp(g.Value.at,g.Value.yaw,$"Back at the {g.Value.name} Pit Stop"));
        }
        IEnumerator Warp(Vector3 at,float yaw,string note)
        {
            player.controlsBlocked=true;var fade=Fade.Get(transform);yield return fade.To(1,.4f);
            player.body.rotation=Quaternion.Euler(0,yaw+180,0);Place(at);yield return new WaitForSeconds(.3f);
            yield return fade.To(0,.5f);player.controlsBlocked=ui.IsOpen;Notify(note);
        }
        // --climb-review: the Hard Castle Climb set up, seen from the start line and three points up the route.
        // --duke-drive-review [--duke-shadow]: the Duke's drive from departure to parking, a chase shot every 10 s and a log
        // line every 5; with --duke-shadow the player's car follows 20 m behind him (the Shadow the Duke payout).
        IEnumerator DukeDriveReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);reviewing=true;
            var duke=DukeDrive.Instance;if(duke==null){Debug.LogError("LITTLE_ROAD_DUKE_REVIEW no duke");Application.Quit(2);yield break;}
            bool shadow=RaceTrial.Has("--duke-shadow");player.body.isKinematic=true;
            if(!shadow)player.body.position=HillcrestRoute.CastleCourt+new Vector3(0,30,0);
            duke.Depart();float t=0,nextShot=0,nextLog=0;int shot=0;
            while(duke.Driving&&t<400) {
                yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                if(shadow){var back=duke.transform.position-duke.transform.forward*20;back.y+=.4f;player.body.MovePosition(back);player.body.MoveRotation(duke.transform.rotation);}
                if(t>=nextLog){nextLog+=5;Debug.Log($"LITTLE_ROAD_DUKE_REVIEW t={t:0} s={duke.Along:0}/{duke.Length:0} speed={duke.Speed:0.0} gear={duke.Gear} rpm={duke.Rpm:0} at=({duke.transform.position.x:0},{duke.transform.position.y:0.0},{duke.transform.position.z:0})");}
                if(t>=nextShot){nextShot+=10;var d=duke.transform;cameraView.transform.position=d.position-d.forward*9+d.right*2.5f+Vector3.up*3.2f;cameraView.transform.LookAt(d.position+Vector3.up*.6f);
                    yield return null;Toy.Capture(Path.Combine(folder,$"d{shot++:00}-t{t:000}.png"));}
            }
            Debug.Log($"LITTLE_ROAD_DUKE_REVIEW done t={t:0.0} driving={duke.Driving} max={duke.MaxSpeed:0.0} toots={duke.Toots} folder={folder}");
            Application.Quit(duke.Driving?3:0);
        }
        IEnumerator ClimbReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            StartClimb(true);var held=player.body.position;float rpm=0;player.commandSource=c=>{if(c.gears!=null)rpm=Mathf.Max(rpm,c.gears.Rpm);return new Vector2(1,0);};   // floored through "get ready": held still, revving
            yield return new WaitForSeconds(1.5f);Toy.Capture(Path.Combine(folder,"c1-start.png"));yield return new WaitForSeconds(1.2f);
            var moved=player.body.position-held;moved.y=0;player.commandSource=null;string hold=$"countdown moved {moved.magnitude:0.00} m, revved {rpm:0} rpm";
            var line=CastleClimb.Line;int shot=2;
            foreach(float f in new[]{.3f,.5f,.7f,.86f,.97f}) {
                int i=Mathf.Clamp(Mathf.RoundToInt(line.Count*f),1,line.Count-2);var ahead=line[i+1]-line[i-1];ahead.y=0;
                player.body.rotation=Quaternion.LookRotation(ahead);Place(line[i]);yield return new WaitForSeconds(1.4f);Toy.Capture(Path.Combine(folder,$"c{shot++}-route-{Mathf.RoundToInt(f*100)}.png"));
            }
            {var court=HillcrestRoute.CastleCourt;reviewing=true;cameraView.transform.position=court+new Vector3(-70,30,-80);cameraView.transform.LookAt(court+Vector3.up*8);yield return new WaitForSeconds(.3f);Toy.Capture(Path.Combine(folder,$"c{shot++}-finish-beacon.png"));reviewing=false;}
            Debug.Log($"LITTLE_ROAD_CLIMB_REVIEW {hold} bollards={climbEvent.Bollards} obstacles={climbEvent.Obstacles} moved={climbEvent.Moved} folder={folder}");
            EndClimb();Application.Quit(0);
        }
        // --pause-review: the driving HUD, then every pause-hub tab (and the map hovering another region).
        // --zip-ride-review [--zip-down]: rides the castle zip line (up, or down) from its bay, a capture every 1.5 s to
        // --trial-captures, to check the riding camera (bug inbox #9: it sat behind the tower wall on the way up).
        IEnumerator ZipRideReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);var zip=ZipLine.Active;
            if(zip==null){Debug.LogError("LITTLE_ROAD_ZIP_RIDE_REVIEW no zip line");Application.Quit(2);yield break;}
            bool down=RaceTrial.Has("--zip-down");var station=down?zip.top:zip.bottom;
            player.body.rotation=Quaternion.Euler(0,station.facing,0);Place(station.bay);CameraPose(true);yield return new WaitForSeconds(1.5f);
            StartCoroutine(RideZip(zip,down));int n=0;
            do{yield return new WaitForSeconds(1.5f);Toy.Capture(Path.Combine(folder,$"zip-{(down?"down":"up")}-{n:00}.png"));n++;}while(riding&&n<60);
            Debug.Log($"LITTLE_ROAD_ZIP_RIDE_REVIEW done captures={n} folder={folder}");Application.Quit(0);
        }
        // --stop-look-review (with --stop-look): the car stopped on the Hillcrest mainline, the right stick held to swing the
        // camera round it; captures of the chase view, the side, the front and from above to --trial-captures.
        IEnumerator StopLookReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            if(!StopLook.Enabled){Debug.LogError("LITTLE_ROAD_STOP_LOOK_REVIEW needs --stop-look");Application.Quit(2);yield break;}
            var main=HillcrestRoute.Mainline;float s=main.Nearest(new Vector3(95,0,-300),out _);player.body.rotation=Quaternion.LookRotation(main.Forward(s));Place(main.At(s));
            StopLook.Injected=Vector2.zero;CameraPose(true);yield return new WaitForSeconds(2);
            void Shot(string name){Toy.Capture(Path.Combine(folder,$"stop-look-{name}.png"));var c=cameraView.transform.position-player.transform.position;
                Debug.Log($"LITTLE_ROAD_STOP_LOOK_REVIEW {name} speed={player.speed:0.00} yaw={StopLook.Now.yaw:0} pitch={StopLook.Now.pitch:0} weight={StopLook.Now.weight:0.00} camera={c.magnitude:0.0}m up={c.y:0.0}m");}
            Shot("0-chase");
            StopLook.Injected=Vector2.right;yield return new WaitForSeconds(1);StopLook.Injected=Vector2.zero;yield return new WaitForSeconds(1.5f);Shot("1-side");
            StopLook.Injected=Vector2.right;yield return new WaitForSeconds(1);StopLook.Injected=Vector2.zero;yield return new WaitForSeconds(1.5f);Shot("2-front");
            StopLook.Injected=Vector2.down;yield return new WaitForSeconds(1.5f);StopLook.Injected=Vector2.zero;yield return new WaitForSeconds(1.5f);Shot("3-above");
            StopLook.Injected=null;Debug.Log("LITTLE_ROAD_STOP_LOOK_REVIEW done folder="+folder);Application.Quit(0);
        }
        IEnumerator PauseReview()
        {
            string folder=RaceTrial.Arg("--trial-captures");Directory.CreateDirectory(folder);
            if(HillcrestTown.Spots.Count>0){var sq=HillcrestRoute.Square;player.body.rotation=Quaternion.Euler(0,30,0);Place(new Vector3(sq.x-20,ValleyField.Height(sq.x-20,sq.z),sq.z));}
            CameraPose(true);yield return new WaitForSeconds(2f);Toy.Capture(Path.Combine(folder,"p0-driving-hud.png"));
            Pause();yield return new WaitForSecondsRealtime(.5f);Toy.Capture(Path.Combine(folder,"p1-map.png"));
            pause.PointAt(new Vector3(-40,0,-100));yield return new WaitForSecondsRealtime(.3f);Toy.Capture(Path.Combine(folder,"p2-map-hover-bayview.png"));
            string[] names={"p3-collection","p4-car","p4b-records","p5-controls","p6-settings"};
            for(int i=1;i<=5;i++){pause.ShowTab(i);yield return new WaitForSecondsRealtime(.5f);Toy.Capture(Path.Combine(folder,names[i-1]+".png"));}
            pause.AskQuit();yield return new WaitForSecondsRealtime(.3f);Toy.Capture(Path.Combine(folder,"p7-quit-confirm.png"));
            pause.ShowSave();yield return new WaitForSecondsRealtime(.3f);Toy.Capture(Path.Combine(folder,"p7b-save-game.png"));
            pause.Close();yield return null;yield return null;
            if(player.controlsBlocked){Debug.LogError("LITTLE_ROAD_PAUSE_REVIEW car still blocked after closing the pause hub");Application.Quit(3);yield break;}
            Debug.Log("LITTLE_ROAD_PAUSE_REVIEW controls restored after close");
            Mara();yield return new WaitForSeconds(.5f);Toy.Capture(Path.Combine(folder,"p8-conversation.png"));
            Debug.Log("LITTLE_ROAD_PAUSE_REVIEW done folder="+folder);Application.Quit(0);
        }
        void LateUpdate()
        {
            Explore();
            bool paused=pause!=null&&pause.IsOpen;
            // Leaving the pause hub always hands the car back (Update may already have run this frame, so
            // it can't be left to Update: that race left the car blocked after closing the hub).
            if(paused!=wasPaused){wasPaused=paused;ui.SetHudVisible(!paused);if(!paused&&player!=null)player.controlsBlocked=riding||ui.IsOpen||(minimap!=null&&minimap.FullOpen);}
            minimap?.SetShown(GraphicsOptions.MinimapShown&&!reviewing&&!paused&&photo?.Active!=true&&!ui.IsOpen&&!browser.IsOpen&&!tuning.IsOpen&&!racePreview.IsOpen&&!paintShop.IsOpen&&!bodyShop.IsOpen);
            if(!reviewing&&player!=null&&cameraView!=null&&photo?.Active!=true)CameraPose(false);
            // The finish beacon (Richard, 1 Oct: you had to guess where the finish was).
            // (The castle's: 6 m from the court's centre towards the gate, on open paving clear of the donjon, its 10 m ring
            // wholly inside the 16 m finish.)
            Vector3? finish=trainRace!=null&&trainRace.Active?trainRace.Goal:climbStart>=0?HillcrestRoute.CastleCourt+Quaternion.Euler(0,172,0)*Vector3.forward*6:null;
            if(finish!=beaconAt){beaconAt=finish;beacon??=new GameObject("Finish beacon").AddComponent<FinishBeacon>();if(finish is Vector3 f)beacon.Show(f,trainRace!=null&&trainRace.Active?15:10);else beacon.Hide();}
            if(photo?.Active==true)ui.Interaction(null,null,Toy.Sun,false);else PromptFrame();
            // Clearer air over the mainland so the valley and ranges read at distance.
            if(cameraView!=null)RenderSettings.fogDensity=Mathf.Lerp(.0014f,.00026f,Mathf.InverseLerp(-300,-600,cameraView.transform.position.z));
        }
        void CameraPose(bool snap)
        {
            bool behind=!ui.IsOpen&&!browser.IsOpen&&DrivingViewInput.LookingBehind;
            bool flip=behind!=wasLookingBehind;snap|=flip;wasLookingBehind=behind;if(snap)WorldLook.Cut(cameraView);
            var viewingForward=player.transform.forward*(behind?-1:1);
            Vector3 target=player.transform.position+Vector3.up*.9f;
            // Riding the zipline: pull back to show the drop and the castle.
            Vector3 desired=player.transform.position-viewingForward*(riding?13f:6.5f)+Vector3.up*(riding?5.5f:3.4f);
            // Riding: near either end the car is in a walled bay inside the tower (its walls are drawn without colliders, so the
            // cast below can't keep the camera out of them: the view was the tower wall, bug inbox #9). Each bay opens towards the
            // cable, so there the camera moves out along the line, raised, looking back at the car; mid-cable it chases.
            if(riding&&ridingZip!=null) {
                var car=player.transform.position;var look=target;
                foreach(var (here,there) in new[]{(ridingZip.top,ridingZip.bottom),(ridingZip.bottom,ridingZip.top)}) {
                    var flat=new Vector2(car.x-here.bay.x,car.z-here.bay.z);float near=Mathf.InverseLerp(20,8,flat.magnitude);if(near<=0)continue;
                    // (The car is hoisted inside the tower from its foot to the deck opening, so frame the opening, where it shows.)
                    float deck=here.cable.y-ZipLine.Hang;var outward=there.bay-here.bay;outward.y=0;outward.Normalize();
                    desired=Vector3.Lerp(desired,new Vector3(here.bay.x,deck,here.bay.z)+outward*24+Vector3.up*4,near);
                    look=Vector3.Lerp(look,new Vector3(target.x,Mathf.Max(target.y,deck),target.z),near);
                }
                cameraView.transform.position=Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-Time.deltaTime*4));
                cameraView.transform.rotation=Quaternion.Slerp(cameraView.transform.rotation,Quaternion.LookRotation(look-cameraView.transform.position),1-Mathf.Exp(-Time.deltaTime*6));
                float rideFov=55+GraphicsOptions.FovOffset;cameraView.fieldOfView=Mathf.Lerp(cameraView.fieldOfView,rideFov,1-Mathf.Exp(-Time.deltaTime*3));
                return;
            }
            // --stop-look: stopped, the right stick swings the camera round the car (StopLook); the cast below still keeps
            // it out of walls and the ground.
            if(StopLook.Enabled){var off=desired-target;StopLook.Now=StopLook.Step(StopLook.Now,player.speed,StopLook.Stick(),!behind&&!ui.IsOpen&&!browser.IsOpen&&!player.controlsBlocked,Time.deltaTime,StopLook.Elevation(off));desired=target+StopLook.Orbit(off,StopLook.Now);}
            Vector3 delta=desired-target;
            foreach(var hit in Physics.SphereCastAll(target,.2f,delta.normalized,delta.magnitude))
                if(!hit.collider.transform.IsChildOf(player.transform)&&hit.distance>1)desired=target+delta.normalized*Mathf.Min(Vector3.Distance(target,desired),hit.distance-.25f);
            // Looking back the camera is locked 6.5 m ahead of the car. Flipping back puts the forward view
            // exactly where it was trailing (snapping to the untrailed spot then drifting back read as a zoom).
            if(behind)cameraView.transform.position=desired;
            else if(flip)cameraView.transform.position=desired+player.transform.TransformVector(chaseLag);
            else if(snap){cameraView.transform.position=desired;chaseLag=Vector3.zero;}
            else{cameraView.transform.position=Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-Time.deltaTime*8));chaseLag=player.transform.InverseTransformVector(cameraView.transform.position-desired);}
            cameraView.transform.LookAt(target+viewingForward*1.7f*(1-StopLook.Now.weight));   // (swung round, it looks at the car)
            float fov=WorldLook.SpeedFov(55+GraphicsOptions.FovOffset,player.speed);cameraView.fieldOfView=snap?fov:Mathf.Lerp(cameraView.fieldOfView,fov,1-Mathf.Exp(-Time.deltaTime*3));
            if(!snap&&!riding)CameraShake.Apply(cameraView);
        }
        void Notify(string value)=>ui.Toast(value);
        // Load profiling (W1 world tiles): seconds since the scene began loading, at each start-up stage.
        static void Mark(string stage){if(stage=="begin"&&RaceTrial.Has("--warm-probe")){LoopProbe.Install();LoopProbe.Watch(100000);}LoopProbe.Mark(stage);if(stage=="camera")LoopProbe.Flush("loading");Debug.Log($"LITTLE_ROAD_LOAD_MARK {stage} {RaceTrial.LoadTime}");}
        Horn playerHorn;bool honking;TrainRace trainRace;PhotoMode photo;FinishBeacon beacon;Vector3? beaconAt;
        // Reviews (--horn-test): one honk and its answers.
        public void TestHonk(){playerHorn?.Toot(.35f,0);HornHeard();}
        void HornHeard()
        {
            var at=player.body.position;int answered=0;
            // Sheep within earshot scatter (docs/60).
            foreach(var g in Grazer.All)if(g!=null&&(g.transform.position-at).sqrMagnitude<35*35)g.Startle(at);
            foreach(var c in NpcChatter.All.OrderBy(c=>c==null?1e9f:(c.transform.position-at).sqrMagnitude).ToArray()) {
                if(c==null||c.transform==player.transform)continue;float d=Vector3.Distance(c.transform.position,at);if(d>26||answered>=2)break;
                var train=ValleyTrain.Instance;if(train!=null&&c.transform.IsChildOf(train.transform))continue;
                if(c.TrySay(bubbles,NpcChatter.HornReplies))answered++;
                var cruiser=c.GetComponent<RoadCruiser>();if(cruiser!=null&&UnityEngine.Random.value<.6f)cruiser.TootBack();
            }
            var t=ValleyTrain.Instance;
            if(t!=null&&t.Loco!=null&&t.Visible&&Vector3.Distance(t.Loco.position,at)<110){t.Whistle();t.Loco.GetComponent<NpcChatter>()?.TrySay(bubbles,new[]{"TOOT TOOT! Hello down there!","Whooo-whoo! Wave to Bert!","TOOT! Race you to the station!"});}
        }
        // Behind the loading card, show the camera the world's main views for a frame each, so shaders and
        // pipeline states are prepared now rather than the first time you see them (drive benchmark db3:
        // 40-55 ms hitches on first sight of the valley and at the start).
        IEnumerator WarmUp()
        {
            if(RaceTrial.Has("--no-warm-up")||cameraView==null)yield break;
            if(WorldCache.Warmed("town")){Debug.Log("LITTLE_ROAD_WARM_UP skipped (this build has warmed on this machine before)");yield break;}
            var views=new List<(Vector3 at,Vector3 look)>{(new Vector3(-10,30,-10),new Vector3(-40,0,-100)),(new Vector3(90,20,-250),new Vector3(100,0,-450))};
            if(HillcrestTown.Spots.Count>0) {
                var c=HillcrestRoute.CastleCourt;var sq=HillcrestRoute.Square;
                views.Add((HillcrestRoute.Vista+Vector3.up*4,new Vector3(200,30,-1100)));views.Add((sq+new Vector3(-20,4,-6),sq+new Vector3(20,3,6)));
                views.Add((c+new Vector3(0,6,-30),c+new Vector3(0,12,20)));views.Add((HillcrestRoute.Paddock+new Vector3(20,15,20),HillcrestRoute.Paddock+new Vector3(-80,0,-120)));
                views.Add((new Vector3(HillcrestRoute.FarmCentre.x,40,HillcrestRoute.FarmCentre.y+120),new Vector3(HillcrestRoute.FarmCentre.x,0,HillcrestRoute.FarmCentre.y)));
                if(ZipLine.Active!=null)views.Add((ZipLine.Active.bottom.bay+new Vector3(20,20,20),ZipLine.Active.top.bay));
            }
            // Uncapped while warming (docs/72): with vertical sync each warm-up frame waited for the display.
            int vsync=QualitySettings.vSyncCount,cap=Application.targetFrameRate;
            {QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;}
            try {
            // Frames per view: 40 frames instead of 64 saved only 0.14 s (8 Oct, docs/72: the time is first-time uploads and
            // driver shader compiles, not frames), so the full counts stay as a guard against first-sight hitches.
            int look1=2,fade=3;
            // (--load-timing: each warm-up frame's length, to see what the time is.)
            bool timing=RaceTrial.Has("--load-timing");var frameClock=System.Diagnostics.Stopwatch.StartNew();var frames=new System.Text.StringBuilder();
            void Frame(){if(timing){frames.Append(frameClock.ElapsedMilliseconds).Append(' ');frameClock.Restart();}}
            // (--warm-probe: Unity's player-loop steps over 3 ms for the loading frames and the first warm-up frames, LoopProbe.)
            LoopProbe.Watch(4);LoopProbe.Mark("warm-up");
            foreach(var (at,look) in views){cameraView.transform.position=at;cameraView.transform.LookAt(look);WorldLook.Cut(cameraView);for(int i=0;i<look1;i++){yield return null;Frame();}}
            // Force LOD cross-fades both ways in each view so the fade variants of every shader (kit, trees,
            // cars) compile now: their first real fade, on the causeway, stalled 400-700 ms (drive benchmark db9).
            float bias=QualitySettings.lodBias;
            foreach(var (at,look) in views) {
                cameraView.transform.position=at;cameraView.transform.LookAt(look);WorldLook.Cut(cameraView);
                QualitySettings.lodBias=bias*.2f;for(int i=0;i<fade;i++){yield return null;Frame();}
                QualitySettings.lodBias=bias;for(int i=0;i<fade;i++){yield return null;Frame();}
            }
            QualitySettings.lodBias=bias;
            if(timing)Debug.Log($"LITTLE_ROAD_WARM_UP views={views.Count} frame_ms={frames}");
            WorldCache.MarkWarmed("town");
            } finally {QualitySettings.vSyncCount=vsync;Application.targetFrameRate=cap;}
        }
        // --town-print [file] (docs/72): once every background district has arrived, a hash of the town layer built on the world:
        // everyone standing in it (residents, townsfolk, parked cars) where they stand, the notice boards and fingerposts, the
        // speed traps' cameras and the activities. A background build must give the same town as one built at load.
        IEnumerator TownPrint()
        {
            while(Districts.Pending)yield return null;
            yield return null;
            var lines=new List<string>();
            // (Idling cars bob and turn to watch you, and traffic drives: those count by where they stand, or only by who they are.)
            foreach(var chatter in FindObjectsByType<NpcChatter>(FindObjectsInactive.Include)) {
                var p=chatter.transform.position;
                if(chatter.GetComponent<Rigidbody>()!=null||chatter.name.Contains("traffic")||chatter.name.Contains("dumper"))lines.Add($"npc {chatter.name} moving");
                else if(chatter.GetComponent<ResidentIdle>()!=null)lines.Add($"npc {chatter.name} ({p.x:F1}, {p.z:F1})");
                else lines.Add($"npc {chatter.name} {p:F2} {chatter.transform.eulerAngles.y:F1}");
            }
            foreach(var b in Wayfinding.Boards)lines.Add($"board {b.town} {b.at:F2}");
            foreach(var (at,text) in Wayfinding.Posts)lines.Add($"post {at:F2} {text}");
            foreach(var t in SpeedTraps.All)lines.Add($"trap {t.id} {(t.camera!=null?t.camera.position.ToString("F2"):"none")}");
            foreach(var r in residents)lines.Add($"resident {r.Key} {r.Value:F2}");
            lines.Add($"activities leap={leap!=null} blast={blast!=null} festival={FindAnyObjectByType<QuarryFestival>()!=null} folk={ValleyLife.People.Count}");
            foreach(var smoke in FindObjectsByType<ChimneySmoke>(FindObjectsInactive.Include))lines.Add($"chimneys {smoke.Count}");
            lines.Sort(StringComparer.Ordinal);ulong h=1469598103934665603;foreach(var l in lines){foreach(char c in l){h^=c;h*=1099511628211;}h^=10;h*=1099511628211;}
            string file=RaceTrial.Arg("--town-print");if(!string.IsNullOrEmpty(file)&&!file.StartsWith("--"))File.WriteAllText(file,string.Join("\n",lines));
            Debug.Log($"LITTLE_ROAD_TOWN_PRINT lines={lines.Count} hash={h:x16}");
            yield return new WaitForSeconds(1);Application.Quit(0);
        }
        // Everyone you can talk to and where they stand (the world map labels them).
        public List<(string name,string role,Vector3 at)> ResidentDirectory()
        {
            var list=new List<(string,string,Vector3)>();
            foreach(var r in residents)if(People.TryGetValue(r.Key,out var who))list.Add((who.speaker,RoleOf(who.label),r.Value));
            return list;
        }
        // Hillcrest Castle zipline: the car hangs in the trolley's cradle for the ride.
        ZipLine ridingZip;
        IEnumerator RideZip(ZipLine zip,bool down)
        {
            if(climbStart>=0){Notify("The zipline is closed · Castle Climb in progress");yield break;}   // no lift to the top during the climb
            riding=true;ridingZip=zip;nearZip=null;nearLine=null;player.controlsBlocked=true;player.body.isKinematic=true;
            Notify(down?"Hold on tight!":"Up we go!");
            try{yield return zip.Ride(player,down);}
            finally {
                player.body.isKinematic=false;player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;
                riding=false;ridingZip=null;player.controlsBlocked=ui.IsOpen;interactionReadyTime=Time.unscaledTime+.6f;
            }
            Notify(down?zip.bottom.arrive:zip.top.arrive);
        }
        void Attempt(Action action){try{action();}catch(Exception e){Debug.LogError(e);Show("Could not save that change",e.Message+"\nYour previous progress is still loaded.",new(){new("Back to the road",Close)});}}
        void Show(string title,string description,List<AdventureUI.Choice> choices){player.controlsBlocked=true;ui.Show(title,description,choices);}
        void Close(){if(browser.IsOpen)browser.Hide();ui.SetVisible(true);ui.Close();player.controlsBlocked=riding;interactionReadyTime=Time.unscaledTime+.25f;}
        void Pause(){player.controlsBlocked=true;pause.Open();}
        void ApplyGraphics(GraphicsOptions.Option o)
        {
            if(o==GraphicsOptions.AntiAliasing)WorldLook.Camera(cameraView);
            else if(o==GraphicsOptions.FrameRate)GraphicsOptions.ApplyPacing();
            else if(o==GraphicsOptions.Shadows)GraphicsOptions.ApplyShadows(false);
            else if(o==GraphicsOptions.Time)TimeOfDay.Apply();
            else if(o==GraphicsOptions.Grass){var grass=GetComponent<GrassField>();if(grass!=null)Destroy(grass);if(GrassField.Wanted)StartCoroutine(AddGrassNextFrame());}
        }
        System.Collections.IEnumerator AddGrassNextFrame(){yield return null;gameObject.AddComponent<GrassField>().Setup(cameraView);}
        public bool Interact(string id)
        {
            if(!residents.ContainsKey(id)||Vector3.Distance(player.body.position,residents[id])>7||Mathf.Abs(player.speed)>1.2f)return false;
            switch(id){case "garage":Mara();break;case "parts":Sid();break;case "hcparts":Rosa();break;case "hcbody":Lina();break;case "post":Pip();break;case "paint":Dot();break;case "motors":Gio();break;case "hillcafe":Fern();break;
                case "raceway":Vito();break;case "station":Stella();break;case "bayhalt":Stanley();break;case "castle":Duke();break;case "vista":Wren();break;case "lookout":Moss();break;case "rallyschool":Gwen();break;case "qgarage":Huw();break;case "qparts":Bethan();break;case "qbody":Nia();break;case "caban":Megan();break;case "slatequay":Mari();break;case "blast":Dai();break;case "farm":Giles();break;case "bwtuner":Jed();break;case "bwpub":Maggie();break;case "bwvicar":Clem();break;case "bwdairy":Peg();break;case "bwmarshal":Ted();break;case "vlpriest":Americo();break;case "vlclerk":Fatima();break;case "vlcafe":Joaquim();break;case "vlbaker":Padeira();break;case "vlfonte":Lurdes();break;case "vlgarage":Armando();break;case "vlparts":Ines();break;case "vlbody":Catia();break;case "vlbus":Vitor();break;case "vlfruta":Amelia();break;case "vlbarco":Fernando();break;case "vllodge":Graca();break;case "vltasca":Alberto();break;case "vlpombal":Teresa();break;case "vllajes":Rui();break;case "vlfojo":Antero();break;case "vlcasal":Manel();break;case "vlmirante":Ermelinda();break;case "vlbelvedere":Belmiro();break;case "vlveiga":Fausto();break;case "vlbombeiros":Nunes();break;case "vlescola":Luisa();break;case "vladega":Sofia();break;case "vlclube":Rogerio();break;case "vlaldeia":Ilda();break;case "vlcorvo":CorvoMan();break;default:Rowan();break;}
            return true;
        }
        void Mara()
        {
            var choices=new List<AdventureUI.Choice>();int stage=Store.Data.questStage;
            if(stage==0)choices.Add(new("I'll take the parcel",()=>Attempt(()=>{Store.AcceptDelivery();Close();Notify("Parcel aboard. Follow the coast road to Pip's lighthouse.");})));
            if(stage==2)choices.Add(new("Collect 350 credits",()=>Attempt(()=>{Store.ClaimDelivery();Mara();})));
            if(stage>=3)choices.Add(new("Race Bayview Coast · C",()=>PreviewRace("bayview",Mara)));
            if(stage>=3)AddTunerRaces(choices,"bayview",Mara);
            if(stage>=3)AddTunerGarage(choices,"Mara",Mara);
            choices.Add(new("Fit parts",()=>OpenParts("bayview",PartsBrowser.Mode.Fit,Mara)));
            choices.Add(new("Tune gearbox",()=>OpenTuning("bayview",Mara)));
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Mara)));
            choices.Add(new("Back to the road",Close));
            Show("Mara / Pit Stop Garage",stage==0?"Pip needs a parcel at the lighthouse. Take the coast road and look for the red lantern cap. I'll have 350 credits for you when you return.":stage==1?"Pip is by the lighthouse on the far side of the loop. There are postcards beside the road if you take a detour.":stage==2?"Pip sent word! The parcel arrived safely. Here's something towards making this car your own.":"A good car becomes your own, one improvement at a time. What shall we work on?",choices);
        }
        void Gio()
        {
            var choices=new List<AdventureUI.Choice>{new("Race the Valley Cup · C",()=>PreviewRace("hillcrest",Gio)),new("Tuner races",()=>TunerRaces("hillcrest",Gio)),new("Fit parts",()=>OpenParts("hillcrest",PartsBrowser.Mode.Fit,Gio)),new("Tune gearbox",()=>OpenTuning("hillcrest",Gio))};
            AddTunerGarage(choices,"Gio",Gio);
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Gio)));
            choices.Add(new("Back to the road",Close));
            Show("Gio / Pit Stop Garage","Benvenuto to the Pit Stop! Sign up for the Valley Cup right here, fit anything you own, or swap to another of your bodies. Rosa next door sells the parts, Lina the new rides.",choices);
        }
        // Pit Stop parts bays: the town's stock (same buy/fit menus as the garage).
        void Sid()=>Show("Sid / Pit Stop Parts","Every car starts somewhere! Engines, turbos, tyres, brakes, steering, gearboxes and chassis. Buy it here, then Mara fits it next door for free.",
            new(){new("Buy parts",()=>OpenParts("bayview",PartsBrowser.Mode.Shop,Sid)),new("Back to the road",Close)});
        void Rosa()=>Show("Rosa / Pit Stop Parts","Everything Sid stocks in Bayview, plus the valley's own: the Valley V6, twin turbos, hill grip and rally tyres, ceramic brakes, the rally rack, carbon chassis, six-speed box.",
            new(){new("Buy parts",()=>OpenParts("hillcrest",PartsBrowser.Mode.Shop,Rosa)),new("Back to the road",Close)});
        void Lina()=>Show("Lina / Pit Stop Body Shop","New car, same you! Every body here is looks only: your fitted parts decide the pace. Buy one and Gio swaps it in the garage bay whenever you like. Fresh paint too.",
            new(){new("Browse new bodies",OpenBodyShop),new("Fresh paint",()=>OpenPaintShop(Lina)),new("Back to the road",Close)});
        void OpenBodyShop()=>OpenBodyShop(Lina,"Gio");
        void OpenBodyShop(Action back,string fitter)
        {
            ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
            bodyShop.Open(Store,fitter=="Huw"?"quarry":"hillcrest",()=>Notify("New body in the garage bay · swap to it with "+fitter),()=>{ui.SetVisible(true);back();});
        }
        // Garage bay: swap between owned bodies (free, looks only), live on the car.
        void SwapBody(Action back)
        {
            var choices=new List<AdventureUI.Choice>();
            foreach(string id in Store.Data.bodies) {
                string b=id;bool driving=b==Store.Data.bodyId;
                choices.Add(new((driving?"Driving · ":"Swap to · ")+CarBodies.Name(b),()=>Attempt(()=>{
                    if(Store.SwitchBody(b)){player.ReplaceBody(b,Paints[Store.Data.paintIndex]);Notify("Fresh look · "+CarBodies.Name(b));}SwapBody(back);}),!driving));
            }
            choices.Add(new("Back",back));
            Show("Garage bay / your bodies","Every body drives the same: your fitted parts decide the pace. Pick the look.",choices);
        }
        void Fern()=>Show("Fern / Bella Vista","Best view in the valley! That's the castle up top, the raceway down by the lake and the wind farm on the far ridge. Vito runs the Valley Cup at the raceway: fast cars, big prizes.",
            new(){new("Back to the road",Close)});
        void Vito()=>Show("Vito / Hillcrest Raceway","Hillcrest Raceway · Class C · 24 cars · 2 laps\nRound the lake and past Vine Hill, flat out past the grandstand. The quickest Class C field yet: street and club kit, and the pole-sitter on Gio's valley parts. Every finish pays 150–750 credits.\n"+RaceCalendar.Progress(Store.Data),
            new(){new("Race Hillcrest Raceway · C",()=>PreviewRace("hillcrest",Vito)),new("Tuner races",()=>TunerRaces("hillcrest",Vito)),new("Back to the road",Close)});
        void Stella()=>Show("Stella / Hillcrest Station","The Hillcrest Line! Out over the valley viaduct, across the Silver Falls gorge, through the headland and along the sea viaduct to Bayview Halt. Wave at the driver, Bert loves that. I collect Hillcrest stamps too: eight are hidden around the valley.",
            Tickets("hillcrest"));
        void Stanley()=>Show("Stanley / Bayview Halt",Store.Data.collected.Contains(Collectibles.VisitHillcrest)?"Welcome to Bayview Halt! The Hillcrest Line runs from here, over the sea and through the headland to Hillcrest. One ticket, you and your car, straight to the station forecourt.":
            "Welcome to Bayview Halt! The line runs through the headland to Hillcrest, but it's best seen with your own eyes first. Drive over the causeway, then come back and ride with us any time.",Tickets("bayview"));
        // Tickets to every other open station on the line (fast travel).
        List<AdventureUI.Choice> Tickets(string here)
        {
            var choices=new List<AdventureUI.Choice>();
            // Beat the Train (docs/51): once the line is open both ways.
            bool toHillcrest=here=="bayview";
            // (Richard, 5 Oct: from Slate Quay it raced a train held at Hillcrest, kilometres away. The train only runs
            // between Bayview Halt and Hillcrest, so the race is only offered at those two.)
            if((here=="bayview"||here=="hillcrest")&&trainRace!=null&&trainRace.CanStart&&climbStart<0&&Store.Data.collected.Contains(Collectibles.VisitHillcrest)) {
                var rec=SpeedTraps.Record(Store.Data,TrainRace.Id(toHillcrest));
                choices.Add(new($"Race the train to {(toHillcrest?"Hillcrest":"Bayview Halt")}"+(rec!=null&&rec.best>0?$" · best {rec.best:0.0} s":$" · {TrainRace.Prize} cr"),()=>StartCoroutine(RaceTheTrain(toHillcrest))));
            }
            foreach(var st in TrainNetwork.All())if(st.id!=here&&st.open(Store.Data)){var s=st;choices.Add(new($"Ticket to {s.name} · {TrainNetwork.Fare} cr",()=>Attempt(()=>Travel(s)),Store.Data.credits>=TrainNetwork.Fare));}
            choices.Add(new("Back to the road",Close));return choices;
        }
        // Stanley or Stella radios Bert; if the train is out on the line it's at the platform after a moment's fade.
        IEnumerator RaceTheTrain(bool toHillcrest)
        {
            Close();var train=ValleyTrain.Instance;int from=toHillcrest?ValleyTrain.BayviewStop:ValleyTrain.HillcrestStop;
            if(train!=null&&!train.WaitingAt(from)) {
                player.controlsBlocked=true;var fade=Fade.Get(transform);yield return fade.To(1,.5f);
                train.Hold(from,6);yield return new WaitForSeconds(.3f);yield return fade.To(0,.5f);player.controlsBlocked=false;
            }
            trainRace.Begin(toHillcrest);
            if(trainRace.Active)BeginActivity(new Activity{title="Beat the Train",goal=$"Race Bert by road to {(toHillcrest?"Hillcrest station":"Bayview Halt")}. He's got a {trainRace.HeadStart:0}-second head start!",
                giver=toHillcrest?"bayhalt":"station",colour=Toy.Red,go=trainRace.GoAt,running=()=>trainRace.Active,exit=()=>trainRace.End(null),restart=()=>StartCoroutine(RaceTheTrain(toHillcrest)),
                detail=()=>$"{Vector3.Distance(player.body.position,trainRace.Goal):0} m to go  ·  the train takes {trainRace.TrainTime:0} s"});
        }
        void Travel(TrainNetwork.Station to)
        {
            if(!Store.Change(p=>{if(p.credits<TrainNetwork.Fare)return false;p.credits-=TrainNetwork.Fare;return true;})){Notify("Not enough credits for a ticket.");return;}
            Close();StartCoroutine(Ride(to));
        }
        IEnumerator Ride(TrainNetwork.Station to)
        {
            player.controlsBlocked=true;var fade=Fade.Get(transform);yield return fade.To(1,.6f);
            player.body.rotation=Quaternion.Euler(0,to.yaw,0);Place(to.arrival);yield return new WaitForSeconds(.4f);
            yield return fade.To(0,.6f);player.controlsBlocked=false;Notify("All change! · Welcome to "+to.name);
        }
        void Duke()=>Show("Duke Rolo / Hillcrest Castle","Welcome to my humble castle! Admiring my Regale? A prototype, darling: carbon, gold and a wing like a drawbridge, and the finest part money can buy in every slot. "+
            (Store.Data.collected.Contains(Collectibles.DukeBeaten)?"You beat me fair and square, and she's yours now. I still race a spare, mind. Rematch any time! ":"Race me round the raceway, if you dare. Beat me and she's yours. ")+
            "Now, fancy the Castle Climb? From the town gate, along the quay and up Via Crest to my courtyard, with the road closed and the whole town cheering. "+
            $"Beat {NormalPar:0} seconds for {NormalPrize} credits. Brave enough for Hard? Hay bales and cones on the road, {HardPar:0} seconds, {HardPrize} credits. Each prize pays once!",
            new(){new("The Duke's Challenge · 3 laps, you against the Regale",DukeChallenge),new($"Castle Climb · par {NormalPar:0} s",()=>StartClimb(false)),new($"Hard Castle Climb · par {HardPar:0} s",()=>StartClimb(true)),new("Back to the road",Close)});
        // The Duke's Challenge (docs/54; Richard, 2 Oct: "a great full circle moment at the end of the game... to be able
        // to come back and beat him like a final boss"). Open from the start: early on it shows how fast cars get.
        void DukeChallenge()
        {
            var roster=RaceRoster.DukeChallenge(Store.Data);var you=roster.Entries[0];var duke=roster.Entries[1];
            float yours=EquipmentCatalog.RacewayLapTime(you.Parts),his=EquipmentCatalog.RacewayLapTime(duke.Parts);
            bool won=Store.Data.collected.Contains(Collectibles.DukeBeaten);
            string verdict=his>=yours-.05f?"On paper we're level. It comes down to the driver, darling.":
                yours>his*1.6f?"Frankly, I could stop for tea halfway round and still win. But do come and see what a real car does!":
                yours>his*1.15f?"A spirited little car. She'll need a good deal more before she can live with the Regale.":"Ooh, close. Very close. This should be fun!";
            Show("The Duke's Challenge / Hillcrest Raceway",
                $"Three laps of my raceway, just you and me. I start on pole.\nYour car: rating {you.Rating}, an ideal lap of {yours:0.0} s.\nThe Regale: rating {duke.Rating}, an ideal lap of {his:0.0} s.\n{verdict}\n"+
                (won?"Rematches are for honour only.":$"Beat me and the Regale is yours, with {AdventureStore.DukePrize:N0} credits for your trouble. Lose, and it costs you nothing."),
                new(){new("Race the Duke",()=>Attempt(()=>StartDukeRace(roster))),new("Not today",Duke)});
        }
        void StartDukeRace(RaceRoster roster)
        {
            Close();player.controlsBlocked=true;
            FaceDuke();   // back to the castle courtyard afterwards
            SaveExplored();AdventureSession.StartRace(roster);
        }
        // The castle courtyard, facing the Duke.
        void FaceDuke()
        {
            if(!residents.TryGetValue("castle",out var at))return;
            var court=HillcrestRoute.CastleCourt;var toward=at-court;toward.y=0;var spot=court+toward.normalized*Mathf.Max(4,toward.magnitude-9);
            spot.y=at.y;AdventureSession.ReturnSpot=spot;AdventureSession.ReturnYaw=Quaternion.LookRotation(at-spot).eulerAngles.y;
        }
        // Castle Climb: a timed run from the Hillcrest town gate to the castle courtyard on a closed road
        // (CastleClimb lines it with bollards and moves the town behind them). Hard adds obstacles.
        // (Richard, 5 Oct: Hard was generous: 10 s off, and a real slalom in CastleClimb.)
        const float NormalPar=85,HardPar=70;const int NormalPrize=300,HardPrize=750;float climbStart=-1;bool climbHard;CastleClimb climbEvent;
        float ClimbPar=>climbHard?HardPar:NormalPar;
        void StartClimb(bool hard)
        {
            Close();climbHard=hard;DukeDrive.Instance?.Recall();var main=HillcrestRoute.Mainline;float d=main.Length-26;var start=main.At(d);
            player.body.rotation=Quaternion.LookRotation(main.Forward(d));Place(start);
            if(climbEvent==null)climbEvent=gameObject.AddComponent<CastleClimb>();climbEvent.Begin(hard,player.transform,bubbles);
            climbStart=Time.time+3;
            BeginActivity(new Activity{title=hard?"Hard Castle Climb":"Castle Climb",goal=hard?"Up through the town to the castle courtyard. Hay bales and cones in the way!":"Up through the town to the castle courtyard.",
                giver="castle",colour=Toy.Pink,go=climbStart,par=ClimbPar,running=()=>climbStart>=0,exit=EndClimb,restart=()=>StartClimb(hard),
                detail=()=>$"Par {ClimbPar:0} s  ·  {Vector3.Distance(player.transform.position,HillcrestRoute.CastleCourt):0} m to the courtyard"});
        }
        void EndClimb(){climbStart=-1;climbEvent?.End();}
        // The Quarry Sprint (docs/59): one timed lap of the rallycross loop on the quarry floor, from Gwen at the rally
        // school. Through the three quarter-lap gates in order, then back over the start line.
        const float SprintPar=95;const int SprintPrize=250;float sprintStart=-1;int sprintGate;RallySchool school;
        void Gwen()
        {
            string done=Store.Data.collected.Contains(Collectibles.QuarrySprint)?" You've beaten the Sprint's par already. Lovely.":$" Beat {SprintPar:0} seconds round the Sprint and I'll pay {SprintPrize} credits.";
            int golds=0;for(int i=0;i<RallySchool.Lessons.Length;i++){var r=RallySchool.Record(Store.Data,i);if(r!=null&&r.stars==3)golds++;}
            string schoolLine=RallySchool.AllGold(Store.Data)?" Gold in every lesson: Bethan at Grit & Gravel has the rally kit for you.":$" Six lessons, bronze to gold: get gold in all six and Bethan sells you the proper rally kit ({golds} of 6 so far).";
            Show("Gwen \"Granite\" Price / Rally School",(QuarryFestival.Festival?"It's Rally Day in town, and it's for you, cariad: there's a parade on the High Street! ":"")+"Croeso! Gravel's not tarmac, cariad: brake in a straight line, turn in early, and let her slide. And tyres! On the rallycross your road tyres grip about half as well as gravel ones."+schoolLine+done,
                new(){new("Race the Quarry Hills Rallycross · C",()=>PreviewRace("quarry",Gwen)),new("Tuner races",()=>TunerRaces("quarry",Gwen)),new("Rally licence",()=>OpenLicences(-1)),new($"Quarry Sprint · time trial (par {SprintPar:0} s)",StartSprint),new("Back to the road",Close)});
        }
        // The rally licence, Gran Turismo style (LicenceScreen; Richard, 4 Oct): the test list with each test's brief, medal
        // times and your best; every attempt ends on a result card (try again, next test, back to the licences), and
        // exiting the licences puts the car back beside Gwen. focus<0 opens on the first test without gold.
        List<LicenceScreen.Test> LicenceTests()
        {
            var list=new List<LicenceScreen.Test>();
            for(int i=0;i<RallySchool.Lessons.Length;i++){var l=RallySchool.Lessons[i];var r=RallySchool.Record(Store.Data,i);
                list.Add(new LicenceScreen.Test{name=l.name,how=l.how,gold=l.gold,silver=l.silver,bronze=l.bronze,best=r?.best??0,medal=r?.stars??0});}
            return list;
        }
        void OpenLicences(int focus)
        {
            school?.End();var tests=LicenceTests();
            if(focus<0){focus=tests.FindIndex(t=>t.medal<3);if(focus<0)focus=0;}
            player.controlsBlocked=true;
            ui.ShowLicences("Gwen's rally school · Quarry Hills",tests,RallySchool.Prizes,focus,i=>{ui.Close();StartLesson(i);},ExitLicences);
        }
        void ExitLicences()
        {
            Close();school?.End();
            if(QuarryHillsTown.Spots.TryGetValue("rallyschool",out var gwen)){player.body.rotation=Quaternion.Euler(0,90,0);Place(gwen.at+new Vector3(0,0,-8));}
        }
        void LessonFinished(LicenceScreen.Attempt a)
        {
            var tests=LicenceTests();int i=a.index;player.controlsBlocked=true;
            ui.ShowLicenceResult(tests[i],tests.Count,a,()=>{ui.Close();StartLesson(i);},i+1<tests.Count?()=>{ui.Close();StartLesson(i+1);}:null,()=>OpenLicences(i));
        }
        void StartLesson(int i)
        {
            if(school==null){school=gameObject.AddComponent<RallySchool>();school.Setup(player,()=>Store,Notify,(p,yaw)=>{player.body.rotation=Quaternion.Euler(0,yaw,0);Place(p);},LessonFinished);}
            sprintStart=-1;player.controlsBlocked=false;interactionReadyTime=Time.unscaledTime+.25f;school.Begin(i);
        }
        // Quarry Hills' Pit Stop (uniform with every town's) and its people.
        void Huw()
        {
            // (Richard, 4 Oct: the town's race at its garage, as Mara and Gio offer theirs.)
            var choices=new List<AdventureUI.Choice>{new("Race the Quarry Hills Rallycross · C",()=>PreviewRace("quarry",Huw)),new("Tuner races",()=>TunerRaces("quarry",Huw)),new("Fit parts",()=>OpenParts("quarry",PartsBrowser.Mode.Fit,Huw)),new("Tune gearbox",()=>OpenTuning("hillcrest",Huw))};
            AddTunerGarage(choices,"Huw",Huw);
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Huw)));
            choices.Add(new("Back to the road",Close));
            Show("Huw / Pit Stop Garage","Croeso i'r Pit Stop! Sign up for the Rallycross down on the quarry floor right here, or bring her in: I'll fit anything you own, set up the gearbox, or swap to another of your bodies. Bethan sells the parts next door, Nia the new rides.",choices);
        }
        void Bethan()=>Show("Bethan / Grit & Gravel",RallySchool.AllGold(Store.Data)?"Gwen says you're ready, so here it is: our gravel tyres and the gravel rack. Knobbly, loud and quick on the loose. Plus everything else you've found on your travels.":
            "Everything you've found on your travels, right here. Our own rally kit (gravel tyres, the gravel rack) comes once Gwen at the rally school says you're ready: gold in all six lessons.",
            new(){new("Buy parts",()=>OpenParts("quarry",PartsBrowser.Mode.Shop,Bethan)),new("Back to the road",Close)});
        void Nia()=>Show("Nia / Pit Stop Body Shop","New car, same you! Bodies are looks only: your parts decide the pace. Huw swaps them in the bay whenever you like. Fresh paint too. Slate grey's popular round here, funnily enough.",
            new(){new("Browse new bodies",()=>OpenBodyShop(Nia,"Huw")),new("Fresh paint",()=>OpenPaintShop(Nia)),new("Back to the road",Close)});
        void Megan()=>Show("Megan / Caban",!QuarryFestival.CabanOpen?"We're shut, cariad! Doors open at seven, kettle on at five past. "+(QuarryFestival.ChoirHours?"Choir's at the chapel tonight if you want a song.":"Mind how you go in the dark."):"The Caban's where the quarrymen ate, and argued, and sang. Still do, mostly the arguing. The town's betting on motorsport now the quarry's winding down: rally school, the Sprint, and there's talk of a proper stage up through the tips and the moor.",
            new(){new("Back to the road",Close)});
        // ---------------------------------------------------------------- running challenges (ActivityHud, doc 64)
        Activity activity;ActivityHud activityHud;FarmChallenges farm;
        void BeginActivity(Activity a){activity=a;activityHud.Begin(a);}
        // The running challenge's title, or null (the snap tool records it).
        public string ActivityTitle=>activity!=null&&(activity.running?.Invoke()??true)?activity.title:null;
        void EndActivity(){activity=null;activityHud.End();}
        // Pause > Exit challenge: the activity cleans up, then you're put back by whoever offered it.
        void ExitActivity()
        {
            var a=activity;if(a==null)return;a.exit?.Invoke();EndActivity();player.holdStill=false;
            if(a.giver!=null&&residents.TryGetValue(a.giver,out var at)){float yaw=residentYaw.TryGetValue(a.giver,out var y)?y:0;var front=at+Quaternion.Euler(0,yaw,0)*Vector3.forward*7;
                var face=at-front;face.y=0;player.body.rotation=Quaternion.LookRotation(face.sqrMagnitude>.01f?face:Vector3.forward);Place(front);}
            else if(a.returnTo is Vector3 back)Place(back);
            Notify($"{a.title} · called off");
        }
        void RestartActivity(){var a=activity;if(a==null)return;a.exit?.Invoke();EndActivity();a.restart?.Invoke();}

        // ---------------------------------------------------------------- Farmer Giles (FarmChallenges, doc 64)
        // Bramblewick's four (docs/82). Jed's parts, Peg's duel and the race come in step 5.
        void Jed()=>Show("Jed / Backyard Tuner","Parts Bayview's never heard of, my 'ansum: still unpacking the crates, mind. But the lanes race is on: three laps of the village and Church Lane, two abreast if you're brave. The hedges don't give way, and neither do I.",
            HybridPower.Enabled?new(){new("Race Bramblewick Lanes · C",()=>PreviewRace("bramblewick",Jed)),new("Buy parts",()=>OpenParts("bramblewick",PartsBrowser.Mode.Shop,Jed)),new("Back to the road",Close)}
                :new(){new("Race Bramblewick Lanes · C",()=>PreviewRace("bramblewick",Jed)),new("Back to the road",Close)});   // (the hybrid prototype: Jed's crates are unpacked, docs/84)
        void Maggie()=>Show("Maggie / The Plough","Welcome to The Plough, my lover! Four hundred years of thatch, cider from the orchard up the hill, and Peg's milk float parked outside every Friday. Mind the hanging baskets on your way out.",
            new(){new("Back to the road",Close)});
        void Armando()
        {
            var choices=new List<AdventureUI.Choice>{new("Fit parts",()=>OpenParts("vinelands",PartsBrowser.Mode.Fit,Armando)),new("Tune gearbox",()=>OpenTuning("hillcrest",Armando))};
            AddTunerGarage(choices,"Sr. Armando",Armando);
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Armando)));
            choices.Add(new("Back to the road",Close));
            Show("Sr. Armando / Pit Stop Garage","Bem-vindo! Rua Direita's garage since my father's day. Bring her in: I'll fit anything you own, set up the gearbox, or swap to another of your bodies. Ines sells the parts next door, Catia does the bodies and the paint. The pumps are self-service, mind.",choices);
        }
        void Ines()=>Show("Ines / Pit Stop Parts","Everything you've found on your travels, right here on the shelves. If it's not here, you haven't found it yet. Go and look!",
            new(){new("Buy parts",()=>OpenParts("vinelands",PartsBrowser.Mode.Shop,Ines)),new("Back to the road",Close)});
        void Catia()=>Show("Catia / Pit Stop Body Shop","New car, same you! Bodies are looks only: your parts decide the pace. Sr. Armando swaps them in the bay whenever you like. And paint: azulejo blue is very popular this year.",
            new(){new("Browse new bodies",()=>OpenBodyShop(Catia,"Sr. Armando")),new("Fresh paint",()=>OpenPaintShop(Catia)),new("Back to the road",Close)});
        void Vitor()=>Show("Sr. Vitor / Carreira","The bus can't turn in the Largo, so here's the end of the line: Rua da Feira. Twice a day to the Portela and the Adega, once to the Cais. On market day I can't move for vans.",
            new(){new("Back to the road",Close)});
        void Amelia()=>Show("Dona Amelia / Frutaria","Oranges from the yards up the hill, figs, grapes when it's the season, couves for the caldo verde. Wednesday's market is the big one: the Terreiro fills with stalls and half the valley comes up to shop.",
            new(){new("Back to the road",Close)});
        void Fernando()=>Show("Mestre Fernando / Barcos Rabelos","These boats carried the wine down to the coast for two hundred years: square sail, a long oar at the stern, and a lot of shouting. Now they carry visitors up to the gorge and back. Ponte Nova put the ferry out of work in fifty-seven; I was the ferryman's boy.",
            new(){new("Back to the road",Close)});
        void Graca()=>Show("Dona Graca / Lodge Sampaio","Welcome to Sampaio's. The wine comes down the Calcada from the quintas every September, rests here in the pipas, and goes out by boat, or these days by lorry over the bridge. The lodges are why there's a town on this river at all.",
            new(){new("Back to the road",Close)});
        void Alberto()=>Show("Sr. Alberto / Tasca do Rio","Sit down, sit down! Grilled sardines, bread from the forno up the hill, a jug of the green wine. The whole Cais eats here at one. In the evening the boatmen sing, and nobody can stop them.",
            new(){new("Back to the road",Close)});
        void Teresa()=>Show("Dona Teresa / Quinta do Pombal","My grandfather planted the cypresses the year the lodges first shipped to England. The old terraces are his; the straight rows down the hill are my son's, the modern way. Both make good wine. Only one makes good photographs.",
            new(){new("Back to the road",Close)});
        void Rui()=>Show("Engenheiro Rui / Quinta das Lajes","These vines are seventy, eighty, some a hundred years old: a dozen grapes mixed in every row, all picked together. Look at the walls: slate slabs, lajes, that's our name. Above us the terraces are empty since ninety. Too steep, too far, too many heirs.",
            new(){new("Back to the road",Close)});
        void Antero()=>Show("Sr. Antero / Quinta do Fojo","The house fell in after my father died. We moved into the stable and kept the lower vines; the top went back to broom. One day I'll put a roof on it. One day.",
            new(){new("Back to the road",Close)});
        void Manel()=>Show("Tio Manel / Casal do Meio","Half way between the town and the river, so the workers didn't have to climb every night. The eira's for the maize, the chapel's for the rest. The Estrada do Meio takes the tractors north to the Adega at harvest: you won't move for them in September.",
            new(){new("Back to the road",Close)});
        void Ermelinda()=>Show("Dona Ermelinda / Casa do Mirante","My grandfather went to Brazil with nothing and came back with coffee money and opinions. He built this up here so the whole town would see his tiles, and so he could see the whole valley. Both views, mind: the vines one side, the Vale the other.",
            new(){new("Back to the road",Close)});
        void Belmiro()=>Show("Sr. Belmiro / Torre do Belvedere","Two hundred and thirty metres. On a clear morning you see the river from the gorge to the sea bend, every terrace, every quinta. Up the other side, the Vale and the circuit. Come at sunset. Bring someone.",
            new(){new("Back to the road",Close)});
        void Fausto()=>Show("Sr. Fausto / Subida das Seis Voltas","The old Mirante road: six voltas from the river to the Belvedere, two point two kilometres, ten per cent on average and more at the top. Staggered starts, one car at a time, against the clock. The climb opens to the faster classes. Until then, go and learn the voltas: Rio, Lajes, Fojo, Oliveira, Capela, Moinho, and the Gancho at the top.",
            new(){new("Back to the road",Close)});
        void Nunes()=>Show("Comandante Nunes / Bombeiros Voluntarios","Every bombeiro here is a volunteer: the baker, the barber, two of the quinta lads. Summer's the worry: fires up on the mortorios when the broom dries out. The siren at noon is just the test. Probably.",
            new(){new("Back to the road",Close)});
        void Luisa()=>Show("Professora Luisa / Escola Basica da Cumeada","Twenty-two pupils from the town, the Casal and the far bank. The bus brings them over Ponte Nova. On Fridays we walk down the Calcada Velha to the river and back. The little ones count the steps. It's never the same number twice.",
            new(){new("Back to the road",Close)});
        void Sofia()=>Show("Engenheira Sofia / Adega Cooperativa","Founded in fifty-six: four hundred growers who couldn't each afford a lagar. In September the tractors queue down the Estrada do Meio from dawn, we weigh every load on the bascula, and the tanks fill for a month. The bottling plant across the road ships it all over the bridge.",
            new(){new("Back to the road",Close)});
        void Rogerio()=>Show("Sr. Rogerio / Clube Automovel da Cumeada","Farm lanes six days a week, a circuit on the seventh. The short lap is the club's, three laps for the little cars: Recta da Eira, the Eira Velha, over the Lomba, round by the Carvalhal, down the esses to the Gancho, and the chicane by the brook. The long lap takes the montado road through the cork oaks: that one's for the Tuners, once you've a class B car.\n"+RaceCalendar.Progress(Store.Data),
            RogerioChoices());
        List<AdventureUI.Choice> RogerioChoices(){var c=new List<AdventureUI.Choice>{new("Race the Circuito da Cumeada · C",()=>PreviewRace("vinelands",Rogerio))};AddTunerRaces(c,"vinelands-long",Rogerio);c.Add(new("Back to the road",Close));return c;}
        void Ilda()=>Show("Dona Ilda / Aldeia da Outra Banda","Twenty houses and eight of us all year: the rest are in France and Lisbon, and every one of them comes home for the festa of Sao Goncalo in August. Then the bunting goes up, the band crosses Ponte Nova, and you can hear us in the Largo. Look across the river from here: the whole town, every terrace, every window.",
            new(){new("Back to the road",Close)});
        void CorvoMan()=>Show("Sr. Joaquim Corvo / Quinta do Corvo","My grandfather's house. The roof went in sixty-two and nobody had the money to put it back. I come over the bridge on Saturdays and work the three hectares I've replanted. The fig in the yard was here before any of us; the Corvos are named after the crows that sit in it.",
            new(){new("Back to the road",Close)});
        void Americo()=>Show("Padre Americo / Igreja Matriz","Bom dia! A chapel stood here where the cattle road crossed the ridge, long before the vines. Go round behind the church: from the adro you can see the whole valley, every terrace down to the river. The best view in Vinelands, and it costs nothing.",
            new(){new("Back to the road",Close)});
        void Fatima()=>Show("Dona Fatima / Pacos do Concelho","Welcome to the town hall. Permits, stamps, the clock: all mine. Rua Direita runs north to the saddle and out to Bramblewick; the Calcada winds down to the quay in nine bends. Mind the bends. I have the forms for when you don't.",
            new(){new("Back to the road",Close)});
        void Joaquim()=>Show("Sr. Joaquim / Cafe Central","Sit, sit! Everyone in Vinelands crosses this square by noon, so you only need to wait here to meet them. The fish van comes on Thursdays and the whole Largo smells of sardines. Glorious.",
            new(){new("Back to the road",Close)});
        void Padeira()=>Show("Dona Rosa / O Forno","The oven's been lit since five: broa, the corn bread, for the whole street. Everyone brings their dough and their gossip. The lanes down here are narrow, querido. Go gently.",
            new(){new("Back to the road",Close)});
        void Lurdes()=>Show("Tia Lurdes / Lavadouro","The spring never stops, not even in August. We wash here, we talk here. Mostly we talk. The water comes off the ridge, under the Largo, and out of that spout, cold as the sea.",
            new(){new("Back to the road",Close)});
        void Clem()=>Show("Reverend Clem / St Michael's","Bless you for stopping. That tower's been watching over the vale since before there were roads in it. Do have a look round the churchyard, and go gently past the lychgate.",
            new(){new("Back to the road",Close)});
        void Peg()=>Show("Peg / Bramblewick Dairy","Forty years on the milk round and never spilt a pint. Reckon you're quick? Dairy Lane's mine, my lover. The float's on charge today, but I'll race you down it soon enough.",
            new(){new("Back to the road",Close)});
        void Ted()=>Show("Ted / Lanes Marshal","Welcome to the Beacon, the best seat on the lanes! Flat over the crest if you've the nerve, light as a feather at the top. Then Gallows, then all the way down Church Hill to the Green Hairpin: that's where they all try it on. Jed runs the race from the village.",
            new(){new("Back to the road",Close)});
        void Giles()
        {
            string Line(FarmChallenges.Kind k){var r=SpeedTraps.Record(Store.Data,FarmChallenges.Ids[(int)k]);return r!=null?$" · best {r.best:0}% · {r.stars}/3 stars":$" · {FarmChallenges.Limit(k):0} s";}
            Show("Farmer Giles / Golden Acre Farm","Harvest's due and the combine's broke! I'll bolt a header on your bumper: cut as much of that wheat as you can before the clock runs out. Or there's the orchard: give the apple trees a good shake. Well, a knock. Stars pay, mind!",
                new(){new("The Harvest"+Line(FarmChallenges.Kind.Harvest),()=>StartFarm(FarmChallenges.Kind.Harvest)),new("Apple Shake"+Line(FarmChallenges.Kind.Apples),()=>StartFarm(FarmChallenges.Kind.Apples)),new("Back to the road",Close)});
        }
        void StartFarm(FarmChallenges.Kind k)
        {
            Close();school?.End();var (at,yaw)=FarmChallenges.Start(k);player.body.rotation=Quaternion.Euler(0,yaw,0);Place(at);farm.Begin(k,player);
            bool harvest=k==FarmChallenges.Kind.Harvest;
            BeginActivity(new Activity{title=FarmChallenges.Name(k),goal=harvest?"Cut the wheat with Giles's header: drive the field in rows, and don't miss a strip!":"Knock the apple trees down! Weave through the rows.",
                giver="farm",colour=harvest?Toy.Gold:Toy.Red,go=farm.GoAt,limit=FarmChallenges.Limit(k),marks=FarmChallenges.Marks(k),progress=()=>farm.Progress,
                progressLabel=harvest?"harvested":"of the trees down",running=()=>farm.Active,exit=farm.Cancel,restart=()=>StartFarm(k)});
        }
        void FarmFinished(FarmChallenges.Kind k,float result,int stars)
        {
            Confetti.Burst(player.body.position+Vector3.up*2);
            Attempt(()=>{int paid=Store.RecordTrap(FarmChallenges.Ids[(int)k],Mathf.Round(result*1000)/10,stars,FarmChallenges.Prizes);var best=SpeedTraps.Record(Store.Data,FarmChallenges.Ids[(int)k]);
                ui.Card($"{result*100:0}% {(k==FarmChallenges.Kind.Harvest?"HARVESTED":"OF THE ORCHARD DOWN")}",$"{new string('★',stars)}{new string('☆',3-stars)}   best {best?.best??0:0}%"+(paid>0?$"   +{paid} credits":"")+(stars<3?$"   next star at {FarmChallenges.Marks(k)[stars]*100:0}%":""),k==FarmChallenges.Kind.Harvest?Toy.Gold:Toy.Red,FarmChallenges.Name(k).ToUpperInvariant());});
        }

        // ---------------------------------------------------------------- the Blast Run (BlastRun, doc 63)
        BlastRun blast;TipLeap leap;
        void Dai()
        {
            var r=Store.Data.traps?.Find(x=>x.id==BlastRun.Id);bool beaten=Store.Data.collected.Contains(Collectibles.BlastRunWin);
            string record=r!=null?$" Your best's {r.best:0.0} seconds.":"";
            Show("Dai the Dumper / Quarry Floor",(beaten?"You again! You've had my money, butt. Fancy another go, for the glory?":
                    $"Shot's set in the face up there. When the siren goes you've got {BlastRun.ShotTime:0} seconds to be up the haul road, over the top and past Chapel Street, or you're eating slate dust. I'll race you up. Beat me out and there's {BlastRun.Prize} credits in it.")+
                $" Don't come back down after, mind: the shot brings the cutting down on the road.{record}",
                new(){new($"Blast Run · beat the shot ({BlastRun.ShotTime:0} s) and Dai",StartBlast),new("Back to the road",Close)});
        }
        void StartBlast()
        {
            Close();school?.End();sprintStart=-1;var (at,rot)=BlastRun.PlayerStart;player.body.rotation=rot;Place(at);
            blast.Begin(player,GameObject.Find("Resident blast"));
            BeginActivity(new Activity{title="Blast Run",goal="Up the haul road and past Chapel Street before the shot. Beat Dai for his money!",giver="blast",colour=Toy.Red,
                go=Time.time+BlastRun.Countdown,limit=BlastRun.ShotTime,running=()=>blast.Active,exit=blast.Cancel,restart=StartBlast,progressLabel="of the way out",
                progress=()=>{var e=BlastRun.Path[^1];var s0=BlastRun.Path[0];return 1-Vector3.Distance(player.body.position,e)/Mathf.Max(1,Vector3.Distance(s0,e));},
                detail=()=>$"Dai's time {BlastRun.DaiTime:0} s  ·  get past Chapel Street"});
        }
        void BlastCrossed(float t,bool beatDai)
        {
            Confetti.Burst(player.body.position+Vector3.up*2);
            Attempt(()=>{Store.RecordTime(BlastRun.Id,t,0);
                if(beatDai&&Store.ClaimTrial(Collectibles.BlastRunWin,BlastRun.Prize))Notify($"Blast Run {t:0.0} s · Out, and ahead of Dai! +{BlastRun.Prize} credits");
                else Notify($"Blast Run {t:0.0} s · Out! "+(beatDai?"Ahead of Dai again. Wait for it...":$"Dai beat you up ({BlastRun.DaiTime:0} s). Wait for it..."));});
        }
        void BlastShot(bool caught)
        {
            if(caught)Notify("BOOM! The dust's got you. Dai: \"Told you, butt. Siren means GO.\"");
            else Notify("BOOM! Look back down the haul road...");
        }
        void Mari()=>Show("Mari / Slate Quay Station","Mari, from the engine shed. The slate trains come down to the quay by gravity and go back up behind the engine, and the Pine & Peak line runs passengers through to Hillcrest and Bayview Halt. One ticket, you and your car.",
            Tickets("quarry"));
        // The Gallery Climb (GalleryClimb): starts itself when you pass the bottom flags heading up; ends at the top flags.
        // Checkpoints split the time; go over the edge (below the last checkpoint's shelf) and you're put back there, +5 s.
        float galleryStart=-1,galleryPenalty;int galleryCheckpoint;Vector3 galleryLast;
        void UpdateGalleryClimb()
        {
            if(!GalleryClimb.Built||riding)return;var p=player.body.position;var moved=(p-galleryLast)/Mathf.Max(Time.deltaTime,1e-3f);galleryLast=p;
            if(galleryStart<0) {
                if(climbStart>=0||sprintStart>=0||school!=null&&school.Active||blast!=null&&blast.Active)return;
                var d=p-GalleryClimb.Start;d.y=0;var v=moved;v.y=0;
                if(d.magnitude<5&&Mathf.Abs(p.y-GalleryClimb.Start.y)<3&&Vector3.Dot(v,GalleryClimb.StartDir)>3){galleryStart=Time.time;galleryPenalty=0;galleryCheckpoint=-1;
                    BeginActivity(new Activity{title="Gallery Climb",goal="Twelve benches to the rim.",colour=Toy.Mint,go=Time.time,par=GalleryClimb.Par,running=()=>galleryStart>=0,exit=()=>galleryStart=-1,
                        returnTo=GalleryClimb.Start,detail=()=>$"Par {GalleryClimb.Par:0} s  ·  checkpoint {galleryCheckpoint+2} of {GalleryClimb.Checkpoints.Count}"+(galleryPenalty>0?$"  ·  +{galleryPenalty:0} s":"")});}
                return;
            }
            float t=Time.time-galleryStart+galleryPenalty;var cps=GalleryClimb.Checkpoints;
            if(t>600){galleryStart=-1;Notify("Gallery Climb · out of time. Back to the bottom flags for another go.");return;}
            // The next checkpoint: through its gate (within 8 m, at its height).
            int next=galleryCheckpoint+1;
            if(next<cps.Count-1){var c=cps[next].at;var dc=p-c;dc.y=0;if(dc.magnitude<8&&Mathf.Abs(p.y-c.y)<4){galleryCheckpoint=next;Notify($"Gallery Climb · checkpoint {next+1} of {cps.Count-1} · {t:0.0} s");}}
            // Over the edge: back to the last checkpoint (or the start).
            float floor=galleryCheckpoint<0?GalleryClimb.Start.y-6:cps[galleryCheckpoint].floor;
            if(p.y<floor) {
                var at=galleryCheckpoint<0?GalleryClimb.Start:cps[galleryCheckpoint].at;var dir=galleryCheckpoint<0?GalleryClimb.StartDir:cps[galleryCheckpoint].dir;
                galleryPenalty+=5;player.body.rotation=Quaternion.LookRotation(dir);Place(at);Notify("Over the edge! Back to the checkpoint · +5 s");return;
            }
            var f=p-GalleryClimb.Finish;f.y=0;
            if(f.magnitude<9&&p.y>GalleryClimb.Finish.y-3) {
                galleryStart=-1;Confetti.Burst(p+Vector3.up*2);
                Attempt(()=>{int paid=t<=GalleryClimb.Par?Store.RecordTime(GalleryClimb.Id,t,GalleryClimb.Prize):Store.RecordTime(GalleryClimb.Id,t,0);
                    Notify($"Gallery Climb {t:0.0} s · "+(t<=GalleryClimb.Par?(paid>0?$"under par! +{paid} credits":"under par again!"):$"par is {GalleryClimb.Par:0} s. Again?"));});
            }
        }
        void StartSprint()
        {
            Close();school?.End();var loop=QuarryFloor.Loop;int s0=HillcrestRoute.Index(loop,new Vector3(-2335,0,-2935));int back=(s0-12+loop.Length)%loop.Length;
            var f=loop[(s0+2)%loop.Length]-loop[(s0+loop.Length-2)%loop.Length];f.y=0;
            player.body.rotation=Quaternion.LookRotation(f.normalized);Place(loop[back]);
            sprintGate=0;sprintStart=Time.time+3;
            BeginActivity(new Activity{title="Quarry Sprint",goal="One lap of the rallycross loop: through the three gates, back over the line.",giver="rallyschool",colour=Toy.Mint,
                go=sprintStart,par=SprintPar,running=()=>sprintStart>=0,exit=()=>sprintStart=-1,restart=StartSprint,detail=()=>$"Par {SprintPar:0} s  ·  gate {Mathf.Min(sprintGate+1,3)} of 3"+(sprintGate>=3?"  ·  to the line!":"")});
        }
        void UpdateSprint()
        {
            if(sprintStart<0||Time.time<sprintStart)return;float t=Time.time-sprintStart;
            if(t>200){sprintStart=-1;Notify("Quarry Sprint · Out of time. Gwen's waiting for another go!");return;}
            var loop=QuarryFloor.Loop;int n=loop.Length,s0=HillcrestRoute.Index(loop,new Vector3(-2335,0,-2935));
            var p=player.body.position;
            if(sprintGate<3){var gate=loop[(s0+n*(sprintGate+1)/4)%n];if(Vector2.Distance(new(p.x,p.z),new(gate.x,gate.z))<14){sprintGate++;Notify($"Quarry Sprint · {t:0.0} s");}return;}
            if(Vector2.Distance(new(p.x,p.z),new(loop[s0].x,loop[s0].z))<12) {
                sprintStart=-1;bool beat=t<=SprintPar;Confetti.Burst(p+Vector3.up*2);
                Attempt(()=>{if(beat&&Store.ClaimTrial(Collectibles.QuarrySprint,SprintPrize))Notify($"Quarry Sprint {t:0.0} s · Under par! +{SprintPrize} credits");else Notify($"Quarry Sprint {t:0.0} s · "+(beat?"Under par again. Da iawn!":$"Par is {SprintPar:0} s. Again?"));});
            }
        }
        void UpdateVisit()
        {
            if(player.transform.position.z<-620&&!Store.Data.collected.Contains(Collectibles.VisitHillcrest))Attempt(()=>Store.ClaimTrial(Collectibles.VisitHillcrest,0));
        }
        void UpdateClimb()
        {
            if(climbStart<0||Time.time<climbStart)return;float t=Time.time-climbStart;
            if(t>240){EndClimb();Notify("Castle Climb · Out of time. Try again with Duke Rolo!");return;}
            if(Vector3.Distance(player.body.position,HillcrestRoute.CastleCourt)<16){
                float par=ClimbPar;int prize=climbHard?HardPrize:NormalPrize;string id=climbHard?Collectibles.CastleTrialHard:Collectibles.CastleTrial,name=climbHard?"Hard Castle Climb":"Castle Climb";
                EndClimb();bool beat=t<=par;Confetti.Burst(player.body.position+Vector3.up*2);
                Attempt(()=>{if(beat&&Store.ClaimTrial(id,prize))Notify($"{name} {t:0.0} s · Under par! +{prize} credits");else Notify($"{name} {t:0.0} s · "+(beat?"Under par again, bravo!":$"Par is {par:0} s. Again?"));});
            }
        }
        void Wren()=>Show("Wren / Valley Viewpoint","Wow, right?! Hillcrest on the hill, the castle on top, the river winding off to the falls... and the mountains just keep going. I've been here all day and I'm still not bored.",
            new(){new("Back to the road",Close)});
        void Moss()=>Show("Moss / Silver Falls","Shh, the fish are biting. The river drops right over the edge there, into the gorge, and the train rattles across just below. Best spot in the valley, if you ask me.",
            new(){new("Back to the road",Close)});
        void Dot()=>Show("Dot / Bayview Paints","Sixteen shades, one coat, no drips! Every colour is 150–200 credits. Pick one and I'll have you gleaming before you can say 'wet paint'.",
            new(){new("Browse the paint colours",()=>OpenPaintShop(Dot)),new("Back to the road",Close)});
        void OpenPaintShop(Action back)
        {
            ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
            paintShop.Open(Store,_=>{ApplyEquipment();Notify("Fresh paint · "+PaintCatalog.All[Store.Data.paintIndex].name);},()=>{ui.SetVisible(true);back();});
        }
        void Pip()
        {
            var choices=new List<AdventureUI.Choice>();if(Store.Data.questStage==1)choices.Add(new("Deliver Mara's parcel",()=>Attempt(()=>{Store.Deliver();Close();Notify("Delivery complete. Return to Mara for your reward.");})));
            // Pip's photo requests (docs/51).
            var wanted=PhotoRequests.All.FindAll(r=>!Store.Data.collected.Contains(r.id)).ConvertAll(r=>r.name.ToLowerInvariant());
            string photos=wanted.Count==0?" And thank you for all the photos! Every one's on a postcard now.":$" I'm making new postcards, too: take photos for me ({(InputGlyphs.Current==InputGlyphs.Family.Keyboard?"P":"D-pad Up")}, photo mode) of {string.Join(", ",wanted)}. {PhotoRequests.Prize} credits each!";
            choices.Add(new("Back to the road",Close));Show("Pip / Lighthouse Post","You found the lighthouse! Some drivers only see the road. Others find the little places beside it. Keep an eye out for Bayview postcards."+photos,choices);
        }
        void Rowan()=>Show("Rowan / The Daily Mile","Bayview Coast · Class C · 24 cars · 3 laps\nEvery valid finish pays 100–500 credits. Rival equipment stays fixed as you improve your car. Across the causeway, Hillcrest Raceway's Valley Cup is a quicker Class C race for bigger prizes.\n"+RaceCalendar.Progress(Store.Data),new(){new("Race Bayview Coast · C",()=>PreviewRace("bayview")),new("Back to the road",Close)});
        void PreviewRace(string track)=>PreviewRace(track,track=="hillcrest"?Vito:track=="quarry"?Gwen:track=="bramblewick"?Jed:track=="vinelands"?Rogerio:Rowan);
        // Class B (docs/67): once it opens, each town's garage offers its Tuner races, easiest first, with their stars
        // and a tick for those you've won (each win's stars count once towards the Tuner parts).
        // The Tuner Garage (docs/67): at every town's garage bay once class B is open. The free Tuner first (a choice of
        // three, with the seven Good Tuner parts fitted), then the Tuner bodies, all sold at every Pit Stop.
        void AddTunerGarage(List<AdventureUI.Choice> choices,string fitter,Action back)
        {
            if(RaceCalendar.Rank(RaceCalendar.Open(Store.Data))<RaceCalendar.Rank("B"))return;
            choices.Insert(0,new(string.IsNullOrEmpty(Store.Data.tunerStarter)?"Tuner Garage · your free Tuner!":"Tuner Garage",()=>TunerGarage(fitter,back)));
        }
        void TunerGarage(string fitter,Action back)
        {
            var choices=new List<AdventureUI.Choice>();string text;
            if(string.IsNullOrEmpty(Store.Data.tunerStarter)) {
                foreach(var id in CarBodies.TunerStarters){string b=id;choices.Add(new($"Take the {CarBodies.Name(b)} · {TunerDrive(b)}",()=>Attempt(()=>{
                    if(Store.ClaimTunerStarter(b)){player.ReplaceBody(b,Paints[Store.Data.paintIndex]);Notify($"Your {CarBodies.Name(b)} · Tuner parts fitted");}TunerGarage(fitter,back);})));}
                text="Class B is open, and your first Tuner is on us! Pick one: each comes with the seven Good Tuner parts fitted. Tuner parts fit Tuner bodies only.";
            } else {
                choices.Add(new("Browse Tuner bodies",()=>{ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
                    bodyShop.Open(Store,"tuner",()=>Notify("New Tuner in the garage bay · swap to it with "+fitter),()=>{ui.SetVisible(true);TunerGarage(fitter,back);});}));
                int next=PartDiscovery.NextStars(Store.Data,"B");
                text=$"{RaceCalendar.StarsWon(Store.Data,"B")}★ won at Tuner races."+(next>0?$" The next Tuner part unlocks at {next}★: harder races give more stars.":" Every Tuner part is unlocked.")+" Tuner parts fit Tuner bodies only; buy them at the parts counter.";
            }
            choices.Add(new("Back",back));Show("Tuner Garage",text,choices);
        }
        static int reviewRaces;
        static string TunerDrive(string id)=>id=="tsubasa"?"front-drive, stable, brakes late":id=="sakura"?"rear-drive, light and playful":"four-wheel drive, grip everywhere";
        void AddTunerRaces(List<AdventureUI.Choice> choices,string track,Action back)
        {
            if(RaceCalendar.Rank(RaceCalendar.Open(Store.Data))<RaceCalendar.Rank("B"))return;
            foreach(var ev in RaceCalendar.AtTrack(track,"B")){var e=ev;choices.Add(new($"{e.venue} · B {e.Stars}{(RaceCalendar.Won(Store.Data,e.id)?"  ✓ won":"")}",()=>PreviewRace(e.id,back)));}
        }
        void TunerRaces(string track,Action back)
        {
            var choices=new List<AdventureUI.Choice>();AddTunerRaces(choices,track,back);
            string text=choices.Count==0?"Tuner races start when you've finished class C: a podium at every class C race.":
                $"{RaceCalendar.Progress(Store.Data)}. Each win's stars count once; harder races give more.";
            choices.Add(new("Back",back));Show("Tuner races",text,choices);
        }
        // `back`: whoever offered the race, so backing out of the preview returns to their menu.
        void PreviewRace(string track,Action back)
        {
            player.controlsBlocked=true;ui.Close();ui.SetVisible(false);
            // (`track` is the event id: a class C event's id is its track; class B events name theirs.)
            var roster=RaceRoster.ForEvent(track,Store.Data);track=roster.Track;bool valley=track=="hillcrest";
            racePreview.Open(roster,()=>Attempt(()=>{
                // After the Valley Cup you come back to Hillcrest's Pit Stop, on the garage bay's forecourt
                // facing out (the raceway paddock is hemmed in by the team garages).
                if(valley&&PitStop.Hosts.TryGetValue("hillcrest/garage",out var host)){AdventureSession.ReturnSpot=GarageForecourt(host);AdventureSession.ReturnYaw=host.yaw;}
                else if(track=="quarry"&&QuarryHillsTown.Spots.TryGetValue("rallyschool",out var gwen)){AdventureSession.ReturnSpot=gwen.at+new Vector3(0,0,-8);AdventureSession.ReturnYaw=90;}
                else if(track=="bramblewick"&&Bramblewick.Lap!=null){var at=Bramblewick.Lap[0];var on=Bramblewick.Lap[3]-at;AdventureSession.ReturnSpot=at;AdventureSession.ReturnYaw=Mathf.Atan2(on.x,on.z)*Mathf.Rad2Deg;}   // (on the start line, facing the way the race runs)
                else if(track.StartsWith("vinelands")&&Vinelands.Lap!=null){var at=Vinelands.Lap[0];var on=Vinelands.Lap[3]-at;AdventureSession.ReturnSpot=at;AdventureSession.ReturnYaw=Mathf.Atan2(on.x,on.z)*Mathf.Rad2Deg;}
                else AdventureSession.ReturnSpot=null;
                SaveExplored();AdventureSession.StartRace(roster);}),()=>{ui.SetVisible(true);back();});
        }
        // The garage bay's forecourt: the host parks 3 m to the bay's side (kit X mirrors on import),
        // 5.4 m out from the frontage; the bay itself is straight in from there.
        static Vector3 GarageForecourt((Vector3 at,float yaw) host)=>host.at+Quaternion.Euler(0,host.yaw,0)*new Vector3(-3,0,1);
        // Pit Stop parts screens: buy at the parts bay (Sid, Rosa), fit what you own at the garage bay
        // (Mara, Gio). Back returns to whoever opened it.
        // The tuning shop (docs/48): the gearbox tune, with a timed test lap of the town's circuit.
        void OpenTuning(string where,Action host)
        {
            ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
            tuning.Open(Store,where,ApplyEquipment,()=>{ui.SetVisible(true);host();},()=>{
                var circuit=where=="hillcrest"?new Circuit(HillcrestRoute.Circuit):world.track;
                testDrive.Begin(player,circuit,where=="hillcrest"?"Hillcrest Raceway":"Bayview Coast",result=>{
                    if(result!=null)TuningScreen.LastTestDrive=result;player.controlsBlocked=true;OpenTuning(where,host);});
            });
        }
        void OpenParts(string where,PartsBrowser.Mode mode,Action host,string slot="engine")
        {
            shop=where;browser.Shop=where;ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
            browser.Open(Store,mode,slot,ApplyEquipment,()=>{ui.SetVisible(true);host();});
        }
        void ApplyEquipment()
        {
            player.tuning=EquipmentCatalog.Stats(Store.Data.Loadout(),Store.Data.Tune());player.body.mass=850*player.tuning.mass;
            foreach(var renderer in player.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(material.name.Contains("BodyPaint"))material.color=Paints[Store.Data.paintIndex];
        }
        // (In front of a resident: on the new Bayview, out from where they face; elsewhere 5 m north, as the old island was laid out.)
        Vector3 Front(string id,float m)=>residents[id]+(NewBayview.On&&residentYaw.TryGetValue(id,out var y)?Quaternion.Euler(0,y,0)*Vector3.forward:Vector3.forward)*m;
        void Place(Vector3 p){player.body.position=p+Vector3.up*.15f;player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;player.speed=0;CameraPose(true);}
        IEnumerator Smoke()
        {
            string folder=RaceTrial.Arg("--trial-captures");if(folder!=null)Directory.CreateDirectory(folder);
            if(AdventureSession.SmokePhase==0&&!RaceTrial.Has("--adventure-verify-save")) {
                if(Store.Data.questStage!=0||Store.Data.credits!=0)throw new Exception("Smoke requires a fresh isolated profile.");
                if(Interact("post"))throw new Exception("Remote interaction was incorrectly accepted.");
                Place(Front("garage",5));Interact("garage");yield return new WaitForSeconds(.3f);Capture("01-mara");
                var virtualPad=InputSystem.AddDevice<Gamepad>("AdventureMenuTest");
                var stopped=player.body.position;InputSystem.QueueStateEvent(virtualPad,new GamepadState{rightTrigger=1});yield return new WaitForSeconds(.12f);
                if(Vector3.Distance(stopped,player.body.position)>.1f)throw new Exception("Throttle moved the car while the menu was open.");
                yield return Pulse(virtualPad,GamepadButton.DpadDown);
                if(!EventSystem.current.currentSelectedGameObject.name.Contains("Fit parts"))throw new Exception("Controller menu down navigation failed.");
                yield return Pulse(virtualPad,GamepadButton.DpadUp);yield return Pulse(virtualPad,GamepadButton.South);
                if(Store.Data.questStage!=1)throw new Exception("Controller submit did not accept the parcel.");
                int driveContacts=player.contacts;Vector3 driveStart=player.body.position;InputSystem.QueueStateEvent(virtualPad,new GamepadState{rightTrigger=1});yield return new WaitForSeconds(1.2f);
                if(Vector3.Distance(driveStart,player.body.position)<2||player.speed<2)throw new Exception("Controller throttle did not drive the free-roam car: menu="+ui.IsOpen+" blocked="+player.controlsBlocked+" speed="+player.speed+" distance="+Vector3.Distance(driveStart,player.body.position));
                Quaternion heading=player.body.rotation;InputSystem.QueueStateEvent(virtualPad,new GamepadState{rightTrigger=.5f,leftStick=new Vector2(.45f,0)});yield return new WaitForSeconds(.5f);
                Debug.Log("ADVENTURE_FORECOURT_INPUT position="+player.body.position+" contacts="+(player.contacts-driveContacts)+" headingChange="+Quaternion.Angle(heading,player.body.rotation));
                if(Quaternion.Angle(heading,player.body.rotation)<2)throw new Exception("Controller steering did not turn the free-roam car.");
                if(player.contacts!=driveContacts)throw new Exception("The resident approach drive hit a forecourt obstruction.");
                InputSystem.QueueStateEvent(virtualPad,new GamepadState());yield return null;InputSystem.RemoveDevice(virtualPad);
                Debug.Log("ADVENTURE_INPUT_CHECK synthetic gamepad navigation/submit and menu throttle block passed; physical DualSense remains untested");
                Debug.Log("ADVENTURE_DRIVING_CHECK synthetic gamepad throttle and steering moved the physical free-roam car");
                Place(Front("post",5));Interact("post");if(!ui.InvokeChoice("Deliver"))throw new Exception("Delivery action missing.");
                Place(Front("garage",5));Interact("garage");if(!ui.InvokeChoice("Collect 350"))throw new Exception("Reward action missing.");Close();
                // This integration test relocates between places; it does not
                // claim a manually driven or controller-tested adventure route.
                foreach(string id in new[]{"postcard_0","postcard_1"}) {Place(finds[id].transform.position);yield return new WaitForSeconds(.15f);}
                // Buy at Sid's parts bay (the shop can't fit), then fit at Mara's garage bay.
                Place(Front("parts",5));Interact("parts");ui.InvokeChoice("Buy parts");
                browser.SelectPart("engine_workshop");
                if(!browser.InvokeAction("buy"))throw new Exception("Workshop engine purchase action missing.");
                if(browser.InvokeAction("fit")||Store.Data.Loadout()["engine"]!="engine_stock")throw new Exception("The parts shop fitted a part.");
                browser.InvokeAction("back");Close();
                Place(Front("garage",5));Interact("garage");ui.InvokeChoice("Fit parts");
                browser.SelectPart("engine_workshop");
                if(!browser.InvokeAction("fit"))throw new Exception("Workshop engine fit action missing.");
                yield return new WaitForSeconds(.3f);Capture("02-garage");
                if(Store.Data.Loadout()["engine"]!="engine_workshop"||Store.Data.credits!=0||player.tuning.power<=CarTuning.Stock.power)throw new Exception("Equipped upgrade did not apply.");
                Close();Place(Front("cafe",5));Interact("cafe");AdventureSession.SmokePhase=1;ui.InvokeChoice("Race Bayview");yield return null;if(folder!=null)racePreview.Capture(Path.Combine(folder,"02b-race-preview.png"));racePreview.Confirm();
            } else {
                var reloaded=new AdventureStore(Store.path);var p=reloaded.Data;
                if(p.questStage!=3||p.collected.Count!=2||p.Loadout()["engine"]!="engine_workshop"||p.credits<100||p.settledSerial!=p.raceSerial)throw new Exception("Adventure round-trip persistence failed.");
                if(reloaded.ClaimDelivery()||reloaded.FinishRace(p.raceSerial,p.bestPlace)!=0)throw new Exception("Duplicate payout accepted after reload.");
                yield return new WaitForSeconds(.3f);Capture("04-return-to-town");yield return new WaitForSeconds(.5f);
                string report=RaceTrial.Arg("--adventure-report");if(report!=null)File.WriteAllText(report,JsonUtility.ToJson(p,true));
                Debug.Log("LITTLE_ROAD_ADVENTURE_SMOKE_PASSED quest/delivery/finds/purchase/fitting/24-car-payout/reload/duplicate-rejection");Application.Quit();
            }
        }
        IEnumerator Pulse(Gamepad pad,GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return new WaitForSeconds(.08f);
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSeconds(.08f);
        }
        void Capture(string name){string folder=RaceTrial.Arg("--trial-captures");if(folder!=null)ui.Capture(Path.Combine(folder,name+".png"));}
    }
}
