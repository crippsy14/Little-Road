using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class TrafficPolicyChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Traffic policy check: "+name);checks.Add(name);}
        float Curve(float s)=>TrafficDriverPolicy.Blend(1,.15f,-3.8f,s,40);
        Check(Mathf.Abs(Curve(0)-1)<.0001f&&Mathf.Abs(Curve(40)+3.8f)<.0001f,"Passing path joins the observed lateral position to the requested lane");
        Check(Mathf.Abs((Curve(.01f)-Curve(0))/.01f-.15f)<.001f,"Passing path preserves initial heading instead of snapping sideways");
        Check(Mathf.Abs((Curve(40)-Curve(39.9f))/.1f)<.001f,"Passing path reaches the target lane parallel to its tangent");
        Check(Mathf.Abs(Curve(45)+3.8f)<.0001f,"Passing path remains in the selected lane beyond its transition");
        float Separation(Vector3 b,Vector3 forward)=>TrafficDriverPolicy.Separation(Vector3.zero,Vector3.forward,1,2,b,forward,1,2);
        Check(Separation(Vector3.forward*3.9f,Vector3.forward)<0&&Separation(Vector3.forward*4.1f,Vector3.forward)>0,"Full car lengths distinguish contact from longitudinal clearance");
        Check(Separation(Vector3.right*1.9f,Vector3.forward)<0&&Separation(Vector3.right*2.1f,Vector3.forward)>0,"Full car widths distinguish side overlap from clearance");
        Check(Separation(Vector3.right*2.9f,Vector3.right)<0&&Separation(Vector3.right*3.1f,Vector3.right)>0,"A sideways car uses its length when assessing lateral clearance");
        var rotated=Quaternion.Euler(0,35,0)*Vector3.forward;var position=new Vector3(2,0,3);
        float a=Separation(position,rotated),b=TrafficDriverPolicy.Separation(position,rotated,1,2,Vector3.zero,Vector3.forward,1,2);
        Check(Mathf.Abs(a-b)<.0001f,"Rotated car separation is symmetric between the two drivers");
        float inner=TrafficDriverPolicy.RouteGain(new[]{0f,8,16},10,12,out float innerScale);
        float outer=TrafficDriverPolicy.RouteGain(new[]{0f,12,24},10,18,out float outerScale);
        Check(Mathf.Abs(inner-15)<.001f&&Mathf.Abs(inner-outer)<.001f,"Equal circuit progress has equal reward despite different arc lengths");
        Check(TrafficDriverPolicy.RouteGain(new[]{0f,12,24},10,12,out _)<inner,"Driving equal metres on a longer outside arc earns less circuit progress");
        Check(Mathf.Abs(8*innerScale-12*outerScale)<.001f,"Terminal pace is compared in common route metres per second");
        Check(Mathf.Abs(TrafficDriverPolicy.AdvanceAnchor(10,995,5,1000,40)-20)<.001f,"Persistent manoeuvre advances correctly across the start line");
        Check(TrafficDriverPolicy.AdvanceAnchor(40,450,550,1000,40)==40,"Completed transition cannot restart after half a lap");
        Check(!TrafficDriverPolicy.RelevantTraffic(-24,20,18,5,10),"Non-closing rear car does not disable clear-road pace");
        Check(TrafficDriverPolicy.RelevantTraffic(-24,18,26,5,10),"Closing rear car remains relevant to a merge");
        Check(TrafficDriverPolicy.RelevantTraffic(-3,20,20,5,10),"Alongside body remains relevant at equal speed");
        Check(TrafficDriverPolicy.RelevantTraffic(95,30,0,5,10),"Stopped car at speed is anticipated beyond a fixed near ring");
        var turningVelocity=Quaternion.Euler(0,30,0)*Vector3.forward*20;
        Check(Mathf.Abs(TrafficDriverPolicy.LateralRoadMotion(turningVelocity,Vector3.right)-10)<.001f,"A lane change without tyre sideslip still predicts road-relative lateral motion");
        Check(Mathf.Abs(TrafficDriverPolicy.LateralRoadMotion(Vector3.forward*20,Vector3.right))<.001f,"Straight road-aligned travel has no predicted lateral motion");
        Check(Mathf.Abs(ObservedMotionPredictor.Travel(10,0,3)-30)<.001f,"Zero observed acceleration preserves constant-speed travel");
        float gaining=ObservedMotionPredictor.Travel(10,5,2);
        Check(gaining>20&&gaining<30,"Observed acceleration advances prediction with fading confidence");
        Check(Mathf.Abs(ObservedMotionPredictor.Travel(4,-20,2)-ObservedMotionPredictor.Travel(4,-20,4))<.001f,"Braking forecast stops instead of predicting reverse travel");
        var memory=new ObservedMotionPredictor();memory.Observe(2,0,10);
        Check(memory.Observe(2,.1,11)>0,"Acceleration memory responds to observed speed increase");
        Check(memory.Observe(2,1,15)==0,"Stale motion history cannot carry old acceleration into a new encounter");
        bool Sweep(Vector3 p,Vector3 movement,Vector3 otherMovement)=>ImmediateTrafficSafety.SweptConflict(Vector3.zero,Vector3.forward,1,2,movement,p,Vector3.forward,1,2,otherMovement,0,out _);
        Check(Sweep(Vector3.forward*10,Vector3.forward*20,Vector3.zero),"Immediate sweep detects a fast car crossing an obstacle between samples");
        Check(!Sweep(Vector3.right*3,Vector3.forward*20,Vector3.forward*20),"Parallel nearby cars with physical clearance do not trigger braking");
        Check(!Sweep(Vector3.forward*10,Vector3.forward*5,Vector3.forward*10),"An opening longitudinal gap is not an immediate threat");
        Check(!Sweep(new Vector3(10,0,10),Vector3.forward*20,Vector3.left*2),"Separate axis overlaps must occur at the same time");
        Check(!ImmediateTrafficSafety.OwnsFollowingResponse(-5)&&ImmediateTrafficSafety.OwnsFollowingResponse(5),"A rear approach cannot demand that the leading car brake");
        return checks;
    }
}
