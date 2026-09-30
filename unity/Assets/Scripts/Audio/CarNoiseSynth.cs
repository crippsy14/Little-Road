using System;

namespace LittleRoad
{
    // Everything a car makes besides its engine (docs/47 stage 4): tyre squeal near the grip limit, rolling noise
    // by surface (asphalt roar, gravel crackle), wind for the player's car, and impact thuds.
    public sealed class CarNoiseSynth
    {
        readonly float fs;readonly bool wind;
        volatile float inSpeed,inSlip,inDirt;volatile int inImpacts;volatile float inImpact;
        OnePole speed,slip,dirt;float speedTarget,slipTarget,dirtTarget;
        Biquad roar,roar2,squeal,squeal2,crackle,windBand,windLow,thudNoise;Noise noise;
        float wobble,wobbleTarget,wobbleTimer,thudPhase;Decay thud,crunch;int seenImpacts;
        float rollCoef,windCoef;
        public float gain=1;

        public CarNoiseSynth(float sampleRate,bool player,uint seed)
        {
            fs=sampleRate;wind=player;noise=new Noise(seed*747796405u+3);
            roar=Biquad.LowPass(fs,300,.7f);roar2=Biquad.Peak(fs,110,1,4);squeal=Biquad.BandPass(fs,900,14);squeal2=Biquad.BandPass(fs,1750,10);
            crackle=Biquad.BandPass(fs,2600,1.1f);windBand=Biquad.BandPass(fs,500,.6f);windLow=Biquad.LowPass(fs,180,.7f);thudNoise=Biquad.LowPass(fs,2400,.7f);
            speed=new OnePole(fs,.08f);slip=new OnePole(fs,.06f);dirt=new OnePole(fs,.15f);
        }
        // Game thread.
        public void Set(float speedNow,float slipNow,float dirtNow,int impacts,float impact)
        {
            inSpeed=speedNow;inSlip=slipNow;inDirt=dirtNow;inImpact=impact;inImpacts=impacts;
        }
        // Audio thread.
        public void Block(int frames)
        {
            speedTarget=Math.Abs(inSpeed);slipTarget=Math.Clamp(inSlip,0,1.2f);dirtTarget=Math.Clamp(inDirt,0,1);
            float v=speedTarget;
            roar.SetLowPass(fs,180+22*v,.7f);windBand.SetBandPass(fs,380+26*v,.6f);
            rollCoef=.05f*MathF.Pow(Math.Min(v,70)/30,1.3f);windCoef=wind?.0011f*MathF.Pow(Math.Min(v,80),1.6f)/100:0;
            // Squeal pitch wanders like a real tyre (the contact patch stick-slip), faster when sliding harder.
            wobbleTimer-=frames/fs;if(wobbleTimer<=0){wobbleTarget=noise.White();wobbleTimer=.06f+.12f*noise.Unit();}
            wobble+=(wobbleTarget-wobble)*.3f;
            float f=820+140*wobble+60*slipTarget;squeal.SetBandPass(fs,f,14);squeal2.SetBandPass(fs,f*1.93f,10);
            int impacts=inImpacts;
            if(impacts!=seenImpacts){seenImpacts=impacts;float k=Math.Clamp(inImpact/14,0,1.2f);thud.Trigger(fs,.7f*k,.09f);crunch.Trigger(fs,.45f*k,.05f+.1f*k);thudPhase=0;}
        }
        public float Next()
        {
            float v=speed.Process(speedTarget),s=slip.Process(slipTarget),d=dirt.Process(dirtTarget);
            float white=noise.White(),pink=noise.Pink();
            // Rolling: asphalt roar with a body boom; on dirt, louder and gritty (random stones in the tread).
            float roll=roar2.Process(roar.Process(pink))*rollCoef*(1+.8f*d);
            if(d>.01f&&noise.Unit()<v*14/fs)roll+=crackle.Process(1)*d*.25f;else roll+=crackle.Process(0)*d*.25f;
            // Squeal only on tarmac; on dirt the slide is a scrabble (more crackle, handled above).
            float sq=0;
            if(s>.01f){float drive=(squeal.Process(white)+.35f*squeal2.Process(white))*s*s*(1-d)*Math.Min(1,v/6);sq=drive*.9f;}
            float air=wind?(windBand.Process(pink)+.6f*windLow.Process(pink))*windCoef:0;
            float hit=0;
            if(thud.Active){thudPhase+=54f/fs;hit+=Sine.At(thudPhase)*thud.Next();}
            if(crunch.Active)hit+=thudNoise.Process(white)*crunch.Next();
            return SoftClip.Tanh((roll+sq+air+hit)*gain);
        }
    }
}
