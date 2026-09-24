using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace LittleRoad
{
    [DefaultExecutionOrder(900)]
    public sealed class RaceHud:MonoBehaviour
    {
        RaceTrial race;RaceTiming timing;Canvas canvas;RectTransform board;Text speed,gear,lap,clock,position,notice,compactHint;
        Text[] names=new Text[11],places=new Text[11],gaps=new Text[11];CoastPanel[] strips=new CoastPanel[11];GameObject[] rows=new GameObject[11];
        RectTransform gasFill,brakeFill;CoastArc speedArc;float nextRefresh;bool compact;
        readonly List<Vector3> boardPositions=new();
        public RaceTiming Timing=>timing;
        public static bool Requested=>RaceTrial.Has("--race-hud")||RaceTrial.Has("--race-hud-review");
        public static string DriveIndicator(float speed,float pedal)=>speed<-.15f||(speed<=.1f&&pedal<0)?"R":"D";
        public void Setup(RaceTrial owner)
        {
            race=owner;var slots=new int[race.cars.Count];for(int i=0;i<slots.Length;i++)slots[i]=race.Roster.Entries[i].Slot;
            timing=new RaceTiming(race.track.length,slots);Build();Sample();Refresh();
            if(RaceTrial.Has("--race-hud-review"))StartCoroutine(Review());
        }
        static void Anchor(RectTransform rect,float x,float y,Vector2 pos){rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.anchoredPosition=pos;}
        void Build()
        {
            canvas=CoastUI.Canvas(transform,"Coast race instruments",25);
            var eventCard=CoastUI.Panel(canvas.transform,"Event postcard",new Vector2(418,94),Vector2.zero,CoastUI.Paper,22,true);Anchor(eventCard,0,1,new Vector2(244,-75));
            CoastUI.Panel(eventCard,"Postcard stripe",new Vector2(7,52),new Vector2(-186,0),CoastUI.Orange,3);
            CoastUI.Label(eventCard,"Series","LITTLE ROAD  /  COAST CLUB",14,new Vector2(350,22),new Vector2(8,24),CoastUI.Muted,true);
            CoastUI.Label(eventCard,"Event",race.track==null?"Coast run":race.Roster.Track=="bayview"?"Bayview coast":"Cedar circuit",31,new Vector2(350,45),new Vector2(8,-10),CoastUI.Ink,true);
            var lapCard=CoastUI.Panel(canvas.transform,"Lap ticket",new Vector2(240,75),Vector2.zero,CoastUI.Ink,22,true);Anchor(lapCard,.5f,1,new Vector2(0,-64));
            lap=CoastUI.Label(lapCard,"Lap","",28,new Vector2(218,54),Vector2.zero,CoastUI.Paper,true,TextAnchor.MiddleCenter);
            var timeCard=CoastUI.Panel(canvas.transform,"Race clock",new Vector2(274,83),Vector2.zero,CoastUI.Paper,22,true);Anchor(timeCard,1,1,new Vector2(-174,-69));
            CoastUI.Label(timeCard,"Clock caption","RACE TIME",13,new Vector2(235,22),new Vector2(0,21),CoastUI.Muted,true,TextAnchor.MiddleCenter);
            clock=CoastUI.Label(timeCard,"Clock","",32,new Vector2(240,43),new Vector2(0,-10),CoastUI.Ink,true,TextAnchor.MiddleCenter);
            board=CoastUI.Panel(canvas.transform,"Race order notebook",new Vector2(350,500),Vector2.zero,new Color(.98f,.94f,.81f,.97f),23,true);Anchor(board,0,1,new Vector2(210,-404));
            CoastUI.Label(board,"Order caption","ON THE ROAD",18,new Vector2(210,34),new Vector2(-43,215),CoastUI.Ink,true);
            position=CoastUI.Label(board,"Position","",24,new Vector2(95,34),new Vector2(110,215),CoastUI.Sea,true,TextAnchor.MiddleRight);
            CoastUI.Label(board,"Columns","POS    DRIVER                        TO YOU",12,new Vector2(310,24),new Vector2(0,177),CoastUI.Muted,true);
            for(int i=0;i<11;i++){
                var row=CoastUI.Panel(board,"Standing "+i,new Vector2(326,31),new Vector2(0,145-i*32),Color.clear,8);rows[i]=row.gameObject;strips[i]=row.GetComponent<CoastPanel>();
                places[i]=CoastUI.Label(row,"Rank","",20,new Vector2(37,28),new Vector2(-135,0),CoastUI.Ink,true);
                names[i]=CoastUI.Label(row,"Driver","",20,new Vector2(176,28),new Vector2(-24,0),CoastUI.Ink);names[i].resizeTextForBestFit=true;names[i].resizeTextMinSize=15;names[i].resizeTextMaxSize=20;
                gaps[i]=CoastUI.Label(row,"Relative time","",18,new Vector2(88,28),new Vector2(111,0),CoastUI.Ink,false,TextAnchor.MiddleRight);
            }
            CoastUI.Label(board,"Gap key","− ahead    + behind    ·    seconds",14,new Vector2(310,27),new Vector2(0,-221),CoastUI.Muted);
            foreach(Transform child in board)boardPositions.Add(child.localPosition);
            var dash=CoastUI.Panel(canvas.transform,"Enamel instruments",new Vector2(350,350),Vector2.zero,CoastUI.Paper,45,true);Anchor(dash,1,0,new Vector2(-212,233));
            CoastUI.Label(dash,"Instrument maker","LITTLE ROAD  •  MOTOR CLUB",12,new Vector2(280,25),new Vector2(0,151),CoastUI.Muted,true,TextAnchor.MiddleCenter);
            var ring=CoastUI.Rect(dash,"Speed scale",new Vector2(248,248),new Vector2(0,11));var track=ring.gameObject.AddComponent<CoastArc>();track.color=new Color(.77f,.81f,.71f);track.raycastTarget=false;
            var fill=CoastUI.Rect(dash,"Live speed arc",new Vector2(248,248),new Vector2(0,11));speedArc=fill.gameObject.AddComponent<CoastArc>();speedArc.color=CoastUI.Sea;speedArc.raycastTarget=false;
            for(int i=0;i<=6;i++){float a=(220-260*i/6f)*Mathf.Deg2Rad;Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*100+new Vector2(0,11);CoastUI.Label(dash,"Speed graduation",(i*20).ToString(),14,new Vector2(36,22),p,CoastUI.Muted,true,TextAnchor.MiddleCenter);}
            speed=CoastUI.Label(dash,"Speed","0",68,new Vector2(175,90),new Vector2(0,25),CoastUI.Ink,true,TextAnchor.MiddleCenter);
            CoastUI.Label(dash,"Units","MPH",16,new Vector2(100,23),new Vector2(0,-20),CoastUI.Muted,true,TextAnchor.MiddleCenter);
            var shift=CoastUI.Panel(dash,"Drive selector",new Vector2(56,38),new Vector2(0,-61),CoastUI.Ink,12);
            gear=CoastUI.Label(shift,"Selected drive","D",27,new Vector2(50,37),Vector2.zero,CoastUI.Paper,true,TextAnchor.MiddleCenter);
            CoastUI.Label(dash,"Transmission","AUTO DRIVE",12,new Vector2(145,20),new Vector2(0,-91),CoastUI.Muted,true,TextAnchor.MiddleCenter);
            gasFill=Meter(dash,"THROTTLE",new Vector2(-80,-144),CoastUI.Sea);brakeFill=Meter(dash,"BRAKE / REVERSE",new Vector2(80,-144),CoastUI.Orange);
            notice=CoastUI.Label(canvas.transform,"Race notice","",31,new Vector2(850,65),new Vector2(0,305),CoastUI.Paper,true,TextAnchor.MiddleCenter);
            var noteShadow=notice.gameObject.AddComponent<Shadow>();noteShadow.effectColor=new Color(0,0,0,.8f);noteShadow.effectDistance=new Vector2(2,-2);
            compactHint=CoastUI.Label(canvas.transform,"HUD controls","H / L3: compact order   ·   Hold Q / Triangle: rear view",17,new Vector2(740,32),Vector2.zero,CoastUI.Paper,false,TextAnchor.MiddleCenter);Anchor(compactHint.rectTransform,.5f,0,new Vector2(0,29));
            compactHint.gameObject.AddComponent<Shadow>().effectColor=CoastUI.Ink;
        }
        RectTransform Meter(Transform parent,string label,Vector2 pos,Color color)
        {
            var rail=CoastUI.Panel(parent,label,new Vector2(136,12),pos,new Color(.77f,.81f,.71f),6);
            CoastUI.Label(rail,"Label",label,13,new Vector2(150,22),new Vector2(0,18),CoastUI.Muted,true,TextAnchor.MiddleCenter);
            var fill=CoastUI.Panel(rail,"Applied pedal",new Vector2(136,12),new Vector2(-68,0),color,5);fill.pivot=new Vector2(0,.5f);return fill;
        }
        void Sample()
        {
            foreach(var c in race.cars){float p=c.finished?race.requiredLaps*race.track.length:c.RankedProgress;timing.Observe(c.driverId,c.finished?c.finishTime:race.elapsed,p,c.finished,c.finishTime,c.recoveries,c.finished?race.FinishPlace(c):0);}
        }
        void FixedUpdate(){if(race!=null)Sample();}
        void LateUpdate()
        {
            if(race==null)return;
            bool visible=!race.PreviewOpen&&!race.ResultsOpen;canvas.enabled=visible;if(!visible)return;
            if(Keyboard.current?.hKey.wasPressedThisFrame==true||Gamepad.current?.leftStickButton.wasPressedThisFrame==true){compact=!compact;Refresh();}
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.1f;Refresh();}
        }
        public void Refresh()
        {
            var player=race.cars[0];timing.Sort();speed.text=Mathf.RoundToInt(Mathf.Abs(player.speed)*2.2369363f).ToString();gear.text=DriveIndicator(player.speed,player.throttle);speedArc.Set(Mathf.Abs(player.speed)*2.2369363f/120);
            gasFill.sizeDelta=new Vector2(136*Mathf.Clamp01(player.throttle),12);brakeFill.sizeDelta=new Vector2(136*Mathf.Clamp01(-player.throttle),12);
            int rank=timing.Rank(0);position.text=rank+" / "+race.cars.Count;lap.text=$"LAP  {Mathf.Min(player.laps+1,race.requiredLaps)} / {race.requiredLaps}";
            float t=Mathf.Max(0,player.finished?player.finishTime:race.elapsed);clock.text=$"{(int)t/60:00}:{t%60:00.0}";
            notice.text=race.WaitingForReady?"PRESS ENTER / OPTIONS TO START":race.elapsed<0?"GET READY   "+Mathf.CeilToInt(-race.elapsed):player.CheckpointMissed?"MISSED CHECKPOINT  ·  R / D-pad Down: recover":race.PlayerRecoveryPending?"WAITING FOR A CLEAR RECOVERY SPACE":player.finished?"FINISHED  ·  "+rank+" / "+race.cars.Count:race.elapsed<1?"LET'S GO!":"";
            int count=Mathf.Min(compact?5:11,race.cars.Count),start=timing.WindowStart(0,count);
            float height=compact?308:500,shift=(height-500)*.5f;board.sizeDelta=new Vector2(350,height);board.anchoredPosition=new Vector2(210,-154-height*.5f);
            for(int i=0;i<board.childCount;i++){var p=boardPositions[i];p.y+=shift;board.GetChild(i).localPosition=p;}
            // The explanatory footer follows the lower edge, not the header.
            var footer=board.GetChild(board.childCount-1);footer.localPosition=new Vector3(0,-height*.5f+29,0);
            for(int i=0;i<11;i++) {
                rows[i].SetActive(i<count);if(i>=count)continue;int id=timing.IdAt(start+i);bool you=id==0;
                rows[i].transform.localPosition=new Vector3(0,145-i*32+shift,0);strips[i].color=you?CoastUI.Sun:i%2==0?new Color(.07f,.22f,.24f,.045f):Color.clear;
                places[i].text=(start+i+1).ToString("00");names[i].text=you?"> YOU":race.Roster.Entries[id].Driver?.Name??"Driver "+id;
                var gap=timing.RelativeTo(id,0);gaps[i].text=GapText(gap);
            }
        }
        public static string GapText(RaceTiming.Gap gap)=>gap.Kind switch {RaceTiming.GapKind.Player=>"YOU",RaceTiming.GapKind.Waiting=>"—",RaceTiming.GapKind.Laps=>(gap.Value<0?"−":"+")+Mathf.Abs(gap.Value).ToString("0")+"L",_=>(gap.Value<0?"−":"+")+Mathf.Abs(gap.Value).ToString("0.0")};
        public void Capture(string path,int width=1920,int height=1080)
        {
            Refresh();Canvas.ForceUpdateCanvases();var cam=Camera.main;var previous=cam.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(width,height,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());cam.targetTexture=previous;RenderTexture.active=active;rt.Release();Destroy(rt);Destroy(tex);
        }
        [Serializable] public sealed class HudEvidence {public float elapsed,speed,pedal;public int rank;public string drive;public List<string> rows=new();}
        IEnumerator Review()
        {
            string folder=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(RaceTrial.Arg("--trial-report"))),"hud");Directory.CreateDirectory(folder);
            foreach(int target in new[]{0,12,25}){
                while(race.elapsed<target)yield return null;
                Refresh();Capture(Path.Combine(folder,"race-"+target+".png"));var p=race.cars[0];var e=new HudEvidence{elapsed=race.elapsed,speed=p.speed,pedal=p.throttle,rank=timing.Rank(0),drive=gear.text};
                for(int i=0;i<11;i++)if(rows[i].activeSelf)e.rows.Add(places[i].text+" "+names[i].text+" "+gaps[i].text);
                File.WriteAllText(Path.Combine(folder,"race-"+target+".json"),JsonUtility.ToJson(e,true));
            }
            var pad=InputSystem.AddDevice<Gamepad>();
            try {
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.LeftStick));yield return new WaitForSecondsRealtime(.15f);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.15f);
                if(!compact)throw new Exception("L3 did not compact the standings.");Capture(Path.Combine(folder,"compact.png"));
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.LeftStick));yield return new WaitForSecondsRealtime(.15f);
                InputSystem.QueueStateEvent(pad,new GamepadState());yield return new WaitForSecondsRealtime(.15f);
                if(compact)throw new Exception("L3 did not restore all eleven rows.");
            }finally{InputSystem.RemoveDevice(pad);}
            Debug.Log("LITTLE_ROAD_HUD_REVIEW_CAPTURED live instruments/relative standings and synthetic L3 compact toggle; physical controller and player review remain required");
        }
    }
}
