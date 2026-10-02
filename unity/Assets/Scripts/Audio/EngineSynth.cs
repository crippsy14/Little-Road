using System;

namespace LittleRoad
{
    // What an engine, its induction and its gearbox sound like (docs/47 stage 4), from the parts' fingerprints.
    public sealed class EngineSoundSpec
    {
        public string id="engine";
        public int cylinders=4,banks=1;
        public float[] angle;public int[] bank;   // firing angle in the 720-degree cycle and exhaust bank, in firing order
        public float litres=1.2f,idle=850,redline=6000;
        public string exhaust="road",intake="box",shiftType="H",firing="even",recording="";
        public bool hybrid,turbo,blowoff,antilag,electricTurbo;
        public float turboPower=1;

#if !SOUND_HARNESS
        public static EngineSoundSpec From(EquipmentCatalog.Part engine,EquipmentCatalog.Part induction,EquipmentCatalog.Part box)
        {
            var s=new EngineSoundSpec{id=engine.id,litres=engine.litres>0?engine.litres:1.2f,idle=engine.idleRpm>0?engine.idleRpm:850,
                redline=engine.redline>0?engine.redline:6000,exhaust=engine.exhaust??"road",intake=engine.intake??"box",hybrid=engine.hybrid,
                firing=engine.firing??"even",recording=engine.sound??"",shiftType=string.IsNullOrEmpty(box?.shiftType)?"H":box.shiftType};
            if(induction!=null){s.turboPower=Math.Max(1,induction.power);s.turbo=s.turboPower>1.001f;s.blowoff=induction.blowoff;s.antilag=induction.antilag;s.electricTurbo=induction.electric;}
            s.Layout(engine.layout,engine.cylinders);
            return s;
        }
#endif
        public void Layout(string layout,int count)
        {
            cylinders=Math.Clamp(count>0?count:4,1,12);bool vee=!string.IsNullOrEmpty(layout)&&layout.StartsWith("V");banks=vee?2:1;
            angle=new float[cylinders];bank=new int[cylinders];
            // Cross-plane V8 (firing order 1-8-7-2-6-5-4-3): each bank fires unevenly, 180/270/180/90 degrees.
            int[] crossplane={0,1,0,1,1,0,1,0};
            for(int i=0;i<cylinders;i++){angle[i]=720f*i/cylinders;bank[i]=!vee?0:firing=="crossplane"&&cylinders==8?crossplane[i]:i%2;}
        }
        public float FiringHz(float rpm)=>rpm/60f*cylinders*.5f;
    }

    // Live engine voice: firing pulses at the real crank angles into per-bank exhaust pipes and mufflers, an intake,
    // valve-train and gear noise, a turbo, a hybrid motor, shift clunks, overrun pops and the limiter.
    // The main thread writes the inputs (Set); the audio thread calls Block then Next per sample.
    public sealed class EngineSynth
    {
        public readonly EngineSoundSpec spec;readonly float fs;
        // Inputs (written by the game, read at the start of each audio block).
        volatile float inRpm,inThrottle,inBoost,inSpeed;volatile bool inLimiter,inShifting;volatile int inUp,inDown,inGear;

