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
        // Version 4 (docs/55): the lift-off drop (pulling against coasting at the same revs) and the rise with revs, in
        // dB, measured from the recording by tools/engine-grains.py; -1 for older banks.
        public float liftDb=-1,riseDb=-1;
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
            int version=r.ReadInt32();if(version<1||version>4)throw new InvalidDataException("Grain bank version "+version);
            var b=new GrainBank{sampleRate=r.ReadInt32(),cylinders=r.ReadInt32(),idle=r.ReadSingle(),redline=r.ReadSingle()};
            int n=r.ReadInt32();
            if(version>=4){b.liftDb=r.ReadSingle();b.riseDb=r.ReadSingle();}b.rpm=new float[n];b.load=new byte[n];b.run=new int[n];b.samples=new float[n][];b.cycle=new int[n];
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
        // Above the recording: a sustain loop of the widest sweep's last clean stretch. Picking "the nearest cycle" up
        // there ping-ponged between two or three cycles: a flutter at half the cycle rate, 9-10 dB over the cycle line,
        // that sounded like two engines at once (Richard, 1 Oct: the Tuner V6 "going up in revs sounded like two different
        // noises combined"). Pulling only: the overrun he called epic stays.
        // Version 3 (Richard, 1 Oct, later: "some low pitched static noise mixed into it"): the loop is one unbroken run
        // of recorded cycles, played forwards, so every join but one is the recording's own (each grain's tail is the
        // next cycle's start). Walking it back and forth (version 2) joined unrelated cycles on every backward step, and
        // dropping odd cycles out of the run joined unrelated cycles at every gap: noise between the harmonics only
        // 4-6 dB under them above 1.3x (in the recording's own range it is 20-30 dB under). The run is chosen, among
        // runs of 12-40 cycles from 85% of the highest sweep's top revs (odd cycles cost a quarter each), for the best wrap: the last cycle's tail most
        // like the first cycle's start, at the same level.
        readonly int[][] loops=new int[2][];readonly float[][] loopGains=new float[2][];
        public int[] SustainLoop(bool pulling){int k=pulling?1:0;if(loops[k]==null)BuildLoop(pulling);return loops[k];}
        public float LoopGain(bool pulling,int index){int k=pulling?1:0;if(loops[k]==null)BuildLoop(pulling);return loopGains[k][index];}
        void BuildLoop(bool pulling)
        {
            int k=pulling?1:0;loops[k]=new int[0];loopGains[k]=new float[0];
            // The sweep that reaches highest (the widest one can top out lower: the C2's widest pull stops at 2300 rpm
            // and its loop played 2000 rpm cycles stretched from 3400 rpm up).
            Sweep widest=null;foreach(var s in sweeps)if(s.pulling==pulling&&(widest==null||s.high>widest.high*1.01f))widest=s;
            if(widest==null)return;var g=widest.grains;
            int top=0;for(int i=0;i<g.Length;i++)if(rpm[g[i]]>rpm[g[top]])top=i;
            int from=top;while(from>0&&top-from<48&&(rpm[g[from-1]]>=rpm[g[top]]*.85f||top-from<12))from--;
            // Odd ones out (a cycle 15% brighter or darker than the median, or a revs spike) may not sit inside a run.
            float Bright(int grain){double d=0,e=0;var x=samples[grain];for(int j=1;j<cycle[grain];j++){d+=(x[j]-x[j-1])*(x[j]-x[j-1]);e+=x[j]*x[j];}return (float)Math.Sqrt(d/Math.Max(1e-12,e));}
            float Level(int grain){double e=0;int n=cycle[grain];for(int j=0;j<n;j++)e+=samples[grain][j]*samples[grain][j];return (float)Math.Sqrt(e/Math.Max(1,n));}
            var bright=new List<float>();for(int i=from;i<=top;i++)bright.Add(Bright(g[i]));var sorted=new List<float>(bright);sorted.Sort();float median=sorted[sorted.Count/2];
            var clean=new bool[top-from+1];
            for(int i=from;i<=top;i++) {
                var near=new List<float>();for(int j=Math.Max(from,i-2);j<=Math.Min(top,i+2);j++)if(j!=i)near.Add(rpm[g[j]]);near.Sort();float local=near.Count>0?near[near.Count/2]:rpm[g[i]];
                clean[i-from]=Math.Abs(bright[i-from]/median-1)<.15f&&Math.Abs(rpm[g[i]]/local-1)<.03f;
            }
            // The wrap: the last cycle's tail (the recorded start of the cycle after it) against the first cycle's start,
            // each over a quarter of its own cycle, as a normalised correlation, less a penalty for a level step.
            const int N=192;
            float Wrap(int last,int first){
                var a=samples[last];var b=samples[first];int la=cycle[last],lb=cycle[first];if(a.Length-la<la/4)return -9;
                double ab=0,aa=0,bb=0;for(int n=0;n<N;n++){double x=GrainSynth.Sample(a,la+n*.25f*la/N),y=GrainSynth.Sample(b,n*.25f*lb/N);ab+=x*y;aa+=x*x;bb+=y*y;}
                return (float)(ab/Math.Sqrt(Math.Max(1e-12,aa*bb))-2*Math.Abs(Math.Log(Math.Max(1e-6,Level(last))/Math.Max(1e-6,Level(first)))));}
            int bestA=-1,bestB=-1;float best=float.MinValue;
            // (Odd cycles count against a run rather than ruling it out: some recordings have no clean dozen.)
            for(int a=from;a<=top;a++)for(int b=a+11;b<=Math.Min(top,a+39);b++) {
                int odd=0;for(int i=a;i<=b;i++)if(!clean[i-from])odd++;
                float w=Wrap(g[b],g[a])+.002f*(b-a)-.25f*odd;if(w>best){best=w;bestA=a;bestB=b;}
            }
            if(bestA<0){bestB=top;bestA=Math.Max(from,top-15);}   // (no clean run: the top 16 cycles as they are)
            var loop=new int[bestB-bestA+1];for(int i=0;i<loop.Length;i++)loop[i]=g[bestA+i];
            var gains=new float[loop.Length];for(int i=0;i<gains.Length;i++)gains[i]=1;
            loops[k]=loop;loopGains[k]=gains;WrapScore[k]=best;
        }
        public readonly float[] WrapScore=new float[2];
        // Pulse marks (for pulse overlap-add in GrainSynth): where each firing pulse peaks in a grain's cycle. The
        // pulses are evenly spaced, so the comb offset that catches the most energy is found first, then each mark is
        // moved to its own pulse's peak within a quarter period.
        float[][] marks;
        public float[] Marks(int g)
        {
            marks??=new float[samples.Length][];if(marks[g]!=null)return marks[g];
            var x=samples[g];int n=Math.Max(1,cylinders),len=cycle[g];float P=len/(float)n;
            var sum=new double[x.Length+1];for(int i=0;i<x.Length;i++)sum[i+1]=sum[i]+x[i]*x[i];
            int half=Math.Max(1,(int)(P/16));double Env(int i){int a=Math.Clamp(i-half,0,x.Length),b=Math.Clamp(i+half+1,0,x.Length);return sum[b]-sum[a];}
            int phase=0;double best=-1;
            for(int f=0;f<(int)P;f++){double e=0;for(int j=0;j<n;j++)e+=Env(f+(int)(j*P));if(e>best){best=e;phase=f;}}
            var m=new float[n];
            for(int j=0;j<n;j++){int c=phase+(int)(j*P),at=c;double top=-1;for(int i=c-(int)(P/4);i<=c+(int)(P/4);i++){if(i<0||i>=len)continue;double e=Env(i);if(e>top){top=e;at=i;}}m[j]=at;}
            Array.Sort(m);return marks[g]=m;
        }
        // How much louder the bank's pulling cycles are than its coasting ones at the same revs (an amplitude ratio,
        // 1-4): the lift-off drop the bank builder measured from the recording (docs/55); estimated for older banks.
        float liftRatio=-1;
        public float LiftRatio {get {
            if(liftRatio>0)return liftRatio;
            if(liftDb>=0)return liftRatio=Math.Clamp(MathF.Pow(10,liftDb/20),1,4);
            var pull=new Dictionary<int,List<float>>();var coast=new Dictionary<int,List<float>>();
            for(int i=0;i<rpm.Length;i++){double e=0;int n=cycle[i];for(int j=0;j<n;j++)e+=samples[i][j]*samples[i][j];var d=load[i]==1?pull:coast;int k=(int)(rpm[i]/250);if(!d.TryGetValue(k,out var l))d[k]=l=new List<float>();l.Add((float)Math.Sqrt(e/Math.Max(1,n)));}
            float Median(List<float> l){l.Sort();return l[l.Count/2];}
            var ratios=new List<float>();foreach(var kv in pull)if(kv.Value.Count>=3&&coast.TryGetValue(kv.Key,out var c)&&c.Count>=3)ratios.Add(Median(kv.Value)/Math.Max(1e-6f,Median(c)));
            return liftRatio=ratios.Count>0?Math.Clamp(Median(ratios),1,4):1.4f;
        }}
        public float TopOf(bool pulling){float t=0;for(int i=0;i<rpm.Length;i++)if((load[i]==1)==pulling)t=Math.Max(t,rpm[i]);return t;}
        public float Bottom {get {float t=float.MaxValue;foreach(var r in rpm)t=Math.Min(t,r);return t;}}
    }

    // Plays a grain bank at any rpm: grains stretched to the exact cycle length for the target revs and overlapped
    // with an equal-power crossfade; pulling and coasting streams blended by load.
    public sealed class GrainSynth
    {
        readonly GrainBank bank;readonly float fs;Noise rng;
        sealed class Stream {public int cur=-1,prev=-1,before=-1,dir=1,sinceTurn;public float gain=1,prevGain=1;public float pos,prevPos,fade=1,fadeStep;}
        // One stream (Richard, 1 Oct: revving up sounded like two engines at once). Mixing a pulling and a coasting
        // recording at part throttle played two unrelated stretches of engine together. Now the stream switches
        // between pulling and coasting sweeps (with hysteresis, through the tail crossfade), and part throttle only
        // shapes that one voice's level and brightness.
        readonly Stream stream=new();bool pullingMode;float bright,lpState,modeLevel=1,liftEnv=1;
        // Lifting off (docs/55, the engine scorecard): real engines lose 1-4 dB in the first 0.7 s, and the overrun keeps
        // its own tone (the Escort's has its crackle, 19 dB more at 8 kHz than the muffled version). The bank now carries
        // each recording's own lifted-against-pulling level (tools/engine-grains.py), so the switch adds no step of its
        // own, the level eases over ~0.2 s, and the overrun is not muffled. LegacyLift (--legacy-lift): the old 0.75x
        // step and muffled overrun, for A/B.
        public static float LiftFade=1.4f;   // 1/s: the lift-off fade's rate (a 0.7 s time constant)
        public static bool LegacyLift=Array.IndexOf(Environment.GetCommandLineArgs(),"--legacy-lift")>=0;
        Biquad toneHigh,toneLow;float toneAt=-1,toneGain=1,levelIn,levelOut;bool toneOn;
        public static bool ToneCorrection=true,SustainLoop=true,PulseOverlap=true;   // (the sound harness compares with and without)
        // Pulse overlap-add above 1.5x (Richard, 1 Oct: the Tuner V6 "sounds great up to about 80% of max and then after it
        // goes very high pitch and changes to be weird"). The Skyline recording pulls only to 3760 rpm, so at 80% of the
        // V6's 7600 rpm its grains play 1.67x fast, which lifts the exhaust's resonances with the note: a chipmunk. Up to
        // 1.5x nothing changes. Beyond it the grain is sped up only 1.5x and its pulses are laid closer together for the
        // rest (each a two-period Hann window centred on its peak, as TD-PSOLA), so the note keeps rising and the pipes
        // stay put. Pulling only, like the sustain loop: the overrun Richard called epic is untouched. The stream is played
        // Lead samples late (5 ms) so the pulses, which need one period of lead-in, line up with it through the crossfade.
        const float FormantCap=1.5f;const int Lead=256;
        sealed class Voice {public bool on;public int g,before;public float u0,t,r,period,gain;}
        readonly Voice[] voices=new Voice[10];readonly float[] late=new float[512];int lateAt;float pulseMix,streamLevel,pulseLevel,pulseGain=1;
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
            bool was=pullingMode;
            if(!shifting){if(pullingMode&&l<.2f)pullingMode=false;else if(!pullingMode&&l>.4f)pullingMode=true;}
            // Lifted, the engine fades by the bank's measured drop over most of a second (real lift-offs lose 1-2 dB in the
            // first half second, 1-6 dB by one second); back on the throttle the pull returns at once.
            // (The bank's two kinds are levelled alike; the drop is a gain here, whichever cycles are playing.)
            float x=Play(stream,rpm,pullingMode);
            // Above the recording's top revs the grains are squeezed shorter, which lifts the exhaust's resonances
            // with the note (Richard, 30 Sep: the Tuner V6, its Skyline recording pulling only to 3760 rpm, went thin in
            // the top half of each gear). A real engine's pipes stay put, so the lift is taken back out: a shelf cut
            // above 1.5x the firing note, 7 dB per doubling, and 3 dB of weight back below it; then its level is matched to
            // the squeezed sound's (the cut alone lost 4 dB on the Skyline, and quieter read as thinner; a fixed make-up
            // overshot on the Viper, whose high firing note sits mostly below the shelf). (Laying the recorded
            // pulses closer together instead, pitch-synchronous overlap-add, kept the tone but lost the rhythm:
            // pulse-to-pulse correlation 0.6 against the recording's 0.93.)
            float over=Math.Max(1,rpm/Top(pullingMode));
            // (With pulse overlap-add the resonances rise only with the 1.5x speed-up: correct that much.)
            float octaves=MathF.Log(over,2)*(1-pulseMix)+MathF.Log(Math.Min(over,FormantCap),2)*pulseMix;
            if(Math.Abs(octaves-toneAt)>.004f) {
                toneAt=octaves;float firing=rpm/120f*bank.cylinders;
                toneHigh.SetHighShelf(fs,firing*1.5f,-7*octaves);toneLow.SetLowShelf(fs,firing*.75f,3*octaves);toneOn=ToneCorrection&&octaves>.002f;
            }
            float shaped=toneLow.Process(toneHigh.Process(x));
            levelIn+=(x*x-levelIn)*.0001f;levelOut+=(shaped*shaped-levelOut)*.0001f;
            toneGain+=(Math.Clamp(MathF.Sqrt(levelIn/Math.Max(1e-9f,levelOut)),.5f,2.5f)-toneGain)*.0005f;
            if(toneOn)x=shaped*toneGain;
            // Part load: quieter and darker (a one-pole low-pass opening from ~1.5 kHz to fully open with load).
            if(LegacyLift) {
                bright+=((pullingMode?.35f+.65f*l:.5f)-bright)*.0005f;
                float k0=Math.Clamp(bright*bright,.05f,1);lpState+=(x-lpState)*k0;
                float s0=(lpState*(1-bright)+x*bright)*(pullingMode?.6f+.4f*l:.75f);
                return cut?s0*.08f:s0;
            }
            bright+=((pullingMode?.35f+.65f*l:1)-bright)*.0005f;
            float k=Math.Clamp(bright*bright,.05f,1);lpState+=(x-lpState)*k;
            // Part throttle and gearchanges dip quickly (real shifts dip 1-11 dB within a tenth of a second); only the
            // lift-off itself fades slowly.
            float liftAim=pullingMode||bank.liftDb<0?1:1/bank.LiftRatio;
            modeLevel+=((pullingMode?.6f+.4f*l:1)-modeLevel)*25f/fs;liftEnv+=(liftAim-liftEnv)*(liftEnv>liftAim?LiftFade:30f)/fs;
            float s=(lpState*(1-bright)+x*bright)*modeLevel*liftEnv;
            return cut?s*.08f:s;
        }
        float Play(Stream s,float rpm,bool on)
        {
            float src=Source(rpm,on);
            if(s.cur<0){s.cur=bank.Next(-1,src,on,ref rng);s.pos=0;}
            var g=bank.samples[s.cur];int length=bank.cycle[s.cur];
            // Stretch the grain so one cycle lasts exactly as long as the target revs' cycle.
            float cycle=fs*120f/Math.Max(200,rpm);float rate=length/cycle;
            float x=Sample(g,s.pos)*s.gain,from=s.pos;
            if(s.prev>=0) {
                // The previous grain's tail (the recorded start of its next cycle) fades out as this grain fades in:
                // for the recorded successor both are the same audio, so the join is the recording itself.
                var p=bank.samples[s.prev];x=x*s.fade+Sample(p,s.prevPos)*s.prevGain*(1-s.fade);
                s.prevPos+=bank.cycle[s.prev]/cycle;s.fade+=s.fadeStep;if(s.fade>=1||s.prevPos>=p.Length-1){s.prev=-1;s.fade=1;}
            }
            s.pos+=rate;
            x=Pulses(s,x,from,rate,on);
            if(s.pos>=length&&s.prev<0) {
                int tail=g.Length-length;
                s.before=s.cur;s.prev=s.cur;s.prevPos=s.pos;s.prevGain=s.gain;s.gain=1;
                var loop=SustainLoop&&on&&rpm>Top(on)?bank.SustainLoop(on):null;
                if(loop!=null&&loop.Length>2) {
                    // Forwards through the loop, wrapping at its end (entering it, from its first cycle).
                    int at=Array.IndexOf(loop,s.cur);at=at<0||at+1>=loop.Length?0:at+1;
                    s.cur=loop[at];s.gain=bank.LoopGain(on,at);
                }
                else s.cur=bank.Next(s.cur,src,on,ref rng);
                s.pos=0;s.fade=0;
                s.fadeStep=tail>8?rate/tail:1;   // (a grain without a tail cuts straight over)
            }
            return x;
        }
        // The stream (Lead samples late) blended with its pulses re-laid at the target spacing.
        float Pulses(Stream s,float x,float from,float rate,bool on)
        {
            late[lateAt]=x;x=late[(lateAt-Lead)&(late.Length-1)];lateAt=(lateAt+1)&(late.Length-1);
            float r=Math.Min(rate,FormantCap),c=rate/r;bool active=PulseOverlap&&on;
            if(active&&(c>1.0001f||pulseMix>.001f)) {
                // A voice for each pulse peak the stream passes: a Hann window two target periods long centred on the peak, so
                // neighbours overlap only in pairs (a two-source-period window overlapped 2c deep and lost 3.5 dB at 2x).
                var marks=bank.Marks(s.cur);float period=bank.cycle[s.cur]/(float)marks.Length;
                foreach(float m in marks) {
                    if(m<from||m>=s.pos)continue;
                    Voice v=null;foreach(var free in voices)if(free!=null&&!free.on){v=free;break;}
                    if(v==null)for(int i=0;i<voices.Length;i++)if(voices[i]==null){v=voices[i]=new Voice();break;}
                    if(v==null)break;
                    v.on=true;v.g=s.cur;v.before=s.before;float half=period/c;v.u0=m-half;v.period=half;v.r=r;v.gain=s.gain;v.t=-Math.Max(0,Lead-half/r)*r;
                }
            }
            float sum=0;
            foreach(var v in voices) {
                if(v==null||!v.on)continue;
                if(v.t>=0) {
                    float u=v.u0+v.t;float y=u>=0?Sample(bank.samples[v.g],u):v.before>=0?Sample(bank.samples[v.before],Math.Max(0,bank.cycle[v.before]+u)):0;
                    sum+=y*(.5f-.5f*MathF.Cos(MathF.PI*v.t/v.period))*v.gain;
                }
                v.t+=v.r;if(v.t>=2*v.period)v.on=false;
            }
            pulseMix+=((active&&c>1.0001f?Math.Min(1,(c-1)/.08f):0)-pulseMix)*.004f;
            if(pulseMix<.0005f)return x;
            // Overlapping pulses are from different moments, so their crossfades sum below unity (the C2 lost 3 dB at
            // 1.8x): the pulses are held at the stretched stream's level, as the tone correction holds its own.
            streamLevel+=(x*x-streamLevel)*.0005f;pulseLevel+=(sum*sum-pulseLevel)*.0005f;
            pulseGain+=(Math.Clamp(MathF.Sqrt(streamLevel/Math.Max(1e-9f,pulseLevel)),.7f,1.6f)-pulseGain)*.0001f;
            return x*(1-pulseMix)+sum*pulseGain*pulseMix;
        }
        internal static float Sample(float[] g,float pos){int i=(int)pos;if(i>=g.Length-1)return i<g.Length?g[i]:0;float f=pos-i;return g[i]+(g[i+1]-g[i])*f;}
    }
}
