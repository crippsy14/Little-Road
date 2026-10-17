using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    public static class CarPerformanceRating
    {
        public const string Version="dry-road-v1";
        static float Pull(CarTuning t)=>Mathf.Max(.01f,Mathf.Min(DriveModel.EngineForce(t,10),t.roadGrip*1.65f)-DriveModel.Resistance(t,10));
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
        public RaceEntrant(int id,int slot,string body,Color paint,string label,IReadOnlyDictionary<string,string> parts,DriverIdentity driver,GearTune tune=null)
        {
            if(id<0||id>=24||slot<0||slot>=24)throw new ArgumentOutOfRangeException(nameof(id));
            if(!CarBodies.IsKnown(body))throw new ArgumentException("Unknown body: "+body);
            var owned=new Dictionary<string,string>();foreach(var item in parts)owned.Add(item.Key,item.Value);
            Tuning=EquipmentCatalog.Stats(owned,tune);Id=id;Slot=slot;Body=body;Paint=paint;EquipmentLabel=label;Driver=driver;
            Parts=new ReadOnlyDictionary<string,string>(owned);Rating=CarPerformanceRating.Calculate(Tuning);
        }
    }
    public sealed class RaceRoster
    {
        [Serializable] public sealed class EntryRecord {public int id,slot,rating;public string body,equipment;public Color paint;public CarTuning tuning;public FittedPart[] parts;public DriverIdentity.Record driver;}
        [Serializable] public sealed class Evidence {public int schema=1,seed,laps;public string track,title,ratingVersion;public bool modern,reversed;public EntryRecord[] entries;}
        public readonly int Seed,Laps;
        public readonly string Track,Title;
        // The event (RaceCalendar id, or DukeEvent); the track's id unless said otherwise.
        public readonly string Event;
        public readonly bool Modern;
        // Driven the other way round (docs/67): the same track id, so every track-specific rule still applies.
        public readonly bool Reversed;
        public readonly IReadOnlyList<RaceEntrant> Entries;
        public RaceRoster(int seed,int laps,string track,string title,bool modern,RaceEntrant[] entrants,string eventId=null,bool reversed=false)
        {
            Event=eventId??track;Reversed=reversed;
            if(entrants==null||entrants.Length<1||entrants.Length>24||laps<1)throw new ArgumentException("Invalid race roster");
            var copy=(RaceEntrant[])entrants.Clone();var slots=new HashSet<int>();
            for(int i=0;i<copy.Length;i++)if(copy[i]==null||copy[i].Id!=i||!slots.Add(copy[i].Slot))throw new ArgumentException("Roster needs contiguous IDs and distinct grid slots");
            Seed=seed;Laps=laps;Track=track;Title=title;Modern=modern;Entries=Array.AsReadOnly(copy);
        }
        public static RaceRoster Trial(int count,int laps,int seed,string track,string setup,string[] opponents,string scenario,string driverMode,bool form,string replayBody=null)
        {
            var entries=new RaceEntrant[count];
            // Car 0 keeps the compact master; the rest come from the generated field by equipment tier,
            // all inside the one standard envelope so shape never changes pace.
            var family=CarBodies.Deal(CarBodies.Family,count,seed);var sporty=CarBodies.Deal(CarBodies.Sporty,count,seed+1);int f=0,s=0;
            for(int i=0;i<count;i++) {
                string equipment=scenario=="class-sort"?RaceGrid.ClassSortEquipment(i):i==0?setup:i-1<opponents.Length?opponents[i-1]:i%4==0?"club":"street";
                string body=replayBody??(i==0?"compact_v1":equipment=="street"||equipment=="stock"?family[f++]:sporty[s++]);
                var paint=i==0?new Color(.08f,.56f,.49f):Color.HSVToRGB(Mathf.Repeat(i*.173f,.999f),.64f,.84f);
                entries[i]=new RaceEntrant(i,RaceGrid.TrialSlot(i,count,scenario),body,paint,equipment,EquipmentCatalog.TestLoadout(equipment),
                    driverMode==null?null:DriverIdentity.ForRace(i,seed,driverMode,form));
            }
            return new RaceRoster(seed,laps,track,track=="bayview"?"BAYVIEW COAST":track=="hillcrest"?"HILLCREST RACEWAY":track=="quarry"?"QUARRY HILLS RALLYCROSS":"CEDAR TECHNICAL",driverMode!=null,entries);
        }
        public static readonly Color FlagshipPaint=new(.05f,.22f,.62f);
        // The Duke's Challenge (docs/54; Richard, 2 Oct: "a great full circle moment at the end of the game... beat him
        // like a final boss"): three laps of Hillcrest Raceway, you against Duke Rolo in the Regale on the maximum
        // build. He starts on pole; the best driver in the game.
        public const string DukeEvent="duke";public static readonly Color DukePaint=new(.36f,.20f,.62f);
        static readonly DriverIdentity.Definition DukeDriver=new(){id="duke_rolo",name="Duke Rolo",style="Duke",braking=.75f,risk=6,consistency=1};
        public static RaceRoster DukeChallenge(AdventureProgress save)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;
            var entries=new[]{
                new RaceEntrant(0,1,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),DriverIdentity.ForRace(0,seed,"mixed",true),save.Tune()),
                new RaceEntrant(1,0,CarBodies.Prototype,DukePaint,"the maximum build",EquipmentCatalog.MaxBuild(),new DriverIdentity(DukeDriver,seed,false))};
            return new RaceRoster(seed,3,"hillcrest","THE DUKE'S CHALLENGE / HILLCREST RACEWAY",true,entries,DukeEvent);
        }
        public static RaceRoster Adventure(AdventureProgress save,bool modern=true)=>Adventure(save,modern,FieldRecipe.On);
        public static RaceRoster Adventure(AdventureProgress save,bool modern,bool recipe)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;
            var trial=Trial(24,3,seed,"bayview","stock",Array.Empty<string>(),"field",modern?"mixed":null,modern);
            if(recipe)return Recipe(save,"bayview","C",RaceCalendar.StarsOf("bayview"),trial,seed,3,"bayview",false,"BAYVIEW COAST / CLASS C",modern,CarBodies.Family,CarBodies.Sporty);
            // Bodies follow equipment tier: street cars are family cars, club cars
            // are sporty, and the pole-sitter runs club kit in the liveried flagship.
            var entries=new RaceEntrant[24];
            var family=CarBodies.Deal(CarBodies.Family,24,seed);var sporty=CarBodies.Deal(CarBodies.Sporty,24,seed+1);int f=0,s=0;
            for(int i=1;i<24;i++){
                var e=trial.Entries[i];
                if(e.Slot==0)entries[i]=new RaceEntrant(e.Id,e.Slot,CarBodies.Flagship,FlagshipPaint,"club",EquipmentCatalog.TestLoadout("club"),e.Driver);
                else entries[i]=new RaceEntrant(e.Id,e.Slot,e.EquipmentLabel=="club"?sporty[s++]:family[f++],e.Paint,e.EquipmentLabel,e.Parts,e.Driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver,save.Tune());
            return new RaceRoster(seed,3,"bayview","BAYVIEW COAST / CLASS C",modern,entries);
        }
        // The Valley Cup at Hillcrest Raceway: Class C (a quicker field than Bayview's), two laps, context drivers (hillcrest-race r5: legacy
        // needed ~350 contacts a race); street and
        // club kit through the field and the pole-sitter on Hillcrest Motors' valley parts.
        public static RaceRoster AdventureHillcrest(AdventureProgress save,bool modern=true)=>AdventureHillcrest(save,modern,FieldRecipe.On);
        public static RaceRoster AdventureHillcrest(AdventureProgress save,bool modern,bool recipe)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;
            var trial=Trial(24,2,seed,"hillcrest","stock",Array.Empty<string>(),"field",modern?"mixed":null,modern);
            if(recipe)return Recipe(save,"hillcrest","C",RaceCalendar.StarsOf("hillcrest"),trial,seed,2,"hillcrest",false,"HILLCREST RACEWAY / CLASS C",modern,CarBodies.Family,CarBodies.Sporty);
            var entries=new RaceEntrant[24];
            var family=CarBodies.Deal(CarBodies.Family,24,seed);var sporty=CarBodies.Deal(CarBodies.Sporty,24,seed+1);int f=0,s=0;
            for(int i=1;i<24;i++){
                var e=trial.Entries[i];string kit=e.Slot==0?"valley":i%3==0?"club":"street";
                entries[i]=new RaceEntrant(e.Id,e.Slot,e.Slot==0?CarBodies.Flagship:kit=="street"?family[f++]:sporty[s++],e.Slot==0?FlagshipPaint:e.Paint,kit,EquipmentCatalog.TestLoadout(kit),e.Driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver,save.Tune());
            return new RaceRoster(seed,2,"hillcrest","HILLCREST RACEWAY / CLASS C",modern,entries);
        }
        // Quarry Hills Rallycross (--quarry-hills): class C, three laps of the floor, the whole field in the town's off-roaders
        // (street and club kit; the pole-sitter on club kit).
        public static RaceRoster AdventureQuarry(AdventureProgress save,bool modern=true)=>AdventureQuarry(save,modern,FieldRecipe.On);
        public static RaceRoster AdventureQuarry(AdventureProgress save,bool modern,bool recipe)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;
            var trial=Trial(24,3,seed,"quarry","stock",Array.Empty<string>(),"field",modern?"mixed":null,modern);
            if(recipe)return Recipe(save,"quarry","C",RaceCalendar.StarsOf("quarry"),trial,seed,3,"quarry",false,"QUARRY HILLS RALLYCROSS / CLASS C",modern,CarBodies.OffRoad,CarBodies.OffRoad);
            var entries=new RaceEntrant[24];var off=CarBodies.Deal(CarBodies.OffRoad,24,seed);int o=0;
            for(int i=1;i<24;i++){
                var e=trial.Entries[i];string kit=e.Slot==0||i%3==0?"club":"street";var parts=EquipmentCatalog.TestLoadout(kit);parts["tyres"]=kit=="club"?"tyres_rally":"tyres_trail";
                entries[i]=new RaceEntrant(e.Id,e.Slot,off[o++],e.Paint,kit+" on gravel tyres",parts,e.Driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver,save.Tune());
            return new RaceRoster(seed,3,"quarry","QUARRY HILLS RALLYCROSS / CLASS C",modern,entries);
        }
        // Class B events (docs/67): the field on the event's star kit, the front of the grid a star up and the back a star
        // down; sporty bodies on tarmac, the off-roaders (on B rally tyres) at the rallycross; laps and direction from
        // the calendar. The field is the Tuner bodies: the road ones on tarmac, off-road and rally ones on gravel.
        public static RaceRoster AdventureTuner(AdventureProgress save,RaceCalendar.Event ev,bool modern=true)=>AdventureTuner(save,ev,modern,FieldRecipe.On);
        public static RaceRoster AdventureTuner(AdventureProgress save,RaceCalendar.Event ev,bool modern,bool recipe)
        {
            save.Validate();int seed=1709+(save.raceSerial+1)*11;bool gravel=ev.track=="quarry";
            var trial=Trial(24,ev.laps,seed,ev.track,"stock",Array.Empty<string>(),"field",modern?"mixed":null,modern);
            if(recipe)return Recipe(save,ev.id,"B",ev.stars,trial,seed,ev.laps,ev.track,ev.reversed,$"{ev.venue.ToUpperInvariant()} / CLASS B {ev.Stars}",modern,gravel?CarBodies.TunerGravel:CarBodies.Tuner,gravel?CarBodies.TunerGravel:CarBodies.Tuner);
            var entries=new RaceEntrant[24];var pool=gravel?CarBodies.Deal(CarBodies.TunerGravel,24,seed):CarBodies.Deal(CarBodies.Tuner,24,seed+1);int o=0;
            for(int i=1;i<24;i++){
                var e=trial.Entries[i];int stars=Mathf.Clamp(ev.stars+(e.Slot<4?1:e.Slot>=18?-1:0),1,10);string kit="b"+stars;
                var parts=EquipmentCatalog.TestLoadout(kit);if(gravel)parts["tyres"]="tyres_b_rally";
                entries[i]=new RaceEntrant(e.Id,e.Slot,pool[o++],e.Paint,$"class B {stars}★"+(gravel?" on rally tyres":""),parts,e.Driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver,save.Tune());
            return new RaceRoster(seed,ev.laps,ev.track,$"{ev.venue.ToUpperInvariant()} / CLASS B {ev.Stars}",modern,entries,ev.id,ev.reversed);
        }
        // A field by recipe (FieldRecipe, --field-recipe; docs/70): drivers and kits both rolled from the grid slot; low kits
        // in the first body pool, high kits in the second; the pole-sitter in the liveried flagship at class C on tarmac.
        static RaceRoster Recipe(AdventureProgress save,string eventId,string raceClass,int stars,RaceRoster trial,int seed,int laps,string track,bool reversed,string title,bool modern,string[] lowBodies,string[] highBodies)
        {
            var entries=new RaceEntrant[24];
            bool topC=CarBodies.TopCOpen(raceClass,stars);if(!topC)highBodies=highBodies.Where(b=>Array.IndexOf(CarBodies.TopC,b)<0).ToArray();
            // --forge-roster: class C bodies from the Car Forge roster by star band (ForgeField). Kits and drivers are unchanged.
            bool forge=(raceClass=="C"||raceClass=="B")&&CarBodies.RosterEnabled&&CarBodies.RosterOf(raceClass).Length>0;
            var forged=forge?ForgeField(stars,FieldRecipe.Discipline(track)=="gravel",topC,seed,raceClass):null;
            var low=CarBodies.Deal(lowBodies,24,seed);var high=CarBodies.Deal(highBodies,24,seed+1);int l=0,h=0;
            for(int i=1;i<24;i++) {
                var e=trial.Entries[i];string disc=FieldRecipe.Discipline(track);var parts=FieldRecipe.Kit(raceClass,disc,stars,e.Slot,seed,out float level);
                var driver=modern?FieldRecipe.Driver(i,seed,raceClass,e.Slot,true):null;
                bool flagship=raceClass=="C"&&disc=="road"&&e.Slot==0;string label=$"class {raceClass} {stars}★ recipe kit {Mathf.RoundToInt(level*100)}%";
                string body=forge?forged[e.Slot]:flagship?(topC?CarBodies.Flagship:CarBodies.LowFlagship):level>=.6f?high[h++]:low[l++];
                entries[i]=new RaceEntrant(e.Id,e.Slot,body,flagship?FlagshipPaint:e.Paint,label,parts,driver);
            }
            entries[0]=new RaceEntrant(0,23,save.bodyId,AdventureGame.Paints[save.paintIndex],"owned equipment",save.Loadout(),trial.Entries[0].Driver,save.Tune());
            return new RaceRoster(seed,laps,track,title,modern,entries,eventId,reversed);
        }
        // --forge-roster: the 23 rival bodies by grid slot (car-forge docs/roster-c-design.md, "How a field of 24 is filled").
        // Pole: the Zenith once the top of class C is open, else the band's first quick-looking car. Slots 1-6: the rest of the
        // quick-looking cars. The middle: the band's own cars. The last two rival slots: veterans from the bands below. Each
        // pool is a shuffled deck; when one runs out the slot takes the next pool's cars, and last the cars of the bands above
        // (a thin gravel field); a model races at most twice in a field, and never in the slot behind itself. (17 Oct, first
        // run: a one-car quick pool dealt the Stock Banger nine times.)
        public static string[] ForgeField(int stars,bool gravel,bool topC,int seed,string cls="C")
        {
            var (quick,home,vet)=CarBodies.RosterPools(stars,gravel,cls);
            var q=CarBodies.Deal(quick.Distinct().ToArray(),quick.Distinct().Count(),seed+1);
            var m=CarBodies.Deal(home.Distinct().ToArray(),home.Distinct().Count(),seed);
            var b=CarBodies.Deal(vet.Distinct().ToArray(),vet.Distinct().Count(),seed+2);
            var spare=CarBodies.RosterSpare(stars,gravel,cls);var s=spare.Length>0?CarBodies.Deal(spare,spare.Length,seed+3):spare;
            var used=new Dictionary<string,int>();var field=new string[23];
            // (the pools in order, each model once, then a second time; the bands above only after that: a 1-star field
            //  had the Kestrel at the back before it repeated a 1-star car)
            string Pick(string prev,params string[][] decks)
            {
                foreach(var group in new[]{decks,new[]{s}})
                    for(int most=1;most<=2;most++)
                        foreach(var deck in group)foreach(var id in deck)
                            if(id!=prev&&(!used.TryGetValue(id,out int n)||n<most)){used[id]=(used.TryGetValue(id,out n)?n:0)+1;return id;}
                var any=decks.Append(s).SelectMany(d=>d).First(id=>id!=prev);used[any]=(used.TryGetValue(any,out int k)?k:0)+1;return any;
            }
            for(int slot=0;slot<23;slot++) {
                string prev=slot>0?field[slot-1]:null;
                if(slot==0&&topC&&!gravel&&cls=="C"){field[0]=CarBodies.RosterFlagship;used[field[0]]=1;continue;}
                field[slot]=slot<=6?Pick(prev,q,m,b):slot>=21?Pick(prev,b,m,q):Pick(prev,m,b,q);
            }
            return field;
        }
        public static RaceRoster ForEvent(string id,AdventureProgress save)
        {
            if(RaceCalendar.Known(id)){var ev=RaceCalendar.Get(id);if(ev.raceClass=="B")return AdventureTuner(save,ev);}
            return id=="hillcrest"?AdventureHillcrest(save):id=="quarry"?AdventureQuarry(save):Adventure(save);
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
            System.IO.File.WriteAllText(path,JsonUtility.ToJson(new Evidence{seed=Seed,laps=Laps,track=Track,title=Title,ratingVersion=CarPerformanceRating.Version,modern=Modern,reversed=Reversed,entries=entries},true));
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
        // Racing AI v2 (docs/46) with the switches it is raced with: recovery, drafting physics, the roster and gate crediting.
        static readonly HashSet<string> switches=new(StringComparer.Ordinal){"--physical-recovery","--light-drafting","--driver-roster","--driver-form","--gate-first"};
        public static bool Enabled(string key)=>Active&&switches.Contains(key)&&!Dropped(key);
        public static string Value(string key,string fallback)
        {
            if(!Active)return fallback;
            return key switch {"--driver-policy"=>"racer","--driver-roster"=>"mixed",
                "--racing-line"=>CommandLineValue("--trial-track")=="cedar"?"cedar-smooth-v2":"bayview-smooth-v1","--traffic-case"=>"field",_=>fallback};
        }
    }
}
