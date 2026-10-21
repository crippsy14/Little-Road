using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The hybrid prototype (HybridPower, --hybrid; docs/84): the City hybrid is in the data but out of the catalogue without
// the flag; a charged battery launches quicker than the petrol car; deploying drains it, braking refills it; it does
// nothing at part throttle or above corner-exit speed; and a flat battery is dead weight, slower than petrol.
public static class HybridChecks
{
    // A flat-out standing start to `target` m/s (the drivetrain checks' sim), with the motor when `hybrid` and the
    // system's weight as a lighter pull from the engine (its drive is per unit mass). Returns seconds.
    static float Launch(CarTuning t,bool hybrid,ref HybridPower.State s,float target)
    {
        var gears=new GearState(t.drivetrain);float v=0,time=0;const float dt=1f/120;float weight=hybrid?1/(1+HybridPower.MildMass):1;
        while(v<target&&time<60) {
            float a=gears.Step(v,1,dt)*weight;if(hybrid)a+=HybridPower.Step(ref s,t,v,1,dt);
            v=Mathf.Max(0,v+(a-DriveModel.Resistance(v,t.mass*(hybrid?1+HybridPower.MildMass:1)))*dt);time+=dt;
        }
        return time;
    }
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Hybrid check: "+label);checks.Add(label);}
        var raw=JsonUtility.FromJson<EquipmentCatalog.Data>(Resources.Load<TextAsset>("Data/parts").text);
        var city=raw.parts.FirstOrDefault(p=>p.id=="engine_city_hybrid");
        Check(city!=null&&city.motorKw>0&&city.batteryKj>0&&city.regenKw>0&&city.hybrid&&city.shop=="bramblewick"&&(city.tier??"C")=="C","the City hybrid is in the data: a class C engine at Jed's, with a motor, a battery and regen");
        Check(!HybridPower.Enabled&&EquipmentCatalog.All.parts.All(p=>p.motorKw<=0),"without --hybrid the catalogue has no hybrid parts (the game is as before)");
        var fit=EquipmentCatalog.StockLoadout();fit["engine"]="engine_club";var petrol=EquipmentCatalog.Stats(fit);
        Check(petrol.batteryKj==0&&petrol.hybridKw==0,"a petrol car has no battery");
        var t=petrol;t.hybridKw=HybridPower.MildKw;t.batteryKj=HybridPower.MildKj;t.regenKw=HybridPower.MildRegenKw;
        var none=HybridPower.Full;float petrolTime=Launch(petrol,false,ref none,22);
        var charged=HybridPower.Full;float hybridTime=Launch(t,true,ref charged,22);
        Check(hybridTime<petrolTime*.95f,$"a charged hybrid launches quicker: 0-22 m/s in {hybridTime:0.00} s against {petrolTime:0.00} s");
        Check(charged.charge<.8f&&charged.deploySeconds>1,$"launching drains the battery (to {charged.charge:0.00} after {charged.deploySeconds:0.0} s of deploy)");
        var flat=HybridPower.Full;flat.charge=0;float flatTime=Launch(t,true,ref flat,22);
        Check(flatTime>petrolTime,$"a flat battery is dead weight: {flatTime:0.00} s against petrol's {petrolTime:0.00} s");
        Check(flat.emptySeconds>1,"running flat is counted (emptySeconds)");
        var s=HybridPower.Full;
        Check(HybridPower.Step(ref s,t,10,.6f,.1f)==0&&s.charge==1,"no deploy at part throttle");
        Check(HybridPower.Step(ref s,t,HybridPower.DeploySpeed+1,1,.1f)==0&&s.charge==1,"no deploy above corner-exit speed");
        Check(HybridPower.Step(ref s,t,5,1,1f/120)<=HybridPower.MotorAccelCap+1e-4f,"the motor's push is capped at low speed");
        s.charge=0;float v=30;for(int i=0;i<240&&v>10;i++){HybridPower.Step(ref s,t,v,-1,1f/120);v-=t.braking/120;}
        Check(s.charge>.05f&&s.regenKj>5,$"braking from 30 to 10 m/s refills the battery (to {s.charge:0.00}, {s.regenKj:0} kJ)");
        s.charge=1;HybridPower.Step(ref s,t,20,-1,1);Check(s.charge==1,"a full battery takes no more");
        return checks;
    }
}
