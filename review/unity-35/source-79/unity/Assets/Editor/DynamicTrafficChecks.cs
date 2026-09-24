using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DynamicTrafficChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Dynamic traffic: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var planner=new DynamicRejoinPlanner(profile);
        float d=course.regionStarts[13]+60;var f=road.Forward(d);var r=road.Right(d);
        var self=new ObservedCar(0,road.At(d),f*20,f,r,d,0,20,1.2f,2.3f);
        var leader=new ObservedCar(1,road.At(d+18),road.Forward(d+18)*12,road.Forward(d+18),road.Right(d+18),d+18,0,12,1.2f,2.3f);
        var rivals=new[]{leader};var o=new DriverObservation(10,1f/120,self,rivals,road,CarTuning.Club,1,0,false);
        ContextPathState previous=default;planner.PrepareTraffic(in o,in previous,false);
        Require(planner.Count==6,"candidate capacity changed");bool left=false,right=false;
        for(int i=0;i<planner.Count;i++) {
            var p=planner.Path(i);Require(p.localSpeed,"traffic path lost own speed schedule");
            Require(Mathf.Abs(ContextPathFollower.Lateral(in p,0,line.Offset(p.anchor))-self.offset)<.0001f,"initial position discontinuity");
            if(!p.guide){left|=p.endOffset< -2;right|=p.endOffset>2;}
        }
        Require(left&&right,"missing opponent-relative passing sides");
        results.Add("Six bounded traffic curves preserve observed position and include both continuous passing sides");
        previous=RoadPathFeasibility.GuideFromMotion(in o,profile);previous.localSpeed=true;previous.consumed=2;
        planner.PrepareTraffic(in o,in previous,true);
        bool freshHold=false,retained=false;left=false;right=false;
        for(int i=0;i<planner.Count;i++) {
            var p=planner.Path(i);
            freshHold|=!p.guide&&p.consumed==0&&p.anchor==self.route&&p.endOffset==self.offset;
            retained|=p.guide&&p.consumed==2;
            if(!p.guide){left|=p.endOffset< -2;right|=p.endOffset>2;}
        }
        Require(planner.Count==6&&freshHold&&retained&&left&&right,"retained turn crowds out fresh hold or passing side");
        results.Add("A retained curve cannot displace the fresh hold or either passing side from the six-candidate set");
        int guideAlternative=-1;
        for(int i=1;i<planner.Count;i++)if(planner.Path(i).guide&&float.IsFinite(planner.EstimatedTime(i))){guideAlternative=i;break;}
        Require(guideAlternative<0||planner.FallbackRank(true)==guideAlternative,"scenery failure does not shortlist a finite guide alternative");
        results.Add("A scenery-blocked traffic proposal uses a guide alternative for its second physical check when available");
        var hold=RoadPathFeasibility.FromMotion(in o,0,false);hold.localSpeed=true;
        var eval=new ContextPathFollower(profile,false,true);
        float free=eval.EstimateGateTime(in o,in hold,120,false),following=eval.EstimateGateTime(in o,in hold,120,true);
        Require(following>free+.01f,"same-corridor slower leader creates no predicted delay");
        results.Add("Curve timing accounts for time lost behind a slower forward car");
        var rear=new ObservedCar(1,road.At(d-18),f*30,f,r,d-18,0,30,1.2f,2.3f);
        rivals[0]=rear;
        float rearTime=eval.EstimateGateTime(in o,in hold,120,true);
        Require(rearTime==free,"rear approach creates pace/yield incentive");
        results.Add("A wholly trailing car cannot create a time reward for yielding");
        rivals[0]=leader;planner.PrepareTraffic(in o,in previous,true);
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<20;i++)planner.PrepareTraffic(in o,in previous,true);
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm traffic ranking allocation");
        results.Add("Warm traffic curve generation and timing allocate zero managed bytes");
        return results;
    }
}
