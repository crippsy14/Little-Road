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
        public const int Postcards=5,Stamps=8;
        public const string VisitHillcrest="visit_hillcrest",CastleTrial="trial_castle",CastleTrialHard="trial_castle_hard";
        public static string Postcard(int i)=>"postcard_"+i;
        public static string Stamp(int i)=>"stamp_"+i;
        public static bool IsPostcard(string id)=>Indexed(id,"postcard_",Postcards);
        public static bool IsStamp(string id)=>Indexed(id,"stamp_",Stamps);
        public static bool Known(string id)=>IsPostcard(id)||IsStamp(id)||id==VisitHillcrest||id==CastleTrial||id==CastleTrialHard;
        static bool Indexed(string id,string prefix,int count)
        {
            if(id==null||!id.StartsWith(prefix,StringComparison.Ordinal))return false;
            for(int i=0;i<count;i++)if(id==prefix+i)return true;
            return false;
        }
    }
    [Serializable] public sealed class AdventureProgress
    {
        public int version,credits,questStage,paintIndex,raceSerial,settledSerial,bestPlace,raceWins;
        public string bodyId;
        public List<string> owned,collected;
        // Bodies bought at Pit Stop body shops (bodyId is the one being driven). Older saves: see AdventureStore.
        public List<string> bodies;
        public List<FittedPart> fitted;
        // Race events (RaceCalendar ids) finished on the podium: a podium at every race of a class opens the next.
        public List<string> podiums;
        // Fog of war (ExploredMap: base64 bit mask) and the places whose "discovered" card has been shown.
        public string explored;public List<string> discovered;
        public static AdventureProgress New()=>new AdventureProgress {version=1,bodyId=CarBodies.Starter,podiums=new List<string>(),discovered=new List<string>(),
            owned=EquipmentCatalog.StockLoadout().Values.ToList(),collected=new List<string>(),bodies=new List<string>{CarBodies.Starter},
            fitted=EquipmentCatalog.StockLoadout().Select(p=>new FittedPart{slot=p.Key,id=p.Value}).ToList()};
        public Dictionary<string,string> Loadout()=>fitted.ToDictionary(p=>p.slot,p=>p.id);
        public void Validate()
        {
            if(version!=1||credits<0||credits>10000000||questStage<0||questStage>3||paintIndex<0||paintIndex>=PaintCatalog.All.Length||
                raceSerial<0||settledSerial<0||settledSerial>raceSerial||raceWins<0||raceWins>settledSerial||bestPlace<0||bestPlace>24)
                throw new InvalidDataException("Save version or progression values are invalid.");
            if(!CarBodies.Ownable(bodyId))throw new InvalidDataException("Unknown body.");
            if(bodies==null||bodies.Distinct().Count()!=bodies.Count||!bodies.Contains(bodyId)||bodies.Any(b=>!CarBodies.Ownable(b)))throw new InvalidDataException("Owned bodies are missing, duplicated or unknown.");
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
        public bool Repaint(int index,int price)=>Change(p=>{if(index==p.paintIndex||index<0||index>=PaintCatalog.All.Length||price<0||p.credits<price)return false;p.credits-=price;p.paintIndex=index;return true;});
        // Body shop: buy a body once; switching bodies (garage bay) is free and changes looks only.
        public bool BuyBody(string id)=>Change(p=>{int price=CarBodies.Price(id);if(price<0||p.bodies.Contains(id)||p.credits<price)return false;p.credits-=price;p.bodies.Add(id);return true;});
        public bool SwitchBody(string id)=>Change(p=>{if(!p.bodies.Contains(id)||p.bodyId==id)return false;p.bodyId=id;return true;});
        public bool Discover(string place)=>Change(p=>{if(p.discovered.Contains(place))return false;p.discovered.Add(place);return true;});
        public bool SaveExplored(string mask)=>Change(p=>{if(p.explored==mask)return false;p.explored=mask;return true;});
        public bool Buy(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(p.owned.Contains(id)||p.credits<part.price)return false;p.credits-=part.price;p.owned.Add(id);return true;});
        public bool Fit(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(!p.owned.Contains(id))return false;var item=p.fitted.Find(f=>f.slot==part.slot);if(item.id==id)return false;item.id=id;return true;});
        public int BeginRace(){Change(p=>{p.raceSerial++;return true;});return Data.raceSerial;}
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
        public static void Load()
        {
            if(Smoke&&!smokeHooked){smokeHooked=true;Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception)Application.Quit(2);};}
            if(Store!=null)return;
            string path=RaceTrial.Arg("--adventure-save");
            if(Smoke&&string.IsNullOrEmpty(path))throw new InvalidOperationException("Smoke test requires its own --adventure-save path.");
            Store=new AdventureStore(path??Path.Combine(Application.persistentDataPath,"little-road-unity-save-v1.json"));
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
        public static void ReturnToTown()
        {
            Racing=false;EnteredRoster=null;Time.timeScale=1;LoadingScreen.Load(SceneManager.GetActiveScene().buildIndex,"Back on the road");
        }
    }
}
