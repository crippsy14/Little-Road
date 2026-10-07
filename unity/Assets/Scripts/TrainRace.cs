using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Beat the Train (Claude's own addition, 30 Sep; docs/51). Stanley at Bayview Halt or Stella at Hillcrest station
    // holds Bert's train at the platform; on GO it pulls out, whistling, and you race it by road to the other station's
    // forecourt: over the causeway, through the headland and down into the valley (or back).
    //   The timetable is fair to the car you start with. The line is longer than the road (1.75-1.9 km against 1.3), so
    //   Bert opens the regulator (24 m/s, 54 mph: an express) and gets a head start instead:
    //   the train leaves first and your GO comes when the stock car's ideal time over the road route (SpeedProfile)
    //   plus 25% would just match it (an autopilot at 95% of that profile, a touch above stock, lost by 22 m and 160 m at 12%; at 20% it won
    //   towards Bayview and lost by 80 m towards Hillcrest, fumbling the tight turn into the station road). A stock car needs a tidy drive; upgrades make it comfortable. The first win
    //   each way pays; best times stay in the save (TrapRecord, lower is better).
    public sealed class TrainRace:MonoBehaviour
    {
        public static readonly string[] Ids={"train-hillcrest","train-bayview"};
        public static bool Known(string id)=>Array.IndexOf(Ids,id)>=0;
        public const int Prize=300;public const float Margin=1.25f,Regulator=24;
        RoadCar player;Func<AdventureStore> store;Action<string> notify;Action<string,string> card;ValleyTrain train;
        public bool Active {get;private set;}
        public bool ToHillcrest {get;private set;}
        public Vector3 Goal {get;private set;}
        float goTime=-1,goAt,trainTime,headStart;int destStop;bool finished;
        public float GoAt=>goAt;public float HeadStart=>headStart;public float TrainTime=>trainTime;

        public void Setup(RoadCar car,Func<AdventureStore> saves,Action<string> toast,Action<string,string> showCard){player=car;store=saves;notify=toast;card=showCard;}
        public static string Id(bool toHillcrest)=>toHillcrest?Ids[0]:Ids[1];
        static Vector3 Arrival(bool hillcrest)=>hillcrest?HillcrestRoute.StationYard+new Vector3(-2,0,4):CoastalLandscape.HaltArrival;

        // The road route between the stations' forecourts: the mainline from the Halt quay to the valley road, along
        // the quay road to the station road, up to the yard.
        public static List<Vector3> Route(bool toHillcrest)
        {
            var path=new List<Vector3>{CoastalLandscape.HaltArrival};
            var main=HillcrestRoute.Mainline.line;int m0=HillcrestRoute.Index(main,CoastalLandscape.HaltArrival);
            for(int i=m0;i<main.Length;i++)path.Add(main[i]);
            var quay=HillcrestRoute.Quay.line;int q0=HillcrestRoute.Index(quay,main[^1]),q1=HillcrestRoute.Index(quay,HillcrestRoute.StationSpur.line[0]);
            for(int i=q0;i!=q1;i+=q1>q0?1:-1)path.Add(quay[i]);
            path.AddRange(HillcrestRoute.StationSpur.line);path.Add(Arrival(true));
            if(!toHillcrest)path.Reverse();
            return path;
        }
        public static float StockTime(bool toHillcrest)=>SpeedProfile.Time(Route(toHillcrest),CarTuning.Stock);

        public bool CanStart=>!Active&&ValleyTrain.Instance!=null;
        public void Begin(bool toHillcrest)
        {
            train=ValleyTrain.Instance;if(train==null||Active)return;
            ToHillcrest=toHillcrest;int fromStop=toHillcrest?ValleyTrain.BayviewStop:ValleyTrain.HillcrestStop;destStop=toHillcrest?ValleyTrain.HillcrestStop:ValleyTrain.BayviewStop;
            float target=StockTime(toHillcrest)*Margin,top=Regulator;
            // (If even the regulator open isn't enough, the train goes a little quicker with no head start.)
            if(train.TimeToNext(fromStop,top)<target){float lo=4,hi=top;for(int i=0;i<22;i++){float mid=(lo+hi)*.5f;if(train.TimeToNext(fromStop,mid)>target)lo=mid;else hi=mid;}top=hi;}
            else if(train.TimeToNext(fromStop,top)>target*1.6f){float lo=top,hi=34;for(int i=0;i<22;i++){float mid=(lo+hi)*.5f;if(train.TimeToNext(fromStop,mid)>target*1.6f)lo=mid;else hi=mid;}top=hi;}
            train.MaxSpeed=top;trainTime=train.TimeToNext(fromStop,top);headStart=Mathf.Max(0,trainTime-target);
            // Bert leaves `headStart` seconds before your GO, and your countdown is at least 3 s (5 Oct: with no head start
            // the old 3-2-1 lasted a hundredth of a second each).
            float hold=Mathf.Max(1.8f,3-headStart);goAt=Time.time+hold+headStart;
            train.Hold(fromStop,hold);train.Arrived+=OnArrived;
            Goal=Arrival(toHillcrest);Active=true;finished=false;goTime=-1;if(player!=null)player.holdStill=true;   // held from the first physics step
            Debug.Log($"LITTLE_ROAD_TRAIN_RACE start to={(toHillcrest?"hillcrest":"bayview")} stock={target/Margin:0.0}s train={trainTime:0.0}s top={train.MaxSpeed*SpeedTraps.Mph:0} mph route={SpeedProfile.Length(Route(toHillcrest)):0}m head={headStart:0.0}s");
            StartCoroutine(Countdown());
        }
        // (The 3-2-1 and its beeps are the challenge HUD's: ActivityHud and ToyCountdown.)
        IEnumerator Countdown()
        {
            while(Time.time<goAt)yield return null;
            goTime=Time.time;
        }
        public float Elapsed=>goTime<0?0:Time.time-goTime;
        // Counting down: the car is held (it can rev).
        public bool Holding=>Active&&goTime<0;
        public string Goal_Text(Vector3 from,bool distance)=>goTime<0?"Beat the Train · get ready!":$"Beat the train to {(ToHillcrest?"Hillcrest station":"Bayview Halt")} · {Elapsed:0.0} s"+(distance?$"  ·  {Vector3.Distance(from,Goal):0} m":"");
        void Update()
        {
            if(player!=null&&Active&&!finished)player.holdStill=Holding;   // held through the countdown (it can rev)
            if(!Active||finished||player==null)return;
            if(goTime>0&&Vector3.Distance(player.body.position,Goal)<15)Win();
            else if(goTime>0&&Elapsed>480)End("Beat the Train · called off. Try again any time!");
        }
        void OnArrived(int stop){if(Active&&!finished&&stop==destStop)Lose();}
        void Win()
        {
            finished=true;float t=Elapsed;int paid=0;string id=Id(ToHillcrest);
            var saves=store();var before=SpeedTraps.Record(saves.Data,id);float previous=before?.best??0;
            try{paid=saves.RecordTime(id,t,Prize);}catch(Exception e){Debug.LogException(e);}
            Confetti.Burst(player.body.position+Vector3.up*2);train?.Whistle();
            string detail=$"{t:0.0} s  ·  the train takes {trainTime:0} s"+(paid>0?$"  ·  +{paid} credits":previous>0&&t<previous-.05f?$"  ·  new best (was {previous:0.0})":previous>0?$"  ·  best {previous:0.0} s":"");
            card?.Invoke("YOU BEAT THE TRAIN!",detail);Debug.Log($"LITTLE_ROAD_TRAIN_RACE win t={t:0.0} paid={paid}");
            End(null);
        }
        void Lose()
        {
            finished=true;float left=Vector3.Distance(player.body.position,Goal);
            card?.Invoke("THE TRAIN WINS!",$"Bert pulled in with you {left:0} m away  ·  faster parts, or a tidier line?");Debug.Log($"LITTLE_ROAD_TRAIN_RACE lose left={left:0}m");
            End(null);
        }
        public void End(string message)
        {
            if(train!=null){train.Arrived-=OnArrived;train.MaxSpeed=13;}
            Active=false;goTime=-1;if(player!=null)player.holdStill=false;if(message!=null)notify?.Invoke(message);
        }
    }
}
