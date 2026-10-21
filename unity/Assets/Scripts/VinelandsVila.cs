using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 1: the Largo and Vila Velha (docs/86), from the town plan (review/street-review/vinelands-town-plan.md).
    // The oldest core: row houses on party walls fanning down the top of the amphitheatre from the Largo, in granite and
    // whitewash and azulejo (the vila kit, car-forge forge/world/kits/vila.py), each on its own floor as the lanes fall; the
    // Largo's frontages (the church, the town hall, the cafe, the post office and bank, the pastelaria and pharmacy); cobbled
    // lanes and stepped lanes (escadinhas); the oven's patio, the coopers' patio, the spring's square with its washing tanks;
    // the shrine, laundry across the lanes, cafe terraces, plane trees.
    public sealed class VinelandsVila
    {
        readonly Transform root;readonly System.Random rng=new(8601),dress=new(8602);
        readonly List<(Vector2 c,Vector2 half,float yaw)> footprints=new();Material plinth;
        public static int Houses,Refused;
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Spots=new();
        public static readonly List<(Vector3 at,float yaw)> Parking=new();
        public VinelandsVila(Transform parent){root=new GameObject("Vinelands Vila Velha").transform;root.SetParent(parent,false);}
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static Vector3 F(float a,float n){var w=Vinelands.Centre+new Vector2(a,n);return new Vector3(w.x,H(w.x,w.y),w.y);}
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return d.sqrMagnitude<1e-6f?0:Quaternion.LookRotation(d).eulerAngles.y;}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door,int? roof=null){var t=new BayviewKit.Tint(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));if(roof!=null)t.roof=Toy.Hex(roof.Value);return t;}
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,bool solid=true,float scale=1)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,solid,scale):null;

        // ---- the old core's palette: warm whitewash (most), the odd ochre or rose wash; azulejo fields in blue, green and ochre;
        // granite; doors and shutters in bottle green, ruby, tile blue, chestnut; Roman tile from fresh to sun-bleached
        static readonly int[] Whites={0xf7f3ea,0xf3eee2,0xf6f0e3,0xefe9dc,0xf4ece0,0xf8f5ee},Washes={0xe8c27a,0xedc9b4,0xe9d4a8,0xd9b48a};
        static readonly int[] Tiles={0x3a6aa6,0x2f5f95,0x3f7f6a,0x6b8fb8,0x2c4f86,0xc99a3a,0x4d8a7a};
        static readonly int[] Doors={0x2e5e3e,0x8b1d2c,0x2a5490,0x5a3a24,0x1f4a32,0x6e2a1e};
        static readonly int[] Roofs={0xc8623c,0xb5532f,0xd27448,0xa9472b,0xc96a4a,0xd88a5e,0xbf5a36};
        static readonly int[] Awnings={0x8b1d2c,0x2a5490,0x2e5e3e,0xd8a21e,0xf2efe6};
        BayviewKit.Tint House(string fam)
        {
            int d=Doors[rng.Next(Doors.Length)];
            int w=fam=="azulejo"?Tiles[rng.Next(Tiles.Length)]:fam=="fechada"?0xe4dccb:rng.NextDouble()<.18?Washes[rng.Next(Washes.Length)]:Whites[rng.Next(Whites.Length)];
            return T(w,fam=="fechada"?0x6f6a5f:d,Awnings[rng.Next(Awnings.Length)],d,Roofs[rng.Next(Roofs.Length)]);
        }
        static readonly Dictionary<int,float> Widths=new(){{5,5.2f},{6,6.1f},{7,7.0f}};
        static Vector2 Footprint(string id)
        {
            if(id.StartsWith("vila_varanda_"))return new(Widths[id[^1]-'0'],14.9f);
            if(id.StartsWith("vila_")&&char.IsDigit(id[^1]))return new(Widths[id[^1]-'0']+(id.StartsWith("vila_alta_")?.24f:0),13.9f);
            return id switch{"vila_esquina"=>new(8.9f,9.2f),"vila_pacos"=>new(23f,13.8f),"vila_forno"=>new(6.4f,8.8f),"vila_lavadouro"=>new(11.1f,5.2f),"igreja_douro"=>new(14.5f,23f),_=>new(6,6)};
        }

        // A building with its floor at the ground under its front's middle (each house steps with the lane); a granite plinth
        // fills any fall under it; refused on another building or a road, or where the slope would bury it.
        bool Building(string id,Vector3 centre,float yaw,BayviewKit.Tint tint=null,string sign=null,float maxFall=3.2f,float roadClear=4.4f,float maxRise=3f)
        {
            var size=Footprint(id);var rot=Quaternion.Euler(0,yaw,0);var c2=new Vector2(centre.x,centre.z);var half=size*.5f-new Vector2(.08f,.3f);
            foreach(var o in footprints)if(QuarryHillsTown.Overlap(c2,half,yaw,o.c,o.half,o.yaw)){Refused++;return false;}
            var front=centre+rot*new Vector3(0,0,size.y*.5f);float floor=H(front.x,front.z)+.05f,low=floor,high=floor;
            for(int i=0;i<5;i++)for(int j=0;j<5;j++) {
                var q=centre+rot*new Vector3((i/2f-1)*(size.x*.5f),0,(j/2f-1)*(size.y*.5f));float g=H(q.x,q.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                if((i%2==0&&j%2==0)&&ValleyField.RoadDistance(q.x,q.z)<roadClear){Refused++;return false;}
            }
            if(floor-low>maxFall||high-floor>maxRise){Refused++;return false;}
            if(floor-low>.2f) {
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="Granite plinth";b.transform.SetParent(root);b.transform.SetPositionAndRotation(new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),rot);
                b.transform.localScale=new Vector3(size.x+.1f,floor-low+.5f,size.y+.1f);b.GetComponent<Renderer>().sharedMaterial=plinth;b.tag="Respawn";
            }
            if(Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign)==null){Refused++;return false;}
            footprints.Add((c2,half,yaw));Houses++;return true;
        }

        public void Build()
        {
            Houses=Refused=0;Spots.Clear();Parking.Clear();yards.Clear();Yards=0;
            plinth=WorldLook.Kit("Vila granite plinth",new Color(.6f,.59f,.56f),WorldLook.Pattern.Stone,0,.1f,true);
            Largo();Lanes();Patios();Escadinhas();Quintais();Details();Adro();Parked();
            Debug.Log($"LITTLE_ROAD_VINELANDS_VILA houses={Houses} refused={Refused} yards={Yards} parking={Parking.Count}");
        }

        // ---- the Largo (70 x 45 m about (-5, 0)): the church east, its front to the square; the town hall on the north side,
        // west of Rua Direita; the cafe, post office and bank west; the pastelaria and pharmacy south; houses on the corners.
        // Its paving: black and white cobbles in waves. The fountain, plane trees, benches, two cafe terraces.
        void Largo()
        {
            var L=Vinelands.Largo;var c=F(-5,0);
            var church=F(41,0);if(Building("igreja_douro",church,Yaw(church,c),T(0xf8f5ee,0x2a5490,0x2a5490,0x5a3a24,0xc8623c),null,4f,4f))Spots["priest"]=(F(26,6),Yaw(F(26,6),c));
            var pacos=F(-32,31);if(Building("vila_pacos",pacos,Yaw(pacos,F(-26,0)),T(0xf6f0e3,0x2e5e3e,0x2e5e3e,0x5a3a24,0xb5532f)))Spots["clerk"]=(F(-30,18),Yaw(F(-30,18),pacos));
            // the frontages, facing in: (edge start, edge end, outward normal), each a row of houses with the square's specials
            var signs=new Queue<string>(new[]{"CAFE CENTRAL","CORREIOS","BANCO","PASTELARIA","FARMACIA","TABACARIA"});
            Frontage(new Vector2(-42,-18),new Vector2(-42,22),new Vector2(-1,0),signs,true);
            Frontage(new Vector2(-38,-26),new Vector2(28,-26),new Vector2(0,-1),signs,true);
            Frontage(new Vector2(4,28),new Vector2(30,28),new Vector2(0,1),signs,true);
            if(BayviewKit.Has("vila_chafariz")){var f=F(12,12);Put("vila_chafariz",new Vector3(f.x,f.y-.02f,f.z),22);}
            foreach(var (a,n) in new[]{(-30f,10f),(-30f,-12f),(22f,18f),(22f,-14f),(-14f,-18f),(-2f,20f)}){var t=F(a,n);if(ValleyField.RoadDistance(t.x,t.z)<6.5f)continue;t.y-=.05f;Put("tree_round",t,(float)dress.NextDouble()*360,null,null,true,1.25f+(float)dress.NextDouble()*.25f);
                var b=F(a+2.5f,n);if(ValleyField.RoadDistance(b.x,b.z)>5.5f)Put("bench",b,Yaw(b,c),null,null,false);}
            foreach(var (a,n) in new[]{(-30f,-4f),(14f,-18f)}){var e=F(a,n);if(ValleyField.RoadDistance(e.x,e.z)>6f)Put("vila_esplanada",e,(float)dress.NextDouble()*360,T(0xffffff,0xffffff,a<0?0x8b1d2c:0x2a5490,0xffffff),null,false);}
            for(int i=0;i<8;i++){var lp=c+Quaternion.Euler(0,i*45+22,0)*new Vector3(0,0,27);lp=new Vector3(lp.x,H(lp.x,lp.z),lp.z);if(ValleyField.RoadDistance(lp.x,lp.z)<5.5f)continue;Put("lamp_post",lp,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            Plaza("Largo",new Vector2(-40,-23),new Vector2(30,23),Waves(),true);
            Spots["cafe"]=(F(-34,-6),Yaw(F(-34,-6),c));
        }
        void Frontage(Vector2 a,Vector2 b,Vector2 outward,Queue<string> signs,bool shops)
        {
            var dir=(b-a).normalized;float len=Vector2.Distance(a,b);float s=0;string last="";
            while(s<len-4) {
                int cls=new[]{6,7,7,6,5}[rng.Next(5)];string fam=shops&&signs.Count>0&&rng.NextDouble()<.7?"loja":Pick(last,true);string id=$"vila_{fam}_{cls}";var size=Footprint(id);
                var mid=a+dir*(s+size.x*.5f);var cen=mid+outward*(size.y*.5f);var c=F(cen.x,cen.y);var at=F(mid.x,mid.y);
                string sign=fam=="loja"&&signs.Count>0?signs.Peek():null;
                if(Building(id,c,Yaw(c,at),House(fam),sign)){last=fam;if(sign!=null)signs.Dequeue();}
                s+=size.x+.05f;
            }
        }
        string Pick(string last,bool nearLargo)
        {
            for(int t=0;t<8;t++) {
                double r=rng.NextDouble();
                string fam=nearLargo&&r<.22?"loja":r<.32?"granito":r<.48?"azulejo":r<.62?"alta":r<.76?"varanda":r<.88?"granito":"fechada";
                if(fam!=last)return fam;
            }
            return last=="granito"?"alta":"granito";
        }

        // ---- the lanes (travessas and the Calçada through the old town), houses tight along both sides, fronts on the lane
        //   Rua da Igreja, Rua do Forno, Rua do Poço, Travessa dos Tanoeiros, Beco do Cego (plan section 2); and the Calçada's
        //   first 300 m, the road through the old town (its houses a pavement back).
        static readonly (string name,float width,Vector2[] pts)[] LaneDefs={
            ("Rua da Igreja",4.2f,new Vector2[]{new(30,45),new(100,70),new(170,80),new(215,40)}),
            ("Rua do Forno",4f,new Vector2[]{new(-10,-35),new(50,-75),new(120,-100),new(200,-100)}),
            ("Rua do Poço",3.6f,new Vector2[]{new(40,-20),new(90,-60),new(110,-110)}),
            ("Travessa dos Tanoeiros",3f,new Vector2[]{new(60,45),new(80,95),new(60,125)}),
            ("Beco do Cego",2.4f,new Vector2[]{new(120,-100),new(150,-60),new(165,-30)})};
        public static readonly List<(string name,float width,Vector3[] line)> Lanes2=new();
        void Lanes()
        {
            Lanes2.Clear();var setts=Setts();
            foreach(var (name,width,pts) in LaneDefs) {
                var line=Dense(pts,1.5f);Lanes2.Add((name,width,line));Ribbon(name,line,width,setts);
                foreach(int side in new[]{1,-1})Row(line,width*.5f+.35f,side,name=="Rua da Igreja"||name=="Rua do Forno");
            }
            // the Calçada through the old town: houses both sides of its first 300 m, a narrow pavement back
            var cr=HillcrestRoute.Find(Vinelands.CaisRoadName);if(cr!=null){var seg=new List<Vector3>();for(float s=40;s<300;s+=1.5f)seg.Add(cr.At(s));foreach(int side in new[]{1,-1})Row(seg.ToArray(),cr.width*.5f+1.6f,side,true);}
            var rm=HillcrestRoute.Find(Vinelands.MiranteName);if(rm!=null){var seg=new List<Vector3>();for(float s=40;s<150;s+=1.5f)seg.Add(rm.At(s));foreach(int side in new[]{1,-1})Row(seg.ToArray(),rm.width*.5f+1.6f,side,false);}
        }
        Vector3[] Dense(Vector2[] f,float step){var o=new List<Vector3>();for(int i=1;i<f.Length;i++){float L=Vector2.Distance(f[i-1],f[i]);int n=Mathf.Max(1,Mathf.CeilToInt(L/step));for(int k=0;k<n;k++){var q=Vector2.Lerp(f[i-1],f[i],k/(float)n);o.Add(F(q.x,q.y));}}o.Add(F(f[^1].x,f[^1].y));return o.ToArray();}
        // houses along one side of a lane: fronts `offset` from its centreline, side by side on party walls (5 cm apart)
        void Row(Vector3[] line,float offset,int side,bool shops,float maxFall=3.2f,float maxRise=3f)
        {
            float total=0;var run=new float[line.Length];for(int i=1;i<line.Length;i++){total+=Vector3.Distance(line[i],line[i-1]);run[i]=total;}
            Vector3 At(float s){for(int i=1;i<line.Length;i++)if(run[i]>=s){float t=(s-run[i-1])/Mathf.Max(.001f,run[i]-run[i-1]);return Vector3.Lerp(line[i-1],line[i],t);}return line[^1];}
            Vector3 Norm(float s){var d=At(Mathf.Min(total,s+1))-At(Mathf.Max(0,s-1));d.y=0;d.Normalize();return new Vector3(-d.z,0,d.x)*side;}
            float s0=2+(float)rng.NextDouble()*2;string last="";float gap=-1;
            // (where houses won't fit, a granite garden wall along the street line with the yard behind it: never open grass)
            // (broken wherever it would cross a lane, a stair or a road: a wall never closes a way)
            void Wall(float a,float b){if(b-a<3)return;var pts=new List<Vector3>();
                for(float s=a;s<=b+.01f;s+=2){var q=At(s)+Norm(s)*(offset+.3f);q.y=H(q.x,q.z);if(OnWay(q)){Lay(pts);pts=new List<Vector3>();}else pts.Add(q);}Lay(pts);}
            void Lay(List<Vector3> pts){if(pts.Count<2)return;
                var L=Runs.Solve(new RunSpec{name=$"Vila garden wall {walls++}",kit="dry_stone",seed=$"vila-wall-{walls}",along=pts.ToArray()});if(L.failures.Count==0&&L.items.Count>0){Runs.Draw(L,root);Runs.Laid.Add(L);}}
            while(s0<total-3) {
                int cls=new[]{5,6,6,7,5}[rng.Next(5)];string fam=Pick(last,shops&&s0<120);
                var n0=Norm(s0+3);var probe=At(s0+3)+n0*(offset+6);bool downhill=H(probe.x,probe.z)<At(s0+3).y-1.2f;
                if(fam=="varanda"&&!downhill)fam="granito";
                string id=$"vila_{fam}_{cls}";var size=Footprint(id);float mid=s0+size.x*.5f;
                var n=Norm(mid);var at=At(mid);var c=at+n*(offset+size.y*.5f);c.y=H(c.x,c.z);
                string sign=fam=="loja"?Signs[dress.Next(Signs.Length)]:null;
                if(Building(id,c,Yaw(c,at),House(fam),sign,maxFall,3.6f,maxRise)) {
                    if(gap>=0){Wall(gap,s0);Garden(gap,s0);gap=-1;}
                    last=fam;var along=Quaternion.Euler(0,90,0)*n;yards.Add((c+n*(size.y*.5f),n,along,size.x,fam));
                    if(dress.NextDouble()<.35){var dp=at+n*(offset+.5f)+along*(size.x*.3f);dp.y=H(dp.x,dp.z);Put("vila_vasos",dp,Yaw(dp,at),null,null,false);}
                    // (a wall lantern beside the door on most houses: the lanes are lit from the walls, not from posts)
                    if(fam!="fechada"&&dress.NextDouble()<.7){var face=at+n*(offset+.02f)+along*(size.x*(dress.NextDouble()<.5?.3f:-.3f));float fy=H(face.x,face.z);Put("vila_lanterna",new Vector3(face.x,Mathf.Max(fy,c.y)+2.7f,face.z),Yaw(c,at),null,null,false);}
                }
                else if(gap<0)gap=s0;
                s0+=size.x+.05f;
            }
            if(gap>=0){Wall(gap,total-1);Garden(gap,total-1);}
            void Garden(float a,float b){if(b-a<4)return;float m=(a+b)*.5f;var nn=Norm(m);var p=At(m)+nn*(offset+.3f);yards.Add((p,nn,Quaternion.Euler(0,90,0)*nn,b-a,"horta"));}
        }
        int walls;
        // on a lane, a stair, a square or within a road's verge (the whole old town's ways, known before any row is built)
        List<(Vector3[] line,float half)> ways;
        bool OnWay(Vector3 q)
        {
            if(ValleyField.RoadDistance(q.x,q.z)<4.5f)return true;
            if(ways==null){ways=new();foreach(var (_,w,pts) in LaneDefs)ways.Add((Dense(pts,1.5f),w*.5f+.6f));foreach(var pts in Stairs)ways.Add((Dense(pts,1.5f),1.8f));}
            var p=new Vector2(q.x,q.z);foreach(var (line,half) in ways)foreach(var v in line)if((new Vector2(v.x,v.z)-p).sqrMagnitude<half*half)return true;
            var f=p-Vinelands.Centre;foreach(var (mn,mx) in Squares)if(f.x>mn.x&&f.x<mx.x&&f.y>mn.y&&f.y<mx.y)return true;
            return false;
        }
        static readonly string[] Signs={"TALHO","PEIXARIA","DROGARIA","MERCEARIA","BARBEARIA","RETROSARIA","PAPELARIA","SAPATARIA","QUIOSQUE"};

        // ---- the patios and the spring's square
        void Patios()
        {
            var forno=F(115,-112);if(Building("vila_forno",forno,Yaw(forno,F(115,-98)),T(0xffffff,0x5a3a24,0x8b1d2c,0x5a3a24,0xb5532f)))Spots["baker"]=(F(112,-100),Yaw(F(112,-100),forno));
            var tank=F(124,-100);Put("bench",tank,Yaw(tank,forno),null,null,false);
            var pt=F(60,128);Put("tree_round",new Vector3(pt.x,pt.y-.05f,pt.z),40,null,null,true,1.0f);Put("bench",F(55,124),70,null,null,false);
            var fonte=F(-150,-10);if(Building("vila_lavadouro",fonte,Yaw(fonte,F(-150,10)),T(0xffffff,0xffffff,0xffffff,0xffffff,0xc96a4a)))Spots["fonte"]=(F(-140,-2),0);
            var lime=F(-138,-22);Put("tree_round",new Vector3(lime.x,lime.y-.05f,lime.z),120,null,null,true,1.3f);
            Plaza("Largo da Fonte",new Vector2(-165,-22),new Vector2(-135,4),Waves(),false);Plaza("Pátio do Forno",new Vector2(104,-118),new Vector2(126,-100),Setts(),false);
        }

        // ---- the escadinhas: stepped lanes down the slope, a 1.2 m tread each step on the ground below it, a handrail, pots
        // on the landings; houses tight along their sides
        static readonly Vector2[][] Stairs={new Vector2[]{new(60,10),new(110,-20),new(150,-70),new(190,-120)},new Vector2[]{new(110,70),new(160,40),new(210,5),new(260,-25)},new Vector2[]{new(-60,-40),new(-110,-30),new(-150,-10)}};
        void Escadinhas()
        {
            var stone=WorldLook.Kit("Vila step granite",new Color(.66f,.64f,.6f),WorldLook.Pattern.Stone,0,.1f,true);var rail=TrialWorld.Material("Vila handrail",new Color(.18f,.2f,.22f),.3f,.4f);
            foreach(var pts in Stairs) {
                var line=Dense(pts,1.2f);Vector3? lastPost=null;
                for(int i=1;i<line.Length;i++) {
                    var a=line[i-1];var b=line[i];var d=b-a;d.y=0;float y=Mathf.Max(a.y,b.y);
                    var st=GameObject.CreatePrimitive(PrimitiveType.Cube);st.name="Escadinha step";Object.Destroy(st.GetComponent<Collider>());st.transform.SetParent(root);
                    st.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,y-.1f,(a.z+b.z)*.5f),Quaternion.LookRotation(d));st.transform.localScale=new Vector3(2.4f,.3f+Mathf.Abs(a.y-b.y),d.magnitude+.05f);st.GetComponent<Renderer>().sharedMaterial=stone;
                    if(i%2==0){var r=GameObject.CreatePrimitive(PrimitiveType.Cylinder);r.name="Handrail post";Object.Destroy(r.GetComponent<Collider>());r.transform.SetParent(root);var side=Vector3.Cross(Vector3.up,d.normalized)*1.1f;r.transform.position=a+side+Vector3.up*.5f;r.transform.localScale=new Vector3(.05f,.5f,.05f);r.GetComponent<Renderer>().sharedMaterial=rail;
                        // (the rail itself, post top to post top)
                        var top=a+side+Vector3.up*1f;if(lastPost!=null){var span=top-lastPost.Value;var h=GameObject.CreatePrimitive(PrimitiveType.Cube);h.name="Handrail";Object.Destroy(h.GetComponent<Collider>());h.transform.SetParent(root);
                            h.transform.SetPositionAndRotation((top+lastPost.Value)*.5f,Quaternion.LookRotation(span));h.transform.localScale=new Vector3(.06f,.06f,span.magnitude+.06f);h.GetComponent<Renderer>().sharedMaterial=rail;}
                        lastPost=top;}
                    if(i%9==0){var p=a-Vector3.Cross(Vector3.up,d.normalized)*1.0f;Put("vila_vasos",p,Yaw(a,b)+90,null,null,false);}
                }
                foreach(int side in new[]{1,-1})Row(line,1.6f,side,false,4.5f,4.5f);   // (houses step down the stairs: a steeper fall and rise allowed)
            }
        }

        // ---- the back yards (quintais): behind each house its own walled yard, as deep as the block allows (7-13 m, stopping
        // at a lane, a road, a square or another yard). In it, what a Douro household grows and keeps: a vine pergola (latada)
        // off the back door, the kitchen plot of couves and beans, a fig or an orange tree, a stone shed, hens, a tank. A
        // shut-up house's yard has gone to a fig. Granite walls, 1.6 m, along each yard's sides and back, stepping with the ground.
        readonly List<(Vector3 back,Vector3 n,Vector3 along,float w,string fam)> yards=new();public static int Yards;
        // (inside the Largo's paving, 2 m in from its edge: the road shoulders stop here, the square's cobbles are its verge)
        public static bool Paved(Vector3 p){if(!Vinelands.Enabled)return false;var f=new Vector2(p.x,p.z)-Vinelands.Centre;return f.x>-38&&f.x<28&&f.y>-21&&f.y<21;}
        static readonly (Vector2 min,Vector2 max)[] Squares={(new(-40,-23),new(30,23)),(new(-165,-22),new(-135,4)),(new(104,-118),new(126,-100)),(new(52,117),new(68,133))};
        void Quintais()
        {
            var paths=new List<(Vector3[] line,float half)>();foreach(var (_,w,l) in Lanes2)paths.Add((l,w*.5f+1f));foreach(var pts in Stairs)paths.Add((Dense(pts,1.5f),2.4f));
            var claimed=new List<(Vector2 c,Vector2 half,float yaw)>();var walls=new List<Matrix4x4>();var yrng=new System.Random(8603);
            bool Inside(Vector2 q,Vector2 c,Vector2 half,float yaw,float pad){var l=Quaternion.Euler(0,-yaw,0)*new Vector3(q.x-c.x,0,q.y-c.y);return Mathf.Abs(l.x)<half.x+pad&&Mathf.Abs(l.z)<half.y+pad;}
            bool Free(Vector3 p)
            {
                if(ValleyField.RoadDistance(p.x,p.z)<5f)return false;var q=new Vector2(p.x,p.z);
                foreach(var o in footprints)if(Inside(q,o.c,o.half,o.yaw,.4f))return false;
                foreach(var o in claimed)if(Inside(q,o.c,o.half,o.yaw,.1f))return false;
                foreach(var (line,half) in paths)foreach(var v in line)if((new Vector2(v.x,v.z)-q).sqrMagnitude<half*half)return false;
                var f=q-Vinelands.Centre;foreach(var (mn,mx) in Squares)if(f.x>mn.x-1&&f.x<mx.x+1&&f.y>mn.y-1&&f.y<mx.y+1)return false;
                return true;
            }
            void Wall(Vector3 a,Vector3 b){var d=b-a;d.y=0;float L=d.magnitude;if(L<.5f)return;int k=Mathf.CeilToInt(L/1.5f);var rot=Quaternion.LookRotation(d);
                for(int i=0;i<k;i++){var m=a+d*((i+.5f)/k);var e0=a+d*(i/(float)k);var e1=a+d*((i+1f)/k);float g=H(m.x,m.z),lo=Mathf.Min(g,Mathf.Min(H(e0.x,e0.z),H(e1.x,e1.z)))-.3f,top=g+1.5f;   // (down to the lowest ground under it: no air on a slope)
                    walls.Add(Matrix4x4.TRS(new Vector3(m.x,(lo+top)*.5f,m.z),rot,new Vector3(.45f,top-lo,L/k+.02f)));}}
            foreach(var (back,n,along,w,fam) in yards) {
                float want=7+(float)yrng.NextDouble()*6,d=0;
                for(float t=1;t<=want;t+=1){bool ok=true;foreach(float k in new[]{-.42f,0,.42f})if(!Free(back+n*t+along*(w*k))){ok=false;break;}if(!ok)break;d=t;}
                if(d<3.5f)continue;
                float yaw=Yaw(back,back+n);var mid=back+n*(d*.5f);claimed.Add((new Vector2(mid.x,mid.z),new Vector2(w*.5f,d*.5f),yaw));Yards++;
                // the walls, inset 20 cm so a neighbour's stands beside this one (one thick wall), not in it
                foreach(float sx in new[]{-1f,1f})Wall(back+along*(sx*(w*.5f-.2f))+n*.3f,back+along*(sx*(w*.5f-.2f))+n*(d-.2f));
                Wall(back+along*(-w*.5f+.2f)+n*(d-.2f),back+along*(w*.5f-.2f)+n*(d-.2f));
                // what's in it, each where it fits (inside the walls, on ground that's level enough, clear of the others)
                var used=new List<(float u,float v,float hu,float hv)>();
                bool Place(string id,float u,float v,float hu,float hv,bool solid)
                {
                    if(u-hu<-w*.5f+.45f||u+hu>w*.5f-.45f||v-hv<.6f||v+hv>d-.45f)return false;
                    foreach(var o in used)if(Mathf.Abs(u-o.u)<hu+o.hu+.3f&&Mathf.Abs(v-o.v)<hv+o.hv+.3f)return false;
                    var p=back+along*u+n*v;float lo=float.MaxValue,hi=float.MinValue;foreach(var (a,b) in new[]{(-1f,-1f),(1f,-1f),(-1f,1f),(1f,1f),(0f,0f)}){var q=p+along*(a*hu)+n*(b*hv);float g=H(q.x,q.z);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
                    bool tree=id=="vila_figueira"||id=="vila_laranjeira";if(hi-lo>(tree?1.6f:.7f))return false;p.y=tree?lo+(hi-lo)*.35f:lo+.02f;
                    if(Put(id,p,yaw,null,null,solid)==null)return false;used.Add((u,v,hu,hv));return true;
                }
                double r()=>yrng.NextDouble();float side=r()<.5?-1:1;
                if(fam=="fechada"){Place("vila_figueira",side*(w*.5f-2.4f),d*.6f,2f,2f,true);continue;}
                if(fam=="horta") {   // (a garden plot in a row's gap: plots across it, a tree or two, a shed, the tank)
                    for(float u=-w*.5f+2.2f;u<w*.5f-2f;u+=3.6f)if(r()<.8)Place("vila_horta",u,Mathf.Min(3.4f,d-2.6f),1.55f,2.55f,false);
                    for(float u=-w*.5f+2.2f;u<w*.5f-1.5f;u+=4.5f)for(float v=d-1.8f;v>1.5f;v-=4.5f)if(r()<.55)Place(r()<.5?"vila_figueira":"vila_laranjeira",u+((float)r()-.5f),v,1.1f,1.1f,true);   // (an orchard of a plot, fig and orange)
                    if(r()<.4)Place("vila_anexo",side*(w*.5f-2.1f),d-1.8f,1.65f,1.35f,true);
                    if(r()<.4)Place("vila_tanque",-side*(w*.5f-1.6f),1.4f,.7f,1.15f,true);
                    if(r()<.3)Place("vila_latada",0,d-3.4f,2.1f,3.05f,false);
                    continue;
                }
                if(r()<.45)Place("vila_latada",0,3.6f,Mathf.Min(2.1f,w*.5f-.5f),3.05f,false);
                if(r()<.6&&!Place("vila_horta",-side*(w*.5f-2f),d-3f,1.55f,2.55f,false))Place("vila_horta",0,d-3f,1.55f,2.55f,false);
                if(r()<.7)Place(r()<.55?"vila_figueira":"vila_laranjeira",side*(w*.5f-1.6f),d-1.9f,1.1f,1.1f,true);
                if(r()<.3&&d>=8)Place("vila_anexo",-side*(w*.5f-2.1f),d-1.9f,1.65f,1.35f,true);
                if(r()<.18)Place("vila_galinheiro",side*(w*.5f-2.1f),d*.5f,1.4f,1f,true);
                if(r()<.2)Place("vila_tanque",-side*(w*.5f-1.6f),d*.45f,.7f,1.15f,true);
            }
            QuarryFloor.Combine("Vila yard walls",walls,plinth,root,true);
        }

        // ---- the churchyard (adro): the granite cross in front of the church, cypresses behind it at the terrace's edge
        void Adro()
        {
            var x=F(22,-13);if(ValleyField.RoadDistance(x.x,x.z)>5f)Put("vila_cruzeiro",x,0);
            foreach(var (a,n) in new[]{(58f,9f),(58f,-9f),(62f,0f)}){var c=F(a,n);if(ValleyField.RoadDistance(c.x,c.z)>4f)Put("vila_cipreste",new Vector3(c.x,c.y-.05f,c.z),0,null,null,true,.9f+(float)dress.NextDouble()*.25f);}
        }

        // ---- where the cars stand: round the Largo's edge in front of the shops, parallel to the frontage (the regulars' cars
        // outside the Cafe Central, the evening's cars); the town hall's side; and the Calcada's first stretch
        void Parked()
        {
            foreach(var (a,b) in new[]{(new Vector2(-37,-17),new Vector2(-37,21)),(new Vector2(-32,-21.5f),new Vector2(24,-21.5f)),(new Vector2(6,23),new Vector2(26,23))}) {
                float L=Vector2.Distance(a,b);for(float s=3;s<L-2;s+=6.2f) {
                    var f=Vector2.Lerp(a,b,s/L);var p=F(f.x,f.y);if(ValleyField.RoadDistance(p.x,p.z)<6f)continue;
                    if(dress.NextDouble()<.15)continue;   // (gaps: not every bay taken)
                    var q=F(b.x,b.y);Parking.Add((new Vector3(p.x,p.y+.08f,p.z),Yaw(F(a.x,a.y),q)));
                }
            }
            // (and at the spring, where people fill their jerry cans and stay to talk; and along the church's flanks, for Mass)
            foreach(var (fa,fn,y) in new[]{(-160f,6f,90f),(-130f,8f,100f),(36f,-11.5f,90f),(36f,11.5f,90f)}){var p=F(fa,fn);if(ValleyField.RoadDistance(p.x,p.z)>5f)Parking.Add((new Vector3(p.x,p.y+.08f,p.z),y));}
        }

        // ---- the small things: the shrine, laundry across the narrow lanes, lanterns at the patios
        void Details()
        {
            {var fo=F(115,-112);var fwd=F(115,-98)-fo;fwd.y=0;fwd.Normalize();var right=Vector3.Cross(Vector3.up,fwd);var o=fo+right*3.0f;o.y=H(o.x,o.z);Put("vila_oratorio",o,Yaw(o,o+right),T(0x3a6aa6,0xffffff,0xffffff,0xffffff),null,false);}   // (on the oven's side wall, facing the Beco)
            foreach(var (name,width,line) in Lanes2) {
                if(width>4.1f)continue;
                for(int i=12;i<line.Length-6;i+=18) {
                    var a=line[i];var d=line[i+1]-line[i-1];d.y=0;d.Normalize();var side=new Vector3(-d.z,0,d.x);
                    var p=a+Vector3.up*(5.2f+(float)dress.NextDouble()*.8f);Put("vila_estendal",p,Yaw(a,a+side),null,null,false,(width+1.0f)/6f);
                }
            }
        }

        // ---- the Largo's paving: one surface over the whole square, carriageway and all (a Portuguese square is paved across,
        // the cars drive on its cobbles), at the highest of the ground and the roads under it (+3 cm), with a collider the cars
        // drive on (a RoadSurface). Its look: black-and-white waves from a generated texture, a wave every 8 m.
        void Plaza(string name,Vector2 min,Vector2 max,Material m,bool drivable)
        {
            Physics.SyncTransforms();const float q=1f;int nx=Mathf.CeilToInt((max.x-min.x)/q),nz=Mathf.CeilToInt((max.y-min.y)/q);
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();var raw=new List<float>();
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++) {
                var w=Vinelands.Centre+new Vector2(min.x+i*q,min.y+j*q);float top=float.MinValue;
                float shoulder=float.MinValue;
                foreach(var hit in Physics.RaycastAll(new Vector3(w.x,400,w.y),Vector3.down,600))if(!hit.collider.isTrigger) {   // (the ground, the roads and their junctions only: not a roof)
                    if(hit.collider.GetComponent<RoadSurface>()!=null||hit.collider.name.Contains("mainland collider")||hit.collider.name.StartsWith("Junction"))top=Mathf.Max(top,hit.point.y);
                    else if(hit.collider.name.ToLowerInvariant().Contains("shoulder"))shoulder=Mathf.Max(shoulder,hit.point.y);
                }
                if(top==float.MinValue)top=H(w.x,w.y);if(shoulder>top)top=shoulder+.04f;   // (over a shoulder's gravel wholly: it can stand 30 cm proud of the road; the ramp below eases it)
                if(top==float.MinValue)top=H(w.x,w.y);float edge=Mathf.Min(Mathf.Min(i,nx-i),Mathf.Min(j,nz-j))*q;raw.Add(top+.03f);v.Add(new Vector3(w.x,top+Mathf.Lerp(.03f,.15f,Mathf.Clamp01(edge/4f)),w.y));   // (15 cm over the roads' paint inside, easing to 3 cm at the edge where the roads run out)
               uv.Add(new Vector2(w.x/8f,w.y/8f));
            }
            // (a 3 x 3 median: a ray that caught some thin edge left a one-vertex ridge that folded the paving into teeth; no road
            // is a metre wide, so the median takes the ridges and spikes and keeps every real surface)
            {var y0=v.ConvertAll(q=>q.y);var nb=new float[9];
                for(int j=1;j<nz;j++)for(int i=1;i<nx;i++){int k=j*(nx+1)+i,cnt=0;for(int dj=-1;dj<=1;dj++)for(int di=-1;di<=1;di++)nb[cnt++]=y0[k+dj*(nx+1)+di];System.Array.Sort(nb);v[k]=new Vector3(v[k].x,nb[4],v[k].z);}}
            // (then over every surface within 3 m, falling 12 % away from it: the paving never dips under a road that stands a
            // little proud of the ground, and the step at the road's edge becomes a gentle ramp, not a fold)
            {var y1=v.ConvertAll(q=>q.y);const int R=3;
                for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){int k=j*(nx+1)+i;float best=y1[k];
                    for(int dj=-R;dj<=R;dj++)for(int di=-R;di<=R;di++){int jj=j+dj,ii=i+di;if(jj<0||jj>nz||ii<0||ii>nx)continue;best=Mathf.Max(best,y1[jj*(nx+1)+ii]-.12f*q*Mathf.Sqrt(di*di+dj*dj));}
                    v[k]=new Vector3(v[k].x,best,v[k].z);}}
            // (and its last 4 m eases back to the ground and roads as they lie, so the paving meets each road leaving the square
            // flush: the filters above carried the raised middle out to the edge, a 20 cm step down onto the road, round 12)
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){int k=j*(nx+1)+i;float e=Mathf.Min(Mathf.Min(i,nx-i),Mathf.Min(j,nz-j))*q;v[k]=new Vector3(v[k].x,Mathf.Lerp(raw[k],v[k].y,Mathf.Clamp01(e/4f)),v[k].z);}
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int k=j*(nx+1)+i;t.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
            var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name+" paving",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;go.isStatic=true;
            if(drivable){go.AddComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<RoadSurface>();}
        }
        static Material waves,setts;
        // calçada portuguesa: white limestone with black basalt waves, the stones' joints faintly showing
        static Material Waves()
        {
            if(waves!=null)return waves;const int N=512;var tex=new Texture2D(N,N,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat,name="Calcada waves"};var px=new Color32[N*N];
            for(int y=0;y<N;y++)for(int x=0;x<N;x++) {
                float u=x/(float)N,v=y/(float)N;float wave=Mathf.Sin((u+Mathf.Sin(v*Mathf.PI*2)*.12f)*Mathf.PI*4);bool dark=wave>.62f;
                int cx=x%10,cy=(y+(x/10%2)*5)%10;bool joint=cx==0||cy==0;
                byte c=dark?(byte)(joint?38:58):(byte)(joint?205:238);px[y*N+x]=new Color32(c,c,(byte)(dark?c+4:c-6),255);
            }
            tex.SetPixels32(px);tex.Apply(true);waves=TrialWorld.Material("Calçada waves",Color.white,0,.1f);waves.SetTexture("_BaseMap",tex);waves.mainTexture=tex;return waves;
        }
        // granite setts: grey squares in courses, each a shade of its own
        static Material Setts()
        {
            if(setts!=null)return setts;const int N=256;var tex=new Texture2D(N,N,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat,name="Granite setts"};var px=new Color32[N*N];var r=new System.Random(9);
            var shade=new byte[17*17];for(int i=0;i<shade.Length;i++)shade[i]=(byte)(128+r.Next(40));
            for(int y=0;y<N;y++)for(int x=0;x<N;x++){int row=y/16;int xs=x+(row%2)*8;bool joint=y%16==0||xs%16==0;byte c=joint?(byte)90:shade[(row%17)*17+(xs/16)%17];px[y*N+x]=new Color32(c,c,(byte)(c-4),255);}
            tex.SetPixels32(px);tex.Apply(true);setts=TrialWorld.Material("Granite setts",Color.white,0,.12f);setts.SetTexture("_BaseMap",tex);setts.mainTexture=tex;return setts;
        }
        // ---- paving: black and white cobbles in waves (calçada portuguesa) over a rectangle in the frame, following the ground,
        // kept off the roads (their asphalt runs through)
        void Paving(Vector2 min,Vector2 max)
        {
            var white=WorldLook.Kit("Calçada white",new Color(.93f,.92f,.88f),WorldLook.Pattern.Stone,0,.08f,true);var black=WorldLook.Kit("Calçada basalt",new Color(.22f,.22f,.24f),WorldLook.Pattern.Stone,0,.08f,true);
            var vw=new List<Vector3>();var tw=new List<int>();var vb=new List<Vector3>();var tb=new List<int>();const float q=1f;
            for(float a=min.x;a<max.x;a+=q)for(float n=min.y;n<max.y;n+=q) {
                var c=Vinelands.Centre+new Vector2(a+q*.5f,n+q*.5f);if(ValleyField.RoadDistance(c.x,c.y)<4.6f)continue;
                bool dark=Mathf.Sin((a+q*.5f)*.32f+Mathf.Sin((n+q*.5f)*.18f)*2.2f)>.55f;var v=dark?vb:vw;var t=dark?tb:tw;int k=v.Count;
                foreach(var (da,dn) in new[]{(0f,0f),(q,0f),(q,q),(0f,q)}){var w=Vinelands.Centre+new Vector2(a+da,n+dn);v.Add(new Vector3(w.x,H(w.x,w.y)+.035f,w.y));}
                t.AddRange(new[]{k,k+3,k+1,k+1,k+3,k+2});
            }
            foreach(var (v,t,m,name) in new[]{(vw,tw,white,"Calçada white"),(vb,tb,black,"Calçada basalt")}) {
                if(v.Count==0)continue;var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;go.isStatic=true;
            }
        }
        // ---- a cobbled lane: a ribbon `width` wide along `line`, on the ground
        void Ribbon(string name,Vector3[] line,float width,Material m)
        {
            var v=new List<Vector3>();var t=new List<int>();var uvs=new List<Vector2>();
            for(int i=0;i<line.Length;i++) {
                var d=line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(i-1,0)];d.y=0;d.Normalize();var side=new Vector3(-d.z,0,d.x)*width*.5f;
                var l=line[i]-side;var r=line[i]+side;int k=v.Count;v.Add(new Vector3(l.x,H(l.x,l.z)+.04f,l.z));v.Add(new Vector3(r.x,H(r.x,r.z)+.04f,r.z));uvs.Add(new Vector2(l.x/4f,l.z/4f));uvs.Add(new Vector2(r.x/4f,r.z/4f));
                if(i>0)t.AddRange(new[]{k-2,k-1,k,k-1,k+1,k});
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uvs);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;go.isStatic=true;
        }
    }
}
