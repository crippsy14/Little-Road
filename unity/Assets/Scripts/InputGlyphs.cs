using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace LittleRoad
{
    // Which controls to show: whatever the player touched last (keyboard/mouse, a PlayStation pad or
    // an Xbox-style pad). Prompts and hints follow it so they always match the hands on the controls
    // (Xbox Accessibility Guidelines; "show the correct button prompts for the controller in use").
    public static class InputGlyphs
    {
        public enum Family{Keyboard,PlayStation,Xbox}
        static Family last=Family.Keyboard;static int frame=-1;
        public static Family Current{get{Poll();return last;}}
        static void Poll()
        {
            if(frame==Time.frameCount)return;frame=Time.frameCount;
            var pad=Gamepad.current;
            if(pad!=null&&PadActive(pad))last=pad is UnityEngine.InputSystem.DualShock.DualShockGamepad?Family.PlayStation:Family.Xbox;
            else if(Keyboard.current?.anyKey.isPressed==true||Mouse.current!=null&&(Mouse.current.leftButton.isPressed||Mouse.current.delta.ReadValue().sqrMagnitude>9))last=Family.Keyboard;
        }
        static bool PadActive(Gamepad pad)
        {
            if(pad.leftStick.ReadValue().sqrMagnitude>.09f||pad.rightStick.ReadValue().sqrMagnitude>.09f||pad.leftTrigger.ReadValue()>.2f||pad.rightTrigger.ReadValue()>.2f)return true;
            foreach(var control in pad.allControls)if(control is ButtonControl button&&!(control is AxisControl&&control.parent is StickControl)&&button.isPressed)return true;
            return false;
        }
        // The confirm / talk button, its colour and letter (PlayStation draws a cross instead of a letter).
        public static string Confirm=>Current switch{Family.PlayStation=>"",Family.Xbox=>"A",_=>"E"};
        public static string Name(string action)=>(Current,action) switch{
            (Family.PlayStation,"talk")=>"Cross",(Family.PlayStation,"back")=>"Circle",(Family.PlayStation,"look")=>"Triangle",(Family.PlayStation,"recover")=>"D-pad Down",(Family.PlayStation,"drive")=>"Stick + R2 / L2",(Family.PlayStation,"choose")=>"D-pad",
            (Family.Xbox,"talk")=>"A",(Family.Xbox,"back")=>"B",(Family.Xbox,"look")=>"Y",(Family.Xbox,"recover")=>"D-pad Down",(Family.Xbox,"drive")=>"Stick + RT / LT",(Family.Xbox,"choose")=>"D-pad",
            (Family.PlayStation,"pause")=>"Options",(Family.Xbox,"pause")=>"Menu",(Family.PlayStation,"map")=>"Create",(Family.Xbox,"map")=>"View",(Family.PlayStation,"tabs")=>"L1 / R1",(Family.Xbox,"tabs")=>"LB / RB",
            (_,"talk")=>"E",(_,"back")=>"Esc",(_,"look")=>"Hold Q",(_,"recover")=>"R",(_,"drive")=>"WASD / arrows",(_,"choose")=>"Arrows",(_,"pause")=>"Esc",(_,"map")=>"M",(_,"tabs")=>"Q / E",(Family.PlayStation,"select")=>"Cross",(Family.PlayStation,"category")=>"L1 / R1",(Family.Xbox,"category")=>"LB / RB",(_,"category")=>"Q / E",(Family.Keyboard,"browse")=>"Arrows",(_,"browse")=>"D-pad / stick",(Family.Keyboard,"spin")=>"Z / X",(_,"spin")=>"Right stick",(Family.Xbox,"select")=>"A",(_,"select")=>"Enter",_=>action};
        public static string Driving=>$"{Name("drive")}: drive     {Name("look")}: look behind     {Name("recover")}: recover     {Name("map")}: map     {Name("pause")}: pause";
        // Footer prompts in the pause hub and menus: the same order and wording on every screen.
        public static string Footer(bool tabs,bool select=true,string backLabel="back")=>(select?$"{Name("select")}: select     ":"")+$"{Name("back")}: {backLabel}"+(tabs?$"     {Name("tabs")}: tabs":"");
        public static string Race=>$"{Name("drive")}: drive     {Name("look")}: look behind     {Name("recover")}: recover     {(Current==Family.Keyboard?"Esc":"Options")}: return to town (unfinished races pay nothing)";
        public static string Menu=>$"{Name("choose")}: choose     {Name("select")}: confirm     {Name("back")}: back";
        // Shop footers: "Arrows: choose     Enter: paint it!     Esc: back" in the player's own glyphs.
        public static string Prompts(params (string action,string verb)[] items)=>string.Join("     ",System.Linq.Enumerable.Select(items,i=>$"{Name(i.action)}: {i.verb}"));
    }
}
