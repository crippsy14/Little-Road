using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The game's mixer (docs/47 stage 4): which cars get a live synthesiser voice (the player and the nearest
    // others), the volume settings, and nothing at all in batch mode (race studies stay silent and unchanged).
    public sealed class AudioDirector : MonoBehaviour
    {
        public const int LiveCars=8;
        // The engine sound test mutes every car while it plays.
        public static volatile bool Muted;
        public static bool Enabled=>!Application.isBatchMode&&!RaceTrial.Has("--no-audio");
        // --silent: everything still plays and is captured (--audio-capture taps the synth before the listener), but nothing
        // reaches the speakers. Every automated run passes it, so test runs don't sound over the player's PC (Richard, 2 Oct).
        public static bool Silent=>RaceTrial.Has("--silent")||RaceTrial.Has("--audio-capture-silent");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void MuteAtStart(){if(Silent)AudioListener.volume=0;}
        static AudioDirector instance;
        static readonly List<CarAudio> cars=new();
        readonly List<(float,CarAudio)> ranked=new();
        float nextRank;
        // Everything but the interface fades in with the scene: silent until a race's countdown starts, then up over
        // 1.5 s; in free roam over 2 s after the cars appear (Richard, 1 Oct: the grid "loading in" was a jumpscare).
        public static float SceneFade {get;private set;}=0;
        // The player's road speed (ambience wildlife fades out with it).
        public static float PlayerSpeed;
        RaceTrial race;float nextRaceLook,sceneStart=-1,lastElapsed=float.NaN;ToneSynth tones;
        sealed class ToneVoice:MonoBehaviour
        {
            public ToneSynth synth;
            void OnAudioFilterRead(float[] data,int channels)
            {
                var t=synth;if(t==null){System.Array.Clear(data,0,data.Length);return;}
                for(int i=0,k=0;i<data.Length/channels;i++){float x=t.Next();for(int c=0;c<channels;c++,k++)data[k]*=x;}
            }
        }
        void Start()
        {
            int fs=AudioSettings.outputSampleRate;tones=new ToneSynth(fs);
            var go=new GameObject("Interface tones");go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>();var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
            var clip=AudioClip.Create("Tone carrier",fs,1,fs,false);clip.SetData(ones,0);source.clip=clip;source.loop=true;source.spatialBlend=0;source.priority=0;
            source.ignoreListenerPause=true;go.AddComponent<ToneVoice>().synth=tones;source.Play();
        }
        void SceneAndCountdown()
        {
            if(Time.unscaledTime>=nextRaceLook||race==null) {
                nextRaceLook=Time.unscaledTime+.5f;var found=FindAnyObjectByType<RaceTrial>();
                if(found!=race){race=found;sceneStart=Time.unscaledTime;lastElapsed=float.NaN;}
            }
            if(race!=null&&!race.freeRoam) {
                SceneFade=race.WaitingForReady?0:Mathf.Clamp01((race.elapsed+3)/1.5f);
                // Beep as each number shows (3, 2, 1), then a long, higher one for GO.
                float e=race.elapsed;
                if(!race.WaitingForReady&&!float.IsNaN(lastElapsed))
                    foreach(float mark in new[]{-2.99f,-2f,-1f,0f})if(lastElapsed<mark&&e>=mark){if(mark<0)tones?.Play(880,.14f);else tones?.Play(1760,.6f);}
                lastElapsed=race.WaitingForReady?-3.5f:e;
            }
            else SceneFade=sceneStart<0?0:Mathf.Clamp01((Time.unscaledTime-sceneStart-.5f)/2f);
        }

        // Interface beeps outside the countdown (speed trap flash, challenge starts).
        public static void Beep(float hz,float seconds){if(instance!=null)instance.tones?.Play(hz,seconds);}
        public static void Register(CarAudio car){cars.Add(car);Ensure();}
        public static void Unregister(CarAudio car){cars.Remove(car);}
        public static void Attach(RoadCar car,bool player){if(Enabled)car.gameObject.AddComponent<CarAudio>().Setup(car,player);}
        public static void Ensure()
        {
            if(instance!=null||!Enabled)return;
            var go=new GameObject("Audio director");DontDestroyOnLoad(go);instance=go.AddComponent<AudioDirector>();Ambience.Ensure();Music.Ensure();
            Apply();
        }
        // Settings changed (or first start): master on the listener, the rest per voice.
        public static void Apply()
        {
            AudioListener.volume=Silent?0:AudioOptions.Level(AudioOptions.Master);
            float effects=AudioOptions.Level(AudioOptions.Effects);
            foreach(var car in cars)car.SetVolumes(effects,effects);
        }

        void Update()
        {
            SceneAndCountdown();
            if(Time.unscaledTime<nextRank)return;nextRank=Time.unscaledTime+.25f;
            var listener=Listener();Vector3 at=listener!=null?listener.position:Vector3.zero;
            ranked.Clear();
            for(int i=cars.Count-1;i>=0;i--){var c=cars[i];if(c==null||c.car==null){cars.RemoveAt(i);continue;}ranked.Add(((c.Player?-1:(c.transform.position-at).sqrMagnitude),c));}
            ranked.Sort((a,b)=>a.Item1.CompareTo(b.Item1));
            for(int i=0;i<ranked.Count;i++)ranked[i].Item2.SetLive(i<LiveCars&&ranked[i].Item1<320*320);
        }
        static Transform cachedListener;
        static Transform Listener()
        {
            if(cachedListener!=null&&cachedListener.gameObject.activeInHierarchy)return cachedListener;
            var l=FindAnyObjectByType<AudioListener>();cachedListener=l!=null?l.transform:null;return cachedListener;
        }
    }

    // Sound settings beside the graphics ones (same PlayerPrefs pattern).
    public static class AudioOptions
    {
        static readonly string[] Steps={"0%","10%","20%","30%","40%","50%","60%","70%","80%","90%","100%"};
        public static readonly GraphicsOptions.Option Master=new("vol_master","Master volume",10,Steps);
        public static readonly GraphicsOptions.Option Effects=new("vol_effects","Engines & effects",10,Steps);
        public static readonly GraphicsOptions.Option Music=new("vol_music","Music",7,Steps);
        public static readonly GraphicsOptions.Option[] All={Master,Effects,Music};
        public static float Level(GraphicsOptions.Option o){float x=o.Index/10f;return x*x;} // perceptual taper
    }
}
