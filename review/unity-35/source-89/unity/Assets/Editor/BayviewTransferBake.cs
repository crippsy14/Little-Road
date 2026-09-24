using System;
using System.Collections.Generic;
using System.IO;
using LittleRoad;
using UnityEditor;
using UnityEngine;

// Track adapter only: bounded geometric seed, independent of either driver's laps.
public static class BayviewTransferBake
{
    const string Asset="Assets/Resources/RacingLines/bayview-smooth-v1.json";
    public static void Prepare()
    {
        var road=new Circuit();int n=Mathf.CeilToInt(road.length/8),count=Mathf.CeilToInt(road.length/2);
        var centres=new Vector3[n];var rights=new Vector3[n];var offsets=new float[n];var path=new Vector3[n];var second=new Vector3[n];
        for(int i=0;i<n;i++){centres[i]=road.At(i*road.length/n);rights[i]=road.Right(i*road.length/n);}
        for(int step=0;step<24000;step++) {
            for(int i=0;i<n;i++)path[i]=centres[i]+rights[i]*offsets[i];
            for(int i=0;i<n;i++)second[i]=path[(i+n-1)%n]-2*path[i]+path[(i+1)%n];
            for(int i=0;i<n;i++)offsets[i]=Mathf.Clamp(offsets[i]-.05f*Vector3.Dot(rights[i],second[(i+n-1)%n]-2*second[i]+second[(i+1)%n]),-4,4);
        }
        for(int i=0;i<n;i++)path[i]=centres[i]+rights[i]*offsets[i];
        var data=new RacingLine.Data{schema=2,id="bayview-smooth-v1",trackId="bayview",trackVersion="bayview-v1",trackHash=RacingLine.BayviewGeometryHash(road),roadLength=road.length,
            status="transfer candidate; geometric seed, not validated ideal line",method="bounded bending-energy controls at ~8m;24000 iterations;4m offset bound; periodic cubic B-spline",offsets=new float[count],positions=new Vector3[count]};
        for(int j=0;j<count;j++) {
            float d=j*road.length/count,coordinate=j*(float)n/count;int i=(int)coordinate;float t=coordinate-i,u=1-t;
            var p=(u*u*u*path[(i+n-1)%n]+(3*t*t*t-6*t*t+4)*path[i]+(-3*t*t*t+3*t*t+3*t+1)*path[(i+1)%n]+t*t*t*path[(i+2)%n])/6;
            data.positions[j]=p;data.offsets[j]=Vector3.Dot(p-road.At(d),road.Right(d));
        }
        string text=JsonUtility.ToJson(data,true);
        if(!File.Exists(Asset)||File.ReadAllText(Asset)!=text){File.WriteAllText(Asset,text);AssetDatabase.ImportAsset(Asset,ImportAssetOptions.ForceSynchronousImport);}
    }
    public static List<string> Run()
    {
        var road=new Circuit();var line=new RacingLine(road,"bayview-smooth-v1","bayview","bayview-v1");float body=0;
        for(int i=0;i<line.Count;i++) {
            var forward=(line.At(i*line.roadStep+1)-line.At(i*line.roadStep-1)).normalized;var right=Vector3.Cross(Vector3.up,forward);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1}){road.Nearest(line.points[i]+right*x*1.3f+forward*z*2.1f,out float offset);body=Mathf.Max(body,Mathf.Abs(offset));}
        }
        if(body>=7.1f)throw new Exception("Bayview geometric seed lacks body clearance: "+body);
        foreach(var tuning in new[]{CarTuning.Stock,CarTuning.Club}) {
            var speed=new RacingSpeedProfile(line,tuning);
            if(!float.IsFinite(speed.predictedLapTime)||speed.predictedLapTime<=10||speed.predictedLapTime>=180)throw new Exception("Invalid Bayview speed profile");
        }
        var changed=(Vector3[])road.points.Clone();changed[0]+=Vector3.right*.01f;
        if(RacingLine.BayviewGeometryHash(road)==RacingLine.BayviewGeometryHash(new Circuit(changed)))throw new Exception("Bayview hash missed geometry change");
        bool rejected=false;try{new RacingLine(new Circuit(changed),"bayview-smooth-v1","bayview","bayview-v1");}catch(ArgumentException){rejected=true;}
        if(!rejected)throw new Exception("Stale Bayview reference was accepted");
        return new(){"Bayview transfer seed has body clearance and finite stock/club speed profiles","Bayview reference rejects modified geometry, independent of Cedar asset hash"};
    }
}
