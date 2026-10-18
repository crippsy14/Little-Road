using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge's dressing (stage two, docs/78), behind --prop-forge: the small things that make a place lived in, placed
    // by rules read from what's already built, in every town at once. Each rule looks at placed kit pieces (BayviewKit.Placed)
    // and puts the street kit's new pieces (forge/world/kits/street.py) where people would:
    //   a bin beside each bench;                     an A-board in front of each shop and cafe;
    //   a pillar box by each post office;            a phone box near each fountain (the squares);
    //   creels, crates, barrels, buoys and rope along the quay tops behind the sea walls;
    //   deckchairs and windbreaks in front of the beach huts;   planters and bollards along the shop fronts;
    //   a barrow, churns and a trough at each barn;   washing lines, sheds, kennels and barrows behind houses;
    //   a bus shelter at each station;               beehives among the orchard's blossom trees.
    // Every spot must be clear (no footprint, no collider, no road) and is stood on the highest solid surface there (the
    // quay's paving, a pavement). Seeded from the spot: the same street every build. Runs once the world is built (before
    // the settle step), so it sees everything placed; a district built later in the background isn't dressed.
    public static class PropForgeDressing
    {
        static Transform root;static System.Random rng;public static int Placed;
        static readonly Dictionary<string,int> counts=new(),refused=new();static void No(string why){refused[why]=refused.TryGetValue(why,out int n)?n+1:1;}
        public static string Summary=>string.Join(" ",counts.OrderBy(k=>k.Key).Select(k=>$"{k.Key}={k.Value}"));

        public static void Build(Transform parent)
        {
            root=new GameObject("Prop Forge dressing").transform;root.SetParent(parent,false);rng=new System.Random(4711);Placed=0;counts.Clear();refused.Clear();
            if(!BayviewKit.Has("litter_bin"))return;
            var placed=BayviewKit.Placed.Where(g=>g!=null).ToList();
            foreach(var g in placed)Dress(g);
            Debug.Log($"LITTLE_ROAD_PROP_FORGE_DRESSING placed={Placed} {Summary} refused: {string.Join(" ",refused.Select(k=>k.Key+"="+k.Value))} seen: {string.Join(" ",placed.GroupBy(x=>x.name).Where(x=>new[]{"bench","shop","cafe","motors","post_office","fountain","beach_hut","barn","station","house_a","house_c","cottage_a","tree_blossom"}.Contains(x.Key)).Select(x=>x.Key+"="+x.Count()))}");
        }
        static void Dress(GameObject g)
        {
            var p=g.transform.position;var right=g.transform.right;var fwd=g.transform.forward;string id=g.name;
            switch(id) {
                case "bench":   // (a bin at one end, on the side away from the next bench)
                    if(!Try("litter_bin",p+right*1.45f,0,.32f))Try("litter_bin",p-right*1.45f,0,.32f);break;
                case "shop": case "cafe": case "motors": case "post_office": case "kiosk": case "market_stall": {
                    var front=Front(g,out var dir);if(front==null){No(id+":no road");break;}
                    Try("sandwich_board",front.Value+dir*1.0f+Side(dir)*Rand(-1.2f,1.2f),Face(dir),.42f);
                    if(id=="post_office")Try("pillar_box",front.Value+dir*1.2f+Side(dir)*2.2f,Face(dir),.3f);
                    if(rng.NextDouble()<.7)Try("planter",front.Value+dir*.9f-Side(dir)*2.4f,Face(dir),.6f);
                    if(id=="cafe")Try("bike_rack",front.Value+dir*1.1f+Side(dir)*3.6f,Face(dir)+90,.9f);
                    break;}
                case "fountain":
                    for(int k=0;k<8;k++){float a=(k*45+Rand(0,20))*Mathf.Deg2Rad;var q=p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Rand(7,10);if(Try("phone_box",q,Rand(0,360),.6f))break;}
                    for(int k=0;k<4;k++){float a=(k*90+45)*Mathf.Deg2Rad;Try("bollard",p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*5.6f,0,.18f);}
                    break;
                case "sea_wall": {   // (the quay top behind the sea wall: clutter in twos and threes, 2-5 m inland)
                    var sea=Downhill(p);if(sea==Vector3.zero||rng.NextDouble()>.55)break;var along=Side(sea);
                    var at=p-sea*Rand(2.5f,5f)+along*Rand(-3,3);at.y=p.y+3;Harbour(at,2.2f,2+rng.Next(3));break;}
                case "barn": {   // (the farmyard: a barrow, churns at the door, a trough out in the field)
                    var front=Front(g,out var dir);var d=front!=null?dir:-fwd;var at=front??p;
                    Try("wheelbarrow",at+d*1.6f+Side(d)*Rand(1.5f,3f),Face(d)+Rand(-40,40),.6f);
                    for(int k=0;k<2;k++)Try("milk_churn",at+d*1.2f-Side(d)*(2.5f+k*.9f),Rand(0,360),.4f);
                    Try("water_trough",p-d*Rand(8,12)+Side(d)*Rand(-6,6),Face(Side(d)),1.2f);break;}
                case "house_a": case "house_b": case "house_c": case "house_d": case "cottage_a": case "cottage_b": case "cottage_c": {
                    var front=Front(g,out var dir);if(front==null)break;var back=-dir;var c=g.GetComponentsInChildren<Renderer>()[0].bounds.center;c.y=p.y;
                    var rs=g.GetComponentsInChildren<Renderer>();var bb=rs[0].bounds;foreach(var r in rs)bb.Encapsulate(r.bounds);float depth=Vector3.Dot(bb.extents,new Vector3(Mathf.Abs(back.x),0,Mathf.Abs(back.z)));
                    var garden=c+back*(depth+3.5f);
                    if(rng.NextDouble()<.45)Try("washing_line",garden+Side(back)*Rand(-1.5f,1.5f),Face(Side(back)),.6f,House());
                    if(rng.NextDouble()<.3)Try("garden_shed",garden+back*2.5f+Side(back)*Rand(2.5f,4f),Face(-back),1.3f,House());
                    if(rng.NextDouble()<.18)Try("kennel",c+back*(depth+1.6f)-Side(back)*Rand(1.5f,3f),Face(-back)+Rand(-30,30),.7f,House());
                    if(rng.NextDouble()<.25)Try("wheelbarrow",garden-Side(back)*Rand(2,3.5f),Rand(0,360),.6f);
                    break;}
                case "station": {
                    var front=Front(g,out var dir);if(front!=null)Try("bus_shelter",front.Value+dir*3.5f+Side(dir)*Rand(-6,6),Face(dir)+180,1.9f,House());break;}
                case "tree_blossom":   // (an orchard's hives, by a few of its trees)
                    if(rng.NextDouble()<.12)Try("beehive",p+new Vector3(Rand(-2.5f,2.5f),0,Rand(-2.5f,2.5f)),Rand(-20,20),.45f);break;
                case "beach_hut": {
                    var sea=Downhill(p);if(sea==Vector3.zero){No("beach:flat");break;}
                    for(int k=0;k<3;k++)Try("deck_chair",p+sea*Rand(3.5f,6f)+Side(sea)*Rand(-2.5f,2.5f),Face(sea)+Rand(-20,20),.45f,Tint());
                    if(rng.NextDouble()<.6)Try("windbreak",p+sea*Rand(5,7)+Side(sea)*Rand(-1,1),Face(sea)+Rand(-15,15),1f,Tint());
                    break;}
            }
        }
        // A cluster of harbour clutter round a point: creels stacked by twos, crates, barrels, a buoy or two, a rope coil.
        static void Harbour(Vector3 at,float radius,int n)
        {
            string[] kinds={"lobster_pot","lobster_pot","fish_crates","barrel","buoy","rope_coil","lobster_pot","fish_crates"};
            for(int k=0;k<n*8&&n>0;k++){float a=Rand(0,360)*Mathf.Deg2Rad;var q=at+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*Rand(0,radius);
                if(Try(kinds[rng.Next(kinds.Length)],q,Rand(0,360),.4f))n--;}
        }
        // (a house's colours for its garden things: a painted shed, a door, washing)
        static BayviewKit.Tint House()
        {
            Color[] paint={new(.18f,.42f,.36f),new(.62f,.2f,.18f),new(.2f,.32f,.58f),new(.86f,.7f,.3f),new(.5f,.62f,.42f)};
            var t=new BayviewKit.Tint();t.shutter=paint[rng.Next(paint.Length)];t.door=paint[rng.Next(paint.Length)];t.awning=new[]{new Color(.92f,.22f,.24f),new Color(.16f,.4f,.86f),new Color(1,.74f,.1f)}[rng.Next(3)];
            t.wall=new Color(.86f,.8f,.66f);return t;
        }
        static BayviewKit.Tint Tint()
        {
            Color[] stripes={new(.92f,.22f,.24f),new(.16f,.4f,.86f),new(.18f,.62f,.36f),new(1,.74f,.1f),new(.96f,.5f,.7f)};
            var t=new BayviewKit.Tint();t.awning=stripes[rng.Next(stripes.Length)];return t;
        }
        // A building's front: the point on its footprint nearest a road, and the direction out to the road (null: no road near).
        static Vector3? Front(GameObject g,out Vector3 dir)
        {
            dir=default;var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)return null;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            Vector3 best=default;float bd=float.MaxValue;
            for(int k=0;k<16;k++){float a=k*22.5f*Mathf.Deg2Rad;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var q=b.center+Vector3.Scale(d,b.extents)*1.02f;q.y=b.min.y;float r=Road(q);if(r<bd){bd=r;best=q;dir=d;}}
            if(bd>14)return null;return best;
        }
        static Vector3 Downhill(Vector3 p)
        {
            Vector3 best=Vector3.zero;float low=Ground.Surface(p.x,p.z)-.3f;
            for(int k=0;k<12;k++){float a=k*30*Mathf.Deg2Rad;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float h=Ground.Surface(p.x+d.x*8,p.z+d.z*8);if(h<low){low=h;best=d;}}
            return best;
        }
        static float Road(Vector3 p)
        {
            float d=ValleyField.RoadDistance(p.x,p.z);
            var land=CoastalLandscape.Current;if(land!=null&&p.z>-380){land.Track.Nearest(p,out float off);d=Mathf.Min(d,Mathf.Abs(off)-Circuit.Width*.5f);}
            return d;
        }
        static Vector3 Side(Vector3 d)=>new(d.z,0,-d.x);
        static float Face(Vector3 d)=>Mathf.Atan2(-d.x,-d.z)*Mathf.Rad2Deg;   // (the street kit's fronts face -z: towards d)
        static float Rand(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
        // Place a piece if the spot is clear: off every road (by 1.5 m), no footprint or collider within `room`, standing on
        // the highest solid surface there and not on a slope steeper than 1 in 5.
        static bool Try(string id,Vector3 at,float yaw,float room,BayviewKit.Tint tint=null)
        {
            if(Road(at)<1.5f){No(id+":road");return false;}
            float top=float.MinValue;foreach(var hit in Physics.RaycastAll(new Vector3(at.x,at.y+40,at.z),Vector3.down,80))if(!hit.collider.isTrigger&&hit.collider.attachedRigidbody==null)top=Mathf.Max(top,hit.point.y);
            if(top==float.MinValue)top=Ground.Surface(at.x,at.z);
            if(Mathf.Abs(top-at.y)>4){No(id+":height");return false;}   // (a roof, a cliff: not the street it was meant for)
            var spot=new Vector3(at.x,top,at.z);var box=new Bounds(spot+Vector3.up*(.15f+1f),new Vector3(room*2,2f,room*2));   // (room: half its width; a box 2 m tall from just above the ground)
            if(BayviewKit.Occupied(box)){No(id+":footprint");return false;}
            if(Physics.OverlapBox(box.center,box.extents*.9f).Any(c=>!c.isTrigger)){No(id+":collider");return false;}
            float s1=Ground.Surface(at.x+.6f,at.z),s2=Ground.Surface(at.x,at.z+.6f),s0=Ground.Surface(at.x,at.z);
            if(Mathf.Abs(top-s0)<.3f&&(Mathf.Abs(s1-s0)>.12f||Mathf.Abs(s2-s0)>.12f)){No(id+":slope");return false;}
            var g=BayviewKit.Spawn(id,spot,yaw,root,tint);Physics.SyncTransforms();
            Placed++;counts[id]=counts.TryGetValue(id,out int n)?n+1:1;
            if(n==0&&RaceTrial.Has("--gate-log"))Debug.Log($"LITTLE_ROAD_DRESSED {id} {spot.x:0.0},{spot.y:0.0},{spot.z:0.0}");
            return g!=null;
        }
    }
}
