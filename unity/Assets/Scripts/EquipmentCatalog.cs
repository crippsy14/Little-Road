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
            public string id,slot,name,hint,shop;
            public string tier; // race class (RaceCalendar): C, B, A, S or E; empty means C
            public int price;
            public float power,torque,accel,power_limit,road,dirt,ceiling,pull,braking,response,yaw,mass;
            // Schema 2 (docs/47, tools/parts-ladder.py). Engine: real specs and a sound fingerprint.
            public string layout,firing,exhaust,intake,sound;public int cylinders,idleRpm,torqueRpm,powerRpm,redline,hp,nm;public float litres;public bool hybrid;
            // Induction: boost builds from spoolStart to spoolFull (fractions of the redline), lag seconds.
            public float spoolStart,spoolFull,lag;public bool blowoff,antilag,electric;
            // Gearbox: gears, first-to-top ratio spread, shift seconds, H/S/P/SL, revs at top speed x peak power.
            public int gears;public float spread,shiftTime,kmhPer1000;public string shiftType;public bool lsd;
        }
        // A class gate (docs/44, 47): base mass, drag and downforce; which class's parts fit.
        [Serializable] public sealed class Platform {public string id,tier,name;public int price;public float kg,drag,downforce;}
        [Serializable] public sealed class Data
        {
            public int schemaVersion;
            public string source;
            public string[] slots;
            public Part[] parts;
            public Platform[] platforms;
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
            if(candidate==null || candidate.schemaVersion!=2 || candidate.slots==null || candidate.parts==null)
                throw new InvalidOperationException("Invalid equipment catalogue schema");
            var index=new Dictionary<string,Part>(StringComparer.Ordinal);
            var slots=new HashSet<string>(candidate.slots,StringComparer.Ordinal);
            foreach(var p in candidate.parts) {
                if(string.IsNullOrEmpty(p.id)||!slots.Contains(p.slot)||string.IsNullOrEmpty(p.name)||p.price<0||index.ContainsKey(p.id)||RaceCalendar.Rank(p.tier)<0)
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
        // Auto-tune (docs/48): the final drive that puts top gear's redline just past the car's power-limited top speed
        // (balanced), shorter for acceleration, longer for top speed; the box's own spacing is kept.
        public enum TunePreset{Acceleration,Balanced,TopSpeed}
        public static GearTune AutoTune(IReadOnlyDictionary<string,string> fit,TunePreset preset=TunePreset.Balanced)
        {
            var train=Stats(fit).drivetrain;if(train==null)return null;
            float want=train.topSpeed*(preset==TunePreset.Acceleration?.97f:preset==TunePreset.TopSpeed?1.1f:1.02f);
            float final=Mathf.Clamp(train.RedlineSpeed/want,GearTune.FinalMin,GearTune.FinalMax);
            return new GearTune{transmission=fit["transmission"],final=final};
        }

        public static Dictionary<string,string> TestLoadout(string name)
        {
            var fit=StockLoadout();
            if(name=="stock")return fit;
            // An exact kit for tests and captures: "engine=engine_club,turbo=turbo_street" over the stock car.
            if(name.Contains("=")){foreach(var pair in name.Split(',')){var kv=pair.Split('=');if(kv.Length!=2||!fit.ContainsKey(kv[0].Trim()))throw new ArgumentException("Bad kit entry: "+pair);fit[kv[0].Trim()]=kv[1].Trim();Get(kv[1].Trim());}return fit;}
            if(name=="street") {
                fit["engine"]="engine_workshop";fit["turbo"]="turbo_street";fit["tyres"]="tyres_touring";
                fit["brakes"]="brakes_sport";fit["steering"]="steering_quick";
            } else if(name=="club") {
                fit["engine"]="engine_club";fit["turbo"]="turbo_club";fit["tyres"]="tyres_sport";
                fit["transmission"]="transmission_long";fit["brakes"]="brakes_race";
                fit["steering"]="steering_precision";fit["chassis"]="chassis_light";
            } else if(name=="valley") {
                fit["engine"]="engine_valley";fit["turbo"]="turbo_club";fit["tyres"]="tyres_grip";
                fit["transmission"]="transmission_long";fit["brakes"]="brakes_ceramic";
                fit["steering"]="steering_precision";fit["chassis"]="chassis_light";
            } else throw new ArgumentException("Unknown test loadout: "+name);
            return fit;
        }
        public static CarTuning Stats(IReadOnlyDictionary<string,string> fit)=>Stats(fit,null);
        // With a gearbox tune (docs/48); a tune for another gearbox is ignored.
        public static CarTuning Stats(IReadOnlyDictionary<string,string> fit,GearTune tune)
        {
            EnsureLoaded();
            foreach(string slot in data.slots)
                if(!fit.TryGetValue(slot,out string id)||Get(id).slot!=slot)throw new ArgumentException("Missing or incompatible equipment: "+slot);
            var e=Get(fit["engine"]);var t=Get(fit["tyres"]);var g=Get(fit["transmission"]);
            var b=Get(fit["brakes"]);var s=Get(fit["steering"]);var c=Get(fit["chassis"]);var turbo=Get(fit["turbo"]);
            if(LegacyDrivetrain)return CarTuning.Create(e.power_limit,e.accel,g.ceiling,g.pull,turbo.power,turbo.torque,t.road,t.dirt,b.braking,s.response,s.yaw,c.mass);
            // Real gears (docs/47): no gearbox ceiling or pull; the gears come from the engine, turbo and box.
            var tuning=CarTuning.Create(e.power_limit,e.accel,999,1,turbo.power,turbo.torque,t.road,t.dirt,b.braking,s.response,s.yaw,c.mass);
            // Calibration: real gear gaps and shift cuts cost a stepped box some pace against the old gearless
            // curve; 10% more power keeps class C near its old standing-start times (docs/47).
            tuning.power*=PowerCalibration;
            if(tune!=null&&(tune.transmission!=g.id||!tune.Valid))tune=null;
            string key=e.id+"|"+turbo.id+"|"+g.id+"|"+c.id+(tune==null?"":"|"+tune.final.ToString("0.000")+"|"+string.Join(",",tune.gears??new float[0]));
            if(!drivetrains.TryGetValue(key,out var train)){train=new Drivetrain(e,turbo,g,tuning,tune?.final??1,tune?.gears);drivetrains.Add(key,train);}
            tuning.drivetrain=train;tuning.gearCeiling=train.RedlineSpeed;return tuning;
        }
        // One drivetrain per engine/turbo/box/frame, so repeated Stats() calls compare equal.
        static readonly Dictionary<string,Drivetrain> drivetrains=new(StringComparer.Ordinal);
        public const float PowerCalibration=1.1f;
        // --legacy-drivetrain: the old model (power cap, gearbox ceiling and pull), for comparison races.
        public static bool LegacyDrivetrain=>Array.IndexOf(Environment.GetCommandLineArgs(),"--legacy-drivetrain")>=0;
        public static int Reward(int place,string raceClass="C")
        {
            if(place<1||place>24)return 0;
            int amount=place==1?500:place==2?400:place==3?330:place==4?280:place<=8?220:place<=12?170:place<=18?130:100;
            float multiplier=raceClass=="A"?3:raceClass=="B"?1.8f:1;
            return (int)(amount*multiplier);
        }
    }
}
