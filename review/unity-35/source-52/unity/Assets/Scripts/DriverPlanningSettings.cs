using System;

namespace LittleRoad
{
    // Driver judgement, not vehicle capability. Immutable for this first
    // deterministic slice; default(struct) is the exact existing controller.
    public readonly struct DriverPlanningSettings
    {
        readonly float braking;
        public readonly int Risk;
        public float BrakeFraction=>braking==0?.75f:braking;
        public float ReferenceBrakeScale=>BrakeFraction/.75f;
        public DriverPlanningSettings(float brakeFraction,int risk=0)
        {
            if(!float.IsFinite(brakeFraction)||brakeFraction<.55f||brakeFraction>.75f)throw new ArgumentOutOfRangeException(nameof(brakeFraction));
            if(risk<0||risk>10)throw new ArgumentOutOfRangeException(nameof(risk));
            braking=brakeFraction;Risk=risk;
        }
        public DriverPlanningSettings WithRisk(int risk)=>new DriverPlanningSettings(BrakeFraction,risk);
        public bool Same(in DriverPlanningSettings other)=>BrakeFraction==other.BrakeFraction&&Risk==other.Risk;
        public static DriverPlanningSettings Preset(string name)=>name switch {
            "neutral"=>default,"earlier"=>new DriverPlanningSettings(.65f),"early"=>new DriverPlanningSettings(.55f),
            _=>throw new ArgumentException("Unknown driver braking preset: "+name)
        };
    }
}
