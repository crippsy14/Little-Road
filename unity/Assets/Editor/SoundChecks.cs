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
        checks.Add($"All {recorded} engines play recorded grain banks");
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
