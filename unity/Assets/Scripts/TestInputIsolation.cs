using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    // Test runs (--silent, batch mode, --trial-auto, any --*-test/-review/-audit) ignore the real gamepads, keyboards and mice (4 Oct: Richard's controller, read by every running
    // copy of the game whatever has the focus, paused a hidden test window mid-capture). Tests drive the game through code
    // and virtual pads (InputSystem.AddDevice), which stay on: only native devices are disabled.
    static class TestInputIsolation
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            var args=System.Environment.GetCommandLineArgs();
            bool test=Application.isBatchMode||args.Any(a=>a=="--silent"||a=="--audio-capture-silent"||a=="--trial-auto"||a.StartsWith("--")&&(a.EndsWith("-test")||a.EndsWith("-review")||a.EndsWith("-audit")));
            if(!test)return;
            foreach(var d in InputSystem.devices.ToArray())Off(d);
            InputSystem.onDeviceChange+=(d,change)=>{if(change==InputDeviceChange.Added||change==InputDeviceChange.Reconnected)Off(d);};
            Debug.Log("LITTLE_ROAD_TEST_INPUT real devices ignored (a test run)");
        }
        static void Off(InputDevice d)
        {
            if(d.native&&(d is Gamepad||d is Keyboard||d is Mouse||d is Joystick)&&d.enabled)InputSystem.DisableDevice(d);
        }
    }
}
