using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleRoad
{
    [Serializable] public sealed class FittedPart {public string slot,id;}
    // Every collectible id a save may hold. The world spawns from here and Validate checks
    // against it, so a new postcard, stamp or trial cannot pass one and fail the other.
    public static class Collectibles
    {
        public const int Postcards=5,Stamps=8,Carvings=12;
        public const string VisitHillcrest="visit_hillcrest",CastleTrial="trial_castle",CastleTrialHard="trial_castle_hard",QuarrySprint="trial_quarry_sprint";
        // Beating Duke Rolo in the Duke's Challenge (docs/54): the Regale and the prize, once.
        public const string DukeBeaten="duke_beaten";
        // Beating Dai out of the blast zone in the Blast Run (BlastRun, doc 63): his prize, once.
        public const string BlastRunWin="trial_blast_run";
        public static string Postcard(int i)=>"postcard_"+i;
        public static string Stamp(int i)=>"stamp_"+i;
        // Quarry Hills' slate carvings (docs/59; --quarry-hills).
        public static string Carving(int i)=>"carving_"+i;
        public static bool IsCarving(string id)=>Indexed(id,"carving_",Carvings);
        public static bool IsPostcard(string id)=>Indexed(id,"postcard_",Postcards);
        public static bool IsStamp(string id)=>Indexed(id,"stamp_",Stamps);
        public static bool Known(string id)=>IsPostcard(id)||IsStamp(id)||IsCarving(id)||id==VisitHillcrest||id==CastleTrial||id==CastleTrialHard||id==QuarrySprint||id==DukeBeaten||id==BlastRunWin||PhotoRequests.Known(id);
        static bool Indexed(string id,string prefix,int count)
        {
            if(id==null||!id.StartsWith(prefix,StringComparison.Ordinal))return false;
            for(int i=0;i<count;i++)if(id==prefix+i)return true;
            return false;
        }
    }
    // A gearbox tune (docs/48): the final drive (0.80-1.25; above 1 shortens every gear) and, on boxes that allow it,
    // each gear's own multiplier (0.85-1.15). Saved per gearbox; a gearbox without one runs its defaults.
    [Serializable] public sealed class GearTune
    {
        public string transmission;public float final=1;public float[] gears;
        public const float FinalMin=.8f,FinalMax=1.25f,GearMin=.85f,GearMax=1.15f;
        public bool Valid=>!string.IsNullOrEmpty(transmission)&&final>=FinalMin&&final<=FinalMax&&(gears==null||gears.Length<=8&&gears.All(g=>g>=GearMin&&g<=GearMax));
    }
    // A speed trap's record (SpeedTraps): the best speed through it (mph) and the stars already paid. Beat the Train
    // (TrainRace) keeps its best time (s) the same way, stars 1 once won.
    [Serializable] public sealed class TrapRecord {public string id;public float best;public int stars;}
    [Serializable] public sealed class AdventureProgress
    {
        public int version,credits,questStage,paintIndex,raceSerial,settledSerial,bestPlace,raceWins;
        // The fitted gadget (Gadgets), empty for none; older saves read as none.
        public string gadget;
        // Parts found at their home shops (PartDiscovery): sold everywhere from then on.
        public List<string> partsFound;
        public string bodyId;
        public List<string> owned,collected;
        // Bodies bought at Pit Stop body shops (bodyId is the one being driven). Older saves: see AdventureStore.
        public List<string> bodies;
        public List<FittedPart> fitted;
        // Race events (RaceCalendar ids) finished on the podium: a podium at every race of a class opens the next.
        public List<string> podiums;
        // Fog of war (ExploredMap: base64 bit mask) and the places whose "discovered" card has been shown.
        public string explored;public List<string> discovered;
        // Gearbox tunes from the tuning shop (docs/48). Older saves have none.
        public List<GearTune> tunes;
        // Where the car was when last saved (x, y, z, yaw): the game resumes there. Older saves have none (empty).
        public float[] spot;
        // Speed traps (SpeedTraps). Older saves have none.
        public List<TrapRecord> traps;
        public GearTune Tune(){var box=fitted?.Find(f=>f.slot=="transmission")?.id;return box==null?null:tunes?.Find(t=>t.transmission==box);}
        public static AdventureProgress New()=>new AdventureProgress {version=1,bodyId=CarBodies.Starter,podiums=new List<string>(),discovered=new List<string>(),
            owned=EquipmentCatalog.StockLoadout().Values.ToList(),collected=new List<string>(),bodies=new List<string>{CarBodies.Starter},
            fitted=EquipmentCatalog.StockLoadout().Select(p=>new FittedPart{slot=p.Key,id=p.Value}).ToList()};
        public Dictionary<string,string> Loadout()=>fitted.ToDictionary(p=>p.slot,p=>p.id);
        public void Validate()
        {
            if(traps!=null&&(traps.Any(t=>t==null||!(SpeedTraps.Known(t.id)||TrainRace.Known(t.id)||DukeDrive.Known(t.id)||RallySchool.Known(t.id)||GalleryClimb.Known(t.id)||BlastRun.Known(t.id)||TipLeap.Known(t.id)||FarmChallenges.Known(t.id))||t.stars<0||t.stars>3||t.best<0||t.best>500)||traps.Select(t=>t.id).Distinct().Count()!=traps.Count))throw new InvalidDataException("A speed trap record is invalid or repeated.");
            if(spot!=null&&spot.Length!=0&&(spot.Length!=4||spot.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||Mathf.Abs(spot[0])>20000||Mathf.Abs(spot[2])>20000||Mathf.Abs(spot[1])>2000))throw new InvalidDataException("The saved position is invalid.");
            if(tunes!=null&&(tunes.Any(t=>t==null||!t.Valid)||tunes.Select(t=>t.transmission).Distinct().Count()!=tunes.Count))throw new InvalidDataException("A gearbox tune is invalid or repeated.");
            if(version!=1||credits<0||credits>10000000||questStage<0||questStage>3||paintIndex<0||paintIndex>=PaintCatalog.All.Length||
                raceSerial<0||settledSerial<0||settledSerial>raceSerial||raceWins<0||raceWins>settledSerial||bestPlace<0||bestPlace>24)
                throw new InvalidDataException("Save version or progression values are invalid.");
            if(!CarBodies.Ownable(bodyId))throw new InvalidDataException("Unknown body.");
            if(!Gadgets.Known(gadget))throw new InvalidDataException("Unknown gadget.");
            if(partsFound!=null&&(partsFound.Distinct().Count()!=partsFound.Count||partsFound.Any(id=>!EquipmentCatalog.Has(id))))throw new InvalidDataException("A found part is unknown or repeated.");
            if(bodies==null||bodies.Distinct().Count()!=bodies.Count||!bodies.Contains(bodyId)||bodies.Any(b=>!CarBodies.Ownable(b)))throw new InvalidDataException("Owned bodies are missing, duplicated or unknown.");
            if(bodies.Contains(CarBodies.Prototype)&&collected?.Contains(Collectibles.DukeBeaten)!=true)throw new InvalidDataException("The Regale is only won from the Duke.");
            if(owned==null||collected==null||fitted==null||owned.Distinct().Count()!=owned.Count||collected.Distinct().Count()!=collected.Count)
                throw new InvalidDataException("Missing or duplicate inventory entries.");
            foreach(string id in owned)EquipmentCatalog.Get(id);
            foreach(string id in collected)if(!Collectibles.Known(id))throw new InvalidDataException("Unknown collectible: "+id);
            if(podiums==null||podiums.Distinct().Count()!=podiums.Count||podiums.Any(id=>!RaceCalendar.Known(id)))throw new InvalidDataException("Podium records are missing, duplicated or unknown.");
            if(discovered==null||discovered.Distinct().Count()!=discovered.Count||discovered.Any(string.IsNullOrEmpty))throw new InvalidDataException("Discovered places are missing or duplicated.");
            if(fitted.Count!=EquipmentCatalog.All.slots.Length||fitted.Any(p=>p==null))throw new InvalidDataException("Invalid fitted equipment.");
            var fit=Loadout();EquipmentCatalog.Stats(fit);
            foreach(string slot in EquipmentCatalog.All.slots)
                if(!owned.Contains(slot+"_stock")||!owned.Contains(fit[slot]))throw new InvalidDataException("Fitted equipment is not owned.");
        }
    }
    // Every award/purchase/fitting is committed to disk before becoming live.
    // Invalid existing saves stop loading; they are never silently overwritten.
    public sealed class AdventureStore
    {
        public AdventureProgress Data {get;private set;}
        public readonly string path;
        public AdventureStore(string file)
        {
            path=Path.GetFullPath(file);
            Data=File.Exists(path)?JsonUtility.FromJson<AdventureProgress>(File.ReadAllText(path)):AdventureProgress.New();
            // The Puddle replaced the compact as the starter car; older saves still on it move across.
            if(Data!=null&&Data.bodyId=="compact_v1")Data.bodyId=CarBodies.Starter;
            // Retired placeholder bodies leave the garage; a car still on one moves to the starter.
            if(Data!=null){
                if(Array.IndexOf(CarBodies.Retired,Data.bodyId)>=0)Data.bodyId=CarBodies.Starter;
                if(Data.bodies!=null&&Data.bodies.RemoveAll(b=>Array.IndexOf(CarBodies.Retired,b)>=0)>0&&Data.bodies.Count>0&&!Data.bodies.Contains(Data.bodyId))Data.bodies.Add(Data.bodyId);
            }
            // Saves from before body shops own exactly the body they drive. JsonUtility reads a
            // missing list as empty, not null.
            if(Data!=null&&(Data.bodies==null||Data.bodies.Count==0))Data.bodies=new List<string>{Data.bodyId};
            // Saves from before the class ladder have no podium record.
            if(Data!=null&&Data.podiums==null)Data.podiums=new List<string>();
            if(Data!=null&&Data.discovered==null)Data.discovered=new List<string>();
            if(Data==null)throw new InvalidDataException("Save is empty.");Data.Validate();
        }
        public bool Change(Func<AdventureProgress,bool> edit)
        {
            var candidate=JsonUtility.FromJson<AdventureProgress>(JsonUtility.ToJson(Data));
            if(!edit(candidate))return false;candidate.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)) {
                using(var writer=new StreamWriter(stream,System.Text.Encoding.UTF8,1024,true)){writer.Write(JsonUtility.ToJson(candidate,true));writer.Flush();}
                stream.Flush(true);
            }
            if(File.Exists(path))File.Replace(temporary,path,path+".bak",true);else File.Move(temporary,path);
            Data=candidate;return true;
        }
        public bool AcceptDelivery()=>Change(p=>{if(p.questStage!=0)return false;p.questStage=1;return true;});
        public bool Deliver()=>Change(p=>{if(p.questStage!=1)return false;p.questStage=2;return true;});
        public bool ClaimDelivery()=>Change(p=>{if(p.questStage!=2)return false;p.questStage=3;p.credits+=350;return true;});
        // One-off time-trial prize (the id joins `collected`, so it pays once).
        public bool ClaimTrial(string id,int prize)=>Change(p=>{if(p.collected.Contains(id)||prize<0)return false;p.collected.Add(id);p.credits+=prize;return true;});
        public bool Collect(string id)=>Change(p=>{if(p.collected.Contains(id))return false;p.collected.Add(id);p.credits+=50;return true;});
        // Paid repaint at Bayview Paints: charges once; the current colour or a short wallet is refused.
        public bool Repaint(int index,int price)=>Change(p=>{if(index==p.paintIndex||index<0||index>=PaintCatalog.All.Length||price<0||p.credits<price||PaintCatalog.LockedBy(p,index)!=null)return false;p.credits-=price;p.paintIndex=index;return true;});
        // Body shop: buy a body once; switching bodies (garage bay) is free and changes looks only.
        public bool BuyBody(string id)=>Change(p=>{int price=CarBodies.Price(id);if(price<0||p.bodies.Contains(id)||p.credits<price)return false;p.credits-=price;p.bodies.Add(id);return true;});
        public bool SwitchBody(string id)=>Change(p=>{if(!p.bodies.Contains(id)||p.bodyId==id)return false;p.bodyId=id;return true;});
        public bool Discover(string place)=>Change(p=>{if(p.discovered.Contains(place))return false;p.discovered.Add(place);return true;});
        public bool SaveSpot(Vector3 at,float yaw)=>Change(p=>{p.spot=new[]{Mathf.Round(at.x*10)/10,Mathf.Round(at.y*100)/100,Mathf.Round(at.z*10)/10,Mathf.Round(yaw)};return true;});
        public bool SaveExplored(string mask)=>Change(p=>{if(p.explored==mask)return false;p.explored=mask;return true;});
        public bool Buy(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(p.owned.Contains(id)||p.credits<part.price)return false;p.credits-=part.price;p.owned.Add(id);return true;});
        // The tuning shop (docs/48): replace the fitted gearbox's tune (null returns it to its defaults).
        public bool SetTune(GearTune tune)=>Change(p=>{
            var box=p.Loadout()["transmission"];if(tune!=null&&(tune.transmission!=box||!tune.Valid))return false;
            p.tunes??=new List<GearTune>();p.tunes.RemoveAll(t=>t.transmission==box);if(tune!=null)p.tunes.Add(tune);return true;});
        public bool Fit(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(!p.owned.Contains(id))return false;var item=p.fitted.Find(f=>f.slot==part.slot);if(item.id==id)return false;item.id=id;return true;});
        // Speed trap: keeps the best speed, pays each newly reached star once (prizes by star). Returns credits paid.
        public int RecordTrap(string id,float mph,int stars,int[] prizes)
        {
            int paid=0;
            Change(p=>{
                p.traps??=new List<TrapRecord>();var r=p.traps.Find(t=>t.id==id);if(r==null){r=new TrapRecord{id=id};p.traps.Add(r);}
                bool better=mph>r.best+.05f;int earned=0;for(int s=r.stars;s<stars&&s<prizes.Length;s++)earned+=prizes[s];
                if(!better&&earned==0)return false;
                if(better)r.best=Mathf.Round(mph*10)/10;if(stars>r.stars)r.stars=stars;p.credits+=earned;paid=earned;return true;});
            return paid;
        }
        // A timed win (Beat the Train): keeps the best (lowest) time, pays the prize the first time. Returns credits paid.
        public int RecordTime(string id,float seconds,int prize)
        {
            int paid=0;
            Change(p=>{
                p.traps??=new List<TrapRecord>();var r=p.traps.Find(t=>t.id==id);if(r==null){r=new TrapRecord{id=id};p.traps.Add(r);}
                bool better=r.best<=0||seconds<r.best-.05f;int earned=r.stars<1?prize:0;
                if(!better&&earned==0)return false;
                if(better)r.best=Mathf.Round(seconds*10)/10;r.stars=1;p.credits+=earned;paid=earned;return true;});
            return paid;
        }
        // A rally school lesson (RallySchool): keeps the best (lowest) time and the best medal (stars 1-3), paying each newly
        // reached medal's prize once. Returns credits paid.
        public int RecordLesson(string id,float seconds,int medal,int[] prizes)
        {
            int paid=0;
            Change(p=>{
                p.traps??=new List<TrapRecord>();var r=p.traps.Find(t=>t.id==id);if(r==null){r=new TrapRecord{id=id};p.traps.Add(r);}
                bool better=r.best<=0||seconds<r.best-.05f;int earned=0;for(int s=r.stars;s<medal&&s<prizes.Length;s++)earned+=prizes[s];
                if(!better&&earned==0)return false;
                if(better)r.best=Mathf.Round(seconds*10)/10;if(medal>r.stars)r.stars=medal;p.credits+=earned;paid=earned;return true;});
            return paid;
        }
        public int BeginRace(){Change(p=>{p.raceSerial++;return true;});return Data.raceSerial;}
        // The Duke's Challenge (docs/54): settled once by serial like any race. The first win pays DukePrize and gives
        // you the Regale; later wins count as wins; a loss costs nothing.
        public const int DukePrize=50000;
        public int FinishDukeRace(int serial,int place)
        {
            int paid=0;
            Change(p=>{if(serial!=p.raceSerial||serial<=p.settledSerial)return false;
                p.settledSerial=serial;
                if(place==1){p.raceWins++;if(!p.collected.Contains(Collectibles.DukeBeaten)){p.collected.Add(Collectibles.DukeBeaten);p.credits+=DukePrize;paid=DukePrize;if(!p.bodies.Contains(CarBodies.Prototype))p.bodies.Add(CarBodies.Prototype);}}
                return true;});
            return paid;
        }
        // Pays a finished race once (by serial). The class's base prize is scaled for the venue, and a podium
        // is recorded towards completing the class.
        public int FinishRace(int serial,int place,string eventId="bayview")
        {
            var race=RaceCalendar.Get(eventId);int reward=(int)(EquipmentCatalog.Reward(place,race.raceClass)*race.rewardScale);
            return Change(p=>{if(reward==0||serial!=p.raceSerial||serial<=p.settledSerial)return false;
                p.settledSerial=serial;p.credits+=reward;p.bestPlace=p.bestPlace==0?place:Math.Min(place,p.bestPlace);if(place==1)p.raceWins++;
                if(place<=3&&!p.podiums.Contains(eventId))p.podiums.Add(eventId);return true;})?reward:0;
        }
    }
    public static class AdventureSession
    {
        public static AdventureStore Store;
        public static bool Racing;
        public static RaceRoster EnteredRoster;
        public static int RaceSerial,SmokePhase;
        public static string Notice;
        // Where free roam resumes after a race (null: the Bayview garage).
        public static Vector3? ReturnSpot;public static float ReturnYaw;
        static bool smokeHooked;
        public static bool Smoke=>RaceTrial.Has("--adventure-smoke")||RaceTrial.Has("--adventure-verify-save");
        // A different slot chosen mid-game (the pause menu's Save into another slot): the store follows it.
        public static void Reopen(string path){Store=new AdventureStore(path);}
        // Max build test drive (--max-build-test; Richard, 2 Oct: "a version where I can control the maxed out car"):
        // a copy of the live save, taken fresh each launch, with the maximum build bought and fitted, starting in the
        // castle courtyard by the Duke. It writes only its own file, so the real slots never see it.
        public static bool MaxBuildTest=>RaceTrial.Has("--max-build-test");
        public static string MaxBuildTestPath=>Path.Combine(SaveSlots.Folder,"little-road-max-build-test.json");
        public static bool TestPlaced;
        public static void FitMaxBuild(AdventureStore store)=>store.Change(p=>{
            foreach(var kv in EquipmentCatalog.MaxBuild()){if(!p.owned.Contains(kv.Value))p.owned.Add(kv.Value);var item=p.fitted.Find(f=>f.slot==kv.Key);if(item!=null)item.id=kv.Value;}
            return true;});
        public static void Load()
        {
            if(Smoke&&!smokeHooked){smokeHooked=true;Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception)Application.Quit(2);};}
            if(Store!=null)return;
            string path=RaceTrial.Arg("--adventure-save");
            if(Smoke&&string.IsNullOrEmpty(path))throw new InvalidOperationException("Smoke test requires its own --adventure-save path.");
            if(path==null)SaveSlots.Migrate();
            if(MaxBuildTest) {
                if(path==null){path=MaxBuildTestPath;if(File.Exists(SaveSlots.LivePath))File.Copy(SaveSlots.LivePath,path,true);else if(File.Exists(path))File.Delete(path);}
                Store=new AdventureStore(path);FitMaxBuild(Store);
                Notice="Test drive: the maximum build is fitted. The Duke is right here; nothing you do is saved to your slots.";
                return;
            }
            Store=new AdventureStore(path??SaveSlots.LivePath);
        }
        public static void StartRace()
        {
            StartRace(RaceRoster.Adventure(Store.Data));
        }
        public static void StartRace(RaceRoster roster)
        {
            // A running entry cannot be confirmed twice. A settled race can
            // enter the next previewed event directly from its results screen.
            if(Racing&&Store.Data.settledSerial<RaceSerial)return;
            if(roster==null||!roster.MatchesPlayer(Store.Data))throw new InvalidOperationException("Race entry changed; reopen the preview.");
            RaceSerial=Store.BeginRace();EnteredRoster=roster;Racing=true;Time.timeScale=1;LoadingScreen.Load(SceneManager.GetActiveScene().buildIndex,roster.Title);
        }
        // The pause menu's Restart race: the running entry is left unsettled (as retiring does) and the same event
        // is entered again from the grid.
        public static void RestartRace()
        {
            // (Richard, 4 Oct: Restart did nothing. The entered roster was seeded for the serial before BeginRace, so it no
            // longer matched the save and StartRace threw.) A fresh roster for the same event, seeded for the next serial.
            var roster=EnteredRoster;if(roster==null){ReturnToTown();return;}
            Racing=false;StartRace(RestartRoster());
        }
        public static RaceRoster RestartRoster()
        {
            var roster=EnteredRoster;if(roster==null)return null;
            return roster.Event==RaceRoster.DukeEvent?RaceRoster.DukeChallenge(Store.Data):RaceRoster.ForEvent(roster.Event,Store.Data);
        }
        public static void ReturnToTown()
        {
            Racing=false;EnteredRoster=null;Time.timeScale=1;LoadingScreen.Load(SceneManager.GetActiveScene().buildIndex,"Back on the road");
        }
    }
}
