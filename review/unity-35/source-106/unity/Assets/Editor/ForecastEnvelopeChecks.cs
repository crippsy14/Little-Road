using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class ForecastEnvelopeChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Forecast envelope check: "+name);checks.Add(name);}
        var envelope=new ForecastRoadEnvelope();var samples=new ContextPathPrediction.Sample[241];
        var straight=new Circuit(new[]{new Vector3(-100,0,0),new Vector3(100,0,0),new Vector3(100,0,100),new Vector3(-100,0,100)});
        var half=new Vector3(1,1,3);var centre=new Vector3(.25f,0,.4f);
        for(int i=0;i<samples.Length;i++)samples[i]=new ContextPathPrediction.Sample{time=i/120f,state=new PlanarDrivePrediction(new Vector3(-30+i*.25f,0,3),Vector3.right*30,Vector3.right,0,0)};
        envelope.Evaluate(straight,samples,samples.Length,centre,half,8);
        Check(envelope.Contained&&envelope.Queries<samples.Length*2,"Ample straight clearance certified with fewer than half the dense corner queries");
        // Endpoints clear, intermediate rotation protrudes: sampling endpoints alone fails.
        for(int i=0;i<2;i++)samples[i]=new ContextPathPrediction.Sample{time=i/120f,state=new PlanarDrivePrediction(new Vector3(0,0,5.6f),Vector3.zero,Quaternion.AngleAxis(i==0?-60:60,Vector3.up)*Vector3.forward,0,0)};
        float left=samples[0].state.MaxBodyOffset(straight,Vector3.zero,half),right=samples[1].state.MaxBodyOffset(straight,Vector3.zero,half);
        var middle=samples[0].state;middle.forward=Vector3.forward;
        envelope.Evaluate(straight,samples,2,Vector3.zero,half,8);
        Check(left<8&&right<8&&middle.MaxBodyOffset(straight,Vector3.zero,half)>8&&!envelope.Contained,"Between-state rotating-corner departure cannot be certified from clear endpoints");
        var random=new System.Random(93117);
        foreach(var road in new[]{new Circuit(),new TechnicalCircuit().track}) {
            bool boundsHold=true,distancesMatch=true;
            for(int test=0;test<32;test++) {
                float start=(float)random.NextDouble()*road.length;
                for(int i=0;i<33;i++) {
                    float d=start+i*1.2f,offset=6*Mathf.Sin(i*.12f+test);
                    var point=road.At(d)+road.Right(d)*offset;
                    samples[i]=new ContextPathPrediction.Sample{time=i/30f,state=new PlanarDrivePrediction(point,Vector3.zero,road.Forward(d),0,0)};
                    float exact=float.MaxValue;
                    for(int j=0;j<road.points.Length;j++) {
                        var a=road.points[j];var v=road.points[(j+1)%road.points.Length]-a;
                        exact=Mathf.Min(exact,Vector3.Distance(point,a+v*Mathf.Clamp01(Vector3.Dot(point-a,v)/v.sqrMagnitude)));
                    }
                    distancesMatch&=Mathf.Abs(exact-road.FlatDistance(point))<.00001f;
                }
                envelope.Evaluate(road,samples,33,centre,half,8);
                for(int i=0;i<32;i++)for(int j=0;j<=8;j++) {
                    var state=samples[i].state;float t=j/8f;
                    state.position=Vector3.Lerp(state.position,samples[i+1].state.position,t);
                    state.forward=Vector3.Slerp(state.forward,samples[i+1].state.forward,t);
                    boundsHold&=state.MaxBodyOffset(road,centre,half)<=envelope.intervalUpperBounds[i]+.0001f;
                }
            }
            Check(distancesMatch,"Flat-distance queries match exhaustive segments: "+road.points.Length);
            Check(boundsHold,"Mirrored curved and off-road corner sweeps remain bounded with collider-centre offset: "+road.points.Length);
        }
        var tinyGrade=new Circuit(new[]{new Vector3(0,0,0),new Vector3(10,.001f,0),new Vector3(0,0,10)});
        bool rejected=false;try{envelope.Evaluate(tinyGrade,samples,2,centre,half,8);}catch(ArgumentException){rejected=true;}
        Check(rejected,"Even sub-threshold elevation is rejected by the flat-distance envelope");
        return checks;
    }
}