        // Crank.
        float rpm,rpmStep,phase;int next;uint cycle;
        // Smoothed controls.
        OnePole load,throttleS,boostS,speedS;float loadTarget,throttleTarget,boostTarget,speedTarget;
        // Per bank: pulse shaper, pipe, muffler, body.
        readonly float[] s1,s2,imp,impNext,rough,bankGain;readonly DelayLine[] pipe;readonly float[] pipeDelay;readonly Biquad[] muffler,muffler2,body;
        float pulseCoef,reflect,exhaustGain,rasp,pulseScale,jitter,popChance;
        // Intake, roar, mechanical.
        Biquad intakeTone,intakeHiss,roarFilter,tickFilter;float intakeImp,intakePulseGain,intakeHissGain,roarGain;Decay tick;
        // Turbo.
        // Turbo and hybrid-motor sounds are off (Richard, 2 Oct: "remove the turbo noises all together as they've only
        // really ever caused complication... a nice to have for now"). Both were synthesised tones: the turbo's whistle
        // and blow-off, and the hybrid motor's whine, which climbed with road speed to ~4 kHz (a 12 kHz harmonic) at
        // full throttle on the maximum build's twelve-hour hybrid. --turbo-sound / --hybrid-sound bring them back.
        // The output's DC blocker (a 2nd-order high-pass). At 25 Hz it took 4-12 dB off a recorded engine's lowest orders
        // at low revs (cycle rate 8-17 Hz at 1000-2000 rpm): the rumble (docs/55). 12 Hz still blocks DC.
        public static float DcCut=12;
        public static readonly bool TurboSound=Array.IndexOf(Environment.GetCommandLineArgs(),"--turbo-sound")>=0;
        public static readonly bool HybridSound=Array.IndexOf(Environment.GetCommandLineArgs(),"--hybrid-sound")>=0;
        float shaft,whistlePhase,whistle2Phase;Biquad whoosh,bovFilter,flutterFilter;Decay bov,flutter;float flutterPhase,lastThrottle=0,bovCooldown;
        // Hybrid and gear whine.
        float motorPhase,gearPhase;
        // Shifts.
        int seenUp,seenDown;Decay thump,click,bang;float thumpPhase;Biquad clickFilter;float blip;
        // Output.
        Biquad dcBlock;Noise noise;public float gain=1;public int Solo=-1,Mute=-1;
        // Optional recorded engine (tools/engine-grains.py): set before the first block, then recalibrate.
        public GrainSynth Grains;public float GrainGain=1;
        // The part's exhaust shapes a recording shared between parts: road muffled, sport fuller, race bright and raspy.
        Biquad grainTone,turboMuffle;float grainDrive=1,spool;
        public void UseGrains(GrainSynth grains,float level=1){Grains=grains;GrainGain=level;Calibrate();}
        public float LastRms {get;private set;}

