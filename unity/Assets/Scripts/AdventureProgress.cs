using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleRoad
{
    [Serializable] public sealed class FittedPart {public string slot,id;}
    [Serializable] public sealed class AdventureProgress
    {
        public int version,credits,questStage,paintIndex,raceSerial,settledSerial,bestPlace,raceWins;
        public string bodyId;
        public List<string> owned,collected;
        public List<FittedPart> fitted;
        public static AdventureProgress New()=>new AdventureProgress {version=1,bodyId="compact_v1",
            owned=EquipmentCatalog.StockLoadout().Values.ToList(),collected=new List<string>(),
            fitted=EquipmentCatalog.StockLoadout().Select(p=>new FittedPart{slot=p.Key,id=p.Value}).ToList()};
        public Dictionary<string,string> Loadout()=>fitted.ToDictionary(p=>p.slot,p=>p.id);
        public void Validate()
        {
            if(version!=1||credits<0||credits>10000000||questStage<0||questStage>3||paintIndex<0||paintIndex>3||
                raceSerial<0||settledSerial<0||settledSerial>raceSerial||raceWins<0||raceWins>settledSerial||bestPlace<0||bestPlace>24)
                throw new InvalidDataException("Save version or progression values are invalid.");
            if(!new[]{"compact_v1","coupe_car","utility_car","formula_car"}.Contains(bodyId))throw new InvalidDataException("Unknown body.");
            if(owned==null||collected==null||fitted==null||owned.Distinct().Count()!=owned.Count||collected.Distinct().Count()!=collected.Count)
                throw new InvalidDataException("Missing or duplicate inventory entries.");
            foreach(string id in owned)EquipmentCatalog.Get(id);
            foreach(string id in collected)if(!new[]{"postcard_0","postcard_1","postcard_2","postcard_3","postcard_4"}.Contains(id))throw new InvalidDataException("Unknown collectible.");
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
        public bool Collect(string id)=>Change(p=>{if(p.collected.Contains(id))return false;p.collected.Add(id);p.credits+=50;return true;});
        public bool Buy(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(p.owned.Contains(id)||p.credits<part.price)return false;p.credits-=part.price;p.owned.Add(id);return true;});
        public bool Fit(string id)=>Change(p=>{var part=EquipmentCatalog.Get(id);if(!p.owned.Contains(id))return false;var item=p.fitted.Find(f=>f.slot==part.slot);if(item.id==id)return false;item.id=id;return true;});
        public int BeginRace(){Change(p=>{p.raceSerial++;return true;});return Data.raceSerial;}
        public int FinishRace(int serial,int place)
        {
            int reward=EquipmentCatalog.Reward(place);
            return Change(p=>{if(reward==0||serial!=p.raceSerial||serial<=p.settledSerial)return false;
                p.settledSerial=serial;p.credits+=reward;p.bestPlace=p.bestPlace==0?place:Math.Min(place,p.bestPlace);if(place==1)p.raceWins++;return true;})?reward:0;
        }
    }
    public static class AdventureSession
    {
        public static AdventureStore Store;
        public static bool Racing;
        public static RaceRoster EnteredRoster;
        public static int RaceSerial,SmokePhase;
        public static string Notice;
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
            RaceSerial=Store.BeginRace();EnteredRoster=roster;Racing=true;Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public static void ReturnToTown()
        {
            Racing=false;EnteredRoster=null;Time.timeScale=1;SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
