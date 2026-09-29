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
            if(RaceTrial.Has("--hill-review")){yield return HillClimb(folder);yield break;}
            if(RaceTrial.Has("--route-drive")){yield return RouteDrive(folder);yield break;}
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
        // Drives up the windmill hill from four directions: records the car's pitch against the
        // ground slope under it, and whether it ever tipped (up.y < 0.6).
        // Bayview road junction -> causeway -> hill road -> Hillcrest square, steering at a point 10 m
        // along the route. Records arrival, time, road-grip share, recoveries and the lowest height.
        IEnumerator RouteDrive(string folder)
        {
            float routeSpeed=float.TryParse(RaceTrial.Arg("--route-speed"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float rs)?rs:26;
            var route=HillcrestRoute.DriveToCastle();float gateAt=-1,squareAt=-1;var gate=HillcrestRoute.Mainline.At(HillcrestRoute.Mainline.Length-26);
            // --climb-event: start on the climb's start line (the closure shuts the mainline behind it).
            if(RaceTrial.Has("--climb-event"))route.RemoveRange(0,HillcrestRoute.Index(route.ToArray(),gate));
            // --climb-from <n>: start n points further up (past the quay hairpin, which this autopilot overshoots).
            if(int.TryParse(RaceTrial.Arg("--climb-from"),out int from))route.RemoveRange(0,Mathf.Clamp(from,0,route.Count-10));
            var start=route[0]+Vector3.up*(.05f-car.ContactBottom);var heading=(route[3]-route[0]);heading.y=0;
            car.body.position=start;car.body.rotation=Quaternion.LookRotation(heading);car.body.linearVelocity=car.body.angularVelocity=Vector3.zero;
            car.transform.SetPositionAndRotation(start,car.body.rotation);Physics.SyncTransforms();
            // --climb-event [hard]: drive it with the Castle Climb closure (and obstacles) in place.
            if(RaceTrial.Arg("--climb-event") is "normal" or "hard")gameObject.AddComponent<CastleClimb>().Begin(RaceTrial.Arg("--climb-event")=="hard",car.transform,null);
            // Hard climb: steer into the free lane past each hay bale.
            var bales=new List<Vector3>();foreach(var b in FindObjectsByType<Transform>(FindObjectsSortMode.None))if(b.name=="hay_bale"&&b.parent!=null&&b.parent.name=="Castle Climb event")bales.Add(b.position);
            int target=0;float t=0,roadSteps=0,steps=0,lowest=99;int recoveriesBefore=car.recoveries;
            var hits=new List<string>();car.Bumped+=(other,speed)=>{if(speed>2)hits.Add($"{other.name}@{car.body.position}");};
            float reverseUntil=-1;int reversals=0;
            car.commandSource=c=>{
                if(t<reverseUntil)return new Vector2(-1,0);   // backing off a knock, as a driver would
                while(target<route.Count-1&&Vector3.Distance(Flat(c.body.position),Flat(route[target]))<10)target++;
                // Keep to the middle of the right-hand lane (valley roads are 7-8.5 m wide), as traffic does.
                var ahead=Flat(route[Mathf.Min(route.Count-1,target+1)]-route[target]).normalized;float lane=2.0f;var across=Vector3.Cross(Vector3.up,ahead);
                float nearest=32;foreach(var b in bales){var d=Flat(b-c.body.position);float along=Vector3.Dot(d,ahead);if(along>-3&&along<nearest&&Mathf.Abs(Vector3.Dot(d,across))<9){nearest=along;lane=Vector3.Dot(Flat(b-route[target]),across)>0?-2.1f:2.1f;}}
                var aim=route[target]+across*lane-c.body.position;aim.y=0;
                // Speed plan from the tightest bend in the next ~30 m (heading change per 10 m -> radius).
                float limit=routeSpeed; // --route-speed (drive benchmark: 20 m/s), default 26
                for(int k=target;k<Mathf.Min(route.Count-6,target+15);k++){float turn=Mathf.Abs(Vector3.SignedAngle(Flat(route[k+1]-route[k]),Flat(route[k+6]-route[k+5]),Vector3.up))*Mathf.Deg2Rad;float radius=10/Mathf.Max(turn,.01f);limit=Mathf.Min(limit,Mathf.Sqrt(5.5f*radius)+(k-target)*.35f);}
                float pedal=target>=route.Count-3?.4f:c.speed>limit+1?-.8f:c.speed>limit?0:1;
                return new Vector2(pedal,Mathf.Clamp(Vector3.SignedAngle(Flat(c.transform.forward),aim,Vector3.up)/25,-1,1));};
            float stalled=0;
            while(t<400&&Vector3.Distance(Flat(car.body.position),Flat(HillcrestRoute.CastleCourt))>16) {
                stalled=Mathf.Abs(car.speed)<.3f?stalled+Time.fixedDeltaTime:0;if(stalled>1.5f&&reversals<6&&t>reverseUntil+2){reverseUntil=t+1.4f;reversals++;stalled=0;}if(stalled>6){t=400;break;}   // stuck: stop and say on what
                if(gateAt<0&&Vector3.Distance(Flat(car.body.position),Flat(gate))<6)gateAt=t;if(steps%600==0)Debug.Log($"ROUTE_DRIVE_TRACE t={t:0} target={target}/{route.Count} at={car.body.position} speed={car.speed:0.0}");if(squareAt<0&&Vector3.Distance(Flat(car.body.position),Flat(HillcrestRoute.Square))<16)squareAt=t;
                // Drive benchmark: a chase-camera still every 20 s (body motion, fades, the look on the move).
                if(RaceTrial.Has("--frame-probe")&&steps%2400==1200){Toy.Capture(Path.Combine(folder,$"drive-{Mathf.RoundToInt(t):000}.png"));FrameProbe.Skip=3;}
                yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;steps++;if(car.Grip==car.tuning.roadGrip)roadSteps++;lowest=Mathf.Min(lowest,car.body.position.y);
            }
            car.commandSource=null;
            if(t>=400)foreach(var c in Physics.OverlapSphere(car.body.position,4.5f))if(!c.transform.IsChildOf(car.transform))hits.Add($"stuck near {c.name} at {c.transform.position} ({(c.transform.parent!=null?c.transform.parent.name:"")})");
            foreach(var b in bales)hits.Add($"bale at {b}");
            string line=$"ROUTE_DRIVE arrived={t<400} seconds={t:0.0} gate={gateAt:0.0} square={squareAt:0.0} castleClimb={t-gateAt:0.0} roadGripShare={roadSteps/Mathf.Max(1,steps):0.00} reversals={reversals} recoveries={car.recoveries-recoveriesBefore} lowestY={lowest:0.0} final={car.body.position}";
            File.WriteAllText(Path.Combine(folder,"route-drive.txt"),line+"\n"+string.Join("\n",hits));Debug.Log(line);Application.Quit();
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        IEnumerator HillClimb(string folder)
        {
            var lines=new List<string>();var top=new Vector3(-24,0,12);
            foreach(float bearing in new[]{0f,90f,180f,270f}) {
                var dir=Quaternion.Euler(0,bearing,0)*Vector3.forward;var start=top-dir*45;
                if(!Physics.Raycast(start+Vector3.up*80,Vector3.down,out var hit,160)){lines.Add("no ground "+bearing);continue;}
                start=hit.point+Vector3.up*(.03f-car.ContactBottom);
                car.body.position=start;car.body.rotation=Quaternion.LookRotation(dir);car.body.linearVelocity=car.body.angularVelocity=Vector3.zero;
                car.transform.SetPositionAndRotation(start,car.body.rotation);Physics.SyncTransforms();
                car.commandSource=_=>new Vector2(1,0);float t=0,maxPitch=0,maxSlope=0,minUp=1,rise=0;float y0=start.y;
                while(t<7){yield return new WaitForFixedUpdate();t+=Time.fixedDeltaTime;
                    float pitch=Vector3.SignedAngle(Vector3.ProjectOnPlane(car.transform.forward,Vector3.up),car.transform.forward,car.transform.right);
                    maxPitch=Mathf.Max(maxPitch,Mathf.Abs(pitch));minUp=Mathf.Min(minUp,car.transform.up.y);rise=Mathf.Max(rise,car.body.position.y-y0);
                    if(Physics.Raycast(car.body.position+Vector3.up*2,Vector3.down,out var g,5)&&!g.collider.transform.IsChildOf(car.transform))maxSlope=Mathf.Max(maxSlope,Vector3.Angle(g.normal,Vector3.up));}
                lines.Add($"bearing={bearing} rise={rise:0.0}m maxPitch={maxPitch:0.0} maxSlope={maxSlope:0.0} minUp={minUp:0.00} tipped={minUp<.6f}");
            }
            car.commandSource=null;File.WriteAllLines(Path.Combine(folder,"hill-review.txt"),lines);
            Debug.Log("HILL_REVIEW "+string.Join(" | ",lines));Application.Quit();
        }
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
