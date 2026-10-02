using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Fireworks over Bayview harbour (Claude's own addition, 30 Sep; docs/51). When it's properly dark a one-minute
    // show goes up from a barge in the harbour, then again every three minutes (nights run three times faster than
    // days, so a clock time would last seconds). Toy fireworks: chunky glowing spheres with bloom rather than fine
    // sparks, and a coloured light per burst on the water. Peonies, rings, gold willows that crackle, and a finale.
    // Sound: the launch whoosh at the barge, the boom arriving at the speed of sound (FireworkSynth).
    // --fireworks-now starts a show two seconds in (reviews).
    public sealed class Fireworks:MonoBehaviour
    {
        public static readonly Vector3 Barge=new(-30,0,-205);
        const float ShowLength=60,Between=180,Gravity=5.5f;
        float nextShow=8,showEnd=-1,nextRocket;bool finale;
        sealed class Rocket{public Transform t;public TrailRenderer trail;public Vector3 v;public float fuse;public int colour,kind;}
        sealed class Spark{public Transform t;public Vector3 v;public float age,life,size,drag,fall;}
        readonly List<Rocket> rockets=new();readonly List<Spark> sparks=new();readonly Stack<Transform> pool=new();
        readonly List<(Light light,float born,float strength)> lights=new();
        Material[] materials;Material trailMaterial;static readonly Color[] Colours={new(1,.2f,.25f),new(1,.72f,.2f),new(.25f,1,.6f),new(.3f,.65f,1),new(1,.4f,.85f),new(1,.97f,.9f)};
        FireworkSynth synth;AudioSource source;Transform root;bool forced;

        public void Setup()
        {
            root=new GameObject("Fireworks").transform;
            materials=new Material[Colours.Length];
            for(int i=0;i<Colours.Length;i++){// (Night adds ~1.3 stops of exposure and ACES bleaches bright highlights: at 7x or even 2.4x every colour came out white.
            // The real culprit was lighting: each burst's point light sits among its own sparks and lit them white. The sparks
            // are black with emission only, so their colour is exactly the emission. Night exposure lifts the dark sky about 5x,
            // so even 1.6x clipped to pure white (254,254,253 in the capture): 0.5x lands at ~2 after exposure, coloured and blooming.)
            var m=TrialWorld.Material("Firework "+i,Color.black,0,0,false);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Colours[i]*.5f);materials[i]=m;}
            trailMaterial=TrialWorld.Material("Firework trail",new Color(1,.8f,.5f)*.4f,0,.2f,false);trailMaterial.EnableKeyword("_EMISSION");trailMaterial.SetColor("_EmissionColor",new Color(1,.75f,.4f)*.5f);
            var barge=new Vector3(Barge.x,CoastalLandscape.SeaLevel,Barge.z);
            if(BayviewKit.Has("boat")){var boat=BayviewKit.Spawn("boat",barge,70,root);boat.name="Fireworks barge";}
            if(AudioDirector.Enabled) {
                var holder=new GameObject("Fireworks sound");holder.transform.SetParent(root);holder.transform.position=barge+Vector3.up*40;
                synth=new FireworkSynth(AudioSettings.outputSampleRate,11);source=holder.AddComponent<AudioSource>();
                int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;var clip=AudioClip.Create("Fireworks carrier",fs,1,fs,false);clip.SetData(ones,0);
                source.clip=clip;source.loop=true;source.spatialBlend=1;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=90;source.maxDistance=2500;source.dopplerLevel=0;source.priority=40;
                holder.AddComponent<Voice>().owner=this;
            }
            forced=RaceTrial.Has("--fireworks-now");if(forced)nextShow=2;
        }
        sealed class Voice:MonoBehaviour
        {
            public Fireworks owner;float effects=1;
            void Update()=>effects=AudioOptions.Level(AudioOptions.Effects);
            void OnAudioFilterRead(float[] data,int channels)
            {
                var s=owner!=null?owner.synth:null;if(s==null||AudioDirector.Muted){System.Array.Clear(data,0,data.Length);return;}
                float g=effects*AudioDirector.SceneFade*.9f;
                for(int i=0,k=0;i<data.Length/channels;i++){float x=s.Next()*g;for(int c=0;c<channels;c++,k++)data[k]*=x;}
            }
        }
        public bool Showing=>Time.time<showEnd;
        // Properly dark, not dusk (Richard, 1 Oct: odd sounds at dusk by the Bayview garage; a show started at 0.75
        // and boomed behind the listener). Announced when you can hear it, so the booms make sense.
        static bool Dark=>TimeOfDay.Night>.9f;
        public System.Action<string> Announce;

        void Update()
        {
            float now=Time.time,dt=Time.deltaTime;if(dt<=0)return;
            // A voice only through a show and its last booms.
            if(source!=null){bool want=now<showEnd+8;if(want&&!source.isPlaying)source.Play();else if(!want&&source.isPlaying)source.Stop();}
            if(!Showing&&now>=nextShow&&(Dark||forced)){showEnd=now+ShowLength;nextShow=showEnd+Between;nextRocket=now+.5f;finale=false;Debug.Log("LITTLE_ROAD_FIREWORKS show");
                var cam=Camera.main;if(cam!=null&&Vector3.Distance(cam.transform.position,Barge)<1500)Announce?.Invoke("Fireworks over Bayview harbour!");}
            else if(!Showing&&!Dark&&!forced)nextShow=Mathf.Min(nextShow,now+8);   // the first dark moment gets a show straight away
            if(Showing&&now>=nextRocket) {
                float left=showEnd-now;finale=left<9;
                int count=finale?Random.Range(2,4):1;for(int i=0;i<count;i++)Launch();
                nextRocket=now+(finale?Random.Range(.25f,.5f):Random.Range(.7f,1.8f));
            }
            for(int i=rockets.Count-1;i>=0;i--) {
                var r=rockets[i];r.v+=Vector3.down*Gravity*.35f*dt;r.t.position+=r.v*dt;r.fuse-=dt;
                if(r.fuse<=0){Burst(r);Recycle(r.t);Destroy(r.trail.gameObject);rockets.RemoveAt(i);}
            }
            for(int i=sparks.Count-1;i>=0;i--) {
                var s=sparks[i];s.age+=dt;if(s.age>=s.life){Recycle(s.t);sparks.RemoveAt(i);continue;}
                s.v+=Vector3.down*Gravity*s.fall*dt;s.v*=Mathf.Exp(-s.drag*dt);s.t.position+=s.v*dt;
                float k=1-s.age/s.life;s.t.localScale=Vector3.one*s.size*Mathf.Sqrt(k)*(1+.15f*Mathf.Sin(s.age*40+i));
            }
            for(int i=lights.Count-1;i>=0;i--){var (l,born,strength)=lights[i];float age=now-born;if(age>1.4f||l==null){if(l!=null)Destroy(l.gameObject);lights.RemoveAt(i);continue;}l.intensity=strength*Mathf.Exp(-age*3);}
        }
        Transform Ball(int colour,float size)
        {
            Transform t;
            if(pool.Count>0){t=pool.Pop();t.gameObject.SetActive(true);}
            else{var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);DestroyImmediate(go.GetComponent<Collider>());go.name="Firework spark";var r=go.GetComponent<Renderer>();r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;t=go.transform;t.SetParent(root,false);}
            t.GetComponent<Renderer>().sharedMaterial=materials[colour];t.localScale=Vector3.one*size;return t;
        }
        void Recycle(Transform t){t.gameObject.SetActive(false);pool.Push(t);}
        void Launch()
        {
            var from=new Vector3(Barge.x+Random.Range(-6f,6f),CoastalLandscape.SeaLevel+1.5f,Barge.z+Random.Range(-4f,4f));
            var r=new Rocket{colour=Random.Range(0,Colours.Length),kind=finale?Random.Range(0,3):Random.value<.2f?2:Random.value<.35f?1:0};
            r.t=Ball(r.colour==5?1:r.colour,.55f);r.t.position=from;
            r.v=new Vector3(Random.Range(-5f,5f),Random.Range(38f,48f),Random.Range(-5f,5f));r.fuse=Random.Range(2f,2.7f);
            var trailGo=new GameObject("Firework trail");trailGo.transform.SetParent(r.t,false);r.trail=trailGo.AddComponent<TrailRenderer>();
            r.trail.sharedMaterial=trailMaterial;r.trail.time=.6f;r.trail.startWidth=.3f;r.trail.endWidth=0;r.trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            rockets.Add(r);synth?.Play(FireworkSynth.Kind.Launch,Delay(from),.6f);
        }
        float Delay(Vector3 at){var cam=Camera.main;return cam==null?0:Vector3.Distance(cam.transform.position,at)/343f;}
        void Burst(Rocket r)
        {
            var at=r.t.position;int colour=r.colour;float size=finale?1.2f:1;
            int count=r.kind==1?56:r.kind==2?80:96;
            for(int i=0;i<count;i++) {
                Vector3 dir;
                if(r.kind==1){float a=i*Mathf.PI*2/count;dir=new Vector3(Mathf.Cos(a),Random.Range(-.08f,.08f),Mathf.Sin(a));}   // ring
                else dir=Random.onUnitSphere;
                bool willow=r.kind==2;
                var s=new Spark{v=dir*(willow?17:Random.Range(24f,28f))*size,life=willow?Random.Range(2.6f,3.4f):Random.Range(1.6f,2.2f),size=willow?1.1f:1.3f,drag=willow?1.1f:1.6f,fall=willow?.55f:.25f};
                s.t=Ball(willow?1:(i%7==0?5:colour),s.size);s.t.position=at;sparks.Add(s);
            }
            var light=new GameObject("Firework light").AddComponent<Light>();light.type=LightType.Point;light.range=220;light.color=Colours[r.kind==2?1:colour];light.transform.position=at;light.intensity=0;light.shadows=LightShadows.None;
            lights.Add((light,Time.time,finale?9:6));
            float delay=Delay(at);synth?.Play(FireworkSynth.Kind.Boom,delay,finale?1:.8f);if(r.kind==2)synth?.Play(FireworkSynth.Kind.Crackle,delay+.4f,.9f);
        }
    }
}
