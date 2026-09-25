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
        Canvas canvas;AdventureStore store;PartPreviewStage stage;ToyShape selectedShape;
        Action fitted,back;Action<string> category;
        EquipmentCatalog.Part[] parts;int index;string remembered="engine_stock",notice;
        Text wallet,title,state,hint,comparisonHeading,explanation,status,leftLabel,rightLabel;readonly Text[] before=new Text[3],after=new Text[3];
        RawImage selectedImage,leftImage,rightImage,carImage;
        Button selectedCard,buy,fit,previous,next,exit;readonly List<Button> buttons=new();readonly List<Button> categories=new();
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        public string SelectedId=>parts[index].id;
        public float PreviewAngle=>stage.Angle;
        public int PreviewRenderCount=>stage.RenderCount;

        public void Open(AdventureStore inventory,Action onFitted,Action onBack,Action<string> onCategory)
        {
            store=inventory;fitted=onFitted;back=onBack;category=onCategory;
            parts=EquipmentCatalog.All.parts.Where(p=>p.slot=="engine").OrderBy(p=>p.price).ToArray();
            index=Mathf.Max(0,Array.FindIndex(parts,p=>p.id==remembered));notice="";
            if(canvas==null)Build();
            stage=new PartPreviewStage(store.Data.bodyId,AdventureGame.Paints[store.Data.paintIndex],parts,Color.clear);
            carImage.texture=stage.Car;selectedImage.texture=stage.Selected;
            Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);
            Refresh(true);StartCoroutine(FocusNextFrame());
        }
        IEnumerator FocusNextFrame(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(selectedCard.gameObject);}
        // Harbour Garage in the toy style: category stickers, part carousel,
        // your car, setup comparison and candy actions. Button names BUY / FIT
        // are used by PartsBrowserReview and the adventure smoke test.
        void Build()
        {
            var root=new GameObject("Visual parts browser",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=.7f;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var background=Toy.Rect(root.transform,"Workshop",new Vector2(1920,1080),Vector2.zero);background.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(background,new Vector2(2600,2600),new Vector2(0,40),Toy.Orange,Toy.OrangeLight,30,2);
            Toy.Checker(background,1920,525);Toy.Checker(background,1920,-525);
            Toy.Title(background,"Garage title","HARBOUR GARAGE",86,new Vector2(900,100),new Vector2(-470,430),Toy.White,TextAnchor.MiddleLeft,-2);
            var purse=Toy.Card(background,"Wallet",new Vector2(360,70),new Vector2(700,440),Toy.Sun,35,6,6);
            var coin=Toy.Card(purse.transform,"Coin",new Vector2(54,54),new Vector2(-138,0),Toy.Gold,27,4,0);Toy.Title(coin.transform,"C","C",34,new Vector2(54,54),Vector2.zero,Toy.White,TextAnchor.MiddleCenter);
            coin.gameObject.AddComponent<ToyCoinFlip>();
            wallet=Toy.Label(purse.transform,"Wallet text",string.Empty,30,new Vector2(280,70),new Vector2(30,0),Toy.Navy,TextAnchor.MiddleCenter,true);

            Toy.Title(background,"Catalogue","PARTS",40,new Vector2(300,50),new Vector2(-770,345),Toy.Sun);
            for(int i=0;i<EquipmentCatalog.All.slots.Length;i++) {
                string slot=EquipmentCatalog.All.slots[i];
                var button=Toy.Candy(background,slot.ToUpperInvariant(),slot.ToUpperInvariant(),null,new Vector2(270,64),new Vector2(-770,280-i*76),slot=="engine"?Toy.Red:Toy.Blue,()=>SwitchCategory(slot));
                categories.Add(button);buttons.Add(button);
            }

            var carCard=Toy.Card(background,"Your car",new Vector2(400,250),new Vector2(700,250),Toy.Cream,30,6,8);
            Toy.Title(carCard.transform,"Your car heading","YOUR CAR",30,new Vector2(360,40),new Vector2(0,100),Toy.Orange,TextAnchor.MiddleCenter);
            carImage=Image(carCard.transform,"Whole car",new Vector2(380,210),new Vector2(0,-16));

            var left=Toy.Card(background,"Previous part",new Vector2(290,320),new Vector2(-355,60),Toy.White,30,6,8);
            leftImage=Image(left.transform,"Model",new Vector2(286,240),new Vector2(0,24));
            leftLabel=Toy.Label(left.transform,"Name",string.Empty,21,new Vector2(260,60),new Vector2(0,-122),Toy.Navy,TextAnchor.MiddleCenter,true);
            var right=Toy.Card(background,"Next part",new Vector2(290,320),new Vector2(470,20),Toy.White,30,6,8);
            rightImage=Image(right.transform,"Model",new Vector2(286,240),new Vector2(0,24));
            rightLabel=Toy.Label(right.transform,"Name",string.Empty,21,new Vector2(260,60),new Vector2(0,-122),Toy.Navy,TextAnchor.MiddleCenter,true);
            selectedShape=Toy.Card(background,"Selected part",new Vector2(620,450),new Vector2(55,70),Toy.Cream,40,8,12);selectedShape.raycastTarget=true;
            var burst=Toy.Sunburst(selectedShape.transform,new Vector2(420,420),new Vector2(0,40),Toy.Sun,Toy.Hex(0xffe58a),16,5);burst.circular=true;
            selectedCard=selectedShape.gameObject.AddComponent<Button>();selectedCard.targetGraphic=selectedShape;selectedCard.transition=Selectable.Transition.None;
            selectedShape.gameObject.AddComponent<PartCarouselNavigation>().browser=this;
            selectedImage=Image(selectedShape.transform,"Rotatable engine",new Vector2(600,340),new Vector2(0,40));
            title=Toy.Title(selectedShape.transform,"Part name",string.Empty,46,new Vector2(580,60),new Vector2(0,-140),Toy.Sun,TextAnchor.MiddleCenter);
            state=Toy.Label(selectedShape.transform,"Part state",string.Empty,22,new Vector2(580,36),new Vector2(0,-192),Toy.Navy,TextAnchor.MiddleCenter,true);
            previous=Toy.Candy(background,"PREVIOUS","< PREV",null,new Vector2(220,70),new Vector2(-355,-160),Toy.Sky,()=>Step(-1));buttons.Add(previous);
            next=Toy.Candy(background,"NEXT","NEXT >",null,new Vector2(220,70),new Vector2(470,-160),Toy.Sky,()=>Step(1));buttons.Add(next);
            Toy.Label(background,"Rotate hint","Left / right: browse · Q / E: spin · L1 / R1: category",19,new Vector2(560,30),new Vector2(55,-214),Toy.Navy,TextAnchor.MiddleCenter,true);

            var detail=Toy.Card(background,"Complete setup comparison",new Vector2(1560,210),new Vector2(110,-340),Toy.Cream,34,7,10);
            hint=Toy.Label(detail.transform,"Hint",string.Empty,25,new Vector2(620,90),new Vector2(-430,44),Toy.Navy,TextAnchor.UpperLeft,true);
            explanation=Toy.Label(detail.transform,"Explanation",string.Empty,18,new Vector2(620,80),new Vector2(-430,-50),Toy.Soft,TextAnchor.UpperLeft,false);
            comparisonHeading=Toy.Label(detail.transform,"Comparison heading",string.Empty,20,new Vector2(820,30),new Vector2(330,76),Toy.Orange,TextAnchor.MiddleLeft,true);
            string[] captions={"POWER INDEX","0-30 MPH","TOP SPEED"};
            for(int i=0;i<3;i++){
                float y=30-i*48;Toy.Label(detail.transform,"Stat "+i,captions[i],20,new Vector2(220,40),new Vector2(20,y),Toy.Navy,TextAnchor.MiddleLeft,true);
                before[i]=Toy.Pill(detail.transform,"Before "+i,string.Empty,20,new Vector2(170,40),new Vector2(290,y),Toy.White,Toy.Navy).GetComponentInChildren<Text>();
                Toy.Title(detail.transform,"Arrow "+i,">",30,new Vector2(40,40),new Vector2(400,y),Toy.Orange,TextAnchor.MiddleCenter);
                after[i]=Toy.Pill(detail.transform,"After "+i,string.Empty,20,new Vector2(170,40),new Vector2(510,y),Toy.Mint,Toy.Navy).GetComponentInChildren<Text>();
            }
            buy=Toy.Candy(background,"BUY","BUY",null,new Vector2(360,82),new Vector2(40,-478),Toy.Gold,Buy,true);buttons.Add(buy);
            fit=Toy.Candy(background,"FIT","FIT",null,new Vector2(330,82),new Vector2(410,-478),Toy.Mint,Fit);buttons.Add(fit);
            exit=Toy.Candy(background,"BACK TO GARAGE","BACK","O",new Vector2(260,82),new Vector2(740,-478),Toy.Red,()=>Leave(back));buttons.Add(exit);
            status=Toy.Label(background,"Status",string.Empty,21,new Vector2(760,62),new Vector2(-520,-478),Toy.Navy,TextAnchor.MiddleLeft,true);
        }
        RawImage Image(Transform parent,string name,Vector2 size,Vector2 pos)
        {var image=Toy.Rect(parent,name,size,pos).gameObject.AddComponent<RawImage>();image.raycastTarget=false;return image;}
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
            comparisonHeading.text=$"{EquipmentCatalog.Get(store.Data.Loadout()[part.slot]).name.ToUpperInvariant()} (FITTED)  >  {part.name.ToUpperInvariant()}";
            before[0].text=$"{actual.power/CarTuning.Stock.power*100:0}";after[0].text=$"{target.power/CarTuning.Stock.power*100:0}";
            before[1].text=$"{Sprint(actual):0.00} s";after[1].text=$"{Sprint(target):0.00} s";
            before[2].text=$"{actual.AttainableSpeed*2.237f:0.0} mph";after[2].text=$"{target.AttainableSpeed*2.237f:0.0} mph";
            explanation.text="Estimates use your other fitted parts on level, dry tarmac. Power index: stock setup = 100.\n"+
                (target.AttainableSpeed>=target.gearCeiling-.05f?"Your transmission limits the top speed of this setup.":"Power, gearing and tyre grip determine usable performance.");
            buy.interactable=!owned&&store.Data.credits>=part.price;buy.GetComponentInChildren<Text>().text=owned?"OWNED":$"BUY  {part.price:N0} CR";
            fit.interactable=owned&&!isFitted;fit.GetComponentInChildren<Text>().text=isFitted?"FITTED":owned?"FIT ENGINE":"BUY FIRST";
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
            if(!IsOpen)return;if(canvas.worldCamera==null)Toy.UseMenuCamera(canvas);
            bool focus=EventSystem.current.currentSelectedGameObject==selectedCard.gameObject;selectedShape.SetBorder(focus?Toy.Orange:Toy.Navy);
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
