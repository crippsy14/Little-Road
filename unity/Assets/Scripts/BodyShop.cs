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
    // Bodies change looks only. Owned bodies are switched at any Pit Stop garage bay.
    public sealed class BodyShop:MonoBehaviour
    {
        Canvas canvas;AdventureStore store;PartPreviewStage stage;Action bought;Action back;string town;
        RawImage carImage;Text wallet,modelName,price,status,hint;readonly List<Button> cards=new();readonly List<ToyShape> frames=new();readonly List<Text> tags=new();
        readonly List<CarBodies.Offer> offers=new();int focus=-1;float turn;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public void Open(AdventureStore inventory,string shop,Action onBought,Action onBack)
        {
            store=inventory;bought=onBought;back=onBack;
            if(canvas==null||town!=shop){town=shop;if(canvas!=null)Destroy(canvas.gameObject);Build();}
            focus=-1;Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);status.text="";
            int start=Mathf.Max(0,offers.FindIndex(o=>o.id==store.Data.bodyId));Focus(start);RefreshTags();
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
            var bg=Toy.Rect(root.transform,"Body shop backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,40),PitStop.Body,Toy.Hex(0xff9fd0),30,2);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            Toy.Title(bg,"Body title","PIT STOP BODY SHOP",78,new Vector2(1000,100),new Vector2(-420,430),Toy.White,TextAnchor.MiddleLeft,-2);
            var purse=Toy.Card(bg,"Wallet",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            wallet=Toy.Label(purse.transform,"Wallet text","",30,new Vector2(300,70),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            var board=Toy.Card(bg,"Model board",new Vector2(820,780),new Vector2(-450,-25),Toy.Cream,40,7,10);
            Toy.Title(board.transform,"Board heading","PICK A NEW RIDE",40,new Vector2(700,60),new Vector2(0,338),PitStop.Body,TextAnchor.MiddleCenter);
            const int columns=3;
            for(int i=0;i<offers.Count;i++) {
                int index=i;var o=offers[i];var pos=new Vector2(-250+(i%columns)*250,265-(i/columns)*88);
                var frame=Toy.Card(board.transform,"Model card "+o.id,new Vector2(236,78),pos,Toy.White,24,5,4);frame.raycastTarget=true;frames.Add(frame);
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
                int r=i/columns,c=i%columns;
                cards[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,
                    selectOnLeft=c>0?cards[i-1]:null,selectOnRight=c<columns-1&&i+1<cards.Count?cards[i+1]:null,
                    selectOnUp=r>0?cards[i-columns]:null,selectOnDown=i+columns<cards.Count?cards[i+columns]:null};
            }
            var showcase=Toy.Card(bg,"Showroom",new Vector2(900,560),new Vector2(470,60),Toy.White,40,7,10);
            Toy.Sunburst(showcase.transform,new Vector2(820,480),Vector2.zero,Toy.Sun,Toy.Hex(0xffe27a),20,-6).circular=true;
            carImage=Toy.Rect(showcase.transform,"Car preview",new Vector2(860,478),Vector2.zero).gameObject.AddComponent<RawImage>();carImage.raycastTarget=false;
            var tag=Toy.Card(bg,"Model tag",new Vector2(760,110),new Vector2(470,-300),Toy.Navy,55,0,8);
            modelName=Toy.Title(tag.transform,"Model title","",42,new Vector2(470,80),new Vector2(-120,0),Toy.White,TextAnchor.MiddleLeft);
            price=Toy.Label(tag.transform,"Model price tag","",28,new Vector2(260,80),new Vector2(230,0),Toy.Sun,TextAnchor.MiddleRight,true);
            status=Toy.Label(bg,"Body status","",26,new Vector2(900,40),new Vector2(470,-380),Toy.Navy,TextAnchor.MiddleCenter,true);
            Toy.Candy(bg,"BACK TO THE ROAD","BACK","O",new Vector2(260,90),new Vector2(-740,-465),Toy.Red,Leave);
            hint=Toy.Label(bg,"Body hint",string.Empty,22,new Vector2(1100,40),new Vector2(220,-465),Toy.Navy,TextAnchor.MiddleCenter,true);
            canvas.gameObject.SetActive(false);
        }
        void RefreshTags()
        {
            wallet.text=store.Data.credits+" CREDITS";
            for(int i=0;i<offers.Count;i++){var o=offers[i];tags[i].text=store.Data.bodyId==o.id?"DRIVING":store.Data.bodies.Contains(o.id)?"OWNED":o.price+" cr";}
        }
        void Focus(int index)
        {
            if(index<0||index>=offers.Count||index==focus)return;focus=index;var o=offers[index];
            stage?.Dispose();stage=new PartPreviewStage(o.id,PaintCatalog.All[store.Data.paintIndex].color,Array.Empty<EquipmentCatalog.Part>(),Color.clear,lightScale:.5f);
            carImage.texture=stage.Car;turn=0;
            modelName.text=CarBodies.Name(o.id).ToUpperInvariant();
            price.text=store.Data.bodyId==o.id?"YOU'RE DRIVING IT":store.Data.bodies.Contains(o.id)?"OWNED":o.price+" credits";
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
            // Slow turntable so each model shows every side.
            turn+=Time.unscaledDeltaTime*24;if(turn>=2){stage?.RotateCar(turn);turn=0;}
        }
        public void Leave(){if(!IsOpen)return;canvas.gameObject.SetActive(false);stage?.Dispose();stage=null;focus=-1;LastCloseFrame=Time.frameCount;back?.Invoke();}
        void OnDestroy(){stage?.Dispose();}
    }
}
