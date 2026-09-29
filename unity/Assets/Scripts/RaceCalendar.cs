using System;
using System.Linq;

namespace LittleRoad
{
    // The career ladder. Every town's race is Class C for now, each a step quicker and better paid than the
    // last (the fields run that town's parts). A podium at every Class C venue completes the class and opens
    // Class B: its races and its bracket of parts. B then opens A, and A opens S (with the endurance race's
    // own parts). A class with no races yet can't be completed, so the ladder stops there until they exist.
    public static class RaceCalendar
    {
        public static readonly string[] Classes={"C","B","A","S"};
        public readonly struct Event
        {
            public readonly string id,venue,raceClass;public readonly float rewardScale;
            public Event(string id,string venue,string raceClass,float rewardScale){this.id=id;this.venue=venue;this.raceClass=raceClass;this.rewardScale=rewardScale;}
        }
        // Event ids are the race tracks (RaceRoster.Track).
        public static readonly Event[] All={
            new("bayview","Bayview Coast","C",1f),
            new("hillcrest","Hillcrest Raceway","C",1.5f)};
        public static Event Get(string id){foreach(var e in All)if(e.id==id)return e;throw new ArgumentException("Unknown race: "+id);}
        public static bool Known(string id)=>All.Any(e=>e.id==id);
        public static int Rank(string raceClass)=>Array.IndexOf(Classes,string.IsNullOrEmpty(raceClass)?"C":raceClass);
        public static int Races(string raceClass)=>All.Count(e=>e.raceClass==raceClass);
        public static int Podiums(AdventureProgress p,string raceClass)=>All.Count(e=>e.raceClass==raceClass&&p.podiums!=null&&p.podiums.Contains(e.id));
        public static bool Complete(AdventureProgress p,string raceClass)=>Races(raceClass)>0&&Podiums(p,raceClass)==Races(raceClass);
        // The highest class open to the player: C, then each class after a completed one.
        public static string Open(AdventureProgress p){int i=0;while(i<Classes.Length-1&&Complete(p,Classes[i]))i++;return Classes[i];}
        public static bool PartOpen(AdventureProgress p,EquipmentCatalog.Part part)=>Rank(part.tier)<=Rank(Open(p));
        // One line for menus: where the player is on the ladder.
        public static string Progress(AdventureProgress p)
        {
            string open=Open(p);int races=Races(open),done=Podiums(p,open);int next=Rank(open)+1;
            if(next>=Classes.Length)return $"Class {open} · the top of the ladder";
            return races==0?$"Class {open} open · its races are on the way":$"Class {open} · podiums {done}/{races} · a podium at every Class {open} race opens Class {Classes[next]}";
        }
    }
}
