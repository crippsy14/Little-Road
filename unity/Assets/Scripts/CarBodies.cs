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
        // Roster cars removed and replaced in their slot (24 Oct: the Patchwork, whose identity was all paint, by the Tubby,
        // built from scratch to its brief); a save that owns or drives one has the replacement instead.
        public static readonly System.Collections.Generic.Dictionary<string,string> Replaced=new(){["forge_patchwork"]="forge_snoozer"};
        // Special models: never sold or raced in fields, only won (Richard, 24 Oct: the Tubby, a bath on wheels, is the prize
        // of a water-based activity to come). A save may own one once it is won.
        public static readonly string[] Prizes={"forge_tubby"};
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
        public static string Class(string id)=>id!=null&&id.StartsWith("forge_")&&Forged(id)?.band!=null?Forged(id).Class:id==Prototype?"E":Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0?"B":"C";
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
        [System.Serializable] public sealed class ForgeData{public string id;public float shoulder,length,wheelFront,wheelRear,trackFront,trackRear,top,camber;public float[] landmarks;public string livery;
            // (a roster car's place in fields, from its concept in car-forge: its star band, its surface, quick-looking)
            public int[] band;public string surface;public bool quick;
            // (and its class: C, or B for the class B roster, docs/roster-b-design.md; files from before 17 Oct have none: C)
            public string cls;public string Class=>string.IsNullOrEmpty(cls)?"C":cls;}
        // The Forge roster (on by default since 20 Oct; --no-forge-roster for the old fields): class C and class B fields drawn from the Car Forge rosters (car-forge
        // docs/roster-c-design.md, "How a field of 24 is filled", and docs/roster-b-design.md): every installed Forge body
        // whose file names a star band, by its class.
        public static bool RosterEnabled=>!RaceTrial.Has("--no-forge-roster");   // (class B's roster is withdrawn for now: tools/forge-cars.txt)
        static string[] rosterAll;
        static readonly System.Collections.Generic.Dictionary<string,string[]> rosters=new();
        static string[] RosterAll
        {
            get {
                if(rosterAll!=null)return rosterAll;var list=new System.Collections.Generic.List<string>();
                foreach(var t in UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>("Cars")) {
                    if(!t.name.StartsWith("forge_")||!t.name.EndsWith("_body"))continue;string id=t.name.Substring(0,t.name.Length-5);
                    var d=Forged(id);if(d?.band!=null&&d.band.Length==2)list.Add(id);
                }
                list.Sort(StringComparer.Ordinal);return rosterAll=list.ToArray();
            }
        }
        public static string[] Roster=>RosterOf("C");
        public static string[] RosterOf(string cls)
        {
            if(rosters.TryGetValue(cls,out var r))return r;
            return rosters[cls]=RosterAll.Where(id=>Forged(id).Class==cls).ToArray();
        }
        // A class C race's pools at a star level: quick (the quick-looking cars of the band: the front of the grid), home
        // (the rest of the band: the middle), veterans (cars whose band ends below: the back). Gravel races take gravel cars.
        // A thin pool borrows: quick from the quick cars below, home from the veterans, so no pool is ever empty. A car of
        // surface "both" (the rally cars) races on either.
        public static (string[] quick,string[] home,string[] veterans) RosterPools(int stars,bool gravel,string cls="C")
        {
            var quick=new System.Collections.Generic.List<string>();var home=new System.Collections.Generic.List<string>();var old=new System.Collections.Generic.List<string>();var oldQuick=new System.Collections.Generic.List<string>();
            foreach(var id in RosterOf(cls)) {
                var d=Forged(id);if(!RacesOn(d,gravel))continue;
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
        public static bool RacesOn(ForgeData d,bool gravel)=>d.surface=="both"||(d.surface=="gravel")==gravel;
        // The cars of the surface whose band starts above this star: a thin field's last resort (bodies never change pace).
        public static string[] RosterSpare(int stars,bool gravel,string cls="C")=>RosterOf(cls).Where(id=>{var d=Forged(id);return RacesOn(d,gravel)&&d.band[0]>stars;}).ToArray();
        public const string RosterFlagship="forge_zenith";
        static readonly System.Collections.Generic.Dictionary<string,ForgeData> forged=new();
        public static ForgeData Forged(string id)
        {
            if(id==null)return null;if(forged.TryGetValue(id,out var d))return d;
            var text=UnityEngine.Resources.Load<UnityEngine.TextAsset>("Cars/"+id+"_body");
            d=text!=null?UnityEngine.JsonUtility.FromJson<ForgeData>(text.text):null;forged[id]=d;return d;
        }
        // The Car Forge models everywhere (23 Oct, with the Forge roster; --no-forge-roster brings the old models back): every
        // older class C body shows as its Forge namesake (the compact as the Pocket) in traffic, parked, as residents, in
        // fields and in the player's own garage (TrialWorld.Car). Bodies with no namesake (the Tuners, the Pit Stop's own
        // vehicles, the Regale) stay as they are.
        public static string Look(string id)
        {
            if(!RosterEnabled||id==null||id.StartsWith("forge_"))return id;
            string forge=id=="compact_v1"?"forge_pocket":"forge_"+id;
            return Forged(forge)?.band!=null?forge:id;
        }
        // A world pool (traffic, parked cars, residents) with the Forge roster's own characters added, the ones with no older
        // model (the Chippy, the Gold Top, the Ladders...): Look shows them as their Forge models. Without the roster, the old pool.
        public static string[] WithForge(string[] old,params string[] characters)=>RosterEnabled?old.Concat(characters).ToArray():old;
        // Quarry Hills' working vehicles: the off-roaders, and with the roster the Haybale and the Coastguard.
        public static string[] OffRoadFolk=>offRoadFolk??=WithForge(OffRoad,"haybale","coastguard","sandhopper");static string[] offRoadFolk;
        public const string ForgeStarter="forge_puddle";
        public static string StarterBody=>RosterEnabled?ForgeStarter:Starter;
        // Each roster car's name, what it is, and its signature (car-forge designs/roster/*.yaml, the concept's first lines).
        static readonly System.Collections.Generic.Dictionary<string,(string name,string what,string signature)> ForgeInfo=new(){
            ["forge_beetle"]=("Beetle bug","a round-backed bug, engine in the back","its round back"),
            ["forge_bramble"]=("Bramble hatch","the everyday hatch: chrome bumpers, hubcaps, a cheerful face","the cheerful round-eyed face of every high street"),
            ["forge_bramble_sport"]=("Bramble Sport","the Bramble in warm trim: stripes, a scoop, a little wing, slotted wheels","twin stripes over the cheerful face"),
            ["forge_bubble"]=("Bubble micro","a bubble car: tiny, round and tall","the smallest, roundest thing racing"),
            ["forge_camper"]=("Camper van","a split-screen camper, two-tone, curtains in the windows","the cream roof over a pastel body"),
            ["forge_chippy"]=("Chippy","a seaside chip van with a fryer chimney on the roof","the chimney"),
            ["forge_coastguard"]=("Coastguard","a coastguard 4x4: light bar, beacon, rope coils","the light bar"),
            ["forge_cob"]=("Cob 4x4","a boxy utility 4x4","square everything"),
            ["forge_cob_rally"]=("Cob Rally","the Cob 4x4 as a raid truck: spot lamps, a roof rack, flared arches","a 4x4 with four spot lamps"),
            ["forge_comet"]=("Comet coupe","a sporty little coupe","a low, sharp roof"),
            ["forge_doodle"]=("Doodle roll-top","a little open car with its canvas roof up, like a pram hood","the fabric hood"),
            ["forge_dover"]=("Dover classic","a classic saloon: chrome, an upright grille, round lamps","its tall chrome grille"),
            ["forge_drover"]=("Drover pickup","a farm-ready pickup","a bull bar and a muddy bed"),
            ["forge_fell"]=("Fell wagon","a long-wheelbase 4x4 wagon with a snorkel","the snorkel up the pillar"),
            ["forge_fell_rally"]=("Fell Rally","the Fell wagon as a stage car: spot lamps on the roof, mud flaps, flares","a pod of lamps on a tall roof"),
            ["forge_gelato"]=("Gelato van","the ice cream van: a giant cone on the roof, pastel two-tone, chrome bumpers","a giant ice cream on the roof"),
            ["forge_gold_top"]=("Gold Top","a milk float: an open pickup bed stacked with crates of bottles","crates of milk in the back"),
            ["forge_hauler"]=("Hauler kei truck","a kei truck with a drop-side bed","a toy-sized pickup"),
            ["forge_haybale"]=("Haybale","a farm pickup with a hay bale in the bed and mud up the sides","the round bale"),
            ["forge_heron"]=("Heron estate","a long family estate with roof rails and a dog guard","its long roof and rails"),
            ["forge_heron_rally"]=("Heron Rally","the Heron estate built for the stages: spot lamps, flares, a roof spare","a rally estate with its spare on the roof"),
            ["forge_hookline"]=("Hookline","a little breakdown truck with a tow crane and an amber beacon","the crane on the back"),
            ["forge_kestrel"]=("Kestrel Turbo","an 80s rally hatch: flares, a roof spare, grille spot lamps","the roof spare and the flares"),
            ["forge_kipper"]=("Kipper van","a little fishmonger's panel van","a fish sign on the roof"),
            ["forge_l_plate"]=("L-Plate","a driving-school hatch with a roof sign","the school sign on its roof"),
            ["forge_ladders"]=("Ladders","a window cleaner's kei van with ladders on the roof","the ladders"),
            ["forge_marlow"]=("Marlow saloon","a sensible three-box family saloon","the most ordinary car in Bayview, proudly"),
            ["forge_merlin"]=("Merlin","a family saloon in rally trim: the Kestrel's big brother","four spot lamps across the grille and a roof spare"),
            ["forge_minibus"]=("Minibus","a twelve-seat minibus in school colours","the longest thing in class C"),
            ["forge_osprey"]=("Osprey","a rally estate with a roof box and long-range lamps","the roof box"),
            ["forge_otter"]=("Otter fastback","a fastback family coupe, sleek in a sensible way","its long sloping back"),
            ["forge_otter_gt"]=("Otter GT","the Otter fastback in warm trim: stripes, a scoop, a wing, slotted wheels","twin stripes down a long fastback"),
            ["forge_pebble"]=("Pebble","a round city car, all wheels and windows","a body like a pebble on a beach"),
            ["forge_picnic"]=("Picnic","a people carrier with a picnic hamper on the roof rack","the wicker hamper on top"),
            ["forge_pocket"]=("Pocket compact","a tiny two-door city runabout","the shortest car on the grid"),
            ["forge_postvan"]=("Post van","a red post van","post-box red with a black roof"),
            ["forge_pronto"]=("Pronto","a pizza delivery microcar with a hot box on the back","the hot box"),
            ["forge_puddle"]=("Puddle","the starter: an eager little hatch that sits up like a puppy waiting for a walk","big round eyes and a contented smile"),
            ["forge_rally"]=("Rally saloon","the rally saloon in full stage kit","four spot lamps and a roof spare"),
            ["forge_rattler"]=("Rattler","a rusty estate: a primer door, a bungeed tailgate, one hubcap missing","a primer-grey door on a faded green estate"),
            ["forge_ritz"]=("Ritz tourer","a grand old tourer with a long bonnet, polished to a shine","its long chrome-nosed bonnet"),
            ["forge_roadster"]=("Roadster","an open roadster, low and lean","the low open cockpit"),
            ["forge_sandhopper"]=("Sandhopper","a beach buggy: open-topped, a surfboard on the back, fat tyres","the surfboard"),
            ["forge_scamp"]=("Scamp 4x4","a tiny 4x4 with big wheels","big wheels on a tiny body"),
            ["forge_shedbuilt"]=("Shedbuilt special","a home-built special: an open body, mismatched wheels, a roll bar of scaffold pole","the scaffold-pole roll bar"),
            ["forge_sprint"]=("Sprint hot hatch","the hot hatch: wide arches, fat wheels, a wing","the flared arches"),
            ["forge_stock_banger"]=("Stock Banger","a banger racer: a painted number, dents everywhere, but quick","battered but first into the corner"),
            ["forge_sunday_best"]=("Sunday Best","grandad's saloon: immaculate, chrome, a tow ball","the shine"),
            ["forge_tallboy"]=("Tallboy","a tall, narrow kei wagon, nearly as high as it is long","its height"),
            ["forge_taxi"]=("Bayview taxi","a black-cab-style town taxi","the TAXI sign on its roof"),
            ["forge_snoozer"]=("Snoozer","a sleepy old family saloon that never quite wakes up","a sway-backed body and heavy-lidded lamps"),
            ["forge_tubby"]=("Tubby","a cast-iron roll-top bath on a go-kart chassis: a special, won on the water","a bath on wheels"),
            ["forge_tumbler"]=("Tumbler jeep","an open jeep with a roll bar","the roll bar over the open seats"),
            ["forge_woody"]=("Woody","a wood-panelled estate","the wooden sides"),
            ["forge_wren"]=("Wren","a three-wheeler delivery van","one wheel at the front"),
            ["forge_zenith"]=("Zenith GT","the flagship: the fastest-looking car in class C","low, wide and winged")};
        public static string About(string id)=>id!=null&&ForgeInfo.TryGetValue(id,out var f)?f.what:null;
        public static string Signature(string id)=>id!=null&&ForgeInfo.TryGetValue(id,out var f)?f.signature:null;
        public static readonly UnityEngine.Bounds Envelope=new(new UnityEngine.Vector3(0,.845f,0),new UnityEngine.Vector3(CollisionWidth,1.69f,3.175f));
        // (The Regale too: the Duke races it, and you can win it and drive it anywhere, as every body shop body does.)
        // (and every Car Forge body: the roster races in fields, and its shape must not change its pace either, 17 Oct)
        public static bool UsesStandardEnvelope(string id)=>(id!=null&&id.StartsWith("forge_"))||id==Starter||id==Flagship||id==Prototype||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||Array.IndexOf(Townsfolk,id)>=0||Array.IndexOf(OffRoad,id)>=0||Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0;
        public static bool IsKnown(string id)=>(id!=null&&id.StartsWith("forge_")&&Forged(id)!=null)||Array.IndexOf(Tuner,id)>=0||Array.IndexOf(TunerOffRoad,id)>=0||Array.IndexOf(Teasers,id)>=0||Array.IndexOf(Owned,id)>=0||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||id==Flagship||Array.IndexOf(Townsfolk,id)>=0||Array.IndexOf(OffRoad,id)>=0||Array.IndexOf(PitStop,id)>=0;
        public static string Name(string id)=>id!=null&&ForgeInfo.TryGetValue(id,out var f)?f.name:id switch{
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
        public static Offer[] ForSale=>RosterEnabled?ForgeSale:OldSale;
        // The Car Forge range (23 Oct): every old body's namesake at its old price, and the roster's new cars priced by star
        // band (a car that races 7-10★ costs about what the Rally saloon did). Hillcrest sells the tarmac and rally cars,
        // Quarry Hills the gravel ones; the Tuner Garage's class B range is unchanged until class B's Forge roster.
        static Offer[] forgeSale;
        static Offer[] ForgeSale=>forgeSale??=ForgeRange.Where(o=>o.shop=="tuner"||Forged(o.id)?.band!=null).ToArray();
        static readonly Offer[] ForgeRange={
            new("forge_pocket","hillcrest",400),new("forge_rattler","hillcrest",400),new("forge_pebble","hillcrest",450),new("forge_snoozer","hillcrest",450),
            new("forge_bubble","hillcrest",500),new("forge_gold_top","hillcrest",500),new("forge_l_plate","hillcrest",500),new("forge_pronto","hillcrest",550),
            new("forge_tallboy","hillcrest",550),new("forge_marlow","hillcrest",600),new("forge_wren","hillcrest",600),new("forge_ladders","hillcrest",600),
            new("forge_beetle","hillcrest",650),new("forge_dover","hillcrest",700),new("forge_postvan","hillcrest",700),new("forge_chippy","hillcrest",750),
            new("forge_bramble","hillcrest",750),new("forge_stock_banger","hillcrest",800),new("forge_picnic","hillcrest",800),new("forge_taxi","hillcrest",850),
            new("forge_shedbuilt","hillcrest",850),new("forge_doodle","hillcrest",900),new("forge_gelato","hillcrest",900),new("forge_hookline","hillcrest",950),
            new("forge_kipper","hillcrest",950),new("forge_sunday_best","hillcrest",950),new("forge_heron","hillcrest",1000),new("forge_minibus","hillcrest",1000),
            new("forge_camper","hillcrest",1100),new("forge_otter","hillcrest",1200),new("forge_woody","hillcrest",1250),new("forge_ritz","hillcrest",1300),
            new("forge_bramble_sport","hillcrest",1500),new("forge_sprint","hillcrest",1800),new("forge_comet","hillcrest",2200),new("forge_kestrel","hillcrest",2400),
            new("forge_merlin","hillcrest",2500),new("forge_roadster","hillcrest",2600),new("forge_osprey","hillcrest",2800),new("forge_rally","hillcrest",3000),
            new("forge_heron_rally","hillcrest",3200),new("forge_otter_gt","hillcrest",3400),new("forge_zenith","hillcrest",4800),
            new("forge_scamp","quarry",900),new("forge_haybale","quarry",1000),new("forge_tumbler","quarry",1100),new("forge_hauler","quarry",1200),
            new("forge_sandhopper","quarry",1300),new("forge_cob","quarry",1500),new("forge_drover","quarry",1600),new("forge_coastguard","quarry",1700),
            new("forge_fell","quarry",1800),new("forge_cob_rally","quarry",2600),new("forge_fell_rally","quarry",2900),
            new("tsubasa","tuner",6000),new("kitsune","tuner",6500),new("sakura","tuner",7000),new("chibi","tuner",5500),new("kogarashi","tuner",6000),new("hachi","tuner",6500),new("tanuki","tuner",6000),new("kiri","tuner",6500),new("midori","tuner",7500),new("hibari","tuner",7500),new("yoru","tuner",8000),new("tsuki","tuner",8000),new("taiyo","tuner",8500),new("kaze","tuner",9000),new("raijin","tuner",9500),new("fubuki","tuner",9500),new("kumo","tuner",10000),new("nagare","tuner",9000),new("kage","tuner",9500),new("arashi","tuner",10000),new("hoshi","tuner",11000),new("hayate","tuner",12000),new("shiro","tuner",14000),new("sabaku","tuner",10000),new("yama","tuner",10500),new("tora","tuner",10000),new("kaminari","tuner",11000)};
        // The older models' range (--no-forge-roster).
        static readonly Offer[] OldSale={
            new("pebble","hillcrest",450),new("bubble","hillcrest",500),new("tallboy","hillcrest",550),new("marlow","hillcrest",600),new("beetle","hillcrest",650),
            new("dover","hillcrest",700),new("bramble","hillcrest",750),new("picnic","hillcrest",800),new("taxi","hillcrest",850),new("doodle","hillcrest",900),
            new("kipper","hillcrest",950),new("heron","hillcrest",1000),new("camper","hillcrest",1100),new("otter","hillcrest",1200),
            new("gelato","hillcrest",900),new("ritz","hillcrest",1300),new("sprint","hillcrest",1800),new("comet","hillcrest",2200),
            new("roadster","hillcrest",2600),new("rally","hillcrest",3000),new("zenith","hillcrest",4800),
            // Quarry Hills' body shop (Nia): the off-roaders.
            // The Tuner Garage (docs/67): every Tuner body, at every town's Pit Stop once class B is open.
            new("tsubasa","tuner",6000),new("kitsune","tuner",6500),new("sakura","tuner",7000),new("chibi","tuner",5500),new("kogarashi","tuner",6000),new("hachi","tuner",6500),new("tanuki","tuner",6000),new("kiri","tuner",6500),new("midori","tuner",7500),new("hibari","tuner",7500),new("yoru","tuner",8000),new("tsuki","tuner",8000),new("taiyo","tuner",8500),new("kaze","tuner",9000),new("raijin","tuner",9500),new("fubuki","tuner",9500),new("kumo","tuner",10000),new("nagare","tuner",9000),new("kage","tuner",9500),new("arashi","tuner",10000),new("hoshi","tuner",11000),new("hayate","tuner",12000),new("shiro","tuner",14000),new("sabaku","tuner",10000),new("yama","tuner",10500),new("tora","tuner",10000),new("kaminari","tuner",11000),
            new("scamp","quarry",900),new("tumbler","quarry",1100),new("hauler","quarry",1200),new("cob","quarry",1500),new("drover","quarry",1600),new("fell","quarry",1800)};
        // (either range: a save keeps every body it bought, whichever models the game shows)
        public static int Price(string id){foreach(var o in ForSale)if(o.id==id)return o.price;foreach(var o in RosterEnabled?OldSale:ForgeRange)if(o.id==id)return o.price;return -1;}
        public static bool Sellable(string id)=>Price(id)>=0;
        // Bodies a save may hold: the starter and the compact plus everything sold.
        // (The Regale only by beating the Duke: AdventureProgress.Validate checks the trophy.)
        public static bool Ownable(string id)=>Array.IndexOf(Owned,id)>=0||id==ForgeStarter||Sellable(id)||id==Prototype||Array.IndexOf(Prizes,id)>=0||(Array.IndexOf(Forge,id)>=0&&AdventureSession.ForgeDrive!=null);
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
