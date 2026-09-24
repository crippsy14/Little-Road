using System;
using System.Collections.Generic;
using System.IO;
using LittleRoad;
using UnityEngine;

public static class CollisionEnvelopeReview
{
    [Serializable] public class Footprint {public string mesh;public Vector2[] hull;}
    [Serializable] public class Entry {public string body;public Vector3 colliderSize,colliderCentre,animatedMin,animatedMax;public float maxHorizontalEscape;public int poses,vertices;public List<Footprint> restFootprints=new();}
    [Serializable] public class Evidence {public string scope="Actual imported mesh vertices at sampled wheel spin/steering poses, compared with existing rest-pose box. Not continuous triangle collision proof.";public List<Entry> bodies=new();}
    public static void Run(string path)
    {
        var result=new Evidence();
        foreach(string asset in new[]{"compact_v1","coupe_car","utility_car","formula_car"}) {
            var car=TrialWorld.Car(asset,Color.white);
            var race=new GameObject("Envelope review owner").AddComponent<RaceTrial>();race.track=new Circuit();
            var driver=car.AddComponent<RoadCar>();driver.Setup(race,0,CarTuning.Stock,false,1,"envelope review");
            var shape=car.GetComponent<BoxCollider>();var physical=new Bounds(shape.center,shape.size);
            var filters=car.GetComponentsInChildren<MeshFilter>();var vertices=new Vector3[filters.Length][];int count=0;
            for(int i=0;i<filters.Length;i++){vertices[i]=filters[i].sharedMesh.vertices;count+=vertices[i].Length;}
            var footprints=new List<Footprint>();
            for(int f=0;f<filters.Length;f++) {
                var matrix=car.transform.worldToLocalMatrix*filters[f].transform.localToWorldMatrix;
                var points=new List<Vector2>();
                foreach(var vertex in vertices[f]){var p=matrix.MultiplyPoint3x4(vertex);points.Add(new Vector2(p.x,p.z));}
                footprints.Add(new Footprint{mesh=filters[f].name,hull=Hull(points)});
            }
            var wheels=new WheelVisuals(car);var animated=new Bounds();bool first=true;int poses=0;float escape=0;
            for(int steer=-25;steer<=25;steer+=5)for(int spin=0;spin<360;spin+=30) {
                wheels.Pose(spin,steer);poses++;
                for(int f=0;f<filters.Length;f++) {
                    var matrix=car.transform.worldToLocalMatrix*filters[f].transform.localToWorldMatrix;
                    foreach(var v in vertices[f]) {
                        var p=matrix.MultiplyPoint3x4(v);
                        if(first){animated=new Bounds(p,Vector3.zero);first=false;}else animated.Encapsulate(p);
                        escape=Mathf.Max(escape,p.x-physical.max.x,physical.min.x-p.x,p.z-physical.max.z,physical.min.z-p.z);
                    }
                }
            }
            result.bodies.Add(new Entry{body=asset,colliderSize=shape.size,colliderCentre=shape.center,animatedMin=animated.min,animatedMax=animated.max,maxHorizontalEscape=escape,poses=poses,vertices=count,restFootprints=footprints});
            UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(race.gameObject);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(result,true));
        Debug.Log("LITTLE_ROAD_ENVELOPE_REVIEW "+path);
    }
    // Per-mesh projected convex hulls are conservative visual overlays, not
    // proposed physics colliders or exact concave silhouette measurements.
    static Vector2[] Hull(List<Vector2> points)
    {
        points.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
        var unique=new List<Vector2>();foreach(var p in points)if(unique.Count==0||!p.Equals(unique[unique.Count-1]))unique.Add(p);
        if(unique.Count<3)return unique.ToArray();
        float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
        var hull=new List<Vector2>();
        foreach(var p in unique){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        int lower=hull.Count;
        for(int i=unique.Count-2;i>=0;i--){var p=unique[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
        hull.RemoveAt(hull.Count-1);return hull.ToArray();
    }
}