        public EngineSynth(EngineSoundSpec spec,float sampleRate,uint seed)
        {
            this.spec=spec;fs=sampleRate;noise=new Noise(seed*2654435761u+1);
            int b=spec.banks;s1=new float[b];s2=new float[b];imp=new float[b];impNext=new float[b];rough=new float[b];bankGain=new float[b];pipe=new DelayLine[b];pipeDelay=new float[b];
            muffler=new Biquad[b];muffler2=new Biquad[b];body=new Biquad[b];
            // Exhaust: pipe length, open-end reflection, muffler cut-off, body boom, rasp (saturation) and level.
            float pipeLength,cutoff,boom,boomDb;
            switch(spec.exhaust) {
                case "race": pipeLength=1.1f;reflect=-.62f;cutoff=5200;boom=240;boomDb=3;rasp=1.8f;exhaustGain=1f;popChance=.05f;break;
                case "sport": pipeLength=2.0f;reflect=-.5f;cutoff=1900;boom=140;boomDb=5;rasp=1.2f;exhaustGain=.8f;popChance=.02f;break;
                default: pipeLength=2.8f;reflect=-.35f;cutoff=680;boom=105;boomDb=6;rasp=1f;exhaustGain=.6f;popChance=0;break;
            }
            // Even-fire vees: near-identical banks, so the note is the firing frequency. Cross-plane V8: the near
            // bank louder than the far one (dual exhaust heard from one side), which brings out each bank's
            // uneven 180/270/180/90 rhythm: the burble.
            bool burble=spec.firing=="crossplane";
            for(int i=0;i<b;i++) {
                float length=pipeLength*(1+(burble?.06f:0f)*i);pipeDelay[i]=2*length/480f*fs;pipe[i]=new DelayLine((int)pipeDelay[i]+4);
                bankGain[i]=i==0||!burble?1:.62f;
                muffler[i]=Biquad.LowPass(fs,cutoff*(1+(burble?.04f:0)*i),.7f);muffler2[i]=Biquad.LowPass(fs,cutoff*2.2f,.6f);body[i]=Biquad.Peak(fs,boom,1.1f,boomDb);
            }
            // Bigger cylinders make bigger pulses; race cams idle lumpier.
            pulseScale=MathF.Sqrt(Math.Max(.15f,spec.litres/spec.cylinders)/.3f);
            jitter=spec.exhaust=="race"?.14f:spec.exhaust=="sport"?.08f:.05f;
            switch(spec.intake) {
                case "itb": intakeTone=Biquad.BandPass(fs,560,4);intakePulseGain=.55f;intakeHiss=Biquad.BandPass(fs,1900,.8f);intakeHissGain=.05f;break;
                case "trumpets": intakeTone=Biquad.BandPass(fs,820,5);intakePulseGain=.6f;intakeHiss=Biquad.BandPass(fs,2600,.8f);intakeHissGain=.06f;break;
                case "cone": intakeTone=Biquad.BandPass(fs,420,2);intakePulseGain=.12f;intakeHiss=Biquad.BandPass(fs,1500,.9f);intakeHissGain=.07f;break;
                default: intakeTone=Biquad.BandPass(fs,330,1.5f);intakePulseGain=.05f;intakeHiss=Biquad.BandPass(fs,420,1.2f);intakeHissGain=.04f;break;
            }
            roarFilter=Biquad.LowPass(fs,Math.Min(cutoff,2400),.6f);roarGain=spec.exhaust=="race"?.09f:spec.exhaust=="sport"?.07f:.05f;
            tickFilter=Biquad.HighPass(fs,3500,.7f);
            whoosh=Biquad.BandPass(fs,1600,1.4f);bovFilter=Biquad.BandPass(fs,2800,.9f);flutterFilter=Biquad.LowPass(fs,900,.7f);
            clickFilter=Biquad.BandPass(fs,spec.shiftType=="H"?3200:2200,spec.shiftType=="H"?2:6);
            dcBlock=Biquad.HighPass(fs,DcCut,.7f);
            // A turbo sits in the exhaust: turbo cars sound a little smoother and deeper than the same engine without.
            turboMuffle=Biquad.LowPass(fs,3600,.6f);
            switch(spec.exhaust){case "raw":grainTone=Biquad.Peak(fs,1000,1,0);break;   // (the comparison harness: the recording untouched)
                case "race":grainTone=Biquad.Peak(fs,2800,.9f,4);grainDrive=1.5f;break;case "sport":grainTone=Biquad.Peak(fs,170,.8f,2.5f);break;default:grainTone=Biquad.LowPass(fs,2600,.6f);break;}
            load=new OnePole(fs,.03f);throttleS=new OnePole(fs,.04f);boostS=new OnePole(fs,.05f);speedS=new OnePole(fs,.1f);
            rpm=spec.idle;inRpm=spec.idle;
            Calibrate();
        }

        // Game thread: the car's state this frame.
        public void Set(float rpmNow,float throttle,float boost,float speed,bool limiter,bool shifting,int gear,int upshifts,int downshifts)
        {
            inRpm=rpmNow;inThrottle=throttle;inBoost=boost;inSpeed=speed;inLimiter=limiter;inShifting=shifting;inGear=gear;inUp=upshifts;inDown=downshifts;
        }

