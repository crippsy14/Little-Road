using System;

namespace LittleRoad
{
    // World ambience (docs/47 stage 4), synthesised live: surf on Bayview's coast, the Silver Falls roar, river
    // babble, gusting wind and birds. Plain C# (the harness renders it); Ambience.cs places it in the world.
    // Each generator gives one sample per call; stereo beds call NextStereo for decorrelated left and right.

    // Breaking waves: a low swell builds, the wave crashes (broadband, closing darker), then foam hisses back.
    public sealed class SurfSynth
    {
        readonly float fs;Noise nl,nr;Biquad rumbleL,rumbleR,crashL,crashR,foamL,foamR,bedL,bedR;
        float waveTimer,waveAge=99,waveSize=1,crashTone;
        public SurfSynth(float sampleRate,uint seed)
        {
            fs=sampleRate;nl=new Noise(seed*3+1);nr=new Noise(seed*7+5);
            rumbleL=Biquad.LowPass(fs,220,.7f);rumbleR=Biquad.LowPass(fs,220,.7f);crashL=Biquad.LowPass(fs,3000,.6f);crashR=Biquad.LowPass(fs,3000,.6f);
            foamL=Biquad.BandPass(fs,4200,.6f);foamR=Biquad.BandPass(fs,4600,.6f);bedL=Biquad.LowPass(fs,480,.6f);bedR=Biquad.LowPass(fs,520,.6f);
            waveTimer=2+nl.Unit()*4;
        }
        public void Block(int frames)
        {
            float dt=frames/fs;waveTimer-=dt;waveAge+=dt;
            if(waveTimer<=0){waveTimer=6.5f+nl.Unit()*5;waveAge=0;waveSize=.6f+nl.Unit()*.5f;}
            // The crash closes from bright to dark as the wave spends itself.
            crashTone=waveAge<1.5f?0:Math.Max(0,1-(waveAge-1.5f)/1.4f);
            float f=700+2800*crashTone;crashL.SetLowPass(fs,f,.6f);crashR.SetLowPass(fs,f*1.07f,.6f);
        }
        (float build,float crash,float foam) Envelope()
        {
            float a=waveAge;
            float build=a<1.5f?a/1.5f:Math.Max(0,1-(a-1.5f)/.6f);
            float crash=a<1.5f?0:a<1.65f?(a-1.5f)/.15f:MathF.Exp(-(a-1.65f)/.9f);
            float foam=a<1.7f?0:a<2.2f?(a-1.7f)/.5f:MathF.Exp(-(a-2.2f)/2.6f);
            return (build*waveSize,crash*waveSize,foam*waveSize);
        }
        public (float,float) NextStereo()
        {
            var (b,c,f)=Envelope();float wl=nl.Pink(),wr=nr.Pink(),hl=nl.White(),hr=nr.White();
            float l=bedL.Process(wl)*.35f+rumbleL.Process(wl)*b*.9f+crashL.Process(wl)*c*.9f+foamL.Process(hl)*f*(.5f+.5f*nl.Unit())*.45f;
            float r=bedR.Process(wr)*.35f+rumbleR.Process(wr)*b*.9f+crashR.Process(wr)*c*.9f+foamR.Process(hr)*f*(.5f+.5f*nr.Unit())*.45f;
            return (l*.5f,r*.5f);
        }
    }

    // A waterfall: a dense roar with a slow, uneven swell and splashes on top.
    public sealed class FallsSynth
    {
        readonly float fs;Noise n;Biquad body,low,splash;OnePole swell;float swellTarget,swellTimer;
        public FallsSynth(float sampleRate,uint seed)
        {
            fs=sampleRate;n=new Noise(seed*11+3);body=Biquad.LowPass(fs,2600,.6f);low=Biquad.Peak(fs,180,.8f,4);splash=Biquad.BandPass(fs,3200,.9f);swell=new OnePole(fs,.8f);
        }
        public void Block(int frames){swellTimer-=frames/fs;if(swellTimer<=0){swellTimer=.4f+n.Unit()*1.6f;swellTarget=.8f+n.Unit()*.35f;}}
        public float Next()
        {
            float s=swell.Process(swellTarget);float w=n.Pink();
            float roar=low.Process(body.Process(w))*s;
            float sp=splash.Process(n.Unit()<.004f?n.White()*6:n.White()*.15f);
            return (roar*.55f+sp*.12f)*.7f;
        }
    }

