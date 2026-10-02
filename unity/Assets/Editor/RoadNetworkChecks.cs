using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Road network geometry (docs/52): a right-angle T, a skewed T and a junction with an external road.
public static class RoadNetworkChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Road network check: "+label);checks.Add(label);}
        Vector3[] Line(Vector3 a,Vector3 b){var l=new Vector3[41];for(int i=0;i<=40;i++)l[i]=Vector3.Lerp(a,b,i/40f);return l;}
        // A right-angle T: an 8 m through road east-west, a 7 m side road north.
        {
            var net=new RoadNetwork{Fillet=6};var n=net.Add(Vector3.zero);
            var through=net.Through("Through",Line(new Vector3(-80,0,0),new Vector3(80,0,0)),8,.02f,n);
            var side=net.Connect("Side",Line(Vector3.zero,new Vector3(0,0,80)),7,.02f,n,null,minorStart:true);
            net.Solve();
            Check(through.Count==2&&n.arms.Count==3,"T junction: the through road splits into two arms at the node");
            // Each through arm is trimmed to where its corner with the side road begins: side half + fillet = 3.5 + 6.
            Check(through.All(r=>Mathf.Abs((r.start??r.end).trim-9.5f)<.05f),$"T junction: through road trimmed to 9.5 m ({string.Join(", ",through.Select(r=>(r.start??r.end).trim.ToString("0.00")))})");
            Check(Mathf.Abs(side.start.trim-10f)<.05f,$"T junction: side road trimmed to through half + fillet, 10 m ({side.start.trim:0.00})");
            // Corner arcs end tangent to both kerbs: every arc point 6 m from its centre, ends on the kerb lines.
            var corners=n.kerbs.Where(k=>k.Count>3).ToList();
            Check(corners.Count==2,$"T junction: two rounded corners, the far side a straight kerb (kerb points {string.Join(",",n.kerbs.Select(k=>k.Count))}; straight {string.Join(",",n.solved.Select(c=>c.straight))}; mouths {string.Join(" ",n.arms.Select(a=>a.hasMouth+":"+a.endDir.ToString("0.0")+a.MouthRight.ToString("0.0")))}; arms {string.Join(",",n.arms.Select(a=>a.dir.ToString("0.0")))})");
            foreach(var k in corners) {
                var ends=new[]{k[0],k[^1]};var centre=new Vector3(Mathf.Sign(k.Average(p=>p.x))*9.5f,0,10);
                bool onKerbs=ends.Any(p=>Mathf.Abs(Mathf.Abs(p.z)-4)<.05f)&&ends.Any(p=>Mathf.Abs(Mathf.Abs(p.x)-3.5f)<.05f);
                bool round=k.Skip(1).Take(k.Count-2).All(p=>Mathf.Abs(new Vector2(p.x-centre.x,p.z-centre.z).magnitude-6)<.05f);
                Check(onKerbs&&round,"T junction: each corner runs from the through kerb to the side kerb on a 6 m radius");
            }
            float minY=n.polygon.Min(p=>p.y),maxY=n.polygon.Max(p=>p.y);
            Check(maxY-minY<.01f&&Mathf.Abs(maxY-.02f)<.01f,"T junction: the surface sits at the roads' height (no step)");
            Check(Mathf.Abs(RoadNetwork.Length(side.trimmed)-70)<.1f,"T junction: the side road's ribbon starts at the mouth");
        }
        // A skewed T (60 degrees): corners still tangent, the acute side trimmed further.
        {
            var net=new RoadNetwork{Fillet=6};var n=net.Add(Vector3.zero);
            net.Through("Through",Line(new Vector3(-80,0,0),new Vector3(80,0,0)),8,0,n);
            var dir=new Vector3(Mathf.Cos(60*Mathf.Deg2Rad),0,Mathf.Sin(60*Mathf.Deg2Rad));
            var side=net.Connect("Side",Line(Vector3.zero,dir*80),7,0,n,null,minorStart:true);
            net.Solve();var trims=n.arms.Where(a=>a.road!=side).Select(a=>a.trim).OrderBy(x=>x).ToArray();
            Check(trims[1]>trims[0]+2&&side.start.trim>9,$"Skewed T: the acute corner trims further ({trims[0]:0.0} m and {trims[1]:0.0} m; side {side.start.trim:0.0} m)");
        }
        // An external road (the Bayview loop): untouched, the side road starts at its kerb, corner pieces beside the mouth.
        {
            var net=new RoadNetwork{Fillet=6};var n=net.Add(Vector3.zero);
            net.External(n,Vector3.right,8,0);net.External(n,Vector3.left,8,0);
            var side=net.Connect("Side",Line(Vector3.zero,new Vector3(0,0,80)),8.5f,0,n,null,minorStart:true);
            net.Solve();
            Check(Mathf.Abs(side.start.trim-7.6f)<.05f&&n.corners.Count==2,$"External road: the side road starts at its kerb, tucked 0.4 m under it (7.6 m; {side.start.trim:0.00}), with two corner pieces");
        }
        return checks;
    }
}
