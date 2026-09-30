using System;
using UnityEngine;

namespace LittleRoad
{
    // Real engine and gears for a fitted car (docs/47). Immutable per build.
    //   Torque curve: normalised tau(rpm) through idle, peak torque, peak power and the redline; a turbo multiplies
    //   it by 1 + (power x - 1) x boost(rpm), boost rising from spoolStart to spoolFull (fractions of the redline).
    //   Scale: at peak power (full boost) the car makes exactly its tuning's power, so top speeds and the game's
    //   pace carry over. The launch limit caps net acceleration (drive less drag) at the tuning's acceleration in
    //   every gear, so first gear always leads off the line and the limit fades as drag grows.
    //   Gears: top gear turns topRpm x peak-power revs at the drag-limited top speed; first is spread x top;
    //   the middle is progressive (closer at the top). No gearbox "ceiling": every build reaches its top speed
    //   in top gear.
    // The best-gear envelope (Envelope) is what the AI and speed profiles plan with (DriveModel.EngineForce).
    public sealed class Drivetrain
    {
        public const float WheelRadius=.31f;
        public readonly float idle,torqueRpm,powerRpm,redline;
        public readonly float[] ratio; // engine rpm per (m/s), per gear (index 0 = first)
        public readonly float shiftTime,topSpeed,launchCap,naPowerPerMass,turboPower,spoolStart,spoolFull,lag;
        public readonly bool turbo,antilag,electric,lsd;
        public readonly string shiftType;
        // The parts it was built from (the sound reads their fingerprints).
        public readonly EquipmentCatalog.Part engine,induction,box;
        public int Gears=>ratio.Length;
        readonly float tauAtPower;
        readonly float[] envelope;const float EnvelopeStep=.25f;
        readonly int[] bestGear;

