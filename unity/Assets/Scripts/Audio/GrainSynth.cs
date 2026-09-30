using System;
using System.Collections.Generic;
using System.IO;

namespace LittleRoad
{
    // A bank of one-cycle engine grains cut from a real recording by tools/engine-grains.py (docs/47 stage 4).
    // Each grain is one full engine cycle (two crank turns) tagged with its rpm and load (1 = pulling, 0 = overrun).
    // Version 2 keeps whole sweeps: runs of consecutive cycles from one real rev-up (pulling) or rev-down (coasting),
    // in time order, so playback can step through real consecutive cycles instead of scattered ones.
    public sealed class GrainBank
    {
        public int sampleRate,cylinders;public float idle,redline;
        public float[] rpm;public byte[] load;public int[] run;public float[][] samples;
        // Version 3: each grain is one cycle (cycle[i] samples) plus a tail, the start of the next cycle as recorded.
        public int[] cycle;
        public sealed class Sweep {public int id;public bool pulling;public float low,high;public int[] grains;}
        public readonly List<Sweep> sweeps=new();
        public Sweep[] sweepOf;   // per grain

        public static GrainBank Read(byte[] data)
        {
            using var r=new BinaryReader(new MemoryStream(data));
            if(new string(r.ReadChars(4))!="LRGB")throw new InvalidDataException("Not an engine grain bank");
            int version=r.ReadInt32();if(version<1||version>3)throw new InvalidDataException("Grain bank version "+version);
            var b=new GrainBank{sampleRate=r.ReadInt32(),cylinders=r.ReadInt32(),idle=r.ReadSingle(),redline=r.ReadSingle()};
            int n=r.ReadInt32();b.rpm=new float[n];b.load=new byte[n];b.run=new int[n];b.samples=new float[n][];b.cycle=new int[n];
            for(int i=0;i<n;i++) {
                b.rpm[i]=r.ReadSingle();b.load[i]=r.ReadByte();b.run[i]=version>=2?r.ReadUInt16():i+1;
                int cycleLength=version>=3?r.ReadInt32():-1,len=r.ReadInt32();b.cycle[i]=cycleLength>0?cycleLength:len;var g=new float[len];
                for(int k=0;k<len;k++)g[k]=r.ReadInt16()/32768f;b.samples[i]=g;
            }
            // Group into sweeps (grains of one run are stored in time order).
            var byRun=new Dictionary<int,List<int>>();
            for(int i=0;i<n;i++){if(!byRun.TryGetValue(b.run[i],out var list))byRun[b.run[i]]=list=new List<int>();list.Add(i);}
            b.sweepOf=new Sweep[n];
            foreach(var kv in byRun) {
                var s=new Sweep{id=kv.Key,pulling=b.load[kv.Value[0]]==1,grains=kv.Value.ToArray(),low=float.MaxValue,high=0};
                foreach(int g in s.grains){s.low=Math.Min(s.low,b.rpm[g]);s.high=Math.Max(s.high,b.rpm[g]);b.sweepOf[g]=s;}
                b.sweeps.Add(s);
            }
            // Widest sweeps first: the main rev-up and rev-down of the recording are preferred.
            b.sweeps.Sort((x,y)=>(y.high/y.low).CompareTo(x.high/x.low));
            return b;
        }

