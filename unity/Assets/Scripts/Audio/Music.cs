using UnityEngine;

namespace LittleRoad
{
    // Music (docs/49): a theme per place and a race version of it, composed in tools/music/compose.py and rendered
    // through FluidSynth into seamless loops (Resources/Music).
    //   Free roam: Bayview's theme on the island, the causeway and the halt; Hillcrest's inland. Crossfades over 4 s.
    //   Races: the track's race theme (Bayview's for tracks without their own), under the engines; its lift stem
    //   (melody, brighter percussion) fades in on the player's last lap.
    // Created with the rest of the sound by AudioDirector; silent in batch mode and during the engine sound test.
    public sealed class Music : MonoBehaviour
    {
        static Music instance;
        AudioSource front,back,lift;string playing;float swap=1;RaceTrial race;float nextLook;
        const float Explore=.6f,Racing=.45f;
        static readonly bool Probe=RaceTrial.Has("--audio-probe");float nextProbe;

        public static void Ensure(){if(instance!=null||!AudioDirector.Enabled)return;var go=new GameObject("Music");DontDestroyOnLoad(go);instance=go.AddComponent<Music>();}

        AudioSource Source(string name)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var s=go.AddComponent<AudioSource>();
            s.loop=true;s.spatialBlend=0;s.playOnAwake=false;s.priority=10;s.volume=0;s.ignoreListenerPause=true;return s;
        }
        void Awake(){front=Source("Music A");back=Source("Music B");lift=Source("Music lift");}

        void Update()
        {
            if(Time.unscaledTime>=nextLook||race==null){nextLook=Time.unscaledTime+.5f;race=FindAnyObjectByType<RaceTrial>();}
            string want=Theme(out bool racing,out bool lastLap);
            float level=AudioOptions.Level(AudioOptions.Music)*(racing?Racing:Explore)*AudioDirector.SceneFade*(AudioDirector.Muted?0:1);
            // A new theme starts on the other source; the two crossfade over 4 s.
            if(want!=null&&want!=playing) {
                playing=want;var clip=Resources.Load<AudioClip>("Music/"+want);
                (front,back)=(back,front);swap=0;
                if(clip!=null){front.clip=clip;front.time=0;front.volume=0;front.Play();}else front.Stop();
                var liftClip=racing?Resources.Load<AudioClip>("Music/"+want+"-lift"):null;
                if(liftClip!=null){lift.clip=liftClip;lift.timeSamples=0;lift.volume=0;lift.Play();}else lift.Stop();
            }
            float dt=Time.unscaledDeltaTime;swap=Mathf.Min(1,swap+dt/4);
            front.volume=level*swap;back.volume=level*(1-swap);if(swap>=1&&back.isPlaying)back.Stop();
            // The lift stem stays in step with its base loop and comes in on the last lap.
            if(lift.isPlaying&&front.isPlaying&&lift.clip.samples==front.clip.samples&&Mathf.Abs(lift.timeSamples-front.timeSamples)>256)lift.timeSamples=front.timeSamples;
            lift.volume=Mathf.MoveTowards(lift.volume,lastLap?level:0,dt*Mathf.Max(.05f,level)/3);
            if(Probe&&Time.unscaledTime>=nextProbe){nextProbe=Time.unscaledTime+3;Debug.Log($"LITTLE_ROAD_MUSIC_PROBE theme={playing} level={front.volume:0.000} lift={lift.volume:0.000} racing={racing} lastLap={lastLap}");}
        }

        string Theme(out bool racing,out bool lastLap)
        {
            racing=false;lastLap=false;
            if(race==null)return playing;
            if(!race.freeRoam) {
                racing=true;var player=race.cars.Count>0?race.cars[0]:null;
                lastLap=player!=null&&race.elapsed>0&&player.laps>=race.requiredLaps-1&&!player.finished;
                return race.TrackId=="hillcrest"?"hillcrest-race":"bayview-race";
            }
            var listener=FindAnyObjectByType<AudioListener>();if(listener==null)return playing;
            string area=AreaBanner.Area(listener.transform.position);
            if(area.Contains("TUNNEL"))return playing??"bayview-explore";   // between the two: keep what's playing
            return area.Contains("BAYVIEW")||area.Contains("CAUSEWAY")?"bayview-explore":"hillcrest-explore";
        }
    }
}
