using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Standing crop (the fields redo, bug inbox #20, docs/82). Wheat, barley, rapeseed and young crops are a canopy: one
    // surface at the crop's height over the whole field, uneven plant by plant, with the stalks showing round its edge, as a
    // real field reads from a lane. Drive into it and the car ploughs a swath that lies flat behind it (CropTrample and the
    // Crops shader); the shader draws the drill rows and the wind going over it close up. Ploughed land and lavender stay
    // as rows of ridges (HillcrestTown.Patchwork). Bramblewick's vale uses the same canopy.
    public static class CropField
    {
        // The canopy's height for the valley's crop palettes (0: rows instead): wheat, plough, young crop, lavender, rapeseed,
        // barley.
        public static float Height(int kind)=>kind switch{0=>.85f,2=>.35f,4=>1.05f,5=>.6f,_=>0};
        // Adds one field's canopy: the field `w` by `d` about `centre`, turned by `rot` (its drill rows run along its x),
        // `top` the crop's colour and `stalk` its sides'. `ground` gives the field's ground at a point, `tone` shades a colour
        // at a point. `lift` carries each vertex's height for the shader to press down (x) and how far up the crop it is (y);
        // `local` its place across the rows, for the drill lines.
        public static void Canopy(List<Vector3> v,List<Color> col,List<int> t,List<Vector2> lift,List<Vector2> local,Vector3 centre,Quaternion rot,float w,float d,Color top,Color stalk,float height,Func<Vector3,float> ground,Func<Color,Vector3,Color> tone)
        {
            while(local.Count<v.Count)local.Add(Vector2.zero);   // (the rows' vertices before it carry none)
            int nx=Mathf.CeilToInt(w/1.1f),nz=Mathf.CeilToInt(d/1.1f),start=v.Count;centre.y=0;
            var foot=new float[(nx+1)*(nz+1)];
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++) {
                float lx=-w/2+i*w/nx,lz=-d/2+j*d/nz;var p=centre+rot*new Vector3(lx,0,lz);
                bool edge=i==0||j==0||i==nx||j==nz;
                // (a little lower at the headland, uneven within: patches stand taller or thinner, as real crop does)
                float h=height*(edge?.84f:.86f+.28f*Mathf.PerlinNoise(p.x*.31f+centre.x*.01f,p.z*.31f));
                float y=ground(p);foot[j*(nx+1)+i]=y;
                v.Add(new Vector3(p.x,y+h,p.z));col.Add(tone(top,p)*(.95f+.1f*h/height));lift.Add(new Vector2(h,1));local.Add(new Vector2(lz,lx));
                if(i<nx&&j<nz){int q=start+j*(nx+1)+i;t.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}
            }
            // The stalks round its edge: a skirt from the ground to the canopy's edge, both sides drawn (it's seen from the
            // lane outside and from the swath inside once the car has broken in).
            var ring=new List<int>();
            for(int i=0;i<nx;i++)ring.Add(i);for(int j=0;j<nz;j++)ring.Add(j*(nx+1)+nx);
            for(int i=nx;i>0;i--)ring.Add(nz*(nx+1)+i);for(int j=nz;j>0;j--)ring.Add(j*(nx+1));
            int skirt=v.Count;
            foreach(int k in ring) {
                var up=v[start+k];var p=new Vector3(up.x,foot[k]-.06f,up.z);float h=lift[start+k].x;
                var c=tone(stalk,p);v.Add(p);v.Add(up);col.Add(c*.8f);col.Add(c);lift.Add(new Vector2(0,0));lift.Add(new Vector2(h,1));local.Add(local[start+k]);local.Add(local[start+k]);
            }
            for(int s=0;s<ring.Count;s++) {
                int q=skirt+s*2,n=skirt+(s+1)%ring.Count*2;
                t.AddRange(new[]{q,q+1,n,n,q+1,n+1});t.AddRange(new[]{q,n,q+1,n,n+1,q+1});
            }
        }
    }
}
