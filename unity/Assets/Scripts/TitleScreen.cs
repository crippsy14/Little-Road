using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // The title screen (Richard, 30 Sep: an intro screen with save management). Shown once per launch, before the
    // world builds: Continue (the newest autosave), New game (pick a slot), Load game (a slot's autosave or its
    // saved game) and Quit. Slots and files: SaveSlots. Skipped for runs with review or test flags, and for
    // --adventure-save; --title shows it anyway, --title-review <folder> captures it and quits.
    public sealed class TitleScreen:MonoBehaviour
    {
        public static bool Wanted
        {
            get {
                if(Application.isBatchMode||AdventureSession.Store!=null||RaceTrial.Has("--no-title"))return false;
                if(RaceTrial.Has("--title")||RaceTrial.Has("--title-review"))return true;
                return Environment.GetCommandLineArgs().Skip(1).Where(a=>a.StartsWith("--")).All(a=>a=="--audio-probe");
            }
        }
        Canvas canvas;RectTransform page;Text footer;bool done;Camera backdropCamera;AudioListener ear;
        readonly List<Button> buttons=new();Action back;

        public IEnumerator Run()
        {
            SaveSlots.Migrate();Build();AudioDirector.Ensure();ShowMain();
            if(RaceTrial.Has("--title-review")){yield return Review(RaceTrial.Arg("--title-review"));yield break;}
            while(!done)yield return null;
            // The camera and its ear stay until the world has its own (Dismiss): the theme plays on through loading.
            Destroy(canvas.gameObject);
        }
        public void Dismiss(){if(backdropCamera!=null)Destroy(backdropCamera.gameObject);Destroy(this);}
        IEnumerator Review(string folder)
        {
            Directory.CreateDirectory(folder);
            yield return new WaitForSecondsRealtime(1);Toy.Capture(Path.Combine(folder,"title-main.png"));
            ShowSlots(false);yield return new WaitForSecondsRealtime(.6f);Toy.Capture(Path.Combine(folder,"title-load.png"));
            ShowSlots(true);yield return new WaitForSecondsRealtime(.6f);Toy.Capture(Path.Combine(folder,"title-new.png"));
            Debug.Log("LITTLE_ROAD_TITLE_REVIEW "+folder);Application.Quit(0);
        }

        void Build()
        {
            // Before the world exists: a plain camera (the screen and captures need one) and an ear for the theme.
            backdropCamera=new GameObject("Title camera").AddComponent<Camera>();backdropCamera.tag="MainCamera";
            backdropCamera.clearFlags=CameraClearFlags.SolidColor;backdropCamera.backgroundColor=Toy.Sky;backdropCamera.cullingMask=0;
            ear=backdropCamera.gameObject.AddComponent<AudioListener>();
            var root=new GameObject("Title screen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=900;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var bg=Toy.Rect(root.transform,"Title backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,-120),Toy.Sky,Toy.SkyLight,30,3);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);Toy.Bunting(bg,1920,470,16);
            Toy.Car(bg,"Title car red",new Vector2(300,150),new Vector2(-700,-330),Toy.Red);
            Toy.Car(bg,"Title car mint",new Vector2(260,130),new Vector2(720,-350),Toy.Mint).rectTransform.localScale=new Vector3(-1,1,1);
            page=Toy.Rect(root.transform,"Page",new Vector2(1920,1080),Vector2.zero);
            footer=Toy.Label(root.transform,"Footer",string.Empty,24,new Vector2(1400,40),new Vector2(0,-470),Toy.Navy,TextAnchor.MiddleCenter,true);
        }
        void Clear(){for(int i=page.childCount-1;i>=0;i--)Destroy(page.GetChild(i).gameObject);buttons.Clear();}
        Button Add(Button b){buttons.Add(b);return b;}
        void Focus()
        {
            var first=buttons.FirstOrDefault(b=>b.interactable);
            StartCoroutine(Select(first));
        }
        IEnumerator Select(Button b){yield return null;if(b!=null)EventSystem.current?.SetSelectedGameObject(b.gameObject);}
        static void Grey(Button b){b.interactable=false;b.GetComponent<ToyButton>()?.Refresh(true);}

        // ---------------------------------------------------------------- pages
        void ShowMain()
        {
            Clear();back=null;
            Toy.Title(page,"Game title","LITTLE ROAD",170,new Vector2(1400,200),new Vector2(0,250),Toy.Sun,TextAnchor.MiddleCenter,-2);
            Toy.Pill(page,"Game subtitle","ROAD TRIPS  ·  RACING  ·  ADVENTURE",26,new Vector2(620,52),new Vector2(0,125),Toy.Navy,Toy.White,-2);
            int newest=SaveSlots.Newest();
            var cont=Add(Toy.Candy(page,"Choice Continue","Continue",null,new Vector2(560,90),new Vector2(0,20),Toy.Mint,()=>Play(newest,false)));
            if(newest==0)Grey(cont);
            else {
                var info=SaveSlots.Read(SaveSlots.Auto(newest));if(!info.exists)info=SaveSlots.Read(SaveSlots.Manual(newest));
                Toy.Label(page,"Continue note",$"Slot {newest}  ·  {info.summary}  ·  {info.When}",22,new Vector2(1100,34),new Vector2(0,-62),Toy.Navy,TextAnchor.MiddleCenter,true);
                if(info.damaged)Grey(cont);
            }
            Add(Toy.Candy(page,"Choice New game","New game",null,new Vector2(560,84),new Vector2(0,-140),Toy.Blue,()=>ShowSlots(true)));
            var load=Add(Toy.Candy(page,"Choice Load game","Load game",null,new Vector2(560,84),new Vector2(0,-240),Toy.Orange,()=>ShowSlots(false)));
            if(newest==0)Grey(load);
            Add(Toy.Candy(page,"Choice Quit","Quit",null,new Vector2(560,84),new Vector2(0,-340),Toy.Red,Application.Quit));
            footer.text="Enter / A: choose";Focus();
        }
        // Three slot cards: New game starts in one (asking first if it's in use); Load game opens its autosave or
        // its saved game.
        void ShowSlots(bool fresh)
        {
            Clear();back=ShowMain;
            Toy.Title(page,"Page title",fresh?"NEW GAME":"LOAD GAME",96,new Vector2(1200,120),new Vector2(0,345),Toy.Sun,TextAnchor.MiddleCenter,-2);
            Toy.Label(page,"Page note",fresh?"Pick a slot. The game autosaves there as you play; save a copy from Settings any time.":"Each slot keeps an autosave and the game you last saved.",
                26,new Vector2(1400,40),new Vector2(0,262),Toy.Navy,TextAnchor.MiddleCenter,true);
            int current=SaveSlots.Current;
            for(int s=1;s<=SaveSlots.Count;s++) {
                int slot=s;float x=(s-2)*580;
                var auto=SaveSlots.Read(SaveSlots.Auto(s));var manual=SaveSlots.Read(SaveSlots.Manual(s));var shown=auto.exists?auto:manual;
                var card=Toy.Card(page,"Slot "+s,new Vector2(530,500),new Vector2(x,-20),Toy.Cream,40,7,10);
                Toy.Title(card.transform,"Slot name","SLOT "+s,54,new Vector2(460,70),new Vector2(0,195),s==current&&shown.exists?Toy.Mint:Toy.Sun,TextAnchor.MiddleCenter);
                if(s==current&&shown.exists)Toy.Pill(card.transform,"Last played","LAST PLAYED",18,new Vector2(170,34),new Vector2(170,232),Toy.Navy,Toy.White,4);
                string summary=shown.exists?shown.summary.Replace(" · ","\n"):"Empty";
                Toy.Label(card.transform,"Summary",summary,26,new Vector2(460,150),new Vector2(0,80),Toy.Navy,TextAnchor.MiddleCenter,true);
                string times=(auto.exists?"Autosave: "+auto.When:"No autosave")+"\n"+(manual.exists?"Saved: "+manual.When:"Not saved yet");
                Toy.Label(card.transform,"Times",times,22,new Vector2(460,64),new Vector2(0,-32),Toy.Soft,TextAnchor.MiddleCenter,false);
                if(fresh) {
                    var b=Add(Toy.Candy(card.transform,"Choice Start slot "+s,shown.exists?"Start over":"Start here",null,new Vector2(440,76),new Vector2(0,-170),shown.exists?Toy.Orange:Toy.Mint,
                        ()=>{if(SaveSlots.Occupied(slot))ConfirmNew(slot);else{SaveSlots.NewGame(slot);Finish();}}));
                }
                else {
                    var a=Add(Toy.Candy(card.transform,"Choice Autosave "+s,"Autosave",null,new Vector2(440,66),new Vector2(0,-122),Toy.Mint,()=>Play(slot,false)));
                    var m=Add(Toy.Candy(card.transform,"Choice Saved "+s,"Saved game",null,new Vector2(440,66),new Vector2(0,-198),Toy.Blue,()=>Play(slot,true)));
                    if(!auto.exists||auto.damaged)Grey(a);if(!manual.exists||manual.damaged)Grey(m);
                }
            }
            Add(Toy.Candy(page,"Choice Back","Back",null,new Vector2(360,76),new Vector2(0,-365),Toy.Red,ShowMain));
            footer.text="Enter / A: choose   ·   Esc / B: back";Focus();
        }
        void ConfirmNew(int slot)
        {
            Clear();back=()=>ShowSlots(true);
            Toy.Title(page,"Confirm title",$"START OVER IN SLOT {slot}?",80,new Vector2(1400,110),new Vector2(0,190),Toy.Sun,TextAnchor.MiddleCenter,-2);
            Toy.Label(page,"Confirm note","The game in this slot is replaced by a new one (a backup copy is kept on disk).",28,new Vector2(1300,44),new Vector2(0,95),Toy.Navy,TextAnchor.MiddleCenter,true);
            Add(Toy.Candy(page,"Choice Keep","Keep that game",null,new Vector2(520,84),new Vector2(0,-20),Toy.Mint,()=>ShowSlots(true)));
            Add(Toy.Candy(page,"Choice Start over","Start a new game",null,new Vector2(520,84),new Vector2(0,-120),Toy.Red,()=>{SaveSlots.NewGame(slot);Finish();}));
            footer.text="Enter / A: choose   ·   Esc / B: back";Focus();
        }
        void Play(int slot,bool manual)
        {
            if(slot<=0)return;
            // Loading an older saved game over a newer autosave asks first.
            if(manual&&!confirming&&File.Exists(SaveSlots.Auto(slot))&&File.GetLastWriteTime(SaveSlots.Auto(slot))>File.GetLastWriteTime(SaveSlots.Manual(slot)).AddSeconds(5)){ConfirmOlder(slot);return;}
            SaveSlots.Load(slot,manual);Finish();
        }
        bool confirming;
        void ConfirmOlder(int slot)
        {
            Clear();back=()=>{confirming=false;ShowSlots(false);};confirming=true;
            Toy.Title(page,"Confirm title","LOAD THE OLDER SAVE?",80,new Vector2(1400,110),new Vector2(0,190),Toy.Sun,TextAnchor.MiddleCenter,-2);
            Toy.Label(page,"Confirm note",$"Slot {slot}'s autosave is newer than its saved game. Loading the saved game replaces the autosave (a backup copy is kept).",28,new Vector2(1300,90),new Vector2(0,85),Toy.Navy,TextAnchor.MiddleCenter,true);
            Add(Toy.Candy(page,"Choice Autosave","Play the autosave",null,new Vector2(560,84),new Vector2(0,-40),Toy.Mint,()=>{confirming=false;Play(slot,false);}));
            Add(Toy.Candy(page,"Choice Saved","Load the saved game",null,new Vector2(560,84),new Vector2(0,-140),Toy.Orange,()=>{SaveSlots.Load(slot,true);confirming=false;Finish();}));
            footer.text="Enter / A: choose   ·   Esc / B: back";Focus();
        }
        void Finish(){Clear();footer.text="";done=true;}

        void Update()
        {
            if(done||back==null)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true)back();
        }
    }
}
