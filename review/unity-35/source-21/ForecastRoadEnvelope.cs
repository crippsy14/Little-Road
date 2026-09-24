using System;
using UnityEngine;

namespace LittleRoad
{
    // Bounds the four collider corners through the piecewise rigid interpolation
    // of a predicted trajectory. This is not a model-error allowance or proof
    // about every point inside a body on an arbitrary non-convex road.
    public sealed class ForecastRoadEnvelope
    {
        readonly float[] distance=new float[361],travel=new float[361];
        public readonly float[] intervalUpperBounds=new float[360];
        public int Queries {get;private set;}
        public float FirstUncertainTime {get;private set;}
        public float MaximumBound {get;private set;}
        public bool Contained=>FirstUncertainTime<0;
        ContextPathPrediction.Sample[] samples;Circuit road;Vector3 centre,half;float limit;
        const float RoundoffPadding=.002f;
        public void Evaluate(Circuit road,ContextPathPrediction.Sample[] samples,int count,Vector3 centre,Vector3 half,float limit)
        {
            if(!road.hasConstantHeight||count<2||count>distance.Length)throw new ArgumentException("Envelope requires 2..361 level-road states.");
            this.road=road;this.samples=samples;this.centre=centre;this.half=half;this.limit=limit;
            Queries=0;FirstUncertainTime=-1;MaximumBound=0;travel[0]=0;
            float radius=new Vector2(Mathf.Abs(centre.x)+half.x,Mathf.Abs(centre.z)+half.z).magnitude;
            for(int i=0;i<count;i++) {
                distance[i]=-1;
                if(i==0)continue;
                var a=samples[i-1].state;var b=samples[i].state;
                float angle=Mathf.Abs(Mathf.Atan2(Vector3.Cross(a.forward,b.forward).y,Vector3.Dot(a.forward,b.forward)));
                float aLength=a.forward.magnitude,bLength=b.forward.magnitude;
                travel[i]=travel[i-1]+Vector3.Distance(a.position,b.position)+radius*(Mathf.Max(aLength,bLength)*angle+Mathf.Abs(aLength-bLength));
            }
            Check(0,count-1);
        }
        float At(int i)
        {
            if(distance[i]>=0)return distance[i];
            var state=samples[i].state;var right=Vector3.Cross(Vector3.up,state.forward);
            var middle=state.position+right*centre.x+state.forward*centre.z;
            float maximum=0;
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2) {
                maximum=Mathf.Max(maximum,road.FlatDistance(middle+right*(side*half.x)+state.forward*(end*half.z)));Queries++;
            }
            return distance[i]=maximum;
        }
        void Check(int a,int b)
        {
            float first=At(a),last=At(b),length=travel[b]-travel[a];
            // Distance to a fixed set is 1-Lipschitz. Every corner travels at
            // most length; the two endpoint cones meet at (first+last+length)/2.
            float bound=Mathf.Max(Mathf.Max(first,last),(first+last+length)*.5f)+RoundoffPadding;
            if(bound<=limit||b==a+1) {
                for(int i=a;i<b;i++)intervalUpperBounds[i]=bound;
                MaximumBound=Mathf.Max(MaximumBound,bound);
                if(bound>limit&&FirstUncertainTime<0)FirstUncertainTime=samples[a].time;
                return;
            }
            int middle=(a+b)/2;Check(a,middle);Check(middle,b);
        }
    }
}
