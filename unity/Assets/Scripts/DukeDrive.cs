using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The Duke's drive (Claude's addition, docs/51): every few minutes of free roam Duke Rolo takes the Regale out of
    // the castle courtyard, down Via Crest, along the quay and into the headland tunnel, turns round out of sight and
    // drives back up to park. A taste of the top classes before you get there: the E-class V8 recording through a paddle
    // box, revving out in the low gears. Brisk but polite: he slows behind traffic and you, and toots when held up.
    // Shadow the Duke: stay within 45 m of him all the way down to the tunnel and he pays 250 credits (once; later runs
    // keep the best time). Kinematic, like the valley traffic; the cruisers give way to him.
    public sealed class DukeDrive:MonoBehaviour
    {
        public const string ShadowId="duke_shadow";public const int ShadowPrize=250;
        public static bool Known(string id)=>id==ShadowId;
        public static DukeDrive Instance{get;private set;}
        public static bool Away=>Instance!=null&&Instance.driving;
        public const float FirstDeparture=150,Between=330;
        // Read by --duke-drive-review.
        public bool Driving=>driving;public float Along=>s;public float Length=>run.Count>0?run[^1]:0;public float Speed=>speed;public int Gear=>gear+1;public float Rpm=>rpm;
        public int Toots{get;private set;}public float MaxSpeed{get;private set;}public float TurnAt=>turnS;public bool ShadowOk=>shadowing;

        RoadCar player;Func<bool> canGo;Action<string> notify;Func<AdventureStore> saves;Action<string,string> card;
        Vector3 home;Quaternion homeRot;bool driving,shadowing,announced;float next,s,speed,turnS,blockedFor,started,shadowGap;
        readonly List<Vector3> path=new();readonly List<float> run=new();readonly List<float> limit=new();
        WheelVisuals wheels;float spin;Rigidbody body;ResidentIdle idle;Horn horn;DukeEngine engine;
        int gear,ups,downs;float rpm,shiftUntil,groundY=float.NaN;

        public void Setup(RoadCar playerCar,Func<bool> allowed,Action<string> toast,Func<AdventureStore> store,Action<string,string> showCard)
        {
            Instance=this;player=playerCar;canGo=allowed;notify=toast;saves=store;card=showCard;
            home=transform.position;homeRot=transform.rotation;next=Time.time+FirstDeparture;
            wheels=new WheelVisuals(gameObject);idle=GetComponent<ResidentIdle>();
            horn=Horn.Attach(gameObject,.92f);
            var voice=new GameObject("Duke's engine");voice.transform.SetParent(transform,false);voice.transform.localPosition=new Vector3(0,.6f,-1.2f);
            engine=voice.AddComponent<DukeEngine>();engine.Setup();
            turnS=BuildRoute(home,homeRot,path,run,limit);
            Debug.Log($"LITTLE_ROAD_DUKE_DRIVE route={run[^1]:0}m turn={turnS:0}m");
        }
        void OnDestroy(){if(Instance==this)Instance=null;}

        // ---------------------------------------------------------------- the route
        // The route out and back, its run (metres) and speed limits; returns the run at the turn. (Static: the editor
        // checks build it too.)
        public static float BuildRoute(Vector3 home,Quaternion homeRot,List<Vector3> path,List<float> run,List<float> limit)
        {
            var full=HillcrestRoute.DriveToCastle().ToArray();var main=HillcrestRoute.Mainline;
            var turnAt=main.At((HillcrestRoute.PortalNorthRun+HillcrestRoute.PortalSouthRun)*.5f);
            int ti=HillcrestRoute.Index(full,turnAt),hi=HillcrestRoute.Index(full,home);
            // Out of his spot to the road through the courtyard, then down the route to the middle of the tunnel.
            var raw=new List<Vector3>{home,home+homeRot*Vector3.forward*4};
            for(int i=hi;i>=ti;i--)raw.Add(full[i]);
            var outLeg=Resample(Smooth(raw,3),1f);
            // Keep right: offset across the road (not in the courtyard or at the far end, where he turns).
            var outLane=Lane(outLeg,1.7f,22,30);
            var back=new List<Vector3>(outLeg);back.Reverse();var backLane=Lane(back,1.7f,30,26);
            path.Clear();path.AddRange(outLane);float turnS=0;
            for(int i=1;i<path.Count;i++)turnS+=Flat(path[i]-path[i-1]).magnitude;
            path.AddRange(backLane);
            run.Clear();run.Add(0);for(int i=1;i<path.Count;i++)run.Add(run[^1]+Mathf.Max(.01f,Flat(path[i]-path[i-1]).magnitude));
            // Speeds: 7 m/s2 across the corners (the Regale grips), 24 m/s at most in the valley, walking pace in the
            // courtyard and through the turn in the tunnel; 5.5 m/s2 to accelerate, 7 to brake.
            limit.Clear();
            for(int i=0;i<path.Count;i++) {
                int a=Mathf.Max(0,i-4),b=Mathf.Min(path.Count-1,i+4);
                float k=0;if(i>0&&i<path.Count-1){var d0=Flat(path[i]-path[a]);var d1=Flat(path[b]-path[i]);if(d0.sqrMagnitude>1e-3f&&d1.sqrMagnitude>1e-3f)k=Vector3.Angle(d0,d1)*Mathf.Deg2Rad/Mathf.Max(1,(d0.magnitude+d1.magnitude)*.5f);}
                // (20 m/s through town and on Via Crest; 24 out on the mainline.)
                float cap=HillcrestRoute.Distance(HillcrestRoute.Mainline.line,path[i].x,path[i].z,out _)<5?24:20;
                float v=k>1e-4f?Mathf.Sqrt(7/k):cap;v=Mathf.Min(v,cap);
                float r=run[i];if(r<30||r>run[^1]-30)v=Mathf.Min(v,5.5f);if(Mathf.Abs(r-turnS)<35)v=Mathf.Min(v,7);
                limit.Add(v);
            }
            limit[0]=0;limit[^1]=0;
            for(int i=limit.Count-2;i>=0;i--)limit[i]=Mathf.Min(limit[i],Mathf.Sqrt(limit[i+1]*limit[i+1]+2*7*(run[i+1]-run[i])));
            for(int i=1;i<limit.Count;i++)limit[i]=Mathf.Min(limit[i],Mathf.Sqrt(limit[i-1]*limit[i-1]+2*5.5f*(run[i]-run[i-1])));
            return turnS;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static List<Vector3> Smooth(List<Vector3> p,int rounds)
        {
            for(int r=0;r<rounds;r++){var q=new List<Vector3>{p[0]};for(int i=0;i+1<p.Count;i++){q.Add(Vector3.Lerp(p[i],p[i+1],.25f));q.Add(Vector3.Lerp(p[i],p[i+1],.75f));}q.Add(p[^1]);p=q;}
            return p;
        }
        static List<Vector3> Resample(List<Vector3> p,float step)
        {
            var o=new List<Vector3>{p[0]};float carry=0;
            for(int i=0;i+1<p.Count;i++){var a=p[i];var b=p[i+1];float l=Flat(b-a).magnitude;float t=step-carry;while(t<=l){o.Add(Vector3.Lerp(a,b,t/Mathf.Max(l,1e-4f)));t+=step;}carry=l-(t-step);}
            if(Flat(o[^1]-p[^1]).magnitude>.2f)o.Add(p[^1]);return o;
        }
        static List<Vector3> Lane(List<Vector3> p,float offset,float startClear,float endClear)
        {
            var o=new List<Vector3>(p.Count);float along=0,total=0;for(int i=1;i<p.Count;i++)total+=Flat(p[i]-p[i-1]).magnitude;
            for(int i=0;i<p.Count;i++) {
                if(i>0)along+=Flat(p[i]-p[i-1]).magnitude;
                var f=Flat(p[Mathf.Min(p.Count-1,i+2)]-p[Mathf.Max(0,i-2)]).normalized;var right=Vector3.Cross(Vector3.up,f);
                float w=Mathf.SmoothStep(0,1,Mathf.Clamp01((along-startClear)/12))*Mathf.SmoothStep(0,1,Mathf.Clamp01((total-endClear-along)/12));
                o.Add(p[i]+right*offset*w);
            }
            return o;
        }
        (Vector3 at,Vector3 fwd,float lim) Sample(float d)
        {
            int i=Mathf.Clamp(run.BinarySearch(d),-run.Count,run.Count-1);if(i<0)i=Mathf.Max(0,~i-1);i=Mathf.Min(i,run.Count-2);
            float t=Mathf.Clamp01((d-run[i])/Mathf.Max(1e-4f,run[i+1]-run[i]));
            int a=Mathf.Max(0,i-2),b=Mathf.Min(path.Count-1,i+3);
            return (Vector3.Lerp(path[i],path[i+1],t),(path[b]-path[a]).normalized,Mathf.Lerp(limit[i],limit[i+1],t));
        }

        // ---------------------------------------------------------------- driving
        public void Depart()
        {
            if(driving)return;driving=true;s=0;speed=0;gear=0;blockedFor=0;started=Time.time;shadowing=true;shadowGap=0;announced=false;MaxSpeed=0;Toots=0;
            if(idle!=null)idle.enabled=false;
            body=GetComponent<Rigidbody>();if(body==null){body=gameObject.AddComponent<Rigidbody>();}body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;
            RoadCruiser.GiveWayTo.Add(transform);engine.Run(true);
            if(player!=null&&Vector3.Distance(player.body.position,home)<900)notify?.Invoke("Duke Rolo is taking the Regale out for a spin!");
            if(horn!=null&&!horn.Busy)horn.Toot(.12f,.08f,.12f,0);
            Debug.Log("LITTLE_ROAD_DUKE_DRIVE depart");
        }
        // Home at once (the Castle Climb closes his road).
        public void Recall(){if(!driving)return;Park();}
        void Park()
        {
            driving=false;speed=0;transform.SetPositionAndRotation(home,homeRot);if(body!=null){Destroy(body);body=null;}
            RoadCruiser.GiveWayTo.Remove(transform);engine.Run(false);if(idle!=null)idle.enabled=true;wheels.Pose(spin,0);next=Time.time+Between;
            Debug.Log($"LITTLE_ROAD_DUKE_DRIVE home after {Time.time-started:0.0}s max {MaxSpeed:0.0}m/s toots {Toots}");
        }
        void Update()
        {
            if(!driving&&Time.time>=next){if(canGo==null||canGo())Depart();else next=Time.time+20;}
        }
        void FixedUpdate()
        {
            if(!driving)return;float dt=Time.fixedDeltaTime;
            var (at,fwd,lim)=Sample(s);
            // Whatever is in his lane ahead: you, the valley traffic.
            float gap=99;Vector3 me=at,f=Flat(fwd).normalized,side=Vector3.Cross(Vector3.up,f);
            void Check(Vector3 p){var d=p-me;float along=Vector3.Dot(d,f),across=Mathf.Abs(Vector3.Dot(d,side));if(along>0&&along<24&&across<2.6f&&Mathf.Abs(d.y)<4)gap=Mathf.Min(gap,along);}
            if(player!=null)Check(player.body.position);
            foreach(var c in RoadCruiser.All)if(c!=null)Check(c.transform.position);
            float target=Mathf.Max(lim,1.5f);if(gap<24)target=Mathf.Min(target,Mathf.Max(0,(gap-7.5f)*1.1f));   // (a crawl at least: the limit is 0 at both ends)
            // Held up: a polite double toot after a moment and again every few seconds (negative = since the last).
            if(speed<1.2f&&gap<11){blockedFor+=dt;if(blockedFor>1.6f&&horn!=null&&!horn.Busy){horn.Toot(.14f,.1f,.14f,0);Toots++;blockedFor=-2.5f;}}else blockedFor=Mathf.Min(0,blockedFor+dt);
            float before=speed;speed=Mathf.MoveTowards(speed,target,(target<speed?7.5f:5.5f)*dt);
            s+=speed*dt;MaxSpeed=Mathf.Max(MaxSpeed,speed);
            if(s>=run[^1]-.3f){Park();return;}
            var (p,dir,_)=Sample(s);
            // Ground under him (roads carry a little lift; the courtyard's paving is a trigger, so the terrain/road is hit).
            float best=float.MinValue;
            foreach(var h in Physics.RaycastAll(p+Vector3.up*2.5f,Vector3.down,6))if(!h.collider.isTrigger&&h.collider.attachedRigidbody==null&&!h.collider.transform.IsChildOf(transform)&&h.point.y<p.y+1.2f)best=Mathf.Max(best,h.point.y);
            float y=best>float.MinValue?best:p.y;
            groundY=float.IsNaN(groundY)?y:Mathf.Lerp(groundY,y,1-Mathf.Exp(-dt*12));
            var pos=new Vector3(p.x,groundY+.02f,p.z);
            var look=Quaternion.LookRotation(Flat(dir).sqrMagnitude>1e-4f?dir:transform.forward);
            if(s>run[^1]-10)look=Quaternion.Slerp(look,homeRot,Mathf.InverseLerp(run[^1]-10,run[^1],s));
            body.MovePosition(pos);body.MoveRotation(Quaternion.Slerp(body.rotation,look,1-Mathf.Exp(-dt*8)));
            // Wheels: roll with the speed, steer into the bend ahead.
            spin+=speed/wheels.Radius*Mathf.Rad2Deg*dt;var (_,ahead,_)=Sample(s+4);
            wheels.Pose(spin,Mathf.Clamp(Vector3.SignedAngle(Flat(dir),Flat(ahead),Vector3.up)*1.6f,-24,24));
            DriveEngine((speed-before)/dt,target,dt);
            Shadow(dt);
        }
        // The paddle box: rpm per m/s in each gear; up at 8,500 rpm (or early when cruising), down when braking if the gear
        // below would sit under 7,600.
        static readonly float[] K={520,380,295,240,200,170,148};
        void DriveEngine(float accel,float target,float dt)
        {
            float r=speed*K[gear];
            // (Cruising, he short-shifts while the next gear stays above 3,600 rpm.)
            if(Time.time>shiftUntil+.4f&&gear<K.Length-1&&(r>8500||accel<.3f&&speed*K[gear+1]>3600)){gear++;ups++;shiftUntil=Time.time+.11f;}
            else if(Time.time>shiftUntil+.4f&&gear>0&&speed*K[gear-1]<7600&&(accel<-.5f||speed*K[gear]<3400)){gear--;downs++;shiftUntil=Time.time+.14f;}
            rpm=Mathf.Max(1300,speed*K[gear]);
            float thr=Mathf.Clamp01(accel>.2f?.45f+accel/5.5f*.55f:target>speed+.3f?.3f:0);
            engine.Set(rpm,thr,speed,Time.time<shiftUntil,gear,ups,downs);
        }
        // Shadow the Duke: within 45 m of him from the courtyard gate to the turn (a few seconds' slack in all).
        void Shadow(float dt)
        {
            if(!shadowing||player==null||s<40)return;
            if(Vector3.Distance(player.body.position,transform.position)>45){shadowGap+=dt;if(shadowGap>4)shadowing=false;}
            if(s>=turnS-2&&shadowing) {
                shadowing=false;float t=Time.time-started;int paid=0;
                try{paid=saves?.Invoke()?.RecordTime(ShadowId,Mathf.Min(t,499),ShadowPrize)??0;}catch(Exception e){Debug.LogException(e);}
                card?.Invoke("YOU KEPT UP WITH THE DUKE!",paid>0?$"\"Splendid driving!\" Duke Rolo pays you {paid} credits.":"\"Splendid as ever!\" Duke Rolo tips his hat.");
                Debug.Log($"LITTLE_ROAD_DUKE_DRIVE shadow t={t:0.0} paid={paid}");
            }
        }
    }

    // The Regale's voice: the E-class V8 recording through the paddle box, its own 3D source (an audio filter runs on
    // every source of its object, so it has a child of its own, as the horn does).
    public sealed class DukeEngine:MonoBehaviour
    {
        EngineSynth synth;AudioSource source;volatile bool live;
        static AudioClip carrier;
        public void Setup()
        {
            var spec=EngineSoundSpec.From(EquipmentCatalog.Get("engine_e_v8"),EquipmentCatalog.Get("turbo_stock"),EquipmentCatalog.Get("transmission_e_paddle"));
            synth=new EngineSynth(spec,AudioSettings.outputSampleRate,4242);EngineBanks.Attach(synth,4242);
            source=gameObject.AddComponent<AudioSource>();source.clip=Carrier();source.loop=true;source.playOnAwake=false;
            source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=9;source.maxDistance=420;source.dopplerLevel=.6f;source.priority=40;source.volume=.85f;
        }
        static AudioClip Carrier()
        {
            if(carrier!=null)return carrier;int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
            carrier=AudioClip.Create("Duke carrier",fs,1,fs,false);carrier.SetData(ones,0);return carrier;
        }
        public void Run(bool on){if(!AudioDirector.Enabled)return;live=on;if(on&&!source.isPlaying)source.Play();else if(!on)source.Stop();}
        public void Set(float rpm,float throttle,float speed,bool shifting,int gear,int ups,int downs)=>synth?.Set(rpm,throttle,0,speed,false,shifting,gear,ups,downs);
        void OnAudioFilterRead(float[] data,int channels)
        {
            if(!live||synth==null||AudioDirector.Muted){Array.Clear(data,0,data.Length);return;}
            int frames=data.Length/channels;synth.Block(frames);float fade=AudioDirector.SceneFade;
            for(int i=0,k=0;i<frames;i++){float x=synth.Next()*fade;for(int c=0;c<channels;c++,k++)data[k]*=x;}
        }
    }
}
