using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LittleRoad
{
    public sealed class RacePreviewRow:MonoBehaviour,ISelectHandler
    {
        public Action selected;
        public void OnSelect(BaseEventData data)=>selected?.Invoke();
    }
    // "Race Day!" pre-race screen: the whole 24-car grid as toy tiles, the selected
    // car on a sunburst turntable, its rating, potential and fitted parts.
    // L1/R1 jump eight grid places ("pages"), Square opens the paddock inspector.
    public sealed class RacePreviewUI:MonoBehaviour
    {
        const int Rows=24;
        Canvas canvas;RaceRoster roster;RaceEntrant[] grid;PartPreviewStage stage;
        Action confirm,cancel;Text driverName,gridLabel,rating,bodyLabel,bubble,speedValue,accelValue,rankValue;
        RectTransform speedBar,accelBar,rankBar;RawImage car;readonly Button[] rows=new Button[Rows];readonly ToyShape[] ratingPills=new ToyShape[Rows];
        Button start,back,inspect,tourButton;int selected;float nextSpin,tourStart,maxSpeed,bestSprint;bool touring;
        readonly Text[] partNames=new Text[7];
        RacePaddockInspector inspector;
        public RacePaddockInspector Inspector=>inspector;
        public float DisplayCarAngle=>stage?.CarAngle??0;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int SelectedEntrant=>selected;
        public int LastCloseFrame {get;private set;}=-1;
        public int Page=>roster==null?0:roster.Entries[selected].Slot/8;
        public RaceRoster Roster=>roster;
        public string DisplayedParts=>string.Join("\n",partNames.Select(t=>t.text));
        public string DisplayedRating=>rating.text+" PP";
        public void FocusStart()=>EventSystem.current.SetSelectedGameObject(start.gameObject);
        static readonly string[] SlotCodes={"EN","TU","TY","GB","BR","ST","CH"};
        static readonly Color[] SlotColours={Toy.Red,Toy.Orange,Toy.Navy,Toy.Blue,Toy.Pink,Toy.Mint,Toy.Sun};

        public void Open(RaceRoster race,Action onStart,Action onBack)
        {
            if(race.Entries.Count!=Rows)throw new ArgumentException("Race preview requires the full 24-car event.");
            stage?.Dispose();stage=null;
            roster=race;grid=race.Entries.OrderBy(e=>e.Slot).ToArray();confirm=onStart;cancel=onBack;
            maxSpeed=race.Entries.Max(e=>e.Tuning.AttainableSpeed);bestSprint=race.Entries.Min(e=>PartsBrowser.Sprint(e.Tuning));
            if(canvas==null)Build();
            Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);
            RefreshTiles();selected=-1;SelectEntry(0);StartCoroutine(Focus());SetTour(false);
        }
        IEnumerator Focus(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(rows[roster.Entries[selected].Slot].gameObject);}
        void Build()
        {
            if(EventSystem.current==null){var go=new GameObject("Race menu input",typeof(EventSystem),typeof(InputSystemUIInputModule));go.transform.SetParent(transform,false);go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            var move=EventSystem.current.GetComponent<InputSystemUIInputModule>()?.move?.action;
            if(move!=null)for(int i=0;i<move.bindings.Count;i++)if(move.bindings[i].path?.StartsWith("<Gamepad>/rightStick")==true)move.ApplyBindingOverride(i,"");
            canvas=CoastUI.Canvas(transform,"Race day",50);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=.5f;
            canvas.gameObject.layer=Toy.MenuLayer;canvas.gameObject.AddComponent<GraphicRaycaster>();
            var root=Toy.Rect(canvas.transform,"Race day root",new Vector2(1920,1080),Vector2.zero);root.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(root,new Vector2(2600,2600),new Vector2(0,-120),Toy.Sky,Toy.SkyLight,32,3);
            Toy.Bunting(root,1920,505,18);Toy.Checker(root,1920,-525);

            Toy.Title(root,"Heading","RACE DAY!",104,new Vector2(640,120),new Vector2(-600,398),Toy.White,TextAnchor.MiddleLeft,3);
            Toy.Title(root,"Venue","Bayview Coast Cup",50,new Vector2(640,64),new Vector2(-600,308),Toy.Sun,TextAnchor.MiddleLeft,3);
            var badges=Toy.Rect(root,"Race badges",new Vector2(320,130),new Vector2(-250,372));badges.localRotation=Quaternion.Euler(0,0,3);
            Toy.Pill(badges,"Class","CLASS C",22,new Vector2(150,50),new Vector2(-80,30),Toy.Red,Toy.White);
            Toy.Pill(badges,"Laps","3 LAPS",22,new Vector2(150,50),new Vector2(80,30),Toy.White,Toy.Navy);
            Toy.Pill(badges,"Field","24 CARS",22,new Vector2(150,50),new Vector2(-80,-32),Toy.White,Toy.Navy);
            Toy.Pill(badges,"Weather","SUNNY",20,new Vector2(150,50),new Vector2(80,-32),Toy.Mint,Toy.Navy);

            // Whole grid, two columns of twelve.
            var board=Toy.Card(root,"Starting grid",new Vector2(860,640),new Vector2(-495,-70),Toy.Cream,34,7,10);
            Toy.Title(board.transform,"Grid heading","STARTING GRID",40,new Vector2(420,56),new Vector2(-190,272),Toy.Orange);
            var legend=Toy.Rect(board.transform,"Legend",new Vector2(380,40),new Vector2(215,272));
            Toy.Pill(legend,"Slower","slower",17,new Vector2(96,34),new Vector2(-150,0),Toy.Slower,Toy.Navy);
            Toy.Pill(legend,"Same","same",17,new Vector2(84,34),new Vector2(-56,0),Toy.White,Toy.Navy);
            Toy.Pill(legend,"Faster","faster",17,new Vector2(92,34),new Vector2(36,0),Toy.Faster,Toy.Navy);
            Toy.Label(legend,"Than you","than you",17,new Vector2(100,34),new Vector2(140,0),Toy.Navy,TextAnchor.MiddleLeft,true);
            for(int i=0;i<Rows;i++){
                int slot=i;int column=i/12,row=i%12;var tile=Tile(board.transform,new Vector2(398,40),new Vector2(-204+column*410,222-row*44),slot);rows[i]=tile;
            }
            for(int i=0;i<Rows;i++){
                int column=i/12,row=i%12;
                rows[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=row>0?rows[i-1]:null,selectOnDown=row<11?rows[i+1]:null,selectOnLeft=column==1?rows[i-12]:null,selectOnRight=column==0?rows[i+12]:null};
            }

            // Selected car.
            var panel=Toy.Card(root,"Entrant detail",new Vector2(1010,780),new Vector2(435,40),Toy.Cream,40,8,12);
            driverName=Toy.Title(panel.transform,"Driver",string.Empty,58,new Vector2(640,80),new Vector2(-150,320),Toy.Sun);
            gridLabel=Toy.Label(Toy.Card(panel.transform,"Grid slot",new Vector2(190,56),new Vector2(365,322),Toy.Navy,28,0,4).transform,"Grid text",string.Empty,26,new Vector2(170,56),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            var turntable=Toy.Card(panel.transform,"Turntable",new Vector2(520,430),new Vector2(-225,70),Toy.Sun,215,7,0);
            var sun=Toy.Sunburst(turntable.transform,new Vector2(506,416),Vector2.zero,Toy.Sun,Toy.Hex(0xffe58a),18,6);sun.circular=true;
            var shadow=Toy.Card(turntable.transform,"Car shadow",new Vector2(320,44),new Vector2(0,-120),new Color(.11f,.17f,.39f,.2f),22,0,0);
            car=Toy.Rect(turntable.transform,"Actual entered car",new Vector2(520,289),new Vector2(0,-6)).gameObject.AddComponent<RawImage>();car.raycastTarget=false;
            var bob=car.gameObject.AddComponent<ToyBob>();bob.height=8;bob.speed=2.2f;
            bodyLabel=Toy.Label(panel.transform,"Body",string.Empty,22,new Vector2(500,34),new Vector2(-225,-170),Toy.Soft,TextAnchor.MiddleCenter,true);

            var rosette=Toy.Card(panel.transform,"Rating rosette",new Vector2(160,160),new Vector2(120,212),Toy.Red,80,7,7);rosette.rectTransform.localRotation=Quaternion.Euler(0,0,-8);
            rating=Toy.Title(rosette.transform,"Rating",string.Empty,58,new Vector2(160,86),new Vector2(0,12),Toy.White,TextAnchor.MiddleCenter);
            Toy.Label(rosette.transform,"PP","PP",22,new Vector2(160,30),new Vector2(0,-40),Toy.White,TextAnchor.MiddleCenter,true);
            speedValue=StatBar(panel.transform,"TOP SPEED",new Vector2(300,82),Toy.Red,out speedBar);
            accelValue=StatBar(panel.transform,"ACCEL 0-30",new Vector2(300,14),Toy.Mint,out accelBar);
            rankValue=StatBar(panel.transform,"RATING RANK",new Vector2(300,-54),Toy.Pink,out rankBar);
            var speech=Toy.Card(panel.transform,"Speech bubble",new Vector2(430,110),new Vector2(250,-135),Toy.White,26,5,5);
            var tail=Toy.Card(speech.transform,"Bubble tail",new Vector2(34,34),new Vector2(-150,58),Toy.White,4,5,0);tail.rectTransform.localRotation=Quaternion.Euler(0,0,45);tail.transform.SetAsFirstSibling();
            bubble=Toy.Label(speech.transform,"Bubble text",string.Empty,20,new Vector2(390,100),Vector2.zero,Toy.Navy,TextAnchor.MiddleLeft,false);

            Toy.Label(panel.transform,"Fitted heading","FITTED FOR THIS RACE",22,new Vector2(900,32),new Vector2(0,-222),Toy.Navy,TextAnchor.MiddleLeft,true);
            for(int i=0;i<7;i++){
                var x=-405+i*135f;var sticker=Toy.Card(panel.transform,"Part "+i,new Vector2(78,78),new Vector2(x,-282),SlotColours[i],39,5,4);
                Toy.Title(sticker.transform,"Code",SlotCodes[i],30,new Vector2(78,78),new Vector2(0,2),Toy.White,TextAnchor.MiddleCenter);
                partNames[i]=Toy.Label(panel.transform,"Part name "+i,string.Empty,17,new Vector2(130,46),new Vector2(x,-348),Toy.Navy,TextAnchor.UpperCenter,true);
            }

            back=Toy.Candy(root,"BACK","BACK","O",new Vector2(270,96),new Vector2(-775,-448),Toy.Red,()=>Leave(false));
            inspect=Toy.Candy(root,"EXPLORE PADDOCK","PADDOCK","[]",new Vector2(310,96),new Vector2(-470,-448),Toy.Blue,OpenInspector);
            tourButton=Toy.Candy(root,"TOUR GRID","TOUR GRID",null,new Vector2(250,84),new Vector2(-160,-448),Toy.Sky,()=>SetTour(!touring));
            start=Toy.Candy(root,"START RACE","START RACE!","X",new Vector2(560,116),new Vector2(640,-440),Toy.Gold,()=>Leave(true),true);
            Toy.Label(root,"Controls","D-pad: browse the grid\nL1 / R1: jump 8 places",19,new Vector2(300,60),new Vector2(170,-448),Toy.Navy,TextAnchor.MiddleLeft,true);
            foreach(var b in new[]{back,inspect,tourButton,start})b.navigation=new Navigation{mode=Navigation.Mode.Automatic};
        }
        Button Tile(Transform parent,Vector2 size,Vector2 position,int slot)
        {
            var shape=Toy.Card(parent,"Grid tile",size,position,Toy.White,14,4,3);shape.raycastTarget=true;
            var button=shape.gameObject.AddComponent<Button>();button.targetGraphic=shape;button.transition=Selectable.Transition.None;
            var number=Toy.Card(shape.transform,"Slot",new Vector2(32,32),new Vector2(-176,0),Toy.Navy,16,0,0);
            Toy.Label(number.transform,"Slot number",(slot+1).ToString(),17,new Vector2(32,32),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            Toy.Car(shape.transform,"Car",new Vector2(44,29),new Vector2(-128,0),Color.white);
            Toy.Label(shape.transform,"Name",string.Empty,21,new Vector2(210,40),new Vector2(10,0),Toy.Navy,TextAnchor.MiddleLeft,true);
            ratingPills[slot]=Toy.Pill(shape.transform,"Rating","",17,new Vector2(64,28),new Vector2(158,0),Toy.White,Toy.Navy);
            shape.gameObject.AddComponent<RacePreviewRow>().selected=()=>SelectEntry(grid[slot].Id);
            shape.gameObject.AddComponent<PaddockHover>().entered=()=>{SetTour(false);EventSystem.current.SetSelectedGameObject(shape.gameObject);};
            button.onClick.AddListener(FocusStart);
            shape.gameObject.AddComponent<ToyTile>().Setup(button,shape);
            return button;
        }
        Text StatBar(Transform parent,string caption,Vector2 position,Color fill,out RectTransform bar)
        {
            var root=Toy.Rect(parent,"Stat "+caption,new Vector2(360,60),position);
            Toy.Label(root,"Caption",caption,20,new Vector2(200,26),new Vector2(-80,16),Toy.Navy,TextAnchor.MiddleLeft,true);
            var value=Toy.Label(root,"Value",string.Empty,20,new Vector2(200,26),new Vector2(80,16),Toy.Navy,TextAnchor.MiddleRight,true);
            var track=Toy.Card(root,"Bar track",new Vector2(360,26),new Vector2(0,-14),Toy.White,13,4,0);
            bar=Toy.Card(track.transform,"Bar fill",new Vector2(352,18),Vector2.zero,fill,9,0,0).rectTransform;bar.pivot=new Vector2(0,.5f);bar.anchoredPosition=new Vector2(-176,0);
            return value;
        }
        void RefreshTiles()
        {
            int own=roster.Entries[0].Rating;
            for(int i=0;i<Rows;i++){
                var e=grid[i];var tile=rows[i];tile.name="Entrant "+e.Id;
                tile.transform.Find("Car").GetComponent<ToyCarIcon>().color=e.Paint;
                tile.transform.Find("Name").GetComponent<Text>().text=e.Id==0?"YOU":e.Driver?.Name??"Driver "+e.Id;
                var pill=ratingPills[i];pill.GetComponentInChildren<Text>().text=e.Rating.ToString();pill.SetFill(e.Rating<own?Toy.Slower:e.Rating>own?Toy.Faster:Toy.White);
                tile.GetComponent<ToyTile>().Player=e.Id==0;
            }
        }
        void SelectRow(int slot){if(IsOpen)SelectEntry(grid[slot].Id);}
        public void SelectEntry(int id)
        {
            if(stage!=null&&selected==id)return;
            var e=roster.Entries[id];selected=id;bool you=id==0;
            driverName.text=you?"YOU":(e.Driver?.Name??"Driver "+id).ToUpperInvariant();gridLabel.text="GRID "+(e.Slot+1);
            rating.text=e.Rating.ToString();
            float speed=e.Tuning.AttainableSpeed,sprint=PartsBrowser.Sprint(e.Tuning);
            int rank=1+roster.Entries.Count(o=>o.Rating>e.Rating);
            speedValue.text=$"{speed*2.237f:0.0} mph";SetBar(speedBar,speed/Mathf.Max(.01f,maxSpeed));
            accelValue.text=$"{sprint:0.00} s";SetBar(accelBar,bestSprint/Mathf.Max(.01f,sprint));
            rankValue.text=Toy.Ordinal(rank)+" of "+Rows;SetBar(rankBar,1-(rank-1)/(float)Rows);
            bodyLabel.text=CarBodies.Name(e.Body)+"  ·  "+e.EquipmentLabel;
            for(int i=0;i<7;i++)partNames[i].text=EquipmentCatalog.Get(e.Parts[EquipmentCatalog.All.slots[i]]).name;
            bubble.text=you?PlayerLine():RivalLine(e);
            stage?.Dispose();stage=new PartPreviewStage(e.Body,e.Paint,EquipmentCatalog.All.slots.Select(slot=>EquipmentCatalog.Get(e.Parts[slot])),Color.clear,showroom:false);car.texture=stage.Car;
            foreach(var row in rows)row.GetComponent<ToyTile>().Chosen=row.name=="Entrant "+id;
        }
        static void SetBar(RectTransform bar,float fraction)=>bar.sizeDelta=new Vector2(352*Mathf.Clamp(fraction,.06f,1),18);
        string PlayerLine()
        {
            int own=roster.Entries[0].Rating,faster=roster.Entries.Count(e=>e.Rating>own),slower=roster.Entries.Count(e=>e.Rating<own);
            return faster==0?"Nobody out-rates you today. Don't throw it away!":slower==0?$"All {faster} rivals out-rate you. Drive it like you stole it!":$"{faster} cars out-rate you, {slower} don't. Pick your battles!";
        }
        static string RivalLine(RaceEntrant e)
        {
            if(e.Driver==null)return "An original driver with no personality profile.";
            string mood=e.Driver.Style switch{"Bold"=>"Loves a late lunge.","Patient"=>"Waits for your mistakes.","Opportunist"=>"Spots every gap.","Precise"=>"Neat, quick and tidy.","Measured"=>"Calm and consistent.",_=>"Keeps things balanced."};
            return $"{e.Driver.Style}: {mood}\nRisk {e.Driver.Planning.Risk}/10 · {BrakeName(e.Driver.BaseBraking)} braking · {(e.Driver.Consistency>=.8f?"steady":e.Driver.Consistency>=.5f?"varied":"unpredictable")}";
        }
        static string BrakeName(float value)=>value>=.74f?"late":value>=.715f?"balanced":"early";
        public void TurnPage(int direction)
        {
            SetTour(false);int slot=(int)Mathf.Repeat(roster.Entries[selected].Slot/8*8+direction*8,Rows);
            EventSystem.current.SetSelectedGameObject(rows[slot].gameObject);SelectRow(slot);
        }
        public void SetTour(bool enabled){touring=enabled;tourStart=Time.unscaledTime;if(tourButton!=null)tourButton.GetComponentInChildren<Text>().text=enabled?"STOP TOUR":"TOUR GRID";}
        public void OpenInspector()
        {
            SetTour(false);if(inspector==null)inspector=gameObject.AddComponent<RacePaddockInspector>();
            inspector.Open(canvas.transform,roster.Entries[selected],roster.Entries[0],stage,()=>EventSystem.current.SetSelectedGameObject(inspect.gameObject));
        }
        void Update()
        {
            if(!IsOpen)return;var k=Keyboard.current;var p=Gamepad.current;
            if(inspector!=null&&(inspector.IsOpen||inspector.LastCloseFrame==Time.frameCount))return;
            if(k?.iKey.wasPressedThisFrame==true||p?.buttonWest.wasPressedThisFrame==true){OpenInspector();return;}
            if(Time.unscaledTime>=nextSpin){nextSpin=Time.unscaledTime+.067f;stage?.RotateCar(.8f);}
            // Tour: step through the grid every 0.75 s, a "page" (eight places) every 6 s.
            if(touring){
                float phase=Time.unscaledTime-tourStart;int slot=(int)(roster.Entries[selected].Slot/8*8+Mathf.Min(7,phase/.75f));
                if(phase>=6){slot=(int)Mathf.Repeat(roster.Entries[selected].Slot/8*8+8,Rows);tourStart=Time.unscaledTime;}
                if(grid[slot].Id!=selected)SelectRow(slot);
                if(k?.upArrowKey.wasPressedThisFrame==true||k?.downArrowKey.wasPressedThisFrame==true||p?.dpad.ReadValue().sqrMagnitude>0)SetTour(false);
            }
            if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true){Leave(false);return;}
            if(k?.pageUpKey.wasPressedThisFrame==true||p?.leftShoulder.wasPressedThisFrame==true)TurnPage(-1);
            if(k?.pageDownKey.wasPressedThisFrame==true||p?.rightShoulder.wasPressedThisFrame==true)TurnPage(1);
        }
        public void Hide(){LastCloseFrame=Time.frameCount;inspector?.Hide();if(canvas!=null)canvas.gameObject.SetActive(false);stage?.Dispose();stage=null;StopAllCoroutines();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
        void Leave(bool begin){Hide();(begin?confirm:cancel)?.Invoke();}
        public void Confirm()=>Leave(true);
        public void Cancel()=>Leave(false);
        public void Capture(string path){Toy.UseMenuCamera(canvas);CoastMenu.RefreshButtons(canvas.transform);Toy.RefreshButtons(canvas.transform);Toy.Capture(path);}
        void OnDestroy()=>stage?.Dispose();
    }
    // Grid tile focus: lifts and brightens when selected; the player's tile is yellow.
    public sealed class ToyTile:MonoBehaviour
    {
        Button button;ToyShape shape;public bool Player,Chosen;
        public void Setup(Button b,ToyShape s){button=b;shape=s;}
        void LateUpdate()
        {
            bool focus=EventSystem.current?.currentSelectedGameObject==gameObject;
            shape.SetFill(Player?Toy.Sun:Chosen||focus?Toy.Hex(0xdff3ff):Toy.White);
            shape.SetBorder(focus?Toy.Orange:Toy.Navy);
            float target=focus?1.05f:1;transform.localScale=Vector3.one*Mathf.MoveTowards(transform.localScale.x,target,Time.unscaledDeltaTime*1.5f);
        }
    }
}