    // A river: running water's hiss plus babble (small bubbles: short sine chirps rising as they burst).
    public sealed class RiverSynth
    {
        readonly float fs;Noise n;Biquad flow;readonly float[] phase=new float[6],freq=new float[6],amp=new float[6],decay=new float[6],rise=new float[6],timer=new float[6];
        public RiverSynth(float sampleRate,uint seed){fs=sampleRate;n=new Noise(seed*13+7);flow=Biquad.BandPass(fs,650,.6f);for(int i=0;i<6;i++)timer[i]=n.Unit()*.1f;}
        public void Block(int frames)
        {
            float dt=frames/fs;
            for(int i=0;i<6;i++){timer[i]-=dt;if(timer[i]<=0){timer[i]=.02f+n.Unit()*.12f;freq[i]=350+n.Unit()*900;amp[i]=.08f+n.Unit()*.12f;float life=.012f+n.Unit()*.03f;decay[i]=MathF.Exp(-1/(fs*life));
                // A bubble's pitch rises 1.3-2x over its short life as it bursts.
                rise[i]=MathF.Exp(MathF.Log(1.3f+n.Unit()*.7f)/(fs*life*2));}}
        }
        public float Next()
        {
            float s=flow.Process(n.Pink())*.35f;
            for(int i=0;i<6;i++){if(amp[i]<1e-4f)continue;phase[i]+=freq[i]/fs;freq[i]*=rise[i];s+=Sine.At(phase[i])*amp[i];amp[i]*=decay[i];}
            return s*.4f;
        }
    }

    // Wind: gusting, wandering band of air, with a leafy rustle when it blows harder.
    public sealed class WindSynth
    {
        readonly float fs;Noise nl,nr;Biquad bandL,bandR,leavesL,leavesR;OnePole gust;float gustTarget=.4f,gustTimer,centre=450;
        public float Exposure=1;   // 0 sheltered .. 1 on a ridge (set by the world)
        public WindSynth(float sampleRate,uint seed)
        {
            fs=sampleRate;nl=new Noise(seed*17+1);nr=new Noise(seed*19+9);bandL=Biquad.BandPass(fs,450,.5f);bandR=Biquad.BandPass(fs,480,.5f);
            leavesL=Biquad.HighPass(fs,3200,.7f);leavesR=Biquad.HighPass(fs,3400,.7f);gust=new OnePole(fs,1.4f);
        }
        public void Block(int frames)
        {
            gustTimer-=frames/fs;
            if(gustTimer<=0){gustTimer=1.5f+nl.Unit()*4.5f;gustTarget=.15f+nl.Unit()*.85f;centre=300+nl.Unit()*450;}
            bandL.SetBandPass(fs,centre,.5f);bandR.SetBandPass(fs,centre*1.08f,.5f);
        }
        public (float,float) NextStereo()
        {
            float g=gust.Process(gustTarget)*(.3f+.7f*Exposure);float l=nl.Pink(),r=nr.Pink();
            float rust=g*g;
            return ((bandL.Process(l)*g+leavesL.Process(nl.White())*rust*.18f)*.35f,(bandR.Process(r)*g+leavesR.Process(nr.White())*rust*.18f)*.35f);
        }
    }

