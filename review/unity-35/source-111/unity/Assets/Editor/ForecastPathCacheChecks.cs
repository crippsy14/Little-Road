using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class ForecastPathCacheChecks
{
    public static List<string> Run()
    {
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var cache=new ForecastPathCache(new RacingSpeedProfile(line,CarTuning.Club));
        var eager=new EagerForecastPathOracle(new RacingSpeedProfile(line,CarTuning.Club));
        int samples=0;float maximum=0;bool exactSamples=true;
        // Non-grid queries across the whole circuit and loop seam, while
        // repeatedly rebuilding the same cache for different proposed paths.
        for(int section=0;section<25;section++)foreach(float side in new[]{-5.5f,0,5.5f})foreach(bool rejoin in new[]{false,true})foreach(bool guide in new[]{false,true})foreach(float consumed in new[]{0,19.7f,65.125f}) {
            var path=new ContextPathState{anchor=section==24?road.length-.2f:road.length*section/24,
                consumed=consumed,startOffset=side,startSlope=side>0?-.3f:.3f,endOffset=-side,blend=42,rejoin=rejoin};
            path.guide=guide;if(guide){path.rejoin=false;path.guideOffset=line.Offset(path.anchor);path.guideSlope=(line.Offset(path.anchor+1)-line.Offset(path.anchor-1))*.5f;}
            cache.Prepare(road,in path);
            eager.Prepare(road,in path);
            if(cache.PreparedSamples!=0)throw new Exception("Forecast eagerly prepared unused geometry.");
            foreach(float progress in new[]{.137f,11.37f,60.61f})for(int i=0;i<41;i++) {
                float ahead=i*3,along=consumed+progress+ahead,d=path.anchor+along;
                if(!cache.TryPoint(along,ahead,out var point))throw new Exception("Prepared geometry unexpectedly missed its declared window.");
                // Frozen101 implementation, including its intermediate storage,
                // serves as the oracle rather than a regrouped math expression.
                if(!eager.TryPoint(along,ahead,out var expected))throw new Exception("Eager oracle unexpectedly missed.");
                exactSamples&=point.Equals(expected);
                float offset=ContextPathFollower.Smooth(path.startOffset,path.startSlope,path.endOffset,along,path.blend);
                if(guide)offset=line.Offset(d)+ContextPathFollower.Smooth(path.startOffset-path.guideOffset,path.startSlope-path.guideSlope,0,along,path.blend);
                if(path.rejoin)offset=Mathf.Lerp(offset,line.Offset(d),Mathf.SmoothStep(0,1,ahead/65));
                var exact=road.At(d)+road.Right(d)*offset;
                maximum=Mathf.Max(maximum,Vector3.Distance(point,exact));samples++;
            }
            if(cache.TryPoint(consumed-.01f,0,out _)||cache.TryPoint(consumed+201,0,out _))throw new Exception("Prepared geometry extrapolates instead of requesting exact fallback.");
            if(cache.PreparedSamples>=401)throw new Exception("Forecast generated the unused end of its geometry table.");
        }
        if(maximum>.02f)throw new Exception("Prepared path geometry exceeds declared 2cm point-error check: "+maximum);
        if(!exactSamples)throw new Exception("Demand-generated geometry differs from the original eager table.");
        // Point agreement is not a curvature/control or whole-body guarantee.
        // Physical prediction and live selection still need separate evidence.
        return new List<string>{"Prepared geometry point error <=2cm over "+samples+" full-circuit/guide/rejoin/seam queries (maximum "+maximum+"m)",
            "Rebuilt prepared geometry requests exact fallback before/after its window",
            "Demand-generated geometry exactly matches eager interpolation across changed paths, guide/rejoin and the loop seam while skipping unused samples"};
    }
}
