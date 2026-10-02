using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The Options screen (OptionsScreen.cs): every setting the game has is on it, in a tab, with a help line; comfort and
// interface settings default to the full experience; units convert; Step wraps and tells listeners.
public static class OptionsChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var host=new GameObject("Options check host");
        try {
            var screen=host.AddComponent<OptionsScreen>();
            var rows=Enumerable.Range(0,OptionsScreen.Tabs.Length).SelectMany(t=>screen.RowsForTab(t)).ToList();
            Check(Enumerable.Range(0,OptionsScreen.Tabs.Length).All(t=>screen.RowsForTab(t).Count>=2),$"Options has {OptionsScreen.Tabs.Length} tabs ({string.Join(", ",OptionsScreen.Tabs)}), each with at least two settings");
            Check(rows.All(r=>!string.IsNullOrWhiteSpace(r.help))&&rows.Select(r=>r.label).Distinct().Count()==rows.Count,$"All {rows.Count} settings have a help line and a unique name");
            var every=GraphicsOptions.All.Concat(AudioOptions.All).Concat(new[]{GraphicsOptions.MotionBlur,GraphicsOptions.SpeedEffects,GraphicsOptions.Shake,GraphicsOptions.Minimap,GraphicsOptions.Prompts,DrivingOptions.Gears,DrivingOptions.Vibration,Units.Speed});
            var missing=every.Where(o=>rows.All(r=>r.label!=o.label)).Select(o=>o.label).ToList();
            Check(missing.Count==0,"Every setting is on the Options screen"+(missing.Count>0?" (missing: "+string.Join(", ",missing)+")":""));
            Check(new[]{GraphicsOptions.MotionBlur,GraphicsOptions.SpeedEffects,GraphicsOptions.Shake,GraphicsOptions.Minimap,GraphicsOptions.Prompts,DrivingOptions.Vibration,Units.Speed}.All(o=>o.fallback==0)&&AudioOptions.Ambience.fallback==10,
                "Comfort settings default to full (blur, speed effects, shake, vibration on; minimap on; prompts automatic; mph; ambience 100%)");
            int was=Units.Speed.Index;
            try{Units.Speed.Index=0;float mph=Units.FromMps(10);Units.Speed.Index=1;float kmh=Units.FromMps(10);
                Check(Mathf.Abs(mph-22.369f)<.01f&&Mathf.Abs(kmh-36f)<.01f&&Units.Label=="km/h"&&Mathf.Abs(Units.FromMph(60)-96.56f)<.01f,"Speed units: 10 m/s shows as 22.4 mph or 36 km/h; a 60 mph trap mark as 96.6 km/h");}
            finally{Units.Speed.Index=was;}
            int before=GraphicsOptions.Prompts.Index;int heard=0;void Listen(GraphicsOptions.Option o){if(o==GraphicsOptions.Prompts)heard++;}
            GraphicsOptions.Changed+=Listen;
            try{GraphicsOptions.Prompts.Index=0;GraphicsOptions.Step(GraphicsOptions.Prompts,-1);int back=GraphicsOptions.Prompts.Index;GraphicsOptions.Step(GraphicsOptions.Prompts,1);
                Check(back==GraphicsOptions.Prompts.values.Length-1&&GraphicsOptions.Prompts.Index==0&&heard==2,"Stepping a setting wraps both ways and tells whoever is listening (changes apply live)");}
            finally{GraphicsOptions.Changed-=Listen;GraphicsOptions.Prompts.Index=before;}
            // Controls (Options > CONTROLS): manual gears on Cross / Square with the triggers as throttle and brake; a rebind
            // that clashes swaps the two actions (nothing is left unbound); reset restores the defaults.
            var saved=Controls.All.Select(b=>(b.Pad,b.Key)).ToArray();
            try {
                Controls.ResetAll();
                Check(Controls.Of(Controls.Act.Throttle).Pad=="rightTrigger"&&Controls.Of(Controls.Act.Brake).Pad=="leftTrigger"&&Controls.Of(Controls.Act.ShiftUp).Pad=="buttonSouth"&&Controls.Of(Controls.Act.ShiftDown).Pad=="buttonWest",
                    "Driving defaults: R2 / L2 throttle and brake (both gearboxes), Cross / Square shift up and down in manual");
                Check(Controls.PadName("buttonSouth",InputGlyphs.Family.PlayStation)=="Cross"&&Controls.PadName("buttonWest",InputGlyphs.Family.Xbox)=="X"&&Controls.PadName("rightTrigger",InputGlyphs.Family.PlayStation)=="R2","Bindings show as printed on the pad (Cross / A, Square / X, R2 / RT)");
                Controls.SetPad(Controls.Act.ShiftUp,"buttonWest");
                Check(Controls.Of(Controls.Act.ShiftUp).Pad=="buttonWest"&&Controls.Of(Controls.Act.ShiftDown).Pad=="buttonSouth","Rebinding to a button already in use swaps the two actions");
                Controls.SetKey(Controls.Act.Horn,"q");
                Check(Controls.Of(Controls.Act.Horn).Key=="q"&&Controls.Of(Controls.Act.LookBehind).Key=="h","Rebinding a key already in use swaps too");
                Controls.ResetAll();
                Check(Controls.All.All(b=>b.Pad==b.padDefault&&b.Key==b.keyDefault),"Reset all controls restores every default");
                var screen2=new GameObject("Controls check").AddComponent<OptionsScreen>();
                try{var tab=Array.IndexOf(OptionsScreen.Tabs,"CONTROLS");Check(tab>=0&&screen2.RowsForTab(tab).Count==Controls.All.Length+1,"Options has a CONTROLS tab with every driving action and Reset");}
                finally{UnityEngine.Object.DestroyImmediate(screen2.gameObject);}
            } finally {for(int i=0;i<saved.Length;i++){Controls.All[i].Pad=saved[i].Pad;Controls.All[i].Key=saved[i].Key;}}
        } finally {UnityEngine.Object.DestroyImmediate(host);}
        return checks;
    }
}
