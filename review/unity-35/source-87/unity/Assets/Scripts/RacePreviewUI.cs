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
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int SelectedEntrant=>selected;
        public int LastCloseFrame {get;private set;}=-1;
        public int Page=>page;
        public RaceRoster Roster=>roster;
        public string DisplayedParts=>parts.text;
        public string DisplayedRating=>rating.text;
        public void FocusStart()=>EventSystem.current.SetSelectedGameObject(start.gameObject);
        static readonly Color Ink=new(.94f,.96f,.91f),Gold=new(.96f,.73f,.35f),Muted=new(.66f,.79f,.80f);
        public void Open(RaceRoster race,Action onStart,Action onBack)
        {
            if(race.Entries.Count!=24)throw new ArgumentException("Race preview requires the full24-car event.");
            stage?.Dispose();stage=null;
            roster=race;grid=race.Entries.OrderBy(e=>e.Slot).ToArray();confirm=onStart;cancel=onBack;
            if(canvas==null)Build();canvas.worldCamera=Camera.main;canvas.gameObject.SetActive(true);
            title.text=race.Title;subtitle.text=$"STARTING GRID     {grid.Length} DRIVERS     {race.Laps} LAPS     DRY TARMAC";
            selected=0;page=Array.FindIndex(grid,e=>e.Id==0)/8;RefreshPage();SelectEntry(0);StartCoroutine(Focus());
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
            var r=Box(parent,text,size,pos,Color.white);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();
            var colors=b.colors;colors.normalColor=new Color(.12f,.27f,.29f);colors.highlightedColor=new Color(.23f,.40f,.41f);colors.selectedColor=Gold;colors.pressedColor=new Color(.78f,.56f,.22f);b.colors=colors;
            Label(r,text,23,size-new Vector2(30,4),Vector2.zero,Ink);b.onClick.AddListener(()=>clicked());buttons.Add(b);return b;
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
            var bg=Box(root.transform,"Race briefing",new Vector2(1920,1080),Vector2.zero,new Color(.025f,.065f,.085f));
            title=Label(bg,"",38,new Vector2(1770,58),new Vector2(0,466),Gold);
            subtitle=Label(bg,"",21,new Vector2(1770,40),new Vector2(0,410),Muted);
            Label(bg,"GRID / DRIVER / CAR RATING",19,new Vector2(620,35),new Vector2(-575,350),Muted);
            for(int i=0;i<8;i++) {
                int row=i;rows[i]=Button(bg,"",new Vector2(620,71),new Vector2(-575,292-i*78),()=>SelectRow(row));
                rows[i].gameObject.AddComponent<RacePreviewRow>().selected=()=>SelectRow(row);
            }
            previous=Button(bg,"<  PREVIOUS",new Vector2(205,52),new Vector2(-782,-351),()=>TurnPage(-1));
            next=Button(bg,"NEXT  >",new Vector2(205,52),new Vector2(-368,-351),()=>TurnPage(1));
            pageLabel=Label(bg,"",22,new Vector2(170,52),new Vector2(-575,-351),Ink);pageLabel.alignment=TextAnchor.MiddleCenter;
            var panel=Box(bg,"Entrant detail",new Vector2(1110,762),new Vector2(342,-22),new Color(.065f,.145f,.17f));
            driverName=Label(panel,"",33,new Vector2(1030,52),new Vector2(0,325),Gold);
            var image=Rect(panel,"Actual entered car",new Vector2(490,274),new Vector2(-273,143));car=image.gameObject.AddComponent<RawImage>();car.raycastTarget=false;
            bodyLabel=Label(panel,"",20,new Vector2(500,35),new Vector2(-273,-8),Muted);
            rating=Label(panel,"",44,new Vector2(465,72),new Vector2(286,228),Gold);
            stats=Label(panel,"",22,new Vector2(465,133),new Vector2(286,121),Ink);
            Label(panel,"ACTUAL FITTED PARTS",18,new Vector2(1030,30),new Vector2(0,-52),Muted);
            parts=Label(panel,"",23,new Vector2(1030,210),new Vector2(0,-172),Ink);
            traits=Label(panel,"",21,new Vector2(1030,80),new Vector2(0,-312),Ink);
            Label(bg,"Equipment rating estimates car potential, not finishing order. Driver traits never add engine power or grip.",20,new Vector2(1780,38),new Vector2(0,-437),Muted);
            back=Button(bg,"BACK",new Vector2(240,57),new Vector2(-770,-496),()=>Leave(false));
            start=Button(bg,"START RACE",new Vector2(330,57),new Vector2(722,-496),()=>Leave(true));
            Label(bg,"D-pad / arrows: inspect    L1 / R1 or PgUp / PgDn: pages\nCross / Enter: confirm    Circle / Esc: back",19,new Vector2(1070,62),new Vector2(-50,-495),Muted);
            for(int i=0;i<8;i++)rows[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i==0?previous:rows[i-1],selectOnDown=i==7?next:rows[i+1],selectOnLeft=previous,selectOnRight=start};
            previous.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[0],selectOnDown=back,selectOnRight=next};
            next.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[7],selectOnDown=back,selectOnLeft=previous,selectOnRight=start};
            back.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=next,selectOnRight=start};
            start.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[7],selectOnLeft=back};
        }
        void RefreshPage()
        {
            pageLabel.text=$"{page+1} / 3";
            for(int i=0;i<8;i++){var e=grid[page*8+i];rows[i].name="Entrant "+e.Id;rows[i].GetComponentInChildren<Text>().text=$"{e.Slot+1:00}   {(e.Id==0?"YOU":e.Driver?.Name??"Driver "+e.Id)}     {e.Rating} PP\n       {(e.Id==0?"Your entered car":e.Driver?.Style??"Original driver")}  /  {BodyName(e.Body)}";}
        }
        void SelectRow(int row){if(IsOpen)SelectEntry(grid[page*8+row].Id);}
        public void SelectEntry(int id)
        {
            if(stage!=null&&selected==id)return;
            var e=roster.Entries[id];selected=id;
            driverName.text=$"{(id==0?"YOU":e.Driver?.Name??"Driver "+id)}   /   GRID {e.Slot+1:00}";
            rating.text=$"{e.Rating} PP";
            stats.text=$"EQUIPMENT POTENTIAL\nTop speed   {e.Tuning.AttainableSpeed*2.237f:0.0} mph\n0–30 mph    {PartsBrowser.Sprint(e.Tuning):0.00}s estimate\nDry-road rating · stock = 100";
            bodyLabel.text=BodyName(e.Body)+"  /  "+e.EquipmentLabel;
            parts.text=string.Join("\n",EquipmentCatalog.All.slots.Select(slot=>$"{slot.ToUpperInvariant(),-14}  {EquipmentCatalog.Get(e.Parts[slot]).name}"));
            traits.text=id==0?"YOU SET THE PACE\nYour inputs control this car. Its rating comes from the fitted equipment.":e.Driver==null?"ORIGINAL DRIVER\nNo personality profile is assigned to this baseline.":$"{e.Driver.Style.ToUpperInvariant()}   ·   Risk {e.Driver.Planning.Risk}/10   ·   Braking {BrakeName(e.Driver.BaseBraking)}\n{(e.Driver.Consistency>=.8f?"Steady":e.Driver.Consistency>=.5f?"Varied":"Variable")} race-to-race braking judgement. Equipment stays fixed.";
            stage?.Dispose();stage=new PartPreviewStage(e.Body,e.Paint,Array.Empty<EquipmentCatalog.Part>());car.texture=stage.Car;
        }
        static string BrakeName(float value)=>value>=.74f?"precise":value>=.715f?"balanced":"early";
        static string BodyName(string body)=>body switch{"compact_v1"=>"Pocket compact","coupe_car"=>"Coast coupe","utility_car"=>"Trail utility","formula_car"=>"Formula body",_=>body};
        public void TurnPage(int direction){page=(page+direction+3)%3;RefreshPage();EventSystem.current.SetSelectedGameObject(rows[0].gameObject);SelectRow(0);}
        void Update()
        {
            if(!IsOpen)return;var k=Keyboard.current;var p=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true){Leave(false);return;}
            if(k?.pageUpKey.wasPressedThisFrame==true||p?.leftShoulder.wasPressedThisFrame==true)TurnPage(-1);
            if(k?.pageDownKey.wasPressedThisFrame==true||p?.rightShoulder.wasPressedThisFrame==true)TurnPage(1);
        }
        void LateUpdate(){if(IsOpen)foreach(var b in buttons)b.GetComponentInChildren<Text>().color=EventSystem.current.currentSelectedGameObject==b.gameObject?new Color(.035f,.08f,.10f):Ink;}
        public void Hide(){LastCloseFrame=Time.frameCount;if(canvas!=null)canvas.gameObject.SetActive(false);stage?.Dispose();stage=null;StopAllCoroutines();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
        void Leave(bool begin){Hide();(begin?confirm:cancel)?.Invoke();}
        public void Confirm()=>Leave(true);
        public void Capture(string path)
        {
            Canvas.ForceUpdateCanvases();var camera=Camera.main;canvas.worldCamera=camera;
            var rt=new RenderTexture(1920,1080,24);var old=camera.targetTexture;var active=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            camera.targetTexture=old;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(image);
        }
        void OnDestroy()=>stage?.Dispose();
    }
}
