using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Prop Forge, step 1 (docs/77): the kit contract read back (car-forge forge/world/kit.py -> Resources/Props), its axes and
// winding, one ground provider, and the run test range's roads. Off by default (--prop-forge, --run-range).
public static class PropForgeChecks
{
    sealed class Slope:Ground.IProvider{public float Height(float x,float z)=>.3f*x+2;}

    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(!PropForge.Enabled&&!RunRange.Requested,"Prop Forge is off without --prop-forge or --run-range");

        // the contract: the probe kit (one post, one rail) as the sync wrote it
        PropKit.Forget();var kit=PropKit.Load("probe");
        Check(kit.file.schema==PropKit.Schema&&kit.file.kit=="probe"&&kit.file.version>=1&&kit.file.pieces.Length==2,"Prop Forge: the probe kit reads (schema prop-forge-kit/0, two pieces)");
        var post=kit.Mesh("post");var rail=kit.Mesh("rail");
        Check(post.triangles.Length/3==kit.Piece("post").lods[0].triangles&&kit.Piece("post").lods.Length==3,"Prop Forge: a piece's mesh has the contract's triangles, and three LODs");
        Check(Mathf.Abs(post.bounds.size.y-1.1f)<.06f&&Mathf.Abs(post.bounds.min.y)<.03f&&Mathf.Abs(post.bounds.center.x)<.01f&&Mathf.Abs(post.bounds.center.z)<.01f,
            $"Prop Forge: axes: the post stands 1.1 m up y from its base at the origin ({post.bounds.min.y:0.00} to {post.bounds.max.y:0.00})");
        Check(Mathf.Abs(rail.bounds.size.z-2.4f)<.06f&&Mathf.Abs(rail.bounds.min.z)<.03f,$"Prop Forge: axes: a run piece runs 2.4 m along +z from 0 ({rail.bounds.min.z:0.00} to {rail.bounds.max.z:0.00})");
        var copy=UnityEngine.Object.Instantiate(post);var given=post.normals;copy.RecalculateNormals();var unity=copy.normals;
        int agree=0;for(int i=0;i<given.Length;i++)if(Vector3.Dot(given[i],unity[i])>0)agree++;UnityEngine.Object.DestroyImmediate(copy);
        Check(agree>=given.Length*.95f,$"Prop Forge: winding: the faces Unity draws face the way the kit's normals do ({agree}/{given.Length})");
        int outward=0;var pv=post.vertices;for(int i=0;i<pv.Length;i++){var radial=pv[i]-new Vector3(0,Mathf.Clamp(pv[i].y,.06f,1.04f),0);if(radial.sqrMagnitude<1e-6f||Vector3.Dot(radial,given[i])>0)outward++;}
        Check(outward>=pv.Length*.95f,$"Prop Forge: the kit's normals face out of the post ({outward}/{pv.Length})");
        Check(kit.MaterialsOf("post").Length==post.subMeshCount&&kit.file.materials.All(m=>PropKit.MaterialFor(m)!=null),"Prop Forge: every material class has a look, one per submesh");
        bool unknown=false;try{PropKit.MaterialFor("chocolate");}catch(InvalidOperationException){unknown=true;}
        Check(unknown,"Prop Forge: an unknown material class fails loudly");
        bool missing=false;try{PropKit.Load("no_such_kit");}catch(InvalidOperationException){missing=true;}
        Check(missing,"Prop Forge: a kit that isn't installed fails loudly");

        // one ground
        Ground.Use(new Slope());
        try {
            Check(Mathf.Abs(Ground.Surface(10,5)-5)<1e-4f,"Ground: the provider in use answers Surface");
            var n=Ground.Normal(0,0);Check(Mathf.Abs(Vector3.Angle(n,new Vector3(-.3f,1,0).normalized))<.5f,"Ground: Normal follows the provider's slope");
        } finally { Ground.Reset(); }
        Check(!Ground.Custom,"Ground: Reset returns to the world's own fields");

        // the run test range
        RunRange.BuildRoads();
        Check(new[]{"straight","spur","drive","crest-hollow","bank-road","bends"}.All(n=>RunRange.Roads.Any(r=>r.name==n)),"Run range: every fixture road is laid");
        var bends=RunRange.Road("bends").line;float minR=float.MaxValue;
        for(int i=1;i+1<bends.Length;i++){Vector2 a=new(bends[i-1].x,bends[i-1].z),b=new(bends[i].x,bends[i].z),c=new(bends[i+1].x,bends[i+1].z);
            float ab=(b-a).magnitude,bc=(c-b).magnitude,ca=(a-c).magnitude,area=Mathf.Abs((b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x))/2;if(area>1e-4f)minR=Mathf.Min(minR,ab*bc*ca/(4*area));}
        Check(minR>7.5f&&minR<8.5f,$"Run range: the tightest bend is 8 m ({minR:0.0} m)");
        Check(Mathf.Abs((RunRange.Land(61,0)-RunRange.Land(49,0))/12-.3f)<.01f,"Run range: the bank road runs along a 30% cross-slope");
        Check(RunRange.Land(-90,60)>2.9f&&RunRange.Land(-90,0)<-2.4f,"Run range: a 3 m crest and a 2.5 m hollow on the crest-hollow road");
        float y=RunRange.Road("crest-hollow").line[30].y;var p=RunRange.Road("crest-hollow").line[30];
        Check(Mathf.Abs(RunRange.Surface(p.x+2,p.z)-y)<.05f,"Run range: the ground is the road across its width (roads first)");
        return checks;
    }
}
