using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RacingLineChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Racing line check: "+label);checks.Add(label);}
        var course=new TechnicalCircuit();
        foreach(string name in new[]{"centre-v1","mincurve-v1","half-v1","early-v1","late-v1","smooth-v2"}) {
            var line=new RacingLine(course.track,"cedar-"+name,course.definition.id,course.definition.version);float maximumFootprint=0,maxError=0;
            for(int i=0;i<line.Count;i++) {
                var forward=(line.points[(i+1)%line.Count]-line.points[(i+line.Count-1)%line.Count]).normalized;var right=Vector3.Cross(Vector3.up,forward);
                for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2){course.track.Nearest(line.points[i]+right*side*1.25f+forward*end*1.9f,out float offset);maximumFootprint=Mathf.Max(maximumFootprint,Mathf.Abs(offset));}
                float projected=line.Project(line.points[i],i*line.roadStep,out float error);maxError=Mathf.Max(maxError,Mathf.Abs(error));
            }
            Check(maximumFootprint<7.1f&&maxError<.001f,"Candidate "+name+" preserves conservative body clearance and projects onto itself: "+maximumFootprint+"m");
            foreach(var tuning in new[]{CarTuning.Stock,CarTuning.Club}) {
                var profile=new RacingSpeedProfile(line,tuning);bool valid=profile.iterations<=64&&profile.predictedLapTime>35&&profile.predictedLapTime<180;
                for(int i=0;i<line.Count;i++)valid&=!float.IsNaN(profile.speed[i])&&profile.speed[i]>1&&profile.speed[i]<=tuning.AttainableSpeed+.001f&&profile.speed[i]*profile.speed[i]*Mathf.Abs(line.curvature[i])<=tuning.roadGrip*1.65f*.781f;
                Check(valid,"Converged capability-bounded speed profile: "+name+" / "+tuning.power+" / "+profile.predictedLapTime+"s");
            }
        }
        Check(Mathf.Abs(TechnicalCircuitReview.WithinFraction(-2,2)-.5f)<.00001f&&TechnicalCircuitReview.WithinFraction(0,.5f)==1&&TechnicalCircuitReview.WithinFraction(2,3)==0,
            "Time adherence interpolates tolerance crossings rather than counting rendered frames");
        return checks;
    }
}
