using System;
using UnityEngine;

namespace LittleRoad
{
    [Serializable]
    public struct CarTuning
    {
        public float power, acceleration, mass, gearCeiling, braking, roadGrip, dirtGrip, response, yaw;
        public static CarTuning Stock => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("stock"));
        public static CarTuning Club => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("club"));
        public static CarTuning Street => EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("street"));
        public static CarTuning Create(float engineLimit, float torque, float gearing, float pull,
            float turboPower, float turboTorque, float grip, float dirt, float brake, float rack, float yawRate, float weight)
        {
            return new CarTuning {power=(.25f+.006f*engineLimit*engineLimit)*engineLimit*turboPower,
                acceleration=torque*pull*turboTorque/weight, mass=weight, gearCeiling=gearing,
                braking=brake/weight, roadGrip=grip, dirtGrip=dirt, response=rack, yaw=yawRate};
        }
        public float AttainableSpeed
        {
            get {float low=0, high=gearCeiling; for(int i=0;i<24;i++) {float mid=(low+high)*.5f;
                if((.25f+.006f*mid*mid)*mid<power) low=mid; else high=mid;} return (low+high)*.5f;}
        }
    }

    public static class DriveModel
    {
        public static float Resistance(float speed, float mass) => (.25f+.006f*speed*speed)/mass;
        public static float EngineForce(CarTuning t, float speed) => Mathf.Min(t.acceleration,t.power/(Mathf.Max(Mathf.Abs(speed),1)*t.mass));
        public static float PedalForSpeed(CarTuning t, float speed, float target)
        {
            if(speed>target+.8f) return -1;
            if(target<.1f) return speed>.1f?-1:0;
            return Mathf.Clamp01((Resistance(speed,t.mass)+(target-speed)*.8f)/Mathf.Max(EngineForce(t,speed),.01f));
        }
        public static void Forces(CarTuning t, float speed, float throttle, float yawRequest, float grip, float slope,
            out float acceleration, out float yaw)
        {
            float budget=Mathf.Max(.1f,grip*1.65f);
            yaw=Mathf.Clamp(yawRequest,-budget*.95f/Mathf.Max(Mathf.Abs(speed),1),budget*.95f/Mathf.Max(Mathf.Abs(speed),1));
            float lateral=Mathf.Abs(yaw*speed), traction=Mathf.Sqrt(Mathf.Max(0,budget*budget-lateral*lateral));
            float drag=Resistance(speed,t.mass);
            if(throttle>0) {
                // A downhill overspeed must coast back toward the usable gear
                // range. Merely clamping net acceleration to zero supplied
                // exactly enough engine force to hold that overspeed forever.
                float drive=speed>=t.gearCeiling?0:Mathf.Min(EngineForce(t,speed)*throttle,traction);
                acceleration=drive-drag-slope;
            }
            else if(throttle<0) acceleration=speed>.1f?-Mathf.Min(t.braking*-throttle,traction)-drag-slope:-Mathf.Min(7,traction);
            else acceleration=-Mathf.Sign(speed)*drag-slope;
        }
    }
}
