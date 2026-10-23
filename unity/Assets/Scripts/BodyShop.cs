using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Pit Stop body shop: a board of every model this town sells. Moving focus puts that body
    // on the showroom turntable in the player's paint; Cross buys it (once); Circle leaves.
    // Bodies change looks only. Owned bodies are switched at any Pit Stop garage bay (GarageScreen).
    // (23 Oct: the Car Forge range is 43 cars at Hillcrest, so the board scrolls with the focus; one preview stage serves
    //  every car, the turntable turns every frame, and the world is not drawn behind the shop. --old-shop-preview: a new
    //  stage per car, the turntable re-rendered every 2 degrees and the town drawn behind, for A/B timing.)
    public sealed class BodyShop:MonoBehaviour
    {
        Canvas canvas;AdventureStore store;PartPreviewStage stage;Action bought;Action back;string town;
        RawImage carImage;Text wallet,modelName,about,price,status,hint,counter;readonly List<Button> cards=new();readonly List<ToyShape> frames=new();readonly List<Text> tags=new();
        readonly List<CarBodies.Offer> offers=new();int focus=-1;float turn;
        RectTransform rows;float scroll,scrollTarget;const float Row=88,View=640;const int Columns=3;
        static bool OldPreview=>RaceTrial.Has("--old-shop-preview");
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        // (the preview stage's renders so far, for the review's timing)
        public int Renders=>stage?.RenderCount??0;
        public void Open(AdventureStore inventory,string shop,Action onBought,Action onBack)
        {
            store=inventory;bought=onBought;back=onBack;
            if(canvas==null||town!=shop){town=shop;if(canvas!=null)Destroy(canvas.gameObject);Build();}
            focus=-1;Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);status.text="";
            if(!OldPreview)Toy.HideWorld(true);
            int start=Mathf.Max(0,offers.FindIndex(o=>o.id==store.Data.bodyId));scroll=scrollTarget=ScrollFor(start);rows.anchoredPosition=new Vector2(0,scroll);
            Focus(start);RefreshTags();
            Toy.RefreshButtons(canvas.transform);StartCoroutine(FocusNextFrame(start));
        }
        IEnumerator FocusNextFrame(int index){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(cards[index].gameObject);}
        void Build()
        {
            offers.Clear();cards.Clear();frames.Clear();tags.Clear();
            foreach(var o in CarBodies.ForSale)if(o.shop==town)offers.Add(o);
            var root=new GameObject("Body shop",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            // (the whole screen in the shop's colour: on a wider screen than 16:9 the world showed at the sides)
            var surround=Toy.Rect(root.transform,"Body shop surround",Vector2.zero,Vector2.zero);surround.anchorMin=Vector2.zero;surround.anchorMax=Vector2.one;
            surround.gameObject.AddComponent<Image>().color=PitStop.Body;surround.GetComponent<Image>().raycastTarget=false;
            var bg=Toy.Rect(root.transform,"Body shop backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,40),PitStop.Body,Toy.Hex(0xff9fd0),30,2);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            Toy.Title(bg,"Body title","PIT STOP BODY SHOP",78,new Vector2(1000,100),new Vector2(-420,430),Toy.White,TextAnchor.MiddleLeft,-2);
            var purse=Toy.Card(bg,"Wallet",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            wallet=Toy.Label(purse.transform,"Wallet text","",30,new Vector2(300,70),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            var board=Toy.Card(bg,"Model board",new Vector2(820,780),new Vector2(-450,-25),Toy.Cream,40,7,10);
            Toy.Title(board.transform,"Board heading","PICK A NEW RIDE",40,new Vector2(700,60),new Vector2(0,338),PitStop.Body,TextAnchor.MiddleCenter);
            var view=Toy.Rect(board.transform,"Model list",new Vector2(790,View),new Vector2(0,-28));view.gameObject.AddComponent<RectMask2D>();
            rows=Toy.Rect(view,"Model rows",new Vector2(790,View),Vector2.zero);
            counter=Toy.Label(board.transform,"Model count","",19,new Vector2(700,28),new Vector2(0,-366),Toy.Soft,TextAnchor.MiddleCenter,true);
            for(int i=0;i<offers.Count;i++) {
                int index=i;var o=offers[i];var pos=new Vector2(-250+(i%Columns)*250,View/2-Row/2+4-(i/Columns)*Row);
                var frame=Toy.Card(rows,"Model card "+o.id,new Vector2(236,78),pos,Toy.White,24,5,4);frame.raycastTarget=true;frames.Add(frame);
                Toy.Label(frame.transform,"Model name",CarBodies.Name(o.id),21,new Vector2(220,36),new Vector2(0,13),Toy.Navy,TextAnchor.MiddleCenter,true);
                tags.Add(Toy.Label(frame.transform,"Model price","",19,new Vector2(220,30),new Vector2(0,-17),PitStop.Body,TextAnchor.MiddleCenter,true));
                var button=frame.gameObject.AddComponent<Button>();button.targetGraphic=frame;button.transition=Selectable.Transition.None;button.name="BODY "+o.id;
                button.onClick.AddListener(()=>Buy(index));
                var trigger=frame.gameObject.AddComponent<EventTrigger>();
                var select=new EventTrigger.Entry{eventID=EventTriggerType.Select};select.callback.AddListener(_=>Focus(index));trigger.triggers.Add(select);
                var hover=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};hover.callback.AddListener(_=>Focus(index));trigger.triggers.Add(hover);
                cards.Add(button);
            }
            for(int i=0;i<cards.Count;i++) {
                int r=i/Columns,c=i%Columns;
                cards[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,
                    selectOnLeft=c>0?cards[i-1]:null,selectOnRight=c<Columns-1&&i+1<cards.Count?cards[i+1]:null,
                    selectOnUp=r>0?cards[i-Columns]:null,selectOnDown=i+Columns<cards.Count?cards[i+Columns]:r+1<(cards.Count+Columns-1)/Columns?cards[cards.Count-1]:null};
            }
            var showcase=Toy.Card(bg,"Showroom",new Vector2(900,560),new Vector2(470,75),Toy.White,40,7,10);
            Toy.Sunburst(showcase.transform,new Vector2(820,480),Vector2.zero,Toy.Sun,Toy.Hex(0xffe27a),20,-6).circular=true;
            carImage=Toy.Rect(showcase.transform,"Car preview",new Vector2(860,478),Vector2.zero).gameObject.AddComponent<RawImage>();carImage.raycastTarget=false;
            var tag=Toy.Card(bg,"Model tag",new Vector2(900,150),new Vector2(470,-300),Toy.Navy,55,0,8);
            modelName=Toy.Title(tag.transform,"Model title","",42,new Vector2(560,60),new Vector2(-130,26),Toy.White,TextAnchor.MiddleLeft);
            about=Toy.Label(tag.transform,"Model about","",21,new Vector2(820,56),new Vector2(0,-34),Toy.SkyLight,TextAnchor.MiddleLeft,true);
            price=Toy.Label(tag.transform,"Model price tag","",28,new Vector2(260,60),new Vector2(300,26),Toy.Sun,TextAnchor.MiddleRight,true);
            status=Toy.Label(bg,"Body status","",26,new Vector2(900,40),new Vector2(470,-405),Toy.Navy,TextAnchor.MiddleCenter,true);
            Toy.Candy(bg,"BACK TO THE ROAD","BACK","O",new Vector2(260,90),new Vector2(-740,-465),Toy.Red,Leave);
            hint=Toy.Label(bg,"Body hint",string.Empty,22,new Vector2(1100,40),new Vector2(220,-465),Toy.Navy,TextAnchor.MiddleCenter,true);
            canvas.gameObject.SetActive(false);
        }
        void RefreshTags()
        {
            wallet.text=store.Data.credits+" CREDITS";
            for(int i=0;i<offers.Count;i++){var o=offers[i];tags[i].text=store.Data.bodyId==o.id?"DRIVING":store.Data.bodies.Contains(o.id)?"OWNED":o.price+" cr";}
        }
        // The board's scroll that shows a card's row (rows above it slide out of the top).
        float ScrollFor(int index)
        {
            int rowCount=(offers.Count+Columns-1)/Columns;float max=Mathf.Max(0,rowCount*Row+8-View);
            float top=(index/Columns)*Row,s=scrollTarget;
            if(top<s+Row*.5f)s=top-Row*.5f;else if(top+Row>s+View-Row*.5f)s=top+Row-View+Row*.5f;
            return Mathf.Clamp(s,0,max);
        }
        void Focus(int index)
        {
            if(index<0||index>=offers.Count||index==focus)return;focus=index;var o=offers[index];var paint=PaintCatalog.All[store.Data.paintIndex].color;
            if(OldPreview||stage==null){stage?.Dispose();stage=new PartPreviewStage(o.id,paint,Array.Empty<EquipmentCatalog.Part>(),Color.clear,lightScale:.5f);}
            else stage.SetCar(o.id,paint);
            carImage.texture=stage.Car;turn=0;scrollTarget=ScrollFor(index);
            modelName.text=CarBodies.Name(o.id).ToUpperInvariant();
            var what=CarBodies.About(o.id);about.text=what==null?"":char.ToUpperInvariant(what[0])+what[1..]+".";
            price.text=store.Data.bodyId==o.id?"YOU'RE DRIVING IT":store.Data.bodies.Contains(o.id)?"OWNED":o.price+" credits";
            counter.text=$"{index+1} of {offers.Count} models  ·  {store.Data.bodies.FindAll(b=>offers.Exists(x=>x.id==b)).Count} yours";
            for(int i=0;i<frames.Count;i++)frames[i].SetBorder(i==index?Toy.Orange:store.Data.bodies.Contains(offers[i].id)?Toy.Mint:Toy.Navy);
        }
        void Buy(int index)
        {
            var o=offers[index];
            if(store.Data.bodies.Contains(o.id)){status.text="Already yours! Swap to it at the garage bay.";return;}
            if(store.Data.credits<o.price){status.text="Not enough credits for the "+CarBodies.Name(o.id)+". Every race pays out!";return;}
            if(!store.BuyBody(o.id)){status.text="Could not save that change.";return;}
            bought?.Invoke();RefreshTags();focus=-1;Focus(index);status.text="Sold! The "+CarBodies.Name(o.id)+" is waiting in the garage bay.";
        }
        void Update()
        {
            if(!IsOpen)return;
            hint.text=InputGlyphs.Prompts(("browse","browse"),("select","buy it!"),("back","back"))+"     Swap bodies at any Pit Stop garage";
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true)Leave();
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected!=null){int i=cards.FindIndex(b=>b.gameObject==selected);if(i>=0)Focus(i);}
            // (the mouse wheel scrolls the board without moving the focus)
            float wheel=Mouse.current?.scroll.ReadValue().y??0;
            if(Mathf.Abs(wheel)>.01f){int rowCount=(offers.Count+Columns-1)/Columns;scrollTarget=Mathf.Clamp(scrollTarget-Mathf.Sign(wheel)*Row,0,Mathf.Max(0,rowCount*Row+8-View));}
            if(Mathf.Abs(scroll-scrollTarget)>.5f){scroll=Mathf.Lerp(scroll,scrollTarget,1-Mathf.Exp(-14*Time.unscaledDeltaTime));rows.anchoredPosition=new Vector2(0,scroll);}
            // Slow turntable so each model shows every side: every frame (it stepped every 2 degrees, about 12 times a
            // second, and looked like a slide show).
            turn+=Time.unscaledDeltaTime*24;
            if(OldPreview){if(turn>=2){stage?.RotateCar(turn);turn=0;}}else{stage?.RotateCar(turn);turn=0;}
        }
        public void Leave(){if(!IsOpen)return;canvas.gameObject.SetActive(false);Toy.HideWorld(false);stage?.Dispose();stage=null;focus=-1;LastCloseFrame=Time.frameCount;back?.Invoke();}
        void OnDestroy(){Toy.HideWorld(false);stage?.Dispose();}
    }
}
