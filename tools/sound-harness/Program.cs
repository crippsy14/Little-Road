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
        if(filter.StartsWith("grains:"))return Grains(root,filter.Substring(7));
        if(filter=="recorded")return Recorded(root);
        if(filter=="ambience")return AmbienceRender(root);
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

    // Layer audit: one engine (id[+turbo id]) at 70% revs, full load and boost; each layer's level and its share
    // above 4 kHz (the piercing band).
    static int Layers(string root,string name)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"unity","Assets","Resources","Data","parts.json")));
        var parts=doc.RootElement.GetProperty("parts").EnumerateArray().ToDictionary(p=>p.GetProperty("id").GetString()!);
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
            for(int done=0;done<x.Length;done+=BlockSize){int n=Math.Min(BlockSize,x.Length-done);synth.Set(spec.redline*.7f,1,1,25,false,false,2,0,0);synth.Render(x,done,n);}
            var hp=Biquad.HighPass(Fs,4000,.7f);double all=0,high=0;for(int i=Fs/4;i<x.Length;i++){all+=x[i]*x[i];float h=hp.Process(x[i]);high+=h*h;}
            int count=x.Length-Fs/4;
            Console.WriteLine($"{names[solo+1],-11} rms {Math.Sqrt(all/count):0.0000}  above 4 kHz {Math.Sqrt(high/count):0.0000} ({(all>0?100*high/all:0):0}% of its energy)");
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
