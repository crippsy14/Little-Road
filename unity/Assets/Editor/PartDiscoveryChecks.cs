using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Part discovery (PartDiscovery.cs; docs/56 §21): found at a part's home shop, then sold everywhere and fitted anywhere;
// unlock conditions gatekeep parts until met.
public static class PartDiscoveryChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var home=EquipmentCatalog.All.parts.Where(p=>p.shop=="hillcrest").ToList();
        Check(home.Count>0&&EquipmentCatalog.All.parts.All(p=>PartDiscovery.Valid(p.unlock)),$"Every part's unlock condition is well formed ({home.Count} parts have a home shop: Hillcrest)");
        string path=Path.Combine(Application.temporaryCachePath,"part-discovery-check.json");if(File.Exists(path))File.Delete(path);
        try {
            var store=new AdventureStore(path);var s=store.Data;var valley=home[0];
            Check(!PartDiscovery.Found(s,valley)&&!PartDiscovery.OnSale(s,valley,"bayview")&&PartDiscovery.OnSale(s,valley,"hillcrest"),$"A new game doesn't find {valley.name} in Bayview; Hillcrest's parts counter shows it");
            var found=PartDiscovery.Visit(store,"hillcrest");
            Check(found.Count==home.Count(p=>PartDiscovery.UnlockMet(store.Data,p))&&PartDiscovery.OnSale(store.Data,valley,"bayview")&&PartDiscovery.Visit(store,"hillcrest").Count==0,
                $"Visiting Hillcrest's parts counter finds its {found.Count} parts once; then Bayview sells them too");
            Check(!PartDiscovery.Met(store.Data,"wins:2")&&PartDiscovery.Need("wins:2")=="Win 2 races"&&PartDiscovery.Need("podiums:B:3")=="3 podiums at class B races"&&PartDiscovery.Need("class:A")=="Reach class A"&&!PartDiscovery.Valid("bogus:1"),
                "Unlock conditions (wins, podiums in a class, class reached) read plainly and hold parts back until met");
            store.Change(p=>{p.raceWins=2;p.settledSerial=Math.Max(p.settledSerial,2);p.raceSerial=Math.Max(p.raceSerial,2);return true;});
            Check(PartDiscovery.Met(store.Data,"wins:2"),"A met condition releases its part (found on the next visit to its shop)");
        } finally {if(File.Exists(path))File.Delete(path);}
        return checks;
    }
}