        // Audio thread: start of a block of `frames` samples.
        public void Block(int frames)
        {
            float target=Math.Clamp(inRpm,spec.idle*.6f,spec.redline*1.03f);rpmStep=(target-rpm)/Math.Max(1,frames);
            float t=inThrottle;throttleTarget=Math.Max(0,t);speedTarget=Math.Abs(inSpeed);boostTarget=spec.turbo?Math.Clamp(inBoost,0,1):0;
            // Load: throttle, cut during a shift (sequential and paddle boxes blip on the way down), light at idle.
            bool shifting=inShifting;
            loadTarget=shifting?blip:Math.Max(throttleTarget,.1f);
            // Pulse width follows the exhaust valve's open time (about 60 crank degrees).
            float tau=Math.Clamp(2.4f/Math.Max(500,target),.00012f,.0015f);pulseCoef=MathF.Exp(-1/(fs*tau));
            int up=inUp,down=inDown;
            if(up!=seenUp){if(seenUp!=0||up==1)Shift(1);seenUp=up;}
            if(down!=seenDown){if(seenDown!=0||down==1)Shift(-1);seenDown=down;}
            if(!shifting)blip=0;
            // Blow-off or flutter on a sharp lift with boost up.
            bovCooldown-=frames/fs;
            if(spec.turbo&&TurboSound&&lastThrottle>.55f&&throttleTarget<.25f&&boostS.y>.3f&&bovCooldown<=0) {
                // Blow-off valve: a short "pssh" whose tone falls as the pressure drops. Without one (a road car's
                // recirculating valve) only a soft, dark whoosh as the turbo spins down (Richard, 1 Oct: the old
                // long bright burst sounded like a bus's air brakes).
                if(spec.blowoff||spec.electricTurbo)bov.Trigger(fs,.12f*boostS.y,.18f);else flutter.Trigger(fs,.05f*boostS.y,.3f);
                bovCooldown=.6f;
            }
            lastThrottle=throttleTarget;
            if(bov.Active)bovFilter.SetBandPass(fs,1100+2200*Math.Min(1,bov.level/.12f),1.1f);
            if(spec.turbo)whoosh.SetBandPass(fs,700+1500*shaft,.8f);
        }
        void Shift(int direction)
        {
            switch(spec.shiftType) {
                // (Understated: a soft "tock", Richard 1 Oct.)
                case "H": thump.Trigger(fs,.22f,.06f);click.Trigger(fs,.14f,.012f);if(direction<0)blip=.35f;break;
                case "S": click.Trigger(fs,.28f,.03f);thump.Trigger(fs,.15f,.04f);if(direction>0&&throttleS.y>.7f)bang.Trigger(fs,1,.02f);else if(direction<0)blip=.6f;break;
                case "P": click.Trigger(fs,.15f,.03f);if(direction<0)blip=.55f;else if(throttleS.y>.8f)bang.Trigger(fs,.6f,.015f);break;
                default: click.Trigger(fs,.1f,.01f);break;
            }
            thumpPhase=0;
        }

