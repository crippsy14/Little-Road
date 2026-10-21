using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// Vinelands (docs/85): its two track designs as laid from their pieces. The Vineyard Loop closes on its start, on its
// start heading, at its designed length, with every leg 40 m or more from the others; the climb road (with the Rua da
// Torre) lands on the Largo, its solved straights sensible, its hairpin legs clear of each other; both stay in the district.
public static class VinelandsChecks
{
    static float Length(List<Vector2> p){float s=0;for(int i=1;i<p.Count;i++)s+=Vector2.Distance(p[i],p[i-1]);return s;}
    static float Clearance(List<Vector2> p,int gap,bool loop)
    {
        float best=float.MaxValue;
        for(int i=0;i<p.Count;i+=2)for(int j=i+gap;j<p.Count;j+=2){if(loop&&p.Count-j+i<gap)continue;best=Mathf.Min(best,Vector2.Distance(p[i],p[j]));}
        return best;
    }
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Vinelands check: "+label);checks.Add(label);}
        var loop=Vinelands.LoopDesign(out float l1,out float l2,out float headingError);float lap=Length(loop);
        Check(Vector2.Distance(loop[0],loop[^1])<1&&Mathf.Abs(headingError)<2,$"the Vineyard Loop closes on its start ({Vector2.Distance(loop[0],loop[^1]):0.0} m, {headingError:0.0} degrees)");
        Check(lap>1600&&lap<1760,$"the Vineyard Loop is {lap:0} m (designed 1683)");
        Check(l1>50&&l2>250&&l2>l1/.65f,$"its solved straights: the Adega Straight {l2:0} m, the Quinta Straight {l1:0} m (under 0.65 of it)");
        float lc=Clearance(loop,45,true);Check(lc>=40,$"its legs stand {lc:0} m apart or more");
        foreach(var q in loop)if(q.magnitude>Vinelands.Radius-300)throw new Exception("Vinelands check: the loop leaves the district at "+q);
        var climb=Vinelands.ClimbDesign(out float t1,out float t2);float road=Length(climb);
        Check(climb[^1].magnitude<1,$"the climb road lands on the Largo ({climb[^1].magnitude:0.0} m off)");
        Check(t1>100&&t2>100,$"the Rua da Torre's solved straights: {t1:0} m and {t2:0} m");
        Check(road>4300&&road<5000,$"the Subida from the Cais to the Largo is {road:0} m (the climb 3.68 km, the Rua da Torre the rest)");
        float cc=Clearance(climb,60,false);Check(cc>=45,$"the stack's legs stand {cc:0} m apart or more");
        foreach(var q in climb)if(q.magnitude>Vinelands.Radius-250)throw new Exception("Vinelands check: the climb leaves the district at "+q);
        return checks;
    }
}
