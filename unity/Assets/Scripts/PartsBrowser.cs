using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Pit Stop parts screens (docs/ui-design.md, "Parts"). Two modes on one layout:
    //  * Shop (Sid, Rosa): the town's catalogue; the only thing you can do is buy.
    //  * Fit (Mara, Gio): only parts you own; the only thing you can do is fit.
    // Categories are icon tabs across the top (LB/RB, Q/E); the current one opens out to show its name.
    // Each part shows one status badge in one place and one action, on the confirm button.
    // Selection is local presentation state; only the explicit action mutates the save.
    public sealed class PartsBrowser : MonoBehaviour
    {
        public enum Mode{Shop,Fit}
        Canvas canvas;AdventureStore store;PartPreviewStage stage,icons;ToyShape selectedShape,badge;
        Action fitted,back;Mode mode;
        EquipmentCatalog.Part[] parts;int index;string slot="engine",notice;
        readonly Dictionary<string,string> remembered=new();
        // "hillcrest" at Hillcrest: the valley parts join the catalogue.
        public string Shop="bayview";
        Text wallet,heading,modePill,title,badgeText,hint,comparisonHeading,explanation,status,leftLabel,rightLabel,keysHint,actionText,actionKey,tabLeft,tabRight;
        readonly Text[] captions=new Text[3],before=new Text[3],after=new Text[3];readonly GameObject[] statRows=new GameObject[3];
        RawImage selectedImage,leftImage,rightImage;GameObject leftCard,rightCard,leftTick,rightTick;
        Button selectedCard,action,previous,next,exit;ToyShape actionShape,actionKeyShape;GameObject actionCross;
        readonly List<(ToyShape shape,RawImage icon,Text label,Button button)> tabs=new();readonly List<float> tabWidths=new();
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public string SelectedId=>parts[index].id;
        public string Slot=>slot;
        public Mode CurrentMode=>mode;
        public int PartCount=>parts.Length;
        public float PreviewAngle=>stage.Angle;
        public int PreviewRenderCount=>stage.RenderCount;
        static string[] Slots=>EquipmentCatalog.All.slots;
        static string SlotName(string s)=>s.ToUpperInvariant();
        string FitterName=>Shop=="hillcrest"?"Gio":"Mara";
        const float TabSmall=118,TabOpen=330,TabGap=12;

        public void Open(AdventureStore inventory,Mode openMode,string startSlot,Action onFitted,Action onBack)
        {
            store=inventory;mode=openMode;fitted=onFitted;back=onBack;notice="";
            if(canvas==null)Build();
            bool valley=Shop=="hillcrest";
            heading.text=mode==Mode.Shop?(valley?"HILLCREST PARTS":"BAYVIEW PARTS"):(valley?"HILLCREST MOTORS":"HARBOUR GARAGE");
            modePill.text=mode==Mode.Shop?"PARTS SHOP  ·  BUY HERE":"FITTING BAY  ·  YOUR PARTS";
            modePill.transform.parent.GetComponent<ToyShape>().SetFill(mode==Mode.Shop?Toy.Gold:Toy.Mint);
            // Category icons: each slot's stock part, rendered once.
            icons?.Dispose();icons=new PartPreviewStage(store.Data.bodyId,AdventureGame.Paints[store.Data.paintIndex],Slots.Select(s=>EquipmentCatalog.Get(s+"_stock")),Color.clear);
            for(int i=0;i<tabs.Count;i++)tabs[i].icon.texture=icons.Card(Slots[i]+"_stock");
            Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);
            Load(startSlot,true);StartCoroutine(FocusNextFrame());
        }
        // Switch category: this mode's parts for the slot, a fresh preview stage, the tab opened out.
        void Load(string newSlot,bool snapTabs=false)
        {
            slot=newSlot;notice="";
            var list=EquipmentCatalog.All.parts.Where(p=>p.slot==slot&&(Shop=="hillcrest"||p.shop!="hillcrest"));
            // Only the open class and below (Richard, 30 Sep: keep the next tier hidden until it's unlocked). The
            // fitting bay shows owned parts the same way, plus whatever is on the car now.
            int open=RaceCalendar.Rank(RaceCalendar.Open(store.Data));string onCar=store.Data.Loadout()[slot];
            list=mode==Mode.Fit?list.Where(p=>store.Data.owned.Contains(p.id)&&(RaceCalendar.Rank(p.tier)<=open||p.id==onCar)):list.Where(p=>p.price>0&&RaceCalendar.Rank(p.tier)<=open);
            parts=list.OrderBy(p=>RaceCalendar.Rank(p.tier)).ThenBy(p=>p.price).ToArray();
            string fittedId=store.Data.Loadout()[slot];
            string want=mode==Mode.Fit?fittedId:remembered.TryGetValue(slot,out var r)?r:parts.FirstOrDefault(p=>!store.Data.owned.Contains(p.id))?.id;
            index=Mathf.Max(0,Array.FindIndex(parts,p=>p.id==want));
            stage?.Dispose();stage=new PartPreviewStage(store.Data.bodyId,AdventureGame.Paints[store.Data.paintIndex],parts,Color.clear);
            selectedImage.texture=stage.Selected;
            for(int i=0;i<tabs.Count;i++){bool on=Slots[i]==slot;tabWidths[i]=on?TabOpen:TabSmall;if(snapTabs)tabs[i].shape.rectTransform.sizeDelta=new Vector2(tabWidths[i],96);}
            Refresh(true);
        }
        IEnumerator FocusNextFrame(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);}
        void Build()
        {
            var root=new GameObject("Visual parts browser",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var background=Toy.Rect(root.transform,"Workshop",new Vector2(1920,1080),Vector2.zero);background.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(background,new Vector2(2600,2600),new Vector2(0,40),Toy.Orange,Toy.OrangeLight,30,2);
            Toy.Checker(background,1920,525);Toy.Checker(background,1920,-525);
            // Header: where you are and what this screen is for, credits on the right.
            heading=Toy.Title(background,"Garage title","HARBOUR GARAGE",70,new Vector2(900,84),new Vector2(-490,440),Toy.White,TextAnchor.MiddleLeft,-2);
            var pill=Toy.Card(background,"Mode pill",new Vector2(420,44),new Vector2(-730,372),Toy.Mint,23,4,4);
            modePill=Toy.Label(pill.transform,"Mode",string.Empty,22,new Vector2(400,46),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            var purse=Toy.Card(background,"Wallet",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            var coin=Toy.Card(purse.transform,"Coin",new Vector2(54,54),new Vector2(-138,0),Toy.Gold,27,4,0);Toy.Title(coin.transform,"C","C",34,new Vector2(54,54),Vector2.zero,Toy.White,TextAnchor.MiddleCenter);
            coin.gameObject.AddComponent<ToyCoinFlip>();
            wallet=Toy.Label(purse.transform,"Wallet text",string.Empty,30,new Vector2(280,70),new Vector2(30,0),Toy.Navy,TextAnchor.MiddleCenter,true);
            // Category tabs: icon chips across the top; the current one opens out with its name.
            var strip=Toy.Rect(background,"Category tabs",new Vector2(1500,110),new Vector2(0,300));
            for(int i=0;i<Slots.Length;i++) {
                string s=Slots[i];
                var shape=Toy.Card(strip,"Tab "+SlotName(s),new Vector2(TabSmall,96),Vector2.zero,Toy.Navy,30,5,6);shape.raycastTarget=true;
                var icon=Toy.Rect(shape.transform,"Icon",new Vector2(104,88),Vector2.zero).gameObject.AddComponent<RawImage>();icon.raycastTarget=false;
                var label=Toy.Title(shape.transform,"Name",SlotName(s),34,new Vector2(210,60),Vector2.zero,Toy.White,TextAnchor.MiddleLeft);
                var b=shape.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>SwitchCategory(s));
                tabs.Add((shape,icon,label,b));tabWidths.Add(TabSmall);
            }
            tabLeft=Toy.Label(strip,"Tab key left",string.Empty,24,new Vector2(90,40),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            tabRight=Toy.Label(strip,"Tab key right",string.Empty,24,new Vector2(90,40),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);

            // Carousel: neighbours either side, the selected part on its stand in the middle.
            var left=Toy.Card(background,"Previous part",new Vector2(280,280),new Vector2(-525,40),Toy.White,30,6,8);leftCard=left.gameObject;
            leftImage=Image(left.transform,"Model",new Vector2(276,210),new Vector2(0,24));
            leftLabel=Toy.Label(left.transform,"Name",string.Empty,21,new Vector2(250,50),new Vector2(0,-108),Toy.Navy,TextAnchor.MiddleCenter,true);
            leftTick=Tick(left.transform,new Vector2(110,110),34);
            var right=Toy.Card(background,"Next part",new Vector2(280,280),new Vector2(525,40),Toy.White,30,6,8);rightCard=right.gameObject;
            rightImage=Image(right.transform,"Model",new Vector2(276,210),new Vector2(0,24));
            rightLabel=Toy.Label(right.transform,"Name",string.Empty,21,new Vector2(250,50),new Vector2(0,-108),Toy.Navy,TextAnchor.MiddleCenter,true);
            rightTick=Tick(right.transform,new Vector2(110,110),34);
            selectedShape=Toy.Card(background,"Selected part",new Vector2(600,390),new Vector2(0,40),Toy.Cream,40,8,12);selectedShape.raycastTarget=true;
            var burst=Toy.Sunburst(selectedShape.transform,new Vector2(380,380),new Vector2(0,30),Toy.Sun,Toy.Hex(0xffe58a),16,5);burst.circular=true;
            selectedCard=selectedShape.gameObject.AddComponent<Button>();selectedCard.targetGraphic=selectedShape;selectedCard.transition=Selectable.Transition.None;
            selectedCard.navigation=new Navigation{mode=Navigation.Mode.None};
            selectedShape.gameObject.AddComponent<PartCardInput>().browser=this;
            selectedImage=Image(selectedShape.transform,"Rotatable part",new Vector2(580,300),new Vector2(0,30));
            title=Toy.Title(selectedShape.transform,"Part name",string.Empty,44,new Vector2(560,56),new Vector2(0,-150),Toy.Sun,TextAnchor.MiddleCenter);
            // The one status badge, always in the card's top-right corner.
            badge=Toy.Card(selectedShape.transform,"Status badge",new Vector2(210,52),new Vector2(190,160),Toy.Mint,26,5,5);
            badgeText=Toy.Label(badge.transform,"Status",string.Empty,24,new Vector2(200,52),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            previous=Toy.Candy(background,"PREVIOUS","<",null,new Vector2(80,80),new Vector2(-350,40),Toy.Sky,()=>Step(-1));
            next=Toy.Candy(background,"NEXT",">",null,new Vector2(80,80),new Vector2(350,40),Toy.Sky,()=>Step(1));
            foreach(var b in new[]{previous,next})b.navigation=new Navigation{mode=Navigation.Mode.None};
            // The one action, with its button shown on it.
            actionShape=Toy.Card(background,"Action",new Vector2(460,78),new Vector2(0,-190),Toy.Gold,39,6,0);actionShape.raycastTarget=true;
            action=actionShape.gameObject.AddComponent<Button>();action.transition=Selectable.Transition.None;action.navigation=new Navigation{mode=Navigation.Mode.None};action.onClick.AddListener(Primary);
            actionKeyShape=Toy.Card(actionShape.transform,"Key",new Vector2(60,50),new Vector2(-180,0),Toy.White,14,0,3);
            actionKey=Toy.Label(actionKeyShape.transform,"Key text",string.Empty,24,new Vector2(90,50),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            actionCross=new GameObject("Cross",typeof(RectTransform));actionCross.transform.SetParent(actionKeyShape.transform,false);
            var mark=Toy.Rect(actionCross.transform,"Cross mark",new Vector2(30,30),Vector2.zero).gameObject.AddComponent<ToyPadGlyph>();mark.symbol=ToyPadGlyph.Symbol.Cross;mark.raycastTarget=false;
            actionText=Toy.Title(actionShape.transform,"Action text",string.Empty,34,new Vector2(360,70),new Vector2(30,2),Toy.White,TextAnchor.MiddleCenter);
            keysHint=Toy.Label(background,"Controls",string.Empty,20,new Vector2(1500,30),new Vector2(0,-248),Toy.Navy,TextAnchor.MiddleCenter,true);

            var detail=Toy.Card(background,"Complete setup comparison",new Vector2(1560,180),new Vector2(0,-362),Toy.Cream,34,7,10);
            hint=Toy.Label(detail.transform,"Hint",string.Empty,25,new Vector2(640,80),new Vector2(-420,40),Toy.Navy,TextAnchor.UpperLeft,true);
            explanation=Toy.Label(detail.transform,"Explanation",string.Empty,18,new Vector2(640,70),new Vector2(-420,-44),Toy.Soft,TextAnchor.UpperLeft,false);
            comparisonHeading=Toy.Label(detail.transform,"Comparison heading",string.Empty,20,new Vector2(820,30),new Vector2(340,70),Toy.Orange,TextAnchor.MiddleLeft,true);
            for(int i=0;i<3;i++){
                float y=26-i*44;var row=Toy.Rect(detail.transform,"Stat row "+i,new Vector2(820,42),new Vector2(0,y));statRows[i]=row.gameObject;
                captions[i]=Toy.Label(row,"Stat "+i,string.Empty,20,new Vector2(220,40),new Vector2(30,0),Toy.Navy,TextAnchor.MiddleLeft,true);
                before[i]=Toy.Pill(row,"Before "+i,string.Empty,20,new Vector2(170,38),new Vector2(300,0),Toy.White,Toy.Navy).GetComponentInChildren<Text>();
                Toy.Title(row,"Arrow "+i,">",30,new Vector2(40,40),new Vector2(410,0),Toy.Orange,TextAnchor.MiddleCenter);
                after[i]=Toy.Pill(row,"After "+i,string.Empty,20,new Vector2(170,38),new Vector2(520,0),Toy.Mint,Toy.Navy).GetComponentInChildren<Text>();
            }
            status=Toy.Label(background,"Status",string.Empty,22,new Vector2(1100,40),new Vector2(-290,-500),Toy.Navy,TextAnchor.MiddleLeft,true);
            exit=Toy.Candy(background,"BACK","BACK","O",new Vector2(220,62),new Vector2(790,-500),Toy.Red,()=>Leave(back));exit.navigation=new Navigation{mode=Navigation.Mode.None};
        }
        RawImage Image(Transform parent,string name,Vector2 size,Vector2 pos)
        {var image=Toy.Rect(parent,name,size,pos).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        static GameObject Tick(Transform parent,Vector2 at,float size)
        {
            var dot=Toy.Card(parent,"Fitted tick",new Vector2(size,size),at,Toy.Mint,size*.5f,4,3);
            Toy.Label(dot.transform,"Tick","✓",Mathf.RoundToInt(size*.7f),new Vector2(size,size),new Vector2(0,1),Toy.Navy,TextAnchor.MiddleCenter,true);
            return dot.gameObject;
        }
        public void Step(int amount){if(parts.Length<2)return;index=(index+amount+parts.Length)%parts.Length;remembered[slot]=SelectedId;notice="";Refresh(true);}
        public void SelectPart(string id)
        {
            var part=EquipmentCatalog.Get(id);if(part.slot!=slot)Load(part.slot);
            int found=Array.FindIndex(parts,p=>p.id==id);if(found<0)throw new ArgumentException(id+" is not on this screen ("+mode+")");index=found;remembered[slot]=id;notice="";Refresh(true);
        }
        // What each category changes, before (fitted) and after (previewed), with your other parts fitted.
        static (string caption,Func<CarTuning,string> value)[] StatsFor(string s)
        {
            string Index(float value,float stock)=>$"{value/stock*100:0}";
            string Mph(float metres)=>$"{metres*2.237f:0.0} mph";
            string Stop(CarTuning t,float mph)=>$"{Mathf.Pow(mph/2.237f,2)/(2*Mathf.Max(t.braking,.1f)):0.0} m";
            var power=("POWER INDEX",(Func<CarTuning,string>)(t=>Index(t.power,CarTuning.Stock.power)));
            var sprint=("0-30 MPH",(Func<CarTuning,string>)(t=>$"{Sprint(t):0.00} s"));
            var top=("TOP SPEED",(Func<CarTuning,string>)(t=>Mph(t.AttainableSpeed)));
            return s switch{
                "engine"=>new[]{power,sprint,top},
                "turbo"=>new[]{power,sprint,top},
                "transmission"=>new[]{top,sprint,("GEARING LIMIT",(Func<CarTuning,string>)(t=>Mph(t.gearCeiling)))},
                "tyres"=>new[]{("ROAD GRIP",(Func<CarTuning,string>)(t=>Index(t.roadGrip,CarTuning.Stock.roadGrip))),("GRAVEL GRIP",t=>Index(t.dirtGrip,CarTuning.Stock.dirtGrip)),sprint},
                "brakes"=>new[]{("STOP FROM 30",(Func<CarTuning,string>)(t=>Stop(t,30))),("STOP FROM 60",t=>Stop(t,60)),("BRAKE INDEX",t=>Index(t.braking,CarTuning.Stock.braking))},
                "steering"=>new[]{("RESPONSE",(Func<CarTuning,string>)(t=>Index(t.response,CarTuning.Stock.response))),("TURN RATE",t=>Index(t.yaw,CarTuning.Stock.yaw))},
                "chassis"=>new[]{("WEIGHT",(Func<CarTuning,string>)(t=>$"{850*t.mass:0} kg")),sprint,("STOP FROM 30",t=>Stop(t,30))},
                _=>new[]{power,sprint,top}};
        }
        static string Explain(string s,CarTuning target)=>s switch{
            "engine" or "turbo"=>"Power index: stock setup = 100. "+(target.AttainableSpeed>=target.gearCeiling-.05f?"Your transmission limits the top speed of this setup.":"Power, gearing and tyre grip decide usable performance."),
            "transmission"=>"Short ratios pull harder out of corners; long ratios raise the top speed if the engine can reach it.",
            "tyres"=>"Road grip holds tarmac corners and braking; gravel grip counts off the road and on dirt. Stock = 100.",
            "brakes"=>"Stopping distances on level, dry tarmac. Brake index: stock = 100.",
            "steering"=>"Response is how quickly the wheels follow your input; turn rate is how sharply the car rotates. Stock = 100.",
            "chassis"=>"A lighter car accelerates, brakes and changes direction better with the same parts.",
            _=>""};
        // The single state of the selected part in this mode, and what (if anything) the action does.
        enum State{Fitted,CanFit,CanBuy,CannotAfford,Owned,Locked}
        State StateOf(EquipmentCatalog.Part part)
        {
            bool owned=store.Data.owned.Contains(part.id),isFitted=store.Data.Loadout()[part.slot]==part.id;
            if(mode==Mode.Fit)return isFitted?State.Fitted:State.CanFit;
            if(owned)return State.Owned;
            if(!RaceCalendar.PartOpen(store.Data,part))return State.Locked;
            return store.Data.credits>=part.price?State.CanBuy:State.CannotAfford;
        }
        void Refresh(bool modelChanged)
        {
            var part=parts[index];var state=StateOf(part);
            wallet.text=$"{store.Data.credits:N0} CREDITS";title.text=part.name;hint.text=part.hint;
            // Badge: what this part is to you right now.
            (string text,Color fill)=state switch{
                State.Fitted=>("✓ FITTED",Toy.Mint),State.CanFit=>("OWNED",Toy.Sky),State.Owned=>("✓ OWNED",Toy.Mint),
                State.Locked=>($"CLASS {part.tier}",Toy.Silver),_=>($"{part.price:N0} CR",Toy.Sun)};
            badgeText.text=text;badge.SetFill(fill);
            // Action: the one thing you can do here.
            bool live=state is State.CanFit or State.CanBuy;
            actionText.text=state switch{State.CanFit=>"FIT "+SlotName(slot),State.CanBuy=>$"BUY  {part.price:N0} CR",State.Fitted=>"FITTED",
                State.Owned=>"ALREADY YOURS",State.Locked=>"LOCKED",_=>$"NEED {part.price-store.Data.credits:N0} MORE"};
            actionShape.SetFill(state==State.CanFit?Toy.Mint:state==State.CanBuy?Toy.Gold:new Color(.78f,.8f,.86f));
            actionText.color=Toy.White;actionKeyShape.gameObject.SetActive(live);action.interactable=live;
            var family=InputGlyphs.Current;actionCross.SetActive(family==InputGlyphs.Family.PlayStation);actionKey.text=family==InputGlyphs.Family.PlayStation?"":InputGlyphs.Name("select");
            actionKeyShape.rectTransform.sizeDelta=new Vector2(family==InputGlyphs.Family.Keyboard?92:52,50);
            var actual=EquipmentCatalog.Stats(store.Data.Loadout());var preview=store.Data.Loadout();preview[part.slot]=part.id;var target=EquipmentCatalog.Stats(preview);
            comparisonHeading.text=$"{EquipmentCatalog.Get(store.Data.Loadout()[part.slot]).name.ToUpperInvariant()} (FITTED)  >  {part.name.ToUpperInvariant()}";
            var stats=StatsFor(slot);
            for(int i=0;i<3;i++){bool used=i<stats.Length;statRows[i].SetActive(used);if(!used)continue;captions[i].text=stats[i].caption;before[i].text=stats[i].value(actual);after[i].text=stats[i].value(target);}
            explanation.text="Estimates use your other fitted parts. "+Explain(slot,target);
            status.text=!string.IsNullOrEmpty(notice)?notice:state switch{
                State.Locked=>$"Class {part.tier} part. {RaceCalendar.Progress(store.Data)}",
                State.CannotAfford=>"Every race finish pays. Come back when you've earned a little more.",
                State.Owned=>$"You own this. {FitterName} fits it for free at the garage bay.",
                State.CanBuy=>$"Buy it here, then {FitterName} fits it for free at the garage bay.",
                State.Fitted=>parts.Length>1?"On your car now. Browse to fit another part you own.":"On your car now. Buy upgrades at the parts shop.",
                _=>"Fitting is free and you can swap back any time."};
            // Neighbours: hidden when there's nothing else to browse; a tick on the one that's fitted.
            bool many=parts.Length>1,three=parts.Length>2;
            rightCard.SetActive(many);leftCard.SetActive(three);previous.gameObject.SetActive(many);next.gameObject.SetActive(many);
            if(many){var r=parts[(index+1)%parts.Length];rightImage.texture=stage.Card(r.id);rightLabel.text=r.name;rightTick.SetActive(store.Data.Loadout()[slot]==r.id);}
            if(three){var l=parts[(index+parts.Length-1)%parts.Length];leftImage.texture=stage.Card(l.id);leftLabel.text=l.name;leftTick.SetActive(store.Data.Loadout()[slot]==l.id);}
            if(modelChanged)stage.Select(part.id);
            if(EventSystem.current.currentSelectedGameObject!=selectedCard.gameObject)EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);
        }
        public static float Sprint(CarTuning tuning)
        {
            float speed=0,time=0;while(speed<30/2.237f&&time<60){DriveModel.Forces(tuning,speed,1,0,tuning.roadGrip,0,out float a,out _);speed+=a*.01f;time+=.01f;}return time;
        }
        // Confirm: buy in the shop, fit in the fitting bay; nothing else.
        public void Primary()
        {
            var part=parts[index];var state=StateOf(part);
            if(state==State.CanBuy)Transaction(()=>{notice=store.Buy(part.id)?$"Bought the {part.name}! {FitterName} fits it for free at the garage bay.":"Purchase unavailable. No credits were taken.";});
            else if(state==State.CanFit)Transaction(()=>{if(store.Fit(part.id)){fitted();notice=$"{part.name} fitted and saved. Ready for the road.";}});
        }
        void Transaction(Action act)
        {try{act();}catch(Exception e){Debug.LogError(e);notice="Could not save: "+e.Message;}Refresh(false);}
        public bool InvokeAction(string what)
        {
            if(what=="back"){Leave(back);return true;}
            var state=StateOf(parts[index]);
            if(what=="buy"&&state!=State.CanBuy||what=="fit"&&state!=State.CanFit)return false;
            if(what!="buy"&&what!="fit"&&what!="primary")return false;
            Primary();return true;
        }
        void SwitchCategory(string s){if(s!=slot)Load(s);EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);}
        void Leave(Action then){Hide();then?.Invoke();}
        public void Hide()
        {
            StopAllCoroutines();LastCloseFrame=Time.frameCount;
            if(canvas!=null)canvas.gameObject.SetActive(false);EventSystem.current?.SetSelectedGameObject(null);
            stage?.Dispose();stage=null;icons?.Dispose();icons=null;
        }
        Vector2 lastMouse;bool dragging;
        void Update()
        {
            if(!IsOpen)return;var keyboard=Keyboard.current;var pad=Gamepad.current;var mouse=Mouse.current;
            if(keyboard?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true){Leave(back);return;}
            int shift=keyboard?.qKey.wasPressedThisFrame==true||keyboard?.leftBracketKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true?-1:
                keyboard?.eKey.wasPressedThisFrame==true||keyboard?.rightBracketKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true?1:0;
            if(shift!=0){int current=Array.IndexOf(Slots,slot);SwitchCategory(Slots[(current+shift+Slots.Length)%Slots.Length]);return;}
            // Spin: right stick, Z / X, or drag the part with the mouse.
            float rotation=(keyboard?.zKey.isPressed==true?-1:0)+(keyboard?.xKey.isPressed==true?1:0)+(pad?.rightStick.x.ReadValue()??0);
            if(Mathf.Abs(rotation)>.1f)stage.Rotate(rotation*85*Time.unscaledDeltaTime);
            if(mouse!=null) {
                var p=mouse.position.ReadValue();
                if(mouse.leftButton.wasPressedThisFrame)dragging=RectTransformUtility.RectangleContainsScreenPoint(selectedShape.rectTransform,p,null);
                if(!mouse.leftButton.isPressed)dragging=false;
                if(dragging&&Mathf.Abs(p.x-lastMouse.x)>.5f)stage.Rotate((p.x-lastMouse.x)*.5f);
                lastMouse=p;
            }
        }
        void LateUpdate()
        {
            if(!IsOpen)return;Toy.UseMenuCamera(canvas);
            keysHint.text=InputGlyphs.Prompts(("browse","browse"),("category","category"),("select",mode==Mode.Shop?"buy":"fit"),("spin","spin"),("back","back"));
            var sides=InputGlyphs.Name("category").Split('/');tabLeft.text=sides[0].Trim();tabRight.text=sides[^1].Trim();
            // Accordion tabs: widths ease to target, laid out left to right, centred.
            float total=0;for(int i=0;i<tabs.Count;i++){var sz=tabs[i].shape.rectTransform.sizeDelta;sz.x=Mathf.Lerp(sz.x,tabWidths[i],1-Mathf.Exp(-Time.unscaledDeltaTime*14));tabs[i].shape.rectTransform.sizeDelta=sz;total+=sz.x;}
            total+=TabGap*(tabs.Count-1);float x=-total/2;
            for(int i=0;i<tabs.Count;i++) {
                var (shape,icon,label,_)=tabs[i];float w=shape.rectTransform.sizeDelta.x;bool on=Slots[i]==slot;
                shape.rectTransform.anchoredPosition=new Vector2(x+w/2,on?6:0);shape.SetFill(on?Toy.Sun:Toy.Navy);shape.SetVerticesDirty();
                icon.rectTransform.anchoredPosition=new Vector2(-w/2+TabSmall/2,0);
                float open=Mathf.InverseLerp(TabSmall+20,TabOpen,w);label.color=new Color(1,1,1,open);
                label.rectTransform.anchoredPosition=new Vector2(-w/2+TabSmall+label.rectTransform.sizeDelta.x/2-8,0);
                x+=w+TabGap;
            }
            tabLeft.rectTransform.anchoredPosition=new Vector2(-total/2-72,0);tabRight.rectTransform.anchoredPosition=new Vector2(total/2+72,0);
            bool focus=EventSystem.current.currentSelectedGameObject==selectedCard.gameObject;selectedShape.SetBorder(focus?Toy.Orange:Toy.Navy);
        }
        void OnDestroy(){stage?.Dispose();icons?.Dispose();}
    }
    // The selected part card: left/right browse, confirm does the one action.
    public sealed class PartCardInput:MonoBehaviour,IMoveHandler,ISubmitHandler
    {
        public PartsBrowser browser;
        public void OnMove(AxisEventData data)
        {if(data.moveDir==MoveDirection.Left)browser.Step(-1);else if(data.moveDir==MoveDirection.Right)browser.Step(1);}
        public void OnSubmit(BaseEventData data)=>browser.Primary();
    }
}
