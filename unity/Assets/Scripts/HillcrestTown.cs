using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Hillcrest: a hill town across the valley. The lower town lines the riverside quay
    // (Hillcrest Motors, the Grand Hotel, the mill, the station); Via Crest climbs past rows
    // of tall painted houses to the market square (town hall clock tower, church, cafÃ©s,
    // bandstand, a balcony over the valley) and on to the castle on the summit. Round about:
    // the vista point, the falls lookout, vineyards, lavender and sunflower fields, farms,
    // wind turbines on the far ridge and the raceway by the lake.
    public sealed class HillcrestTown
    {
        public static readonly Vector3 Square=HillcrestRoute.Square;
        // The market square's paving disc (MarketSquare): 33 m round a point 3 m out from the square's centre, away from the hill.
        public static bool OnMarketPaving(Vector3 p){var outward=new Vector3(Square.x-HillcrestRoute.HillCentre.x,0,Square.z-HillcrestRoute.HillCentre.y).normalized;var c=Square+outward*3;return new Vector2(p.x-c.x,p.z-c.z).sqrMagnitude<34.5f*34.5f;}
        // Where residents stand (filled in by Build; read by AdventureGame).
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Spots=new();
        // Front yards of the street houses (parking spot, parallel yaw, house kit).
        public static readonly List<(Vector3 yard,float yaw,string house)> Homes=new();
        // The castle gatehouse (wall ring r 38 m, nominally at 172 degrees). Via Crest swings in from its spiral and
        // crosses the ring ~15 degrees off the radial, so the gatehouse sits on the road as built and faces out along
        // it: the drawbridge (2-10 m out) then lies under the road instead of across its edge.
        public static (Vector3 at,float yaw) CastleGate()
        {
            var c=HillcrestRoute.CastleCourt;Vector3 Ring(float r){float a=172*Mathf.Deg2Rad;return new Vector3(c.x+Mathf.Sin(a)*r,0,c.z+Mathf.Cos(a)*r);}
            var crest=HillcrestRoute.Crest;var at=crest.At(crest.Nearest(Ring(38),out _));at.y=HillcrestRoute.CastleY-.05f;
            return (at,Quaternion.LookRotation(-crest.Forward(crest.Nearest(Ring(43),out _))).eulerAngles.y);
        }
        public static readonly List<Vector3> Chimneys=new();
        static readonly List<(Vector2 centre,float radius)> reserved=new();
        // Footprints of the landmarks placed before the rows (street review, 5 Oct: a terrace stood over the mill's water
        // wheel, which turned inside the house next door). The mill keeps 3 m clear on its wheel sides.
        static readonly List<(Vector2 c,Vector2 half,float yaw)> landmarks=new();
        static bool OnLandmark(Vector3 centre,Vector2 size,float yaw){var c=new Vector2(centre.x,centre.z);foreach(var l in landmarks)if(QuarryHillsTown.Overlap(c,size*.5f,yaw,l.c,l.half,l.yaw))return true;return false;}
        public static bool Reserved(float x,float z)
        {
            if(Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)<196)return true;
            foreach(var r in reserved)if(Vector2.Distance(new(x,z),r.centre)<r.radius)return true;
            return false;
        }
        readonly Transform root;readonly System.Random rng=new(2207);
        Material paving,plinth,railWhite;int buildings,plinths,buried,footings;readonly List<Matrix4x4> footingBoxes=new(); // (one mesh: ~2000 pieces)
        public HillcrestTown(CoastalLandscape landscape,Transform parent){root=new GameObject("Hillcrest town").transform;root.SetParent(parent);}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
        // Earth pigments of a Ligurian hill town: ochres, saffron, terracotta, Pompeian red, rose, sienna, sage.
        static readonly int[] Walls={0xe3a83b,0xeab04a,0xc9683f,0xd98545,0xb5493a,0xd98b7e,0xe39a72,0xb7703f,0xe8c27a,0xa9b58c,0xe6d3ad,0xd7a09a,0xe8cd5e,0xcf7a4a};
        static readonly int[] Shutters={0x2f6b3f,0x3b5f8a,0x2a6b5c,0x7a4a2e,0x9c3b30,0x2d5f73,0x5a6e35,0x3d7a4e};
        static readonly int[] Awnings={0xff4d5e,0x3a7bd5,0x2e9e5b,0xffb400,0xff7ac8,0x1f8a70};
        // Roofs: mostly fresh terracotta, then weathered and darker tiles, and some grey Ligurian slate. Their own
        // random stream, so the town's layout (drawn from `rng`) doesn't move.
        static readonly int[] Roofs={0,0,0,0xc4684c,0xb04a2e,0xd98760,0xa85a3c,0x69707a};
        readonly System.Random roofRng=new(5113);
        BayviewKit.Tint RandomTint(){int s=Shutters[rng.Next(Shutters.Length)];var t=T(Walls[rng.Next(Walls.Length)],s,Awnings[rng.Next(Awnings.Length)],s);
            int roof=Roofs[roofRng.Next(Roofs.Length)];if(roof!=0)t.roof=Toy.Hex(roof);return t;}
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return Quaternion.LookRotation(d).eulerAngles.y;}
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,float scale=1,bool solid=true,Transform parent=null)=>
            BayviewKit.Spawn(id,p,yaw,parent??root,tint,sign,solid,scale);
        GameObject Ground(string id,float x,float z,float yaw,BayviewKit.Tint tint=null,string sign=null,float scale=1,bool solid=true)=>Put(id,new Vector3(x,H(x,z)-.04f,z),yaw,tint,sign,scale,solid);
        // (--prop-forge: a parapet's cap drawn as Prop Forge's saddleback coping stone, docs/78)
        static int copingLogged;static void Coping(GameObject cap){if(!PropForge.Enabled)return;cap.GetComponent<MeshFilter>().sharedMesh=PropKit.Load("armco").Mesh("coping");if(RaceTrial.Has("--gate-log")&&(++copingLogged)%30==1)Debug.Log($"LITTLE_ROAD_COPING {cap.name} {cap.transform.position.x:0.0},{cap.transform.position.y:0.0},{cap.transform.position.z:0.0}");}
        GameObject Box(string name,Vector3 p,Vector3 size,Quaternion rotation,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root);go.transform.SetPositionAndRotation(p,rotation);go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material;if(solid)go.tag="Respawn";else Object.Destroy(go.GetComponent<Collider>());return go;
        }
        public void Build()
        {
            if(!BayviewKit.Has("terrace_a"))return;
            Spots.Clear();reserved.Clear();landmarks.Clear();Homes.Clear();Chimneys.Clear();StreetLamps.Clear();ZipTop=null;ZipLine.Active=null;ZipLine.All.Clear();
            paving=WorldLook.Kit("Hillcrest paving",Toy.Hex(0xb3a68c),WorldLook.Pattern.Stone,0,.15f,true);plinth=WorldLook.Kit("Hillcrest stone plinth",Toy.Hex(0xc2b08e),WorldLook.Pattern.Stone,0,.12f,true);
            railWhite=TrialWorld.Material("Balustrade white",Toy.Hex(0xfff8ec),0,.25f);
            // A race away from the town (RaceArea) builds only the raceway's own buildings.
            if(RaceArea.Active&&!RaceArea.Near(Square,500)){if(RaceArea.Near(HillcrestRoute.Circuit[0],600))Raceway();Debug.Log($"LITTLE_ROAD_HILLCREST race-area only buildings={buildings}");return;}
            CoastalLandscape.Lap("town start");ReserveZipLanding();CoastalLandscape.Lap("town ReserveZipLanding");MarketSquare();CoastalLandscape.Lap("town MarketSquare");Castle();CoastalLandscape.Lap("town Castle");LowerTown();CoastalLandscape.Lap("town LowerTown");Station();CoastalLandscape.Lap("town Station");Streets();CoastalLandscape.Lap("town Streets");SquareRim();CoastalLandscape.Lap("town SquareRim");Vista();CoastalLandscape.Lap("town Vista");Lookout();CoastalLandscape.Lap("town Lookout");Raceway();CoastalLandscape.Lap("town Raceway");Countryside();CoastalLandscape.Lap("town Countryside");HillSlopes();CoastalLandscape.Lap("town HillSlopes");Patchwork();CoastalLandscape.Lap("town Patchwork");ZipLineBuild();CoastalLandscape.Lap("town ZipLineBuild");TunnelFrame();CoastalLandscape.Lap("town TunnelFrame");Gardens();CoastalLandscape.Lap("town Gardens");
            if(footingBoxes.Count>0){QuarryFloor.Combine("Hillside footings",footingBoxes,plinth,root,false);footingBoxes.Clear();}
            Debug.Log($"LITTLE_ROAD_HILLCREST buildings={buildings} plinths={plinths} buried={buried} footings={footings} spots={Spots.Count}");
        }
        // ---------------------------------------------------------------- public gardens
        // (Street review, 4 Oct: bare lawns at the castle's arrival, on Station Road and by the river at the quay.) A small
        // garden on the nearest clear, level 16 m plot to each: a gravel cross of paths, four flower beds, benches facing in,
        // cypresses at the corners and an urn of flowers in the middle.
        void Gardens()
        {
            var gravel=WorldLook.Kit("Garden gravel",Toy.Hex(0xd8c9a8),WorldLook.Pattern.Rock,0,.1f,true);int made=0,road=0,river=0,steep=0,taken=0;
            foreach(var near in new[]{new Vector2(232,-1145),new Vector2(400,-950),new Vector2(110,-890)}) {
                for(float r=0;r<=30;r+=3){bool done=false;for(float a=0;a<360&&!done;a+=r<1?360:30) {
                    var c=near+new Vector2(Mathf.Cos(a*Mathf.Deg2Rad),Mathf.Sin(a*Mathf.Deg2Rad))*r;
                    if(ValleyField.RoadDistance(c.x,c.y)<11){road++;continue;}if(HillcrestRoute.Distance(HillcrestRoute.River.line,c.x,c.y,out _)<HillcrestRoute.River.width*.5f+5){river++;continue;}
                    float h0=H(c.x,c.y),lo=h0,hi=h0;for(int i=-1;i<=1;i++)for(int j=-1;j<=1;j++){float h=H(c.x+i*8,c.y+j*8);lo=Mathf.Min(lo,h);hi=Mathf.Max(hi,h);}
                    if(hi-lo>1.6f){steep++;continue;}if(BayviewKit.Occupied(new Bounds(new Vector3(c.x,h0+1,c.y),new Vector3(16,3,16)))){taken++;continue;}
                    var at=new Vector3(c.x,h0,c.y);float yaw=Yaw(at,new Vector3(near.x,h0,near.y)+new Vector3(.01f,0,.01f));var rot=Quaternion.Euler(0,yaw,0);
                    foreach(var (along,len) in new[]{(rot*Vector3.forward,16f),(rot*Vector3.right,16f)})
                        Box("Garden path",at+Vector3.up*.03f,new Vector3(1.8f,.06f,len),Quaternion.LookRotation(along),gravel,false);
                    foreach(var (x,z) in new[]{(-4f,-4f),(4f,-4f),(-4f,4f),(4f,4f)}){var p=at+rot*new Vector3(x,0,z);p.y=H(p.x,p.z)-.02f;Put("flower_bed",p,yaw,null,null,.9f,false);}
                    foreach(var (x,z) in new[]{(-7f,-7f),(7f,-7f),(-7f,7f),(7f,7f)}){var p=at+rot*new Vector3(x,0,z);p.y=H(p.x,p.z)-.04f;Put("tree_cypress",p,0,null,null,.9f,true);}
                    foreach(var (x,z,f) in new[]{(0f,-6.2f,0f),(0f,6.2f,180f),(-6.2f,0f,90f),(6.2f,0f,270f)}){var p=at+rot*new Vector3(x,0,z);p.y=H(p.x,p.z)-.02f;p+=rot*(x==0?Vector3.right:Vector3.forward)*2.2f;Put("bench",p,yaw+f,null,null,1,false);}
                    Put("fountain",new Vector3(at.x,h0-.02f,at.z),yaw,null,null,.55f,true);
                    reserved.Add((c,10));made++;done=true;}
                    if(done)break;}
            }
            Debug.Log($"LITTLE_ROAD_HILLCREST_GARDENS made={made} refused road={road} river={river} steep={steep} taken={taken}");
        }
        // ---------------------------------------------------------------- buildings on slopes
        // Kit footprints (width along the frontage, depth).
        static Vector2 Footprint(string id)=>id switch{"terrace_a"=>new(5.4f,8),"terrace_b"=>new(5.4f,8),"terrace_c"=>new(5.4f,8),"terrace_d"=>new(5.4f,8),"terrace_e"=>new(5.4f,8),"terrace_f"=>new(5.4f,8),"terrace_g"=>new(5.4f,8),
            "house_a"=>new(8.5f,7.5f),"house_b"=>new(6.2f,7.5f),"house_c"=>new(10.8f,8.5f),"house_d"=>new(5.6f,7.4f),
            "shop"=>new(9,8),"cafe"=>new(9,8),"post_office"=>new(9,8),"church"=>new(12,14),"motors"=>new(15,11),"hotel"=>new(16,10),"town_hall"=>new(20,16),
            "station"=>new(22,8),"mill"=>new(12,11),_=>new(8,8)};
        // Stands a building with its frontage on `centre`'s height; a stone plinth fills any drop.
        bool Building(string id,Vector3 centre,float yaw,BayviewKit.Tint tint,string sign=null,bool force=false)
        {
            var size=Footprint(id);var rot=Quaternion.Euler(0,yaw,0);float floor=centre.y,low=floor,high=floor;
            // A 5 x 5 grid over the footprint (as Quarry Hills': street review, 5 Oct, corners alone missed a dip between them, so
            // a plinth stopped short and the house stood over a gap). The road test stays on the corners and middle.
            for(int i=0;i<5;i++)for(int j=0;j<5;j++) {
                var c=new Vector2(i/2f-1,j/2f-1);var p=centre+rot*new Vector3(c.x*size.x*.5f,0,c.y*size.y*.5f);float g=H(p.x,p.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                if(!force&&(i%2==0&&j%2==0)&&ValleyField.RoadDistance(p.x,p.z)<5.3f)return false;
            }
            if(floor-low>7.5f&&!force)return false;
            // (Street review, 5 Oct: on Via Crest's steep side the hill rose 3-4 m up a house's side wall on a diagonal, so it
            // read as half-buried.) No more than 5 m of hillside against a house; below that, a stepped stone footing on
            // each side and back wall up to just above the grass, so the hill meets stone, as on a real hillside house
            // (3 m as a hard limit took a third of the town away).
            if(high-floor>5f&&!force){buried++;return false;}
            if(high-floor>.5f) {
                float hx=size.x*.5f,hz=size.y*.5f;
                void Footing(Vector3 a,Vector3 b,Vector3 outward)
                {
                    int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/1.2f));bool alongZ=Mathf.Abs(outward.x)>.5f;
                    for(int k=0;k<n;k++) {
                        Vector3 p0=Vector3.Lerp(a,b,k/(float)n),p1=Vector3.Lerp(a,b,(k+1)/(float)n),w0=centre+rot*p0,w1=centre+rot*p1,wm=(w0+w1)*.5f;
                        float top=Mathf.Max(H(w0.x,w0.z),H(w1.x,w1.z),H(wm.x,wm.z))+.3f;if(top-floor<.45f)continue;
                        float len=Vector3.Distance(p0,p1)+.02f,tall=top-floor+.3f;var mid=centre+rot*((p0+p1)*.5f+outward*.09f);mid.y=floor-.3f+tall*.5f;
                        footingBoxes.Add(Matrix4x4.TRS(mid,rot,alongZ?new Vector3(.16f,tall,len):new Vector3(len,tall,.16f)));footings++;
                    }
                }
                Footing(new(-hx,0,-hz),new(-hx,0,hz),Vector3.left);Footing(new(hx,0,-hz),new(hx,0,hz),Vector3.right);Footing(new(-hx,0,-hz),new(hx,0,-hz),Vector3.back);
            }
            if(floor-low>.25f){Box("Stone plinth",new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),new Vector3(size.x+.5f,floor-low+.5f,size.y+.5f),rot,plinth,true);plinths++;}
            Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign);buildings++;
            if(force)landmarks.Add((new(centre.x,centre.z),size*.5f+(id=="mill"?new Vector2(3,1):new Vector2(.5f,.5f)),yaw));
            return true;
        }
        static readonly string[] ShopNames={"BAKERY","GELATO","FLORIST","BOOKS","TOYS","BARBER","PASTA","CHEESE","TAILOR","CHEMIST","MUSIC","CAMERAS","SWEETS","BIKES","HATS","WINE","OLIVES","KITES","PIZZA","SOAP"};
        int shopName;
        // Fills one side of a road with a row of houses facing it, between runs [from,to].
        void Row(HillcrestRoute.Road road,int side,float from,float to,float setback=12.2f,float gapChance=.14f)
        {
            float s=from;string last=null;
            while(s<to) {
                // Terraces (plain, shop, arcade, bay window, four-storey palazzetto, low cottage, painted gable) mostly; villas, tower houses and cottages between.
                double roll=rng.NextDouble();
                string id=roll<.62?new[]{"terrace_a","terrace_a","terrace_b","terrace_b","terrace_c","terrace_d","terrace_d","terrace_e","terrace_f","terrace_g"}[rng.Next(10)]:
                    roll<.72?"house_c":roll<.82?"house_d":roll<.91?"house_b":"house_a";
                // No two of the same front side by side (street review, 4 Oct: twin houses read as copy-paste).
                if(id==last)id=id=="terrace_f"?"terrace_d":id.StartsWith("house")?"terrace_a":"terrace_f";
                float w=Footprint(id).x;float mid=s+w*.5f;if(mid+w*.5f>to)break;
                if(rng.NextDouble()<gapChance){Greenery(road,side,s+3,setback);GardenGaps.Add((road,side,s,s+7));s+=7;continue;}
                Vector3 at=road.At(mid),right=road.Right(mid);var centre=at+right*side*(setback+Footprint(id).y*.5f-4);centre.y=at.y+.02f;
                if(Vector2.Distance(new(centre.x,centre.z),new(Square.x,Square.z))<44||Vector2.Distance(new(centre.x,centre.z),HillcrestRoute.HillCentre)<50){s+=3;continue;}
                // Keep the Pit Stop plot and forecourt clear, and the zipline landing and its lane.
                if(Vector2.Distance(new(centre.x,centre.z),new(HillcrestRoute.PitStopSite.at.x,HillcrestRoute.PitStopSite.at.z))<24||InZipLanding(centre.x,centre.z)){s+=3;continue;}
                if(OnLandmark(centre,Footprint(id),Yaw(centre,at))){s+=3;continue;}
                if(Building(id,centre,Yaw(centre,at),RandomTint(),id=="terrace_b"?ShopNames[shopName++%ShopNames.Length]:null)) {
                    // The front yard between pavement and door: where the resident parks.
                    last=id;
                    // (Street review, 4 Oct: the old spot, 2.9 m out, put cars half on the pavement.) The middle of the garden
                    // between the pavement's back (2.3 m) and the house front; gardens under 2.5 m deep have no parking (street review, 5 Oct: at 2 m a car stood half on the pavement, across a door).
                    float garden0=road.width*.5f+2.3f,garden1=setback-4-.15f;
                    if(garden1-garden0>=2.5f&&id!="terrace_f"&&id!="terrace_b"&&id!="terrace_c"){var yard=at+right*side*((garden0+garden1)*.5f);yard.y=at.y;Homes.Add((yard,Quaternion.LookRotation(road.Forward(mid)*(rng.NextDouble()<.5?1:-1)).eulerAngles.y,id));}
                    // Chimney tops (kit x mirrors on import): some of them smoke.
                    if(id!="house_b"&&id!="house_d"&&rng.NextDouble()<.35)Chimneys.Add(centre+Quaternion.Euler(0,Yaw(centre,at),0)*(id switch{"house_a"=>new Vector3(-2.6f,10.5f,-1.2f),"house_c"=>new Vector3(-2.8f,10.2f,-1.4f),
                        "terrace_e"=>new Vector3(-1.9f,15.2f,2.7f),"terrace_f"=>new Vector3(-1.9f,10.7f,-.6f),"terrace_g"=>new Vector3(1.6f,13.1f,-2.4f),_=>new Vector3(-1.4f,12.1f,-1.5f)}));
                    // (Street review, 5 Oct: a 35 cm slit between neighbours showed the hillside through the row.) Neighbours meet.
                    // A 2.5 m gap to the next house now and then: on a hillside's downhill side it's a garden gap like the wider
                    // ones, closed by a low wall where the ground behind falls away (the edge audit's open drops along Via
                    // Crest, 12 Oct, were these gaps).
                    bool spaced=rng.NextDouble()<.2;if(spaced)GardenGaps.Add((road,side,s+w+.05f,s+w+.05f+2.5f));
                    s+=w+.05f+(spaced?2.5f:0);
                }
                else s+=3;
            }
        }
        void Greenery(HillcrestRoute.Road road,int side,float s,float setback)
        {
            Vector3 at=road.At(s),p=at+road.Right(s)*side*(setback-1.5f);
            if(ValleyField.RoadDistance(p.x,p.z)<8f||Reserved(p.x,p.z))return;
            Ground(rng.NextDouble()<.5?"tree_cypress":"olive_tree",p.x,p.z,(float)rng.NextDouble()*360,null,null,.9f+(float)rng.NextDouble()*.3f);
        }
        // Pavement strips either side of a street, broken where another road crosses.
        void Pavements(HillcrestRoute.Road road,float from,float to)
        {
            foreach(int side in new[]{-1,1}) {
                var run=new List<Vector3>();
                // (Street review, 4 Oct: a pavement ending square on bare grass.) Each run ends at a stone post on its outer side.
                void Flush(){if(run.Count>3){Strip("Pavement",run.ToArray(),2.3f,.06f);
                    foreach(var (end,next) in new[]{(run[0],run[1]),(run[^1],run[^2])}){bool junction=false;foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,end.x,end.z,out _)<other.width*.5f+20)junction=true;
                        if(junction||Vector3.Distance(end,Square)<50)continue;var d=end-next;d.y=0;var outward=end-road.At(road.Nearest(end,out _));outward.y=0;outward.Normalize();
                        var post=end+outward*.85f+d.normalized*.2f;post.y=H(post.x,post.z);Box("Pavement end post",post+Vector3.up*.45f,new Vector3(.5f,1.1f,.5f),Quaternion.LookRotation(d),plinth,true);}}
                    run.Clear();}
                for(float s=from;s<=to;s+=2) {
                    Vector3 p=road.At(s)+road.Right(s)*side*(road.width*.5f+1.15f);
                    bool clear=true;foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+2.4f){clear=false;break;}
                    // (Street review, 5 Oct: at a junction the strip's square end stood past the rounded kerb, in the road.)
                    // Nor inside a junction's surface.
                    if(clear&&CoastalLandscape.Network!=null&&CoastalLandscape.Network.OnJunction(p,1.5f))clear=false;
                    if(clear)run.Add(p);else Flush();
                }
                Flush();
            }
        }
        void Strip(string name,Vector3[] line,float width,float lift)
        {
            int n=line.Length;var v=new Vector3[n*2];var t=new List<int>();
            for(int i=0;i<n;i++) {
                Vector3 f=line[Mathf.Min(n-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                v[i*2]=line[i]-r*width*.5f+Vector3.up*lift;v[i*2+1]=line[i]+r*width*.5f+Vector3.up*lift;if(i<n-1){int a=i*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh{name=name,vertices=v};mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=paving;
        }
        void Lamps(HillcrestRoute.Road road,float from,float to,float every)
        {
            int k=0;
            for(float s=from;s<=to;s+=every,k++) {
                int side=k%2==0?1:-1;Vector3 p=road.At(s)+road.Right(s)*side*(road.width*.5f+.7f);
                bool clear=true;foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+1.2f){clear=false;break;}
                if(clear){var lamp=new Vector3(p.x,road.At(s).y,p.z);Put("lamp_post",lamp,0,T(0xffffff,0x2e5d3a,0xffffff,0xffffff),null,1,false);StreetLamps.Add(lamp);}
            }
        }
        // Street lamps placed so far (cross-street bunting hangs from them where one stands at its end).
        public static readonly List<Vector3> StreetLamps=new();
        // Bunting across a street at run `s`, hung from a lamp post on each pavement (Richard, 2 Oct: runs across the
        // Quay and Via Crest hung from nothing). The street's own lamps alternate sides, so a lamp is added at any end
        // without one within 2.5 m; a run whose end would stand on another road is left out.
        void StreetBunting(HillcrestRoute.Road road,float s)
        {
            var ends=new Vector3[2];
            for(int i=0;i<2;i++) {
                int side=i==0?1:-1;var p=road.At(s)+road.Right(s)*side*(road.width*.5f+.7f);
                foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,p.x,p.z,out _)<other.width*.5f+1.2f)return;
                p.y=road.At(s).y;ends[i]=p;
            }
            for(int i=0;i<2;i++) {
                bool lit=false;foreach(var l in StreetLamps)if(Vector2.Distance(new(l.x,l.z),new(ends[i].x,ends[i].z))<2.5f){ends[i]=l;lit=true;break;}
                if(!lit){Put("lamp_post",ends[i],0,T(0xffffff,0x2e5d3a,0xffffff,0xffffff),null,1,false);StreetLamps.Add(ends[i]);}
            }
            Bunting(ends[0]+Vector3.up*4.4f,ends[1]+Vector3.up*4.4f,.9f);
        }
        // ---------------------------------------------------------------- market square
        void MarketSquare()
        {
            Vector3 c=Square;var inward=new Vector3(HillcrestRoute.HillCentre.x-c.x,0,HillcrestRoute.HillCentre.y-c.z).normalized;var outward=-inward;
            reserved.Add((new(c.x,c.z),46));
            Vector3 Rim(float phi,float rho){var d=Quaternion.Euler(0,phi,0)*outward;var p=c+d*rho;p.y=HillcrestRoute.SquareY;return p;}
            // Paving disc (visual) with a grip trigger.
            var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disc.name="Market square paving";disc.transform.SetParent(root);Object.Destroy(disc.GetComponent<Collider>());
            disc.transform.position=c+Vector3.up*.03f+outward*3;disc.transform.localScale=new Vector3(66,.02f,66);disc.GetComponent<Renderer>().sharedMaterial=paving;
            var grip=new GameObject("Market square grip",typeof(BoxCollider),typeof(RoadSurface));grip.transform.SetParent(root);grip.transform.position=c+outward*3;
            var box=grip.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(66,1.5f,66);
            // Town hall across Via Crest, facing the square; the rim buildings face the fountain.
            var hall=c+inward*31;hall.y=HillcrestRoute.SquareY+.02f;Building("town_hall",hall,Yaw(hall,c),T(0xe8c27a,0x3a7bd5,0xff4d5e,0x3a7bd5),"TOWN HALL",true);
            Building("cafe",Rim(58,29),Yaw(Rim(58,29),c),T(0xd98545,0x2e9e5b,0x3a7bd5,0x2e9e5b),"BELLA VISTA",true);
            Building("church",Rim(80,30),Yaw(Rim(80,30),c),T(0xe6d8b8,0x2e9e5b,0xffffff,0x6b4226),null,true);
            Building("terrace_b",Rim(100,27),Yaw(Rim(100,27),c),T(0xeab04a,0x3a7bd5,0xff7ac8,0x3a7bd5),"GELATO",true);
            Building("cafe",Rim(-58,29),Yaw(Rim(-58,29),c),T(0xc9683f,0x1f8a70,0xffb400,0x1f8a70),"CAFFE LUNA",true);
            Building("shop",Rim(-78,29),Yaw(Rim(-78,29),c),T(0xe3b95a,0x9a5b3c,0x2e9e5b,0x9a5b3c),"HILLCREST DELI",true);
            Building("terrace_b",Rim(-98,27),Yaw(Rim(-98,27),c),T(0xa9b58c,0xd9534f,0xff4d5e,0xd9534f),"BAKERY",true);
            Spots["hillcafe"]=(Rim(58,20.5f),Yaw(Rim(58,20.5f),c)+90);Spots["square"]=(c,0);Spots["gelato"]=(Rim(-20,8.5f),Yaw(Rim(-20,8.5f),c)+90);
            // Fountain, stalls, bandstand, lamps and bunting, benches facing the view, flags.
            Put("fountain",Rim(0,2)+Vector3.down*.02f,0);
            int k=0;foreach(float phi in new[]{30f,90f,150f,-30f,-90f,-150f}){var p=Rim(phi,11);Put("market_stall",p,Yaw(p,c),T(0xffffff,0,Awnings[k%Awnings.Length],0));k++;}
            Put("bandstand",Rim(0,19),0,T(0xffffff,0,0xff4d5e,0));
            foreach(float phi in new[]{25f,50f,75f,-25f,-50f,-75f})Put("lamp_post",Rim(phi,23),0,T(0xffffff,0x2e5d3a,0xffffff,0xffffff));
            for(int i=0;i<3;i++)Bunting(Rim(25+25*i,23)+Vector3.up*4.4f,Rim(-25-25*i,23)+Vector3.up*4.4f,2.2f);
            Bunting(Rim(25,23)+Vector3.up*4.4f,Rim(50,23)+Vector3.up*4.4f,.8f);Bunting(Rim(-25,23)+Vector3.up*4.4f,Rim(-50,23)+Vector3.up*4.4f,.8f);
            foreach(float phi in new[]{-36f,-14f,14f,36f}){var p=Rim(phi,29.5f);Put("bench",p,Yaw(c,p)+180);}
            foreach(float x in new[]{-9f,0f,9f}){var d=Quaternion.Euler(0,90,0)*outward;Put("flag_pole",Rim(0,32)+d*x,0,T(0xffffff,x==0?0x3a7bd5:0xffb400,x==0?0xff4d5e:0x2e9e5b,0));}
            // Balustrade on the valley side (colliders keep cars on the terrace), with flower beds.
            for(float phi=-58;phi<58;phi+=4) {
                Vector3 a=Rim(phi,33.5f),b=Rim(phi+4,33.5f),m=(a+b)*.5f;var rot=Quaternion.LookRotation(b-a);
                Box("Balustrade rail",m+Vector3.up*1.0f,new Vector3(.3f,.16f,Vector3.Distance(a,b)+.05f),rot,railWhite,true);
                Box("Balustrade base",m+Vector3.up*.18f,new Vector3(.45f,.36f,Vector3.Distance(a,b)+.05f),rot,plinth,true);
                for(int i=0;i<4;i++)Box("Baluster",Vector3.Lerp(a,b,(i+.5f)/4)+Vector3.up*.6f,new Vector3(.16f,.8f,.16f),rot,railWhite,false);
                if(((int)phi+58)%16==0)Put("flower_bed",Rim(phi,31.8f),Yaw(Rim(phi,31.8f),c),null,null,.55f,false);
            }
            foreach(float phi in new[]{120f,-120f,135f,-135f}){var p=Rim(phi,34);if(ValleyField.RoadDistance(p.x,p.z)>6)Ground("tree_round",p.x,p.z,phi,null,null,.8f);}
        }
        // Flag bunting: little triangles on a sagging line between two points.
        static Material buntingMaterial;
        void Bunting(Vector3 a,Vector3 b,float sag)=>BuntingLine(root,a,b,sag);
        // Shared with Bayview's high street.
        // Every bunting line's two ends (the placement audit checks each hangs from something).
        public static readonly List<(Vector3 a,Vector3 b)> BuntingEnds=new();
        public static GameObject BuntingLine(Transform parent,Vector3 a,Vector3 b,float sag)
        {
            BuntingEnds.Add((a,b));
            // LittleRoad/Bunting: vertex alpha is how free a vertex is, so pennant tips sway and flutter.
            if(buntingMaterial==null)buntingMaterial=new Material(Shader.Find("LittleRoad/Bunting")){name="Bunting"};
            var v=new List<Vector3>();var col=new List<Color>();var t=new List<int>();float length=Vector3.Distance(a,b);int flags=Mathf.Max(2,(int)(length/.62f));
            Color[] palette=System.Array.ConvertAll(new[]{Toy.Red,Toy.Sun,Toy.Sky,Toy.Mint,Toy.Pink,Toy.White},WorldLook.Surface);
            Vector3 P(float u)=>Vector3.Lerp(a,b,u)+Vector3.down*sag*4*u*(1-u);
            for(int i=0;i<flags;i++) {
                float u0=(i+.1f)/flags,u1=(i+.9f)/flags;Vector3 p0=P(u0),p1=P(u1),tip=(p0+p1)*.5f+Vector3.down*.36f;int s=v.Count;var c=palette[i%palette.Length];
                var tied=c;tied.a=0;var free=c;free.a=1;
                v.AddRange(new[]{p0,p1,tip});col.AddRange(new[]{tied,tied,free});t.AddRange(new[]{s,s+1,s+2,s,s+2,s+1});
            }
            var w=new Color(.95f,.95f,.9f,0);
            for(int i=0;i<24;i++){Vector3 p0=P(i/24f),p1=P((i+1)/24f);int s=v.Count;v.AddRange(new[]{p0,p1,p1+Vector3.down*.04f,p0+Vector3.down*.04f});col.AddRange(new[]{w,w,w,w});t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3,s,s+2,s+1,s,s+3,s+2});}
            var mesh=new Mesh{name="Bunting"};mesh.SetVertices(v);mesh.SetColors(col);mesh.SetTriangles(t,0);var up=new Vector3[v.Count];for(int i=0;i<up.Length;i++)up[i]=Vector3.up;mesh.normals=up;
            var go=new GameObject("Bunting",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=buntingMaterial;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
        }
        // ---------------------------------------------------------------- castle
        void Castle()
        {
            var c=HillcrestRoute.CastleCourt;const float R=38;const float gateTheta=172f;
            reserved.Add((new(c.x,c.z),52));
            Vector3 At(float theta,float r){float a=theta*Mathf.Deg2Rad;return new Vector3(c.x+Mathf.Sin(a)*r,HillcrestRoute.CastleY,c.z+Mathf.Cos(a)*r);}
            Vector3 Tangent(float theta){float a=theta*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a),0,-Mathf.Sin(a));}
            bool grand=BayviewKit.Has("castle_donjon");var red=T(0xffffff,0,0xd8343f,0);
            var gate=CastleGate();Put("castle_gatehouse",gate.at,gate.yaw,red,"HILLCREST CASTLE");
            var towers=new List<float>();for(int k=0;k<7;k++)towers.Add(gateTheta+26+k*(360-52)/6f);
            int[] pennants={0xff4d5e,0x3a7bd5,0xffb400,0x2e9e5b,0xff7ac8,0x1f8a70,0xff4d5e};
            for(int k=0;k<towers.Count;k++){var p=At(towers[k],R);p.y=Mathf.Min(p.y,H(p.x,p.z))-.3f;Put("castle_tower",p,towers[k],T(0xffffff,0,pennants[k],0));}
            // The north-west run (towers 2-3) holds the Zip Tower, flanked by short walls: ZipLine.Survey
            // found the clearest steep line from it (11 m under a hanging car, 46 m of drop).
            const int zipRun=2;
            for(int k=0;k<towers.Count-1;k++) {
                float mid=(towers[k]+towers[k+1])*.5f,half=(towers[k+1]-towers[k])*.5f*Mathf.Deg2Rad;float chord=2*R*Mathf.Sin(half)-7;var centre=At(mid,R*Mathf.Cos(half))+Vector3.down*.1f;
                CurtainRuns[k]=mid;
                if(grand&&k==zipRun) {
                    float piece=(chord-11)*.5f;
                    foreach(int sgn in new[]{-1,1}){var w=Put("castle_wall",centre+Tangent(mid)*sgn*(5.5f+piece*.5f),mid,red);w.transform.localScale=new Vector3(piece/20,1,1);}
                    ZipTopTheta=mid;ZipTop=Put("zip_tower",centre+Vector3.up*.1f,mid,red,"ZIPLINE");
                    continue;
                }
                var wall=Put("castle_wall",centre,mid,red);wall.transform.localScale=new Vector3(chord/20,1,1);
            }
            foreach(int sgn in new[]{-1,1}){float a=gateTheta+sgn*17.5f;var w=Put("castle_wall",At(a,R*Mathf.Cos(8.5f*Mathf.Deg2Rad))+Vector3.down*.1f,a,red);w.transform.localScale=new Vector3(.2f,1,1);}
            if(grand) {
                // The donjon (west) fronts the gate; the palazzo (east) turns its loggia to the valley.
                var donjon=At(265,13)+Vector3.down*.05f;Put("castle_donjon",donjon,Yaw(donjon,At(gateTheta,R)),red);
                Put("castle_palace",At(95,25.5f)+Vector3.down*.05f,95,T(0xe3a83b,0x2f6b3f,0xd8343f,0x3b5f8a));
                // Cobbled courtyard with a grip trigger, like the market square.
                var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disc.name="Castle courtyard paving";disc.transform.SetParent(root);Object.Destroy(disc.GetComponent<Collider>());
                disc.transform.position=c+Vector3.up*.03f;disc.transform.localScale=new Vector3(74,.02f,74);disc.GetComponent<Renderer>().sharedMaterial=plinth;
                var grip=new GameObject("Castle courtyard grip",typeof(BoxCollider),typeof(RoadSurface));grip.transform.SetParent(root);grip.transform.position=c;
                var box=grip.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(74,1.5f,74);
                Put("fountain",At(318,20)+Vector3.down*.02f,0);
                foreach(float a in new[]{140f,152f})Put("market_stall",At(a,21),a+180,T(0xffffff,0,a<150?0xffb400:0x3a7bd5,0));
                foreach(float a in new[]{228f,300f,30f})Put("flag_pole",At(a,27),0,T(0xffffff,0x3a7bd5,0xffb400,0));
                foreach(float a in new[]{215f,335f,62f})Put("olive_tree",At(a,24)+Vector3.down*.05f,a,null,null,1.1f);
                foreach(float a in new[]{120f,196f,250f,285f,345f,48f})Put("lamp_post",At(a,30),a+180);
            } else {
                Put("castle_keep",At(352,11)+Vector3.down*.05f,gateTheta,red);
                foreach(float a in new[]{120f,230f,300f,60f})Put("flag_pole",At(a,24),0,T(0xffffff,0x3a7bd5,0xffb400,0));
                foreach(float a in new[]{100f,250f})Put("olive_tree",At(a,20)+Vector3.down*.05f,a,null,null,1.1f);
            }
            // Duke Rolo waits beside the courtyard road, not on it (the drive benchmark's car stopped against him).
            Spots["castle"]=(At(gateTheta+20,24),gateTheta+90);
        }
        // Zip line: the Zip Tower in the castle's north wall and the lower-town station it runs to
        // (sited by ZipLine.Survey, review/map-sweep/zip-survey-3: clear footprint, 10 m under the car, 26 m off a road).
        // The landing plaza: paving that follows the ground from the road to round the station, a gateway
        // at the road, a gelato kiosk, flags and bunting, lamps, benches and flower beds.
        void ZipLanding(Vector3 at,float yaw)
        {
            var road=zipRoad;var lane=new Vector3(road.x-at.x,0,road.z-at.z);float len=lane.magnitude;var u=lane/Mathf.Max(len,.01f);var v=Vector3.Cross(Vector3.up,u);
            // Paving: a grid over [-16, the road's edge] along the lane and +-15 across, draped on the
            // terrain. Valley roads are up to 8.5 m wide: nothing of the landing stands within 7 m of the
            // road's centreline (the gateway's piers once blocked the lane).
            const float RoadHalf=4.3f,Clear=7;
            float u0=-16,u1=len-RoadHalf,w=15;int nu=Mathf.CeilToInt((u1-u0)/2),nv=15;var verts=new List<Vector3>();var tris=new List<int>();
            for(int i=0;i<=nu;i++)for(int j=0;j<=nv;j++){float s=Mathf.Lerp(u0,u1,i/(float)nu),t=Mathf.Lerp(-w,w,j/(float)nv);
                // Narrow to a 9 m lane between the plaza and the road: the gateway's piers (inner faces 5.2 m
                // out) stand on the verges either side, clear of the lane.
                float half=s>16?Mathf.Lerp(w,4.5f,Mathf.InverseLerp(16,24,s)):w;var p=at+u*s+v*(t*half/w);p.y=H(p.x,p.z)+.05f;verts.Add(p-at);}
            // Wound to face up (the first version faced down and was culled).
            for(int i=0;i<nu;i++)for(int j=0;j<nv;j++){int a=i*(nv+1)+j,b=a+nv+1;tris.AddRange(new[]{a,b,a+1,b,b+1,a+1});}
            var mesh=new Mesh{name="Zip landing paving"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var uvs=new List<Vector2>();foreach(var p in verts)uvs.Add(new Vector2(p.x,p.z)*.25f);mesh.SetUVs(0,uvs);
            var pave=new GameObject("Zip landing paving",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider),typeof(RoadSurface));pave.transform.SetParent(root);pave.transform.position=at;
            pave.GetComponent<MeshFilter>().sharedMesh=mesh;pave.GetComponent<MeshRenderer>().sharedMaterial=plinth;pave.GetComponent<MeshCollider>().sharedMesh=mesh;
            Vector3 P(float s,float t){var p=at+u*s+v*t;p.y=H(p.x,p.z)-.02f;return p;}
            float face=Quaternion.LookRotation(-u).eulerAngles.y;
            // Gateway over the lane, set back from the road, with a sign pointing up to the castle.
            Put("town_gate",P(Mathf.Max(14,len-11),0),Quaternion.LookRotation(u).eulerAngles.y,T(0xffffff,0,0xffb400,0),"ZIPLINE TO THE CASTLE");
            float laneEnd=len-10; // roadside decor stays 10 m back from the centreline (a flower bed here caught a car sliding wide)
            Put("kiosk",P(4,-11),Quaternion.LookRotation(v).eulerAngles.y,T(0xff7ac8,0,0x3a7bd5,0));
            Put("map_board",P(12,10),Quaternion.LookRotation(-v).eulerAngles.y);
            foreach(var (s,t) in new[]{(-13f,-13f),(-13f,13f),(14f,-13f),(14f,13f)})Put("flag_pole",P(s,t),0,T(0xffffff,0x3a7bd5,0xffb400,0));
            Bunting(P(-13,-13)+Vector3.up*7.5f,P(14,-13)+Vector3.up*7.5f,1.2f);Bunting(P(-13,13)+Vector3.up*7.5f,P(14,13)+Vector3.up*7.5f,1.2f);
            foreach(var s in new[]{-8f,6f,Mathf.Min(18,laneEnd)})foreach(int sd in new[]{-1,1})Put("lamp_post",P(s,sd*(s>16?7.8f:14.2f)),Quaternion.LookRotation(-v*sd).eulerAngles.y);
            foreach(var s in new[]{0f,8f})Put("bench",P(s,12.6f),Quaternion.LookRotation(-v).eulerAngles.y);
            // (Street review, 4 Oct: the beds by the gateway sat inside its piers' feet, and the picnic tables were jammed against
            // the kiosk.) Beds 3.5 m clear of the piers; the tables on the other side of the paving from the kiosk's counter.
            foreach(var (s,t) in new[]{(-9f,-10f),(-9f,10f),(9f,-13.5f),(Mathf.Min(20,laneEnd),-9.8f),(Mathf.Min(20,laneEnd),9.8f)})Put("flower_bed",P(s,t),face);
            foreach(var (s,t) in new[]{(-4f,-6.5f),(-6.5f,-3f)})Put("picnic_table",P(s,t),face+90);
        }
        public static float ZipTopTheta;public static GameObject ZipTop;public static readonly float[] CurtainRuns=new float[6];
        const float ZipBottomTheta=336,ZipBottomRadius=195;
        static Vector3 zipAt,zipRoad;
        // Keeps the lower town's houses off the landing: the station, its plaza and the lane in from the
        // nearest valley road (checked before any building goes down).
        void ReserveZipLanding()
        {
            var c=HillcrestRoute.HillCentre;float a=ZipBottomTheta*Mathf.Deg2Rad;
            zipAt=new Vector3(c.x+Mathf.Sin(a)*ZipBottomRadius,0,c.y+Mathf.Cos(a)*ZipBottomRadius);zipAt.y=H(zipAt.x,zipAt.z);
            float best=float.MaxValue;zipRoad=zipAt;
            foreach(var road in HillcrestRoute.All())foreach(var p in road.line){float d=(new Vector2(p.x-zipAt.x,p.z-zipAt.z)).sqrMagnitude;if(d<best){best=d;zipRoad=p;}}
            var from=new Vector2(zipAt.x,zipAt.z);var to=new Vector2(zipRoad.x,zipRoad.z);
            reserved.Add((from,26));for(float s=0;s<=1;s+=.1f)reserved.Add((Vector2.Lerp(from,to,s),16));
        }
        // Within the zipline landing (plaza round the station, lane to the road) plus a house's depth.
        static bool InZipLanding(float x,float z)
        {
            var p=new Vector2(x,z);var a=new Vector2(zipAt.x,zipAt.z);var b=new Vector2(zipRoad.x,zipRoad.z);
            if(Vector2.Distance(p,a)<30)return true;var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,1e-3f));
            return Vector2.Distance(p,a+ab*t)<18;
        }
        void ZipLineBuild()
        {
            if(ZipTop==null||!BayviewKit.Has("zip_station"))return;
            var at=zipAt+Vector3.down*.04f;
            // The station's +Z (its boom) points at the Zip Tower.
            float yaw=Yaw(at,ZipTop.transform.position);var station=Put("zip_station",at,yaw,T(0xe8c27a,0,0xffb400,0),"ZIPLINE");
            ZipLanding(at,yaw);
            var trolley=Put("zip_trolley",at+Vector3.up*20,0,null,null,1,false,BayviewKit.AnimatedRoot);
            var steel=TrialWorld.Material("Zipline steel",new Color(.22f,.24f,.26f),.8f,.55f);
            new ZipLine(ZipTop.transform,station.transform,trolley.transform,root,steel);
        }
        // ---------------------------------------------------------------- lower town
        void LowerTown()
        {
            var quay=HillcrestRoute.Quay;var main=HillcrestRoute.Mainline;
            float Run(float x){float best=0,bd=float.MaxValue;for(float s=0;s<quay.Length;s+=1){float d=Mathf.Abs(quay.At(s).x-x);if(d<bd){bd=d;best=s;}}return best;}
            // Landmarks on the river side (-1: left of travel eastward, the north side).
            Vector3 Front(float x,int side,string id,out float yaw){float s=Run(x);Vector3 at=quay.At(s);var p=at+quay.Right(s)*side*(quay.width*.5f+2.4f+Footprint(id).y*.5f);p.y=at.y+.02f;yaw=Yaw(p,at);return p;}
            // Pit Stop (all three bays) in place of the old Hillcrest Motors workshop. Gio keeps the
            // garage bay; the parts and body bays get their own hosts.
            if(BayviewKit.Has("pit_stop_3")) {
                var (ps,psy)=HillcrestRoute.PitStopSite;reserved.Add((new(ps.x,ps.z),24));
                PitStop.Build("hillcrest",true,new Vector3(ps.x,ps.y-.03f,ps.z),psy,root);
                // Paved forecourt from the bay doors to the kerb (visual; cars drive on the levelled pad).
                var fq=Quaternion.Euler(0,psy,0);Box("Pit Stop forecourt",ps+fq*new Vector3(0,-.01f,10.6f),new Vector3(38,.04f,9.6f),fq,paving,false);
                Spots["motors"]=PitStop.Hosts["hillcrest/garage"];Spots["hcparts"]=PitStop.Hosts["hillcrest/parts"];Spots["hcbody"]=PitStop.Hosts["hillcrest/body"];
            } else {
                var motors=Front(30,-1,"motors",out float my);Building("motors",motors,my,T(0xf2e2c0,0x1d6fa8,0xffd23f,0x1d6fa8),"HILLCREST MOTORS",true);
                Spots["motors"]=(motors+Quaternion.Euler(0,my,0)*new Vector3(-9.5f,0,6.5f),my+90);
            }
            var hotel=Front(200,-1,"hotel",out float hy);Building("hotel",hotel,hy,T(0xfff0c8,0xd9534f,0xff4d5e,0xd9534f),"GRAND HOTEL",true);
            var cafe=Front(160,-1,"cafe",out float cy);Building("cafe",cafe+Quaternion.Euler(0,cy,0)*Vector3.back*2.5f,cy,T(0xcfe0f5,0x3a7bd5,0x3a7bd5,0x3a7bd5),"RIVERSIDE",true);
            var mill=Front(318,-1,"mill",out float mly);Building("mill",mill,mly,T(0xfff0c8,0x9a5b3c,0xff4d5e,0x9a5b3c),null,true);
            var post=Front(-40,1,"post_office",out float py);Building("post_office",post,py,T(0xffe3a3,0xd9534f,0xff4d5e,0xd9534f),"POST",true);
            Spots["postvan"]=(post+Quaternion.Euler(0,py,0)*new Vector3(7.2f,0,5.6f),py+90);
            // The rest of both sides: rows of houses and shops.
            Row(quay,1,Run(-80),quay.Length-6);Row(quay,-1,Run(-80),quay.Length-6,12.2f,.2f);
            Pavements(quay,6,quay.Length-6);Lamps(quay,8,quay.Length-8,22);
            for(float x=-60;x<420;x+=64)StreetBunting(quay,Run(x));
            // Riverside promenade: benches and trees looking over the water.
            var river=HillcrestRoute.River;
            for(float x=-80;x<440;x+=18) {
                float s=river.Nearest(new Vector3(x,0,-870),out _);Vector3 bank=river.At(s)-river.Right(s)*15;
                if(ValleyField.RoadDistance(bank.x,bank.z)<7||Mathf.Abs(bank.x-104)<16)continue;
                int pick=Mathf.Abs((int)x/18)%3;
                if(pick==0)Ground("bench",bank.x,bank.z,Yaw(bank,river.At(s)),null,null,1,false);else Ground(pick==1?"tree_round":"olive_tree",bank.x,bank.z,x*7,null,null,.9f);
            }
            // Town gate across the mainline between the bridge and the quay.
            float g=main.Nearest(new Vector3(107,0,-884),out _);Put("town_gate",main.At(g)+Vector3.down*.02f,Yaw(main.At(g+2),main.At(g-2)),T(0xffffff,0,0,0),"HILLCREST");
            // A little jetty with boats below the hotel.
            float js=river.Nearest(new Vector3(215,0,-860),out _);var jetty=river.At(js)-river.Right(js)*8;jetty.y=river.At(js).y+.4f;
            Put("pier",jetty,Yaw(jetty,river.At(js)),null,null,.6f);
            for(int i=0;i<3;i++){var bp=river.At(js+8+i*9)-river.Right(js+8+i*9)*(i%2==0?2:-3);bp.y=river.At(js).y+.05f;var boat=Put("boat",bp,Yaw(river.At(js),river.At(js+10))+90*i,T(0xffffff,0,Awnings[i],0),null,.7f,false,BayviewKit.AnimatedRoot);boat.AddComponent<KitBob>().phase=i;}
        }
        void Station()
        {
            var loop=HillcrestRoute.StationLoop;var west=new Vector3(loop.x-HillcrestRoute.StationLoopRadius,HillcrestRoute.RailY,loop.y);
            var centre=new Vector3(west.x-12,HillcrestRoute.StationYard.y+.02f,west.z);
            Building("station",centre,270,T(0xfbe3a2,0x1f8a70,0xff4d5e,0x1f8a70),"HILLCREST",true);
            reserved.Add((new(centre.x,centre.z),40));
            Spots["station"]=(centre+new Vector3(-8,0,7),180);Spots["taxirank"]=(centre+new Vector3(-16,0,-8),0);
            foreach(float dz in new[]{-14f,14f})Ground("tree_cypress",centre.x-7,centre.z+dz,0);
            Ground("flag_pole",centre.x-9,centre.z-9,0,T(0xffffff,0x3a7bd5,0xff4d5e,0));
            // Forecourt: paved from the station road to the booking hall, bays for the waiting cars.
            var tarmac=TrialWorld.Material("Forecourt tarmac",new Color(.34f,.36f,.39f),0,.2f);var paint=TrialWorld.Material("Bay paint",new Color(.96f,.95f,.9f),0,.3f);
            Apron("Station forecourt",new Vector3(430,0,-1032),new Vector2(22,36),0,tarmac);
            for(int k=0;k<4;k++)Bay(new Vector3(418.25f+k*5.5f,0,-1022),Vector3.forward,5,paint);
        }
        // A paved surface draped on the ground: drivable, tarmac grip.
        void Apron(string name,Vector3 centre,Vector2 size,float yaw,Material material)
        {
            var rot=Quaternion.Euler(0,yaw,0);var v=new List<Vector3>();var t=new List<int>();int nx=Mathf.CeilToInt(size.x/2),nz=Mathf.CeilToInt(size.y/2);
            for(int i=0;i<=nz;i++)for(int j=0;j<=nx;j++) {
                var p=centre+rot*new Vector3(size.x*(j/(float)nx-.5f),0,size.y*(i/(float)nz-.5f));
                bool border=i==0||j==0||i==nz||j==nx;p.y=border?H(p.x,p.z)+.01f:Mathf.Max(H(p.x,p.z),H(p.x+1,p.z),H(p.x-1,p.z),H(p.x,p.z+1),H(p.x,p.z-1))+.05f;v.Add(p);
                if(i<nz&&j<nx){int q=i*(nx+1)+j;t.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<RoadSurface>();
        }
        // One parking-bay line on an apron.
        void Bay(Vector3 at,Vector3 along,float length,Material paint)
        {
            at.y=Mathf.Max(H(at.x,at.z),H(at.x,at.z+length*.5f),H(at.x,at.z-length*.5f),H(at.x+1,at.z),H(at.x-1,at.z))+.07f;
            Box("Bay line",at,new Vector3(.14f,.02f,length),Quaternion.LookRotation(along),paint,false);
        }
        void Streets()
        {
            var crest=HillcrestRoute.Crest;
            Row(crest,1,14,crest.Length-60);Row(crest,-1,14,crest.Length-60,12.2f,.22f);
            Pavements(crest,4,crest.Length-50);Lamps(crest,10,crest.Length-40,26);
            for(float s=60;s<crest.Length-60;s+=90)StreetBunting(crest,s);
            Parapets(crest,14,crest.Length-30);
        }
        // A low stone parapet along the downhill side of a hill street, where the ground beyond the pavement falls away
        // and nothing else stands (the edge audit found 128 open drops along Via Crest's outer side, 2 Oct). Gaps are
        // left for house yards, side roads, the market square and the castle approach's gate.
        // Garden gaps between houses (Row): on a hillside's downhill side a low wall closes each (docs/57, 2.4).
        readonly List<(HillcrestRoute.Road road,int side,float from,float to)> GardenGaps=new();
        // (Street review, 5 Oct: on the vineyard side the market square's paving ended at a 3 m grass drop with no wall.) A stone
        // parapet round the paving's rim wherever the ground beyond falls more than 1.2 m within 6 m, clear of roads and buildings.
        void SquareRim()
        {
            var stone=WorldLook.Kit("Hillcrest parapet",Toy.Hex(0xc2b08e),WorldLook.Pattern.Stone,0,.12f,true);var cap=TrialWorld.Material("Parapet cap",Toy.Hex(0xefe6d2),0,.25f);
            var outward=new Vector3(Square.x-HillcrestRoute.HillCentre.x,0,Square.z-HillcrestRoute.HillCentre.y).normalized;var c=Square+outward*3;float y=HillcrestRoute.SquareY;int pieces=0;
            Vector3 Rim(float a){var p=c+Quaternion.Euler(0,a,0)*Vector3.forward*32.6f;p.y=y;return p;}
            bool Open(float a)
            {
                var p=Rim(a);var r=(p-c).normalized;float lowest=float.MaxValue;for(float d=1;d<=6;d+=1){var b=p+r*d;lowest=Mathf.Min(lowest,H(b.x,b.z));}
                if(lowest>y-1.2f||ValleyField.RoadDistance(p.x,p.z)<6.5f)return false; // (a road leaving the square, and its pavement)
                return !BayviewKit.Occupied(new Bounds(p+Vector3.up*1.2f,new Vector3(1.5f,2,1.5f)));
            }
            const float step=4;
            for(float a=0;a<360;a+=step) {
                if(!Open(a)||!Open(a+step))continue;
                Vector3 p0=Rim(a),p1=Rim(a+step),mid=(p0+p1)*.5f;var f=p1-p0;f.y=0;var rot=Quaternion.LookRotation(f);float len=f.magnitude+.06f;
                float bottom=Mathf.Min(H(p0.x,p0.z),H(p1.x,p1.z),y)-.4f,top=y+.75f;
                Box("Square parapet",new Vector3(mid.x,(top+bottom)*.5f,mid.z),new Vector3(.34f,top-bottom,len),rot,stone,true);
                Coping(Box("Square parapet cap",new Vector3(mid.x,top+.06f,mid.z),new Vector3(.46f,.12f,len+.02f),rot,cap,false));pieces++;
            }
            Debug.Log("LITTLE_ROAD_SQUARE_RIM pieces="+pieces);
        }
        void Parapets(HillcrestRoute.Road road,float from,float to)
        {
            var stone=WorldLook.Kit("Hillcrest parapet",Toy.Hex(0xc2b08e),WorldLook.Pattern.Stone,0,.12f,true);
            var cap=TrialWorld.Material("Parapet cap",Toy.Hex(0xefe6d2),0,.25f);
            float off=road.width*.5f+2.45f;int pieces=0;const float step=2;
            bool Open(float s,int side,bool gap=false)
            {
                Vector3 at=road.At(s),r=road.Right(s)*side,wall=at+r*off;
                float lowest=float.MaxValue;for(float d=1;d<=8;d+=1){var b=at+r*(off+d);lowest=Mathf.Min(lowest,H(b.x,b.z));}
                if(lowest>at.y-1.2f)return false;
                foreach(var other in HillcrestRoute.All())if(other!=road&&HillcrestRoute.Distance(other.line,wall.x,wall.z,out _)<other.width*.5f+5)return false;
                // (Street review, 5 Oct: 46 m round the square left a 3 m drop beside its pavement open.) Only where the square's
                // paving lies beyond the wall (it must stay open to the road).
                if(OnMarketPaving(at+r*(off+1.5f)))return false;
                if(gap)return true; // a garden gap between houses is clear by construction
                foreach(var h in Homes)if(Vector2.Distance(new(h.yard.x,h.yard.z),new(wall.x,wall.z))<3.2f)return false; // a gate-wide gap at each front yard
                var probe=new Bounds(wall+Vector3.up*1.2f,new Vector3(1.5f,2,1.5f));if(BayviewKit.Occupied(probe))return false;
                return true;
            }
            // Open stretches, plus the garden gaps between houses on this street (their yards are clear by construction).
            var spans=new List<(int side,float a,float b,bool gap)>();
            foreach(var g in GardenGaps)if(g.road==road)spans.Add((g.side,g.from+.5f,g.to-.5f,true));
            spans.Add((-1,from,to,false));spans.Add((1,from,to,false));
            var done=new HashSet<(int,int)>();
            foreach(var (side,a0,b0,gap) in spans)for(float s=a0;s<b0;s+=step) {
                if(done.Contains((side,Mathf.RoundToInt(s/step))))continue;
                if(!Open(s,side,gap)||!Open(Mathf.Min(s+step,b0),side,gap))continue;
                done.Add((side,Mathf.RoundToInt(s/step)));
                float s2=Mathf.Min(s+step,b0);
                Vector3 a=road.At(s)+road.Right(s)*side*off,b=road.At(s2)+road.Right(s2)*side*off;
                float top=Mathf.Max(road.At(s).y,road.At(s2).y)+.75f,bottom=Mathf.Min(H(a.x,a.z),H(b.x,b.z),road.At(s).y)-.4f;
                var mid=(a+b)*.5f;var f=b-a;f.y=0;var rot=Quaternion.LookRotation(f);float len=f.magnitude+.06f;
                Box("Parapet",new Vector3(mid.x,(top+bottom)*.5f,mid.z),new Vector3(.34f,top-bottom,len),rot,stone,true);
                Coping(Box("Parapet cap",new Vector3(mid.x,top+.06f,mid.z),new Vector3(.46f,.12f,len+.02f),rot,cap,false));pieces++;
            }
            Debug.Log($"LITTLE_ROAD_PARAPETS {road.name} pieces={pieces}");
        }
        // ---------------------------------------------------------------- viewpoints
        void Vista()
        {
            var v=HillcrestRoute.Vista;var view=new Vector3(200,30,-1100);reserved.Add((new(v.x,v.z),24));
            var face=(view-v);face.y=0;face.Normalize();var side=Vector3.Cross(Vector3.up,face);
            for(float a=-70;a<70;a+=7) {
                Vector3 p0=v+Quaternion.Euler(0,a,0)*face*11.5f,p1=v+Quaternion.Euler(0,a+7,0)*face*11.5f;p0.y=p1.y=v.y;var m=(p0+p1)*.5f;var rot=Quaternion.LookRotation(p1-p0);
                Box("Viewpoint rail",m+Vector3.up*.95f,new Vector3(.18f,.14f,Vector3.Distance(p0,p1)+.05f),rot,railWhite,true);
                Box("Viewpoint post",p0+Vector3.up*.5f,new Vector3(.2f,1f,.2f),rot,railWhite,true);
            }
            Put("map_board",v-face*2.5f+side*3,Yaw(v,v-face),null,"HILLCREST VALLEY");
            Put("telescope",v+face*9.5f-side*2,Yaw(v,view));Put("telescope",v+face*9.5f+side*3,Yaw(v,view));
            Put("bench",v+face*8f+side*.5f,Yaw(v+face*8,v));Put("flag_pole",v-face*4-side*5,0,T(0xffffff,0x2e9e5b,0xffb400,0));
            Put("flower_bed",v-face*1-side*6,Yaw(v,view),null,null,.8f,false);
            Spots["vista"]=(v-side*4+face*2,Yaw(v,view)+90);
        }
        void Lookout()
        {
            var l=HillcrestRoute.Lookout;var falls=HillcrestRoute.FallsHead;reserved.Add((new(l.x,l.z),26));
            var face=falls-l;face.y=0;face.Normalize();var side=Vector3.Cross(Vector3.up,face);
            for(float a=-44;a<44;a+=8) {
                Vector3 p0=l+Quaternion.Euler(0,a,0)*face*9,p1=l+Quaternion.Euler(0,a+8,0)*face*9;p0.y=p1.y=l.y;var m=(p0+p1)*.5f;var rot=Quaternion.LookRotation(p1-p0);
                Box("Lookout rail",m+Vector3.up*.95f,new Vector3(.18f,.14f,Vector3.Distance(p0,p1)+.05f),rot,railWhite,true);Box("Lookout post",p0+Vector3.up*.5f,new Vector3(.2f,1f,.2f),rot,railWhite,true);
            }
            Put("map_board",l-face*3-side*4,Yaw(l,l-face),null,"SILVER FALLS");Put("telescope",l+face*7.5f,Yaw(l,falls));
            Put("picnic_table",l-face*2+side*6,Yaw(l,falls));Put("picnic_table",l+face*3+side*7,Yaw(l,falls)+20);
            // Clear of both tables (it used to sit 2 m from one, so the resident's car parked through it).
            Spots["lookout"]=(l+side*3+face*1,Yaw(l,falls)+90);
        }
        void Raceway()
        {
            var circuit=HillcrestRoute.Circuit;var pad=HillcrestRoute.Paddock;reserved.Add((new(pad.x,pad.z),60));
            int i0=HillcrestRoute.Index(circuit,new Vector3(-420,0,-1083));Vector3 a=circuit[i0],f=circuit[(i0+4)%circuit.Length]-circuit[(i0+circuit.Length-4)%circuit.Length];f.y=0;f.Normalize();
            var right=Vector3.Cross(Vector3.up,f);var north=right.z>0?right:-right;
            // Grandstand on the north side facing the straight; pit garages further along.
            var stand=a+north*21;stand.y=H(stand.x,stand.z)-.05f;Put("grandstand",stand,Yaw(stand,a),T(0xffffff,0x1d6fa8,0xffd23f,0x1d6fa8),"HILLCREST RACEWAY");
            // Pit row: four team garages, hospitality deck, race control tower and timing pylon.
            if(BayviewKit.Has("pit_garages")){var p=a+f*-76+north*23;p.y=H(p.x,p.z)-.05f;Put("pit_garages",p,Yaw(p,p-north),T(0xffffff,0x1d6fa8,0xffd23f,0x1d6fa8),"bay1=ROSSO|bay2=AZZURRO|bay3=VERDE|bay4=GIALLO|control=RACE CONTROL|paddockback=TEAM PADDOCK");}
            else for(int k=0;k<3;k++){var p=a+f*(-60-k*16)+north*22;p.y=H(p.x,p.z)-.05f;Put("motors",p,Yaw(p,p-north),T(new[]{0xff4d5e,0x3a7bd5,0x2e9e5b}[k],0xffffff,0xffd23f,0x1d6fa8),"PIT "+(k+1));}
            foreach(float d in new[]{-30f,-10f,10f,30f,50f}){var p=a+f*d+north*14.5f;p.y=H(p.x,p.z);Put("flag_pole",p,0,T(0xffffff,new[]{0xff4d5e,0x3a7bd5,0xffb400}[(int)(d+30)/20%3],0xffffff,0));}
            Spots["raceway"]=(pad+new Vector3(4,0,-6),90);
            // Paddock: tarmac under the pit garages and the drivers' car park, bays for the cars.
            var paddockTarmac=TrialWorld.Material("Paddock tarmac",new Color(.33f,.35f,.38f),0,.2f);var bayPaint=TrialWorld.Material("Bay paint",new Color(.96f,.95f,.9f),0,.3f);
            Apron("Raceway paddock",new Vector3(-352,0,-1050),new Vector2(100,38),0,paddockTarmac);
            for(int k=0;k<=8;k++)Bay(new Vector3(pad.x-37.25f+k*6.5f,0,pad.z+10),Vector3.forward,5,bayPaint);
            // Tyre walls on the outside of the corners, sponsor boards along the straights.
            if(BayviewKit.Has("tyre_wall")) {
                int n=circuit.Length;float since=99;string[] brands={"HILLCREST MOTORS","BELLA VISTA","GRAND HOTEL","GELATO","VALLEY V6","CAFFE LUNA","SILVER FALLS","HILLCREST DELI","TWIN TURBO","BAKERY"};int brand=0,board=0;
                for(int i=0;i<n;i+=3) {
                    Vector3 p=circuit[i],back=circuit[(i+n-12)%n],ahead=circuit[(i+12)%n];Vector3 tan=ahead-back;tan.y=0;tan.Normalize();var r=Vector3.Cross(Vector3.up,tan);
                    float turn=Vector3.SignedAngle(new Vector3(p.x-back.x,0,p.z-back.z),new Vector3(ahead.x-p.x,0,ahead.z-p.z),Vector3.up);since+=4.5f;
                    if(Mathf.Abs(turn)>11) {
                        int outside=turn>0?-1:1;var q=p+r*outside*15.2f;if(ValleyField.RoadDistance(q.x,q.z)<14.5f)continue;
                        q.y=H(q.x,q.z)-.02f;Put("tyre_wall",q,Yaw(q,q-r*outside),null,null,1);
                    } else if(since>55) {
                        since=0;int side=board++%2==0?1:-1;var q=p+r*side*16f;if(ValleyField.RoadDistance(q.x,q.z)<15f||Vector2.Distance(new(q.x,q.z),new(pad.x,pad.z))<40)continue;
                        q.y=H(q.x,q.z)-.02f;Put("ad_board",q,Yaw(q,q-r*side),T(new[]{0xffffff,0xffd23f,0x5ec8ff,0xff7ac8,0x3ee6a4}[brand%5],new[]{0xff4d5e,0x3a7bd5,0x1d2b64,0x2e9e5b}[brand%4],0,0),brands[brand%brands.Length]);brand++;
                    }
                }
            }
            var access=HillcrestRoute.RacewayAccess;float s=access.Length-70;Put("town_gate",access.At(s)+Vector3.down*.02f,Yaw(access.At(s+2),access.At(s-2)),T(0xffffff,0,0,0),"RACEWAY");
        }
        // ---------------------------------------------------------------- countryside
        void Countryside()
        {
            // Vineyards on the town hill's southern skirt and around Vine Hill.
            for(float x=110;x<320;x+=13)for(float z=-1360;z>-1440;z-=3.4f)Vine(x,z);reserved.Add((new(215,-1400),70));
            var vine=new Vector2(-565,-1330);for(float x=vine.x-70;x<vine.x+70;x+=13)for(float z=vine.y-60;z<vine.y+60;z+=3.4f)if(Vector2.Distance(new(x,z),vine)<66)Vine(x,z);reserved.Add((vine,72));
            // All the valley's farming is one estate now (lavender, sunflowers, hay, wheat).
            Estate();
            // An olive grove west of the lower town.
            for(float x=-260;x<-120;x+=9)for(float z=-980;z>-1050;z-=9)if(FieldClear(x,z,5))Ground("olive_tree",x+(float)rng.NextDouble()*2,z,(float)rng.NextDouble()*360,null,null,.9f);
            // Wind turbines along the southern ridge crest (rotors face the valley).
            for(float x=-760;x<=1060;x+=150) {
                float bestZ=-1700,best=-99;for(float z=-1580;z>-1960;z-=10){float h=H(x,z);if(h>best){best=h;bestZ=z;}}
                Put("wind_turbine",new Vector3(x,best-.5f,bestZ),0,null,null,1,true,BayviewKit.AnimatedRoot);
            }
        }
        // ---------------------------------------------------------------- Golden Acre Farm
        // One working farm on levelled ground (see HillcrestRoute.Farm): a farmyard with the
        // house, a big barn, silo, tractor and bales; a fenced 130 x 86 m wheat field you can
        // drive through and flatten; sunflower and lavender plots; a ploughed field and a hay
        // meadow. A gate over the lane from the station names it.
        public static readonly Vector2 WheatLocal=new(20,-8),WheatSize=new(130,86);
        void Estate()
        {
            float yaw=HillcrestRoute.FarmYaw;var rot=Quaternion.Euler(0,yaw,0);
            reserved.Add((HillcrestRoute.FarmCentre,HillcrestRoute.FarmOuter-15));
            var holder=new GameObject("Golden Acre Farm").transform;holder.SetParent(root);
            Vector3 F(float x,float z){var p=HillcrestRoute.Farm(x,z);p.y=H(p.x,p.z);return p;}
            GameObject Kit(string id,float x,float z,float turn,BayviewKit.Tint tint=null,string sign=null,float scale=1,bool solid=true)=>Put(id,F(x,z)+Vector3.down*.04f,yaw+turn,tint,sign,scale,solid,holder);
            GameObject Slab(string name,float x,float z,float sx,float sy,float sz,Material m,float lift=0,bool solid=false){var b=Box(name,F(x,z)+Vector3.up*(sy*.5f+lift),new Vector3(sx,sy,sz),rot,m,solid);b.transform.SetParent(holder);return b;}
            // Fence runs along a local edge, leaving the given gaps (local coordinate ranges).
            void Fence(Vector2 a,Vector2 b,params Vector2[] gaps)
            {
                float length=Vector2.Distance(a,b);int n=Mathf.Max(1,Mathf.RoundToInt(length/6));var d=(b-a)/n;float turn=Mathf.Atan2(-d.y,d.x)*Mathf.Rad2Deg;
                for(int i=0;i<n;i++) {
                    var m=a+d*(i+.5f);bool open=false;foreach(var g in gaps)if(Mathf.Abs(d.x)>Mathf.Abs(d.y)?m.x>g.x&&m.x<g.y:m.y>g.x&&m.y<g.y)open=true;
                    if(!open)Kit("fence",m.x,m.y,turn);
                }
            }
            var earth=TrialWorld.Material("Farmyard earth",new Color(.62f,.52f,.38f),0,.08f);var soil=TrialWorld.Material("Ploughed soil",new Color(.46f,.33f,.22f),0,.05f);
            var soilDark=TrialWorld.Material("Plough ridge",new Color(.38f,.26f,.17f),0,.05f);var mown=TrialWorld.Material("Mown hay stripe",new Color(.70f,.78f,.40f),0,.05f);
            // Gate over the lane.
            var lane=HillcrestRoute.FarmLane;float g=lane.Nearest(F(-116,54),out _);
            Put("town_gate",lane.At(g)+Vector3.down*.02f,Yaw(lane.At(g+2),lane.At(g-2)),T(0xffffff,0,0,0),"GOLDEN ACRE FARM",1,true,holder);
            // Farmyard.
            Slab("Farmyard",-76,62,48,.05f,34,earth);
            Kit("house_a",-94,80,180,T(0xfff3dc,0x2e9e5b,0xd9534f,0x2e9e5b));
            Kit("barn",-62,80,180,T(0xd94040,0xffffff,0xffffff,0xd94040),null,1.6f);
            var silo=GameObject.CreatePrimitive(PrimitiveType.Cylinder);silo.name="Farm silo";silo.transform.SetParent(holder);silo.transform.position=F(-44,82)+Vector3.up*7;silo.transform.localScale=new Vector3(5.2f,7,5.2f);
            silo.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Silo steel",new Color(.84f,.86f,.88f),.3f,.45f);silo.tag="Respawn";
            var dome=GameObject.CreatePrimitive(PrimitiveType.Sphere);dome.name="Silo dome";dome.transform.SetParent(holder);dome.transform.position=F(-44,82)+Vector3.up*14;dome.transform.localScale=new Vector3(5.4f,3.2f,5.4f);
            dome.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Silo dome red",Toy.Hex(0xd94040),.1f,.4f);Object.Destroy(dome.GetComponent<Collider>());
            Kit("tractor",-70,52,55);Kit("tractor",-86,48,-30,null,null,.95f);
            foreach(var (x,z,y) in new[]{(-50f,64f,.65f),(-48.7f,64f,.65f),(-51.3f,64f,.65f),(-49.35f,64f,1.9f),(-50.65f,64f,1.9f),(-50f,64f,3.1f)}) {var b=Kit("hay_bale",x,z,0);b.transform.position+=Vector3.up*(y-.65f);}
            Fence(new(-100,46),new(-52,46),new Vector2(-94,-80));Fence(new(-52,46),new(-52,78));
            // Farmer Giles waits by the yard: a resident now, with the farm's challenges (FarmChallenges, doc 64).
            Spots["farm"]=(F(-60,58),yaw+200);
            // The wheat: fenced, with gaps on the yard side and the lane side to drive in.
            var wc=HillcrestRoute.Farm(WheatLocal.x,WheatLocal.y);float x0=WheatLocal.x-WheatSize.x*.5f,x1=WheatLocal.x+WheatSize.x*.5f,z0=WheatLocal.y-WheatSize.y*.5f,z1=WheatLocal.y+WheatSize.y*.5f;
            WheatField.Create("Golden Acre wheat",holder,wc,WheatSize,yaw,H,331);
            Fence(new(x0-1.5f,z0-1.5f),new(x1+1.5f,z0-1.5f),new Vector2(10,30));Fence(new(x0-1.5f,z1+1.5f),new(x1+1.5f,z1+1.5f),new Vector2(-30,-10),new Vector2(50,70));
            Fence(new(x0-1.5f,z0-1.5f),new(x0-1.5f,z1+1.5f),new Vector2(-18,2));Fence(new(x1+1.5f,z0-1.5f),new(x1+1.5f,z1+1.5f),new Vector2(-18,2));
            // Scarecrow in the middle (not solid: drive past it, or through it).
            var sc=F(WheatLocal.x+6,WheatLocal.y+4);var wood=WorldLook.Kit("Scarecrow wood",new Color(.45f,.32f,.2f),WorldLook.Pattern.Wood,0,.1f,true);
            Box("Scarecrow post",sc+Vector3.up*1.6f,new Vector3(.14f,3.2f,.14f),rot,wood,false).transform.SetParent(holder);
            Box("Scarecrow arms",sc+Vector3.up*2.35f,new Vector3(2.2f,.12f,.12f),rot,wood,false).transform.SetParent(holder);
            Box("Scarecrow shirt",sc+Vector3.up*2.1f,new Vector3(.9f,.9f,.35f),rot,TrialWorld.Material("Scarecrow shirt",Toy.Hex(0x3a7bd5),0,.1f),false).transform.SetParent(holder);
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Scarecrow head";head.transform.SetParent(holder);head.transform.position=sc+Vector3.up*2.95f;head.transform.localScale=Vector3.one*.55f;
            head.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Scarecrow sack",new Color(.9f,.8f,.55f),0,.1f);Object.Destroy(head.GetComponent<Collider>());
            Box("Scarecrow hat",sc+Vector3.up*3.27f,new Vector3(.8f,.1f,.8f),rot,TrialWorld.Material("Scarecrow hat",Toy.Hex(0xd94040),0,.1f),false).transform.SetParent(holder);
            Box("Scarecrow crown",sc+Vector3.up*3.42f,new Vector3(.42f,.3f,.42f),rot,TrialWorld.Material("Scarecrow hat",Toy.Hex(0xd94040),0,.1f),false).transform.SetParent(holder);
            // Sunflowers and lavender beside the wheat, rows along the farm's axis.
            void Plot(string id,float cx,float cz,int rows)
            {
                for(int r=0;r<rows;r++)for(int c=-1;c<=1;c++)Kit(id,cx+c*12.2f,cz-rows*1.2f+r*2.4f,0,null,null,1,false);
                Fence(new(cx-19,cz-rows*1.2f-2),new(cx+19,cz-rows*1.2f-2),new Vector2(cx-3,cx+3));Fence(new(cx-19,cz+rows*1.2f+1),new(cx+19,cz+rows*1.2f+1));
            }
            if(BayviewKit.Has("sunflower_row"))Plot("sunflower_row",-98,10,14);
            if(BayviewKit.Has("lavender_row"))Plot("lavender_row",-98,-36,14);
            // Ploughed field south of the wheat: dark ridges on turned soil.
            Slab("Ploughed field",WheatLocal.x,-72,130,.04f,24,soil);
            for(float z=-83;z<=-61;z+=1.6f)Slab("Plough ridge",WheatLocal.x,z,129,.2f,.7f,soilDark,.02f);
            Fence(new(x0-1.5f,-85.5f),new(x1+1.5f,-85.5f));
            // Hay meadow north of the wheat: mown stripes and rows of round bales.
            for(float z=40;z<=76;z+=6)Slab("Mown stripe",WheatLocal.x+8,z,112,.02f,3,mown,.02f);
            // (The meadow's round bales made way for the orchard: Apple Shake, doc 64.) A few bales at its west end.
            for(int c=0;c<4;c++)Kit("hay_bale",-40+(c%2)*2.6f,74-c*1.6f,(c*37)%90);
            int apples=Orchard.Build(holder,(x,z)=>F(x,z),yaw);Debug.Log("LITTLE_ROAD_ORCHARD trees="+apples);
            Debug.Log("LITTLE_ROAD_FARM estate=Golden Acre wheat="+WheatSize.x+"x"+WheatSize.y);
        }
        // Olive trees, cypresses and the odd cottage on the slopes between the spiral's loops.
        // The town hill dressed as a Ligurian hillside. Around the hill, 15° sectors each take one use:
        // terraced vines or lavender along the contours (every other terrace held by a low stone wall on its
        // downhill side), an olive grove, or meadow with cypresses. Everything keeps clear of roads, the
        // square and every building already placed (BayviewKit footprints). The old version checked
        // Reserved(), which reserves the whole hill, so nothing was ever planted and the slopes were bare.
        void HillSlopes()
        {
            var c=HillcrestRoute.HillCentre;int pieces=0;int existing=BayviewKit.Footprints.Count; // buildings, not our own rows
            bool Clear(Vector3 p,Vector3 size,float yaw)
            {
                if(ValleyField.RoadDistance(p.x,p.z)<9||Vector2.Distance(new(p.x,p.z),new(Square.x,Square.z))<48||ReservedZone(p.x,p.z)||InZipLanding(p.x,p.z))return false;
                var b=new Bounds(p+Vector3.up*1.5f,Quaternion.Euler(0,yaw,0)*size);b.size=new Vector3(Mathf.Abs(b.size.x),size.y,Mathf.Abs(b.size.z));
                return !BayviewKit.Occupied(b,existing)&&ValleyField.Normal(p.x,p.z).y>.78f;
            }
            Vector3 At(float r,float a){float rad=a*Mathf.Deg2Rad;var p=new Vector3(c.x+Mathf.Sin(rad)*r,0,c.y+Mathf.Cos(rad)*r);p.y=H(p.x,p.z);return p;}
            for(int sector=0;sector<24;sector++) {
                double roll=new System.Random(sector*7919+13).NextDouble();
                string use=roll<.38?"vines":roll<.52?"lavender":roll<.78?"olives":"meadow";
                float a0=sector*15f,a1=a0+15;int terrace=0;
                for(float r=56;r<192;r+=use=="olives"?7:use=="meadow"?9:3f,terrace++) {
                    float step=(use=="olives"?7:use=="meadow"?11:12.6f)/(r*Mathf.Deg2Rad);
                    for(float a=a0+step*.5f;a<a1;a+=step) {
                        var p=At(r,a+(use=="olives"?(float)(rng.NextDouble()-.5)*step*.4f:0));
                        if(use=="vines"||use=="lavender") {
                            if(!Clear(p,new Vector3(12.8f,3,2.4f),a))continue;
                            // (At the lowest ground along the row, as the walls below: bug inbox #6/#10.)
                            {var axis=Quaternion.Euler(0,a,0)*Vector3.right;float low=p.y;for(int k=-2;k<=2;k++){var q=p+axis*(k*3.2f);low=Mathf.Min(low,H(q.x,q.z));}
                            if(p.y-low>.7f)continue;p.y=low;}
                            Put(use=="vines"?"vine_row":"lavender_row",p+Vector3.down*.04f,a,null,null,1,false);pieces++;
                            // Dry-stone terrace wall on the downhill side of every other terrace.
                            // It stands on the ground along its whole length: its foot under the lowest point, its top
                            // 0.7 m over the highest (one height from its middle left the downhill end in the air, bug inbox #10).
                            if(terrace%3==0) {
                                var w=At(r+1.5f,a);var along=Quaternion.Euler(0,a,0)*Vector3.right;float low=float.MaxValue,high=float.MinValue;
                                for(int k=-2;k<=2;k++){var q=w+along*(k*3.2f);float g=H(q.x,q.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);}
                                float foot=low-.4f,top=high+.7f;
                                if(top-foot<2.4f)Box("Terrace wall",new Vector3(w.x,(foot+top)*.5f,w.z),new Vector3(12.8f,top-foot,.6f),Quaternion.Euler(0,a,0),plinth,false);
                            }
                        } else if(use=="olives") {
                            if(!Clear(p,new Vector3(5,5,5),0))continue;
                            Put("olive_tree",p+Vector3.down*.05f,(float)rng.NextDouble()*360,null,null,.85f+(float)rng.NextDouble()*.35f);pieces++;
                        } else if(rng.NextDouble()<.5&&Clear(p,new Vector3(3,6,3),0)){Put(rng.NextDouble()<.6?"tree_cypress":"olive_tree",p+Vector3.down*.05f,0,null,null,.8f+(float)rng.NextDouble()*.4f);pieces++;}
                    }
                }
            }
            Debug.Log($"LITTLE_ROAD_HILL_SLOPES pieces={pieces}");
        }
        // The arrival from the Headland Tunnel frames the first view of the valley: tall stone pines and
        // cypresses close to the verges just past the portal (a dark near frame), opening out further on.
        void TunnelFrame()
        {
            var main=HillcrestRoute.Mainline;float from=HillcrestRoute.PortalSouthRun;int planted=0;
            foreach(var (along,side,id,scale) in new[]{(22f,-1,"tree_pine",1.5f),(26f,1,"tree_pine",1.6f),(38f,1,"tree_cypress",1.35f),
                (44f,-1,"tree_cypress",1.3f),(52f,1,"tree_pine",1.45f),(62f,-1,"tree_pine",1.4f),(70f,1,"tree_cypress",1.25f)}) {
                float d=from+along;var at=main.At(d)+main.Right(d)*side*(main.width*.5f+5.5f+(along>40?3:0)+(side<0&&along>30?9:0)); // left cypresses stand wide of the castle view
                if(ValleyField.RoadDistance(at.x,at.z)<main.width*.5f+4)continue;
                KitLod.Attach(Ground(id,at.x,at.z,along*37,null,null,scale),id);planted++;
            }
            Debug.Log($"LITTLE_ROAD_TUNNEL_FRAME trees={planted}");
        }
        // Reserved plots other than the whole-hill rule in Reserved().
        static bool ReservedZone(float x,float z){foreach(var r in reserved)if(Vector2.Distance(new(x,z),r.centre)<r.radius)return true;return false;}
        // Draped crop fields across the valley floor: striped rows in two tones per field.
        // A Prop Forge hedgerow along a field edge (docs/78): a deforming run, driven through (no collider), as the bushes were.
        int hedges;
        void HedgeRun(List<Vector3> line)
        {
            var L=Runs.Solve(new RunSpec{name=$"hedge {hedges}",kit="hedge",seed=$"hedge-{hedges}",along=line.ToArray(),solid=false});hedges++;
            if(RaceTrial.Has("--gate-log")&&hedges%15==1)Debug.Log($"LITTLE_ROAD_HEDGE {line[0].x:0.0},{line[0].y:0.0},{line[0].z:0.0} to {line[^1].x:0.0},{line[^1].z:0.0}");
            Runs.Draw(L,root);
        }
        void Patchwork()
        {
            var palettes=new[]{(new Color(.95f,.80f,.36f),new Color(.86f,.68f,.28f)),(new Color(.66f,.46f,.30f),new Color(.55f,.37f,.24f)),(new Color(.52f,.74f,.30f),new Color(.44f,.64f,.26f)),
                (new Color(.70f,.58f,.90f),new Color(.47f,.62f,.36f)),(new Color(.99f,.86f,.22f),new Color(.42f,.62f,.26f)),(new Color(.80f,.86f,.42f),new Color(.66f,.76f,.34f))};
            var v=new List<Vector3>();var col=new List<Color>();var t=new List<int>();int fields=0;
            void Flush()
            {
                if(v.Count==0)return;var mesh=new Mesh{name="Valley fields"};mesh.SetVertices(v);mesh.SetColors(col);mesh.SetTriangles(t,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("Valley fields",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.GetComponent<MeshRenderer>();if(fieldMaterial==null){fieldMaterial=new Material(Shader.Find("LittleRoad/Distant")){name="Valley fields"};fieldMaterial.SetFloat("_Haze",.00008f);fieldMaterial.SetColor("_HazeColor",new Color(.66f,.80f,.90f));}
                r.sharedMaterial=fieldMaterial;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;v.Clear();col.Clear();t.Clear();
            }
            // Fields share the farm's grid and axis (as real field systems do), in two sizes.
            for(float gx=-700;gx<1000;gx+=74)for(float gz=-690;gz>-1520;gz-=62) {
                float x=gx,z=gz;bool big=rng.NextDouble()<.5;float w=big?60:42,d=big?44:34;
                if(Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)<215||!FieldClear(x,z,Mathf.Max(w,d)*.5f)||!FootprintClear(x,z,w,d))continue;
                if(rng.NextDouble()<.25)continue;
                reserved.Add((new(x,z),Mathf.Max(w,d)*.55f));fields++;
                // Hedgerows along one or two of the field's edges, with gaps and the odd tree.
                if(BayviewKit.Has("bush")&&rng.NextDouble()<.65) {
                    var hedgeRot=Quaternion.Euler(0,HillcrestRoute.FarmYaw+(rng.NextDouble()<.5?0:90),0);
                    foreach(int edge in rng.NextDouble()<.5?new[]{0}:new[]{0,1}) {
                        float len=edge==0?w:d,off=(edge==0?d:w)*.5f+1.4f;
                        var hedge=new List<Vector3>();   // (--prop-forge: the edge's stretches laid as Prop Forge hedgerows; the same random draws)
                        void LayHedge(){if(hedge.Count>=3)HedgeRun(hedge);hedge.Clear();}
                        for(float e=-len*.5f;e<len*.5f;e+=2.6f) {
                            if(rng.NextDouble()<.12){LayHedge();continue;}var local=edge==0?new Vector3(e,0,off):new Vector3(off,0,e);var q=new Vector3(x,0,z)+hedgeRot*local;
                            if(ValleyField.RoadDistance(q.x,q.z)<8){LayHedge();continue;}
                            bool tree=rng.NextDouble()<.07;float yaw=(float)rng.NextDouble()*360,sc=tree?.9f+(float)rng.NextDouble()*.3f:.9f+(float)rng.NextDouble()*.4f;
                            if(PropForge.Enabled){q.y=H(q.x,q.z);hedge.Add(q);if(!tree)continue;}
                            var g=Ground(tree?"tree_round":"bush",q.x,q.z,yaw,null,null,sc,tree);
                            KitLod.Attach(g,tree?"tree_round":"bush");
                        }
                        LayHedge();
                    }
                }
                // Crop rows as real ridges (lit, so they shade) over a ground sheet in the furrow colour.
                int kind=rng.Next(palettes.Length);var (a,b)=palettes[kind];var rot=Quaternion.Euler(0,HillcrestRoute.FarmYaw+(rng.NextDouble()<.5?0:90),0);
                int rows=Mathf.Max(4,(int)(d/2.6f)),cells=Mathf.CeilToInt(w/2.2f);float spacing=d/rows;
                float ridge=kind switch{0=>.55f,1=>.16f,2=>.32f,3=>.45f,4=>.7f,_=>.22f};
                float Top(Vector3 p)=>Mathf.Max(H(p.x,p.z),H(p.x+1.2f,p.z),H(p.x-1.2f,p.z),H(p.x,p.z+1.2f),H(p.x,p.z-1.2f))+.08f;
                Color Tone(Color c,Vector3 p)=>WorldLook.Surface(c*(.9f+.2f*Mathf.PerlinNoise(p.x*.13f,p.z*.13f)));
                var furrow=b*(kind==1?.9f:.78f);int sheet=v.Count;
                for(int iz=0;iz<=rows;iz++)for(int cell=0;cell<=cells;cell++) {
                    var p=new Vector3(x,0,z)+rot*new Vector3(-w/2+cell*w/cells,0,-d/2+iz*spacing);p.y=Top(p);v.Add(p);col.Add(Tone(furrow,p));
                    if(iz<rows&&cell<cells){int q=sheet+iz*(cells+1)+cell;t.AddRange(new[]{q,q+cells+1,q+1,q+1,q+cells+1,q+cells+2});}
                }
                for(int row=0;row<rows;row++) {
                    float lz=-d/2+(row+.5f)*spacing,half=spacing*.31f;int start=v.Count;
                    for(int cell=0;cell<=cells;cell++) {
                        var p=new Vector3(x,0,z)+rot*new Vector3(-w/2+cell*w/cells,0,lz);float y=Top(p);var across=rot*Vector3.forward;
                        var c=Tone(row%2==0||kind==1?a:Color.Lerp(a,b,.35f),p);
                        v.Add(new Vector3(p.x,y,p.z)-across*half);v.Add(new Vector3(p.x,y+ridge,p.z));v.Add(new Vector3(p.x,y,p.z)+across*half);col.Add(c*.92f);col.Add(c);col.Add(c*.92f);
                    }
                    for(int cell=0;cell<cells;cell++){int q=start+cell*3;t.AddRange(new[]{q,q+1,q+3,q+3,q+1,q+4,q+1,q+2,q+4,q+4,q+2,q+5});}
                }
                if(v.Count>50000)Flush();
            }
            Flush();Debug.Log("LITTLE_ROAD_HILLCREST_FIELDS fields="+fields);
        }
        static Material fieldMaterial;
        // A field's whole rectangle (either way round, as it isn't turned yet) stays off the roads and their verges. FieldClear
        // alone samples the centre and four points on a circle, so a road across a corner or an edge between them got the
        // striped field draped over it (bug inbox #3, the valley S-bend at (0,-736)).
        public static bool FootprintClear(float x,float z,float w,float d)
        {
            foreach(float yaw in new[]{HillcrestRoute.FarmYaw,HillcrestRoute.FarmYaw+90}) {
                var rot=Quaternion.Euler(0,yaw,0);
                for(float u=-w*.5f-2;u<=w*.5f+2.01f;u+=Mathf.Max(1,(w+4)/Mathf.Ceil((w+4)/5)))
                    for(float v=-d*.5f-2;v<=d*.5f+2.01f;v+=Mathf.Max(1,(d+4)/Mathf.Ceil((d+4)/5))) {
                        var p=new Vector3(x,0,z)+rot*new Vector3(u,0,v);
                        if(ValleyField.RoadDistance(p.x,p.z)<RoadMargin)return false;
                    }
            }
            return true;
        }
        public const float RoadMargin=11;   // from a centreline: the raceway's 8 m half-width and run-off, or a lane road and its verge
        bool FieldClear(float x,float z,float r)
        {
            foreach(var d in new[]{Vector2.zero,new Vector2(r,0),new Vector2(-r,0),new Vector2(0,r),new Vector2(0,-r)}) {
                float px=x+d.x,pz=z+d.y;
                if(ValleyField.RoadDistance(px,pz)<14||HillcrestRoute.Distance(HillcrestRoute.Rail.line,px,pz,out _)<10||HillcrestRoute.Distance(HillcrestRoute.River.line,px,pz,out _)<22)return false;
                if(ValleyField.Normal(px,pz).y<.92f||H(px,pz)<HillcrestRoute.LakeY+1)return false;
            }
            foreach(var res in reserved)if(Vector2.Distance(new(x,z),res.centre)<res.radius+r)return false;
            return true;
        }
        void Vine(float x,float z)
        {
            if(ValleyField.RoadDistance(x,z)<14||ValleyField.RoadDistance(x+6,z)<14||ValleyField.RoadDistance(x-6,z)<14||ValleyField.Normal(x,z).y<.88f)return;
            Ground("vine_row",x,z,0,null,null,1,false);
        }
        void Rows(float x,float z,float yaw,string id)
        {
            var rot=Quaternion.Euler(0,yaw,0);
            for(int r=0;r<14;r++)for(int c=-1;c<=1;c++){var p=new Vector3(x,0,z)+rot*new Vector3(c*12.5f,0,-16+r*2.4f);Ground(id,p.x,p.z,yaw,null,null,1,false);}
        }
    }
}
