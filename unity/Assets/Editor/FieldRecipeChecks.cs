using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LittleRoad;
using UnityEngine;

// Field recipes (FieldRecipe, --field-recipe; docs/70; Richard, 7 Oct): driver ratings by class, the best drivers near
// the front with a few good ones further back, and kits from each race's part window. Writes docs/data/field-recipes.json,
// the per-star table behind docs/70's reference (tools/field-table.py).
public static class FieldRecipeChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Field recipe: "+why);}
    static int Dr(RaceEntrant e)=>RacerRadar.DriverRating(RacerAbilities.From(e.Driver));
    [Serializable] class Planned {public string place_id,track,name,@event,disc;public int stars;}
    [Serializable] class PlannedData {public Planned[] C,B;}
    static readonly int[] Seeds={1709,1710,1711};
    static int Pp(Dictionary<string,string> kit)=>CarPerformanceRating.Calculate(EquipmentCatalog.Stats(kit));
    public static List<string> Run()
    {
        var checks=new List<string>();
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        var plan=JsonUtility.FromJson<PlannedData>(File.ReadAllText(Path.Combine(root,"docs/data/race-stars.json")));
        Require(plan.C.Length==21&&plan.B.Length==26,"planned race list changed: update docs/70");

        // The stars you'd have won before each B star level: every easier race on the planned map.
        for(int s=1;s<=10;s++)Require(FieldRecipe.BStarsBefore[s]==plan.B.Where(e=>e.stars<s).Sum(e=>e.stars),$"BStarsBefore[{s}] out of step with docs/data/race-stars.json");
        // The calendar's events match the plan's stars and surfaces.
        foreach(var p in plan.C.Concat(plan.B).Where(p=>!string.IsNullOrEmpty(p.@event))) {
            var ev=RaceCalendar.Every.First(e=>e.id==p.@event);Require(ev.stars==p.stars&&FieldRecipe.Discipline(ev.track)==p.disc,$"{p.@event} differs from the plan ({ev.stars}★ {FieldRecipe.Discipline(ev.track)} against {p.stars}★ {p.disc})");
        }

        // Every built race, on three serials.
        var save=AdventureProgress.New();
        foreach(int serial in new[]{0,1,2}) {
            save.raceSerial=serial;save.settledSerial=serial;
            var rosters=new List<(RaceRoster r,string cls,int stars,string disc)>{
                (RaceRoster.Adventure(save,true,true),"C",1,"road"),(RaceRoster.AdventureHillcrest(save,true,true),"C",2,"road"),(RaceRoster.AdventureQuarry(save,true,true),"C",3,"gravel"),(RaceRoster.AdventureBramblewick(save,true,true),"C",2,"road"),(RaceRoster.AdventureVinelands(save,true),"C",3,"road")};
            foreach(var ev in RaceCalendar.Every.Where(e=>e.raceClass=="B"))rosters.Add((RaceRoster.AdventureTuner(save,ev,true,true),"B",ev.stars,FieldRecipe.Discipline(ev.track)));
            foreach(var (roster,cls,stars,disc) in rosters) {
                var (lo,hi)=FieldRecipe.DriverBand(cls);var ai=roster.Entries.Skip(1).ToList();
                Require(ai.All(e=>Dr(e)>=lo&&Dr(e)<=hi),$"{roster.Event}: driver ratings outside {lo}-{hi}: "+string.Join(",",ai.Select(Dr)));
                double front=ai.Where(e=>e.Slot<8).Average(Dr),back=ai.Where(e=>e.Slot>=15).Average(Dr);
                Require(front>back+.15*(hi-lo),$"{roster.Event}: the front ({front:0.0}) isn't stronger than the back ({back:0.0})");
                Require(ai.Count(e=>e.Slot>=12&&Dr(e)>=hi)>=1,$"{roster.Event}: no good driver starts in the back half");
                foreach(var e in ai)foreach(var slot in EquipmentCatalog.All.slots) {
                    var open=FieldRecipe.Available(cls,disc,stars,slot);var (f,c)=FieldRecipe.Window(cls,disc,stars,slot);int at=Array.IndexOf(open,e.Parts[slot]);
                    Require(at>=f&&at<=c,$"{roster.Event} car {e.Id} has {e.Parts[slot]} outside its {slot} window");
                }
                Require(roster.Entries[0].Slot==23&&roster.MatchesPlayer(save),$"{roster.Event}: player not 24th on their own car");
                Require(cls=="C"||ai.All(e=>Array.IndexOf(disc=="gravel"?CarBodies.TunerGravel:CarBodies.Tuner,e.Body)>=0),$"{roster.Event}: non-Tuner bodies in a class B field");
            }
        }
        // Richard's two ground truths: Bayview class C runs stock to Bayview's garage; Bayview's first B race runs the
        // best class C part to Good.
        Require(FieldRecipe.Available("C","road",1,"engine").First()=="engine_stock"&&!FieldRecipe.Available("C","road",1,"engine").Contains("engine_valley"),"Bayview class C isn't stock to Bayview's garage");
        Require(FieldRecipe.Available("B","road",1,"tyres").SequenceEqual(new[]{"tyres_b_sport"})&&FieldRecipe.Available("B","road",1,"engine").SequenceEqual(new[]{"engine_valley","engine_b_twincam"}),"Bayview class B isn't best class C to Good");
        var plain=RaceRoster.Adventure(save,true,false);Require(plain.Entries.Skip(1).All(e=>e.Driver.TargetAbility<0),"recipe applied with the flag off");
        // The traffic corner margin (--traffic-corner-margin, on with the recipe): 92% sits at the old roster's best Line
        // (0.85) and below a DR 17-20 driver's own corner use, so it only bites for the new top drivers, in a pack.
        var line85=RacerAbilities.Default;line85.line=.85f;var line100=RacerAbilities.Default;line100.line=1f;
        Require(Math.Abs(line85.CornerUse-.92f)<.005f&&line100.CornerUse>.92f,"traffic corner cap no longer sits at the old roster's best Line");
        // Hillcrest Raceway's corner barriers (docs/70): both corners where class B cars left the circuit have armco outside.
        var circuit=HillcrestRoute.Circuit;var caught=CoastalLandscape.CatchSides(circuit,CoastalLandscape.RacewayCatchRadius,CoastalLandscape.RacewayCatchReach);
        foreach(float metres in new[]{240f,930f}){int i=Mathf.RoundToInt(metres/1.5f);Require(caught[i,0]||caught[i,1],$"no corner barrier at Hillcrest Raceway {metres} m");}
        int railed=0;for(int i=0;i<circuit.Length;i++)if(caught[i,0]||caught[i,1])railed++;
        Require(railed<circuit.Length*.8f,$"corner barriers line {railed*100/circuit.Length}% of the raceway: the straights should stay open");
        // --grip-budget (docs/71): a lane change's peak sideways acceleration (6 shift v^2 / L^2) fits the spare grip.
        float L=RacerDriverPolicy.LaneChangeDistance(4,40,3);
        Require(Math.Abs(6*4*40*40/(L*L)-3)<.01f&&RacerDriverPolicy.LaneChangeDistance(4,40,1.5f)>L&&RacerDriverPolicy.LaneChangeDistance(4,20,3)<L,"lane-change pacing doesn't fit the spare grip");
        var shaped=RacerAbilities.Default.WithMean(.9f);
        Require(Math.Abs((shaped.braking+shaped.line+shaped.control+shaped.consistency+shaped.racecraft)/5-.9f)<.002f,"WithMean misses its mean");

        // The table: every class, surface and star level on the plan; PP of the 23 rivals over three seeds.
        var json=new StringBuilder("{\n \"note\": \"Written by FieldRecipeChecks (docs/70). Per class, surface and stars: each slot's window (floor and ceiling part) and the PP of the 23 rivals over seeds 1709-1711. PP is the dry-road rating (CarPerformanceRating); gravel fields are rated as if on tarmac. Ice uses the road ladder until ice tyres exist.\",\n \"rows\": [\n");
        var rows=new List<string>();var mean=new Dictionary<string,double>();
        foreach(string cls in new[]{"C","B"})foreach(string disc in new[]{"road","gravel","ice"})foreach(int stars in cls=="C"?Enumerable.Range(1,8):Enumerable.Range(1,8).Append(10)) {
            string ladderDisc=disc=="ice"?"road":disc;var pps=new List<int>();
            foreach(int seed in Seeds)for(int slot=0;slot<23;slot++)pps.Add(Pp(FieldRecipe.Kit(cls,ladderDisc,stars,slot,seed,out _)));
            var floor=EquipmentCatalog.StockLoadout();var ceiling=EquipmentCatalog.StockLoadout();var win=new List<string>();
            foreach(string slot in EquipmentCatalog.All.slots) {
                var open=FieldRecipe.Available(cls,ladderDisc,stars,slot);var (f,c)=FieldRecipe.Window(cls,ladderDisc,stars,slot);floor[slot]=open[f];ceiling[slot]=open[c];
                win.Add($"\"{slot}\":[\"{EquipmentCatalog.Get(open[f]).name}\",\"{EquipmentCatalog.Get(open[c]).name}\"]");
            }
            mean[$"{cls}/{disc}/{stars}"]=pps.Average();
            rows.Add(string.Format(CultureInfo.InvariantCulture,"  {{\"class\":\"{0}\",\"disc\":\"{1}\",\"stars\":{2},\"ppMin\":{3},\"ppMean\":{4:0.0},\"ppMax\":{5},\"ppFloor\":{6},\"ppCeiling\":{7},\"window\":{{{8}}}}}",
                cls,disc,stars,pps.Min(),pps.Average(),pps.Max(),Pp(floor),Pp(ceiling),string.Join(",",win)));
        }
        json.Append(string.Join(",\n",rows)).Append("\n ],\n");
        var starter=EquipmentCatalog.StockLoadout();foreach(var slot in EquipmentCatalog.TunerOrder)starter[slot]=EquipmentCatalog.TunerSteps[slot][0];
        json.Append($" \"playerStockPp\": {Pp(EquipmentCatalog.StockLoadout())},\n \"playerFreeTunerPp\": {Pp(starter)}\n}}\n");
        // The difficulty curve rises with the stars in every class and surface (the step to 10 included).
        foreach(string cls in new[]{"C","B"})foreach(string disc in new[]{"road","gravel"}) {
            var curve=(cls=="C"?Enumerable.Range(1,8):Enumerable.Range(1,8).Append(10)).Select(s=>mean[$"{cls}/{disc}/{s}"]).ToList();
            for(int i=1;i<curve.Count;i++)Require(curve[i]>=curve[i-1]-.5,$"class {cls} {disc}: mean PP falls between star levels ({string.Join(", ",curve.Select(v=>v.ToString("0")))})");
        }
        string path=Path.Combine(root,"docs/data/field-recipes.json"),text=json.ToString();
        if(!File.Exists(path)||File.ReadAllText(path)!=text)File.WriteAllText(path,text);
        checks.Add($"Field recipe (default; --no-field-recipe for the old fields): all nine built races keep skill 2-3 (C) / 4-5 (B), front stronger than back with a good driver in the back half, every part inside its window; Bayview C stock to garage and Bayview B best C to Good (on Good tyres); mean PP rises with the stars ({string.Join("/",Enumerable.Range(1,8).Select(s=>mean[$"C/road/{s}"].ToString("0")))} C, {string.Join("/",Enumerable.Range(1,8).Append(10).Select(s=>mean[$"B/road/{s}"].ToString("0")))} B); docs/data/field-recipes.json written");
        return checks;
    }
}
