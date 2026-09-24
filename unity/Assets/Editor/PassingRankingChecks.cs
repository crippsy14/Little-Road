using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-pace-experiments step 4: --sustained-leader-ranking keeps a followed car
// in the candidate gate-time estimate for the whole horizon.
public static class PassingRankingChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Passing ranking: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);var eval=new ContextPathFollower(profile,false);
        // Club car at 25 m/s on the Cedar home straight, 3.5 m right of centre.
        float d=80,speed=25,offset=3.5f;
        ObservedCar Car(int id,float route,float side,float v){var f=road.Forward(route);return new ObservedCar(id,road.At(route)+road.Right(route)*side,f*v,f,road.Right(route),route,side,v,1.2f,2.3f);}
        var self=Car(0,d,offset,speed);
        DriverObservation With(int frame,params ObservedCar[] others)
        {
            var all=new ObservedCar[others.Length+1];all[0]=self;Array.Copy(others,0,all,1,others.Length);
            return new DriverObservation(frame,1f/120,self,all,road,CarTuning.Club,1,0,false);
        }
        ContextPathState Hold(in DriverObservation o){var p=RoadPathFeasibility.FromMotion(in o,offset,false);p.localSpeed=true;return p;}
        ContextPathState Pass(in DriverObservation o){var p=RoadPathFeasibility.FromMotion(in o,offset-2.7f,false);p.blend=Mathf.Clamp(p.blend*.8f,16,100);p.localSpeed=true;return p;}
        const float Horizon=200,Sustained=float.PositiveInfinity;

        var clear=With(1);var clearHold=Hold(in clear);
        Check(eval.EstimateGateTime(in clear,in clearHold,Horizon,true,3)==eval.EstimateGateTime(in clear,in clearHold,Horizon,true,Sustained),"With no traffic the traffic window length changes nothing");

        var fast=With(2,Car(1,d+14,offset,34));var fastHold=Hold(in fast);
        float free=eval.EstimateGateTime(in clear,in clearHold,Horizon,false);
        Check(Mathf.Abs(eval.EstimateGateTime(in fast,in fastHold,Horizon,true,Sustained)-free)<.001f,"A faster car ahead never slows the sustained estimate");

        var slow=With(3,Car(1,d+14,offset,25));var slowHold=Hold(in slow);var slowPass=Pass(in slow);
        float shortHold=eval.EstimateGateTime(in slow,in slowHold,Horizon,true,3);
        float longHold=eval.EstimateGateTime(in slow,in slowHold,Horizon,true,Sustained);
        Check(longHold>shortHold+.5f,"Following a slower car costs time for the whole horizon, not only 3 s");
        float longPass=eval.EstimateGateTime(in slow,in slowPass,Horizon,true,Sustained);
        Check(float.IsFinite(longPass)&&longPass<longHold,"With the sustained window, passing a slower car ranks ahead of holding behind it");
        return checks;
    }
}
