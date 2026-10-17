using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    public sealed class DisplaySettings:MonoBehaviour
    {
        static int windowWidth=1600,windowHeight=900;
        static DisplaySettings instance;
        public static bool Fullscreen=>Screen.fullScreenMode!=FullScreenMode.Windowed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){var go=new GameObject("Display settings");DontDestroyOnLoad(go);instance=go.AddComponent<DisplaySettings>();}
        void Start()
        {
            if(Application.isBatchMode)return;
            // Explicit diagnostic/launcher mode takes precedence over the saved
            // preference without changing what the player selected previously.
            if(RaceTrial.Has("--start-windowed")) {
                if(int.TryParse(RaceTrial.Arg("-screen-width"),out int width))windowWidth=Mathf.Clamp(width,640,7680);
                if(int.TryParse(RaceTrial.Arg("-screen-height"),out int height))windowHeight=Mathf.Clamp(height,480,4320);
                Set(false,false);
            }
            else if(RaceTrial.Has("--start-fullscreen")||PlayerPrefs.GetInt("display.fullscreen",0)==1)Set(true,false);
        }
        void Update(){if(Keyboard.current?.f11Key.wasPressedThisFrame==true)Toggle();}
        public static void Toggle()=>Set(!Fullscreen,true);
        static void Set(bool fullscreen,bool remember)
        {
            if(Application.isBatchMode)return;
            if(fullscreen) {
                if(!Fullscreen){windowWidth=Screen.width;windowHeight=Screen.height;}
                // (the monitor the window is on, at its own resolution: Screen.currentResolution could report a window size
                //  a test run had left behind, and the game came up at 1600x900 on a 4K screen; Richard, 16 Oct)
                var info=Screen.mainWindowDisplayInfo;var display=Screen.currentResolution;
                int w=info.width>0?info.width:display.width,h=info.height>0?info.height:display.height;
                Screen.SetResolution(w,h,FullScreenMode.FullScreenWindow);
            }else Screen.SetResolution(windowWidth,windowHeight,FullScreenMode.Windowed);
            if(remember){PlayerPrefs.SetInt("display.fullscreen",fullscreen?1:0);PlayerPrefs.Save();}
            Debug.Log("LITTLE_ROAD_DISPLAY requested="+(fullscreen?"borderless fullscreen":"windowed"));
            instance.StartCoroutine(LogAppliedMode());
        }
        static System.Collections.IEnumerator LogAppliedMode(){yield return null;yield return null;Debug.Log($"LITTLE_ROAD_DISPLAY applied={Screen.fullScreenMode} size={Screen.width}x{Screen.height}");}
        public readonly struct HudScale:System.IDisposable
        {
            readonly Matrix4x4 previous;
            public readonly float width,height;
            public HudScale(bool enabled){previous=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);width=Screen.width/scale;height=Screen.height/scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));}
            public void Dispose(){GUI.matrix=previous;}
        }
    }
}
