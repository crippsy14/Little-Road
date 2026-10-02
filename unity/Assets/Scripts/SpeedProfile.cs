using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // How fast a car can drive a road (docs/51): each point's corner limit from its curvature and the tyres' grip, a
    // forward pass of the car's real acceleration envelope (with the grade), and a backward pass of its braking. The
    // speed traps' marks and Beat the Train's timetable both come from it.
    public static class SpeedProfile
    {
        public static float[] Speeds(IReadOnlyList<Vector3> path,CarTuning t,float startSpeed=0,bool brakeAtEnd=false)
        {
            int n=path.Count;var v=new float[n];if(n==0)return v;
            float budget=t.roadGrip*1.65f,aero=DriveModel.Aero(t);var cap=new float[n];
            for(int s=0;s<n;s++) {
                float curve=0;
                if(s>0&&s+1<n){var u=path[s]-path[s-1];var w=path[s+1]-path[s];u.y=w.y=0;curve=Vector3.Angle(u,w)*Mathf.Deg2Rad/Mathf.Max(.5f,(u.magnitude+w.magnitude)*.5f);}
                float room=curve-budget*.64f*aero;   // v^2*curve = .64*budget*(1+aero*v^2)
                cap[s]=Mathf.Min(t.AttainableSpeed,curve>1e-4f&&room>1e-6f?Mathf.Sqrt(budget*.64f/room):float.MaxValue);
            }
            v[0]=Mathf.Min(startSpeed,cap[0]);
            for(int s=1;s<n;s++) {
                float ds=Mathf.Max(.01f,Vector3.Distance(path[s-1],path[s]));
                DriveModel.Forces(t,v[s-1],1,0,t.roadGrip*DriveModel.AeroGrip(t,v[s-1]),9.81f*(path[s].y-path[s-1].y)/ds,out float accel,out _);
                v[s]=Mathf.Min(cap[s],Mathf.Sqrt(Mathf.Max(0,v[s-1]*v[s-1]+2*accel*ds)));
            }
            if(brakeAtEnd)v[n-1]=0;
            for(int s=n-2;s>=0;s--) {
                float ds=Mathf.Max(.01f,Vector3.Distance(path[s],path[s+1]));
                DriveModel.Forces(t,v[s+1],-1,0,t.roadGrip*DriveModel.AeroGrip(t,v[s+1]),9.81f*(path[s+1].y-path[s].y)/ds,out float decel,out _);
                v[s]=Mathf.Min(v[s],Mathf.Sqrt(v[s+1]*v[s+1]+2*Mathf.Max(.5f,-decel)*ds));
            }
            return v;
        }
        // The ideal time to drive the whole path from a standstill.
        public static float Time(IReadOnlyList<Vector3> path,CarTuning t)
        {
            var v=Speeds(path,t);float time=0;
            for(int s=1;s<path.Count;s++){float ds=Vector3.Distance(path[s-1],path[s]);time+=ds/Mathf.Max(.5f,(v[s-1]+v[s])*.5f);}
            return time;
        }
        public static float Length(IReadOnlyList<Vector3> path){float d=0;for(int s=1;s<path.Count;s++)d+=Vector3.Distance(path[s-1],path[s]);return d;}
    }
}
