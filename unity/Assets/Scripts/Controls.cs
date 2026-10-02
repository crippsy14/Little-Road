using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LittleRoad
{
    // Driving controls, remappable (Options > CONTROLS; Richard, 2 Oct). Each action has one controller button and one
    // key, saved in PlayerPrefs; arrows stay as fixed alternatives for throttle and brake. Manual gears: R2/L2 throttle
    // and brake as in automatic, Cross shifts up and Square down (it used to be the triggers, with the right stick as
    // throttle). Steering stays on the left stick and A/D or the arrows. Menus keep their fixed buttons.
    public static class Controls
    {
        public enum Act{Throttle,Brake,ShiftUp,ShiftDown,LookBehind,Recover,Horn,Gadget}
        public sealed class Binding
        {
            public readonly Act act;public readonly string label,padDefault,keyDefault,help;
            public Binding(Act act,string label,string pad,string key,string help){this.act=act;this.label=label;padDefault=pad;keyDefault=key;this.help=help;}
            public string Pad{get=>PlayerPrefs.GetString("bind.pad."+act,padDefault);set{PlayerPrefs.SetString("bind.pad."+act,value);PlayerPrefs.Save();}}
            public string Key{get=>PlayerPrefs.GetString("bind.key."+act,keyDefault);set{PlayerPrefs.SetString("bind.key."+act,value);PlayerPrefs.Save();}}
        }
        public static readonly Binding[] All={
            new(Act.Throttle,"Accelerate","rightTrigger","w","Analogue on the trigger. (Up arrow always works too.)"),
            new(Act.Brake,"Brake / reverse","leftTrigger","s","Analogue on the trigger. (Down arrow always works too.)"),
            new(Act.ShiftUp,"Shift up (manual)","buttonSouth","leftShift","Manual gears only. Near someone to talk to, Cross / A talks instead."),
            new(Act.ShiftDown,"Shift down (manual)","buttonWest","leftCtrl","Manual gears only."),
            new(Act.LookBehind,"Look behind (hold)","buttonNorth","q","Hold to look back."),
            new(Act.Recover,"Recover to the road","dpad/down","r","Puts the car back on the road nearby."),
            new(Act.Horn,"Horn","leftStickPress","h","Folk answer; traffic beeps back."),
            new(Act.Gadget,"Gadget","rightShoulder","g","Works the fitted gadget (jet packs, wings and the like, when you have one).")};
        public static Binding Of(Act a)=>All[(int)a];
        // Controller buttons a binding may use (Start/Options stays pause; the sticks stay steering).
        public static readonly string[] PadButtons={"buttonSouth","buttonEast","buttonWest","buttonNorth","leftShoulder","rightShoulder","leftTrigger","rightTrigger","leftStickPress","rightStickPress","dpad/up","dpad/down","dpad/left","dpad/right","select"};
        static ButtonControl PadControl(Gamepad g,string path)=>g==null?null:g.TryGetChildControl<ButtonControl>(path);
        static KeyControl KeyControlFor(Keyboard k,string name)=>k==null?null:k.TryGetChildControl<KeyControl>(name);
        // Any connected pad counts (a second controller must not steal input), as elsewhere.
        public static float Value(Act a)
        {
            var b=Of(a);float v=0;
            foreach(var g in Gamepad.all){var c=PadControl(g,b.Pad);if(c!=null)v=Mathf.Max(v,c.ReadValue());}
            var k=Keyboard.current;if(KeyControlFor(k,b.Key)?.isPressed==true)v=1;
            if(a==Act.Throttle&&k?.upArrowKey.isPressed==true||a==Act.Brake&&k?.downArrowKey.isPressed==true)v=1;
            return v;
        }
        public static bool Held(Act a)=>Value(a)>.5f;
        public static bool Pressed(Act a)
        {
            var b=Of(a);
            foreach(var g in Gamepad.all)if(PadControl(g,b.Pad)?.wasPressedThisFrame==true)return true;
            return KeyControlFor(Keyboard.current,b.Key)?.wasPressedThisFrame==true;
        }
        // Set by free roam each frame while a talk / use prompt is ready: Cross talks rather than shifting.
        public static bool InteractionReady;
        // Bind the pad or key; an action already on it takes this one's old binding (a swap, so nothing is lost).
        public static void SetPad(Act a,string path){var b=Of(a);var other=All.FirstOrDefault(x=>x!=b&&x.Pad==path);if(other!=null)other.Pad=b.Pad;b.Pad=path;}
        public static void SetKey(Act a,string key){var b=Of(a);var other=All.FirstOrDefault(x=>x!=b&&x.Key==key);if(other!=null)other.Key=b.Key;b.Key=key;}
        public static void ResetAll(){foreach(var b in All){PlayerPrefs.DeleteKey("bind.pad."+b.act);PlayerPrefs.DeleteKey("bind.key."+b.act);}PlayerPrefs.Save();}
        // Names as printed on the controller or keyboard.
        public static string PadName(string path,InputGlyphs.Family family)
        {
            bool ps=family==InputGlyphs.Family.PlayStation;
            return path switch{
                "buttonSouth"=>ps?"Cross":"A","buttonEast"=>ps?"Circle":"B","buttonWest"=>ps?"Square":"X","buttonNorth"=>ps?"Triangle":"Y",
                "leftShoulder"=>ps?"L1":"LB","rightShoulder"=>ps?"R1":"RB","leftTrigger"=>ps?"L2":"LT","rightTrigger"=>ps?"R2":"RT",
                "leftStickPress"=>ps?"L3":"LS","rightStickPress"=>ps?"R3":"RS","select"=>ps?"Create":"View",
                "dpad/up"=>"D-pad Up","dpad/down"=>"D-pad Down","dpad/left"=>"D-pad Left","dpad/right"=>"D-pad Right",_=>path};
        }
        public static string KeyName(string key)
        {
            var c=KeyControlFor(Keyboard.current,key);if(c!=null)return c.displayName;
            return key switch{"leftShift"=>"Left Shift","leftCtrl"=>"Left Ctrl","rightShift"=>"Right Shift","rightCtrl"=>"Right Ctrl",_=>key.Length==1?key.ToUpperInvariant():key};
        }
        // What to show for an action with the controller in use (keyboard: the key).
        public static string Name(Act a)
        {
            var f=InputGlyphs.Current;var b=Of(a);
            return f==InputGlyphs.Family.Keyboard?KeyName(b.Key):PadName(b.Pad,f);
        }
    }
}
