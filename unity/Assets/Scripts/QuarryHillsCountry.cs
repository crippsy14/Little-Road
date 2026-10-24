using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills' countryside and town life (Richard, 3 Oct: "just a few streets with houses either side... Hillview has hot
    // air balloons, farms, and a load of other things"; the roads out of town had "nothing other than grass"):
    //   - hill farms (farmhouse, barn, hay, tractor, a yard and a lane to the road) with flocks of sheep grazing round them;
    //   - the rugby ground (pitch, posts, a stand and the clubhouse), on its own level pad (QuarryHills.Pads);
    //   - the quarry workshops with the Great Wheel, a 15 m water wheel turning beside them;
    //   - a campsite by the source tarn and a wind farm on the southern moor;
    //   - along every road out of town: dry-stone walls with field gates, telegraph poles, a welcome gate; red phone and post boxes
    //     in town.
    // Everything stands on the ground (ValleyField heights at each piece), clear of roads and of the town's buildings.
    public static class QuarryHillsCountry
    {
        public static readonly Vector2 Pitch=new(-2470,-2130);public const float PitchLength=100,PitchWidth=66,PitchYaw=28;
        static Transform root;static System.Random rng;
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static Vector3 G(float x,float z)=>new(x,H(x,z),z);
        static float R()=>(float)rng.NextDouble();
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door,int roof=-1){var t=new BayviewKit.Tint(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));if(roof>=0)t.roof=Toy.Hex(roof);return t;}
        static GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,float scale=1,bool solid=true,Transform parent=null)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,parent??root,tint,sign,solid,scale):null;
        // Clear of roads by `road` m, of the town's buildings, the quarry and the sea.
        static bool Open(float x,float z,float road=8)
        {
            if(!QuarryHills.Contains(x,z)||x>QuarryHills.SeamX-60||QuarryHills.Bare(x,z)||H(x,z)<-4)return false;
            if(ValleyField.RoadDistance(x,z)<road)return false;
            if(Vector2.Distance(new(x,z),Pitch)<110)return false;   // (the rugby ground)
            return QuarryHillsTown.Last==null||QuarryHillsTown.Last.Clear(new Vector2(x,z),new Vector2(4,4),0);
        }
        static float Slope(float x,float z,float r){float lo=float.MaxValue,hi=float.MinValue;for(int k=0;k<8;k++){float a=k*Mathf.PI/4;float h=H(x+Mathf.Cos(a)*r,z+Mathf.Sin(a)*r);lo=Mathf.Min(lo,h);hi=Mathf.Max(hi,h);}return (hi-lo)/(2*r);}
        static readonly List<Matrix4x4> walls=new(),caps=new(),posts=new(),gates=new(),lines=new();
        static readonly List<List<Vector3>> ForgeRuns=new();   // (--prop-forge: each wall stretch as a line, laid as a dry-stone run)
        public static readonly List<Vector3> SheepFields=new();
        public static int Sheep,Farms,WallMetres;

        public static void Build(Transform parent)=>TrialWorld.RunNow(BuildRoutine(parent));
        // (A coroutine: it yields between pieces of work when built in the background, Districts; docs/72.)
        public static System.Collections.IEnumerator BuildRoutine(Transform parent)
        {
            if(!QuarryHills.Enabled)yield break;
            root=new GameObject("Quarry Hills country").transform;root.SetParent(parent);rng=new System.Random(8823);
            walls.Clear();caps.Clear();posts.Clear();gates.Clear();lines.Clear();ForgeRuns.Clear();SheepFields.Clear();Sheep=Farms=WallMetres=LaneSheep=0;
            CoastalLandscape.Lap("qhc start");RugbyGround();CoastalLandscape.Lap("qhc rugby");Workshops();CoastalLandscape.Lap("qhc workshops");yield return null;yield return Farms_();CoastalLandscape.Lap("qhc farms");yield return null;Campsite();CoastalLandscape.Lap("qhc campsite");WindFarm();CoastalLandscape.Lap("qhc wind");Roadsides();CoastalLandscape.Lap("qhc roadsides");TownBits();CoastalLandscape.Lap("qhc townbits");yield return null;
            var stone=WorldLook.Kit("Dry-stone wall",new Color(.43f,.44f,.45f),WorldLook.Pattern.Stone,0,.08f,true,1.6f);
            var cap=WorldLook.Kit("Wall coping",new Color(.36f,.37f,.39f),WorldLook.Pattern.Rock,0,.08f,true);
            QuarryFloor.Combine("Field walls",walls,stone,root,true);QuarryFloor.Combine("Field wall copes",caps,cap,root,false);
            // (with --prop-forge, the field and farmyard walls are Prop Forge's dry-stone runs, in slate: bent along the
            //  roads, following the ground, with piers at their ends; docs/78)
            if(PropForge.Enabled){var slate=new Dictionary<string,string>{{"drystone","drystone_slate"},{"drystone_pier","drystone_slate_pier"}};
                // (a run's field gates are openings cut in it, framed by the wall's own gate piers)
                for(int i=0;i<ForgeRuns.Count;i++){var gs=RunGates.TryGetValue(i,out var g)&&g.Count>0?g:null;
                    var L=Runs.Solve(new RunSpec{name=$"quarry wall {i}",kit="dry_stone",seed=$"quarry-{i}",along=ForgeRuns[i].ToArray(),restyle=slate,cut=gs==null?null:p=>InGateway(p,gs)});Runs.Draw(L,root);WallMetres+=(int)L.length;}
                ForgeRuns.Clear();RunGates.Clear();}
            QuarryFloor.Combine("Telegraph wires",lines,TrialWorld.Material("Wire",new Color(.12f,.12f,.13f),.3f,.4f),root,false);
            QuarryFloor.Combine("Field gates",gates,TrialWorld.Material("Gate paint",new Color(.92f,.92f,.88f),0,.3f),root,true);
            QuarryFloor.Combine("Gate posts",posts,WorldLook.Kit("Gate post stone",new Color(.45f,.45f,.46f),WorldLook.Pattern.Stone,0,.1f,true),root,true);
            Debug.Log($"LITTLE_ROAD_QUARRY_COUNTRY farms={Farms} sheep={Sheep} lane_sheep={LaneSheep} wall_m={WallMetres} gates={gates.Count/5}");
        }

        // ---------------------------------------------------------------- the rugby ground
        static void RugbyGround()
        {
            float y=QuarryHills.PitchY;var rot=Quaternion.Euler(0,PitchYaw,0);Vector3 P(float u,float v)=>new Vector3(Pitch.x,y,Pitch.y)+rot*new Vector3(u,0,v);
            var white=TrialWorld.Material("Pitch lines",new Color(.96f,.96f,.94f),0,.3f);var lineList=new List<Matrix4x4>();
            void Line(Vector3 a,Vector3 b){var d=b-a;lineList.Add(Matrix4x4.TRS((a+b)*.5f+Vector3.up*.03f,Quaternion.LookRotation(d),new Vector3(.18f,.02f,d.magnitude)));}
            float L=PitchLength*.5f,W=PitchWidth*.5f;
            Line(P(-W,-L),P(W,-L));Line(P(-W,L),P(W,L));Line(P(-W,-L),P(-W,L));Line(P(W,-L),P(W,L));Line(P(-W,0),P(W,0));
            foreach(float v in new[]{-L+22,L-22})Line(P(-W,v),P(W,v));
            foreach(float v in new[]{-L-8,L+8})Line(P(-W,v),P(W,v));   // dead-ball lines
            QuarryFloor.Combine("Rugby pitch lines",lineList,white,root,false);
            // H posts: two 11 m uprights 5.6 m apart and the crossbar at 3 m, padded at the foot.
            var post=new List<Matrix4x4>();
            foreach(float v in new[]{-L,L}){foreach(float u in new[]{-2.8f,2.8f}){post.Add(Matrix4x4.TRS(P(u,v)+Vector3.up*5.5f,rot,new Vector3(.16f,11,.16f)));}post.Add(Matrix4x4.TRS(P(0,v)+Vector3.up*3,rot,new Vector3(5.6f,.14f,.14f)));}
            QuarryFloor.Combine("Rugby posts",post,white,root,false);
            var pads=new List<Matrix4x4>();foreach(float v in new[]{-L,L})foreach(float u in new[]{-2.8f,2.8f})pads.Add(Matrix4x4.TRS(P(u,v)+Vector3.up*.9f,rot,new Vector3(.6f,1.8f,.6f)));
            QuarryFloor.Combine("Post pads",pads,TrialWorld.Material("Post pad red",Toy.Red,0,.3f),root,false);
            // The stand along one touchline and the clubhouse behind the far dead-ball line.
            Put("grandstand",P(W+14,0)+Vector3.down*.05f,PitchYaw-90,T(0x8c9095,0xc0392b,0x229954,0xc0392b),"QUARRY HILLS RFC");
            Put("house_c",P(0,L+26)+Vector3.down*.04f,PitchYaw+180,T(0xe9e3d3,0xc0392b,0xc0392b,0xc0392b,0x4b5158),"CLUBHOUSE");
            foreach(float u in new[]{-W-4,W+4})Put("flag_pole",P(u,-L-4),0,T(0xffffff,0xc0392b,0xffffff,0));
            // A muddy practice scrum machine is too far; a few benches instead.
            foreach(float v in new[]{-30f,-10f,10f,30f})Put("bench",P(-W-5,v),PitchYaw+90,null,null,1,false);
            QuarryHillsTown.Spots["rugby"]=(P(-W-8,0),PitchYaw);
        }
        // ---------------------------------------------------------------- the quarry workshops and the Great Wheel
        public static readonly Vector3 Wheel=new(-2185,0,-2800);
        static void Workshops()
        {
            var at=G(Wheel.x,Wheel.z);
            Put("mill",at+new Vector3(-16,-.04f,4),75,T(0x7f868d,0x3b4047,0x3b4047,0xc0392b,0x40464d),"QUARRY WORKSHOPS");
            Put("barn",at+new Vector3(-18,-.04f,-22),75,T(0x8a8478,0x3b4047,0x3b4047,0x3b4047,0x4b5158),"SMITHY");
            // The wheel: a rim, 24 spokes and buckets, turning on an axle between two stone piers; a leat brings the water.
            var wheel=new GameObject("Great Wheel");wheel.transform.SetParent(root);wheel.transform.position=at+Vector3.up*8.2f;wheel.transform.rotation=Quaternion.Euler(0,75,0);
            var iron=TrialWorld.Material("Wheel iron",Toy.Hex(0x8c3b2e),.25f,.35f);var wood=TrialWorld.Material("Wheel buckets",Toy.Hex(0x5a4632),0,.2f);
            var rimParts=new List<Matrix4x4>();var spokes=new List<Matrix4x4>();var buckets=new List<Matrix4x4>();
            const float rad=7.5f;
            for(int k=0;k<48;k++){float a=k*Mathf.PI*2/48;var p=new Vector3(0,Mathf.Sin(a)*rad,Mathf.Cos(a)*rad);
                // (Rim boards run round the wheel: tangent, not radial.)
                foreach(float x in new[]{-.9f,.9f})rimParts.Add(Matrix4x4.TRS(p+Vector3.right*x,Quaternion.Euler(-90-a*Mathf.Rad2Deg,0,0),new Vector3(.18f,.3f,1.05f)));
                if(k%2==0)buckets.Add(Matrix4x4.TRS(p*.93f,Quaternion.Euler(-a*Mathf.Rad2Deg,0,0),new Vector3(1.7f,.7f,.12f)));}
            for(int k=0;k<24;k++){float a=k*Mathf.PI*2/24;foreach(float x in new[]{-.9f,.9f})spokes.Add(Matrix4x4.TRS(new Vector3(x,Mathf.Sin(a)*rad*.5f,Mathf.Cos(a)*rad*.5f),Quaternion.Euler(-a*Mathf.Rad2Deg,0,0),new Vector3(.12f,.12f,rad)));}
            spokes.Add(Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(0,0,90),new Vector3(.7f,2.6f,.7f)));
            QuarryFloor.Combine("Wheel rim",rimParts,iron,wheel.transform,false);QuarryFloor.Combine("Wheel spokes",spokes,iron,wheel.transform,false);QuarryFloor.Combine("Wheel buckets",buckets,wood,wheel.transform,false);
            foreach(Transform c in wheel.transform)c.localPosition=Vector3.zero;   // (Combine bakes world matrices: the parts are in the wheel's space)
            wheel.AddComponent<Spin>().degreesPerSecond=9;
            var stone=WorldLook.Kit("Wheel pier",new Color(.45f,.46f,.47f),WorldLook.Pattern.Stone,0,.1f,true);
            foreach(float x in new[]{-1.9f,1.9f}){var pier=GameObject.CreatePrimitive(PrimitiveType.Cube);pier.name="Wheel pier";pier.transform.SetParent(root);
                pier.transform.SetPositionAndRotation(at+Quaternion.Euler(0,75,0)*new Vector3(x,4.4f,0),Quaternion.Euler(0,75,0));pier.transform.localScale=new Vector3(1.4f,9,2.4f);pier.GetComponent<Renderer>().sharedMaterial=stone;pier.tag="Respawn";}
            // The leat: a stone channel on piers bringing water in over the top of the wheel.
            var leat=GameObject.CreatePrimitive(PrimitiveType.Cube);leat.name="Leat";leat.transform.SetParent(root);var dir=Quaternion.Euler(0,75,0)*Vector3.forward;
            leat.transform.SetPositionAndRotation(at+dir*-14+Vector3.up*16.4f,Quaternion.LookRotation(dir));float leatY=at.y+16;leat.transform.localScale=new Vector3(1.6f,.8f,16);leat.GetComponent<Renderer>().sharedMaterial=stone;
            var water=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(water.GetComponent<Collider>());water.name="Leat water";water.transform.SetParent(leat.transform,false);
            water.transform.localPosition=new Vector3(0,.52f,0);water.transform.localScale=new Vector3(.8f,.05f,1);water.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Leat water",new Color(.25f,.55f,.65f),0,.8f);
            for(int k=1;k<4;k++){var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name="Leat pier";p.transform.SetParent(root);var foot=at+dir*(-14-k*5.5f+8);float g=H(foot.x,foot.z);
                p.transform.SetPositionAndRotation(new Vector3(foot.x,(g+leatY)*.5f,foot.z),Quaternion.LookRotation(dir));p.transform.localScale=new Vector3(1.2f,Mathf.Max(.5f,leatY-g),1.2f);p.GetComponent<Renderer>().sharedMaterial=stone;p.tag="Respawn";}
            QuarryHillsTown.Spots["workshops"]=(at+dir*10+Quaternion.Euler(0,75,0)*Vector3.right*10,255);
        }
        // ---------------------------------------------------------------- hill farms and their sheep
        // (Yields after each farm when built in the background: Districts.)
        static System.Collections.IEnumerator Farms_()
        {
            var seeds=new[]{(new Vector2(-2880,-2060),"TY GWYN"),(new Vector2(-2120,-2050),"BRYN FARM"),(new Vector2(-3330,-2320),"HAFOD"),(new Vector2(-3000,-2650),"PANT Y GOG")};
            foreach(var (seed,name) in seeds) {
                yield return null;
                // The flattest open spot within 220 m of the seed, 25-160 m from a road.
                Vector2 best=default;float bs=float.MaxValue;
                for(int i=0;i<900;i++){var c=seed+new Vector2((R()-.5f)*600,(R()-.5f)*600);if(!Open(c.x,c.y,22))continue;   // (RoadDistance is only known near roads: farther is simply open country)
                    bool clear=true;for(int k=0;k<8&&clear;k++){float a=k*Mathf.PI/4;if(!Open(c.x+Mathf.Cos(a)*22,c.y+Mathf.Sin(a)*22,10))clear=false;}if(!clear)continue;
                    float s=Slope(c.x,c.y,20);if(s<bs){bs=s;best=c;}}
                Debug.Log($"LITTLE_ROAD_FARM {name} slope={bs:0.00} at={best}");
                if(bs>.32f)continue;Farm(best,name);
            }
            // Flocks on the open hills and the moor (away from the farms too). (Only with --animals: Grazer.Animals.)
            for(int i=0;i<60000&&Sheep<520&&Grazer.Animals;i++){float x=QuarryHills.West+300+R()*(QuarryHills.SeamX-QuarryHills.West-500),z=QuarryHills.South+400+R()*2700;
                float h=H(x,z);if(h<6||h>230||!Open(x,z,12)||Slope(x,z,6)>.45f)continue;
                if(Vector2.Distance(new(x,z),QuarryHills.Square)<420||R()>.25f)continue;Flock(new Vector3(x,h,z),4+rng.Next(9));}
        }
        static void Farm(Vector2 c,string name)
        {
            Farms++;var at=G(c.x,c.y);float yaw=R()*360;var rot=Quaternion.Euler(0,yaw,0);Vector3 P(float u,float v){var p=at+rot*new Vector3(u,0,v);p.y=H(p.x,p.z);return p;}
            Put("house_c",P(0,0)+Vector3.down*.04f,yaw,T(0xe9e3d3,0x229954,0xc0392b,0x229954,0x4b5158),name);
            Put("barn",P(18,-6)+Vector3.down*.04f,yaw+90,T(0x8a8478,0x3b4047,0x3b4047,0x3b4047,0x4b5158));
            Put("tractor",P(8,-14),yaw+40,T(0xc0392b,0x222222,0x222222,0x222222));
            for(int i=0;i<6;i++)Put("hay_bale",P(26+(i%3)*1.4f,6+(i/3)*1.4f),yaw);
            // A farmyard wall round the yard (and, with --farm-lanes, a lane to the nearest road).
            if(PropForge.Enabled){var ring=new List<Vector3>();for(float a=24;a<=360;a+=6){var p=at+Quaternion.Euler(0,a,0)*Vector3.forward*28;ring.Add(G(p.x,p.z));}ForgeRuns.Add(ring);}
            else for(float a=0;a<360;a+=12){float r=28;var p=at+Quaternion.Euler(0,a,0)*Vector3.forward*r;var q=at+Quaternion.Euler(0,a+12,0)*Vector3.forward*r;if(a<24)continue;Wall(G(p.x,p.z),G(q.x,q.z));}
            // (Street review, 5 Oct: a farm near town ran its lane to Chapel Street, straight through the back gardens and over
            // a yard wall.) The lane meets the nearest road outside the built-up town (its rows stop 520 m from the square).
            // (Richard's snaps, 24 Oct: "random path that looks awful is entering the road and going through wall. It also
            //  floats all the way down", "please remove". A straight strip laid over the hills, 20 cm above the highest ground
            //  near it, ignoring the roadside walls: gone until a lane can be laid as a road is. --farm-lanes for the old ones.)
            Vector3 road=default;float rd=float.MaxValue;if(RaceTrial.Has("--farm-lanes"))foreach(var r in HillcrestRoute.All()){if(!QuarryHills.Owns(r.name))continue;
                for(float s=0;s<r.Length;s+=4){var q=r.At(s);if(Vector2.Distance(new(q.x,q.z),QuarryHills.Square)<540)continue;float d=Vector2.Distance(new(q.x,q.z),new(at.x,at.z));if(d<rd){rd=d;road=q;}}}
            if(rd<float.MaxValue) {
                Lane(at+rot*Vector3.forward*26,road);
                // A few sheep loose in the lane (docs/60): they amble off when a car comes, and scatter for the horn.
                var mid=Vector3.Lerp(at+rot*Vector3.forward*26,road,.55f);mid.y=H(mid.x,mid.z);LaneFlock(mid,3+rng.Next(3));
            }
            for(int k=0;k<3&&Grazer.Animals;k++){float a=R()*Mathf.PI*2,r=60+R()*140;var f=new Vector3(at.x+Mathf.Cos(a)*r,0,at.z+Mathf.Sin(a)*r);f.y=H(f.x,f.z);if(Open(f.x,f.z,10)&&Slope(f.x,f.z,6)<.4f)Flock(f,7+rng.Next(10));}
            QuarryHillsTown.Spots["farm "+name]=(P(-8,10),yaw);
        }
        static Mesh sheepBody,sheepHead,sheepLeg;static Material wool,face;
        static void Flock(Vector3 centre,int n)
        {
            if(sheepBody==null){sheepBody=Prim(PrimitiveType.Sphere);sheepHead=Prim(PrimitiveType.Sphere);sheepLeg=Prim(PrimitiveType.Cylinder);
                wool=WorldLook.Kit("Fleece",new Color(.93f,.92f,.86f),WorldLook.Pattern.Plaster,0,.05f);face=TrialWorld.Material("Sheep face",new Color(.10f,.10f,.11f),0,.3f);}
            SheepFields.Add(centre);
            for(int i=0;i<n;i++){float a=R()*Mathf.PI*2,r=Mathf.Sqrt(R())*14;var p=new Vector3(centre.x+Mathf.Cos(a)*r,0,centre.z+Mathf.Sin(a)*r);if(!Open(p.x,p.z,5))continue;p.y=H(p.x,p.z);
                var s=new GameObject("Sheep");s.transform.SetParent(root);s.transform.SetPositionAndRotation(p,Quaternion.Euler(0,R()*360,0));float k=.85f+R()*.3f;s.transform.localScale=Vector3.one*k;
                if(PropForge.Enabled)ForgeSheep(s);else{
                Part(s,"Body",sheepBody,wool,new Vector3(0,.62f,0),new Vector3(.78f,.62f,1.1f));Part(s,"Head",sheepHead,face,new Vector3(0,.78f,.62f),new Vector3(.32f,.36f,.42f));
                foreach(var (x,z) in new[]{(-.22f,.32f),(.22f,.32f),(-.22f,-.32f),(.22f,-.32f)})Part(s,"Leg",sheepLeg,face,new Vector3(x,.18f,z),new Vector3(.1f,.18f,.1f));}
                s.AddComponent<Grazer>().Setup(centre,15);Sheep++;}
        }
        static void LaneFlock(Vector3 centre,int n)
        {
            Flock(centre,0);   // (makes the meshes)
            for(int i=0;i<n;i++){float a=R()*Mathf.PI*2,r=R()*4;var p=new Vector3(centre.x+Mathf.Cos(a)*r,0,centre.z+Mathf.Sin(a)*r);p.y=H(p.x,p.z);
                var s=new GameObject("Lane sheep");s.transform.SetParent(root);s.transform.SetPositionAndRotation(p,Quaternion.Euler(0,R()*360,0));s.transform.localScale=Vector3.one*(.85f+R()*.3f);
                if(PropForge.Enabled)ForgeSheep(s);else{
                Part(s,"Body",sheepBody,wool,new Vector3(0,.62f,0),new Vector3(.78f,.62f,1.1f));Part(s,"Head",sheepHead,face,new Vector3(0,.78f,.62f),new Vector3(.32f,.36f,.42f));
                foreach(var (x,z) in new[]{(-.22f,.32f),(.22f,.32f),(-.22f,-.32f),(.22f,-.32f)})Part(s,"Leg",sheepLeg,face,new Vector3(x,.18f,z),new Vector3(.1f,.18f,.1f));}
                var g=s.AddComponent<Grazer>();g.Setup(centre,5);g.lane=true;Sheep++;LaneSheep++;}
        }
        public static int LaneSheep;
        static Mesh Prim(PrimitiveType t){var g=GameObject.CreatePrimitive(t);var m=g.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(g);return m;}
        // (--prop-forge: Prop Forge's sheep, docs/78. A seeded body and head from the sheep kit: the body near in full and
        //  at a distance its LOD1, the head on its LOD1 at the neck, where the grazer nods it)
        public static void ForgeSheep(GameObject s)
        {
            var kit=PropKit.Load("sheep");var at=s.transform.position;int hash=(Mathf.RoundToInt(at.x*7)*73856093)^(Mathf.RoundToInt(at.z*7)*19349663);int v=(hash&0x7fffffff)%4,h=((hash>>8)&0x7fffffff)%3;   // (from where it stands: the layout's own random stream is left alone)
            var body=new GameObject("Body");body.transform.SetParent(s.transform,false);var rs=new List<Renderer>();
            for(int lod=0;lod<2;lod++){var g=new GameObject("LOD"+lod,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(body.transform,false);
                g.GetComponent<MeshFilter>().sharedMesh=kit.Mesh("body_"+v,lod);var r=g.GetComponent<MeshRenderer>();r.sharedMaterials=kit.MaterialsOf("body_"+v,lod);rs.Add(r);}
            var group=body.AddComponent<LODGroup>();group.SetLODs(new[]{new LOD(.12f,new[]{rs[0]}),new LOD(.004f,new[]{rs[1]})});
            var head=new GameObject("Head",typeof(MeshFilter),typeof(MeshRenderer));head.transform.SetParent(s.transform,false);head.transform.localPosition=new Vector3(0,.78f,.62f);
            head.GetComponent<MeshFilter>().sharedMesh=kit.Mesh("head_"+h,1);head.GetComponent<MeshRenderer>().sharedMaterials=kit.MaterialsOf("head_"+h,1);
        }
        static void Part(GameObject parent,string name,Mesh mesh,Material mat,Vector3 at,Vector3 size)
        {
            var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent.transform,false);g.transform.localPosition=at;g.transform.localScale=size;
            g.GetComponent<MeshFilter>().sharedMesh=mesh;var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=name=="Body"?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // A farm lane: a strip of gravel on the ground from the farmyard to the road.
        static void Lane(Vector3 from,Vector3 to)
        {
            var d=to-from;d.y=0;float len=d.magnitude;if(len<4)return;var v=new List<Vector3>();var t=new List<int>();var side=Vector3.Cross(Vector3.up,d.normalized)*2.2f;
            // Every metre, each edge on the highest of the ground there, at the centre and half a metre either way (street
            // review, 5 Oct: at 2 m steps the grass bulged up through it on a convex bank, so it showed in broken pieces).
            // (Street review, 5 Oct: at 6 cm the terrain's own triangles covered it everywhere but the dips, so a lane showed as a
            // chain of pale patches across the fields.) 0.2 m clear of the sampled ground.
            float Top(Vector3 q,Vector3 f){float h=H(q.x,q.z);foreach(var o in new[]{f*1.5f,-f*1.5f,side*.4f,-side*.4f})h=Mathf.Max(h,H(q.x+o.x,q.z+o.z));return h+.2f;}
            var dir=d.normalized;
            for(float s=0;s<=len;s+=1){var p=from+dir*s;var a=p-side;var b=p+side;a.y=Top(a,dir);b.y=Top(b,dir);int k=v.Count;v.Add(a);v.Add(b);if(k>=2)t.AddRange(new[]{k-2,k,k-1,k-1,k,k+1});}
            var mesh=new Mesh{name="Farm lane"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Farm lane",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=WorldLook.Kit("Farm lane gravel",new Color(.55f,.52f,.46f),WorldLook.Pattern.Rock,0,.08f,true);
        }
        // ---------------------------------------------------------------- the tarn campsite and the moor's wind farm
        static void Campsite()
        {
            var c=QuarryHills.Source;var centre=G(c.x+QuarryHills.SourceRadius+34,c.y+20);
            int[] tents={0xc0392b,0x229954,0xf1c40f,0x2e86c1,0xe67e22};int k=0;
            foreach(var (x,z) in new[]{(-8f,6f),(0f,10f),(9f,7f),(-12f,-4f),(12f,-6f)}){var p=centre+new Vector3(x,0,z);p.y=H(p.x,p.z);if(Open(p.x,p.z,10))Put("tent",p,R()*360,T(tents[k++%5],0xffffff,0xffd23f,0xffffff));}
            Put("campfire",centre,0);var cv=centre+new Vector3(-6,0,-14);cv.y=H(cv.x,cv.z);if(Open(cv.x,cv.z,10))Put("caravan",cv,110,T(0xe9e3d3,0xffffff,0x229954,0x229954));
            Put("picnic_table",centre+new Vector3(4,H(centre.x+4,centre.z-3)-centre.y,-3),20,null,null,1,false);
            QuarryHillsTown.Spots["campsite"]=(centre,0);
        }
        static void WindFarm()
        {
            int placed=0;
            for(float x=-3500;x<=-2300&&placed<6;x+=240){float bz=0,bh=float.MinValue;for(float z=-3900;z<=-3550;z+=15){float h=H(x,z);if(h>bh){bh=h;bz=z;}}
                if(bh<170||!Open(x,bz,20))continue;Put("wind_turbine",new Vector3(x,bh-.5f,bz),200,null,null,1,true,BayviewKit.AnimatedRoot);placed++;}
        }
        // ---------------------------------------------------------------- the roads out of town
        static void Roadsides()
        {
            foreach(var road in HillcrestRoute.All()) {
                if(!QuarryHills.Owns(road.name)||QuarryHills.Gravel(road.name)&&road.name==QuarryHills.HaulRoadName)continue;
                Poles(road);
                foreach(int side in new[]{-1,1}) {
                    // The wall's line, sampled every 2 m: where a wall may stand at all (`allowed`: not in town, where the terraces
                    // have their own yards, by the river, near a junction or on another road) and where the ground suits one (level
                    // with the road, and roughly level across the wall's line: street review, 4 Oct, a wall halfway up a cutting bank).
                    // (`Open` here allows 2.5 m to the nearest road's middle, not 3.5: the wall stands 4.5 m out from its own road's,
                    //  and the distance is read off a coarse grid, so a 1 m margin flickered and broke the wall every few metres.)
                    int n=Mathf.Max(0,Mathf.FloorToInt((road.Length-20)/2)+1);var at=new Vector3[n];var hard=new bool[n];var allowed=new bool[n];var ok=new bool[n];
                    for(int i=0;i<n;i++) {
                        float s=10+2*i;var c=road.At(s);var right=road.Right(s)*side;var p=c+right*(road.width*.5f+4.5f);at[i]=G(p.x,p.z);
                        hard[i]=Vector2.Distance(new(p.x,p.z),QuarryHills.Square)>520&&HillcrestRoute.Distance(HillcrestRoute.River.line,p.x,p.z,out _)>30;
                        foreach(var node in QuarryHills.JunctionPoints())if(Vector3.Distance(new(p.x,0,p.z),new(node.x,0,node.z))<30)hard[i]=false;
                        allowed[i]=hard[i]&&Open(p.x,p.z,road.width*.5f+2.5f);
                        if(!allowed[i])continue;
                        var i0=c+right*(road.width*.5f+3.2f);var o0=c+right*(road.width*.5f+5.8f);
                        ok[i]=Mathf.Abs(H(p.x,p.z)-c.y)<1.6f&&Mathf.Abs(H(i0.x,i0.z)-H(o0.x,o0.z))<.9f;
                    }
                    // (Richard's snaps, 24 Oct: "walls have random gaps in them all the way. They should all be joined". Each 2 m
                    //  sample tested the ground on its own, so a bump or a dip anywhere broke the wall in two, with a pier each side.)
                    // Where only the ground fails, for up to 20 m between two good stretches, the wall carries on across; and across
                    // any break of 8 m or less outside the town, the river's banks and the junctions.
                    for(int i=0;i<n;) {
                        if(ok[i]){i++;continue;}int j=i;while(j<n&&!ok[j])j++;
                        bool bridge=i>0&&j<n&&j-i<=10;bool shortGap=j-i<=4;for(int k=i;k<j&&bridge;k++)if(!allowed[k]&&!(shortGap&&hard[k]))bridge=false;
                        if(bridge)for(int k=i;k<j;k++)ok[k]=true;i=j;
                    }
                    // Each unbroken stretch is one wall (8 m or longer, a pier at each end: street review, 4 Oct); a field gate every
                    // 90-170 m, in an opening its own width that the wall's gate piers frame, wall on both sides.
                    var run=new List<Vector3>();var gatesHere=new List<Vector3>();float sinceGate=40+R()*60;
                    void Lay()
                    {
                        if(run.Count>=5) {
                            if(PropForge.Enabled){RunGates[ForgeRuns.Count]=new List<Vector3>(gatesHere);ForgeRuns.Add(new List<Vector3>(run));}
                            else{for(int i=1;i<run.Count;i++)if(!InGateway((run[i-1]+run[i])*.5f,gatesHere))Wall(run[i-1],run[i]);Pier(run[0]);Pier(run[^1]);}
                        }
                        run.Clear();gatesHere.Clear();
                    }
                    for(int i=0;i<n;i++) {
                        if(!ok[i]){Lay();continue;}
                        run.Add(at[i]);sinceGate-=2;
                        bool room=run.Count>=5&&i+4<n;for(int k=1;k<=4&&room;k++)if(!ok[i+k])room=false;
                        if(sinceGate<=0&&room){gatesHere.Add(at[i]);Gate(at[i],at[i+1]-at[i-1]);sinceGate=90+R()*80;}
                    }
                    Lay();
                }
            }
            // The welcome gate on Quarry Road where the town begins, and Slate Quay's at the bottom of Quay Lane.
            var q=HillcrestRoute.Find(QuarryHills.QuarryRoadName);float gs=q.Length-560;HillcrestTown.GroundPots(Put("town_gate",q.At(gs)+Vector3.down*.02f,Quaternion.LookRotation(q.Forward(gs)).eulerAngles.y,T(0xffffff,0,0xc0392b,0),"CROESO I QUARRY HILLS"));
            var ql=HillcrestRoute.Find(QuarryHills.QuayLaneName);float qs=ql.Length-40;HillcrestTown.GroundPots(Put("town_gate",ql.At(qs)+Vector3.down*.02f,Quaternion.LookRotation(ql.Forward(qs)).eulerAngles.y,T(0xffffff,0,0x1f3a93,0),"SLATE QUAY"));
        }
        // A squared end pier, a little taller than the wall, where a stretch stops.
        static void Pier(Vector3 p){walls.Add(Matrix4x4.TRS(p+Vector3.up*.45f,Quaternion.identity,new Vector3(.95f,1.9f,.95f)));caps.Add(Matrix4x4.TRS(p+Vector3.up*1.45f,Quaternion.identity,new Vector3(1.1f,.18f,1.1f)));}
        // A length of dry-stone wall between two ground points: a battered body and a row of upright copes.
        static void Wall(Vector3 a,Vector3 b)
        {
            // (Street review, 5 Oct: level blocks stepped up a slope like a broken stair.) Each length follows the ground.
            var d=b-a;float len=new Vector2(d.x,d.z).magnitude;if(len<.5f||len>4)return;var m=(a+b)*.5f;var rot=Quaternion.LookRotation(d);len=d.magnitude;
            float h=1.15f+(Mathf.PerlinNoise(m.x*.012f,m.z*.012f)-.5f)*.3f;
            walls.Add(Matrix4x4.TRS(m+Vector3.up*(h*.5f-.25f),rot,new Vector3(.7f,h+.5f,len+.05f)));
            caps.Add(Matrix4x4.TRS(m+Vector3.up*(h-.12f),rot,new Vector3(.5f,.28f,len+.02f)));WallMetres+=(int)len;
        }
        // A field gate across its opening in the wall (`along`: the wall's line). With Prop Forge, the farm kit's five-bar gate,
        // hung to swing open when a car drives into it (SwingGate); the wall's own gate piers stand either side of it.
        // Without, two stone posts and a white five-bar gate.
        public const float Gateway=SwingGate.Width+.9f;   // (the opening: the gate, and half a pier's width either side)
        static readonly Dictionary<int,List<Vector3>> RunGates=new();   // (each Prop Forge wall run's gate centres)
        static bool InGateway(Vector3 p,List<Vector3> centres){foreach(var g in centres)if(new Vector2(p.x-g.x,p.z-g.z).sqrMagnitude<Gateway*Gateway*.25f)return true;return false;}
        static void Gate(Vector3 centre,Vector3 along)
        {
            along.y=0;if(along.sqrMagnitude<.01f)return;along.Normalize();
            if(PropForge.Enabled){var at=centre;at.y=H(at.x,at.z);var gate=Put("field_gate",at,Quaternion.LookRotation(along).eulerAngles.y+90,null,null,1,false,BayviewKit.AnimatedRoot);SwingGate.Hang(gate);
                if(RaceTrial.Has("--gate-log"))Debug.Log($"LITTLE_ROAD_FIELD_GATE {at.x:0.0},{at.y:0.0},{at.z:0.0} along {along.x:0.00},{along.z:0.00}");return;}
            var a=centre-along*SwingGate.Width*.5f;var b=centre+along*SwingGate.Width*.5f;a.y=H(a.x,a.z);b.y=H(b.x,b.z);
            var rot=Quaternion.LookRotation(along);var m=(a+b)*.5f;float w=SwingGate.Width;
            posts.Add(Matrix4x4.TRS(a+Vector3.up*.65f,rot,new Vector3(.45f,1.6f,.45f)));posts.Add(Matrix4x4.TRS(b+Vector3.up*.65f,rot,new Vector3(.45f,1.6f,.45f)));
            for(int k=0;k<5;k++)gates.Add(Matrix4x4.TRS(m+Vector3.up*(.25f+k*.24f),rot,new Vector3(.06f,.08f,w-.5f)));
        }
        // Telegraph poles along the road's left side every 45 m, wired pole to pole (Richard's snaps, 24 Oct: "electricity
        // poles stop in the middle of nowhere", "pole isn't joined to the next"). A pole stood only where a wall could, so every
        // break in the wall broke the line. Now a pole stands wherever there's open ground for it, 6.5 m off the road or, failing
        // that, 4.5 or 9 m; the line only ends in town, at the river or where the road meets another.
        static void Poles(HillcrestRoute.Road road)
        {
            Vector3? last=null;float since=45;
            for(float s=10;s<road.Length-10;s+=2) {
                since+=2;if(since<45)continue;
                var c=road.At(s);var left=-road.Right(s);Vector3? spot=null;
                foreach(float off in new[]{6.5f,4.5f,9f}) {
                    var tp=c+left*(road.width*.5f+off);
                    if(Vector2.Distance(new(tp.x,tp.z),QuarryHills.Square)>520&&Open(tp.x,tp.z,road.width*.5f+off-1)&&HillcrestRoute.Distance(HillcrestRoute.River.line,tp.x,tp.z,out _)>14){tp.y=H(tp.x,tp.z);spot=tp;break;}
                }
                if(spot==null){if(last.HasValue&&Vector2.Distance(new(last.Value.x,last.Value.z),new(c.x,c.z))>70)last=null;continue;}
                var obj=Put("telegraph_pole",spot.Value,Quaternion.LookRotation(road.Forward(s)).eulerAngles.y+90);if(obj==null)return;
                if(last.HasValue&&Vector3.Distance(last.Value,spot.Value)<75){var a=last.Value+Vector3.up*7.2f;var b=spot.Value+Vector3.up*7.2f;var d=b-a;lines.Add(Matrix4x4.TRS((a+b)*.5f+Vector3.down*.3f,Quaternion.LookRotation(d),new Vector3(.04f,.04f,d.magnitude)));}
                last=spot;since=0;
            }
        }
        // ---------------------------------------------------------------- in town: phone and post boxes, the chapel's graveyard
        static void TownBits()
        {
            var red=TrialWorld.Material("Pillar box red",Toy.Hex(0xc0392b),0,.35f);var glass=TrialWorld.Material("Kiosk glass",new Color(.75f,.85f,.9f),0,.6f);
            var sq=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);
            // (--prop-forge: the street kit's phone and pillar boxes, not primitives: docs/80 round 4)
            bool forge=PropForge.Enabled&&BayviewKit.Has("phone_box")&&BayviewKit.Has("pillar_box");
            void Kiosk(Vector3 p,float yaw){p.y=H(p.x,p.z);if(forge){BayviewKit.Spawn("phone_box",p,yaw,root);return;}var k=GameObject.CreatePrimitive(PrimitiveType.Cube);k.name="Phone box";k.transform.SetParent(root);k.transform.SetPositionAndRotation(p+Vector3.up*1.25f,Quaternion.Euler(0,yaw,0));k.transform.localScale=new Vector3(1.05f,2.5f,1.05f);k.GetComponent<Renderer>().sharedMaterial=red;k.tag="Respawn";
                foreach(var o in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right}){var w=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(w.GetComponent<Collider>());w.transform.SetParent(k.transform,false);w.transform.localPosition=o*.505f+Vector3.up*.05f;w.transform.localScale=new Vector3(o.x!=0?.02f:.7f,.55f,o.z!=0?.02f:.7f);w.GetComponent<Renderer>().sharedMaterial=glass;}}
            void Pillar(Vector3 p){p.y=H(p.x,p.z);if(forge){BayviewKit.Spawn("pillar_box",p,0,root);return;}var b=GameObject.CreatePrimitive(PrimitiveType.Cylinder);b.name="Post box";b.transform.SetParent(root);b.transform.position=p+Vector3.up*.75f;b.transform.localScale=new Vector3(.55f,.75f,.55f);b.GetComponent<Renderer>().sharedMaterial=red;b.tag="Respawn";}
            Kiosk(sq+new Vector3(-17,0,13),45);Pillar(sq+new Vector3(15,0,-15));
            Kiosk(QuarryHills.QuayPoint(-60,20,QuarryHills.QuayY),0);Pillar(QuarryHills.QuayPoint(-48,21,QuarryHills.QuayY));
            // Capel Bethel's graveyard: slate headstones in rows behind the chapel.
            if(QuarryHillsTown.Spots.TryGetValue("chapel",out var chapel)) {
                var slate=WorldLook.Kit("Headstone slate",new Color(.30f,.31f,.35f),WorldLook.Pattern.Slate,0,.06f,true);var stones=new List<Matrix4x4>();
                var back=Quaternion.Euler(0,chapel.yaw,0)*Vector3.back;var across=Quaternion.Euler(0,chapel.yaw,0)*Vector3.right;
                for(int r=0;r<4;r++)for(int c=-5;c<=5;c++){var p=chapel.at+back*(14+r*2.6f)+across*c*1.8f;if(!Open(p.x,p.z,4))continue;p.y=H(p.x,p.z);stones.Add(Matrix4x4.TRS(p+Vector3.up*.45f,Quaternion.Euler(R()*6-3,chapel.yaw,R()*6-3),new Vector3(.6f,.9f,.1f)));}
                QuarryFloor.Combine("Headstones",stones,slate,root,false);
            }
        }
    }
    // Slow constant turning about the local x axis (the Great Wheel).
    public sealed class Spin:MonoBehaviour{public float degreesPerSecond=10;void Update()=>transform.Rotate(degreesPerSecond*Time.deltaTime,0,0,Space.Self);}
    // A sheep: grazes (head down), ambles a few metres now and then within its field, never far from home; only near the player.
    // Startled (the horn, or a car bearing down on a lane sheep) it trots off away from the noise, then drifts back.
    public sealed class Grazer:MonoBehaviour
    {
        Vector3 home,target;float radius,wait,headPhase,trot;Transform head;
        public System.Func<float,float,float> ground;   // (the land it walks on: the mainland's unless set, as on Bayview's farm)
        public bool lane;public Vector3 headAt=new(0,.78f,.62f);public float nod=.35f;   // (a cow's head is higher and further forward: Bramblewick)
        public static Transform Car;static Vector3 carLast;static float carSpeed;static int carFrame=-1;
        public void Startle(Vector3 from)
        {
            var away=transform.position-from;away.y=0;if(away.sqrMagnitude<.01f)away=transform.right;
            away=Quaternion.Euler(0,Random.Range(-35f,35f),0)*away.normalized;target=transform.position+away*Random.Range(9f,15f);trot=Random.Range(2.2f,3f);wait=Random.Range(6f,12f);
        }
        public static readonly List<Grazer> All=new();   // (Ambience counts the sheep near you)
        // No animals in a world of cars (Richard, 24 Oct: "Animals all look bad, lets remove them all from all areas. We are in
        // a car world so anything not car related isn't canon"). The builders place none; any other place's animal (Bayview's
        // farm sheep) is taken away as it wakes. --animals brings them back.
        public static bool Animals=>RaceTrial.Has("--animals");
        void Awake(){if(!Animals)Destroy(gameObject);}
        void OnEnable()=>All.Add(this);
        void OnDisable()=>All.Remove(this);
        public void Setup(Vector3 field,float r){home=field;radius=r;target=transform.position;wait=Random.Range(1f,8f);head=transform.Find("Head");headPhase=Random.value*6;}
        void Update()
        {
            var cam=Camera.main;if(cam!=null&&(cam.transform.position-transform.position).sqrMagnitude>250*250)return;
            // A lane sheep steps out of the way of a car coming at more than walking pace.
            if(lane&&Car!=null) {
                if(carFrame!=Time.frameCount){carFrame=Time.frameCount;carSpeed=Time.deltaTime>0?Vector3.Distance(Car.position,carLast)/Time.deltaTime:0;carLast=Car.position;}
                if(trot<=0&&carSpeed>2&&(Car.position-transform.position).sqrMagnitude<11*11)Startle(Car.position);
            }
            float gait=trot>0?trot:.6f;if(trot>0&&(target-transform.position).sqrMagnitude<.1f)trot=0;
            if(head!=null)head.localPosition=new Vector3(headAt.x,trot>0?headAt.y+.08f:headAt.y-Mathf.Max(0,Mathf.Sin(Time.time*.6f+headPhase))*nod,headAt.z);
            var d=target-transform.position;d.y=0;
            if(d.magnitude<.2f){trot=0;wait-=Time.deltaTime;if(wait<=0){var a=Random.value*Mathf.PI*2;target=home+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Random.Range(0f,radius);wait=Random.Range(4f,14f);}return;}
            var step=d.normalized*Mathf.Min(d.magnitude,gait*Time.deltaTime);var p=transform.position+step;p.y=ground!=null?ground(p.x,p.z):ValleyField.Height(p.x,p.z);transform.position=p;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),(trot>0?360:90)*Time.deltaTime);
        }
    }
}
