using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Bramblewick's village (docs/82, step 3), behind --bramblewick: the green with its pond on the north side of Valley Road
    // South, the parish church and its lychgate beyond the green, The Plough across the road, thatched cob cottages along
    // the street either side, the backyard tuner's barn behind the east end, and the dairy out along the road towards the
    // Vinelands gate with its cows in the field. Pieces from Car Forge's village, farm and cows kits.
    public sealed partial class BramblewickVillage
    {
        readonly Transform root;readonly System.Random rng=new(8240);
        readonly List<(Vector2 c,Vector2 half,float yaw)> footprints=new();
        Material plinth;
        public static int Buildings,Plinths,Refused;
        public static readonly Dictionary<string,(Vector3 at,float yaw)> Spots=new();
        // Where the village's cars stand (ValleyLife parks them): by the green, outside The Plough, lay-bys in the vale, the dairy yard.
        public static readonly List<(Vector3 at,float yaw)> Parking=new();
        // Photo sights (PhotoRequests) and the six lucky horseshoes, on the ground (docs/82 step 5): St Michael's porch, the
        // duck pond, the orchard, the dairy yard, a field gate out in the vale, the top of the pass.
        public static Vector3 Tower,PubFront,PondAt,DairyAt;
        public static readonly List<Vector3> Horseshoes=new();
        readonly Vector3[] shoe=new Vector3[6];
        public BramblewickVillage(Transform parent){root=new GameObject("Bramblewick").transform;root.SetParent(parent,false);}
        static float H(float x,float z)=>ValleyField.Height(x,z);
        static float Yaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return d.sqrMagnitude<1e-6f?0:Quaternion.LookRotation(d).eulerAngles.y;}
        static BayviewKit.Tint T(int wall,int shutter,int awning,int door)=>new(Toy.Hex(wall),Toy.Hex(shutter),Toy.Hex(awning),Toy.Hex(door));
        GameObject Put(string id,Vector3 p,float yaw,BayviewKit.Tint tint=null,string sign=null,bool solid=true,float scale=1)=>BayviewKit.Has(id)?BayviewKit.Spawn(id,p,yaw,root,tint,sign,solid,scale):null;
        // Stepping slabs from `a` to `b` (a garden path, a path across the green), each sat on the ground under it: its top 4 cm
        // over the high side, deep enough to reach the low side (as Quarry Hills' garden paths).
        Material slabStone,yard;
        void Slabs(Vector3 a,Vector3 b,float width,float pitch=.9f)
        {
            var d=b-a;d.y=0;float len=d.magnitude;if(len<.5f)return;var rot=Quaternion.LookRotation(d/len);slabStone??=TrialWorld.Material("Bramblewick path stone",new Color(.86f,.83f,.74f),0,.2f);
            for(float s=pitch*.5f;s<len;s+=pitch) {
                var p=a+d/len*s;float lo=float.MaxValue,hi=float.MinValue;
                foreach(var c in new[]{new Vector3(-width*.5f,0,-.3f),new Vector3(width*.5f,0,-.3f),new Vector3(-width*.5f,0,.3f),new Vector3(width*.5f,0,.3f)}){var q=p+rot*c;float g=H(q.x,q.z);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
                var g2=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(g2.GetComponent<Collider>());g2.name="Bramblewick path";g2.transform.SetParent(root);
                g2.transform.SetPositionAndRotation(new Vector3(p.x,hi+.04f-(.3f+hi-lo)*.5f,p.z),rot*Quaternion.Euler(0,((int)(s*7)%3-1)*3,0));g2.transform.localScale=new Vector3(width,.3f+hi-lo,pitch*.82f);
                var r=g2.GetComponent<Renderer>();r.sharedMaterial=slabStone;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        readonly List<Vector3> doors=new();const float setbackClear=3f;readonly System.Random dressRng=new(8241);   // (dressing has its own draws, so the village layout stays put)   // (front doors: the street lamps keep clear of them, review BW1-08)
        // Each building's ground rectangle (width along its front, depth), as the kit models them.
        static Vector2 Footprint(string id)=>id switch{"cottage_thatch_a"=>new(9.8f,7.0f),"cottage_thatch_b"=>new(7.6f,6.6f),"cottage_thatch_c"=>new(8.4f,8.8f),"parish_church"=>new(11f,28f),
            "pub"=>new(13.2f,7.8f),"milking_parlour"=>new(16.4f,10.4f),"dutch_barn"=>new(18.6f,9.6f),"barn"=>new(12f,10f),_=>new(6,6)};
        // Cob, limewash and ochre: each cottage its own; doors in the West Country's dark greens, blues and reds.
        static readonly int[] Walls={0xf3e6c8,0xe9b8a6,0xe2bf72,0xf6f1e6,0xd9c9a3,0xf0d8c0,0xe7d2a8};
        static readonly int[] Doors={0x2f5d46,0x2c4a7a,0x8c2f2a,0x3d6b6b,0x5b4a3a,0x1f3a5a};
        BayviewKit.Tint Cottage(){int door=Doors[rng.Next(Doors.Length)];return T(Walls[rng.Next(Walls.Length)],door,door,door);}

        // A building with its floor at the ground under its front's middle: a stone plinth fills any fall under it; refused on
        // another building, on a road, or where the hill would bury it or the plinth would stand over 3 m.
        bool Building(string id,Vector3 centre,float yaw,BayviewKit.Tint tint=null,string sign=null)
        {
            var size=Footprint(id);var rot=Quaternion.Euler(0,yaw,0);var c2=new Vector2(centre.x,centre.z);var half=size*.5f-new Vector2(.4f,.4f);
            foreach(var o in footprints)if(QuarryHillsTown.Overlap(c2,half,yaw,o.c,o.half,o.yaw)){Refused++;return false;}
            var front=centre+rot*new Vector3(0,0,size.y*.5f);float floor=H(front.x,front.z)+.05f,low=floor,high=floor;
            for(int i=0;i<5;i++)for(int j=0;j<5;j++) {
                var q=centre+rot*new Vector3((i/2f-1)*(size.x*.5f+.3f),0,(j/2f-1)*(size.y*.5f+.3f));float g=H(q.x,q.z);low=Mathf.Min(low,g);high=Mathf.Max(high,g);
                if((i%2==0&&j%2==0)&&ValleyField.RoadDistance(q.x,q.z)<4.6f){Refused++;return false;}
            }
            if(floor-low>3f||high-floor>2.5f){Refused++;return false;}
            if(floor-low>.25f) {
                var b=GameObject.CreatePrimitive(PrimitiveType.Cube);b.name="Stone plinth";b.transform.SetParent(root);b.transform.SetPositionAndRotation(new Vector3(centre.x,(low-.5f+floor)*.5f,centre.z),rot);
                b.transform.localScale=new Vector3(size.x+.4f,floor-low+.5f,size.y+.4f);b.GetComponent<Renderer>().sharedMaterial=plinth;b.tag="Respawn";Plinths++;
            }
            if(Put(id,new Vector3(centre.x,floor-.03f,centre.z),yaw,tint,sign)==null){Refused++;return false;}
            footprints.Add((c2,half,yaw));Buildings++;return true;
        }

        public void Build()
        {
            Buildings=Plinths=Refused=0;Spots.Clear();Parking.Clear();Horseshoes.Clear();
            plinth=WorldLook.Kit("Bramblewick plinth",new Color(.62f,.58f,.50f),WorldLook.Pattern.Stone,0,.1f,true);
            var road=HillcrestRoute.Find(Bramblewick.RoadName);float sC=road.Nearest(new Vector3(Bramblewick.Centre.x,0,Bramblewick.Centre.y),out _);
            Vector3 N(float s){var r=road.Right(s);r.y=0;r.Normalize();return r.z>0?r:-r;}   // (the north side: the green's)
            float half=road.width*.5f;var green=Bramblewick.Green;
            // ---- the green: a pond at its east end with reeds, benches round it, the village sign at the road
            var pond=green+Quaternion.Euler(0,Yaw(road.At(sC-1),road.At(sC+1)),0)*Vector3.forward*9;pond.y=green.y-.12f;PondAt=pond+Vector3.up;
            {var dir=road.At(sC+1)-road.At(sC-1);dir.y=0;var s0=pond+dir.normalized*10.5f;s0.y=H(s0.x,s0.z);shoe[1]=s0;}   // (on the grass past the pond's east end)
            {var w=GameObject.CreatePrimitive(PrimitiveType.Cylinder);w.name="Bramblewick pond";Object.Destroy(w.GetComponent<Collider>());w.transform.SetParent(root);
             w.transform.position=pond;w.transform.localScale=new Vector3(14,.03f,10);w.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Pond water",new Color(.32f,.48f,.46f),.1f,.92f);
             // (a muddy bank round it, just under the water's top: the pond had a hard edge on the grass, review BW1-04)
             var bank=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bank.name="Bramblewick pond bank";Object.Destroy(bank.GetComponent<Collider>());bank.transform.SetParent(root);
             bank.transform.position=pond+Vector3.down*.012f;bank.transform.localScale=new Vector3(16.6f,.03f,12.4f);bank.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Pond bank",new Color(.45f,.42f,.30f),0,.15f);
             for(int k=0;k<9;k++){float a=k*40+(float)rng.NextDouble()*20;var q=pond+Quaternion.Euler(0,a,0)*new Vector3(6.6f,0,0);q.z=pond.z+(q.z-pond.z)*.72f;q.y=H(q.x,q.z);Put("reeds",q,a,null,null,false);}}
            foreach(float a in new[]{30f,90f,150f}){var b=pond+Quaternion.Euler(0,Yaw(road.At(sC-1),road.At(sC+1))+a,0)*new Vector3(0,0,10.5f);b.y=H(b.x,b.z);Put("bench",b,Yaw(b,pond),null,null,false);}   // (round the pond's far side, facing it, clear of the path: review BW1-10)
            {var at=road.At(sC-60)+N(sC-60)*(half+2.2f);at.y=H(at.x,at.z);Put("map_board",at,Yaw(at,road.At(sC-60)),null,"BRAMBLEWICK");}
            foreach(float a in new[]{60f,140f,215f}){var t=green+Quaternion.Euler(0,a,0)*new Vector3(0,0,20+(float)rng.NextDouble()*4);t.y=H(t.x,t.z)-.05f;Put("tree_round",t,(float)rng.NextDouble()*360,null,null,true,1.3f+(float)rng.NextDouble()*.3f);}
            // ---- the church north of the green, its tower and lychgate towards it
            var nC=N(sC);var church=green+nC*44;church.y=H(church.x,church.z);
            if(Building("parish_church",church,Yaw(church,green))){Spots["church"]=(green+nC*13,Yaw(green,church));   // (on the green, clear of the lychgate: review BW1-05)
                var gate=green+nC*(44-Footprint("parish_church").y*.5f-6);gate.y=H(gate.x,gate.z);   // (in the churchyard wall's front)
                Tower=church-nC*(Footprint("parish_church").y*.5f-3)+Vector3.up*11;{var s0=gate+nC*3.5f;s0.y=H(s0.x,s0.z);shoe[0]=s0;}   // (the tower stands towards the green; a horseshoe inside the lychgate, at the porch path)
                Put("village_lychgate",gate,Yaw(gate,green)+90);
                Slabs(road.At(sC)+nC*(half+2.6f),gate-nC*1.6f,1.5f,.7f);   // (a flagstone path across the green from the road to the lychgate, review BW1-10)
                ChurchyardWall(church,Yaw(church,green),gate);}
            // ---- The Plough across the road, facing the green
            var nS=-nC;var pub=road.At(sC)+nS*(half+2.4f+3.6f+3.9f);pub.y=H(pub.x,pub.z);
            if(Building("pub",pub,Yaw(pub,road.At(sC)),T(0xf6f1e6,0x2f5d46,0x2f5d46,0x2f5d46),"THE PLOUGH")){PubFront=pub+Vector3.up*4;Spots["pub"]=(road.At(sC)+nS*(half+2.6f),Yaw(pub,road.At(sC))+180);}
            {var pb=road.At(sC+14)+nS*(half+1.4f);pb.y=H(pb.x,pb.z);Put("pillar_box",pb,Yaw(pb,road.At(sC+14)));
             var ph=road.At(sC-14)+nS*(half+2.2f);ph.y=H(ph.x,ph.z);Put("phone_box",ph,Yaw(ph,road.At(sC-14)));}
            // ---- the street: cottages either side, the green's and the pub's frontages left open
            // (a village, not a terrace: tight round the green, thinning out with distance, a field's gap now and then, its last
            //  cottages 230 m out; each set back and turned a little its own way, a hedge along its garden's front with a gap
            //  at the gate, an apple tree or two behind)
            foreach(int sideSign in new[]{1,-1}) {
                float s=Mathf.Max(20,sC-230);
                while(s<Mathf.Min(road.Length-20,sC+230)) {
                    bool north=sideSign>0;float far=Mathf.Abs(s-sC)/230f;
                    if(north&&Mathf.Abs(s-sC)<40){s=sC+40;continue;}
                    if(!north&&Mathf.Abs(s-sC)<11){s=sC+11;continue;}
                    if(rng.NextDouble()<.08+far*.35){s+=18+(float)rng.NextDouble()*30;continue;}   // (a field gate's gap, a paddock)
                    string id="cottage_thatch_"+"abc"[rng.Next(3)];var size=Footprint(id);float mid=s+size.x*.5f;
                    var at=road.At(mid);var n=sideSign>0?N(mid):-N(mid);float setback=half+2.2f+2.6f+(float)rng.NextDouble()*(2+far*6);
                    var c=at+n*(setback+size.y*.5f);c.y=H(c.x,c.z);float yaw=Yaw(c,at)+((float)rng.NextDouble()-.5f)*10;
                    if(Building(id,c,yaw,Cottage()))Garden(road,id,c,yaw,n,mid,size,setback);
                    s+=size.x+3f+(float)rng.NextDouble()*(4+far*14);
                }
            }
            // ---- lamps along the street, old cast iron, alternate sides every 38 m
            int k2=0;for(float s=sC-280;s<sC+290;s+=38,k2++){var n=k2%2==0?N(s):-N(s);var lp=road.At(s)+n*(half+1.3f);
                float ls=s;for(int t=0;t<4;t++){bool near=false;foreach(var d in doors)if(Mathf.Abs(road.Nearest(d,out _)-ls)<setbackClear&&Vector3.Dot(d-road.At(ls),n)>0)near=true;if(!near)break;ls=s+(t+1)*3.5f;lp=road.At(ls)+n*(half+1.3f);}   // (not in front of a door, review BW1-08)
               lp.y=H(lp.x,lp.z);Put("lamp_post",lp,0,T(0xffffff,0x1d2b3a,0xffffff,0xffffff),null,false);}
            // ---- the backyard tuner: a barn behind the street's east end, its doors to the lane
            {float s=sC+170;var n=N(s);var barn=road.At(s)+n*(half+2.2f+3.2f+7f+12f);barn.y=H(barn.x,barn.z);
             if(Building("barn",barn,Yaw(barn,road.At(s)),T(0x7a8a8c,0xc0392b,0xc0392b,0x2f5d46),"TUNER")) {
                 Spots["tuner"]=(road.At(s)+n*(half+2.2f+3.2f+7f+3f),Yaw(barn,road.At(s))+180);
                 // (a concrete yard between the lane and the barn doors, where the tuner works on cars: review BW1-08)
                 var yc=barn-n*(5f+4f);yc.y=0;var yrot=Quaternion.Euler(0,Yaw(barn,road.At(s)),0);   // (an apron 8 m deep in front of the doors)
                 // (it falls with the ground from the barn's doors to the lane, pitched, so it has no step at its front: on a 0.75 m fall
                 //  a level slab stood up as a platform)
                 float Side(float z){float m=float.MinValue;foreach(float x in new[]{-4f,0f,4f}){var q=yc+yrot*new Vector3(x,0,z);m=Mathf.Max(m,H(q.x,q.z));}return m;}
                 float back=Side(-4),front=Side(4),lat=0;foreach(float z in new[]{-4f,0f,4f}){var l=yc+yrot*new Vector3(-4,0,z);var r=yc+yrot*new Vector3(4,0,z);lat=Mathf.Max(lat,Mathf.Abs(H(l.x,l.z)-H(r.x,r.z)));}
                 bool clash=false;for(int f=0;f<footprints.Count-1;f++)if(QuarryHillsTown.Overlap(new(yc.x,yc.z),new(3.6f,3.6f),Yaw(barn,road.At(s)),footprints[f].c,footprints[f].half,footprints[f].yaw))clash=true;
                 if(!clash&&lat<.6f&&Mathf.Abs(back-front)<2f){var y=GameObject.CreatePrimitive(PrimitiveType.Cube);y.name="Tuner yard";y.transform.SetParent(root);
                     float pitch=Mathf.Atan2(back-front,8)*Mathf.Rad2Deg,thick=.3f+lat;
                     y.transform.SetPositionAndRotation(new Vector3(yc.x,(back+front)*.5f+.04f,yc.z)+yrot*Quaternion.Euler(pitch,0,0)*Vector3.down*(thick*.5f),yrot*Quaternion.Euler(pitch,0,0));y.transform.localScale=new Vector3(8,thick,8.2f);
                     y.GetComponent<Renderer>().sharedMaterial=yard??=TrialWorld.Material("Tuner yard concrete",new Color(.66f,.65f,.62f),0,.15f);}
             }}
            // ---- the dairy, out along the road towards the gate, its cows in the field behind
            {float s=Mathf.Min(road.Length-60,sC+420);var n=-N(s);
             var parlour=road.At(s)+n*(half+12f+5.2f);parlour.y=H(parlour.x,parlour.z);if(Building("milking_parlour",parlour,Yaw(parlour,road.At(s)))){Spots["dairy"]=(road.At(s)+n*(half+6),Yaw(parlour,road.At(s))+180);DairyAt=parlour+Vector3.up*4;}
             var dutch=road.At(s+26)+n*(half+12f+4.8f);dutch.y=H(dutch.x,dutch.z);Building("dutch_barn",dutch,Yaw(dutch,road.At(s+26)));
             var churn=road.At(s-14)+n*(half+1.6f);churn.y=H(churn.x,churn.z);Put("churn_stand",churn,Yaw(churn,road.At(s-14)));{var s0=road.At(s+38)+n*(half+5);s0.y=H(s0.x,s0.z);shoe[3]=s0;}   // (the yard in front of the Dutch barn)
             var field=road.At(s-30)+n*(half+60);field.y=H(field.x,field.z);herd=field;for(int i=0;i<9;i++)Cow(field+new Vector3((float)rng.NextDouble()*40-20,0,(float)rng.NextDouble()*40-20),field);}
            // ---- parked cars: two by the green, two outside The Plough, six lay-bys out in the vale, two in the dairy yard
            void Park(float s,Vector3 side,float off){if(s<20||s>road.Length-20)return;var p=road.At(s)+side*(half+off);p.y=H(p.x,p.z);Parking.Add((p,Yaw(road.At(s-1),road.At(s+1))));}
            Park(sC-30,N(sC-30),4f);Park(sC+26,N(sC+26),4f);Park(sC-5,-N(sC-5),4f);Park(sC+5,-N(sC+5),4f);   // (4 m off the edge: at 2.6-3 m they sat over the edge line, review BW1-01)
            foreach(float ds in new[]{-470f,-640f,-820f,-1000f,-1180f,-1350f})Park(sC+ds,N(sC+ds),3.8f);   // (out along the pass, where the vale's fields are)
            Park(sC+440,-N(sC+440),4f);Park(sC+447,-N(sC+447),4f);
            Vale(road,sC,herd);
            HumpbackBridge();Ford();RaceDay();
            // (the top of the pass: the road's highest point between the T and the village, 7 m off its north side)
            {float top=-1,best=float.MinValue;for(float s=60;s<sC-500;s+=10)if(road.At(s).y>best){best=road.At(s).y;top=s;}
             if(top>0){var s0=road.At(top)+N(top)*(half+7);s0.y=H(s0.x,s0.z);shoe[5]=s0;}}
            if(Spots.TryGetValue("orchard",out var orch)){var s0=orch.at+Quaternion.Euler(0,orch.yaw,0)*new Vector3(3,0,3);s0.y=H(s0.x,s0.z);shoe[2]=s0;}   // (between the apple rows)
            bool all=true;foreach(var p in shoe)if(p==Vector3.zero)all=false;if(all)Horseshoes.AddRange(shoe);
            Debug.Log($"LITTLE_ROAD_BRAMBLEWICK fields={Fields} copses={Copses} buildings={Buildings} plinths={Plinths} refused={Refused} spots={Spots.Count} hedges={hedges} hedge_refused={hedgeRefused} {hedgeWhy}");
        }
        // ---- the vale (step 4): a patchwork of hedged fields round the village, squared to the road through it as the farm
        // grids are; pasture (some with the dairy's heifers), hay meadow, wheat and rapeseed, the crops standing as canopies
        // the car can plough through (CropField, CropTrample). Every field hedged all round, one hedge on each shared boundary,
        // each side with a gateway; copses in the corners the fields leave, and the orchard on the north slope.
        public static int Fields,Copses;
        void Vale(HillcrestRoute.Road road,float sC,Vector3 cowField)
        {
            var yaw=Yaw(road.At(sC-1),road.At(sC+1));var rot=Quaternion.Euler(0,yaw,0);var c0=new Vector3(Bramblewick.Centre.x,0,Bramblewick.Centre.y);
            var green=new Vector3(Bramblewick.Green.x,0,Bramblewick.Green.z);var herd=new Vector3(cowField.x,0,cowField.z);var orchard=c0+rot*new Vector3(-150,0,250);
            float Ground(Vector3 p)=>Mathf.Max(H(p.x,p.z),H(p.x+1.2f,p.z),H(p.x-1.2f,p.z),H(p.x,p.z+1.2f),H(p.x,p.z-1.2f))+.08f;
            // The lattice: hedge lines across the vale at uneven spacings, each crossing nudged its own way, so the fields come
            // out four-sided and all different, and neighbours still share their boundaries.
            var xs=new List<float>();for(float x=-760;x<760;x+=55+(float)rng.NextDouble()*55)xs.Add(x);
            var zs=new List<float>();for(float z=-760;z<760;z+=42+(float)rng.NextDouble()*45)zs.Add(z);
            var L=new Vector3[xs.Count,zs.Count];
            for(int i=0;i<xs.Count;i++)for(int j=0;j<zs.Count;j++)L[i,j]=c0+rot*new Vector3(xs[i]+((float)rng.NextDouble()-.5f)*20,0,zs[j]+((float)rng.NextDouble()-.5f)*16);
            Vector3 At(int i,int j,float u,float w)=>Vector3.Lerp(Vector3.Lerp(L[i,j],L[i+1,j],u),Vector3.Lerp(L[i,j+1],L[i+1,j+1],u),w);
            int CellKind(int i,int j)   // (0 no field: road, village, hill; 1 a field; 2 rough ground the fields leave, for copses)
            {
                var c=At(i,j,.5f,.5f);float d=Vector2.Distance(new(c.x,c.z),Bramblewick.Centre);if(d>700)return 0;
                if(Vector3.Distance(c,green)<130||Vector3.Distance(c,herd)<55||Vector3.Distance(c,orchard)<70)return 0;
                float lo=float.MaxValue,hi=float.MinValue;
                for(int a=0;a<=4;a++)for(int b=0;b<=4;b++){var q=At(i,j,a/4f,b/4f);if(ValleyField.RoadDistance(q.x,q.z)<12)return 0;float g=H(q.x,q.z);lo=Mathf.Min(lo,g);hi=Mathf.Max(hi,g);}
                foreach(var o in footprints)if(Vector2.Distance(o.c,new(c.x,c.z))<Vector3.Distance(L[i,j],L[i+1,j+1])*.5f+o.half.magnitude+4)return 0;
                return hi-lo>7?2:1;   // (the steeper banks stay rough grazing and wood)
            }
            int nx=xs.Count-1,nz=zs.Count-1;var kind=new int[nx,nz];
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++){kind[i,j]=CellKind(i,j);if(kind[i,j]==1&&rng.NextDouble()<.1)kind[i,j]=2;}
            var crops=CropTrample.Material("Bramblewick",new Rect(Bramblewick.Centre.x-800,Bramblewick.Centre.y-800,1600,1600));
            var v=new List<Vector3>();var col=new List<Color>();var t=new List<int>();var lift=new List<Vector2>();var rowAt=new List<Vector2>();
            void Flush()
            {
                if(v.Count==0)return;var mesh=new Mesh{name="Bramblewick fields"};mesh.SetVertices(v);mesh.SetColors(col);mesh.SetTriangles(t,0);mesh.SetUVs(0,lift);mesh.SetUVs(1,rowAt);
                mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject("Bramblewick fields",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=crops;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;v.Clear();col.Clear();t.Clear();lift.Clear();rowAt.Clear();
            }
            // (hay, wheat and rapeseed in Hillcrest's colours: its palettes 5, 0 and 4)
            var hay=(new Color(.80f,.86f,.42f),new Color(.66f,.76f,.34f));var wheat=(new Color(.95f,.80f,.36f),new Color(.86f,.68f,.28f));var rape=(new Color(.99f,.86f,.22f),new Color(.42f,.62f,.26f));
            Fields=0;
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++) {
                if(kind[i,j]!=1)continue;Fields++;double roll=rng.NextDouble();var c=At(i,j,.5f,.5f);
                if(roll<.42) {   // pasture: the vale's own grass, a few heifers in one field in three
                    if(rng.NextDouble()<.35){int n=3+rng.Next(4);for(int k=0;k<n;k++)Cow(At(i,j,.2f+.6f*(float)rng.NextDouble(),.2f+.6f*(float)rng.NextDouble()),c);}
                    continue;
                }
                var (a,b,h)=roll<.64?(hay.Item1,hay.Item2,.5f):roll<.87?(wheat.Item1,wheat.Item2,.85f):(rape.Item1,rape.Item2,1.05f);
                // (the crop stops 3.5 m short of the hedges: the headland, where the tractor turns)
                var q=new Vector3[4];var corners=new[]{L[i,j],L[i+1,j],L[i+1,j+1],L[i,j+1]};
                for(int k=0;k<4;k++){var inward=(c-corners[k]);inward.y=0;q[k]=corners[k]+inward.normalized*4.9f;}
                if(rng.NextDouble()<.5)q=new[]{q[1],q[2],q[3],q[0]};   // (drilled the other way)
                CropField.Canopy(v,col,t,lift,rowAt,q,a,b,h,Ground,CropField.Tone);if(v.Count>50000)Flush();
            }
            Flush();
            // Hedges along every lattice line with a field on either side: one hedge to a boundary, continuous but for a
            // gateway in about half of them (with its gate in most), and where a road crosses.
            void Hedge(Vector3 a,Vector3 b,bool gateway)
            {
                var dir=b-a;dir.y=0;float len=dir.magnitude;dir/=len;float gap=gateway&&len>14?len*(.2f+.6f*(float)rng.NextDouble()):-99;
                var run=new List<Vector3>();
                void Lay(){if(run.Count>=3){var R=Runs.Solve(new RunSpec{name=$"Bramblewick hedge {hedges}",kit="hedge",seed=$"bramble-field-{hedges}",along=run.ToArray(),solid=false});if(R.items.Count>0){Runs.Draw(R,root);Runs.Laid.Add(R);hedges++;}else hedgeRefused++;}run.Clear();}
                for(float s=0;s<=len+.01f;s+=Mathf.Min(2.6f,len)) {
                    var q=a+dir*s;if(Mathf.Abs(s-gap)<2.4f||ValleyField.RoadDistance(q.x,q.z)<8||Bramblewick.BrookCut(q.x,q.z)>.3f||Mathf.Abs(H(q.x+1.2f,q.z)-H(q.x-1.2f,q.z))+Mathf.Abs(H(q.x,q.z+1.2f)-H(q.x,q.z-1.2f))>.55f){Lay();continue;}   // (a hedge stops at the brook: on its banks the hedge's ends stood out of level, the area audit)
                    q.y=H(q.x,q.z);run.Add(q);
                    if(rng.NextDouble()<.03){var tr=q;tr.y-=.05f;Put("tree_round",tr,(float)rng.NextDouble()*360,null,null,true,.9f+(float)rng.NextDouble()*.35f);}   // (a hedgerow oak)
                }
                Lay();
                if(gap>0&&rng.NextDouble()<.8){var g=a+dir*gap;if(ValleyField.RoadDistance(g.x,g.z)>8){g.y=H(g.x,g.z);Put("field_gate",g,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg-90);
                    // (the first gate out past 450 m keeps a horseshoe on its post's side, 2.5 m in front of it)
                    if(shoe[4]==Vector3.zero&&Vector2.Distance(new(g.x,g.z),Bramblewick.Centre)>450){var s0=g+Vector3.Cross(Vector3.up,dir)*2.5f;s0.y=H(s0.x,s0.z);if(ValleyField.RoadDistance(s0.x,s0.z)>8)shoe[4]=s0;}}}
            }
            bool Field(int i,int j)=>i>=0&&j>=0&&i<nx&&j<nz&&kind[i,j]==1;
            for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++) {
                if(i<nx&&(Field(i,j)||Field(i,j-1)))Hedge(L[i,j],L[i+1,j],rng.NextDouble()<.5);   // (along x: between cells (i,j-1) and (i,j))
                if(j<nz&&(Field(i,j)||Field(i-1,j)))Hedge(L[i,j],L[i,j+1],rng.NextDouble()<.5);   // (along z: between cells (i-1,j) and (i,j))
            }
            // Copses on the rough ground the fields leave, off the roads.
            Copses=0;
            for(int i=0;i<nx;i++)for(int j=0;j<nz;j++) {
                if(kind[i,j]!=2||rng.NextDouble()<.4)continue;
                int n=6+rng.Next(9);
                for(int k=0;k<n;k++){var q=At(i,j,.15f+.7f*(float)rng.NextDouble(),.15f+.7f*(float)rng.NextDouble());if(ValleyField.RoadDistance(q.x,q.z)<10)continue;q.y=H(q.x,q.z)-.05f;
                    Put(rng.NextDouble()<.75?"tree_round":"tree_pine",q,(float)rng.NextDouble()*360,null,null,true,.9f+(float)rng.NextDouble()*.5f);}
                Copses++;
            }
            // The orchard: apple trees in rows on the north slope, 6 m apart.
            {var o=orchard;
             for(int a=-4;a<=4;a++)for(int b=-3;b<=3;b++) {
                var q=o+rot*new Vector3(a*6+((float)rng.NextDouble()-.5f),0,b*6+((float)rng.NextDouble()-.5f));if(ValleyField.RoadDistance(q.x,q.z)<9)continue;
                bool hit=false;foreach(var f in footprints)if(QuarryHillsTown.Overlap(new(q.x,q.z),new(2,2),0,f.c,f.half,f.yaw))hit=true;if(hit)continue;
                q.y=H(q.x,q.z)-.05f;Put("tree_blossom",q,(float)rng.NextDouble()*360,null,null,true,.62f+(float)rng.NextDouble()*.15f);
             }
             o.y=H(o.x,o.z);Spots["orchard"]=(o,yaw);}
        }
        Vector3 herd;
        // The ford on Church Lane, where the lanes circuit crosses the brook's low point (rebuilt after launch review r1,
        // LR1-01: strips of water read as a placeholder):
        //   - one sheet of water across the lane, 12 m along it, a few centimetres deep (no collider: you splash through);
        //   - low stone wing walls along both road edges the length of the ford, which the water's ends tuck under, and
        //     reeds at their ends where the brook comes in and goes out;
        //   - a white depth post, FORD boards from both directions, and spray off the wheels of anything driving through.
        void Ford()
        {
            if(!Bramblewick.Marks.TryGetValue("Ford",out var at2))return;var lane=HillcrestRoute.Find(Bramblewick.LaneName);float s0=lane.Nearest(new Vector3(at2.x,0,at2.y),out _);
            var water=TrialWorld.Material("Ford water",new Color(.24f,.39f,.4f),0,.72f);var stone=WorldLook.Kit("Ford stone",new Color(.6f,.58f,.52f),WorldLook.Pattern.Stone,0,.1f,true);
            float half=lane.width*.5f;
            for(float s=s0-6;s<s0+6;s+=1.5f) {
                var a=lane.At(s);var b=lane.At(s+1.5f);var f=b-a;var right=lane.Right(s+.75f);right.y=0;right.Normalize();
                var w=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(w.GetComponent<Collider>());w.name="Ford water";w.transform.SetParent(root);
                w.transform.SetPositionAndRotation((a+b)*.5f+Vector3.up*.06f,Quaternion.LookRotation(f));w.transform.localScale=new Vector3(lane.width+1.4f,.02f,f.magnitude+.03f);
                var r=w.GetComponent<Renderer>();r.sharedMaterial=water;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach(int side in new[]{-1,1}){var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Ford wing wall";wall.transform.SetParent(root);
                    wall.transform.SetPositionAndRotation((a+b)*.5f+right*side*(half+.95f)+Vector3.up*.2f,Quaternion.LookRotation(f));wall.transform.localScale=new Vector3(.5f,.65f,f.magnitude+.04f);wall.GetComponent<Renderer>().sharedMaterial=stone;wall.tag="Respawn";}
            }
            var rightF=lane.Right(s0);rightF.y=0;rightF.Normalize();
            foreach(int side in new[]{-1,1})foreach(float end in new[]{-7.5f,7.5f}){var q=lane.At(s0+end)+rightF*side*(half+2.2f);q.y=H(q.x,q.z);Put("reeds",q,(float)dressRng.NextDouble()*360,null,null,false);}
            var post=lane.At(s0)+rightF*(half+1.6f);post.y=H(post.x,post.z);
            var p=GameObject.CreatePrimitive(PrimitiveType.Cube);p.name="Ford depth post";p.transform.SetParent(root);p.transform.position=post+Vector3.up*.75f;p.transform.localScale=new Vector3(.18f,1.5f,.18f);
            p.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material("Depth post white",new Color(.95f,.95f,.92f),0,.3f);p.tag="Respawn";
            foreach(int dir in new[]{-1,1}){float sb=s0+dir*45;var fb=lane.Forward(sb)*dir;fb.y=0;fb.Normalize();var rb=Vector3.Cross(Vector3.up,fb);var b=lane.At(sb)+rb*(half+2.4f);if(ValleyField.RoadDistance(b.x,b.z)<half+1.6f)continue;b.y=H(b.x,b.z);Put("map_board",b,Yaw(b,b-fb),null,"FORD");}
            var splash=new GameObject("Ford splash").AddComponent<FordSplash>();splash.transform.SetParent(root);splash.Setup(lane.At(s0),lane.Forward(s0),6.5f,half+.4f);
            Spots["ford"]=(lane.At(s0)+rightF*(half+6),Yaw(lane.At(s0)+rightF*(half+6),lane.At(s0)));
        }
        // The humpback bridge on Church Lane (the lanes circuit's crest over the brook): stone parapets along both edges of
        // the deck, following its hump, and a stone face down to the brook either side.
        void HumpbackBridge()
        {
            if(Bramblewick.BridgeAt==Vector3.zero)return;var lane=HillcrestRoute.Find(Bramblewick.LaneName);float s0=lane.Nearest(Bramblewick.BridgeAt,out _);float half=lane.width*.5f;
            for(float s=s0-13;s<s0+13;s+=1.6f) {
                var a=lane.At(s);var b=lane.At(s+1.6f);var right=lane.Right(s+.8f);right.y=0;right.Normalize();var mid=(a+b)*.5f;var along=b-a;
                foreach(int side in new[]{-1,1}) {
                    var w=GameObject.CreatePrimitive(PrimitiveType.Cube);w.name="Bridge parapet";w.transform.SetParent(root);
                    w.transform.SetPositionAndRotation(mid+right*side*(half+.3f)+Vector3.up*.35f,Quaternion.LookRotation(along));w.transform.localScale=new Vector3(.45f,.9f,along.magnitude+.06f);
                    w.GetComponent<Renderer>().sharedMaterial=plinth;w.tag="Respawn";
                    // (the face below: down to a metre under the deck at the crown, where the brook runs)
                    float g=H(mid.x+right.x*side*(half+.3f),mid.z+right.z*side*(half+.3f));if(mid.y-g<.2f)continue;
                    var f=GameObject.CreatePrimitive(PrimitiveType.Cube);f.name="Bridge face";Object.Destroy(f.GetComponent<Collider>());f.transform.SetParent(root);
                    f.transform.SetPositionAndRotation(new Vector3(mid.x,(mid.y+g)*.5f-.4f,mid.z)+right*side*(half+.3f),Quaternion.LookRotation(along));f.transform.localScale=new Vector3(.5f,mid.y-g+.8f,along.magnitude+.06f);
                    f.GetComponent<Renderer>().sharedMaterial=plinth;
                }
            }
            {float sb=s0-55;var fb=lane.Forward(sb);fb.y=0;fb.Normalize();var rb=Vector3.Cross(Vector3.up,fb);var bb=lane.At(sb)+rb*(half+2.4f);if(ValleyField.RoadDistance(bb.x,bb.z)>half+1.6f){bb.y=H(bb.x,bb.z);Put("map_board",bb,Yaw(bb,bb-fb),null,"HUMPBACK BRIDGE");}}
            Spots["bridge"]=(Bramblewick.BridgeAt+Bramblewick.BridgeAlong*30,Yaw(Bramblewick.BridgeAt+Bramblewick.BridgeAlong*30,Bramblewick.BridgeAt));
        }
        // A cottage garden: a hedge along the front, from party line to party line, with a 2.4 m gap at the gate before the
        // front door (the kit's Door socket); apple trees in the back garden.
        void Garden(HillcrestRoute.Road road,string id,Vector3 c,float yaw,Vector3 n,float mid,Vector2 size,float setback)
        {
            float gate=mid;var door=ForgeStandIns.Socket(id,c,yaw,"Door");if(door.HasValue){gate=road.Nearest(door.Value,out _);doors.Add(door.Value);}
            // (the garden: slabs from the gate to the front door, flowers inside the hedge either side of the gate, review BW1-07)
            {var side=Vector3.Dot(road.Right(gate),n)>0?road.Right(gate):-road.Right(gate);var g0=road.At(gate)+side*(setback-1.2f);g0.y=H(g0.x,g0.z);
             if(door.HasValue)Slabs(g0,door.Value-(door.Value-g0).normalized*.6f,.85f);
             if(BayviewKit.Has("flower_bush"))foreach(int f in new[]{-1,1}){var fl=g0+road.Forward(gate)*f*2.2f+side*1.2f;fl.y=H(fl.x,fl.z);Put("flower_bush",fl,(float)dressRng.NextDouble()*360,null,null,false,.8f);}}
            float s0=mid-size.x*.5f-.5f,s1=mid+size.x*.5f+.5f,off=setback-1.2f;
            foreach(var (a,b) in new[]{(s0,gate-1.2f),(gate+1.2f,s1)}) {
                if(b-a<1.6f)continue;var line=new List<Vector3>();for(float s=a;s<=b+.01f;s+=Mathf.Max(.5f,(b-a)/6)){var p=road.At(s)+(Vector3.Dot(road.Right(s),n)>0?road.Right(s):-road.Right(s))*off;p.y=H(p.x,p.z);line.Add(p);}
                if(line.Count<2)continue;var L=Runs.Solve(new RunSpec{name=$"Bramblewick garden hedge {hedges}",kit="hedge",seed=$"bramble-garden-{hedges}",along=line.ToArray(),solid=false});
                if(L.failures.Count==0&&L.items.Count>0){Runs.Draw(L,root);Runs.Laid.Add(L);hedges++;}else{hedgeRefused++;hedgeWhy=L.failures.Count>0?L.failures[0]:L.notes.Count>0?L.notes[0]:"none laid";}
            }
            int trees=rng.Next(0,3);var back=-n;
            for(int k=0;k<trees;k++){var t=c+back*(size.y*.5f+4+(float)rng.NextDouble()*5)+Quaternion.Euler(0,yaw,0)*Vector3.right*((float)rng.NextDouble()-.5f)*size.x;t.y=H(t.x,t.z)-.05f;
                if(ValleyField.RoadDistance(t.x,t.z)>8)Put(rng.NextDouble()<.6?"tree_blossom":"tree_round",t,(float)rng.NextDouble()*360,null,null,true,.75f+(float)rng.NextDouble()*.25f);}
        }
        int hedges,hedgeRefused;string hedgeWhy="";
        // The churchyard: a dry-stone wall round the church, 6 m clear of it, open at the lychgate.
        void ChurchyardWall(Vector3 church,float yaw,Vector3 gate)
        {
            var rot=Quaternion.Euler(0,yaw,0);var hw=new Vector2(Footprint("parish_church").x*.5f+6,Footprint("parish_church").y*.5f+6);
            var corners=new[]{new Vector3(-hw.x,0,-hw.y),new Vector3(hw.x,0,-hw.y),new Vector3(hw.x,0,hw.y),new Vector3(-hw.x,0,hw.y)};
            // (the side towards the green carries the lychgate: two runs either side of it; the other three one run)
            var front=new List<Vector3>();var rest=new List<Vector3>();
            for(int k=0;k<4;k++){var p=church+rot*corners[(k+3)%4];p.y=H(p.x,p.z);rest.Add(p);}   // (round the back from the front-left corner to the front-right: +z faces the green)
            Vector3 fl=rest[0],fr=rest[^1];var g=gate;g.y=H(g.x,g.z);var along=(fr-fl);along.y=0;along.Normalize();
            foreach(var line in new[]{rest,new List<Vector3>{fl,g-along*1.8f},new List<Vector3>{g+along*1.8f,fr}}) {
                var L=Runs.Solve(new RunSpec{name="Churchyard wall",kit="dry_stone",seed=$"bramble-churchyard-{line.Count}-{line[0].x:0}",along=line.ToArray()});
                if(L.items.Count>0){Runs.Draw(L,root);Runs.Laid.Add(L);}
            }
        }
        // A Friesian, grazing (the cows kit: body and head, the head nodding at the neck as the sheep's do).
        void Cow(Vector3 p,Vector3 field)
        {
            if(!PropKit.Has("cows"))return;p.y=H(p.x,p.z);
            var cow=new GameObject("Cow");cow.transform.SetParent(root);cow.transform.SetPositionAndRotation(p,Quaternion.Euler(0,(float)rng.NextDouble()*360,0));
            var kit=PropKit.Load("cows");int v=rng.Next(4),h=rng.Next(3);
            var body=new GameObject("Body",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(cow.transform,false);
            body.GetComponent<MeshFilter>().sharedMesh=kit.Mesh("body_"+v,0);body.GetComponent<MeshRenderer>().sharedMaterials=kit.MaterialsOf("body_"+v,0);
            var head=new GameObject("Head",typeof(MeshFilter),typeof(MeshRenderer));head.transform.SetParent(cow.transform,false);head.transform.localPosition=new Vector3(0,1.15f,1.2f);
            head.GetComponent<MeshFilter>().sharedMesh=kit.Mesh("head_"+h,0);head.GetComponent<MeshRenderer>().sharedMaterials=kit.MaterialsOf("head_"+h,0);
            var g=cow.AddComponent<Grazer>();g.headAt=new Vector3(0,1.15f,1.2f);g.nod=.5f;g.Setup(field,22);
        }
    }
}
