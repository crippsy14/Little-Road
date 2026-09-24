using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// ai-racing-review goal 2: --corner-aware-pass (inside of the next corner, later
// braking for a committed car alongside on the inside).
public static class CornerAwarePassChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception("Corner-aware pass: "+message);checks.Add(message);}
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var profile=new RacingSpeedProfile(line,CarTuning.Club);
        ObservedCar Car(float route,float v){var f=road.Forward(route);return new ObservedCar(0,road.At(route),f*v,f,road.Right(route),route,0,v,1.2f,2.3f);}
        DriverObservation At(float route,float v){var self=Car(route,v);return new DriverObservation(1,1f/120,self,new[]{self},road,CarTuning.Club,1,0,false);}

        // Sign convention: +1 means the road ahead bends toward positive (right) offsets.
        int right=0,left=0,agree=0;
        for(float d=0;d<road.length;d+=25) {
            var o=At(d,20);int side=ContextDriverPolicy.NextCornerSide(in o);if(side==0)continue;
            if(side>0)right++;else left++;
            float sharpest=0,bend=0;
            for(float ahead=20;ahead<=150;ahead+=6){float a=d+ahead;float turn=Vector3.Cross(road.Forward(a),road.Forward(a+6)).y;if(Mathf.Abs(turn)>Mathf.Abs(sharpest)){sharpest=turn;bend=Vector3.Dot(road.Forward(a+6)-road.Forward(a),road.Right(a));}}
            if(bend*side>0)agree++;
        }
        Check(right>0&&left>0,"Cedar has corners of both directions ahead of sampled points ("+right+" right, "+left+" left)");
        Check(agree==right+left,"The inside side always matches the direction the road turns toward");

        // Later braking: the same approach with a higher braking fraction never
        // lowers the follower's target speed and raises it somewhere before a corner.
        var follower=new ContextPathFollower(profile,false);
        int raised=0;bool lowered=false;
        for(float d=0;d<road.length;d+=10) {
            var o=At(d,Mathf.Min(33,profile.Speed(d)+4));
            var path=RoadPathFeasibility.FromMotion(in o,0,false);path.localSpeed=true;
            follower.BrakeFractionOverride=0;float normal=follower.Follow(in o,in path).targetSpeed;
            follower.BrakeFractionOverride=.95f;float late=follower.Follow(in o,in path).targetSpeed;
            follower.BrakeFractionOverride=0;
            if(late<normal-.001f)lowered=true;if(late>normal+.05f)raised++;
        }
        Check(!lowered&&raised>0,"A higher braking fraction brakes later before corners and never earlier ("+raised+" approach points raised)");
        return checks;
    }
}
