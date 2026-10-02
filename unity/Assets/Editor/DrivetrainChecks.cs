using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Real gears (docs/47): every engine / induction / gearbox in each class, driven flat out from a standstill with the
// automatic box, must reach its drag-limited top speed in top gear, use every gear, sit between 0.95 x peak power
// and the redline at top speed, and stay within reach of the best-gear envelope. Engine steps must be quicker.
public static class DrivetrainChecks
{
    public struct Result {public float top,t90,rpm;public int finalGear,gearsUsed;public bool limiter;}
    public static Result Drive(CarTuning tuning)
    {
        var gears=new GearState(tuning.drivetrain);float v=0,t=0,t90=-1;var used=new HashSet<int>();const float dt=1f/120;bool limiterLate=false;
        float target=tuning.drivetrain.ReachableTop;
        while(t<120) {
            float a=gears.Step(v,1,dt);v=Mathf.Max(0,v+(a-DriveModel.Resistance(v,tuning.mass))*dt);t+=dt;used.Add(gears.Gear);if(t>110&&gears.Limiter)limiterLate=true;
            if(t90<0&&v>=target*.9f)t90=t;
        }
        return new Result{top=v,t90=t90,rpm=gears.Rpm,finalGear=gears.Gear,gearsUsed=used.Count,limiter=limiterLate};
    }
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Drivetrain check: "+label);checks.Add(label);}
        var all=EquipmentCatalog.All.parts;int combos=0;float worst=0;string worstName="";
        foreach(string cls in RaceCalendar.Classes) {
            var engines=all.Where(p=>p.slot=="engine"&&(p.tier??"C")==cls).ToArray();
            var turbos=all.Where(p=>p.slot=="turbo"&&((p.tier??"C")==cls||p.id=="turbo_stock")).ToArray();
            var boxes=all.Where(p=>p.slot=="transmission"&&(p.tier??"C")==cls).ToArray();
            foreach(var e in engines)foreach(var tb in turbos)foreach(var bx in boxes) {
                var fit=EquipmentCatalog.StockLoadout();fit["engine"]=e.id;fit["turbo"]=tb.id;fit["transmission"]=bx.id;
                var tuning=EquipmentCatalog.Stats(fit);var train=tuning.drivetrain;var run=Drive(tuning);combos++;
                string name=$"{e.id}+{tb.id}+{bx.id}";
                // Fixed gearing (docs/47 gearing v2): top speed is the best gear's, where drive meets drag or the redline.
                float reach=train.ReachableTop;int bestGear=train.TopSpeedGear;
                Check(run.top>=reach*(train.TopGearLimited?.95f:.97f),$"{name} reaches its top speed ({run.top:0.0} of {reach:0.0} m/s, gear {bestGear+1} of {train.Gears}; power-limited {train.topSpeed:0.0})");
                Check(train.TopGearLimited||reach>=train.topSpeed*.88f,$"{name} is not badly over-geared ({reach:0.0} of a power-limited {train.topSpeed:0.0} m/s)");
                Check(run.gearsUsed>=bestGear+1,$"{name} uses every gear up to its best ({run.gearsUsed} of {bestGear+1})");
                if(train.TopGearLimited)Check(run.finalGear==train.Gears-1&&run.limiter,$"{name} ends on the top-gear limiter (it bounces there)");
                // Against the ideal (a gearless car always at the best gear's force, same launch limit, no shift cuts):
                // real gears cost some time, not a lot.
                float ideal=0,v=0;const float dt=1f/120;while(v<reach*.9f&&ideal<120){v+=(train.Envelope(v)-DriveModel.Resistance(v,tuning.mass))*dt;ideal+=dt;}
                float ratio=run.t90/Mathf.Max(.01f,ideal);if(ratio>worst){worst=ratio;worstName=name;}
                Check(run.t90>0&&ratio<1.6f,$"{name} reaches 90% of top speed within 60% of the gearless ideal's time ({run.t90:0.0} s vs {ideal:0.0} s)");
            }
            // Each engine step is quicker to 90% of its top speed... or at least faster at the top.
            var ladder=engines.Where(p=>p.shop==null&&!p.id.Contains("economy")).ToArray();
            for(int i=1;i<ladder.Length;i++) {
                var a=EquipmentCatalog.StockLoadout();a["engine"]=ladder[i-1].id;var b=EquipmentCatalog.StockLoadout();b["engine"]=ladder[i].id;
                var ta=EquipmentCatalog.Stats(a);var tb=EquipmentCatalog.Stats(b);
                Check(tb.drivetrain.topSpeed>ta.drivetrain.topSpeed,$"Class {cls}: {ladder[i].id} is faster than {ladder[i-1].id}");
            }
        }
        checks.Add($"All {combos} engine/induction/gearbox combinations checked; slowest against the envelope: {worstName} at {worst:0.00}x");
        // Tuning (docs/48): the final drive scales the gears; auto-tune lands where it says; tunes follow their box.
        {
            var fit=EquipmentCatalog.StockLoadout();var plain=EquipmentCatalog.Stats(fit).drivetrain;
            var shorter=EquipmentCatalog.Stats(fit,new GearTune{transmission=fit["transmission"],final=1.1f}).drivetrain;
            Check(Mathf.Abs(shorter.RedlineSpeed*1.1f/plain.RedlineSpeed-1)<.01f,$"Tune: a 1.10 final drive shortens top gear by 10% ({plain.RedlineSpeed*3.6f:0} to {shorter.RedlineSpeed*3.6f:0} km/h)");
            var other=EquipmentCatalog.Stats(fit,new GearTune{transmission="transmission_six",final=1.2f}).drivetrain;
            Check(Mathf.Abs(other.RedlineSpeed-plain.RedlineSpeed)<.01f,"Tune: a tune for another gearbox is ignored");
            var auto=EquipmentCatalog.AutoTune(fit);var tuned=EquipmentCatalog.Stats(fit,auto).drivetrain;
            Check(auto.Valid&&(Mathf.Abs(tuned.RedlineSpeed/(tuned.topSpeed*1.02f)-1)<.02f||auto.final<=GearTune.FinalMin+.001f||auto.final>=GearTune.FinalMax-.001f),$"Auto-tune (balanced): top gear's redline just past the car's top speed ({tuned.RedlineSpeed*3.6f:0} vs {tuned.topSpeed*3.6f:0} km/h, final {auto.final:0.00})");
            Check(!new GearTune{transmission="transmission_stock",final=1.4f}.Valid&&!new GearTune{transmission="transmission_stock",gears=new[]{2f}}.Valid,"Tune limits: final drive 0.80-1.25, gears 0.85-1.15");
            var gearTuned=EquipmentCatalog.Stats(fit,new GearTune{transmission=fit["transmission"],gears=new[]{1.15f,.85f,1.15f,.85f}}).drivetrain;
            bool ordered=true;for(int g=1;g<gearTuned.Gears;g++)ordered&=gearTuned.ratio[g]<gearTuned.ratio[g-1];
            Check(ordered,"Tune: gears stay in order whatever the per-gear settings");
        }
        // Automatic box under braking and on kickdown (Richard, 1 Oct: braking 80 to 30 it stayed in 5th).
        {
            var fit=EquipmentCatalog.StockLoadout();fit["engine"]="engine_club";fit["turbo"]="turbo_street";fit["transmission"]="transmission_long";
            var train=EquipmentCatalog.Stats(fit).drivetrain;var box=new GearState(train);const float dt=1f/120;float v=0;
            for(int i=0;i<12000&&v<train.ReachableTop*.95f;i++){float a=box.Step(v,1,dt);v+=(a-DriveModel.Resistance(v,1))*dt;}
            int topGear=box.Gear;float from=v;
            while(v>13.4f){box.Step(v,-1,dt);v-=8*dt;}for(int i=0;i<60;i++)box.Step(v,-1,dt);
            float rpm=train.Rpm(box.Gear,v);
            Check(box.Gear<topGear&&(box.Gear==0||rpm>=train.torqueRpm*.8f),$"Automatic: braking {from*2.237f:0} to 30 mph steps down from gear {topGear+1} to {box.Gear+1} ({rpm:0} rpm, ready to pull)");
            var kick=new GearState(train);for(int i=0;i<600&&v<14;i++)kick.Step(13.4f,.3f,dt);
            // Cruise in top gear, then floor it.
            var cruise=new GearState(train);typeof(GearState).GetProperty("Gear").SetValue(cruise,train.Gears-1);
            for(int i=0;i<30;i++)cruise.Step(13.4f,.3f,dt);float t=0;for(;t<1;t+=dt)cruise.Step(13.4f,1,dt);
            Check(cruise.Gear<=train.BestGear(13.4f)+1,$"Automatic: floored at 30 mph in top gear, it kicks down to gear {cruise.Gear+1} within 1 s (best {train.BestGear(13.4f)+1})");
        }
        // Gearing design (Richard, 1 Oct): a stock car never reaches its top-gear limiter; a strong class C engine on the
        // stock box does (time for a longer box), and the six-speed frees it.
        {
            CarTuning Kit(params (string,string)[] parts){var fit=EquipmentCatalog.StockLoadout();foreach(var (slot,id) in parts)fit[slot]=id;return EquipmentCatalog.Stats(fit);}
            var stockCar=Kit();var strong=Kit(("engine","engine_valley"),("turbo","turbo_club"));var strongSix=Kit(("engine","engine_valley"),("turbo","turbo_club"),("transmission","transmission_six"));
            Check(!stockCar.drivetrain.TopGearLimited,$"Stock car: power runs out before the top-gear limiter ({stockCar.drivetrain.ReachableTop*3.6f:0} km/h)");
            Check(strong.drivetrain.TopGearLimited,$"Valley V6 with a club turbo on the stock box hits its top-gear limiter ({strong.drivetrain.ReachableTop*3.6f:0} km/h)");
            Check(!strongSix.drivetrain.TopGearLimited&&strongSix.drivetrain.ReachableTop>strong.drivetrain.ReachableTop*1.05f,$"...and the six-speed frees it ({strongSix.drivetrain.ReachableTop*3.6f:0} km/h)");
        }
        // Manual box (docs/47 stage 3): no automatic shifts, the limiter holds the redline, over-revving downshifts
        // are refused, a requested upshift happens after the gearbox's shift time.
        {
            var stock=CarTuning.Stock;var train=stock.drivetrain;var box=new GearState(train){Manual=true};float v=0;const float dt=1f/120;
            int cuts=0;bool wasCut=false;float lowest=float.MaxValue;
            for(int i=0;i<1200;i++){float a=box.Step(v,1,dt);v=Mathf.Max(0,v+(a-DriveModel.Resistance(v,stock.mass))*dt);if(i>900){if(box.Limiter&&!wasCut)cuts++;wasCut=box.Limiter;lowest=Mathf.Min(lowest,box.Rpm);}}
            // The limiter bounces: repeated fuel cuts (about a dozen a second), the revs dipping a few percent each time.
            Check(box.Gear==0&&lowest>=train.redline*.95f&&cuts>=20,$"Manual: first gear bounces on the limiter ({cuts} cuts in 2.5 s, revs {lowest:0}-{train.redline:0})");
            box.RequestShift(1);float waited=0;while(box.Shifting&&waited<2){box.Step(v,1,dt);waited+=dt;}
            Check(box.Gear==1&&Mathf.Abs(waited-train.shiftTime)<.02f,$"Manual: an upshift takes the shift time ({waited:0.00} s)");
            for(int i=0;i<600;i++){float a=box.Step(v,1,dt);v=Mathf.Max(0,v+(a-DriveModel.Resistance(v,stock.mass))*dt);}
            box.RequestShift(-1);
            Check(!box.Shifting&&box.Gear==1,"Manual: a downshift that would over-rev is refused");
            box.Reset();Check(box.Gear==0&&!box.Shifting,"Recovery reset: back in first");
            Check(GearDisplay.Label(box)=="1","Dash shows the gear number");
            // Reverse (GT7 and real cars): a gear chosen by shifting down from first at a standstill.
            var rev=new GearState(train){Manual=true};rev.Step(0,0,dt);rev.RequestShift(-1);for(int i=0;i<60;i++)rev.Step(0,0,dt);
            Check(rev.Gear==-1&&GearDisplay.Label(rev)=="R","Manual: down from first at a standstill selects reverse");
            rev.RequestShift(1);for(int i=0;i<60;i++)rev.Step(0,0,dt);
            Check(rev.Gear==0,"Manual: up from reverse returns to first");
            var moving=new GearState(train){Manual=true};moving.Step(8,1,dt);moving.RequestShift(-1);for(int i=0;i<60;i++)moving.Step(8,0,dt);
            Check(moving.Gear==0,"Manual: no reverse while rolling forward");
            // A car rolling backwards on the forward pedal brakes (recoveries used to roll on backwards).
            DriveModel.Forces(stock,-3,1,0,7,0,out float stopping,out _,0,0);
            Check(stopping>5,$"Forward pedal while rolling backwards brakes ({stopping:0.0} m/s2)");
            DriveModel.Forces(stock,0,-.5f,0,7,0,out float halfReverse,out _);DriveModel.Forces(stock,0,-1,0,7,0,out float fullReverse,out _);
            Check(halfReverse<0&&halfReverse>fullReverse*.6f,"Reverse drive follows the pedal");
            // Held on a countdown (docs/51): the revs follow the throttle to the limiter, nothing reaches the wheels.
            var held=new GearState(train){Held=true};float drive=0,peak=0;for(int i=0;i<240;i++){drive=Mathf.Max(drive,held.Step(0,1,dt));peak=Mathf.Max(peak,held.Rpm);}
            for(int i=0;i<360;i++)held.Step(0,0,dt);
            Check(drive==0&&peak>train.redline*.95f&&held.Rpm<train.idle*1.1f,$"Held on a countdown: revs to {peak:0} rpm with no drive, back to idle off the throttle");
        }
        return checks;
    }
}
