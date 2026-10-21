using UnityEngine;

namespace LittleRoad
{
    // Hybrids (Richard's idea, 20 Oct; docs/83 "Hybrids", docs/84): a hybrid engine carries an e-motor and a battery.
    // At full throttle below corner-exit speed the motor adds drive from the battery: off the line and out of slow corners.
    // Braking harvests energy back. An empty battery is dead weight: the pack's mass stays in the car.
    // The extra drive goes in before the grip budget (DriveModel.Forces), so the tyres clip it like any other drive.
    // Prototype behind --hybrid (off by default): the City hybrid engine at Jed's in Bramblewick.
    // --hybrid-everyone gives every car in a race the mild system on its own engine, for same-field keep/reject races.
    public static class HybridPower
    {
        public static bool Enabled=>enabled??=RaceTrial.Has("--hybrid")||Everyone;static bool? enabled;
        public static bool Everyone=>everyone??=RaceTrial.Has("--hybrid-everyone");static bool? everyone;
        // The mild system (the City hybrid's, and --hybrid-everyone's): an 18 kW motor, a 110 kJ pack, 15 kW of regen and
        // 6% more weight. About 6 s of full deploy from full; a lap's braking gives back roughly one and a half packs.
        public const float MildKw=18,MildKj=110,MildRegenKw=15,MildMass=.06f;
        // Deploy only below this speed (m/s, ~58 mph): launches and corner exits, not the straights.
        public const float DeploySpeed=26;
        // The motor's own torque limit, as an acceleration (m/s²): it can't push harder than this at low speed.
        public const float MotorAccelCap=2f;
        // Of the braking power, the share the motor-generator can take back (the friction brakes do the rest).
        public const float RegenShare=.6f;
        // Kilograms per unit of CarTuning.mass (RoadCar.Setup's body.mass=850*mass).
        public const float KgPerMass=850;

        public struct State {public float charge,deploySeconds,emptySeconds,regenKj,deployKj;}
        public static State Full=>new State{charge=1};

        // One physics step. Returns the motor's extra drive (m/s², before the grip budget) and updates the battery.
        // throttle is the signed pedal (negative brakes); speed along the car's heading (m/s).
        public static float Step(ref State s,in CarTuning t,float speed,float throttle,float dt)
        {
            if(t.batteryKj<=0)return 0;
            float kg=KgPerMass*Mathf.Max(.1f,t.mass),v=Mathf.Abs(speed);
            if(throttle>=.9f&&speed>-.5f&&speed<DeploySpeed) {
                if(s.charge<=0){s.emptySeconds+=dt;return 0;}
                float watts=t.hybridKw*1000;float accel=Mathf.Min(MotorAccelCap,watts/(kg*Mathf.Max(v,1)));
                float used=accel*kg*Mathf.Max(v,1)*dt/1000;   // (kJ: the motor's power at this speed, over the step)
                float left=s.charge*t.batteryKj;if(used>left){accel*=left/used;used=left;}
                s.charge=Mathf.Max(0,s.charge-used/t.batteryKj);s.deploySeconds+=dt;s.deployKj+=used;return accel;
            }
            if(throttle<-.05f&&v>3&&s.charge<1) {
                float braking=t.braking*-throttle*kg*v*RegenShare;float watts=Mathf.Min(t.regenKw*1000,braking);   // (the motor-generator's limit, not the pedal's depth: a light brake at speed sheds plenty; study 243)
                float got=watts*dt/1000;s.charge=Mathf.Min(1,s.charge+got/t.batteryKj);s.regenKj+=got;
            }
            return 0;
        }
    }
}
