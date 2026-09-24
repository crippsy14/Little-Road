using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class DraftingIntegrationChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Drafting integration: "+why);}
    static ObservedCar Car(int id,float d,float side=0,float speed=30)=>new ObservedCar(id,new Vector3(side,0,d),Vector3.forward*speed,Vector3.forward,Vector3.right,d,side,speed,1.2f,2.3f);
    public static List<string> Run()
    {
        var checks=new List<string>();var tuning=CarTuning.Club;
        foreach(float pedal in new[]{-1f,-.35f,0f,.5f,1f})foreach(float speed in new[]{12f,26f,36f}) {
            DriveModel.Forces(tuning,speed,pedal,.08f,tuning.roadGrip,0,out float oldA,out float oldY);
            DriveModel.Forces(tuning,speed,pedal,.08f,tuning.roadGrip,0,out float newA,out float newY,DraftingModel.MaximumReduction);
            float expected=.006f*speed*speed*DraftingModel.MaximumReduction/tuning.mass;
            Require(Mathf.Abs((newA-oldA)-expected)<.00001f&&oldY==newY,"force changes beyond aerodynamic loss");
        }
        checks.Add("Powered, coasting and braking acceleration differs only by the lost aerodynamic drag; steering grip is unchanged");
        var solo=new PlanarDrivePrediction(Vector3.zero,Vector3.forward*26,Vector3.forward,0,0);var tow=solo;
        float soloTime=-1,towTime=-1;
        for(int i=0;i<360;i++){
            solo.Step(tuning,1,0,1f/120);tow.Step(tuning,1,0,1f/120,DraftingModel.MaximumReduction);
            if(soloTime<0&&solo.velocity.z>=32)soloTime=(i+1)/120f;
            if(towTime<0&&tow.velocity.z>=32)towTime=(i+1)/120f;
        }
        float gain=tow.velocity.z-solo.velocity.z;
        Require(towTime>0&&soloTime-towTime>.01f&&soloTime-towTime<.25f&&tow.position.z>solo.position.z,"fixed-wake acceleration benefit missing/excessive: times="+soloTime+","+towTime);
        Require(tow.velocity.z<tuning.gearCeiling+.15f,"draft bypasses gearing");
        checks.Add("Constant-wake 26 to 32 m/s: solo "+soloTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" s, tow "+towTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" s; gearing remains enforced");
        var own=Car(0,0);var leader=Car(1,9);var traffic=new[]{own,leader};
        var branch=new ObservedCar(1,leader.position,leader.velocity,leader.forward,leader.right,500,0,30,1.2f,2.3f);
        Require(DraftingModel.Reduction(in own,new[]{branch},out _,1000)==0,"parallel track branch grants tow");
        var seamOwn=Car(0,995);var seamLead=new ObservedCar(1,new Vector3(0,0,1004),Vector3.forward*30,Vector3.forward,Vector3.right,4,0,30,1.2f,2.3f);
        Require(DraftingModel.Reduction(in seamOwn,new[]{seamLead},out _,1000)>0,"track seam loses wake");
        checks.Add("Route identity rejects a parallel branch while retaining a legitimate start/finish seam wake");
        var future=new ObservedCar(0,new Vector3(4,0,30),Vector3.forward*30,Vector3.forward,Vector3.right,30,4,30,1.2f,2.3f);
        Require(DraftingModel.Reduction(in future,traffic,out _,1000,1)==0,"forecast keeps tow after lateral exit");
        var futureAligned=Car(0,30);
        Require(DraftingModel.Reduction(in futureAligned,traffic,out _,1000,1)==DraftingModel.MaximumReduction,"forecast forgot forward movement of source");
        checks.Add("Prospective wake follows observed source velocity and disappears after the chasing car pulls out");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,tuning,drafting:true);
        float d=5;var f=road.Forward(d);var right=road.Right(d);var p=road.At(d);
        own=new ObservedCar(0,p,f*26,f,right,d,0,26,1.2f,2.3f);
        leader=new ObservedCar(1,p+f*9,f*26,f,right,d+9,0,26,1.2f,2.3f);
        traffic=new[]{own,leader};var o=new DriverObservation(100,1f/120,own,traffic,road,tuning,1,0,false,drafting:true);
        Require(o.dragReduction==DraftingModel.MaximumReduction,"live observation differs from kernel");
        var path=RoadPathFeasibility.FromMotion(in o,0,false);
        var request=new ForecastRequest(in o,in path,Vector3.zero,new Vector3(1.2f,.5f,2.3f));
        var direct=new ContextPathPrediction(profile);request.Run(direct,traffic);
        var job=new ForecastWork(new ContextPathPrediction(profile));job.Set(in request,traffic);
        Require(job.MatchesTraffic(traffic),"fresh traffic mismatch");
        // Mutate the caller's array after Set: the worker must have its own copy.
        traffic[1]=new ObservedCar(1,p+right*20,f*26,f,right,d,20,26,1.2f,2.3f);
        Require(!job.MatchesTraffic(traffic),"changed traffic can reuse old forecast");
        var jobs=new[]{job};using(var pool=new ForecastWorkerPool(2)) {
            pool.Run(jobs,1);
            for(int i=0;i<direct.Count;i++)Require(direct.samples[i].Equals(job.prediction.samples[i]),"serial/owned worker divergence at "+i);
            Require(direct.envelope.MaximumBound==job.prediction.envelope.MaximumBound,"worker envelope differs");
            pool.Run(jobs,1);Require(job.AllocatedBytes==0&&pool.MainAllocatedBytes==0,"warm draft worker allocation");
        }
        checks.Add("Drafting forecasts own opponent observations, reject changed sources and match serial motion/envelope exactly with zero warm allocation");
        var off=new DriverObservation(100,1f/120,own,traffic,road,tuning,1,0,false);
        var offRequest=new ForecastRequest(in off,in path,Vector3.zero,new Vector3(1.2f,.5f,2.3f));
        Require(!request.Matches(in offRequest)&&off.dragReduction==0,"disabled request reused drafting output");
        checks.Add("Drafting is part of forecast identity; disabled observations retain zero assistance");
        // A following car must still brake for a slower leader despite the tow.
        own=Car(0,0,0,30);leader=Car(1,12,0,15);
        o=new DriverObservation(101,1f/120,own,new[]{own,leader},road,tuning,1,0,false,drafting:true);
        var guard=new ImmediateTrafficSafety();var command=guard.Apply(in o,new DriveCommand(1,0,40,0,0));
        Require(command.pedal<1&&guard.Active,"draft bypasses imminent following constraint");
        checks.Add("Immediate traffic safety still intervenes for an imminent slower-leader collision with drafting enabled");
        return checks;
    }
}
