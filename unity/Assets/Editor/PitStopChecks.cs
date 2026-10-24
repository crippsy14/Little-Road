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
        // (the Forge range since 23 Oct: the off-roaders are the gravel cars; the namesakes of the old bodies at their old prices)
        bool forge=CarBodies.RosterEnabled;string F(string id)=>CarBodies.Namesake(id);
        Check(CarBodies.ForSale.All(o=>CarBodies.UsesStandardEnvelope(o.id)&&o.price>0&&(o.shop=="hillcrest"&&CarBodies.Forged(o.id)?.surface!="gravel"||o.shop=="quarry"&&(forge?CarBodies.Forged(o.id)?.surface=="gravel":System.Array.IndexOf(CarBodies.OffRoad,o.id)>=0)||o.shop=="tuner"&&CarBodies.Class(o.id)=="B")),"Every body for sale uses the standard racing envelope (looks only) and is sold at Hillcrest (Quarry Hills sells its off-roaders, the Tuner Garage the Tuners)");
        Check(CarBodies.ForSale.Select(o=>o.id).Distinct().Count()==CarBodies.ForSale.Length&&!CarBodies.Sellable(CarBodies.StarterBody),"Body offers are unique and the starter is not for sale");
        // (the Zenith and the old namesakes only while they are installed: the roster was cleared on 24 Oct, to be rebuilt)
        if(!forge||CarBodies.Forged(F("zenith"))!=null)Check(CarBodies.Price(F("zenith"))==CarBodies.ForSale.Where(o=>CarBodies.Class(o.id)=="C").Max(o=>o.price),"The Zenith GT is class C's top prize");
        Check(CarBodies.Price("shiro")==CarBodies.ForSale.Where(o=>CarBodies.Class(o.id)=="B").Max(o=>o.price),"The Shiro GT is class B's top prize (the halo car, docs/65)");
        if(forge) {
            var sold=CarBodies.ForSale.Where(o=>o.shop!="tuner").Select(o=>o.id).ToList();
            Check(CarBodies.Roster.All(id=>id==CarBodies.ForgeStarter||sold.Contains(id))&&sold.All(id=>CarBodies.Roster.Contains(id)),$"Every Car Forge roster car but the starter is sold ({sold.Count}), and only roster cars");
            Check(new[]{"puddle","pebble","comet","zenith","gelato","rally","cob","fell"}.All(id=>CarBodies.Price(F(id))==CarBodies.Price(id)||id=="puddle"||CarBodies.Forged(F(id))==null),"The old bodies' namesakes keep the old prices");
            Check(CarBodies.ForSale.Where(o=>o.shop!="tuner").All(o=>CarBodies.Name(o.id)!=o.id&&CarBodies.About(o.id)!=null&&CarBodies.Signature(o.id)!=null),"Every Forge car for sale has a name, what it is and its signature");
        }
        string folder=Path.Combine(evidenceRoot,"bodies-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var store=new AdventureStore(Path.Combine(folder,"bodies.json"));string pebble=forge?CarBodies.ForSale.First(o=>o.shop=="hillcrest").id:F("pebble"),starter=CarBodies.StarterBody;   // (the Snoozer since 24 Oct)
        Check(store.Data.bodies.SequenceEqual(new[]{starter}),"A fresh profile owns only the starter body");
        Check(!store.BuyBody(pebble)&&!store.SwitchBody(pebble),"Unaffordable body and unowned swap are refused");
        store.Change(p=>{p.credits=1000;return true;});
        Check(store.BuyBody(pebble)&&store.Data.credits==1000-CarBodies.Price(pebble)&&!store.BuyBody(pebble),"A body is charged exactly once");
        Check(store.Data.bodyId==starter,"Buying does not swap: the garage bay does");
        Check(store.SwitchBody(pebble)&&store.Data.bodyId==pebble&&!store.SwitchBody(pebble),"Swapping to an owned body is free and changes the driven body");
        var tuning=EquipmentCatalog.Stats(store.Data.Loadout());
        store=new AdventureStore(store.path);
        Check(store.Data.bodyId==pebble&&store.Data.bodies.Contains(starter)&&EquipmentCatalog.Stats(store.Data.Loadout()).power==tuning.power,"Owned bodies and the driven body survive reload; tuning is untouched");
        // A save from before the Forge models (old body ids) loads on their namesakes, keeping everything else.
        if(forge) {
            string older=Path.Combine(folder,"older.json");
            // (since 24 Oct: the Puddle, and the Patchwork's replacement the Snoozer; a Tuner stays a Tuner)
            File.WriteAllText(older,JsonUtility.ToJson(AdventureProgress.New()).Replace("\"bodyId\":\""+starter+"\"","\"bodyId\":\"forge_patchwork\"").Replace("\"bodies\":[\""+starter+"\"]","\"bodies\":[\"puddle\",\"forge_patchwork\",\"sakura\",\"forge_snoozer\"]"));
            var up=new AdventureStore(older);
            Check(up.Data.bodyId=="forge_snoozer"&&up.Data.bodies.SequenceEqual(new[]{"forge_puddle","forge_snoozer","sakura"}),"An old save's bodies become their Forge namesakes and replacements (a Tuner stays; no duplicates): "+string.Join(",",up.Data.bodies));
            Check(up.Change(p=>{p.credits+=1;return true;})&&new AdventureStore(older).Data.bodyId=="forge_snoozer","The migrated save writes back and reloads");
        }
        bool refused=false;try{store.BuyBody("formula_car");}catch(Exception){refused=true;}
        Check(!store.Data.bodies.Contains("formula_car"),"Bodies not in the catalogue cannot be bought"+(refused?" (refused)":""));
        // A save from before body shops (no `bodies`) migrates to owning exactly the body it drives.
        string legacy=Path.Combine(folder,"legacy.json");
        var old=JsonUtility.ToJson(AdventureProgress.New()).Replace("\"bodies\":[\""+starter+"\"],","");
        File.WriteAllText(legacy,old);var migrated=new AdventureStore(legacy);
        Check(!old.Contains("\"bodies\"")&&migrated.Data.bodies.SequenceEqual(new[]{migrated.Data.bodyId}),"Pre-body-shop saves own exactly the body they drive");
        // A save driving a retired placeholder body moves to the starter and keeps what else it owns.
        string retired=Path.Combine(folder,"retired.json");
        File.WriteAllText(retired,JsonUtility.ToJson(AdventureProgress.New()).Replace("\"bodyId\":\""+starter+"\"","\"bodyId\":\"coupe_car\"").Replace("\"bodies\":[\""+starter+"\"]","\"bodies\":[\"coupe_car\",\""+pebble+"\",\"formula_car\"]"));
        var moved=new AdventureStore(retired);
        Check(moved.Data.bodyId==starter&&moved.Data.bodies.SequenceEqual(new[]{pebble,starter})&&CarBodies.Retired.All(b=>!CarBodies.IsKnown(b)),"Saves on a retired body move to the starter and keep their other bodies");
        // Scratch profile: kept only when a check fails (the exception above leaves it as evidence).
        Directory.Delete(folder,true);
        return checks;
    }
}
