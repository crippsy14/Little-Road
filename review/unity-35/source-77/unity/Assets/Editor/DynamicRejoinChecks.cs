using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DynamicRejoinChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Dynamic rejoin: "+why);}
    public static List<string> Run()
    {
        var results=new List<string>();var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var planner=new DynamicRejoinPlanner(profile);
        float d=course.regionStarts[13]+190;var f=road.Forward(d);var r=road.Right(d);
        var self=new ObservedCar(0,road.At(d)+r*4,f*18+r*.8f,f,r,d,4,18,1.2f,2.3f);
        var o=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        ContextPathState previous=default;planner.Prepare(in o,in previous,false);
        Require(planner.Count==5&&!float.IsInfinity(planner.EstimatedTime(0)),"no bounded viable initial set");
        for(int i=0;i<planner.Count;i++) {
            var p=planner.Path(i);float start=ContextPathFollower.Lateral(in p,0,line.Offset(p.anchor));
            Require(p.localSpeed&&p.guide&&!p.rejoin&&Mathf.Abs(start-self.offset)<.0001f,"curve does not begin at observed position");
            float end=ContextPathFollower.Lateral(in p,p.blend,line.Offset(p.anchor+p.blend));
            Require(Mathf.Abs(end-line.Offset(p.anchor+p.blend))<.0001f,"curve misses its guide endpoint");
            float left=ContextPathFollower.Lateral(in p,p.blend-.02f,line.Offset(p.anchor+p.blend-.02f));
            float expected=line.Offset(p.anchor+p.blend-.02f);
            Require(Mathf.Abs(left-expected)<.001f,"endpoint approach is discontinuous");
        }
        results.Add("Five continuous solo rejoin curves start at observed offset and meet the guide at different future points");
        previous=planner.Path(0);previous.consumed=2;
        planner.Prepare(in o,in previous,true);Require(planner.Count==6,"current continuation not included");
        bool found=false;for(int i=0;i<planner.Count;i++)found|=planner.Path(i).consumed==2;
        Require(found,"current trajectory loses consumed progress during comparison");
        results.Add("The current viable curve is a sixth candidate with preserved consumed geometry");
        previous=planner.Path(0);var changed=previous;changed.localSpeed=!previous.localSpeed;
        var a=new ForecastRequest(in o,in previous,Vector3.zero,new Vector3(1.2f,.5f,2.3f));
        var b=new ForecastRequest(in o,in changed,Vector3.zero,new Vector3(1.2f,.5f,2.3f));
        Require(!a.Matches(in b),"speed model omitted from owned forecast identity");
        results.Add("A per-path local speed schedule invalidates a forecast using a different speed model");
        planner.Prepare(in o,in previous,false);long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<20;i++)planner.Prepare(in o,in previous,false);
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm curve ranking allocation");
        results.Add("Bounded common-gate curve ranking allocates no managed memory when warm");
        var path=planner.Path(0);var follower=new ContextPathFollower(profile,false);
        var live=follower.Follow(in o,in path);var prediction=new ContextPathPrediction(profile,false);
        prediction.Predict(in o,path,Vector3.zero,new Vector3(1.2f,.5f,2.3f),1);
        var forecast=prediction.samples[0].command;
        Require(live.pedal==forecast.pedal&&live.steering==forecast.steering&&live.targetSpeed==forecast.targetSpeed,"local guide speed differs in forecast");
        results.Add("Dynamic guide curves use the same local speed commands in live and physical forecast control");
        return results;
    }
}
