using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // The tuning shop's test drive (docs/48): the car on the start line of the town's circuit, one timed lap (lap time,
    // top speed, 0-60 mph), then back to the tuning screen with the result and the car back at the garage.
    public sealed class TestDrive : MonoBehaviour
    {
        RoadCar car;Circuit track;Action<string> done;Canvas canvas;Text readout;
        Vector3 homePosition;Quaternion homeRotation;
        float time,progress,lastPath,top,to60=-1;bool started,moving;
        public bool Active {get;private set;}

        public void Begin(RoadCar player,Circuit circuit,string circuitName,Action<string> onDone)
        {
            car=player;track=circuit;done=onDone;homePosition=car.body.position;homeRotation=car.body.rotation;
            var start=track.At(2)+Vector3.up*.4f;car.body.position=start;car.body.rotation=Quaternion.LookRotation(track.Forward(2));
            car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;car.gears?.Reset();car.controlsBlocked=false;
            lastPath=track.Nearest(start,out _);progress=0;time=0;top=0;to60=-1;started=false;moving=false;Active=true;
            if(canvas==null) {
                canvas=CoastUI.Canvas(transform,"Test drive",40);
                var pill=Toy.Card(canvas.transform,"Test drive panel",new Vector2(900,120),new Vector2(0,420),Toy.Navy,30,5,6);
                Toy.Title(pill.transform,"Test drive title","TEST DRIVE  ·  "+circuitName.ToUpperInvariant(),28,new Vector2(860,40),new Vector2(0,28),Toy.Sun,TextAnchor.MiddleCenter);
                readout=Toy.Label(pill.transform,"Test drive readout",string.Empty,26,new Vector2(860,44),new Vector2(0,-20),Toy.White,TextAnchor.MiddleCenter,true);
            }
            canvas.gameObject.SetActive(true);
        }

        void Update()
        {
            if(!Active)return;
            var k=Keyboard.current;var p=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||p?.startButton.wasPressedThisFrame==true){Finish(null);return;}
            float speed=Mathf.Abs(car.speed);
            // The clock starts when the car does; the lap is one full circuit of progress along the road.
            if(!moving&&speed>.5f)moving=true;
            if(moving){time+=Time.deltaTime;started=true;}
            float path=track.Nearest(car.body.position,out _);float step=Mathf.Repeat(path-lastPath+track.length*.5f,track.length)-track.length*.5f;
            if(Mathf.Abs(step)<30)progress+=step;lastPath=path;
            top=Mathf.Max(top,speed);if(to60<0&&speed>=26.82f)to60=time;
            readout.text=$"{Clock(time)}   ·   {Units.Show(speed)}   ·   top {Units.Show(top)}   ·   {Mathf.Clamp01(progress/track.length)*100:0}%   ·   Esc: back to tuning";
            if(started&&progress>=track.length)Finish($"Test drive: lap {Clock(time)}  ·  top {top*3.6f:0} km/h  ·  0-60 {(to60>0?to60.ToString("0.0")+" s":"—")}");
        }
        static string Clock(float t)=>$"{Mathf.FloorToInt(t/60)}:{t%60:00.0}";
        void Finish(string result)
        {
            Active=false;canvas.gameObject.SetActive(false);
            car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;car.body.position=homePosition;car.body.rotation=homeRotation;car.gears?.Reset();
            done?.Invoke(result);
        }
    }
}
