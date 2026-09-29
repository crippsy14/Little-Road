using System;

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
        // Hillcrest townsfolk: free-roam residents only, never dealt into race fields.
        public static readonly string[] Townsfolk={"bubble","beetle","taxi","camper","gelato","minibus","ritz","postvan"};
        // The compact master's rendered envelope. Longer or taller generated bodies
        // (up to 3.8 m / 2.2 m) jammed the 24-car start, and shape must not change pace.
        // Width: the body's shoulders, not the mirrors (2.216 m incl. mirrors, then forced to 2.32 m, left visible gaps
        // at contact). The Puddle is 1.77 m across the body, 1.97 m across the tyres.
        public const float CollisionWidth=2.05f;
        // Each body's collision width: its shoulders and tyres, not its mirrors (the 99.5th percentile of vertex |x|
        // across the model, measured in Blender, +2 cm; review/map-sweep/contact-*). Length and height come from
        // the model itself (RoadCar.CollisionBox).
        static readonly System.Collections.Generic.Dictionary<string,float> Shoulders=new(){["beetle"]=1.98f,["bramble"]=2.04f,["bubble"]=1.78f,["camper"]=2.08f,["comet"]=2.10f,["compact_v1"]=2.10f,["doodle"]=1.99f,["dover"]=2.01f,["gelato"]=2.01f,["heron"]=2.09f,["kipper"]=2.09f,["little_car"]=2.19f,["marlow"]=2.02f,["minibus"]=2.14f,["otter"]=2.06f,["pebble"]=2.03f,["picnic"]=2.03f,["postvan"]=2.09f,["puddle"]=1.97f,["rally"]=2.15f,["ritz"]=2.06f,["roadster"]=2.10f,["sprint"]=2.10f,["tallboy"]=1.98f,["taxi"]=2.02f,["zenith"]=2.20f};
        public static float? ShoulderWidth(string id)=>id!=null&&Shoulders.TryGetValue(id,out float w)?w:null;
        // Bumper-to-bumper length of each model (Blender, +1 cm): the renderer bounds (LODs included) ran ~8 cm long.
        static readonly System.Collections.Generic.Dictionary<string,float> Lengths=new(){["beetle"]=3.427f,["bramble"]=3.288f,["bubble"]=2.542f,["camper"]=4.152f,["comet"]=3.700f,["compact_v1"]=3.171f,["doodle"]=3.201f,["dover"]=3.780f,["gelato"]=3.920f,["heron"]=3.740f,["kipper"]=3.600f,["little_car"]=3.110f,["marlow"]=3.660f,["minibus"]=4.500f,["otter"]=3.640f,["pebble"]=3.167f,["picnic"]=3.590f,["postvan"]=3.600f,["puddle"]=3.193f,["rally"]=3.727f,["ritz"]=4.240f,["roadster"]=3.490f,["sprint"]=3.328f,["tallboy"]=3.240f,["taxi"]=3.740f,["zenith"]=3.790f};
        public static float? Length(string id)=>id!=null&&Lengths.TryGetValue(id,out float l)?l:null;
        public static readonly UnityEngine.Bounds Envelope=new(new UnityEngine.Vector3(0,.845f,0),new UnityEngine.Vector3(CollisionWidth,1.69f,3.175f));
        public static bool UsesStandardEnvelope(string id)=>id==Starter||id==Flagship||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||Array.IndexOf(Townsfolk,id)>=0;
        public static bool IsKnown(string id)=>Array.IndexOf(Owned,id)>=0||Array.IndexOf(Family,id)>=0||Array.IndexOf(Sporty,id)>=0||id==Flagship||Array.IndexOf(Townsfolk,id)>=0;
        public static string Name(string id)=>id switch{
            "puddle"=>"Puddle starter","compact_v1"=>"Pocket compact",
            "pebble"=>"Pebble city car","tallboy"=>"Tallboy kei wagon","marlow"=>"Marlow saloon","dover"=>"Dover classic",
            "heron"=>"Heron estate","bramble"=>"Bramble hatch","picnic"=>"Picnic people carrier","kipper"=>"Kipper van",
            "doodle"=>"Doodle roll-top","otter"=>"Otter fastback","sprint"=>"Sprint hot hatch","comet"=>"Comet coupe",
            "rally"=>"Rally saloon","roadster"=>"Roadster hardtop","zenith"=>"Zenith GT",
            "bubble"=>"Bubble micro","beetle"=>"Beetle bug","taxi"=>"Bayview taxi","camper"=>"Camper van","gelato"=>"Gelato van",
            "minibus"=>"Minibus","ritz"=>"Ritz tourer","postvan"=>"Post van",_=>id};
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
            new("roadster","hillcrest",2600),new("rally","hillcrest",3000),new("zenith","hillcrest",4800)};
        public static int Price(string id){foreach(var o in ForSale)if(o.id==id)return o.price;return -1;}
        public static bool Sellable(string id)=>Price(id)>=0;
        // Bodies a save may hold: the starter and the compact plus everything sold.
        public static bool Ownable(string id)=>Array.IndexOf(Owned,id)>=0||Sellable(id);
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
