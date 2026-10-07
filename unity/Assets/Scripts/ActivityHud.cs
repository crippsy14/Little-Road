using System;
using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // One running challenge (doc 64): what the HUD shows and how to restart or leave it. Every timed free-roam activity
    // describes itself with one of these when it starts (AdventureGame.BeginActivity) and clears it when it ends.
    public sealed class Activity
    {
        public string title,goal,giver;public Color colour=Toy.Orange;
        public float go;              // Time.time of GO (the countdown runs before it)
        public float limit;           // > 0: a time limit, shown counting down; otherwise the clock counts up
        public float par;             // > 0: shown beside a counting-up clock
        public Func<float> progress;  // 0..1, or null for no bar
        public float[] marks;         // star thresholds on the bar (0..1), or null
        public string progressLabel="";   // e.g. "harvested"
        public Func<string> detail;   // one extra line, or null
        public Action restart,exit;   // exit: the activity's own clean-up (the caller then returns you to the giver)
        public Func<bool> running;    // false once it's over (the HUD then retires it)
        public Vector3? returnTo;     // where Exit puts you when there's no giver
        public float Elapsed=>Time.time-go;
        public int Stars{get{if(progress==null||marks==null)return 0;float p=progress();int s=0;for(int i=0;i<marks.Length;i++)if(p>=marks[i])s=i+1;return s;}}
    }

    // The challenge HUD (doc 64; research: Forza Horizon's events and PR stunts, and common objective-tracker practice):
    //   1. On start, a big title card at the top centre: the name, the goal and the star marks, over the 3-2-1 countdown.
    //   2. At GO it shrinks into a compact tracker in the top-left corner: the clock (counting down when there's a limit,
    //      red and pulsing in the last 10 s), a progress bar with star ticks, and one detail line. It pulses on each star.
    //   3. With no challenge running, the quest card takes the corner: a new objective is shown big for a moment, then
    //      shrinks into the corner, and the card fades away while you drive (it comes back when you stop).
    public sealed class ActivityHud : MonoBehaviour
    {
        Canvas canvas;RectTransform intro,tracker,quest,questBig;CanvasGroup introFade,trackerFade,questFade,questBigFade;
        Text introTitle,introGoal,introMarks,introTag;ToyShape introCard,introStripe,introTagPill;
        Text trackTitle,trackClock,trackDetail,trackPercent;ToyShape trackCard,trackStripe,barBack,barFill;RectTransform ticks;
        Text questText,questBigText;ToyShape questCard,questBigCard;
        Activity shown;int hurrySecond=-1;float shownAt,lastBeepSecond=99,pulseAt=-9,lastProgress;int lastStars;string questShown;float questChangedAt=-99;
        static readonly Vector2 Corner=new(-700,436);   // the tracker's centre, top-left (1920 x 1080 reference)
        public bool Busy=>shown!=null;public Func<bool> visible;

        void Awake()
        {
            var root=new GameObject("Activity HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform);root.layer=Toy.MenuLayer;
            canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=21;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var rt=(RectTransform)root.transform;
            // The intro card.
            intro=Toy.Rect(rt,"Challenge intro",new Vector2(900,230),new Vector2(0,370));introFade=intro.gameObject.AddComponent<CanvasGroup>();introFade.blocksRaycasts=false;
            introCard=Toy.Card(intro,"Intro card",new Vector2(900,230),Vector2.zero,Toy.White,34,6,10);
            introStripe=Toy.Card(intro,"Intro stripe",new Vector2(900,16),new Vector2(0,107),Toy.Orange,8,0,0);
            introTagPill=Toy.Card(intro,"Intro tag",new Vector2(220,40),new Vector2(0,124),Toy.Orange,20,4,3);
            introTag=Toy.Label(introTagPill.transform,"Tag","CHALLENGE",20,new Vector2(220,40),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            introTitle=Toy.Title(intro,"Title","",64,new Vector2(860,80),new Vector2(0,44),Toy.Orange,TextAnchor.MiddleCenter);
            introGoal=Toy.Label(intro,"Goal","",26,new Vector2(840,60),new Vector2(0,-22),Toy.Navy,TextAnchor.MiddleCenter,true);
            introMarks=Toy.Label(intro,"Marks","",24,new Vector2(840,36),new Vector2(0,-78),Toy.Soft,TextAnchor.MiddleCenter,true);
            intro.gameObject.SetActive(false);
            // The corner tracker.
            tracker=Toy.Rect(rt,"Challenge tracker",new Vector2(480,166),Corner);trackerFade=tracker.gameObject.AddComponent<CanvasGroup>();trackerFade.blocksRaycasts=false;
            trackCard=Toy.Card(tracker,"Tracker card",new Vector2(480,166),Vector2.zero,Toy.White,28,5,7);
            trackStripe=Toy.Card(tracker,"Tracker stripe",new Vector2(12,130),new Vector2(-222,0),Toy.Orange,6,0,0);
            trackTitle=Toy.Title(tracker,"Name","",28,new Vector2(300,40),new Vector2(-50,52),Toy.Orange,TextAnchor.MiddleLeft);
            trackClock=Toy.Title(tracker,"Clock","",48,new Vector2(220,60),new Vector2(118,46),Toy.Navy,TextAnchor.MiddleRight);
            barBack=Toy.Card(tracker,"Bar",new Vector2(400,22),new Vector2(-6,0),new Color(.86f,.88f,.93f),11,0,0);
            barFill=Toy.Card(tracker,"Bar fill",new Vector2(0,22),new Vector2(-206,0),Toy.Orange,11,0,0);barFill.rectTransform.pivot=new Vector2(0,.5f);
            ticks=Toy.Rect(tracker,"Ticks",new Vector2(400,22),new Vector2(-6,0));
            trackPercent=Toy.Label(tracker,"Percent","",20,new Vector2(400,26),new Vector2(-6,-26),Toy.Navy,TextAnchor.MiddleLeft,true);
            trackDetail=Toy.Label(tracker,"Detail","",20,new Vector2(420,26),new Vector2(-6,-56),Toy.Soft,TextAnchor.MiddleLeft,true);
            tracker.gameObject.SetActive(false);
            // The quest card (corner) and its big announcement.
            quest=Toy.Rect(rt,"Quest card",new Vector2(560,64),Corner+new Vector2(40,14));questFade=quest.gameObject.AddComponent<CanvasGroup>();questFade.blocksRaycasts=false;questFade.alpha=0;
            questCard=Toy.Card(quest,"Quest bg",new Vector2(560,64),Vector2.zero,Toy.Sun,28,5,5);
            var bang=Toy.Card(quest,"Quest mark",new Vector2(44,44),new Vector2(-250,0),Toy.Red,22,4,0);Toy.Title(bang.transform,"Bang","!",32,new Vector2(44,44),new Vector2(0,1),Toy.White,TextAnchor.MiddleCenter);
            questText=Toy.Label(quest,"Quest text","",23,new Vector2(480,56),new Vector2(24,0),Toy.Navy,TextAnchor.MiddleLeft,true);
            questBig=Toy.Rect(rt,"Quest announcement",new Vector2(900,120),new Vector2(0,330));questBigFade=questBig.gameObject.AddComponent<CanvasGroup>();questBigFade.blocksRaycasts=false;questBigFade.alpha=0;
            questBigCard=Toy.Card(questBig,"Announcement bg",new Vector2(900,120),Vector2.zero,Toy.Sun,34,6,9);
            var newPill=Toy.Card(questBig,"New tag",new Vector2(240,38),new Vector2(0,62),Toy.Red,19,4,3);Toy.Label(newPill.transform,"Tag","NEW OBJECTIVE",19,new Vector2(240,38),Vector2.zero,Toy.White,TextAnchor.MiddleCenter,true);
            questBigText=Toy.Title(questBig,"Announcement","",36,new Vector2(860,90),new Vector2(0,-4),Toy.Navy,TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- challenges
        public void Begin(Activity a)
        {
            shown=a;shownAt=Time.unscaledTime;lastBeepSecond=99;hurrySecond=-1;lastStars=0;lastProgress=0;pulseAt=-9;
            foreach(var g in new Graphic[]{introStripe,introTagPill,trackStripe,barFill})g.color=a.colour;introTitle.color=trackTitle.color=trackClock.color=a.colour;
            introTitle.text=a.title.ToUpperInvariant();introGoal.text=a.goal;
            introMarks.text=a.marks!=null?string.Join("     ",Array.ConvertAll(a.marks,m=>$"{new string('★',Array.IndexOf(a.marks,m)+1)} {m*100:0}%"))+(a.limit>0?$"     ·     {a.limit:0} seconds":""):a.limit>0?$"{a.limit:0} seconds on the clock":a.par>0?$"Par {a.par:0} s":"";
            trackTitle.text=a.title.ToUpperInvariant();
            bool bar=a.progress!=null;barBack.gameObject.SetActive(bar);barFill.gameObject.SetActive(bar);ticks.gameObject.SetActive(bar);trackPercent.gameObject.SetActive(bar);
            for(int i=ticks.childCount-1;i>=0;i--)Destroy(ticks.GetChild(i).gameObject);
            if(bar&&a.marks!=null)for(int i=0;i<a.marks.Length;i++){float x=-200+400*a.marks[i];Toy.Card(ticks,"Tick",new Vector2(4,30),new Vector2(x,0),Toy.Navy,2,0,0);
                Toy.Label(ticks,"Star",new string('★',i+1),16,new Vector2(80,20),new Vector2(x,24),Toy.Gold,TextAnchor.MiddleCenter,true);}
            bool more=a.detail!=null||a.par>0||a.marks!=null;float h=bar?(more?186:150):(more?126:96);tracker.sizeDelta=trackCard.rectTransform.sizeDelta=new Vector2(480,h);trackStripe.rectTransform.sizeDelta=new Vector2(12,h-36);
            trackTitle.rectTransform.anchoredPosition=new Vector2(-50,h*.5f-31);trackClock.rectTransform.anchoredPosition=new Vector2(118,h*.5f-37);
            trackDetail.rectTransform.anchoredPosition=new Vector2(-6,bar?-56:h*.5f-82);
            intro.gameObject.SetActive(true);tracker.gameObject.SetActive(true);introFade.alpha=0;trackerFade.alpha=0;
        }
        public void End(){shown=null;intro.gameObject.SetActive(false);tracker.gameObject.SetActive(false);}

        void Update()
        {
            canvas.enabled=Time.timeScale>0&&(visible==null||visible());
            QuestFrame();
            if(shown==null)return;var a=shown;float t=a.Elapsed,age=Time.unscaledTime-shownAt;
            // Countdown beeps (ToyCountdown draws the 3 / 2 / 1 / GO).
            if(t<0){int s=Mathf.CeilToInt(-t);if(s!=lastBeepSecond&&s<=3){lastBeepSecond=s;AudioDirector.Beep(880,.14f);}}
            else if(lastBeepSecond>0&&lastBeepSecond<99){lastBeepSecond=0;AudioDirector.Beep(1760,.5f);}
            // 1: the intro card, big, until a beat before GO; 2: it flies to the corner as the tracker fades in.
            float fly=Mathf.Clamp01((t+.6f)/.45f);float e=1-(1-fly)*(1-fly)*(1-fly);
            introFade.alpha=age<.2f?age/.2f:1-e;
            intro.anchoredPosition=Vector2.Lerp(new Vector2(0,370),Corner,e);intro.localScale=Vector3.one*Mathf.Lerp(1,.5f,e);
            if(introFade.alpha<=.01f&&fly>=1)intro.gameObject.SetActive(false);
            trackerFade.alpha=Mathf.Clamp01((fly-.5f)*2);
            // The clock.
            float clock=a.limit>0?Mathf.Max(0,a.limit-Mathf.Max(0,t)):Mathf.Max(0,t);
            trackClock.text=clock>=60?$"{(int)clock/60}:{clock%60:00.0}":$"{clock:0.0}";
            bool hurry=a.limit>0&&t>0&&clock<10;trackClock.color=hurry?Toy.Red:a.colour;
            if(hurry){int s=Mathf.CeilToInt(clock);if(s!=hurrySecond){hurrySecond=s;AudioDirector.Beep(660,.06f);}}
            float pulse=hurry?1+.08f*Mathf.Abs(Mathf.Sin(Time.unscaledTime*Mathf.PI)):1;trackClock.rectTransform.localScale=Vector3.one*pulse;
            // Progress, stars and the detail line.
            if(a.progress!=null) {
                float p=Mathf.Clamp01(a.progress());lastProgress=p;barFill.rectTransform.sizeDelta=new Vector2(Mathf.Max(22,400*p),22);barFill.rectTransform.anchoredPosition=new Vector2(-206,0);
                int stars=a.Stars;trackPercent.text=$"{p*100:0}% {a.progressLabel}"+(stars>0?$"   {new string('★',stars)}":"");
                if(stars>lastStars){lastStars=stars;pulseAt=Time.unscaledTime;AudioDirector.Beep(1320,.18f);}
            }
            trackDetail.text=a.detail!=null?a.detail():a.marks!=null&&a.progress!=null?(a.Stars<a.marks.Length?$"Next star at {a.marks[a.Stars]*100:0}%":"Three stars! Keep going for your best"):a.par>0?$"Par {a.par:0} s":"";
            float punch=Time.unscaledTime-pulseAt<.35f?1+.1f*Mathf.Sin((Time.unscaledTime-pulseAt)/.35f*Mathf.PI):1;tracker.localScale=Vector3.one*punch;
        }

        // ---------------------------------------------------------------- the quest card
        // `text` is the current objective; `show` whether the card should be in view (stopped, or just changed).
        public void Quest(string text,bool show)
        {
            if(text!=questShown){
                bool fresh=!string.IsNullOrEmpty(text)&&!string.IsNullOrEmpty(questShown)&&Head(text)!=Head(questShown);questShown=text;
                questText.text=text??"";float w=Mathf.Clamp(Mathf.Ceil(questText.preferredWidth)+110,300,1100);quest.sizeDelta=questCard.rectTransform.sizeDelta=new Vector2(w,64);
                quest.anchoredPosition=new Vector2(-940+w*.5f,450);questText.rectTransform.sizeDelta=new Vector2(w-80,56);questText.rectTransform.anchoredPosition=new Vector2(28,0);
                quest.Find("Quest mark").GetComponent<RectTransform>().anchoredPosition=new Vector2(-w*.5f+34,0);questCard.SetVerticesDirty();
                if(fresh){questBigText.text=Head(text);questChangedAt=Time.unscaledTime;}
            }
            wantQuest=show&&!string.IsNullOrEmpty(text)&&shown==null;
        }
        bool wantQuest;
        static string Head(string s){if(string.IsNullOrEmpty(s))return "";int i=s.IndexOf("  ·  ",StringComparison.Ordinal);return i>0?s.Substring(0,i):s;}   // (without the distance)
        void QuestFrame()
        {
            float since=Time.unscaledTime-questChangedAt,step=Time.unscaledDeltaTime*5;
            // A new objective: big at the top for 2.4 s, then it shrinks up into the corner card.
            bool announcing=since<2.9f&&shown==null;
            if(announcing){float fly=Mathf.Clamp01((since-2.4f)/.5f),e=1-(1-fly)*(1-fly);
                questBigFade.alpha=since<.25f?since/.25f:1-e;questBig.anchoredPosition=Vector2.Lerp(new Vector2(0,330),quest.anchoredPosition,e);questBig.localScale=Vector3.one*Mathf.Lerp(1,.55f,e);}
            else questBigFade.alpha=0;
            float target=shown!=null?0:announcing?(since>2.6f?1:0):wantQuest||since<9?1:0;
            questFade.alpha=Mathf.MoveTowards(questFade.alpha,target,step);
        }
    }
}
