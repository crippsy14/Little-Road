using UnityEngine;

namespace LittleRoad
{
    // Short memory of public motion only. Never reads another driver's controls
    // or equipment. Acceleration evidence decays instead of persisting forever.
    public sealed class ObservedMotionPredictor
    {
        readonly double[] time=new double[64];
        readonly float[] speed=new float[64],acceleration=new float[64];
        readonly bool[] known=new bool[64];
        public float Observe(int id,double timestamp,float velocity)
        {
            if(id<0||id>=known.Length)return 0;
            double dt=timestamp-time[id];
            if(known[id]&&dt<=0)return acceleration[id];
            if(!known[id]||dt>.3)acceleration[id]=0;
            else {
                float measured=Mathf.Clamp((velocity-speed[id])/(float)dt,-25,15);
                acceleration[id]=Mathf.Lerp(acceleration[id],measured,1-Mathf.Exp(-(float)dt/.2f));
            }
            known[id]=true;time[id]=timestamp;speed[id]=velocity;
            return acceleration[id];
        }
        public float Acceleration(int id)=>id>=0&&id<known.Length?acceleration[id]:0;
        public static float Travel(float velocity,float acceleration,float seconds)
        {
            const float decay=.75f;
            float v=Mathf.Max(0,velocity),t=Mathf.Max(0,seconds);
            // Integrate v+a*tau*(1-exp(-t/tau)), stopping before velocity
            // becomes negative. An observed brake is not a prediction of reverse.
            if(acceleration*decay < -v)t=Mathf.Min(t,-decay*Mathf.Log(1+v/(acceleration*decay)));
            return Mathf.Max(0,v*t+acceleration*decay*(t-decay*(1-Mathf.Exp(-t/decay))));
        }
    }
}
