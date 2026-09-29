using System.Diagnostics;

namespace LittleRoad
{
    // Script time per rendered frame for the frame probe: every car's fixed step (driving + AI) and the
    // race's own step, in stopwatch ticks, plus how many physics steps ran. FrameProbe takes and resets it.
    public static class FrameCost
    {
        public static long Cars,Race,Plan,PlanMax,Physics;public static int Steps;
        public static (float carsMs,float raceMs,int steps) Take()
        {
            double toMs=1000.0/Stopwatch.Frequency;var r=((float)(Cars*toMs),(float)(Race*toMs),Steps);Cars=0;Race=0;Steps=0;return r;
        }
        public static (float planMs,float planMaxMs,float physicsMs) TakePlan(){double toMs=1000.0/Stopwatch.Frequency;var r=((float)(Plan*toMs),(float)(PlanMax*toMs),(float)(Physics*toMs));Plan=0;PlanMax=0;Physics=0;return r;}
    }
}
