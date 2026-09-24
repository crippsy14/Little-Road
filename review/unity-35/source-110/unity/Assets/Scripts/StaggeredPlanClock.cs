using System;

namespace LittleRoad
{
    // Only expensive route selection is staggered. The first decision is
    // immediate; steering, leader response and safety still run every step.
    public struct StaggeredPlanClock
    {
        bool initialized;
        float step;
        long nextTick;
        double lastTime;
        static long Tick(double time,float step)=>(long)Math.Round(time/step);
        public bool Due(double time,float dt)=>!initialized||dt!=step||time<lastTime||Tick(time,dt)>=nextTick;
        public void Completed(double time,float dt,int driverId,int groups=0)
        {
            if(!double.IsFinite(time)||!float.IsFinite(dt)||dt<=0||driverId<0||groups<0||groups>24)
                throw new ArgumentOutOfRangeException("Planning clock requires finite time, positive step and nonnegative driver ID.");
            // Tolerance only absorbs representation error at 30/60/120 Hz.
            // For non-divisors round down so a plan is never held longer.
            int period=Math.Max(1,(int)Math.Floor(.1/dt+1e-6));
            int count=groups==0?period:Math.Min(groups,period);
            long now=Tick(time,dt),phase=(driverId%count)*period/count;
            long delay=(phase-now%period+period)%period;
            if(delay==0)delay=period;
            nextTick=now+delay;step=dt;lastTime=time;initialized=true;
        }
    }
}
