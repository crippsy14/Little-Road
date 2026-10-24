using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LittleRoad
{
    // Adventure-mode overlay: journey stickers (top), control hint (bottom),
    // conversations as a name tag + speech bubble with candy choices, and the
    // race results screen. All drawn with the Toy kit on the menu camera.
    public sealed class AdventureUI:MonoBehaviour
    {
        public sealed class Choice {public string text;public Action action;public bool enabled=true;public Choice(string text,Action action,bool enabled=true){this.text=text;this.action=action;this.enabled=enabled;}}
        Canvas canvas;RectTransform panel,choiceColumn,topBar,bottomBar;Text heading,place,description,prompt,objective;ToyShape promptPill,objectivePill,placePill;
        readonly List<ToyShape> chips=new();readonly List<Text> chipTexts=new();string shownHud,shownControls;
        // Free-roam HUD (docs/ui-design.md): the race speed dash at bottom centre; the objective and the
        // controls hint fade in only when they're useful.
        RectTransform dash;Text dashSpeed,dashDrive,dashUnit;GearDisplay dashGears;CanvasGroup objectiveFade,controlsFade;bool roaming;
        // Interaction prompt ("act" tier): button glyph + verb + name, role in the character's colour.
        ToyShape interaction,glyph;Text interactionTitle,interactionRole,glyphLetter;GameObject glyphCross;float interactionShown;string shownInteraction;InputGlyphs.Family shownFamily;
        RaceResultScreen result;ToyToaster toaster;
        readonly List<Button> buttons=new();
        static readonly Color[] ChoiceColours={Toy.Blue,Toy.Mint,Toy.Orange,Toy.Pink,Toy.Red,Toy.Sky};
        public bool IsOpen=>(panel!=null&&panel.gameObject.activeSelf)||result!=null;
        public string DisplayedFinishTime=>result?.TimeLabel?.text;
        public RaceResultScreen Result=>result;
        void Awake()
        {
            var root=new GameObject("Adventure interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            if(EventSystem.current==null){var events=new GameObject("Menu input",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            // Reserve the right stick for model inspection. Unity's defaults
            // bind both gamepad sticks to UI movement.
            var move=EventSystem.current.GetComponent<InputSystemUIInputModule>()?.move?.action;
            if(move!=null)for(int i=0;i<move.bindings.Count;i++)
                if(move.bindings[i].path?.StartsWith("<Gamepad>/rightStick")==true)move.ApplyBindingOverride(i,"");

            topBar=Toy.Rect(root.transform,"Journey HUD",new Vector2(1880,150),new Vector2(0,450));
            objectivePill=Toy.Card(topBar,"Objective",new Vector2(900,56),new Vector2(-470,-40),Toy.Sun,28,5,5);
            var bang=Toy.Card(objectivePill.transform,"Objective mark",new Vector2(44,44),new Vector2(-424,0),Toy.Red,22,4,0);
            Toy.Title(bang.transform,"Bang","!",32,new Vector2(44,44),new Vector2(0,1),Toy.White,TextAnchor.MiddleCenter);
            objective=Toy.Label(objectivePill.transform,"Objective text",string.Empty,24,new Vector2(820,56),new Vector2(24,0),Toy.Navy,TextAnchor.MiddleLeft,true);
            bottomBar=Toy.Rect(root.transform,"Journey controls",new Vector2(1880,80),new Vector2(0,-490));
            promptPill=Toy.Card(bottomBar,"Controls pill",new Vector2(1200,58),Vector2.zero,Toy.Navy,29,4,4);promptPill.borderColor=Toy.White;
            prompt=Toy.Label(promptPill.transform,"Controls",string.Empty,24,new Vector2(1160,58),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            // One consistent place for "what can I do here": just above the controls bar.
            // Compact: a keycap, "Talk to Mara" and the role underneath, just above the speed dash.
            interaction=Toy.Card(root.transform,"Interaction prompt",new Vector2(500,72),new Vector2(0,-330),Toy.Navy,36,4,6);interaction.borderColor=Toy.White;
            glyph=Toy.Card(interaction.transform,"Button glyph",new Vector2(50,50),new Vector2(-212,0),Toy.White,12,0,3);
            // Bold label face: the outlined display face smeared into a blob inside the small keycap.
            glyphLetter=Toy.Label(glyph.transform,"Glyph letter","E",28,new Vector2(50,50),new Vector2(0,1),Toy.Navy,TextAnchor.MiddleCenter,true);
            glyphCross=new GameObject("Glyph cross",typeof(RectTransform));glyphCross.transform.SetParent(glyph.transform,false);
            foreach(float a in new[]{45f,-45f}){var bar=Toy.Card(glyphCross.transform,"Cross bar",new Vector2(30,7),Vector2.zero,Toy.Hex(0x7fb3ff),5,0,0);bar.rectTransform.localRotation=Quaternion.Euler(0,0,a);}
            interactionTitle=Toy.Title(interaction.transform,"Interaction title",string.Empty,26,new Vector2(400,36),new Vector2(36,11),Toy.White,TextAnchor.MiddleLeft);
            interactionRole=Toy.Label(interaction.transform,"Interaction role",string.Empty,17,new Vector2(400,24),new Vector2(36,-17),Toy.Sun,TextAnchor.MiddleLeft,true);
            interaction.gameObject.SetActive(false);
            objectiveFade=objectivePill.gameObject.AddComponent<CanvasGroup>();controlsFade=promptPill.gameObject.AddComponent<CanvasGroup>();
            dash=CoastUI.Panel(root.transform,"Speed dash",new Vector2(250,105),new Vector2(0,-434),new Color(0,0,0,.14f),1);
            dashSpeed=DashText(dash,"Speed","0",70,new Vector2(150,87),new Vector2(-40,6),TextAnchor.MiddleRight);
            dashUnit=DashText(dash,"Speed unit","mph",15,new Vector2(150,25),new Vector2(-42,-39),TextAnchor.MiddleRight);
            CoastUI.Panel(dash,"Separator",new Vector2(1,66),new Vector2(52,0),new Color(1,1,1,.6f),0);
            dashDrive=DashText(dash,"Drive selector","D",54,new Vector2(75,70),new Vector2(90,7),TextAnchor.MiddleCenter);
            var dashMode=DashText(dash,"Transmission","AUTO",13,new Vector2(76,23),new Vector2(90,-38),TextAnchor.MiddleCenter);
            dashGears=new GearDisplay(dashDrive,dashMode,dash,220,46);
            dash.gameObject.SetActive(false);

            toaster=Toy.Rect(root.transform,"Toasts",new Vector2(1920,1080),Vector2.zero).gameObject.AddComponent<ToyToaster>();
            panel=Toy.Rect(root.transform,"Conversation",new Vector2(1920,1080),Vector2.zero);
            var bubble=Toy.Card(panel,"Speech bubble",new Vector2(1060,330),new Vector2(-380,-300),Toy.White,44,7,11);
            var tail=Toy.Card(bubble.transform,"Bubble tail",new Vector2(56,56),new Vector2(-380,168),Toy.White,6,7,0);tail.rectTransform.localRotation=Quaternion.Euler(0,0,45);tail.transform.SetAsFirstSibling();
            var tag=Toy.Card(panel,"Name tag",new Vector2(560,86),new Vector2(-640,-96),Toy.Sun,24,6,7);tag.rectTransform.localRotation=Quaternion.Euler(0,0,-3);
            heading=Toy.Title(tag.transform,"Heading",string.Empty,46,new Vector2(520,80),new Vector2(6,2),Toy.White,TextAnchor.MiddleCenter);
            placePill=Toy.Pill(panel,"Place",string.Empty,22,new Vector2(320,46),new Vector2(-175,-80),Toy.Navy,Toy.White,-3);place=placePill.GetComponentInChildren<Text>();
            description=Toy.Label(bubble.transform,"Description",string.Empty,31,new Vector2(960,250),new Vector2(0,-6),Toy.Navy,TextAnchor.UpperLeft,false);
            description.lineSpacing=1.1f;
            choiceColumn=Toy.Rect(panel,"Choices",new Vector2(600,700),new Vector2(590,-40));
            panel.gameObject.SetActive(false);
        }
        static Text DashText(Transform parent,string name,string value,int size,Vector2 box,Vector2 pos,TextAnchor align)
        {
            var text=CoastUI.Label(parent,name,value,size,box,pos,Color.white,false,align);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.88f);shadow.effectDistance=new Vector2(1.5f,-1.5f);return text;
        }
        // Free roam: speed dash, the objective pill top left and the controls hint bottom left, each shown
        // only when asked (they fade). Conversations hide the dash and always show the menu controls.
        public void Roam(string goal,bool showGoal,string controls,bool showControls,RoadCar car)
        {
            if(!roaming){roaming=true;foreach(var c in chips)c.gameObject.SetActive(false);shownHud=null;shownControls=null;objectiveFade.alpha=0;controlsFade.alpha=0;}
            dash.gameObject.SetActive(!IsOpen&&topBar.gameObject.activeSelf);dashSpeed.text=Mathf.RoundToInt(Units.FromMps(Mathf.Abs(car.speed))).ToString();dashUnit.text=Units.Label;dashGears.Show(car);
            if(goal!=shownHud) {
                shownHud=goal;objective.text=goal??string.Empty;
                float w=Mathf.Clamp(Mathf.Ceil(objective.preferredWidth)+110,300,1300);objectivePill.rectTransform.sizeDelta=new Vector2(w,56);objectivePill.rectTransform.anchoredPosition=new Vector2(-920+w*.5f,20);
                objective.rectTransform.sizeDelta=new Vector2(w-80,56);objective.rectTransform.anchoredPosition=new Vector2(28,0);objectivePill.transform.Find("Objective mark").GetComponent<RectTransform>().anchoredPosition=new Vector2(-w*.5f+34,0);objectivePill.SetVerticesDirty();
            }
            if(IsOpen){controls=InputGlyphs.Menu;showControls=true;showGoal=false;}
            if(controls!=shownControls) {
                // One line: shrink the type (20 down to 14 px) rather than wrap.
                shownControls=controls;prompt.text=controls;prompt.horizontalOverflow=HorizontalWrapMode.Overflow;prompt.fontSize=20;float room=IsOpen?1150:700;
                if(prompt.preferredWidth>room)prompt.fontSize=Mathf.Max(14,Mathf.FloorToInt(20*room/prompt.preferredWidth));
                float w=Mathf.Ceil(prompt.preferredWidth)+50;promptPill.rectTransform.sizeDelta=new Vector2(w,50);prompt.rectTransform.sizeDelta=new Vector2(w-30,50);
                promptPill.rectTransform.anchoredPosition=IsOpen?Vector2.zero:new Vector2(-940+w*.5f,0);promptPill.SetVerticesDirty();
            }
            objectivePill.gameObject.SetActive(!string.IsNullOrEmpty(goal));promptPill.gameObject.SetActive(!string.IsNullOrEmpty(controls));
            float step=Time.unscaledDeltaTime*5;
            objectiveFade.alpha=Mathf.MoveTowards(objectiveFade.alpha,showGoal?1:0,step);controlsFade.alpha=Mathf.MoveTowards(controlsFade.alpha,showControls?1:0,step);
        }
        public void HUD(string value,string controls)
        {
            if(roaming){roaming=false;dash.gameObject.SetActive(false);objectiveFade.alpha=1;controlsFade.alpha=1;promptPill.rectTransform.anchoredPosition=Vector2.zero;prompt.fontSize=24;shownHud=null;}
            if(value==shownHud&&controls==shownControls)return;shownHud=value;shownControls=controls;
            var lines=(value??string.Empty).Split('\n');var parts=lines[0].Split('·');
            while(chips.Count<parts.Length){
                int i=chips.Count;var chip=Toy.Card(topBar,"Chip "+i,new Vector2(200,56),Vector2.zero,i==0?Toy.Red:Toy.White,28,5,5);
                chipTexts.Add(Toy.Label(chip.transform,"Chip text",string.Empty,i==0?26:24,new Vector2(400,56),Vector2.zero,i==0?Toy.White:Toy.Navy,TextAnchor.MiddleCenter,true));chips.Add(chip);
            }
            float x=-920;
            for(int i=0;i<chips.Count;i++){
                bool used=i<parts.Length&&parts[i].Trim().Length>0;chips[i].gameObject.SetActive(used);if(!used)continue;
                var text=chipTexts[i];text.text=parts[i].Trim().ToUpperInvariant();float width=Mathf.Ceil(text.preferredWidth)+46;
                chips[i].rectTransform.sizeDelta=new Vector2(width,56);text.rectTransform.sizeDelta=new Vector2(width,56);chips[i].rectTransform.anchoredPosition=new Vector2(x+width*.5f,30);chips[i].SetVerticesDirty();x+=width+14;
            }
            string goal=lines.Length>1?lines[1].Trim():string.Empty;objectivePill.gameObject.SetActive(goal.Length>0);objective.text=goal;
            float goalWidth=Mathf.Clamp(Mathf.Ceil(objective.preferredWidth)+110,300,1400);objectivePill.rectTransform.sizeDelta=new Vector2(goalWidth,56);objectivePill.rectTransform.anchoredPosition=new Vector2(-920+goalWidth*.5f,-40);
            objective.rectTransform.sizeDelta=new Vector2(goalWidth-80,56);objective.rectTransform.anchoredPosition=new Vector2(28,0);objectivePill.transform.Find("Objective mark").GetComponent<RectTransform>().anchoredPosition=new Vector2(-goalWidth*.5f+34,0);objectivePill.SetVerticesDirty();
            prompt.text=controls;float promptWidth=Mathf.Clamp(Mathf.Ceil(prompt.preferredWidth)+60,300,1860);promptPill.rectTransform.sizeDelta=new Vector2(promptWidth,58);prompt.rectTransform.sizeDelta=new Vector2(promptWidth-30,58);promptPill.SetVerticesDirty();
            promptPill.gameObject.SetActive(!string.IsNullOrEmpty(controls));
        }
        // "Bayview postcard found · +50 credits" -> headline "BAYVIEW POSTCARD FOUND!" + detail pill.
        public void Toast(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return;int split=value.IndexOf('·');
            string headline=(split<0?value:value[..split]).Trim().TrimEnd('.').ToUpperInvariant(),detail=split<0?string.Empty:value[(split+1)..].Trim();
            if(!headline.EndsWith("!")&&!headline.EndsWith("?"))headline+="!";
            toaster.Show(headline,detail,value.Contains("postcard")?Toy.Pink:value.Contains("credit")?Toy.Gold:Toy.Sky);
        }
        // First visit to a place: the toast card with a DISCOVERED tag.
        public void Discovery(string place,string subtitle)=>toaster.Show(place,subtitle,Toy.Sky,"DISCOVERED");
        public void Card(string headline,string detail,Color colour,string tag)=>toaster.Show(headline,detail,colour,tag);
        public void SetVisible(bool value){canvas.enabled=value;}
        public bool Visible=>canvas!=null&&canvas.enabled;
        public void SetHudVisible(bool value){topBar.gameObject.SetActive(value);bottomBar.gameObject.SetActive(value);if(!value)dash.gameObject.SetActive(false);}
        void LateUpdate()
        {
            Toy.UseMenuCamera(canvas);
        }
        // Show the interaction prompt (null title hides it). `ready` false greys the button and the
        // title explains why (e.g. "Slow down to talk to Mara").
        public void Interaction(string title,string role,Color roleColour,bool ready)
        {
            var family=InputGlyphs.Current;string key=title==null||IsOpen?null:title+"|"+role+"|"+ready;
            if(key!=shownInteraction||family!=shownFamily) {
                bool appearing=shownInteraction==null&&key!=null;shownInteraction=key;shownFamily=family;
                interaction.gameObject.SetActive(key!=null);
                if(key!=null) {
                    interactionTitle.text=title;interactionRole.text=role??string.Empty;interactionRole.color=roleColour;
                    bool ps=family==InputGlyphs.Family.PlayStation,xbox=family==InputGlyphs.Family.Xbox;
                    glyph.color=ps?Toy.Navy:xbox?Toy.Hex(0x3fb24a):Toy.White;glyph.borderColor=ps?Toy.White:glyph.color;
                    glyph.rectTransform.sizeDelta=new Vector2(50,50);glyph.radius=ps||xbox?25:12;
                    glyphLetter.text=InputGlyphs.Confirm;glyphLetter.color=xbox?Toy.White:Toy.Navy;glyphCross.SetActive(ps);
                    var fade=glyph.GetComponent<CanvasGroup>();if(fade==null)fade=glyph.gameObject.AddComponent<CanvasGroup>();fade.alpha=ready?1:.35f;
                    glyph.SetVerticesDirty();if(appearing)interactionShown=Time.unscaledTime;
                }
            }
            if(interaction.gameObject.activeSelf){float t=Mathf.Clamp01((Time.unscaledTime-interactionShown)/.16f);interaction.rectTransform.localScale=Vector3.one*Mathf.Lerp(.86f,1,1-(1-t)*(1-t));}
        }
        public void Capture(string path)
        {
            Toy.UseMenuCamera(canvas);result?.RefreshNow();Toy.RefreshButtons(canvas.transform);Toy.Capture(path);
        }
        public void Show(string title,string body,List<Choice> choices)
        {
            ClearChoices();CloseResult();
            panel.gameObject.SetActive(true);
            int split=title.IndexOf('/');heading.text=(split<0?title:title[..split]).Trim();place.text=split<0?string.Empty:title[(split+1)..].Trim().ToUpperInvariant();placePill.gameObject.SetActive(place.text.Length>0);
            description.text=body;
            float height=Mathf.Min(92,660f/Mathf.Max(1,choices.Count)),top=330-height*.5f;
            for(int i=0;i<choices.Count;i++) {
                var choice=choices[i];
                var button=Toy.Candy(choiceColumn,"Choice "+choice.text,choice.text,null,new Vector2(580,height-16),new Vector2(0,top-i*height),ChoiceColours[i%ChoiceColours.Length],choice.action);
                var caption=button.GetComponentInChildren<Text>();caption.fontSize=Mathf.RoundToInt(Mathf.Min(34,(height-16)*.42f));caption.horizontalOverflow=HorizontalWrapMode.Wrap;caption.resizeTextForBestFit=true;caption.resizeTextMinSize=18;caption.resizeTextMaxSize=caption.fontSize;
                caption.rectTransform.sizeDelta=new Vector2(540,height-16);caption.rectTransform.anchoredPosition=new Vector2(0,2);
                button.interactable=choice.enabled;buttons.Add(button);
            }
            FocusChoices();
        }
        void ClearChoices()
        {
            StopAllCoroutines();foreach(var button in buttons){button.gameObject.SetActive(false);Destroy(button.gameObject);}buttons.Clear();EventSystem.current.SetSelectedGameObject(null);
        }
        void FocusChoices()
        {
            var active=buttons.FindAll(b=>b.interactable);
            for(int i=0;i<active.Count;i++)active[i].navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=active[(i+active.Count-1)%active.Count],selectOnDown=active[(i+1)%active.Count]};
            if(active.Count>0)StartCoroutine(SelectNextFrame(active[0]));
        }
        public static string FinishTimeLabel(float seconds)=>$"{(int)seconds/60:0}:{seconds%60:00.000}";
        public void ShowRaceResult(RaceResultSummary summary,Action returnToTown,Action retry)
        {
            ClearChoices();panel.gameObject.SetActive(false);SetHudVisible(false);CloseResult();
            result=gameObject.AddComponent<RaceResultScreen>();result.Build(canvas.transform,summary,returnToTown,retry);
            StartCoroutine(SelectNextFrame(result.Choices[2]));
        }
        void CloseResult(){if(result!=null){result.Close();result=null;}}
        IEnumerator SelectNextFrame(Button button){yield return null;if(button!=null&&IsOpen)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        public void Close(){StopAllCoroutines();panel.gameObject.SetActive(false);CloseResult();EventSystem.current.SetSelectedGameObject(null);}
        // B / Esc: the menu's own Back choice ("Back", "Back to all parts", "Back to the road"); false if none.
        public bool Back(){var b=buttons.Find(x=>x.interactable&&x.name.StartsWith("Choice Back"));if(b==null)return false;b.onClick.Invoke();return true;}
        public bool InvokeChoice(string startsWith)
        {
            var button=buttons.Find(b=>b.name.StartsWith("Choice "+startsWith)&&b.interactable)??result?.Choices.Find(b=>b.name.StartsWith("Choice "+startsWith)&&b.interactable);if(button==null)return false;button.onClick.Invoke();return true;
        }
    }
}
