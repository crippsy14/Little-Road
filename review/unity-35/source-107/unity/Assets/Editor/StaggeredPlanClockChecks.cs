using System;
using System.Collections.Generic;
using LittleRoad;

public static class StaggeredPlanClockChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Plan clock: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();
        foreach(float dt in new[]{1f/120,1f/60,1f/50,1f/30,.013f,.06f}){
            int period=Math.Max(1,(int)Math.Floor(.1/dt+1e-6));
            var clocks=new StaggeredPlanClock[24];var last=new int[24];
            for(int tick=0;tick<6000;tick++){
                int count=0;double time=1000+(double)tick*dt;
                for(int id=0;id<24;id++){
                    bool due=clocks[id].Due(time,dt);
                    Require(due==clocks[id].Due(time,dt),"preparation changed due state");
                    if(!due)continue;
                    if(tick>0)Require(tick-last[id]<=period,"plan held beyond cadence");
                    clocks[id].Completed(time,dt,id);last[id]=tick;count++;
                    Require(!clocks[id].Due(time,dt),"same observation selected twice");
                }
                if(tick==0)Require(count==24,"initial plans delayed");
                else Require(count<=Math.Ceiling(24.0/period),"steady planning burst not bounded");
            }
        }
        checks.Add("Staggered24-driver plans start immediately, distribute across physics ticks and retain at least10Hz across six fixed-step rates");
        var clock=new StaggeredPlanClock();const float step=1f/120;
        clock.Completed(20,step,0);
        Require(clock.Due(25,step)&&clock.Due(25,step),"suppressed/recovery plan lost");
        clock.Completed(25,step,0);
        Require(clock.Due(24,step),"rewound time kept stale deadline");
        Require(clock.Due(25,1f/60),"changed fixed step kept stale deadline");
        bool invalid=false;try{clock.Completed(0,0,0);}catch(ArgumentOutOfRangeException){invalid=true;}
        Require(invalid,"invalid step accepted");
        checks.Add("Staggered clock remains due through recovery suppression and resets on changed step or rewound time");
        clock.Completed(20,step,3);
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++){double time=20+i*(double)step;if(clock.Due(time,step))clock.Completed(time,step,3);}
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"clock allocates");
        checks.Add("Steady planning clock allocates no managed memory");
        return checks;
    }
}
