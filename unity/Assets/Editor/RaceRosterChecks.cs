using System;
using System.Linq;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class RaceRosterChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("Race roster: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();var fit=EquipmentCatalog.TestLoadout("club");
        var identity=DriverIdentity.ForRace(1,1709,"mixed",true);
        var a=new RaceEntrant(1,0,"compact_v1",Color.red,"club",fit,identity);
        fit["engine"]="engine_stock";
        Require(a.Parts["engine"]=="engine_club"&&a.Tuning.Equals(CarTuning.Club),"entry aliases mutable source equipment");
        bool rejected=false;try{((IDictionary<string,string>)a.Parts)["engine"]="engine_stock";}catch(NotSupportedException){rejected=true;}
        Require(rejected,"public loadout can be modified");
        var b=new RaceEntrant(1,0,"pebble",Color.blue,"club",a.Parts,DriverIdentity.ForRace(1,2005,"mixed",true));
        Require(a.Rating==b.Rating&&a.Tuning.Equals(b.Tuning),"body/paint/driver inflate rating or equipment");
        checks.Add("Race entries own immutable validated loadouts; body, paint and driver have no equipment-rating bonus");

        int stock=CarPerformanceRating.Calculate(CarTuning.Stock),street=CarPerformanceRating.Calculate(CarTuning.Street),club=CarPerformanceRating.Calculate(CarTuning.Club);
        Require(stock==100&&stock<street&&street<club,"stock/street/club rating order invalid");
        var lowPower=CarTuning.Stock;lowPower.gearCeiling=100;var excessive=lowPower;excessive.gearCeiling=200;
        Require(CarPerformanceRating.Calculate(lowPower)==CarPerformanceRating.Calculate(excessive),"unreachable gearing grants rating");
        var roadTyres=EquipmentCatalog.StockLoadout();roadTyres["tyres"]="tyres_sport";
        Require(CarPerformanceRating.Calculate(EquipmentCatalog.Stats(roadTyres))>stock,"road grip upgrade not reflected");
        checks.Add($"Dry-road rating has equipment ordering ({stock}/{street}/{club}) and no unbounded reward for unreachable gearing");

        // Class C fields race class C bodies only (Richard, 14 Oct): B bodies, and the <id>_tuner kits that move a C car onto
        // the B chassis, belong to class B. Every C venue, every star.
        {var c0=AdventureProgress.New();foreach(var field in new[]{RaceRoster.Adventure(c0),RaceRoster.AdventureHillcrest(c0),RaceRoster.AdventureQuarry(c0)})
            foreach(var e in field.Entries)Require(Array.IndexOf(CarBodies.Tuner,e.Body)<0&&Array.IndexOf(CarBodies.TunerOffRoad,e.Body)<0&&!e.Body.EndsWith("_tuner"),"class B body "+e.Body+" in a class C field ("+field.Title+")");}
        {var c1=AdventureProgress.New();var low=RaceRoster.Adventure(c1);var high=RaceRoster.AdventureHillcrest(c1);Require(low.Entries.All(e=>Array.IndexOf(CarBodies.TopC,e.Body)<0)&&high.Entries.All(e=>Array.IndexOf(CarBodies.TopC,e.Body)<0)&&!CarBodies.TopCOpen("C",7)&&CarBodies.TopCOpen("C",8)&&CarBodies.TopCOpen("B",1),"the Rally saloon and Zenith GT race only the 8-10 star class C events");}
        var save=AdventureProgress.New();var roster=RaceRoster.Adventure(save);var same=RaceRoster.Adventure(save);
        Require(roster.Entries.Count==24&&roster.Entries[0].Slot==23&&roster.MatchesPlayer(save)&&save.raceSerial==0,"preview changes save or wrong grid");
        for(int i=0;i<24;i++)Require(roster.Entries[i].Driver.Planning.Same(in same.Entries[i].Driver.Planning),"reopening preview rerolls form");
        save.owned.Add("engine_workshop");save.fitted.Find(p=>p.slot=="engine").id="engine_workshop";
        Require(!roster.MatchesPlayer(save),"stale player fit accepted");var upgraded=RaceRoster.Adventure(save);
        for(int i=1;i<24;i++)Require(roster.Entries[i].Tuning.Equals(upgraded.Entries[i].Tuning)&&roster.Entries[i].Driver.Planning.Same(in upgraded.Entries[i].Driver.Planning),"player upgrade changes rivals");
        Require(upgraded.Entries[0].Rating>roster.Entries[0].Rating,"actual player upgrade absent in preview");
        checks.Add("Adventure preview starts player24th, does not spend a race serial/re-roll form, rejects stale equipment and fixes opponents across upgrades");

        var fresh=AdventureProgress.New();Require(fresh.bodyId==CarBodies.StarterBody,"new save does not start in the starter car");
        foreach(var track in new[]{"cedar","bayview"}) {
            var trialField=RaceRoster.Trial(24,1,1709,track,"stock",Array.Empty<string>(),"field","mixed",true);
            Require(trialField.Entries[0].Body=="compact_v1","trial car 0 is not the compact master");
            foreach(var e in trialField.Entries)Require(e.Id==0||CarBodies.UsesStandardEnvelope(e.Body),"trial entrant outside the standard envelope: "+e.Body);
        }
        checks.Add("Trial fields race the compact plus generated bodies, all in the standard envelope");
        foreach(var serial in new[]{0,1,2,7}) {
            fresh.raceSerial=serial;fresh.settledSerial=serial;var field=RaceRoster.Adventure(fresh,true,false); /* the old field (--no-field-recipe); the recipe has FieldRecipeChecks */int flagships=0;var used=new HashSet<string>();
            foreach(var e in field.Entries) {
                if(e.Id==0)continue;used.Add(e.Body);
                if(e.Slot==0){Require(e.Body==CarBodies.Flagship&&e.EquipmentLabel=="club","pole car is not the flagship on club equipment");flagships++;}
                else if(e.EquipmentLabel=="club")Require(Array.IndexOf(CarBodies.Sporty,e.Body)>=0,"club entrant not in a sporty body: "+e.Body);
                else Require(Array.IndexOf(CarBodies.Family,e.Body)>=0,"street entrant not in a family body: "+e.Body);
                Require(e.Body!=CarBodies.Flagship||e.Slot==0,"flagship livery appears off pole");
            }
            Require(flagships==1,"expected exactly one flagship");
            foreach(var body in CarBodies.Family)Require(used.Contains(body),"family model missing from field: "+body);
            Require(field.Entries[1].Rating>=field.Entries[2].Rating,"pole car slower than its neighbour");
        }
        foreach(var body in CarBodies.Family)Require(CarBodies.UsesStandardEnvelope(body),"family body outside standard envelope: "+body);
        foreach(var body in CarBodies.Sporty)Require(CarBodies.UsesStandardEnvelope(body),"sporty body outside standard envelope: "+body);
        Require(CarBodies.UsesStandardEnvelope(CarBodies.Flagship)&&CarBodies.UsesStandardEnvelope(CarBodies.Starter)&&!CarBodies.UsesStandardEnvelope("compact_v1"),"envelope set wrong");
        var probe=TrialWorld.Car(CarBodies.Flagship,Color.white);
        try{Require(probe.GetComponent<StandardEnvelope>()!=null,"flagship car missing its standard envelope marker");}finally{UnityEngine.Object.DestroyImmediate(probe);}
        checks.Add("Generated bodies share one physical envelope; body shape grants no collision advantage");
        checks.Add("Adventure field matches bodies to tier: one liveried Zenith on pole with club kit, sporty club cars, every family model among street cars");

        var oldRoster=AdventureSession.EnteredRoster;bool oldRacing=AdventureSession.Racing;
        try {
            AdventureSession.EnteredRoster=roster;AdventureSession.Racing=true;
            // Racing AI v2 (docs/46) with recovery, drafting and the roster.
            Require(RaceAiPreset.Enabled("--physical-recovery")&&RaceAiPreset.Enabled("--light-drafting")&&RaceAiPreset.Enabled("--driver-roster")
                &&RaceAiPreset.Value("--driver-policy",null)=="racer"&&RaceAiPreset.Value("--racing-line",null)=="bayview-smooth-v1","modern adventure missing retained package");
            AdventureSession.Racing=false;Require(!RaceAiPreset.Enabled("--physical-recovery")&&RaceAiPreset.Value("--driver-policy","legacy")=="legacy","race preset leaks into exploration");
        }finally{AdventureSession.EnteredRoster=oldRoster;AdventureSession.Racing=oldRacing;}
        checks.Add("Connected race uses Racing AI v2 with recovery, drafting and the roster; driver flags stop applying on return to exploration");
        return checks;
    }
}
