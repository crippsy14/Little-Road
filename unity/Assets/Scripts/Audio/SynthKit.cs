using System;

namespace LittleRoad
{
    // Small DSP parts for the live synthesisers (docs/47 stage 4). Plain C# with no Unity calls, so the audio
    // thread can run them and the editor checks can render them offline. No allocation after construction.

    // RBJ-cookbook biquad (transposed direct form II).
    public struct Biquad
    {
        float b0,b1,b2,a1,a2,z1,z2;
        public static Biquad LowPass(float fs,float f,float q){var b=new Biquad();b.SetLowPass(fs,f,q);return b;}
        public static Biquad HighPass(float fs,float f,float q){var b=new Biquad();b.SetHighPass(fs,f,q);return b;}
        public static Biquad BandPass(float fs,float f,float q){var b=new Biquad();b.SetBandPass(fs,f,q);return b;}
        public static Biquad Peak(float fs,float f,float q,float db){var b=new Biquad();b.SetPeak(fs,f,q,db);return b;}
        static void Common(float fs,float f,float q,out float cos,out float alpha)
        {
            float w=2*MathF.PI*Math.Clamp(f,10,fs*.45f)/fs;cos=MathF.Cos(w);alpha=MathF.Sin(w)/(2*Math.Max(.05f,q));
        }
        void Set(float nb0,float nb1,float nb2,float a0,float na1,float na2){b0=nb0/a0;b1=nb1/a0;b2=nb2/a0;a1=na1/a0;a2=na2/a0;}
        public void SetLowPass(float fs,float f,float q){Common(fs,f,q,out float c,out float a);Set((1-c)*.5f,1-c,(1-c)*.5f,1+a,-2*c,1-a);}
        public void SetHighPass(float fs,float f,float q){Common(fs,f,q,out float c,out float a);Set((1+c)*.5f,-(1+c),(1+c)*.5f,1+a,-2*c,1-a);}
        // Constant 0 dB peak gain band-pass.
        public void SetBandPass(float fs,float f,float q){Common(fs,f,q,out float c,out float a);Set(a,0,-a,1+a,-2*c,1-a);}
        public void SetPeak(float fs,float f,float q,float db)
        {
            Common(fs,f,q,out float c,out float a);float g=MathF.Pow(10,db/40);
            Set(1+a*g,-2*c,1-a*g,1+a/g,-2*c,1-a/g);
        }
        public float Process(float x)
        {
            float y=b0*x+z1;z1=b1*x-a1*y+z2;z2=b2*x-a2*y;
            // Denormals and runaway (a filter swept too hard) would poison the voice: flush them.
            if(!(y>-1e6f&&y<1e6f)){z1=z2=0;return 0;}
            return y;
        }
        public void Clear(){z1=z2=0;}
    }

    // One-pole smoother / low-pass. Coefficient from a time constant in seconds.
    public struct OnePole
    {
        public float y;float a;
        public OnePole(float fs,float seconds){y=0;a=seconds<=0?0:MathF.Exp(-1/(fs*seconds));}
        public void SetTime(float fs,float seconds){a=seconds<=0?0:MathF.Exp(-1/(fs*seconds));}
        public float Process(float x){y=x+(y-x)*a;return y;}
    }

    // xorshift32 noise: white in [-1,1], and pink (Paul Kellet's economy filter).
    public struct Noise
    {
        uint state;float p0,p1,p2;
        public Noise(uint seed){state=seed==0?0x9E3779B9u:seed;p0=p1=p2=0;}
        public float White(){state^=state<<13;state^=state>>17;state^=state<<5;return (state&0xFFFFFF)/(float)0x800000-1;}
        public float Unit(){state^=state<<13;state^=state>>17;state^=state<<5;return (state&0xFFFFFF)/(float)0x1000000;}
        public float Pink(){float w=White();p0=.99765f*p0+w*.0990460f;p1=.96300f*p1+w*.2965164f;p2=.57000f*p2+w*1.0526913f;return (p0+p1+p2+w*.1848f)*.25f;}
    }

    // Table sine for oscillators (phase in cycles, 0-1).
    public static class Sine
    {
        const int Size=4096;static readonly float[] table=Build();
        static float[] Build(){var t=new float[Size+1];for(int i=0;i<=Size;i++)t[i]=MathF.Sin(2*MathF.PI*i/Size);return t;}
        public static float At(float phase)
        {
            phase-=MathF.Floor(phase);float f=phase*Size;int i=(int)f;if(i>=Size)i=Size-1;float r=f-i;return t0(i)+(t0(i+1)-t0(i))*r;
        }
        static float t0(int i)=>table[i];
    }

    // Fractional delay line (linear interpolation) for pipes, combs and reverbs.
    public sealed class DelayLine
    {
        readonly float[] buffer;int write;
        public DelayLine(int maxSamples){buffer=new float[Math.Max(4,maxSamples+2)];}
        public int Capacity=>buffer.Length-2;
        public float Read(float delay)
        {
            delay=Math.Clamp(delay,1,buffer.Length-2);float pos=write-delay;if(pos<0)pos+=buffer.Length;
            int i=(int)pos;float r=pos-i;int j=i+1;if(j>=buffer.Length)j=0;return buffer[i]+(buffer[j]-buffer[i])*r;
        }
        public void Write(float x){buffer[write]=x;write++;if(write>=buffer.Length)write=0;}
        public void Clear(){Array.Clear(buffer,0,buffer.Length);}
    }

    // Decaying envelope for one-shot sounds (a click, a thud, a blow-off).
    public struct Decay
    {
        public float level;float k;
        public void Trigger(float fs,float amount,float seconds){level=Math.Max(level,amount);k=MathF.Exp(-1/(fs*Math.Max(.001f,seconds)));}
        public float Next(){float l=level;level*=k;if(level<1e-5f)level=0;return l;}
        public bool Active=>level>0;
    }

    public static class SoftClip
    {
        // Smooth saturation, unity slope at zero, limits at +-1.
        public static float Tanh(float x){if(x>3)return 1;if(x< -3)return -1;float x2=x*x;return x*(27+x2)/(27+9*x2);}
    }
}
