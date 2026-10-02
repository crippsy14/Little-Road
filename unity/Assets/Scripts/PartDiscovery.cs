using System.Collections.Generic;
using System.Linq;

namespace LittleRoad
{
    // Part discovery (Richard, 2 Oct): a part with a home shop (EquipmentCatalog.Part.shop) is found the first time you
    // visit that parts shop, and from then on every parts shop sells it and every garage fits it, so Bayview can be a
    // sandbox for everything you've found. A part can also carry an unlock condition (Part.unlock) that "gatekeeps" it:
    // until it's met, its home shop shows it locked with the condition; it's found on the next visit after.
    //   unlock: "wins:5" (race wins), "podiums:B:3" (podiums at class B events), "class:A" (class A open). Empty: none.
    // Class gating still applies on top: only your open class and below are on sale (RaceCalendar.PartOpen).
    public static class PartDiscovery
    {
        public static bool Everywhere(EquipmentCatalog.Part p)=>string.IsNullOrEmpty(p.shop);
        // Found: sold everywhere (no home shop), owned (older saves: what you bought is yours anywhere), or discovered.
        public static bool Found(AdventureProgress s,EquipmentCatalog.Part p)=>Everywhere(p)||s.owned.Contains(p.id)||s.partsFound!=null&&s.partsFound.Contains(p.id);
        // On the shelves at `shop`: found, or at home here (shown locked while its condition isn't met).
        public static bool OnSale(AdventureProgress s,EquipmentCatalog.Part p,string shop)=>Found(s,p)||p.shop==shop;
        public static bool UnlockMet(AdventureProgress s,EquipmentCatalog.Part p)=>Met(s,p.unlock);
        public static bool Met(AdventureProgress s,string unlock)
        {
            if(string.IsNullOrEmpty(unlock))return true;var f=unlock.Split(':');
            return f[0] switch{
                "wins"=>f.Length==2&&int.TryParse(f[1],out int w)&&s.raceWins>=w,
                "podiums"=>f.Length==3&&int.TryParse(f[2],out int n)&&RaceCalendar.Podiums(s,f[1])>=n,
                "class"=>f.Length==2&&RaceCalendar.Rank(RaceCalendar.Open(s))>=RaceCalendar.Rank(f[1]),
                _=>false};
        }
        public static bool Valid(string unlock)
        {
            if(string.IsNullOrEmpty(unlock))return true;var f=unlock.Split(':');
            return f[0]=="wins"&&f.Length==2&&int.TryParse(f[1],out _)||f[0]=="podiums"&&f.Length==3&&RaceCalendar.Rank(f[1])>=0&&int.TryParse(f[2],out _)||f[0]=="class"&&f.Length==2&&RaceCalendar.Rank(f[1])>=0;
        }
        // The condition, said plainly ("Win 5 races", "3 podiums in class B", "Reach class A").
        public static string Need(string unlock)
        {
            if(string.IsNullOrEmpty(unlock))return "";var f=unlock.Split(':');
            return f[0] switch{"wins"=>$"Win {f[1]} race{(f[1]=="1"?"":"s")}","podiums"=>$"{f[2]} podium{(f[2]=="1"?"":"s")} at class {f[1]} races","class"=>$"Reach class {f[1]}",_=>unlock};
        }
        // A visit to `shop`'s parts counter: everything at home here whose condition is met is found (once). Returns them.
        public static List<EquipmentCatalog.Part> Visit(AdventureStore store,string shop)
        {
            var fresh=EquipmentCatalog.All.parts.Where(p=>p.shop==shop&&!Found(store.Data,p)&&UnlockMet(store.Data,p)).ToList();
            if(fresh.Count>0)store.Change(s=>{s.partsFound??=new List<string>();foreach(var p in fresh)if(!s.partsFound.Contains(p.id))s.partsFound.Add(p.id);return true;});
            return fresh;
        }
    }
}
