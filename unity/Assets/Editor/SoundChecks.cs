using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Sound (docs/47 stage 4): every engine in the ladder, with and without its class's inductions, synthesises a
// finite, unclipped, audible note at its firing frequency, cheaply; the car noises stay finite too. The full
// listening sweep (WAVs and pitch report) is tools/sound-harness.ps1.
public static class SoundChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Sound check: "+label);checks.Add(label);}
        const int fs=48000;var all=EquipmentCatalog.All.parts;int voices=0;double worstCost=0;
        var box=all.First(p=>p.slot=="transmission");
        foreach(var engine in all.Where(p=>p.slot=="engine"))
            foreach(var turbo in all.Where(p=>p.slot=="turbo"&&(p.id=="turbo_stock"||(p.tier??"C")==(engine.tier??"C")))) {
                var spec=EngineSoundSpec.From(engine,turbo,box);var synth=new EngineSynth(spec,fs,3);var buffer=new float[fs/4];
                var clock=System.Diagnostics.Stopwatch.StartNew();
                synth.Set(spec.redline*.6f,1,1,25,false,false,2,0,0);float rms=synth.Render(buffer,0,buffer.Length);
                double cost=clock.Elapsed.TotalMilliseconds*4;worstCost=Math.Max(worstCost,cost);voices++;
                bool finite=buffer.All(float.IsFinite);float peak=buffer.Max(Math.Abs);
                Check(finite&&peak<=1&&rms>.03f,$"{engine.id}+{turbo.id} sounds (rms {rms:0.000}, peak {peak:0.00})");
            }
        Check(worstCost<40,$"Engine synthesis is cheap: worst {worstCost:0.0} ms per second of sound per voice (editor, unoptimised)");
        var noise=new CarNoiseSynth(fs,true,5);var n=new float[fs/4];noise.Set(30,.8f,0,1,12);noise.Block(n.Length);
        bool ok=true;float loud=0;for(int i=0;i<n.Length;i++){n[i]=noise.Next();ok&=float.IsFinite(n[i]);loud=Math.Max(loud,Math.Abs(n[i]));}
        Check(ok&&loud>.01f&&loud<=1,"Tyres, road, wind and impact noise are finite and audible");
        checks.Add($"All {voices} engine and induction voices synthesised");
        // Recorded engines (grain banks, docs/47): every engine names one, it loads, and it plays cleanly.
        int recorded=0;
        foreach(var engine in all.Where(p=>p.slot=="engine")) {
            Check(!string.IsNullOrEmpty(engine.sound),$"{engine.id} has a recorded engine");
            var bank=EngineBanks.Get(engine.sound);Check(bank!=null&&bank.samples.Length>100,$"{engine.id}: grain bank '{engine.sound}' loads ({bank?.samples.Length??0} grains)");
            var spec=EngineSoundSpec.From(engine,all.First(p=>p.id=="turbo_stock"),box);var synth=new EngineSynth(spec,fs,5);
            synth.UseGrains(new GrainSynth(bank,fs,5,spec.idle,spec.redline));var buffer=new float[fs/4];
            synth.Set(spec.redline*.6f,1,0,25,false,false,2,0,0);float rms=synth.Render(buffer,0,buffer.Length);
            Check(buffer.All(float.IsFinite)&&buffer.Max(Math.Abs)<=1&&rms>.03f,$"{engine.id} plays its recording ({engine.sound}, rms {rms:0.000})");recorded++;
        }
        // Above a recording's top pulling revs the pull is stretched, not swapped for the lift-off recording (Richard,
        // 30 Sep: the Tuner V6 went thin in the top half of every gear), and it stays as loud as at the top of the pull.
        foreach(string sound in all.Where(p=>p.slot=="engine").Select(p=>p.sound).Distinct()) {
            var bank=EngineBanks.Get(sound);if(bank==null)continue;float top=bank.TopOf(true)*.97f;
            float Level(float rpm){var g=new GrainSynth(bank,fs,3,bank.idle,top*2);double sum=0;int count=fs/2;for(int i=0;i<count;i++){float x=g.Next(rpm,1,false);if(i>=count/2)sum+=x*x;}return (float)Math.Sqrt(sum/(count/2));}
            float at=Level(top*.98f),above=Level(top*1.8f);
            // (Since the in-game scorecard, docs/55, the level keeps rising above the recording at the recording's own
            // rate, riseDb per doubling of revs, up to +12 dB: never thinner, and about as much louder as the real engine.)
            float expect=bank.riseDb>0?Math.Min(4f,MathF.Pow(1.8f*.98f/.98f,bank.riseDb/6.0206f)):1;
            Check(above>at*.75f*expect&&above<at*1.5f*expect,$"{sound}: pulling above its recording ({top:0} rpm) keeps its level rising ({at:0.000} -> {above:0.000} at 1.8x, expected x{expect:0.00})");
        }
        // Above 1.5x the pulses are re-laid rather than sped up further (Richard, 1 Oct: the Tuner V6 went "very high
        // pitch" past 80% of its revs): at its 7600 rpm redline on the Skyline the sound is darker than the stretched one.
        // Brightness: the RMS of the first difference over the RMS, which rises with the spectrum's mean frequency.
        {
            var bank=EngineBanks.Get("skyline");float redline=7600;
            float Bright(bool pulses){GrainSynth.PulseOverlap=pulses;var g=new GrainSynth(bank,fs,3,bank.idle,redline);double d=0,e=0;float last=0;int count=fs;for(int i=0;i<count;i++){float x=g.Next(redline,1,false);if(i>=count/2){d+=(x-last)*(x-last);e+=x*x;}last=x;}GrainSynth.PulseOverlap=true;return (float)Math.Sqrt(d/Math.Max(1e-12,e));}
            float stretched=Bright(false),pulsed=Bright(true);
            Check(pulsed<stretched*.92f,$"Skyline at the Tuner V6's redline (2x its recording): pulse overlap-add is darker than stretching (brightness {stretched:0.000} -> {pulsed:0.000})");
        }
        // Above the recording the sustain loop stays as clean as the recording (Richard, 1 Oct: "some low pitched static
        // noise mixed into it"; a loop walked back and forth put noise between the harmonics only 4-6 dB under them).
        // Below 600 Hz, the energy between the cycle-rate harmonics against the energy on them, at 1.4x and 1.8x.
        {
            var bank=EngineBanks.Get("skyline");float top=bank.TopOf(true);
            foreach(float over in new[]{1.4f,1.8f}) {
                float rpm=top*over;var g=new GrainSynth(bank,fs,7,bank.idle,7600);int fftN=16384;var x=new float[fs/2+fftN];for(int i=0;i<x.Length;i++)x[i]=g.Next(rpm,1,false);
                var re=new double[fftN];var im=new double[fftN];for(int i=0;i<fftN;i++)re[i]=x[x.Length-fftN+i]*(.5-.5*Math.Cos(2*Math.PI*i/fftN));
                for(int i=1,j=0;i<fftN;i++){int bit=fftN>>1;for(;(j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j){(re[i],re[j])=(re[j],re[i]);}}
                for(int len=2;len<=fftN;len<<=1){double ang=-2*Math.PI/len;for(int i=0;i<fftN;i+=len)for(int k=0;k<len/2;k++){double wr=Math.Cos(ang*k),wi=Math.Sin(ang*k);int a=i+k,b=a+len/2;double xr=re[b]*wr-im[b]*wi,xi=re[b]*wi+im[b]*wr;re[b]=re[a]-xr;im[b]=im[a]-xi;re[a]+=xr;im[a]+=xi;}}
                double cyc=rpm/120.0,tonal=0,between=0;
                for(int k=1;k<fftN/2;k++){double hz=k*(double)fs/fftN;if(hz<25||hz>600)continue;double ph=hz/cyc,off=Math.Abs(ph-Math.Round(ph)),pw=re[k]*re[k]+im[k]*im[k];if(off<.12)tonal+=pw;else if(off>.25)between+=pw;}
                double db=10*Math.Log10((between+1e-20)/(tonal+1e-20));
                Check(db<-12,$"Skyline pulling at {over:0.0}x its recording: noise between the harmonics {db:0.0} dB under them (at most -12)");
            }
        }
        checks.Add($"All {recorded} engines play recorded grain banks");
        // The in-game scorecard's tools (docs/55) change nothing unless asked: the scripted test drive is off, and the
        // limiter's cut keeps its level.
        Check(!SoundDrive.On&&Math.Abs(GrainSynth.CutLevel-.5f)<1e-6f,"In-game scorecard tools are off by default (--sound-drive, --audio-capture-silent)");
        // Lifting off (docs/55, the engine scorecard): every bank carries its recording's measured lift-off drop and
        // rise with revs, and a lift fades rather than steps (real engines lose 1-2 dB in the first half second; the
        // old voice dropped 6-8 dB at once).
        foreach(string sound in all.Where(p=>p.slot=="engine").Select(p=>p.sound).Distinct()) {
            var bank=EngineBanks.Get(sound);
            Check(bank.liftDb>=3&&bank.liftDb<=12&&bank.riseDb>=3&&bank.riseDb<=12,$"{sound}: measured lift-off drop {bank.liftDb:0.0} dB and rise {bank.riseDb:0.0} dB per doubling of revs");
        }
        {
            var bank=EngineBanks.Get("skyline");var g=new GrainSynth(bank,fs,5,bank.idle,6600);float at=bank.TopOf(true)*.9f;
            double Rms(float load,int count){double e=0;for(int i=0;i<count;i++){float x=g.Next(at,load,false);e+=x*x;}return Math.Sqrt(e/count);}
            Rms(1,fs);double pulling=Rms(1,fs/4),early=Rms(0,fs/4);Rms(0,fs*2);double late=Rms(0,fs/4);
            double dEarly=20*Math.Log10(early/pulling),dLate=20*Math.Log10(late/pulling);
            Check(dEarly>-4&&dLate<dEarly-2&&dLate>-bank.liftDb-4,$"Skyline lift-off fades: {dEarly:0.0} dB in the first quarter second, {dLate:0.0} dB after two seconds (measured drop {bank.liftDb:0.0} dB)");
        }
        // Turbo and hybrid-motor sounds are off unless asked for (Richard, 2 Oct: "remove the turbo noises all together").
        {
            var e12=all.First(p=>p.id=="engine_e_12h");var twin=all.First(p=>p.id=="turbo_e_twin");var spec=EngineSoundSpec.From(e12,twin,all.First(p=>p.id=="transmission_e_long"));
            // (The second half only: the level calibration leaves a residue draining out of the DC blocker.)
            float Layer(int solo){var synth=new EngineSynth(spec,fs,9){Solo=solo};var buffer=new float[fs];synth.Set(spec.redline*.8f,1,1,70,false,false,6,0,0);synth.Render(buffer,0,fs/2);return synth.Render(buffer,fs/2,fs/2);}
            float turbo=Layer(4),motor=Layer(5);
            Check(!EngineSynth.TurboSound&&!EngineSynth.HybridSound&&turbo<1e-4f&&motor<1e-4f,$"No turbo or hybrid-motor sound by default (rms {turbo:0.000000}, {motor:0.000000}; --turbo-sound, --hybrid-sound)");
        }
        // Music (docs/49): every loop loads in stereo; each race's lift stem matches its base loop sample for sample.
        foreach(string place in new[]{"bayview","hillcrest"}) {
            var explore=Resources.Load<AudioClip>("Music/"+place+"-explore");var raceLoop=Resources.Load<AudioClip>("Music/"+place+"-race");var liftLoop=Resources.Load<AudioClip>("Music/"+place+"-race-lift");
            Check(explore!=null&&explore.channels==2&&explore.length>20,$"Music: {place} theme loads ({explore?.length??0:0} s)");
            Check(raceLoop!=null&&liftLoop!=null&&raceLoop.samples==liftLoop.samples,$"Music: {place} race loop and its last-lap lift are in step ({raceLoop?.length??0:0} s)");
        }
        foreach(string bed in new[]{"forest-birds","river-birds"}){var clip=Resources.Load<AudioClip>("Ambience/"+bed);Check(clip!=null&&clip.length>15&&clip.channels==2,$"Ambience bed {bed} loads (stereo, {clip?.length??0:0} s)");}
        // World ambience generators: finite, audible, under full scale.
        {
            var surf=new SurfSynth(fs,1);var wind=new WindSynth(fs,2);var falls=new FallsSynth(fs,3);var river=new RiverSynth(fs,4);
            foreach(var (name,next,block) in new (string,System.Func<float>,System.Action<int>)[]{("surf",()=>surf.NextStereo().Item1,surf.Block),("wind",()=>wind.NextStereo().Item1,wind.Block),("falls",falls.Next,falls.Block),("river",river.Next,river.Block)}) {
                double sum=0;bool finite=true;float peak=0;int length=fs*2;
                for(int done=0;done<length;done+=1024){block?.Invoke(1024);for(int i=0;i<1024;i++){float x=next();finite&=float.IsFinite(x);peak=Math.Max(peak,Math.Abs(x));sum+=x*x;}}
                float rms=(float)Math.Sqrt(sum/length);Check(finite&&peak<=1&&rms>.002f,$"Ambience {name} sounds (rms {rms:0.000})");
            }
        }
        return checks;
    }
}
