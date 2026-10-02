using System;

namespace LittleRoad
{
    // Fireworks' sounds (Claude's addition, docs/51): the launch's rising whoosh, the burst's boom (a low thump and a
    // noise crack), and a willow's crackle. Events are queued from the game thread with a delay in samples (the
    // boom arrives at the speed of sound) and mixed on the audio thread. Plain C#, so the harness can render it.
    public sealed class FireworkSynth
    {
        public enum Kind{Launch,Boom,Crackle}
        sealed class Voice{public Kind kind;public int wait,age,length;public float size,phase;public Biquad band,low;}
        readonly float fs;readonly Voice[] voices=new Voice[24];readonly System.Collections.Generic.Queue<(Kind,int,float)> pending=new();Noise noise;
        public FireworkSynth(float sampleRate,uint seed){fs=sampleRate;noise=new Noise(seed*2246822519u+5);for(int i=0;i<voices.Length;i++)voices[i]=new Voice{length=0};}
        // Game thread.
        public void Play(Kind kind,float delaySeconds,float size){lock(pending)pending.Enqueue((kind,(int)(Math.Max(0,delaySeconds)*fs),size));}
        public float Next()
        {
            if(pending.Count>0)lock(pending)while(pending.Count>0){var (k,w,s)=pending.Dequeue();Start(k,w,s);}
            float x=0;
            foreach(var v in voices) {
                if(v.length==0)continue;if(v.wait>0){v.wait--;continue;}
                float t=v.age/fs,life=v.age/(float)v.length;v.age++;if(v.age>=v.length){v.length=0;continue;}
                float w=noise.White();
                switch(v.kind) {
                    case Kind.Launch: {
                        // Rising whoosh: a band of noise sweeping up, with a thin whistle on top, fading as it climbs.
                        float f=600+2400*life;v.band.SetBandPass(fs,f,3);v.phase+=f*1.02f/fs;if(v.phase>1)v.phase-=1;
                        float env=Math.Min(1,t*12)*(1-life);x+=(v.band.Process(w)*.9f+Sine.At(v.phase)*.05f)*env*.5f*v.size;break;}
                    case Kind.Boom: {
                        // A thump (a falling low sine) and a crack of low-passed noise.
                        v.phase+=(38+30*(float)Math.Exp(-t*18))/fs;if(v.phase>1)v.phase-=1;
                        float thump=Sine.At(v.phase)*(float)Math.Exp(-t*5.5f),crack=v.low.Process(w)*(float)Math.Exp(-t*14);
                        x+=(thump*.9f+crack*.8f)*v.size;break;}
                    case Kind.Crackle: {
                        // Scattered clicks, thinning out.
                        if(noise.Unit()<.0022f*(1-life)){v.band.SetBandPass(fs,1800+noise.Unit()*3200,2.5f);v.phase=1;}
                        v.phase*=.985f;x+=v.band.Process(w)*v.phase*.6f*v.size;break;}
                }
            }
            return SoftClip.Tanh(x);
        }
        void Start(Kind kind,int wait,float size)
        {
            foreach(var v in voices)if(v.length==0){
                v.kind=kind;v.wait=wait;v.age=0;v.size=size;v.phase=0;
                v.length=(int)(fs*(kind==Kind.Launch?1.3f:kind==Kind.Boom?1.6f:2.2f));
                v.band=Biquad.BandPass(fs,1000,3);v.low=Biquad.LowPass(fs,900,.7f);return;}
        }
    }
}
