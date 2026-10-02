using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // The tuning shop (docs/48): the fitted gearbox's final drive and, on sport boxes and up, each gear, with GT's
    // gear chart (revs against road speed per gear, the redline, the 85%-torque band, the top speed) and the power
    // and torque curve beside it. Changes save at once; the test drive runs a timed lap of the town's circuit.
    public sealed class TuningScreen : MonoBehaviour
    {
        Canvas canvas;AdventureStore store;Action applied,back,testDrive;string town="bayview";
        GearTune tune;Drivetrain train;CarTuning car;
        readonly List<Row> rows=new();int focus;
        Text heading,numbers,verdict,hint,results;RawImage gearChart,powerChart;Texture2D gearTex,powerTex;
        readonly List<Text> gearLabels=new();RectTransform gearCard;Text boxTitle;
        public bool IsOpen=>canvas!=null&&canvas.gameObject.activeSelf;
        public int LastCloseFrame {get;private set;}=-1;
        // Results of the last test drive (shown under the numbers until the tune changes).
        public static string LastTestDrive;

        sealed class Row{public string name;public ToyShape shape;public Text label,value;public Action<int> adjust;public Action press;}

        public void Open(AdventureStore inventory,string where,Action onApplied,Action onBack,Action onTestDrive)
        {
            store=inventory;town=where;applied=onApplied;back=onBack;testDrive=onTestDrive;
            if(canvas==null)Build();
            var box=store.Data.Loadout()["transmission"];tune=Copy(store.Data.Tune())??new GearTune{transmission=box};
            heading.text=(town=="hillcrest"?"HILLCREST MOTORS":"HARBOUR GARAGE")+"  ·  TUNING";
            BuildRows();focus=0;canvas.gameObject.SetActive(true);Refresh();
        }
        public void Close(){if(!IsOpen)return;canvas.gameObject.SetActive(false);LastCloseFrame=Time.frameCount;back?.Invoke();}
        static GearTune Copy(GearTune t)=>t==null?null:new GearTune{transmission=t.transmission,final=t.final,gears=t.gears?.ToArray()};
        static bool GearsAdjustable(string box)=>box!="transmission_stock"&&box!="transmission_close";

        void Build()
        {
            var root=new GameObject("Tuning shop",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var bg=Toy.Rect(root.transform,"Workshop",new Vector2(1920,1080),Vector2.zero);bg.gameObject.AddComponent<RectMask2D>();
            Toy.Sunburst(bg,new Vector2(2600,2600),new Vector2(0,40),Toy.Blue,Toy.Sky,30,2);Toy.Checker(bg,1920,525);Toy.Checker(bg,1920,-525);
            heading=Toy.Title(bg,"Tuning title","HARBOUR GARAGE  ·  TUNING",60,new Vector2(1200,80),new Vector2(-330,440),Toy.White,TextAnchor.MiddleLeft,-2);
            // Left: the car's numbers.
            var left=Toy.Card(bg,"Car numbers",new Vector2(470,560),new Vector2(-690,40),Toy.Cream,30,6,8);
            Toy.Label(left.transform,"Numbers title","YOUR CAR",32,new Vector2(420,50),new Vector2(0,235),Toy.Navy,TextAnchor.MiddleCenter,true);
            numbers=Toy.Label(left.transform,"Numbers",string.Empty,24,new Vector2(420,300),new Vector2(0,40),Toy.Navy,TextAnchor.UpperLeft,true);
            verdict=Toy.Label(left.transform,"Verdict",string.Empty,22,new Vector2(420,110),new Vector2(0,-150),Toy.Soft,TextAnchor.UpperLeft,true);
            results=Toy.Label(left.transform,"Test drive",string.Empty,21,new Vector2(420,70),new Vector2(0,-240),Toy.Blue,TextAnchor.UpperLeft,true);
            // Middle: the gear table and the actions.
            gearCard=Toy.Card(bg,"Gearbox",new Vector2(560,700),new Vector2(-150,-10),Toy.White,30,6,8).rectTransform;
            // Right: the charts.
            var chartCard=Toy.Card(bg,"Charts",new Vector2(760,700),new Vector2(540,-10),Toy.Navy,30,6,8);
            Toy.Title(chartCard.transform,"Gear chart title","GEAR CHART",26,new Vector2(700,40),new Vector2(0,315),Toy.White,TextAnchor.MiddleLeft);
            gearTex=new Texture2D(720,380,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};powerTex=new Texture2D(720,200,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            gearChart=Toy.Rect(chartCard.transform,"Gear chart",new Vector2(720,380),new Vector2(0,100)).gameObject.AddComponent<RawImage>();gearChart.texture=gearTex;gearChart.raycastTarget=false;
            for(int i=0;i<8;i++){var t=Toy.Label(gearChart.transform,"Gear "+(i+1),(i+1).ToString(),20,new Vector2(40,28),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);gearLabels.Add(t);}
            Toy.Title(chartCard.transform,"Power title","POWER AND TORQUE",22,new Vector2(700,36),new Vector2(0,-120),Toy.White,TextAnchor.MiddleLeft);
            Toy.Label(chartCard.transform,"Gear chart legend","red: redline  ·  amber: peak power  ·  shaded: 85% torque  ·  green: top speed",15,new Vector2(720,24),new Vector2(0,-96),Toy.Silver,TextAnchor.MiddleCenter,true);
            Toy.Label(chartCard.transform,"Power legend","blue: torque · yellow: power · along: revs",15,new Vector2(300,24),new Vector2(210,-120),Toy.Silver,TextAnchor.MiddleRight,true);
            powerChart=Toy.Rect(chartCard.transform,"Power chart",new Vector2(720,200),new Vector2(0,-235)).gameObject.AddComponent<RawImage>();powerChart.texture=powerTex;powerChart.raycastTarget=false;
            hint=Toy.Label(bg,"Controls",string.Empty,20,new Vector2(1700,30),new Vector2(0,-400),Toy.Navy,TextAnchor.MiddleCenter,true);
        }

        void BuildRows()
        {
            foreach(var r in rows)Destroy(r.shape.gameObject);rows.Clear();
            var fit=store.Data.Loadout();string box=fit["transmission"];var part=EquipmentCatalog.Get(box);
            boxTitle??=Toy.Label(gearCard,"Box title",string.Empty,28,new Vector2(500,44),new Vector2(0,315),Toy.Navy,TextAnchor.MiddleCenter,true);boxTitle.text=part.name.ToUpperInvariant();
            float y=255;
            Row Add(string name,Action<int> adjust,Action press,Color fill)
            {
                var shape=Toy.Card(gearCard,"Row "+name,new Vector2(500,40),new Vector2(0,y),fill,16,3,0);shape.raycastTarget=true;
                var label=Toy.Label(shape.transform,"Name",name,21,new Vector2(260,40),new Vector2(-110,0),Toy.Navy,TextAnchor.MiddleLeft,true);
                var value=Toy.Label(shape.transform,"Value",string.Empty,21,new Vector2(220,40),new Vector2(125,0),Toy.Navy,TextAnchor.MiddleRight,true);
                var row=new Row{name=name,shape=shape,label=label,value=value,adjust=adjust,press=press};int index=rows.Count;
                var b=shape.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;b.navigation=new Navigation{mode=Navigation.Mode.None};
                b.onClick.AddListener(()=>{focus=index;press?.Invoke();Refresh();});
                rows.Add(row);y-=46;return row;
            }
            Add("Final drive",d=>{tune.final=Mathf.Clamp(Mathf.Round((tune.final+d*.01f)*100)/100,GearTune.FinalMin,GearTune.FinalMax);Save();},null,Toy.Sun);
            int gears=Mathf.Clamp(part.gears,3,8);
            for(int g=0;g<gears;g++) {
                int gear=g;
                Add("Gear "+(g+1),GearsAdjustable(box)?d=>{tune.gears??=Enumerable.Repeat(1f,gears).ToArray();tune.gears[gear]=Mathf.Clamp(Mathf.Round((tune.gears[gear]+d*.01f)*100)/100,GearTune.GearMin,GearTune.GearMax);Save();}:null,null,Toy.Cream);
            }
            y-=8;
            Add("Auto-tune: acceleration",null,()=>AutoTune(EquipmentCatalog.TunePreset.Acceleration),Toy.Mint);
            Add("Auto-tune: balanced",null,()=>AutoTune(EquipmentCatalog.TunePreset.Balanced),Toy.Mint);
            Add("Auto-tune: top speed",null,()=>AutoTune(EquipmentCatalog.TunePreset.TopSpeed),Toy.Mint);
            Add("Reset to the box's own",null,()=>{tune=new GearTune{transmission=box};Save();},Toy.Silver);
            Add("Test drive: a timed lap",null,()=>{if(testDrive!=null){canvas.gameObject.SetActive(false);LastCloseFrame=Time.frameCount;testDrive();}},Toy.Orange);
            Add("Done",null,Close,Toy.Gold);
        }
        void AutoTune(EquipmentCatalog.TunePreset preset){var t=EquipmentCatalog.AutoTune(store.Data.Loadout(),preset);if(t!=null){tune=t;Save();}}
        void Save()
        {
            bool isDefault=Mathf.Approximately(tune.final,1)&&(tune.gears==null||tune.gears.All(g=>Mathf.Approximately(g,1)));
            store.SetTune(isDefault?null:Copy(tune));LastTestDrive=null;applied?.Invoke();Refresh();
        }

        void Refresh()
        {
            var fit=store.Data.Loadout();car=EquipmentCatalog.Stats(fit,store.Data.Tune());train=car.drivetrain;if(train==null)return;
            var e=EquipmentCatalog.Get(fit["engine"]);var turbo=EquipmentCatalog.Get(fit["turbo"]);
            // Rows: values and focus.
            float kmh(float mps)=>mps*3.6f;
            for(int i=0;i<rows.Count;i++) {
                var r=rows[i];bool on=i==focus;r.shape.SetFill(on?Toy.Sun:r.adjust==null&&r.press==null?Toy.Silver:r.name.StartsWith("Final")?Toy.Cream:r.press!=null?(r.name=="Done"?Toy.Gold:r.name.StartsWith("Test")?Toy.Orange:r.name.StartsWith("Reset")?Toy.Silver:Toy.Mint):Toy.Cream);
                if(r.name=="Final drive")r.value.text=$"{tune.final:0.00}  ·  {kmh(train.RedlineSpeed):0} km/h";
                else if(r.name.StartsWith("Gear ")){int g=int.Parse(r.name.Substring(5))-1;r.value.text=g<train.Gears?$"{kmh(train.redline/train.ratio[g]):0} km/h"+(r.adjust==null?"":$"  ({(tune.gears!=null&&g<tune.gears.Length?tune.gears[g]:1):0.00})"):"";}
            }
            // Numbers.
            var (to60,to100,top)=Measure(car);
            float hp=e.hp*(turbo.power>0?turbo.power:1);
            numbers.text=$"{e.name}{(turbo.power>1.001f?" + "+turbo.name:"")}\n{hp:0} hp  ·  redline {train.redline:N0} rpm\n\n"+
                $"Top speed  {kmh(train.ReachableTop):0} km/h  ({train.ReachableTop*2.237f:0} mph)\n   in gear {train.TopSpeedGear+1} of {train.Gears}\n"+
                $"0-60 mph   {(to60>0?to60.ToString("0.0")+" s":"—")}\n0-100 km/h {(to100>0?to100.ToString("0.0")+" s":"—")}";
            verdict.text=train.TopGearLimited?"On the limiter in top gear: this box (or final drive) is holding the car back. A longer final drive or box will go faster.":
                train.TopSpeedGear<train.Gears-1?"Over-geared: the car tops out in a lower gear. A shorter final drive will pull harder.":
                "Top gear is well matched: the car runs out of power just short of the limiter.";
            results.text=LastTestDrive??"";
            DrawGearChart();DrawPowerChart();
            hint.text=InputGlyphs.Current==InputGlyphs.Family.Keyboard?"↑ ↓  choose   ·   ← →  adjust   ·   Enter  select   ·   Esc  done":"D-pad ↑ ↓  choose   ·   ← →  adjust   ·   Cross  select   ·   Circle  done";
        }

        // Standing start with the automatic box and the tyres' grip: 0-60 mph, 0-100 km/h and the top reached.
        static (float,float,float) Measure(CarTuning car)
        {
            var box=new GearState(car.drivetrain);float v=0,t=0,to60=-1,to100=-1;const float dt=1f/60;float grip=car.roadGrip*1.65f;
            while(t<40){float a=Mathf.Min(box.Step(v,1,dt),grip);v=Mathf.Max(0,v+(a-DriveModel.Resistance(car,v))*dt);t+=dt;if(to60<0&&v>=26.82f)to60=t;if(to100<0&&v>=27.78f)to100=t;}
            return (to60,to100,v);
        }

        void Fill(Texture2D tex,Color c){var px=tex.GetPixels32();Color32 c32=c;for(int i=0;i<px.Length;i++)px[i]=c32;tex.SetPixels32(px);}
        void Line(Texture2D tex,float x0,float y0,float x1,float y1,Color c,int width=2)
        {
            int n=Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0)))+1;
            for(int i=0;i<=n;i++){float t=i/(float)n;int x=Mathf.RoundToInt(Mathf.Lerp(x0,x1,t)),y=Mathf.RoundToInt(Mathf.Lerp(y0,y1,t));
                for(int dx=0;dx<width;dx++)for(int dy=0;dy<width;dy++){int px=x+dx,py=y+dy;if(px>=0&&py>=0&&px<tex.width&&py<tex.height)tex.SetPixel(px,py,c);}}
        }
        void DrawGearChart()
        {
            var tex=gearTex;Fill(tex,new Color(.1f,.14f,.3f));
            const float left=34,bottom=24;float w=tex.width-left-10,h=tex.height-bottom-10;
            float maxSpeed=train.RedlineSpeed*1.08f,maxRpm=train.redline*1.08f;
            float X(float mps)=>left+w*mps/maxSpeed;float Y(float rpm)=>bottom+h*rpm/maxRpm;
            // The 85%-torque band (where every gear should start after a shift) and the power peak.
            float lo=float.MaxValue,hi=0;for(float r=train.idle;r<=train.redline;r+=25)if(train.Tau(r)>=.85f){lo=Mathf.Min(lo,r);hi=Mathf.Max(hi,r);}
            for(int y=Mathf.RoundToInt(Y(lo));y<=Mathf.RoundToInt(Y(hi));y++)for(int x=(int)left;x<tex.width-10;x++)tex.SetPixel(x,y,new Color(.14f,.27f,.3f));
            Line(tex,left,Y(train.powerRpm),left+w,Y(train.powerRpm),new Color(1,.78f,.3f),1);
            Line(tex,left,Y(train.redline),left+w,Y(train.redline),new Color(1,.32f,.27f),2);
            Line(tex,left,bottom,left,bottom+h,Color.white,1);Line(tex,left,bottom,left+w,bottom,Color.white,1);
            // Each gear from rest to its redline, with the drop into the next gear (the sawtooth of a full pull).
            Color[] hues={Toy.Sky,Toy.Mint,Toy.Sun,Toy.Orange,Toy.Pink,Toy.Red,Toy.SkyLight,Toy.Gold};
            for(int g=0;g<train.Gears;g++) {
                float end=train.redline/train.ratio[g];Line(tex,X(0),Y(0),X(end),Y(train.redline),hues[g%hues.Length],2);
                if(g+1<train.Gears){float drop=end*train.ratio[g+1];Line(tex,X(end),Y(train.redline),X(end),Y(drop),new Color(1,1,1,.5f),1);}
                var label=gearLabels[g];label.gameObject.SetActive(true);var rt=label.rectTransform;rt.anchoredPosition=new Vector2(X(end)-tex.width*.5f,Y(train.redline)+14-tex.height*.5f);
            }
            for(int g=train.Gears;g<gearLabels.Count;g++)gearLabels[g].gameObject.SetActive(false);
            // The car's top speed.
            float top=train.ReachableTop;Line(tex,X(top),bottom,X(top),bottom+h,Toy.Mint,2);
            tex.Apply();
        }
        void DrawPowerChart()
        {
            var tex=powerTex;Fill(tex,new Color(.1f,.14f,.3f));
            const float left=34,bottom=16;float w=tex.width-left-10,h=tex.height-bottom-10;
            float boost=train.turbo?1:0,peakPower=0;var torque=new List<float>();var power=new List<float>();
            for(float r=train.idle;r<=train.redline;r+=50){float tq=train.Tau(r)*(1+(train.turboPower-1)*train.BoostTarget(r));torque.Add(tq);power.Add(tq*r);peakPower=Mathf.Max(peakPower,tq*r);}
            float peakTorque=torque.Max();
            for(int i=1;i<torque.Count;i++) {
                float x0=left+w*(i-1)/(torque.Count-1),x1=left+w*i/(torque.Count-1);
                Line(tex,x0,bottom+h*torque[i-1]/peakTorque*.9f,x1,bottom+h*torque[i]/peakTorque*.9f,Toy.Sky,2);
                Line(tex,x0,bottom+h*power[i-1]/peakPower*.95f,x1,bottom+h*power[i]/peakPower*.95f,Toy.Sun,2);
            }
            Line(tex,left,bottom,left,bottom+h,Color.white,1);Line(tex,left,bottom,left+w,bottom,Color.white,1);
            tex.Apply();
        }

        void Update()
        {
            if(!IsOpen)return;
            var k=Keyboard.current;var p=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true){Close();return;}
            int move=(k?.downArrowKey.wasPressedThisFrame==true||k?.sKey.wasPressedThisFrame==true||p?.dpad.down.wasPressedThisFrame==true?1:0)-(k?.upArrowKey.wasPressedThisFrame==true||k?.wKey.wasPressedThisFrame==true||p?.dpad.up.wasPressedThisFrame==true?1:0);
            if(move!=0){do{focus=(focus+move+rows.Count)%rows.Count;}while(rows[focus].adjust==null&&rows[focus].press==null);Refresh();}
            int adjust=(k?.rightArrowKey.wasPressedThisFrame==true||k?.dKey.wasPressedThisFrame==true||p?.dpad.right.wasPressedThisFrame==true?1:0)-(k?.leftArrowKey.wasPressedThisFrame==true||k?.aKey.wasPressedThisFrame==true||p?.dpad.left.wasPressedThisFrame==true?1:0);
            if(adjust!=0&&rows[focus].adjust!=null)rows[focus].adjust(adjust);
            if((k?.enterKey.wasPressedThisFrame==true||k?.spaceKey.wasPressedThisFrame==true||p?.buttonSouth.wasPressedThisFrame==true)&&rows[focus].press!=null)rows[focus].press();
        }
    }
}
