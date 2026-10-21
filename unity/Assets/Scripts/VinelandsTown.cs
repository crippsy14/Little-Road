using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands' buildings, water and life (docs/85), built after the land and roads, from the douro kit (car-forge
    // forge/world/kits/douro.py): whitewash and granite, Roman tile, azulejo fronts, schist.
    //   - the Rua Direita (Vinelands Road's last 350 m into the Largo) and the Rua da Torre, lined both sides, tight;
    //   - the Largo: the church on its east side, its front to the square, a fountain, plane trees;
    //   - the Pit Stop at the town gate, on the view side;
    //   - the Adega in the Vineyard Loop's yard; the Belvedere's bell tower inside the road's hairpin;
    //   - the Cais: wine lodges along the river, their names facing the water, rabelo boats moored;
    //   - three quintas on the amphitheatre, each name in giant letters on the slope below it, read across the water;
    //   - the river's water.
    public sealed class VinelandsTown
    {
        readonly Transform root;readonly System.Random rng=new(8501);
        readonly List<(Vector2 c,Vector2 half,float yaw)> footprints=new();Material plinth;
        public static int Buildings,Refused;
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Spots=new();
        public static readonly List<(Vector3 at,float yaw)> Parking=new();
        public VinelandsTown(Transform parent){root=new GameObject("Vinelands").transform;root.SetParent(parent,false);}
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return d.sqrMagnitude<1e-6f?0:Quaternion.LookRotation(d).eulerAngles.y;}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,bool solid=true,float scale=1)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,solid,scale):null;
        // Footprints (width along the front, depth) as the kit models them (its contact sheet's sizes).
        static Vector2 Footprint(string id)=>id switch{"casa_douro_a"=>new(9.0f,8.4f),"casa_douro_b"=>new(6.5f,9.6f),"casa_douro_c"=>new(10.7f,9.9f),"casa_douro_d"=>new(9.6f,9.6f),"casa_douro_e"=>new(9.0f,10.4f),
            "casa_douro_f"=>new(8.5f,9.4f),"casa_douro_g"=>new(8.0f,10.4f),"igreja_douro"=>new(14.5f,23f),
            "torre_belvedere"=>new(5.1f,5.1f),"adega_coop"=>new(31f,16.4f),"armazem_vinho"=>new(41f,12f),"quinta_casa"=>new(21f,12.5f),_=>new(6,6)};
        // Whitewash (warm whites), the odd ochre or rose; azulejo tile colours for the tiled townhouses; doors and shutters in
        // the valley's bottle green, ruby, tile blue and chestnut.
        static readonly int[] Whites={0xf6f2ea,0xf2ece0,0xf7efe0,0xeee8dc,0xe8c27a,0xedc9b4,0xf4ead2};
        static readonly int[] Tiles={0x3a6aa6,0x2f5f95,0x3f7f6a,0x6b8fb8,0x2c4f86};
        static readonly int[] Doors={0x2e5e3e,0x8b1d2c,0x2a5490,0x5a3a24,0x1f4a32};
        // Roman tile, fresh to sun-bleached: each roof its own.
        static readonly int[] Roofs={0xc8623c,0xb5532f,0xd27448,0xa9472b,0xc96a4a,0xd88a5e};
        static readonly int[] Awnings={0x8b1d2c,0x2a5490,0x2e5e3e,0xd8a21e};
        BayviewKit.Tint House(string id)
        {
            int d=Doors[rng.Next(Doors.Length)];int w=id=="casa_douro_b"?Tiles[rng.Next(Tiles.Length)]:id=="casa_douro_f"?(rng.NextDouble()<.5?0xe8c27a:0xedc9b4):Whites[rng.Next(Whites.Length)];
            int aw=id=="casa_douro_g"?Tiles[rng.Next(Tiles.Length)]:Awnings[rng.Next(Awnings.Length)];
            var t=T(w,d,aw,d);t.roof=Toy.Hex(Roofs[rng.Next(Roofs.Length)]);return t;
        }
        // The street's mix: each house its own design, never the same as its neighbour; shops and cafes near the Largo.
        static readonly string[] Shops={"CAFE DO LARGO","PADARIA","MERCEARIA","TABERNA","FARMACIA","ADEGA VELHA","BARBEARIA","PASTELARIA"};int shop;
        string Pick(string last,bool nearLargo)
        {
            for(int t=0;t<8;t++){double r=rng.NextDouble();string id=nearLargo&&r<.3?"casa_douro_e":r<.18?"casa_douro_a":r<.36?"casa_douro_b":r<.48?"casa_douro_f":r<.6?"casa_douro_g":r<.72?"casa_douro_d":r<.8?"casa_douro_c":r<.9?"casa_douro_e":"casa_douro_a";if(id!=last)return id;}
            return last=="casa_douro_a"?"casa_douro_f":"casa_douro_a";
        }

        // A building with its floor at the ground under its front's middle; a granite plinth fills any fall; refused on
        // another building, a road, or where the slope would bury it (as Bramblewick's).
        bool Building(string id,Vector3 centre,float yaw,BayviewKit.Tint tint=null,string sign=null,float maxFall=3.5f,float maxRise=2.5f)
        {
            var size=Footprint(id);var rot=Quaternion.Euler(0,yaw,0);var c2=new Vector2(centre.x,centre.z);var half=size*.5f-new Vector2(.4f,.4f);
            foreach(var o in footprints)if(QuarryHillsTown.Overlap(c2,half,yaw,o.c,o.half,o.yaw)){Refused++;return false;}
            var front=centre+rot*new Vector3(0,0,size.y*.5f);float floor=H(front.x,front.z)+.05f,low=floor,high=floor;
            for(int i=0;i<5;i++)for(int j=0;j<5;j++) {
                var q=centre+rot*new Vector3((i/2f-1)*(size.x*.5f+.3f),0,(j/2f-1)*(size.y*.5f+.3f));float g=H(q.x,q.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                if((i%2==0&&j%2==0)&&ValleyField.RoadDistance(q.x,q.z)<4.6f){Refused++;return false;}
            }
            if(floor-low>maxFall||high-floor>maxRise){Refused++;return false;}
            if(floor-low>.25f) {
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="Stone plinth";b.transform.SetParent(root);b.transform.SetPositionAndRotation(new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),rot);
                b.transform.localScale=new Vector3(size.x+.4f,floor-low+.5f,size.y+.4f);b.GetComponent<Renderer>().sharedMaterial=plinth;b.tag="Respawn";
            }
            if(Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign)==null){Refused++;return false;}
            footprints.Add((c2,half,yaw));Buildings++;return true;
        }
        void Words(string words,Vector3 at,Quaternion rotation,float size,Color colour)
        {
            var go=new GameObject("Sign / "+words);go.transform.SetParent(root);go.transform.SetPositionAndRotation(at,rotation);
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.LowerCenter;text.alignment=TextAlignment.Center;text.fontSize=80;text.characterSize=size;text.color=colour;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }

        public void Build()
        {
            Buildings=Refused=0;Spots.Clear();Parking.Clear();
            plinth=WorldLook.Kit("Vinelands granite plinth",new Color(.58f,.57f,.55f),WorldLook.Pattern.Stone,0,.1f,true);
            AreaAudit.ExtraNature.Clear();River();   // (the restart, docs/85: zone by zone from here; the first build's town is retired)
            Physics.SyncTransforms();new VinelandsVila(root).Build();   // (zone 1: the Largo and Vila Velha, docs/86)
            Debug.Log($"LITTLE_ROAD_VINELANDS_TOWN buildings={Buildings} refused={Refused}");
        }

        // ---- the Largo: the church on its east side, front to the square; a fountain; plane trees round the edge
        void Largo()
        {
            var L=Vinelands.Largo;var east=new Vector3(1,0,0);
            var church=L+east*33;church.y=H(church.x,church.z);
            if(Building("igreja_douro",church,Yaw(church,L),T(0xf7f3ea,0x2a5490,0x2a5490,0x5a3a24),null,4.5f))Spots["church"]=(L+east*16,Yaw(L,church));
            var f=L+new Vector3(14,0,-14);f.y=H(f.x,f.z);Put("fountain",f,45,null,null,true);
            foreach(float a in new[]{20f,70f,160f,290f,330f}){var t=L+Quaternion.Euler(0,a,0)*new Vector3(0,0,46);if(ValleyField.RoadDistance(t.x,t.z)<8)continue;t.y=H(t.x,t.z)-.05f;Put("tree_round",t,(float)rng.NextDouble()*360,null,null,true,1.2f+(float)rng.NextDouble()*.3f);}
            foreach(float a in new[]{40f,110f,200f,250f}){var b=L+Quaternion.Euler(0,a,0)*new Vector3(0,0,30);if(ValleyField.RoadDistance(b.x,b.z)<6)continue;b.y=H(b.x,b.z);Put("bench",b,Yaw(b,L),null,null,false);}
            for(int i=0;i<10;i++){var lp=L+Quaternion.Euler(0,i*36+18,0)*new Vector3(0,0,24);if(ValleyField.RoadDistance(lp.x,lp.z)<5.5f)continue;lp.y=H(lp.x,lp.z);Put("lamp_post",lp,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}   // (lamps round the square)
            foreach(float a in new[]{0f,60f,120f,180f,240f,300f}){var f2=L+new Vector3(14,0,-14)+Quaternion.Euler(0,a,0)*new Vector3(0,0,4.5f);f2.y=H(f2.x,f2.z);Put("flower_bed",f2,a,null,null,false);}
        }

        // ---- a street: houses tight along both sides of `road`'s last `metres` before the Largo (or its first, `toLargo`
        // false), set back behind a pavement; lamps every 30 m, alternate sides
        void Street(HillcrestRoute.Road road,float metres,bool toLargo)
        {
            if(road==null)return;float half=road.width*.5f;float s0=toLargo?Mathf.Max(20,road.Length-metres):55,s1=toLargo?road.Length-58:Mathf.Min(road.Length-20,metres);
            var L=Vinelands.Largo;
            foreach(int side in new[]{1,-1}) {
                float s=s0+(float)rng.NextDouble()*4;string last="";
                while(s<s1) {
                    bool near=Vector3.Distance(road.At(s),L)<150;string id=Pick(last,near);var size=Footprint(id);float mid=s+size.x*.5f;
                    var n=road.Right(mid);n.y=0;n.Normalize();n*=side;
                    var c=road.At(mid)+n*(half+2.4f+size.y*.5f+(float)rng.NextDouble()*.6f);c.y=H(c.x,c.z);
                    if(Building(id,c,Yaw(c,road.At(mid)),House(id),id=="casa_douro_e"?Shops[shop++%Shops.Length]:null)) {
                        last=id;
                        // (a tree in the back garden now and then: an orange, an olive, a fig)
                        if(rng.NextDouble()<.55){var t=c+n*(size.y*.5f+3.5f+(float)rng.NextDouble()*3);if(ValleyField.RoadDistance(t.x,t.z)>8){t.y=H(t.x,t.z)-.05f;Put(rng.NextDouble()<.5?"olive_tree":"tree_round",t,(float)rng.NextDouble()*360,null,null,true,.75f+(float)rng.NextDouble()*.3f);}}
                        if(rng.NextDouble()<.5){var b=road.At(mid)+n*(half+2.0f)+road.Forward(mid)*(size.x*.35f);b.y=H(b.x,b.z);Put("flower_bush",b,(float)rng.NextDouble()*360,null,null,false,.8f);}
                    }
                    s+=size.x+.6f+(float)rng.NextDouble()*1.8f;
                }
            }
            int k=0;for(float s=s0;s<s1;s+=22,k++){var n=road.Right(s);n.y=0;n.Normalize();if(k%2==1)n=-n;var lp=road.At(s)+n*(half+1.3f);lp.y=H(lp.x,lp.z);Put("lamp_post",lp,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            for(float s=s0+12;s<s1;s+=46){var n=road.Right(s);n.y=0;n.Normalize();var p=road.At(s)+n*(half+.9f);p.y=H(p.x,p.z);Parking.Add((p,Yaw(road.At(s-1),road.At(s+1))));}
        }

        // ---- the Pit Stop at the town gate, and a stone gateway over the road just inside it
        void PitStopAndGate()
        {
            var at=Vinelands.PitStopAt;at.y=H(at.x,at.z);
            if(BayviewKit.Has("pit_stop_3")){PitStop.Build("vinelands",true,at,Vinelands.PitStopYaw,root);footprints.Add((new Vector2(at.x,at.z),new Vector2(14,10),Vinelands.PitStopYaw));Buildings++;}
            var road=HillcrestRoute.Find(Vinelands.RoadName);float s=road.Length-370;var n=road.Right(s);n.y=0;n.Normalize();
            foreach(int side in new[]{1,-1}){var p=road.At(s)+n*side*(road.width*.5f+1.8f);p.y=H(p.x,p.z);Put("map_board",p,Yaw(p,road.At(s)),null,side>0?"VINELANDS":null);}
        }

        // ---- the Adega in the Vineyard Loop's yard, north-west of the junction (the loop leaves west, the lane comes from
        // the north, the loop's last bend from the east)
        void Adega()
        {
            var J=Vinelands.Adega;var at=J+new Vector3(-30,0,26);at.y=H(at.x,at.z);
            if(Building("adega_coop",at,Yaw(at,J),T(0xf6f2ea,0x8b1d2c,0x8b1d2c,0x5a3a24),"ADEGA COOPERATIVA",4.5f))Spots["adega"]=(J+new Vector3(-12,0,10),Yaw(J,at));
        }

        // ---- the Belvedere: the bell tower inside the road's hairpin round it
        void Belvedere()
        {
            if(!Vinelands.Marks.TryGetValue("Belvedere",out var m))return;
            var start=new Vector3(m.x,0,m.y);var dir=new Vector2(Mathf.Cos(160*Mathf.Deg2Rad),Mathf.Sin(160*Mathf.Deg2Rad));var right=new Vector2(dir.y,-dir.x);
            var c=m+right*28;var at=new Vector3(c.x,0,c.y);at.y=H(at.x,at.z);   // (Marks are world x, z)
            if(Building("torre_belvedere",at,Yaw(at,start),T(0xf7f3ea,0x2a5490,0x2a5490,0x2e5e3e),null,4f))Spots["belvedere"]=(at+(start-at).normalized*12,Yaw(at,start));
        }

        // ---- the Cais: three wine lodges between the road and the river, their name walls to the water; rabelos moored
        void Cais()
        {
            var road=HillcrestRoute.Find(Vinelands.SubidaName);float sc=road.Nearest(Vinelands.Cais,out _);
            string[] names={"CAVES DO RIO","LAGARES DA RIBEIRA","VINHOS DA VILA"};int k=0;
            foreach(float off in new[]{-70f,0f,70f}) {
                float s=sc+off;var e=road.Right(s);e.y=0;e.Normalize();if(e.x<0)e=-e;   // (the river side: east)
                var at=road.At(s)+e*(road.width*.5f+3.5f+6f);at.y=H(at.x,at.z);
                Building("armazem_vinho",at,Yaw(at,at+e),T(0xf7f3ea,0x8b1d2c,0x8b1d2c,0x8b1d2c),names[k++],5f,5f);
            }
            // (the quay wall: granite blocks from below the water up to the quay's edge, along its 200 m: round 3's bank was a raw cliff)
            var granite=WorldLook.Kit("Vinelands quay granite",new Color(.6f,.59f,.56f),WorldLook.Pattern.Stone,0,.1f,true);
            for(float s=sc-100;s<=sc+100;s+=6) {
                var e=road.Right(s);e.y=0;e.Normalize();if(e.x<0)e=-e;var f=road.Forward(s);f.y=0;f.Normalize();
                var top=road.At(s);var foot=top+e*(road.width*.5f+3.5f+12f+20f+3f);float y0=Vinelands.Water-1.5f,y1=top.y-.1f;
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="Quay wall";b.transform.SetParent(root);b.transform.SetPositionAndRotation(new Vector3(foot.x,(y0+y1)*.5f,foot.z),Quaternion.LookRotation(f));
                b.transform.localScale=new Vector3(2.2f,y1-y0,6.3f);b.GetComponent<Renderer>().sharedMaterial=granite;
            }
            for(int i=0;i<4;i++) {   // (moored in a line off the quay, bows upstream)
                var f=Vinelands.CaisAt+new Vector2(68,-60+i*38);var w=Vinelands.Centre+f;var p=new Vector3(w.x,Vinelands.Water-.25f,w.y);
                Put("rabelo",p,(i%2==0?-8:6),null,null,false);
            }
        }

        // ---- three quintas on the amphitheatre, facing the river; each name in giant white letters on the slope below
        void Quintas()
        {
            var sites=new[]{(new Vector2(470,380),"QUINTA DO LAGAR"),(new Vector2(560,-300),"QUINTA DAS ANDORINHAS"),(new Vector2(430,-720),"QUINTA DA CANTEIRA")};
            foreach(var (f,name) in sites) {
                var w=Vinelands.Centre+f;var at=new Vector3(w.x,0,w.y);at.y=H(at.x,at.z);var east=new Vector3(1,0,0);
                if(!Building("quinta_casa",at,Yaw(at,at+east),T(0xf7f3ea,0x2e5e3e,0x2e5e3e,0x5a3a24),null,4.5f))continue;
                var l=at+east*70;l.y=H(l.x,l.z)+.2f;   // (the letters stand on the slope 70 m below the house, facing the water)
                Words(name,l,Quaternion.LookRotation(-east),1.15f,new Color(.98f,.97f,.93f));
            }
        }

        // ---- the terraces (docs/85, the signature): the land (Vinelands.Terrace) steps the amphitheatre into benches and
        // risers every 5 m of height. Here each riser gets a schist band laid over it (foot to top: it reads as the dry-stone
        // wall), and each planted bench eight rows of vines along its contour, as merged strips (never one object per vine:
        // docs/68). Contours are found every 2 m along the face by bisecting the built ground between the crest and the river.
        // Some benches are left bare: abandoned terraces, as the valley has. Each 12.8 m of vines counts for the area audit
        // as the vine row it is (AreaAudit.ExtraNature).
        public static int VineMetres,WallMetres;
        void Terraces()
        {
            VineMetres=WallMetres=0;
            var vineMat=WorldLook.Kit("Vinelands vines",new Color(.31f,.47f,.18f),WorldLook.Pattern.Leaf,0,.25f,true);
            var wallMat=WorldLook.Kit("Vinelands schist",new Color(.47f,.43f,.38f),WorldLook.Pattern.DryStone,0,.1f,true);
            var vines=new Strips(root,"Vines",vineMat);var walls=new Strips(root,"Terrace walls",wallMat);
            float Contour(float n,float t,float a0,float a1)   // (the face falls eastward: H(a0) above t, H(a1) below)
            {
                float A(float a){var w=Vinelands.Centre+new Vector2(a,n);return H(w.x,w.y);}
                if(A(a0)<t||A(a1)>t)return float.NaN;
                for(int i=0;i<22;i++){float m=(a0+a1)*.5f;if(A(m)>t)a0=m;else a1=m;}
                return (a0+a1)*.5f;
            }
            bool Ok(Vector2 f)
            {
                var w=Vinelands.Centre+f;if(f.magnitude>Vinelands.Radius-420||f.magnitude<360)return false;
                if(ValleyField.RoadDistance(w.x,w.y)<9)return false;
                foreach(var o in footprints)if(Vector2.Distance(o.c,w)<Mathf.Max(o.half.x,o.half.y)+6)return false;
                return true;
            }
            float step=Vinelands.TerraceStep;
            for(int k=4;k<46;k++) {
                float foot=k*step+1.1f,top=(k+1)*step;var rows=new float[5];for(int j=0;j<5;j++)rows[j]=k*step+.14f+j*.19f;
                var wallRun=new List<(Vector3 a,Vector3 b)>();var rowRuns=new List<Vector3>[5];for(int j=0;j<5;j++)rowRuns[j]=new();
                float since=0;
                for(float n=-1400;n<=1100;n+=3) {
                    var (ca,_)=Vinelands.CrestAtN(n);float ra=Vinelands.RiverAtN(n);float a0=ca+30,a1=ra-110;if(a1<=a0){Flush();continue;}
                    float af=Contour(n,foot,a0,a1),at=Contour(n,top,a0,a1);
                    if(!float.IsNaN(af)&&!float.IsNaN(at)&&Ok(new Vector2(af,n))&&Ok(new Vector2(at,n))) {
                        var wf=Vinelands.Centre+new Vector2(af,n);var wt=Vinelands.Centre+new Vector2(at,n);
                        wallRun.Add((new Vector3(wf.x,H(wf.x,wf.y)+.06f,wf.y),new Vector3(wt.x,H(wt.x,wt.y)+.06f,wt.y)));
                    } else {walls.Band(wallRun,ref WallMetres);wallRun.Clear();}
                    // (planted or bare: parcels from a slow noise along the bench)
                    bool planted=Mathf.PerlinNoise(n*.0045f+k*.37f,k*.71f)>.36f;
                    for(int j=0;j<5;j++) {
                        float ar=planted?Contour(n,rows[j],a0,a1):float.NaN;
                        if(!float.IsNaN(ar)&&Ok(new Vector2(ar,n))){var w=Vinelands.Centre+new Vector2(ar,n);rowRuns[j].Add(new Vector3(w.x,H(w.x,w.y),w.y));}
                        else{vines.Row(rowRuns[j],ref VineMetres);rowRuns[j].Clear();}
                    }
                    void Flush(){walls.Band(wallRun,ref WallMetres);wallRun.Clear();for(int j=0;j<5;j++){vines.Row(rowRuns[j],ref VineMetres);rowRuns[j].Clear();}}
                }
                walls.Band(wallRun,ref WallMetres);for(int j=0;j<5;j++)vines.Row(rowRuns[j],ref VineMetres);
            }
            vines.Finish();walls.Finish();
            Debug.Log($"LITTLE_ROAD_VINELANDS_TERRACES vines={VineMetres} m walls={WallMetres} m tris={vines.Tris+walls.Tris}");
        }
        // Merged strip meshes in chunks of about 60k vertices: a vine row is a rounded box 0.8 m wide and 1.3 m high along its
        // points; a wall band is a ribbon laid over the riser between its foot and top contours.
        sealed class Strips
        {
            readonly Transform parent;readonly string name;readonly Material mat;readonly List<Vector3> v=new();readonly List<int> t=new();public int Tris;int part;float countRun;
            public Strips(Transform parent,string name,Material mat){this.parent=parent;this.name=name;this.mat=mat;}
            public void Row(List<Vector3> p,ref int metres)
            {
                if(p.Count<3)return;
                for(int i=0;i<p.Count;i++) {
                    var dir=(p[Mathf.Min(i+1,p.Count-1)]-p[Mathf.Max(i-1,0)]);dir.y=0;dir.Normalize();var side=new Vector3(-dir.z,0,dir.x)*.4f;
                    int b=v.Count;var q=p[i];v.Add(q-side);v.Add(q-side*.8f+Vector3.up*1.15f);v.Add(q+Vector3.up*1.32f);v.Add(q+side*.8f+Vector3.up*1.15f);v.Add(q+side);
                    if(i>0){for(int s=0;s<4;s++){int a0=b-5+s,a1=b-5+s+1,c0=b+s,c1=b+s+1;t.AddRange(new[]{a0,a1,c0,a1,c1,c0});}Tris+=8;
                        float seg=Vector3.Distance(p[i],p[i-1]);metres+=Mathf.RoundToInt(seg);countRun+=seg;if(countRun>=12.8f){countRun-=12.8f;AreaAudit.ExtraNature.Add(q);}}
                }
                if(v.Count>60000)Finish();
            }
            public void Band(List<(Vector3 a,Vector3 b)> p,ref int metres)
            {
                if(p.Count<3)return;
                for(int i=0;i<p.Count;i++){int b=v.Count;v.Add(p[i].a);v.Add(p[i].b);if(i>0){t.AddRange(new[]{b-2,b-1,b,b-1,b+1,b});Tris+=2;metres+=Mathf.RoundToInt(Vector3.Distance(p[i].a,p[i-1].a));}}
                if(v.Count>60000)Finish();
            }
            public void Finish()
            {
                if(v.Count==0)return;
                var mesh=new Mesh{name=name+" "+part};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(name+" "+part++,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;go.isStatic=true;v.Clear();t.Clear();
            }
        }

        // ---- the river: one water surface at Vinelands.Water down the channel, bank to bank (about 80 m), to the district's
        // edge, where the land falls to the sea bed. InlandWater, as Bramblewick's ford.
        void River()
        {
            WorldLook.EnsureNoise();var inland=Shader.Find("LittleRoad/InlandWater");Material water;
            if(inland!=null){water=new Material(inland){name="River water"};water.SetFloat("_Flow",.15f);water.SetFloat("_DepthFade",.6f);water.SetFloat("_Ripple",.45f);}
            else water=TrialWorld.Material("River water",new Color(.22f,.37f,.4f),0,.72f);
            var v=new List<Vector3>();var t=new List<int>();var r=Vinelands.River;
            for(int i=1;i<r.Length;i++) {
                var a=r[i-1];var ab=r[i]-a;float seg=ab.magnitude;var dir=ab/seg;var across=new Vector2(-dir.y,dir.x)*70;
                for(float u=0;u<=seg;u+=8) {
                    var f=a+ab*(u/seg);var w=Vinelands.Centre+f;if(f.magnitude>Vinelands.Radius-120)continue;
                    var l=w-across;var rr=w+across;int k=v.Count;v.Add(new Vector3(l.x,Vinelands.Water,l.y));v.Add(new Vector3(rr.x,Vinelands.Water,rr.y));
                    if(k>=2){t.AddRange(new[]{k-2,k-1,k,k-1,k+1,k});}   // (clockwise from above: round 2's faced down and was culled)
                }
            }
            var mesh=new Mesh{name="Vinelands river",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject("River water",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=water;
        }
    }
}
