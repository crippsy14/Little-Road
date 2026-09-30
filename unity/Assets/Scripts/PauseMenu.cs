using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // The pause hub (docs/ui-design.md): Esc / Start opens it over the dimmed, paused world. Tabs (LB/RB,
    // Q/E or click): MAP, COLLECTION, CAR, SETTINGS. The same header (tabs, area, credits) and footer
    // (button prompts) on every tab; B / Esc goes up one level, and only the top level closes the hub.
    public sealed class PauseMenu:MonoBehaviour
    {
        public sealed class Context
        {
            public Func<AdventureProgress> save;public Func<string> area,objective;public Transform car;
            public Minimap minimap;public Func<List<Minimap.Mark>> marks;public RenderTexture map;
            public Func<List<(string label,Action act)>> settings;public Action quit;
            public Func<(string body,Color paint,string paintName,List<string> parts,int rating,string ladder)> car_;
        }
        static readonly string[] Tabs={"MAP","COLLECTION","CAR","CONTROLS","SETTINGS"};const int Settings=4;
        Context ctx;Canvas canvas;RectTransform root,body;Text areaText,creditText,footer,objectiveText;readonly List<(ToyShape pill,Text text)> tabPills=new();
        int tab;bool subPage;float openedAt;readonly Text[] tabGlyphs=new Text[2];
        public bool IsOpen{get;private set;}
        public int LastCloseFrame{get;private set;}=-1;
        // Map tab state.
        RectTransform mapRect;RegionLayer regions;Minimap.MarkLayer mapMarks;RectTransform cursor;Vector2 cursorAt;MapRegions.Region hovered;Text regionName,regionBlurb;RectTransform countsHolder;
        PartPreviewStage stage;Material mapMaterial;
        const float MapSize=660;

        public void Setup(Context context)
        {
            ctx=context;
            canvas=CoastUI.Canvas(transform,"Pause hub",40);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;canvas.gameObject.AddComponent<GraphicRaycaster>();
            root=(RectTransform)canvas.transform;
            var dim=Toy.Rect(root,"Dim",new Vector2(4000,2000),Vector2.zero).gameObject.AddComponent<Image>();dim.color=new Color(.05f,.08f,.2f,.78f);
            Toy.Card(root,"Hub card",new Vector2(1760,940),new Vector2(0,-6),Toy.Cream,44,7,12);
            // Header: tab pills with the bumper glyphs at either end, area and credits on the right.
            var header=Toy.Rect(root,"Header",new Vector2(1700,90),new Vector2(0,405));
            float x=-700;
            for(int i=0;i<Tabs.Length;i++) {
                var pill=Toy.Card(header,"Tab "+Tabs[i],new Vector2(220,66),new Vector2(x+110,0),Toy.Navy,33,5,5);
                var t=Toy.Label(pill.transform,"Tab text",Tabs[i],25,new Vector2(220,66),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
                int index=i;var b=pill.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>ShowTab(index));b.navigation=new Navigation{mode=Navigation.Mode.None};
                pill.raycastTarget=true;tabPills.Add((pill,t));x+=232;
            }
            tabGlyphs[0]=Toy.Label(header,"Tab glyph left",string.Empty,22,new Vector2(90,40),new Vector2(-760,0),Toy.Navy,TextAnchor.MiddleCenter,true);
            tabGlyphs[1]=Toy.Label(header,"Tab glyph right",string.Empty,22,new Vector2(90,40),new Vector2(x+24,0),Toy.Navy,TextAnchor.MiddleCenter,true);
            areaText=Toy.Label(header,"Area",string.Empty,26,new Vector2(340,40),new Vector2(670,14),Toy.Navy,TextAnchor.MiddleRight,true);
            creditText=Toy.Label(header,"Credits",string.Empty,24,new Vector2(340,34),new Vector2(670,-20),PitStop.Brand,TextAnchor.MiddleRight,true);
            objectiveText=Toy.Label(root,"Objective",string.Empty,22,new Vector2(1600,34),new Vector2(0,344),Toy.Soft,TextAnchor.MiddleCenter,true);
            body=Toy.Rect(root,"Body",new Vector2(1680,740),new Vector2(0,-40));
            footer=Toy.Label(root,"Footer",string.Empty,22,new Vector2(1600,34),new Vector2(-40,-438),Toy.Navy,TextAnchor.MiddleLeft,true);
            canvas.gameObject.SetActive(false);
        }
        public void Open(int startTab=0)
        {
            if(IsOpen)return;IsOpen=true;openedAt=Time.unscaledTime;canvas.gameObject.SetActive(true);
            Time.timeScale=0;AudioListener.pause=true;ShowTab(startTab);
        }
        public void Close()
        {
            if(!IsOpen)return;IsOpen=false;LastCloseFrame=Time.frameCount;canvas.gameObject.SetActive(false);ClearBody();
            Time.timeScale=1;AudioListener.pause=false;EventSystem.current?.SetSelectedGameObject(null);
        }
        void ClearBody(){stage?.Dispose();stage=null;for(int i=body.childCount-1;i>=0;i--)Destroy(body.GetChild(i).gameObject);mapRect=null;regions=null;mapMarks=null;cursor=null;subPage=false;}
        public void ShowTab(int i,int focus=0)
        {
            settingsFocus=focus;
            tab=(i+Tabs.Length)%Tabs.Length;ClearBody();EventSystem.current?.SetSelectedGameObject(null);
            for(int k=0;k<tabPills.Count;k++){bool on=k==tab;tabPills[k].pill.color=on?Toy.Sun:Toy.Navy;tabPills[k].text.color=on?Toy.Navy:Toy.White;tabPills[k].pill.rectTransform.localScale=Vector3.one*(on?1.06f:1);}
            var sides=InputGlyphs.Name("tabs").Split('/');tabGlyphs[0].text=sides[0].Trim();tabGlyphs[1].text=sides[^1].Trim();
            var save=ctx.save();areaText.text=ctx.area().ToUpperInvariant();creditText.text=$"{save.credits:N0} CREDITS";
            string goal=ctx.objective();objectiveText.text=string.IsNullOrEmpty(goal)?"":"Next: "+goal;
            switch(tab){case 0:BuildMap();break;case 1:BuildCollection(save);break;case 2:BuildCar(save);break;case 3:BuildControls();break;default:BuildSettings();break;}
            footer.text=tab==0?InputGlyphs.Footer(true,true,"close")+"     Stick / mouse: look around":InputGlyphs.Footer(true,tab==Settings,"close");
        }
        // ---------------------------------------------------------------- MAP
        void BuildMap()
        {
            var frame=Toy.Card(body,"Map frame",new Vector2(MapSize+16,MapSize+16),new Vector2(-440,-10),Toy.Navy,20,0,6);
            mapRect=Toy.Rect(body,"Map",new Vector2(MapSize,MapSize),new Vector2(-440,-10));
            var img=mapRect.gameObject.AddComponent<RawImage>();img.texture=ctx.map;img.raycastTarget=true;
            mapMaterial??=Minimap.FlatMaterial();if(mapMaterial!=null)img.material=mapMaterial;
            regions=Toy.Rect(mapRect,"Regions",new Vector2(MapSize,MapSize),Vector2.zero).gameObject.AddComponent<RegionLayer>();regions.raycastTarget=false;
            mapMarks=Toy.Rect(mapRect,"Marks",new Vector2(MapSize,MapSize),Vector2.zero).gameObject.AddComponent<Minimap.MarkLayer>();mapMarks.raycastTarget=false;
            cursor=Toy.Rect(mapRect,"Cursor",new Vector2(34,34),Vector2.zero);
            foreach(var (w,h) in new[]{(34f,5f),(5f,34f)}){var bar=Toy.Card(cursor,"Cursor bar",new Vector2(w,h),Vector2.zero,Toy.White,2,0,0);bar.shadow=Vector2.zero;}
            cursorAt=MapPoint(ctx.car.position);
            // Region panel on the right.
            var panel=Toy.Card(body,"Region panel",new Vector2(760,420),new Vector2(400,110),Toy.White,34,6,8);
            regionName=Toy.Title(panel.transform,"Region name","",52,new Vector2(660,70),new Vector2(0,150),Toy.Sun,TextAnchor.MiddleCenter);
            regionBlurb=Toy.Label(panel.transform,"Region blurb","",24,new Vector2(660,40),new Vector2(0,92),Toy.Soft,TextAnchor.MiddleCenter,true);
            countsHolder=Toy.Rect(panel.transform,"Counts",new Vector2(660,240),new Vector2(0,-60));
            var open=Toy.Candy(body,"Choice Open full map","Open the full map",null,new Vector2(560,86),new Vector2(400,-210),Toy.Blue,()=>ctx.minimap?.OpenFull(true));
            Toy.Label(body,"Legend","■ Pit Stop   ◆ Zipline   ★ Objective   ● People",20,new Vector2(760,30),new Vector2(400,-290),Toy.Navy,TextAnchor.MiddleCenter,true);
            hovered=null;UpdateMap();
        }
        Vector2 MapPoint(Vector3 world)=>(WorldMap.Uv(world)-new Vector2(.5f,.5f))*MapSize;
        Vector3 WorldPoint(Vector2 map){var uv=map/MapSize+new Vector2(.5f,.5f);return new Vector3(WorldMap.X0+uv.x*WorldMap.Size,0,WorldMap.Z0+uv.y*WorldMap.Size);}
        void UpdateMap()
        {
            if(mapRect==null)return;
            // Cursor: the mouse over the map, or the left stick / arrow keys.
            var mouse=Mouse.current;var pad=Gamepad.current;var k=Keyboard.current;
            if(mouse!=null&&mouse.delta.ReadValue().sqrMagnitude>.5f&&RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect,mouse.position.ReadValue(),null,out var local)&&mapRect.rect.Contains(local))cursorAt=local;
            var move=pad!=null?pad.leftStick.ReadValue():Vector2.zero;
            if(k!=null)move+=new Vector2((k.rightArrowKey.isPressed||k.dKey.isPressed?1:0)-(k.leftArrowKey.isPressed||k.aKey.isPressed?1:0),(k.upArrowKey.isPressed||k.wKey.isPressed?1:0)-(k.downArrowKey.isPressed||k.sKey.isPressed?1:0));
            if(move.sqrMagnitude>.02f)cursorAt+=move*Time.unscaledDeltaTime*420;
            cursorAt=new Vector2(Mathf.Clamp(cursorAt.x,-MapSize*.5f,MapSize*.5f),Mathf.Clamp(cursorAt.y,-MapSize*.5f,MapSize*.5f));
            cursor.anchoredPosition=cursorAt;
            var region=MapRegions.At(WorldPoint(cursorAt));
            if(region!=hovered){hovered=region;ShowRegion(region);}
            regions.Set(region.id,MapRegions.At(ctx.car.position).id);
            mapMarks.Begin();foreach(var m in ctx.marks())mapMarks.Add(MapPoint(m.at),m.colour,m.shape,m.shape==Minimap.Shape.Objective?10:m.shape==Minimap.Shape.Dot?5:8);
            var f=ctx.car.forward;f.y=0;mapMarks.Player(MapPoint(ctx.car.position),f.sqrMagnitude>1e-4f?Mathf.Atan2(f.x,f.z):0,12);mapMarks.End();
        }
        void ShowRegion(MapRegions.Region r)
        {
            regionName.text=r.name.ToUpperInvariant();regionBlurb.text=r.blurb+(r==MapRegions.At(ctx.car.position)?"  ·  you are here":"");
            for(int i=countsHolder.childCount-1;i>=0;i--)Destroy(countsHolder.GetChild(i).gameObject);
            var save=ctx.save();float y=100;int rows=0;
            foreach(var (label,isKind,colour) in new (string,Func<string,bool>,Color)[]{("Postcards",Collectibles.IsPostcard,Toy.Pink),("Stamps",Collectibles.IsStamp,Toy.Gold)}) {
                var ids=MapRegions.Keepsakes.Where(p=>isKind(p.Key)&&r.contains(p.Value.at.x,p.Value.at.z)).Select(p=>p.Key).ToList();if(ids.Count==0)continue;
                int found=ids.Count(save.collected.Contains);rows++;
                var icon=Toy.Card(countsHolder,label+" icon",new Vector2(60,76),new Vector2(-230,y),colour,8,4,4);icon.rectTransform.localRotation=Quaternion.Euler(0,0,-6);
                Toy.Label(countsHolder,label+" name",label,34,new Vector2(300,50),new Vector2(-40,y),Toy.Navy,TextAnchor.MiddleLeft,true);
                Toy.Label(countsHolder,label+" count",$"{found} / {ids.Count}",40,new Vector2(200,50),new Vector2(210,y),found==ids.Count?Toy.Hex(0x2e9e5b):Toy.Navy,TextAnchor.MiddleRight,true);
                y-=110;
            }
            if(rows==0)Toy.Label(countsHolder,"None","Nothing to collect here.",30,new Vector2(600,50),new Vector2(0,60),Toy.Soft,TextAnchor.MiddleCenter,true);
        }
        // ---------------------------------------------------------------- COLLECTION
        void BuildCollection(AdventureProgress save)
        {
            float y=285;
            void Album(string title,int count,Func<int,string> id,Func<int,string> place,Color colour)
            {
                int found=Enumerable.Range(0,count).Count(i=>save.collected.Contains(id(i)));
                Toy.Title(body,title+" title",$"{title}  {found}/{count}",40,new Vector2(1200,56),new Vector2(-180,y),colour,TextAnchor.MiddleLeft);
                for(int i=0;i<count;i++) {
                    bool has=save.collected.Contains(id(i));float cx=-760+i*195;
                    var card=Toy.Card(body,title+" "+i,new Vector2(170,200),new Vector2(cx+85,y-140),has?colour:Toy.Silver,18,5,6);card.rectTransform.localRotation=Quaternion.Euler(0,0,(i%2==0?-2:2));
                    Toy.Label(card.transform,"Mark",has?(i+1).ToString():"?",has?54:70,new Vector2(170,90),new Vector2(0,26),has?Toy.White:Toy.Navy,TextAnchor.MiddleCenter,true);
                    var region=MapRegions.Keepsakes.TryGetValue(id(i),out var k)?MapRegions.At(k.at).name:"";
                    Toy.Label(card.transform,"Place",has?place(i):"Somewhere in "+region,17,new Vector2(156,60),new Vector2(0,-56),has?Toy.White:Toy.Navy,TextAnchor.MiddleCenter,true);
                }
                y-=310;
            }
            Album("BAYVIEW POSTCARDS",Collectibles.Postcards,Collectibles.Postcard,MapRegions.PostcardPlace,Toy.Pink);
            Album("HILLCREST STAMPS",Collectibles.Stamps,Collectibles.Stamp,MapRegions.StampPlace,Toy.Gold);
            // Badges: one-off achievements.
            float bx=-620;
            foreach(var (id,label) in new[]{(Collectibles.VisitHillcrest,"Reached Hillcrest"),(Collectibles.CastleTrial,"Castle Climb beaten"),(Collectibles.CastleTrialHard,"Hard Climb beaten")}) {
                bool has=save.collected.Contains(id);var badge=Toy.Card(body,"Badge "+label,new Vector2(380,64),new Vector2(bx,-335),has?Toy.Mint:Toy.Silver,32,5,5);
                Toy.Label(badge.transform,"Badge text",(has?"✓ ":"")+label,24,new Vector2(360,64),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,true);bx+=400;
            }
        }
        // ---------------------------------------------------------------- CAR
        void BuildCar(AdventureProgress save)
        {
            var c=ctx.car_();
            try {
                stage=new PartPreviewStage(c.body,c.paint,EquipmentCatalog.All.slots.Select(s=>EquipmentCatalog.Get(save.Loadout()[s])),Color.clear,showroom:false);
                var pic=Toy.Rect(body,"Car picture",new Vector2(900,500),new Vector2(-360,70)).gameObject.AddComponent<RawImage>();pic.texture=stage.Car;pic.raycastTarget=false;
            } catch(Exception e){Debug.LogWarning("Car preview skipped: "+e.Message);}
            Toy.Title(body,"Car name",CarBodies.Name(c.body).ToUpperInvariant(),48,new Vector2(900,60),new Vector2(-360,-220),Toy.Sun,TextAnchor.MiddleCenter);
            Toy.Label(body,"Paint",c.paintName+" paint",26,new Vector2(900,36),new Vector2(-360,-270),Toy.Soft,TextAnchor.MiddleCenter,true);
            var panel=Toy.Card(body,"Car panel",new Vector2(640,660),new Vector2(500,10),Toy.White,34,6,8);
            float y=280;
            Toy.Label(panel.transform,"Parts title","FITTED PARTS",26,new Vector2(560,40),new Vector2(0,y),Toy.Navy,TextAnchor.MiddleLeft,true);y-=46;
            foreach(var line in c.parts){Toy.Label(panel.transform,"Part",line,22,new Vector2(560,32),new Vector2(0,y),Toy.Navy,TextAnchor.MiddleLeft);y-=34;}
            y-=16;Toy.Label(panel.transform,"Rating",$"Performance rating {c.rating}",26,new Vector2(560,36),new Vector2(0,y),PitStop.Brand,TextAnchor.MiddleLeft,true);y-=50;
            Toy.Label(panel.transform,"Record",$"Wins {save.raceWins}   ·   Best finish {(save.bestPlace==0?"–":Toy.Ordinal(save.bestPlace))}",22,new Vector2(560,32),new Vector2(0,y),Toy.Navy,TextAnchor.MiddleLeft,true);y-=48;
            Toy.Label(panel.transform,"Ladder",c.ladder,20,new Vector2(560,70),new Vector2(0,y-10),Toy.Soft,TextAnchor.UpperLeft,true);
            Toy.Label(body,"Car note","Change parts, paint and bodies at any Pit Stop.",20,new Vector2(640,30),new Vector2(500,-345),Toy.Soft,TextAnchor.MiddleCenter,true);
        }
        // ---------------------------------------------------------------- CONTROLS
        // Every binding in one place (the HUD no longer shows button hints while driving).
        static readonly (string action,string keys,string xbox,string ps)[] Bindings={
            ("Accelerate","W / Up","RT","R2"),("Brake / reverse","S / Down","LT","L2"),
            ("Manual gears: up / down","L Shift / L Ctrl","RT / LT","R2 / L2"),("Manual: throttle / brake","W / S","Right stick","Right stick"),("Steer","A D / Left Right","Left stick","Left stick"),
            ("Look behind (hold)","Q","Y","Triangle"),("Recover to the road","R","D-pad down","D-pad down"),("Talk / use","E","A","Cross"),
            ("Map","M","View","Create"),("Pause menu","Esc","Menu","Options"),("Race HUD size","H","L3","L3"),
            ("Menus: select","Enter / Space","A","Cross"),("Menus: back","Esc","B","Circle"),("Menus: tabs","Q / E","LB / RB","L1 / R1"),
            ("Garage: spin a part","Z / X  or drag","Right stick","Right stick"),("Full screen / windowed","F11","",""),};
        void BuildControls()
        {
            var family=InputGlyphs.Current;
            float[] cols={-560,-40,300,600};string[] heads={"ACTION","KEYBOARD & MOUSE","XBOX","PLAYSTATION"};
            for(int c=0;c<4;c++){bool mine=c==1&&family==InputGlyphs.Family.Keyboard||c==2&&family==InputGlyphs.Family.Xbox||c==3&&family==InputGlyphs.Family.PlayStation;
                Toy.Label(body,"Head "+c,heads[c]+(mine?"  ·  IN USE":""),22,new Vector2(c==0?420:c==1?460:320,34),new Vector2(cols[c],300),mine?PitStop.Brand:Toy.Soft,c==0?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter,true);}
            float y=250;int row=0;
            foreach(var (action,keys,xbox,ps) in Bindings) {
                if(row%2==0){var band=Toy.Card(body,"Row band",new Vector2(1560,40),new Vector2(0,y),new Color(1,1,1,.55f),12,0,0);band.shadow=Vector2.zero;}
                Toy.Label(body,"Action",action,22,new Vector2(420,36),new Vector2(-560,y),Toy.Navy,TextAnchor.MiddleLeft,true);
                string[] cells={keys,xbox,ps};
                for(int c=0;c<3;c++)Toy.Label(body,"Binding",cells[c],22,new Vector2(320,36),new Vector2(cols[c+1],y),Toy.Navy,TextAnchor.MiddleCenter,false);
                y-=42;row++;
            }
        }
        // ---------------------------------------------------------------- SETTINGS
        readonly List<Button> rows=new();int settingsFocus;
        void BuildSettings()
        {
            rows.Clear();var items=ctx.settings();
            if(AdventureSession.Store!=null)items.Insert(0,("Save game",()=>AskSave()));
            items.Add(("Quit game",()=>AskQuit()));
            float h=Mathf.Min(70,660f/items.Count),top=320;
            for(int i=0;i<items.Count;i++){var (label,act)=items[i];int row=i;
                string shown=label.Contains(" · ")?label.Replace(" · ","    ‹ ")+" ›":label;   // "Shadows  ‹ High ›": the row cycles its value
                var b=Toy.Candy(body,"Choice "+label,shown,null,new Vector2(900,h-10),new Vector2(0,top-i*h),i==items.Count-1?Toy.Red:Toy.Blue,()=>{act();if(IsOpen&&!subPage){ShowTab(Settings,row);StartCoroutine(Rebuild(row));}});
                var cap=b.GetComponentInChildren<Text>();cap.fontSize=Mathf.RoundToInt(Mathf.Min(30,h*.45f));rows.Add(b);}
            Focus(rows,settingsFocus);
        }
        // Resolution and window changes land a frame or two later; relabel once they have.
        System.Collections.IEnumerator Rebuild(int row){yield return null;yield return null;if(IsOpen&&tab==Settings&&!subPage)ShowTab(Settings,row);}
        // Reviews: put the map cursor over a world point.
        public void PointAt(Vector3 world){cursorAt=MapPoint(world);UpdateMap();}
        public void AskQuit()
        {
            ClearBody();subPage=true;rows.Clear();
            Toy.Title(body,"Quit title","QUIT LITTLE ROAD?",56,new Vector2(1000,80),new Vector2(0,160),Toy.Sun,TextAnchor.MiddleCenter);
            Toy.Label(body,"Quit note",$"Your progress autosaves{(SaveSlots.Current>0?" to slot "+SaveSlots.Current:"")} after every race, purchase and find.",26,new Vector2(1000,40),new Vector2(0,90),Toy.Navy,TextAnchor.MiddleCenter,true);
            rows.Add(Toy.Candy(body,"Choice Keep playing","Keep playing",null,new Vector2(520,84),new Vector2(0,-20),Toy.Mint,()=>ShowTab(Settings)));
            rows.Add(Toy.Candy(body,"Choice Quit","Quit to desktop",null,new Vector2(520,84),new Vector2(0,-120),Toy.Red,()=>ctx.quit()));
            Focus(rows);footer.text=InputGlyphs.Footer(false,true,"back");
        }
        // Save game (docs/50): the game as it stands into a slot's saved game. Saving into another slot moves the game
        // there, autosave and all; a slot holding a different game asks first.
        public void ShowSave()=>AskSave();   // (reviews)
        void AskSave(int confirm=0,string note=null)
        {
            ClearBody();subPage=true;rows.Clear();int current=SaveSlots.Current;
            if(confirm>0) {
                Toy.Title(body,"Save title",$"SAVE OVER SLOT {confirm}?",56,new Vector2(1000,80),new Vector2(0,170),Toy.Sun,TextAnchor.MiddleCenter);
                Toy.Label(body,"Save note","That slot holds another game. It is replaced by this one (a backup copy is kept on disk).",26,new Vector2(1100,40),new Vector2(0,100),Toy.Navy,TextAnchor.MiddleCenter,true);
                rows.Add(Toy.Candy(body,"Choice Keep","Keep that game",null,new Vector2(560,84),new Vector2(0,-10),Toy.Mint,()=>AskSave()));
                rows.Add(Toy.Candy(body,"Choice Save over","Save over it",null,new Vector2(560,84),new Vector2(0,-110),Toy.Red,()=>SaveInto(confirm)));
                Focus(rows);footer.text=InputGlyphs.Footer(false,true,"back");return;
            }
            Toy.Title(body,"Save title","SAVE GAME",60,new Vector2(1000,80),new Vector2(0,250),Toy.Sun,TextAnchor.MiddleCenter);
            Toy.Label(body,"Save note",note??"The game also autosaves to its slot after every race, purchase and find.",26,new Vector2(1200,40),new Vector2(0,185),Toy.Navy,TextAnchor.MiddleCenter,true);
            for(int s=1;s<=SaveSlots.Count;s++) {
                int slot=s;var info=SaveSlots.Read(SaveSlots.Manual(s));var auto=SaveSlots.Read(SaveSlots.Auto(s));
                string label=s==current?$"Slot {s} (this game)  ·  {(info.exists?"saved "+info.When:"not saved yet")}":
                    $"Slot {s}  ·  {(info.exists||auto.exists?(info.exists?info:auto).summary:"empty")}";
                var b=Toy.Candy(body,"Choice Slot "+s,label,null,new Vector2(1100,78),new Vector2(0,90-(s-1)*96),s==current?Toy.Mint:Toy.Blue,
                    ()=>{if(slot!=current&&SaveSlots.Occupied(slot))AskSave(slot);else SaveInto(slot);});
                b.GetComponentInChildren<Text>().fontSize=26;rows.Add(b);
            }
            rows.Add(Toy.Candy(body,"Choice Back","Back",null,new Vector2(360,70),new Vector2(0,-230),Toy.Red,()=>ShowTab(Settings)));
            Focus(rows,Mathf.Max(0,current-1));footer.text=InputGlyphs.Footer(false,true,"back");
        }
        void SaveInto(int slot)
        {
            string note;
            try{bool moved=slot!=SaveSlots.Current;SaveSlots.SaveTo(AdventureSession.Store,slot);if(moved)AdventureSession.Reopen(SaveSlots.LivePath);note=$"Saved to slot {slot}.";}
            catch(System.Exception e){Debug.LogException(e);note="Couldn't save: "+e.Message;}
            AskSave(0,note);
        }
        void Focus(List<Button> list,int first=0)
        {
            for(int i=0;i<list.Count;i++)list[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=list[(i+list.Count-1)%list.Count],selectOnDown=list[(i+1)%list.Count]};
            if(list.Count>0)StartCoroutine(Select(list[Mathf.Clamp(first,0,list.Count-1)]));
        }
        System.Collections.IEnumerator Select(Button b){yield return null;if(b!=null&&IsOpen)EventSystem.current?.SetSelectedGameObject(b.gameObject);}
        // ---------------------------------------------------------------- input
        void Update()
        {
            if(!IsOpen||EngineBench.Showing)return;
            if(ctx.minimap!=null&&(ctx.minimap.FullOpen||ctx.minimap.LastCloseFrame==Time.frameCount))return; // the full map has the input
            if(Time.unscaledTime-openedAt<.1f)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            bool back=k?.escapeKey.wasPressedThisFrame==true||pad?.buttonEast.wasPressedThisFrame==true;
            bool start=pad?.startButton.wasPressedThisFrame==true;
            if(back||start){if(subPage&&!start)ShowTab(Settings);else Close();return;}
            bool left=pad?.leftShoulder.wasPressedThisFrame==true||k?.qKey.wasPressedThisFrame==true,right=pad?.rightShoulder.wasPressedThisFrame==true||k?.eKey.wasPressedThisFrame==true;
            if(left){ShowTab(tab-1);return;}if(right){ShowTab(tab+1);return;}
            if(tab==0) {
                UpdateMap();
                bool open=pad?.buttonSouth.wasPressedThisFrame==true||k?.enterKey.wasPressedThisFrame==true||k?.spaceKey.wasPressedThisFrame==true||
                    Mouse.current?.leftButton.wasPressedThisFrame==true&&RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect,Mouse.current.position.ReadValue(),null,out var p)&&mapRect.rect.Contains(p);
                if(open)ctx.minimap?.OpenFull(true);
            }
        }

        // Region outlines on the pause map: every region thin and faint, the hovered one bold with a tint.
        public sealed class RegionLayer:MaskableGraphic
        {
            string hover,here;
            public void Set(string hovered,string current){if(hover==hovered&&here==current)return;hover=hovered;here=current;SetVerticesDirty();}
            protected override void OnPopulateMesh(VertexHelper vh)
            {
                vh.Clear();var size=rectTransform.rect.size.x;
                Vector2 P(Vector2 w)=>(WorldMap.Uv(new Vector3(w.x,0,w.y))-new Vector2(.5f,.5f))*size;
                foreach(var r in MapRegions.All) {
                    // Outlines only across explored ground: the fog keeps the shape of places you haven't found.
                    bool on=r.id==hover;var pts=r.outline.Select(P).ToList();
                    var line=on?Toy.White:new Color(1,1,1,.45f);float w=on?4.5f:1.8f;
                    for(int i=0;i<pts.Count;i++){
                        var wa=r.outline[i];var wb=r.outline[(i+1)%pts.Count];var mid=(wa+wb)*.5f;if(!ExploredMap.Seen(new Vector3(mid.x,0,mid.y)))continue;
                        var a=pts[i];var b=pts[(i+1)%pts.Count];var n=Vector2.Perpendicular((b-a).normalized)*w*.5f;ToyMesh.Quad(vh,a-n,a+n,b+n,b-n,line);}
                }
            }
        }
    }
}
