using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    public static class GroundCoverGeometryAudit
    {
        [Serializable] public class Sample {public string stage,mesh,indexFormat;public bool batched,readable;public int vertices,triangles,longEdges;public float maxEdge;}
        public static void Record(string stage,Transform root)
        {
            string folder=RaceTrial.Arg("--geometry-audit");if(folder==null)return;
            Directory.CreateDirectory(folder);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
                if(!filter.name.StartsWith("Low coastal ground cover"))continue;
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                var sample=new Sample{stage=stage,mesh=mesh.name,indexFormat=mesh.indexFormat.ToString(),batched=renderer.isPartOfStaticBatch,readable=mesh.isReadable};
                if(!mesh.isReadable){File.WriteAllText(Path.Combine(folder,stage+"-"+filter.name+".json"),JsonUtility.ToJson(sample,true));continue;}
                var v=mesh.vertices;sample.vertices=v.Length;
                for(int s=0;s<renderer.sharedMaterials.Length;s++) {
                    var indices=mesh.GetTriangles(renderer.subMeshStartIndex+s);sample.triangles+=indices.Length/3;
                    for(int i=0;i<indices.Length;i+=3) {
                        float edge=Mathf.Max(Vector3.Distance(v[indices[i]],v[indices[i+1]]),Vector3.Distance(v[indices[i+1]],v[indices[i+2]]),Vector3.Distance(v[indices[i+2]],v[indices[i]]));
                        sample.maxEdge=Mathf.Max(sample.maxEdge,edge);if(edge>2)sample.longEdges++;
                    }
                }
                File.WriteAllText(Path.Combine(folder,stage+"-"+filter.name+".json"),JsonUtility.ToJson(sample,true));
                Debug.Log("GROUND_COVER_AUDIT "+stage+" batched="+sample.batched+" vertices="+sample.vertices+" maxEdge="+sample.maxEdge+" malformedTriangles="+sample.longEdges);
            }
        }
    }
}
