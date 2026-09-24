using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace LittleRoad
{
    public static class CarPerformanceRating
    {
        public const string Version="dry-road-v1";
        static float Pull(CarTuning t)=>Mathf.Max(.01f,Mathf.Min(DriveModel.EngineForce(t,10),t.roadGrip*1.65f)-DriveModel.Resistance(10,t.mass));
        public static int Calculate(CarTuning tuning)
        {
            var stock=CarTuning.Stock;
            // Dimensionless equipment contributions; no driver or cosmetic input.
            float value=.28f*tuning.AttainableSpeed/stock.AttainableSpeed+.28f*Pull(tuning)/Pull(stock)
                +.23f*tuning.roadGrip/stock.roadGrip+.13f*tuning.braking/stock.braking+.08f*tuning.yaw/stock.yaw;
            return Mathf.RoundToInt(100*value);
        }
    }
    public sealed class RaceEntrant
    {
        public readonly int Id,Slot,Rating;
        public readonly string Body,EquipmentLabel;
        public readonly Color Paint;
        public readonly DriverIdentity Driver;
        public readonly CarTuning Tuning;
        public readonly IReadOnlyDictionary<string,string> Parts;
        public RaceEntrant(int id,int slot,string body,Color paint,string label,IReadOnlyDictionary<string,string> parts,DriverIdentity driver)
        {
            if(id<0||id>=24||slot<0||slot>=24)throw new ArgumentOutOfRangeException(nameof(id));
            if(!CarBodies.IsKnown(body))throw new ArgumentException("Unknown body: "+body);
            var owned=new Dictionary<string,string>();foreach(var item in parts)owned.Add(item.Key,item.Value);
            Tuning=EquipmentCatalog.Stats(owned);Id=id;Slot=slot;Body=body;Paint=paint;EquipmentLabel=label;Driver=driver;
            Parts=new ReadOnlyDictionary<string,string>(owned);Rating=CarPerformanceRating.Calculate(Tuning);
        }
    }
    public sealed class RaceRoster
    {
        [Serializable] public sealed class EntryRecord {public int id,slot,rating;public string body,equipment;public Color paint;public CarTuning tuning;public FittedPart[] parts;public DriverIdentity.Record driver;}
        [Serializable] public sealed class Evidence {public int schema=1,seed,laps;public string track,title,ratingVersion;public bool modern;public EntryRecord[] entries;}
        public readonly int Seed,Laps;
        public readonly string Track,Title;
        public readonly bool Modern;
        public readonly IReadOnlyList<RaceEntrant> Entries;
        public RaceRoster(int seed,int laps,string track,string title,bool modern,RaceEntrant[] entrants)
        {
            if(entrants==null||entrants.Length<1||entrants.Length>24||laps<1)throw new ArgumentException("Invalid race roster");
            var copy=(RaceEntrant[])entrants.Clone();var slots=new HashSet<int>();
            for(int i=0;i<copy.Length;i++)if(copy[i]==null||copy[i].Id!=i||!slots.Add(copy[i].Slot))throw new ArgumentException("Roster needs contiguous IDs and distinct grid slots");
            Seed=seed;Laps=laps;Track=track;Title=title;Modern=modern;Entries=Array.AsReadOnly(copy);
        }
        public static RaceRoster Trial(int count,int laps,int seed,string track,string setup,string[] opponents,string scenario,string driverMode,bool form,string replayBody=null)
        {
            var entries=new RaceEntrant[count];
            for(int i=0;i<count;i++) {
                string body=replayBody??(i%3==1?"coupe_car":i%3==2?"utility_car":"compact_v1");
                string equipment=i==0?setup:i-1<opponents.Length?opponents[i-1]:i%4==0?"club":"street";
                var paint=i==0?new Color(.08f,.56f,.49f):Color.HSVToRGB(Mathf.Repeat(i*.173f,.999f),.64f,.84f);
                entries[i]=new RaceEntrant(i,RaceGrid.TrialSlot(i,count,scenario),body,paint,equipment,EquipmentCatalog.TestLoadout(equipment),
                    driverMode==null?null:DriverIdentity.ForRace(i,seed,driverMode,form));
            }
            return new RaceRoster(seed,laps,track,track=="bayview"?"BAYVIEW COAST":"CEDAR TECHNICAL",driverMode!=null,entries);
        }
        public static readonly Color FlagshipPaint=new(.05f,.22f,.62f);
        public static RaceRoster Adventure(AdventureProgress save,bool modern=true)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;
            var trial=Trial(24,3,seed,"bayview","stock",Array.Empty<string>(),"field",modern?"mixed":null,modern);
            // Bodies follow equipment tier: street cars are family cars, club cars
            // are sporty, and the pole-sitter runs club kit in the liveried flagship.
            var entries=new RaceEntrant[24];
            var family=CarBodies.Deal(CarBodies.Family,24,seed);var sporty=CarBodies.Deal(CarBodies.Sporty,24,seed+1);int f=0,s=0;
            for(int i=1;i<24;i++){
                var e=trial.Entries[i];
                if(e.Slot==0)entries[i]=new RaceEntrant(e.Id,e.Slot,CarBodies.Flagship,FlagshipPaint,"club",EquipmentCatalog.TestLoadout("club"),e.Driver);
                else entries[i]=new RaceEntrant(e.Id,e.Slot,e.EquipmentLabel=="club"?sporty[s++]:family[f++],e.Paint,e.EquipmentLabel,e.Parts,e.Driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver);
            return new RaceRoster(seed,3,"bayview","BAYVIEW COAST / CLASS C",modern,entries);
        }
        public bool MatchesPlayer(AdventureProgress save)
        {
            if(Seed!=1709+(save.raceSerial+1)*11||Entries[0].Body!=save.bodyId||Entries[0].Paint!=AdventureGame.Paints[save.paintIndex])return false;
            var fit=save.Loadout();foreach(var pair in Entries[0].Parts)if(!fit.TryGetValue(pair.Key,out string part)||pair.Value!=part)return false;
            return true;
        }
        public void Save(string path)
        {
            var entries=new EntryRecord[Entries.Count];
            for(int i=0;i<entries.Length;i++) {
                var e=Entries[i];var parts=new List<FittedPart>();foreach(string slot in EquipmentCatalog.All.slots)parts.Add(new FittedPart{slot=slot,id=e.Parts[slot]});
                entries[i]=new EntryRecord{id=e.Id,slot=e.Slot,rating=e.Rating,body=e.Body,equipment=e.EquipmentLabel,paint=e.Paint,tuning=e.Tuning,parts=parts.ToArray(),driver=e.Driver?.Export(e.Id)};
            }
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)));
            System.IO.File.WriteAllText(path,JsonUtility.ToJson(new Evidence{seed=Seed,laps=Laps,track=Track,title=Title,ratingVersion=CarPerformanceRating.Version,modern=Modern,entries=entries},true));
        }
    }
    // The approved driver package, shared by connected adventure races and
    // trials (`--ai-preset field`) so tests measure what is played. Explicit
    // CLI values win; `--preset-without <switch>` drops one switch for A/B runs.
    public static class RaceAiPreset
    {
        static bool Active=>AdventureSession.Racing&&AdventureSession.EnteredRoster?.Modern==true||CommandLineValue("--ai-preset")=="field";
        static string CommandLineValue(string key){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        static bool Dropped(string key){var args=Environment.GetCommandLineArgs();for(int i=0;i+1<args.Length;i++)if(args[i]=="--preset-without"&&args[i+1]==key)return true;return false;}
        static readonly HashSet<string> switches=new(StringComparer.Ordinal){"--stagger-context-plans","--physical-recovery","--traffic-diagnostics","--road-feasibility","--prepared-path","--guide-path-fallback","--compare-continuation","--traffic-aware-progress","--following-feasibility-fallback","--shared-observed-traffic","--parallel-continuation-forecast","--rear-pressure-filter","--light-drafting","--coast-within-envelope","--no-gap-centres","--traffic-path-check","--anticipate-passing-space","--traffic-coast-continuation","--parallel-road-forecasts","--dynamic-rejoin","--dynamic-traffic","--local-origin-curvature","--dynamic-field-study","--dynamic-blocking-target","--dynamic-traffic-fallback","--parallel-dynamic-forecast","--open-space-priority","--driver-roster","--driver-form"};
        public static bool Enabled(string key)=>Active&&switches.Contains(key)&&!Dropped(key);
        public static string Value(string key,string fallback)
        {
            if(!Active)return fallback;
            return key switch {"--driver-policy"=>"context","--driver-risk"=>"5","--driver-braking"=>"neutral","--parallel-forecast-workers"=>"8","--driver-roster"=>"mixed","--context-plan-groups"=>"3","--following-fallback-scope"=>"all",
                "--racing-line"=>CommandLineValue("--trial-track")=="cedar"?"cedar-smooth-v2":"bayview-smooth-v1","--traffic-case"=>"field",_=>fallback};
        }
    }
}
