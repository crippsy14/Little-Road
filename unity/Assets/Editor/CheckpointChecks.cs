using System;
using System.Collections.Generic;
using System.Reflection;
using LittleRoad;
using UnityEngine;

public static class CheckpointChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Checkpoint check failed: "+name);checks.Add(name);}
        Check(CheckpointCrossing.Forward(-.1f,.1f,0,0,0,10),"Forward centre crossing accepted");
        Check(CheckpointCrossing.Forward(-.1f,.1f,-9.9f,-9.9f,0,10)&&CheckpointCrossing.Forward(-.1f,.1f,9.9f,9.9f,0,10),"Both legal road edges accepted");
        Check(!CheckpointCrossing.Forward(-.1f,.1f,10.1f,10.1f,0,10),"Crossing outside checkpoint width rejected");
        Check(CheckpointCrossing.Forward(-.1f,.9f,0,12,0,10),"Legal crossing still counts when car ends sample outside road");
        Check(!CheckpointCrossing.Forward(-.1f,.9f,12,0,0,10),"Rejoining after an outside crossing cannot award a gate");
        Check(!CheckpointCrossing.Forward(.1f,-.1f,0,0,0,10),"Reverse crossing rejected");
        Check(!CheckpointCrossing.Forward(0,0,0,0,0,10)&&CheckpointCrossing.Forward(0,.1f,0,0,0,10),"Standing on line cannot score; forward departure can");
        Check(!CheckpointCrossing.Forward(1,2,0,0,0,10),"Being downstream cannot silently skip a missing gate");
        Check(CheckpointCrossing.Forward(-.1f,8.5f,0,0,0,10),"Swept crossing cannot step over an eight-metre detection window");
        var root=new GameObject("Checkpoint checks");
        try {
            var race=root.AddComponent<RaceTrial>();race.track=new Circuit();race.requiredLaps=3;race.elapsed=1;
            var car=new GameObject("Car").AddComponent<RoadCar>();car.transform.SetParent(root.transform);car.race=race;
            var observe=typeof(RoadCar).GetMethod("TrackGates",BindingFlags.Instance|BindingFlags.NonPublic);
            void Cross(float gate,float lateral) {
                car.progress=gate+.1f;car.pathDistance=Mathf.Repeat(car.progress,race.track.length);car.offset=lateral;
                car.speed=-2;car.transform.rotation=Quaternion.LookRotation(-race.track.Forward(car.pathDistance));
                observe.Invoke(car,new object[]{gate-.1f,lateral});
            }
            Cross(0,0);Check(car.nextGate==1&&car.laps==0,"Spun car crossing starts timing without awarding a lap");
            car.progress=car.nextGateProgress+9;observe.Invoke(car,new object[]{car.progress-1,0f});
            Check(car.nextGate==1&&car.CheckpointMissed&&car.CheckpointMessage.Contains("R / D-pad Down"),"Missed gate stays required and exposes recovery guidance");
            Check(car.RankedProgress==car.nextGateProgress,"Missing gate cannot inflate race ranking beyond unvalidated progress");
            car.progress=car.nextGateProgress-1;observe.Invoke(car,new object[]{car.nextGateProgress+1,0f});
            Check(car.nextGate==1&&!car.CheckpointMissed,"Returning behind missed gate gives no reverse-crossing credit");
            for(int i=1;i<=36;i++) {race.elapsed=i+1;Cross(car.nextGateProgress,i%2==0?9.5f:-9.5f);
                if(i==12||i==24)Check(car.laps==i/12&&!car.finished,"Ordered lap "+car.laps+" remains unfinished before required third lap");}
            Check(car.finished&&car.laps==3&&car.finishTime==37,"Exactly three ordered full laps finish, including start-line wrap");
            int lastGate=car.nextGate;Cross(car.nextGateProgress,0);race.Finish(car);
            var finishOrder=(List<RoadCar>)typeof(RaceTrial).GetField("finishOrder",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(race);
            Check(car.laps==3&&car.nextGate==lastGate&&finishOrder.Count==1,"Repeated line crossing and finish callback cannot duplicate finish order");
        } finally {UnityEngine.Object.DestroyImmediate(root);}
        // --gate-first (study 125). Progress projects onto 1.5 m route segments, so on the inside of a
        // tight corner one 28 m/s physics tick (0.23 m) can move it >0.5 m. At Hillcrest T5 that lands
        // past gate 6 in a single tick; recovery then read a missed gate before TrackGates credited it.
        Check(!RoadCar.GateFirst,"Gate-first ordering is off unless --gate-first");
        var raceway=new Circuit(HillcrestRoute.Circuit);float gate6=raceway.length/12*6,worstLanding=0;
        for(float lateral=-6;lateral<=-3;lateral+=.5f) {
            float before=raceway.Nearest(raceway.At(gate6-8)+raceway.Right(gate6-8)*lateral+Vector3.up*.5f,out _);
            for(float s=gate6-8+.233f;s<gate6+8;s+=.233f) {
                float after=raceway.Nearest(raceway.At(s)+raceway.Right(s)*lateral+Vector3.up*.5f,out _);
                if(before<=gate6&&after>gate6)worstLanding=Mathf.Max(worstLanding,after-gate6);
                before=after;
            }
        }
        Check(worstLanding>.5f,"Raceway T5 inside line can first appear >0.5 m past gate 6 in one tick ("+worstLanding.ToString("0.00")+" m): gates must be credited before recovery reads them");
        return checks;
    }
}
