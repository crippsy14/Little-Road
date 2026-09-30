using UnityEngine;

namespace LittleRoad
{
    // One car's sound (docs/47 stage 4): its engine synthesiser and its tyres, road, wind and impacts, played
    // through a 3D AudioSource. The source plays a constant clip; OnAudioFilterRead multiplies it by the synthesised
    // signal, so Unity's distance, panning and Doppler still apply. AudioDirector decides which cars have a live voice.
    public sealed class CarAudio : MonoBehaviour
    {
        public RoadCar car {get;private set;}
        public bool Player {get;private set;}
        AudioSource source;EngineSynth engine;CarNoiseSynth noise;
        volatile bool live;volatile float engineVolume=1,effectsVolume=1;
        int impacts;float impact;
        // --audio-probe: the audio thread's carrier level (Unity's distance and panning gains) and output level.
        static readonly bool Probe=RaceTrial.Has("--audio-probe");double probeIn,probeOut;int probeFrames;float nextProbe;
        static AudioClip dc;

        public void Setup(RoadCar owner,bool player)
        {
            car=owner;Player=player;int fs=AudioSettings.outputSampleRate;uint seed=(uint)(owner.driverId*7919+13);
            engine=BuildEngine(owner,seed);builtFor=owner.tuning.drivetrain;noise=new CarNoiseSynth(fs,player,seed);this.seed=seed;
            // Your own car is the loudest thing you hear: louder at the speaker, not inside the synthesiser (a boost
            // before its soft limiter distorted the pulls; Richard, 1 Oct). Other cars sit lower instead.
            source=gameObject.AddComponent<AudioSource>();source.clip=DcClip();source.loop=true;source.playOnAwake=false;
            source.volume=player?1:.6f;
            // The player's car is heard from the chase camera just behind it: mostly centred, no Doppler.
            source.spatialBlend=player?.45f:1;source.dopplerLevel=player?0:.6f;source.rolloffMode=AudioRolloffMode.Logarithmic;
            source.minDistance=player?12:7;source.maxDistance=320;source.priority=player?0:128;
            owner.Bumped+=(_,speed)=>{if(speed>1.5f){impacts++;impact=speed;}};
            AudioDirector.Register(this);
            if(player&&CapturePath!=null)capture=new float[AudioSettings.outputSampleRate*120];
        }
        void OnDestroy(){AudioDirector.Unregister(this);WriteCapture();}
        void OnApplicationQuit(){WriteCapture();}
        // --audio-capture <file.wav>: the player's engine and tyre signal as synthesised (before Unity's gains),
        // with revs, throttle, gear and load every 10 ms beside it (<file>.csv), to see exactly what was heard.
        static readonly string CapturePath=RaceTrial.Has("--audio-capture")?RaceTrial.Arg("--audio-capture"):null;
        float[] capture;int captured;readonly System.Text.StringBuilder captureLog=new();float nextLog;bool written;
        void WriteCapture()
        {
            if(capture==null||written)return;written=true;int n=Mathf.Min(captured,capture.Length);
            using(var w=new System.IO.BinaryWriter(System.IO.File.Create(CapturePath))) {
                int fs=AudioSettings.outputSampleRate;w.Write("RIFF".ToCharArray());w.Write(36+n*2);w.Write("WAVE".ToCharArray());w.Write("fmt ".ToCharArray());
                w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(fs);w.Write(fs*2);w.Write((short)2);w.Write((short)16);w.Write("data".ToCharArray());w.Write(n*2);
                for(int i=0;i<n;i++)w.Write((short)Mathf.Clamp(capture[i]*32767,-32768,32767));
            }
            System.IO.File.WriteAllText(System.IO.Path.ChangeExtension(CapturePath,".csv"),"time,rpm,gear,throttle,speed,boost,shifting,limiter\n"+captureLog);
            Debug.Log("LITTLE_ROAD_AUDIO_CAPTURE "+CapturePath+" seconds="+(n/(float)AudioSettings.outputSampleRate).ToString("0.0"));
        }

        static int cars(){int n=0;foreach(var c in FindObjectsByType<CarAudio>(FindObjectsSortMode.None))if(c.live)n++;return n;}
        uint seed;Drivetrain builtFor;
        static EngineSynth BuildEngine(RoadCar owner,uint seed){var e=new EngineSynth(Spec(owner),AudioSettings.outputSampleRate,seed);EngineBanks.Attach(e,seed);return e;}
        static EngineSoundSpec Spec(RoadCar owner)
        {
            var train=owner.tuning.drivetrain;
            if(train!=null)return EngineSoundSpec.From(train.engine,train.induction,train.box);
            var fit=EquipmentCatalog.StockLoadout();
            return EngineSoundSpec.From(EquipmentCatalog.Get(fit["engine"]),EquipmentCatalog.Get(fit["turbo"]),EquipmentCatalog.Get(fit["transmission"]));
        }
        static AudioClip DcClip()
        {
            if(dc!=null)return dc;int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
            dc=AudioClip.Create("Synth carrier",fs,1,fs,false);dc.SetData(ones,0);return dc;
        }

