using UnityEngine;

namespace LittleRoad
{
    // Puts the world ambience (AmbienceSynth) where it belongs (docs/47 stage 4):
    //   surf  - a stereo bed, as loud as the sea is near (the ground sampled in a ring round the listener);
    //   wind  - a stereo bed, stronger high up and out in the open;
    //   falls - a 3D source at the Silver Falls (the river's steepest drop);
    //   river - a 3D source that follows the nearest stretch of the Silver river;
    //   birds - recorded stereo beds (forest birds by day, river birds by the river; synthesised crickets at night), very
    //           faint and only when slow or stopped (Richard, 1 Oct: no 100 mph birds; synthesised calls sounded
    //           "inside your head"). Loops cut from field recordings by tools/ambience-loops.py.
    // Created by AudioDirector with the first car; silent in batch mode like the rest of the sound.
    public sealed class Ambience : MonoBehaviour
    {
        static Ambience instance;
        public static float SeaNear {get;private set;}
        Voice surf,wind,falls,river,crickets;AudioSource forest,riverBirds;
        static readonly bool Probe=RaceTrial.Has("--audio-probe");float nextProbe;
        float nextSurvey;Vector3 riverTarget;bool hasFalls;

        public static void Ensure(){if(instance!=null||!AudioDirector.Enabled)return;var go=new GameObject("World ambience");DontDestroyOnLoad(go);instance=go.AddComponent<Ambience>();}

