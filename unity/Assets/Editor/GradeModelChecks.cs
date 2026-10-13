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
            // (Dips add grip, so a few points in a braking zone can be faster; the zone overall is slower.
            // The geared engine envelope, docs/47, arrives faster and moves the zones: net slower, not 2:1.)
            Check(downhillSlower>1&&downhillFaster<downhillSlower*.95f,$"Downhill braking zones plan lower speeds overall ({downhillSlower:0.0} m/s lower, {downhillFaster:0.0} higher at dips)");
            Check(graded.predictedLapTime>flat.predictedLapTime,"Hills cost lap time in the plan");
            Check(GradeModel.Load(20,-.005f)<1&&GradeModel.Load(20,.005f)>1,"Crest unloads, dip loads the tyres");
        } finally {GradeModel.Enabled=saved;GradeModel.Physics=savedPhysics;}
        return checks;
    }
}
