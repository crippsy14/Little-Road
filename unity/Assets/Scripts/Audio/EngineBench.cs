using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LittleRoad
{
    // Engine sound test (Settings > Engine sound test, or --sound-bench): pick any engine and induction in the
    // ladder and hear it through the game's own audio. Cross / Space runs Richard's test (idle, pull to max revs,
    // hold on the limiter, lift back to idle); R2 / F revs it freely. Other cars fall silent while it is open.
    public sealed class EngineBench : MonoBehaviour
    {
        public static bool Showing {get;private set;}
        static EngineBench instance;
        readonly List<EquipmentCatalog.Part> engines=new();int engineIndex,inductionIndex;
        List<EquipmentCatalog.Part> inductions=new();
        EngineSynth synth;RevCycle cycle;AudioSource source;bool wasPaused;
        Canvas canvas;Text title,detail,induction,phase,rpmText;readonly List<Graphic> lamps=new();
        const int Lamps=32;

        public static void Show(string startEngine=null)
        {
            if(!AudioDirector.Enabled)return;
            if(instance==null){var go=new GameObject("Engine sound test");DontDestroyOnLoad(go);instance=go.AddComponent<EngineBench>();instance.Build();}
            instance.Open(startEngine);
        }
        void Build()
        {
            string[] order=RaceCalendar.Classes;
            engines.AddRange(EquipmentCatalog.All.parts.Where(p=>p.slot=="engine"));
            engines.Sort((a,b)=>System.Array.IndexOf(order,a.tier??"C").CompareTo(System.Array.IndexOf(order,b.tier??"C")));
            source=gameObject.AddComponent<AudioSource>();source.spatialBlend=0;source.loop=true;source.playOnAwake=false;source.priority=0;
            int fs=AudioSettings.outputSampleRate;var ones=new float[fs];for(int i=0;i<fs;i++)ones[i]=1;
            var clip=AudioClip.Create("Bench carrier",fs,1,fs,false);clip.SetData(ones,0);source.clip=clip;source.ignoreListenerPause=true;
            canvas=CoastUI.Canvas(transform,"Engine sound test canvas",5000);
            var panel=CoastUI.Panel(canvas.transform,"Bench panel",new Vector2(1240,430),new Vector2(0,-40),new Color(.04f,.08f,.12f,.92f),28,true);
            title=CoastUI.Label(panel,"Bench title","ENGINE SOUND TEST",40,new Vector2(1160,56),new Vector2(0,168),Color.white,true,TextAnchor.MiddleCenter);
            detail=CoastUI.Label(panel,"Bench engine","",30,new Vector2(1160,44),new Vector2(0,110),new Color(1,.86f,.45f),true,TextAnchor.MiddleCenter);
            induction=CoastUI.Label(panel,"Bench induction","",24,new Vector2(1160,36),new Vector2(0,70),new Color(.8f,.9f,1),false,TextAnchor.MiddleCenter);
            float width=1080,step=width/Lamps;
            for(int i=0;i<Lamps;i++)lamps.Add(CoastUI.Panel(panel,"Bench lamp "+i,new Vector2(step-4,34),new Vector2(-width*.5f+step*(i+.5f),10),new Color(1,1,1,.12f),4).GetComponent<CoastPanel>());
            rpmText=CoastUI.Label(panel,"Bench rpm","",34,new Vector2(560,44),new Vector2(-250,-44),Color.white,true,TextAnchor.MiddleLeft);
            phase=CoastUI.Label(panel,"Bench phase","",28,new Vector2(560,44),new Vector2(250,-44),new Color(.55f,.92f,.55f),true,TextAnchor.MiddleRight);
            CoastUI.Label(panel,"Bench controls","◀ ▶  engine  (A/D, ←/→, D-pad)      ▲ ▼  induction  (W/S, ↑/↓, D-pad)\nCross / Space: rev test (pull, hold at max revs, back down)      R2 / hold F: throttle      Circle / Esc: close",
                22,new Vector2(1160,80),new Vector2(0,-140),new Color(1,1,1,.8f),false,TextAnchor.MiddleCenter);
        }
        void Open(string startEngine)
        {
            Showing=true;canvas.gameObject.SetActive(true);
            // The bench plays while the game is paused; every other voice is muted until it closes.
            wasPaused=AudioListener.pause;AudioListener.pause=false;AudioDirector.Muted=true;
            if(startEngine!=null){int i=engines.FindIndex(p=>p.id==startEngine);if(i>=0)engineIndex=i;}
            Select(engineIndex,0);source.volume=AudioOptions.Level(AudioOptions.Effects);source.Play();
        }
        void Close()
        {
            Showing=false;canvas.gameObject.SetActive(false);source.Stop();AudioDirector.Muted=false;AudioListener.pause=wasPaused;
        }
        void Select(int engine,int inductionChoice)
        {
            engineIndex=(engine+engines.Count)%engines.Count;var e=engines[engineIndex];string cls=e.tier??"C";
            inductions=EquipmentCatalog.All.parts.Where(p=>p.slot=="turbo"&&(p.id=="turbo_stock"||(p.tier??"C")==cls)).ToList();
            inductionIndex=(inductionChoice+inductions.Count)%inductions.Count;var t=inductions[inductionIndex];
            var box=EquipmentCatalog.All.parts.FirstOrDefault(p=>p.slot=="transmission"&&(p.tier??"C")==cls);
            var spec=EngineSoundSpec.From(e,t,box);
            // Build the new voice off the audio thread, then swap it in.
            var next=new EngineSynth(spec,AudioSettings.outputSampleRate,17);EngineBanks.Attach(next,17);
            cycle=new RevCycle(spec,t.spoolStart>0?t.spoolStart:.35f,t.spoolFull>0?t.spoolFull:.75f,t.lag);synth=next;
            detail.text=$"Class {cls} · {e.name} · {e.layout} {e.litres:0.0} L · {e.exhaust} exhaust, {e.intake} intake · redline {e.redline:N0}"+(next.Grains!=null?$" · recorded: {spec.recording}":" · modelled");
            induction.text="Induction: "+t.name+(t.blowoff?" (blow-off valve)":"")+(t.antilag?" (anti-lag)":"")+(t.electric?" (electric)":"");
        }
        void Update()
        {
            if(!Showing)return;
            var k=Keyboard.current;var p=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||p?.buttonEast.wasPressedThisFrame==true){Close();return;}
            int de=(k?.dKey.wasPressedThisFrame==true||k?.rightArrowKey.wasPressedThisFrame==true||p?.dpad.right.wasPressedThisFrame==true||p?.rightShoulder.wasPressedThisFrame==true?1:0)
                  -(k?.aKey.wasPressedThisFrame==true||k?.leftArrowKey.wasPressedThisFrame==true||p?.dpad.left.wasPressedThisFrame==true||p?.leftShoulder.wasPressedThisFrame==true?1:0);
            int di=(k?.wKey.wasPressedThisFrame==true||k?.upArrowKey.wasPressedThisFrame==true||p?.dpad.up.wasPressedThisFrame==true?1:0)
                  -(k?.sKey.wasPressedThisFrame==true||k?.downArrowKey.wasPressedThisFrame==true||p?.dpad.down.wasPressedThisFrame==true?1:0);
            if(de!=0)Select(engineIndex+de,0);else if(di!=0)Select(engineIndex,inductionIndex+di);
            if(k?.spaceKey.wasPressedThisFrame==true||k?.enterKey.wasPressedThisFrame==true||p?.buttonSouth.wasPressedThisFrame==true)cycle.Start();
            float free=Mathf.Max(k?.fKey.isPressed==true?1:0,p!=null?p.rightTrigger.ReadValue():0);
            if(free>.05f)cycle.Stop();
            cycle.Step(Mathf.Min(Time.unscaledDeltaTime,.05f),free);
            synth.Set(cycle.Rpm,cycle.Throttle,cycle.Boost,20,cycle.Limiter,false,2,0,0);
            var spec=synth.spec;float frac=Mathf.InverseLerp(spec.idle,spec.redline,cycle.Rpm);int lit=Mathf.RoundToInt(frac*Lamps);
            for(int i=0;i<Lamps;i++)lamps[i].color=i<lit?(i>=Lamps-3?new Color(1,.32f,.27f):i>=Lamps*.75f?new Color(1,.78f,.3f):new Color(.55f,.92f,.55f)):new Color(1,1,1,.12f);
            rpmText.text=$"{cycle.Rpm:N0} rpm"+(spec.turbo?$"   boost {cycle.Boost*100:0}%":"");
            phase.text=cycle.Phase;
        }
        void OnAudioFilterRead(float[] data,int channels)
        {
            var voice=synth;if(!Showing||voice==null){System.Array.Clear(data,0,data.Length);return;}
            int frames=data.Length/channels;voice.Block(frames);
            for(int i=0,k=0;i<frames;i++){float s=voice.Next();for(int c=0;c<channels;c++,k++)data[k]*=s;}
        }
    }
}
