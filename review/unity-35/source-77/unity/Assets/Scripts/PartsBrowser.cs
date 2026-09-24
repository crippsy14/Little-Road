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
    // The same catalogue and transactional store power the road and the garage.
    // Selection is local presentation state; only explicit actions mutate saves.
    public sealed class PartsBrowser : MonoBehaviour
    {
        Canvas canvas;Font font;AdventureStore store;PartPreviewStage stage;Outline cardFocus;
        Action fitted,back;Action<string> category;
        EquipmentCatalog.Part[] parts;int index;string remembered="engine_stock",notice;
        Text wallet,title,state,hint,comparison,comparisonHeading,explanation,status,leftLabel,rightLabel;
        RawImage selectedImage,leftImage,rightImage,carImage;
        Button selectedCard,buy,fit,previous,next,exit;readonly List<Button> buttons=new();readonly List<Button> categories=new();
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public string SelectedId=>parts[index].id;
        public float PreviewAngle=>stage.Angle;
        public int PreviewRenderCount=>stage.RenderCount;
        static readonly Color Ink=new(.94f,.95f,.89f),Gold=new(.94f,.71f,.34f);

        public void Open(AdventureStore inventory,Action onFitted,Action onBack,Action<string> onCategory)
        {
            store=inventory;fitted=onFitted;back=onBack;category=onCategory;
            parts=EquipmentCatalog.All.parts.Where(p=>p.slot=="engine").OrderBy(p=>p.price).ToArray();
            index=Mathf.Max(0,Array.FindIndex(parts,p=>p.id==remembered));notice="";
            if(canvas==null)Build();
            stage=new PartPreviewStage(store.Data.bodyId,AdventureGame.Paints[store.Data.paintIndex],parts);
            carImage.texture=stage.Car;selectedImage.texture=stage.Selected;
            canvas.gameObject.SetActive(true);canvas.worldCamera=Camera.main;
            Refresh(true);StartCoroutine(FocusNextFrame());
        }
        IEnumerator FocusNextFrame(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);}
        void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("Visual parts browser",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=.7f;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var background=Box(root.transform,"Workshop",new Vector2(1920,1080),Vector2.zero,new Color(.035f,.085f,.105f));
            Label(background,"HARBOUR GARAGE",39,new Vector2(1000,64),new Vector2(-390,465),Gold);
            wallet=Label(background,"",28,new Vector2(550,64),new Vector2(630,465),Ink);wallet.alignment=TextAnchor.MiddleRight;
            Label(background,"PARTS CATALOGUE",19,new Vector2(260,35),new Vector2(-760,372),Gold);
            for(int i=0;i<EquipmentCatalog.All.slots.Length;i++) {
                string slot=EquipmentCatalog.All.slots[i];
                var button=ActionButton(background,slot.ToUpperInvariant(),new Vector2(268,58),new Vector2(-760,310-i*69),()=>SwitchCategory(slot));
                if(slot=="engine")button.GetComponentInChildren<Text>().text="ENGINE  /  SELECTED";
                categories.Add(button);
            }
            Label(background,"Choose a category\nL1 / R1 or [ / ]",20,new Vector2(268,70),new Vector2(-760,-184),Ink);
            Label(background,"YOUR CAR",18,new Vector2(380,28),new Vector2(700,420),Gold);
            carImage=Image(background,"Whole car",new Vector2(380,210),new Vector2(700,300));
            var left=Box(background,"Previous part",new Vector2(300,340),new Vector2(-350,66),new Color(.13f,.23f,.25f));
            leftImage=Image(left,"Model",new Vector2(298,250),new Vector2(0,28));
            leftLabel=Label(left,"",20,new Vector2(205,77),new Vector2(-36,-125),Ink);leftLabel.alignment=TextAnchor.MiddleCenter;
            var right=Box(background,"Next part",new Vector2(300,340),new Vector2(505,20),new Color(.13f,.23f,.25f));
            rightImage=Image(right,"Model",new Vector2(298,250),new Vector2(0,28));
            rightLabel=Label(right,"",21,new Vector2(275,77),new Vector2(0,-125),Ink);rightLabel.alignment=TextAnchor.MiddleCenter;
            var selected=Box(background,"Selected part",new Vector2(660,464),new Vector2(55,83),new Color(.13f,.23f,.25f));
            selectedCard=selected.gameObject.AddComponent<Button>();selectedCard.targetGraphic=selected.GetComponent<Image>();
            cardFocus=selected.gameObject.AddComponent<Outline>();cardFocus.effectColor=Gold;cardFocus.effectDistance=new Vector2(3,3);
            var colors=selectedCard.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.selectedColor=new Color(1.25f,1.25f,1.15f);selectedCard.colors=colors;
            selected.gameObject.AddComponent<PartCarouselNavigation>().browser=this;
            selectedImage=Image(selected,"Rotatable engine",new Vector2(656,370),new Vector2(0,28));
            title=Label(selected,"",31,new Vector2(610,48),new Vector2(0,-159),Gold);title.alignment=TextAnchor.MiddleCenter;
            state=Label(selected,"",21,new Vector2(610,40),new Vector2(0,-205),Ink);state.alignment=TextAnchor.MiddleCenter;
            previous=ActionButton(background,"<  PREVIOUS",new Vector2(240,50),new Vector2(-345,-199),()=>Step(-1));
            next=ActionButton(background,"NEXT  >",new Vector2(240,50),new Vector2(495,-199),()=>Step(1));
            Label(background,"Left / right: preview\nQ / E or right stick: rotate",18,new Vector2(500,46),new Vector2(65,-195),Ink).alignment=TextAnchor.MiddleCenter;
            var detail=Box(background,"Complete setup comparison",new Vector2(1790,218),new Vector2(0,-346),new Color(.07f,.15f,.17f));
            hint=Label(detail,"",24,new Vector2(650,100),new Vector2(-535,47),Ink);
            explanation=Label(detail,"",19,new Vector2(650,90),new Vector2(-535,-56),new Color(.70f,.81f,.80f));
            comparisonHeading=Label(detail,"",19,new Vector2(960,30),new Vector2(365,77),Gold);
            comparison=Label(detail,"",23,new Vector2(960,122),new Vector2(365,-8),Ink);
            buy=ActionButton(background,"BUY",new Vector2(300,58),new Vector2(80,-496),Buy);
            fit=ActionButton(background,"FIT",new Vector2(270,58),new Vector2(385,-496),Fit);
            exit=ActionButton(background,"BACK TO GARAGE",new Vector2(330,58),new Vector2(730,-496),()=>Leave(back));
            status=Label(background,"",21,new Vector2(760,62),new Vector2(-535,-496),Ink);
        }
        RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        RectTransform Box(Transform parent,string name,Vector2 size,Vector2 pos,Color color)
        {var rect=Rect(parent,name,size,pos);rect.gameObject.AddComponent<Image>().color=color;return rect;}
        Text Label(Transform parent,string value,int size,Vector2 dimensions,Vector2 position,Color color)
        {
            var text=Rect(parent,"Label",dimensions,position).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.text=value;text.color=color;
            text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        RawImage Image(Transform parent,string name,Vector2 size,Vector2 pos)
        {var image=Rect(parent,name,size,pos).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
        Button ActionButton(Transform parent,string label,Vector2 size,Vector2 pos,Action action)
        {
            var rect=Box(parent,label,size,pos,Color.white);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=new Color(.16f,.34f,.35f);colors.highlightedColor=new Color(.28f,.47f,.45f);
            colors.selectedColor=Gold;colors.pressedColor=new Color(.74f,.56f,.27f);colors.disabledColor=new Color(.09f,.17f,.19f);button.colors=colors;
            Label(rect,label,22,size-new Vector2(20,0),Vector2.zero,Ink).alignment=TextAnchor.MiddleCenter;
            button.onClick.AddListener(()=>action());buttons.Add(button);return button;
        }
        public void Step(int amount){index=(index+amount+parts.Length)%parts.Length;remembered=SelectedId;notice="";Refresh(true);}
        public void SelectPart(string id)
        {int found=Array.FindIndex(parts,p=>p.id==id);if(found<0)throw new ArgumentException(id);index=found;remembered=id;notice="";Refresh(true);}
        void Refresh(bool modelChanged)
        {
            var previousFocus=EventSystem.current.currentSelectedGameObject;
            var part=parts[index];bool owned=store.Data.owned.Contains(part.id),isFitted=store.Data.Loadout()[part.slot]==part.id;
            wallet.text=$"{store.Data.credits:N0} CREDITS";title.text=part.name;
            state.text=isFitted?"FITTED  /  OWNED":owned?"OWNED  /  FREE TO FIT":$"{part.price:N0} CREDITS  /  NOT OWNED";
            hint.text=part.hint;
            var actual=EquipmentCatalog.Stats(store.Data.Loadout());var preview=store.Data.Loadout();preview[part.slot]=part.id;var target=EquipmentCatalog.Stats(preview);
            comparisonHeading.text=$"SETUP / {EquipmentCatalog.Get(store.Data.Loadout()[part.slot]).name} (fitted) > {part.name}";
            comparison.text=$"Power index                 {actual.power/CarTuning.Stock.power*100:0}   >   {target.power/CarTuning.Stock.power*100:0}\n"+
                $"0–30 mph estimate        {Sprint(actual):0.00}s   >   {Sprint(target):0.00}s\n"+
                $"Top speed estimate       {actual.AttainableSpeed*2.237f:0.0}   >   {target.AttainableSpeed*2.237f:0.0} mph";
            explanation.text="Estimates use your other fitted parts on level, dry tarmac. Power index: stock setup = 100.\n"+
                (target.AttainableSpeed>=target.gearCeiling-.05f?"Your transmission limits the top speed of this setup.":"Power, gearing and tyre grip determine usable performance.");
            buy.interactable=!owned&&store.Data.credits>=part.price;buy.GetComponentInChildren<Text>().text=owned?"OWNED":$"BUY  /  {part.price:N0} CR";
            fit.interactable=owned&&!isFitted;fit.GetComponentInChildren<Text>().text=isFitted?"FITTED":owned?"FIT ENGINE":"BUY BEFORE FITTING";
            status.text=!string.IsNullOrEmpty(notice)?notice:!owned&&store.Data.credits<part.price?$"Need {part.price-store.Data.credits:N0} more credits. Every race finish pays.":"Preview freely. Buy to own; fit separately for free.";
            var left=parts[(index+parts.Length-1)%parts.Length];var right=parts[(index+1)%parts.Length];
            leftImage.texture=stage.Card(left.id);rightImage.texture=stage.Card(right.id);leftLabel.text=left.name;rightLabel.text=right.name;
            if(modelChanged)stage.Select(part.id);
            ConfigureNavigation();
            // Disabling a selected uGUI Button clears EventSystem selection
            // immediately, so retain its identity before updating availability.
            if(EventSystem.current.currentSelectedGameObject==null||previousFocus!=null&&previousFocus.TryGetComponent<Button>(out var focused)&&!focused.interactable)
                EventSystem.current.SetSelectedGameObject((fit.interactable?fit:selectedCard).gameObject);
        }
        public static float Sprint(CarTuning tuning)
        {
            float speed=0,time=0;while(speed<30/2.237f&&time<60){DriveModel.Forces(tuning,speed,1,0,tuning.roadGrip,0,out float a,out _);speed+=a*.01f;time+=.01f;}return time;
        }
        void ConfigureNavigation()
        {
            Button action=buy.interactable?buy:fit.interactable?fit:exit;
            selectedCard.navigation=new Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnLeft=selectedCard,selectOnRight=selectedCard,selectOnUp=categories[0],selectOnDown=action};
            for(int i=0;i<categories.Count;i++)categories[i].navigation=new Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=categories[(i+categories.Count-1)%categories.Count],selectOnDown=categories[(i+1)%categories.Count],selectOnRight=selectedCard};
            var actions=new[]{previous,next,buy,fit,exit}.Where(b=>b.interactable).ToArray();
            for(int i=0;i<actions.Length;i++)actions[i].navigation=new Navigation{mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=selectedCard,selectOnDown=selectedCard,selectOnLeft=actions[(i+actions.Length-1)%actions.Length],selectOnRight=actions[(i+1)%actions.Length]};
        }
        void Buy()=>Transaction(()=>{notice=store.Buy(SelectedId)?"Purchased. Select FIT ENGINE to install it.":"Purchase unavailable. No credits were taken.";});
        void Fit()=>Transaction(()=>{if(store.Fit(SelectedId)){fitted();notice="Engine fitted and saved. Ready for the road.";}});
        void Transaction(Action action)
        {try{action();}catch(Exception e){Debug.LogError(e);notice="Could not save: "+e.Message;}Refresh(false);}
        public bool InvokeAction(string action)
        {var button=action=="buy"?buy:action=="fit"?fit:action=="back"?exit:null;if(button==null||!button.interactable)return false;button.onClick.Invoke();return true;}
        void SwitchCategory(string slot){if(slot=="engine"){EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);return;}Leave(()=>category(slot));}
        void Leave(Action action){Hide();action?.Invoke();}
        public void Hide()
        {
            StopAllCoroutines();LastCloseFrame=Time.frameCount;
            if(canvas!=null)canvas.gameObject.SetActive(false);EventSystem.current?.SetSelectedGameObject(null);
            stage?.Dispose();stage=null;
        }
        void Update()
        {
            if(!IsOpen)return;var keyboard=Keyboard.current;var pad=Gamepad.current;
            if(keyboard?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true){Leave(back);return;}
            int shift=keyboard?.leftBracketKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true?-1:
                keyboard?.rightBracketKey.wasPressedThisFrame==true||pad?.rightShoulder.wasPressedThisFrame==true?1:0;
            if(shift!=0){int current=Array.IndexOf(EquipmentCatalog.All.slots,"engine");SwitchCategory(EquipmentCatalog.All.slots[(current+shift+categories.Count)%categories.Count]);return;}
            float rotation=(keyboard?.qKey.isPressed==true?-1:0)+(keyboard?.eKey.isPressed==true?1:0)+(pad?.rightStick.x.ReadValue()??0);
            if(Mathf.Abs(rotation)>.1f)stage.Rotate(rotation*85*Time.unscaledDeltaTime);
        }
        void LateUpdate()
        {
            if(!IsOpen)return;canvas.worldCamera=Camera.main;
            cardFocus.enabled=EventSystem.current.currentSelectedGameObject==selectedCard.gameObject;
            foreach(var button in buttons)button.GetComponentInChildren<Text>().color=EventSystem.current.currentSelectedGameObject==button.gameObject&&button.interactable?
                new Color(.035f,.085f,.105f):button.interactable?Ink:new Color(.58f,.67f,.66f);
        }
        void OnDestroy(){stage?.Dispose();}
    }
    public sealed class PartCarouselNavigation:MonoBehaviour,IMoveHandler
    {
        public PartsBrowser browser;
        public void OnMove(AxisEventData data)
        {if(data.moveDir==MoveDirection.Left)browser.Step(-1);else if(data.moveDir==MoveDirection.Right)browser.Step(1);}
    }
}
