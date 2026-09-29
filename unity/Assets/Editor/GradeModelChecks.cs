using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// --grade-aware: level circuits plan exactly as before; Cedar Hills is the declared elevated
// circuit; gravity and load act in the right direction in the forecast and the speed profile.
public static class GradeModelChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();bool saved=GradeModel.Enabled,savedPhysics=GradeModel.Physics;GradeModel.Physics=true;
        void Check(bool value,string name){if(!value){GradeModel.Enabled=saved;GradeModel.Physics=savedPhysics;throw new Exception("Grade model check failed: "+name);}checks.Add(name);}
        try {
            // Level parity: the Bayview profile is bit-identical with the flag on.
            var bayview=new Circuit();var flatLine=new RacingLine(bayview,RacingLine.BakeSmooth(bayview,"grade-check","bayview"));
            GradeModel.Enabled=false;var off=new RacingSpeedProfile(flatLine,CarTuning.Club);
            GradeModel.Enabled=true;var on=new RacingSpeedProfile(flatLine,CarTuning.Club);
            bool same=off.predictedLapTime==on.predictedLapTime;for(int i=0;i<off.speed.Length&&same;i++)same=off.speed[i]==on.speed[i];
            Check(same,"Level circuit speed profile is identical with --grade-aware");
            Check(GradeModel.Slope(bayview,100,bayview.Forward(100))==0,"No slope force on a level circuit");
            // Cedar Hills: Cedar's layout, 20-25 m of climb, grades under 10%.
            var hills=new TechnicalCircuit("cedar-hills");var cedar=new TechnicalCircuit();
            float low=float.MaxValue,high=float.MinValue,steepest=0;
            for(float d=0;d<hills.track.length;d+=2){float y=hills.track.At(d).y;low=Mathf.Min(low,y);high=Mathf.Max(high,y);steepest=Mathf.Max(steepest,Mathf.Abs(GradeModel.Grade(hills.track,d)));}
            Check(hills.track.hasElevation&&!cedar.track.hasElevation,"Cedar Hills is elevated; Cedar stays level");
            Check(Mathf.Abs(hills.track.length-cedar.track.length)<cedar.track.length*.01f,"Cedar Hills keeps Cedar's length (within 1%)");
            Check(high-low>20&&high-low<26&&steepest<.1f,$"Cedar Hills climbs {high-low:0.0} m with grades under 10% (max {steepest*100:0.0}%)");
            // Profile on the hills: downhill braking zones are slower, the lap is slower overall.
            var line=new RacingLine(hills.track,RacingLine.Transfer(hills.track,"cedar-smooth-v2","cedar-hills"));
            GradeModel.Enabled=false;var flat=new RacingSpeedProfile(line,CarTuning.Club);
            GradeModel.Enabled=true;var graded=new RacingSpeedProfile(line,CarTuning.Club);
            float downhillSlower=0,downhillFaster=0;
            for(int i=0;i<line.Count;i++){var d=line.points[(i+1)%line.Count]-line.points[i];float g=d.y/Mathf.Max(.01f,new Vector2(d.x,d.z).magnitude);
                if(g<-.03f&&flat.acceleration[i]<-.5f){downhillSlower+=Mathf.Max(0,flat.speed[i]-graded.speed[i]);downhillFaster+=Mathf.Max(0,graded.speed[i]-flat.speed[i]);}}
            // (Dips add grip, so a few points in a braking zone can be faster; the zone overall is slower.)
            Check(downhillSlower>1&&downhillFaster<downhillSlower*.5f,$"Downhill braking zones plan lower speeds overall ({downhillSlower:0.0} m/s lower, {downhillFaster:0.0} higher at dips)");
            Check(graded.predictedLapTime>flat.predictedLapTime,"Hills cost lap time in the plan");
            // Forecast physics: uphill slows, downhill speeds a full-throttle car.
            PlanarDrivePrediction Run(float slope){var p=new PlanarDrivePrediction(Vector3.zero,Vector3.forward*15,Vector3.forward,0,0);for(int k=0;k<240;k++)p.Step(CarTuning.Club,1,0,1f/120,0,-1,slope);return p;}
            float up=Run(GradeModel.Gravity*.08f).velocity.z,level=Run(0).velocity.z,down=Run(-GradeModel.Gravity*.08f).velocity.z;
            Check(up<level&&level<down,"Forecast: uphill slower, downhill faster than level");
            Check(GradeModel.Load(20,-.005f)<1&&GradeModel.Load(20,.005f)>1,"Crest unloads, dip loads the tyres");
            // Steering on a slope: on Cedar Hills' steepest descent, the command depends only on the
            // horizontal geometry, not on how far the look-ahead point sits below the car.
            {
                float steep=0,at=0;for(float d=0;d<hills.track.length;d+=4){float g=GradeModel.Grade(hills.track,d);if(g<steep&&hills.track.Curvature(d)<.002f){steep=g;at=d;}}
                var profile=new RacingSpeedProfile(line,CarTuning.Club);var follower=new ContextPathFollower(profile);
                var fwd=hills.track.Forward(at);fwd.y=0;fwd.Normalize();var path=new ContextPathState{anchor=at,blend=60};
                float Steer(float height){var self=new ObservedCar(0,hills.track.At(at)+Vector3.right*.3f+Vector3.up*height,fwd*25,fwd,Vector3.Cross(Vector3.up,fwd),at,.3f,25,.9f,2,0,Vector3.zero);
                    var obs=new DriverObservation(0,1f/120,self,ReadOnlySpan<ObservedCar>.Empty,hills.track,CarTuning.Club,1,0,false,0,false,0);return follower.Follow(in obs,in path).steering;}
                float ride=Steer(.55f),raised=Steer(2.2f);
                Check(Mathf.Abs(ride-raised)<1e-4f,$"Downhill: steering ignores the look-ahead point's height ({ride:0.0000} vs {raised:0.0000} at {steep*100:0.0}% grade)");
            }
        } finally {GradeModel.Enabled=saved;GradeModel.Physics=savedPhysics;}
        return checks;
    }
}