        // The next grain for these revs. Stay in the current sweep while it covers them, moving to the cycles
        // nearest the revs (never repeating the same cycle); otherwise the widest sweep that covers them.
        public int Next(int current,float target,bool pulling,ref Noise rng)
        {
            Sweep sweep=current>=0?sweepOf[current]:null;
            if(sweep==null||sweep.pulling!=pulling||target<sweep.low*.985f||target>sweep.high*1.015f) {
                sweep=null;
                foreach(var s in sweeps)if(s.pulling==pulling&&target>=s.low*.985f&&target<=s.high*1.015f){sweep=s;break;}
                // Then a sweep of the other kind at these revs (idle on throttle), before stretching a distant one.
                if(sweep==null)foreach(var s in sweeps)if(target>=s.low*.985f&&target<=s.high*1.015f){sweep=s;break;}
                // No sweep of this kind covers these revs: the nearest edge of any sweep of the kind (or either kind).
                if(sweep==null)sweep=Nearest(target,pulling)??Nearest(target,!pulling);
            }
            var list=sweep.grains;
            // The recorded next cycle whenever it suits these revs: it joins seamlessly (its start is the tail
            // just played). Otherwise the cycle nearest the revs, not the one just played (a held note would loop).
            int at=current>=0&&sweepOf[current]==sweep?Array.IndexOf(list,current):-1;
            if(at>=0&&at+1<list.Length&&Math.Abs(rpm[list[at+1]]/target-1)<.015f)return list[at+1];
            int best=0;float bestDiff=float.MaxValue;
            for(int i=0;i<list.Length;i++){float d=Math.Abs(rpm[list[i]]-target);if(d<bestDiff){bestDiff=d;best=i;}}
            if(list[best]==current)best=best+1<list.Length?best+1:Math.Max(0,best-1);
            return list[best];
        }
        Sweep Nearest(float target,bool pulling)
        {
            Sweep best=null;float bestDiff=float.MaxValue;
            foreach(var s in sweeps){if(s.pulling!=pulling)continue;float d=target<s.low?s.low-target:target>s.high?target-s.high:0;if(d<bestDiff){bestDiff=d;best=s;}}
            return best;
        }
        public float Top {get {float t=0;foreach(var r in rpm)t=Math.Max(t,r);return t;}}
        public float TopOf(bool pulling){float t=0;for(int i=0;i<rpm.Length;i++)if((load[i]==1)==pulling)t=Math.Max(t,rpm[i]);return t;}
        public float Bottom {get {float t=float.MaxValue;foreach(var r in rpm)t=Math.Min(t,r);return t;}}
    }

