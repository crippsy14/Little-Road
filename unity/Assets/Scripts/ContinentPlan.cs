using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // The continent (docs/58): layout B "Glasswater" from the world blueprint, as data. Kilometres on a 20 x 20 km sheet,
    // x east and y north, exactly as the blueprint draws it; ToWorld puts it in game coordinates (Bayview island at the
    // origin, +z north, metres), so today's Bayview and Hillcrest sit where they already are.
    public static class ContinentPlan
    {
        public const float SizeKm=20;
        public static readonly Vector2 BayKm=new(6.0f,18.98f);
        public static Vector3 ToWorld(float xKm,float yKm,float y=0)=>new((xKm-BayKm.x)*1000,y,(yKm-BayKm.y)*1000);
        public static Vector3 ToWorld(Vector2 km,float y=0)=>ToWorld(km.x,km.y,y);
        public static Vector2 ToKm(Vector3 world)=>new(world.x/1000+BayKm.x,world.z/1000+BayKm.y);

        public sealed class Place
        {
            public string id,name,cls,kind;public float x,y; // kind: hub, main, island, village, secret
            public Vector2 Km=>new(x,y);
            public bool Main=>kind=="hub"||kind=="main"||kind=="island";
            // How far the town's flat ground and buildings reach (metres).
            public float Radius=>kind=="hub"?850:kind=="island"?420:kind=="main"?380:kind=="secret"?220:140;
        }
        public sealed class Road {public string a,b,type,name;public Vector2[] via;}

        static Place P(string id,string name,string cls,string kind,float x,float y)=>new(){id=id,name=name,cls=cls,kind=kind,x=x,y=y};
        public static readonly Place[] Places={
            P("bayview","Bayview","C","main",BayKm.x,BayKm.y-.15f),P("hillcrest","Hillcrest","C","main",BayKm.x+.1f,BayKm.y-1.15f),
            P("quarry","Quarry Hills","C","main",3.7f,16.2f),P("vinelands","Vinelands","C","main",9.4f,15.7f),
            P("saltmarsh","Saltmarsh","C","village",12.6f,18.2f),P("bramble","Bramblewick","C","village",8.4f,17.2f),P("kettle","Kettle Falls","C","village",2.9f,14.6f),
            P("cobble","Cobble Cove","C","village",11.6f,17.5f),P("orchard","Orchard End","C","village",7.3f,15.3f),
            P("harbour","Harbour Point","B","main",14.6f,18.15f),P("highmoor","Highmoor","B","village",7.9f,13.4f),P("millbrook","Millbrook","B","village",10.4f,14.3f),
            P("dunmore","Dunmore Junction","B","village",13.3f,14.6f),P("tollgate","Tollgate","B","village",4.3f,13.6f),P("summit","Summit Pass","B","main",5.5f,10.6f),
            P("ridgeway","Ridgeway Circuit","B","village",8.5f,7.1f),P("coralkey","Coral Key","B","island",1.75f,18.95f),P("lowtown","Lowtown","B","secret",11.7f,8.5f),
            P("mycity","My City","A","hub",11.0f,12.6f),P("cinder","Cinder Flats","A","village",17.8f,14.0f),P("portcity","Port City","A","main",18.4f,7.5f),
            P("lantern","Cape Lantern","A","village",18.9f,5.0f),P("lowmere","Lowmere","A","village",12.5f,6.1f),P("larchwood","Larchwood","A","village",3.3f,6.3f),
            P("pinewood","Pinewood Coast","A","main",2.3f,8.7f),P("fernhollow","Fernhollow","A","village",2.4f,11.6f),
            P("icefall","Icefall Hut","S","village",7.2f,4.3f),P("frostcrown","Frostcrown","S","main",9.3f,3.3f),P("mirage","Mirage Wells","S","village",16.1f,10.5f),
            P("redrock","Redrock Mesa","S","main",17.2f,11.4f),P("copper","Copper Gulch","S","village",18.0f,12.9f),
            P("aurora","Aurora Raceway","E","main",15.9f,2.3f),P("polar","Polar Post","E","village",14.3f,1.95f)};
        public static readonly Dictionary<string,Vector2> Junctions=new(){
            ["R1"]=new(11.0f,13.2f),["R2"]=new(14.6f,11.9f),["R3"]=new(15.4f,9.2f),["R4"]=new(14.0f,6.3f),["R5"]=new(11.0f,5.6f),["R6"]=new(7.8f,6.6f),["R7"]=new(6.6f,9.3f),["R8"]=new(7.6f,12.0f)};
        static Road R(string a,string b,string type,string name=null,params float[] via)
        {
            var v=new Vector2[via.Length/2];for(int i=0;i<v.Length;i++)v[i]=new Vector2(via[i*2],via[i*2+1]);
            return new Road{a=a,b=b,type=type,name=name??"",via=v};
        }
        public static readonly Road[] Roads={
            R("R1","R2","motorway","North Shore"),R("R2","R3","motorway","East Shore"),R("R3","R4","motorway","Reed Bay Causeway",15.2f,7.4f),R("R4","R5","motorway","South Shore"),
            R("R5","R6","motorway","Glacier Bay Causeway"),R("R6","R7","motorway","West Shore"),R("R7","R8","motorway","Spine Foot"),R("R8","R1","motorway","North Shore"),
            R("mycity","R1","road"),R("bayview","hillcrest","causeway","Causeway & tunnel"),R("hillcrest","vinelands","road","Valley Road South",7.4f,16.9f),R("vinelands","R8","road",null,8.2f,14.0f),
            R("vinelands","millbrook","road"),R("millbrook","mycity","road"),
            R("hillcrest","quarry","road","Quarry Road",4.9f,17.1f),R("hillcrest","cobble","road","Coast Road",9.2f,18.3f),R("cobble","saltmarsh","road","Coast Road"),R("saltmarsh","harbour","road","Coast Road"),
            R("harbour","R2","motorway","Harbour Spoke",14.8f,15.0f),
            R("quarry","fernhollow","gravel","West Coast Track",2.2f,13.6f),R("fernhollow","pinewood","gravel","West Coast Track",1.9f,10.6f),R("quarry","tollgate","gravel","Old Quarry Road"),
            R("tollgate","summit","gravel","Old Quarry Road"),R("quarry","kettle","gravel"),
            R("summit","R7","mountain","Summit Pass"),R("summit","pinewood","mountain","Pass West",3.7f,9.8f),
            R("redrock","R2","road",null,16.2f,12.2f),R("redrock","portcity","gravel","Canyon Track",18.2f,9.6f),R("portcity","R3","motorway","Port Spoke",17.0f,8.4f),
            R("copper","redrock","gravel"),R("lantern","portcity","road"),R("mirage","R3","road"),R("mirage","redrock","gravel"),
            R("frostcrown","R5","mountain","Glacier Climb",10.2f,4.3f),R("frostcrown","polar","ice","Frozen Lake Road",12.9f,2.15f),R("polar","aurora","road"),R("aurora","R4","road","Tundra Road",15.2f,4.3f),
            R("pinewood","larchwood","gravel","Rainforest Track",3.0f,7.6f),R("larchwood","frostcrown","gravel","Rainforest Track",6.4f,3.4f),
            R("harbour","cinder","gravel","Dry Coast Track",16.6f,15.6f),R("cinder","redrock","gravel","Dry Coast Track",17.4f,12.8f),R("R1","R5","causeway","Lake Causeway",11.0f,9.3f),
            R("bramble","hillcrest","road"),R("bramble","vinelands","road"),R("orchard","vinelands","road"),R("orchard","highmoor","road"),R("highmoor","R8","road"),R("highmoor","vinelands","road"),
            R("dunmore","R1","road"),R("dunmore","harbour","road"),R("ridgeway","R6","road"),R("lowmere","R4","road"),R("lowmere","R5","road"),
            R("icefall","R6","mountain","Glacier Road West"),R("icefall","frostcrown","mountain","Glacier Road West"),
            R("bayview","coralkey","undersea","Undersea Road",4.6f,19.2f,3.0f,19.15f),R("mycity","lowtown","undersea","Dive ramp",11.6f,11.4f,11.8f,9.8f)};

        // Road types: carriageway width, the steepest grade the profile may use, the height-smoothing window, and how
        // much a country road wanders either side of the blueprint's straight line (metres).
        public sealed class RoadType {public float width,grade,smooth,wander,wavelength,blend;public bool tarmac,overWater=true;}
        public static readonly Dictionary<string,RoadType> Types=new(){
            ["motorway"]=new(){width=20,grade=.06f,smooth=500,wander=40,wavelength=2400,blend=70,tarmac=true},
            ["road"]=new(){width=9,grade=.09f,smooth=180,wander=70,wavelength=900,blend=45,tarmac=true},
            ["mountain"]=new(){width=8,grade=.12f,smooth=90,wander=130,wavelength=520,blend=40,tarmac=true},
            ["gravel"]=new(){width=7,grade=.13f,smooth=110,wander=95,wavelength=650,blend=35},
            ["ice"]=new(){width=10,grade=.08f,smooth=150,wander=40,wavelength=900,blend=35},
            ["causeway"]=new(){width=11,grade=.06f,smooth=300,wander=0,wavelength=1000,blend=50,tarmac=true},
            ["undersea"]=new(){width=9,grade=.16f,smooth=140,wander=80,wavelength=700,blend=35,overWater=false}};

        // ---------------------------------------------------------------- climate inputs (the blueprint's layout B)
        public static readonly float[][] Blobs={new[]{10.2f,9.9f,9.3f,9.1f,1},new[]{2.3f,9.5f,2.6f,5.6f,.9f},new[]{16.8f,15.8f,3.0f,2.8f,.7f},new[]{17.6f,4.4f,2.6f,3.2f,.8f}};
        public static readonly float[][] Cuts={new[]{6.4f,19.75f,2.6f,.92f},new[]{10.6f,19.8f,2.0f,1.4f},new[]{.2f,12.3f,1.5f,.8f},new[]{19.95f,7.4f,1.15f,1.5f},new[]{19.2f,.6f,2.2f,1.2f},new[]{.6f,2.0f,1.6f,1.9f}};
        public static readonly float[][] Lakes={new[]{11.0f,9.3f,3.5f,2.6f},new[]{12.9f,2.15f,.85f,.55f}};
        public static readonly string[] LakeNames={"Glasswater","Frozen lake"};
        public static readonly float[][] Islands={new[]{1.75f,18.95f,.78f,.5f}};
        public sealed class Ridge {public Vector2[] pts;public float h,w;}
        static Vector2[] Pts(params float[] p){var v=new Vector2[p.Length/2];for(int i=0;i<v.Length;i++)v[i]=new Vector2(p[i*2],p[i*2+1]);return v;}
        public static readonly Ridge[] Ridges={
            new(){pts=Pts(6.1f,15.9f,5.6f,13.2f,5.4f,10.6f,5.9f,7.6f,7.0f,5.2f,8.6f,3.4f),h=1.0f,w=.85f},
            new(){pts=Pts(8.6f,3.4f,10.6f,2.7f,13.2f,3.5f,15.0f,2.6f),h=.9f,w=1.0f},
            new(){pts=Pts(15.4f,14.6f,15.1f,12.2f,15.25f,10.3f),h=.5f,w=.75f}, // ends short of the ring (R3) so the motorway passes round it
            new(){pts=Pts(4.9f,16.5f,7.4f,16.4f),h=.38f,w=.45f}};
        public static readonly Vector2[][] Rivers={Pts(10.4f,4.4f,10.8f,6.7f),Pts(11.0f,12.0f,10.4f,14.6f,10.5f,17.2f,10.7f,18.9f),Pts(3.0f,13.8f,1.6f,12.6f,.6f,12.4f),Pts(16.8f,8.8f,18.0f,8.0f,19.2f,7.5f)};
        public static readonly float[][] Basins={new[]{11.0f,9.3f,3.6f},new[]{BayKm.x,BayKm.y-1.2f,1.1f}};
        // Today's world: the Hillcrest mainland (always land) and the channel to Bayview island.
        public static readonly Rect TodayValley=Rect.MinMaxRect(BayKm.x-1.1f,BayKm.y-2.0f,BayKm.x+1.3f,BayKm.y-.38f);
        public static readonly Rect TodayIsland=Rect.MinMaxRect(BayKm.x-.17f,BayKm.y-.45f,BayKm.x+.19f,BayKm.y+.13f);

        public static Place Find(string id){foreach(var p in Places)if(p.id==id)return p;return null;}
        public static Vector2 NodeKm(string id){var p=Find(id);return p!=null?p.Km:Junctions[id];}
    }
}
