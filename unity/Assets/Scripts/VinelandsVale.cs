using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 7 (docs/92), with zone 1's machinery (VinelandsVila.cs): Eiras and the Vale, the west side of the ridge.
    // The workers' and farmers' cottages on the lip below the Largo with their threshing floor; the Vale shelf falling west:
    // the kitchen gardens along the brook, the almond orchards, the olive grove on the stony north-west flank, the cork-oak
    // montado on the poor south-west ground, the farmsteads; and on the shelf, the Circuito da Cumeada, the club circuit on
    // farm lanes, with its race day (the start, kerbs, bale walls, corner boards, spectators' cars) and its paddock in the
    // infield, entered from the Caminho Velho do Vale.
    public sealed partial class VinelandsVila
    {
        void Zone7(){Eiras();Vale();Clube();}
        // the long lap's own corners (their boards and bales go on its line), and the heavy stops (bale walls on the outside)
        static readonly string[] LongOnly={"Curva da Ribeira","Esses da Ribeira","Recta das Amendoeiras","Alto da Ribeira","Gancho do Casal","Chicane do Lagar","Caracol","Esses do Pombal","Curva da Adega","Curva do Palheiro"};
        static readonly string[] HeavyStops={"Curva da Eira Velha","Curva do Carvalhal","Curva das Courelas","Gancho do Sobreiro","Curva da Ribeira","Gancho do Casal","Caracol","Curva do Palheiro"};
        // (field walls keep off the junctions' corners, which lie beyond both roads' lines: a stub on the Eira Velha's inside corner stuck
        // five cars, second design round 2)
        static bool NearNode(Vector3 p,float d){if(Vinelands.CircuitNodes==null)return false;foreach(var j in Vinelands.CircuitNodes)if(Vector2.Distance(new(p.x,p.z),new(j.x,j.z))<d)return true;return false;}
        bool InPolyF(Vector2[] poly,Vector2 p){bool c=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)if((poly[i].y>p.y)!=(poly[j].y>p.y)&&p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)c=!c;return c;}
        // trees on a jittered grid inside a frame polygon, clear of roads, buildings and the circuit's run-off
        int Grove(Vector2[] poly,float step,string id,float scaleMin,float scaleMax,int salt,float keep=1,BayviewKit.Tint tint=null)
        {
            float a0=float.MaxValue,a1=float.MinValue,n0=float.MaxValue,n1=float.MinValue;foreach(var q in poly){a0=Mathf.Min(a0,q.x);a1=Mathf.Max(a1,q.x);n0=Mathf.Min(n0,q.y);n1=Mathf.Max(n1,q.y);}
            int placed=0;
            for(float a=a0;a<=a1;a+=step)for(float n=n0;n<=n1;n+=step) {
                if(Hash(a,n,salt)>keep)continue;var f=new Vector2(a+(Hash(a,n,salt+1)-.5f)*step*.6f,n+(Hash(a,n,salt+2)-.5f)*step*.6f);if(!InPolyF(poly,f))continue;
                var p=FV(f);if(!Clear(p,4f,true))continue;
                if(Put(id,new Vector3(p.x,p.y-.05f,p.z),Hash(a,n,salt+3)*360,tint,null,true,scaleMin+(scaleMax-scaleMin)*Hash(a,n,salt+4))!=null){placed++;if(placed%3==0)AreaAudit.ExtraNature.Add(p);}
            }
            return placed;
        }

        // ---- Eiras: the cottages along the Caminho Velho do Vale below the Largo, low, balconies to the sunset; the Eira Velha
        void Eiras()
        {
            var vv=HillcrestRoute.Find(Vinelands.ValeRoadName);if(vv==null)return;var erng=new System.Random(9201);string[] ids={"casa_douro_c","casa_douro_d","casa_douro_a","casa_douro_d"};int placed=0;
            foreach(int side in new[]{1,-1})for(float s=70;s<vv.Length-120;) {
                string id=ids[erng.Next(ids.Length)];var size=Footprint(id);
                if(Front(vv,s+size.x*.5f,side,id,3f,House(erng.NextDouble()<.25?"azulejo":"granito"),null,4f,3.5f)){placed++;var at=vv.At(s+size.x*.5f);var n=vv.Right(s+size.x*.5f);n.y=0;n.Normalize();n*=side;var c=at+n*(vv.width*.5f+3+size.y*.5f);yards.Add((c+n*(size.y*.5f),n,Quaternion.Euler(0,90,0)*n,size.x,"horta"));}
                s+=size.x+4+(float)erng.NextDouble()*8;
            }
            {var e=F(-190,95);Building("quinta_eira",e,0,null,null,1.5f,4.4f,1.5f);}
            {float s=SNear(vv,new(-250,60));Front(vv,s,SideOf(vv,s,new(-250,90)),"quinta_alpendre",4f,null,null,4f,4f);}   // (the carpenter's shed)
            Debug.Log($"LITTLE_ROAD_VINELANDS_EIRAS houses={placed}");
        }

        // ---- the Vale shelf's land, by what grows where (the town plan's section 5)
        void Vale()
        {
            int hortas=0;
            // the Hortas da Ribeira: kitchen plots along the brook from the Fonte Velha, reeds along the water's line
            var hr=new Vector2[]{new(-300,-230),new(-470,-350),new(-780,-440),new(-780,-360),new(-480,-290),new(-300,-140)};
            for(float a=-780;a<=-300;a+=9)for(float n=-440;n<=-140;n+=7) {
                if(Hash(a,n,70)<.3f)continue;var f=new Vector2(a+(Hash(a,n,71)-.5f)*3,n);if(!InPolyF(hr,f))continue;var p=FV(f);if(!Clear(p,3f,true))continue;
                if(Hash(a,n,72)<.12f){if(BayviewKit.Has("reeds"))Put("reeds",p,Hash(a,n,73)*360,null,null,false);continue;}
                {float lo=float.MaxValue,hi=float.MinValue;foreach(var (da,dn) in new[]{(-1.6f,-2.6f),(1.6f,-2.6f),(-1.6f,2.6f),(1.6f,2.6f)}){var q=FV(f+new Vector2(da,dn));lo=Mathf.Min(lo,q.y);hi=Mathf.Max(hi,q.y);}if(hi-lo>1f)continue;p.y=lo+.02f;}   // (on level enough ground: one hung off the slope by the start, round 2)
                if(Put("vila_horta",p,90+(Hash(a,n,74)-.5f)*12,null,null,false)!=null){hortas++;if(hortas%2==0)AreaAudit.ExtraNature.Add(p);}
            }
            // the almond and fruit orchards; the olive grove on the north-west flank; the cork-oak montado
            int almonds=Grove(new Vector2[]{new(-980,300),new(-720,300),new(-560,430),new(-520,640),new(-980,640)},11,"tree_blossom",.8f,1.1f,80,.85f);
            int olives=Grove(new Vector2[]{new(-700,1100),new(-400,1060),new(-270,860),new(-310,740),new(-460,660),new(-700,700)},12,"olive_tree",.8f,1.1f,90,.8f);
            int cork=Grove(new Vector2[]{new(-1200,-380),new(-900,-380),new(-640,-600),new(-660,-1000),new(-700,-1300),new(-1200,-1320)},20,"tree_round",1.1f,1.6f,100,.6f,T(0xffffff,0xffffff,0xffffff,0xffffff));
            // the open fields between: rye stubble, pasture, fresh plough, in plots that grow from 0.2 ha near the town to 2-3 ha
            // out west, each walled in dry stone along its two downhill sides (the town plan's courelas), clear of the orchards
            int fields=0;var stub=FieldMat("Rye stubble",new Color(.78f,.68f,.36f));var past=FieldMat("Pasture",new Color(.36f,.52f,.22f));var plough=FieldMat("Plough",new Color(.52f,.40f,.28f));
            var keep=new[]{new Vector2[]{new(-980,300),new(-720,300),new(-560,430),new(-520,640),new(-980,640)},hr,new Vector2[]{new(-1200,-380),new(-900,-380),new(-640,-600),new(-660,-1000),new(-700,-1300),new(-1200,-1320)}};
            // (columns of plots west from the ridge's foot: wider and deeper further out)
            float fa=-1180;
            while(fa<-400) {
                float far=Mathf.InverseLerp(-400,-1150,fa);float w=40+60*far+(Hash(fa,0,130)-.5f)*16;float fn=-360;
                while(fn<720) {
                    float d=34+50*far+(Hash(fa,fn,131)-.5f)*14;var c=new Vector2(fa+w*.5f,fn+d*.5f);fn+=d+6;
                    bool skip=Hash(fa,fn,132)<.25f;foreach(var poly in keep)if(InPolyF(poly,c))skip=true;if(skip)continue;
                    bool clear=true;for(int gi=0;gi<5&&clear;gi++)for(int gj=0;gj<5;gj++){var q=FV(c+new Vector2((gi/4f-.5f)*w,(gj/4f-.5f)*d));if(!Clear(q,6f,true)){clear=false;break;}}if(!clear)continue;   // (on a 5 x 5 grid: a road could pass between the corners under a 2 ha field)
                    float r=Hash(fa,fn,133);Field("Vale field "+fields,c-new Vector2(w*.5f-3,d*.5f-3),c+new Vector2(w*.5f-3,d*.5f-3),r<.4f?stub:r<.75f?past:plough);fields++;
                    if(r<.4f&&BayviewKit.Has("hay_bale"))for(int k=0;k<3;k++){var q=FV(c+new Vector2((Hash(fa,k,134)-.5f)*w*.6f,(Hash(fn,k,135)-.5f)*d*.6f));Put("hay_bale",q,Hash(fa,fn+k,136)*360,null,null,true,1f);}
                    // (its dry-stone walls along its west and south sides, broken where they meet a road or a way)
                    var corners=new[]{c+new Vector2(-w*.5f,d*.5f),c+new Vector2(-w*.5f,-d*.5f),c+new Vector2(w*.5f,-d*.5f)};
                    for(int sg=0;sg<2;sg++){var pts=new List<Vector3>();int k2=Mathf.CeilToInt(Vector2.Distance(corners[sg],corners[sg+1])/2);for(int i=0;i<=k2;i++){var q=FV(Vector2.Lerp(corners[sg],corners[sg+1],i/(float)k2));if(OnWay(q)||NearNode(q,40)||ValleyField.RoadDistance(q.x,q.z)<Vinelands.LoopWidth*.5f+4){LayWall(pts);pts=new List<Vector3>();}else pts.Add(q);}LayWall(pts);}
                }
                fa+=w+6;
            }
            Debug.Log($"LITTLE_ROAD_VINELANDS_FIELDS fields={fields}");
            // the farmsteads: a farmhouse, its barn, a threshing floor, a fig
            foreach(var (a,n,name) in new[]{(-1040f,160f,"Casal do Carvalhal"),(-575f,445f,"Casal das Amendoeiras"),(-1120f,-480f,"Casal do Sobreiro"),(-1075f,525f,"Casal da Ribeira")}) {
                var h=F(a,n);if(!Building("casa_douro_c",h,(a*n)%360,House("granito"),null,4f,4.4f,4f))continue;
                var b=F(a+18,n+6);if(BayviewKit.Has("barn"))Building("barn",b,Yaw(b,h),null,null,4f,4.4f,4f);
                var e=F(a-6,n-20);Building("quinta_eira",e,0,null,null,1.5f,4.4f,1.5f);
                var g=F(a-14,n+10);Put("vila_figueira",new Vector3(g.x,g.y-.05f,g.z),Hash(a,n,110)*360);
                for(int k=0;k<4;k++){var q=F(a+30+k*6,n-14);if(Clear(q,1,true))Put("vila_horta",q,0,null,null,false);}
            }
            Debug.Log($"LITTLE_ROAD_VINELANDS_VALE hortas={hortas} almonds={almonds} olives={olives} cork={cork}");
        }

        // a field's surface: a 2 m grid on the ground itself (+6 cm), nothing lifted (the paving's filters raised fields on a
        // slope into cushions, round 4)
        void Field(string name,Vector2 min,Vector2 max,Material m)
        {
            const float q=2f;int nx=Mathf.Max(1,Mathf.CeilToInt((max.x-min.x)/q)),nz=Mathf.Max(1,Mathf.CeilToInt((max.y-min.y)/q));var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){var w=Vinelands.Centre+new Vector2(Mathf.Lerp(min.x,max.x,i/(float)nx),Mathf.Lerp(min.y,max.y,j/(float)nz));v.Add(new Vector3(w.x,H(w.x,w.y)+.06f,w.y));uv.Add(new Vector2(w.x/6f,w.y/6f));}
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int k=j*(nx+1)+i;t.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            if(mesh.normals[0].y<0){for(int i=0;i<t.Count;i+=3)(t[i+1],t[i+2])=(t[i+2],t[i+1]);mesh.SetTriangles(t,0);mesh.RecalculateNormals();}
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;go.isStatic=true;
        }
        static readonly Dictionary<string,Material> fieldMats=new();
        static Material FieldMat(string name,Color c){if(fieldMats.TryGetValue(name,out var m)&&m!=null)return m;m=WorldLook.Kit("Vale "+name,c,WorldLook.Pattern.Leaf,0,.15f,true);fieldMats[name]=m;return m;}
        // ---- race day's layout (in a race only): the road the race doesn't use is closed with a wall of bales across it, and a
        // chevron board on the outside shows the way. The short lap: the long lap's road shut 70 m up the Eira Velha's escape
        // road, the chevrons pointing left; and at the Palheiro, the long lap's arm shut. The long lap: the short lap's road
        // shut past both its Ts, the chevrons at the Palheiro pointing right. (Richard: "how do I know to turn on the short
        // track?" Until now nothing told a driver.)
        public static void RaceDay(bool longLap)
        {
            var nodes=Vinelands.CircuitNodes;if(nodes==null||!BayviewKit.Has("hay_bale"))return;var root=new GameObject("Cumeada race day").transform;
            Vector3 D(float heading)=>new(Mathf.Cos(heading*Mathf.Deg2Rad),0,Mathf.Sin(heading*Mathf.Deg2Rad));
            // (on the road where it crosses one, at its line's own height: the ray found the ground under the road's embankment
            // while the world was building, and the bales stood 1.5 m deep in the road, first pictures)
            var lines=new[]{HillcrestRoute.Find(Vinelands.CircuitName)?.line,HillcrestRoute.Find(Vinelands.ExtName)?.line};
            Vector3 OnTop(Vector3 p){if(Physics.Raycast(new Vector3(p.x,p.y+60,p.z),Vector3.down,out var hit,140,~0,QueryTriggerInteraction.Ignore))p.y=hit.point.y;else p.y=ValleyField.Height(p.x,p.z);
                foreach(var l in lines)if(l!=null)foreach(var q in l)if(new Vector2(q.x-p.x,q.z-p.z).sqrMagnitude<Vinelands.LoopWidth*Vinelands.LoopWidth*.36f)p.y=Mathf.Max(p.y,q.y+.03f);return p;}
            void Close(Vector3 node,Vector3 dir,float d){var c=node+dir*d;var across=Vector3.Cross(Vector3.up,dir);float yaw=Quaternion.LookRotation(across).eulerAngles.y;for(float x=-7;x<=7.01f;x+=1.75f)BayviewKit.Spawn("hay_bale",OnTop(c+across*x),yaw,root,null,null,true,1.15f);}
            var boardMat=TrialWorld.Material("Cumeada chevron board",Toy.Hex(0x4a0f18),0,.3f);var postMat=TrialWorld.Material("Board post",new Color(.3f,.3f,.32f),0,.3f);
            void Cube(Vector3 at,Vector3 size,Quaternion rot,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Cumeada chevron";g.transform.SetParent(root);g.transform.SetPositionAndRotation(at,rot);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;Object.Destroy(g.GetComponent<Collider>());}
            // (f: the forward of the cars that read it; not solid, as the boards in the run-off)
            void Chevrons(Vector3 at,Vector3 f,string arrows){f.y=0;f.Normalize();at=OnTop(at);var across=Vector3.Cross(Vector3.up,f);
                float bw=Mathf.Max(6.6f,arrows.Length*.82f);foreach(int s in new[]{-1,1})Cube(at+across*s*(bw*.5f-.4f)+Vector3.up*1.6f,new Vector3(.16f,3.2f,.16f),Quaternion.identity,postMat);
                Cube(at+Vector3.up*3.1f,new Vector3(bw,1.7f,.08f),Quaternion.LookRotation(-f),boardMat);
                var go=new GameObject("Sign / "+arrows);go.transform.SetParent(root);go.transform.SetPositionAndRotation(at+Vector3.up*3.1f-f*.05f,Quaternion.LookRotation(f));var tm=go.AddComponent<TextMesh>();tm.text=arrows;tm.anchor=TextAnchor.MiddleCenter;tm.fontSize=96;tm.characterSize=.16f;tm.color=new Color(.98f,.95f,.82f);tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=tm.font.material;go.AddComponent<WorldText>();}
            Vector3 eira=nodes[0],pal=nodes[1];
            if(!longLap) {
                // (straight on at the Eira Velha is the escape road: the board at its mouth, the bales 22 m behind it; at 46 m the board
                // read small from the straight, first pictures)
                Close(eira,D(90),60);Chevrons(eira+D(90)*38,D(90),"<  SHORT LAP  <");
                Close(pal,D(110),62);
            } else {
                // (the closed road at the Eira Velha is round the corner behind its wall: a board on the right of the way on, seen from
                // the straight, and the bales right at the arm's mouth)
                Close(eira,D(190),34);Chevrons(eira+D(90)*40+D(0)*(Vinelands.LoopWidth*.5f+5),D(90),"^  LONG LAP  ^");
                Close(pal,D(-10),46);
                Chevrons(pal+D(-90)*15+D(-10)*12,D(290),">  LONG LAP  >");   // (on the Palheiro's outside, facing the cars coming down from the Casal)
            }
        }

        // ---- the Clube Automovel da Cumeada: the circuit's race day and its paddock
        void Clube()
        {
            if(Vinelands.Lap==null)return;
            var track=new Circuit(Vinelands.Lap,Vinelands.LoopWidth);var longT=new Circuit(Vinelands.LapLong,Vinelands.LoopWidth);float half=Vinelands.LoopWidth*.5f;
            Material Mat(string name,Color c)=>TrialWorld.Material(name,c,0,.3f);
            var white=Mat("Cumeada paint",new Color(.96f,.96f,.92f));var red=Mat("Cumeada kerb red",new Color(.84f,.2f,.17f));var dark=Mat("Cumeada chequer",new Color(.1f,.1f,.12f));
            GameObject Block(string name,Vector3 at,Vector3 size,Quaternion rot,Material m,bool solid){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.SetPositionAndRotation(at,rot);g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;if(!solid)Object.Destroy(g.GetComponent<Collider>());else g.tag="Respawn";return g;}
            var nodes=Vinelands.CircuitNodes;
            bool NearJunction(Vector3 p,float d){foreach(var j in nodes)if(Vector2.Distance(new(p.x,p.z),new(j.x,j.z))<d)return true;return false;}
            // the start: chequers across the main straight, grid boxes, the gantry with its banner
            {var s0=track.At(0);var fw=track.Forward(0);fw.y=0;fw.Normalize();var r0=Vector3.Cross(Vector3.up,fw);
                for(int i=0;i<9;i++)for(int j=0;j<2;j++)Block("Cumeada start line",s0+r0*(-4f+i)+fw*j*.5f+Vector3.up*.05f,new Vector3(1,.02f,.5f),Quaternion.LookRotation(fw),(i+j)%2==0?white:dark,false);
                for(int slot=0;slot<24;slot++){float d=RaceGrid.Distance(slot,false);var p=RaceGrid.Position(track,slot,false);var f=track.Forward(d).normalized;var fl=f;fl.y=0;fl.Normalize();var rr=Vector3.Cross(Vector3.up,fl);   // (along the road's slope: flat on the 8 % straight, their ends stood off it)
                    Vector3 OnRoad(Vector3 q){q.y=Top(q)+.03f;return q;}var front=OnRoad(p+fl*2.3f);
                    Block("Cumeada grid box",front,new Vector3(2.3f,.02f,.16f),Quaternion.LookRotation(f),white,false);foreach(int leg in new[]{-1,1})Block("Cumeada grid box",OnRoad(front+rr*leg*1.07f-fl*.6f),new Vector3(.16f,.02f,1.2f),Quaternion.LookRotation(f),white,false);}
                var green=Mat("Cumeada gantry",Toy.Hex(0x7a1f2b));var banner=Mat("Cumeada banner",new Color(.97f,.95f,.88f));
                foreach(int side in new[]{-1,1})Block("Cumeada gantry post",s0+r0*side*7f+Vector3.up*3.2f,new Vector3(.45f,6.4f,.45f),Quaternion.identity,green,true);
                Block("Cumeada gantry",s0+Vector3.up*6.2f,new Vector3(14.4f,1.1f,.45f),Quaternion.LookRotation(fw),green,false);
                foreach(int face in new[]{-1,1}){Block("Cumeada gantry banner",s0+Vector3.up*6.2f+fw*face*.26f,new Vector3(10,.8f,.06f),Quaternion.LookRotation(fw),banner,false);
                    var go=new GameObject("Sign / CIRCUITO DA CUMEADA");go.transform.SetParent(root);go.transform.SetPositionAndRotation(s0+Vector3.up*6.2f+fw*face*.31f,Quaternion.LookRotation(-fw*face));var tm=go.AddComponent<TextMesh>();tm.text="CIRCUITO DA CUMEADA";tm.anchor=TextAnchor.MiddleCenter;tm.fontSize=80;tm.characterSize=.08f;tm.color=Toy.Hex(0x4a0f18);tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=tm.font.material;go.AddComponent<WorldText>();}}
            // kerbs inside every corner tighter than 75 m, on both laps (painted, flush, no collider: Bramblewick's lesson)
            int kerbs=0;var laid=new List<Vector3>();
            foreach(var tr in new[]{track,longT})for(float s=0;s<tr.length;s+=3.6f) {
                var a=tr.At(s);if(NearJunction(a,24))continue;bool done=false;foreach(var q in laid)if((q-a).sqrMagnitude<4){done=true;break;}if(done)continue;
                var f0=tr.Forward(s-10);var f1=tr.Forward(s+10);f0.y=f1.y=0;float turn=Vector3.SignedAngle(f0,f1,Vector3.up);float radius=20/Mathf.Max(.001f,Mathf.Abs(turn)*Mathf.Deg2Rad);if(radius>75)continue;
                var f=tr.Forward(s);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);int inside=turn>0?1:-1;var k=a+right*inside*(half+.35f);k.y=Mathf.Max(a.y,H(k.x,k.z))+.035f;
                Block("Cumeada kerb",k,new Vector3(.7f,.04f,3.4f),Quaternion.LookRotation(f),kerbs++%2==0?red:white,false);laid.Add(a);
            }
            // bale walls on the outside of the heavy stops; a board with each corner's name; spectators' cars at the best corners
            int bales=0,boards=0;
            foreach(var (name,at) in Vinelands.CircuitMarks) {
                if(name=="Recta da Eira"||name=="Mergulho"||name=="Subida"||name=="Recta das Amendoeiras")continue;   // (straights: no board)
                var tr=System.Array.IndexOf(LongOnly,name)>=0?longT:track;var w=Vinelands.Centre+at;float sc=tr.Nearest(new Vector3(w.x,0,w.y),out _);
                bool heavy=System.Array.IndexOf(HeavyStops,name)>=0;
                if(heavy&&BayviewKit.Has("hay_bale"))for(float s=sc;s<sc+70;s+=1.6f) {
                    var f0=tr.Forward(s-8);var f1=tr.Forward(s+8);f0.y=f1.y=0;float turn=Vector3.SignedAngle(f0,f1,Vector3.up);if(Mathf.Abs(turn)<4)continue;
                    var f=tr.Forward(s);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);int outside=turn>0?-1:1;var p=tr.At(s)+right*outside*(half+6.2f);
                    if(NearJunction(p,14)||ValleyField.RoadDistance(p.x,p.z)<half+5)continue;p.y=H(p.x,p.z);Put("hay_bale",p,Quaternion.LookRotation(f).eulerAngles.y,null,null,true,1.1f);bales++;
                }
                {   // the board, 40 m before the corner on its outside, facing the cars coming
                    float sb=sc-40;var f=tr.Forward(sb);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var f0=tr.Forward(sc-8);var f1=tr.Forward(sc+8);f0.y=f1.y=0;int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;
                    var p=tr.At(sb)+right*outside*(half+5.5f);if(!NearJunction(p,14)&&ValleyField.RoadDistance(p.x,p.z)>half+4){p.y=H(p.x,p.z);
                        var post=Mat("Board post",new Color(.3f,.3f,.32f));var face=Quaternion.LookRotation(-f);var across=Vector3.Cross(Vector3.up,f);float bw=name.Length*.36f+1f;
                        foreach(int sd in new[]{-1,1})Block("Sign post",p+across*sd*(bw*.42f)+Vector3.up*1.45f,new Vector3(.12f,2.9f,.12f),Quaternion.identity,post,false);
                        Block("Sign board",p+Vector3.up*2.75f,new Vector3(bw,1.05f,.07f),face,Mat("Cumeada board",Toy.Hex(0x4a0f18)),false);
                        var go=new GameObject("Sign / "+name);go.transform.SetParent(root);go.transform.SetPositionAndRotation(p+Vector3.up*2.75f-f*.045f,Quaternion.LookRotation(f));var tm=go.AddComponent<TextMesh>();tm.text=name.ToUpperInvariant();tm.anchor=TextAnchor.MiddleCenter;tm.fontSize=80;tm.characterSize=.058f;tm.color=new Color(.98f,.95f,.82f);tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=tm.font.material;go.AddComponent<WorldText>();boards++;}
                }
                if(name=="Gancho do Sobreiro"||name=="Curva da Eira Velha"||name=="Curva do Carvalhal"||name=="Gancho do Casal"||name=="Caracol") {   // (spectators: a row of cars on the outside bank)
                    var f=tr.Forward(sc+10);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var f0=tr.Forward(sc-8);var f1=tr.Forward(sc+8);f0.y=f1.y=0;int outside=Vector3.SignedAngle(f0,f1,Vector3.up)>0?-1:1;
                    var bank=tr.At(sc+10)+right*outside*(half+16);
                    for(int k=0;k<7;k++){var p=bank+f*((k-3)*3.4f);if(ValleyField.RoadDistance(p.x,p.z)<half+7||NearJunction(p,14))continue;p.y=H(p.x,p.z);
                        var car=TrialWorld.Car(new[]{"marlow","heron","picnic","doodle","otter","pebble","tallboy","dover","kipper","beetle"}[(int)(Hash(k,sc,120)*10)],PaintCatalog.All[(int)(Hash(k,sc,121)*PaintCatalog.Standard)].color);car.name="Parked car";car.transform.SetParent(root);car.transform.SetPositionAndRotation(p+Vector3.up*.02f,Quaternion.Euler(0,Yaw(p,tr.At(sc+10)),0));}
                }
            }
            // the landmarks the corners are named for: the old cork oak inside the Gancho do Sobreiro, the wayside cross outside the
            // Curva da Cruz
            foreach(var (name,at) in Vinelands.CircuitMarks) {
                if(name!="Gancho do Sobreiro"&&name!="Curva da Cruz")continue;var w=Vinelands.Centre+at;float sc=track.Nearest(new Vector3(w.x,0,w.y),out _);
                var f=track.Forward(sc);f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);var p=track.At(sc);
                if(name=="Gancho do Sobreiro"){var c=p+right*21f;c.y=H(c.x,c.z);Put("tree_round",new Vector3(c.x,c.y-.05f,c.z),40,T(0xffffff,0xffffff,0xffffff,0xffffff),null,true,2.4f);}
                else{var c=p+right*(half+7f);c.y=H(c.x,c.z);Put("vila_cruzeiro",c,Quaternion.LookRotation(-right).eulerAngles.y);}
            }
            // the paddock in the infield, west of the main straight: gravel, the clubhouse, awnings, the club's cars and trailers
            Plaza("Cumeada paddock",new Vector2(-625,-5),new Vector2(-535,62),Gravel(),true);   // (in the infield between the Recta da Eira and the Subida, 60 m off each: the second design)
            {var c=F(-600,45);c.y=H(c.x,c.z);Building("portela_saude",c,Yaw(c,F(-600,10)),T(0xf3eee2,0x7a1f2b,0x7a1f2b,0x7a1f2b),"CLUBE AUTOMOVEL DA CUMEADA",3f,1f,3f);}
            if(BayviewKit.Has("tent"))foreach(var (a,n) in new[]{(-612f,15f),(-597f,15f)}){var t=F(a,n);t.y=Top(t)+.05f;Put("tent",t,90,T(0xffffff,0x7a1f2b,0x7a1f2b,0xffffff),null,false);}
            if(BayviewKit.Has("market_stall")){var m=F(-548,45);m.y=Top(m)+.05f;Put("market_stall",m,90,T(0xffffff,0xffffff,0x7a1f2b,0xffffff),null,false);}
            for(int k=0;k<10;k++){var q=F(-585+(k%5)*8,20-(k/5)*14);q.y=Top(q)+.15f;Parking.Add((q,k/5==0?180:0));}
            {var q=F(-555,10);q.y=Top(q)+.1f;Spots["clube"]=(q,90);}
            Debug.Log($"LITTLE_ROAD_VINELANDS_CLUBE kerbs={kerbs} bales={bales} boards={boards}");
            if(RaceTrial.Has("--race-day"))RaceDay(RaceTrial.Arg("--race-day")=="long");   // (test views: the race-day closures in free roam, short|long)
        }
    }
}
