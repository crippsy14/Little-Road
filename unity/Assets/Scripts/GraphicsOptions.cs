using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // Player graphics options (pause menu > Graphics), saved in PlayerPrefs and applied live.
    // Command-line switches (--aa, --no-grass) still win for reviews and trials.
    public static class GraphicsOptions
    {
        public sealed class Option
        {
            public readonly string key,label;public readonly string[] values;public readonly int fallback;
            public Option(string key,string label,int fallback,params string[] values){this.key=key;this.label=label;this.fallback=fallback;this.values=values;}
            public int Index{get=>Mathf.Clamp(PlayerPrefs.GetInt("gfx."+key,fallback),0,values.Length-1);set{PlayerPrefs.SetInt("gfx."+key,value);PlayerPrefs.Save();}}
            public string Value=>values[Index];
        }
        public static readonly Option AntiAliasing=new("aa","Anti-aliasing",0,"TAA","SMAA","Off");
        public static readonly Option Grass=new("grass","Grass",0,"High","Low","Off");
        public static readonly Option Shadows=new("shadows","Shadows",0,"High","Medium");
        public static readonly Option FrameRate=new("pacing","Frame rate",0,"VSync","60","120","Unlimited");
        public static readonly Option FieldOfView=new("fov","Field of view",0,"Normal","Wide");
        public static readonly Option Time=new("time2","Time of day",0,"Cycle","Morning","Afternoon","Golden hour","Dusk","Night");
        public static readonly Option[] All={Time,AntiAliasing,Grass,Shadows,FrameRate,FieldOfView};
        // Comfort and interface (the Options screen; the genre survey in docs/57: XAG 117 asks for camera shake, motion
        // blur and speed-effect toggles; units and a minimap toggle are standard).
        public static readonly Option MotionBlur=new("blur","Motion blur",0,"On","Off");
        public static readonly Option SpeedEffects=new("speedfx","Speed effects",0,"Full","Reduced","Off");
        public static readonly Option Shake=new("shake","Camera shake",0,"Full","Reduced","Off");
        public static readonly Option Minimap=new("minimap","Minimap",0,"On","Off");
        public static readonly Option Prompts=new("prompts","Button prompts",0,"Automatic","Keyboard","PlayStation","Xbox");
        static float Scale3(Option o)=>!Active?1:o.Index switch{1=>.5f,2=>0,_=>1};
        public static float SpeedEffectScale=>Scale3(SpeedEffects);
        public static float ShakeScale=>Scale3(Shake);
        public static bool MinimapShown=>!Active||Minimap.Index==0;
        // Every change from the Options screen goes through Step, so whoever is listening applies it live
        // (free roam: grass; races: their pacing; everywhere: camera, shadows, blur, audio levels).
        public static event Action<Option> Changed;
        public static void Step(Option option,int direction)
        {
            int n=option.values.Length;option.Index=((option.Index+direction)%n+n)%n;
            ApplyCommon(option);Changed?.Invoke(option);
        }
        static void ApplyCommon(Option o)
        {
            if(o==FrameRate){if(RaceTrial.Racing)ApplyRacePacing();else ApplyPacing();}
            else if(o==Shadows)ApplyShadows(RaceTrial.Racing);
            else if(o==AntiAliasing&&UnityEngine.Camera.main!=null)WorldLook.Camera(UnityEngine.Camera.main);
            else if(o==Time)TimeOfDay.Apply();
            else if(o==MotionBlur)WorldLook.RaceBlurSetting();
            else if(Array.IndexOf(AudioOptions.All,o)>=0)AudioDirector.Apply();
        }
        // Reviews, frame-cost studies and batch trials always measure the defaults.
        public static bool Active=>!Application.isBatchMode&&!RaceTrial.Has("--world-review");
        public static string AaMode=>!Active?"taa":AntiAliasing.Index switch{1=>"smaa",2=>"off",_=>"taa"};
        public static float GrassDensity=>!Active?1:Grass.Index switch{1=>.45f,2=>0,_=>1};
        public static float FovOffset=>Active&&FieldOfView.Index==1?8:0;
        public static float ShadowScale=>Active&&Shadows.Index==1?.6f:1;
        public static void Cycle(Option option)=>Step(option,1);
        // Presets set the detail options together (Low suits older laptops; Ultra is today's default look).
        public static readonly string[] Presets={"Low","Medium","High","Ultra"};
        public static string PresetName{get{
            for(int p=0;p<Presets.Length;p++)if(PresetValues(p)==(AntiAliasing.Index,Grass.Index,Shadows.Index))return Presets[p];
            return "Custom";}}
        static (int aa,int grass,int shadows) PresetValues(int p)=>p switch{0=>(1,2,1),1=>(1,1,1),2=>(0,1,0),_=>(0,0,0)};
        public static void NextPreset()
        {
            int current=System.Array.IndexOf(Presets,PresetName);var (aa,grass,shadows)=PresetValues((current+1)%Presets.Length);
            AntiAliasing.Index=aa;Grass.Index=grass;Shadows.Index=shadows;
        }
        // Display: distinct resolutions the monitor offers, and the window mode.
        public static string ResolutionName=>Screen.fullScreenMode==FullScreenMode.Windowed?$"{Screen.width} x {Screen.height}":$"{Screen.currentResolution.width} x {Screen.currentResolution.height}";
        public static void NextResolution()
        {
            var sizes=new System.Collections.Generic.List<Vector2Int>();
            foreach(var r in Screen.resolutions){var s=new Vector2Int(r.width,r.height);if(s.x>=1280&&!sizes.Contains(s))sizes.Add(s);}
            if(sizes.Count==0)return;int at=sizes.IndexOf(new Vector2Int(Screen.width,Screen.height));var next=sizes[(at+1)%sizes.Count];
            Screen.SetResolution(next.x,next.y,Screen.fullScreenMode);
        }
        public static string ModeName=>Screen.fullScreenMode switch{FullScreenMode.ExclusiveFullScreen=>"Fullscreen",FullScreenMode.FullScreenWindow=>"Borderless",_=>"Windowed"};
        public static void NextMode()
        {
            var next=Screen.fullScreenMode switch{FullScreenMode.ExclusiveFullScreen=>FullScreenMode.FullScreenWindow,FullScreenMode.FullScreenWindow=>FullScreenMode.Windowed,_=>FullScreenMode.ExclusiveFullScreen};
            Screen.fullScreenMode=next;PlayerPrefs.SetInt("display.fullscreen",next==FullScreenMode.Windowed?0:1);PlayerPrefs.Save();
        }
        public static void ApplyPacing()
        {
            switch(Active?FrameRate.Index:0){
                case 1:QualitySettings.vSyncCount=0;Application.targetFrameRate=60;break;
                case 2:QualitySettings.vSyncCount=0;Application.targetFrameRate=120;break;
                case 3:QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;break;
                default:QualitySettings.vSyncCount=1;Application.targetFrameRate=-1;break;
            }
        }
        // Races: 23 AI drivers step at a fixed 120 Hz, and a frame that carries a step costs ~8-9 ms. On a
        // high-refresh display (144 Hz = 6.9 ms) those frames missed the refresh and the race juddered
        // (frame-cost-127). With VSync, races sync to every second refresh above 100 Hz (144 -> 72 fps),
        // a steady budget every step fits. Other frame-rate choices are left as chosen.
        public static void ApplyRacePacing()
        {
            ApplyPacing();
            if(QualitySettings.vSyncCount==1&&Screen.currentResolution.refreshRateRatio.value>100)QualitySettings.vSyncCount=2;
        }
        public static void ApplyShadows(bool racing)
        {
            if(GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp)return;
            urp.shadowDistance=(racing?WorldLook.ShadowDistanceRacing:WorldLook.ShadowDistance)*ShadowScale;
            urp.mainLightShadowmapResolution=ShadowScale<1?2048:4096;
        }
    }

    // Driving settings beside the graphics ones (same PlayerPrefs pattern). Gears: automatic by default; manual
    // shifts on R2/L2 (Left Shift/Left Ctrl) with throttle and brake on the right stick (docs/47 stage 3).
    public static class DrivingOptions
    {
        public static readonly GraphicsOptions.Option Gears=new("gears","Gears",0,"Automatic","Manual");
        public static readonly GraphicsOptions.Option Vibration=new("rumble","Vibration",0,"Full","Reduced","Off");
        public static float VibrationScale=>Vibration.Index switch{1=>.5f,2=>0,_=>1};
        public static bool Manual=>Gears.Index==1||RaceTrial.Has("--manual-gears");
    }
}

namespace LittleRoad
{
    // Speed shown to the player in their chosen unit (Options > Driving). Speeds are m/s inside the game; speed-trap
    // marks and records are kept in mph and shown converted.
    public static class Units
    {
        public static readonly GraphicsOptions.Option Speed=new("units","Speed units",0,"mph","km/h");
        public const float MphPerMps=2.2369363f,KmhPerMps=3.6f;
        public static bool Metric=>Speed.Index==1;
        public static string Label=>Metric?"km/h":"mph";
        public static float FromMps(float mps)=>mps*(Metric?KmhPerMps:MphPerMps);
        public static float FromMph(float mph)=>Metric?mph*1.609344f:mph;
        public static string Show(float mps,string format="0")=>FromMps(mps).ToString(format)+" "+Label;
        public static string ShowMph(float mph,string format="0")=>FromMph(mph).ToString(format)+" "+Label;
    }
}
