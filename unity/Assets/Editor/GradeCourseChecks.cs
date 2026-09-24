using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class GradeCourseChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool pass,string label){if(!pass)throw new Exception("Grade course check failed: "+label);checks.Add(label);}
        Check(Mathf.Abs(PerformanceCourse.HillHeight(400)-35.2f)<.001f&&Mathf.Abs(PerformanceCourse.HillHeight(500)-35.2f)<.001f,"Surveyed 35.2 m summit and 100 m plateau");
        Check(PerformanceCourse.HillHeight(120)==0&&Mathf.Abs(PerformanceCourse.HillHeight(780))<.001f,"Hill returns to original ground datum");
        var tuning=CarTuning.Club;
        DriveModel.Forces(tuning,tuning.gearCeiling+4,1,0,tuning.roadGrip,0,out float overspeed,out _);
        Check(Mathf.Abs(overspeed+DriveModel.Resistance(tuning.gearCeiling+4,tuning.mass))<.0001f,"Engine cuts propulsion above gear ceiling while drag remains");
        var hill=new PerformanceCourse("hill");var combined=new PerformanceCourse("combined");
        Check(hill.track.hasElevation&&combined.track.hasElevation&&!new Circuit().hasElevation,"Elevation support is route-specific; flat Bayview remains flat");
        float maxCurve=0,maxGrade=0,maxNormalStep=0;Vector3 previous=hill.track.Normal(hill.start);
        for(float d=hill.start;d<hill.finish;d+=2) {
            maxCurve=Mathf.Max(maxCurve,hill.track.Curvature(d));maxGrade=Mathf.Max(maxGrade,Mathf.Abs(hill.track.Forward(d).y));
            var normal=hill.track.Normal(d);maxNormalStep=Mathf.Max(maxNormalStep,Vector3.Angle(previous,normal));previous=normal;
        }
        Check(maxCurve<.00001f,"Vertical hill transitions do not become steering corners");
        Check(Mathf.Abs(maxGrade-.16f/Mathf.Sqrt(1+.16f*.16f))<.001f,"Surveyed maximum grade is 16 percent");
        Check(maxNormalStep<.6f,"Hill transitions have no abrupt surface-normal seam");
        Check(combined.gates.Length==9&&combined.turnStart>1200&&combined.finish>combined.turnEnd,"Combined course retains ordered climb/descent/braking/turn/exit gates");
        foreach(string name in new[]{"coast-flat","coast-up","coast-down"}) {
            var course=new PerformanceCourse(name);float grade=course.track.Forward(course.start).y;
            Check(Mathf.Abs(course.finish-course.start-20)<.001f,"Matched 20 m coasting distance: "+name);
            Check(name=="coast-flat"?Mathf.Abs(grade)<.001f:name=="coast-up"?grade>.15f:grade<-.15f,"Correct coasting slope sign: "+name);
        }
        return checks;
    }
}
