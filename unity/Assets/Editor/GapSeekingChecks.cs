using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-racing-review goal 4: --gap-seeking adds candidates aimed at the lateral
// gaps between all cars ahead and the road edges, not only either side of the blocker.
public static class GapSeekingChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Gap seeking: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        float d=80;
        ObservedCar Car(int id,float route,float side,float v){var f=road.Forward(route);return new ObservedCar(id,road.At(route)+road.Right(route)*side,f*v,f,road.Right(route),route,side,v,1.2f,2.3f);}
        // Blocker straight ahead at +2 m; a second slow car further left at -5.5 m.
        var self=Car(0,d,2,25);var ahead=Car(1,d+15,2,18);var left=Car(2,d+12,-5.5f,18);
        var o=new DriverObservation(1,1f/120,self,new[]{self,ahead,left},road,CarTuning.Club,1,0,false);
        bool Has(DynamicRejoinPlanner p,float target){for(int i=0;i<p.Count;i++)if(Mathf.Abs(p.Path(i).endOffset-target)<.3f)return true;return false;}
        // Expected gaps (car half-widths 1.2 + clearance 0.35): between the cars (-2.75..-0.75)
        // and outside the blocker up to the road edge (4.75..6.6).
        float between=-1.75f,outside=(4.75f+Circuit.Width*.5f-1.2f-.2f)*.5f;
        var plain=new DynamicRejoinPlanner(profile,true,true,false);plain.PrepareTraffic(in o,default,false);
        Check(plain.TrafficPlan&&plain.Blocker==1&&!Has(plain,between)&&!Has(plain,outside),"Without gap seeking only the sides of the blocking car are offered");
        var seeking=new DynamicRejoinPlanner(profile,true,true,true);seeking.PrepareTraffic(in o,default,false);
        Check(seeking.Count>plain.Count&&seeking.Count<=DynamicRejoinPlanner.Capacity,"Gap seeking adds bounded extra candidates ("+plain.Count+" -> "+seeking.Count+")");
        Check(Has(seeking,between),"A candidate aims between the two cars ahead");
        Check(Has(seeking,outside),"A candidate aims outside the blocking car, using road up to 0.2 m from the edge");
        var clear=new DriverObservation(2,1f/120,self,new[]{self},road,CarTuning.Club,1,0,false);
        seeking.PrepareTraffic(in clear,default,false);
        Check(!seeking.TrafficPlan,"With no traffic the ordinary clear-road plan is unchanged");
        // --use-shoulder: a straight-line hold at +7.2 m overhangs the painted edge by ~0.4 m.
        var edgeCar=Car(0,d,7.2f,25);
        var edgeObs=new DriverObservation(3,1f/120,edgeCar,new[]{edgeCar},road,CarTuning.Club,1,0,false);
        var edgePath=RoadPathFeasibility.FromMotion(in edgeObs,7.2f,false);
        var prediction=new ContextPathPrediction(profile);var half=new Vector3(1.2f,.5f,2.3f);
        prediction.Predict(in edgeObs,edgePath,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true);
        bool inside=prediction.envelope.Contained;
        prediction.Predict(in edgeObs,edgePath,Vector3.zero,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:-RoadPathFeasibility.ShoulderOverhang,preparedPath:true);
        Check(!inside&&prediction.envelope.Contained,"A body overhanging the painted edge is rejected normally but allowed with the 1 m shoulder allowance");
        return checks;
    }
}
