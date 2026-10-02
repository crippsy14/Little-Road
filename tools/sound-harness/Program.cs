// Offline listening and analysis for the live synthesisers (docs/47 stage 4), outside Unity.
// Built and run by tools/sound-harness.ps1 from the game's own DSP sources (compiled with SOUND_HARNESS).
// For every engine (with its class's induction variants) it renders a drive to review/sound/<name>.wav:
// idle, a full-throttle pull, an upshift, a pull onto the limiter, then a lift into overrun. It prints the
// pitch found at steady revs against the firing frequency, the level, peaks and the cost per voice.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using LittleRoad;

static class Program
{
    const int Fs=48000,BlockSize=1024;
    static int Main(string[] args)
    {
        string root=args.Length>0?args[0]:".";string outDir=Path.Combine(root,"review","sound");Directory.CreateDirectory(outDir);
        string filter=args.Length>1?args[1]:"";
        if(filter.StartsWith("layers:"))return Layers(root,filter.Substring(7));
        if(filter.StartsWith("static:"))return Static(root,filter.Substring(7));
        if(filter.StartsWith("grains:"))return Grains(root,filter.Substring(7));
        if(filter=="recorded")return Recorded(root);
        if(filter.StartsWith("follow:"))return Follow(root,filter.Substring(7));
if(filter.StartsWith("tone:"))return Tone(root,filter.Substring(5));
        if(filter=="ambience")return AmbienceRender(root);
if(filter=="horns")return Horns(root);
if(filter=="fireworks")return FireworksRender(root);
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        var parts=doc.RootElement.GetProperty("parts").EnumerateArray().ToList();
        string S(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
        float F(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number?v.GetSingle():0;
        bool B(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.True;
        string Tier(JsonElement p)=>S(p,"tier")??"C";
        int failures=0;var report=new List<string>();
        foreach(var e in parts.Where(p=>S(p,"slot")=="engine")) {
            string tier=Tier(e);
            var box=parts.First(p=>S(p,"slot")=="transmission"&&Tier(p)==tier);
            var inductions=new List<JsonElement?>{null};
            // Each class's strongest induction too, and the anti-lag and e-turbo characters.
            foreach(var t in parts.Where(p=>S(p,"slot")=="turbo"&&Tier(p)==tier&&F(p,"power")>1.001f))
                if(t.GetProperty("id").GetString()!.Contains("antilag")||t.GetProperty("id").GetString()!.Contains("eturbo")||t.GetProperty("id").GetString()!.Contains("club")||t.GetProperty("id").GetString()!.Contains("twin_scroll")||t.GetProperty("id").GetString()!.Contains("race"))inductions.Add(t);
            if(inductions.Count==1){var first=parts.FirstOrDefault(p=>S(p,"slot")=="turbo"&&Tier(p)==tier&&F(p,"power")>1.001f);if(first.ValueKind!=JsonValueKind.Undefined)inductions.Add(first);}
            foreach(var ind in inductions.Take(3)) {
                var spec=new EngineSoundSpec{id=S(e,"id"),litres=F(e,"litres"),idle=F(e,"idleRpm"),redline=F(e,"redline"),exhaust=S(e,"exhaust")??"road",
                    intake=S(e,"intake")??"box",hybrid=B(e,"hybrid"),firing=S(e,"firing")??"even",shiftType=S(box,"shiftType")??"H"};
                if(ind is JsonElement t){spec.turboPower=F(t,"power");spec.turbo=true;spec.blowoff=B(t,"blowoff");spec.antilag=B(t,"antilag");spec.electricTurbo=B(t,"electric");}
                spec.Layout(S(e,"layout"),(int)F(e,"cylinders"));
                string name=spec.id+(ind is JsonElement ti?"+"+ti.GetProperty("id").GetString():"");
                if(filter.Length>0&&!name.Contains(filter))continue;
                failures+=RenderDrive(spec,name,outDir,report);
            }
        }
        File.WriteAllLines(Path.Combine(outDir,"report.txt"),report);
        foreach(var line in report)Console.WriteLine(line);
        Console.WriteLine(failures==0?"SOUND_HARNESS ok":$"SOUND_HARNESS {failures} failures");
        return failures==0?0:1;
    }

    // A scripted drive: (seconds, rpm from, rpm to, throttle, boost, flags).
    static int RenderDrive(EngineSoundSpec spec,string name,string outDir,List<string> report)
    {
        var synth=new EngineSynth(spec,Fs,7);
        var output=new List<float>();int up=0,failures=0;float idle=spec.idle,red=spec.redline;
        var clock=Stopwatch.StartNew();
        void Seg(float seconds,float r0,float r1,float thr,float boost,bool limiter=false,bool shifting=false)
        {
            int frames=(int)(seconds*Fs);var buffer=new float[BlockSize];
            for(int done=0;done<frames;done+=BlockSize) {
                int n=Math.Min(BlockSize,frames-done);float t=(done+n)/(float)frames;
                synth.Set(r0+(r1-r0)*t,thr,boost,10+30*t,limiter&&((done/BlockSize)%3!=2),shifting,shifting?1:0,up,0);
                synth.Render(buffer,0,n);for(int i=0;i<n;i++)output.Add(buffer[i]);
            }
        }
        // Richard's listening test (the same RevCycle the in-game bench plays): idle, pull to max revs, hold on
        // the limiter, lift back to idle.
        var cycle=new RevCycle(spec);cycle.Start();var block=new float[BlockSize];float dt=BlockSize/(float)Fs;
        while(cycle.Running){cycle.Step(dt,0);synth.Set(cycle.Rpm,cycle.Throttle,cycle.Boost,20,cycle.Limiter,false,2,0,0);synth.Render(block,0,BlockSize);output.AddRange(block);}
        double seconds=output.Count/(double)Fs,ms=clock.Elapsed.TotalMilliseconds;
        // Steady-state pitch at 55% revs, full load (the calibration's own voice, separately).
        var steady=new EngineSynth(spec,Fs,11);var tone=new float[Fs/2];
        for(int done=0;done<tone.Length;done+=BlockSize){int n=Math.Min(BlockSize,tone.Length-done);steady.Set(red*.55f,1,1,20,false,false,2,0,0);steady.Render(tone,done,n);}
        float expected=spec.FiringHz(red*.55f),found=Pitch(tone,Fs/8,expected);
        float ratio=found/expected;
        // Uneven firing repeats once a cycle: accept the firing frequency or a whole fraction of it.
        bool pitchOk=(spec.firing=="crossplane"?new[]{1f,.5f,1f/3,.25f}:new[]{1f}).Any(k=>Math.Abs(ratio/k-1)<.05f);
        float peak=output.Max(Math.Abs),rms=(float)Math.Sqrt(output.Average(x=>x*x));bool finite=output.All(float.IsFinite);
        bool ok=pitchOk&&finite&&peak<=1&&rms>.02f&&rms<.5f;if(!ok)failures++;
        report.Add($"{(ok?"ok  ":"FAIL")} {name,-44} firing {expected,6:0} Hz, found {found,6:0} ({ratio:0.00}x)  rms {rms:0.000} peak {peak:0.00}  cost {ms/seconds:0.0} ms per s");
        WriteWav(Path.Combine(outDir,name+".wav"),output);
        return failures;
    }

    // follow:<engine id>:<bank>:<trace.csv>:<out.wav>[:raw] (tools/engine-compare.py, docs/55): the engine voice driven
    // along a recorded drive's trace (time s, rpm, throttle 0-1, shifting 0/1, upshift count), 10 ms at a time, so the
    // game's sound can be set against the real recording sample for sample. "raw": the grains alone and untouched (no
    // exhaust shaping, no modelled layers), to judge the playback method by itself.
    static int Follow(string root,string arg)
    {
        var a=arg.Split('|');if(a.Length<4)throw new ArgumentException("follow:<engine>|<bank>|<trace.csv>|<out.wav>[|raw]");
        // Options: raw; legacy (the voice before docs/55: old lift-off and the 25 Hz DC blocker, with an old bank given
        // as a .bytes path in place of the bank name).
        bool raw=a.Length>4&&a.Skip(4).Contains("raw");bool legacy=a.Skip(4).Contains("legacy");GrainSynth.LegacyLift=legacy;EngineSynth.DcCut=legacy?25:12;
        var spec=SpecFor(root,a[0]);if(raw)spec.exhaust="raw";
        var bank=GrainBank.Read(File.ReadAllBytes(a[1].EndsWith(".bytes")?a[1]:Path.Combine(root,"unity","Assets","Resources","Engines",a[1]+".bytes")));
        foreach(var o in a.Skip(4))if(o.StartsWith("exhaust="))spec.exhaust=o.Substring(8);
        var synth=new EngineSynth(spec,Fs,7){Solo=raw?0:-1};synth.UseGrains(new GrainSynth(bank,Fs,7,spec.idle,spec.redline));
        foreach(var o in a.Skip(4))if(o.StartsWith("solo="))synth.Solo=int.Parse(o.Substring(5));else if(o.StartsWith("mute="))synth.Mute=int.Parse(o.Substring(5));
        var rows=File.ReadAllLines(a[2]).Skip(1).Select(l=>l.Split(',').Select(v=>float.Parse(v,System.Globalization.CultureInfo.InvariantCulture)).ToArray()).ToList();
        var output=new List<float>();const int step=Fs/100;var block=new float[step];
        for(int i=0;i<rows.Count;i++) {
            var r=rows[i];synth.Set(r[1],r[2],0,20,false,r[3]>.5f,2,(int)r[4],0);synth.Render(block,0,step);output.AddRange(block);
        }
        WriteWav(a[3],output);
        Console.WriteLine($"FOLLOW {a[0]} on {a[1]}{(raw?" (raw)":"")}: {rows.Count/100f:0.0} s -> {a[3]}");
        return 0;
    }

    // Layer audit: one engine (id[+turbo id]) at 70% revs, full load and boost; each layer's level and its share
    // above 4 kHz (the piercing band).
    static int Layers(string root,string name)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        var parts=doc.RootElement.GetProperty("parts").EnumerateArray().ToDictionary(p=>p.GetProperty("id").GetString()!);
        // (layers:<engine>[+turbo][@fraction of the redline], default 0.7)
        float at=.7f;if(name.Contains('@')){at=float.Parse(name.Substring(name.IndexOf('@')+1),System.Globalization.CultureInfo.InvariantCulture);name=name.Substring(0,name.IndexOf('@'));}
        string[] ids=name.Split('+');var e=parts[ids[0]];
        string S(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
        float F(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number?v.GetSingle():0;
        bool B(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.True;
        var spec=new EngineSoundSpec{id=ids[0],litres=F(e,"litres"),idle=F(e,"idleRpm"),redline=F(e,"redline"),exhaust=S(e,"exhaust")??"road",intake=S(e,"intake")??"box",hybrid=B(e,"hybrid"),firing=S(e,"firing")??"even"};
        if(ids.Length>1){var t=parts[ids[1]];spec.turboPower=F(t,"power");spec.turbo=spec.turboPower>1.001f;spec.blowoff=B(t,"blowoff");spec.antilag=B(t,"antilag");spec.electricTurbo=B(t,"electric");}
        spec.Layout(S(e,"layout"),(int)F(e,"cylinders"));
        string[] names={"all","exhaust","roar","intake","mechanical","turbo","hybrid","shifts"};
        for(int solo=-1;solo<=6;solo++) {
            var synth=new EngineSynth(spec,Fs,7){Solo=solo};var x=new float[Fs];
            // With its recording when it has one (the game's mix), else the modelled exhaust.
            string sound=S(e,"sound");string bankFile=Path.Combine(root,"unity","Assets","Resources","Engines",(sound??"")+".bytes");
            if(!string.IsNullOrEmpty(sound)&&File.Exists(bankFile)){var bank=GrainBank.Read(File.ReadAllBytes(bankFile));synth.UseGrains(new GrainSynth(bank,Fs,7,spec.idle,spec.redline));if(solo==-1)Console.WriteLine($"(recording: {sound}; measured at a fixed low gain, clear of the soft limiter)");synth.gain=.05f;}
            for(int done=0;done<x.Length;done+=BlockSize){int n=Math.Min(BlockSize,x.Length-done);synth.Set(spec.redline*at,1,1,25,false,false,2,0,0);synth.Render(x,done,n);}
            var hp=Biquad.HighPass(Fs,4000,.7f);double all=0,high=0,dd=0;for(int i=Fs/4;i<x.Length;i++){all+=x[i]*x[i];float h=hp.Process(x[i]);high+=h*h;dd+=(x[i]-x[i-1])*(x[i]-x[i-1]);}
            int count=x.Length-Fs/4;
            Console.WriteLine($"{names[solo+1],-11} rms {Math.Sqrt(all/count):0.0000}  above 4 kHz {Math.Sqrt(high/count):0.0000} ({(all>0?100*high/all:0):0}% of its energy)  brightness {Math.Sqrt(dd/Math.Max(1e-12,all)):0.000}");
        }
        return 0;
    }

    // A recorded engine: grains:<bank.bytes>:<engine id>[+turbo id]. Plays the listening test through the grain
    // bank (with the engine's other layers) to review/sound/<bank>-test.wav and checks the pitch follows the revs.
    static int Grains(string root,string arg)
    {
        int split=arg.LastIndexOf(':');string file=arg.Substring(0,split),name=arg.Substring(split+1);
        var spec=SpecFor(root,name);var bank=GrainBank.Read(File.ReadAllBytes(Path.Combine(root,file)));
        var synth=new EngineSynth(spec,Fs,7);synth.UseGrains(new GrainSynth(bank,Fs,7,spec.idle,spec.redline));
        var output=new List<float>();var cycle=new RevCycle(spec);cycle.Start();var block=new float[BlockSize];float dt=BlockSize/(float)Fs;
        while(cycle.Running){cycle.Step(dt,0);synth.Set(cycle.Rpm,cycle.Throttle,cycle.Boost,20,cycle.Limiter,false,2,0,0);synth.Render(block,0,BlockSize);output.AddRange(block);}
        string outName=Path.GetFileNameWithoutExtension(file)+"-test.wav";WriteWav(Path.Combine(root,"review","sound",outName),output);
        var steady=new EngineSynth(spec,Fs,11);steady.UseGrains(new GrainSynth(bank,Fs,11,spec.idle,spec.redline));var tone=new float[Fs/2];
        for(int done=0;done<tone.Length;done+=BlockSize){int n=Math.Min(BlockSize,tone.Length-done);steady.Set(spec.redline*.55f,1,1,20,false,false,2,0,0);steady.Render(tone,done,n);}
        float expected=spec.FiringHz(spec.redline*.55f),found=Pitch(tone,Fs/8,expected);
        float peak=output.Max(Math.Abs),rms=(float)Math.Sqrt(output.Average(x=>x*x));
        Console.WriteLine($"grains {bank.samples.Length} ({bank.idle:0}-{bank.redline:0} rpm bank) -> {outName}: firing {expected:0} Hz, found {found:0} ({found/expected:0.00}x), rms {rms:0.000}, peak {peak:0.00}");
        return 0;
    }
    // Every engine in the ladder through its assigned recording (parts.json "sound"): the listening test to
    // review/sound/recorded/<engine>.wav, with pitch against the firing frequency and how far past the recording
    // the engine revs.
    static int Recorded(string root)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        string dir=Path.Combine(root,"review","sound","recorded");Directory.CreateDirectory(dir);int failures=0;
        foreach(var e in doc.RootElement.GetProperty("parts").EnumerateArray()) {
            if(e.GetProperty("slot").GetString()!="engine")continue;
            string id=e.GetProperty("id").GetString()!,sound=e.TryGetProperty("sound",out var v)?v.GetString()??"":"";
            string file=Path.Combine(root,"unity","Assets","Resources","Engines",sound+".bytes");
            if(sound.Length==0||!File.Exists(file)){Console.WriteLine($"-    {id,-20} no recording");continue;}
            var spec=SpecFor(root,id);var bank=GrainBank.Read(File.ReadAllBytes(file));
            var synth=new EngineSynth(spec,Fs,7);var grains=new GrainSynth(bank,Fs,7,spec.idle,spec.redline);synth.UseGrains(grains);
            var output=new List<float>();var cycle=new RevCycle(spec);cycle.Start();var block=new float[BlockSize];float dt=BlockSize/(float)Fs;
            while(cycle.Running){cycle.Step(dt,0);synth.Set(cycle.Rpm,cycle.Throttle,cycle.Boost,20,cycle.Limiter,false,2,0,0);synth.Render(block,0,BlockSize);output.AddRange(block);}
            WriteWav(Path.Combine(dir,id+".wav"),output);
            var steady=new EngineSynth(spec,Fs,11);steady.UseGrains(new GrainSynth(bank,Fs,11,spec.idle,spec.redline));var tone=new float[Fs/2];
            float at=Math.Min(spec.redline*.55f,bank.redline*.8f);
            for(int done=0;done<tone.Length;done+=BlockSize){int n=Math.Min(BlockSize,tone.Length-done);steady.Set(at,1,1,20,false,false,2,0,0);steady.Render(tone,done,n);}
            // The recording's cylinder count sets the note (a six's grains played for a V6 fire six times a cycle).
            float expected=at/60f*bank.cylinders*.5f,found=Pitch(tone,Fs/8,expected);
            float peak=output.Max(Math.Abs),rms=(float)Math.Sqrt(output.Average(x=>x*x));// Real recordings often lock onto a cycle-level sub-note (1/2, 1/3): fine. Grains an octave too high are not.
            bool ok=found/expected<1.1f&&peak<=1.001f&&rms>.02f&&rms<.35f;if(!ok)failures++;
            Console.WriteLine($"{(ok?"ok  ":"FAIL")} {id,-20} {sound,-11} ({bank.cylinders} cyl) at {at:0} rpm: firing {expected:0} Hz, found {found:0}; stretched x{grains.TopStretch:0.00} at the redline; rms {rms:0.000}");
        }
        return failures==0?0:1;
    }

    // tone:<engine id>: the recorded engine's grains alone, held at full load at 30-100% of the redline (1.2 s each),
    // squeezed with and without the tone correction (GrainSynth.ToneCorrection) to review/sound/tone/<engine>-<way>.wav, with each step's
    // level, body (share of energy below 2.5x the firing rate) and brightness (spectral centroid).
    static int Tone(string root,string id)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        var e=doc.RootElement.GetProperty("parts").EnumerateArray().First(p=>p.GetProperty("id").GetString()==id);
        string sound=e.GetProperty("sound").GetString()!;var spec=SpecFor(root,id);
        var bank=GrainBank.Read(File.ReadAllBytes(Path.Combine(root,"unity","Assets","Resources","Engines",sound+".bytes")));
        string dir=Path.Combine(root,"review","sound","tone");Directory.CreateDirectory(dir);
        Console.WriteLine($"{id} on {sound}: recording pulls to {bank.Top:0} rpm, engine revs to {spec.redline:0}");
        foreach(bool pulses in new[]{false,true}) {
            GrainSynth.PulseOverlap=pulses;var g=new GrainSynth(bank,Fs,7,spec.idle,spec.redline);var all=new List<float>();
            Console.WriteLine(pulses?" pulse overlap-add above 1.5x":" stretched (old)");
            for(float f=.3f;f<=1.001f;f+=.1f) {   // (2.4 s each: long enough for a sustain loop to come round several times)
                float rpm=spec.redline*f;var x=new float[(int)(Fs*2.4f)];for(int i=0;i<x.Length;i++)x[i]=g.Next(rpm,1,false);all.AddRange(x);
                int n=16384,start=x.Length-n;var re=new double[n];for(int i=0;i<n;i++)re[i]=x[start+i]*(.5-.5*Math.Cos(2*Math.PI*i/n));
                var power=Spectrum(re);double tot=1e-12,low=0,cen=0;float firing=rpm/120f*bank.cylinders;
                for(int k=1;k<power.Length;k++){double hz=k*(double)Fs/n;tot+=power[k];cen+=power[k]*hz;if(hz<2.5*firing)low+=power[k];}
                double rms=Math.Sqrt(x.Skip(start).Average(v=>v*v));
                float found=Pitch(x.Skip(x.Length-Fs/2).ToArray(),0,firing);
                // Periodicity: normalised autocorrelation at one pulse, one crank turn and one engine cycle.
                double Corr(double lagSamples){int lag=(int)Math.Round(lagSamples);double s=0,e0=0;for(int i=start;i<x.Length-lag;i++){s+=x[i]*x[i+lag];e0+=x[i]*x[i];}return s/Math.Max(1e-9,e0);}
                double pulse=Fs/firing;
                // Ping-pong between two cycles puts energy at half the cycle rate: its share against the cycle line.
                double cycleHz=rpm/120.0,half=0,whole=0;for(int k=1;k<power.Length;k++){double hz=k*(double)Fs/n;if(Math.Abs(hz-cycleHz*.5)<cycleHz*.12)half+=power[k];if(Math.Abs(hz-cycleHz)<cycleHz*.12)whole+=power[k];}
                // Wobble: the spread of 30 ms levels after the first 0.2 s (a sustain loop's seam or uneven cycles show here).
                var wins=new List<double>();for(int w0=Fs/5;w0+Fs*3/100<=x.Length;w0+=Fs*3/100){double en=0;for(int k=w0;k<w0+Fs*3/100;k++)en+=x[k]*x[k];wins.Add(Math.Sqrt(en/(Fs*3/100)));}
                double dd=0,ee=0;for(int k=Fs/5+1;k<x.Length;k++){dd+=(x[k]-x[k-1])*(x[k]-x[k-1]);ee+=x[k]*x[k];}double brightness=Math.Sqrt(dd/Math.Max(1e-12,ee));
                wins.Sort();double wobble=20*Math.Log10(wins[(int)(wins.Count*.9)]/Math.Max(1e-9,wins[(int)(wins.Count*.1)]));
                Console.WriteLine($"  {rpm,5:0} rpm (x{Math.Max(1,rpm/bank.TopOf(true)):0.00})  rms {rms:0.000}  centroid {cen/tot,5:0} Hz  firing {firing:0} Hz, found {found:0}  corr pulse {Corr(pulse):0.00} cycle {Corr(pulse*bank.cylinders):0.00}  half-cycle flutter {10*Math.Log10(Math.Max(1e-12,half)/Math.Max(1e-12,whole)):0} dB  wobble {wobble:0.0} dB  brightness {brightness:0.000}");
            }
            WriteWav(Path.Combine(dir,$"{id}-{(pulses?"pulses":"stretched")}.wav"),all);
        }
        GrainSynth.PulseOverlap=true;return 0;
    }

    // static:<engine>[+turbo]: where low-frequency noise comes from (Richard, 1 Oct: "some low pitched static noise mixed
    // into it"). Each layer, and the car-noise synth, at steady states; below 600 Hz, the energy on the engine's
    // harmonics (multiples of the cycle rate, rpm/120) against the energy between them. Noise near the harmonics'
    // level is heard as static under the note.
    static int Static(string root,string name)
    {
        var spec=SpecFor(root,name);
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        string sound=doc.RootElement.GetProperty("parts").EnumerateArray().First(p=>p.GetProperty("id").GetString()==name.Split('+')[0]).GetProperty("sound").GetString()!;
        var bank=GrainBank.Read(File.ReadAllBytes(Path.Combine(root,"unity","Assets","Resources","Engines",sound+".bytes")));
        string[] names={"all","exhaust","roar","intake","mechanical","turbo"};
        (double tonal,double noise,double rms) Measure(float[] x,float rpm){
            int n=32768,start=x.Length-n;var re=new double[n];for(int i=0;i<n;i++)re[i]=x[start+i]*(.5-.5*Math.Cos(2*Math.PI*i/n));
            var pw=Spectrum(re);double cyc=rpm/120.0,t=0,z=0;
            for(int k=1;k<pw.Length;k++){double hz=k*(double)Fs/n;if(hz<25||hz>600)continue;double ph=hz/cyc;double off=Math.Abs(ph-Math.Round(ph));if(off<.12)t+=pw[k];else if(off>.25)z+=pw[k];}
            double r=Math.Sqrt(x.Skip(start).Average(v=>(double)v*v));return (t,z,r);}
        // The grains alone, pulling, across the revs, with each stage of GrainSynth switched off in turn.
        {var lp=bank.SustainLoop(true);Console.WriteLine($"sustain loop: {lp.Length} cycles, {bank.rpm[lp[0]]:0}-{bank.rpm[lp[^1]]:0} rpm, wrap score {bank.WrapScore[1]:0.00}");}
        Console.WriteLine($"grains alone, pulling (recording pulls to {bank.TopOf(true):0} rpm): noise/tone below 600 Hz");
        foreach(float rpm in new[]{1800f,2600f,3400f,3700f,3900f,4200f,4600f,5000f,5400f,6000f,7000f}) {
            var row=new System.Text.StringBuilder($"  {rpm,5:0} rpm");
            foreach(var (lab,tc,sl,po) in new[]{("all on",true,true,true),("no tone corr",false,true,true),("no loop",true,false,true),("no pulses",true,true,false)}) {
                GrainSynth.ToneCorrection=tc;GrainSynth.SustainLoop=sl;GrainSynth.PulseOverlap=po;
                var g=new GrainSynth(bank,Fs,7,spec.idle,spec.redline);var x=new float[Fs*2];for(int i=0;i<x.Length;i++)x[i]=g.Next(rpm,1,false);
                var (t,z,_)=Measure(x,rpm);row.Append($"  {lab} {10*Math.Log10((z+1e-20)/(t+1e-20)),5:0.0}");
            }
            GrainSynth.ToneCorrection=GrainSynth.SustainLoop=GrainSynth.PulseOverlap=true;Console.WriteLine(row);
        }
        foreach(var (label,frac,thr) in new[]{("idle",0f,0f),("cruise 40%, part throttle",.4f,.3f),("pull 60%",.6f,1f),("pull 90%",.9f,1f),("lift 70%",.7f,0f)}) {
            float rpm=frac<=0?spec.idle:spec.redline*frac;Console.WriteLine($"{label} ({rpm:0} rpm)");
            for(int solo=-1;solo<=4;solo++) {
                var synth=new EngineSynth(spec,Fs,7);synth.UseGrains(new GrainSynth(bank,Fs,7,spec.idle,spec.redline));synth.Solo=solo;   // (solo after calibrating: the mix sets the gain)
                var x=new float[Fs*2];for(int done=0;done<x.Length;done+=BlockSize){int k=Math.Min(BlockSize,x.Length-done);synth.Set(rpm,thr,thr,20,false,false,2,0,0);synth.Render(x,done,k);}
                var (t,z,r)=Measure(x,rpm);if(r<1e-6)continue;
                Console.WriteLine($"  {names[solo+1],-10} rms {r:0.0000}  harmonics<600Hz {10*Math.Log10(t+1e-20),6:0.0} dB  between {10*Math.Log10(z+1e-20),6:0.0} dB  noise/tone {10*Math.Log10((z+1e-20)/(t+1e-20)),5:0.0} dB");
            }
            var cn=new CarNoiseSynth(Fs,true,5);var y=new float[Fs*2];for(int done=0;done<y.Length;done+=BlockSize){int k=Math.Min(BlockSize,y.Length-done);cn.Set(frac*45,0,0,0,0);cn.Block(k);for(int i=0;i<k;i++)y[done+i]=cn.Next();}
            var (t2,z2,r2)=Measure(y,rpm);Console.WriteLine($"  {"car noise",-10} rms {r2:0.0000}  (road and wind at {frac*45:0} m/s)  below 600 Hz {10*Math.Log10(t2+z2+1e-20),6:0.0} dB");
        }
        return 0;
    }

    // Power spectrum (radix-2 FFT, length a power of two).
    static double[] Spectrum(double[] re)
    {
        int n=re.Length;var im=new double[n];
        for(int i=1,j=0;i<n;i++){int bit=n>>1;for(;(j&bit)!=0;bit>>=1)j^=bit;j^=bit;if(i<j){(re[i],re[j])=(re[j],re[i]);}}
        for(int len=2;len<=n;len<<=1){double ang=-2*Math.PI/len;for(int i=0;i<n;i+=len)for(int k=0;k<len/2;k++){double wr=Math.Cos(ang*k),wi=Math.Sin(ang*k);int a=i+k,b=a+len/2;double xr=re[b]*wr-im[b]*wi,xi=re[b]*wi+im[b]*wr;re[b]=re[a]-xr;im[b]=im[a]-xi;re[a]+=xr;im[a]+=xi;}}
        var p=new double[n/2];for(int k=0;k<n/2;k++)p[k]=re[k]*re[k]+im[k]*im[k];return p;
    }

    // horns: a small car's toot, a normal horn held, a bus, and the train whistle (docs/51), to review/sound/horns/.
    static int Horns(string root)
    {
        string dir=Path.Combine(root,"review","sound","horns");Directory.CreateDirectory(dir);int failures=0;
        foreach(var (name,pitch,whistle,hold) in new[]{("bubble-toot",1.3f,false,.12f),("horn-hold",1f,false,.8f),("minibus",.78f,false,.6f),("train-whistle",1f,true,.9f)}) {
            var h=new HornSynth(Fs,pitch,whistle,3);var output=new List<float>();
            for(int i=0;i<Fs*2;i++){h.Press(i<Fs*hold||(!whistle&&i>Fs*(hold+.1f)&&i<Fs*(hold*2+.1f)));output.Add(h.Next());}
            float peak=output.Max(Math.Abs),rms=(float)Math.Sqrt(output.Take((int)(Fs*hold)).Average(x=>x*x));float expected=pitch*(whistle?587:415),found=Pitch(output.Skip(Fs/10).Take(Fs/4).ToArray(),0,expected);
            bool ok=output.All(float.IsFinite)&&peak<=1&&rms>.05f&&output[^1]==0;if(!ok)failures++;
            WriteWav(Path.Combine(dir,name+".wav"),output);Console.WriteLine($"{(ok?"ok  ":"FAIL")} {name,-14} rms {rms:0.000} peak {peak:0.00} note {found:0} Hz (lowest pipe {expected:0})");
        }
        return failures==0?0:1;
    }
    // fireworks: a launch, a boom 0.6 s later (as if 200 m away), a willow's crackle (docs/51), to review/sound/horns/.
    static int FireworksRender(string root)
    {
        string dir=Path.Combine(root,"review","sound","horns");Directory.CreateDirectory(dir);
        var f=new FireworkSynth(Fs,3);f.Play(FireworkSynth.Kind.Launch,0,.6f);f.Play(FireworkSynth.Kind.Boom,2.6f,.8f);f.Play(FireworkSynth.Kind.Crackle,3.0f,.9f);
        var output=new List<float>();for(int i=0;i<Fs*6;i++)output.Add(f.Next());
        float peak=output.Max(Math.Abs);float Rms(int a,int b)=>(float)Math.Sqrt(output.Skip(a).Take(b-a).Average(x=>x*x));
        float launch=Rms(Fs/10,Fs),boom=Rms((int)(Fs*2.6f),(int)(Fs*3.1f)),crackle=Rms((int)(Fs*3.6f),(int)(Fs*5f)),quiet=Rms((int)(Fs*1.4f),(int)(Fs*2.5f));
        bool ok=output.All(float.IsFinite)&&peak<=1&&launch>.01f&&boom>launch&&crackle>.001f&&quiet<.01f;
        WriteWav(Path.Combine(dir,"fireworks.wav"),output);Console.WriteLine($"{(ok?"ok  ":"FAIL")} fireworks  launch {launch:0.000} boom {boom:0.000} crackle {crackle:0.000} between {quiet:0.0000} peak {peak:0.00}");
        return ok?0:1;
    }
    // World ambience: 20 s of each generator to review/sound/ambience/, with levels.
    static int AmbienceRender(string root)
    {
        string dir=Path.Combine(root,"review","sound","ambience");Directory.CreateDirectory(dir);int failures=0;
        void Save(string name,Func<float> next,Action<int> block)
        {
            var output=new List<float>();for(int done=0;done<Fs*20;done+=BlockSize){block?.Invoke(BlockSize);for(int i=0;i<BlockSize;i++)output.Add(next());}
            float peak=output.Max(Math.Abs),rms=(float)Math.Sqrt(output.Average(x=>x*x));bool ok=output.All(float.IsFinite)&&peak<=1&&rms>.005f&&rms<.3f;if(!ok)failures++;
            WriteWav(Path.Combine(dir,name+".wav"),output);Console.WriteLine($"{(ok?"ok  ":"FAIL")} {name,-10} rms {rms:0.000} peak {peak:0.00}");
        }
        var surf=new SurfSynth(Fs,1);Save("surf",()=>surf.NextStereo().Item1,surf.Block);
        var wind=new WindSynth(Fs,2);Save("wind",()=>wind.NextStereo().Item1,wind.Block);
        var falls=new FallsSynth(Fs,3);Save("falls",falls.Next,falls.Block);
        var river=new RiverSynth(Fs,4);Save("river",river.Next,river.Block);
        return failures==0?0:1;
    }

    static EngineSoundSpec SpecFor(string root,string name)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        var parts=doc.RootElement.GetProperty("parts").EnumerateArray().ToDictionary(p=>p.GetProperty("id").GetString()!,p=>p.Clone());
        string S(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.String?v.GetString():null;
        float F(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number?v.GetSingle():0;
        bool B(JsonElement p,string k)=>p.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.True;
        string[] ids=name.Split('+');var e=parts[ids[0]];
        var spec=new EngineSoundSpec{id=ids[0],litres=F(e,"litres"),idle=F(e,"idleRpm"),redline=F(e,"redline"),exhaust=S(e,"exhaust")??"road",intake=S(e,"intake")??"box",hybrid=B(e,"hybrid"),firing=S(e,"firing")??"even"};
        if(ids.Length>1){var t=parts[ids[1]];spec.turboPower=F(t,"power");spec.turbo=spec.turboPower>1.001f;spec.blowoff=B(t,"blowoff");spec.antilag=B(t,"antilag");spec.electricTurbo=B(t,"electric");}
        spec.Layout(S(e,"layout"),(int)F(e,"cylinders"));return spec;
    }

    // Autocorrelation pitch: the strongest period between a quarter and four times the expected one.
    static float Pitch(float[] raw,int start,float expected)
    {
        // Measure the engine's note, not the turbo whistle or e-motor: low-pass (two poles) at 4x the firing rate.
        var x=new float[raw.Length];var lp=Biquad.LowPass(Fs,Math.Min(4000,expected*4),.7f);for(int i=0;i<raw.Length;i++)x[i]=lp.Process(raw[i]);
        int n=x.Length-start;int lo=Math.Max(8,(int)(Fs/(expected*4.2f))),hi=Math.Min(n/2,(int)(Fs/(expected*.23f)));
        // The shortest period whose correlation is within 90% of the best (a periodic sound correlates as well at
        // two periods as at one; the ear hears the shorter).
        var r=new double[hi+2];double best=double.MinValue;
        for(int k=lo;k<=hi+1;k++){double s=0;for(int i=start;i<start+n-hi-1;i++)s+=x[i]*x[i+k];r[k]=s;if(k<=hi&&s>best)best=s;}
        for(int k=lo+1;k<=hi;k++)if(r[k]>=.9*best&&r[k]>=r[k-1]&&r[k]>=r[k+1])return Fs/(float)k;
        return Fs/(float)lo;
    }

    static void WriteWav(string path,List<float> samples)
    {
        using var w=new BinaryWriter(File.Create(path));
        int bytes=samples.Count*2;
        w.Write("RIFF".ToCharArray());w.Write(36+bytes);w.Write("WAVE".ToCharArray());
        w.Write("fmt ".ToCharArray());w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(Fs);w.Write(Fs*2);w.Write((short)2);w.Write((short)16);
        w.Write("data".ToCharArray());w.Write(bytes);
        foreach(float s in samples)w.Write((short)Math.Clamp((int)(s*32767),-32768,32767));
    }
}
