using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class PreparedRankingChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Prepared ranking: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        float distance=course.regionStarts[13]+190;var forward=road.Forward(distance);var right=road.Right(distance);
        var self=new ObservedCar(0,road.At(distance),forward*18,forward,right,distance,0,18,1.2f,2.3f);
        var other=new ObservedCar(1,road.At(distance+13),forward*14,forward,right,distance+13,0,14,1.2f,2.3f);
        var traffic=new[]{self,other};
        var o=new DriverObservation(20,1f/120,self,traffic,road,CarTuning.Club,1,0,false);
        var previous=RoadPathFeasibility.GuideFromMotion(in o,profile);previous.localSpeed=true;previous.consumed=3.5f;
        foreach(bool aware in new[]{false,true})foreach(bool hasPrevious in new[]{false,true}){
            var source=new DynamicRejoinPlanner(profile,true,true);var reused=new DynamicRejoinPlanner(profile,true,true);var fresh=new DynamicRejoinPlanner(profile,true,true);
            // Repeated calls ensure cumulative diagnostics are not copied from
            // speculation or overwritten, including fallback/selection state.
            for(int iteration=0;iteration<3;iteration++){
                source.PrepareOwned(in o,in previous,hasPrevious,aware);
                Require(reused.TryUsePrepared(source,in o,in previous,hasPrevious,aware),"identical inputs rejected");
                if(aware)fresh.PrepareTraffic(in o,in previous,hasPrevious);else fresh.Prepare(in o,in previous,hasPrevious);
                Require(reused.Count==fresh.Count&&reused.Horizon==fresh.Horizon&&reused.CornerExit==fresh.CornerExit,"horizon/count differs");
                for(int i=0;i<fresh.Count;i++)Require(reused.Path(i).Equals(fresh.Path(i))&&reused.EstimatedTime(i)==fresh.EstimatedTime(i)&&reused.Clearance(i)==fresh.Clearance(i),"ranked curve/cost/clearance differs");
                foreach(bool scenery in new[]{false,true})foreach(bool corridor in new[]{false,true})Require(reused.FallbackRank(scenery,corridor)==fresh.FallbackRank(scenery,corridor),"physical fallback order differs");
                reused.Selected(0);fresh.Selected(0);
                Require(JsonUtility.ToJson(reused.Export(0))==JsonUtility.ToJson(fresh.Export(0)),"live counters/selection history differ");
                Require(!reused.TryUsePrepared(source,in o,in previous,hasPrevious,aware),"prepared ranking consumed twice");
            }
        }
        results.Add("Prepared solo/traffic ranking exactly preserves curves, ordering, costs, fallbacks and cumulative live diagnostics with/without a prior curve");
        var prepared=new DynamicRejoinPlanner(profile,true,true);var live=new DynamicRejoinPlanner(profile,true,true);
        // Independently change all input families used by the planner. Failed
        // reuse must not mutate the receiving planner or become usable later.
        for(int change=0;change<12;change++){
            prepared.PrepareOwned(in o,in previous,true,true);
            var tuning=CarTuning.Club;if(change==2)tuning.power+=.01f;
            var own=change==3?new ObservedCar(0,self.position,self.velocity+right*.01f,forward,right,distance,0,18,1.2f,2.3f):self;
            var rivals=change==4?new[]{other,self}:change==5?new[]{self}:traffic;
            var changedPath=previous;if(change==6)changedPath.consumed+=.01f;
            var changed=new DriverObservation(change==0?20.01:20,change==1?1f/60:1f/120,own,rivals,road,tuning,change==9?.9f:1,change==10?.1f:0,change==11,0,change==8,change==8?.04f:-1);
            Require(!live.TryUsePrepared(prepared,in changed,in changedPath,change!=7,true),"changed input accepted: "+change);
            Require(live.Evaluated==0&&!live.TryUsePrepared(prepared,in o,in previous,true,true),"failed input changed live state or survived mismatch");
        }
        prepared.PrepareOwned(in o,in previous,true,true);
        traffic[1]=new ObservedCar(1,other.position,other.velocity,forward,right,other.route,.1f,14,1.2f,2.3f);
        Require(!live.TryUsePrepared(prepared,in o,in previous,true,true),"mutable traffic buffer was borrowed instead of copied");traffic[1]=other;
        prepared.PrepareOwned(in o,in previous,true,true);
        Require(!live.TryUsePrepared(prepared,in o,in previous,true,false),"solo/traffic mode mismatch accepted");
        prepared.PrepareOwned(in o,in previous,true,true);
        var differentProfile=new DynamicRejoinPlanner(new RacingSpeedProfile(line,CarTuning.Club,new DriverPlanningSettings(.65f)),true,true);
        Require(!differentProfile.TryUsePrepared(prepared,in o,in previous,true,true),"different driver profile accepted");
        prepared.PrepareOwned(in o,in previous,true,true);prepared.InvalidatePrepared();
        Require(!live.TryUsePrepared(prepared,in o,in previous,true,true),"invalidated preparation accepted");
        results.Add("Prepared ranking rejects changed time, step, car/traffic, equipment, path, driving state, mode and profile; owns traffic and consumes once");
        prepared.PrepareOwned(in o,in previous,true,true);live.TryUsePrepared(prepared,in o,in previous,true,true);
        long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<20;i++){prepared.PrepareOwned(in o,in previous,true,true);Require(live.TryUsePrepared(prepared,in o,in previous,true,true),"warm reuse rejected");}
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm ranking reuse allocates");
        results.Add("Owned ranking preparation and guarded reuse allocate no managed memory when warm");
        return results;
    }
}
