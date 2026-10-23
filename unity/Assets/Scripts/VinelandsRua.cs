using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Vinelands zone 2 (docs/87), with zone 1's machinery (VinelandsVila.cs): Rua Direita, the merchant street from the Largo
    // to the Portela, its calçada pavements, shops, the Pit Stop and its pumps, the grocer's parking and the long gardens
    // behind; Rua da Feira's bus terminus and market hall, and the Terreiro da Feira (the Wednesday stalls, the town's car park
    // under its planes); the Jardim da Cerca, the convent's walled garden. The rua kit is car-forge forge/world/kits/rua.py.
    public sealed partial class VinelandsVila
    {

        // ---- Rua Direita: both sides from the Largo's north edge (n 30) to the Portela (n 520), fronts behind a 2.2 m pavement
        static readonly string[] RuaSigns={"TALHO","PEIXARIA","DROGARIA","BARBEARIA","RETROSARIA","PAPELARIA","SAPATARIA","FLORISTA","OURIVESARIA","PADARIA",
            "TASCA DO ZE","MERCEARIA","ELECTRICA","MODAS LURDES","SEGUROS","QUIOSQUE","FOTO ARTE","CASA DAS LOUCAS"};
        static readonly int[] RuaWashes={0xe8c27a,0xedc9b4,0xe9d4a8,0xd9b48a,0xcfdccb,0xc9d6e3,0xe7b8a8,0xf0d9a0};
        static Vector3[] ruaLine;static Rect ruaBox;
        // the side lanes off it, down to the gardens and the Vale (west) or the slope (east): where the frontage breaks
        static readonly (float n,bool west,string name)[] Travessas={(455,true,"Travessa da Fonte Nova"),(360,false,"Travessa do Arco"),(265,false,"Travessa do Solar"),(150,true,"Travessa das Flores")};
        // (the street's stretch, Largo edge to n 520: where its houses stand, and where the road's gravel shoulders give way to
        // the pavements; known as soon as the roads are, before any shoulder is laid)
        static Vector3[] RuaStretch()
        {
            if(ruaLine!=null)return ruaLine;var road=HillcrestRoute.Find(Vinelands.RuaDireitaName);if(road==null)return null;
            var o=new List<Vector3>();for(float s=0;s<=road.Length;s+=1.5f){var q=road.At(s);float n=q.z-Vinelands.Centre.y;if(n>=30&&n<=520)o.Add(q);}
            ruaLine=o.ToArray();var mn=new Vector2(float.MaxValue,float.MaxValue);var mx=-mn;foreach(var q in ruaLine){mn=Vector2.Min(mn,new(q.x,q.z));mx=Vector2.Max(mx,new(q.x,q.z));}
            ruaBox=Rect.MinMaxRect(mn.x-8,mn.y-8,mx.x+8,mx.y+8);return ruaLine;
        }
        static float DistTo(Vector3[] line,Vector2 q)
        {
            float best=float.MaxValue;
            for(int i=1;i<line.Length;i++){var a=new Vector2(line[i-1].x,line[i-1].z);var ab=new Vector2(line[i].x,line[i].z)-a;float t=Mathf.Clamp01(Vector2.Dot(q-a,ab)/Mathf.Max(1e-4f,ab.sqrMagnitude));best=Mathf.Min(best,(a+ab*t-q).sqrMagnitude);}
            return Mathf.Sqrt(best);
        }
        // (zone 2's paved ground: the Terreiro)
        static bool PavedZone2(Vector3 p)
        {
            // (Rua Direita keeps its gravel shoulders: they're the ramp from the road's edge to the ground, round 1's 188 lips
            // without them; its pavements lie over them)
            var f=new Vector2(p.x,p.z)-Vinelands.Centre;return f.x>12&&f.x<88&&f.y>-218&&f.y<-172;
        }
        // the ground or road under a point (what the paving lies on)
        static float Top(Vector3 p)
        {
            float top=float.MinValue;
            foreach(var hit in Physics.RaycastAll(new Vector3(p.x,400,p.z),Vector3.down,600))if(!hit.collider.isTrigger&&(hit.collider.GetComponent<RoadSurface>()!=null||hit.collider.name.Contains("mainland collider")||hit.collider.name.StartsWith("Junction")))top=Mathf.Max(top,hit.point.y);
            return top==float.MinValue?H(p.x,p.z):top;
        }

        void RuaDireita()
        {
            var line=RuaStretch();var road=HillcrestRoute.Find(Vinelands.RuaDireitaName);if(line==null||line.Length<10)return;float half=road.width*.5f;
            Physics.SyncTransforms();
            foreach(int side in new[]{1,-1}){Pavement(line,half,2.2f,side);Street(line,half+2.3f,side,half);}
        }
        // a pavement along one side: a granite kerb 8 cm up from the road, calçada with a black band along each edge; level
        // across where the ground falls away behind it (it never dips more than 25 cm below the kerb), rising with ground that
        // climbs. Visual only: the cars' ground is the road and the land.
        void Pavement(Vector3[] line,float half,float width,int side)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float run=0;
            for(int i=0;i<line.Length;i++) {
                if(i>0)run+=Vector3.Distance(line[i],line[i-1]);
                var d=line[Mathf.Min(i+1,line.Length-1)]-line[Mathf.Max(i-1,0)];d.y=0;d.Normalize();var n=new Vector3(-d.z,0,d.x)*side;
                var a=line[i]+n*half;var b=line[i]+n*(half+width);float road=Top(a),kerb=road+.08f,outer=Mathf.Max(H(b.x,b.z)+.05f,kerb-.25f);
                int k=v.Count;v.Add(new Vector3(a.x,road-.03f,a.z));v.Add(new Vector3(a.x,kerb,a.z));v.Add(new Vector3(b.x,outer,b.z));
                uv.Add(new Vector2(run/width,.005f));uv.Add(new Vector2(run/width,.01f));uv.Add(new Vector2(run/width,.99f));
                if(i>0){int p=k-3;t.AddRange(new[]{p,p+1,k,p+1,k+1,k,p+1,p+2,k+1,p+2,k+2,k+1});}
            }
            if(t.Count==0)return;
            // (wound so the top faces up, whichever side of the street)
            var n0=Vector3.Cross(v[t[7]]-v[t[6]],v[t[8]]-v[t[6]]);if(n0.y<0)for(int i=0;i<t.Count;i+=3)(t[i+1],t[i+2])=(t[i+2],t[i+1]);
            var mesh=new Mesh{name="Rua Direita pavement",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Rua Direita pavement",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Passeio();go.isStatic=true;
        }
        BayviewKit.Tint Merchant()
        {
            int d=Doors[rng.Next(Doors.Length)];double r=rng.NextDouble();
            int w=r<.42?Tiles[rng.Next(Tiles.Length)]:r<.78?RuaWashes[rng.Next(RuaWashes.Length)]:Whites[rng.Next(Whites.Length)];
            return T(w,d,Awnings[rng.Next(Awnings.Length)],d,Roofs[rng.Next(Roofs.Length)]);
        }
        // one side of the street: merchant houses shoulder to shoulder, the Pit Stop (west, n ~400), the grocer with its parking
        // (west, n ~330), the solar (east, n ~230); lamps on the kerb, cars half on the pavement, long gardens behind
        void Street(Vector3[] line,float offset,int side,float half)
        {
            float total=0;var run=new float[line.Length];for(int i=1;i<line.Length;i++){total+=Vector3.Distance(line[i],line[i-1]);run[i]=total;}
            Vector3 At(float s){for(int i=1;i<line.Length;i++)if(run[i]>=s){float u=(s-run[i-1])/Mathf.Max(.001f,run[i]-run[i-1]);return Vector3.Lerp(line[i-1],line[i],u);}return line[^1];}
            Vector3 Norm(float s){var d=At(Mathf.Min(total,s+1))-At(Mathf.Max(0,s-1));d.y=0;d.Normalize();return new Vector3(-d.z,0,d.x)*side;}
            Vector2 Frame(float s){var q=At(s);return new Vector2(q.x,q.z)-Vinelands.Centre;}
            bool west=Norm(total*.5f).x<0;float s0=3;string last="";bool pit=false,mini=false,solar=false;float gap=-1,nextLamp=side>0?8:26,nextCar=5;int sign=side>0?0:9;
            var noParking=new List<(float a,float b)>();var laneDone=new bool[Travessas.Length];
            void Gap(float a,float b){if(b-a<3)return;var pts=new List<Vector3>();for(float s=a;s<=b+.01f;s+=2){var q=At(s)+Norm(s)*(offset+.3f);q.y=H(q.x,q.z);if(OnWay(q)){LayWall(pts);pts=new List<Vector3>();}else pts.Add(q);}LayWall(pts);
                if(b-a>=4){float m=(a+b)*.5f;var nn=Norm(m);yards.Add((At(m)+nn*(offset+.3f),nn,Quaternion.Euler(0,90,0)*nn,b-a,"horta"));}}
            while(s0<total-4) {
                var f=Frame(s0+4);
                if(west&&!pit&&f.y<Vinelands.PitN+17) {   // (the Pit Stop: the town's garage, parts and body shop, on its own level pad set
                    // back from the street (Vinelands.PitStopAt), its forecourt paved out to the pavement)
                    pit=true;var site=Vinelands.PitStopAt;site.y=H(site.x,site.z);float yaw=Vinelands.PitStopYaw;
                    if(Building("pit_stop_3",site,yaw,null,null,2f,4.4f,2f,pos=>PitStop.Build("vinelands",true,pos,yaw,root))) {
                        Debug.Log($"LITTLE_ROAD_VINELANDS_PITSTOP at {site}");
                        foreach(var (key,hostId) in new[]{("garage","vinelands/garage"),("parts","vinelands/parts"),("body","vinelands/body")})if(PitStop.Hosts.TryGetValue(hostId,out var host))Spots[key]=host;
                        Forecourt(Vinelands.PitStopAt,yaw);
                        if(gap>=0){Gap(gap,s0);gap=-1;}
                        float sEnd=s0;while(sEnd<total-4&&Frame(sEnd).y>Vinelands.PitN-17)sEnd+=1;
                        float ps=sEnd+4;var pa=At(ps)+Norm(ps)*(offset+3.4f);pa.y=H(pa.x,pa.z);float py=Yaw(At(ps-1),At(ps+1));   // (the pumps beside it, under their canopy)
                        if(Put("rua_bombas",pa,py)!=null)footprints.Add((new Vector2(pa.x,pa.z),new Vector2(4.2f,2.8f),py));
                        noParking.Add((s0-4,ps+8));s0=ps+8;last="";continue;
                    }
                    Debug.Log($"LITTLE_ROAD_VINELANDS_PITSTOP refused at {site}");
                }
                int lane=-1;for(int k=0;k<Travessas.Length;k++)if(!laneDone[k]&&Travessas[k].west==west&&f.y<Travessas[k].n){lane=k;break;}
                if(lane>=0) {   // (a travessa: a cobbled lane 30 m back between the houses, their gable windows on it)
                    laneDone[lane]=true;if(gap>=0){Gap(gap,s0);gap=-1;}
                    float mouth=s0+2.75f;var ln=Norm(mouth);var a0=At(mouth)+ln*(half+.2f);var pts=new List<Vector3>();for(float o=0;o<=30;o+=1.5f){var q=a0+ln*o;q.y=H(q.x,q.z);pts.Add(q);}
                    var tl=pts.ToArray();string tn=Travessas[lane].name;Ribbon(tn,tl,3.4f,Setts());Lanes2.Add((tn,3.4f,tl));OnWay(Vector3.zero);ways.Add((tl,2.3f));
                    foreach(float u in new[]{.4f,5.1f}){var fp=At(s0+u)+Norm(s0+u)*(half+.35f);fp.y=Top(At(s0+u)+Norm(s0+u)*half)+.08f;Put("rua_frade",fp,0,null,null,false);}   // (bollards at its mouth)
                    noParking.Add((s0-2,s0+8));s0+=5.5f;last="";continue;
                }
                string id;string fam;bool isMini=false,isSolar=false;
                if(west&&!mini&&f.y<345){id="rua_burguesa_10";fam="rua";isMini=mini=true;}
                else if(!west&&!solar&&f.y<240){id="rua_solar";fam="solar";isSolar=solar=true;}
                else{double r=rng.NextDouble();if(r<.45)id="rua_burguesa_8";else if(r<.7)id="rua_burguesa_10";else{fam=Pick(last,true);if(fam=="varanda"||fam=="fechada"||fam=="granito")fam="loja";id=$"vila_{fam}_{(rng.NextDouble()<.5?6:7)}";}fam=id.StartsWith("rua_")?"rua":id.Split('_')[1];
                    if(id==last){id=id=="rua_burguesa_8"?"rua_burguesa_10":"rua_burguesa_8";}}
                var size=Footprint(id);float midS=s0+size.x*.5f;var n=Norm(midS);var at2=At(midS);var c2=at2+n*(offset+size.y*.5f);c2.y=H(c2.x,c2.z);
                string label=isMini?"MINIMERCADO":isSolar?null:(id.StartsWith("rua_burguesa")||fam=="loja")?RuaSigns[sign++%RuaSigns.Length]:null;
                var tint=isSolar?T(0xf6f1e6,0x2e5e3e,0x2e5e3e,0x5a3a24,0xb5532f):id.StartsWith("rua_")?Merchant():House(fam);
                if(Building(id,c2,Yaw(c2,at2),tint,label,4.8f,4.4f,4.8f)) {
                    if(gap>=0){Gap(gap,s0);gap=-1;}
                    last=id;var along=Quaternion.Euler(0,90,0)*n;yards.Add((c2+n*(size.y*.5f),n,along,size.x,"rua"));
                    if(!isSolar&&dress.NextDouble()<.45){var face=at2+n*(offset+.02f)+along*(size.x*(dress.NextDouble()<.5?.32f:-.32f));Put("vila_lanterna",new Vector3(face.x,Mathf.Max(H(face.x,face.z),c2.y)+3.0f,face.z),Yaw(c2,at2),null,null,false);}
                    if(isMini) {   // (the grocer's yard beside it: three bays nose-in off the street)
                        for(int k=0;k<3;k++){float ss=s0+size.x+3+k*5f;var q=At(ss)+Norm(ss)*(offset+3f);q.y=H(q.x,q.z)+.05f;Parking.Add((q,Yaw(q,At(ss))));}
                        noParking.Add((s0+size.x,s0+size.x+18));s0+=size.x+18;continue;
                    }
                }
                else if(gap<0)gap=s0;
                s0+=size.x+.05f;
            }
            if(gap>=0)Gap(gap,total-1);
            // the pavement: lamps every 36 m at its back, by the fronts; cars up on it at the kerb (about two bays in five: round
            // 2's lamps on the kerb line pushed them out into the traffic lane)
            for(float s=nextLamp;s<total-3;s+=36){var p=At(s)+Norm(s)*(half+1.95f);p.y=Top(At(s)+Norm(s)*half)+.08f;Put("lamp_post",p,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            for(float s=nextCar;s<total-5;s+=7.5f) {
                bool blocked=false;foreach(var (a,b) in noParking)if(s>a&&s<b)blocked=true;if(blocked||dress.NextDouble()>.2)continue;   // (one bay in five: at two in five the street read as a car park, Richard 23 Oct)
                var p=At(s)+Norm(s)*(half+1.0f);p.y=Top(At(s)+Norm(s)*half)+.1f;Parking.Add((p,Yaw(At(s-1),At(s+1))+(side>0?0:180)));
            }
        }
        // the Pit Stop's forecourt: concrete flags from the bays out to the pavement, level with the street, drivable
        void Forecourt(Vector3 site,float yaw)
        {
            // (a 1 m grid, each point at the ground or the floor, whichever is higher, +4 cm: round 1's flat slab sank under the
            // pad's blended edge, and grass showed in front of the parts bay)
            var rot=Quaternion.Euler(0,yaw,0);var v=new List<Vector3>();var t=new List<int>();const int nx=34,nz=13;
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){var q=site+rot*new Vector3(-17+i,0,5.6f+j);v.Add(new Vector3(q.x,Mathf.Max(H(q.x,q.z),site.y)+.04f,q.z));}
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int k=j*(nx+1)+i;t.AddRange(new[]{k,k+nx+1,k+1,k+1,k+nx+1,k+nx+2});}
            var mesh=new Mesh{name="Pit Stop forecourt"};mesh.SetVertices(v);mesh.SetUVs(0,v.ConvertAll(q=>new Vector2(q.x/3f,q.z/3f)));mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            if(mesh.normals[0].y<0){for(int i=0;i<t.Count;i+=3)(t[i+1],t[i+2])=(t[i+2],t[i+1]);mesh.SetTriangles(t,0);mesh.RecalculateNormals();}
            footprints.Add((new Vector2((site+rot*new Vector3(0,0,12)).x,(site+rot*new Vector3(0,0,12)).z),new Vector2(17,7),yaw));   // (no garden wall or yard on it)
            var go=new GameObject("Pit Stop forecourt",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=WorldLook.Kit("Pit forecourt flags",Toy.Hex(0xc9c4b8),WorldLook.Pattern.Stone,0,.12f,true);go.GetComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<RoadSurface>();
        }
        void LayWall(List<Vector3> pts)
        {
            if(pts.Count<2)return;
            var L=Runs.Solve(new RunSpec{name=$"Vila garden wall {walls++}",kit="dry_stone",seed=$"vila-wall-{walls}",along=pts.ToArray()});if(L.failures.Count==0&&L.items.Count>0){Runs.Draw(L,root);Runs.Laid.Add(L);}
        }

        // ---- Rua da Feira: the bus terminus at its top, the market hall (1958) on its north side, the Terreiro at its end
        void Feira()
        {
            var road=HillcrestRoute.Find(Vinelands.FeiraName);if(road==null)return;float half=road.width*.5f;
            Vector3 North(float s){var r=road.Right(s);r.y=0;r.Normalize();return r.z>0?r:-r;}
            {   // the shelter, and the bus waiting at it (Sr. Vítor)
                float s=16;var nn=North(s);var p=road.At(s)+nn*(half+2.4f);p.y=H(p.x,p.z);if(Put("rua_abrigo",p,Yaw(p,road.At(s)))!=null)footprints.Add((new Vector2(p.x,p.z),new Vector2(2.3f,1f),Yaw(p,road.At(s))));
                var b=road.At(s+9)+nn*(half+1.3f);b.y=H(b.x,b.z);Spots["bus"]=(b,Yaw(road.At(s+8),road.At(s+10)));
            }
            {   // the hall on the Terreiro's north edge, its portico to the square (the fairground is its forecourt; on the slope
                // where the plan had it, a 36 m hall couldn't stand: round 1); the stalls under the colonnade, Dona Amélia's van
                var c=F(50,-160);var nn=(F(50,-150)-c).normalized;nn.y=0;nn.Normalize();float yaw=Yaw(c,F(50,-200));
                if(Building("rua_mercado",c,yaw,T(0xf1e6c8,0x2a5490,0x8b1d2c,0x5a3a24),"MERCADO MUNICIPAL",6.5f,1f,4.5f))   // (on a granite terrace plinth where the fill falls away behind it)
                {
                    var along=Quaternion.Euler(0,90,0)*nn;
                    foreach(float u in new[]{-12f,-6f,0f,6f,12f}){var q=c-nn*(10.4f-1.7f)+along*u;q.y=c.y+.1f;Put("rua_banca",q,yaw,T(0xffffff,0xffffff,Awnings[dress.Next(Awnings.Length)],0xffffff),null,false);}
                    var v=c-nn*(10.4f+.9f)+along*20f;v.y=H(v.x,v.z);Spots["fruta"]=(v,Yaw(v,v+along));
                }
            }
            // the Terreiro: paved, planes round its edge; the stalls on its west half, the town's cars on its east
            Plaza("Terreiro da Feira",new Vector2(10,-220),new Vector2(90,-171),Setts(),true);
            var Ty=Vinelands.Terreiro.y;
            for(float a=14;a<=86;a+=12)foreach(float n in new[]{-216.5f,-183.5f}){var t=F(a,n);if(ValleyField.RoadDistance(t.x,t.z)<6.5f)continue;Put("tree_round",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360,null,null,true,1.2f+(float)dress.NextDouble()*.2f);}
            for(float a=18;a<=42;a+=6)foreach(var (n,yw) in new[]{(-195f,0f),(-205f,180f)}){var q=F(a,n);if(ValleyField.RoadDistance(q.x,q.z)<5f)continue;q.y=Mathf.Max(q.y,Ty)+.15f;Put("rua_banca",q,yw,T(0xffffff,0xffffff,Awnings[dress.Next(Awnings.Length)],0xffffff),null,false);}
            for(float a=52;a<=86;a+=3.3f)foreach(var (n,yw) in new[]{(-190f,180f),(-210f,0f)}){if(dress.NextDouble()>.62)continue;var q=F(a,n);if(ValleyField.RoadDistance(q.x,q.z)<5f)continue;q.y=Mathf.Max(q.y,Ty)+.17f;Parking.Add((q,yw));}
        }

        // ---- the Jardim da Cerca: the convent's walls (whitewashed, 3 m; the east one a parapet over the amphitheatre), the
        // gate off the Caminho da Cerca, gravel paths in a cross with the pond at their crossing, box parterres on the upper
        // terrace, the cloister's arches along the north wall, the bandstand on the lower terrace, planes, palms, camellias,
        // benches along the parapet; two granite terrace walls stepping it down the slope
        void Jardim()
        {
            var cal=TrialWorld.Material("Cerca whitewash",new Color(.94f,.92f,.86f),0,.08f);var white=new List<Matrix4x4>();var stone=new List<Matrix4x4>();
            void Wall(Vector2 a,Vector2 b,float height,List<Matrix4x4> into,float thick){var A=F(a.x,a.y);var d=F(b.x,b.y)-A;d.y=0;float L=d.magnitude;if(L<.3f)return;int k=Mathf.CeilToInt(L/2f);var rot=Quaternion.LookRotation(d);
                for(int i=0;i<k;i++){var m=A+d*((i+.5f)/k);var e0=A+d*(i/(float)k);var e1=A+d*((i+1f)/k);float g=H(m.x,m.z),lo=Mathf.Min(g,Mathf.Min(H(e0.x,e0.z),H(e1.x,e1.z)))-.3f,top=g+height;
                    into.Add(Matrix4x4.TRS(new Vector3(m.x,(lo+top)*.5f,m.z),rot,new Vector3(thick,top-lo,L/k+.02f)));}}
            Plaza("Jardim path east-west",new Vector2(101,134.5f),new Vector2(199,140.5f),Gravel(),false);Plaza("Jardim path north-south",new Vector2(147,111),new Vector2(153,164),Gravel(),false);
            Wall(new(100,165),new(200,165),3f,white,.6f);Wall(new(100,110),new(200,110),3f,white,.6f);
            Wall(new(100,110),new(100,123),3f,white,.6f);Wall(new(100,127.6f),new(100,165),3f,white,.6f);Wall(new(200,110),new(200,165),1.1f,white,.6f);
            var gate=F(100,125.3f);Put("rua_portao",gate,Yaw(gate,F(90,125.3f)),null,null,false);
            foreach(var (a,n0,n1) in new[]{(133f,111f,133.5f),(133f,141.5f,164f),(166f,111f,133.5f),(166f,141.5f,164f)})Wall(new(a,n0),new(a,n1),.9f,stone,.45f);   // (the terraces' edges, open at the path)
            // (on granite terrace plinths: the garden falls east; round 1 refused the pond and the bandstand on it)
            var lago=F(150,137.5f);Building("rua_lago",lago,22.5f,null,null,4.5f,1f,4.5f);
            var coreto=F(181,151);Building("rua_coreto",coreto,Yaw(coreto,F(150,137.5f)),null,null,5f,1f,5f);
            var cl=F(128,160.5f);Building("rua_claustro",cl,Yaw(cl,F(128,150)),null,null,4f,1f,4f);
            foreach(var (a,n) in new[]{(110f,118f),(118f,118f),(126f,118f),(110f,126f),(118f,126f),(126f,126f),(110f,148f),(118f,148f),(140f,118f),(140f,126f),(140f,149f)})Building("rua_buxo",F(a,n),0,null,null,3f,1f,3f);
            foreach(var (a,n) in new[]{(172f,117f),(190f,121f),(172f,130f),(112f,157f),(190f,131f)}){var t=F(a,n);Put("tree_round",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360,null,null,true,1.3f+(float)dress.NextDouble()*.2f);}
            if(BayviewKit.Has("palm"))foreach(var (a,n) in new[]{(143f,131f),(157f,144f)}){var t=F(a,n);Put("palm",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360);}
            for(float a=140;a<=196;a+=5){if(a>172&&a<190)continue;var t=F(a,162.5f);Put("rua_camelia",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360);}
            for(float a=104;a<=196;a+=5){if(a>145&&a<156)continue;var t=F(a,112.5f);Put("rua_camelia",new Vector3(t.x,t.y-.05f,t.z),(float)dress.NextDouble()*360);}
            for(float n=116;n<=160;n+=8){if(n>132&&n<143)continue;var b=F(197.5f,n);Put("bench",b,Yaw(b,F(220,n)),null,null,false);}
            foreach(var (a,n,fa,fn) in new[]{(144f,137.5f,150f,137.5f),(156f,137.5f,150f,137.5f),(150f,131.5f,150f,137.5f),(150f,143.5f,150f,137.5f)}){var b=F(a,n);Put("bench",b,Yaw(b,F(fa,fn))+180,null,null,false);}
            foreach(var (a,n) in new[]{(104f,137.5f),(196f,137.5f),(150f,112f),(150f,162f)}){var l=F(a,n);Put("lamp_post",l,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            QuarryFloor.Combine("Jardim da Cerca walls",white,cal,root,true);QuarryFloor.Combine("Jardim terrace walls",stone,plinth,root,true);
        }

        static Material passeio,gravel;
        // a pavement's calçada: white limestone setts, their joints faint, a black basalt band along each edge
        static Material Passeio()
        {
            if(passeio!=null)return passeio;const int N=256;var tex=new Texture2D(N,N,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat,name="Passeio"};var px=new Color32[N*N];var r=new System.Random(11);
            for(int y=0;y<N;y++)for(int x=0;x<N;x++){bool band=y<22||y>N-23;int cx=x%9,cy=(y+(x/9%2)*4)%9;bool joint=cx==0||cy==0;byte c=band?(byte)(joint?40:62):(byte)(joint?200:228+(x*7+y*13)%14);px[y*N+x]=new Color32(c,c,(byte)(band?c+4:c-8),255);}
            tex.SetPixels32(px);tex.Apply(true);passeio=TrialWorld.Material("Passeio calçada",Color.white,0,.1f);passeio.SetTexture("_BaseMap",tex);passeio.mainTexture=tex;return passeio;
        }
        // the garden's paths: raked buff gravel, specked
        static Material Gravel()
        {
            if(gravel!=null)return gravel;const int N=256;var tex=new Texture2D(N,N,TextureFormat.RGBA32,true){wrapMode=TextureWrapMode.Repeat,name="Garden gravel"};var px=new Color32[N*N];var r=new System.Random(12);
            for(int i=0;i<N*N;i++){int k=r.Next(100);byte c=(byte)(k<8?150:k<20?178:200+r.Next(18));px[i]=new Color32(c,(byte)(c-8),(byte)(c-26),255);}
            tex.SetPixels32(px);tex.Apply(true);gravel=TrialWorld.Material("Garden gravel",Color.white,0,.05f);gravel.SetTexture("_BaseMap",tex);gravel.mainTexture=tex;return gravel;
        }
    }
}
