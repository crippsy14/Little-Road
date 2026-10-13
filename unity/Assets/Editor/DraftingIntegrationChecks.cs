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
            float expected=DriveModel.DragK*speed*speed*DraftingModel.MaximumReduction/tuning.mass;
            Require(Mathf.Abs((newA-oldA)-expected)<.00001f&&oldY==newY,"force changes beyond aerodynamic loss");
        }
        checks.Add("Powered, coasting and braking acceleration differs only by the lost aerodynamic drag; steering grip is unchanged");
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
        float d=5;var f=road.Forward(d);var right=road.Right(d);var p=road.At(d);
        own=new ObservedCar(0,p,f*26,f,right,d,0,26,1.2f,2.3f);
        leader=new ObservedCar(1,p+f*9,f*26,f,right,d+9,0,26,1.2f,2.3f);
        traffic=new[]{own,leader};var o=new DriverObservation(100,1f/120,own,traffic,road,tuning,1,0,false,drafting:true);
        Require(o.dragReduction==DraftingModel.MaximumReduction,"live observation differs from kernel");
        var off=new DriverObservation(100,1f/120,own,traffic,road,tuning,1,0,false);
        Require(off.dragReduction==0,"disabled observation still drafts");
        checks.Add("A live observation's tow matches the drafting kernel; with drafting off it is zero");
        return checks;
    }
}