        public readonly float FinalDrive;
        public Drivetrain(EquipmentCatalog.Part engine,EquipmentCatalog.Part induction,EquipmentCatalog.Part box,CarTuning tuning,float finalDrive=1,float[] gearTune=null)
        {
            this.engine=engine;this.induction=induction;this.box=box;
            idle=engine.idleRpm>0?engine.idleRpm:900;redline=engine.redline>0?engine.redline:6500;
            powerRpm=engine.powerRpm>0?engine.powerRpm:redline*.88f;torqueRpm=engine.torqueRpm>0?engine.torqueRpm:powerRpm*.65f;
            turboPower=Mathf.Max(1,induction.power);turbo=turboPower>1.001f;
            spoolStart=induction.spoolStart;spoolFull=Mathf.Max(induction.spoolFull,induction.spoolStart+.05f);lag=induction.lag;
            antilag=induction.antilag;electric=induction.electric;lsd=box.lsd;
            shiftTime=box.shiftTime>0?box.shiftTime:.4f;shiftType=string.IsNullOrEmpty(box.shiftType)?"H":box.shiftType;
            massFactor=Mathf.Max(.01f,tuning.mass);naPowerPerMass=tuning.power/turboPower/massFactor;
            launchCap=tuning.acceleration*(lsd?1.05f:1);
            tauAtPower=Tau(powerRpm);
            // Drag-limited top speed with the full power (the tuning's AttainableSpeed without a gearbox cap).
            float low=0,high=120;for(int i=0;i<32;i++){float mid=(low+high)*.5f;if((.25f+DriveModel.DragK*mid*mid)*mid<tuning.power)low=mid;else high=mid;}
            topSpeed=(low+high)*.5f;
            int gears=Mathf.Clamp(box.gears>0?box.gears:4,3,8);float spread=box.spread>0?box.spread:3.6f;
            // Top gear: the box's km/h per 1000 rpm (fixed ratios, docs/47 gearing v2), times the final-drive tune
            // (above 1 shortens every gear: more pull, less top speed). rpm per m/s = 3600 / (km/h per 1000 rpm).
            FinalDrive=finalDrive;float top=3600f/(box.kmhPer1000>0?box.kmhPer1000:18)*finalDrive;
            ratio=new float[gears];
            // Progressive spacing: the log-ratio steps shrink towards top gear (k<1 bunches the upper gears).
            const float k=.82f;float total=0;var steps=new float[gears-1];
            for(int i=0;i<gears-1;i++){steps[i]=Mathf.Pow(k,i);total+=steps[i];}
            float logSpread=Mathf.Log(spread),acc=0;ratio[0]=top*spread;
            for(int i=0;i<gears-1;i++){acc+=steps[i]/total;ratio[i+1]=top*Mathf.Exp(logSpread*(1-acc));}
            ratio[gears-1]=top;
            // A tune's per-gear multipliers (above 1 shortens that gear), kept in order: each gear taller than the last.
            if(gearTune!=null)for(int i=0;i<gears&&i<gearTune.Length;i++)ratio[i]*=Mathf.Clamp(gearTune[i],GearTune.GearMin,GearTune.GearMax);
            for(int i=1;i<gears;i++)ratio[i]=Mathf.Min(ratio[i],ratio[i-1]*.97f);
            int bins=Mathf.CeilToInt(topSpeed*1.15f/EnvelopeStep)+2;envelope=new float[bins];bestGear=new int[bins];
            for(int b=0;b<bins;b++){float v=b*EnvelopeStep;int best=0;float a=0;for(int g=0;g<gears;g++){float ag=GearAccel(g,v,1);if(ag>a){a=ag;best=g;}}envelope[b]=a;bestGear[b]=best;}
        }
        // Normalised torque: rises from idle to its peak, eases to 0.88 at peak power, falls to 0.72 at the redline.
        public float Tau(float rpm)
        {
            if(rpm<=torqueRpm){float t=Mathf.InverseLerp(idle*.6f,torqueRpm,rpm);return Mathf.Lerp(.5f,1,t*(2-t));}
            if(rpm<=powerRpm)return Mathf.Lerp(1,.88f,(rpm-torqueRpm)/Mathf.Max(1,powerRpm-torqueRpm));
            return Mathf.Lerp(.88f,.72f,Mathf.Clamp01((rpm-powerRpm)/Mathf.Max(1,redline-powerRpm)));
        }
        // Steady boost at these revs (0-1); the car's live boost chases it with the turbo's lag.
        public float BoostTarget(float rpm)=>!turbo?0:Mathf.SmoothStep(0,1,Mathf.InverseLerp(spoolStart*redline,spoolFull*redline,rpm));
        public float Rpm(int gear,float speed)=>Mathf.Abs(speed)*ratio[Mathf.Clamp(gear,0,Gears-1)];
        // Wheel acceleration available in `gear` at `speed` with this much boost (m/s^2 per unit mass, like EngineForce).
        // Torque at these revs times the gear: power/speed without dividing by a near-zero speed. The launch limit
        // (clutch, traction and a gentle toy start) caps net acceleration in every gear; tyre grip limits it too
        // (DriveModel). Capping drive itself would stop fast classes reaching top speed (drag outgrows it).
        public float GearAccel(int gear,float speed,float boost)
        {
            int g=Mathf.Clamp(gear,0,Gears-1);float rpm=Mathf.Max(Rpm(g,speed),idle);
            if(rpm>=redline)return 0;
            float tau=Tau(rpm)*(1+(turboPower-1)*boost);
            float accel=naPowerPerMass/(tauAtPower*powerRpm)*tau*ratio[g];
            return Mathf.Min(launchCap+DriveModel.Resistance(speed,massFactor),accel);
        }
        public float SteadyAccel(int gear,float speed)=>GearAccel(gear,speed,BoostTarget(Rpm(gear,speed)));
        // Best-gear envelope with steady boost: what the AI and speed profiles plan with.
        public float Envelope(float speed)
        {
            float f=Mathf.Abs(speed)/EnvelopeStep;int i=Mathf.Min(envelope.Length-2,(int)f);return Mathf.Lerp(envelope[i],envelope[i+1],Mathf.Clamp01(f-i));
        }
        public int BestGear(float speed){int i=Mathf.Clamp(Mathf.RoundToInt(Mathf.Abs(speed)/EnvelopeStep),0,bestGear.Length-1);return bestGear[i];}
        // The redline in top gear: the old "gear ceiling" for code that still asks.
        public float RedlineSpeed=>redline/ratio[Gears-1];
        // The top speed a gear can hold: where its drive meets drag, or its redline.
        public float GearTop(int gear)
        {
            float low=0,high=redline/ratio[gear]*.999f;
            if(SteadyAccel(gear,high)>DriveModel.Resistance(high,massFactor))return high;   // on the limiter
            for(int i=0;i<30;i++){float mid=(low+high)*.5f;if(SteadyAccel(gear,mid)>DriveModel.Resistance(mid,massFactor))low=mid;else high=mid;}
            return (low+high)*.5f;
        }
        // The car's top speed: the best gear's (a long box on a weak engine tops out a gear down).
        public float ReachableTop {get {float best=0;for(int g=0;g<Gears;g++)best=Mathf.Max(best,GearTop(g));return best;}}
        public int TopSpeedGear {get {int best=0;float v=0;for(int g=0;g<Gears;g++){float t=GearTopCached(g);if(t>v){v=t;best=g;}}return best;}}
        float[] gearTops;
        public float GearTopCached(int gear){if(gearTops==null){var tops=new float[Gears];for(int g=0;g<Gears;g++)tops[g]=GearTop(g);gearTops=tops;}return gearTops[Mathf.Clamp(gear,0,Gears-1)];}
        // True when top gear runs into its limiter before drag stops it (time for a longer box or final drive).
        public bool TopGearLimited=>SteadyAccel(Gears-1,RedlineSpeed*.999f)>DriveModel.Resistance(RedlineSpeed*.999f,massFactor);
        readonly float massFactor;
    }

