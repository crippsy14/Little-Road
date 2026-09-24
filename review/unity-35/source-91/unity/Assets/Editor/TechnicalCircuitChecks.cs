using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TechnicalCircuitChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string text){if(!pass)throw new Exception("Technical circuit check: "+text);checks.Add(text);}
        var course=new TechnicalCircuit();var track=course.track;
        Check(track.length>1600&&track.length<2100,"Cedar authored length 1.6–2.1 km: "+track.length);
        Check(!track.hasElevation,"First AI technical circuit is level, separating line tests from grade tests");
        float worstProjection=0,worstOffset=0,maxCurvature=0,minSeparation=float.MaxValue;int positive=0,negative=0;
        for(float d=0;d<track.length;d+=4) {
            foreach(float side in new[]{-6f,0,6f}) {
                float near=track.Nearest(track.At(d)+track.Right(d)*side,out float offset);
                worstProjection=Mathf.Max(worstProjection,Mathf.Abs(Mathf.DeltaAngle(d/track.length*360,near/track.length*360))/360*track.length);
                worstOffset=Mathf.Max(worstOffset,Mathf.Abs(offset-side));
            }
            float curve=Vector3.SignedAngle(track.Forward(d-4),track.Forward(d+4),Vector3.up)*Mathf.Deg2Rad/8;
            maxCurvature=Mathf.Max(maxCurvature,Mathf.Abs(curve));if(curve>.008f)positive++;if(curve<-.008f)negative++;
            // Exclude the local 40 m neighbourhood. A >28 m separation gives
            // the two 24 m support strips clearance, including sampling allowance.
            for(float other=d+40;other<track.length;other+=4)if(track.length-(other-d)>=40)
                minSeparation=Mathf.Min(minSeparation,Vector3.Distance(track.At(d),track.At(other)));
        }
        Check(worstProjection<.5f&&worstOffset<.06f,"Cedar projection stable across racing width: "+worstProjection+"m / "+worstOffset+"m");
        Check(minSeparation>28,"Nonlocal road supports have clearance: "+minSeparation+"m");
        Check(maxCurvature<1f/16&&positive>10&&negative>10,"Both turn directions with minimum sampled radius "+1/maxCurvature+"m");
        // Test every ribbon cell for an inside-edge fold, not just centreline separation.
        bool unfolded=true;
        for(int i=0;i<track.points.Length;i++) {
            float a=track.distance[i],b=track.distance[i+1];var direction=track.Forward((a+b)*.5f);
            foreach(int side in new[]{-1,1})unfolded&=Vector3.Dot(track.At(b)+track.Right(b)*side*12-track.At(a)-track.Right(a)*side*12,direction)>0;
        }
        Check(unfolded,"Cedar 24 m support has no reversed/folded inside edge");
        float back=course.regionStarts[14]-course.regionStarts[13];
        Check(back>380&&back<390,"Cedar genuine back straight: "+back+"m");
        return checks;
    }
}
