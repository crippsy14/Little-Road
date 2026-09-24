using System;

namespace LittleRoad
{
    // Driver judgement, not vehicle capability. Immutable for this first
    // deterministic slice; default(struct) is the exact existing controller.
    public readonly struct DriverPlanningSettings
    {
        readonly float braking;
        public readonly int Risk;
        public readonly bool PassingStyle;
        // Risk5 is bit-identical to the retained .35m dynamic passing margin.
        public float PassingClearance=>!PassingStyle||Risk==5?.35f:Risk<5?.8f-.09f*Risk:.35f-.05f*(Risk-5);
        public float BrakeFraction=>braking==0?.75f:braking;
        public float ReferenceBrakeScale=>BrakeFraction/.75f;
        public DriverPlanningSettings(float brakeFraction,int risk=0,bool passingStyle=false)
        {
            if(!float.IsFinite(brakeFraction)||brakeFraction<.55f||brakeFraction>.75f)throw new ArgumentOutOfRangeException(nameof(brakeFraction));
            if(risk<0||risk>10)throw new ArgumentOutOfRangeException(nameof(risk));
            braking=brakeFraction;Risk=risk;PassingStyle=passingStyle;
        }
        public DriverPlanningSettings WithRisk(int risk)=>new DriverPlanningSettings(BrakeFraction,risk,PassingStyle);
        public bool Same(in DriverPlanningSettings other)=>BrakeFraction==other.BrakeFraction&&Risk==other.Risk&&PassingStyle==other.PassingStyle;
        public static DriverPlanningSettings Preset(string name)=>name switch {
            "neutral"=>default,"earlier"=>new DriverPlanningSettings(.65f),"early"=>new DriverPlanningSettings(.55f),
            _=>throw new ArgumentException("Unknown driver braking preset: "+name)
        };
    }
}
