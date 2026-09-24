using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public sealed class RacePreviewUI:MonoBehaviour
    {
        Canvas canvas;Font font;RaceRoster roster;RaceEntrant[] grid;PartPreviewStage stage;
        Action confirm,cancel;Text title,subtitle,pageLabel,driverName,traits,rating,stats,parts,bodyLabel;
        RawImage car;readonly Button[] rows=new Button[8];readonly List<Button> buttons=new();
        Button previous,next,start,back;int page,selected;
        Text[] partNames=new Text[7];
        RacePaddockInspector inspector;Button inspect,tourButton;RectTransform gridMotion;CanvasGroup gridFade;bool touring;float tourStart,nextSpin;Text comparison;
        public RacePaddockInspector Inspector=>inspector;
        public float DisplayCarAngle=>stage?.CarAngle??0;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int SelectedEntrant=>selected;
        public int LastCloseFrame {get;private set;}=-1;
        public int Page=>page;
        public RaceRoster Roster=>roster;
        public string DisplayedParts=>string.Join("\n",partNames.Select(t=>t.text));
        public string DisplayedRating=>rating.text;
        public void FocusStart()=>EventSystem.current.SetSelectedGameObject(start.gameObject);
        static readonly Color Ink=CoastMenu.Navy,Gold=CoastUI.Sun,Muted=CoastUI.Muted;
        public void Open(RaceRoster race,Action onStart,Action onBack)
        {
            if(race.Entries.Count!=24)throw new ArgumentException("Race preview requires the full24-car event.");
            stage?.Dispose();stage=null;
            roster=race;grid=race.Entries.OrderBy(e=>e.Slot).ToArray();confirm=onStart;cancel=onBack;
            if(canvas==null)Build();canvas.worldCamera=Camera.main;canvas.gameObject.SetActive(true);
            title.text=race.Title;subtitle.text=$"STARTING GRID     {grid.Length} DRIVERS     {race.Laps} {(race.Laps==1?"LAP":"LAPS")}     DRY TARMAC";
            selected=0;page=Array.FindIndex(grid,e=>e.Id==0)/8;RefreshPage();SelectEntry(0);StartCoroutine(Focus());
            SetTour(false);
        }
        IEnumerator Focus(){yield return null;if(IsOpen)EventSystem.current.SetSelectedGameObject(rows[Array.FindIndex(grid,e=>e.Id==selected)%8].gameObject);}
        RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 pos)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=pos;return r;}
        RectTransform Box(Transform parent,string name,Vector2 size,Vector2 pos,Color color)
        {var r=Rect(parent,name,size,pos);r.gameObject.AddComponent<Image>().color=color;return r;}
        Text Label(Transform parent,string value,int size,Vector2 dimensions,Vector2 pos,Color color)
        {var t=Rect(parent,"Label",dimensions,pos).gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;}
        Button Button(Transform parent,string text,Vector2 size,Vector2 pos,Action clicked)
        {
            var b=CoastMenu.Button(parent,text,text,size,pos,clicked,text=="START RACE");buttons.Add(b);return b;
        }
        void Build()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(EventSystem.current==null){var go=new GameObject("Race menu input",typeof(EventSystem),typeof(InputSystemUIInputModule));go.transform.SetParent(transform,false);go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            var move=EventSystem.current.GetComponent<InputSystemUIInputModule>()?.move?.action;
            if(move!=null)for(int i=0;i<move.bindings.Count;i++)if(move.bindings[i].path?.StartsWith("<Gamepad>/rightStick")==true)move.ApplyBindingOverride(i,"");
            var root=new GameObject("Race entry preview",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.planeDistance=.6f;canvas.sortingOrder=50;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
            var bg=CoastMenu.Backdrop(root.transform);
            Label(bg,"LITTLE ROAD  /  RACE DAY",18,new Vector2(1770,32),new Vector2(0,496),CoastUI.Sea);
            title=Label(bg,"",40,new Vector2(1770,58),new Vector2(0,450),CoastMenu.Navy);title.fontStyle=FontStyle.BoldAndItalic;
            subtitle=Label(bg,"",19,new Vector2(1770,36),new Vector2(0,402),CoastMenu.Navy);
            var gridPaper=CoastMenu.Paper(bg,"Starting grid paper",new Vector2(658,766),new Vector2(-575,-23));
            Label(bg,"THE STARTING GRID",18,new Vector2(596,35),new Vector2(-575,338),Muted);
            gridMotion=Rect(bg,"Touring grid entries",new Vector2(620,620),Vector2.zero);gridFade=gridMotion.gameObject.AddComponent<CanvasGroup>();
            for(int i=0;i<8;i++) {
                int row=i;rows[i]=Button(gridMotion,"",new Vector2(614,69),new Vector2(-575,278-i*76),()=>SelectRow(row));
                rows[i].gameObject.AddComponent<RacePreviewRow>().selected=()=>SelectRow(row);
                rows[i].gameObject.AddComponent<PaddockHover>().entered=()=>{SetTour(false);EventSystem.current.SetSelectedGameObject(rows[row].gameObject);};
            }
            previous=Button(bg,"<  PREVIOUS",new Vector2(205,52),new Vector2(-782,-351),()=>TurnPage(-1));
            next=Button(bg,"NEXT  >",new Vector2(205,52),new Vector2(-368,-351),()=>TurnPage(1));
            pageLabel=Label(bg,"",22,new Vector2(170,52),new Vector2(-575,-351),Ink);pageLabel.alignment=TextAnchor.MiddleCenter;
            var panel=CoastMenu.Paper(bg,"Entrant detail",new Vector2(1110,766),new Vector2(342,-23));
            driverName=Label(panel,"",32,new Vector2(640,52),new Vector2(-190,330),Ink);driverName.fontStyle=FontStyle.Bold;
            inspect=Button(panel,"EXPLORE PADDOCK",new Vector2(340,46),new Vector2(340,330),OpenInspector);
            var frame=CoastUI.Panel(panel,"Vehicle postcard",new Vector2(518,290),new Vector2(-258,139),CoastMenu.Pale,18);
            var image=Rect(frame,"Actual entered car",new Vector2(500,278),Vector2.zero);car=image.gameObject.AddComponent<RawImage>();car.raycastTarget=false;
            bodyLabel=Label(panel,"",18,new Vector2(520,38),new Vector2(-248,-28),Muted);
            rating=Label(panel,"",49,new Vector2(447,72),new Vector2(287,236),Ink);rating.fontStyle=FontStyle.Bold;
            stats=Label(panel,"",22,new Vector2(447,156),new Vector2(287,118),Ink);
            comparison=Label(panel,"",18,new Vector2(447,35),new Vector2(287,12),Ink);
            Label(panel,"FITTED FOR THIS RACE",16,new Vector2(1020,30),new Vector2(0,-72),Muted);
            for(int i=0;i<7;i++){
                float y=-107-i*25;string slot=EquipmentCatalog.All.slots[i];
                Label(panel,slot.ToUpperInvariant(),16,new Vector2(180,25),new Vector2(-420,y),Muted);
                partNames[i]=Label(panel,"",21,new Vector2(790,25),new Vector2(100,y),Ink);
            }
            CoastUI.Panel(panel,"Driver divider",new Vector2(1020,1),new Vector2(0,-285),CoastMenu.Pale,0);
            traits=Label(panel,"",20,new Vector2(1020,70),new Vector2(0,-332),Ink);
            Label(bg,"Car rating compares equipment. Your driving makes the difference.",19,new Vector2(1780,36),new Vector2(0,-437),CoastMenu.Cream);
            back=Button(bg,"BACK",new Vector2(240,57),new Vector2(-770,-496),()=>Leave(false));
            start=Button(bg,"START RACE",new Vector2(330,57),new Vector2(722,-496),()=>Leave(true));
            tourButton=Button(bg,"TOUR GRID",new Vector2(220,38),new Vector2(775,-438),()=>SetTour(!touring));
            Label(bg,"D-pad / arrows: browse   L1 / R1: pages   Square / I: paddock\nCross / Enter: confirm    Circle / Esc: back",18,new Vector2(1070,62),new Vector2(-50,-495),CoastMenu.Cream);
            for(int i=0;i<8;i++)rows[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i==0?previous:rows[i-1],selectOnDown=i==7?next:rows[i+1],selectOnLeft=previous,selectOnRight=start};
            previous.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[0],selectOnDown=back,selectOnRight=next};
            next.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[7],selectOnDown=back,selectOnLeft=previous,selectOnRight=start};
            back.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=next,selectOnRight=start};
            start.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=inspect,selectOnLeft=back};
            inspect.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnDown=tourButton,selectOnLeft=rows[0]};
            tourButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=inspect,selectOnDown=start,selectOnLeft=next};
        }
        void RefreshPage()
        {
            pageLabel.text=$"{page+1} / 3";
            for(int i=0;i<8;i++){var e=grid[page*8+i];rows[i].name="Entrant "+e.Id;string colour=e.Rating<roster.Entries[0].Rating?(e.Id==selected?"#A2FFD0":"#145E38"):e.Rating>roster.Entries[0].Rating?(e.Id==selected?"#FFD0B8":"#A62820"):(e.Id==selected?"#FFFFFF":"#13343B");string direction=e.Rating<roster.Entries[0].Rating?"↓":e.Rating>roster.Entries[0].Rating?"↑":"=";rows[i].GetComponentInChildren<Text>().text=$"{e.Slot+1:00}   {(e.Id==0?"YOU":e.Driver?.Name??"Driver "+e.Id)}   <color={colour}>{e.Rating} PP {direction}</color>\n       {(e.Id==0?"Your entered car":e.Driver?.Style??"Original driver")}  /  {BodyName(e.Body)}";}
        }
        void SelectRow(int row){if(IsOpen)SelectEntry(grid[page*8+row].Id);}
        public void SelectEntry(int id)
        {
            if(stage!=null&&selected==id)return;
            var e=roster.Entries[id];selected=id;
            driverName.text=$"{(id==0?"YOU":e.Driver?.Name??"Driver "+id)}   /   GRID {e.Slot+1:00}";
            rating.text=$"{e.Rating} PP";
            int delta=e.Rating-roster.Entries[0].Rating;rating.color=delta<0?new Color(.08f,.37f,.22f):delta>0?new Color(.65f,.16f,.12f):Ink;
            comparison.text=id==0?"YOUR EQUIPMENT BASELINE":delta<0?$"↓ {Math.Abs(delta)} PP below your car":delta>0?$"↑ {delta} PP above your car":"= Same rating as your car";comparison.color=rating.color;
            stats.text=$"EQUIPMENT POTENTIAL\nTop speed   {e.Tuning.AttainableSpeed*2.237f:0.0} mph\n0–30 mph    {PartsBrowser.Sprint(e.Tuning):0.00}s estimate\nDry-road rating · stock = 100";
            bodyLabel.text=BodyName(e.Body)+"  /  "+e.EquipmentLabel;
            for(int i=0;i<7;i++)partNames[i].text=EquipmentCatalog.Get(e.Parts[EquipmentCatalog.All.slots[i]]).name;
            traits.text=id==0?"YOU SET THE PACE\nYour inputs control this car. Its rating comes from the fitted equipment.":e.Driver==null?"ORIGINAL DRIVER\nNo personality profile is assigned to this baseline.":$"{e.Driver.Style.ToUpperInvariant()}   ·   Risk {e.Driver.Planning.Risk}/10   ·   Braking {BrakeName(e.Driver.BaseBraking)}\n{(e.Driver.Consistency>=.8f?"Steady":e.Driver.Consistency>=.5f?"Varied":"Variable")} race-to-race braking judgement. Equipment stays fixed.";
            stage?.Dispose();stage=new PartPreviewStage(e.Body,e.Paint,EquipmentCatalog.All.slots.Select(slot=>EquipmentCatalog.Get(e.Parts[slot])),Color.clear,showroom:true);car.texture=stage.Car;RefreshPage();
        }
        static string BrakeName(float value)=>value>=.74f?"precise":value>=.715f?"balanced":"early";
        static string BodyName(string body)=>body switch{"compact_v1"=>"Pocket compact","coupe_car"=>"Coast coupe","utility_car"=>"Trail utility","formula_car"=>"Formula body",_=>body};
        public void TurnPage(int direction){SetTour(false);page=(page+direction+3)%3;RefreshPage();EventSystem.current.SetSelectedGameObject(rows[0].gameObject);SelectRow(0);}
        public void SetTour(bool enabled){touring=enabled;tourStart=Time.unscaledTime;if(tourButton!=null)tourButton.GetComponentInChildren<Text>().text=enabled?"STOP GRID TOUR":"TOUR GRID";if(gridMotion!=null){gridMotion.anchoredPosition=Vector2.zero;gridFade.alpha=1;}}
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
            if(touring){float phase=Time.unscaledTime-tourStart;gridMotion.anchoredPosition=new Vector2(0,Mathf.Max(0,phase-5)*35);gridFade.alpha=phase<.4f?Mathf.Lerp(.2f,1,phase/.4f):phase>5?6-phase:1;if(phase>=6){page=(page+1)%3;RefreshPage();SelectEntry(grid[page*8].Id);tourStart=Time.unscaledTime;}if(k?.upArrowKey.wasPressedThisFrame==true||k?.downArrowKey.wasPressedThisFrame==true||p?.dpad.ReadValue().sqrMagnitude>0)SetTour(false);}
            if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true){Leave(false);return;}
            if(k?.pageUpKey.wasPressedThisFrame==true||p?.leftShoulder.wasPressedThisFrame==true)TurnPage(-1);
            if(k?.pageDownKey.wasPressedThisFrame==true||p?.rightShoulder.wasPressedThisFrame==true)TurnPage(1);
        }
        public void Hide(){LastCloseFrame=Time.frameCount;inspector?.Hide();if(canvas!=null)canvas.gameObject.SetActive(false);stage?.Dispose();stage=null;StopAllCoroutines();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
        void Leave(bool begin){Hide();(begin?confirm:cancel)?.Invoke();}
        public void Confirm()=>Leave(true);
        public void Cancel()=>Leave(false);
        public void Capture(string path)
        {
            CoastMenu.RefreshButtons(canvas.transform);Canvas.ForceUpdateCanvases();var camera=Camera.main;canvas.worldCamera=camera;
            var rt=new RenderTexture(1920,1080,24);var old=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);
        }
        void OnDestroy()=>stage?.Dispose();
    }
}