    // One car's live gearbox: current gear, shifts, revs and boost. Automatic (AI and the default) or manual.
    public sealed class GearState
    {
        public readonly Drivetrain train;
        public int Gear {get;private set;} // 0 = first; -1 = reverse
        public float Rpm {get;private set;}
        public float Boost {get;private set;}
        public bool Shifting=>shiftLeft>0;
        public bool Limiter {get;private set;}
        // Shift events for the sound: +1 upshift, -1 downshift, set for one step.
        public int ShiftEvent {get;private set;}
        // Running counts for listeners that sample less often than the physics (the sound).
        public int Upshifts {get;private set;}
        public int Downshifts {get;private set;}
        public bool Manual;
        float shiftLeft,hold;int target;
        public GearState(Drivetrain drivetrain){train=drivetrain;Rpm=drivetrain.idle;}
        public void RequestShift(int direction)
        {
            if(Shifting)return;
            // Manual: reverse is a gear, as in a real car and GT7. Down from first at a standstill selects it;
            // up from reverse (stopped or nearly) returns to first. The brake never selects it.
            if(Manual&&direction<0&&Gear==0){if(Mathf.Abs(lastSpeed)<1)Begin(-1);return;}
            if(Manual&&direction>0&&Gear<0){if(lastSpeed>-1)Begin(0);return;}
            int next=Mathf.Clamp(Gear+direction,0,train.Gears-1);if(next==Gear||Gear<0)return;
            // No money shifts: a downshift that would over-rev is refused.
            if(direction<0&&train.Rpm(next,lastSpeed)>train.redline*1.02f)return;
            Begin(next);
        }
        void Begin(int next){ShiftEvent=next>Gear?1:-1;if(ShiftEvent>0){Upshifts++;sinceUpshift=0;kickdown=false;}else Downshifts++;target=next;shiftLeft=train.shiftTime;}
        public int TargetGear=>Shifting?target:Gear;
        float lastSpeed;
        // Advance one physics step; returns the wheel acceleration the engine delivers at this throttle.
        public float Step(float speed,float throttle,float dt)
        {
            ShiftEvent=0;lastSpeed=speed;
            // Automatic: holding the brake at a stop reverses; forward again once stopped.
            if(!Manual){if(speed< -.3f&&throttle<0){Gear=-1;}else if(Gear<0&&speed>-.1f&&!Shifting)Gear=0;}
            if(shiftLeft>0){shiftLeft-=dt;if(shiftLeft<=0){Gear=target;shiftLeft=0;}}
            // Nearly stopped (a recovery, a crash): straight into first rather than stepping down gear by gear.
            else if(!Manual&&Gear>0&&Mathf.Abs(speed)<1.5f)Gear=0;
            else if(!Manual&&Gear>=0)Automatic(speed,throttle,dt);
            int g=Mathf.Max(0,Gear);
            // Revs: road speed through the gear, the clutch slipping up to launch revs at walking pace.
            // Launch: a slipping clutch lets the revs flare, then they climb with the car until the clutch is fully
            // in (Richard, 1 Oct: flat launch revs up to 10 mph sounded like standing still).
            // (The slip closes by about 1.4x the flare revs, well under any redline: a slip that outlasted the gear
            // held the revs on the limiter in first; DrivetrainChecks.)
            float road=train.Rpm(g,speed),flare=train.idle+Mathf.Clamp01(throttle)*(train.torqueRpm-train.idle)*.6f;
            float want=g==0?Mathf.Max(road,road+Mathf.Max(0,flare-road*.7f)):Mathf.Max(road,train.idle);
            if(Shifting)want=Mathf.Lerp(Rpm,train.Rpm(target,speed),.25f);
            Rpm=Mathf.MoveTowards(Rpm,Mathf.Min(want,train.redline*1.01f),train.redline*dt*6);
            // Rev limiter: at the redline the ignition cuts for 75 ms (no drive, the revs dip ~3.5%), then fires
            // again: it bounces about a dozen times a second, "brap-brap", as a real limiter does.
            if(limiterCut>0)limiterCut-=dt;
            else if(Rpm>=train.redline*.995f&&throttle>.1f&&!Shifting)limiterCut=LimiterCut;
            Limiter=limiterCut>0;
            if(Limiter)Rpm=Mathf.Min(Rpm,train.redline*(1-.035f*(1-limiterCut/LimiterCut)));
            // Boost chases its steady value with the turbo's lag; anti-lag keeps it up off throttle.
            float boostTarget=throttle>.2f||train.antilag?train.BoostTarget(Rpm):0;
            Boost=train.lag<=0?boostTarget:Mathf.MoveTowards(Boost,boostTarget,dt/train.lag*(boostTarget>Boost?1:2.5f));
            if(Shifting||throttle<=0||Gear<0)return 0;
            float accel=Limiter?0:train.GearAccel(g,speed,Boost);
            return accel*throttle;
        }
        // Up when the next gear pulls harder (or at the redline); down when a lower gear pulls clearly harder,
        // and through the gears under braking so the revs stay in the band (and the sound blips).
        float sinceUpshift=99,lastThrottle;bool kickdown;
        void Automatic(float speed,float throttle,float dt)
        {
            sinceUpshift+=dt;kickdown=throttle>.9f&&lastThrottle<.6f||kickdown&&throttle>.9f&&sinceUpshift<3;lastThrottle=throttle;
            int best=train.BestGear(speed);float rpm=train.Rpm(Gear,speed);
            if(throttle>.1f) {
                // (At the redline, only up into a gear that can hold this speed: with a long box on a weak engine the
                // best gear is a lower one, and a good automatic holds it on the limiter.)
                if(Gear<train.Gears-1&&((best>Gear&&Mathf.Abs(speed)>2)||rpm>=train.redline*.985f&&train.GearTopCached(Gear+1)>=Mathf.Abs(speed)*.99f)){hold+=dt;if(hold>.05f){hold=0;Begin(Gear+1);}}
                // Down only into a gear with real rev room that pulls clearly harder, and not straight after an
                // upshift unless the throttle was just floored (Richard, 1 Oct: 4th-5th hunting near top speed
                // cut the drive every second and stuttered).
                // Kickdown: floored, straight down to the best gear with rev room, skipping gears as a real automatic does.
                else if(kickdown&&best<Gear-1){int to=best;while(to<Gear&&train.Rpm(to,speed)>train.redline*.85f)to++;if(to<Gear){hold+=dt;if(hold>.08f){hold=0;Begin(to);}}}
                else if(Gear>0&&best<Gear&&(sinceUpshift>1.2f||kickdown)&&train.Rpm(Gear-1,speed)<train.redline*.85f&&train.SteadyAccel(Gear-1,speed)>train.SteadyAccel(Gear,speed)*1.15f){hold+=dt;if(hold>.15f){hold=0;Begin(Gear-1);}}
                else hold=0;
            } else {
                // Braking or coasting: step down through the gears (each one blipped) so the revs stay in a band ready to
                // pull: at least 85% of the torque peak under braking, 60% coasting; never into a gear over 88% of the
                // redline (Richard, 1 Oct: braking from 80 to 30 mph it stayed in 5th).
                float keep=train.torqueRpm*(throttle< -.1f?.85f:.6f);
                if(Gear>0&&rpm<keep&&train.Rpm(Gear-1,speed)<train.redline*.88f){hold+=dt;if(hold>.1f){hold=0;Begin(Gear-1);}}
                else hold=0;
            }
        }
        public void Reset(){Gear=0;shiftLeft=0;Boost=0;Rpm=train.idle;limiterCut=0;}
        const float LimiterCut=.075f;float limiterCut;
    }
}
