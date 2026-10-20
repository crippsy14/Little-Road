using System.Collections.Generic;
using System.Linq;
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
        Material plinth,paving,laneGravel,stone;int buildings,plinths,lamps,backHouses,deeper;float deepest;Vector3 deepestAt;
        readonly List<Vector3> streetLamps=new();
        readonly List<Matrix4x4> walls=new(),paths=new();
        public QuarryHillsTown(Transform parent){root=new GameObject("Quarry Hills town").transform;root.SetParent(parent);Last=this;}
        // The town as last built: the countryside asks it where buildings stand.
        public static QuarryHillsTown Last;
        public bool Clear(Vector2 c,Vector2 half,float yaw)=>Free(c,half,yaw);
        static float H(float x,float z)=>ValleyField.Height(x,z);
        // (--prop-forge: the ground as drawn, where it's lower than the height field: by a road's cutting the terrain's
        //  triangles fall away inside a house's footprint, and the end house of Quay Lane stood over the void with no plinth
        //  under it, street review QH2-01. The first solid surface at or below the field; the field where there's none
        //  within 12 m.)
        float Drawn(float x,float z)
        {
            float h=H(x,z),best=float.MinValue;foreach(var hit in Physics.RaycastAll(new Vector3(x,h+.3f,z),Vector3.down,12.3f))if(!hit.collider.isTrigger&&hit.point.y>best)best=hit.point.y;
            if(best==float.MinValue)return h;if(best<h-.3f)deeper++;if(h-best>deepest){deepest=h-best;deepestAt=new Vector3(x,best,z);}return Mathf.Min(h,best);
        }
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
        static readonly string[] ShopNames={"CAFFI","BARA","GROSER","POST","CIGYDD","LLYFRAU","CHIPS","HARDWARE","SLATE CRAFTS","CHEMIST","BAKERY","WOOL","SWEETS","TOOLS","PIES","BIKES"};
        int shopName;
        static Vector2 Footprint(string id)=>id switch{"terrace_a"=>new(5.4f,8),"terrace_b"=>new(5.4f,8),"terrace_c"=>new(5.4f,8),"terrace_d"=>new(5.4f,8),"terrace_e"=>new(5.4f,8),"terrace_f"=>new(5.4f,8),"terrace_g"=>new(5.4f,8),
            "house_a"=>new(8.5f,7.5f),"house_b"=>new(6.2f,7.5f),"house_c"=>new(10.8f,8.5f),"house_d"=>new(5.6f,7.4f),"cafe"=>new(9,8),"shop"=>new(9,8),"post_office"=>new(9,8),
            "cottage_a"=>new(5.4f,7),"cottage_b"=>new(5.4f,7),"cottage_c"=>new(5.4f,7),"church"=>new(12,14),"town_hall"=>new(20,16),"station"=>new(22,8),"mill"=>new(12,11),_=>new(8,8)};
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
        internal static bool Overlap(Vector2 c1,Vector2 h1,float y1,Vector2 c2,Vector2 h2,float y2)
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
            // A 5 x 5 grid over the footprint, 0.3 m past its edges (street review, 4 Oct: corners alone missed a bank between
            // them, and a house on Quay Lane hung over it with no plinth).
            for(int i=0;i<5;i++)for(int j=0;j<5;j++) {
                var c=new Vector2(i/2f-1,j/2f-1);var p=centre+rot*new Vector3(c.x*(size.x*.5f+.3f),0,c.y*(size.y*.5f+.3f));float g=PropForge.Enabled?Drawn(p.x,p.z):H(p.x,p.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                var q=centre+rot*new Vector3(c.x*size.x*.5f,0,c.y*size.y*.5f);if(!force&&(i%2==0&&j%2==0)&&ValleyField.RoadDistance(q.x,q.z)<5.3f)return false;
            }
            if(!force&&(floor-low>7.5f||high-floor>bury))return false;
            if(floor-low>.25f){Box("Stone plinth",new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),new Vector3(size.x+.5f,floor-low+.5f,size.y+.5f),rot,plinth);plinths++;
                // Steps up to the door from the garden where the plinth stands high at the front (street review, 4 Oct: doors
                // above a stone wall with no way up): 18 cm risers, 30 cm treads, across the middle of the frontage.
                var fwd=rot*Vector3.forward;var foot=centre+fwd*(size.y*.5f+.25f);float drop=floor-H(foot.x,foot.z);
                if(drop>.35f){int n=Mathf.CeilToInt(drop/.18f);float rise=drop/n;
                    for(int k=0;k<n;k++){var p=centre+fwd*(size.y*.5f+.25f+.3f*(k+.5f));float top=floor-rise*(k+1),g=H(p.x,p.z);if(top<=g)break;
                        Box("Door steps",new Vector3(p.x,(top+g-.1f)*.5f,p.z),new Vector3(Mathf.Min(2.6f,size.x-.6f),top-g+.1f,.3f),rot,plinth);}}}
            Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign);buildings++;Take(c2,half,yaw);
            // Chimney tops (kit x mirrors on import): some of them smoke, as coal fires do.
            if((id.StartsWith("terrace")||id.StartsWith("cottage")||id=="house_a"||id=="house_c")&&rng.NextDouble()<.3)HillcrestTown.Chimneys.Add(ForgeStandIns.ChimneyOr(id,centre,yaw,id switch{"house_a"=>new Vector3(-2.6f,10.5f,-1.2f),"house_c"=>new Vector3(-2.8f,10.2f,-1.4f),
                "cottage_a" or "cottage_b" or "cottage_c"=>new Vector3(-2.25f,9.4f,0),"terrace_e"=>new Vector3(-1.9f,15.2f,2.7f),"terrace_f"=>new Vector3(-1.9f,10.7f,-.6f),"terrace_g"=>new Vector3(1.6f,13.1f,-2.4f),_=>new Vector3(-1.4f,12.1f,-1.5f)},!string.IsNullOrEmpty(sign)));
            return true;
        }
        public void Build()=>TrialWorld.RunNow(BuildRoutine());
        // (A coroutine: it yields between pieces of work when built in the background, Districts; docs/72.)
        public System.Collections.IEnumerator BuildRoutine()
        {
            if(!QuarryHills.Enabled||!BayviewKit.Has("terrace_a"))yield break;
            Spots.Clear();Homes.Clear();GardenProps.Clear();
            plinth=WorldLook.Kit("Quarry Hills slate plinth",Toy.Hex(0x7d838a),WorldLook.Pattern.Stone,0,.12f,true);
            paving=WorldLook.Kit("Quarry Hills square flags",Toy.Hex(0x8f9296),WorldLook.Pattern.Stone,0,.15f,true);
            laneGravel=WorldLook.Kit("Quarry Hills back lane",Toy.Hex(0x6f7174),WorldLook.Pattern.Rock,0,.1f,true);
            stone=WorldLook.Kit("Quarry Hills yard wall",Toy.Hex(0x80858a),WorldLook.Pattern.Stone,0,.1f,true);
            CoastalLandscape.Lap("qht start");Square();CoastalLandscape.Lap("qht square");yield return null;PitStopAndShops();CoastalLandscape.Lap("qht shops");Driveway();CoastalLandscape.Lap("qht driveway");yield return null;yield return Streets();CoastalLandscape.Lap("qht streets");yield return RetainingWalls();CoastalLandscape.Lap("qht retaining");yield return null;FlushWalls();CoastalLandscape.Lap("qht flush");KettleGate();CoastalLandscape.Lap("qht kettle");
            Debug.Log($"LITTLE_ROAD_QUARRY_TOWN buildings={buildings} back={backHouses} plinths={plinths} lamps={lamps} homes={Homes.Count}"+(PropForge.Enabled?$" ground_lower_than_field={deeper} deepest={deepest:0.0}m at {deepestAt}":""));
        }
        // ---------------------------------------------------------------- the square
        void Square()
        {
            var c=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);
            // Paved square (slate flags) under the junction's edges; nothing else builds on it.
            // (4 Oct: a flat 46 m slab stood up to 0.8 m clear of the ground where the junction cut is low, with benches and
            // stalls on its top and nothing under them.) The flags drape the ground on a 2 m grid, 3 cm up.
            {const int n=23;var v=new List<Vector3>();var tri=new List<int>();
                for(int j=0;j<=n;j++)for(int i=0;i<=n;i++){float x=c.x-23+i*2,z=c.z-23+j*2;v.Add(new Vector3(x,H(x,z)+(ValleyField.RoadDistance(x,z)<5.5f?-.15f:.03f),z));} // (tucked under the junction's road surface)
                for(int j=0;j<n;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i;tri.AddRange(new[]{a,a+n+1,a+1,a+1,a+n+1,a+n+2});}
                var mesh=new Mesh{name="Square paving"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
                if(mesh.normals[0].y<0){for(int t=0;t<tri.Count;t+=3)(tri[t+1],tri[t+2])=(tri[t+2],tri[t+1]);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();}
                var pave=new GameObject("Square paving",typeof(MeshFilter),typeof(MeshRenderer));pave.transform.SetParent(root);pave.GetComponent<MeshFilter>().sharedMesh=mesh;pave.GetComponent<MeshRenderer>().sharedMaterial=paving;}
            Take(new Vector2(c.x,c.z),new Vector2(25,25),0);
            Vector3 Corner(float bearing,float r){float a=bearing*Mathf.Deg2Rad;var p=c+new Vector3(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);p.y=QuarryHills.SquareY;return p;}
            // The roads come in roughly from the four quarters: the landmarks stand on the diagonals, facing in.
            var inst=Corner(135,36);Building("town_hall",inst,Yaw(inst,c),T(0x8c9095,0xf4f1ea,0xc0392b,0x1f3a93,0x40464d),"INSTITUTE",true);
            var chapel=Corner(225,34);Building("church",chapel,Yaw(chapel,c),T(0x969089,0xf4f1ea,0x2e86c1,0x229954,0x4b5158),"CAPEL BETHEL",true);
            var caban=Corner(315,30);Building("cafe",caban,Yaw(caban,c),T(0xe9e3d3,0xc0392b,0xc0392b,0xc0392b,0x4b5158),"CABAN",true);Spots["caban"]=(Corner(315,21),Yaw(caban,c)+90);
            var shop=Corner(45,30);Building("terrace_b",shop,Yaw(shop,c),T(0xf0dc9a,0x1f3a93,0x2e86c1,0x1f3a93,0x565c63),"SIOP Y GORNEL",true); // (not "SIOP": it read as STOP at the junction)
            Spots["institute"]=(inst,Yaw(inst,c));Spots["chapel"]=(chapel,Yaw(chapel,c));
            // Each landmark's neighbours: a short run of terraces either side, so the square is enclosed.
            foreach(var (b,r) in new[]{(135f,36f),(225f,34f),(315f,30f),(45f,30f)})
                foreach(float off in new[]{-24f,-18f,18f,24f}) {
                    var p=Corner(b+off,r+2);var id=new[]{"terrace_a","terrace_d","terrace_f","terrace_b"}[rng.Next(4)];
                    Building(id,p,Yaw(p,c),RandomTint(.5f),id=="terrace_b"?ShopNames[shopName++%ShopNames.Length]:null,false,4);
                }
            // A fountain in the middle, stalls on market day (every day), benches, flags in the Welsh colours, bunting (all
            // stood on the ground itself: the junction's cut leaves it a little under the square's level in places).
            // (The paving now drapes the ground, so everything stands on the ground: street review, 4 Oct, a bench sank into the
            // old flat slab to its back slats and the map board lay face-up in it.)
            Vector3 OnGround(Vector3 p){p.y=H(p.x,p.z);return p;}
            Put("fountain",OnGround(c)+Vector3.down*.02f,0);
            int k=0;foreach(float b in new[]{60f,120f,240f,300f}){var p=OnGround(Corner(b,11));Put("market_stall",p,Yaw(p,c),T(0xffffff,0,new[]{0xc0392b,0x229954,0xf1c40f,0x2e86c1}[k++%4],0,0x4b5158));}
            foreach(float b in new[]{90f,180f,270f,0f}){var p=OnGround(Corner(b+40,17));Put("bench",p,Yaw(p,c)+180);}
            // Y Ddraig Goch either side of a white-and-green pole (street review, 4 Oct: plain stripes didn't read as Welsh).
            foreach(float x in new[]{-4f,0f,4f}){var p=OnGround(Corner(180,6)+new Vector3(x,0,0));if(x!=0&&BayviewKit.Has("flag_wales"))Put("flag_wales",p,0);else Put("flag_pole",p,0,T(0xffffff,x==0?0xc0392b:0x229954,x==0?0xffffff:0xc0392b,0,0xffffff));}
            var ring=new List<Vector3>();
            foreach(float b in new[]{20f,70f,110f,160f,200f,250f,290f,340f}){var p=Corner(b,21);p.y=H(p.x,p.z);if(Put("lamp_post",p,0,T(0xffffff,0x1f3a93,0xffffff,0xffffff,0xffffff),null,1,false)!=null){lamps++;streetLamps.Add(p);ring.Add(p);}}
            // Round the square lamp to lamp, and two runs across it.
            for(int i=0;i<ring.Count;i++)HillcrestTown.BuntingLine(root,ring[i]+Vector3.up*4.4f,ring[(i+1)%ring.Count]+Vector3.up*4.4f,.9f);
            if(ring.Count==8)foreach(int i in new[]{1,3})HillcrestTown.BuntingLine(root,ring[i]+Vector3.up*4.4f,ring[i+4]+Vector3.up*4.4f,2.2f);
            Spots["square"]=(c,0);
        }
        // Garden and pavement furniture a parked car must keep clear of (ValleyLife): bushes, benches and lamps.
        public static readonly List<Vector3> GardenProps=new();
        // (Street review, 4 Oct: the approach run and the town's own lamps doubled up 2-4 m apart.) None within 6 m of another.
        void Lamp(Vector3 p){foreach(var o in streetLamps)if((o-p).sqrMagnitude<36)return;GardenProps.Add(p);if(Put("lamp_post",p,0,T(0xffffff,0x1f3a93,0xffffff,0xffffff,0xffffff),null,1,false)!=null){lamps++;streetLamps.Add(p);}}
        // The end of the Kettle Falls road for now: a gate across it, closed, with chevrons behind.
        void KettleGate()
        {
            var road=HillcrestRoute.Find(QuarryHills.KettleRoadName);float s=road.Length-6;var at=road.At(s);var f=road.Forward(s);
            Put("town_gate",at+Vector3.down*.02f,Quaternion.LookRotation(-f).eulerAngles.y,T(0xf4f1ea,0,0xc0392b,0,0xffffff),"KETTLE FALLS - ROAD CLOSED");
            foreach(float x in new[]{-2.5f,0f,2.5f}){var p=road.At(road.Length-1)+Vector3.Cross(Vector3.up,f)*x+f*2;p.y=H(p.x,p.z);Put("chevron_sign",p,Quaternion.LookRotation(-f).eulerAngles.y);}
        }
        // ---------------------------------------------------------------- Pit Stop
        // (Street review, 4 Oct: the pavement ran unbroken past the forecourt, so you crossed a kerb and grass to reach the bays.)
        // A 16 m paved apron from Quarry Road's edge to the forecourt, the pavement broken across it.
        static bool InDriveway(Vector3 p)
        {
            var site=QuarryHills.PitStopSite;var l=Quaternion.Inverse(Quaternion.Euler(0,site.yaw,0))*(p-site.at);return Mathf.Abs(l.x)<9&&l.z>4&&l.z<20;
        }
        void Driveway()
        {
            var road=HillcrestRoute.Find(QuarryHills.QuarryRoadName);var site=QuarryHills.PitStopSite;var face=Quaternion.Euler(0,site.yaw,0)*Vector3.forward;
            float s=road.Nearest(site.at,out _);var edge=road.At(s)-face*(road.width*.5f-.3f);
            var front=site.at+face*6;float len=Vector3.Distance(new Vector3(edge.x,0,edge.z),new Vector3(front.x,0,front.z));var mid=(edge+front)*.5f;
            float y0=edge.y,y1=Mathf.Max(H(front.x,front.z),site.at.y);
            var apron=Box("Pit Stop apron",new Vector3(mid.x,(y0+y1)*.5f-.12f,mid.z),new Vector3(16,.3f,len),Quaternion.LookRotation(new Vector3(edge.x-front.x,(y0-y1),edge.z-front.z)),paving,true);
            apron.tag="Untagged";
        }
        void PitStopAndShops()
        {
            var site=QuarryHills.PitStopSite;
            PitStop.Build("quarry",true,new Vector3(site.at.x,site.at.y-.03f,site.at.z),site.yaw,root);
            Take(new Vector2(site.at.x,site.at.z),new Vector2(24,19),site.yaw);
            // Paved yard under the bays and in front of them, to the back of the pavement (street review, 5 Oct: the pad's
            // grass showed through the 4 cm bay floors beside the driveway, so the body shop stood on a lawn).
            // Draped on a 1 m grid, never under the ground (the terrain stands a few cm over the pad in places, so a flat slab
            // at the pad's height was still hidden under the grass).
            {var fq=Quaternion.Euler(0,site.yaw,0);const int nx=31,nz=20;var v=new List<Vector3>();var tri=new List<int>();
                for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++){var q=site.at+fq*new Vector3(i-nx*.5f,0,j-7f);v.Add(new Vector3(q.x,Mathf.Max(H(q.x,q.z),site.at.y)+.04f,q.z));}
                for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;tri.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
                var mesh=new Mesh{name="Pit Stop yard"};mesh.SetVertices(v);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();
                if(mesh.normals[0].y<0){for(int t=0;t<tri.Count;t+=3)(tri[t+1],tri[t+2])=(tri[t+2],tri[t+1]);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();}
                var yard=new GameObject("Pit Stop yard",typeof(MeshFilter),typeof(MeshRenderer));yard.transform.SetParent(root);yard.GetComponent<MeshFilter>().sharedMesh=mesh;yard.GetComponent<MeshRenderer>().sharedMaterial=paving;}
            if(PitStop.Hosts.TryGetValue("quarry/garage",out var g))Spots["qgarage"]=g;
            if(PitStop.Hosts.TryGetValue("quarry/parts",out var p))Spots["qparts"]=p;
            if(PitStop.Hosts.TryGetValue("quarry/body",out var b))Spots["qbody"]=b;
        }
        // ---------------------------------------------------------------- streets
        // Distances out from a street's edge (its half width): pavement, front gardens, the street row, its yard, the back
        // lane, the back row's yard and the back row, its footpath, and (in the core) the third row facing it.
        const float Pave=2.3f,Garden=2.6f,Yard=4.5f,LaneW=3.6f,Path=7.4f;
        System.Collections.IEnumerator Streets()
        {
            var quarry=HillcrestRoute.Find(QuarryHills.QuarryRoadName);var high=HillcrestRoute.Find(QuarryHills.HighStreetName);
            var quay=HillcrestRoute.Find(QuarryHills.QuayLaneName);var haul=HillcrestRoute.Find(QuarryHills.HaulRoadName);
            var chapel=HillcrestRoute.Find(QuarryHills.ChapelStreetName);var terrace=HillcrestRoute.Find(QuarryHills.TerraceRowName);
            var streets=new List<(HillcrestRoute.Road road,float from,float to)>{
                (high,30,high.Length-10),(quarry,quarry.Length-430,quarry.Length-30),(quay,30,Mathf.Min(quay.Length-60,420)),
                (haul,30,Mathf.Min(haul.Length-40,260)),(chapel,14,chapel.Length-14),(terrace,14,terrace.Length-14)};
            // Street rows first on every street (they claim the frontages), then the blocks behind them.
            foreach(var (road,from,to) in streets)foreach(int side in new[]{-1,1}){yield return StreetRow(road,side,from,to);yield return null;}
            foreach(var (road,from,to) in streets)foreach(int side in new[]{-1,1}){Block(road,side,from,to);yield return null;}
            foreach(var (road,from,to) in streets){Pavements(road,from-6,to+6);yield return null;}
            for(float s=50;s<high.Length-40;s+=64)StreetBunting(high,s);yield return null;
            // Benches on the pavements, lamps along Quarry Road's approach, map boards in the square and by the Pit Stop.
            foreach(var (road,from,to) in streets){int k=0;for(float s=from+20;s<to-20;s+=30,k++)Bench(road,s,k%2==0?1:-1);yield return null;}
            for(float s=quarry.Length-1400;s<quarry.Length-430;s+=24){int side=((int)(s/24))%2==0?1:-1;var lp=quarry.At(s)+quarry.Right(s)*side*(quarry.width*.5f+.9f);lp.y=quarry.At(s).y;if(ValleyField.RoadDistance(lp.x,lp.z)>quarry.width*.5f-.5f)Lamp(lp);}
            var sq=new Vector3(QuarryHills.Square.x,QuarryHills.SquareY,QuarryHills.Square.y);var mb=sq+new Vector3(18,0,-18);mb.y=H(mb.x,mb.z);Put("map_board",mb,Yaw(mb,sq));
            var ps=QuarryHills.PitStopSite.at;var mb2=ps+Quaternion.Euler(0,QuarryHills.PitStopSite.yaw,0)*new Vector3(16,0,14);mb2.y=H(mb2.x,mb2.z);Put("map_board",mb2,QuarryHills.PitStopSite.yaw);
            for(float s=quarry.Length-300;s<quarry.Length-50;s+=80)StreetBunting(quarry,s);
        }
        // A bench at the back of the pavement facing the street, where no other road or bench is near.
        // Where one house meets the next along each street side (a bench there stands between two doors, not in front of one:
        // street review, 4 Oct).
        readonly Dictionary<(string,int),List<float>> partyWalls=new();
        void Bench(HillcrestRoute.Road road,float s,int side)
        {
            if(partyWalls.TryGetValue((road.name,side),out var walls)){float best=float.MaxValue;foreach(float w in walls)if(Mathf.Abs(w-s)<Mathf.Abs(best-s))best=w;if(Mathf.Abs(best-s)<6)s=best;}
            // (Street review, 5 Oct: 0.45 m from the pavement's back, the bench's back went into the slate fence behind it.)
            var p=road.At(s)+road.Right(s)*side*(road.width*.5f+Pave-.8f);p.y=road.At(s).y;
            foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+6)return;
            if(Vector2.Distance(new(p.x,p.z),QuarryHills.Square)<30||Vector2.Distance(new(p.x,p.z),new(QuarryHills.PitStopSite.at.x,QuarryHills.PitStopSite.at.z))<30)return;
            Put("bench",p,Quaternion.LookRotation(-road.Right(s)*side).eulerAngles.y,null,null,1,false);GardenProps.Add(p);
        }
        float RowCentre(HillcrestRoute.Road road,float depth)=>road.width*.5f+Pave+Garden+depth*.5f;
        // (Yields after each house when built in the background: Districts.)
        System.Collections.IEnumerator StreetRow(HillcrestRoute.Road road,int side,float from,float to)
        {
            float s=from;var core=QuarryHills.Square;string last=null;
            while(s<to) {
                yield return null;
                double roll=rng.NextDouble();
                string id=roll<.8?new[]{"terrace_a","terrace_a","terrace_c","terrace_d","terrace_d","terrace_e","terrace_f","terrace_f","terrace_g","terrace_b"}[rng.Next(10)]:roll<.9?"house_c":"house_d";
                // Quarrymen's cottages (street review, 4 Oct: the town read as Ligurian): most houses away from the square,
                // a quarter of them in the middle.
                {var p0=road.At(s);if(rng.NextDouble()<(Vector2.Distance(new(p0.x,p0.z),core)<230?.25:.62))id=new[]{"cottage_a","cottage_b","cottage_c"}[rng.Next(3)];}
                // No two of the same front side by side (street review, 4 Oct: twin loggia houses read as copy-paste).
                if(id==last)id=id.StartsWith("cottage")?(id=="cottage_a"?"cottage_b":"cottage_a"):id=="terrace_f"?"terrace_d":id=="house_d"?"terrace_a":"terrace_f";
                var size=Footprint(id);float mid=s+size.x*.5f;if(mid+size.x*.5f>to)break;
                var at=road.At(mid);var right=road.Right(mid);var centre=at+right*side*RowCentre(road,size.y);centre.y=at.y+.05f;
                // (--prop-forge: a 5.4 m terrace has no room for a car in front of it, which stood across doors and shop windows,
                //  street review QH2-05. The car goes in a gap between houses instead, nose in, as a side drive.)
                if(rng.NextDouble()<(PropForge.Enabled?.1:.04)){if(PropForge.Enabled)SideDrive(road,side,s+3);s+=6;continue;}
                // Shops crowd the middle of town; houses further out.
                bool central=Vector2.Distance(new(centre.x,centre.z),core)<230;if(central&&id.StartsWith("t")&&rng.NextDouble()<.3)id="terrace_b";
                bool shopFront=id=="terrace_b"||PropForge.Enabled&&(id=="terrace_e"||id=="terrace_g");   // (the Forge's e and g are always shops: QH2-06, empty fascias)
                if(Building(id,centre,Yaw(centre,at),RandomTint(),shopFront?ShopNames[shopName++%ShopNames.Length]:null)) {
                    // The middle of the front garden (street review, 4 Oct: 0.4 m further out it overlapped the house, and settling
                    // then pushed cars onto the pavement, benches and lamps). Not in front of a cottage's porch bench.
                    // About half the gardens are a drive with a car on it (no porch or bay in the way); the rest keep a front
                    // fence of upright slate slabs (crawiau, as the slate valleys have), with a gap for the gate.
                    last=id;if(!partyWalls.TryGetValue((road.name,side),out var pw))partyWalls[(road.name,side)]=pw=new();pw.Add(mid-size.x*.5f);pw.Add(mid+size.x*.5f);
                    var yard=at+right*side*(road.width*.5f+Pave+Garden*.5f-.05f);yard.y=at.y;
                    // (Street review, 5 Oct: cars in a shop's front garden stood against its window and door, and an arcade's piers
                    // stand out into the garden.) No drive in front of a shop or an arcade.
                    bool drive=id!="terrace_f"&&id!="cottage_b"&&id!="cottage_c"&&id!="terrace_b"&&id!="terrace_c"&&rng.NextDouble()<.55;
                    if(PropForge.Enabled){if(!shopFront)Crawiau(road,side,mid,size.x);}
                    else if(drive)Homes.Add((yard,Quaternion.LookRotation(road.Forward(mid)*(rng.NextDouble()<.5?1:-1)).eulerAngles.y,id));else Crawiau(road,side,mid,size.x);
                    // (--prop-forge: at the party wall, between two fronts: in the middle of one it stood before a door or a
                    //  shop's name, street review QH2-03/04)
                    if(lamps<900&&((int)(mid/16))%2==(side>0?0:1)&&Mathf.Repeat(mid,16)<size.x){float ls=PropForge.Enabled?mid-size.x*.5f:mid;var lp=road.At(ls)+road.Right(ls)*side*(road.width*.5f+.7f);lp.y=road.At(ls).y;Lamp(lp);}
                    // Now and then a flowering bush at the corner of the front garden (clear of the parking spot's middle).
                    if(rng.NextDouble()<.16){var fb=at+right*side*(road.width*.5f+Pave+Garden-.7f)+road.Forward(mid)*(size.x*.5f-.7f);fb.y=at.y;Put("flower_bush",fb,(float)rng.NextDouble()*360,null,null,.8f,false);GardenProps.Add(fb);}
                    // The back yard behind it: a wall along the lane and one between it and next door.
                    YardWalls(centre,Yaw(centre,at),size,Yard);
                    s+=size.x+.02f;
                } else{s+=2;FlushYardRun();}
            }
            FlushYardRun();
        }
        // A car parked nose-in in a gap between two houses (--prop-forge), clear of the pavement, where no other road is near.
        void SideDrive(HillcrestRoute.Road road,int side,float s)
        {
            var right=road.Right(s)*side;var p=road.At(s)+right*(road.width*.5f+Pave+.4f+2.3f);p.y=road.At(s).y;
            foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+5)return;
            if(!Free(new Vector2(p.x,p.z),new Vector2(1.2f,2.6f),Quaternion.LookRotation(right).eulerAngles.y))return;
            float lo=float.MaxValue,hi=float.MinValue;foreach(float u in new[]{-2.3f,2.3f})foreach(float w in new[]{-1f,1f}){var q=p+right*u+Vector3.Cross(Vector3.up,right)*w;float g=H(q.x,q.z);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
            if(hi-lo>.45f)return;   // (a gap on a bank: the car stood off the ground, the area audit's cars check)
            Homes.Add((p,Quaternion.LookRotation(rng.NextDouble()<.7?right:-right).eulerAngles.y,"drive"));
        }
        readonly List<Matrix4x4> slabs=new();
        void Crawiau(HillcrestRoute.Road road,int side,float mid,float width)
        {
            float off=road.width*.5f+Pave+.12f;
            for(float s=mid-width*.5f+.35f;s<=mid+width*.5f-.35f;s+=.64f) {
                if(Mathf.Abs(s-mid)<.75f)continue; // the gate
                var p=road.At(s)+road.Right(s)*side*off;float g=H(p.x,p.z);if(ValleyField.RoadDistance(p.x,p.z)<road.width*.5f+1.5f)continue;
                float h=.75f+(float)rng.NextDouble()*.3f,tilt=((float)rng.NextDouble()-.5f)*6;
                GardenProps.Add(p); // (parked cars keep clear of the fence: ValleyLife.NearProp)
                slabs.Add(Matrix4x4.TRS(new Vector3(p.x,g+h*.5f-.15f,p.z),Quaternion.LookRotation(road.Forward(s))*Quaternion.Euler(0,0,tilt),new Vector3(.07f,h+.15f,.6f)));
            }
        }
        // The yard walls behind a house facing away along `yaw` (wall height ~1.4 m, slate stone).
        void YardWalls(Vector3 centre,float yaw,Vector2 size,float depth)
        {
            var rot=Quaternion.Euler(0,yaw,0);var back=rot*Vector3.back;var across=rot*Vector3.right;
            if(PropForge.Enabled) {   // (the back walls as slate dry-stone runs, a row's joined into one across the small gaps a bend opens; docs/80 round 4)
                var mid=centre+back*(size.y*.5f+depth);var a=mid-across*size.x*.5f;var b=mid+across*size.x*.5f;
                if(YardWallAllowed(mid)) {
                    // (a row runs either way along `across`: the end nearer the run so far joins it)
                    if(yardRun.Count>0){var last=new Vector2(yardRun[^1].x,yardRun[^1].z);if(Vector2.Distance(last,new(b.x,b.z))<Vector2.Distance(last,new(a.x,a.z)))(a,b)=(b,a);}
                    if(yardRun.Count==0||Vector2.Distance(new(yardRun[^1].x,yardRun[^1].z),new(a.x,a.z))>2.5f){FlushYardRun();yardRun.Add(a);}else if(Vector2.Distance(new(yardRun[^1].x,yardRun[^1].z),new(a.x,a.z))>.05f)yardRun.Add(a);
                    yardRun.Add(b);
                } else FlushYardRun();
                // (the short walls between yards stay stone boxes: as 4 m runs they were 1,500 more runs, +1.1 s to build the
                //  town, and their piers stood on the hillside's fall)
                WallBox(centre+back*(size.y*.5f+depth*.5f)-across*size.x*.5f,back,depth);return;
            }
            var b0=centre+back*(size.y*.5f+depth);WallBox(b0,across,size.x);
            var side0=centre+back*(size.y*.5f+depth*.5f)-across*size.x*.5f;WallBox(side0,back,depth);
        }
        readonly List<Vector3> yardRun=new();readonly List<List<Vector3>> yardRuns=new();
        void FlushYardRun(){if(yardRun.Count>=2)yardRuns.Add(new List<Vector3>(yardRun));yardRun.Clear();}
        bool YardWallAllowed(Vector3 mid)=>ValleyField.RoadDistance(mid.x,mid.z)>=9f&&Vector2.Distance(new(mid.x,mid.z),QuarryHills.Square)>=36;
        static Vector3 At(List<Vector3> line,float s){for(int k=1;k<line.Count;k++){float d=Vector3.Distance(line[k-1],line[k]);if(s<=d||k==line.Count-1)return Vector3.Lerp(line[k-1],line[k],Mathf.Clamp01(s/Mathf.Max(d,1e-4f)));s-=d;}return line[0];}
        // (a stretch of a yard wall's line as stone boxes, a metre or so each so they step with the ground)
        void BoxRun(List<Vector3> line,float s0,float s1){int n=Mathf.Max(1,Mathf.CeilToInt((s1-s0)/1.5f));for(int k=0;k<n;k++){var a=At(line,s0+(s1-s0)*k/n);var b=At(line,s0+(s1-s0)*(k+1)/n);var d=b-a;d.y=0;if(d.sqrMagnitude<1e-4f)continue;WallBox((a+b)*.5f,d.normalized,d.magnitude);}}
        void WallBox(Vector3 mid,Vector3 along,float length)
        {
            // (Street review, 4 Oct: where two streets run close, one row's back-yard walls stood across the next street's front
            // gardens, in front of its doors. Within 9 m of a road's centreline is pavement and front garden.)
            float g=H(mid.x,mid.z);if(ValleyField.RoadDistance(mid.x,mid.z)<9f)return;
            if(Vector2.Distance(new(mid.x,mid.z),QuarryHills.Square)<36)return;   // (a lone yard wall stood on the square's edge: street review, 4 Oct)
            walls.Add(Matrix4x4.TRS(new Vector3(mid.x,g+.3f,mid.z),Quaternion.LookRotation(along),new Vector3(.4f,2.2f,length+.4f)));
        }
        // Retaining walls (doc 57 banks; the area audit's Terrace Row banks): where the ground beside a town street stays
        // level a few metres then drops away more than 1.2 m into the gardens below, a slate wall along the top of the drop,
        // so a car off the pavement stops there instead of tumbling into a garden it can't climb out of. Clear of junctions.
        public static int Retaining;
        // (Yields after each street when built in the background: Districts.)
        System.Collections.IEnumerator RetainingWalls()
        {
            Retaining=0;var junctions=new List<Vector3>(QuarryHills.JunctionPoints());
            foreach(var name in new[]{QuarryHills.HighStreetName,QuarryHills.QuayLaneName,QuarryHills.ChapelStreetName,QuarryHills.TerraceRowName,QuarryHills.QuarryRoadName}) {
                yield return null;
                var road=HillcrestRoute.Find(name);if(road==null)continue;float half=road.width*.5f;
                foreach(int side in new[]{-1,1}) {
                    // Each side's wall points in order along the street; neighbours (2 m apart along it) join into one wall.
                    var run=new List<(Vector3 top,float low)>();
                    void Lay()
                        {
                        // (Street review, 4 Oct: runs of one or two points stood alone as stubs on banks and corners.) A run needs
                        // four points (6 m) to be built; each carries on 1.5 m past its ends.
                        if(run.Count<4){run.Clear();return;}
                        if(run.Count>=2){var (f0,l0)=run[0];var (f1,_)=run[1];var d0=f0-f1;d0.y=0;if(d0.sqrMagnitude>.01f)run.Insert(0,(f0+d0.normalized*1.5f,l0));
                            var (e0,le)=run[^1];var (e1,_)=run[^2];var d1=e0-e1;d1.y=0;if(d1.sqrMagnitude>.01f)run.Add((e0+d1.normalized*1.5f,le));}
                        for(int i=1;i<run.Count;i++) {
                            var (a0,l0)=run[i-1];var (a1,l1)=run[i];var d=a1-a0;d.y=0;float len=d.magnitude;if(len<.05f)continue;
                            float top=Mathf.Max(a0.y,a1.y)+.8f,low=Mathf.Min(l0,l1);var mid=(a0+a1)*.5f;
                            walls.Add(Matrix4x4.TRS(new Vector3(mid.x,(top+low)*.5f,mid.z),Quaternion.LookRotation(d),new Vector3(.45f,top-low,len+.45f)));Retaining++;
                        }
                        run.Clear();
                    }
                    int misses=0;
                    for(float s=4;s<road.Length-4;s+=2) {
                        var p=road.At(s);var ahead=road.At(s+1)-road.At(s-1);ahead.y=0;if(ahead.sqrMagnitude<1e-4f){Lay();continue;}ahead.Normalize();var right=Vector3.Cross(Vector3.up,ahead);
                        bool nearJunction=false;foreach(var j in junctions)if(new Vector2(j.x-p.x,j.z-p.z).magnitude<26)nearJunction=true;if(nearJunction){Lay();continue;}
                        float Ground(float k){var q=p+right*side*(half+k);return H(q.x,q.z);}
                        bool placed=false;
                        for(float k=1.5f;k<=11;k+=.5f) {
                            float top=Ground(k),low=Mathf.Min(Ground(k+1),Mathf.Min(Ground(k+2),Ground(k+3)));
                            // (The drop starts: more than 0.5 m down over the next 1.5 m, and over 1.2 m below the road within 3 m.)
                            if(top-Ground(k+1.5f)<.5f||p.y-low<1.2f||p.y-top>1.4f)continue;
                            // (Another street within reach on this side: its own edge, not a garden.)
                            var at=p+right*side*(half+k);if(ValleyField.RoadDistance(at.x,at.z)<k+half-.5f)break;
                            // (Not through a house or a yard: the buildings already stand.)
                            // (Street review, 4 Oct: a wall across a front garden stood in front of the door. Within 4 m of a house
                            // the drop is its garden; the plinth and its door steps handle it.)
                            bool inside=false;foreach(var b in BayviewKit.Footprints){if(b.size.x<4&&b.size.z<4)continue;var e=b;e.Expand(new Vector3(8,40,8));if(e.Contains(new Vector3(at.x,b.center.y,at.z))){inside=true;break;}}
                            if(inside)break;
                            at.y=top;run.Add((at,low-.2f));placed=true;break;
                        }
                        // One missing point doesn't break a wall (a gap of 4 m read as two stubs).
                        if(placed)misses=0;else if(++misses>=2)Lay();
                    }
                    Lay();
                }
            }
            Debug.Log("LITTLE_ROAD_QUARRY_RETAINING segments="+Retaining);
        }
        void FlushWalls(){
            if(yardRuns.Count>0){var slate=new Dictionary<string,string>{{"drystone","drystone_slate"},{"drystone_pier","drystone_slate_pier"}};
                // A run ends in piers, which need level ground: each end moves in off a steep fall (half a metre across 1.2 m),
                // and a run left under 8 m is stone boxes, as are the ends trimmed off it (the yards stay closed).
                int laid=0;
                for(int i=0;i<yardRuns.Count;i++) {
                    var line=yardRuns[i];float len=0;for(int k=1;k<line.Count;k++)len+=Vector3.Distance(line[k-1],line[k]);
                    float s0=0,s1=len;bool Steep(float at,float dir){var p0=At(line,at);var p1=At(line,at+dir*1.2f);return Mathf.Abs(H(p0.x,p0.z)-H(p1.x,p1.z))>.5f;}
                    while(s1-s0>=8&&s0<6&&Steep(s0,1))s0+=.5f;while(s1-s0>=8&&len-s1<6&&Steep(s1,-1))s1-=.5f;
                    if(s1-s0<8){BoxRun(line,0,len);continue;}
                    if(s0>0)BoxRun(line,0,s0);if(s1<len)BoxRun(line,s1,len);
                    var L=Runs.Solve(new RunSpec{name=$"Quarry Hills yard wall {i}",kit="dry_stone",seed=$"qh-yard-{i}",along=line.ToArray(),from=s0,to=s1,restyle=slate});
                    if(L.failures.Count>0){BoxRun(line,s0,s1);continue;}   // (a pier that can't stand level on a rough hillside: boxes there)
                    Runs.Draw(L,root);Runs.Laid.Add(L);laid++;
                }
                Debug.Log($"LITTLE_ROAD_QUARRY_YARD_RUNS lines={yardRuns.Count} runs={laid} longest at {yardRuns.OrderByDescending(r=>r.Count).First()[0]}");yardRuns.Clear();}
Combine("Yard walls",walls,stone,true);Combine("Footpaths",paths,paving,false);Combine("Crawiau",slabs,WorldLook.Kit("Crawiau slate",new Color(.36f,.38f,.43f),WorldLook.Pattern.Slate,0,.1f,true),true);}
        void Combine(string name,List<Matrix4x4> boxes,Material material,bool solid)
        {
            if(boxes.Count==0)return;
            var temp=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=temp.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(temp);
            for(int start=0;start<boxes.Count;start+=3000) {
                int n=Mathf.Min(3000,boxes.Count-start);var combine=new CombineInstance[n];
                // (--prop-forge: the crawiau as split-slate slabs from the slate kit, eight variants in the unit cube the boxes
                //  filled, so each slab's own matrix places it: docs/80 round 4)
                var slabsKit=name=="Crawiau"&&PropForge.Enabled?PropKit.Load("slate"):null;
                for(int i=0;i<n;i++)combine[i]=new CombineInstance{mesh=slabsKit!=null?slabsKit.Mesh("crawiau_"+((start+i)*7919%8),0):cube,transform=boxes[start+i]};
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
                if(i-i0>=2){Strip("Back lane",lanePath,placedOnLane[i0]-4,placedOnLane[i-1]+4,LaneW,laneGravel);
                    // (--prop-forge: the cars live here, parked down the middle of the back lane, the yard walls 0.7 m clear either
                    //  side, as a terrace's cars do; the fronts have no room for one: QH2-05)
                    if(PropForge.Enabled)for(float s=placedOnLane[i0]+3;s<placedOnLane[i-1]-3;s+=19)if(rng.NextDouble()<.55) {
                        var p=lanePath.At(s);var f=lanePath.Forward(s);if(Mathf.Abs(H(p.x+f.x*2.2f,p.z+f.z*2.2f)-H(p.x-f.x*2.2f,p.z-f.z*2.2f))>.6f)continue;
                        p.y=H(p.x,p.z);Homes.Add((p,Quaternion.LookRotation(rng.NextDouble()<.5?f:-f).eulerAngles.y,"lane"));}}
                i0=i;
            }
        }
        // One row of houses `d` metres out with its back towards the road (or, for `facing`, its front towards the road
        // across the footpath, i.e. its back at `d`).
        void BackRow(HillcrestRoute.Road road,int side,float d,float from,float to,bool facing,List<float> onLane,HillcrestRoute.Road lanePath)
        {
            var core=QuarryHills.Square;
            for(float s=from;s<to;) {
                string id=rng.NextDouble()<.85?new[]{"terrace_a","terrace_c","terrace_d","terrace_d","terrace_f","terrace_f","terrace_g"}[rng.Next(7)]:"house_d";
                if(rng.NextDouble()<.5)id=new[]{"cottage_a","cottage_b","cottage_c"}[rng.Next(3)];
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
                    // Its footpath, a short flag strip at the door, only where the ground under it is near flat and clear of
                    // any street (street review, 5 Oct: on a bank the strip stood out over the drop as a ledge, and next to a
                    // neighbouring street's front gardens only its tilted edge showed, as a stray slab on the lawn).
                    var p=front+outward*(facing?-1.3f:1.3f);var pr=Quaternion.Euler(0,yaw,0);float lo=float.MaxValue,hi=float.MinValue;
                    foreach(var (cx,cz) in new[]{(-1f,-1f),(1f,-1f),(-1f,1f),(1f,1f),(0f,0f)}){var q=p+pr*new Vector3(cx*(size.x*.5f+.05f),0,cz*1.1f);float g=H(q.x,q.z);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
                    if(hi-lo<.3f&&ValleyField.RoadDistance(p.x,p.z)>9f)paths.Add(Matrix4x4.TRS(new Vector3(p.x,hi-.12f,p.z),pr,new Vector3(size.x+.1f,.3f,2.2f)));
                    s+=size.x+.3f;
                } else s+=3;
            }
        }
        // Pavement strips either side of a street, broken where another road crosses (as Hillcrest's).
        // A pavement's own end (not a break at a junction or the square, where a post would stand in the way of turning cars).
        static bool PavementEnd(HillcrestRoute.Road road,Vector3 p)
        {
            if(Vector2.Distance(new(p.x,p.z),QuarryHills.Square)<40)return false;
            foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+20)return false;
            return true;
        }
        void Pavements(HillcrestRoute.Road road,float from,float to)
        {
            foreach(int side in new[]{-1,1}) {
                var run=new List<Vector3>();
                // (Street review, 4 Oct: pavements stopped in a square edge on the verge.) Each run ends at a slate post on its
                // garden side.
                void Flush(){if(run.Count>3){StripLine("Pavement",run.ToArray(),Pave,.06f,paving);
                    foreach(var (end,next) in new[]{(run[0],run[1]),(run[^1],run[^2])}){if(!PavementEnd(road,end))continue;var d=end-next;d.y=0;var outward=Vector3.Cross(Vector3.up,d.normalized)*side;
                        if(Vector3.Dot(outward,end-road.At(road.Nearest(end,out _)))<0)outward=-outward;var post=end+outward*(Pave*.5f-.3f)+d.normalized*.2f;
                        walls.Add(Matrix4x4.TRS(new Vector3(post.x,H(post.x,post.z)+.45f,post.z),Quaternion.LookRotation(d),new Vector3(.5f,1.1f,.5f)));}}
                    run.Clear();}
                for(float s=Mathf.Max(0,from);s<=Mathf.Min(road.Length,to);s+=2) {
                    Vector3 p=road.At(s)+road.Right(s)*side*(road.width*.5f+Pave*.5f);
                    bool clear=Vector2.Distance(new(p.x,p.z),QuarryHills.Square)>26;
                    // (Not into a back street's junction: its fillets reach ~15 m from the node.)
                    foreach(var node in new[]{QuarryHills.ChapelFrom,QuarryHills.ChapelTo,QuarryHills.TerraceFrom,QuarryHills.TerraceTo})if(Vector2.Distance(new(p.x,p.z),new(node.x,node.z))<15)clear=false;
                    if(clear)foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+2.4f){clear=false;break;}
                    // The Pit Stop's driveway: no pavement across it (its apron runs from the kerb to the forecourt).
                    if(clear&&road.name==QuarryHills.QuarryRoadName&&InDriveway(p))clear=false;
                    // (--prop-forge: not over a drop: at Quay Lane's end the pavement hung down the cutting, QH2-01)
                    if(clear&&PropForge.Enabled){var o=road.At(s)+road.Right(s)*side*(road.width*.5f+Pave);if(road.At(s).y-Drawn(o.x,o.z)>.6f)clear=false;}
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
                bool lit=false;foreach(var l in streetLamps)if(Vector2.Distance(new(l.x,l.z),new(ends[i].x,ends[i].z))<6f){ends[i]=l;lit=true;break;}
                if(!lit)Lamp(ends[i]);
            }
            HillcrestTown.BuntingLine(root,ends[0]+Vector3.up*4.4f,ends[1]+Vector3.up*4.4f,.9f);
        }
    }
}
