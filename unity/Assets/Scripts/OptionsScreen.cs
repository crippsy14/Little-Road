using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LittleRoad
{
    // The Options screen (Richard, 2 Oct: "a designated options screen with separation between graphics, audio etc"):
    // one place for every setting, in tabs (GRAPHICS, DISPLAY, AUDIO, DRIVING, INTERFACE), opened from the pause hub,
    // the race pause menu and the title screen. Each row shows its value as "‹ value ›": left/right (or click, or
    // confirm) steps it and the change applies at once (GraphicsOptions.Step). L1/R1 or Q/E change tab; B/Esc closes.
    // It doesn't touch the time scale: whoever opens it has already paused (or is the title screen).
    public sealed class OptionsScreen:MonoBehaviour
    {
        public sealed class Row
        {
            public string label,help;public Func<string> value;public Action<int> step;public Action activate;
            public Row(string label,Func<string> value,Action<int> step,string help){this.label=label;this.value=value;this.step=step;this.help=help;}
            public static Row For(GraphicsOptions.Option o,string help)=>new(o.label,()=>o.Value,d=>GraphicsOptions.Step(o,d),help);
        }
        public static readonly string[] Tabs={"GRAPHICS","DISPLAY","AUDIO","DRIVING","CONTROLS","INTERFACE"};
        static OptionsScreen instance;
        public static bool IsOpen=>instance!=null&&instance.canvas!=null&&instance.canvas.gameObject.activeSelf;
        public static int LastCloseFrame{get;private set;}=-1;
        // Open over whatever is showing; onClose runs when it shuts (B/Esc or the Back row). engine: the fitted engine
        // for the engine sound test (null hides it).
        public static OptionsScreen Open(Action onClose,string engine=null,int tab=0)
        {
            if(instance==null)instance=new GameObject("Options screen").AddComponent<OptionsScreen>();
            instance.Show(onClose,engine,tab);return instance;
        }
        public static OptionsScreen Instance=>instance;
        public List<Row> RowsForTab(int t)=>RowsFor(t);   // (checks)

        Canvas canvas;RectTransform body;Text help,footer;readonly List<(ToyShape pill,Text text)> tabPills=new();
        readonly List<(Button button,Text value,Row row)> rows=new();
        Action closed;string engine;int tab;float openedAt;
        public int Tab=>tab;
        public IReadOnlyList<string> RowLabels=>rows.Select(r=>r.row.label).ToList();

        void Build()
        {
            if(EventSystem.current==null){var es=new GameObject("Options input",typeof(EventSystem),typeof(InputSystemUIInputModule));es.transform.SetParent(transform,false);es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            canvas=CoastUI.Canvas(transform,"Options",70);canvas.GetComponent<CanvasScaler>().matchWidthOrHeight=1;canvas.gameObject.AddComponent<GraphicRaycaster>();
            var root=(RectTransform)canvas.transform;
            var dim=Toy.Rect(root,"Dim",new Vector2(4000,2000),Vector2.zero).gameObject.AddComponent<Image>();dim.color=new Color(.05f,.08f,.2f,.82f);
            Toy.Card(root,"Options card",new Vector2(1500,940),new Vector2(0,-6),Toy.Cream,44,7,12);
            Toy.Title(root,"Options title","OPTIONS",64,new Vector2(600,80),new Vector2(-480,385),Toy.Sun,TextAnchor.MiddleLeft);
            var header=Toy.Rect(root,"Tabs",new Vector2(1400,70),new Vector2(0,300));float x=-640;
            for(int i=0;i<Tabs.Length;i++) {
                var pill=Toy.Card(header,"Tab "+Tabs[i],new Vector2(206,62),new Vector2(x+103,0),Toy.Navy,31,5,5);
                var t=Toy.Label(pill.transform,"Tab text",Tabs[i],22,new Vector2(206,62),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
                int index=i;var b=pill.gameObject.AddComponent<Button>();b.transition=Selectable.Transition.None;b.onClick.AddListener(()=>ShowTab(index));b.navigation=new Navigation{mode=Navigation.Mode.None};
                pill.raycastTarget=true;tabPills.Add((pill,t));x+=216;
            }
            body=Toy.Rect(root,"Rows",new Vector2(1400,560),new Vector2(0,-10));
            var helpCard=Toy.Card(root,"Help card",new Vector2(1340,70),new Vector2(0,-330),Toy.White,30,5,5);
            help=Toy.Label(helpCard.transform,"Help","",23,new Vector2(1280,60),Vector2.zero,Toy.Navy,TextAnchor.MiddleCenter,false);
            footer=Toy.Label(root,"Footer","",22,new Vector2(1400,34),new Vector2(0,-412),Toy.Navy,TextAnchor.MiddleCenter,true);
        }
        void Show(Action onClose,string fittedEngine,int startTab)
        {
            if(canvas==null)Build();
            closed=onClose;engine=fittedEngine;openedAt=Time.unscaledTime;
            Toy.UseMenuCamera(canvas);canvas.gameObject.SetActive(true);ShowTab(startTab);
        }
        public void Close()
        {
            if(!IsOpen)return;canvas.gameObject.SetActive(false);LastCloseFrame=Time.frameCount;EventSystem.current?.SetSelectedGameObject(null);
            PlayerPrefs.Save();var c=closed;closed=null;c?.Invoke();
        }
        List<Row> RowsFor(int t)=>t switch{
            0=>new(){
                new("Quality preset",()=>GraphicsOptions.PresetName,d=>{GraphicsOptions.NextPreset();foreach(var o in new[]{GraphicsOptions.AntiAliasing,GraphicsOptions.Shadows,GraphicsOptions.Grass})GraphicsOptions.Step(o,0);},
                    "Low suits older laptops; Ultra is the full look. Sets anti-aliasing, shadows and grass together."),
                Row.For(GraphicsOptions.AntiAliasing,"TAA is smoothest; SMAA is sharper and cheaper; Off is fastest."),
                Row.For(GraphicsOptions.Shadows,"Medium draws shadows closer in and at lower resolution."),
                Row.For(GraphicsOptions.Grass,"The grass blades near the road. Low halves them; Off removes them."),
                Row.For(GraphicsOptions.MotionBlur,"The streak of the world going past in races (your own car stays sharp)."),
                Row.For(GraphicsOptions.Time,"Cycle follows the clock through day and night; the others hold one time of day.")},
            1=>new(){
                new("Display mode",()=>GraphicsOptions.ModeName,d=>{GraphicsOptions.NextMode();StartCoroutine(Relabel());},"Fullscreen, borderless window or a window."),
                new("Resolution",()=>GraphicsOptions.ResolutionName,d=>{GraphicsOptions.NextResolution();StartCoroutine(Relabel());},"The sizes your screen offers."),
                Row.For(GraphicsOptions.FrameRate,"VSync matches your screen; 60 / 120 cap the rate; Unlimited runs as fast as it can."),
                Row.For(GraphicsOptions.FieldOfView,"Wide shows more of the road around the car."),
                Row.For(GraphicsOptions.SpeedEffects,"How much the view widens as you speed up. Reduced or Off if it's uncomfortable."),
                Row.For(GraphicsOptions.Shake,"The camera's jolt when you hit something.")},
            2=>AudioRows(),
            4=>ControlRows(),
            3=>new(){
                Row.For(DrivingOptions.Gears,"Manual: shift up and down yourself (Cross / Square, or remap them under CONTROLS)."),
                Row.For(Units.Speed,"Speeds on the dash, speed traps and the parts shop."),
                Row.For(DrivingOptions.Vibration,"Controller rumble on knocks and off the tarmac.")},
            _=>new(){
                Row.For(GraphicsOptions.Minimap,"The round map in the corner while driving (the pause map is always there)."),
                Row.For(GraphicsOptions.Prompts,"Automatic follows whatever you last touched; or always show one kind of button.")}};
        // ---- CONTROLS: each driving action's button and key. Confirm a row, then press the new button or key.
        Controls.Act? listening;int listenFrom;
        public bool Listening=>listening.HasValue;
        List<Row> ControlRows()
        {
            var list=new List<Row>();
            foreach(var b in Controls.All) {
                var act=b.act;
                list.Add(new Row(b.label,()=>Listening&&listening==act?"press a button or key…":
                    $"{Controls.PadName(b.Pad,InputGlyphs.Current==InputGlyphs.Family.PlayStation?InputGlyphs.Family.PlayStation:InputGlyphs.Family.Xbox)}   ·   {Controls.KeyName(b.Key)}",null,
                    b.help+"  Confirm, then press the new button or key (Backspace: back to default, Esc: cancel)."){activate=()=>Listen(act)});
            }
            list.Add(new Row("Reset all controls",()=>"Defaults",null,"Every driving control back to its default."){activate=()=>{Controls.ResetAll();Refresh();}});
            return list;
        }
        void Listen(Controls.Act act){listening=act;listenFrom=Time.frameCount+1;Refresh();help.text="Press the new button or key for "+Controls.Of(act).label+"   ·   Backspace: default   ·   Esc: cancel";}
        // While listening: the first pad button (of the remappable set) or key pressed becomes the binding.
        bool ListenUpdate()
        {
            if(!listening.HasValue)return false;if(Time.frameCount<listenFrom)return true;
            var act=listening.Value;var k=Keyboard.current;
            if(k?.escapeKey.wasPressedThisFrame==true||Gamepad.all.Any(g=>g.startButton.wasPressedThisFrame)){listening=null;Refresh();return true;}
            if(k?.backspaceKey.wasPressedThisFrame==true){var b=Controls.Of(act);Controls.SetPad(act,b.padDefault);Controls.SetKey(act,b.keyDefault);listening=null;Refresh();return true;}
            foreach(var g in Gamepad.all)foreach(var path in Controls.PadButtons){var c=g.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>(path);if(c!=null&&c.wasPressedThisFrame){Controls.SetPad(act,path);listening=null;Refresh();return true;}}
            if(k!=null)foreach(var key in k.allKeys){if(key==null||!key.wasPressedThisFrame||key==k.escapeKey||key==k.backspaceKey||key==k.enterKey||key==k.numpadEnterKey)continue;Controls.SetKey(act,key.name);listening=null;Refresh();return true;}
            return true;
        }
        public void Rebind(Controls.Act act,string pad,string key){if(pad!=null)Controls.SetPad(act,pad);if(key!=null)Controls.SetKey(act,key);Refresh();}   // (checks, reviews)
        List<Row> AudioRows()
        {
            var list=new List<Row>{
                Row.For(AudioOptions.Master,"Everything at once."),
                Row.For(AudioOptions.Effects,"Engines, tyres, the horn and knocks."),
                Row.For(AudioOptions.Music,"The music in towns and races."),
                Row.For(AudioOptions.Ambience,"Sea, wind, the falls and the river, birds.")};
            if(engine!=null&&!RaceTrial.Racing)list.Add(new Row("Engine sound test",()=>"Open",d=>{Close();EngineBench.Show(engine);},"Rev your fitted engine on the bench, through the gears."));
            return list;
        }
        public void ShowTab(int t)
        {
            listening=null;
            tab=(t+Tabs.Length)%Tabs.Length;
            for(int k=0;k<tabPills.Count;k++){bool on=k==tab;tabPills[k].pill.color=on?Toy.Sun:Toy.Navy;tabPills[k].text.color=on?Toy.Navy:Toy.White;tabPills[k].pill.rectTransform.localScale=Vector3.one*(on?1.06f:1);}
            foreach(var r in rows)Destroy(r.button.gameObject);rows.Clear();
            var list=RowsFor(tab);float h=Mathf.Min(86,520f/Mathf.Max(1,list.Count+1)),top=212;
            for(int i=0;i<list.Count;i++) {
                var row=list[i];int index=i;
                var b=Toy.Candy(body,"Option "+row.label,"",null,new Vector2(1240,h-12),new Vector2(0,top-i*h),i%2==0?Toy.Blue:Toy.Sky,()=>Activate(index));
                var caption=b.GetComponentInChildren<Text>();caption.text=row.label;caption.alignment=TextAnchor.MiddleLeft;caption.fontSize=28;caption.rectTransform.sizeDelta=new Vector2(560,h-12);caption.rectTransform.anchoredPosition=new Vector2(-300,2);
                var value=Toy.Label(b.transform,"Value","",28,new Vector2(520,h-12),new Vector2(330,2),Toy.White,TextAnchor.MiddleCenter,true);
                b.gameObject.AddComponent<OptionRowFocus>().focused=()=>help.text=row.help;
                rows.Add((b,value,row));
            }
            var back=Toy.Candy(body,"Option Back","Back",null,new Vector2(360,h-14),new Vector2(0,top-list.Count*h-8),Toy.Red,Close);
            rows.Add((back,null,null));back.gameObject.AddComponent<OptionRowFocus>().focused=()=>help.text="Back to where you were. Settings are saved as you change them.";
            for(int i=0;i<rows.Count;i++)rows[i].button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=rows[(i+rows.Count-1)%rows.Count].button,selectOnDown=rows[(i+1)%rows.Count].button};
            Refresh();StartCoroutine(Select(rows[0].button));
            footer.text=$"{InputGlyphs.Name("tabs")}: tabs     {InputGlyphs.Name("choose")}: choose and change     {InputGlyphs.Name("back")}: back";
        }
        void Refresh(){foreach(var (_,value,row) in rows)if(row!=null)value.text="‹  "+row.value()+"  ›";}
        IEnumerator Select(Button b){yield return null;if(b!=null&&IsOpen)EventSystem.current?.SetSelectedGameObject(b.gameObject);}
        IEnumerator Relabel(){yield return null;yield return null;if(IsOpen)Refresh();}
        public void Step(int index,int direction)
        {
            if(index<0||index>=rows.Count||rows[index].row==null)return;var row=rows[index].row;
            if(row.step!=null)row.step(direction);else if(direction==1&&row.activate!=null)row.activate();
            if(IsOpen)Refresh();
        }
        void Activate(int index){if(index<0||index>=rows.Count||rows[index].row==null)return;var row=rows[index].row;if(row.activate!=null)row.activate();else row.step?.Invoke(1);if(IsOpen)Refresh();}
        // Reviews and checks: step the row with this label.
        public bool StepRow(string label,int direction){int i=rows.FindIndex(r=>r.row!=null&&r.row.label==label);if(i<0)return false;Step(i,direction);return true;}
        public string ValueOf(string label)=>rows.Find(r=>r.row!=null&&r.row.label==label).row?.value();
        public void Capture(string path){Toy.UseMenuCamera(canvas);Toy.RefreshButtons(canvas.transform);Toy.Capture(path);}
        int Selected(){var go=EventSystem.current?.currentSelectedGameObject;return rows.FindIndex(r=>r.button!=null&&r.button.gameObject==go);}
        void Update()
        {
            if(!IsOpen||Time.unscaledTime-openedAt<.1f)return;
            if(ListenUpdate())return;
            var k=Keyboard.current;bool Pad(Func<Gamepad,UnityEngine.InputSystem.Controls.ButtonControl> c)=>Gamepad.all.Any(g=>c(g).wasPressedThisFrame);
            if(k?.escapeKey.wasPressedThisFrame==true||Pad(g=>g.buttonEast)||Pad(g=>g.startButton)){Close();return;}
            if(k?.qKey.wasPressedThisFrame==true||Pad(g=>g.leftShoulder)){ShowTab(tab-1);return;}
            if(k?.eKey.wasPressedThisFrame==true||Pad(g=>g.rightShoulder)){ShowTab(tab+1);return;}
            int dir=(k?.rightArrowKey.wasPressedThisFrame==true||k?.dKey.wasPressedThisFrame==true||Pad(g=>g.dpad.right)?1:0)-(k?.leftArrowKey.wasPressedThisFrame==true||k?.aKey.wasPressedThisFrame==true||Pad(g=>g.dpad.left)?1:0);
            int sel=Selected();if(dir!=0&&sel>=0&&rows[sel].row?.step!=null)Step(sel,dir);
        }
        void OnDestroy(){if(instance==this)instance=null;}
    }
    // Shows a row's help line while it is selected.
    public sealed class OptionRowFocus:MonoBehaviour,ISelectHandler,IPointerEnterHandler
    {
        public Action focused;
        public void OnSelect(BaseEventData data)=>focused?.Invoke();
        public void OnPointerEnter(PointerEventData data){focused?.Invoke();EventSystem.current?.SetSelectedGameObject(gameObject);}
    }
}
