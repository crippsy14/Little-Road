using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The Castle Climb as a proper event: while it runs the route (town gate, the quay, Via Crest up to
    // the courtyard) is lined with red-and-white bollards that close every side turning; parked cars,
    // townsfolk and valley traffic on or beside the route are moved behind the bollards and cheer as
    // you pass. Hard mode adds hay bales and cones across alternate lanes. Everything is put back
    // when the climb ends.
    public sealed class CastleClimb:MonoBehaviour
    {
        public bool Active=>root!=null;public bool Hard{get;private set;}
        public int Bollards{get;private set;}public int Obstacles{get;private set;}public int Moved=>moved.Count;
        Transform root;SpeechBubbles bubbles;Transform player;float nextCheer;
        readonly List<(Transform t,Vector3 at,Quaternion rot,bool active)> moved=new();
        readonly List<ResidentIdle> cheering=new();readonly List<RoadCruiser> held=new();
        static List<Vector3> line;static List<float> half;static List<float> run;
        static readonly string[] Cheers={"Go go go!","Floor it!","You've got this!","Up the hill!","Wheee!","Beat the par!","Allez allez!","Mind the bend!","Hooray!","Nearly there!"};

        // The climb route from the start line to the courtyard, with each point's road half-width.
        public static List<Vector3> Line{get{Build();return line;}}
        static void Build()
        {
            if(line!=null)return;
            var main=HillcrestRoute.Mainline;var full=HillcrestRoute.DriveToCastle();
            line=new();half=new();run=new();
            float startRun=main.Length-34;
            for(int i=0;i<full.Count;i++) {
                if(i<main.line.Length&&main.run[i]<startRun)continue;
                var p=full[i];float best=float.MaxValue,w=4;
                foreach(var road in new[]{main,HillcrestRoute.Quay,HillcrestRoute.Crest}){float d=HillcrestRoute.Distance(road.line,p.x,p.z,out _);if(d<best){best=d;w=road.width*.5f;}}
                if(line.Count>0&&Flat(p-line[^1]).magnitude<.5f)continue;
                run.Add(line.Count==0?0:run[^1]+Flat(p-line[^1]).magnitude);line.Add(p);half.Add(w);
            }
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        // Distance from p to the route's edge (negative on the road), and the nearest centre point, its
        // right-hand direction and the run to it.
        public static float Edge(Vector3 p,out Vector3 centre,out Vector3 right,out float along)
        {
            Build();float best=float.MaxValue;centre=line[0];right=Vector3.right;along=0;
            for(int i=0;i+1<line.Count;i++) {
                var a=line[i];var b=line[i+1];var ab=Flat(b-a);float t=Mathf.Clamp01(Vector3.Dot(Flat(p-a),ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));
                var c=Vector3.Lerp(a,b,t);float d=Flat(p-c).magnitude-Mathf.Lerp(half[i],half[i+1],t);
                if(d<best){best=d;centre=c;right=Vector3.Cross(Vector3.up,ab.normalized);along=Mathf.Lerp(run[i],run[i+1],t);}
            }
            return best;
        }
        static (Vector3 at,Vector3 fwd,float half) At(float s)
        {
            int i=0;while(i+2<run.Count&&run[i+1]<s)i++;
            float t=Mathf.Clamp01((s-run[i])/Mathf.Max(run[i+1]-run[i],1e-4f));
            return (Vector3.Lerp(line[i],line[i+1],t),Flat(line[i+1]-line[i]).normalized,Mathf.Lerp(half[i],half[i+1],t));
        }

        public void Begin(bool hard,Transform playerCar,SpeechBubbles speech)
        {
            End();Build();Hard=hard;player=playerCar;bubbles=speech;
            root=new GameObject("Castle Climb event").transform;Physics.SyncTransforms();
            PlaceBollards();if(hard)PlaceObstacles();
            ClearRoute();Physics.SyncTransforms();
            Debug.Log($"CASTLE_CLIMB_EVENT hard={hard} bollards={Bollards} obstacles={Obstacles} moved={moved.Count} held={held.Count} cheering={cheering.Count} route={run[^1]:0}m");
        }
        public void End()
        {
            if(root!=null)Destroy(root.gameObject);root=null;
            foreach(var (t,at,rot,active) in moved)if(t!=null){t.SetPositionAndRotation(at,rot);t.gameObject.SetActive(active);}
            foreach(var c in held)if(c!=null){c.gameObject.SetActive(true);c.Release();}
            foreach(var r in cheering)if(r!=null)r.cheering=false;
            moved.Clear();held.Clear();cheering.Clear();Bollards=0;Obstacles=0;Physics.SyncTransforms();
        }
        void OnDestroy()=>End();

        // ---------------------------------------------------------------- bollards
        void PlaceBollards()
        {
            var spots=new List<Vector3>();float end=run[^1];var court=HillcrestRoute.CastleCourt;
            bool Free(Vector3 p,float near,out Vector3 at)
            {
                at=p;if(Edge(p,out _,out _,out _)<.45f)return false;                          // inside a bend: another stretch of the route
                if(!Ground(p,near,out float y))return false;at.y=y;
                if(BayviewKit.Occupied(new Bounds(at+Vector3.up*.5f,new Vector3(.4f,1,.4f))))return false;
                foreach(var q in spots)if(Flat(q-at).sqrMagnitude<1.1f)return false;
                return true;
            }
            // 1. Along both edges, but never on a road: junction mouths and the route's own corners are
            //    left to step 2 (edge lines there cut across the corner a car actually drives). Denser on
            //    paving (the market square) so it reads as closed.
            foreach(int side in new[]{-1,1})for(float s=10;s<end;) {
                var (c,f,h)=At(s);float step=4.5f;
                // Sharp turns (the gate and Via Crest junctions) stay open on both sides: drivers cut the
                // inside and run wide on the outside, and an edge line there made a pocket a car could be
                // trapped in. The closure lines (step 2) still shut the roads beyond.
                float turn=Vector3.SignedAngle(At(s-22).fwd,At(s+22).fwd,Vector3.up);
                if(Mathf.Abs(turn)<=25&&Flat(c-court).magnitude>=30) {
                    var p=c+Vector3.Cross(Vector3.up,f)*side*(h+.8f);
                    if(!OnRoad(p)&&Free(p,c.y,out var at)){spots.Add(at);if(Paved(at,c.y))step=2.2f;}
                }
                s+=step;
            }
            // 2. Every road that leaves the route gets a close-set line across it, 7 m beyond the corridor
            //    (run-off for a car that overshoots the turn at a T-junction).
            foreach(var road in HillcrestRoute.All()) {
                bool wasIn=Edge(road.At(0),out _,out _,out _)<0;
                for(float s=1;s<=road.Length;s+=1) {
                    bool nowIn=Edge(road.At(s),out _,out _,out _)<0;if(nowIn==wasIn)continue;wasIn=nowIn;
                    float at=nowIn?s-8:s+7;   // outside, on the side leaving the route
                    if(at<0||at>road.Length)continue;var c=road.At(at);if(Flat(c-court).magnitude<30)continue;
                    var right=road.Right(at);
                    for(float k=-road.width*.5f-.4f;k<=road.width*.5f+.45f;k+=1.5f)if(Free(c+right*k,c.y,out var p))spots.Add(p);
                }
            }
            Bollards=spots.Count;
            var shaft=new List<CombineInstance>();var band=new List<CombineInstance>();var cyl=Cylinder(12);
            var colliders=new GameObject("Bollard colliders").transform;colliders.SetParent(root);
            foreach(var p in spots) {
                shaft.Add(new CombineInstance{mesh=cyl,transform=Matrix4x4.TRS(p+Vector3.up*.45f,Quaternion.identity,new Vector3(.2f,.9f,.2f))});
                band.Add(new CombineInstance{mesh=cyl,transform=Matrix4x4.TRS(p+Vector3.up*.66f,Quaternion.identity,new Vector3(.215f,.16f,.215f))});
                band.Add(new CombineInstance{mesh=cyl,transform=Matrix4x4.TRS(p+Vector3.up*.93f,Quaternion.identity,new Vector3(.17f,.06f,.17f))});
                var box=new GameObject("Bollard").AddComponent<BoxCollider>();box.transform.SetParent(colliders);box.transform.position=p+Vector3.up*.45f;box.size=new Vector3(.22f,.9f,.22f);
            }
            Combined("Bollards white",shaft,TrialWorld.Material("Climb bollard white",new Color(.94f,.94f,.9f),0,.35f));
            Combined("Bollards red",band,TrialWorld.Material("Climb bollard red",new Color(.85f,.16f,.16f),0,.45f));
        }
        // Within a valley road's width (route roads included).
        static bool OnRoad(Vector3 p){foreach(var road in HillcrestRoute.All())if(HillcrestRoute.Distance(road.line,p.x,p.z,out _)<road.width*.5f+.3f)return true;return false;}
        static bool Paved(Vector3 p,float near)
        {
            foreach(var h in Physics.RaycastAll(new Vector3(p.x,near+4,p.z),Vector3.down,9,~0,QueryTriggerInteraction.Ignore))if(h.collider.GetComponentInParent<RoadSurface>()!=null)return true;
            return false;
        }
        static bool Ground(Vector3 p,float near,out float y)
        {
            y=float.MinValue;
            foreach(var h in Physics.RaycastAll(new Vector3(p.x,near+4,p.z),Vector3.down,9,~0,QueryTriggerInteraction.Ignore))
                if(h.collider.GetComponentInParent<RoadCar>()==null&&h.point.y>y)y=h.point.y;
            if(y==float.MinValue)y=ValleyField.Height(p.x,p.z);
            return Mathf.Abs(y-near)<2.2f;
        }
        void Combined(string name,List<CombineInstance> parts,Material material)
        {
            if(parts.Count==0)return;
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(parts.ToArray(),true,true);mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        }
        // Unit cylinder (radius 0.5, height 1, centred), smooth sides and flat caps.
        static Mesh cylinder;
        static Mesh Cylinder(int sides)
        {
            if(cylinder!=null)return cylinder;
            var v=new List<Vector3>();var n=new List<Vector3>();var tri=new List<int>();
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));v.Add(d*.5f+Vector3.down*.5f);n.Add(d);v.Add(d*.5f+Vector3.up*.5f);n.Add(d);}
            for(int i=0;i<sides;i++){int k=i*2;tri.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}
            int top=v.Count;v.Add(Vector3.up*.5f);n.Add(Vector3.up);
            for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Sin(a)*.5f,.5f,Mathf.Cos(a)*.5f));n.Add(Vector3.up);}
            for(int i=0;i<sides;i++)tri.AddRange(new[]{top,top+1+i,top+2+i});
            cylinder=new Mesh{name="Climb cylinder"};cylinder.SetVertices(v);cylinder.SetNormals(n);cylinder.SetTriangles(tri,0);cylinder.RecalculateBounds();return cylinder;
        }

        // ---------------------------------------------------------------- hard mode
        // A hay bale across one lane every ~55 m (alternating sides) with a short run of knockable cones
        // leading into it; none on tight bends, so there is always a clean line through.
        void PlaceObstacles()
        {
            float end=run[^1];int side=1;var coneMat=TrialWorld.Material("Climb cone",new Color(1,.45f,.1f),0,.4f);var court=HillcrestRoute.CastleCourt;
            for(float s=80;s<end-50;s+=55) {
                var (c,f,h)=At(s);if(Flat(c-court).magnitude<45)continue;
                var (_,f0,_)=At(s-10);var (_,f1,_)=At(s+10);if(Vector3.Angle(f0,f1)>28)continue;   // tight bend: skip
                var right=Vector3.Cross(Vector3.up,f);var lane=c+right*side*h*.48f;
                if(!Ground(lane,c.y,out float y))continue;lane.y=y;
                if(BayviewKit.Has("hay_bale")) {
                    var bale=BayviewKit.Spawn("hay_bale",lane,Quaternion.LookRotation(right).eulerAngles.y,BayviewKit.AnimatedRoot,scale:1.25f);bale.transform.SetParent(root,true);
                    if(bale.GetComponentInChildren<Collider>()==null){var b=bale.AddComponent<BoxCollider>();b.center=Vector3.up*.6f;b.size=new Vector3(2.2f,1.2f,1.4f);}
                } else {var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.transform.SetParent(root);b.transform.SetPositionAndRotation(lane+Vector3.up*.6f,Quaternion.LookRotation(f));b.transform.localScale=new Vector3(h*.9f,1.2f,1.4f);b.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Climb bale",new Color(.86f,.72f,.36f));}
                for(int k=1;k<=3;k++) {
                    var p=lane-f*(k*3.2f)+right*side*(k*.25f);if(!Ground(p,c.y,out float cy))continue;p.y=cy;
                    Cone(p,coneMat);
                }
                Obstacles++;side=-side;
            }
        }
        void Cone(Vector3 at,Material material)
        {
            var go=new GameObject("Climb cone",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.transform.position=at;
            go.GetComponent<MeshFilter>().sharedMesh=ConeMesh();go.GetComponent<MeshRenderer>().sharedMaterial=material;
            var box=go.AddComponent<BoxCollider>();box.center=Vector3.up*.3f;box.size=new Vector3(.42f,.6f,.42f);
            var body=go.AddComponent<Rigidbody>();body.mass=3;body.linearDamping=.4f;body.angularDamping=.6f;body.interpolation=RigidbodyInterpolation.Interpolate;
        }
        static Mesh cone;
        static Mesh ConeMesh()
        {
            if(cone!=null)return cone;const int sides=12;var v=new List<Vector3>();var tri=new List<int>();
            v.Add(Vector3.up*.62f);for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Sin(a)*.2f,.04f,Mathf.Cos(a)*.2f));}
            for(int i=0;i<sides;i++)tri.AddRange(new[]{0,1+i,2+i});
            int b=v.Count;foreach(var (x,z) in new[]{(-.25f,-.25f),(-.25f,.25f),(.25f,.25f),(.25f,-.25f)}){v.Add(new Vector3(x,.04f,z));}foreach(var (x,z) in new[]{(-.25f,-.25f),(-.25f,.25f),(.25f,.25f),(.25f,-.25f)}){v.Add(new Vector3(x,0,z));}
            tri.AddRange(new[]{b,b+1,b+2,b,b+2,b+3});for(int i=0;i<4;i++){int j=(i+1)%4;tri.AddRange(new[]{b+i,b+4+i,b+j,b+j,b+4+i,b+4+j});}
            cone=new Mesh{name="Climb cone"};cone.SetVertices(v);cone.SetTriangles(tri,0);cone.RecalculateNormals();cone.RecalculateBounds();return cone;
        }

        // ---------------------------------------------------------------- the crowd
        void ClearRoute()
        {
            bool Clear(Vector3 q)=>Edge(q,out _,out _,out _)>=2f;
            // Standing cars (residents, townsfolk, parked cars) on or right beside the route.
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)) {
                if(!(t.name.StartsWith("Resident ")||t.name=="Parked car")||!t.gameObject.activeInHierarchy)continue;
                float edge=Edge(t.position,out var c,out var right,out _);
                if(edge<1.5f) {
                    moved.Add((t,t.position,t.rotation,true));
                    if(!Beside(t.gameObject,c,right,Clear))t.gameObject.SetActive(false);
                }
                if(edge<40){var idle=t.GetComponent<ResidentIdle>();if(idle!=null){idle.cheering=true;cheering.Add(idle);}}
            }
            // Valley traffic on the route's roads (or near it) pulls over behind the bollards.
            foreach(var car in RoadCruiser.All.ToArray()) {
                if(car==null)continue;float edge=Edge(car.transform.position,out var c,out var right,out _);
                if(!car.OnRoad(HillcrestRoute.Mainline)&&!car.OnRoad(HillcrestRoute.Quay)&&!car.OnRoad(HillcrestRoute.Crest)&&edge>40)continue;
                car.Hold();held.Add(car);
                if(!Beside(car.gameObject,c,right,Clear))car.gameObject.SetActive(false);
            }
        }
        // Stand the car beside the route, behind the bollards on its own side, facing the road.
        static bool Beside(GameObject car,Vector3 centre,Vector3 right,System.Func<Vector3,bool> clear)
        {
            var t=car.transform;float side=Mathf.Sign(Vector3.Dot(Flat(t.position-centre),right));if(side==0)side=1;
            float h=-Edge(centre,out _,out _,out _);
            var p=centre+right*side*(Mathf.Max(h,3.7f)+3.6f);if(Ground(p,centre.y,out float y))p.y=y+.03f;else p.y=ValleyField.Height(p.x,p.z)+.03f;
            t.SetPositionAndRotation(p,Quaternion.LookRotation(-right*side));
            return StationarySpot.Settle(car,clear)>=0;
        }
        void Update()
        {
            if(!Active||player==null||bubbles==null||Time.time<nextCheer)return;nextCheer=Time.time+1.1f;
            // A cheer from someone just ahead of you.
            ResidentIdle pick=null;float best=float.MaxValue;
            foreach(var r in cheering) {
                if(r==null||!r.isActiveAndEnabled)continue;var d=r.transform.position-player.position;float dist=d.magnitude;
                if(dist<8||dist>34||Vector3.Dot(d,player.forward)<0)continue;float score=dist+Random.value*14;if(score<best){best=score;pick=r;}
            }
            if(pick==null)return;var chat=pick.GetComponent<NpcChatter>();
            bubbles.Show(pick.transform,chat!=null?chat.speaker:"Fan",Cheers[Random.Range(0,Cheers.Length)],chat!=null?chat.colour:Toy.Sun);
            if(chat!=null)chat.LastSaid=Time.time;
        }
    }
}
