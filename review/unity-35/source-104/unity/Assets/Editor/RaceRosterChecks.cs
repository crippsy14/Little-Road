using System;
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
        var b=new RaceEntrant(1,0,"formula_car",Color.blue,"club",a.Parts,DriverIdentity.ForRace(1,2005,"mixed",true));
        Require(a.Rating==b.Rating&&a.Tuning.Equals(b.Tuning),"body/paint/driver inflate rating or equipment");
        checks.Add("Race entries own immutable validated loadouts; body, paint and driver have no equipment-rating bonus");

        int stock=CarPerformanceRating.Calculate(CarTuning.Stock),street=CarPerformanceRating.Calculate(CarTuning.Street),club=CarPerformanceRating.Calculate(CarTuning.Club);
        Require(stock==100&&stock<street&&street<club,"stock/street/club rating order invalid");
        var lowPower=CarTuning.Stock;lowPower.gearCeiling=100;var excessive=lowPower;excessive.gearCeiling=200;
        Require(CarPerformanceRating.Calculate(lowPower)==CarPerformanceRating.Calculate(excessive),"unreachable gearing grants rating");
        var roadTyres=EquipmentCatalog.StockLoadout();roadTyres["tyres"]="tyres_sport";
        Require(CarPerformanceRating.Calculate(EquipmentCatalog.Stats(roadTyres))>stock,"road grip upgrade not reflected");
        checks.Add($"Dry-road rating has equipment ordering ({stock}/{street}/{club}) and no unbounded reward for unreachable gearing");

        var save=AdventureProgress.New();var roster=RaceRoster.Adventure(save);var same=RaceRoster.Adventure(save);
        Require(roster.Entries.Count==24&&roster.Entries[0].Slot==23&&roster.MatchesPlayer(save)&&save.raceSerial==0,"preview changes save or wrong grid");
        for(int i=0;i<24;i++)Require(roster.Entries[i].Driver.Planning.Same(in same.Entries[i].Driver.Planning),"reopening preview rerolls form");
        save.owned.Add("engine_workshop");save.fitted.Find(p=>p.slot=="engine").id="engine_workshop";
        Require(!roster.MatchesPlayer(save),"stale player fit accepted");var upgraded=RaceRoster.Adventure(save);
        for(int i=1;i<24;i++)Require(roster.Entries[i].Tuning.Equals(upgraded.Entries[i].Tuning)&&roster.Entries[i].Driver.Planning.Same(in upgraded.Entries[i].Driver.Planning),"player upgrade changes rivals");
        Require(upgraded.Entries[0].Rating>roster.Entries[0].Rating,"actual player upgrade absent in preview");
        checks.Add("Adventure preview starts player24th, does not spend a race serial/re-roll form, rejects stale equipment and fixes opponents across upgrades");

        var oldRoster=AdventureSession.EnteredRoster;bool oldRacing=AdventureSession.Racing;
        try {
            AdventureSession.EnteredRoster=roster;AdventureSession.Racing=true;
            Require(AdventureDriverPreset.Enabled("--parallel-dynamic-forecast")&&AdventureDriverPreset.Value("--driver-policy",null)=="context"&&AdventureDriverPreset.Value("--racing-line",null)=="bayview-smooth-v1","modern adventure missing retained package");
            AdventureSession.Racing=false;Require(!AdventureDriverPreset.Enabled("--dynamic-rejoin")&&AdventureDriverPreset.Value("--driver-policy","legacy")=="legacy","race preset leaks into exploration");
        }finally{AdventureSession.EnteredRoster=oldRoster;AdventureSession.Racing=oldRacing;}
        checks.Add("Connected race reuses the retained driver package; driver flags stop applying on return to exploration");
        return checks;
    }
}
