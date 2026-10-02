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
                // Duke Rolo, the final boss (docs/54): the best line and car control in the game.
                "Duke"=>(1f,.95f,.75f,.6f),
                _=>(.7f,.7f,.6f,.5f)
            };
            return a;
        }

        // Test fixture (--racer-abilities line-0.2_braking-1): override named abilities for every driver.
        public RacerAbilities WithOverrides(string spec)
        {
            if(string.IsNullOrWhiteSpace(spec))return this;var a=this;
            foreach(var pair in spec.Split('_',',')) {
                var kv=pair.Split(new[]{'-','='},2);if(kv.Length!=2||!float.TryParse(kv[1],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float v))throw new ArgumentException("Bad ability override: "+pair);
                v=Mathf.Clamp01(v);
                switch(kv[0].Trim()) {
                    case "braking":a.braking=v;break;case "line":a.line=v;break;case "control":a.control=v;break;case "consistency":a.consistency=v;break;
                    case "racecraft":a.racecraft=v;break;case "strategy":a.strategy=v;break;case "aggression":a.aggression=v;break;case "patience":a.patience=v;break;
                    case "all":a.braking=a.line=a.control=a.consistency=a.racecraft=v;break;
                    default:throw new ArgumentException("Unknown ability: "+kv[0]);
                }
            }
            return a;
        }

        // How the abilities turn into driving numbers. Kept together so checks and tuning see one table.
        // Shortfalls count to the power 1.5: a 0.8 driver is only a little off the best, a 0.5 driver clearly slower,
        // and a novice at 0 up to ~50% slower over a lap (docs/46, "abilities that show"). The rating shown to
        // players is 1 + 19 x ability, so 1/20 to 20/20.
        static float Short(float ability)=>Mathf.Pow(1-Mathf.Clamp01(ability),1.5f);
        public float CornerUse=>.95f-.55f*Short(line);       // share of the steady cornering limit
        public float BrakeUse=>.97f-.5f*Short(braking);      // share of brake/tyre deceleration
        public float ApexError=>1.2f*(1-line);               // metres of seeded lateral error per corner
        public float BrakeMargin=>8*(1-braking)+35*Short(braking); // metres early a driver wants to be at corner speed
        public float CoastDistance=>90*Short(braking);       // metres of lift-and-coast before slowing for a corner
        public float CornerThrottle=>1-.75f*Short(control);  // throttle limit while still turning
        public float BrakeScatter=>.05f*(1-braking);         // +- share of BrakeUse, seeded per corner
        public float PedalRise=>9-7.5f*Short(control);       // pedal units per second when getting back on power
        public float LateralRate=>3.2f-1.8f*Short(control);  // metres per second of deliberate lateral movement
        public float LaunchDelay=>.06f+.22f*(1-control);     // seconds after the lights
        public float SpellDip=>.01f+.15f*Short(consistency); // grip/brake use lost during an off spell
        public static int Rating(float ability)=>1+Mathf.RoundToInt(19*Mathf.Clamp01(ability));
        public float Reaction=>.25f-.13f*racecraft;          // Gipps reaction time in traffic, seconds
        public float Standstill=>1.3f-.7f*aggression;        // metres kept behind a car at low speed
        public float Clearance=>.85f-.55f*aggression;        // metres of side room wanted when passing
        public float AttackCooldown=>1.2f+4f*patience;       // seconds before another attempt after an abort
        public float FollowBeforeTrying=>.3f+2.5f*patience;  // seconds stuck behind before a marginal attempt
        public float LateBrake=>.035f*aggression*(.5f+.5f*braking); // extra brake use with inside overlap
    }
}
