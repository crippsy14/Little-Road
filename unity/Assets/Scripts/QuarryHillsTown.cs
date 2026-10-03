using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Quarry Hills town (docs/59): slate country terraces, built in blocks the way the quarry towns were. Every street is
    // lined with quarrymen's houses behind small front gardens and a pavement; behind each row a walled back yard, a back
    // lane, the next row's yards and a second terrace (its front on a footpath); in the core a third row faces it across
    // the path. Mostly grey stone with slate roofs, some limewashed or painted, bright doors; shop fronts with a light
    // Welsh flavour; the square with the Institute, Capel Bethel and the Caban café; the uniform Pit Stop on Quarry Road
    // as you arrive. Placement follows Hillcrest's rules (floor on the frontage's level, a stone plinth fills any drop,
    // nothing on a road) plus a footprint test, so the rows of neighbouring streets never run into each other.
    public sealed class QuarryHillsTown
    {
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Spots=new();
        // Front gardens of the street houses (parking spot, parallel yaw, house kit): ValleyLife parks residents there.
        public static readonly List<(Vector3 yard,float yaw,string house)> Homes=new();
        readonly Transform root;readonly System.Random rng=new(6011);
        Material plinth,paving,laneGravel,stone;int buildings,plinths,lamps,backHouses;
        readonly List<Vector3> streetLamps=new();
        readonly List<Matrix4x4> walls=new(),paths=new();
        public QuarryHillsTown(Transform parent){root=new GameObject("Quarry Hills town").transform;root.SetParent(parent);}
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return Quaternion.LookRotation(d).eulerAngles.y;}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door,int roof){var t=new BayviewKit.Tint(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));t.roof=Toy.Hex(roof);return t;}
        // Slate-country walls: grey and blue-grey stone mostly, then limewash and the odd painted front.
        static readonly int[] Stone={0x7f868d,0x8c9095,0x737a82,0x969089,0x8a8478,0x7c8388,0x9aa0a4};
        static readonly int[] Painted={0xe9e3d3,0xd6dfe4,0xdcc9a6,0xbac8b4,0xe5bfa8,0xf0dc9a,0xc9d3e8,0xf2c9c9,0xa9c9d9};
        static readonly int[] Doors={0xc0392b,0x2e86c1,0x229954,0xf1c40f,0x8e44ad,0x16a085,0xd35400,0x1f3a93};
        static readonly int[] Roofs={0x4b5158,0x565c63,0x40464d,0x50555c};
        BayviewKit.Tint RandomTint(float painted=.32f)
        {
            int wall=rng.NextDouble()>painted?Stone[rng.Next(Stone.Length)]:Painted[rng.Next(Painted.Length)];int door=Doors[rng.Next(Doors.Length)];
            return T(wall,rng.NextDouble()<.5?0xf4f1ea:door,Doors[rng.Next(Doors.Length)],door,Roofs[rng.Next(Roofs.Length)]);
        }
        static readonly string[] ShopNames={"CAFFI","BARA","SIOP","POST","CIGYDD","LLYFRAU","CHIPS","HARDWARE","SLATE CRAFTS","CHEMIST","BAKERY","WOOL","SWEETS","TOOLS","PIES","BIKES"};
        int shopName;
        static Vector2 Footprint(string id)=>id switch{"terrace_a"=>new(5.4f,8),"terrace_b"=>new(5.4f,8),"terrace_c"=>new(5.4f,8),"terrace_d"=>new(5.4f,8),"terrace_e"=>new(5.4f,8),"terrace_f"=>new(5.4f,8),"terrace_g"=>new(5.4f,8),
            "house_a"=>new(8.5f,7.5f),"house_b"=>new(6.2f,7.5f),"house_c"=>new(10.8f,8.5f),"house_d"=>new(5.6f,7.4f),"cafe"=>new(9,8),"shop"=>new(9,8),"post_office"=>new(9,8),
            "church"=>new(12,14),"town_hall"=>new(20,16),"station"=>new(22,8),"mill"=>new(12,11),_=>new(8,8)};
        GameObject Box(string name,Vector3 p,Vector3 size,Quaternion rotation,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,rotation);go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;if(solid)go.tag="Respawn";else Object.Destroy(go.GetComponent<Collider>());return go;
        }
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,float scale=1,bool solid=true)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,solid,scale):null;

        // ---------------------------------------------------------------- footprints
        // Every building's ground rectangle (centre, half size along its width and depth, yaw), in 24 m cells.
        readonly Dictionary<(int,int),List<(Vector2 c,Vector2 half,float yaw)>> taken=new();
        const float Cell=24;
        static (Vector2 x,Vector2 z) Axes(float yaw){float a=yaw*Mathf.Deg2Rad;return (new Vector2(Mathf.Cos(a),-Mathf.Sin(a)),new Vector2(Mathf.Sin(a),Mathf.Cos(a)));}
        static bool Overlap(Vector2 c1,Vector2 h1,float y1,Vector2 c2,Vector2 h2,float y2)
        {
            var (x1,z1)=Axes(y1);var (x2,z2)=Axes(y2);var d=c2-c1;
            foreach(var a in new[]{x1,z1,x2,z2}) {
                float r1=h1.x*Mathf.Abs(Vector2.Dot(x1,a))+h1.y*Mathf.Abs(Vector2.Dot(z1,a)),r2=h2.x*Mathf.Abs(Vector2.Dot(x2,a))+h2.y*Mathf.Abs(Vector2.Dot(z2,a));
                if(Mathf.Abs(Vector2.Dot(d,a))>r1+r2)return false;
            }
            return true;
        }
        IEnumerable<(int,int)> Cells(Vector2 c,Vector2 half){float r=half.magnitude;for(int i=Mathf.FloorToInt((c.x-r)/Cell);i<=Mathf.FloorToInt((c.x+r)/Cell);i++)for(int j=Mathf.FloorToInt((c.y-r)/Cell);j<=Mathf.FloorToInt((c.y+r)/Cell);j++)yield return (i,j);}
        bool Free(Vector2 c,Vector2 half,float yaw){foreach(var k in Cells(c,half))if(taken.TryGetValue(k,out var list))foreach(var o in list)if(Overlap(c,half,yaw,o.c,o.half,o.yaw))return false;return true;}
        void Take(Vector2 c,Vector2 half,float yaw){foreach(var k in Cells(c,half)){if(!taken.TryGetValue(k,out var list))taken[k]=list=new();list.Add((c,half,yaw));}}

        // Stands a building with its floor at `centre`'s height: a stone plinth fills any drop; the ground may rise up to
        // `bury` metres behind it (cut into the hillside, as the terraces are).
        bool Building(string id,Vector3 centre,float yaw,BayviewKit.Tint tint,string sign=null,bool force=false,float bury=6)
        {
            var size=Footprint(id);var rot=Quaternion.Euler(0,yaw,0);float floor=centre.y,low=floor,high=floor;
            var c2=new Vector2(centre.x,centre.z);var half=size*.5f-new Vector2(.2f,.2f);
            if(!force&&!Free(c2,half,yaw))return false;
            foreach(var c in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(0,0)}) {
                var p=centre+rot*new Vector3(c.x*size.x*.5f,0,c.y*size.y*.5f);float g=H(p.x,p.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                if(!force&&ValleyField.RoadDistance(p.x,p.z)<5.3f)return false;
            }
            if(!force&&(floor-low>7.5f||high-floor>bury))return false;
            if(floor-low>.25f){Box("Stone plinth",new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),new Vector3(size.x+.5f,floor-low+.5f,size.y+.5f),rot,plinth);plinths++;}
            Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign);buildings++;Take(c2,half,yaw);
            // Chimney tops (kit x mirrors on import): some of them smoke, as coal fires do.
            if((id.StartsWith("terrace")||id=="house_a"||id=="house_c")&&rng.NextDouble()<.3)HillcrestTown.Chimneys.Add(centre+rot*(id switch{"house_a"=>new Vector3(-2.6f,10.5f,-1.2f),"house_c"=>new Vector3(-2.8f,10.2f,-1.4f),
                "terrace_e"=>new Vector3(-1.9f,15.2f,2.7f),"terrace_f"=>new Vector3(-1.9f,10.7f,-.6f),"terrace_g"=>new Vector3(1.6f,13.1f,-2.4f),_=>new Vector3(-1.4f,12.1f,-1.5f)}));
            return true;
        }
        public void Build()
        {
            if(!QuarryHills.Enabled||!BayviewKit.Has("terrace_a"))return;
            Spots.Clear();Homes.Clear();
            plinth=WorldLook.Kit("Quarry Hills slate plinth",Toy.Hex(0x7d838a),WorldLook.Pattern.Stone,0,.12f,true);
            paving=WorldLook.Kit("Quarry Hills square flags",Toy.Hex(0x8f9296),WorldLook.Pattern.Stone,0,.15f,true);
            laneGravel=WorldLook.Kit("Quarry Hills back lane",Toy.Hex(0x6f7174),WorldLook.Pattern.Rock,0,.1f,true);
            stone=WorldLook.Kit("Quarry Hills yard wall",Toy.Hex(0x80858a),WorldLook.Pattern.Stone,0,.1f,true);
            Square();PitStopAndShops();Streets();FlushWalls();KettleGate();
            Debug.Log($"LITTLE_ROAD_QUARRY_TOWN buildings={buildings} back={backHouses} plinths={plinths} lamps={lamps} homes={Homes.Count}");
        }
        // ---------------------------------------------------------------- the square
        void Square()
        {
            var c=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);
            // Paved square (slate flags) under the junction's edges; nothing else builds on it.
            var pave=Box("Square paving",c+Vector3.down*.18f,new Vector3(46,.4f,46),Quaternion.identity,paving,false);
            Take(new Vector2(c.x,c.z),new Vector2(25,25),0);
            Vector3 Corner(float bearing,float r){float a=bearing*Mathf.Deg2Rad;var p=c+new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);p.y=QuarryHills.SquareY;return p;}
            // The roads come in roughly from the four quarters: the landmarks stand on the diagonals, facing in.
            var inst=Corner(135,36);Building("town_hall",inst,Yaw(inst,c),T(0x8c9095,0xf4f1ea,0xc0392b,0x1f3a93,0x40464d),"INSTITUTE",true);
            var chapel=Corner(225,34);Building("church",chapel,Yaw(chapel,c),T(0x969089,0xf4f1ea,0x2e86c1,0x229954,0x4b5158),"CAPEL BETHEL",true);
            var caban=Corner(315,30);Building("cafe",caban,Yaw(caban,c),T(0xe9e3d3,0xc0392b,0xc0392b,0xc0392b,0x4b5158),"CABAN",true);Spots["caban"]=(Corner(315,21),Yaw(caban,c)+90);
            var shop=Corner(45,30);Building("terrace_b",shop,Yaw(shop,c),T(0xf0dc9a,0x1f3a93,0x2e86c1,0x1f3a93,0x565c63),"SIOP",true);
            Spots["institute"]=(inst,Yaw(inst,c));Spots["chapel"]=(chapel,Yaw(chapel,c));
            // Each landmark's neighbours: a short run of terraces either side, so the square is enclosed.
            foreach(var (b,r) in new[]{(135f,36f),(225f,34f),(315f,30f),(45f,30f)})
                foreach(float off in new[]{-24f,-18f,18f,24f}) {
                    var p=Corner(b+off,r+2);var id=new[]{"terrace_a","terrace_d","terrace_f","terrace_b"}[rng.Next(4)];
                    Building(id,p,Yaw(p,c),RandomTint(.5f),id=="terrace_b"?ShopNames[shopName++%ShopNames.Length]:null,false,4);
                }
            // A fountain in the middle, stalls on market day (every day), benches, flags in the Welsh colours, bunting (all
            // stood on the ground itself: the junction's cut leaves it a little under the square's level in places).
            Vector3 OnGround(Vector3 p){p.y=H(p.x,p.z);return p;}
            Put("fountain",OnGround(c)+Vector3.down*.02f,0);
            int k=0;foreach(float b in new[]{60f,120f,240f,300f}){var p=OnGround(Corner(b,11));Put("market_stall",p,Yaw(p,c),T(0xffffff,0,new[]{0xc0392b,0x229954,0xf1c40f,0x2e86c1}[k++%4],0,0x4b5158));}
            foreach(float b in new[]{90f,180f,270f,0f}){var p=OnGround(Corner(b+40,17));Put("bench",p,Yaw(p,c)+180);}
            foreach(float x in new[]{-4f,0f,4f}){var p=OnGround(Corner(180,6)+new Vector3(x,0,0));Put("flag_pole",p,0,T(0xffffff,x==0?0xc0392b:0x229954,x==0?0xffffff:0xc0392b,0,0xffffff));}
            var ring=new List<Vector3>();
            foreach(float b in new[]{20f,70f,110f,160f,200f,250f,290f,340f}){var p=Corner(b,21);p.y=H(p.x,p.z);if(Put("lamp_post",p,0,T(0xffffff,0x1f3a93,0xffffff,0xffffff,0xffffff),null,1,false)!=null){lamps++;streetLamps.Add(p);ring.Add(p);}}
            // Round the square lamp to lamp, and two runs across it.
            for(int i=0;i<ring.Count;i++)HillcrestTown.BuntingLine(root,ring[i]+Vector3.up*4.4f,ring[(i+1)%ring.Count]+Vector3.up*4.4f,.9f);
            if(ring.Count==8)foreach(int i in new[]{1,3})HillcrestTown.BuntingLine(root,ring[i]+Vector3.up*4.4f,ring[i+4]+Vector3.up*4.4f,2.2f);
            Spots["square"]=(c,0);
        }
        void Lamp(Vector3 p){if(Put("lamp_post",p,0,T(0xffffff,0x1f3a93,0xffffff,0xffffff,0xffffff),null,1,false)!=null){lamps++;streetLamps.Add(p);}}
        // The end of the Kettle Falls road for now: a gate across it, closed, with chevrons behind.
        void KettleGate()
        {
            var road=HillcrestRoute.Find(QuarryHills.KettleRoadName);float s=road.Length-6;var at=road.At(s);var f=road.Forward(s);
            Put("town_gate",at+Vector3.down*.02f,Quaternion.LookRotation(-f).eulerAngles.y,T(0xf4f1ea,0,0xc0392b,0,0xffffff),"KETTLE FALLS - ROAD CLOSED");
            foreach(float x in new[]{-2.5f,0f,2.5f}){var p=road.At(road.Length-1)+Vector3.Cross(Vector3.up,f)*x+f*2;p.y=H(p.x,p.z);Put("chevron_sign",p,Quaternion.LookRotation(-f).eulerAngles.y);}
        }
        // ---------------------------------------------------------------- Pit Stop
        void PitStopAndShops()
        {
            var site=QuarryHills.PitStopSite;
            PitStop.Build("quarry",true,new Vector3(site.at.x,site.at.y-.03f,site.at.z),site.yaw,root);
            Take(new Vector2(site.at.x,site.at.z),new Vector2(24,19),site.yaw);
            if(PitStop.Hosts.TryGetValue("quarry/garage",out var g))Spots["qgarage"]=g;
            if(PitStop.Hosts.TryGetValue("quarry/parts",out var p))Spots["qparts"]=p;
            if(PitStop.Hosts.TryGetValue("quarry/body",out var b))Spots["qbody"]=b;
        }
        // ---------------------------------------------------------------- streets
        // Distances out from a street's edge (its half width): pavement, front gardens, the street row, its yard, the back
        // lane, the back row's yard and the back row, its footpath, and (in the core) the third row facing it.
        const float Pave=2.3f,Garden=2.6f,Yard=4.5f,LaneW=3.6f,Path=7.4f;
        void Streets()
        {
            var quarry=HillcrestRoute.Find(QuarryHills.QuarryRoadName);var high=HillcrestRoute.Find(QuarryHills.HighStreetName);
            var quay=HillcrestRoute.Find(QuarryHills.QuayLaneName);var haul=HillcrestRoute.Find(QuarryHills.HaulRoadName);
            var chapel=HillcrestRoute.Find(QuarryHills.ChapelStreetName);var terrace=HillcrestRoute.Find(QuarryHills.TerraceRowName);
            var streets=new List<(HillcrestRoute.Road road,float from,float to)>{
                (high,30,high.Length-10),(quarry,quarry.Length-430,quarry.Length-30),(quay,30,Mathf.Min(quay.Length-60,420)),
                (haul,30,Mathf.Min(haul.Length-40,260)),(chapel,14,chapel.Length-14),(terrace,14,terrace.Length-14)};
            // Street rows first on every street (they claim the frontages), then the blocks behind them.
            foreach(var (road,from,to) in streets)foreach(int side in new[]{-1,1})StreetRow(road,side,from,to);
            foreach(var (road,from,to) in streets)foreach(int side in new[]{-1,1})Block(road,side,from,to);
            foreach(var (road,from,to) in streets){Pavements(road,from-6,to+6);}
            for(float s=50;s<high.Length-40;s+=64)StreetBunting(high,s);
            // Benches on the pavements, lamps along Quarry Road's approach, map boards in the square and by the Pit Stop.
            foreach(var (road,from,to) in streets){int k=0;for(float s=from+20;s<to-20;s+=46,k++)Bench(road,s,k%2==0?1:-1);}
            for(float s=quarry.Length-900;s<quarry.Length-430;s+=36){int side=((int)(s/36))%2==0?1:-1;var lp=quarry.At(s)+quarry.Right(s)*side*(quarry.width*.5f+.9f);lp.y=quarry.At(s).y;if(ValleyField.RoadDistance(lp.x,lp.z)>quarry.width*.5f-.5f)Lamp(lp);}
            var sq=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);var mb=sq+new Vector3(18,0,-18);mb.y=H(mb.x,mb.z);Put("map_board",mb,Yaw(mb,sq));
            var ps=QuarryHills.PitStopSite.at;var mb2=ps+Quaternion.Euler(0,QuarryHills.PitStopSite.yaw,0)*new Vector3(16,0,14);mb2.y=H(mb2.x,mb2.z);Put("map_board",mb2,QuarryHills.PitStopSite.yaw);
            for(float s=quarry.Length-300;s<quarry.Length-50;s+=80)StreetBunting(quarry,s);
        }
        // A bench at the back of the pavement facing the street, where no other road or bench is near.
        void Bench(HillcrestRoute.Road road,float s,int side)
        {
            var p=road.At(s)+road.Right(s)*side*(road.width*.5f+Pave-.45f);p.y=road.At(s).y;
            foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+6)return;
            if(Vector2.Distance(new(p.x,p.z),QuarryHills.Square)<30||Vector2.Distance(new(p.x,p.z),new(QuarryHills.PitStopSite.at.x,QuarryHills.PitStopSite.at.z))<30)return;
            Put("bench",p,Quaternion.LookRotation(-road.Right(s)*side).eulerAngles.y,null,null,1,false);
        }
        float RowCentre(HillcrestRoute.Road road,float depth)=>road.width*.5f+Pave+Garden+depth*.5f;
        void StreetRow(HillcrestRoute.Road road,int side,float from,float to)
        {
            float s=from;var core=QuarryHills.Square;
            while(s<to) {
                double roll=rng.NextDouble();
                string id=roll<.8?new[]{"terrace_a","terrace_a","terrace_c","terrace_d","terrace_d","terrace_e","terrace_f","terrace_f","terrace_g","terrace_b"}[rng.Next(10)]:roll<.9?"house_c":"house_d";
                var size=Footprint(id);float mid=s+size.x*.5f;if(mid+size.x*.5f>to)break;
                var at=road.At(mid);var right=road.Right(mid);var centre=at+right*side*RowCentre(road,size.y);centre.y=at.y+.05f;
                if(rng.NextDouble()<.04){s+=6;continue;}
                // Shops crowd the middle of town; houses further out.
                bool central=Vector2.Distance(new(centre.x,centre.z),core)<230;if(central&&id.StartsWith("t")&&rng.NextDouble()<.3)id="terrace_b";
                if(Building(id,centre,Yaw(centre,at),RandomTint(),id=="terrace_b"?ShopNames[shopName++%ShopNames.Length]:null)) {
                    var yard=at+right*side*(road.width*.5f+Pave+Garden*.5f+.4f);yard.y=at.y;Homes.Add((yard,Quaternion.LookRotation(road.Forward(mid)*(rng.NextDouble()<.5?1:-1)).eulerAngles.y,id));
                    if(lamps<500&&((int)(mid/32))%2==(side>0?0:1)&&Mathf.Repeat(mid,32)<size.x){var lp=at+right*side*(road.width*.5f+.7f);lp.y=at.y;Lamp(lp);}
                    // Now and then a flowering bush at the corner of the front garden (clear of the parking spot's middle).
                    if(rng.NextDouble()<.16){var fb=at+right*side*(road.width*.5f+Pave+Garden-.7f)+road.Forward(mid)*(size.x*.5f-.7f);fb.y=at.y;Put("flower_bush",fb,(float)rng.NextDouble()*360,null,null,.8f,false);}
                    // The back yard behind it: a wall along the lane and one between it and next door.
                    YardWalls(centre,Yaw(centre,at),size,Yard);
                    s+=size.x+.02f;
                } else s+=2;
            }
        }
        // The yard walls behind a house facing away along `yaw` (wall height ~1.4 m, slate stone).
        void YardWalls(Vector3 centre,float yaw,Vector2 size,float depth)
        {
            var rot=Quaternion.Euler(0,yaw,0);var back=rot*Vector3.back;var across=rot*Vector3.right;
            var b0=centre+back*(size.y*.5f+depth);WallBox(b0,across,size.x);
            var side0=centre+back*(size.y*.5f+depth*.5f)-across*size.x*.5f;WallBox(side0,back,depth);
        }
        void WallBox(Vector3 mid,Vector3 along,float length)
        {
            float g=H(mid.x,mid.z);if(ValleyField.RoadDistance(mid.x,mid.z)<4.5f)return;
            walls.Add(Matrix4x4.TRS(new Vector3(mid.x,g+.3f,mid.z),Quaternion.LookRotation(along),new Vector3(.4f,2.2f,length+.4f)));
        }
        void FlushWalls(){Combine("Yard walls",walls,stone,true);Combine("Footpaths",paths,paving,false);}
        void Combine(string name,List<Matrix4x4> boxes,Material material,bool solid)
        {
            if(boxes.Count==0)return;
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(temp);
            for(int start=0;start<boxes.Count;start+=3000) {
                int n=Mathf.Min(3000,boxes.Count-start);var combine=new CombineInstance[n];
                for(int i=0;i<n;i++)combine[i]=new CombineInstance{mesh=cube,transform=boxes[start+i]};
                var mesh=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
                if(solid){go.AddComponent<MeshCollider>().sharedMesh=mesh;go.tag="Respawn";}
            }
        }
        // A line `d` metres out on one side of a road, on the ground (folds on tight bends are dropped).
        static HillcrestRoute.Road Offset(HillcrestRoute.Road road,int side,float d,float from,float to)
        {
            var pts=new List<Vector3>();
            for(float s=Mathf.Max(0,from);s<=Mathf.Min(road.Length,to);s+=2) {
                var p=road.At(s)+road.Right(s)*side*d;p.y=H(p.x,p.z);
                if(pts.Count>0){var step=p-pts[^1];step.y=0;if(step.magnitude<1||Vector3.Dot(step,road.Forward(s))<step.magnitude*.5f)continue;}
                pts.Add(p);
            }
            return pts.Count>2?HillcrestRoute.Make("offset",pts.ToArray(),0,0,0):null;
        }
        // The block behind a street row: back lane, the back row (its back to the lane, front on a footpath), and near the
        // square a third row facing the second across the path.
        void Block(HillcrestRoute.Road road,int side,float from,float to)
        {
            float w=road.width*.5f,rowBack=w+Pave+Garden+8;float lane=rowBack+Yard+LaneW*.5f,back2=lane+LaneW*.5f+Yard;
            var lanePath=Offset(road,side,lane,from,to);if(lanePath==null)return;
            var placedOnLane=new List<float>();
            BackRow(road,side,back2,from,to,false,placedOnLane,lanePath);
            float front2=back2+8,back3=front2+Path+8;
            BackRow(road,side,back3,from,to,true,null,null);
            // The lane (gravel), where the yards behind it are built.
            placedOnLane.Sort();int i0=0;
            for(int i=1;i<=placedOnLane.Count;i++)if(i==placedOnLane.Count||placedOnLane[i]-placedOnLane[i-1]>24) {
                if(i-i0>=2)Strip("Back lane",lanePath,placedOnLane[i0]-4,placedOnLane[i-1]+4,LaneW,laneGravel);i0=i;
            }
        }
        // One row of houses `d` metres out with its back towards the road (or, for `facing`, its front towards the road
        // across the footpath, i.e. its back at `d`).
        void BackRow(HillcrestRoute.Road road,int side,float d,float from,float to,bool facing,List<float> onLane,HillcrestRoute.Road lanePath)
        {
            var core=QuarryHills.Square;
            for(float s=from;s<to;) {
                string id=rng.NextDouble()<.85?new[]{"terrace_a","terrace_c","terrace_d","terrace_d","terrace_f","terrace_f","terrace_g"}[rng.Next(7)]:"house_d";
                var size=Footprint(id);
                var basePt=road.At(s);var right=road.Right(s);
                var centre=basePt+right*side*(d+size.y*.5f);
                if(facing&&Vector2.Distance(new(centre.x,centre.z),core)>340)return;
                if(Vector2.Distance(new(centre.x,centre.z),core)>520){s+=6;continue;}
                // Facing out (second row) or in (third row); the floor is on the ground at its front door.
                var outward=right*side;var front=facing?centre-outward*size.y*.5f:centre+outward*size.y*.5f;centre.y=H(front.x,front.z)+.05f;
                float yaw=Quaternion.LookRotation(facing?-outward:outward).eulerAngles.y;
                if(rng.NextDouble()>.05&&Building(id,centre,yaw,RandomTint(.42f),null,false,3.2f)) {
                    backHouses++;
                    if(!facing){YardWalls(centre,yaw,size,Yard);if(lanePath!=null)onLane?.Add(lanePath.Nearest(centre,out _));}
                    else YardWalls(centre,yaw,size,Yard);
                    // Its footpath, a short flag strip at the door.
                    var p=front+outward*(facing?-1.3f:1.3f);paths.Add(Matrix4x4.TRS(new Vector3(p.x,H(p.x,p.z)-.05f,p.z),Quaternion.Euler(0,yaw,0),new Vector3(size.x+.1f,.3f,2.2f)));
                    s+=size.x+.3f;
                } else s+=3;
            }
        }
        // Pavement strips either side of a street, broken where another road crosses (as Hillcrest's).
        void Pavements(HillcrestRoute.Road road,float from,float to)
        {
            foreach(int side in new[]{-1,1}) {
                var run=new List<Vector3>();
                void Flush(){if(run.Count>3)StripLine("Pavement",run.ToArray(),Pave,.06f,paving);run.Clear();}
                for(float s=Mathf.Max(0,from);s<=Mathf.Min(road.Length,to);s+=2) {
                    Vector3 p=road.At(s)+road.Right(s)*side*(road.width*.5f+Pave*.5f);
                    bool clear=Vector2.Distance(new(p.x,p.z),QuarryHills.Square)>26;
                    // (Not into a back street's junction: its fillets reach ~15 m from the node.)
                    foreach(var node in new[]{QuarryHills.ChapelFrom,QuarryHills.ChapelTo,QuarryHills.TerraceFrom,QuarryHills.TerraceTo})if(Vector2.Distance(new(p.x,p.z),new(node.x,node.z))<15)clear=false;
                    if(clear)foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+2.4f){clear=false;break;}
                    if(clear)run.Add(p);else Flush();
                }
                Flush();
            }
        }
        // A ground-hugging strip along `path` between runs [from,to], broken where it would cross a road.
        void Strip(string name,HillcrestRoute.Road path,float from,float to,float width,Material material)
        {
            var run=new List<Vector3>();
            void Flush(){if(run.Count>3)StripLine(name,run.ToArray(),width,.09f,material);run.Clear();}
            for(float s=Mathf.Max(0,from);s<=Mathf.Min(path.Length,to);s+=2){var p=path.At(s);p.y=H(p.x,p.z);if(ValleyField.RoadDistance(p.x,p.z)>6.5f)run.Add(p);else Flush();}
            Flush();
        }
        void StripLine(string name,Vector3[] line,float width,float lift,Material material)
        {
            int n=line.Length;var v=new Vector3[n*2];var t=new List<int>();
            for(int i=0;i<n;i++) {
                Vector3 f=line[Mathf.Min(n-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                Vector3 a=line[i]-r*width*.5f,b=line[i]+r*width*.5f;
                if(material!=paving){a.y=H(a.x,a.z);b.y=H(b.x,b.z);}
                v[i*2]=a+Vector3.up*lift;v[i*2+1]=b+Vector3.up*lift;if(i<n-1){int k=i*2;t.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}
            }
            var mesh=new Mesh{name=name,vertices=v};mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        // Bunting across a street, hung from a lamp post on each pavement (added where none stands within 2.5 m).
        void StreetBunting(HillcrestRoute.Road road,float s)
        {
            var ends=new Vector3[2];
            for(int i=0;i<2;i++) {
                int side=i==0?1:-1;var p=road.At(s)+road.Right(s)*side*(road.width*.5f+.7f);
                foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+1.2f)return;
                p.y=road.At(s).y;ends[i]=p;
            }
            for(int i=0;i<2;i++) {
                bool lit=false;foreach(var l in streetLamps)if(Vector2.Distance(new(l.x,l.z),new(ends[i].x,ends[i].z))<2.5f){ends[i]=l;lit=true;break;}
                if(!lit)Lamp(ends[i]);
            }
            HillcrestTown.BuntingLine(root,ends[0]+Vector3.up*4.4f,ends[1]+Vector3.up*4.4f,.9f);
        }
    }
}
