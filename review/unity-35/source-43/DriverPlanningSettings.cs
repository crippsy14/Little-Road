using System;

namespace LittleRoad
{
    // Driver judgement, not vehicle capability. Immutable for this first
    // deterministic slice; default(struct) is the exact existing controller.
    public readonly struct DriverPlanningSettings
    {
        readonly float braking;
        public float BrakeFraction=>braking==0?.75f:braking;
        public float ReferenceBrakeScale=>BrakeFraction/.75f;
        public DriverPlanningSettings(float brakeFraction)
        {
            if(!float.IsFinite(brakeFraction)||brakeFraction<.55f||brakeFraction>.75f)throw new ArgumentOutOfRangeException(nameof(brakeFraction));
            braking=brakeFraction;
        }
        public bool Same(in DriverPlanningSettings other)=>BrakeFraction==other.BrakeFraction;
        public static DriverPlanningSettings Preset(string name)=>name switch {
            "neutral"=>default,"earlier"=>new DriverPlanningSettings(.65f),"early"=>new DriverPlanningSettings(.55f),
            _=>throw new ArgumentException("Unknown driver braking preset: "+name)
        };
    }
}
