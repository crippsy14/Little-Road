using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Speed traps (Claude's own addition, 30 Sep; docs/51): a camera on the straightest stretch of seven roads across
    // Bayview and Hillcrest. Drive through one and it flashes your speed; its three star marks pay credits once each,
    // and your best through every camera stays in the save.
    //   The marks come from the road itself: along each approach (both directions) a forward pass works out the
    //   fastest speed a car can reach at the camera from its real acceleration envelope, the road's curvature and
    //   grade and its tyre grip. One star sits just under what the stock car can do, two under the Street kit, three
    //   under the best Class C parts in every slot, so the stars follow the balance and the roads if either changes.
    public sealed class SpeedTraps:MonoBehaviour
    {
        public sealed class Trap
        {
            public string id,name;public Vector3 at,forward;public float half;public int[] marks=new int[3];public Transform camera;
            // The road leading in (from 300 m back to the camera), from the side the stock car is fastest.
            public List<Vector3> approach=new();
            public float Along(Vector3 p)=>Vector3.Dot(p-at,forward);
        }
        public static readonly string[] Ids={"coast","causeway","valley","quay","farm","falls","raceway","quarryroad","bramble"};
        // The cameras this run places (Quarry Road's only with --quarry-hills).
        public static string[] ActiveIds=>Array.FindAll(Ids,id=>(id!="quarryroad"||QuarryHills.Enabled)&&(id!="bramble"||Bramblewick.Enabled));
        public static bool Known(string id)=>Array.IndexOf(Ids,id)>=0;
        public static readonly int[] Prizes={50,100,150};
        public static readonly List<Trap> All=new();
        public const float Mph=2.23694f;

        RoadCar player;Func<AdventureStore> store;Action<string,string> card;Vector3 last;bool hasLast;float cooldown;
        readonly List<(Light light,float until)> flashes=new();

        // The roads (Bayview's loop is the race circuit), each limited to where a camera makes sense.
        static IEnumerable<(string id,string name,Vector3[] line,bool closed,float half,Func<Vector3,bool> allowed)> Roads(Circuit bayview)
        {
            yield return ("coast","Coast Road",bayview.points,true,Circuit.Width*.5f,p=>true);
            yield return ("causeway","The Causeway",HillcrestRoute.Mainline.line,false,HillcrestRoute.Mainline.width*.5f,p=>p.z<-190&&p.z>-430);
            yield return ("valley","Valley Road",HillcrestRoute.Mainline.line,false,HillcrestRoute.Mainline.width*.5f,p=>p.z<-760);
            yield return ("quay","Quay Straight",HillcrestRoute.Quay.line,false,HillcrestRoute.Quay.width*.5f,p=>true);
            yield return ("farm","Farm Lane",HillcrestRoute.FarmLane.line,false,HillcrestRoute.FarmLane.width*.5f,p=>true);
            yield return ("falls","Falls Road",HillcrestRoute.Falls.line,false,HillcrestRoute.Falls.width*.5f,p=>true);
            yield return ("raceway","Raceway Road",HillcrestRoute.RacewayAccess.line,false,HillcrestRoute.RacewayAccess.width*.5f,p=>true);
            // Quarry Hills (--quarry-hills): the run down from the crest towards the town.
            if(QuarryHills.Enabled){var q=HillcrestRoute.Find(QuarryHills.QuarryRoadName);yield return ("quarryroad","Quarry Road",q.line,false,q.width*.5f,p=>p.x<-1500&&p.x>-2300);}
            // Bramblewick (--bramblewick, docs/82): Valley Road South's straightest run across the vale, outside the village.
            if(Bramblewick.Enabled){var b=HillcrestRoute.Find(Bramblewick.RoadName);yield return ("bramble","Vale Straight",b.line,false,b.width*.5f,p=>p.x>900&&Vector2.Distance(new(p.x,p.z),Bramblewick.Centre)>420);}
        }

        // Where the cameras go and their marks. Pure geometry and the drive model: the editor checks call it too.
        public static List<Trap> Plan(Circuit bayview)
        {
            var traps=new List<Trap>();
            var kits=new[]{EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("stock")),EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("street")),EquipmentCatalog.Stats(BestOfClass("C"))};
            foreach(var (id,name,line,closed,half,allowed) in Roads(bayview)) {
                int n=line.Length;var run=Run(line,closed);float total=run[^1];
                // The straightest point (least heading change within 70 m either way), clear of road ends.
                int best=-1;float bestTurn=float.MaxValue;
                for(int i=0;i<n;i++) {
                    if(!allowed(line[i]))continue;
                    if(!closed&&(run[i]<90||total-run[i]<90))continue;
                    float turn=Turning(line,run,closed,i,70);if(turn<bestTurn){bestTurn=turn;best=i;}
                }
                if(best<0)continue;
                var f=Tangent(line,best,closed);
                var trap=new Trap{id=id,name=name,at=line[best],forward=f,half=half};
                for(int k=0;k<3;k++){float reach=Mathf.Max(Reach(line,run,closed,best,1,kits[k]),Reach(line,run,closed,best,-1,kits[k]));trap.marks[k]=Mathf.FloorToInt(reach*Mph*(k==2?.95f:.96f));}
                int way=Reach(line,run,closed,best,1,kits[0])>=Reach(line,run,closed,best,-1,kits[0])?1:-1;trap.approach=Back(line,closed,best,way,300);
                trap.marks[1]=Mathf.Max(trap.marks[1],trap.marks[0]+3);trap.marks[2]=Mathf.Max(trap.marks[2],trap.marks[1]+3);
                traps.Add(trap);
            }
            return traps;
        }
        // The dearest part of a class in every slot (stock where the class has none).
        public static Dictionary<string,string> BestOfClass(string cls)
        {
            var fit=EquipmentCatalog.StockLoadout();
            foreach(string slot in EquipmentCatalog.All.slots){var top=EquipmentCatalog.All.parts.Where(p=>p.slot==slot&&(p.tier??"C")==cls).OrderByDescending(p=>p.price).FirstOrDefault();if(top!=null)fit[slot]=top.id;}
            return fit;
        }
        static float[] Run(Vector3[] line,bool closed){var r=new float[line.Length+(closed?1:0)];for(int i=1;i<r.Length;i++)r[i]=r[i-1]+Vector3.Distance(line[i%line.Length],line[i-1]);return r;}
        static Vector3 Tangent(Vector3[] line,int i,bool closed)
        {
            int n=line.Length;int a=closed?(i+n-2)%n:Mathf.Max(0,i-2),b=closed?(i+2)%n:Mathf.Min(n-1,i+2);var f=line[b]-line[a];f.y=0;return f.normalized;
        }
        // Heading change (degrees) summed over `reach` metres either side.
        static float Turning(Vector3[] line,float[] run,bool closed,int i,float reach)
        {
            float sum=0;var prev=Tangent(line,i,closed);
            foreach(int dir in new[]{1,-1}){int j=i;float d=0;var h=prev;int n=line.Length;
                while(d<reach){int k=closed?(j+dir+n)%n:j+dir;if(k<0||k>=n)return float.MaxValue;d+=Vector3.Distance(line[j],line[k]);j=k;var t=Tangent(line,j,closed);sum+=Vector3.Angle(h,t);h=t;}}
            return sum;
        }
        // The road `length` metres back from point i, arriving along dir, in driving order.
        static List<Vector3> Back(Vector3[] line,bool closed,int i,int dir,float length)
        {
            int n=line.Length;var path=new List<Vector3>{line[i]};int j=i;float back=0;
            while(back<length){int k=closed?(j-dir+n)%n:j-dir;if(k<0||k>=n)break;back+=Vector3.Distance(line[j],line[k]);j=k;path.Add(line[j]);}
            path.Reverse();return path;
        }
        // The fastest a car can be going at point i, arriving along direction dir (+1: with the line), from 700 m back.
        static float Reach(Vector3[] line,float[] run,bool closed,int i,int dir,CarTuning t)
        {
            int n=line.Length;var path=new List<Vector3>();int j=i;float back=0;
            path.Add(line[j]);
            while(back<700){int k=closed?(j-dir+n)%n:j-dir;if(k<0||k>=n)break;back+=Vector3.Distance(line[j],line[k]);j=k;path.Add(line[j]);}
            path.Reverse();if(path.Count<3)return 0;
            float budget=t.roadGrip*1.65f,v=0;
            for(int s=1;s<path.Count;s++) {
                var a=path[s-1];var b=path[s];float ds=Mathf.Max(.01f,Vector3.Distance(a,b));
                float curve=0;if(s+1<path.Count){var u=b-a;var w=path[s+1]-b;u.y=w.y=0;curve=Vector3.Angle(u,w)*Mathf.Deg2Rad/Mathf.Max(.5f,(u.magnitude+w.magnitude)*.5f);}
                float corner=curve>1e-4f?Mathf.Sqrt(budget*.64f/curve):float.MaxValue;
                DriveModel.Forces(t,v,1,0,t.roadGrip,9.81f*(b.y-a.y)/ds,out float accel,out _);
                v=Mathf.Sqrt(Mathf.Max(0,v*v+2*accel*ds));v=Mathf.Min(v,Mathf.Min(corner,t.AttainableSpeed));
            }
            return v;
        }

        // ---------------------------------------------------------------- in the world
        public void Setup(RoadCar car,Circuit bayview,Func<AdventureStore> saves,Action<string,string> showCard)
        {
            player=car;store=saves;card=showCard;All.Clear();All.AddRange(Plan(bayview));
            var root=new GameObject("Speed traps").transform;
            var pole=TrialWorld.Material("Trap pole",new Color(.36f,.38f,.42f),.4f,.35f);var housing=TrialWorld.Material("Trap housing",Toy.Sun,0,.3f);
            var lens=TrialWorld.Material("Trap lens",new Color(.08f,.09f,.12f),.2f,.8f);var white=TrialWorld.Material("Trap strip white",new Color(.95f,.95f,.93f),0,.2f);var dark=TrialWorld.Material("Trap strip dark",new Color(.12f,.13f,.16f),0,.2f);
            void Build(Trap trap) {
                var holder=new GameObject("Speed trap "+trap.name).transform;holder.SetParent(root);
                var right=Vector3.Cross(Vector3.up,trap.forward);
                // Chequered strip across the road: where the camera measures.
                int tiles=Mathf.RoundToInt(trap.half*2/1f);
                // (Street review, 5 Oct: dark squares vanished into the asphalt, so the line read as stray white patches.) A white
                // band from kerb to kerb, the chequer's dark squares on it, on the highest road surface under each.
                for(int k=0;k<tiles;k++){var p=trap.at+right*(-trap.half+.5f+k);p.y=Surface(p)+.045f;Block(holder,"Trap strip",p,trap.forward,new Vector3(1.02f,.01f,.9f),white);
                    if(k%2==1)Block(holder,"Trap chequer",p+Vector3.up*.004f,trap.forward,new Vector3(1,.01f,.45f),dark);}
                // The camera on a pole beside the road, on whichever side has ground.
                var tries=new[]{(1,0f),(-1,0f),(1,7f),(-1,7f),(1,-7f),(-1,-7f)};
                for(int pass=0;pass<2&&trap.camera==null;pass++)foreach(var (side,slide) in tries) {
                    var at=trap.at+right*side*(trap.half+2.2f)+trap.forward*slide;float g=Ground(at);
                    if(float.IsNaN(g)||Mathf.Abs(g-trap.at.y)>2.5f)continue;at.y=g;
                    // (Street review, 5 Oct: on Quay road the pole stood beside a front door.) Not within 4 m of a building.
                    if(pass==0&&BayviewKit.Occupied(new Bounds(at+Vector3.up*2+right*side*2.5f,new Vector3(4,3,4))))continue; // (second pass: anywhere with ground)
                    Block(holder,"Trap pole",at+Vector3.up*2,trap.forward,new Vector3(.18f,4,.18f),pole);
                    var head=Block(holder,"Trap camera",at+Vector3.up*4.1f-right*side*.25f,trap.forward,new Vector3(.55f,.5f,.9f),housing);
                    Block(head,"Trap lens",head.position-right*side*.3f,trap.forward,new Vector3(.12f,.34f,.34f),lens,true);
                    Block(holder,"Trap band",at+Vector3.up*1.2f,trap.forward,new Vector3(.24f,.35f,.24f),housing);
                    trap.camera=head;break;
                }
            }
            // (A trap in a district still building in the background goes up once it's built: its camera keeps clear of the
            // buildings, Districts.)
            foreach(var trap in All){if(Districts.Waiting("quarry-hills")&&QuarryHills.Contains(trap.at.x,trap.at.z)){var later=trap;Districts.WhenBuilt("quarry-hills",()=>Build(later));continue;}Build(trap);}
            Debug.Log("LITTLE_ROAD_SPEED_TRAPS "+string.Join(" ",All.Select(t=>$"{t.id}@({t.at.x:0},{t.at.z:0}) marks {t.marks[0]}/{t.marks[1]}/{t.marks[2]} mph")));
        }
        static float Surface(Vector3 p){float best=float.NaN;foreach(var h in Physics.RaycastAll(p+Vector3.up*6,Vector3.down,14))if(h.collider.GetComponentInParent<RoadSurface>()!=null&&(float.IsNaN(best)||h.point.y>best))best=h.point.y;return float.IsNaN(best)?p.y+.03f:best;}
        static float Ground(Vector3 p){float best=float.NaN;foreach(var h in Physics.RaycastAll(p+Vector3.up*8,Vector3.down,30))if(!h.collider.isTrigger&&(float.IsNaN(best)||h.point.y>best))best=h.point.y;return best;}
        static Transform Block(Transform parent,string name,Vector3 at,Vector3 forward,Vector3 size,Material material,bool local=false)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent,false);go.transform.SetPositionAndRotation(at,Quaternion.LookRotation(forward));go.transform.localScale=local?Vector3.Scale(size,new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z)):size;
            go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }

        public static TrapRecord Record(AdventureProgress p,string id)=>p.traps?.Find(t=>t.id==id);
        public static int Stars(Trap t,float mph){int s=0;for(int k=0;k<3;k++)if(mph>=t.marks[k])s=k+1;return s;}
        void FixedUpdate()
        {
            if(player==null||player.controlsBlocked){hasLast=false;return;}
            var now=player.body.position;cooldown-=Time.fixedDeltaTime;
            if(hasLast&&cooldown<=0)foreach(var trap in All) {
                float a=trap.Along(last),b=trap.Along(now);if(Mathf.Sign(a)==Mathf.Sign(b)||Mathf.Abs(a-b)>8)continue;
                var mid=Vector3.Lerp(last,now,Mathf.Abs(a)/Mathf.Max(1e-4f,Mathf.Abs(a-b)));var off=mid-trap.at;
                if(Mathf.Abs(Vector3.Dot(off,Vector3.Cross(Vector3.up,trap.forward)))>trap.half+1.5f||Mathf.Abs(off.y)>4)continue;
                Snap(trap,player.body.linearVelocity.magnitude*Mph);cooldown=1.5f;break;
            }
            last=now;hasLast=true;
        }
        void Snap(Trap trap,float mph)
        {
            var saves=store();var before=Record(saves.Data,trap.id);float previous=before?.best??0;int stars=Stars(trap,mph),had=before?.stars??0;
            int paid=0;try{paid=saves.RecordTrap(trap.id,mph,stars,Prizes);}catch(Exception e){Debug.LogException(e);}
            string starText=stars==0?"No stars yet":stars==1?"1 star":$"{stars} stars";
            string detail=stars<3?$"{starText}  ·  next star at {Units.ShowMph(trap.marks[stars])}":$"{starText}  ·  top marks!";
            if(paid>0)detail+=$"  ·  +{paid} credits";else if(mph>previous+.05f&&previous>0)detail+=$"  ·  new best (was {Units.FromMph(previous):0})";else if(previous>0)detail+=$"  ·  best {Units.FromMph(previous):0}";
            card?.Invoke($"{trap.name.ToUpperInvariant()}  {Units.ShowMph(mph).ToUpperInvariant()}",detail);
            // The flash and the shutter: two quick high beeps.
            if(trap.camera!=null){var l=new GameObject("Trap flash").AddComponent<Light>();l.type=LightType.Point;l.range=14;l.intensity=6;l.color=new Color(1,.97f,.9f);l.transform.position=trap.camera.position;flashes.Add((l,Time.time+.12f));}
            AudioDirector.Beep(1568,.05f);AudioDirector.Beep(2093,.09f);
            if(stars>had)Confetti.Burst(player.body.position+Vector3.up*2);
            Debug.Log($"LITTLE_ROAD_SPEED_TRAP {trap.id} mph={mph:0.0} stars={stars} paid={paid}");
        }
        void Update(){for(int i=flashes.Count-1;i>=0;i--)if(Time.time>flashes[i].until){if(flashes[i].light!=null)Destroy(flashes[i].light.gameObject);flashes.RemoveAt(i);}}
    }
}
