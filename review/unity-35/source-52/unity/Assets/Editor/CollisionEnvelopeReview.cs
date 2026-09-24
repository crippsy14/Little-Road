using System;
using System.Collections.Generic;
using System.IO;
using LittleRoad;
using UnityEngine;

public static class CollisionEnvelopeReview
{
    [Serializable] public class Entry {public string body;public Vector3 colliderSize,colliderCentre,animatedMin,animatedMax;public float maxHorizontalEscape;public int poses,vertices;}
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
            result.bodies.Add(new Entry{body=asset,colliderSize=shape.size,colliderCentre=shape.center,animatedMin=animated.min,animatedMax=animated.max,maxHorizontalEscape=escape,poses=poses,vertices=count});
            UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(race.gameObject);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(result,true));
        Debug.Log("LITTLE_ROAD_ENVELOPE_REVIEW "+path);
    }
}
