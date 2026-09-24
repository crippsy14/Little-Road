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
        RaceTrial world;RoadCar player;Camera cameraView;AdventureUI ui;PartsBrowser browser;RacePreviewUI racePreview;
        readonly Dictionary<string,Vector3> residents=new();
        readonly Dictionary<string,GameObject> finds=new();
        string nearest;float interactionReadyTime;
        public static readonly Color[] Paints={new Color(.08f,.56f,.49f),new Color(.88f,.36f,.17f),new Color(.30f,.45f,.75f),new Color(.88f,.70f,.25f)};
        AdventureStore Store=>AdventureSession.Store;
        bool reviewing,wasLookingBehind;
        IEnumerator Start()
        {
            ui=gameObject.AddComponent<AdventureUI>();browser=gameObject.AddComponent<PartsBrowser>();racePreview=gameObject.AddComponent<RacePreviewUI>();
            try{AdventureSession.Load();}catch(Exception e){ui.Show("Your save needs attention",e.Message+"\nThe existing save has been left intact.",new List<AdventureUI.Choice>{new("Quit",()=>Application.Quit())});yield break;}
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.fixedDeltaTime=1f/120;
            world=GetComponent<RaceTrial>();world.freeRoam=true;world.track=new Circuit();world.elapsed=1;TrialWorld.Create(world.track,false);
            residents["garage"]=new Vector3(-62,0,-100);residents["cafe"]=new Vector3(-17,0,-99);
            residents["post"]=world.track.At(world.track.length*.46f)-world.track.Right(world.track.length*.46f)*16;
            CreatePlaces();
            var car=TrialWorld.Car(Store.Data.bodyId,Paints[Store.Data.paintIndex]);car.transform.SetPositionAndRotation(new Vector3(-63,.15f,-94),Quaternion.Euler(0,90,0));
            player=car.AddComponent<RoadCar>();player.Setup(world,0,EquipmentCatalog.Stats(Store.Data.Loadout()),false,1709,"owned equipment");world.cars.Add(player);world.running=true;
            if(RaceTrial.Has("--vertical-audit"))gameObject.AddComponent<VerticalMotionAudit>().Setup(world);
            cameraView=new GameObject("Adventure camera").AddComponent<Camera>();cameraView.tag="MainCamera";cameraView.fieldOfView=55;cameraView.nearClipPlane=.15f;cameraView.farClipPlane=1500;
            cameraView.gameObject.AddComponent<AudioListener>();var cd=cameraView.GetUniversalAdditionalCameraData();cd.renderPostProcessing=true;cd.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            CameraPose(true);if(!string.IsNullOrEmpty(AdventureSession.Notice)){Notify(AdventureSession.Notice);AdventureSession.Notice=null;}
            else if(Store.Data.questStage==0)Notify("Welcome to Bayview · Mara has a parcel for the lighthouse");
            Debug.Log("LITTLE_ROAD_ADVENTURE_READY save="+Store.path+" quest="+Store.Data.questStage+" credits="+Store.Data.credits);
            if(RaceTrial.Has("--geometry-audit-only")){Application.Quit();yield break;}
            if(RaceTrial.Has("--world-review")){RefreshHUD();reviewing=true;player.controlsBlocked=true;gameObject.AddComponent<VillageReview>();yield break;}
            if(RaceTrial.Has("--vehicle-lights-review")){reviewing=true;gameObject.AddComponent<VehicleLightReview>();yield break;}
            if(RaceTrial.Has("--evening-route-review")){gameObject.AddComponent<EveningRouteReview>();yield break;}
            if(RaceTrial.Has("--parts-browser-review")){Parts("engine");gameObject.AddComponent<PartsBrowserReview>().Setup(browser,Store,player,ui,()=>Parts("engine"));yield break;}
            yield return null;if(AdventureSession.Smoke)StartCoroutine(Smoke());
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
        void CreatePlaces()
        {
            foreach(var pair in residents) {
                Color color=pair.Key=="garage"?new Color(.85f,.38f,.19f):pair.Key=="post"?new Color(.9f,.7f,.23f):new Color(.38f,.48f,.68f);
                var npc=TrialWorld.Car(pair.Key=="cafe"?"otter":pair.Key=="garage"?"kipper":"pebble",color);npc.name="Resident "+pair.Key;npc.transform.position=pair.Value+Vector3.up*.05f;
                var renderers=npc.GetComponentsInChildren<Renderer>();Bounds visualBounds=renderers[0].bounds;foreach(var renderer in renderers)visualBounds.Encapsulate(renderer.bounds);
                var bounds=npc.AddComponent<BoxCollider>();bounds.center=visualBounds.center-npc.transform.position;bounds.size=visualBounds.size+Vector3.one*.02f;npc.tag="Respawn";
                npc.transform.position+=Vector3.up*(.018f-visualBounds.min.y);
                string name=pair.Key=="garage"?"MARA / GARAGE":pair.Key=="post"?"PIP / LIGHTHOUSE":"ROWAN / COAST RUN";
                Sign(name,name,pair.Value+Vector3.up*3);
            }
            var post=residents["post"];var stone=new Color(.91f,.83f,.65f);var red=new Color(.71f,.26f,.18f);
            // Keep the landmark behind its resident, away from the nearest
            // carriageway; a world-Z offset can move it into a curved road.
            float road=world.track.Nearest(post,out _);Vector3 a=world.track.At(road);
            Vector3 tower=post+(post-a).normalized*9;
            Block("Lighthouse footing",tower+Vector3.up*.3f,new Vector3(9,.6f,9),stone,true,PrimitiveType.Cylinder);
            Block("Lighthouse tower",tower+Vector3.up*5,new Vector3(4,5,4),stone,true,PrimitiveType.Cylinder);
            Block("Lighthouse red band",tower+Vector3.up*7.4f,new Vector3(4.08f,.55f,4.08f),red,false,PrimitiveType.Cylinder);
            Block("Lantern balcony",tower+Vector3.up*10,new Vector3(6,.25f,6),stone,false,PrimitiveType.Cylinder);
            Block("Lantern housing",tower+Vector3.up*11,new Vector3(3.4f,1,3.4f),new Color(.34f,.65f,.68f),false,PrimitiveType.Cylinder);
            Block("Lantern cap",tower+Vector3.up*12.3f,new Vector3(5,.35f,5),red,false,PrimitiveType.Cylinder);
            var lamp=new GameObject("Lighthouse beacon").AddComponent<Light>();lamp.transform.position=tower+Vector3.up*11;lamp.type=LightType.Point;lamp.color=new Color(1,.81f,.44f);lamp.range=20;lamp.intensity=4;
            Vector3 outward=(post-a).normalized;
            Vector3 laneStart=a+outward*12,laneEnd=post+outward*2.5f,delta=laneEnd-laneStart;
            var spur=Block("Lighthouse lane",(laneStart+laneEnd)*.5f+Vector3.up*.043f,new Vector3(6,.04f,delta.magnitude),new Color(.66f,.62f,.46f));spur.transform.rotation=Quaternion.LookRotation(delta);
            for(int i=0;i<5;i++) {
                string id="postcard_"+i;if(Store.Data.collected.Contains(id))continue;
                float d=world.track.length*(.12f+i*.17f);Vector3 p=world.track.At(d)+world.track.Right(d)*(i%2==0?13:-13)+Vector3.up*1.1f;
                finds[id]=Block(id,p,new Vector3(.8f,.65f,.10f),new Color(1,.75f,.22f));
            }
        }
        void Update()
        {
            if(player==null||reviewing||browser.IsOpen||racePreview.IsOpen||racePreview.LastCloseFrame==Time.frameCount||browser.LastCloseFrame==Time.frameCount)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true) {if(ui.IsOpen)Close();else Pause();}
            if(ui.IsOpen){RefreshHUD();return;}
            nearest=residents.OrderBy(p=>Vector3.Distance(player.body.position,p.Value)).First().Key;
            if(Vector3.Distance(player.body.position,residents[nearest])>7||Mathf.Abs(player.speed)>1.2f)nearest=null;
            if(Time.unscaledTime>interactionReadyTime&&(k?.eKey.wasPressedThisFrame==true||pad?.buttonSouth.wasPressedThisFrame==true))if(nearest!=null)Interact(nearest);
            if(k?.rKey.wasPressedThisFrame==true||pad?.dpad.down.wasPressedThisFrame==true)player.TryRecover();
            foreach(var pair in finds.ToArray()) {
                pair.Value.transform.Rotate(0,35*Time.deltaTime,0,Space.World);
                if(Vector3.Distance(player.body.position,pair.Value.transform.position)<2.7f)Attempt(()=>{
                    if(Store.Collect(pair.Key)){Destroy(pair.Value);finds.Remove(pair.Key);Notify("Bayview postcard found · +50 credits");}});
            }
            RefreshHUD();
        }
        void RefreshHUD()
        {
            string[] objectives={"Visit Mara at Harbour Garage","Deliver Mara's parcel to Pip at the lighthouse","Return to Mara for 350 credits","Explore, collect, upgrade and enter Rowan's coast race"};
            string target=Store.Data.questStage==1?"post":Store.Data.questStage<3?"garage":"cafe";
            float distance=Vector3.Distance(player.transform.position,residents[target]);
            ui.HUD($"BAYVIEW   ·   {Store.Data.credits} credits   ·   {Store.Data.collected.Count}/5 postcards   ·   {Mathf.RoundToInt(Mathf.Abs(player.speed)*2.237f)} mph\n{objectives[Store.Data.questStage]}  ·  {distance:0} m",
                ui.IsOpen?"Arrows / D-pad: choose     Enter / Cross: confirm     Esc / Circle: back to the road":nearest!=null?"E / Cross: talk     Esc / Circle: pause":"WASD / arrows or stick + R2/L2: drive     Hold Q / Triangle: look behind     E / Cross: talk     R / D-pad Down: recover     Esc / Circle: pause");
        }
        void LateUpdate(){if(!reviewing&&player!=null&&cameraView!=null)CameraPose(false);}
        void CameraPose(bool snap)
        {
            bool behind=!ui.IsOpen&&!browser.IsOpen&&DrivingViewInput.LookingBehind;
            snap|=behind!=wasLookingBehind;wasLookingBehind=behind;
            var viewingForward=player.transform.forward*(behind?-1:1);
            Vector3 target=player.transform.position+Vector3.up*.9f;
            Vector3 desired=player.transform.position-viewingForward*6.5f+Vector3.up*3.4f;
            Vector3 delta=desired-target;
            foreach(var hit in Physics.SphereCastAll(target,.2f,delta.normalized,delta.magnitude))
                if(!hit.collider.transform.IsChildOf(player.transform)&&hit.distance>1)desired=target+delta.normalized*Mathf.Min(Vector3.Distance(target,desired),hit.distance-.25f);
            cameraView.transform.position=snap?desired:Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-Time.deltaTime*8));cameraView.transform.LookAt(target+viewingForward*1.7f);
        }
        void Notify(string value)=>ui.Toast(value);
        void Attempt(Action action){try{action();}catch(Exception e){Debug.LogError(e);Show("Could not save that change",e.Message+"\nYour previous progress is still loaded.",new(){new("Back to the road",Close)});}}
        void Show(string title,string description,List<AdventureUI.Choice> choices){player.controlsBlocked=true;ui.Show(title,description,choices);}
        void Close(){if(browser.IsOpen)browser.Hide();ui.SetVisible(true);ui.Close();player.controlsBlocked=false;interactionReadyTime=Time.unscaledTime+.25f;}
        void Pause()=>Show("Bayview / paused","Progress saves after each reward, purchase and equipment change. Menus support mouse, keyboard arrows/Enter and controller D-pad/Cross. F11 switches fullscreen/windowed.",new(){new("Back to the road",Close),new("Switch fullscreen / windowed",DisplaySettings.Toggle),new("Quit",()=>Application.Quit())});
        public bool Interact(string id)
        {
            if(!residents.ContainsKey(id)||Vector3.Distance(player.body.position,residents[id])>7||Mathf.Abs(player.speed)>1.2f)return false;
            if(id=="garage")Mara();else if(id=="post")Pip();else Rowan();return true;
        }
        void Mara()
        {
            var choices=new List<AdventureUI.Choice>();int stage=Store.Data.questStage;
            if(stage==0)choices.Add(new("I'll take the parcel",()=>Attempt(()=>{Store.AcceptDelivery();Close();Notify("Parcel aboard. Follow the coast road to Pip's lighthouse.");})));
            if(stage==2)choices.Add(new("Collect 350 credits",()=>Attempt(()=>{Store.ClaimDelivery();Mara();})));
            choices.Add(new("Parts & fitted equipment",Garage));choices.Add(new("Try the next paint colour · free",()=>Attempt(()=>{Store.Change(p=>{p.paintIndex=(p.paintIndex+1)%4;return true;});ApplyEquipment();})));
            choices.Add(new("Back to the road",Close));
            Show("Mara / Harbour Garage",stage==0?"Pip needs a parcel at the lighthouse. Take the coast road and look for the red lantern cap. I'll have 350 credits for you when you return.":stage==1?"Pip is by the lighthouse on the far side of the loop. There are postcards beside the road if you take a detour.":stage==2?"Pip sent word! The parcel arrived safely. Here's something towards making this car your own.":"A good car becomes your own, one improvement at a time. What shall we work on?",choices);
        }
        void Pip()
        {
            var choices=new List<AdventureUI.Choice>();if(Store.Data.questStage==1)choices.Add(new("Deliver Mara's parcel",()=>Attempt(()=>{Store.Deliver();Close();Notify("Delivery complete. Return to Mara for your reward.");})));
            choices.Add(new("Back to the road",Close));Show("Pip / Lighthouse Post","You found the lighthouse! Some drivers only see the road. Others find the little places beside it. Keep an eye out for Bayview postcards.",choices);
        }
        void Rowan()=>Show("Rowan / The Daily Mile","Bayview Coast · Class C · 24 cars · 3 laps\nEvery valid finish pays 100–500 credits. Rival equipment stays fixed as you improve your car. Finishing in the top twelve earns qualification for the planned Quarry Link event; that track is not built yet.",new(){new("Race Bayview Coast · C",PreviewRace),new("Back to the road",Close)});
        void PreviewRace()
        {
            player.controlsBlocked=true;ui.Close();ui.SetVisible(false);
            var roster=RaceRoster.Adventure(Store.Data);
            racePreview.Open(roster,()=>Attempt(()=>AdventureSession.StartRace(roster)),()=>{ui.SetVisible(true);Rowan();});
        }
        void Garage()
        {
            var choices=EquipmentCatalog.All.slots.Select(slot=>new AdventureUI.Choice(slot.ToUpperInvariant()+" · "+EquipmentCatalog.Get(Store.Data.Loadout()[slot]).name,()=>Parts(slot))).ToList();
            choices.Add(new("Back to Mara",Mara));Show("Harbour Garage / "+Store.Data.credits+" credits","Buy parts to own them, then fit them separately. Owned parts can be refitted for free. Engine power, gearing and tyre grip work together.",choices);
        }
        void Parts(string slot)
        {
            if(slot=="engine") {
                ui.Close();ui.SetVisible(false);player.controlsBlocked=true;
                browser.Open(Store,ApplyEquipment,()=>{ui.SetVisible(true);Garage();},other=>{ui.SetVisible(true);Parts(other);});return;
            }
            var choices=new List<AdventureUI.Choice>();
            foreach(var part in EquipmentCatalog.All.parts.Where(p=>p.slot==slot).OrderBy(p=>p.price)) {
                bool owned=Store.Data.owned.Contains(part.id),fitted=Store.Data.Loadout()[slot]==part.id;
                choices.Add(new((fitted?"Fitted · ":owned?"Fit · ":"Buy · ")+part.name+(owned?"":" · "+part.price+" cr"),()=>Attempt(()=>{
                    if(owned){Store.Fit(part.id);ApplyEquipment();}else if(!Store.Buy(part.id))Notify("Not enough credits. Every completed race earns more.");Parts(slot);
                }),!fitted&&(owned||Store.Data.credits>=part.price)));
            }
            choices.Add(new("Back to all parts",Garage));
            string notes=string.Join("\n",EquipmentCatalog.All.parts.Where(p=>p.slot==slot&&p.price>0).OrderBy(p=>p.price).Select(p=>p.name+": "+p.hint));
            Show(slot.ToUpperInvariant()+" / "+Store.Data.credits+" credits",notes,choices);
        }
        void ApplyEquipment()
        {
            player.tuning=EquipmentCatalog.Stats(Store.Data.Loadout());player.body.mass=850*player.tuning.mass;
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
                if(!EventSystem.current.currentSelectedGameObject.name.Contains("Parts &"))throw new Exception("Controller menu down navigation failed.");
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
                Place(residents["garage"]+Vector3.forward*5);Interact("garage");ui.InvokeChoice("Parts &");ui.InvokeChoice("ENGINE");
                browser.SelectPart("engine_workshop");
                if(!browser.InvokeAction("buy"))throw new Exception("Workshop engine purchase action missing.");
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
