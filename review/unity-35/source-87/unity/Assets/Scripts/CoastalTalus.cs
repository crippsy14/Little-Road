using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Original fractured blocks: intersect a set of irregular cutting planes,
    // then construct the resulting convex faces. Shared variants avoid hundreds
    // of unique rock meshes. Surface detail uses the existing coast material.
    public sealed class CoastalTalus
    {
        readonly CoastalLandscape land;readonly Transform parent;readonly Material material;
        readonly Mesh[] meshes=new Mesh[8];int count;
        public CoastalTalus(CoastalLandscape surface,Transform root,Material stone){land=surface;parent=root;material=stone;}
        public void Scatter(Vector2 centre,Vector3 extent,int seed)
        {
            var rng=new System.Random(seed+8961);
            for(int i=0;i<24;i++) {
                float angle=(float)rng.NextDouble()*Mathf.PI*2;
                float distance=.55f+(float)rng.NextDouble()*.60f;
                Vector3 sample=new Vector3(centre.x+Mathf.Cos(angle)*extent.x*distance,0,centre.y+Mathf.Sin(angle)*extent.z*distance);
                // Fallen blocks belong on lower, supported banks, not stuck
                // halfway up a steep face or perched on the formation's cap.
                if(!land.SurfacePoint(sample,out var hit,false)||hit.point.y< -8||hit.normal.y<.88f||hit.point.y>land.Height(centre.x,centre.y)-1.5f)continue;
                float size=1.1f+(float)rng.NextDouble()*1.5f;
                Vector3 scale=new Vector3(size*(.8f+(float)rng.NextDouble()*.7f),size*.85f,size);
                Quaternion rotation=Quaternion.Euler((float)rng.NextDouble()*16-8,(float)rng.NextDouble()*360,(float)rng.NextDouble()*16-8);
                Vector3 position=hit.point-Vector3.up*size*.22f;
                int variant=(seed+i)%meshes.Length;Mesh mesh=meshes[variant]??=Create(variant);
                bool clear=true;
                foreach(var vertex in mesh.vertices)if(!land.SolidClearance(position+rotation*Vector3.Scale(vertex,scale))){clear=false;break;}
                if(!clear)continue;
                var go=new GameObject("Fractured coastal talus "+count,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
                go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(position,rotation);go.transform.localScale=scale;go.tag="Respawn";
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshCollider>().sharedMesh=mesh;count++;
            }
        }
        public void Record()=>Debug.Log("LITTLE_ROAD_TALUS blocks="+count+" sharedVariants="+meshes.Length);
        static Mesh Create(int variant)
        {
            var rng=new System.Random(4721+variant*67);var normals=new List<Vector3>();var distances=new List<float>();
            void Plane(Vector3 n,float d){normals.Add(n.normalized);distances.Add(d);}
            foreach(var axis in new[]{Vector3.right,Vector3.up,Vector3.forward})foreach(int sign in new[]{-1,1})Plane(axis*sign,.65f+(float)rng.NextDouble()*.22f);
            for(int i=0;i<18;i++) {
                float y=(float)rng.NextDouble()*2-1,a=(float)rng.NextDouble()*Mathf.PI*2,r=Mathf.Sqrt(1-y*y);
                Plane(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r),.73f+(float)rng.NextDouble()*.30f);
            }
            var corners=new List<Vector3>();
            for(int a=0;a<normals.Count-2;a++)for(int b=a+1;b<normals.Count-1;b++)for(int c=b+1;c<normals.Count;c++) {
                Vector3 cross=Vector3.Cross(normals[b],normals[c]);float denominator=Vector3.Dot(normals[a],cross);
                if(Mathf.Abs(denominator)<.0001f)continue;
                Vector3 p=(distances[a]*cross+distances[b]*Vector3.Cross(normals[c],normals[a])+distances[c]*Vector3.Cross(normals[a],normals[b]))/denominator;
                bool inside=true;for(int j=0;j<normals.Count;j++)if(Vector3.Dot(normals[j],p)>distances[j]+.0001f){inside=false;break;}
                if(inside&&!corners.Exists(v=>(v-p).sqrMagnitude<.0000001f))corners.Add(p);
            }
            var vertices=new List<Vector3>();var triangles=new List<int>();var faceNormals=new List<Vector3>();
            for(int face=0;face<normals.Count;face++) {
                Vector3 n=normals[face];var polygon=corners.FindAll(p=>Mathf.Abs(Vector3.Dot(n,p)-distances[face])<.0002f);
                if(polygon.Count<3)continue;
                Vector3 centre=Vector3.zero;foreach(var p in polygon)centre+=p;centre/=polygon.Count;
                Vector3 tangent=Vector3.Cross(n,Mathf.Abs(n.y)>.9f?Vector3.right:Vector3.up).normalized,bitangent=Vector3.Cross(n,tangent);
                polygon.Sort((a,b)=>Mathf.Atan2(Vector3.Dot(a-centre,bitangent),Vector3.Dot(a-centre,tangent)).CompareTo(Mathf.Atan2(Vector3.Dot(b-centre,bitangent),Vector3.Dot(b-centre,tangent))));
                int first=vertices.Count;foreach(var p in polygon){vertices.Add(p);faceNormals.Add(n);}
                for(int i=1;i<polygon.Count-1;i++){triangles.Add(first);triangles.Add(first+i);triangles.Add(first+i+1);}
            }
            var mesh=new Mesh{name="Original cut limestone block "+variant};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetNormals(faceNormals);mesh.RecalculateBounds();return mesh;
        }
    }
}
