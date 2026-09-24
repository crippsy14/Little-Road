using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    public sealed class CoastalGroundCover
    {
        readonly CoastalLandscape land;readonly Circuit track;readonly Transform parent;
        readonly System.Random random=new(46139);
        readonly List<Vector3> vertices=new();readonly List<int>[] triangles={new(),new(),new()};
        int clumps;
        public CoastalGroundCover(CoastalLandscape surface,Circuit road,Transform root){land=surface;track=road;parent=root;}
        float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        public void Build()
        {
            for(float d=3;d<track.length;d+=2.5f)foreach(int side in new[]{-1,1})for(int i=0;i<7;i++) {
                float along=d+Range(-1.8f,1.8f),offset=Range(13.8f,21)*side;
                Add(track.At(along)+track.Right(along)*offset,Range(.7f,1.2f));
            }
            for(int i=0;i<2600;i++) {
                var p=new Vector3(Range(-145,145),0,Range(-115,115));float d=track.Nearest(p,out _);
                if(Vector3.Distance(p,track.At(d))>48)continue;Add(p,Range(.7f,1.5f));
            }
            var mesh=new Mesh{name="Coastal grass and dry herb clumps",indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("Low coastal ground cover",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{
                TrialWorld.Material("Living grass blades",new Color(.25f,.34f,.14f),0,.1f),
                TrialWorld.Material("Olive grass blades",new Color(.35f,.40f,.21f),0,.12f),
                TrialWorld.Material("Dry herb stems",new Color(.49f,.45f,.28f),0,.1f)};
            Debug.Log("LITTLE_ROAD_GROUND_COVER clumps="+clumps+" triangles="+(triangles[0].Count+triangles[1].Count+triangles[2].Count)/3);
        }
        void Add(Vector3 p,float scale)
        {
            if(!land.SolidClearance(p)||Mathf.PerlinNoise(p.x*.13f+40,p.z*.13f+90)<.37f)return;
            for(int i=0;i<5;i++) {
                float d=track.length*(.12f+i*.17f);Vector3 find=track.At(d)+track.Right(d)*(i%2==0?13:-13);
                if(new Vector2(p.x-find.x,p.z-find.z).sqrMagnitude<4)return;
            }
            if(!land.SurfacePoint(p,out var hit)||hit.normal.y<.64f||hit.point.y<-.2f)return;
            p=hit.point-Vector3.up*.015f;clumps++;
            int blades=random.Next(7,13);
            for(int blade=0;blade<blades;blade++) {
                float angle=Range(0,Mathf.PI*2),height=Range(.22f,.60f)*scale,width=Range(.025f,.055f)*scale;
                Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),side=Vector3.Cross(Vector3.up,direction);
                Vector3 centre=p+direction*Range(0,.22f)*scale,bend=direction*Range(.08f,.25f)*scale;
                int tone=random.Next(3);
                for(int face=0;face<2;face++) {
                    int first=vertices.Count;
                    for(int ring=0;ring<4;ring++) {
                        float t=ring/3f;Vector3 point=centre+Vector3.up*height*t+bend*t*t;
                        float halfWidth=width*(1-t)*.5f+.0001f;
                        vertices.Add(point-side*halfWidth);vertices.Add(point+side*halfWidth);
                    }
                    for(int ring=0;ring<3;ring++) {
                        int a=first+ring*2,b=a+1,c=a+2,d=a+3;
                        if(face==0){Triangle(a,c,b,tone);Triangle(b,c,d,tone);}else{Triangle(a,b,c,tone);Triangle(b,d,c,tone);}
                    }
                }
            }
        }
        void Triangle(int a,int b,int c,int tone){triangles[tone].Add(a);triangles[tone].Add(b);triangles[tone].Add(c);}
    }
}
