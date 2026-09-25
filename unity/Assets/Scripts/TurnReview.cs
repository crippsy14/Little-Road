using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in (--turn-review) free-roam driving probe: scripted full-throttle
    // weaves at several places, logging every fixed step where the car loses
    // speed or yaw abruptly and what it was touching at the time.
    public sealed class TurnReview:MonoBehaviour
    {
        [Serializable] public class Jolt {public string place;public float time,speed,drop,yawRate,yawJump,velocity,steer;public Vector3 position,heading,motion;public string[] contacts,before;}
        [Serializable] public class Run {public string place;public float topSpeed,worstDrop;public int jolts;}
        [Serializable] public class Evidence {public string scope="Scripted free-roam weaves (throttle held, 0.7 s full-lock steering pulses), 120 Hz; a jolt is a one-step speed loss above 0.2 m/s or yaw-rate jump above 0.5 rad/s";public List<string> edges=new();public List<Run> runs=new();public List<Jolt> jolts=new();public bool passed;}
        RaceTrial world;RoadCar car;readonly Evidence data=new();TurnContactProbe probe;
        IEnumerator Start()
        {
            string folder=RaceTrial.Arg("--trial-captures");if(string.IsNullOrEmpty(folder))throw new Exception("Turn review requires --trial-captures");Directory.CreateDirectory(folder);
            world=GetComponent<RaceTrial>();while(!world.running||world.cars.Count==0)yield return null;car=world.cars[0];probe=car.gameObject.AddComponent<TurnContactProbe>();
            if(RaceTrial.Has("--kick-probe")){yield return KickProbe(folder);yield break;}
            var track=world.track;
            var places=new List<(string name,Vector3 at,Vector3 heading)>{("village paving",new Vector3(-50,0,-94),Vector3.left),("village forecourt",new Vector3(-40,0,-97),Vector3.left)};
            for(int i=0;i<14;i++){float d=track.length*i/14;places.Add(("road "+Mathf.RoundToInt(d),track.At(d),track.Forward(d)));}
            // Surface heights across the road edge (road/shoulder support vs island).
            for(int i=0;i<14;i++){float d=track.length*i/14;var line="edge "+Mathf.RoundToInt(d)+":";
                for(float o=9;o<=15.01f;o+=.5f){var p=track.At(d)+track.Right(d)*o;string top="-";float h=-9;
                    foreach(var hit in Physics.RaycastAll(p+Vector3.up*5,Vector3.down,10))if(!hit.collider.transform.IsChildOf(car.transform)&&hit.point.y>h){h=hit.point.y;top=hit.collider.name.Split(' ')[0];}
                    line+=$" {o:0.0}={h:0.000}{top[0]}";}
                data.edges.Add(line);}
            foreach(var (name,at,heading) in places)yield return Weave(name,at,heading);
            // Human-like laps: steer at a point 14 m up the road. "wide" swings from -11 to +11 m,
            // crossing the painted edge onto the runoff; the trace records every step.
            foreach(var (name,swing) in new[]{("lap centre",0f),("lap wide",11f)})yield return Lap(name,swing,folder);
            // Realistic edge crossings: cruise on the shoulder, ease off the road, ease back on.
            for(int i=0;i<14;i++){float d=track.length*i/14;yield return Weave("edge cross "+Mathf.RoundToInt(d),track.At(d)+track.Right(d)*10,track.Forward(d),true);}
            data.passed=data.jolts.Count==0;
            File.WriteAllText(Path.Combine(folder,"turn-review.json"),JsonUtility.ToJson(data,true));
            Debug.Log("TURN_REVIEW jolts="+data.jolts.Count+" passed="+data.passed);Application.Quit();
        }
        // Random mid-corner states on the asphalt at 20 m/s, full lock toward the bend with the
        // throttle held; counts road-support contacts that change velocity by over 1.5 m/s.
        IEnumerator KickProbe(string folder)
        {
            var track=world.track;var random=new System.Random(1709);Time.timeScale=4;int kicks=0,trials=int.Parse(RaceTrial.Arg("--kick-trials","150"));float worst=0;var lines=new List<string>();
            for(int trial=0;trial<trials;trial++) {
                float d=(float)random.NextDouble()*track.length,side=(float)(random.NextDouble()*10-5);
                float bend=Vector3.Cross(track.Forward(d-4),track.Forward(d+4)).y,sign=Mathf.Abs(bend)>.01f?Mathf.Sign(bend):(random.Next(2)*2-1);
                var heading=Quaternion.Euler(0,(float)(random.NextDouble()*6-3),0)*track.Forward(d);var at=track.At(d)+track.Right(d)*side;at.y=.025f+.0075f-car.ContactBottom-.0075f;
                car.body.position=at;car.body.rotation=Quaternion.LookRotation(heading);car.transform.SetPositionAndRotation(at,car.body.rotation);Physics.SyncTransforms();
                car.body.linearVelocity=heading*20;car.body.angularVelocity=new Vector3(0,sign*.45f,0);
                car.commandSource=_=>new Vector2(1,sign);float t=0,trialWorst=0;
                yield return new WaitForFixedUpdate();car.observedContactDeltaV=0;
                while(t<1.2f){yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                    if(car.observedContactSurface!=null&&car.observedContactSurface.StartsWith("Continuous road"))trialWorst=Mathf.Max(trialWorst,car.observedContactDeltaV);
                    car.observedContactDeltaV=0;car.observedContactSurface=null;}
                if(trialWorst>1.5f){kicks++;lines.Add($"d={d:0.0} side={side:0.00} steer={sign} kick={trialWorst:0.00}");}worst=Mathf.Max(worst,trialWorst);
            }
            car.commandSource=null;Time.timeScale=1;
            lines.Insert(0,$"KICK_PROBE trials={trials} kicks={kicks} worst={worst:0.00}");
            File.WriteAllLines(Path.Combine(folder,"kick-probe.txt"),lines);Debug.Log(lines[0]);Application.Quit();
        }
        IEnumerator Lap(string name,float swing,string folder)
        {
            var track=world.track;var start=track.At(20);start.y=.015f-car.ContactBottom;
            car.body.position=start;car.body.rotation=Quaternion.LookRotation(track.Forward(20));car.body.linearVelocity=car.body.angularVelocity=Vector3.zero;
            car.transform.SetPositionAndRotation(start,car.body.rotation);Physics.SyncTransforms();
            var csv=new System.Text.StringBuilder("t,path,offset,speed,velocity,yaw,steer,steerState,camera\n");float t=0,travelled=0,last=car.pathDistance;
            var view=Camera.main;
            car.commandSource=c=>{
                float d=c.pathDistance+14,target=swing*Mathf.Sin(travelled*Mathf.PI/90);
                var aim=track.At(d)+track.Right(d)*target-c.body.position;aim.y=0;
                return new Vector2(1,Mathf.Clamp(Vector3.SignedAngle(c.transform.forward,aim,Vector3.up)/25,-1,1));};
            while(travelled<track.length&&t<120) {
                yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                float step=Mathf.Repeat(car.pathDistance-last+track.length*.5f,track.length)-track.length*.5f;if(Mathf.Abs(step)<10)travelled+=step;last=car.pathDistance;
                csv.Append($"{t:0.000},{travelled:0.00},{car.offset:0.00},{car.speed:0.000},{car.body.linearVelocity.magnitude:0.000},{car.body.angularVelocity.y:0.000},{car.steering:0.00},{car.SteeringState:0.000},{(view!=null?Vector3.Distance(view.transform.position,car.transform.position):0):0.00}\n");
            }
            car.commandSource=null;File.WriteAllText(Path.Combine(folder,name.Replace(' ','-')+".csv"),csv.ToString());
        }
        IEnumerator Weave(string name,Vector3 at,Vector3 heading,bool gentle=false)
        {
            at.y=.015f-car.ContactBottom;
            car.body.position=at;car.body.rotation=Quaternion.LookRotation(heading);car.body.linearVelocity=car.body.angularVelocity=Vector3.zero;
            car.transform.SetPositionAndRotation(at,car.body.rotation);Physics.SyncTransforms();
            var run=new Run{place=name};float t=0,lastSpeed=0,lastYaw=0;string[] previous=new string[0];
            car.commandSource=_=>new Vector2(1,gentle?(t<2.5f?0:t<3.1f?.45f:t<3.9f?0:t<4.5f?-.45f:0):t<2.5f?0:Mathf.Sign(Mathf.Sin((t-2.5f)*Mathf.PI/.7f)));
            yield return new WaitForFixedUpdate();
            while(t<6.5f) {
                yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                float speed=car.speed,yaw=car.body.angularVelocity.y,drop=lastSpeed-speed,jump=Mathf.Abs(yaw-lastYaw);
                run.topSpeed=Mathf.Max(run.topSpeed,speed);
                if(t>.2f&&(drop>.2f||jump>.5f)) {
                    run.jolts++;run.worstDrop=Mathf.Max(run.worstDrop,drop);
                    if(data.jolts.Count<400)data.jolts.Add(new Jolt{place=name,time=t,speed=speed,drop=drop,yawRate=yaw,yawJump=jump,velocity=car.body.linearVelocity.magnitude,steer=car.SteeringState,position=car.body.position,heading=car.transform.forward,motion=car.body.linearVelocity,contacts=probe.Take(),before=previous});
                }
                previous=probe.Take();lastSpeed=speed;lastYaw=yaw;
            }
            car.commandSource=null;data.runs.Add(run);
        }
    }
    public sealed class TurnContactProbe:MonoBehaviour
    {
        readonly List<string> seen=new();
        void OnCollisionEnter(Collision c)=>Record(c,"enter");
        void OnCollisionStay(Collision c)=>Record(c,"stay");
        void Record(Collision c,string phase)
        {
            if(c.contactCount==0)return;var line=phase+" "+c.collider.name+" contacts="+c.contactCount+" impulse="+c.impulse.ToString("F1");
            for(int i=0;i<c.contactCount&&i<6;i++){var p=c.GetContact(i);line+=" | n="+p.normal.ToString("F2")+" at="+transform.InverseTransformPoint(p.point).ToString("F2")+" sep="+p.separation.ToString("F3");}
            seen.Add(line);
        }
        public string[] Take(){var a=seen.ToArray();seen.Clear();return a;}
    }
}
