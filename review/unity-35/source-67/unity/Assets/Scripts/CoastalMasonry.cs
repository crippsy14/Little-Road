using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Original chamfered ashlar. Visible stones sit inside the established
    // continuous collision envelopes; recessed mortar closes their small joints.
    public sealed class CoastalMasonry
    {
        readonly Transform root=new GameObject("Coastal masonry / bounded mesh batches").transform;
        readonly Material[] materials=new Material[5];
        readonly List<Vector3> vertices=new();
        readonly List<Vector2> uv=new();
        readonly List<int>[] triangles={new(),new(),new(),new(),new()};
        int blocks,batches,totalVertices,totalTriangles;
        public CoastalMasonry()
        {
            var albedo=Resources.Load<Texture2D>("Surfaces/rock_08/rock_08_diff_2k");
            var normal=Resources.Load<Texture2D>("Surfaces/rock_08/rock_08_nor_gl_2k");
            if(albedo==null||normal==null)throw new InvalidOperationException("Missing credited masonry surface maps");
            for(int i=0;i<materials.Length;i++) {
                Color colour=i==4?new Color(1.52f,1.56f,1.57f):new Color(1.38f,1.46f,1.52f)*Mathf.Lerp(.95f,1.05f,i/3f);
                var material=TrialWorld.Material("Weathered ashlar tone "+i,colour,0,.13f);
                material.SetTexture("_BaseMap",albedo);material.SetTexture("_BumpMap",normal);
                material.SetFloat("_BumpScale",.18f);material.EnableKeyword("_NORMALMAP");materials[i]=material;
            }
        }
        public void Body(float length,float width,float bottom,float top,int seed,Func<Vector3,Vector3> map)
        {
            var rng=new System.Random(seed);
            float[] heights={bottom,bottom+(top-bottom)*.34f,bottom+(top-bottom)*.67f,top};
            for(int row=0;row<3;row++) {
                int count=Mathf.Max(1,Mathf.RoundToInt(length/(row%2==0?.79f:.66f)));
                Row(length,width,heights[row],heights[row+1],count,rng,map,false);
            }
        }
        public void Coping(float length,float width,float bottom,float top,int seed,Func<Vector3,Vector3> map)
        {
            Row(length,width,bottom,top,Mathf.Max(1,Mathf.RoundToInt(length/.73f)),new System.Random(seed),map,true);
        }
        void Row(float length,float width,float bottom,float top,int count,System.Random rng,Func<Vector3,Vector3> map,bool coping)
        {
            var spans=new float[count];float sum=0;
            for(int i=0;i<count;i++){spans[i]=.82f+(float)rng.NextDouble()*.36f;sum+=spans[i];}
            float start=-length*.5f;
            for(int i=0;i<count;i++) {
                float span=length*spans[i]/sum;
                float depth=width-.022f-(float)rng.NextDouble()*.020f;
                float height=top-bottom-.016f-(float)rng.NextDouble()*.008f;
                var centre=new Vector3(0,(bottom+top)*.5f,start+span*.5f);
                Block(centre,new Vector3(depth,height,span-.012f),coping?.015f:.020f,coping?4:rng.Next(4),rng,map);
                start+=span;
            }
        }
        void Block(Vector3 centre,Vector3 size,float bevel,int material,System.Random rng,Func<Vector3,Vector3> map)
        {
            if(vertices.Count+96>48000)Flush();
            Vector3 half=size*.5f;var cuts=new float[8];
            for(int i=0;i<8;i++)cuts[i]=Mathf.Min(bevel*(.65f+(float)rng.NextDouble()*.7f),Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.4f);
            Vector3 Corner(int x,int y,int z,int face)
            {
                float cut=cuts[(x>0?1:0)+(y>0?2:0)+(z>0?4:0)];
                return centre+new Vector3(x*(half.x-(face==0?0:cut)),y*(half.y-(face==1?0:cut)),z*(half.z-(face==2?0:cut)));
            }
            Vector3 Point(int axis,int sign,int a,int b)
            {
                return axis==0?Corner(sign,a,b,axis):axis==1?Corner(a,sign,b,axis):Corner(a,b,sign,axis);
            }
            void Face(params Vector3[] points)
            {
                Vector3 middle=Vector3.zero;foreach(var p in points)middle+=p;middle/=points.Length;
                if(Vector3.Dot(Vector3.Cross(points[1]-points[0],points[2]-points[0]),middle-centre)<0)Array.Reverse(points);
                int first=vertices.Count;for(int i=0;i<points.Length;i++)points[i]=map(points[i]);
                Vector3 n=Vector3.Cross(points[1]-points[0],points[2]-points[0]).normalized;
                foreach(var p in points) {
                    vertices.Add(p);
                    uv.Add((Mathf.Abs(n.y)>.6f?new Vector2(p.x,p.z):Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y):new Vector2(p.x,p.y))*1.15f);
                }
                for(int i=1;i<points.Length-1;i++){triangles[material].Add(first);triangles[material].Add(first+i);triangles[material].Add(first+i+1);}
            }
            foreach(int axis in new[]{0,1,2})foreach(int sign in new[]{-1,1})Face(Point(axis,sign,-1,-1),Point(axis,sign,1,-1),Point(axis,sign,1,1),Point(axis,sign,-1,1));
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})Face(Corner(x,y,-1,0),Corner(x,y,-1,1),Corner(x,y,1,1),Corner(x,y,1,0));
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})Face(Corner(x,-1,z,0),Corner(x,-1,z,2),Corner(x,1,z,2),Corner(x,1,z,0));
            foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})Face(Corner(-1,y,z,1),Corner(-1,y,z,2),Corner(1,y,z,2),Corner(1,y,z,1));
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})Face(Corner(x,y,z,0),Corner(x,y,z,1),Corner(x,y,z,2));
            blocks++;
        }
        void Flush()
        {
            if(vertices.Count==0)return;
            var mesh=new Mesh{name="Chamfered coastal masonry batch "+batches};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=materials.Length;
            for(int i=0;i<materials.Length;i++){mesh.SetTriangles(triangles[i],i);totalTriangles+=triangles[i].Count/3;triangles[i].Clear();}
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;
            totalVertices+=vertices.Count;batches++;vertices.Clear();uv.Clear();
        }
        public void Finish(){Flush();Debug.Log("LITTLE_ROAD_MASONRY stones="+blocks+" batches="+batches+" vertices="+totalVertices+" triangles="+totalTriangles);}
    }
}