    // Night crickets: a few individuals, each chirping (a short burst of pulses on a high carrier) on its own
    // irregular timing, at its own distance and side. Never in step: a synchronised chorus sounded like a marching band.
    public sealed class CricketSynth
    {
        readonly float fs;Noise n;
        readonly float[] carrier=new float[5],phase=new float[5],gain=new float[5],pan=new float[5],wait=new float[5],pulseRate=new float[5],t=new float[5];
        readonly int[] pulses=new int[5];
        public CricketSynth(float sampleRate,uint seed)
        {
            fs=sampleRate;n=new Noise(seed*29+3);
            for(int i=0;i<5;i++){carrier[i]=3900+n.Unit()*1300;gain[i]=.15f+n.Unit()*.5f;pan[i]=n.Unit();wait[i]=n.Unit()*1.5f;pulseRate[i]=24+n.Unit()*14;}
        }
        public void Block(int frames)
        {
            float dt=frames/fs;
            for(int i=0;i<5;i++){if(pulses[i]>0)continue;wait[i]-=dt;if(wait[i]<=0){pulses[i]=2+(int)(n.Unit()*3);t[i]=0;wait[i]=.7f+n.Unit()*2.2f;}}
        }
        public (float,float) NextStereo()
        {
            float l=0,r=0;
            for(int i=0;i<5;i++) {
                if(pulses[i]<=0)continue;
                t[i]+=1/fs;float period=1/pulseRate[i],k=t[i]%period/period;
                if(t[i]>=period*pulses[i]){pulses[i]=0;continue;}
                float env=k<.45f?MathF.Sin(k/.45f*MathF.PI):0;phase[i]+=carrier[i]/fs;
                float x=Sine.At(phase[i])*env*gain[i]*.08f;l+=x*(1-pan[i]);r+=x*pan[i];
            }
            return (l,r);
        }
    }

    // Interface tones: the race countdown's beep-beep-beep-BEEP (a bright, slightly square arcade tone).
    public sealed class ToneSynth
    {
        readonly float fs;readonly System.Collections.Generic.Queue<(float,float)> queue=new();volatile int pending;
        float phase,freq,t,length;
        public ToneSynth(float sampleRate){fs=sampleRate;}
        public void Play(float hz,float seconds){lock(queue){queue.Enqueue((hz,seconds));pending=queue.Count;}}
        public float Next()
        {
            if(t>=length) {
                if(pending==0)return 0;
                lock(queue){if(queue.Count==0){pending=0;return 0;}(freq,length)=queue.Dequeue();pending=queue.Count;}
                t=0;
            }
            t+=1/fs;phase+=freq/fs;
            float env=Math.Min(1,t/.004f)*Math.Min(1,(length-t)/.03f);
            return (Sine.At(phase)*.7f+Sine.At(phase*2)*.2f+Sine.At(phase*3)*.1f)*env*.3f;
        }
    }

    // Quarry Hills' blasting (docs/60, ambience by area): the motor siren winds up, holds and runs down, then the shot
    // thumps out of the pit and rumbles round the benches. Fire() starts one (from the main thread); silent between.
    public sealed class BlastSynth
    {
        public const float SirenEnd=14,Shot=21,End=31;
        readonly float fs;Noise n;Biquad thump,rumble,rotor;float t=End,phase;volatile bool fire;
        public BlastSynth(float sampleRate,uint seed){fs=sampleRate;n=new Noise(seed*23+3);thump=Biquad.LowPass(fs,70,.9f);rumble=Biquad.LowPass(fs,260,.6f);rotor=Biquad.BandPass(fs,900,.7f);}
        public void Fire()=>fire=true;
        public bool Busy=>t<End;
        public float Next()
        {
            if(fire){fire=false;t=0;}
            if(t>=End)return 0;
            t+=1/fs;float x=0;
            if(t<SirenEnd) {
                // Wind-up 0-4 s, hold to 10 s (a slow wobble), run-down to 14 s: pitch and loudness follow the rotor.
                float spin=t<4?Smooth(t/4):t<10?1+.015f*MathF.Sin(t*2.1f):Smooth(1-(t-10)/4);
                float f=90+540*spin;phase+=f/fs;if(phase>1)phase-=1;
                float tone=Sine.At(phase)*.6f+Sine.At(phase*2)*.25f+Sine.At(phase*3)*.12f+rotor.Process(n.White())*.25f;
                x+=tone*spin*spin*.55f;
            }
            if(t>=Shot) {
                float a=t-Shot;
                x+=thump.Process(n.White())*MathF.Exp(-a/.35f)*5.5f;          // the shot
                x+=rumble.Process(n.Pink())*MathF.Exp(-a/2.6f)*Math.Min(1,a/.15f)*1.6f;   // its rumble round the pit
            }
            return x;
        }
        static float Smooth(float k){k=Math.Clamp(k,0,1);return k*k*(3-2*k);}
    }
}
