using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Per-car racing lines (CarLines, --car-lines; docs/71): on Hillcrest Raceway's real geometry, a stock class C car's and
// the free Tuner's own lines lap faster for that car than the shared smooth line, stay inside the corridor, bake quickly,
// and differ from each other.
public static class CarLinesChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Car lines: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();
        var road=new Circuit(HillcrestRoute.Circuit);
        var shared=new RacingLine(road,RacingLine.BakeSmooth(road,"hillcrest-smooth-v1","hillcrest"));
        var stock=EquipmentCatalog.Stats(EquipmentCatalog.StockLoadout());
        var good=EquipmentCatalog.StockLoadout();foreach(var slot in EquipmentCatalog.TunerOrder)good[slot]=EquipmentCatalog.TunerSteps[slot][0];var tuner=EquipmentCatalog.Stats(good);
        Require(CarLines.Group(stock)!=CarLines.Group(tuner),"a stock C car and the free Tuner share a performance group");
        var results=new List<string>();var made=new List<RacingLine>();
        foreach(var (name,tuning) in new[]{("stock C",stock),("free Tuner",tuner)}) {
            var watch=Stopwatch.StartNew();
            var data=CarLines.Bake(road,shared,tuning,"check-"+name,out float before,out float after);long ms=watch.ElapsedMilliseconds;
            var line=new RacingLine(road,data);made.Add(line);   // (the constructor checks the corridor and direction)
            float sharedLap=new RacingSpeedProfile(shared,tuning).predictedLapTime,ownLap=new RacingSpeedProfile(line,tuning).predictedLapTime;
            Require(after<=before+.001f,$"{name}: the optimiser made its own point-mass lap slower ({before:0.00} -> {after:0.00} s)");
            Require(ownLap<sharedLap,$"{name}: its own line isn't faster for it by the speed profile ({sharedLap:0.00} s shared, {ownLap:0.00} s own)");
            Require(data.offsets.All(o=>Mathf.Abs(o)<=RacingLine.MaxOffset+.01f),$"{name}: line leaves the corridor");
            Require(ms<2000,$"{name}: bake took {ms} ms");
            results.Add($"{name} {sharedLap:0.00}->{ownLap:0.00} s ({ms} ms)");
        }
        float differ=0;for(int i=0;i<made[0].Count&&i<made[1].Count;i++)differ=Mathf.Max(differ,Mathf.Abs(made[0].data.offsets[i]-made[1].data.offsets[i]));
        Require(differ>.2f,"the stock C car's and the Tuner's lines are the same");
        // Bayview reversed (8 Oct: a baked line cut a tight bend past the corridor and stopped the race): every line builds.
        var harbour=new Circuit().Reversed();var harbourShared=new RacingLine(harbour,RacingLine.BakeSmooth(harbour,"bayview-reversed-smooth-v1","bayview"));
        foreach(var (name,tuning) in new[]{("stock C",stock),("free Tuner",tuner)}){var d=CarLines.Bake(harbour,harbourShared,tuning,"check-rev-"+name,out _,out _);try{_=new RacingLine(harbour,d);}catch(Exception e){Require(false,$"{name}'s line on Bayview reversed fails its checks: {e.Message}");}}
        checks.Add($"Per-car lines (default; --no-car-lines) build on Bayview reversed; on Hillcrest Raceway: {string.Join(", ",results)}; corridor +-{RacingLine.MaxOffset} m; lines differ by up to {differ:0.0} m");
        return checks;
    }
}
