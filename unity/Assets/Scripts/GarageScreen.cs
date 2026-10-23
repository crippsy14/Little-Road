using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // The garage bay's own screen (23 Oct): every model you own, to look through and drive. The focused car turns on the
    // reveal studio's turntable in your paint, beside its card (what it is, its signature, the surface it was built for);
    // below, a strip of stills of every car you own, the one you drive marked. Cross drives the focused car (free, looks
    // only: Store.SwitchBody), Circle leaves. One preview stage serves every car (PartPreviewStage.SetCar), the stills are
    // rendered once each on opening (a few a frame), and the world is not drawn behind the screen (Toy.HideWorld).
    public sealed class GarageScreen:MonoBehaviour
    {
        Canvas canvas;AdventureStore store;PartPreviewStage stage;Func<string,bool> drive;Action back;
        RawImage carImage;Text modelName,about,signature,note,status,hint,count,ribbonText;ToyShape ribbon;RectTransform chips,strip;
        readonly List<string> ids=new();readonly List<Button> cards=new();readonly List<ToyShape> frames=new();readonly List<RawImage> stills=new();readonly List<GameObject> badges=new();
        int focus=-1;float turn,scroll,scrollTarget;
        const float Card=214,StripView=1820;const int StillW=384,StillH=216;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public int Renders=>stage?.RenderCount??0;
        public string Focused=>focus>=0&&focus<ids.Count?ids[focus]:null;
        public void Open(AdventureStore inventory,Func<string,bool> onDrive,Action onBack)
        {
            store=inventory;drive=onDrive;back=onBack;
            if(canvas!=null)Destroy(canvas.gameObject);ids.Clear();ids.AddRange(store.Data.bodies);Build();
            Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);Toy.HideWorld(true);status.text="";
            int start=Mathf.Max(0,ids.IndexOf(store.Data.bodyId));
            stage=new PartPreviewStage(ids[start],Paint,Array.Empty<EquipmentCatalog.Part>(),Color.clear,showroom:true);carImage.texture=stage.Car;
            focus=-1;scroll=scrollTarget=ScrollFor(start);strip.anchoredPosition=new Vector2(-scroll,0);Focus(start,false);Refresh();
            Toy.RefreshButtons(canvas.transform);StartCoroutine(FocusNextFrame(start));StartCoroutine(RenderStills());
        }
        Color Paint=>PaintCatalog.All[store.Data.paintIndex].color;
        IEnumerator FocusNextFrame(int index){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(cards[index].gameObject);}
        // A still a frame, nearest the focus first, so the cards you can see fill in first.
        IEnumerator RenderStills()
        {
            var order=new List<int>();for(int i=0;i<ids.Count;i++)order.Add(i);order.Sort((a,b)=>Mathf.Abs(a-focus).CompareTo(Mathf.Abs(b-focus)));
            foreach(int i in order){yield return null;if(!IsOpen||stage==null)yield break;stills[i].texture=stage.Still(ids[i],Paint,StillW,StillH,30);stills[i].color=Color.white;}
        }
        void Build()
        {
            cards.Clear();frames.Clear();stills.Clear();badges.Clear();
            var root=new GameObject("Garage",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var surround=Toy.Rect(root.transform,"Garage surround",Vector2.zero,Vector2.zero);surround.anchorMin=Vector2.zero;surround.anchorMax=Vector2.one;
            var fill=surround.gameObject.AddComponent<Image>();fill.color=PitStop.Garage;fill.raycastTarget=false;
            var bg=Toy.Rect(root.transform,"Garage backdrop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,40),PitStop.Garage,Toy.Hex(0x5fe0d2),30,2);
            Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            Toy.Title(bg,"Garage title","MY GARAGE",78,new Vector2(1000,100),new Vector2(-420,430),Toy.White,TextAnchor.MiddleLeft,-2);
            var tally=Toy.Card(bg,"Garage count",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            count=Toy.Label(tally.transform,"Garage count text","",30,new Vector2(320,70),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            // The reveal studio: the focused car on its turntable.
            var showroom=Toy.Card(bg,"Garage showroom",new Vector2(1100,620),new Vector2(-345,82),Toy.Navy,40,7,10);
            carImage=Toy.Rect(showroom.transform,"Garage car",new Vector2(1068,593),Vector2.zero).gameObject.AddComponent<RawImage>();carImage.raycastTarget=false;
            ribbon=Toy.Card(showroom.transform,"Driving ribbon",new Vector2(300,58),new Vector2(-370,262),Toy.Mint,29,5,5);
            ribbonText=Toy.Label(ribbon.transform,"Driving ribbon text","",24,new Vector2(280,58),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);
            // Its card.
            var info=Toy.Card(bg,"Model card",new Vector2(640,620),new Vector2(600,82),Toy.Cream,40,7,10);
            modelName=Toy.Title(info.transform,"Model name","",52,new Vector2(580,70),new Vector2(0,232),PitStop.Garage,TextAnchor.MiddleCenter);   // (a navy fill merged with the title's navy outline)
            about=Toy.Label(info.transform,"Model about","",25,new Vector2(560,110),new Vector2(0,128),Toy.Navy,TextAnchor.MiddleCenter,false);
            Toy.Label(info.transform,"Signature heading","SIGNATURE",19,new Vector2(560,30),new Vector2(0,44),Toy.Soft,TextAnchor.MiddleCenter,true);
            signature=Toy.Label(info.transform,"Model signature","",27,new Vector2(560,70),new Vector2(0,4),PitStop.Garage,TextAnchor.MiddleCenter,true);
            chips=Toy.Rect(info.transform,"Model chips",new Vector2(580,52),new Vector2(0,-84));
            note=Toy.Label(info.transform,"Model note","",21,new Vector2(560,70),new Vector2(0,-170),Toy.Soft,TextAnchor.MiddleCenter,true);
            status=Toy.Label(info.transform,"Garage status","",24,new Vector2(580,40),new Vector2(0,-252),PitStop.Brand,TextAnchor.MiddleCenter,true);
            // The strip of every car you own.
            var view=Toy.Rect(bg,"Garage strip",new Vector2(StripView,160),new Vector2(0,-316));view.gameObject.AddComponent<RectMask2D>();
            strip=Toy.Rect(view,"Garage strip cars",new Vector2(StripView,160),Vector2.zero);
            float centre=Mathf.Max(0,(StripView-16-ids.Count*Card)/2);   // (a short garage sits in the middle)
            for(int i=0;i<ids.Count;i++) {
                int index=i;string id=ids[i];var pos=new Vector2(-StripView/2+Card/2+8+centre+i*Card,4);
                var frame=Toy.Card(strip,"Garage card "+id,new Vector2(Card-14,140),pos,Toy.White,26,5,4);frame.raycastTarget=true;frames.Add(frame);
                var still=Toy.Rect(frame.transform,"Car still",new Vector2(Card-30,(Card-30)*StillH/(float)StillW),new Vector2(0,12)).gameObject.AddComponent<RawImage>();
                still.raycastTarget=false;still.color=new Color(1,1,1,0);stills.Add(still);
                Toy.Label(frame.transform,"Car name",CarBodies.Name(id),17,new Vector2(Card-30,26),new Vector2(0,-52),Toy.Navy,TextAnchor.MiddleCenter,true);
                var badge=Toy.Card(frame.transform,"Driving badge",new Vector2(96,28),new Vector2(-38,58),Toy.Mint,14,3,0);
                Toy.Label(badge.transform,"Driving badge text","DRIVING",15,new Vector2(96,28),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);badges.Add(badge.gameObject);
                var button=frame.gameObject.AddComponent<Button>();button.targetGraphic=frame;button.transition=Selectable.Transition.None;button.name="GARAGE "+id;
                button.onClick.AddListener(()=>Drive(index));
                var trigger=frame.gameObject.AddComponent<EventTrigger>();
                var select=new EventTrigger.Entry{eventID=EventTriggerType.Select};select.callback.AddListener(_=>Focus(index,true));trigger.triggers.Add(select);
                var hover=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};hover.callback.AddListener(_=>Focus(index,true));trigger.triggers.Add(hover);
                cards.Add(button);
            }
            for(int i=0;i<cards.Count;i++)cards[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i>0?cards[i-1]:null,selectOnRight=i+1<cards.Count?cards[i+1]:null};
            Toy.Candy(bg,"BACK TO THE GARAGE BAY","BACK","O",new Vector2(260,90),new Vector2(-740,-465),Toy.Red,Leave);
            hint=Toy.Label(bg,"Garage hint",string.Empty,22,new Vector2(1200,40),new Vector2(200,-465),Toy.Navy,TextAnchor.MiddleCenter,true);
            canvas.gameObject.SetActive(false);
        }
        float ScrollFor(int index)
        {
            float max=Mathf.Max(0,ids.Count*Card+16-StripView),left=index*Card,s=scrollTarget;
            if(left<s+Card*.5f)s=left-Card*.5f;else if(left+Card>s+StripView-Card*.5f)s=left+Card-StripView+Card*.5f;
            return Mathf.Clamp(s,0,max);
        }
        void Refresh()
        {
            count.text=ids.Count==1?"1 MODEL":ids.Count+" MODELS";
            for(int i=0;i<ids.Count;i++)badges[i].SetActive(ids[i]==store.Data.bodyId);
            for(int i=0;i<frames.Count;i++)frames[i].SetBorder(i==focus?Toy.Orange:Toy.Navy);
            if(focus<0)return;bool driving=ids[focus]==store.Data.bodyId;
            ribbon.color=driving?Toy.Mint:Toy.Sun;ribbonText.text=driving?"DRIVING NOW":"IN THE GARAGE";
        }
        void Focus(int index,bool show)
        {
            if(index<0||index>=ids.Count||index==focus)return;focus=index;string id=ids[index];
            if(show)stage.SetCar(id,Paint,stage.CarAngle);
            scrollTarget=ScrollFor(index);
            modelName.text=CarBodies.Name(id).ToUpperInvariant();
            string what=CarBodies.About(id),sign=CarBodies.Signature(id);
            about.text=what==null?(CarBodies.Class(id)=="B"?"A class B Tuner from the Tuner Garage.":id==CarBodies.Prototype?"Duke Rolo's prototype, won on the hill.":""):char.ToUpperInvariant(what[0])+what[1..]+".";
            signature.text=sign==null?"":char.ToUpperInvariant(sign[0])+sign[1..];
            var d=CarBodies.Forged(id);var tags=new List<(string text,Color fill,Color ink)>{("CLASS "+CarBodies.Class(id),Toy.Navy,Toy.White)};
            if(d?.surface!=null)tags.Add((d.surface=="both"?"RALLY: ANY SURFACE":d.surface=="gravel"?"BUILT FOR GRAVEL":"BUILT FOR TARMAC",PitStop.Garage,Toy.White));
            if(d?.quick==true)tags.Add(("QUICK-LOOKING",Toy.Orange,Toy.White));
            Chips(tags);
            note.text=$"In {PaintCatalog.All[store.Data.paintIndex].name}. Looks only: your fitted parts decide the pace.";
            status.text="";Refresh();
        }
        void Chips(List<(string text,Color fill,Color ink)> tags)
        {
            foreach(Transform child in chips)Destroy(child.gameObject);
            var widths=tags.ConvertAll(t=>t.text.Length*11f+34);float total=-10;foreach(var w in widths)total+=w+10;float x=-total/2;
            for(int i=0;i<tags.Count;i++){var chip=Toy.Card(chips,"Chip",new Vector2(widths[i],46),new Vector2(x+widths[i]/2,0),tags[i].fill,23,0,4);
                Toy.Label(chip.transform,"Chip text",tags[i].text,18,new Vector2(widths[i],46),Vector2.zero,tags[i].ink,TextAnchor.MiddleCenter,true);x+=widths[i]+10;}
        }
        void Drive(int index)
        {
            string id=ids[index];
            if(id==store.Data.bodyId){status.text="You're already driving it!";return;}
            if(drive==null||!drive(id)){status.text="Could not save that change.";return;}
            Refresh();status.text="Fresh look! You're driving the "+CarBodies.Name(id)+".";
        }
        void Update()
        {
            if(!IsOpen)return;
            hint.text=InputGlyphs.Prompts(("browse","browse"),("select","drive this one"),("back","back"))+"     Buy more at any Pit Stop body shop";
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true)Leave();
            var selected=EventSystem.current?.currentSelectedGameObject;
            if(selected!=null){int i=cards.FindIndex(b=>b.gameObject==selected);if(i>=0)Focus(i,true);}
            float wheel=Mouse.current?.scroll.ReadValue().y??0;
            if(Mathf.Abs(wheel)>.01f)scrollTarget=Mathf.Clamp(scrollTarget-Mathf.Sign(wheel)*Card,0,Mathf.Max(0,ids.Count*Card+16-StripView));
            if(Mathf.Abs(scroll-scrollTarget)>.5f){scroll=Mathf.Lerp(scroll,scrollTarget,1-Mathf.Exp(-14*Time.unscaledDeltaTime));strip.anchoredPosition=new Vector2(-scroll,0);}
            turn+=Time.unscaledDeltaTime*24;stage?.RotateCar(turn);turn=0;
        }
        public void Leave(){if(!IsOpen)return;StopAllCoroutines();canvas.gameObject.SetActive(false);Toy.HideWorld(false);stage?.Dispose();stage=null;focus=-1;LastCloseFrame=Time.frameCount;back?.Invoke();}
        void OnDestroy(){Toy.HideWorld(false);stage?.Dispose();}
    }
}
