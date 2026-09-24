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
        bool freshHold=false,retained=false;int holdRank=-1;left=false;right=false;
        for(int i=0;i<planner.Count;i++) {
            var p=planner.Path(i);
            freshHold|=!p.guide&&p.consumed==0&&p.anchor==self.route&&p.endOffset==self.offset;
            if(!p.guide&&p.consumed==0&&p.anchor==self.route&&p.endOffset==self.offset)holdRank=i;
            retained|=p.guide&&p.consumed==2;
            if(!p.guide){left|=p.endOffset< -2;right|=p.endOffset>2;}
        }
        Require(planner.Count==6&&freshHold&&retained&&left&&right,"retained turn crowds out fresh hold or passing side");
        results.Add("A retained curve cannot displace the fresh hold or either passing side from the six-candidate set");
        Require(holdRank>=0&&(holdRank==0||planner.FallbackRank(false,true)==holdRank),"corridor failure ignores fresh hold");
        results.Add("An occupied lateral corridor spends the fallback check on the fresh hold when not already checked");
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
        // Packed launch: the nearest car is alongside in the middle column;
        // the slower car ahead occupies our outer column. Both road sides and
        // a start-line wrap must retain an outward passing proposal.
        foreach(float route in new[]{50f,road.length-4})foreach(float side in new[]{-1f,1f}) {
            float lateral=side*3.5f;var forward=road.Forward(route);var rightAxis=road.Right(route);
            var own=new ObservedCar(0,road.At(route)+rightAxis*lateral,forward*25,forward,rightAxis,route,lateral,25,1.17f,2.3f);
            float leadRoute=Mathf.Repeat(route+11,road.length),besideRoute=Mathf.Repeat(route+1,road.length);
            var ahead=new ObservedCar(1,road.At(leadRoute)+road.Right(leadRoute)*lateral,road.Forward(leadRoute)*20,road.Forward(leadRoute),road.Right(leadRoute),leadRoute,lateral,20,1.17f,2.3f);
            var beside=new ObservedCar(2,road.At(besideRoute),road.Forward(besideRoute)*25,road.Forward(besideRoute),road.Right(besideRoute),besideRoute,0,25,1.17f,2.3f);
            var packed=new DriverObservation(20,1f/120,own,new[]{beside,ahead},road,CarTuning.Club,1,0,false);
            var targeted=new DynamicRejoinPlanner(profile,true,true);
            var old=new DynamicRejoinPlanner(profile,true,false);
            ContextPathState prior=default;targeted.PrepareTraffic(in packed,in prior,false);old.PrepareTraffic(in packed,in prior,false);
            bool outward=false,oldOutward=false;
            for(int rank=0;rank<targeted.Count;rank++)outward|=!targeted.Path(rank).guide&&targeted.Path(rank).endOffset*side>6;
            for(int rank=0;rank<old.Count;rank++)oldOutward|=!old.Path(rank).guide&&old.Path(rank).endOffset*side>6;
            Require(outward&&!oldOutward,"alongside rival hides the blocking leader's outer passing route");
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int n=0;n<8;n++)targeted.PrepareTraffic(in packed,in prior,false);
            Require(GC.GetAllocatedBytesForCurrentThread()==before,"blocking target allocates on warm plans");
        }
        results.Add("Blocking-leader proposals expose both outer passes despite a nearer alongside rival, including wrapped start-line routes");
        results.Add("Blocking-leader candidate ranking retains zero warm managed allocation");
        return results;
    }
}