        public float Next()
        {
            rpm+=rpmStep;float l=load.Process(loadTarget),thr=throttleS.Process(throttleTarget),bst=boostS.Process(boostTarget),spd=speedS.Process(speedTarget);
            float rpmFrac=rpm/spec.redline;
            // Crank: fire each cylinder as the phase passes its angle.
            float step=rpm*6f/fs;phase+=step; // degrees per sample = rpm/60*360/fs
            while(true) {
                if(next>=spec.cylinders){if(phase<720)break;phase-=720;next=0;cycle++;continue;}
                if(phase<spec.angle[next])break;
                // Place the pulse between samples where the crank actually passed the angle (whole-sample
                // placement alternates 133/134-sample gaps and puts a false half-order note on the engine).
                Fire(next,l,thr,rpmFrac,Math.Clamp((phase-spec.angle[next])/Math.Max(1e-4f,step),0,1));next++;
            }
            float exhaust=0;
            // A recorded engine (grain bank) replaces the modelled exhaust; the other layers stay.
            if(Grains!=null) {
                float g=grainTone.Process(Grains.Next(rpm,l,inLimiter,inShifting));if(spec.turbo)g=turboMuffle.Process(g)*1.1f;
                exhaust=g*GrainGain;
                if(grainDrive>1)exhaust=SoftClip.Tanh(exhaust*grainDrive)/grainDrive;}
            else {
                for(int b=0;b<spec.banks;b++) {
                    s1[b]=s1[b]*pulseCoef+imp[b];s2[b]=s2[b]*pulseCoef+s1[b]*(1-pulseCoef);imp[b]=impNext[b];impNext[b]=0;
                    float pulse=s2[b]*2.72f*(1+rough[b]*noise.White());
                    float y=pulse+reflect*pipe[b].Read(pipeDelay[b]);pipe[b].Write(y);
                    float m=body[b].Process(muffler2[b].Process(muffler[b].Process(y)));
                    exhaust+=m*bankGain[b];
                }
                if(rasp>1)exhaust=SoftClip.Tanh(exhaust*rasp)/rasp*1.1f;
                exhaust*=exhaustGain;
            }
            // Flow roar: turbulence through the system, with load and revs.
            // (Not over a recording, docs/55: the recording carries its own flow noise, and the modelled roar on top of it
            // buried the engine's orders: the sprint car's character scored 0.8 of 10 with it, 6.9 without.)
            float roar=Grains!=null&&!GrainSynth.LegacyLift?0:roarFilter.Process(noise.Pink())*roarGain*(.2f+.8f*l)*rpmFrac;
            // Intake: the firing pulses through the intake's resonance plus induction hiss, with throttle.
            float intake=intakeTone.Process(intakeImp)*intakePulseGain*(.3f+.7f*thr);intakeImp=0;
            intake+=intakeHiss.Process(noise.White())*intakeHissGain*thr*rpmFrac*rpmFrac;
            // Valve train.
            float mech=tickFilter.Process(noise.White())*tick.Next();
            // Straight-cut gears whine on sequential, paddle and seamless boxes.
            if(spec.shiftType!="H"){gearPhase+=rpm/60f*(26+inGear*3)/fs;mech+=Sine.At(gearPhase)*.012f*(.3f+.7f*l)*rpmFrac;}
            // Turbo: shaft speed follows boost; whistle, whoosh, blow-off or flutter, anti-lag.
            float turbo=0;
            if(spec.turbo&&TurboSound) {
                float before=shaft;shaft+=(bst-shaft)*(spec.electricTurbo?.002f:.0006f);
                // Spooling: how fast the shaft is gaining speed (the turbo is heard most while boost builds).
                spool+=(Math.Clamp((shaft-before)*fs*2.5f,0,1)-spool)*.0015f;
                // Heard from outside a car a turbo is mostly air: a spool whoosh rising with boost. The whistle is
                // faint and stays under 3.5 kHz (Richard, 30 Sep: a loud 1.6-8.8 kHz tone was piercing).
                // Heard from outside, a turbo is air (Richard, 1 Oct: the steady whistle was too high and constant).
                // The whoosh swells while boost builds and settles once it holds; the whine is faint and only there
                // while spooling; a little steady air remains under load.
                float f=1100+1600*shaft;whistlePhase+=f/fs;whistle2Phase+=f*2.01f/fs;
                turbo=(Sine.At(whistlePhase)+.15f*Sine.At(whistle2Phase))*.0025f*spool;
                turbo+=whoosh.Process(noise.Pink())*(.07f*spool+.012f*shaft*thr);
                if(spec.electricTurbo)turbo+=Sine.At(whistlePhase*1.5f)*.0015f*spool;
                if(bov.Active)turbo+=bovFilter.Process(noise.White())*bov.Next();
                if(flutter.Active)turbo+=flutterFilter.Process(noise.Pink())*flutter.Next();
            }
            // Hybrid motor-generator: deploys with throttle, harvests under braking; pitch with road speed.
            float motor=0;
            if(spec.hybrid&&HybridSound){motorPhase+=(60+52*spd)/fs;float drive=Math.Max(thr,inThrottle<0?-inThrottle*.8f:0);motor=(Sine.At(motorPhase)+.25f*Sine.At(motorPhase*3))*(.006f+.03f*drive)*Math.Min(1,spd/8);}
            // Shift clunk, click and ignition-cut bang.
            float shift=0;
            if(thump.Active){thumpPhase+=62f/fs;shift+=Sine.At(thumpPhase)*thump.Next();}
            if(click.Active)shift+=clickFilter.Process(noise.White())*click.Next();
            if(bang.Active){float k=bang.Next();for(int b=0;b<spec.banks;b++)imp[b]+=k*.08f;}
            // Diagnostics (the harness): Solo picks one layer (0 exhaust, 1 roar, 2 intake, 3 mechanical, 4 turbo,
            // 5 hybrid, 6 shifts); -1 plays them all.
            // (Over a recording the gearchange is the recording's own dip: the modelled thump put a 62 Hz burst into every
            // shift that no real recording has, and filled the dip; docs/55: shift scores up on 9 of 11 engines without it.)
            if(Grains!=null&&!GrainSynth.LegacyLift)shift=0;
            if(Mute>=0){if(Mute==1)roar=0;else if(Mute==2)intake=0;else if(Mute==3)mech=0;else if(Mute==6)shift=0;}
            float mix=Solo<0?exhaust+roar+intake+mech+turbo+motor+shift:Solo==0?exhaust:Solo==1?roar:Solo==2?intake:Solo==3?mech:Solo==4?turbo:Solo==5?motor:shift;
            mix=dcBlock.Process(mix)*gain;
            return SoftClip.Tanh(mix);
        }

