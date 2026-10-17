using System;
using System.Linq;

namespace LittleRoad
{
    // Body catalogue. Shape never changes performance; fitted equipment does.
    // Bodies from source-art/build_family_cars.py are matched to an entrant's
    // equipment tier so faster cars also look faster.
    public static class CarBodies
    {
        public const string Starter="puddle",Flagship="zenith";
        // Bodies a save may own (the player's car).
        public static readonly string[] Owned={Starter,"compact_v1"};
        // Placeholder bodies removed on 2026-09-28; older saves drop them on load.
        public static readonly string[] Retired={"coupe_car","utility_car","formula_car"};
        // Street-spec field: family cars and saloons.
        public static readonly string[] Family={"pebble","tallboy","marlow","dover","heron","bramble","picnic","kipper","doodle","otter"};
        // Club-spec field: mass-produced sporty bodies.
        public static readonly string[] Sporty={"sprint","comet","rally","roadster"};
        // The fastest-looking C cars race only the top C events, 8★ to 10★ on the finished game's 1-10★ C ladder (Richard,
        // 14 Oct: "keep the Rally saloon and Zenith for higher-star races ... from 8 upwards"); below that the pole-sitter's
        // livery goes on the Comet.
        public static readonly string[] TopC={"rally",Flagship};public const int TopCStars=8;public const string LowFlagship="comet";
        public static bool TopCOpen(string raceClass,int stars)=>raceClass!="C"||stars>=TopCStars;
        // Hillcrest townsfolk: free-roam residents only, never dealt into race fields.
        public static readonly string[] Townsfolk={"bubble","beetle","taxi","camper","gelato","minibus","ritz","postvan"};
        // Quarry Hills' off-roaders (Richard, 3 Oct): stock class C cars with an off-road look. They race the rallycross and fill
        // the rally town's streets; sold at Nia's body shop.
        public static readonly string[] OffRoad={"cob","drover","scamp","fell","tumbler","hauler"};
        // Class B, the Tuners (docs/65, docs/67): the 21 road bodies race the tarmac Tuner events; the six off-road B bodies,
        // with the rally-bred road ones, race the gravel. Bigger than class C (up to 4.7 m by 2.3 m): each races its own
        // measured box (Shoulders, Lengths).
        public static readonly string[] Tuner={"hayate","kumo","fubuki","kaze","arashi","raijin","hoshi","sakura","yoru","tsuki","taiyo","hibari","tsubasa","kitsune","hachi","kogarashi","chibi","midori","shiro","nagare","kage"};
        public static readonly string[] TunerOffRoad={"sabaku","yama","tanuki","tora","kaminari","kiri"};
        // The free class B car (docs/67, Richard 7 Oct): one of three, a drivetrain each (FWD, RWD, AWD).
        public static readonly string[] TunerStarters={"tsubasa","sakura","kitsune"};
        // A body's class: the parts it can take (docs/65, docs/67). Class C bodies take class C parts; a Tuner body is the
        // class B platform. The Regale (the Duke's prototype) takes anything.
        public static string Class(string id)=>id==Prototype?"E":Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0?"B":"C";
        public static readonly string[] TunerGravel={"sabaku","yama","tanuki","tora","kaminari","kiri","kumo","fubuki","taiyo","kitsune"};
        // The Pit Stop's own vehicles (Richard, 4 Oct: one model per job, the same in every town, so the player knows who's who):
        // the garage's recovery truck, the parts van, the body shop's panel truck. Hosts only: never sold or raced.
        public const string PitGarage="pitgarage",PitParts="pitparts",PitBody="pitbody";
        public static readonly string[] PitStop={PitGarage,PitParts,PitBody};
        // Teasers of the later classes: free-roam only, never sold or raced yet. The Regale is Duke Rolo's prototype
        // (docs/53), the sort of car the S and E classes will race.
        public const string Prototype="regale";
        public static readonly string[] Teasers={Prototype};
        // Car Forge test bodies: the engine's stress designs (car-forge, gate E9), exported by its tools/export_suite.py and
        // tools/install_game.py. Not in any field yet; checked by the build and shown by --forge-lineup.
        public static readonly string[] Forge={"forge_closed_defaults","forge_closed_hatch","forge_closed_4x4","forge_closed_van",
            "forge_closed_pickup","forge_open_roadster","forge_canopy_prototype","forge_three_wheeler",
            "forge_closed_supercar","forge_closed_rally","forge_closed_gt","forge_kestrel"};
        // The compact master's rendered envelope. Longer or taller generated bodies
        // (up to 3.8 m / 2.2 m) jammed the 24-car start, and shape must not change pace.
        // Width: the body's shoulders, not the mirrors (2.216 m incl. mirrors, then forced to 2.32 m, left visible gaps
        // at contact). The Puddle is 1.77 m across the body, 1.97 m across the tyres.
        public const float CollisionWidth=2.05f;
        // Each body's collision width: its shoulders and tyres, not its mirrors (the 99.5th percentile of vertex |x|
        // across the model, measured in Blender, +2 cm; review/map-sweep/contact-*). Length and height come from
        // the model itself (RoadCar.CollisionBox).
        static readonly System.Collections.Generic.Dictionary<string,float> Shoulders=new(){["hayate"]=2.197f,["kumo"]=2.197f,["fubuki"]=2.237f,["kaze"]=2.204f,["arashi"]=2.244f,["raijin"]=2.197f,["hoshi"]=2.204f,["sakura"]=2.177f,["yoru"]=2.15f,["tsuki"]=2.197f,["taiyo"]=2.197f,["hibari"]=2.15f,["tsubasa"]=2.13f,["kitsune"]=2.103f,["hachi"]=2.056f,["kogarashi"]=2.058f,["chibi"]=1.924f,["midori"]=2.15f,["shiro"]=2.204f,["nagare"]=2.192f,["kage"]=2.204f,["sabaku"]=2.267f,["yama"]=2.22f,["tanuki"]=1.999f,["tora"]=2.314f,["kaminari"]=2.254f,["kiri"]=2.104f,["regale"]=2.03f,["beetle"]=1.98f,["bramble"]=2.04f,["bubble"]=1.78f,["camper"]=2.08f,["comet"]=2.10f,["compact_v1"]=2.10f,["doodle"]=1.99f,["dover"]=2.01f,["gelato"]=2.01f,["heron"]=2.09f,["kipper"]=2.09f,["little_car"]=2.19f,["marlow"]=2.02f,["minibus"]=2.14f,["otter"]=2.06f,["pebble"]=2.03f,["picnic"]=2.03f,["postvan"]=2.09f,["puddle"]=1.97f,["rally"]=2.15f,["ritz"]=2.06f,["roadster"]=2.10f,["sprint"]=2.10f,["tallboy"]=1.98f,["taxi"]=2.02f,["zenith"]=2.20f,["cob"]=2.15f,["drover"]=2.13f,["scamp"]=2.07f,["fell"]=2.15f,["tumbler"]=2.07f,["hauler"]=2.07f,["pitgarage"]=2.18f,["pitparts"]=2.11f,["pitbody"]=2.11f};
        public static float? ShoulderWidth(string id)=>id!=null&&Shoulders.TryGetValue(id,out float w)?w:Forged(id)?.shoulder;
        // Bumper-to-bumper length of each model (Blender, +1 cm): the renderer bounds (LODs included) ran ~8 cm long.
        static readonly System.Collections.Generic.Dictionary<string,float> Lengths=new(){["hayate"]=3.927f,["kumo"]=4.007f,["fubuki"]=3.847f,["kaze"]=3.716f,["arashi"]=4.036f,["raijin"]=3.772f,["hoshi"]=4.062f,["sakura"]=3.942f,["yoru"]=3.916f,["tsuki"]=3.942f,["taiyo"]=3.996f,["hibari"]=3.84f,["tsubasa"]=3.36f,["kitsune"]=3.36f,["hachi"]=3.64f,["kogarashi"]=3.351f,["chibi"]=3.062f,["midori"]=3.796f,["shiro"]=4.15f,["nagare"]=4.197f,["kage"]=4.262f,["sabaku"]=4.047f,["yama"]=4.7f,["tanuki"]=3.607f,["tora"]=4.247f,["kaminari"]=3.678f,["kiri"]=4.167f,["regale"]=5.01f,["beetle"]=3.427f,["bramble"]=3.288f,["bubble"]=2.542f,["camper"]=4.152f,["comet"]=3.700f,["compact_v1"]=3.171f,["doodle"]=3.201f,["dover"]=3.780f,["gelato"]=3.920f,["heron"]=3.740f,["kipper"]=3.600f,["little_car"]=3.110f,["marlow"]=3.660f,["minibus"]=4.500f,["otter"]=3.640f,["pebble"]=3.167f,["picnic"]=3.590f,["postvan"]=3.600f,["puddle"]=3.193f,["rally"]=3.727f,["ritz"]=4.240f,["roadster"]=3.490f,["sprint"]=3.328f,["tallboy"]=3.240f,["taxi"]=3.740f,["zenith"]=3.790f,["cob"]=4.187f,["drover"]=4.097f,["scamp"]=3.427f,["fell"]=4.229f,["tumbler"]=3.967f,["hauler"]=3.579f,["pitgarage"]=4.33f,["pitparts"]=3.61f,["pitbody"]=3.59f};
        public static float? Length(string id)=>id!=null&&Lengths.TryGetValue(id,out float l)?l:Forged(id)?.length;
        // Car Forge bodies (docs/75 §4.8) carry their own data, written by the Forge beside the model: Cars/<id>_body.json.
        [System.Serializable] public sealed class ForgeData{public string id;public float shoulder,length,wheelFront,wheelRear,trackFront,trackRear;public float[] landmarks;public string livery;
            // (a roster car's place in fields, from its concept in car-forge: its star band, its surface, quick-looking)
            public int[] band;public string surface;public bool quick;}
        // --forge-roster (off by default): class C fields drawn from the Car Forge roster (car-forge docs/roster-c-design.md,
        // "How a field of 24 is filled"): every installed Forge body whose file names a star band.
        public static bool RosterEnabled=>RaceTrial.Has("--forge-roster");
        static string[] roster;
        public static string[] Roster
        {
            get {
                if(roster!=null)return roster;var list=new System.Collections.Generic.List<string>();
                foreach(var t in UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>("Cars")) {
                    if(!t.name.StartsWith("forge_")||!t.name.EndsWith("_body"))continue;string id=t.name.Substring(0,t.name.Length-5);
                    var d=Forged(id);if(d?.band!=null&&d.band.Length==2)list.Add(id);
                }
                list.Sort(StringComparer.Ordinal);return roster=list.ToArray();
            }
        }
        // A class C race's pools at a star level: quick (the quick-looking cars of the band: the front of the grid), home
        // (the rest of the band: the middle), veterans (cars whose band ends below: the back). Gravel races take gravel cars.
        // A thin pool borrows: quick from the quick cars below, home from the veterans, so no pool is ever empty.
        public static (string[] quick,string[] home,string[] veterans) RosterPools(int stars,bool gravel)
        {
            var quick=new System.Collections.Generic.List<string>();var home=new System.Collections.Generic.List<string>();var old=new System.Collections.Generic.List<string>();var oldQuick=new System.Collections.Generic.List<string>();
            foreach(var id in Roster) {
                var d=Forged(id);if((d.surface=="gravel")!=gravel)continue;
                int lo=d.band[0],hi=d.band[1];
                if(lo<=stars&&stars<=hi)(d.quick?quick:home).Add(id);
                else if(hi<stars){old.Add(id);if(d.quick)oldQuick.Add(id);}
            }
            if(quick.Count<3)quick.AddRange(oldQuick.Where(x=>!quick.Contains(x)));
            if(home.Count<8)home.AddRange(old.Where(x=>!home.Contains(x)&&!quick.Contains(x)));
            if(quick.Count==0)quick.AddRange(home);if(home.Count==0)home.AddRange(quick);
            if(old.Count==0)old.AddRange(home);
            return (quick.ToArray(),home.ToArray(),old.ToArray());
        }
        public const string RosterFlagship="forge_zenith";
        static readonly System.Collections.Generic.Dictionary<string,ForgeData> forged=new();
        public static ForgeData Forged(string id)
        {
            if(id==null)return null;if(forged.TryGetValue(id,out var d))return d;
            var text=UnityEngine.Resources.Load<UnityEngine.TextAsset>("Cars/"+id+"_body");
            d=text!=null?UnityEngine.JsonUtility.FromJson<ForgeData>(text.text):null;forged[id]=d;return d;
        }
        public static readonly UnityEngine.Bounds Envelope=new(new UnityEngine.Vector3(0,.845f,0),new UnityEngine.Vector3(CollisionWidth,1.69f,3.175f));
        // (The Regale too: the Duke races it, and you can win it and drive it anywhere, as every body shop body does.)
        // (and every Car Forge body: the roster races in fields, and its shape must not change its pace either, 17 Oct)
        public static bool UsesStandardEnvelope(string id)=>(id!=null&&id.StartsWith("forge_"))||id==Starter||id==Flagship||id==Prototype||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||Array.IndexOf(Townsfolk,id)>=0||Array.IndexOf(OffRoad,id)>=0||Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0;
        public static bool IsKnown(string id)=>(id!=null&&id.StartsWith("forge_")&&Forged(id)!=null)||Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0||Array.IndexOf(Teasers,id)>=0||Array.IndexOf(Owned,id)>=0||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||id==Flagship||Array.IndexOf(Townsfolk,id)>=0||Array.IndexOf(OffRoad,id)>=0||Array.IndexOf(PitStop,id)>=0;
        public static string Name(string id)=>id switch{
            "puddle"=>"Puddle starter","compact_v1"=>"Pocket compact",
            "pebble"=>"Pebble city car","tallboy"=>"Tallboy kei wagon","marlow"=>"Marlow saloon","dover"=>"Dover classic",
            "heron"=>"Heron estate","bramble"=>"Bramble hatch","picnic"=>"Picnic people carrier","kipper"=>"Kipper van",
            "doodle"=>"Doodle roll-top","otter"=>"Otter fastback","sprint"=>"Sprint hot hatch","comet"=>"Comet coupe",
            "rally"=>"Rally saloon","roadster"=>"Roadster hardtop","zenith"=>"Zenith GT",
            "regale"=>"Regale prototype","bubble"=>"Bubble micro","beetle"=>"Beetle bug","taxi"=>"Bayview taxi","camper"=>"Camper van","gelato"=>"Gelato van",
            "minibus"=>"Minibus","ritz"=>"Ritz tourer","postvan"=>"Post van",
            "cob"=>"Cob 4x4","drover"=>"Drover pickup","scamp"=>"Scamp 4x4","fell"=>"Fell wagon","tumbler"=>"Tumbler jeep","hauler"=>"Hauler kei truck","hayate"=>"Hayate GT-4","kumo"=>"Kumo Evo","fubuki"=>"Fubuki 22B","kaze"=>"Kaze Z","arashi"=>"Arashi Z","raijin"=>"Raijin RX","hoshi"=>"Hoshi 80","sakura"=>"Sakura S","yoru"=>"Yoru SX","tsuki"=>"Tsuki","taiyo"=>"Taiyo GT-4","hibari"=>"Hibari R","tsubasa"=>"Tsubasa R","kitsune"=>"Kitsune GTI-R","hachi"=>"Hachi","kogarashi"=>"Kogarashi","chibi"=>"Chibi","midori"=>"Midori MR","shiro"=>"Shiro GT","nagare"=>"Nagare GT-B","kage"=>"Kage","sabaku"=>"Sabaku Evo","yama"=>"Yama 80","tanuki"=>"Tanuki","tora"=>"Tora Runner","kaminari"=>"Kaminari","kiri"=>"Kiri","pitgarage"=>"Pit Stop recovery truck","pitparts"=>"Pit Stop parts van","pitbody"=>"Pit Stop panel truck",_=>id};
        // Pit Stop body shops: whole new models, looks only (every one races in the standard
        // envelope with the player's own fitted equipment). Bought once, switched at any garage bay.
        // Bayview (the tutorial town) has no body shop; Hillcrest sells everything, everyday cars
        // first and the Zenith GT last. `shop` names the town, ready for later towns' own ranges.
        public readonly struct Offer{public readonly string id,shop;public readonly int price;public Offer(string i,string s,int p){id=i;shop=s;price=p;}}
        public static readonly Offer[] ForSale={
            new("pebble","hillcrest",450),new("bubble","hillcrest",500),new("tallboy","hillcrest",550),new("marlow","hillcrest",600),new("beetle","hillcrest",650),
            new("dover","hillcrest",700),new("bramble","hillcrest",750),new("picnic","hillcrest",800),new("taxi","hillcrest",850),new("doodle","hillcrest",900),
            new("kipper","hillcrest",950),new("heron","hillcrest",1000),new("camper","hillcrest",1100),new("otter","hillcrest",1200),
            new("gelato","hillcrest",900),new("ritz","hillcrest",1300),new("sprint","hillcrest",1800),new("comet","hillcrest",2200),
            new("roadster","hillcrest",2600),new("rally","hillcrest",3000),new("zenith","hillcrest",4800),
            // Quarry Hills' body shop (Nia): the off-roaders.
            // The Tuner Garage (docs/67): every Tuner body, at every town's Pit Stop once class B is open.
            new("tsubasa","tuner",6000),new("kitsune","tuner",6500),new("sakura","tuner",7000),new("chibi","tuner",5500),new("kogarashi","tuner",6000),new("hachi","tuner",6500),new("tanuki","tuner",6000),new("kiri","tuner",6500),new("midori","tuner",7500),new("hibari","tuner",7500),new("yoru","tuner",8000),new("tsuki","tuner",8000),new("taiyo","tuner",8500),new("kaze","tuner",9000),new("raijin","tuner",9500),new("fubuki","tuner",9500),new("kumo","tuner",10000),new("nagare","tuner",9000),new("kage","tuner",9500),new("arashi","tuner",10000),new("hoshi","tuner",11000),new("hayate","tuner",12000),new("shiro","tuner",14000),new("sabaku","tuner",10000),new("yama","tuner",10500),new("tora","tuner",10000),new("kaminari","tuner",11000),
            new("scamp","quarry",900),new("tumbler","quarry",1100),new("hauler","quarry",1200),new("cob","quarry",1500),new("drover","quarry",1600),new("fell","quarry",1800)};
        public static int Price(string id){foreach(var o in ForSale)if(o.id==id)return o.price;return -1;}
        public static bool Sellable(string id)=>Price(id)>=0;
        // Bodies a save may hold: the starter and the compact plus everything sold.
        // (The Regale only by beating the Duke: AdventureProgress.Validate checks the trophy.)
        public static bool Ownable(string id)=>Array.IndexOf(Owned,id)>=0||Sellable(id)||id==Prototype||(Array.IndexOf(Forge,id)>=0&&AdventureSession.ForgeDrive!=null);
        // Deterministic draw from a deck: every model appears before any repeats,
        // and each race seed shuffles the order.
        public static string[] Deal(string[] models,int count,int seed)
        {
            var random=new Random(seed);var result=new string[count];var deck=(string[])models.Clone();
            for(int i=0;i<count;i++){
                int k=i%deck.Length;
                if(k==0)for(int j=deck.Length-1;j>0;j--){int swap=random.Next(j+1);(deck[j],deck[swap])=(deck[swap],deck[j]);}
                result[i]=deck[k];
            }
            return result;
        }
    }
    // Marks a car whose physics collider uses CarBodies.Envelope.
    public sealed class StandardEnvelope:UnityEngine.MonoBehaviour{}
}
