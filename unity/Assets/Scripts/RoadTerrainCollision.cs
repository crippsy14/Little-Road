using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    public static class RoadTerrainCollision
    {
        // The rendered island remains intact. Clip only its collider away from
        // the 24 m supported route, so hidden terrain cannot supply ramp normals.
        public static Mesh Build(Circuit track,Mesh source)
        {
            var input=source.vertices;var vertices=new List<Vector3>(input);var output=new List<int>();
            var edgeVertices=new Dictionary<ulong,int>();var signed=new float[input.Length];
            // In parallel (docs/72): Nearest and At only read the circuit.
            System.Threading.Tasks.Parallel.For(0,input.Length,i=>{
                var flat=new Vector3(input[i].x,0,input[i].z);float d=track.Nearest(flat,out _);
                signed[i]=Vector3.Distance(flat,track.At(d))-12;
            });
            int Intersection(int a,int b)
            {
                int low=Mathf.Min(a,b),high=Mathf.Max(a,b);ulong key=((ulong)(uint)low<<32)|(uint)high;
                if(edgeVertices.TryGetValue(key,out int index))return index;
                float t=signed[low]/(signed[low]-signed[high]);index=vertices.Count;
                vertices.Add(Vector3.Lerp(input[low],input[high],t));edgeVertices[key]=index;return index;
            }
            int[] polygon=new int[4];var triangles=source.triangles;
            for(int i=0;i<triangles.Length;i+=3) {
                int count=0;
                for(int edge=0;edge<3;edge++) {
                    int a=triangles[i+edge],b=triangles[i+(edge+1)%3];bool insideA=signed[a]>=0,insideB=signed[b]>=0;
                    if(insideA)polygon[count++]=a;
                    if(insideA!=insideB)polygon[count++]=Intersection(a,b);
                }
                for(int j=1;j<count-1;j++){output.Add(polygon[0]);output.Add(polygon[j]);output.Add(polygon[j+1]);}
            }
            var mesh=new Mesh{name="Island collision outside supported road",indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetTriangles(output,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
