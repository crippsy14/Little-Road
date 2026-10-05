using System;
using Random=UnityEngine.Random;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The Blast Run (docs/59's "things to do"; Claude's addition, 5 Oct, doc 63). Dai the Dumper's dare on the quarry floor:
    // when the siren goes, race him up the haul road, over the crest and past Chapel Street's mouth (the safe line) before
    // the charge in the galleries goes off. The siren wails while you drive and winds down seven seconds before the shot;
    // then the shot (BlastSynth), a flash, a slate-grey dust cloud rolling off the benches, rock flung across the floor, and
    // boulders shaken down onto the haul road behind you. Out before the shot: cleared, with your time on the Records tab;
    // out before Dai: his 300 credits, once. Still in the zone at the shot: the dust swallows you.
    //
    // The times come from the road, as Beat the Train's do: the stock car's ideal time over the course (SpeedProfile),
    // Dai at 1.3 times it and the shot at 1.6 times it (a road car on gravel loses grip; doc 62's rallycross).
    public sealed class BlastRun : MonoBehaviour
    {
        public const string Id="blast_run";public const int Prize=300;
        public static bool Known(string id)=>id==Id;
        public const float DaiFactor=1.3f,ShotFactor=1.6f,Countdown=3;
        // The course, bottom to top, and its numbers (Plan, once the roads stand).
        public static readonly List<Vector3> Path=new();
        public static float Length,IdealTime,DaiTime,ShotTime;
        public static Vector3 BlastAt,BenchAlong,ToFloor;public static (Vector3 at,float yaw) DaiSpot;
        public static bool Ready=>Path.Count>1;
        // Where you line up: on the haul road's foot, right of Dai, facing up the road.
        public static (Vector3 at,Quaternion rot) PlayerStart{get{var f=Path[3]-Path[0];f.y=0;f.Normalize();return (Path[0]+Vector3.Cross(Vector3.up,f)*1.9f,Quaternion.LookRotation(f));}}
        public static void Plan()
        {
            Path.Clear();if(!QuarryHills.Enabled)return;
            var haul=HillcrestRoute.Find(QuarryHills.HaulRoadName);if(haul==null)return;
            float safe=haul.Nearest(QuarryHills.ChapelFrom,out _)+10,start=haul.Length-18;
            for(float s=start;s>safe;s-=2)Path.Add(haul.At(s));Path.Add(haul.At(safe));
            Length=SpeedProfile.Length(Path);IdealTime=SpeedProfile.Time(Path,CarTuning.Stock);
            DaiTime=Mathf.Round(IdealTime*DaiFactor);ShotTime=Mathf.Round(IdealTime*ShotFactor);
            // The charge: in the big gallery face across Glas from the floor (in full view from the haul road), on a bench
            // halfway up; the shot runs along the bench.
            var toFloor=(QuarryHills.FloorCentre-QuarryHills.Pit).normalized;var b=QuarryHills.PitPoint(-toFloor,QuarryHills.Benches*QuarryHills.BenchWidth*.45f);b.y=ValleyField.Height(b.x,b.z);BlastAt=b;
            BenchAlong=new Vector3(-toFloor.y,0,toFloor.x);ToFloor=new Vector3(toFloor.x,0,toFloor.y);
            // Dai waits on the floor beside the haul road's foot, facing up it.
            float s0=haul.Length-8;var at=haul.At(s0)+haul.Right(s0)*7.5f;at.y=ValleyField.Height(at.x,at.z);
            DaiSpot=(at,Quaternion.LookRotation(-haul.Forward(s0)).eulerAngles.y);
            Debug.Log($"LITTLE_ROAD_BLAST_RUN course={Length:0}m ideal={IdealTime:0.0}s dai={DaiTime:0}s shot={ShotTime:0}s charge=({BlastAt.x:0},{BlastAt.y:0},{BlastAt.z:0})");
        }

        enum Phase{Idle,Countdown,Running,Aftermath}
        Phase phase;float go,crossedAt=-1,cleanupAt=-1;bool shotFired,daiHome;
        RoadCar player;GameObject dai,daiResident,haulTraffic;Rigidbody daiBody;float[] daiSpeed;float daiDistance;float[] along;
        readonly List<GameObject> debris=new();BlastDust dust;Light flash;
        public bool Active=>phase==Phase.Countdown||phase==Phase.Running;
        public bool Showing=>phase!=Phase.Idle;
        public float Elapsed=>phase==Phase.Running?Time.time-go:0;
        public bool LastCleared,LastBeatDai,LastShot;public float LastTime;
        // Reported once: when you cross the safe line (time, beat Dai) and at the shot (caught or not).
        public event Action<float,bool> Crossed;public event Action<bool> Shot;

        public void Begin(RoadCar car,GameObject resident)
        {
            if(!Ready||Active)return;Clear();
            player=car;daiResident=resident;LastCleared=LastBeatDai=LastShot=false;LastTime=0;crossedAt=-1;shotFired=false;daiHome=false;
            // (The caller puts the car at PlayerStart.)
            if(daiResident!=null)daiResident.SetActive(false);
            // Emrys's dumper works the haul road: he waits in the cab until it's over.
            haulTraffic=GameObject.Find("Emrys's dumper");if(haulTraffic!=null)haulTraffic.SetActive(false);
            dai=TrialWorld.Car("hauler",Toy.Hex(0xf1c40f));dai.name="Dai racing";var box=dai.AddComponent<BoxCollider>();box.center=new Vector3(0,1.1f,0);box.size=new Vector3(2.3f,2.2f,5.2f);dai.tag="Respawn";
            daiBody=dai.AddComponent<Rigidbody>();daiBody.isKinematic=true;daiBody.interpolation=RigidbodyInterpolation.Interpolate;
            // Dai drives the stock car's ideal profile, slowed to his time; his line is 1.9 m left of the middle.
            along=new float[Path.Count];for(int i=1;i<Path.Count;i++)along[i]=along[i-1]+Vector3.Distance(Path[i-1],Path[i]);
            var ideal=SpeedProfile.Speeds(Path,CarTuning.Stock);daiSpeed=new float[ideal.Length];for(int i=0;i<ideal.Length;i++)daiSpeed[i]=Mathf.Max(2,ideal[i]*IdealTime/DaiTime);
            daiDistance=0;PoseDai();
            phase=Phase.Countdown;go=Time.time+Countdown;
            // The siren starts with the countdown; the shot lands ShotTime after GO.
            Ambience.FireBlast(BlastAt+Vector3.up*10,ShotTime+Countdown);
        }
        public void Cancel(){if(phase==Phase.Idle)return;phase=Phase.Idle;Clear();}

        void PoseDai()
        {
            int i=0;while(i<along.Length-2&&along[i+1]<daiDistance)i++;
            float k=Mathf.InverseLerp(along[i],along[i+1],daiDistance);var p=Vector3.Lerp(Path[i],Path[i+1],k);
            var f=Path[Mathf.Min(i+2,Path.Count-1)]-Path[Mathf.Max(i-1,0)];f.y=0;f.Normalize();var left=-Vector3.Cross(Vector3.up,f);
            p+=left*1.9f;p.y=ValleyField.Height(p.x,p.z)+.02f;var rot=Quaternion.LookRotation(f);
            if(daiBody!=null&&daiBody.isKinematic&&phase==Phase.Running){daiBody.MovePosition(p);daiBody.MoveRotation(rot);}else dai.transform.SetPositionAndRotation(p,rot);
        }
        int SpeedIndex(){int i=0;while(i<along.Length-1&&along[i+1]<daiDistance)i++;return i;}

        void FixedUpdate()
        {
            if(phase!=Phase.Running||dai==null||daiHome)return;
            daiDistance+=daiSpeed[SpeedIndex()]*Time.fixedDeltaTime;
            if(daiDistance>=Length){daiDistance=Length;daiHome=true;}
            PoseDai();
        }
        void Update()
        {
            if(phase==Phase.Countdown){if(Time.time>=go){phase=Phase.Running;go=Time.time;}return;}
            if(phase==Phase.Running) {
                float t=Time.time-go;
                // The safe line: within 9 m of the course's top end, or past it along the road.
                if(crossedAt<0&&player!=null) {
                    var p=player.body.position;var end=Path[^1];var dir=Path[^1]-Path[^2];dir.y=0;
                    if(Vector2.Distance(new(p.x,p.z),new(end.x,end.z))<9||Vector3.Dot(new Vector3(p.x-end.x,0,p.z-end.z),dir)>0&&Vector2.Distance(new(p.x,p.z),new(end.x,end.z))<20) {
                        crossedAt=t;LastCleared=true;LastTime=t;LastBeatDai=!daiHome;Crossed?.Invoke(t,LastBeatDai);
                    }
                }
                if(t>=ShotTime&&!shotFired){shotFired=true;Fire();}
                return;
            }
            if(phase==Phase.Aftermath&&Time.time-go>ShotTime+16){phase=Phase.Idle;Clear(false);cleanupAt=Time.time+90;}
            // The boulders stay on the haul road a while (for the photo), then the quarry clears them.
            if(phase==Phase.Idle&&cleanupAt>0&&Time.time>cleanupAt){cleanupAt=-1;foreach(var d in debris)if(d!=null)Destroy(d);debris.Clear();if(haulTraffic!=null)haulTraffic.SetActive(true);}
        }

        // The shot: the flash, the dust, the flung rock, the rockfall behind you, the jolt.
        void Fire()
        {
            LastShot=true;phase=Phase.Aftermath;bool caught=crossedAt<0;
            if(flash==null){flash=new GameObject("Blast flash").AddComponent<Light>();flash.type=LightType.Point;flash.color=new Color(1,.86f,.62f);flash.range=420;flash.shadows=LightShadows.None;}
            flash.transform.position=BlastAt+Vector3.up*25;flash.intensity=40;
            if(dust==null){dust=new GameObject("Blast dust").AddComponent<BlastDust>();dust.Setup();}
            dust.Burst(BlastAt,BenchAlong,Path[0],caught&&player!=null?player.body.position:(Vector3?)null);
            // Rock flung out of the face across the floor (visual only; it settles and goes).
            var rng=new System.Random(Mathf.RoundToInt(Time.time*100));float R()=>(float)rng.NextDouble();
            for(int i=0;i<34;i++) {
                var rock=BayviewKit.Spawn(R()<.5f?"rock_a":"rock_b",BlastAt+BenchAlong*((R()-.5f)*90)+Vector3.up*(4+R()*8),R()*360,null,null,null,false,.8f+R()*1.6f);if(rock==null)break;
                var fly=rock.AddComponent<FlungRock>();
                fly.velocity=Quaternion.Euler(0,(R()-.5f)*70,0)*ToFloor*(16+R()*30)+Vector3.up*(12+R()*22);fly.spin=new Vector3(R(),R(),R())*400;debris.Add(rock);
            }
            // The shot shakes boulders off the cutting onto the haul road's lower reach: the way back down is blocked.
            int dropped=0;
            for(int i=0;i<16;i++) {
                int k=Mathf.Clamp((int)(Path.Count*.05f+R()*Path.Count*.3f),0,Path.Count-1);var at=Path[k];var f=Path[Mathf.Min(k+1,Path.Count-1)]-Path[Mathf.Max(k-1,0)];f.y=0;f.Normalize();
                at+=Vector3.Cross(Vector3.up,f)*((R()-.5f)*6);if(player!=null&&Vector3.Distance(at,player.body.position)<12)continue;
                var boulder=BayviewKit.Spawn(R()<.5f?"rock_a":"rock_b",at+Vector3.up*(12+R()*8),R()*360,null,null,null,true,.55f+R()*.5f);if(boulder==null)break;
                var body=boulder.AddComponent<Rigidbody>();body.mass=1800;body.angularVelocity=new Vector3(R(),R(),R())*3;debris.Add(boulder);dropped++;
            }
            float d=player!=null?Vector3.Distance(player.body.position,BlastAt):600;CameraShake.Kick(Mathf.Lerp(1,.35f,Mathf.InverseLerp(150,700,d)));
            if(dai!=null&&!daiHome){daiHome=true;}
            Debug.Log($"LITTLE_ROAD_BLAST_RUN shot caught={caught} crossed={crossedAt:0.0} dai={DaiTime:0} boulders={dropped}");
            Shot?.Invoke(caught);
        }
        void LateUpdate(){if(flash!=null&&flash.intensity>0)flash.intensity=Mathf.Max(0,flash.intensity-Time.deltaTime*70);}

        void Clear(bool removeDebris=true)
        {
            if(dai!=null)Destroy(dai);dai=null;daiBody=null;
            if(daiResident!=null)daiResident.SetActive(true);
            if(haulTraffic!=null&&cleanupAt<0&&removeDebris)haulTraffic.SetActive(true);
            if(!removeDebris)return;
            foreach(var d in debris)if(d!=null)Destroy(d);debris.Clear();
        }
        void OnDestroy(){foreach(var d in debris)if(d!=null)Destroy(d);}
    }

    // A rock flung by the shot: ballistic with a tumble, then it settles on the ground and stays a while.
    public sealed class FlungRock:MonoBehaviour
    {
        public Vector3 velocity,spin;float settled=-1;
        void Update()
        {
            if(settled>=0){if(Time.time-settled>40)Destroy(gameObject);return;}
            velocity+=Physics.gravity*Time.deltaTime;transform.position+=velocity*Time.deltaTime;transform.Rotate(spin*Time.deltaTime,Space.Self);
            var p=transform.position;float g=ValleyField.Height(p.x,p.z);
            if(p.y<g&&velocity.y<0){p.y=g;transform.position=p;settled=Time.time;}
        }
    }

    // The shot's dust: soft slate-grey puffs boiling up off the benches and rolling out across the floor; if you were
    // caught, a cloud round the car as well.
    public sealed class BlastDust:PuffPool
    {
        static readonly Color Slate=new(.66f,.67f,.70f);
        Vector3 at,along,toward;Vector3? around;float started=-1;int burst,front,near;
        public void Setup(){soft=true;material=SoftMaterial("Blast dust",Slate);}
        public void Burst(Vector3 where,Vector3 bench,Vector3 target,Vector3? player){at=where;along=bench;toward=target;around=player;started=Time.time;burst=front=near=0;}
        Color Shade()=>Color.Lerp(Slate,new Color(.52f,.53f,.56f),Random.value);
        void Update()
        {
            Step(Time.deltaTime);if(started<0)return;float t=Time.time-started;
            // The shot: a line of dust bursting out along 160 m of the bench, then boiling up into a column that climbs ~90 m
            // over six seconds (read from the haul road, 1.2 km off: the puffs are 60-140 m across).
            for(int want=Mathf.Min(130,(int)(t<1?t*60:60+(t-1)*14));burst<want;burst++) {
                float rise=Mathf.Min(6,t)*15;var out_=(toward-at);out_.y=0;out_.Normalize();
                var p=at+along*Random.Range(-80f,80f)*(1-Mathf.Min(1,t/8)*.5f)+out_*Random.Range(0,30)+Vector3.up*(Random.Range(0,15)+rise);
                Emit(p,out_*Random.Range(4,12)+Vector3.up*Random.Range(6,14),Random.Range(60f,140f)*(t<1?.8f:1.1f),Random.Range(20f,30f),1.7f,.9f,Shade());
            }
            // Then a rolling front of dust across the floor towards the haul road's foot over 14 s.
            for(int want=Mathf.Min(90,(int)(Mathf.Max(0,t-1)*6.5f));front<want;front++) {
                float k=Mathf.Clamp01((t-1)/14);var c=Vector3.Lerp(at,toward,k);var side=new Vector3(-(toward-at).z,0,(toward-at).x).normalized;var p=c+side*Random.Range(-70f,70f);
                p.y=Mathf.Max(ValleyField.Height(p.x,p.z),QuarryHills.FloorY)+Random.Range(4,12);
                Emit(p,(toward-at).normalized*4+Vector3.up*1.5f,Random.Range(40f,75f),Random.Range(10f,15f),1.3f,.7f,Shade());
            }
            // Caught: a cloud round the car.
            if(around.HasValue)for(int want=Mathf.Min(30,(int)(t*10));near<want;near++)Emit(around.Value+Random.insideUnitSphere*7+Vector3.up*2,Random.insideUnitSphere*2,Random.Range(9f,16f),Random.Range(7f,11f),1.4f,1,Slate);
            if(t>16)started=-1;
        }
    }
}
