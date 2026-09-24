using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Immutable segment hierarchy. Unlike a last-position hint, this still
    // searches the whole route after a crash, teleport or nearby track branch.
    public sealed class RouteProjectionIndex
    {
        struct Node {public Vector3 min,max;public int start,count,left,right;}
        sealed class AxisOrder:IComparer<int>
        {
            readonly Vector3[] points;readonly int axis;
            public AxisOrder(Vector3[] points,int axis){this.points=points;this.axis=axis;}
            public int Compare(int a,int b)
            {
                float ca=points[a][axis]+points[(a+1)%points.Length][axis];
                float cb=points[b][axis]+points[(b+1)%points.Length][axis];
                int order=ca.CompareTo(cb);return order!=0?order:a.CompareTo(b);
            }
        }
        readonly Vector3[] points;
        readonly Vector3[] vectors;
        readonly float[] squaredLengths,lengths;
        readonly float[] distances;
        readonly int[] segments;
        readonly Node[] nodes;
        readonly AxisOrder[] orders;
        int used;
        public RouteProjectionIndex(Vector3[] points,float[] distances)
        {
            this.points=points;this.distances=distances;
            vectors=new Vector3[points.Length];squaredLengths=new float[points.Length];lengths=new float[points.Length];
            for(int i=0;i<points.Length;i++) {
                vectors[i]=points[(i+1)%points.Length]-points[i];
                squaredLengths[i]=vectors[i].sqrMagnitude;lengths[i]=vectors[i].magnitude;
            }
            segments=new int[points.Length];nodes=new Node[points.Length*2];
            orders=new[]{new AxisOrder(points,0),new AxisOrder(points,1),new AxisOrder(points,2)};
            for(int i=0;i<segments.Length;i++)segments[i]=i;
            Build(0,segments.Length);
        }
        int Build(int start,int count)
        {
            int index=used++;var min=Vector3.one*float.PositiveInfinity;var max=Vector3.one*float.NegativeInfinity;
            for(int j=start;j<start+count;j++) {
                int i=segments[j];min=Vector3.Min(min,Vector3.Min(points[i],points[(i+1)%points.Length]));
                max=Vector3.Max(max,Vector3.Max(points[i],points[(i+1)%points.Length]));
            }
            // Conservative padding prevents bound rounding from discarding a
            // segment whose exact projection ties the current closest point.
            var padding=Vector3.one*.001f;
            var node=new Node{min=min-padding,max=max+padding,start=start,count=count,left=-1,right=-1};
            if(count>8) {
                var span=max-min;int axis=span.x>=span.y&&span.x>=span.z?0:span.y>=span.z?1:2;
                Array.Sort(segments,start,count,orders[axis]);int split=count/2;
                node.left=Build(start,split);node.right=Build(start+split,count-split);node.count=0;
            }
            nodes[index]=node;return index;
        }
        static float Bound(Vector3 p,in Node node)
        {
            var delta=Vector3.Max(Vector3.zero,Vector3.Max(node.min-p,p-node.max));return delta.sqrMagnitude;
        }
        public float Nearest(Vector3 p,out Vector3 closest)
        {
            float best=float.MaxValue,result=0;int segment=int.MaxValue;closest=Vector3.zero;
            Search(0,p,Bound(p,in nodes[0]),ref best,ref segment,ref result,ref closest);return result;
        }
        void Search(int index,Vector3 p,float bound,ref float best,ref int segment,ref float result,ref Vector3 closest)
        {
            ref var node=ref nodes[index];if(bound>best)return;
            if(node.count>0) {
                for(int j=node.start;j<node.start+node.count;j++) {
                    int i=segments[j];Vector3 a=points[i],v=vectors[i];
                    // Preserve the original arithmetic and earliest-segment
                    // tie break exactly; traversal order must not change a lap.
                    float t=Mathf.Clamp01(Vector3.Dot(p-a,v)/squaredLengths[i]);var q=a+v*t;float sq=(q-p).sqrMagnitude;
                    if(sq<best||(sq==best&&i<segment)){best=sq;segment=i;result=distances[i]+t*lengths[i];closest=q;}
                }
                return;
            }
            int first=node.left,second=node.right;
            // Bounds depend only on this query and immutable node geometry.
            // Reuse the ordering calculation when visiting each child, still
            // comparing against the freshly tightened best distance on entry.
            float firstBound=Bound(p,in nodes[first]),secondBound=Bound(p,in nodes[second]);
            if(firstBound>secondBound){first=node.right;second=node.left;float swap=firstBound;firstBound=secondBound;secondBound=swap;}
            Search(first,p,firstBound,ref best,ref segment,ref result,ref closest);
            Search(second,p,secondBound,ref best,ref segment,ref result,ref closest);
        }
    }
}
