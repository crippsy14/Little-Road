using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TrafficProgressChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Traffic progress: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);
        var self=new ObservedCar(0,road.At(d),forward*18,forward,road.Right(d),d,0,18,1.2f,2.3f);
        var lead=new ObservedCar(1,road.At(d+10),road.Forward(d+10)*10,road.Forward(d+10),road.Right(d+10),d+10,0,10,1.2f,2.3f);
        var rivals=new[]{lead};var o=new DriverObservation(1,1f/120,self,rivals,road,CarTuning.Club,1,0,false);
        var requested=new DriveCommand(1,.1f,33,0,0);
        var limited=MovingLeaderControl.Apply(in o,requested,out int id,out float limit,out bool active);
        Check(active&&id==1&&limit<self.speed&&limited.pedal<0&&limited.steering==requested.steering,"Slower close leader constrains power while preserving steering");
        var rear=new ObservedCar(1,road.At(d-10),forward*30,forward,road.Right(d-10),d-10,0,30,1.2f,2.3f);
        var rearObservation=new DriverObservation(2,1f/120,self,new[]{rear},road,CarTuning.Club,1,0,false);
        var unchanged=MovingLeaderControl.Apply(in rearObservation,requested,out _,out _,out bool rearLimited);
        Check(!rearLimited&&unchanged.pedal==requested.pedal,"Faster rear car does not become a following speed cap");
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var prediction=new ContextPathPrediction(profile);
        var path=RoadPathFeasibility.FromMotion(in o,0,false);var half=new Vector3(1.2f,.5f,2.3f);
        prediction.Predict(in o,path,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true);
        float free=prediction.samples[prediction.Count-1].route;
        var traffic=new ObservedTrafficRollout();traffic.Prepare(in o);
        prediction.Predict(in o,path,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
        var bounded=prediction.samples[prediction.Count-1].state;float constrained=prediction.samples[prediction.Count-1].route;
        Check(constrained<free-2&&constrained>d+10,"Forecasted progress reflects the actual slower-leader controller");
        rivals[0]=rear;
        prediction.Predict(in o,path,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
        var repeated=prediction.samples[prediction.Count-1].state;
        Check(repeated.position.Equals(bounded.position)&&repeated.velocity.Equals(bounded.velocity),"Prepared traffic owns its observations despite source-buffer mutation");
        bool rejected=false;var stale=new DriverObservation(3,1f/120,self,rivals,road,CarTuning.Club,1,0,false);
        try{traffic.Validate(in stale,3,240);}catch(InvalidOperationException){rejected=true;}
        Check(rejected,"Traffic score rejects a different decision timestamp");
        rivals[0]=lead;traffic.Prepare(in o);
        prediction.Predict(in o,path,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<3;i++){traffic.Prepare(in o);prediction.Predict(in o,path,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);}
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(allocated==0,"Warm owned traffic progress forecasts allocate no managed bytes");
        return checks;
    }
}
