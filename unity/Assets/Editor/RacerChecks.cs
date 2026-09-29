using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// Racing AI v2 (docs/46): ability mapping stays inside physical limits, offset paths use their own
// curvature, and the launch, following and alongside rules hold on Cedar.
public static class RacerChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Racer check: "+label);checks.Add(label);}
        var low=new RacerAbilities();var high=new RacerAbilities{braking=1,line=1,control=1,consistency=1,racecraft=1,strategy=1,aggression=1,patience=1};
        Check(low.CornerUse>=.78f&&high.CornerUse<=.96f&&low.BrakeUse>=.85f&&high.BrakeUse<.98f&&high.LateBrake<=.04f,
            "Abilities map to grip and brake use near, never beyond, the physical limit");
        Check(low.Reaction>high.Reaction&&high.Reaction>.1f&&low.Standstill>high.Standstill&&high.Standstill>.5f&&low.Clearance>high.Clearance&&high.Clearance>.2f,
            "Racecraft shortens reaction time and Aggression tightens gaps, within safe floors");
        var identity=DriverIdentity.ForRace(3,1709,"mixed",false);var from=RacerAbilities.From(identity);
        Check(from.braking>=0&&from.braking<=1&&from.aggression>=0&&from.aggression<=1&&from.line>0,"Existing driver records convert to v2 abilities");

        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var track=RacerTrack.For(line);
        int sharp=0;for(int i=0;i<line.Count;i++)if(Mathf.Abs(line.curvature[i])>Mathf.Abs(line.curvature[sharp]))sharp=i;
        float k=line.curvature[sharp],inside=Mathf.Sign(k);
        track.EnsureRoad(road);float at=sharp*track.Step,centre=track.RoadCurvature(at);
        Check(Mathf.Abs(track.PathCurvature(at,inside*5,0))>Mathf.Abs(centre)&&Mathf.Abs(track.PathCurvature(at,-inside*5,0))<Mathf.Abs(centre),"A path parallel to the road is tighter on the inside of a corner and wider outside");
        float error=0,peak=0;
        for(int i=0;i<line.Count;i++) {
            float s=i*track.Step,o=track.LineOffset(s),h=4*track.Step,o2=(track.LineOffset(s+h)-2*o+track.LineOffset(s-h))/(h*h);
            float e=Mathf.Abs(track.PathCurvature(s,o,o2)-line.curvature[i]);error+=e/line.Count;peak=Mathf.Max(peak,Mathf.Abs(line.curvature[i]));
        }
        Check(error<.1f*peak,"Path curvature measured against the road reproduces the racing line's own curvature (mean error "+error.ToString("0.0000")+", peak "+peak.ToString("0.000")+")");
        Check(Mathf.Abs(track.Relative(5,track.Length-5)-10)<.01f&&Mathf.Abs(track.Relative(track.Length-5,5)+10)<.01f,"Along-track distances wrap across the line");

        // A straight: 80 m where the racing line barely turns.
        float straight=-1;
        for(float s=0;s<road.length&&straight<0;s+=4){bool flat=true;for(float x=0;x<80;x+=2)flat&=Mathf.Abs(line.curvature[line.Index(s+x,out _)])<.0025f;if(flat)straight=s;}
        Check(straight>=0,"Cedar has a straight for the traffic checks");
        ObservedCar Car(int id,float s,float offset,float speed)
        {
            var f=road.Forward(s);var r=road.Right(s);
            return new ObservedCar(id,road.At(s)+r*offset,f*speed,f,r,Mathf.Repeat(s,road.length),offset,speed,.9f,2.1f);
        }
        var half=new Vector3(.9f,.7f,2.1f);
        DriveCommand Step(RacerDriverPolicy p,ObservedCar[] cars)=>p.Decide(new DriverObservation(0,1f/120,cars[0],cars,road,CarTuning.Club,1,0,false));

        var launch=new RacerDriverPolicy(line,RacerAbilities.Default,0,1709,half);
        var grid=new[]{Car(0,straight,0,0)};
        launch.Elapsed=-1;var waiting=Step(launch,grid);
        launch.Elapsed=1;DriveCommand go=default;for(int i=0;i<30;i++)go=Step(launch,grid);
        Check(waiting.pedal==0&&go.pedal>.5f,"Launch: no throttle before the lights, full power after the reaction delay");

        var follower=new RacerDriverPolicy(line,RacerAbilities.Default,0,1709,half);follower.Elapsed=20;
        var queue=new[]{Car(0,straight,0,24),Car(1,straight+11,0,12)};
        DriveCommand behind=default;for(int i=0;i<3;i++)behind=Step(follower,queue);
        Check((follower.Binding==RacerDriverPolicy.Reason.Follow||follower.Binding==RacerDriverPolicy.Reason.Guard)&&behind.pedal<0,
            "Closing fast on a slower car in our lane: traffic sets the speed and the car brakes");

        var cruiser=new RacerDriverPolicy(line,RacerAbilities.Default,0,1709,half);cruiser.Elapsed=20;
        var gap=new[]{Car(0,straight,0,24),Car(1,straight+70,0,24)};
        DriveCommand free=default;for(int i=0;i<90;i++)free=Step(cruiser,gap);
        Check(cruiser.Binding==RacerDriverPolicy.Reason.Limit&&free.pedal>.9f,"A car far ahead at the same speed does not make us lift (no pausing): "+cruiser.Binding+" pedal "+free.pedal.ToString("0.00"));

        var squeezed=new RacerDriverPolicy(line,RacerAbilities.Default,0,1709,half);squeezed.Elapsed=20;
        float lineHere=track.LineOffset(straight+20);
        var alongside=new[]{Car(0,straight,lineHere-3.2f,24),Car(1,straight+.5f,lineHere-.4f,24)};
        var side=Step(squeezed,alongside);
        Check(side.chosenLane<lineHere-.4f-1.8f+.3f,"A car alongside is a wall: we keep our side of it rather than cutting back to the line ("+side.chosenLane.ToString("0.00")+" m)");
        return checks;
    }
}
