using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class OpenSpacePriorityChecks
{
    sealed class Straight:IDriverRoute
    {public float Length=>10000;public Vector3 At(float d)=>Vector3.forward*d;public Vector3 Forward(float d)=>Vector3.forward;public Vector3 Right(float d)=>Vector3.right;}
    static ObservedCar Car(int id,float z,float side,float speed)=>new ObservedCar(id,new Vector3(side,0,z),Vector3.forward*speed,Vector3.forward,Vector3.right,z,side,speed,1.2f,2.3f);
    static void Require(bool value,string why){if(!value)throw new Exception("Open space: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();var route=new Straight();var self=Car(0,100,0,26);var other=Car(1,120,0,10);
        var traffic=new[]{self,other};var o=new DriverObservation(10,1f/120,self,traffic,route,CarTuning.Club,1,0,false);
        var estimate=new OpenSpacePriority();estimate.Prepare(in o,34);
        float blocked=estimate.HoldProgress,open=estimate.Progress(5.5f);
        Require(open>blocked+2&&open<=estimate.FreeProgress,"usable passing corridor has no greater progress");
        Require(OpenSpacePriority.Preference(open,blocked,estimate.FreeProgress)>0,"open route receives no priority");
        checks.Add("A route into open forward space gains priority over following a slower leader");
        Require(estimate.Progress(-5.5f)==open,"straight passing estimate is not symmetric");
        checks.Add("Left and right opportunities use the same progress model");
        var blockedObservation=new DriverObservation(10,1f/120,self,new[]{self,other,Car(2,120,5.5f,10)},route,CarTuning.Club,1,0,false);
        estimate.Prepare(in blockedObservation,34);Require(estimate.Progress(5.5f)<open,"occupied destination treated as empty");
        checks.Add("A second slow car occupying the proposed destination removes its open-space advantage");
        var empty=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),route,CarTuning.Club,1,0,false);
        estimate.Prepare(in empty,34);float free=estimate.FreeProgress;
        Require(estimate.HoldProgress==free&&estimate.Progress(5.5f)==free,"clear road invents a traffic delay");
        var rear=new DriverObservation(10,1f/120,self,new[]{self,Car(1,90,0,34)},route,CarTuning.Club,1,0,false);
        estimate.Prepare(in rear,34);Require(estimate.HoldProgress==free&&estimate.Progress(5.5f)==free,"rear car rewards leader yielding");
        checks.Add("Empty road and a faster rear follower do not create a reward for moving aside");
        estimate.Prepare(in o,34);long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int repeat=0;repeat<50;repeat++){estimate.Prepare(in o,34);for(int i=0;i<25;i++)estimate.Progress(-6+i*.5f);}
        Require(GC.GetAllocatedBytesForCurrentThread()==bytes,"warm estimate allocation");
        checks.Add("Owned preparation and all25 candidate progress estimates allocate no managed memory when warm");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club,new DriverPlanningSettings(.75f,5));
        float d=course.regionStarts[13]+190;var f=road.Forward(d);var right=road.Right(d);
        self=new ObservedCar(0,road.At(d),f*26,f,right,d,0,26,1.2f,2.3f);
        var clear=new DriverObservation(10,1f/120,self,Array.Empty<ObservedCar>(),road,CarTuning.Club,1,0,false);
        var original=new ContextPreferenceMap(profile,false,true,false,true);var candidate=new ContextPreferenceMap(profile,false,true,false,true,true);
        original.Fill(in clear,false,0,true,true);candidate.Fill(in clear,false,0,true,true);
        for(int i=0;i<original.Count;i++)Require(original.value[i]==candidate.value[i]&&original.reachable[i]==candidate.reachable[i],"clear-road preference changed");
        original.Pick(out float a);candidate.Pick(out float b);Require(a==b,"clear-road continuous target changed");
        checks.Add("Clear-road preference values and interpolated target remain exactly unchanged");
        other=new ObservedCar(1,road.At(d+20),road.Forward(d+20)*10,road.Forward(d+20),road.Right(d+20),d+20,0,10,1.2f,2.3f);
        var withLeader=new DriverObservation(10,1f/120,self,new[]{self,other},road,CarTuning.Club,1,0,false);
        original.Fill(in withLeader,false,0,true,true);candidate.Fill(in withLeader,false,0,true,true);
        bool changed=false;for(int i=0;i<original.Count;i++){changed|=candidate.value[i]!=original.value[i];Require(original.reachable[i]==candidate.reachable[i],"progress weakens corridor bounds");}
        Require(changed&&original.Count==candidate.Count&&original.ReachableLeft==candidate.ReachableLeft&&original.ReachableRight==candidate.ReachableRight,"priority absent or hard corridor changed");
        checks.Add("Progress changes tactical preference without changing candidate count or physical side reservations");
        var clearSelf=new ObservedCar(0,road.At(d)+right*5.5f,f*26,f,right,d,5.5f,26,1.2f,2.3f);
        var alreadyPassing=new DriverObservation(10,1f/120,clearSelf,new[]{clearSelf,other},road,CarTuning.Club,1,0,false);
        original.Fill(in alreadyPassing,true,5.5f,true,true);candidate.Fill(in alreadyPassing,true,5.5f,true,true);
        Require(candidate.OpenSpaceHoldProgress==candidate.OpenSpaceFreeProgress,"declared passing side is not open");
        Require(candidate.value[ContextPreferenceMap.Slots/2]<original.value[ContextPreferenceMap.Slots/2],"return toward slower traffic is not penalised");
        checks.Add("An already-open passing route retains a progress advantage over prematurely returning behind its slower rival");
        return checks;
    }
}
