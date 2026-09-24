using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Stable IDs and tuning data are shared with the preserved game. Body art
    // remains independent of this loadout; no silhouette grants hidden stats.
    public static class EquipmentCatalog
    {
        [Serializable] public sealed class Part
        {
            public string id,slot,name,hint;
            public int price;
            public float power,torque,accel,power_limit,road,dirt,ceiling,pull,braking,response,yaw,mass;
        }
        [Serializable] public sealed class Data
        {
            public int schemaVersion;
            public string source;
            public string[] slots;
            public Part[] parts;
        }
        static Data data;
        static Dictionary<string,Part> byId;
        public static Data All {get {EnsureLoaded();return data;}}
        static void EnsureLoaded()
        {
            if(data!=null)return;
            var asset=Resources.Load<TextAsset>("Data/parts");
            if(asset==null)throw new InvalidOperationException("Missing original equipment catalogue");
            var candidate=JsonUtility.FromJson<Data>(asset.text);
            if(candidate==null || candidate.schemaVersion!=1 || candidate.slots==null || candidate.parts==null)
                throw new InvalidOperationException("Invalid equipment catalogue schema");
            var index=new Dictionary<string,Part>(StringComparer.Ordinal);
            var slots=new HashSet<string>(candidate.slots,StringComparer.Ordinal);
            foreach(var p in candidate.parts) {
                if(string.IsNullOrEmpty(p.id)||!slots.Contains(p.slot)||string.IsNullOrEmpty(p.name)||p.price<0||index.ContainsKey(p.id))
                    throw new InvalidOperationException("Invalid or duplicate part: "+p.id);
                index.Add(p.id,p);
            }
            foreach(string slot in slots)if(!index.ContainsKey(slot+"_stock")||index[slot+"_stock"].slot!=slot)
                throw new InvalidOperationException("Missing stock part for "+slot);
            data=candidate;byId=index;
        }
        public static Part Get(string id)
        {
            EnsureLoaded();if(id==null||!byId.TryGetValue(id,out var p))throw new ArgumentException("Unknown part ID: "+id);return p;
        }
        public static Dictionary<string,string> StockLoadout()
        {
            EnsureLoaded();var result=new Dictionary<string,string>();foreach(string slot in data.slots)result.Add(slot,slot+"_stock");return result;
        }
        public static Dictionary<string,string> TestLoadout(string name)
        {
            var fit=StockLoadout();
            if(name=="stock")return fit;
            if(name=="street") {
                fit["engine"]="engine_workshop";fit["turbo"]="turbo_street";fit["tyres"]="tyres_touring";
                fit["brakes"]="brakes_sport";fit["steering"]="steering_quick";
            } else if(name=="club") {
                fit["engine"]="engine_club";fit["turbo"]="turbo_club";fit["tyres"]="tyres_sport";
                fit["transmission"]="transmission_long";fit["brakes"]="brakes_race";
                fit["steering"]="steering_precision";fit["chassis"]="chassis_light";
            } else throw new ArgumentException("Unknown test loadout: "+name);
            return fit;
        }
        public static CarTuning Stats(IReadOnlyDictionary<string,string> fit)
        {
            EnsureLoaded();
            foreach(string slot in data.slots)
                if(!fit.TryGetValue(slot,out string id)||Get(id).slot!=slot)throw new ArgumentException("Missing or incompatible equipment: "+slot);
            var e=Get(fit["engine"]);var t=Get(fit["tyres"]);var g=Get(fit["transmission"]);
            var b=Get(fit["brakes"]);var s=Get(fit["steering"]);var c=Get(fit["chassis"]);var turbo=Get(fit["turbo"]);
            return CarTuning.Create(e.power_limit,e.accel,g.ceiling,g.pull,turbo.power,turbo.torque,t.road,t.dirt,b.braking,s.response,s.yaw,c.mass);
        }
        public static int Reward(int place,string raceClass="C")
        {
            if(place<1||place>24)return 0;
            int amount=place==1?500:place==2?400:place==3?330:place==4?280:place<=8?220:place<=12?170:place<=18?130:100;
            float multiplier=raceClass=="A"?3:raceClass=="B"?1.8f:1;
            return (int)(amount*multiplier);
        }
    }
}