        // AudioDirector: give or take the live voice.
        public void SetLive(bool on)
        {
            if(on==live)return;live=on;
            if(on){source.enabled=true;if(!source.isPlaying)source.Play();}else source.Stop();
        }
        public void SetVolumes(float engines,float effects){engineVolume=engines;effectsVolume=effects;}

        void Update()
        {
            if(car==null)return;
            // Refitted (new engine, induction, gearbox or frame): a new voice, swapped in whole for the audio thread
            // (Richard, 1 Oct: the sound only changed after a loading screen).
            if(car.tuning.drivetrain!=builtFor){builtFor=car.tuning.drivetrain;engine=BuildEngine(car,seed);}
            if(Player)AudioDirector.PlayerSpeed=Mathf.Abs(car.speed);
            if(!live)return;
            var box=car.gears;float speed=car.speed,throttle=car.throttle;
            if(box!=null)engine.Set(box.Rpm,throttle,box.Boost,speed,box.Limiter,box.Shifting,Mathf.Max(0,box.TargetGear),box.Upshifts,box.Downshifts);
            else engine.Set(engine.spec.idle+Mathf.Abs(speed)*140,throttle,0,speed,false,false,2,0,0);
            // Tyres: lateral grip used (yaw rate x speed against the grip budget), sliding, and locking brakes.
            float budget=Mathf.Max(.1f,(car.Grip>0?car.Grip:car.tuning.roadGrip)*1.65f);
            float yawRate=car.body!=null?Mathf.Abs(car.body.angularVelocity.y):0;
            float used=yawRate*Mathf.Abs(speed)/budget,slide=Mathf.Abs(car.lateralSpeed);
            float brakeSlip=throttle< -.7f&&Mathf.Abs(speed)>6&&car.tuning.braking>budget?.55f:0;
            float slip=Mathf.Max(Mathf.Max((used-.82f)/.3f,(slide-.6f)/2.5f),brakeSlip);
            float dirt=car.Grip>0&&car.Grip<car.tuning.roadGrip*.9f?1:0;
            noise.Set(speed,slip,dirt,impacts,impact);
            if(capture!=null&&Time.time>=nextLog){nextLog=Time.time+.01f;captureLog.Append($"{captured/(float)AudioSettings.outputSampleRate:0.000},{(box!=null?box.Rpm:0):0},{(box!=null?box.Gear+1:0)},{throttle:0.00},{speed:0.0},{(box!=null?box.Boost:0):0.00},{(box!=null&&box.Shifting?1:0)},{(box!=null&&box.Limiter?1:0)}\n");}
            if(Probe&&Player&&Time.unscaledTime>=nextProbe) {
                nextProbe=Time.unscaledTime+2;int count=Mathf.Max(1,probeFrames);
                Debug.Log($"LITTLE_ROAD_AUDIO_PROBE rpm={(box!=null?box.Rpm:0):0} gear={(box!=null?box.Gear+1:0)} carrier={probeIn/count:0.000} out_rms={System.Math.Sqrt(probeOut/count):0.0000} live={cars()} rate={AudioSettings.outputSampleRate}");
                probeIn=probeOut=0;probeFrames=0;
            }
        }

        void OnAudioFilterRead(float[] data,int channels)
        {
            if(!live||AudioDirector.Muted){System.Array.Clear(data,0,data.Length);return;}
            int frames=data.Length/channels;var blockVoice=engine;blockVoice.Block(frames);noise.Block(frames);if(Probe)probeFrames+=data.Length;
            bool record=Player&&capture!=null;
            float fade=AudioDirector.SceneFade,ev=engineVolume*fade,fv=effectsVolume*fade;var voice=engine;
            for(int i=0,k=0;i<frames;i++) {
                float s=voice.Next()*ev+noise.Next()*fv;
                if(record&&captured<capture.Length)capture[captured++]=s;
                for(int c=0;c<channels;c++,k++){if(Probe)probeIn+=Mathf.Abs(data[k]);data[k]*=s;if(Probe)probeOut+=data[k]*data[k];}
            }
        }
    }

    // Recorded engines (grain banks in Resources/Engines, docs/47): loaded once and shared read-only by every voice.
    // --synth-engines keeps the modelled exhaust for comparison.
    public static class EngineBanks
    {
        static readonly System.Collections.Generic.Dictionary<string,GrainBank> cache=new();
        public static bool Off=>RaceTrial.Has("--synth-engines");
        public static GrainBank Get(string name)
        {
            if(Off||string.IsNullOrEmpty(name))return null;
            if(cache.TryGetValue(name,out var bank))return bank;
            var asset=Resources.Load<TextAsset>("Engines/"+name);bank=asset!=null?GrainBank.Read(asset.bytes):null;cache[name]=bank;return bank;
        }
        public static void Attach(EngineSynth synth,uint seed)
        {
            var bank=Get(synth.spec.recording);
            if(bank!=null)synth.UseGrains(new GrainSynth(bank,AudioSettings.outputSampleRate,seed,synth.spec.idle,synth.spec.redline));
        }
    }
}
