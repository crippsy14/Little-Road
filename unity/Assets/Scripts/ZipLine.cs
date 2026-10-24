using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Hillcrest Castle zipline: one cable from the Zip Tower in the castle's north wall down to the
    // lower-town station. A car rides it hanging in the trolley's cradle: hoisted to the launch deck, out
    // along the sagging cable (a fast zip down, a steady winch up) and lowered into the other bay.
    // One trolley; called from the far end it is simply waiting there (no empty run over first).
    public sealed class ZipLine
    {
        public static ZipLine Active;
        // Every line in the world: today only the castle zipline (Active; Quarry Hills' Blondin was retired on 24 Oct).
        public static readonly List<ZipLine> All=new();
        public string title="Hillcrest Castle Zipline";
        public const float Hang=2.7f;          // trolley (on the cable) to the car's ground contact
        public const float TopDeck=10.2f;      // zip tower launch deck above the tower's base
        public const float BottomHead=13.4f;   // trolley height under the lower station's headframe
        public const float SagPerMetre=.022f;  // mid-span sag per metre of horizontal span
        // ride: the prompt in this station's bay; arrive: the note on getting here.
        public sealed class Station{public Vector3 bay,cable;public float facing;public string name,ride,arrive;}
        public readonly Station top,bottom;
        public bool Busy{get;private set;}
        public bool TrolleyAtTop{get;private set;}=true;
        readonly Transform trolley;
        readonly List<Vector3> path=new();readonly List<float> along=new();
        float cableFrom,cableTo;               // path distances where the cable span starts and ends
        float parkTop,parkBottom;              // trolley parks under each headframe (path distances)
        public float Length=>along[^1];
        public IReadOnlyList<Vector3> Path=>path;

        public ZipLine(Transform topTower,Transform bottomStation,Transform trolley,Transform parent,Material cable)
        {
            top=new Station{bay=Child(topTower,"BayAnchor"),cable=Child(topTower,"CableAnchor"),facing=topTower.eulerAngles.y,name="Castle Zip Tower",ride="Ride the zipline down",arrive="Welcome to Hillcrest Castle"};
            bottom=new Station{bay=Child(bottomStation,"BayAnchor"),cable=Child(bottomStation,"CableAnchor"),facing=bottomStation.eulerAngles.y,name="Lower Town zip station",ride="Ride the lift up to the castle",arrive="Wheee! Welcome to the lower town"};
            this.trolley=trolley;
            // Top bay -> hoist -> launch boom -> cable -> lower boom -> over the bay -> down into it.
            Line(top.bay+Vector3.up*Hang,top.bay+Vector3.up*(TopDeck+Hang));parkTop=Measure();
            // Level out through the launch arch, then up to the boom once clear of the wall (a diagonal
            // carried the car through the masonry above the arch).
            var clear=new Vector3(top.cable.x,path[^1].y,top.cable.z);Line(path[^1],clear);Line(clear,top.cable);cableFrom=Measure();
            var span=Catenary(top.cable,bottom.cable,SagPerMetre);
            for(int i=1;i<span.Count;i++)Add(span[i]);cableTo=Measure();
            Line(path[^1],bottom.bay+Vector3.up*BottomHead);parkBottom=Measure();Line(path[^1],bottom.bay+Vector3.up*Hang);
            BuildCable(span,parent,cable);
            Park(true);Active=this;All.Add(this);
        }
        // A cableway whose bays lie under the rope itself (as Quarry Hills' Blondin was; none today): the car is hoisted straight up on the fall
        // rope to the main rope `pickTop` metres along the span from its top end, carried along it, and lowered into the
        // bay under the rope `pickBottom` metres short of the bottom end. The rope's own mesh is the caller's.
        public ZipLine(string title,Station top,Station bottom,List<Vector3> span,Transform trolley)
        {
            this.title=title;this.top=top;this.bottom=bottom;this.trolley=trolley;
            Line(top.bay+Vector3.up*Hang,span[0]);parkTop=Measure();cableFrom=parkTop;
            for(int i=1;i<span.Count;i++)Add(span[i]);cableTo=Measure();parkBottom=cableTo;
            Line(path[^1],bottom.bay+Vector3.up*Hang);
            Park(true);All.Add(this);
        }
        static Vector3 Child(Transform t,string name){foreach(var c in t.GetComponentsInChildren<Transform>(true))if(c.name==name)return c.position;throw new System.InvalidOperationException(t.name+" has no "+name);}
        void Add(Vector3 p){along.Add(path.Count==0?0:along[^1]+Vector3.Distance(path[^1],p));path.Add(p);}
        void Line(Vector3 a,Vector3 b){if(path.Count==0)Add(a);int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));for(int i=1;i<=n;i++)Add(Vector3.Lerp(a,b,i/(float)n));}
        float Measure()=>along[^1];
        public static List<Vector3> Catenary(Vector3 a,Vector3 b,float sagPerMetre)
        {
            var d=b-a;d.y=0;float span=d.magnitude,sag=span*sagPerMetre;int n=Mathf.Max(8,Mathf.CeilToInt(span/1.5f));var pts=new List<Vector3>();
            for(int i=0;i<=n;i++){float t=i/(float)n;pts.Add(Vector3.Lerp(a,b,t)+Vector3.down*(4*sag*t*(1-t)));}
            return pts;
        }
        public Vector3 At(float s)
        {
            s=Mathf.Clamp(s,0,Length);int lo=0,hi=along.Count-1;
            while(hi-lo>1){int m=(lo+hi)/2;if(along[m]<=s)lo=m;else hi=m;}
            float seg=along[hi]-along[lo];return seg<1e-5f?path[lo]:Vector3.Lerp(path[lo],path[hi],(s-along[lo])/seg);
        }
        void Park(bool atTop){TrolleyAtTop=atTop;PoseTrolley(At(atTop?parkTop:parkBottom),atTop?top.facing:bottom.facing);}
        void PoseTrolley(Vector3 p,float yaw){trolley.SetPositionAndRotation(p,Quaternion.Euler(0,yaw,0));}

        // Thin steel tube along the span (6 sides).
        static void BuildCable(List<Vector3> pts,Transform parent,Material material)
        {
            const int sides=6;const float r=.055f;var v=new List<Vector3>();var n=new List<Vector3>();var t=new List<int>();
            for(int i=0;i<pts.Count;i++) {
                var fwd=(pts[Mathf.Min(i+1,pts.Count-1)]-pts[Mathf.Max(i-1,0)]).normalized;var side=Vector3.Cross(Vector3.up,fwd).normalized;var up=Vector3.Cross(fwd,side);
                for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides;var dir=side*Mathf.Cos(a)+up*Mathf.Sin(a);v.Add(pts[i]+dir*r);n.Add(dir);}
            }
            for(int i=0;i<pts.Count-1;i++)for(int k=0;k<sides;k++){int a=i*sides+k,b=i*sides+(k+1)%sides;t.AddRange(new[]{a,a+sides,b,b,a+sides,b+sides});}
            var mesh=new Mesh{name="Zipline cable",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Zipline cable",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }

        // Travel speed at path distance s: slow hoists and boom runs, the span ramps up and brakes.
        float Speed(float s,bool down,bool empty)
        {
            if(s<cableFrom||s>cableTo)return 4.2f;
            float top=empty?14:down?17:10,accel=down?2.6f:2f;
            float fromStart=down?s-cableFrom:cableTo-s,toEnd=down?cableTo-s:s-cableFrom;
            return Mathf.Clamp(Mathf.Min(4.2f+Mathf.Sqrt(2*accel*fromStart),4.2f+Mathf.Sqrt(2*3.2f*toEnd)),4.2f,top);
        }
        // Moves the trolley from path distance a to b (car optional), with its speed profile and sway.
        IEnumerator Travel(float a,float b,RoadCar car,bool empty)
        {
            float s=a,dir=Mathf.Sign(b-a),yaw=trolley.eulerAngles.y,swing=0,swingVel=0,lastV=0;bool down=dir>0;
            while(dir>0?s<b:s>b) {
                float v=Speed(s,down,empty);s=dir>0?Mathf.Min(b,s+v*Time.deltaTime):Mathf.Max(b,s-v*Time.deltaTime);
                var p=At(s);var ahead=At(s+dir*1.5f)-At(s-dir*.5f);ahead.y=0;
                if(ahead.sqrMagnitude>.25f&&s>cableFrom-3&&s<cableTo+3)yaw=Mathf.MoveTowardsAngle(yaw,Quaternion.LookRotation(ahead).eulerAngles.y,90*Time.deltaTime);
                PoseTrolley(p,yaw);
                if(car!=null) {
                    // The car swings on its straps: it lags as the trolley speeds up and swings on as it brakes.
                    float acc=(v-lastV)/Mathf.Max(Time.deltaTime,1e-3f);lastV=v;
                    swingVel+=(-(9.81f/Hang)*swing-1.6f*swingVel-acc/Hang)*Time.deltaTime;swing=Mathf.Clamp(swing+swingVel*Time.deltaTime,-.35f,.35f);
                    var hang=Quaternion.Euler(0,yaw,0)*Quaternion.AngleAxis(swing*Mathf.Rad2Deg,Vector3.right);
                    var carPos=p+hang*(Vector3.down*Hang);car.body.position=carPos;car.body.rotation=hang;car.transform.SetPositionAndRotation(carPos,hang);
                }
                yield return null;
            }
        }
        // Empty run to the other end so the trolley is where the car is.
        public IEnumerator Fetch(bool toTop)
        {
            // The trolley is simply waiting at the end it's called to (Richard, 14 Oct: the empty run from the far end was just
            // a wait). It used to travel the whole line first.
            if(TrolleyAtTop==toTop)yield break;
            Park(toTop);yield return null;
        }
        // Carries the car from one end to the other. The car must be kinematic for the ride.
        public IEnumerator Ride(RoadCar car,bool down)
        {
            Busy=true;
            try {
                yield return Fetch(down);
                var from=down?top:bottom;var body=car.body;
                // Trolley lowers into the bay while the car settles under it, facing the way it leaves.
                var startPos=body.position;var startRot=body.rotation;var settled=Quaternion.Euler(0,from.facing,0);
                float park=down?parkTop:parkBottom,bay=down?0:Length;
                for(float t=0;t<1;t+=Time.deltaTime/1.0f){
                    float e=Mathf.SmoothStep(0,1,t);body.position=Vector3.Lerp(startPos,from.bay,e);body.rotation=Quaternion.Slerp(startRot,settled,e);
                    car.transform.SetPositionAndRotation(body.position,body.rotation);PoseTrolley(At(Mathf.Lerp(park,bay,e)),from.facing);yield return null;
                }
                yield return Travel(bay,down?Length:0,car,false);
                var level=Quaternion.Euler(0,car.body.rotation.eulerAngles.y,0);body.rotation=level;car.transform.rotation=level;
                // Trolley winds back up to its park under the headframe.
                float arrive=down?Length:0,home=down?parkBottom:parkTop;
                for(float t=0;t<1;t+=Time.deltaTime/1.2f){PoseTrolley(At(Mathf.Lerp(arrive,home,Mathf.SmoothStep(0,1,t))),trolley.eulerAngles.y);yield return null;}
                Park(!down);
            } finally {Busy=false;}
        }
        // Clearance of a hanging car along a candidate span (car bottom minus whatever is under it),
        // ignoring the first and last few metres at the stations. Used to site the stations.
        public static float Clearance(Vector3 a,Vector3 b,float sagPerMetre,float skipStart,float skipEnd,System.Func<Collider,bool> ignore=null)
        {
            var pts=Catenary(a,b,sagPerMetre);float total=0,best=float.MaxValue;
            for(int i=1;i<pts.Count;i++){total+=Vector3.Distance(pts[i-1],pts[i]);}
            float run=0;var dir=b-a;dir.y=0;var side=Vector3.Cross(Vector3.up,dir.normalized);
            for(int i=0;i<pts.Count;i++){
                if(i>0)run+=Vector3.Distance(pts[i-1],pts[i]);if(run<skipStart||total-run<skipEnd)continue;
                float bottomY=pts[i].y-Hang-.4f;
                foreach(float o in new[]{-1.3f,0,1.3f}){
                    var from=pts[i]+side*o+Vector3.up*200;
                    foreach(var hit in Physics.RaycastAll(from,Vector3.down,400)){if(ignore!=null&&ignore(hit.collider))continue;best=Mathf.Min(best,bottomY-hit.point.y);}
                    best=Mathf.Min(best,bottomY-ValleyField.Height(pts[i].x+side.x*o,pts[i].z+side.z*o));
                }
            }
            return best;
        }
        // Siting report: for each bearing from the castle and each distance for the lower station, the
        // worst clearance under a hanging car, the drop, the distance to a road and the ground's slope.
        public static string Survey()
        {
            Physics.SyncTransforms();var c=HillcrestRoute.HillCentre;var sb=new System.Text.StringBuilder("run theta r clearance drop road slope blocked\n");
            bool Ignore(Collider col)=>col.isTrigger||(HillcrestTown.ZipTop!=null&&col.transform.IsChildOf(HillcrestTown.ZipTop.transform));
            // The Zip Tower can take any curtain run (their mid bearings); stations anywhere within 40 degrees of it.
            foreach(float run in HillcrestTown.CurtainRuns) {
                float ra=run*Mathf.Deg2Rad;var topCable=new Vector3(c.x+Mathf.Sin(ra)*44.8f,HillcrestRoute.CastleY+15.65f,c.y+Mathf.Cos(ra)*44.8f);
                for(int theta=Mathf.RoundToInt(run)-40;theta<=run+40;theta+=5)for(int r=150;r<=270;r+=15) {
                    float a=theta*Mathf.Deg2Rad;var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                    var at=new Vector3(c.x,0,c.y)+dir*r;at.y=ValleyField.Height(at.x,at.z);var toTop=topCable-at;toTop.y=0;toTop.Normalize();
                    var bottomCable=at+toTop*7.6f+Vector3.up*15.45f;
                    float clear=Clearance(topCable,bottomCable,SagPerMetre,12,12,Ignore);
                    float road=FreeRoamRoads.Nearest(at,out var onRoad,out _)?Vector3.Distance(new Vector3(at.x,0,at.z),new Vector3(onRoad.x,0,onRoad.z)):999;
                    float slope=0;for(int k=0;k<8;k++){float b=k*Mathf.PI/4;slope=Mathf.Max(slope,Mathf.Abs(ValleyField.Height(at.x+Mathf.Sin(b)*6,at.z+Mathf.Cos(b)*6)-at.y));}
                    // Anything solid in the station's footprint (12 x 10 m with its canopy), terrain aside.
                    int blocked=Physics.OverlapBox(at+Vector3.up*5,new Vector3(7,4.5f,7),Quaternion.LookRotation(toTop)).Count(col=>!col.isTrigger&&!col.name.Contains("mainland collider"));
                    sb.AppendLine($"{run:0.0} {(theta+360)%360} {r} {clear:0.0} {topCable.y-bottomCable.y:0.0} {road:0.0} {slope:0.00} {blocked}");
                }
            }
            return sb.ToString();
        }
    }
}