    // Plays a grain bank at any rpm: grains stretched to the exact cycle length for the target revs and overlapped
    // with an equal-power crossfade; pulling and coasting streams blended by load.
    public sealed class GrainSynth
    {
        readonly GrainBank bank;readonly float fs;Noise rng;
        sealed class Stream {public int cur=-1,prev=-1;public float pos,prevPos,fade=1,fadeStep;}
        // One stream (Richard, 1 Oct: revving up sounded like two engines at once). Mixing a pulling and a coasting
        // recording at part throttle played two unrelated stretches of engine together. Now the stream switches
        // between pulling and coasting sweeps (with hysteresis, through the tail crossfade), and part throttle only
        // shapes that one voice's level and brightness.
        readonly Stream stream=new();bool pullingMode;float bright,lpState;
        Biquad toneHigh,toneLow;float toneAt=-1,toneGain=1,levelIn,levelOut;bool toneOn;
        public static bool ToneCorrection=true;   // (the sound harness compares with and without)
        // Each grain plays at its own recorded revs where the recording covers them (the most natural sound);
        // above the recording's top the top grains are stretched to our exact cycle length.
        // Each kind has its own top (Richard, 30 Sep: the Tuner V6 thinned out in the top half of every gear). The
        // Skyline's pulls reach 3760 rpm and its lifts 3825: clamping a pull to 3825 found no pulling sweep there and
        // played the lift-off recording for the whole top half of the rev range.
        readonly float redline,srcLow,pullHigh,coastHigh;
        public GrainSynth(GrainBank grains,float sampleRate,uint seed,float targetIdle=0,float targetRedline=0)
        {
            bank=grains;fs=sampleRate;rng=new Noise(seed+99);
            // (A recorded pull's last few cycles are the driver lifting for the next gear, quieter and thinner: the
            // Escort's fell from 0.159 to 0.105. Above the pull the stretch starts from just below them.)
            srcLow=grains.Bottom;pullHigh=grains.TopOf(true)*.97f;coastHigh=grains.TopOf(false);
            if(pullHigh<=0)pullHigh=coastHigh;if(coastHigh<=0)coastHigh=pullHigh;
            redline=targetRedline>0?targetRedline:pullHigh;
        }
        float Top(bool pulling)=>pulling?pullHigh:coastHigh;
        float Source(float rpm,bool pulling)=>Math.Clamp(rpm,Math.Min(srcLow,Top(pulling)),Top(pulling));
        // How far above the recording's pulls this engine revs (1 = covered); the checks report it.
        public float TopStretch=>Math.Max(1,redline/pullHigh);
        // One sample at these revs; load 0..1 blends overrun into pulling; cut mutes (limiter, shift cut).
        public float Next(float rpm,float load,bool cut,bool shifting=false)
        {
            float l=Math.Clamp(load,0,1);
            // (During a gearshift the load drops for a moment: stay on the pulling recording, just quieter; switching
            // to the coasting one there sounded like the engine stopping dead between gears.)
            if(!shifting){if(pullingMode&&l<.2f)pullingMode=false;else if(!pullingMode&&l>.4f)pullingMode=true;}
            float x=Play(stream,rpm,pullingMode);
            // Above the recording's top revs the grains are squeezed shorter, which lifts the exhaust's resonances
            // with the note (Richard, 30 Sep: the Tuner V6, its Skyline recording pulling only to 3760 rpm, went thin in
            // the top half of each gear). A real engine's pipes stay put, so the lift is taken back out: a shelf cut
            // above 1.5x the firing note, 7 dB per doubling, and 3 dB of weight back below it; then its level is matched to
            // the squeezed sound's (the cut alone lost 4 dB on the Skyline, and quieter read as thinner; a fixed make-up
            // overshot on the Viper, whose high firing note sits mostly below the shelf). (Laying the recorded
            // pulses closer together instead, pitch-synchronous overlap-add, kept the tone but lost the rhythm:
            // pulse-to-pulse correlation 0.6 against the recording's 0.93.)
            float over=rpm/Top(pullingMode);
            if(Math.Abs(over-toneAt)>.005f) {
                toneAt=over;float octaves=MathF.Log(Math.Max(1,over),2),firing=rpm/120f*bank.cylinders;
                toneHigh.SetHighShelf(fs,firing*1.5f,-7*octaves);toneLow.SetLowShelf(fs,firing*.75f,3*octaves);toneOn=ToneCorrection&&octaves>.002f;
            }
            float shaped=toneLow.Process(toneHigh.Process(x));
            levelIn+=(x*x-levelIn)*.0001f;levelOut+=(shaped*shaped-levelOut)*.0001f;
            toneGain+=(Math.Clamp(MathF.Sqrt(levelIn/Math.Max(1e-9f,levelOut)),.5f,2.5f)-toneGain)*.0005f;
            if(toneOn)x=shaped*toneGain;
            // Part load: quieter and darker (a one-pole low-pass opening from ~1.5 kHz to fully open with load).
            bright+=((pullingMode?.35f+.65f*l:.5f)-bright)*.0005f;
            float k=Math.Clamp(bright*bright,.05f,1);lpState+=(x-lpState)*k;
            float s=(lpState*(1-bright)+x*bright)*(pullingMode?.6f+.4f*l:.75f);
            return cut?s*.08f:s;
        }
        float Play(Stream s,float rpm,bool on)
        {
            float src=Source(rpm,on);
            if(s.cur<0){s.cur=bank.Next(-1,src,on,ref rng);s.pos=0;}
            var g=bank.samples[s.cur];int length=bank.cycle[s.cur];
            // Stretch the grain so one cycle lasts exactly as long as the target revs' cycle.
            float cycle=fs*120f/Math.Max(200,rpm);float rate=length/cycle;
            float x=Sample(g,s.pos);
            if(s.prev>=0) {
                // The previous grain's tail (the recorded start of its next cycle) fades out as this grain fades in:
                // for the recorded successor both are the same audio, so the join is the recording itself.
                var p=bank.samples[s.prev];x=x*s.fade+Sample(p,s.prevPos)*(1-s.fade);
                s.prevPos+=bank.cycle[s.prev]/cycle;s.fade+=s.fadeStep;if(s.fade>=1||s.prevPos>=p.Length-1){s.prev=-1;s.fade=1;}
            }
            s.pos+=rate;
            if(s.pos>=length&&s.prev<0) {
                int tail=g.Length-length;
                s.prev=s.cur;s.prevPos=s.pos;s.cur=bank.Next(s.cur,src,on,ref rng);s.pos=0;s.fade=0;
                s.fadeStep=tail>8?rate/tail:1;   // (a grain without a tail cuts straight over)
            }
            return x;
        }
        static float Sample(float[] g,float pos){int i=(int)pos;if(i>=g.Length-1)return i<g.Length?g[i]:0;float f=pos-i;return g[i]+(g[i+1]-g[i])*f;}
    }
}
