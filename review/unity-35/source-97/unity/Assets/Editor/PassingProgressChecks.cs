using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class PassingProgressChecks
{
    sealed class Straight:IDriverRoute
    {public float Length=>10000;public Vector3 At(float d)=>Vector3.forward*d;public Vector3 Forward(float d)=>Vector3.forward;public Vector3 Right(float d)=>Vector3.right;}
    static ObservedCar Car(int id,float z,float side,float speed)=>new ObservedCar(id,new Vector3(side,0,z),Vector3.forward*speed,Vector3.forward,Vector3.right,z,side,speed,1.2f,2.3f);
    static void Require(bool condition,string why){if(!condition)throw new Exception("Integrated passing: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();var road=new Straight();var model=new PassingProgress();
        var self=Car(0,100,0,18);var rival=Car(1,125,0,10);
        var o=new DriverObservation(10,1f/120,self,new[]{self,rival},road,CarTuning.Club,1,0,false);
        model.Prepare(in o,34);float hold=model.HoldProgress,open=model.Progress(5.5f),free=model.FreeProgress;
        Require(open>hold+1&&open<=free+.001f,"passing has no measured forward payoff or exceeds free acceleration");
        Require(model.Progress(-5.5f)==open,"asymmetric passing opportunity");
        checks.Add("Integrated passing accumulates a symmetric forward payoff after clearing slower traffic, within free progress");
        var blocked=new DriverObservation(10,1f/120,self,new[]{self,rival,Car(2,125,5.5f,10)},road,CarTuning.Club,1,0,false);
        model.Prepare(in blocked,34);Require(model.Progress(5.5f)<open,"occupied route receives the same payoff as a clear pass");
        checks.Add("A second slow car ahead in the destination removes the open passing payoff");
        var empty=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        model.Prepare(in empty,34);Require(model.HoldProgress==model.FreeProgress&&model.Progress(5.5f)==model.FreeProgress,"empty road invents a passing bonus");
        float emptyProgress=model.FreeProgress;
        var rear=new DriverObservation(10,1f/120,self,new[]{self,Car(1,90,0,34)},road,CarTuning.Club,1,0,false);
        model.Prepare(in rear,34);Require(model.HoldProgress==emptyProgress&&model.Progress(5.5f)==emptyProgress,"rear follower induces yielding");
        checks.Add("Clear road and faster rear followers do not reward a path switch");
        var start=Car(0,100,0,0);
        var stationary=new DriverObservation(10,1f/120,start,new[]{start,Car(1,106,0,0)},road,CarTuning.Club,1,0,false);
        model.Prepare(in stationary,34);Require(model.HoldProgress<6-4.6f&&model.HoldProgress>=0,"launch forecast assumes a stopped rival accelerates");
        checks.Add("A stationary close leader is not assigned invented acceleration to justify launching through it");
        var stock=new DriverObservation(10,1f/120,start,Array.Empty<ObservedCar>(),road,CarTuning.Stock,1,0,false);
        var club=new DriverObservation(10,1f/120,start,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        model.Prepare(in stock,34);float stockProgress=model.FreeProgress;model.Prepare(in club,34);
        Require(model.FreeProgress>stockProgress,"equipment cannot improve the integrated acceleration phase");
        checks.Add("The same four-second acceleration estimate respects stronger equipment");
        model.Prepare(in o,34);float expected=model.Progress(5.5f);long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int n=0;n<20;n++){model.Prepare(in o,34);for(int i=0;i<25;i++)model.Progress(-6+i*.5f);}
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes&&model.Progress(5.5f)==expected,"allocation or repeat instability");
        checks.Add("Warm bounded passing estimates allocate zero memory and reproduce the same observed encounter");
        return checks;
    }
}
