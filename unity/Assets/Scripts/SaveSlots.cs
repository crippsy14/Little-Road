using System;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Save slots (Richard, 30 Sep: choose a slot at the title screen to load from and save to, with autosaves kept
    // apart, as games do). Each of three slots holds its own autosave (the live store, written after every race,
    // purchase and find, as before) and a manual save (written from the pause menu). Continue opens the newest
    // autosave; loading a slot's manual save restores it over that slot's autosave. The save from before slots
    // (little-road-unity-save-v1.json) becomes slot 1's autosave; the file itself is left where it was.
    public static class SaveSlots
    {
        public const int Count=3;
        static string folder;
        // Tests point this at a scratch folder.
        public static string Folder {get=>folder??Application.persistentDataPath;set=>folder=value;}
        public static string Legacy=>Path.Combine(Folder,"little-road-unity-save-v1.json");
        public static string Manual(int slot)=>Path.Combine(Folder,$"little-road-slot-{slot}.json");
        public static string Auto(int slot)=>Path.Combine(Folder,$"little-road-slot-{slot}-autosave.json");
        static string CurrentFile=>Path.Combine(Folder,"little-road-slot.txt");
        // The slot being played (0: none chosen yet).
        public static int Current
        {
            get {try{return File.Exists(CurrentFile)&&int.TryParse(File.ReadAllText(CurrentFile).Trim(),out int s)&&s>=1&&s<=Count?s:0;}catch(IOException){return 0;}}
            set {Directory.CreateDirectory(Folder);File.WriteAllText(CurrentFile,value.ToString());}
        }
        // Where the live store writes.
        public static string LivePath=>Current>0?Auto(Current):Legacy;
        public static void Migrate()
        {
            bool any=false;for(int s=1;s<=Count;s++)any|=File.Exists(Auto(s))||File.Exists(Manual(s));
            if(!any&&File.Exists(Legacy)){Copy(Legacy,Auto(1));File.SetLastWriteTime(Auto(1),File.GetLastWriteTime(Legacy));Current=1;}
            else if(Current==0&&any)Current=Newest();
        }

        public readonly struct Info
        {
            public readonly bool exists,damaged;public readonly DateTime saved;public readonly string summary;
            public Info(bool exists,bool damaged,DateTime saved,string summary){this.exists=exists;this.damaged=damaged;this.saved=saved;this.summary=summary;}
            public string When=>exists?Ago(saved):"";
        }
        public static Info Read(string path)
        {
            if(!File.Exists(path))return new Info(false,false,default,"Empty");
            var saved=File.GetLastWriteTime(path);
            // (Through the store, which brings older saves up to date before checking them; it writes nothing.)
            try{return new Info(true,false,saved,Describe(new AdventureStore(path).Data));}
            catch(Exception){return new Info(true,true,saved,"This save can't be read");}
        }
        public static string Describe(AdventureProgress p)
        {
            int podiums=p.podiums?.Count??0;
            return $"Class {RaceCalendar.Open(p)} · {podiums} podium{(podiums==1?"":"s")} · {p.credits:N0} CR · {CarBodies.Name(p.bodyId)}";
        }
        static string Ago(DateTime t)
        {
            var d=DateTime.Now-t;
            if(d.TotalMinutes<1)return "just now";if(d.TotalHours<1)return $"{(int)d.TotalMinutes} min ago";
            if(t.Date==DateTime.Today)return "today "+t.ToString("HH:mm");if(t.Date==DateTime.Today.AddDays(-1))return "yesterday "+t.ToString("HH:mm");
            return t.ToString("d MMM HH:mm");
        }
        // The slot whose autosave (or, without one, manual save) was written last; 0 when there are none.
        public static int Newest()
        {
            // (Ties, two saves in one clock tick, go to the slot being played.)
            int best=0,current=Current;DateTime when=DateTime.MinValue;
            for(int s=1;s<=Count;s++)foreach(var path in new[]{Auto(s),Manual(s)}){if(!File.Exists(path))continue;var t=File.GetLastWriteTime(path);if(t>when||t==when&&s==current){when=t;best=s;}}
            return best;
        }
        public static bool Occupied(int slot)=>File.Exists(Auto(slot))||File.Exists(Manual(slot));

        // Play a slot: its autosave, or its manual save restored over the autosave (the old autosave kept as .bak).
        public static void Load(int slot,bool manual)
        {
            if(manual)Copy(Manual(slot),Auto(slot));
            else if(!File.Exists(Auto(slot))&&File.Exists(Manual(slot)))Copy(Manual(slot),Auto(slot));
            Current=slot;
        }
        // A fresh game in a slot; anything already there is kept as .bak.
        public static void NewGame(int slot)
        {
            Directory.CreateDirectory(Folder);
            if(File.Exists(Manual(slot)))File.Copy(Manual(slot),Manual(slot)+".bak",true);
            if(File.Exists(Manual(slot)))File.Delete(Manual(slot));
            Write(Auto(slot),JsonUtility.ToJson(AdventureProgress.New(),true));Current=slot;
        }
        // The pause menu's Save: the game as it stands into a slot's manual save. Saving into another slot moves the
        // game there (its autosave too), so it keeps saving where it was last saved.
        public static void SaveTo(AdventureStore live,int slot)
        {
            Copy(live.path,Manual(slot));
            if(slot!=Current){Copy(live.path,Auto(slot));Current=slot;}
        }
        static void Copy(string from,string to){Write(to,File.ReadAllText(from));File.SetLastWriteTime(to,DateTime.Now);}
        static void Write(string path,string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));string temporary=path+".tmp";File.WriteAllText(temporary,text);
            if(File.Exists(path))File.Replace(temporary,path,path+".bak",true);else File.Move(temporary,path);
        }
    }
}