        void Fire(int i,float l,float thr,float rpmFrac,float late)
        {
            int b=spec.bank[i];
            // Limiter: cut two firings in three, so the revs bounce and the note stutters.
            if(inLimiter)return;
            float a=pulseScale*(.14f+.86f*l);
            // Idle and light load: race cams are lumpy.
            float lumpy=jitter*(1.5f-Math.Min(1,rpmFrac*1.5f));
            a*=1+(jitter+lumpy)*noise.White();
            float r=.25f+.25f*l;
            // Overrun: weak pulses, and pops from sport and race exhausts.
            if(thr<.05f&&rpmFrac>.4f) {
                a*=.45f;
                if(popChance>0&&noise.Unit()<popChance*rpmFrac){a=pulseScale*(1.4f+noise.Unit());r=.9f;}
                // Anti-lag keeps firing into the turbo: bangs.
                if(spec.antilag&&TurboSound&&noise.Unit()<.12f){a=pulseScale*(1.8f+noise.Unit());r=1;}
            }
            // `late`: how far past the angle this sample is; the pulse lands one sample on, less that.
            imp[b]+=a*late;impNext[b]+=a*(1-late);rough[b]=r;
            intakeImp+=a*.6f;
            tick.Trigger(fs,.018f*rpmFrac,.0004f);
        }

        // Normalise loudness: render a moment at 70% revs, full load, and scale to this exhaust's level.
        void Calibrate()
        {
            // Full load across the rev range (a recording's level varies with revs; one point misjudged it).
            float saved=gain;gain=1;double sum=0;int count=0;
            foreach(float at in new[]{.35f,.55f,.75f,.9f}) {
                Set(spec.redline*at,1,1,20,false,false,2,0,0);rpm=spec.redline*at;int n=(int)(fs*.15f);
                Block(n);for(int i=0;i<n;i++){float x=Next();if(i>n/3){sum+=x*x;count++;}}
            }
            float rms=(float)Math.Sqrt(sum/Math.Max(1,count));
            // Louder than at first (Richard, 1 Oct: the engines were mostly quiet), with headroom under the soft clip.
            float level=spec.exhaust=="race"?.3f:spec.exhaust=="sport"?.26f:.22f;
            gain=rms>1e-4f?level/rms:saved;
            // Settle back to idle.
            Set(spec.idle,0,0,0,false,false,0,0,0);rpm=spec.idle;shaft=0;
            for(int b=0;b<spec.banks;b++){pipe[b].Clear();s1[b]=s2[b]=0;}
        }

        // Offline rendering for the checks and the listening sweep.
        public float Render(float[] output,int start,int count)
        {
            Block(count);double sum=0;for(int i=0;i<count;i++){float x=Next();output[start+i]=x;sum+=x*x;}
            LastRms=(float)Math.Sqrt(sum/Math.Max(1,count));return LastRms;
        }
    }
}
