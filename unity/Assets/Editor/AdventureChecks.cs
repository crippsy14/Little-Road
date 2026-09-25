using System;
using System.Collections.Generic;
using System.IO;
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
        var paints=new AdventureStore(Path.Combine(folder,"paint.json"));
        Check(!paints.Repaint(5,150)&&paints.Data.paintIndex==0,"Repaint refused without credits");
        paints.Change(p=>{p.credits=400;return true;});
        Check(!paints.Repaint(0,150)&&paints.Repaint(5,150)&&paints.Data.credits==250&&!paints.Repaint(5,150),"Repaint charges once and refuses the current colour");
        paints=new AdventureStore(paints.path);Check(paints.Data.paintIndex==5&&paints.Data.credits==250,"Repaint persists across reload");
        Check(PaintCatalog.All.Length==16&&PaintCatalog.All[0].color==new Color(.08f,.56f,.49f),"Paint catalogue keeps the original first colours for old saves");
        return checks;
    }
}
