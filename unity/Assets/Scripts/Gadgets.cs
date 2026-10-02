using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Gadgets (Richard, 2 Oct: "a road trip inspired game... rogue things like jet packs / wings down the line... put the
    // blocks in place ahead of time"). A gadget is a road-trip toy bolted to the car beside the seven performance parts:
    // one fitted at a time (AdventureProgress.gadget), worked by its own button (Controls: R1 / G by default), stepped with
    // the car's physics, and showing a charge for the HUD. The catalogue (Resources/Data/gadgets.json) lists what can be
    // sold; the behaviours are registered here by id. None are on sale yet: the "hop" test gadget (--gadget hop) proves
    // the pipeline end to end (button, physics, cooldown) and is the seed of a jet pack.
    public abstract class Gadget
    {
        public string id;
        public RoadCar car;
        // 0..1: how ready it is (a jet pack's fuel, wings' deploy); the HUD shows it.
        public virtual float Charge=>1;
        public virtual void Fitted(RoadCar onCar){car=onCar;}
        // Every physics step while fitted; `held` is the gadget button, `pressed` its press this frame.
        public abstract void Step(bool held,bool pressed,float dt);
        public virtual void Removed(){}
    }

    public static class Gadgets
    {
        [Serializable] public sealed class Entry{public string id,name,hint,tier,discipline;public int price;}
        [Serializable] sealed class Catalogue{public int schema;public Entry[] gadgets=Array.Empty<Entry>();}
        static Catalogue catalogue;
        public static IReadOnlyList<Entry> All{get{if(catalogue==null){var file=Resources.Load<TextAsset>("Data/gadgets");catalogue=file!=null?JsonUtility.FromJson<Catalogue>(file.text):new Catalogue();}return catalogue.gadgets;}}
        // Behaviours by id. A new gadget: a Gadget subclass, an entry here, and a row in gadgets.json.
        static readonly Dictionary<string,Func<Gadget>> behaviours=new(){{"hop",()=>new HopGadget()}};
        public static bool Known(string id)=>string.IsNullOrEmpty(id)||behaviours.ContainsKey(id);
        public static Gadget Create(string id)=>!string.IsNullOrEmpty(id)&&behaviours.TryGetValue(id,out var make)?make():null;
        // What a car carries: the save's gadget for the player, or --gadget <id> for tests.
        public static string ForPlayer(AdventureProgress save)=>RaceTrial.Has("--gadget")?RaceTrial.Arg("--gadget"):save?.gadget;
    }

    // Test gadget: a short upward push (0.35 s), then a 3 s recharge. Not sold; --gadget hop fits it.
    public sealed class HopGadget:Gadget
    {
        float burn,cooldown;
        public const float Push=16f,BurnTime=.35f,Recharge=3f;
        public override float Charge=>cooldown>0?1-cooldown/Recharge:1;
        public override void Step(bool held,bool pressed,float dt)
        {
            if(cooldown>0)cooldown=Mathf.Max(0,cooldown-dt);
            if(pressed&&cooldown<=0&&burn<=0)burn=BurnTime;
            if(burn>0){car.body.AddForce(Vector3.up*Push,ForceMode.Acceleration);burn-=dt;if(burn<=0)cooldown=Recharge;}
        }
    }
}
