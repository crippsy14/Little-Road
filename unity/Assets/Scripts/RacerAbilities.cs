using System;
using UnityEngine;

namespace LittleRoad
{
    // Racing AI v2 (docs/46): the six driver abilities and two personality traits, each 0..1. Every value
    // has a consumer in RacerDriverPolicy; Strategy is reserved for fuel, tyres and pits (class A) and is
    // not read yet. Until racers carry their own radar (docs/45 F1) the values derive from the existing
    // driver records, so both AIs race the same roster.
    [Serializable]
    public struct RacerAbilities
    {
        public float braking,line,control,consistency,racecraft,strategy,aggression,patience;

        public static RacerAbilities Default=>new RacerAbilities{braking=.8f,line=.8f,control=.8f,consistency=.8f,racecraft=.7f,strategy=.5f,aggression=.5f,patience=.5f};

        public static RacerAbilities From(DriverIdentity driver)
        {
            if(driver==null)return Default;
            var a=Default;
            // Race-day form is already folded into the effective brake fraction (0.55..0.75).
            a.braking=Mathf.Clamp01((driver.Planning.BrakeFraction-.55f)/.2f);
            a.consistency=Mathf.Clamp01(driver.Consistency);
            a.aggression=Mathf.Clamp01(driver.Planning.Risk/10f);
            (a.line,a.control,a.racecraft,a.patience)=driver.Style switch {
                "Benchmark"=>(.8f,.8f,.7f,.5f),
                "Measured"=>(.7f,.75f,.55f,.8f),
                "Balanced"=>(.65f,.65f,.6f,.5f),
                "Bold"=>(.6f,.55f,.7f,.2f),
                "Opportunist"=>(.6f,.6f,.8f,.35f),
                "Patient"=>(.65f,.7f,.6f,.9f),
                "Precise"=>(.85f,.7f,.5f,.6f),
                _=>(.7f,.7f,.6f,.5f)
            };
            return a;
        }

        // How the abilities turn into driving numbers. Kept together so checks and tuning see one table.
        public float CornerUse=>.88f+.07f*line;              // share of the steady cornering limit
        public float BrakeUse=>.86f+.11f*braking;            // share of brake/tyre deceleration
        public float ApexError=>.6f*(1-line);                // metres of seeded lateral error per corner
        public float BrakeScatter=>.05f*(1-braking);         // +- share of BrakeUse, seeded per corner
        public float PedalRise=>3+6*control;                 // pedal units per second when getting back on power
        public float LateralRate=>1.6f+1.6f*control;         // metres per second of deliberate lateral movement
        public float LaunchDelay=>.06f+.22f*(1-control);     // seconds after the lights
        public float SpellDip=>.005f+.035f*(1-consistency);  // grip/brake use lost during an off spell
        public float Reaction=>.25f-.13f*racecraft;          // Gipps reaction time in traffic, seconds
        public float Standstill=>1.3f-.7f*aggression;        // metres kept behind a car at low speed
        public float Clearance=>.85f-.55f*aggression;        // metres of side room wanted when passing
        public float AttackCooldown=>1.2f+4f*patience;       // seconds before another attempt after an abort
        public float FollowBeforeTrying=>.3f+2.5f*patience;  // seconds stuck behind before a marginal attempt
        public float LateBrake=>.035f*aggression*(.5f+.5f*braking); // extra brake use with inside overlap
    }
}
