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
        float nextReveal,nextExploreSave;readonly HashSet<string> viewpointsSeen=new();
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
            LoadingScreen.Begin("Little Road");
            world=GetComponent<RaceTrial>();world.freeRoam=true;world.track=new Circuit();world.elapsed=1;yield return TrialWorld.CreateRoutine(world.track,false);
            residents["garage"]=new Vector3(-62,0,-100);
            // Pit Stop bay hosts (Bayview: garage + parts). Hillcrest's arrive through HillcrestTown.Spots.
            foreach(var (id,key) in new[]{("garage","bayview/garage"),("parts","bayview/parts")})if(PitStop.Hosts.TryGetValue(key,out var host)){residents[id]=host.at;residentYaw[id]=host.yaw;}
            residents["cafe"]=new Vector3(-40.5f,0,-101.8f);residents["paint"]=new Vector3(3.5f,0,-102.2f);
            if(HillcrestTown.Spots.Count>0){residents["bayhalt"]=CoastalLandscape.HaltResident;residentYaw["bayhalt"]=270;}
            foreach(var id in new[]{"motors","hcparts","hcbody","hillcafe","raceway","station","castle","vista","lookout"})if(HillcrestTown.Spots.TryGetValue(id,out var spot)){residents[id]=spot.at;residentYaw[id]=spot.yaw;}
            // Quarry Hills (--quarry-hills): Gwen at the rally school.
            foreach(var id in new[]{"rallyschool","qgarage","qparts","qbody","caban","slatequay"})if(QuarryHillsTown.Spots.TryGetValue(id,out var spot)){residents[id]=spot.at;residentYaw[id]=spot.yaw;}
            residents["post"]=world.track.At(world.track.length*.46f)-world.track.Right(world.track.length*.46f)*16;
            CreatePlaces();
            if(AdventureSession.MaxBuildTest&&!AdventureSession.TestPlaced){AdventureSession.TestPlaced=true;FaceDuke();}
            var car=TrialWorld.Car(Store.Data.bodyId,Paints[Store.Data.paintIndex]);car.transform.SetPositionAndRotation(new Vector3(-63,.15f,-94),Quaternion.Euler(0,90,0));
            if(AdventureSession.ReturnSpot is Vector3 back)car.transform.SetPositionAndRotation(back+Vector3.up*.15f,Quaternion.Euler(0,AdventureSession.ReturnYaw,0));
            player=car.AddComponent<RoadCar>();player.Setup(world,0,EquipmentCatalog.Stats(Store.Data.Loadout(),Store.Data.Tune()),false,1709,"owned equipment");world.cars.Add(player);world.running=true;FrameProbe.Attach(player.transform);CameraShake.Reset();CameraShake.Watch(player);
            playerHorn=Horn.Attach(car,Horn.PitchFor(Store.Data.bodyId),player:true);
            player.Bumped+=(other,speed)=>{if(speed>1f&&!ui.IsOpen&&!paintShop.IsOpen&&!bodyShop.IsOpen)other.GetComponentInParent<NpcChatter>()?.TrySay(bubbles);};
            if(RaceTrial.Has("--vertical-audit"))gameObject.AddComponent<VerticalMotionAudit>().Setup(world);
            if(RaceTrial.Has("--ambient-traffic"))AmbientTraffic();
            if(!RaceTrial.Has("--no-valley-life"))gameObject.AddComponent<ValleyLife>().Setup(player);
            // Fireworks over Bayview harbour at night (docs/51).
            var fireworks=gameObject.AddComponent<Fireworks>();fireworks.Setup();fireworks.Announce=Notify;
            // Speed traps on seven roads (docs/51).
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
            if(!RaceTrial.Has("--free-roam-speculative"))player.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            cameraView=new GameObject("Adventure camera").AddComponent<Camera>();cameraView.tag="MainCamera";cameraView.fieldOfView=55;cameraView.nearClipPlane=.15f;cameraView.farClipPlane=6000;
            if(title!=null)title.Dismiss();cameraView.gameObject.AddComponent<AudioListener>();WorldLook.Camera(cameraView);
            photo=gameObject.AddComponent<PhotoMode>();photo.Setup(cameraView,player.transform,ui,()=>AreaBanner.Area(player.transform.position),Notify,show=>ui.SetHudVisible(show));
            // Pip's photo requests (docs/51): each sight pays once, the first photo that shows it.
            photo.Photographed=cam=>{foreach(var r in PhotoRequests.Seen(cam))Attempt(()=>{if(Store.ClaimTrial(r.id,PhotoRequests.Prize)){int got=0;foreach(var q in PhotoRequests.All)if(Store.Data.collected.Contains(q.id))got++;
                ui.Card($"PHOTO FOR PIP: {r.name.ToUpperInvariant()}",$"{got} of {PhotoRequests.All.Count} requests  ·  +{PhotoRequests.Prize} credits",Toy.Pink,"POSTCARD PHOTO");}});};
            if(GrassField.Wanted)gameObject.AddComponent<GrassField>().Setup(cameraView);
            CameraPose(true);if(!string.IsNullOrEmpty(AdventureSession.Notice)){Notify(AdventureSession.Notice);AdventureSession.Notice=null;}
            else if(Store.Data.questStage==0)Notify("Welcome to Bayview · Mara has a parcel for the lighthouse");
            yield return WarmUp();CameraPose(true);
            // Free-roam map: the world baked from above once, behind the loading card.
            ExploredMap.Load(Store.Data.explored);
            // Only where you've been (Richard: no free reveals; viewpoints will fill empty country later).
            ExploredMap.Reveal(player.transform.position,ExploredMap.DriveRadius);ExploredMap.Flush();
            var banner=GetComponent<AreaBanner>();
            if(banner!=null){if(!Store.Data.discovered.Contains(banner.Current))Attempt(()=>Store.Discover(banner.Current));   // where you start is known already
                banner.known=place=>Store.Data.discovered.Contains(place);
                banner.discovered=(place,subtitle)=>Attempt(()=>{if(Store.Discover(place))ui.Discovery(place,subtitle);});
}
            RenderTexture mapTexture=null;
            if(!Application.isBatchMode&&!RaceTrial.Has("--no-minimap")){mapTexture=WorldMap.Bake();minimap=gameObject.AddComponent<Minimap>();minimap.Setup(player,mapTexture,MapMarks,()=>WorldMap.Labels(ResidentDirectory(),world.track));}
            GraphicsOptions.Changed+=OnOptionChanged;
            pause=gameObject.AddComponent<PauseMenu>();pause.Setup(new PauseMenu.Context{save=()=>Store.Data,area=()=>AreaBanner.Area(player.transform.position),objective=()=>GoalText(false),car=player.transform,
                minimap=minimap,marks=MapMarks,map=mapTexture,settings=SettingsItems,quit=Application.Quit,car_=CarSummary});
            CarContact.SeatAll(); // every standing car onto its four wheels (docs/57, 2.5)
            Debug.Log("LITTLE_ROAD_ADVENTURE_READY save="+Store.path+" quest="+Store.Data.questStage+" credits="+Store.Data.credits+" "+RaceTrial.LoadTime);LoadingScreen.Ready();
            if(RaceTrial.Has("--geometry-audit-only")){Application.Quit();yield break;}
            if(RaceTrial.Has("--minimap-review")){reviewing=true;yield return MinimapReview();yield break;}
            if(RaceTrial.Has("--pause-review")){yield return PauseReview();yield break;}
            // --photo-review: photo mode's five looks (docs/51) to --trial-captures, framed and unframed.
            if(RaceTrial.Has("--photo-review")){reviewing=true;var main=HillcrestRoute.Mainline;float s=main.Nearest(new Vector3(95,0,-300),out _);player.body.rotation=Quaternion.LookRotation(-main.Forward(s));Place(main.At(s));
                yield return new WaitForSeconds(1.5f);CameraPose(true);cameraView.transform.position=player.transform.position+player.transform.right*5.5f+player.transform.forward*4+Vector3.up*1.6f;yield return null;photo.Open();string folder=RaceTrial.Arg("--trial-captures");
                for(int l=0;l<5;l++)yield return photo.Review(Path.Combine(folder,$"photo-{l}.png"),l,l!=1);photo.Close();Application.Quit(0);yield break;}
            if(RaceTrial.Has("--contact-review")){reviewing=true;yield return ContactReview.Run(RaceTrial.Arg("--trial-captures"));Application.Quit(0);yield break;}
            if(RaceTrial.Has("--climb-review")){yield return ClimbReview();yield break;}
            if(RaceTrial.Has("--duke-drive-review")){yield return DukeDriveReview();yield break;}
            if(RaceTrial.Has("--world-review")){RefreshHUD();reviewing=true;player.controlsBlocked=true;gameObject.AddComponent<VillageReview>();yield break;}
            if(RaceTrial.Has("--vehicle-lights-review")){reviewing=true;gameObject.AddComponent<VehicleLightReview>();yield break;}
            if(RaceTrial.Has("--evening-route-review")){gameObject.AddComponent<EveningRouteReview>();yield break;}
            if(RaceTrial.Has("--turn-review")){gameObject.AddComponent<TurnReview>();yield break;}
            if(RaceTrial.Has("--valley-tour")){reviewing=true;gameObject.AddComponent<ValleyTour>().Setup(player,cameraView,ui);yield break;}
            if(RaceTrial.Has("--paint-shop-review")){reviewing=true;yield return PaintShopReview();yield break;}
            if(RaceTrial.Has("--body-shop-review")){reviewing=true;yield return BodyShopReview();yield break;}
            // Opt-in: enter the Valley Cup straight away (with --trial-auto the player's car drives itself).
            if(RaceTrial.Has("--duke-race-review")&&!AdventureSession.Racing&&Store.Data.settledSerial==Store.Data.raceSerial){yield return new WaitForSeconds(.5f);StartDukeRace(RaceRoster.DukeChallenge(Store.Data));yield break;}
            if(RaceTrial.Has("--valley-race-review")&&!AdventureSession.Racing&&Store.Data.settledSerial==Store.Data.raceSerial){yield return new WaitForSeconds(.5f);PreviewRace("hillcrest");yield return new WaitForSeconds(.5f);racePreview.Confirm();yield break;}
            if(RaceTrial.Has("--parts-browser-review")){OpenParts("bayview",PartsBrowser.Mode.Shop,Sid);gameObject.AddComponent<PartsBrowserReview>().Setup(browser,Store,player,ui,m=>OpenParts("bayview",m,m==PartsBrowser.Mode.Shop?Sid:Mara));yield break;}
            if(RaceTrial.Has("--sound-bench"))EngineBench.Show(RaceTrial.Arg("--sound-bench"));
            // Review: the tuning screen, captured (docs/48).
            if(RaceTrial.Has("--tuning-review")){reviewing=true;OpenTuning("bayview",Mara);yield return new WaitForSeconds(1.5f);
                string shot=RaceTrial.Arg("--tuning-review");ui.Capture(shot);yield return new WaitForSeconds(1);Debug.Log("LITTLE_ROAD_TUNING_REVIEW "+shot);Application.Quit();yield break;}
            // Test (--quarry-hills --sprint-test): talk to Gwen, start the Quarry Sprint, carry the car round the loop at 22 m/s and
            // check the lap is timed and the prize claimed (the result goes to the log, then the game quits).
            if(RaceTrial.Has("--sprint-test")&&residents.ContainsKey("rallyschool")){StartCoroutine(SprintTest());yield break;}
            if(RaceTrial.Has("--carving-test")&&QuarryHillsTown.Spots.Count>0){StartCoroutine(CarvingTest());yield break;}
            if(RaceTrial.Has("--blondin-test")){StartCoroutine(BlondinTest());yield break;}
            if(RaceTrial.Has("--climb-test")){StartCoroutine(ClimbTest());yield break;}
            if(RaceTrial.Has("--lesson-test")&&residents.ContainsKey("rallyschool")){StartCoroutine(LessonTest());yield break;}
            yield return null;if(AdventureSession.Smoke)StartCoroutine(Smoke());
        }
        // Test (--quarry-hills --carving-test): every carving is clear of buildings and props, and collecting all twelve pays.
        IEnumerator CarvingTest()
        {
            yield return new WaitForSeconds(1);int blocked=0;float credits=Store.Data.credits;
            for(int i=0;i<Collectibles.Carvings;i++) {
                string id=Collectibles.Carving(i);if(!finds.TryGetValue(id,out var go))continue;var at=go.transform.position;
                int hits=0;string what="";foreach(var c in Physics.OverlapSphere(at,2.2f))if(c.CompareTag("Respawn")&&c.GetComponentInParent<RoadCar>()==null){hits++;what+=c.name+";";}if(hits>0)blocked++;
                Debug.Log($"LITTLE_ROAD_CARVING {i} {MapRegions.CarvingPlace(i)} at=({at.x:0},{at.y:0.0},{at.z:0}) ground={ValleyField.Height(at.x,at.z):0.0} road={ValleyField.RoadDistance(at.x,at.z):0} blocked={hits} {what}");
                player.body.isKinematic=true;player.body.position=at+Vector3.down*.8f;yield return new WaitForFixedUpdate();yield return null;yield return null;
            }
            player.body.isKinematic=false;int got=0;foreach(var c in Store.Data.collected)if(Collectibles.IsCarving(c))got++;
            Debug.Log($"LITTLE_ROAD_CARVING_TEST collected={got}/{Collectibles.Carvings} blocked={blocked} credits={credits}->{Store.Data.credits}");
            Application.Quit(got==Collectibles.Carvings&&blocked==0?0:1);
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
            for(int i=0;i<RallySchool.Lessons.Length;i++) {
                StartLesson(i);yield return new WaitForSeconds(3.2f);var path=RallySchool.TestPath(i);float speed=i==5?20:14;
                player.body.isKinematic=true;
                for(int k=0;k+1<path.Count&&school.Active;k++){var a=path[k];var b=path[k+1];float len=Vector3.Distance(a,b);
                    for(float d=0;d<len&&school.Active;d+=speed*Time.fixedDeltaTime){var p=Vector3.Lerp(a,b,d/len);p.y=ValleyField.Height(p.x,p.z)+.6f;player.body.MovePosition(p);
                        var f=b-a;f.y=0;if(f.sqrMagnitude>.01f)player.body.MoveRotation(Quaternion.LookRotation(f));yield return new WaitForFixedUpdate();}}
                for(int k=0;k<30&&school.Active;k++)yield return new WaitForFixedUpdate();
                player.body.isKinematic=false;var r=RallySchool.Record(Store.Data,i);report.Add($"{i}:{(r==null?"none":RallySchool.MedalName(r.stars)+" "+r.best)}");
                school.End();yield return new WaitForSeconds(.3f);
            }
            bool unlocked=PartDiscovery.Met(Store.Data,RallySchool.Unlock);
            Debug.Log($"LITTLE_ROAD_LESSON_TEST {string.Join(" ",report)} allGold={RallySchool.AllGold(Store.Data)} partsUnlocked={unlocked} credits={credits}->{Store.Data.credits}");
            Application.Quit(unlocked?0:1);
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
        static readonly Dictionary<string,(string body,Color colour,string label,string speaker,Color bubble,string[] lines)> People=new(){
            ["garage"]=("kipper",new Color(.85f,.38f,.19f),"MARA / PIT STOP GARAGE","Mara",Toy.Orange,new[]{"Careful! I only just fixed that dent.","Bring it round the garage and I'll take a look!","Ha! You drive like I did at your age."}),
            ["parts"]=("bubble",Toy.Hex(0xffbd10),"SID / PIT STOP PARTS","Sid",Toy.Sun,new[]{"Mind the shelves! That's a whole engine up there.","Tyres, turbos, brakes... just not bumpers.","Need parts? Try buying them, not headbutting them!"}),
            ["hcparts"]=("beetle",Toy.Hex(0xffbd10),"ROSA / PIT STOP PARTS","Rosa",Toy.Sun,new[]{"Careful! The Valley V6 bruises easily.","Hill grip tyres would have stopped that.","Parts are for fitting, not for dents!"}),
            ["hcbody"]=("roadster",Toy.Hex(0xff57a6),"LINA / PIT STOP BODY SHOP","Lina",Toy.Pink,new[]{"Easy on the show car's cousin!","That dent? I could sell you a whole new car instead.","Ooh, a bump. Fancy a new look while you're here?"}),
            ["post"]=("pebble",new Color(.9f,.7f,.23f),"PIP / LIGHTHOUSE","Pip",Toy.Sun,new[]{"Whoa! The lighthouse is that way, not through me!","Letters, parcels... and now bumps. Busy day!"}),
            ["paint"]=("picnic",Toy.Hex(0xff7ac8),"DOT / PAINTS","Dot",Toy.Pink,new[]{"Ooh, that scrape would look lovely in candy pink.","Fancy a fresh coat after that?","Mind my wet paint!"}),
            ["cafe"]=("otter",new Color(.38f,.48f,.68f),"ROWAN / COAST RUN","Rowan",Toy.Sky,new[]{"Save it for the race!","Rubbing's racing... but not by the café!","Is that a challenge? See you on the grid!"}),
            ["motors"]=("comet",Toy.Hex(0xffb400),"GIO / PIT STOP GARAGE","Gio",Toy.Sun,new[]{"Easy! Hill brakes are a work of art.","That knock? Sounds like you need better tyres.","Welcome to Hillcrest, speedy!"}),
            ["hillcafe"]=("tallboy",Toy.Hex(0x3ee6a4),"FERN / BELLA VISTA","Fern",Toy.Mint,new[]{"Careful, the espresso's still hot!","You drove up that hill in THAT?","Save the bumping for the raceway!"}),
            ["raceway"]=("rally",Toy.Hex(0xff4d5e),"VITO / RACEWAY","Vito",Toy.Red,new[]{"Whoa! Save that for the Valley Cup!","You call that a bump? Wait till turn one!","Sign up, then we'll see who's bumping who!"}),
            ["station"]=("heron",Toy.Hex(0x2e9e5b),"STELLA / STATION","Stella",Toy.Mint,new[]{"Mind the gap! And my bonnet!","The 3:15 is never late. Unlike you!","Tickets, please! Oh, you're a car."}),
            ["castle"]=(CarBodies.Prototype,Toy.Hex(0x9b6bd9),"DUKE ROLO / CASTLE","Duke Rolo",Toy.Pink,new[]{"How dare you! I am a DUKE!","Guards! Oh... I don't have guards.","Mind the ancient cobbles, young racer!"}),
            ["vista"]=("doodle",Toy.Hex(0x5ec8ff),"WREN / VIEWPOINT","Wren",Toy.Sky,new[]{"Hey! You're in my photo!","Isn't it the BEST view?","Careful, don't knock me over the edge!"}),
            ["bayhalt"]=("minibus",Toy.Hex(0x1d6fa8),"STANLEY / BAYVIEW HALT","Stanley",Toy.Sky,new[]{"Mind the platform edge! And my wing mirror!","All aboard! Well... not you, you're a car.","The 4:10 to Hillcrest is running early!"}),
            ["lookout"]=("bramble",Toy.Hex(0x3ee6a4),"MOSS / FALLS","Moss",Toy.Mint,new[]{"Shh! You'll scare the fish!","Oof. That's the third bump today.","Mind the drop, it's a long way down!"}),
            ["rallyschool"]=("rally",Toy.Hex(0x229954),"GWEN / RALLY SCHOOL","Gwen",Toy.Mint,new[]{"Oi! Save the contact for the rallycross!","Lift and turn, cariad, lift and turn.","That's a sump guard's job, that is!"}),
            ["qgarage"]=("kipper",Toy.Hex(0xc0392b),"HUW / PIT STOP GARAGE","Huw",Toy.Red,new[]{"Easy, butt! I only just swept the bay.","Gravel in the arches again? Bring her in.","Duw, that was a clatter!"}),
            ["qparts"]=("bubble",Toy.Hex(0xf1c40f),"BETHAN / GRIT & GRAVEL","Bethan",Toy.Sun,new[]{"Mind the shelves! Mud flaps everywhere now.","Parts are for fitting, not for bumping, cariad.","Ooh, that'll want a sump guard."}),
            ["qbody"]=("roadster",Toy.Hex(0x8e44ad),"NIA / PIT STOP BODY SHOP","Nia",Toy.Pink,new[]{"Slate grey would hide that scratch, you know.","Careful! That's my show car's nan.","New look? I've got just the thing."}),
            ["caban"]=("otter",Toy.Hex(0xe9e3d3),"MEGAN / CABAN","Megan",Toy.Sky,new[]{"Careful! The cawl's still hot!","Tea's brewing, no need to knock!","Duw, you'll have the cakes off the shelf!"}),
            ["slatequay"]=("heron",Toy.Hex(0x1f3a93),"MARI / SLATE QUAY","Mari",Toy.Mint,new[]{"Mind the wagons! They're loaded!","Engine's warming up, keep clear of the shed!","Whoa! The quay's for slate, not stunts!"}),
        };
        void CreatePlaces()
        {
            var moved=new Dictionary<string,Vector3>();
            foreach(var pair in residents) {
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
                var chatter=npc.AddComponent<NpcChatter>();(chatter.speaker,chatter.colour,chatter.lines)=(who.speaker,who.bubble,who.lines);chatter.talkable=false;
                badges[pair.Key]=TalkBadge.Add(npc,who.bubble);
                // Out of any building, prop or wall they were placed in (Vito sat in the raceway's control tower).
                if(StationarySpot.Settle(npc)>0)moved[pair.Key]=npc.transform.position;
            }
            foreach(var m in moved)residents[m.Key]=m.Value;
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
            if(QuarryHillsTown.Spots.Count>0) {
                var spots=QuarryHills.CarvingSpots();if(spots.Length!=Collectibles.Carvings)throw new InvalidOperationException("Carving spots and Collectibles.Carvings disagree.");
                for(int i=0;i<spots.Length;i++){string id=Collectibles.Carving(i);var p=spots[i]+Vector3.up*1.3f;MapRegions.Keepsakes[id]=(p,MapRegions.CarvingPlace(i));if(Store.Data.collected.Contains(id))continue;finds[id]=Keepsake(id,p,2,i);}
            }
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
        // kind: 0 Bayview postcard, 1 Hillcrest stamp, 2 Quarry Hills slate carving.
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
            if(kind==2) {
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
        // Opt-in (--ambient-traffic): a few family cars cruising the loop on stock kit, using
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
            var rowan=GameObject.Find("Resident cafe");Place(residents["cafe"]+Vector3.forward*6);cameraView.transform.position=residents["cafe"]+new Vector3(-3,4,9);cameraView.transform.LookAt(residents["cafe"]+Vector3.up);
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
            if(inRange==null&&stopped&&climbStart<0&&sprintStart<0&&(school==null||!school.Active))foreach(var line in ZipLine.All) {
                if(line.Busy)continue;
                if(Vector3.Distance(player.body.position,line.top.bay)<6)nearZip=line.top;else if(Vector3.Distance(player.body.position,line.bottom.bay)<6)nearZip=line.bottom;
                if(nearZip!=null){nearLine=line;nearFolk=null;nearChat=null;break;}
            }
            // Cross / A (and E) talks while someone (or the zipline) is in reach; then it doesn't also shift up (Controls).
            Controls.InteractionReady=Time.unscaledTime>interactionReadyTime&&(nearest!=null||nearZip!=null||nearFolk!=null||nearChat!=null);
            if(Time.unscaledTime>interactionReadyTime&&(k?.eKey.wasPressedThisFrame==true||pad?.buttonSouth.wasPressedThisFrame==true)){if(nearest!=null)Interact(nearest);else if(nearZip!=null)StartCoroutine(RideZip(nearLine,nearZip==nearLine.top));else if(nearFolk!=null)TalkFolk(nearFolk);else if(nearChat!=null)TalkChatter(nearChat);}
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
                        Func<string,bool> kind=Collectibles.IsCarving(pair.Key)?Collectibles.IsCarving:Collectibles.IsStamp(pair.Key)?Collectibles.IsStamp:Collectibles.IsPostcard;
                        int got=0;foreach(string c in Store.Data.collected)if(kind(c))got++;
                        Notify(Collectibles.IsCarving(pair.Key)?$"Slate carving {got} of {Collectibles.Carvings} found · +50 credits":Collectibles.IsStamp(pair.Key)?$"Hillcrest stamp {got} of {Collectibles.Stamps} found · +50 credits":$"Bayview postcard {got} of {Collectibles.Postcards} found · +50 credits");}});
            }
            // Countdowns hold the car still but let it rev (Richard, 1 Oct).
            player.holdStill=climbStart>=0&&Time.time<climbStart||trainRace!=null&&trainRace.Holding||school!=null&&school.Holding;

            UpdateClimb();UpdateSprint();UpdateGalleryClimb();UpdateVisit();RefreshHUD();
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
            if(galleryStart>=0)return $"Gallery Climb · {Time.time-galleryStart:0.0} s · par {GalleryClimb.Par:0} s"+(distance?$"  ·  {Mathf.Max(0,GalleryClimb.Finish.y-player.transform.position.y):0} m to climb":"");
            if(sprintStart>=0)return Time.time<sprintStart?"Quarry Sprint · get ready!":$"Quarry Sprint · {Time.time-sprintStart:0.0} s · par {SprintPar:0} s";
            if(climbStart>=0)return (Time.time<climbStart?"Castle Climb · get ready!":$"Castle Climb · {Time.time-climbStart:0.0} s · par {ClimbPar:0} s · to the castle")+(distance?$"  ·  {Vector3.Distance(player.transform.position,HillcrestRoute.CastleCourt):0} m":"");
            return objectives[Store.Data.questStage]+(distance?$"  ·  {Vector3.Distance(player.transform.position,residents[target]):0} m":"");
        }
        void RefreshHUD()
        {
            float now=Time.unscaledTime;if(controlsUntil<0)controlsUntil=now+15;
            string goal=GoalText(false);if(goal!=lastGoal&&climbStart<0&&sprintStart<0&&(school==null||!school.Active)){lastGoal=goal;goalUntil=now+8;}
            if(Mathf.Abs(player.speed)<=1.2f){if(stoppedSince<0)stoppedSince=now;}else stoppedSince=-1;
            float stopped=stoppedSince<0?0:now-stoppedSince;
            bool timed=climbStart>=0||sprintStart>=0||galleryStart>=0||school!=null&&school.Active;
            ui.Roam(GoalText(true),timed||now<goalUntil||stopped>1.5f,null,false,player);
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
        static readonly HashSet<string> PitHosts=new(){"garage","parts","motors","hcparts","hcbody","qgarage","qparts","qbody"};
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
            }
            ExploredMap.Flush();
            if(ExploredMap.Dirty&&now>nextExploreSave){nextExploreSave=now+20;SaveExplored();}
        }
        void SaveExplored(){if(ExploredMap.Loaded&&ExploredMap.Dirty)Attempt(()=>Store.SaveExplored(ExploredMap.Save()));}
        void OnApplicationQuit()=>SaveExplored();
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
            Vector3 delta=desired-target;
            foreach(var hit in Physics.SphereCastAll(target,.2f,delta.normalized,delta.magnitude))
                if(!hit.collider.transform.IsChildOf(player.transform)&&hit.distance>1)desired=target+delta.normalized*Mathf.Min(Vector3.Distance(target,desired),hit.distance-.25f);
            // Looking back the camera is locked 6.5 m ahead of the car. Flipping back puts the forward view
            // exactly where it was trailing (snapping to the untrailed spot then drifting back read as a zoom).
            if(behind)cameraView.transform.position=desired;
            else if(flip)cameraView.transform.position=desired+player.transform.TransformVector(chaseLag);
            else if(snap){cameraView.transform.position=desired;chaseLag=Vector3.zero;}
            else{cameraView.transform.position=Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-Time.deltaTime*8));chaseLag=player.transform.InverseTransformVector(cameraView.transform.position-desired);}
            cameraView.transform.LookAt(target+viewingForward*1.7f);
            float fov=WorldLook.SpeedFov(55+GraphicsOptions.FovOffset,player.speed);cameraView.fieldOfView=snap?fov:Mathf.Lerp(cameraView.fieldOfView,fov,1-Mathf.Exp(-Time.deltaTime*3));
            if(!snap&&!riding)CameraShake.Apply(cameraView);
        }
        void Notify(string value)=>ui.Toast(value);
        Horn playerHorn;bool honking;TrainRace trainRace;PhotoMode photo;FinishBeacon beacon;Vector3? beaconAt;
        // Reviews (--horn-test): one honk and its answers.
        public void TestHonk(){playerHorn?.Toot(.35f,0);HornHeard();}
        void HornHeard()
        {
            var at=player.body.position;int answered=0;
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
            var views=new List<(Vector3 at,Vector3 look)>{(new Vector3(-10,30,-10),new Vector3(-40,0,-100)),(new Vector3(90,20,-250),new Vector3(100,0,-450))};
            if(HillcrestTown.Spots.Count>0) {
                var c=HillcrestRoute.CastleCourt;var sq=HillcrestRoute.Square;
                views.Add((HillcrestRoute.Vista+Vector3.up*4,new Vector3(200,30,-1100)));views.Add((sq+new Vector3(-20,4,-6),sq+new Vector3(20,3,6)));
                views.Add((c+new Vector3(0,6,-30),c+new Vector3(0,12,20)));views.Add((HillcrestRoute.Paddock+new Vector3(20,15,20),HillcrestRoute.Paddock+new Vector3(-80,0,-120)));
                views.Add((new Vector3(HillcrestRoute.FarmCentre.x,40,HillcrestRoute.FarmCentre.y+120),new Vector3(HillcrestRoute.FarmCentre.x,0,HillcrestRoute.FarmCentre.y)));
                if(ZipLine.Active!=null)views.Add((ZipLine.Active.bottom.bay+new Vector3(20,20,20),ZipLine.Active.top.bay));
            }
            foreach(var (at,look) in views){cameraView.transform.position=at;cameraView.transform.LookAt(look);WorldLook.Cut(cameraView);yield return null;yield return null;}
            // Force LOD cross-fades both ways in each view so the fade variants of every shader (kit, trees,
            // cars) compile now: their first real fade, on the causeway, stalled 400-700 ms (drive benchmark db9).
            float bias=QualitySettings.lodBias;
            foreach(var (at,look) in views) {
                cameraView.transform.position=at;cameraView.transform.LookAt(look);WorldLook.Cut(cameraView);
                QualitySettings.lodBias=bias*.2f;for(int i=0;i<3;i++)yield return null;
                QualitySettings.lodBias=bias;for(int i=0;i<3;i++)yield return null;
            }
            QualitySettings.lodBias=bias;
        }
        // Everyone you can talk to and where they stand (the world map labels them).
        public List<(string name,string role,Vector3 at)> ResidentDirectory()
        {
            var list=new List<(string,string,Vector3)>();
            foreach(var r in residents)if(People.TryGetValue(r.Key,out var who))list.Add((who.speaker,RoleOf(who.label),r.Value));
            return list;
        }
        // Hillcrest Castle zipline: the car hangs in the trolley's cradle for the ride.
        IEnumerator RideZip(ZipLine zip,bool down)
        {
            if(climbStart>=0){Notify("The zipline is closed · Castle Climb in progress");yield break;}   // no lift to the top during the climb
            riding=true;nearZip=null;nearLine=null;player.controlsBlocked=true;player.body.isKinematic=true;
            Notify(down?"Hold on tight!":"Up we go!");
            try{yield return zip.Ride(player,down);}
            finally {
                player.body.isKinematic=false;player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;
                riding=false;player.controlsBlocked=ui.IsOpen;interactionReadyTime=Time.unscaledTime+.6f;
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
                case "raceway":Vito();break;case "station":Stella();break;case "bayhalt":Stanley();break;case "castle":Duke();break;case "vista":Wren();break;case "lookout":Moss();break;case "rallyschool":Gwen();break;case "qgarage":Huw();break;case "qparts":Bethan();break;case "qbody":Nia();break;case "caban":Megan();break;case "slatequay":Mari();break;default:Rowan();break;}
            return true;
        }
        void Mara()
        {
            var choices=new List<AdventureUI.Choice>();int stage=Store.Data.questStage;
            if(stage==0)choices.Add(new("I'll take the parcel",()=>Attempt(()=>{Store.AcceptDelivery();Close();Notify("Parcel aboard. Follow the coast road to Pip's lighthouse.");})));
            if(stage==2)choices.Add(new("Collect 350 credits",()=>Attempt(()=>{Store.ClaimDelivery();Mara();})));
            if(stage>=3)choices.Add(new("Race Bayview Coast · C",()=>PreviewRace("bayview",Mara)));
            choices.Add(new("Fit parts",()=>OpenParts("bayview",PartsBrowser.Mode.Fit,Mara)));
            choices.Add(new("Tune gearbox",()=>OpenTuning("bayview",Mara)));
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Mara)));
            choices.Add(new("Back to the road",Close));
            Show("Mara / Pit Stop Garage",stage==0?"Pip needs a parcel at the lighthouse. Take the coast road and look for the red lantern cap. I'll have 350 credits for you when you return.":stage==1?"Pip is by the lighthouse on the far side of the loop. There are postcards beside the road if you take a detour.":stage==2?"Pip sent word! The parcel arrived safely. Here's something towards making this car your own.":"A good car becomes your own, one improvement at a time. What shall we work on?",choices);
        }
        void Gio()
        {
            var choices=new List<AdventureUI.Choice>{new("Race the Valley Cup · B",()=>PreviewRace("hillcrest",Gio)),new("Fit parts",()=>OpenParts("hillcrest",PartsBrowser.Mode.Fit,Gio)),new("Tune gearbox",()=>OpenTuning("hillcrest",Gio))};
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
            bodyShop.Open(Store,"hillcrest",()=>Notify("New body in the garage bay · swap to it with "+fitter),()=>{ui.SetVisible(true);back();});
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
            new(){new("Race Hillcrest Raceway · C",()=>PreviewRace("hillcrest",Vito)),new("Back to the road",Close)});
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
            if(trainRace!=null&&trainRace.CanStart&&climbStart<0&&Store.Data.collected.Contains(Collectibles.VisitHillcrest)) {
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
        const float NormalPar=85,HardPar=80;const int NormalPrize=300,HardPrize=750;float climbStart=-1;bool climbHard;CastleClimb climbEvent;
        float ClimbPar=>climbHard?HardPar:NormalPar;
        void StartClimb(bool hard)
        {
            Close();climbHard=hard;DukeDrive.Instance?.Recall();var main=HillcrestRoute.Mainline;float d=main.Length-26;var start=main.At(d);
            player.body.rotation=Quaternion.LookRotation(main.Forward(d));Place(start);
            if(climbEvent==null)climbEvent=gameObject.AddComponent<CastleClimb>();climbEvent.Begin(hard,player.transform,bubbles);
            climbStart=Time.time+3;Notify((hard?"Hard Castle Climb":"Castle Climb")+" · Ready... GO in 3!");
        }
        void EndClimb(){climbStart=-1;climbEvent?.End();}
        // The Quarry Sprint (docs/59): one timed lap of the rallycross loop on the quarry floor, from Gwen at the rally
        // school. Through the three quarter-lap gates in order, then back over the start line.
        const float SprintPar=80;const int SprintPrize=250;float sprintStart=-1;int sprintGate;RallySchool school;
        void Gwen()
        {
            string done=Store.Data.collected.Contains(Collectibles.QuarrySprint)?" You've beaten the Sprint's par already. Lovely.":$" Beat {SprintPar:0} seconds round the Sprint and I'll pay {SprintPrize} credits.";
            int golds=0;for(int i=0;i<RallySchool.Lessons.Length;i++){var r=RallySchool.Record(Store.Data,i);if(r!=null&&r.stars==3)golds++;}
            string schoolLine=RallySchool.AllGold(Store.Data)?" Gold in every lesson: Bethan at Grit & Gravel has the rally kit for you.":$" Six lessons, bronze to gold: get gold in all six and Bethan sells you the proper rally kit ({golds} of 6 so far).";
            Show("Gwen \"Granite\" Price / Rally School","Croeso! Gravel's not tarmac, cariad: brake in a straight line, turn in early, and let her slide."+schoolLine+done,
                new(){new("Rally lessons",GwenLessons),new($"Quarry Sprint · time trial (par {SprintPar:0} s)",StartSprint),new("Back to the road",Close)});
        }
        void GwenLessons()
        {
            var choices=new List<AdventureUI.Choice>();
            for(int i=0;i<RallySchool.Lessons.Length;i++){int k=i;var l=RallySchool.Lessons[i];var r=RallySchool.Record(Store.Data,i);
                string mark=r==null||r.stars==0?"":$" · {RallySchool.MedalName(r.stars)} {r.best:0.0} s";
                choices.Add(new($"{i+1}. {l.name}{mark}",()=>GwenLesson(k)));}
            choices.Add(new("Back",Gwen));
            Show("Gwen / Rally lessons",$"Pick a lesson. Medals pay once each: bronze {RallySchool.Prizes[0]}, silver {RallySchool.Prizes[1]}, gold {RallySchool.Prizes[2]} credits.",choices);
        }
        void GwenLesson(int i)
        {
            var l=RallySchool.Lessons[i];
            Show($"Gwen / {l.name}",$"{l.how}\nGold {l.gold:0.0} s · silver {l.silver:0.0} s · bronze {l.bronze:0} s. Knocked cones cost 2 s each.",
                new(){new("Go!",()=>{Close();StartLesson(i);}),new("Back",GwenLessons)});
        }
        void StartLesson(int i)
        {
            if(school==null){school=gameObject.AddComponent<RallySchool>();school.Setup(player,()=>Store,Notify,(p,yaw)=>{player.body.rotation=Quaternion.Euler(0,yaw,0);Place(p);});}
            sprintStart=-1;school.Begin(i);
        }
        // Quarry Hills' Pit Stop (uniform with every town's) and its people.
        void Huw()
        {
            var choices=new List<AdventureUI.Choice>{new("Fit parts",()=>OpenParts("quarry",PartsBrowser.Mode.Fit,Huw)),new("Tune gearbox",()=>OpenTuning("hillcrest",Huw))};
            if(Store.Data.bodies.Count>1)choices.Add(new("Swap body ("+Store.Data.bodies.Count+" owned)",()=>SwapBody(Huw)));
            choices.Add(new("Back to the road",Close));
            Show("Huw / Pit Stop Garage","Croeso i'r Pit Stop! Bring her in: I'll fit anything you own, set up the gearbox, or swap to another of your bodies. Bethan sells the parts next door, Nia the new rides.",choices);
        }
        void Bethan()=>Show("Bethan / Grit & Gravel",RallySchool.AllGold(Store.Data)?"Gwen says you're ready, so here it is: our gravel tyres and the gravel rack. Knobbly, loud and quick on the loose. Plus everything else you've found on your travels.":
            "Everything you've found on your travels, right here. Our own rally kit (gravel tyres, the gravel rack) comes once Gwen at the rally school says you're ready: gold in all six lessons.",
            new(){new("Buy parts",()=>OpenParts("quarry",PartsBrowser.Mode.Shop,Bethan)),new("Back to the road",Close)});
        void Nia()=>Show("Nia / Pit Stop Body Shop","New car, same you! Bodies are looks only: your parts decide the pace. Huw swaps them in the bay whenever you like. Fresh paint too. Slate grey's popular round here, funnily enough.",
            new(){new("Browse new bodies",()=>OpenBodyShop(Nia,"Huw")),new("Fresh paint",()=>OpenPaintShop(Nia)),new("Back to the road",Close)});
        void Megan()=>Show("Megan / Caban","The Caban's where the quarrymen ate, and argued, and sang. Still do, mostly the arguing. The town's betting on motorsport now the quarry's winding down: rally school, the Sprint, and there's talk of a proper stage up through the tips and the moor.",
            new(){new("Back to the road",Close)});
        void Mari()=>Show("Mari / Slate Quay","Mari, from the engine shed. The slate trains come down to the quay by gravity and go back up behind the engine. When the line's open again you can race one down the valley, if you think you're quick enough.",
            new(){new("Back to the road",Close)});
        // The Gallery Climb (GalleryClimb): starts itself when you pass the bottom flags heading up; ends at the top flags.
        float galleryStart=-1;Vector3 galleryLast;
        void UpdateGalleryClimb()
        {
            if(!GalleryClimb.Built||riding)return;var p=player.body.position;var moved=(p-galleryLast)/Mathf.Max(Time.deltaTime,1e-3f);galleryLast=p;
            if(galleryStart<0) {
                if(climbStart>=0||sprintStart>=0||school!=null&&school.Active)return;
                var d=p-GalleryClimb.Start;d.y=0;var v=moved;v.y=0;
                if(d.magnitude<5&&Mathf.Abs(p.y-GalleryClimb.Start.y)<3&&Vector3.Dot(v,GalleryClimb.StartDir)>3){galleryStart=Time.time;Notify("Gallery Climb · GO! Twelve benches to the rim.");}
                return;
            }
            float t=Time.time-galleryStart;
            if(t>400||p.y<GalleryClimb.Start.y-6){galleryStart=-1;Notify("Gallery Climb · off the course. Back to the bottom flags for another go.");return;}
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
            sprintGate=0;sprintStart=Time.time+3;Notify("Quarry Sprint · Ready... GO in 3!");
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
        void PreviewRace(string track)=>PreviewRace(track,track=="hillcrest"?Vito:Rowan);
        // `back`: whoever offered the race, so backing out of the preview returns to their menu.
        void PreviewRace(string track,Action back)
        {
            player.controlsBlocked=true;ui.Close();ui.SetVisible(false);bool valley=track=="hillcrest";
            var roster=valley?RaceRoster.AdventureHillcrest(Store.Data):RaceRoster.Adventure(Store.Data);
            racePreview.Open(roster,()=>Attempt(()=>{
                // After the Valley Cup you come back to Hillcrest's Pit Stop, on the garage bay's forecourt
                // facing out (the raceway paddock is hemmed in by the team garages).
                if(valley&&PitStop.Hosts.TryGetValue("hillcrest/garage",out var host)){AdventureSession.ReturnSpot=GarageForecourt(host);AdventureSession.ReturnYaw=host.yaw;}
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
        void Place(Vector3 p){player.body.position=p+Vector3.up*.15f;player.body.linearVelocity=Vector3.zero;player.body.angularVelocity=Vector3.zero;player.speed=0;CameraPose(true);}
        IEnumerator Smoke()
        {
            string folder=RaceTrial.Arg("--trial-captures");if(folder!=null)Directory.CreateDirectory(folder);
            if(AdventureSession.SmokePhase==0&&!RaceTrial.Has("--adventure-verify-save")) {
                if(Store.Data.questStage!=0||Store.Data.credits!=0)throw new Exception("Smoke requires a fresh isolated profile.");
                if(Interact("post"))throw new Exception("Remote interaction was incorrectly accepted.");
                Place(residents["garage"]+Vector3.forward*5);Interact("garage");yield return new WaitForSeconds(.3f);Capture("01-mara");
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
                Place(residents["post"]+Vector3.forward*5);Interact("post");if(!ui.InvokeChoice("Deliver"))throw new Exception("Delivery action missing.");
                Place(residents["garage"]+Vector3.forward*5);Interact("garage");if(!ui.InvokeChoice("Collect 350"))throw new Exception("Reward action missing.");Close();
                // This integration test relocates between places; it does not
                // claim a manually driven or controller-tested adventure route.
                foreach(string id in new[]{"postcard_0","postcard_1"}) {Place(finds[id].transform.position);yield return new WaitForSeconds(.15f);}
                // Buy at Sid's parts bay (the shop can't fit), then fit at Mara's garage bay.
                Place(residents["parts"]+Vector3.forward*5);Interact("parts");ui.InvokeChoice("Buy parts");
                browser.SelectPart("engine_workshop");
                if(!browser.InvokeAction("buy"))throw new Exception("Workshop engine purchase action missing.");
                if(browser.InvokeAction("fit")||Store.Data.Loadout()["engine"]!="engine_stock")throw new Exception("The parts shop fitted a part.");
                browser.InvokeAction("back");Close();
                Place(residents["garage"]+Vector3.forward*5);Interact("garage");ui.InvokeChoice("Fit parts");
                browser.SelectPart("engine_workshop");
                if(!browser.InvokeAction("fit"))throw new Exception("Workshop engine fit action missing.");
                yield return new WaitForSeconds(.3f);Capture("02-garage");
                if(Store.Data.Loadout()["engine"]!="engine_workshop"||Store.Data.credits!=0||player.tuning.power<=CarTuning.Stock.power)throw new Exception("Equipped upgrade did not apply.");
                Close();Place(residents["cafe"]+Vector3.forward*5);Interact("cafe");AdventureSession.SmokePhase=1;ui.InvokeChoice("Race Bayview");yield return null;if(folder!=null)racePreview.Capture(Path.Combine(folder,"02b-race-preview.png"));racePreview.Confirm();
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
