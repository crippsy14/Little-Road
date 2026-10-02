using System;

namespace LittleRoad
{
    // Car horns and the steam train's whistle (Claude's addition, docs/51). Plain C# like the other synthesisers,
    // so the sound harness can render it.
    //   Horn: two notes a major third apart (most car horns are a pair of disc horns tuned about a third apart),
    //   each a rounded square wave with the buzzy edge of a real diaphragm, through a body resonance. `pitch` scales
    //   both: small cars toot high, buses and campers blare low.
    //   Whistle: a chime whistle's minor triad of pipes, breathy with steam noise, swelling in over a fifth of a
    //   second and bending up to pitch as the steam builds.
    public sealed class HornSynth
    {
        readonly float fs;readonly bool whistle;readonly float[] freq;readonly float[] phase;
        Biquad body,air;Noise noise;float env,bend;volatile bool down;
        public HornSynth(float sampleRate,float pitch,bool steamWhistle,uint seed)
        {
            fs=sampleRate;whistle=steamWhistle;noise=new Noise(seed*2654435761u+17);
            freq=whistle?new[]{587f*pitch,698f*pitch,880f*pitch}:new[]{415f*pitch,523f*pitch};phase=new float[freq.Length];
            body=Biquad.Peak(fs,whistle?760*pitch:470*pitch,1.2f,whistle?3:7);air=Biquad.BandPass(fs,whistle?1400*pitch:2400,.8f);
        }
        public void Press(bool on)=>down=on;
        public bool Sounding=>env>1e-4f;
        public float Next()
        {
            // Horn: 6 ms in, 45 ms out. Whistle: 180 ms in, 350 ms out, with the pitch bend.
            float target=down?1:0,rate=whistle?(down?1/(.18f*fs):1/(.35f*fs)):(down?1/(.006f*fs):1/(.045f*fs));
            env+=Math.Clamp(target-env,-rate,rate);if(env<=1e-4f){env=0;bend=0;return 0;}
            bend=down?Math.Min(1,bend+1/(.25f*fs)):bend;
            float x=0;
            for(int i=0;i<freq.Length;i++) {
                float f=freq[i]*(whistle?.94f+.06f*bend:1);phase[i]+=f/fs;if(phase[i]>=1)phase[i]-=1;
                float s=Sine.At(phase[i]);
                x+=whistle?s*(1-.25f*i):SoftClip.Tanh(s*4.5f)*.8f;
            }
            float breath=air.Process(noise.White())*(whistle?.9f:.12f);
            float y=body.Process(x*(whistle?.28f:.34f))+breath*env;
            // (Horns sit with the engines, about 0.25 rms before the voice volume; the whistle carries from afar.)
            return SoftClip.Tanh(y*env*1.2f)*(whistle?.8f:.42f);
        }
    }
}
