using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class ContextDriverChecks
{
    public static List<string> Run()
    {
        var results=new List<string>();
        void Check(bool ok,string label){if(!ok)throw new Exception("Context driver: "+label);results.Add(label);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        var reference=new ReferenceDriverPolicy(profile);var context=new ContextDriverPolicy(profile);
        float d=course.regionStarts[13]+190;var forward=road.Forward(d);
        var self=new ObservedCar(0,line.At(d),forward*18,forward,road.Right(d),d,line.Offset(d),18);
        var rear=new ObservedCar(1,line.At(d-40),forward*12,forward,road.Right(d-40),d-40,line.Offset(d-40),12);
        var observed=new[]{self,rear};
        var o=new DriverObservation(1,1f/120,self,observed,road,CarTuning.Club,1,0,false);
        var expected=reference.Decide(in o);var actual=context.Decide(in o);
        Check(expected.pedal==actual.pedal&&expected.steering==actual.steering&&expected.targetSpeed==actual.targetSpeed,
            "Safely trailing traffic preserves the clear-road reference command exactly");
        // A quadratic preference with its maximum between samples has a known
        // continuous optimum. Selecting only the central slot would fail this.
        Check(Mathf.Abs(ContextDriverPolicy.PeakOffset(-1.5625f,-.0625f,-.5625f,1)-.25f)<.0001f,
            "Context interpolation returns a continuous optimum between track-width samples");
        // Actual boxes have a 5 cm gap, but the desired traffic clearance
        // margins overlap. This is a compressed three-abreast situation, not
        // a wall and not permission to force a return to the reference line.
        var left=new ObservedCar(1,self.position-road.Right(d)*2.25f,forward*18,forward,road.Right(d),d,self.offset-2.25f,18,.8f,1.5f);
        var right=new ObservedCar(2,self.position+road.Right(d)*2.25f,forward*18,forward,road.Right(d),d,self.offset+2.25f,18,.8f,1.5f);
        var squeezed=new DriverObservation(2,1f/120,self,new[]{self,left,right},road,CarTuning.Club,1,0,false);
        var constrained=new ContextDriverPolicy(profile);constrained.Decide(in squeezed);
        Check(constrained.ReachableLeft>constrained.ReachableRight&&!constrained.CheckedReference,
            "Adjacent-car clearance exhaustion cannot trigger the scenery reference fallback");
        foreach(int side in new[]{-1,1}) {
            var offLine=new ObservedCar(0,road.At(d)+road.Right(d)*side*4,forward*18,forward,road.Right(d),d,side*4,18);
            var fastRear=new ObservedCar(1,line.At(d-12),forward*26,forward,road.Right(d-12),d-12,line.Offset(d-12),26);
            var merge=new DriverObservation(1,1f/120,offLine,new[]{offLine,fastRear},road,CarTuning.Street,1,0,false);
            var merging=new ContextDriverPolicy(new RacingSpeedProfile(line,CarTuning.Street));
            var held=merging.Decide(in merge);
            Check(merging.RearApproaches==1&&side*merging.SelectedOffset>=3.2f&&held.pedal>0,
                "Mirrored closing rear traffic preserves its passing corridor without braking the front car: "+side);
        }
        foreach(var state in new[]{(offset:0f,speed:26f,gap:12f),(offset:4f,speed:12f,gap:12f),(offset:4f,speed:18f,gap:12f),(offset:4f,speed:26f,gap:60f)}) {
            var harmless=new ObservedCar(1,road.At(d-state.gap)+road.Right(d-state.gap)*state.offset,forward*state.speed,forward,road.Right(d-state.gap),d-state.gap,state.offset,state.speed);
            var control=new DriverObservation(1,1f/120,self,new[]{self,harmless},road,CarTuning.Club,1,0,false);
            var normal=new ContextDriverPolicy(profile);var observedCommand=normal.Decide(in control);var clearCommand=reference.Decide(in control);
            Check(normal.RearApproaches==0&&observedCommand.pedal==clearCommand.pedal&&observedCommand.steering==clearCommand.steering,
                "Same-corridor, receding, parallel or distant rear traffic leaves clear driving unchanged: "+state);
        }
        var ahead=new ObservedCar(1,self.position+forward*5.5f,forward*18,forward,road.Right(d),d+5.5f,self.offset,18);
        var aheadObservation=new DriverObservation(1,1f/120,self,new[]{self,ahead},road,CarTuning.Club,1,0,false);
        Check(!RoadPathFeasibility.LongitudinallyAdjacent(in aheadObservation,in ahead),"Same-corridor next grid row does not reserve lateral space");
        var beside=new ObservedCar(1,self.position+forward*.6f+road.Right(d)*3.5f,forward*18,forward,road.Right(d),d+.6f,self.offset+3.5f,18);
        Check(RoadPathFeasibility.LongitudinallyAdjacent(in aheadObservation,in beside),"Staggered physically adjacent car retains its side reservation");
        var shifted=new ObservedCar(1,ahead.position,ahead.velocity,ahead.forward,ahead.right,ahead.route,ahead.offset,ahead.speed,colliderCentre:new Vector3(0,0,-1));
        Check(RoadPathFeasibility.LongitudinallyAdjacent(in aheadObservation,in shifted),"Offset collider centre participates in longitudinal adjacency");
        return results;
    }
}
