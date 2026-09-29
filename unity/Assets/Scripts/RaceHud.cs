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
        RaceTrial race;RaceTiming timing;Canvas canvas;RectTransform board;Text speed,gear,lap,clock,position,notice,compactHint,totalTime,bestLap;
        Text[] names=new Text[11],places=new Text[11],gaps=new Text[11];CoastPanel[] strips=new CoastPanel[11];GameObject[] rows=new GameObject[11];
        RectTransform gasFill,brakeFill,steerPip;float nextRefresh;bool compact;
        readonly RaceLapHistory lapHistory=new();Text[] recentLaps=new Text[3];Text lapHistoryTitle;RaceMapCars mapCars;float finalLapUntil=-1;
        public RaceTiming Timing=>timing;
        public static bool Requested=>RaceTrial.Has("--race-hud")||RaceTrial.Has("--race-hud-review");
        public static string DriveIndicator(float speed,float pedal)=>speed<-.15f||(speed<=.1f&&pedal<0)?"R":"D";
        public void Setup(RaceTrial owner)
        {
            race=owner;var slots=new int[race.cars.Count];for(int i=0;i<slots.Length;i++)slots[i]=race.Roster.Entries[i].Slot;
            timing=new RaceTiming(race.track.length,slots);Build();Sample();Refresh();
            gameObject.AddComponent<ToyCountdown>().Setup(()=>race.elapsed,()=>race.WaitingForReady||race.PreviewOpen||race.ResultsOpen);
            if(RaceTrial.Has("--race-hud-review"))StartCoroutine(Review());
        }
        static void Anchor(RectTransform rect,float x,float y,Vector2 pos){rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.anchoredPosition=pos;}
        static readonly Color White=new(.98f,.99f,1),Dim=new(.80f,.85f,.88f),Accent=new(.32f,.91f,.94f);
        Text Readout(Transform parent,string name,string value,int size,Vector2 box,Vector2 pos,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var text=CoastUI.Label(parent,name,value,size,box,pos,White,bold,align);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.88f);shadow.effectDistance=new Vector2(1.5f,-1.5f);return text;
        }
        void Build()
        {
            canvas=CoastUI.Canvas(transform,"Clear race instruments",4);
            var posRoot=CoastUI.Rect(canvas.transform,"Position",new Vector2(180,75),Vector2.zero);Anchor(posRoot,0,1,new Vector2(127,-73));
            Readout(posRoot,"Caption","POSITION",14,new Vector2(180,23),new Vector2(0,31),true);
            position=Readout(posRoot,"Place","",39,new Vector2(185,55),new Vector2(0,-3),true);
            var lapRoot=CoastUI.Rect(canvas.transform,"Lap",new Vector2(165,70),Vector2.zero);Anchor(lapRoot,0,1,new Vector2(300,-73));
            Readout(lapRoot,"Caption","LAP",14,new Vector2(160,23),new Vector2(0,31),true);
            lap=Readout(lapRoot,"Lap count","",29,new Vector2(170,48),new Vector2(0,-4));
            var timeRoot=CoastUI.Rect(canvas.transform,"Current lap",new Vector2(245,72),Vector2.zero);Anchor(timeRoot,.5f,1,new Vector2(0,-69));
            Readout(timeRoot,"Caption","LAP TIME",13,new Vector2(245,24),new Vector2(0,26),false,TextAnchor.MiddleCenter);
            clock=Readout(timeRoot,"Timer","",32,new Vector2(245,45),new Vector2(0,-4),false,TextAnchor.MiddleCenter);
            bestLap=Readout(canvas.transform,"Best lap","",20,new Vector2(290,38),Vector2.zero,false,TextAnchor.MiddleRight);Anchor(bestLap.rectTransform,1,1,new Vector2(-185,-61));
            var map=CoastUI.Rect(canvas.transform,"Course map",new Vector2(290,185),Vector2.zero);Anchor(map,1,1,new Vector2(-184,-193));
            var projection=new TrackMapProjection(race.track.points,map.sizeDelta);map.gameObject.AddComponent<RaceTrackMap>().Setup(race.track,projection);
            var markers=CoastUI.Rect(map,"Physical entrants",map.sizeDelta,Vector2.zero);mapCars=markers.gameObject.AddComponent<RaceMapCars>();mapCars.Setup(race,projection);
            var mapKey=Readout(canvas.transform,"Map key","COURSE   ·   ▲ YOU",12,new Vector2(290,24),Vector2.zero,false,TextAnchor.MiddleRight);Anchor(mapKey.rectTransform,1,1,new Vector2(-185,-307));
            lapHistoryTitle=Readout(canvas.transform,"Recent lap heading","RECENT LAPS",12,new Vector2(290,24),Vector2.zero,false,TextAnchor.MiddleRight);Anchor(lapHistoryTitle.rectTransform,1,1,new Vector2(-185,-353));
            for(int i=0;i<3;i++){recentLaps[i]=Readout(canvas.transform,"Completed lap "+i,"",19,new Vector2(290,27),Vector2.zero,false,TextAnchor.MiddleRight);Anchor(recentLaps[i].rectTransform,1,1,new Vector2(-185,-382-i*29));}
            board=CoastUI.Rect(canvas.transform,"Relative standings",new Vector2(300,360),Vector2.zero);Anchor(board,0,1,new Vector2(187,-329));
            Readout(board,"Columns","DRIVER                       GAP / s",13,new Vector2(274,25),new Vector2(0,169));
            for(int i=0;i<11;i++){
                var row=CoastUI.Panel(board,"Standing "+i,new Vector2(300,28),new Vector2(0,137-i*29),new Color(0,0,0,.20f),1);rows[i]=row.gameObject;strips[i]=row.GetComponent<CoastPanel>();
                places[i]=Readout(row,"Rank","",19,new Vector2(35,27),new Vector2(-126,0),true);
                names[i]=Readout(row,"Driver","",19,new Vector2(165,27),new Vector2(-24,0));names[i].resizeTextForBestFit=true;names[i].resizeTextMinSize=15;names[i].resizeTextMaxSize=19;
                gaps[i]=Readout(row,"Relative time","",18,new Vector2(81,27),new Vector2(103,0),false,TextAnchor.MiddleRight);
            }
            Readout(board,"Gap key","− ahead   /   + behind",13,new Vector2(290,26),new Vector2(0,-184));
            var dash=CoastUI.Panel(canvas.transform,"Driving instruments",new Vector2(370,105),Vector2.zero,new Color(0,0,0,.12f),1);Anchor(dash,.5f,0,new Vector2(0,106));
            speed=Readout(dash,"Speed","0",70,new Vector2(176,87),new Vector2(-49,6),false,TextAnchor.MiddleRight);
            Readout(dash,"Speed unit","mph",15,new Vector2(160,25),new Vector2(-51,-39),false,TextAnchor.MiddleRight);
            CoastUI.Panel(dash,"Separator",new Vector2(1,66),new Vector2(49,0),new Color(1,1,1,.60f),0);
            gear=Readout(dash,"Drive selector","D",54,new Vector2(75,70),new Vector2(96,7),false,TextAnchor.MiddleCenter);
            Readout(dash,"Transmission","AUTO",13,new Vector2(76,23),new Vector2(96,-38),false,TextAnchor.MiddleCenter);
            gasFill=Meter(dash,"THR",new Vector2(210,0),White);brakeFill=Meter(dash,"BRK / R",new Vector2(-210,0),new Color(1,.32f,.27f));
            CoastUI.Panel(dash,"Steering input rail",new Vector2(150,1),new Vector2(0,62),new Color(1,1,1,.45f),0);
            steerPip=CoastUI.Panel(dash,"Steering input",new Vector2(4,9),new Vector2(0,62),Accent,0);
            totalTime=Readout(canvas.transform,"Race time","",19,new Vector2(330,32),Vector2.zero);Anchor(totalTime.rectTransform,0,0,new Vector2(207,69));
            notice=Readout(canvas.transform,"Race notice","",29,new Vector2(920,60),Vector2.zero,true,TextAnchor.MiddleCenter);Anchor(notice.rectTransform,.5f,1,new Vector2(0,-146));

        }
        RectTransform Meter(Transform parent,string label,Vector2 pos,Color color)
        {
            var rail=CoastUI.Panel(parent,label,new Vector2(12,62),pos,new Color(1,1,1,.23f),0);
            Readout(rail,"Label",label,12,new Vector2(70,22),new Vector2(0,-45),false,TextAnchor.MiddleCenter);
            var fill=CoastUI.Panel(rail,"Applied pedal",new Vector2(12,62),new Vector2(0,-31),color,0);fill.pivot=new Vector2(.5f,0);return fill;
        }
        void Sample()
        {
            foreach(var c in race.cars){float p=c.finished?race.requiredLaps*race.track.length:c.RankedProgress;timing.Observe(c.driverId,c.finished?c.finishTime:race.elapsed,p,c.finished,c.finishTime,c.recoveries,c.finished?race.FinishPlace(c):0);}
            var player=race.cars[0];if(lapHistory.Observe(player.laps,player.LastLapTime)&&race.requiredLaps>1&&player.laps==race.requiredLaps-1&&!player.finished)finalLapUntil=race.elapsed+3;
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
            var player=race.cars[0];timing.Sort();speed.text=Mathf.RoundToInt(Mathf.Abs(player.speed)*2.2369363f).ToString();gear.text=DriveIndicator(player.speed,player.throttle);steerPip.anchoredPosition=new Vector2(Mathf.Clamp(player.steering,-1,1)*75,62);
            gasFill.sizeDelta=new Vector2(12,62*Mathf.Clamp01(player.throttle));brakeFill.sizeDelta=new Vector2(12,62*Mathf.Clamp01(-player.throttle));
            int rank=timing.Rank(0);position.text=rank+" / "+race.cars.Count;lap.text=$"{Mathf.Min(player.laps+1,race.requiredLaps)} / {race.requiredLaps}";
            float t=Mathf.Max(0,player.finished?player.finishTime:race.elapsed);totalTime.text=$"TOTAL  {(int)t/60:00}:{t%60:00.0}";float lapTime=player.CurrentLapTime;clock.text=player.nextGateProgress<=0?"—":$"{(int)lapTime/60:0}:{lapTime%60:00.000}";bestLap.text=player.bestLap==float.MaxValue?"":$"BEST  {(int)player.bestLap/60:0}:{player.bestLap%60:00.000}";
            notice.text=race.WaitingForReady?"PRESS ENTER / OPTIONS TO START":race.elapsed<0?"":player.CheckpointMissed?"MISSED CHECKPOINT  ·  R / D-pad Down: recover":race.PlayerRecoveryPending?"WAITING FOR A CLEAR RECOVERY SPACE":player.finished?"FINISHED  ·  "+rank+" / "+race.cars.Count:race.elapsed<1?"LET'S GO!":"";
            if(notice.text.Length==0&&race.elapsed<finalLapUntil)notice.text="FINAL LAP";
            mapCars.Refresh();lapHistoryTitle.gameObject.SetActive(lapHistory.Count>0);
            for(int i=0;i<3;i++){recentLaps[i].gameObject.SetActive(i<lapHistory.Count);if(i<lapHistory.Count){float seconds=lapHistory.Seconds(i);recentLaps[i].text=$"L{lapHistory.Lap(i)}   {(int)seconds/60:0}:{seconds%60:00.000}";}}
            int count=Mathf.Min(compact?5:11,race.cars.Count),start=timing.WindowStart(0,count);
            var footer=board.GetChild(board.childCount-1);footer.localPosition=new Vector3(0,137-count*29-12,0);
            for(int i=0;i<11;i++) {
                rows[i].SetActive(i<count);if(i>=count)continue;int id=timing.IdAt(start+i);bool you=id==0;
                rows[i].transform.localPosition=new Vector3(0,137-i*29,0);strips[i].color=you?new Color(.10f,.57f,.61f,.55f):new Color(0,0,0,.20f);
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
            if(race.requiredLaps>1){
                var player=race.cars[0];while(player.laps<race.requiredLaps-1)yield return null;
                Refresh();if(lapHistory.Count<1||lapHistory.Lap(0)!=player.laps||Mathf.Abs(lapHistory.Seconds(0)-player.LastLapTime)>.0001f)throw new Exception("Recent lap HUD differs from completed lap.");
                Capture(Path.Combine(folder,"final-lap.png"),Screen.width,Screen.height);
                File.WriteAllText(Path.Combine(folder,"lap-history.json"),JsonUtility.ToJson(new LapEvidence{completed=player.laps,lastLap=player.LastLapTime,notice=notice.text,rows=Array.ConvertAll(recentLaps,v=>v.text),width=Screen.width,height=Screen.height},true));
                Debug.Log("LITTLE_ROAD_HUD_LAP_HISTORY_PASSED actual completed laps and final-lap view captured");
            }
        }
        [Serializable] public sealed class LapEvidence{public int completed,width,height;public float lastLap;public string notice;public string[] rows;}
    }
}
