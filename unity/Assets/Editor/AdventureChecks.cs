using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

public static class AdventureChecks
{
    public static List<string> Run(string evidenceRoot)
    {
        var checks=new List<string>();string folder=Path.Combine(evidenceRoot,"profiles-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var store=new AdventureStore(Path.Combine(folder,"roundtrip.json"));
        void Check(bool result,string title){if(!result)throw new Exception("Adventure check failed: "+title);checks.Add(title);}
        Check(store.Data.credits==0&&store.Data.fitted.Count==7,"Fresh profile has zero credits and seven stock slots");
        Check(!store.Buy("engine_workshop")&&!store.Fit("engine_workshop"),"Unaffordable purchase and unowned fitting rejected");
        Check(!store.Deliver()&&!store.ClaimDelivery(),"Delivery cannot skip acceptance or hand-in");
        Check(store.AcceptDelivery()&&!store.AcceptDelivery(),"Parcel accepted only once");
        store=new AdventureStore(store.path);Check(store.Data.questStage==1,"Accepted delivery survives reload");
        Check(store.Deliver()&&!store.Deliver()&&store.ClaimDelivery()&&!store.ClaimDelivery(),"Hand-in and delivery reward are idempotent");
        Check(store.Data.credits==350,"Delivery preserves original 350-credit reward");
        Check(store.Collect("postcard_0")&&!store.Collect("postcard_0")&&store.Collect("postcard_1"),"Postcards award only once per stable ID");
        Check(store.Buy("engine_workshop")&&store.Data.credits==0&&!store.Buy("engine_workshop"),"Part ownership charges exactly once");
        Check(store.Data.Loadout()["engine"]=="engine_stock"&&store.Fit("engine_workshop"),"Buying and fitting are separate operations");
        store=new AdventureStore(store.path);
        Check(store.Data.Loadout()["engine"]=="engine_workshop"&&EquipmentCatalog.Stats(store.Data.Loadout()).power>CarTuning.Stock.power,"Owned fitted engine persists and increases actual tuning power");
        int abandoned=store.BeginRace(),serial=store.BeginRace();
        Check(store.FinishRace(abandoned,1)==0&&store.FinishRace(serial,0)==0&&store.FinishRace(serial,25)==0,"Old race IDs and invalid places cannot pay");
        Check(store.FinishRace(serial,24)==100,"Last finisher receives 100 credits");
        store=new AdventureStore(store.path);Check(store.FinishRace(serial,24)==0&&store.Data.credits==100&&store.Data.bestPlace==24,"Race reward and record survive reload without duplicate payment");
        // Class ladder: every current race is Class C; podiums at both open Class B (which has no races yet, so
        // the ladder stops there). Hillcrest pays 1.5x Bayview for the same place.
        Check(RaceCalendar.All.All(e=>e.raceClass=="C")&&EquipmentCatalog.All.parts.All(p=>RaceCalendar.Rank(p.tier)>=0)&&RaceCalendar.Classes.All(c=>EquipmentCatalog.All.parts.Any(p=>p.tier==c)),"Every race is Class C for now; the parts ladder covers all five classes");
        Check(RaceCalendar.Open(store.Data)=="C"&&!store.Data.podiums.Contains("bayview"),"A 24th place is no podium; Class C is open");
        int s2=store.BeginRace();Check(store.FinishRace(s2,3,"bayview")==330&&store.Data.podiums.SequenceEqual(new[]{"bayview"})&&RaceCalendar.Open(store.Data)=="C","A Bayview podium pays 330 and counts one of two Class C podiums");
        int s3=store.BeginRace();Check(store.FinishRace(s3,2,"hillcrest")==600&&RaceCalendar.Complete(store.Data,"C")&&RaceCalendar.Open(store.Data)=="C","A Hillcrest podium pays 1.5x and completes Class C; Class B stays shut until it has races");
        store=new AdventureStore(store.path);Check(RaceCalendar.Complete(store.Data,"C")&&store.Data.podiums.Count==2,"Podiums and the completed class survive reload");
        Check(!EquipmentCatalog.All.parts.Any(p=>RaceCalendar.Rank(p.tier)>0&&RaceCalendar.PartOpen(store.Data,p)),"Parts above Class C stay locked while Class B has no races");
        // Save slots (docs/50): the pre-slot save becomes slot 1's autosave; a new game, a save into another slot and
        // loading each land where they should; a slot's summary reads its class and credits.
        {
            string slots=Path.Combine(folder,"slots");Directory.CreateDirectory(slots);SaveSlots.Folder=slots;
            try {
                File.Copy(store.path,SaveSlots.Legacy);SaveSlots.Migrate();
                Check(File.Exists(SaveSlots.Auto(1))&&SaveSlots.Current==1&&SaveSlots.Read(SaveSlots.Auto(1)).summary.StartsWith("Class C")&&SaveSlots.Read(SaveSlots.Auto(1)).summary.Contains("CR"),"Save slots: the old save becomes slot 1's autosave");
                SaveSlots.NewGame(2);Check(SaveSlots.Current==2&&new AdventureStore(SaveSlots.LivePath).Data.credits==0&&!File.Exists(SaveSlots.Manual(2)),"Save slots: a new game starts fresh in its own slot");
                var live=new AdventureStore(SaveSlots.LivePath);live.Collect("postcard_2");SaveSlots.SaveTo(live,2);
                Check(new AdventureStore(SaveSlots.Manual(2)).Data.credits==50,"Save slots: Save writes the slot's saved game");
                SaveSlots.SaveTo(live,3);Check(SaveSlots.Current==3&&File.Exists(SaveSlots.Manual(3))&&File.Exists(SaveSlots.Auto(3))&&SaveSlots.Newest()==3,"Save slots: saving into another slot moves the game there");
                live=new AdventureStore(SaveSlots.Auto(2));live.Collect("postcard_3");SaveSlots.Load(2,true);
                Check(SaveSlots.Current==2&&new AdventureStore(SaveSlots.Auto(2)).Data.credits==50&&File.Exists(SaveSlots.Auto(2)+".bak"),"Save slots: loading a saved game restores it over the autosave, keeping a backup");
                SaveSlots.NewGame(1);Check(new AdventureStore(SaveSlots.Auto(1)).Data.credits==0&&File.Exists(SaveSlots.Auto(1)+".bak"),"Save slots: starting over keeps the old game as a backup");
            }
            finally{SaveSlots.Folder=null;}
        }
        // Speed traps (docs/51): seven cameras on straight road, marks rising from what the stock car can reach to
        // what the best Class C kit can; a record keeps the best and pays each star once.
        {
            var traps=SpeedTraps.Plan(new Circuit());
            Check(traps.Select(t=>t.id).SequenceEqual(SpeedTraps.ActiveIds),$"Speed traps: all {SpeedTraps.ActiveIds.Length} cameras placed ({string.Join(", ",traps.Select(t=>$"{t.id} {t.marks[0]}/{t.marks[1]}/{t.marks[2]}"))} mph)");
            Check(traps.All(t=>t.marks[0]>=35&&t.marks[0]<t.marks[1]&&t.marks[1]<t.marks[2]&&t.marks[2]<=160),"Speed traps: marks rise from the stock car's reach to the best Class C kit's");
            var trapStore=new AdventureStore(Path.Combine(folder,"traps.json"));int c0=trapStore.Data.credits;
            int paid=trapStore.RecordTrap("quay",60,2,SpeedTraps.Prizes);int again=trapStore.RecordTrap("quay",58,2,SpeedTraps.Prizes);int third=trapStore.RecordTrap("quay",80,3,SpeedTraps.Prizes);
            trapStore=new AdventureStore(trapStore.path);var rec=SpeedTraps.Record(trapStore.Data,"quay");
            Check(paid==SpeedTraps.Prizes[0]+SpeedTraps.Prizes[1]&&again==0&&third==SpeedTraps.Prizes[2]&&rec.best==80&&rec.stars==3&&trapStore.Data.credits==c0+SpeedTraps.Prizes.Sum(),"Speed traps: each star pays once and the best speed is kept");
            bool bad=false;try{trapStore.Change(p=>{p.traps.Add(new TrapRecord{id="moon",best=1,stars=1});return true;});}catch(InvalidDataException){bad=true;}
            Check(bad,"Speed traps: an unknown trap in a save is rejected");
        }
        // Beat the Train (docs/51): the road route joins the two forecourts; the stock car's ideal time is sane; a win
        // pays once and keeps the lowest time.
        {
            var route=TrainRace.Route(true);float length=SpeedProfile.Length(route),stock=TrainRace.StockTime(true),back=TrainRace.StockTime(false);
            Check(length>1200&&length<5000&&Vector3.Distance(route[0],CoastalLandscape.HaltArrival)<1&&Vector3.Distance(route[^1],HillcrestRoute.StationYard)<8,$"Beat the Train: road route {length:0} m from Bayview Halt to Hillcrest station");
            Check(stock>60&&stock<600&&back>60&&back<600,$"Beat the Train: stock car's ideal {stock:0} s there, {back:0} s back");
            var raceStore=new AdventureStore(Path.Combine(folder,"train.json"));int c0=raceStore.Data.credits;
            int first=raceStore.RecordTime("train-hillcrest",150,TrainRace.Prize),slower=raceStore.RecordTime("train-hillcrest",160,TrainRace.Prize),faster=raceStore.RecordTime("train-hillcrest",140,TrainRace.Prize);
            raceStore=new AdventureStore(raceStore.path);var r=SpeedTraps.Record(raceStore.Data,"train-hillcrest");
            Check(first==TrainRace.Prize&&slower==0&&faster==0&&r.best==140&&raceStore.Data.credits==c0+TrainRace.Prize,"Beat the Train: the first win pays once; the best time is the lowest");
        }
        store.Change(p=>{p.podiums.Clear();return true;});
        string before=File.ReadAllText(store.path);bool rejected=false;
        try{store.Change(p=>{p.owned.Add("unknown-part");return true;});}catch(ArgumentException){rejected=true;}
        Check(rejected&&File.ReadAllText(store.path)==before&&!store.Data.owned.Contains("unknown-part"),"Invalid candidate leaves live state and disk unchanged");
        var failure=new AdventureStore(Path.Combine(folder,"write-failure.json"));Directory.CreateDirectory(failure.path+".tmp");rejected=false;
        try{failure.AcceptDelivery();}catch(IOException){rejected=true;}catch(UnauthorizedAccessException){rejected=true;}
        Check(rejected&&failure.Data.questStage==0&&!File.Exists(failure.path),"Failed disk commit does not award or advance live progress");
        string broken=Path.Combine(folder,"invalid.json");File.WriteAllText(broken,"{\"version\":99}");rejected=false;
        try{new AdventureStore(broken);}catch(Exception){rejected=true;}
        Check(rejected&&File.ReadAllText(broken)=="{\"version\":99}","Unsupported save is rejected without overwrite");
        Check(File.Exists(store.path+".bak"),"Atomic replacement retains previous save backup");
        // Collectibles registry: every id the world can hand out is one Validate accepts, and nothing else.
        var keeps=new AdventureStore(Path.Combine(folder,"collectibles.json"));bool allKept=true;
        for(int i=0;i<Collectibles.Postcards;i++)allKept&=keeps.Collect(Collectibles.Postcard(i));
        for(int i=0;i<Collectibles.Stamps;i++)allKept&=keeps.Collect(Collectibles.Stamp(i));
        allKept&=keeps.ClaimTrial(Collectibles.VisitHillcrest,0)&&keeps.ClaimTrial(Collectibles.CastleTrial,300);
        keeps=new AdventureStore(keeps.path);
        Check(allKept&&keeps.Data.collected.Count==Collectibles.Postcards+Collectibles.Stamps+2,"Every registered collectible can be collected and survives reload");
        int refused=0;foreach(string id in new[]{"postcard_"+Collectibles.Postcards,"stamp_"+Collectibles.Stamps,"stamp_-1","stamp_+1","mystery"})
            try{keeps.Collect(id);}catch(InvalidDataException){refused++;}
        Check(refused==5&&keeps.Data.collected.Count==Collectibles.Postcards+Collectibles.Stamps+2,"Unregistered collectible ids are refused and change nothing");
        var paints=new AdventureStore(Path.Combine(folder,"paint.json"));
        Check(!paints.Repaint(5,150)&&paints.Data.paintIndex==0,"Repaint refused without credits");
        paints.Change(p=>{p.credits=400;return true;});
        Check(!paints.Repaint(0,150)&&paints.Repaint(5,150)&&paints.Data.credits==250&&!paints.Repaint(5,150),"Repaint charges once and refuses the current colour");
        paints=new AdventureStore(paints.path);Check(paints.Data.paintIndex==5&&paints.Data.credits==250,"Repaint persists across reload");
        Check(PaintCatalog.Standard==16&&PaintCatalog.All.Length==PaintCatalog.Standard+PaintCatalog.Rewards.Length&&PaintCatalog.All[0].color==new Color(.08f,.56f,.49f),"Paint catalogue keeps the original first colours for old saves");
        // Keepsake set rewards (docs/60): the set's paint is locked until every keepsake in it is found.
        Check(!paints.Repaint(PaintCatalog.Standard,0)&&PaintCatalog.LockedBy(paints.Data,PaintCatalog.Standard)=="Bayview postcards","A set reward paint is locked until its set is complete");
        Check(PaintCatalog.RewardFor(Collectibles.Carving(3))==18&&PaintCatalog.RewardFor(Collectibles.Stamp(0))==17&&PaintCatalog.RewardFor("duke_beaten")==-1,"Each keepsake set has its own reward paint");
        // World tiles (W1): far decoration waits on its tile with its spot reserved; near decoration builds at once; Complete builds the rest.
        int built=0;WorldTiles.Plan(Vector3.zero);WorldTiles.Begin(true);
        WorldTiles.Place(new Vector3(100,0,0),0,()=>built++);WorldTiles.Place(new Vector3(2000,0,600),1.5f,()=>built++);
        bool queued=built==1&&WorldTiles.Pending==1&&WorldTiles.Blocked(new Vector3(2001,0,600),1)&&!WorldTiles.Blocked(new Vector3(2010,0,600),1);
        WorldTiles.Complete();bool done=built==2&&WorldTiles.Pending==0&&!WorldTiles.Blocked(new Vector3(2001,0,600),1);
        WorldTiles.Begin(false);WorldTiles.Place(new Vector3(2000,0,600),1.5f,()=>built++);
        Check(queued&&done&&built==3,"World tiles: far decoration waits on its tile (spot reserved), near builds at once, races never wait");
        Check(Wayfinding.Miles(1609)=="1"&&Wayfinding.Miles(300)=="¼"&&Wayfinding.Miles(2414)=="1½"&&Wayfinding.Miles(1207)=="¾","Fingerpost distances read in quarter miles");
        // Scratch profile: kept only when a check fails (the exception above leaves it as evidence).
        Directory.Delete(folder,true);
        return checks;
    }
}
