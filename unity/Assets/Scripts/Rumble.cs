using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    // Controller vibration for the player's car (Options > Driving > Vibration; the genre survey in docs/57 lists it as
    // essential, with an intensity setting, GAG Basic). Knocks give a jolt on both motors scaled by the impact; driving
    // off the tarmac (grass, gravel, dirt: lower grip than the road) gives a low rumble that grows with speed. Silent
    // while paused, in menus (time stopped) or when the window loses focus; --no-rumble turns it off for tests.
    public sealed class Rumble:MonoBehaviour
    {
        RoadCar car;float jolt;bool running;
        public static void Watch(RoadCar car)
        {
            if(Application.isBatchMode||RaceTrial.Has("--no-rumble")||car.GetComponent<Rumble>()!=null)return;
            var r=car.gameObject.AddComponent<Rumble>();r.car=car;
            car.Bumped+=(other,speed)=>{if(speed>=2f)r.jolt=Mathf.Max(r.jolt,Mathf.InverseLerp(2,14,speed)*.9f+.1f);};
        }
        void Update()
        {
            var pad=Gamepad.current;if(pad==null)return;
            float scale=DrivingOptions.VibrationScale;
            if(scale<=0||Time.timeScale<=0||!Application.isFocused||car==null){Stop(pad);return;}
            jolt=Mathf.Max(0,jolt-Time.unscaledDeltaTime*3.5f);
            float rough=car.Grip<car.tuning.roadGrip*.92f?Mathf.InverseLerp(2,20,Mathf.Abs(car.speed))*.22f:0;
            float low=Mathf.Clamp01((jolt+rough)*scale),high=Mathf.Clamp01(jolt*.6f*scale);
            if(low<.01f&&high<.01f){Stop(pad);return;}
            pad.SetMotorSpeeds(low,high);running=true;
        }
        void Stop(Gamepad pad){if(!running)return;pad.SetMotorSpeeds(0,0);running=false;}
        void OnDisable(){if(running)Gamepad.current?.SetMotorSpeeds(0,0);running=false;}
        void OnApplicationFocus(bool focus){if(!focus&&running){Gamepad.current?.SetMotorSpeeds(0,0);running=false;}}
    }
}
