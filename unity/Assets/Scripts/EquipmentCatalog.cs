using System;
using System.Linq;
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
            // Discipline (Richard, 2 Oct): road, rally or ice; empty means road. Rally and ice parts are planned; the parts
            // shop already filters by it (its Road / Rally / Ice pills show the others as coming soon).
            public string discipline;
            // An unlock condition (PartDiscovery): "wins:5", "podiums:B:3", "class:A"; empty for none.
            public string unlock;
            public string Discipline=>string.IsNullOrEmpty(discipline)?"road":discipline;
            public int price;
            public float power,torque,accel,power_limit,road,dirt,ceiling,pull,braking,response,yaw,mass;
            // Schema 2 (docs/47, tools/parts-ladder.py). Engine: real specs and a sound fingerprint.
            public string layout,firing,exhaust,intake,sound;public int cylinders,idleRpm,torqueRpm,powerRpm,redline,hp,nm;public float litres;public bool hybrid;
            // A hybrid engine's e-motor and battery (HybridPower; only with --hybrid): motor kW, pack kJ, regen kW, and the
            // system's weight as a share of the car's (added to the frame's mass).
            public float motorKw,batteryKj,regenKw,hybridMass;
            // Induction: boost builds from spoolStart to spoolFull (fractions of the redline), lag seconds.
            public float spoolStart,spoolFull,lag;public bool blowoff,antilag,electric;
            // Gearbox: gears, first-to-top ratio spread, shift seconds, H/S/P/SL, revs at top speed x peak power.
            public int gears;public float spread,shiftTime,kmhPer1000;public string shiftType;public bool lsd;
        }
        // A class gate (docs/44, 47): base mass, drag and downforce; which class's parts fit.
        [Serializable] public sealed class Platform {public string id,tier,name;public int price;public float kg,drag,downforce,firm;}
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
            // The hybrid prototype's parts (an e-motor) exist only with --hybrid: without it the catalogue is as before.
            if(!HybridPower.Enabled)candidate.parts=System.Array.FindAll(candidate.parts,p=>p.motorKw<=0);
            foreach(var p in candidate.parts) {
                if(string.IsNullOrEmpty(p.id)||!slots.Contains(p.slot)||string.IsNullOrEmpty(p.name)||p.price<0||index.ContainsKey(p.id)||RaceCalendar.Rank(p.tier)<0)
                    throw new InvalidOperationException("Invalid or duplicate part: "+p.id);
                index.Add(p.id,p);
            }
            foreach(string slot in slots)if(!index.ContainsKey(slot+"_stock")||index[slot+"_stock"].slot!=slot)
                throw new InvalidOperationException("Missing stock part for "+slot);
            data=candidate;byId=index;
        }
        public static bool Has(string id)=>!string.IsNullOrEmpty(id)&&All.parts.Any(p=>p.id==id);
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

        // The class B ladder, Good/Better/Best per slot (parts.json; the Better and Best unlock with stars won).
        public static readonly string[] TunerOrder={"tyres","brakes","engine","steering","turbo","chassis","transmission"};
        public static readonly Dictionary<string,string[]> TunerSteps=new(){
            {"engine",new[]{"engine_b_twincam","engine_b_highcomp","engine_b_v6"}},{"turbo",new[]{"turbo_b_single","turbo_b_twinscroll","turbo_b_twin"}},
            {"tyres",new[]{"tyres_b_sport","tyres_b_performance","tyres_b_semislick"}},{"transmission",new[]{"transmission_b_six","transmission_b_shortsix","transmission_b_lsd"}},
            {"brakes",new[]{"brakes_b_drilled","brakes_b_braided","brakes_b_carbon"}},{"steering",new[]{"steering_b_quick","steering_b_rally","steering_b_wheel"}},
            {"chassis",new[]{"chassis_b_strut","chassis_b_halfcage","chassis_b_carbon"}}};
        public static Dictionary<string,string> TestLoadout(string name)
        {
            var fit=StockLoadout();
            if(name=="stock")return fit;
            if(name=="max")return MaxBuild();   // the Duke's Challenge build (docs/54)
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
            } else if(name.Length>=2&&name[0]=='b'&&int.TryParse(name.Substring(1),out int stars)&&stars>=1&&stars<=10) {
                // Class B fields by stars (docs/67): Good in every slot at 1, then about two slots a step up per star
                // in the ladder's order (tyres and brakes first, the gearbox last): all Better at 5, the podium build
                // (Best in four) at 7, all Best from 9.
                int ups=new[]{0,2,4,6,7,9,11,13,14,14}[stars-1];
                for(int i=0;i<TunerOrder.Length;i++){string slot=TunerOrder[i];int step=(ups>i?1:0)+(ups>i+7?1:0);fit[slot]=TunerSteps[slot][step];}
            } else throw new ArgumentException("Unknown test loadout: "+name);
            return fit;
        }
        // The maximum build (the Duke's Challenge, docs/54): the parts, of every class, that lap Hillcrest Raceway
        // fastest by SpeedProfile (the ideal lap: grip, power, braking and drag, no driver). Coordinate descent from the
        // dearest E-class part in each slot, a slot at a time, until nothing changes. "The best part" depends on the
        // track (a long box or a short one, qualifiers or hard tyres), so it is measured, not picked by price.
        static Dictionary<string,string> maxBuild;public static float MaxBuildLap{get;private set;}
        public static Dictionary<string,string> MaxBuild()
        {
            if(maxBuild!=null)return new Dictionary<string,string>(maxBuild);
            EnsureLoaded();
            var fit=StockLoadout();
            foreach(string slot in data.slots){var top=data.parts.Where(p=>p.slot==slot&&p.tier=="E").OrderByDescending(p=>p.price).FirstOrDefault();if(top!=null)fit[slot]=top.id;}
            var lap=RacewayLap();float best=LapTime(lap,fit);
            for(int round=0;round<4;round++) {
                bool changed=false;
                foreach(string slot in data.slots)foreach(var p in data.parts.Where(p=>p.slot==slot&&(p.tier=="E"||p.tier=="S"))) {
                    if(fit[slot]==p.id)continue;var trial=new Dictionary<string,string>(fit){[slot]=p.id};float t=LapTime(lap,trial);
                    if(t<best-.005f){best=t;fit=trial;changed=true;}
                }
                if(!changed)break;
            }
            maxBuild=fit;MaxBuildLap=best;return new Dictionary<string,string>(fit);
        }
        // Two laps of the raceway at 2 m; the second is timed (a flying lap).
        static List<Vector3> RacewayLap()
        {
            var c=new Circuit(HillcrestRoute.Circuit);var path=new List<Vector3>();
            for(int lapNo=0;lapNo<2;lapNo++)for(float d=0;d<c.length;d+=2)path.Add(c.At(d));
            return path;
        }
        public static float LapTime(List<Vector3> path,IReadOnlyDictionary<string,string> fit)
        {
            var v=SpeedProfile.Speeds(path,Stats(fit));float t=0;int half=path.Count/2;
            for(int i=half+1;i<path.Count;i++)t+=Vector3.Distance(path[i-1],path[i])/Mathf.Max(1,(v[i]+v[i-1])*.5f);
            return t;
        }
        public static float RacewayLapTime(IReadOnlyDictionary<string,string> fit)=>LapTime(RacewayLap(),fit);
        public static CarTuning Stats(IReadOnlyDictionary<string,string> fit)=>Stats(fit,null);
        // With a gearbox tune (docs/48); a tune for another gearbox is ignored.
        public static CarTuning Stats(IReadOnlyDictionary<string,string> fit,GearTune tune)
        {
            EnsureLoaded();
            foreach(string slot in data.slots)
                if(!fit.TryGetValue(slot,out string id)||Get(id).slot!=slot)throw new ArgumentException("Missing or incompatible equipment: "+slot);
            // --field-engine <id> (a test flag, docs/84): every car runs that engine, for engine-against-engine races on one field.
            var e=Get(FieldEngine!=null&&byId.ContainsKey(FieldEngine)?FieldEngine:fit["engine"]);var t=Get(fit["tyres"]);var g=Get(fit["transmission"]);
            var b=Get(fit["brakes"]);var s=Get(fit["steering"]);var c=Get(fit["chassis"]);var turbo=Get(fit["turbo"]);
            if(LegacyDrivetrain){var legacy=CarTuning.Create(e.power_limit,e.accel,g.ceiling,g.pull,turbo.power,turbo.torque,t.road,t.dirt,b.braking,s.response,s.yaw,c.mass);legacy.downforce=Downforce(c);legacy.dragSave=DragSave(c);legacy.firm=Firm(c);return legacy;}
            // Real gears (docs/47): no gearbox ceiling or pull; the gears come from the engine, turbo and box.
            // A hybrid (HybridPower): the motor and battery's weight is the frame's to carry, charged or flat.
            bool hybrid=e.motorKw>0||HybridPower.Everyone;
            var tuning=CarTuning.Create(e.power_limit,e.accel,999,1,turbo.power,turbo.torque,t.road,t.dirt,b.braking,s.response,s.yaw,c.mass+(e.motorKw>0?e.hybridMass:hybrid?HybridPower.MildMass:0));
            if(hybrid){tuning.hybridKw=e.motorKw>0?e.motorKw:HybridPower.MildKw;tuning.batteryKj=e.motorKw>0?e.batteryKj:HybridPower.MildKj;tuning.regenKw=e.motorKw>0?e.regenKw:HybridPower.MildRegenKw;}
            // Calibration: real gear gaps and shift cuts cost a stepped box some pace against the old gearless
            // curve; 10% more power keeps class C near its old standing-start times (docs/47).
            tuning.power*=PowerCalibration;tuning.downforce=Downforce(c);tuning.dragSave=DragSave(c);tuning.firm=Firm(c);
            if(tune!=null&&(tune.transmission!=g.id||!tune.Valid))tune=null;
            string key=e.id+"|"+turbo.id+"|"+g.id+"|"+c.id+(hybrid?"|hybrid":"")+(tune==null?"":"|"+tune.final.ToString("0.000")+"|"+string.Join(",",tune.gears??new float[0]));
            if(!drivetrains.TryGetValue(key,out var train)){train=new Drivetrain(e,turbo,g,tuning,tune?.final??1,tune?.gears);drivetrains.Add(key,train);}
            tuning.drivetrain=train;tuning.gearCeiling=train.RedlineSpeed;return tuning;
        }
        // The chassis's class platform's downforce (S and E; none below). --no-downforce: none at all, for A/B runs.
        public static float Downforce(Part chassis)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--no-downforce")>=0||data.platforms==null)return 0;
            string tier=string.IsNullOrEmpty(chassis.tier)?"C":chassis.tier;foreach(var p in data.platforms)if(p.tier==tier)return p.downforce;return 0;
        }
        // The chassis's class platform's low-drag bodywork (docs/54): the share of drag it saves (0 for class C).
        // --no-platform-drag: everyone on the street car's drag, for A/B.
        public static float DragSave(Part chassis)
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--no-platform-drag")>=0||data.platforms==null)return 0;
            string tier=string.IsNullOrEmpty(chassis.tier)?"C":chassis.tier;foreach(var p in data.platforms)if(p.tier==tier)return Mathf.Clamp(1-p.drag,0,.6f);return 0;
        }
        // The platform's suspension firmness (docs/54): visual weight transfer only (BodyMotion), 0 for class C.
        public static float Firm(Part chassis)
        {
            if(data.platforms==null)return 0;
            string tier=string.IsNullOrEmpty(chassis.tier)?"C":chassis.tier;foreach(var p in data.platforms)if(p.tier==tier)return Mathf.Max(0,p.firm);return 0;
        }
        // One drivetrain per engine/turbo/box/frame, so repeated Stats() calls compare equal.
        static readonly Dictionary<string,Drivetrain> drivetrains=new(StringComparer.Ordinal);
        public const float PowerCalibration=1.1f;
        static readonly string FieldEngine=RaceTrial.Arg("--field-engine");
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
