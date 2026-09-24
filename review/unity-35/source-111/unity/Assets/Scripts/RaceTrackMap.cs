using UnityEngine;
using UnityEngine.UI;

namespace LittleRoad
{
    // One fixed north-up projection shared by course geometry and physical car positions.
    public readonly struct TrackMapProjection
    {
        readonly Vector2 centre,half;readonly float scale;
        public TrackMapProjection(Vector3[] points,Vector2 size,float padding=14)
        {
            var low=new Vector2(float.MaxValue,float.MaxValue);var high=new Vector2(float.MinValue,float.MinValue);
            foreach(var p in points){var q=new Vector2(p.x,p.z);low=Vector2.Min(low,q);high=Vector2.Max(high,q);}
            centre=(low+high)*.5f;half=size*.5f-Vector2.one*padding;
            var extent=high-low+Vector2.one*Circuit.Width*2;
            scale=Mathf.Min(half.x*2/Mathf.Max(1,extent.x),half.y*2/Mathf.Max(1,extent.y));
        }
        public Vector2 Project(Vector3 world)
        {
            var p=(new Vector2(world.x,world.z)-centre)*scale;
            return new Vector2(Mathf.Clamp(p.x,-half.x,half.x),Mathf.Clamp(p.y,-half.y,half.y));
        }
    }
    // Static route mesh and dynamic markers are separate so only the small marker mesh refreshes.
    public sealed class RaceTrackMap:MaskableGraphic
    {
        Vector2[] route;Vector2 startA,startB;
        public void Setup(Circuit track,TrackMapProjection projection)
        {
            int count=Mathf.Min(512,track.points.Length);route=new Vector2[count];
            for(int i=0;i<count;i++)route[i]=projection.Project(track.At(track.length*i/count));
            var start=track.At(0);var right=track.Right(0)*12;startA=projection.Project(start-right);startB=projection.Project(start+right);
            raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(route==null)return;
            for(int i=0;i<route.Length;i++)Line(vh,route[i],route[(i+1)%route.Length],6,new Color(0,0,0,.65f));
            for(int i=0;i<route.Length;i++)Line(vh,route[i],route[(i+1)%route.Length],2,new Color(1,1,1,.82f));
            Line(vh,startA,startB,5,new Color(0,0,0,.8f));Line(vh,startA,startB,2,Color.white);
        }
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color colour)
        {
            var d=b-a;if(d.sqrMagnitude<.00001f)return;var n=new Vector2(-d.y,d.x).normalized*width*.5f;int v=vh.currentVertCount;
            vh.AddVert(a-n,colour,Vector2.zero);vh.AddVert(a+n,colour,Vector2.zero);vh.AddVert(b+n,colour,Vector2.zero);vh.AddVert(b-n,colour,Vector2.zero);
            vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
        }
    }
    public sealed class RaceMapCars:MaskableGraphic
    {
        RaceTrial race;TrackMapProjection projection;
        public void Setup(RaceTrial owner,TrackMapProjection project){race=owner;projection=project;raycastTarget=false;}
        public void Refresh(){SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(race==null)return;
            for(int i=1;i<race.cars.Count;i++){
                var p=projection.Project(race.cars[i].body.position);Dot(vh,p,4,new Color(0,0,0,.9f));Dot(vh,p,2.3f,Color.white);
            }
            // Player last: larger directional triangle stays visible in a packed grid, independently of colour.
            var player=race.cars[0];var at=projection.Project(player.body.position);var f=player.transform.forward;var forward=new Vector2(f.x,f.z).normalized;
            Triangle(vh,at,forward,9,Color.black);Triangle(vh,at,forward,6,new Color(.32f,.91f,.94f));
        }
        static void Dot(VertexHelper vh,Vector2 p,float radius,Color colour)
        {
            int v=vh.currentVertCount;vh.AddVert(p,colour,Vector2.zero);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,colour,Vector2.zero);}
            for(int i=0;i<8;i++)vh.AddTriangle(v,v+1+i,v+1+(i+1)%8);
        }
        static void Triangle(VertexHelper vh,Vector2 p,Vector2 f,float radius,Color colour)
        {
            var side=new Vector2(-f.y,f.x);int v=vh.currentVertCount;
            vh.AddVert(p+f*radius,colour,Vector2.zero);vh.AddVert(p-f*radius*.7f+side*radius*.7f,colour,Vector2.zero);vh.AddVert(p-f*radius*.7f-side*radius*.7f,colour,Vector2.zero);vh.AddTriangle(v,v+1,v+2);
        }
    }
}
