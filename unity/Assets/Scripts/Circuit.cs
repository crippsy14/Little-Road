using UnityEngine;

namespace LittleRoad
{
    // Arc-length resampled authored spline; vehicles steer toward it through
    // physics. No car position is prescribed by a spline parameter.
    public sealed class Circuit:IDriverRoute
    {
        public const float Width=16;
        // This circuit's own width (Richard, 20 Oct: per-circuit widths, for Bramblewick's lanes). Every circuit built so far
        // is the standard 16 m, and everything that reads it gives exactly the old numbers there.
        public readonly float width=Width;
        public float Half=>width*.5f;
        public Circuit(Vector3[] route,float width):this(route){this.width=width;}
        public readonly Vector3[] points;
        public readonly float[] distance;
        public readonly float length;
        readonly RouteProjectionIndex projection;
        // Segment lookup only: interpolation still uses the authored vertices
        // and original arithmetic. No sampled approximation of the road.
        const float LookupStep=4;
        readonly int[] segmentLookup;
        public float Length=>length;
        public readonly bool hasElevation;
        public readonly bool hasConstantHeight;
        // Explicit closed routes support authored proving grounds without
        // changing the existing Bayview spline or the physical car controller.
        public Circuit(Vector3[] route)
        {
            if(route==null||route.Length<3)throw new System.ArgumentException("A closed route needs at least three points.");
            points=(Vector3[])route.Clone();distance=new float[points.Length+1];
            float low=points[0].y,high=low;
            for(int i=0;i<points.Length;i++) {
                low=Mathf.Min(low,points[i].y);high=Mathf.Max(high,points[i].y);
                float segment=Vector3.Distance(points[i],points[(i+1)%points.Length]);
                if(segment<.001f)throw new System.ArgumentException("Route contains a zero-length segment.");
                distance[i+1]=distance[i]+segment;
            }
            length=distance[points.Length];
            segmentLookup=BuildSegmentLookup();
            hasElevation=high-low>.02f;
            hasConstantHeight=high==low;
            projection=new RouteProjectionIndex(points,distance);
        }
        // The island loop: Bayview's, or the new Bayview's Harbour Loop with --new-bayview (docs/79).
        public Circuit():this(NewBayview.On?NewBayview.LoopPoints():null,true){}
        Circuit(Vector3[] island,bool _)
        {
            if(island!=null) {
                points=(Vector3[])island.Clone();distance=new float[points.Length+1];float low=points[0].y,high=low;
                for(int i=0;i<points.Length;i++){low=Mathf.Min(low,points[i].y);high=Mathf.Max(high,points[i].y);distance[i+1]=distance[i]+Vector3.Distance(points[i],points[(i+1)%points.Length]);}
                length=distance[points.Length];segmentLookup=BuildSegmentLookup();hasElevation=high-low>.02f;hasConstantHeight=high==low;
                projection=new RouteProjectionIndex(points,distance);return;
            }
            Vector3[] anchors={new(-70,0,-82),new(15,0,-82),new(87,0,-68),new(118,0,-25),
                new(108,0,27),new(63,0,57),new(8,0,42),new(-27,0,70),new(-83,0,83),
                new(-118,0,47),new(-117,0,-10)};
            int n=anchors.Length*36; points=new Vector3[n];distance=new float[n+1];
            for(int i=0;i<n;i++) {int s=i/36; float t=(i%36)/36f;
                Vector3 a=anchors[(s+anchors.Length-1)%anchors.Length],b=anchors[s],c=anchors[(s+1)%anchors.Length],d=anchors[(s+2)%anchors.Length];
                points[i]=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);}
            for(int i=0;i<n;i++) distance[i+1]=distance[i]+Vector3.Distance(points[i],points[(i+1)%n]);
            length=distance[n];
            segmentLookup=BuildSegmentLookup();
            hasConstantHeight=true;
            projection=new RouteProjectionIndex(points,distance);
        }
        // The same closed route driven the other way (docs/67: class B's reversed events). Point 0, the start
        // line, stays where it is; the road, its world and its features are untouched.
        public Circuit Reversed(){var r=new Vector3[points.Length];r[0]=points[0];for(int i=1;i<points.Length;i++)r[i]=points[points.Length-i];return new Circuit(r);}
        public Vector3 At(float meters)
        {
            meters=Mathf.Repeat(meters,length);
            int bucket=Mathf.Min(segmentLookup.Length-1,(int)(meters/LookupStep));
            int lo=segmentLookup[bucket];
            // The backwards correction also covers floating-point rounding at
            // a bucket boundary; forward correction preserves vertex ties.
            while(lo>0&&distance[lo]>meters)lo--;
            while(lo<points.Length-1&&distance[lo+1]<=meters)lo++;
            return Vector3.Lerp(points[lo],points[(lo+1)%points.Length],(meters-distance[lo])/(distance[lo+1]-distance[lo]));
        }
        int[] BuildSegmentLookup()
        {
            var lookup=new int[Mathf.CeilToInt(length/LookupStep)+1];int segment=0;
            for(int i=0;i<lookup.Length;i++) {
                while(segment<points.Length-1&&distance[segment+1]<=i*LookupStep)segment++;
                lookup[i]=segment;
            }
            return lookup;
        }
        public Vector3 Forward(float meters) => (At(meters+1)-At(meters-1)).normalized;
        public Vector3 Right(float meters) {var right=Vector3.Cross(Vector3.up,Forward(meters));return hasElevation?right.normalized:right;}
        public Vector3 Normal(float meters) => Vector3.Cross(Forward(meters),Right(meters)).normalized;
        public float Curvature(float meters)
        {
            // A vertical crest is not a horizontal corner. Grade/contact
            // constraints are measured separately by the ground-aware car.
            if(hasElevation) {
                var a=Vector3.ProjectOnPlane(Forward(meters-4),Vector3.up);var b=Vector3.ProjectOnPlane(Forward(meters+4),Vector3.up);
                return Mathf.Abs(Mathf.Atan2(Vector3.Cross(a,b).y,Vector3.Dot(a,b)))/8;
            }
            return Vector3.Angle(Forward(meters-4),Forward(meters+4))*Mathf.Deg2Rad/8;
        }
        public float Nearest(Vector3 p, out float offset)
        {
            float result=projection.Nearest(p,out var closest);
            offset=Vector3.Dot(p-closest,Right(result));return result;
        }
        // Distance to the complete flat centreline, without tangent queries.
        // It bounds the magnitude of the signed road-offset projection.
        public float FlatDistance(Vector3 p)
        {
            if(!hasConstantHeight&&!GradeModel.Enabled)throw new System.InvalidOperationException("Flat-distance envelope requires a level circuit.");
            projection.Nearest(p,out var closest);var delta=p-closest;delta.y=0;return delta.magnitude;
        }
        public Mesh Ribbon(float center, float width, float height)
        {
            int n=points.Length;Vector3[] v=new Vector3[(n+1)*2];Vector2[] uv=new Vector2[v.Length];int[] tris=new int[n*6];
            for(int i=0;i<=n;i++) {Vector3 p=points[i%n],r=Right(distance[i]);
                v[i*2]=p+r*(center-width*.5f)+Vector3.up*height;v[i*2+1]=p+r*(center+width*.5f)+Vector3.up*height;
                uv[i*2]=new Vector2(0,distance[i]/5);uv[i*2+1]=new Vector2(1,distance[i]/5);
                if(i<n) {int a=i*2,k=i*6;tris[k]=a;tris[k+1]=a+2;tris[k+2]=a+1;tris[k+3]=a+1;tris[k+4]=a+2;tris[k+5]=a+3;}}
            Mesh mesh=new Mesh {name="Authored circuit ribbon",vertices=v,uv=uv,triangles=tris};mesh.RecalculateNormals();return mesh;
        }
        public Mesh DrivableSupport()
        {
            // Join ground, shoulder and asphalt continuously. Separate raised
            // flat colliders leave 11/14 mm vertical edges that can stop a box
            // chassis entering from the garage even though the road looks flat.
            float[] offsets={-12,-8.8f,-8,8,8.8f,12};
            float[] heights={0,.011f,.025f,.025f,.011f,0};
            int n=points.Length,columns=offsets.Length;var vertices=new Vector3[(n+1)*columns];
            var triangles=new int[n*(columns-1)*6];int triangle=0;
            for(int row=0;row<=n;row++) {
                var right=Right(distance[row]);
                for(int column=0;column<columns;column++) {
                    vertices[row*columns+column]=points[row%n]+right*offsets[column]+Vector3.up*heights[column];
                    if(row==n||column==columns-1)continue;
                    int a=row*columns+column,b=a+columns;
                    triangles[triangle++]=a;triangles[triangle++]=b;triangles[triangle++]=a+1;
                    triangles[triangle++]=a+1;triangles[triangle++]=b;triangles[triangle++]=b+1;
                }
            }
            var mesh=new Mesh{name="Continuous road and shoulder support",vertices=vertices,triangles=triangles};
            mesh.RecalculateNormals();return mesh;
        }
    }
}
