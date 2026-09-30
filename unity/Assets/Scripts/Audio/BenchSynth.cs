using System;

namespace LittleRoad
{
    // The engine listening test (Richard, 30 Sep): rev from low to high, hold a few seconds at max revs, then back
    // down. One class drives both the in-game bench (EngineBench) and the harness WAVs, so they play the same test.
    // Free mode: throttle held by the player, with engine inertia and the limiter.
    public sealed class RevCycle
    {
        public const float Idle=1.5f,Pull=4f,Hold=3f,Lift=3f,Rest=1f;
        public static float Length=>Idle+Pull+Hold+Lift+Rest;
        readonly EngineSoundSpec spec;readonly float spoolStart,spoolFull,lag;
        public float Rpm {get;private set;}
        public float Boost {get;private set;}
        public float Throttle {get;private set;}
        public bool Limiter {get;private set;}
        public string Phase {get;private set;}="IDLE";
        float time=-1,limiterClock;

        public RevCycle(EngineSoundSpec engine,float spoolStart=.35f,float spoolFull=.75f,float lag=.6f)
        {
            spec=engine;this.spoolStart=spoolStart;this.spoolFull=Math.Max(spoolFull,spoolStart+.05f);this.lag=lag;Rpm=engine.idle;
        }
        public bool Running=>time>=0;
        public void Start(){time=0;}
        public void Stop(){time=-1;}
        // Advance by dt seconds; freeThrottle is used when the scripted cycle is not running.
        public void Step(float dt,float freeThrottle)
        {
            float idle=spec.idle,red=spec.redline;
            if(time>=0) {
                time+=dt;float t=time;
                if(t<Idle){Phase="IDLE";Throttle=0;Rpm=Approach(Rpm,idle,dt,.25f);}
                else if(t<Idle+Pull){Phase="PULL";Throttle=1;float k=(t-Idle)/Pull;Rpm=idle*1.15f+(red-idle*1.15f)*MathF.Pow(k,.85f);}
                else if(t<Idle+Pull+Hold){Phase="MAX REVS (LIMITER)";Throttle=1;Rpm=red;}
                else if(t<Idle+Pull+Hold+Lift){Phase="LIFT";Throttle=0;Rpm=Approach(Rpm,idle,dt,.9f);}
                else if(t<Length){Phase="IDLE";Throttle=0;Rpm=Approach(Rpm,idle,dt,.3f);}
                else{Phase="IDLE";time=-1;}
            } else {
                Throttle=Math.Clamp(freeThrottle,0,1);Phase=Throttle>.05f?"THROTTLE":"IDLE";
                // Free revving: climbs with throttle (quicker for small engines), falls back to idle when lifted.
                float target=idle+(red*1.02f-idle)*Throttle;float rate=Throttle>.05f&&target>Rpm?.55f:.7f;
                Rpm=Approach(Rpm,target,dt,rate);
            }
            // Limiter: cut and recover about 12 times a second at the redline.
            limiterClock+=dt;
            if(Rpm>=red*.995f&&Throttle>.5f){Limiter=(limiterClock*12)%1<.5f;if(Limiter)Rpm=red*.985f;else Rpm=Math.Min(Rpm,red);}
            else Limiter=false;
            float boostTarget=spec.turbo&&Throttle>.2f?SmoothStep((Rpm/red-spoolStart)/(spoolFull-spoolStart)):0;
            Boost=lag<=0?boostTarget:Approach(Boost,boostTarget,dt,boostTarget>Boost?lag*.5f:lag*.25f);
        }
        static float Approach(float from,float to,float dt,float seconds)=>to+(from-to)*MathF.Exp(-dt/Math.Max(.01f,seconds));
        static float SmoothStep(float x){x=Math.Clamp(x,0,1);return x*x*(3-2*x);}
    }
}
