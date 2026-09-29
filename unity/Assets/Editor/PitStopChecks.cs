using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Pit Stop services (every town) and the body shop economy.
public static class PitStopChecks
{
    public static List<string> Run(string evidenceRoot)
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("Pit Stop check failed: "+name);checks.Add(name);}
        // Kits: one silhouette, two sizes. Every bay has a host marker and a sign; the pylon has its brand and tyre.
        foreach(var (id,bays) in new[]{("pit_stop_2",new[]{"garage","parts"}),("pit_stop_3",new[]{"garage","parts","body"})}) {
            var kit=Resources.Load<GameObject>("Bayview/"+id);Check(kit!=null,"Kit "+id+" exported");
            var names=kit.GetComponentsInChildren<Transform>(true).Select(t=>t.name).ToList();
            Check(bays.All(b=>names.Contains("HostAnchor_"+b)&&names.Contains("SignAnchor_"+b))&&names.Contains("SignAnchor_brand"),id+" carries a host and a sign for every bay plus the pylon brand");
            Check(names.Count(n=>n.StartsWith("HostAnchor_"))==bays.Length,id+" has exactly "+bays.Length+" bays");
            Check(names.Any(n=>n.StartsWith("Spin_")),id+" pylon tyre spins");
        }
        // Spinning parts turn about their axle: a quarter turn of a wheel or a four-sail cross keeps its
        // world bounds; spinning about the wrong axis (the old local-Z spin) swaps them and clips the tower.
        foreach(var id in new[]{"pit_stop_2","windmill","wind_turbine"}) {
            var go=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Bayview/"+id));
            try {
                var part=go.GetComponentsInChildren<Transform>(true).First(t=>t.name.StartsWith("Spin_"));var r=part.GetComponent<Renderer>();
                var spin=part.gameObject.AddComponent<KitSpin>();spin.axle=part.name.StartsWith("Spin_X")?Vector3.right:Vector3.forward;spin.kit=go.transform;
                var before=r.bounds.size;spin.Step(90/spin.degreesPerSecond);var after=r.bounds.size;
                Check((before-after).magnitude<.06f*before.magnitude+.05f,$"{id} spins about its axle (bounds {before} -> {after} after a quarter turn)");
            } finally{UnityEngine.Object.DestroyImmediate(go);}
        }
        // Body catalogue: looks only, every body sold races in the standard envelope, prices rise to the Zenith.
        Check(CarBodies.ForSale.All(o=>CarBodies.UsesStandardEnvelope(o.id)&&o.price>0&&o.shop=="hillcrest"),"Every body for sale uses the standard racing envelope (looks only) and is sold at Hillcrest");
        Check(CarBodies.ForSale.Select(o=>o.id).Distinct().Count()==CarBodies.ForSale.Length&&!CarBodies.Sellable(CarBodies.Starter),"Body offers are unique and the starter is not for sale");
        Check(CarBodies.Price("zenith")==CarBodies.ForSale.Max(o=>o.price),"The Zenith GT is the top prize");
        string folder=Path.Combine(evidenceRoot,"bodies-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var store=new AdventureStore(Path.Combine(folder,"bodies.json"));
        Check(store.Data.bodies.SequenceEqual(new[]{CarBodies.Starter}),"A fresh profile owns only the starter body");
        Check(!store.BuyBody("pebble")&&!store.SwitchBody("pebble"),"Unaffordable body and unowned swap are refused");
        store.Change(p=>{p.credits=1000;return true;});
        Check(store.BuyBody("pebble")&&store.Data.credits==1000-CarBodies.Price("pebble")&&!store.BuyBody("pebble"),"A body is charged exactly once");
        Check(store.Data.bodyId==CarBodies.Starter,"Buying does not swap: the garage bay does");
        Check(store.SwitchBody("pebble")&&store.Data.bodyId=="pebble"&&!store.SwitchBody("pebble"),"Swapping to an owned body is free and changes the driven body");
        var tuning=EquipmentCatalog.Stats(store.Data.Loadout());
        store=new AdventureStore(store.path);
        Check(store.Data.bodyId=="pebble"&&store.Data.bodies.Contains(CarBodies.Starter)&&EquipmentCatalog.Stats(store.Data.Loadout()).power==tuning.power,"Owned bodies and the driven body survive reload; tuning is untouched");
        bool refused=false;try{store.BuyBody("formula_car");}catch(Exception){refused=true;}
        Check(!store.Data.bodies.Contains("formula_car"),"Bodies not in the catalogue cannot be bought"+(refused?" (refused)":""));
        // A save from before body shops (no `bodies`) migrates to owning exactly the body it drives.
        string legacy=Path.Combine(folder,"legacy.json");
        var old=JsonUtility.ToJson(AdventureProgress.New()).Replace("\"bodies\":[\"puddle\"],","");
        File.WriteAllText(legacy,old);var migrated=new AdventureStore(legacy);
        Check(!old.Contains("\"bodies\"")&&migrated.Data.bodies.SequenceEqual(new[]{migrated.Data.bodyId}),"Pre-body-shop saves own exactly the body they drive");
        // A save driving a retired placeholder body moves to the starter and keeps what else it owns.
        string retired=Path.Combine(folder,"retired.json");
        File.WriteAllText(retired,JsonUtility.ToJson(AdventureProgress.New()).Replace("\"bodyId\":\"puddle\"","\"bodyId\":\"coupe_car\"").Replace("\"bodies\":[\"puddle\"]","\"bodies\":[\"coupe_car\",\"pebble\",\"formula_car\"]"));
        var moved=new AdventureStore(retired);
        Check(moved.Data.bodyId==CarBodies.Starter&&moved.Data.bodies.SequenceEqual(new[]{"pebble",CarBodies.Starter})&&CarBodies.Retired.All(b=>!CarBodies.IsKnown(b)),"Saves on a retired body move to the starter and keep their other bodies");
        // Scratch profile: kept only when a check fails (the exception above leaves it as evidence).
        Directory.Delete(folder,true);
        return checks;
    }
}
