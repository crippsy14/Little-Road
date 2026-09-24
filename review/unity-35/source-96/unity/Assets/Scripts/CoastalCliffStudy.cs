using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Opt-in comparison of a credited CC0 scan; no change to default scenery.
    public static class CoastalCliffStudy
    {
        public static void Build(CoastalLandscape land,Material limestone)
        {
            var root=new GameObject("Coastal cliff scan study");
            root.transform.SetPositionAndRotation(new Vector3(-97,0,-7),Quaternion.Euler(0,-90,0));
            root.transform.localScale=new Vector3(1,1.5f,1);
            // Fit the anchor toward the slope only as far as every source
            // vertex allows; the lighthouse approach is protected as well.
            var source=Resources.Load<GameObject>("CoastStudy/cliff_scan_lod0");
            if(source==null)throw new InvalidOperationException("Missing cliff source");
            bool allowed=false;
            for(int step=0;step<=18&&!allowed;step++) {
                root.transform.position=new Vector3(-97+step*.5f,0,-7);allowed=true;
                foreach(var filter in source.GetComponentsInChildren<MeshFilter>()) {
                    // Keep the imported FBX root's unit/axis conversion, just as
                    // Instantiate(prefab,parent) does for its local transform.
                    Matrix4x4 transform=root.transform.localToWorldMatrix*filter.transform.localToWorldMatrix;
                    foreach(var vertex in filter.sharedMesh.vertices)if(!land.SolidClearance(transform.MultiplyPoint3x4(vertex))){allowed=false;break;}
                    if(!allowed)break;
                }
            }
            if(!allowed)throw new InvalidOperationException("No safe cliff anchor in comparison area");
            // The scan has an uneven open lower edge. Measure each half-metre
            // column from LOD0 so all LODs share the same fitted foundation.
            var bottom=new float[49];var top=new float[49];
            for(int i=0;i<49;i++){bottom[i]=float.PositiveInfinity;top[i]=float.NegativeInfinity;}
            foreach(var filter in source.GetComponentsInChildren<MeshFilter>()) {
                Matrix4x4 transform=root.transform.localToWorldMatrix*filter.transform.localToWorldMatrix;
                foreach(var vertex in filter.sharedMesh.vertices){var p=transform.MultiplyPoint3x4(vertex);int c=Mathf.Clamp(Mathf.RoundToInt((p.z+19)*2),0,48);bottom[c]=Mathf.Min(bottom[c],p.y);top[c]=Mathf.Max(top[c],p.y);}
            }
            var material=RaceTrial.Has("--cliff-photo-surface")?Resources.Load<Material>("CoastStudy/CoastalCliff"):limestone;
            if(material==null)throw new InvalidOperationException("Missing authored cliff material");
            var levels=new List<LOD>();int[] counts=new int[3];float[] thresholds={.18f,.07f,.006f};
            Bounds bounds=new Bounds();bool first=true;int outside=0;
            for(int lod=0;lod<3;lod++) {
                var prefab=Resources.Load<GameObject>("CoastStudy/cliff_scan_lod"+lod);
                if(prefab==null)throw new InvalidOperationException("Missing scanned cliff LOD "+lod);
                var visual=UnityEngine.Object.Instantiate(prefab,root.transform);
                var renderers=visual.GetComponentsInChildren<Renderer>();
                foreach(var filter in visual.GetComponentsInChildren<MeshFilter>()) {
                    // Bury both cut ends into the supporting slope while leaving
                    // the central scanned face intact. Use identical world-space
                    // deformation for all LODs and the near collision mesh.
                    var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;
                    for(int v=0;v<vertices.Length;v++) {
                        var p=filter.transform.TransformPoint(vertices[v]);
                        float end=Mathf.SmoothStep(0,1,Mathf.InverseLerp(8,12,Mathf.Abs(p.z+7)));
                        // Turn cropped ends into the hillside instead of pulling
                        // an entire exposed column down into a stretched curtain.
                        p.x+=end*6;
                        int column=Mathf.Clamp(Mathf.RoundToInt((p.z+19)*2),0,48);
                        float low=float.IsFinite(bottom[column])?bottom[column]:0,high=float.IsFinite(top[column])?top[column]:15;
                        float foot=Mathf.SmoothStep(0,1,Mathf.InverseLerp(low,Mathf.Lerp(low,high,.60f),p.y));
                        p.y=Mathf.Lerp(Mathf.Min(p.y,land.Height(p.x,p.z)-1.2f),p.y,foot);
                        vertices[v]=filter.transform.InverseTransformPoint(p);
                    }
                    mesh.vertices=vertices;CloseLowerBoundary(mesh,filter.transform,land);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();filter.sharedMesh=mesh;
                    counts[lod]+=filter.sharedMesh.triangles.Length/3;
                    if(lod==0)foreach(var vertex in filter.sharedMesh.vertices)if(!land.SolidClearance(filter.transform.TransformPoint(vertex)))outside++;
                    if(lod==0){filter.gameObject.name="Layered coastal outcrop scan collider";filter.gameObject.tag="Respawn";filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;}
                }
                foreach(var renderer in renderers){renderer.sharedMaterial=material;if(lod==0){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}}
                levels.Add(new LOD(thresholds[lod],renderers));
            }
            if(outside>0)throw new InvalidOperationException("Scanned cliff intrudes into protected corridor: "+outside+" vertices at "+root.transform.position);
            var group=root.AddComponent<LODGroup>();group.SetLODs(levels.ToArray());group.RecalculateBounds();
            Debug.Log("LITTLE_ROAD_CLIFF_STUDY triangles="+string.Join(",",counts)+" bounds="+bounds+" protectedVertices="+outside);
        }

        static void CloseLowerBoundary(Mesh mesh,Transform transform,CoastalLandscape land)
        {
            // Photogrammetry is an open surface. Extend only true lower boundary
            // edges into the earth; weld position keys so UV seams do not grow
            // internal skirts. The same resulting mesh is used by the collider.
            var vertices=new List<Vector3>(mesh.vertices);var uv=new List<Vector2>(mesh.uv);
            var triangles=new List<int>(mesh.triangles);var keys=new Dictionary<Vector3Int,int>();var canonical=new int[vertices.Count];
            for(int i=0;i<vertices.Count;i++) {
                var p=transform.TransformPoint(vertices[i]);var key=new Vector3Int(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000));
                if(!keys.TryGetValue(key,out int id)){id=keys.Count;keys.Add(key,id);}canonical[i]=id;
            }
            var edges=new Dictionary<long,(int a,int b,int count)>();
            void Edge(int a,int b) {
                int ca=canonical[a],cb=canonical[b];if(ca==cb)return;
                long key=((long)Mathf.Min(ca,cb)<<32)|(uint)Mathf.Max(ca,cb);
                if(edges.TryGetValue(key,out var old))edges[key]=(old.a,old.b,old.count+1);else edges[key]=(a,b,1);
            }
            for(int i=0;i<triangles.Count;i+=3){Edge(triangles[i],triangles[i+1]);Edge(triangles[i+1],triangles[i+2]);Edge(triangles[i+2],triangles[i]);}
            foreach(var edge in edges.Values) {
                if(edge.count!=1)continue;
                var a=transform.TransformPoint(vertices[edge.a]);var b=transform.TransformPoint(vertices[edge.b]);
                if((a.y+b.y)*.5f>11.5f)continue;
                float ay=Mathf.Min(a.y,land.Height(a.x,a.z)-1.2f),by=Mathf.Min(b.y,land.Height(b.x,b.z)-1.2f);
                if(a.y-ay<.02f&&b.y-by<.02f)continue;
                int lower=vertices.Count;vertices.Add(transform.InverseTransformPoint(new Vector3(a.x,ay,a.z)));vertices.Add(transform.InverseTransformPoint(new Vector3(b.x,by,b.z)));
                uv.Add(uv[edge.a]);uv.Add(uv[edge.b]);
                triangles.AddRange(new[]{edge.b,edge.a,lower,edge.b,lower,lower+1});
            }
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
        }
    }
}
