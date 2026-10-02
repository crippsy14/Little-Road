using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The Duke's Challenge (docs/54): the maximum build is the fastest the game can build round Hillcrest Raceway, the
// roster is you against the Duke on pole, the first win pays once and gives you the Regale, and a save can only hold
// the Regale with the trophy.
public static class DukeChallengeChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Duke challenge check: "+label);checks.Add(label);}
        var max=EquipmentCatalog.MaxBuild();float lap=EquipmentCatalog.MaxBuildLap;
        Check(max.Values.All(id=>{var t=EquipmentCatalog.Get(id).tier;return t=="E"||t=="S";}),$"The maximum build is top-class parts in every slot ({string.Join(", ",max.Values)})");
        // No single swap, of any part in the game, laps faster.
        float best=float.MaxValue;string better=null;
        foreach(var slot in max.Keys.ToList())foreach(var part in EquipmentCatalog.All.parts.Where(p=>p.slot==slot&&p.id!=max[slot])){var fit=new Dictionary<string,string>(max){[slot]=part.id};float t=EquipmentCatalog.RacewayLapTime(fit);if(t<best){best=t;better=part.id;}}
        Check(best>=lap-.01f,$"No single part swap laps the raceway faster than the maximum build ({lap:0.00} s ideal; best swap {better} {best:0.00} s)");
        float stock=EquipmentCatalog.RacewayLapTime(EquipmentCatalog.StockLoadout()),valley=EquipmentCatalog.RacewayLapTime(EquipmentCatalog.TestLoadout("valley"));
        Check(stock>lap*1.3f&&valley>lap*1.15f,$"It shows how fast cars get: stock {stock:0.0} s and the valley kit {valley:0.0} s against {lap:0.0} s");
        var folder=Path.Combine(Path.GetTempPath(),"little-road-duke-check");Directory.CreateDirectory(folder);var path=Path.Combine(folder,"duke.json");if(File.Exists(path))File.Delete(path);
        var store=new AdventureStore(path);var roster=RaceRoster.DukeChallenge(store.Data);
        Check(roster.Entries.Count==2&&roster.Laps==3&&roster.Track=="hillcrest"&&roster.Event==RaceRoster.DukeEvent&&roster.Entries[1].Slot==0&&roster.Entries[1].Body==CarBodies.Prototype&&roster.Entries[1].Driver.Name=="Duke Rolo",
            "Three laps of Hillcrest Raceway, you against Duke Rolo in the Regale, him on pole");
        Check(CarBodies.UsesStandardEnvelope(CarBodies.Prototype),"The Regale races in the standard envelope (shape never changes pace)");
        int wallet=store.Data.credits;int serial=store.BeginRace();
        Check(store.FinishDukeRace(serial,2)==0&&store.Data.credits==wallet&&!store.Data.bodies.Contains(CarBodies.Prototype),"A loss costs nothing and gives nothing");
        serial=store.BeginRace();int paid=store.FinishDukeRace(serial,1);
        Check(paid==AdventureStore.DukePrize&&store.Data.bodies.Contains(CarBodies.Prototype)&&store.Data.collected.Contains(Collectibles.DukeBeaten),"The first win pays the prize and gives you the Regale");
        Check(store.FinishDukeRace(serial,1)==0,"A result settles once");
        serial=store.BeginRace();Check(store.FinishDukeRace(serial,1)==0&&store.Data.raceWins==2,"Rematch wins count as wins, for honour only");
        store=new AdventureStore(path);Check(store.Data.bodies.Contains(CarBodies.Prototype),"The Regale survives a reload");
        var forged=AdventureProgress.New();forged.bodies.Add(CarBodies.Prototype);bool refused=false;try{forged.Validate();}catch(InvalidDataException){refused=true;}
        Check(refused,"A save cannot hold the Regale without beating the Duke");
        // Aero grip (docs/54): only downforce cars get it, it nearly doubles grip at the maximum build's top speed, and
        // the ideal lap counts it.
        var maxTune=EquipmentCatalog.Stats(max);var stockTune=EquipmentCatalog.Stats(EquipmentCatalog.StockLoadout());
        Check(DriveModel.AeroGrip(stockTune,40)==1&&DriveModel.AeroGrip(EquipmentCatalog.Stats(EquipmentCatalog.TestLoadout("valley")),40)==1,"Cars without downforce (classes C to A) get no aero grip");
        float atTop=DriveModel.AeroGrip(maxTune,75);
        Check(atTop>1.6f&&atTop<2.2f&&DriveModel.AeroGrip(maxTune,20)<1.1f,$"The maximum build's grip grows with speed: x{DriveModel.AeroGrip(maxTune,20):0.00} at 20 m/s, x{atTop:0.00} at 75 m/s");
        Check(Mathf.Min(maxTune.braking,maxTune.roadGrip*1.65f*atTop)>=maxTune.braking*.99f,$"At top speed the E brakes brake at their rating ({maxTune.braking:0} m/s^2), not at the tyres' static grip ({maxTune.roadGrip*1.65f:0})");
        // Platform drag and firmness (docs/54, 2 Oct): class C's air and springs exactly as before; the maximum build
        // tops out at prototype speed (Richard: "up the max speed to be similar to the actual cars").
        Check(stockTune.dragSave==0&&stockTune.DragFactor==1&&DriveModel.Resistance(stockTune,40)==DriveModel.Resistance(40,stockTune.mass)&&stockTune.firm==0,"Class C cars keep the street car's drag and springs exactly");
        // (On Hillcrest's short straights the measured maximum build may take a shorter E box; the long box is the
        // top-speed build for the bigger tracks to come.)
        float top=maxTune.drivetrain.ReachableTop;var longBox=EquipmentCatalog.Stats(new Dictionary<string,string>(max){["transmission"]="transmission_e_long"});float vmax=longBox.drivetrain.ReachableTop;
        Check(vmax>90&&vmax<97&&top>80,$"Top speed is a prototype's: {vmax:0.0} m/s ({vmax*3.6f:0} km/h) on the long box; the raceway build ({max["transmission"]}) {top:0.0} m/s ({top*3.6f:0} km/h)");
        Check(maxTune.firm>=2&&1/(1+maxTune.firm)<=.35f,$"The maximum build sits on race springs: it leans {100/(1+maxTune.firm):0}% as much as a street car");
        // The test drive (--max-build-test): the maximum build bought and fitted on a copy of a save.
        if(File.Exists(path))File.Delete(path);store=new AdventureStore(path);AdventureSession.FitMaxBuild(store);
        Check(max.All(kv=>store.Data.Loadout()[kv.Key]==kv.Value&&store.Data.owned.Contains(kv.Value)),"The test drive fits the whole maximum build to a copy of the save");
        File.Delete(path);return checks;
    }
}
