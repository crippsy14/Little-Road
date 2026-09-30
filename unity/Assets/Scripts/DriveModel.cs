using System;
using UnityEngine;

namespace LittleRoad
{
    [Serializable]
    public struct CarTuning
    {
        public float power, acceleration, mass, gearCeiling, braking, roadGrip, dirtGrip, response, yaw;
        // Real engine and gears (docs/47); null with --legacy-drivetrain.
        [NonSerialized] public Drivetrain drivetrain;
        public static CarTuning Stock => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("stock"));
        public static CarTuning Club => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("club"));
        public static CarTuning Street => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("street"));
        public static CarTuning Create(float engineLimit, float torque, float gearing, float pull,
            float turboPower, float turboTorque, float grip, float dirt, float brake, float rack, float yawRate, float weight)
        {
            return new CarTuning {power=(.25f+DriveModel.DragK*engineLimit*engineLimit)*engineLimit*turboPower,
                acceleration=torque*pull*turboTorque/weight, mass=weight, gearCeiling=gearing,
                braking=brake/weight, roadGrip=grip, dirtGrip=dirt, response=rack, yaw=yawRate};
        }
        public float AttainableSpeed
        {
            get {float low=0, high=gearCeiling; for(int i=0;i<24;i++) {float mid=(low+high)*.5f;
                if((.25f+DriveModel.DragK*mid*mid)*mid<power) low=mid; else high=mid;} return (low+high)*.5f;}
        }
    }

    public static class DriveModel
    {
        // Air drag per (m/s)^2 (Richard, 1 Oct: gears made little difference). The old arcade 0.006 made engines need so
        // much power to reach their top speeds that a launch cap had to flatten the low gears; at 0.0035 the same top
        // speeds need 60% of the power, the cap binds only in the low gears, and the gear chosen matters (5th pulls
        // about half what 3rd does at 30 mph).
        // --legacy-drag restores 0.006 for A/B.
        public static readonly float DragK=RaceTrial.Has("--legacy-drag")?.006f:.0035f;
        public static float Resistance(float speed, float mass) => (.25f+DragK*speed*speed)/mass;
        // With real gears this is the best-gear envelope: what the AI and speed profiles plan with.
        public static float EngineForce(CarTuning t, float speed) => t.drivetrain!=null?t.drivetrain.Envelope(speed):Mathf.Min(t.acceleration,t.power/(Mathf.Max(Mathf.Abs(speed),1)*t.mass));
        public static float PedalForSpeed(CarTuning t, float speed, float target)
        {
            if(speed>target+.8f) return -1;
            if(target<.1f) return speed>.1f?-1:0;
            return Mathf.Clamp01((Resistance(speed,t.mass)+(target-speed)*.8f)/Mathf.Max(EngineForce(t,speed),.01f));
        }
        public static readonly bool LegacyReverse=RaceTrial.Has("--legacy-reverse");
        public static void Forces(CarTuning t, float speed, float throttle, float yawRequest, float grip, float slope,
            out float acceleration, out float yaw,float dragReduction=0,float engineAccel=-1)
        {
            float budget=Mathf.Max(.1f,grip*1.65f);
            yaw=Mathf.Clamp(yawRequest,-budget*.95f/Mathf.Max(Mathf.Abs(speed),1),budget*.95f/Mathf.Max(Mathf.Abs(speed),1));
            float lateral=Mathf.Abs(yaw*speed), traction=Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral));
            float drag=DraftingModel.Resistance(speed,t.mass,dragReduction);
            // Rolling backwards on the forward pedal (a reversing car stopping, a recovery pulling away): it brakes
            // to a stop first. With real gears the engine gives nothing in reverse, so without this the car rolled
            // on backwards (recoveries reversed for 15-30 s). --legacy-reverse restores that for A/B.
            if(throttle>0&&speed< -.1f&&!LegacyReverse){acceleration=Mathf.Min(t.braking*throttle,traction)+drag-slope;return;}
            if(throttle>0) {
                // A downhill overspeed must coast back toward the usable gear
                // range. Merely clamping net acceleration to zero supplied
                // exactly enough engine force to hold that overspeed forever.
                // engineAccel: the live gearbox's delivery (RoadCar); otherwise the envelope.
                float drive=speed>=t.gearCeiling?0:Mathf.Min(engineAccel>=0?engineAccel:EngineForce(t,speed)*throttle,traction);
                acceleration=drive-drag-slope;
            }
            else if(throttle<0) acceleration=speed>.1f?-Mathf.Min(t.braking*-throttle,traction)-drag-slope:-Mathf.Min(LegacyReverse?7:7*-throttle,traction);
            else acceleration=-Mathf.Sign(speed)*drag-slope;
        }
    }
}
