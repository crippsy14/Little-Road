using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleRoad
{
    // On by default since 20 Oct (Richard: "remove the flag and include it by default"); --no-stop-look turns it off. (Richard, 17 Oct: "when at a complete stop ... use the right stick to move around the
    // car on both axes so you can admire the area"). Stopped dead in free roam, the right stick swings the chase camera
    // round the car: all the way round, and from near the ground to high above. The view stays where it's left while the
    // car stands; driving off eases it back behind the car. Directions and rates as Photo Mode's (stick up lowers the
    // camera, so the view looks up).
    public static class StopLook
    {
        public static bool Enabled=>!RaceTrial.Has("--no-stop-look");
        // Stopped below 0.15 m/s, moving again above 0.6 m/s (so a car settling on its springs doesn't flicker it).
        // The stick's dead zone; degrees a second at full stick; the camera's height range in degrees above the car's
        // look point; how quickly the view eases in and back.
        public const float StopSpeed=.15f,GoSpeed=.6f,Dead=.15f,YawRate=90,PitchRate=60,MinElevation=3,MaxElevation=75,Ease=5;
        public struct State{public bool stopped;public float yaw,pitch,weight;}
        public static State Now;
        public static Vector2? Injected;   // (--stop-look-review holds the stick)
        public static Vector2 Stick()=>Injected??Gamepad.current?.rightStick.ReadValue()??Vector2.zero;
        // One frame. speed: the car's, m/s; free: nothing else has the view (no menu, not looking behind, controls not
        // blocked); elevation: the chase camera's own height in degrees, so the swing stays inside the range.
        public static State Step(State s,float speed,Vector2 stick,bool free,float dt,float elevation)
        {
            float v=Mathf.Abs(speed);s.stopped=s.stopped?v<GoSpeed:v<StopSpeed;
            bool hold=free&&s.stopped;
            if(hold&&stick.magnitude>Dead) {
                s.yaw=Mathf.Repeat(s.yaw+stick.x*YawRate*dt+180,360)-180;
                s.pitch=Mathf.Clamp(s.pitch-stick.y*PitchRate*dt,MinElevation-elevation,MaxElevation-elevation);
            }
            // (on once the stick has moved the view, held while stopped; off and reset once driving)
            float goal=hold&&(s.yaw!=0||s.pitch!=0)?1:0;
            s.weight=Mathf.Lerp(s.weight,goal,1-Mathf.Exp(-dt*Ease));
            if(goal==1&&s.weight>.995f)s.weight=1;
            if(goal==0&&s.weight<.005f){s.weight=0;s.yaw=0;s.pitch=0;}
            return s;
        }
        public static float Elevation(Vector3 offset)=>Mathf.Atan2(offset.y,new Vector2(offset.x,offset.z).magnitude)*Mathf.Rad2Deg;
        // The camera's offset from its look point, swung by the state: round the vertical by yaw, up or down by pitch,
        // at the same distance.
        public static Vector3 Orbit(Vector3 offset,State s)
        {
            float r=offset.magnitude;if(s.weight<=0||r<1e-4f)return offset;
            float head=Mathf.Atan2(offset.x,offset.z)*Mathf.Rad2Deg+s.yaw*s.weight;
            float e=Mathf.Clamp(Elevation(offset)+s.pitch*s.weight,MinElevation,MaxElevation)*Mathf.Deg2Rad,h=head*Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(h)*Mathf.Cos(e),Mathf.Sin(e),Mathf.Cos(h)*Mathf.Cos(e))*r;
        }
    }
}
