using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RouteProjectionChecks
{
    static Vector3 BinaryAt(Circuit road,float meters)
    {
        meters=Mathf.Repeat(meters,road.length);int lo=0,hi=road.points.Length-1;
        while(lo<hi){int m=(lo+hi+1)/2;if(road.distance[m]<=meters)lo=m;else hi=m-1;}
        return Vector3.Lerp(road.points[lo],road.points[(lo+1)%road.points.Length],
            (meters-road.distance[lo])/(road.distance[lo+1]-road.distance[lo]));
    }
    static void EqualAt(Circuit road,float meters)
    {
        var expected=BinaryAt(road,meters);var actual=road.At(meters);
        if(!expected.Equals(actual))throw new Exception($"Route lookup changed at {meters}: {expected} != {actual}");
    }
    static float Exhaustive(Circuit road,Vector3 p,out float offset)
    {
        float best=float.MaxValue,result=0;Vector3 closest=Vector3.zero;
        for(int i=0;i<road.points.Length;i++) {
            Vector3 a=road.points[i],v=road.points[(i+1)%road.points.Length]-a;
            float t=Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude);Vector3 q=a+v*t;float sq=(q-p).sqrMagnitude;
            if(sq<best){best=sq;result=road.distance[i]+t*v.magnitude;closest=q;}
        }
        offset=Vector3.Dot(p-closest,road.Right(result));return result;
    }
    static void Equal(Circuit road,Vector3 position)
    {
        float expected=Exhaustive(road,position,out float side),actual=road.Nearest(position,out float actualSide);
        if(expected!=actual||side!=actualSide)throw new Exception($"Route projection changed at {position}: {expected}/{side} != {actual}/{actualSide}");
    }
    public static List<string> Run()
    {
        var results=new List<string>();var random=new System.Random(73021);
        foreach(var road in new[]{new Circuit(),new TechnicalCircuit().track}) {
            foreach(float vertex in road.distance)foreach(float delta in new[]{-.0001f,0,.0001f})EqualAt(road,vertex+delta);
            for(float bucket=0;bucket<=road.length;bucket+=4)foreach(float delta in new[]{-.0001f,0,.0001f})EqualAt(road,bucket+delta);
            for(int i=0;i<5000;i++)EqualAt(road,(float)random.NextDouble()*road.length*8-road.length*4);
            results.Add("Exact binary-search route sampling parity at vertices, lookup boundaries and 5000 wrapped distances: "+road.points.Length+" segments");
            foreach(var point in road.points)Equal(road,point);
            for(int i=0;i<1600;i++) {
                float distance=(float)random.NextDouble()*road.length,side=(float)random.NextDouble()*100-50;
                Equal(road,road.At(distance)+road.Right(distance)*side+Vector3.up*((float)random.NextDouble()*20-5));
            }
            Equal(road,new Vector3(10000,100,-10000));Equal(road,new Vector3(-10000,-100,10000));
            results.Add("Exact exhaustive projection parity at all vertices and 1602 on/off-track samples: "+road.points.Length+" segments");
        }
        // More than a leaf: duplicated/equidistant segments can lie in different
        // branches. Earliest original segment wins regardless of search order.
        var repeated=new Vector3[16];for(int i=0;i<16;i++)repeated[i]=new[]{new Vector3(-10,0,-10),new Vector3(10,0,-10),new Vector3(10,0,10),new Vector3(-10,0,10)}[i%4];
        var tied=new Circuit(repeated);Equal(tied,Vector3.zero);foreach(var p in repeated)Equal(tied,p);
        results.Add("Projection preserves earliest-segment ties across hierarchy branches and loop seam");
        var elevated=new Circuit(new[]{new Vector3(-20,0,0),new Vector3(0,12,20),new Vector3(20,0,0),new Vector3(0,-3,-20)});
        for(float d=-elevated.length;d<elevated.length*2;d+=.13f)EqualAt(elevated,d);
        results.Add("Exact route sampling parity on elevated routes and negative loop distances");
        for(int i=0;i<200;i++)Equal(elevated,new Vector3((float)random.NextDouble()*60-30,(float)random.NextDouble()*30-10,(float)random.NextDouble()*60-30));
        results.Add("Exact projection parity on elevated routes");return results;
    }
}
