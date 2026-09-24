using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Small authored asset, shared by the physical road, map and AI survey.
    // Handles preserve tangent continuity; this first fixture is not a C2 racing line.
    public sealed class TechnicalCircuit
    {
        [Serializable] public sealed class Knot
        {
            public string name;
            public float x,z,dx,dz,incoming,outgoing;
            public Vector3 Position=>new Vector3(x,0,z);
            public Vector3 Tangent=>new Vector3(dx,0,dz).normalized;
        }
        [Serializable] public sealed class Definition {public string id,version,title;public Knot[] knots;}
        public readonly Definition definition;
        public readonly Circuit track;
        public readonly float[] regionStarts;
        public TechnicalCircuit()
        {
            var asset=Resources.Load<TextAsset>("Tracks/cedar-technical");
            if(asset==null)throw new InvalidOperationException("Missing Cedar technical circuit asset.");
            definition=JsonUtility.FromJson<Definition>(asset.text);
            if(definition.knots==null||definition.knots.Length<4)throw new ArgumentException("Circuit needs at least four authored knots.");
            var points=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<definition.knots.Length;i++) {
                var a=definition.knots[i];var b=definition.knots[(i+1)%definition.knots.Length];
                if(a.Tangent.sqrMagnitude<.9f||a.incoming<=0||a.outgoing<=0)throw new ArgumentException("Invalid circuit handle: "+a.name);
                indices.Add(points.Count);
                var p=a.Position;var q=p+a.Tangent*a.outgoing;var s=b.Position;var r=s-b.Tangent*b.incoming;
                int count=Mathf.CeilToInt((Vector3.Distance(p,q)+Vector3.Distance(q,r)+Vector3.Distance(r,s))/2);
                for(int j=0;j<count;j++) {float t=j/(float)count,u=1-t;points.Add(u*u*u*p+3*u*u*t*q+3*u*t*t*r+t*t*t*s);}
            }
            track=new Circuit(points.ToArray());regionStarts=new float[indices.Count];
            for(int i=0;i<indices.Count;i++)regionStarts[i]=track.distance[indices[i]];
        }
        public string Region(float distance)
        {
            distance=Mathf.Repeat(distance,track.length);
            for(int i=regionStarts.Length-1;i>=0;i--)if(distance>=regionStarts[i])return definition.knots[i].name;
            return definition.knots[0].name;
        }
    }
}