        // One synthesised source (a constant carrier multiplied by the synth, as CarAudio does).
        sealed class Voice:MonoBehaviour
        {
            public System.Func<(float,float)> stereo;public System.Func<float> mono;public System.Action<int> block;public volatile float level;
            AudioSource source;
            public Voice Setup(bool spatial,float min,float max)
            {
                source=gameObject.AddComponent<AudioSource>();int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
                var clip=AudioClip.Create("Ambience carrier",fs,1,fs,false);clip.SetData(ones,0);source.clip=clip;source.loop=true;source.playOnAwake=false;
                source.spatialBlend=spatial?1:0;source.rolloffMode=AudioRolloffMode.Logarithmic;source.minDistance=min;source.maxDistance=max;source.dopplerLevel=0;source.priority=200;
                source.Play();return this;
            }
            float smooth;
            void OnAudioFilterRead(float[] data,int channels)
            {
                if(stereo==null&&mono==null){System.Array.Clear(data,0,data.Length);return;}   // (not set up yet)
                int frames=data.Length/channels;block?.Invoke(frames);float target=AudioDirector.Muted?0:level*AudioDirector.SceneFade;
                for(int i=0,k=0;i<frames;i++) {
                    smooth+=(target-smooth)*.0005f;
                    if(stereo!=null&&channels>=2){var (l,r)=stereo();data[k]*=l*smooth;data[k+1]*=r*smooth;for(int c=2;c<channels;c++)data[k+c]=0;k+=channels;}
                    else{float s=(mono!=null?mono():stereo().Item1)*smooth;for(int c=0;c<channels;c++,k++)data[k]*=s;}
                }
            }
        }
        Voice Make(string name,bool spatial,float min,float max)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);return go.AddComponent<Voice>().Setup(spatial,min,max);
        }

        void Start()
        {
            int fs=AudioSettings.outputSampleRate;
            var s=new SurfSynth(fs,1);surf=Make("Surf",false,1,1);surf.stereo=s.NextStereo;surf.block=s.Block;
            var w=new WindSynth(fs,2);wind=Make("Wind",false,1,1);wind.stereo=w.NextStereo;wind.block=f=>{w.Exposure=windExposure;w.Block(f);};
            var f1=new FallsSynth(fs,3);falls=Make("Silver Falls",true,18,420);falls.mono=f1.Next;falls.block=f1.Block;
            var r=new RiverSynth(fs,4);river=Make("Silver river",true,6,90);river.mono=r.Next;river.block=r.Block;
            forest=Bed("forest-birds");riverBirds=Bed("river-birds");
            var c=new CricketSynth(fs,7);crickets=Make("Crickets",false,1,1);crickets.stereo=c.NextStereo;crickets.block=c.Block;
            PlaceFalls();
        }
        volatile float windExposure=1;
        AudioSource Bed(string clip)
        {
            var go=new GameObject("Ambience bed "+clip);go.transform.SetParent(transform,false);var a=go.AddComponent<AudioSource>();
            a.clip=Resources.Load<AudioClip>("Ambience/"+clip);a.loop=true;a.spatialBlend=0;a.volume=0;a.priority=220;a.playOnAwake=false;
            if(a.clip!=null){a.time=Random.value*a.clip.length*.9f;a.Play();}return a;
        }

        // The Silver Falls: the steepest drop along the river line.
        void PlaceFalls()
        {
            var line=HillcrestRoute.River?.line;if(line==null||line.Length<3)return;
            int best=0;float drop=0;for(int i=0;i+1<line.Length;i++){float d=line[i].y-line[i+1].y;if(d>drop){drop=d;best=i;}}
            if(drop<1)return;hasFalls=true;falls.transform.position=(line[best]+line[best+1])*.5f;
        }

        void Update()
        {
            var listener=Listener();if(listener==null)return;Vector3 at=listener.position;
            if(Time.unscaledTime>=nextSurvey) {
                nextSurvey=Time.unscaledTime+.5f;Survey(at);
                if(Probe&&Time.unscaledTime>=nextProbe){nextProbe=Time.unscaledTime+3;Debug.Log($"LITTLE_ROAD_AMBIENCE_PROBE sea={SeaNear:0.00} exposure={windExposure:0.00} falls={(hasFalls?Vector3.Distance(at,falls.transform.position):-1):0}m river={(riverTarget!=Vector3.zero?Vector3.Distance(at,riverTarget):-1):0}m night={TimeOfDay.Night:0.00}");}
            }
            float effects=AudioOptions.Level(AudioOptions.Effects);
            surf.level=SeaNear*.9f*effects;
            wind.level=(.25f+.5f*windExposure)*effects;
            falls.level=hasFalls?effects:0;
            // The river voice slides along the water to the nearest point.
            river.transform.position=Vector3.Lerp(river.transform.position,riverTarget,Mathf.Min(1,Time.unscaledDeltaTime*2));
            river.level=riverTarget!=Vector3.zero?.8f*effects:0;
            // Wildlife: very faint, and only when crawling or stopped (gone by 12 m/s).
            float day=1-TimeOfDay.Night,slow=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,12,AudioDirector.PlayerSpeed));
            float calm=slow*AudioDirector.SceneFade*effects*(AudioDirector.Muted?0:1);
            float nearRiver=riverTarget!=Vector3.zero?Mathf.Clamp01(1-Vector3.Distance(at,riverTarget)/70):0;
            forest.volume=Mathf.MoveTowards(forest.volume,.12f*day*(1-.8f*SeaNear)*(1-nearRiver)*calm,Time.unscaledDeltaTime*.5f);
            riverBirds.volume=Mathf.MoveTowards(riverBirds.volume,.15f*day*nearRiver*calm,Time.unscaledDeltaTime*.5f);
            crickets.level=.5f*TimeOfDay.Night*(1-.8f*SeaNear)*slow*effects;
        }

        // How near the sea is (ground below sea level, or no ground, in a ring of samples) and how exposed the
        // listener is (height above the sea), and the nearest river point.
        void Survey(Vector3 at)
        {
            float sea=0,weight=0;
            foreach(float radius in new[]{20f,55f,120f})for(int k=0;k<12;k++) {
                float a=k*Mathf.PI/6+radius*.01f;var p=at+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);float w=1/radius;weight+=w;
                bool water=!Physics.Raycast(new Vector3(p.x,at.y+150,p.z),Vector3.down,out var hit,400,~0,QueryTriggerInteraction.Ignore)||hit.point.y<CoastalLandscape.SeaLevel+.4f;
                if(water)sea+=w;
            }
            SeaNear=Mathf.Clamp01(sea/weight*1.6f);
            windExposure=Mathf.Clamp01((at.y-CoastalLandscape.SeaLevel)/45f*.7f+SeaNear*.3f);
            var line=HillcrestRoute.River?.line;riverTarget=Vector3.zero;
            if(line!=null){float best=float.MaxValue;foreach(var p in line){float d=(p-at).sqrMagnitude;if(d<best){best=d;riverTarget=p;}}if(best>160*160)riverTarget=Vector3.zero;}
        }
        static Transform cached;
        static Transform Listener(){if(cached!=null&&cached.gameObject.activeInHierarchy)return cached;var l=FindAnyObjectByType<AudioListener>();cached=l!=null?l.transform:null;return cached;}
    }
}
