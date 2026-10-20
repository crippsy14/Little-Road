using System;
using System.Linq;

namespace LittleRoad
{
    // The career ladder. A class opens once the class before it is complete, and only once it has races of its own
    // (Richard, 30 Sep: a first playthrough sees only its own tier and below), so the ladder waits at a completed class
    // until the next one's events exist.
    // Class C completes with a podium at every C event. Class B (docs/67, Richard 7 Oct) races today's three tracks
    // forwards and reversed, each event rated 1-10 stars over the whole map; the stars of the events you've won are your
    // progress (they unlock the Tuner parts, "stars:B:N"), so beating a harder race through skill moves you on faster.
    public static class RaceCalendar
    {
        public static readonly string[] Classes={"C","B","A","S","E"};
        public readonly struct Event
        {
            public readonly string id,venue,raceClass,track;public readonly float rewardScale;public readonly int stars,laps;public readonly bool reversed;
            public Event(string id,string venue,string raceClass,float rewardScale,int stars,int laps,string track=null,bool reversed=false)
            {this.id=id;this.venue=venue;this.raceClass=raceClass;this.rewardScale=rewardScale;this.stars=stars;this.laps=laps;this.track=track??id;this.reversed=reversed;}
            public string Stars=>new string('★',stars);
        }
        // Class C event ids are their tracks (RaceRoster.Track); class B's name theirs.
        static readonly Event[] C={
            new("bayview","Bayview Coast","C",1f,1,3),
            new("hillcrest","Hillcrest Raceway","C",1.5f,2,2),
            // Quarry Hills Rallycross (--quarry-hills): loose slate on the quarry floor, the town's off-roaders.
            new("quarry","Quarry Hills Rallycross","C",1.8f,3,3),
            // Bramblewick Lanes (--bramblewick, docs/82): the village street and Church Lane, 2★ (docs/70), three laps.
            new("bramblewick","Bramblewick Lanes","C",1.5f,2,3)};
        // Class B on today's tracks (docs/67): each town an easier event forwards and a harder one reversed. The planned
        // B map is 26 events and 120 stars (docs/67, "The B map"); these six are the near end of it, 12 stars, and the
        // Tuner Championship is the 10-star finale. Pay rises 15% a star.
        static readonly Event[] B={
            new("b_coast","Coast Tuners","B",1f,1,3,"bayview"),
            new("b_valley","Valley Tuners","B",1f,1,2,"hillcrest"),
            new("b_harbour_rev","Harbour Reverse","B",1.15f,2,3,"bayview",true),
            new("b_slate","Slate Rallycross","B",1.15f,2,3,"quarry"),
            new("b_raceway_rev","Raceway Reverse","B",1.3f,3,2,"hillcrest",true),
            new("b_rallycross_rev","Rallycross Reverse","B",1.3f,3,3,"quarry",true)};
        public static readonly Event[] All=C.Where(e=>(e.track!="quarry"||QuarryHills.Enabled)&&(e.track!="bramblewick"||Bramblewick.Enabled)).Concat(QuarryHills.Enabled?B:B.Where(e=>e.track!="quarry")).ToArray();
        // Every event, Quarry Hills' included whether or not --quarry-hills is on (field recipes and their checks).
        public static Event[] Every=>C.Concat(B).ToArray();
        public static int StarsOf(string id)=>Every.First(e=>e.id==id).stars;
        public static Event Get(string id){foreach(var e in All)if(e.id==id)return e;throw new ArgumentException("Unknown race: "+id);}
        public static bool Known(string id)=>All.Any(e=>e.id==id);
        // Any event the game has, whether or not its place is switched on: a save keeps a Bramblewick podium when it's
        // loaded without --bramblewick (it just doesn't count until the place is back).
        public static bool Recorded(string id)=>Every.Any(e=>e.id==id);
        // A town's events of one class, easiest first (the garage offers them).
        public static Event[] AtTrack(string track,string raceClass)=>All.Where(e=>e.track==track&&e.raceClass==raceClass).OrderBy(e=>e.stars).ToArray();
        public static int Rank(string raceClass)=>Array.IndexOf(Classes,string.IsNullOrEmpty(raceClass)?"C":raceClass);
        public static int Races(string raceClass)=>All.Count(e=>e.raceClass==raceClass);
        public static int Podiums(AdventureProgress p,string raceClass)=>All.Count(e=>e.raceClass==raceClass&&p.podiums!=null&&p.podiums.Contains(e.id));
        public static bool Won(AdventureProgress p,string id)=>p.wins!=null&&p.wins.Contains(id);
        // The stars of every event of the class you've won, each event once.
        public static int StarsWon(AdventureProgress p,string raceClass)=>All.Where(e=>e.raceClass==raceClass&&Won(p,e.id)).Sum(e=>e.stars);
        public static int StarsOnOffer(string raceClass)=>All.Where(e=>e.raceClass==raceClass).Sum(e=>e.stars);
        // Class C: a podium at every event. Class B finishes with the Tuner Championship (docs/67), which isn't built yet,
        // so B stays open (and A shut, having no races).
        public static bool Complete(AdventureProgress p,string raceClass)=>raceClass=="C"?Races("C")>0&&Podiums(p,"C")==Races("C"):false;
        // The highest class open to the player: C, then each class after a completed one.
        public static string Open(AdventureProgress p){int i=0;while(i<Classes.Length-1&&Complete(p,Classes[i])&&Races(Classes[i+1])>0)i++;return Classes[i];}
        public static bool PartOpen(AdventureProgress p,EquipmentCatalog.Part part)=>Rank(part.tier)<=Rank(Open(p));
        // One line for menus: where the player is on the ladder.
        public static string Progress(AdventureProgress p)
        {
            string open=Open(p);int races=Races(open),done=Podiums(p,open);int next=Rank(open)+1;
            if(next>=Classes.Length)return $"Class {open} · the top of the ladder";
            if(open=="B")return $"Class B · {StarsWon(p,"B")}★ won of {StarsOnOffer("B")}★ · win Tuner races to unlock Tuner parts";
            // What comes next stays unnamed: the player's own class and below is all the game shows.
            if(Complete(p,open))return $"Class {open} complete · new races and parts are on the way";
            return $"Class {open} · podiums {done}/{races} · a podium at every Class {open} race opens new races and parts";
        }
    }
}
