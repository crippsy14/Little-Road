using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Quarry Hills (docs/59), behind --quarry-hills: the landform's numbers (galleries, the pit lake, the tips, Slate
// Quay's apron) and the quarry floor's layout (the Quarry Sprint loop, the rally school), from the pure
// shape functions, so a change that breaks the design fails the build.
public static class QuarryHillsChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(QuarryHills.Enabled,"Quarry Hills is part of the game (--no-quarry-hills leaves it out)");
        // Galleries: benches of a real slate quarry's height (Dinorwic's are about 15 m) that step up to the rim.
        Check(QuarryHills.Benches>=4&&QuarryHills.BenchHeight>5&&QuarryHills.BenchHeight<25&&Mathf.Abs(QuarryHills.GalleryStep(QuarryHills.Benches*QuarryHills.BenchWidth+1)-QuarryHills.RimY)<.01f,
            $"The galleries are {QuarryHills.Benches} benches of {QuarryHills.BenchHeight} m up to the rim at {QuarryHills.RimY} m");
        bool rising=true;for(float e=0;e<QuarryHills.Benches*QuarryHills.BenchWidth;e+=.5f)if(QuarryHills.GalleryStep(e+.5f)<QuarryHills.GalleryStep(e)-.001f)rising=false;
        Check(rising,"The gallery steps only ever climb away from the pit");
        // Glas: its bed above the sea (the sea plane showed through the clear water when it was below).
        Check(QuarryHills.LakeBed>CoastalLandscape.SeaLevel+2&&QuarryHills.LakeY>QuarryHills.LakeBed+15&&QuarryHills.LakeY<QuarryHills.FloorY-5,
            $"Glas lies {QuarryHills.FloorY-QuarryHills.LakeY:0} m below the floor, {QuarryHills.LakeY-QuarryHills.LakeBed:0} m deep, its bed above the sea");
        // Tips: flat tops at their height, sides at the spoil's angle of repose.
        foreach(var t in QuarryHills.Tips) {
            var mid=(t.a+t.b)*.5f;var across=Vector2.Perpendicular((t.b-t.a).normalized);
            float top=QuarryHills.TipSurface(mid.x,mid.y);var side=mid+across*(t.half+40);float drop=top-QuarryHills.TipSurface(side.x,side.y);
            Check(Mathf.Abs(top-t.top)<9&&drop>15&&drop<45,$"Tip at ({mid.x:0},{mid.y:0}) is flat-topped near {t.top:0} m with steep spoil sides ({drop:0} m down over 40 m)");
        }
        Check(Mathf.Atan(QuarryHills.TipSlope)*Mathf.Rad2Deg>30&&Mathf.Atan(QuarryHills.TipSlope)*Mathf.Rad2Deg<40,$"Tip sides stand at {Mathf.Atan(QuarryHills.TipSlope)*Mathf.Rad2Deg:0} degrees, slate spoil's angle of repose");
        // Slate Quay: a level apron a few metres above the sea, reaching out past the waterline.
        Check(QuarryHills.QuayY>CoastalLandscape.SeaLevel+2&&QuarryHills.QuayY<CoastalLandscape.SeaLevel+5,$"Slate Quay's apron stands {QuarryHills.QuayY-CoastalLandscape.SeaLevel:0.0} m above the sea");
        var seaEnd=QuarryHills.QuayPoint(0,-QuarryHills.QuaySea+4,0);var landEnd=QuarryHills.QuayPoint(0,QuarryHills.QuayLand-4,0);
        Check(Mathf.Abs(QuarryHills.Natural(seaEnd.x,seaEnd.z)-QuarryHills.QuayY)<.01f&&Mathf.Abs(QuarryHills.Natural(landEnd.x,landEnd.z)-QuarryHills.QuayY)<.01f,"The apron is level from its sea wall to its landward wall");
        var offshore=QuarryHills.QuayPoint(0,-QuarryHills.QuaySea-4,0);
        Check(QuarryHills.Natural(offshore.x,offshore.z)<CoastalLandscape.SeaLevel-4,"Deep water lies right off the sea wall (boats can moor)");
        // The quarry floor: the Quarry Sprint loop on the floor, clear of the pit, about the doc's length.
        var loop=QuarryFloor.Loop;float length=0;for(int i=0;i<loop.Length;i++)length+=Vector3.Distance(loop[i],loop[(i+1)%loop.Length]);
        Check(length>1000&&length<1700,$"The Quarry Sprint loop is {length:0} m round (docs/59: a short rallycross lap)");
        Check(loop.All(p=>QuarryHills.FloorQ(p.x,p.z)<.95f),"The whole loop lies on the level quarry floor");
        Check(loop.All(p=>{QuarryHills.PitMetric(p.x,p.z,out float q,out _,out _);return q>1.1f;}),"The loop keeps clear of the pit's edge");
        Check(QuarryHills.FloorQ(QuarryFloor.School.x,QuarryFloor.School.z)<.9f&&loop.All(p=>Vector2.Distance(new(p.x,p.z),new(QuarryFloor.School.x,QuarryFloor.School.z))>40),"The rally school stands on the floor, 40 m or more from the loop");
        Check(QuarryHills.Owns(QuarryHills.ChapelStreetName)&&QuarryHills.Owns(QuarryHills.TerraceRowName)&&QuarryHills.Gravel(QuarryHills.HaulRoadName)&&!QuarryHills.Gravel(QuarryHills.HighStreetName),
            "Quarry Hills owns its streets (back streets included); the haul road is gravel and the High Street tarmac");
        // A quarry race builds only its area (4 Oct): the floor, the school and the town, not Hillcrest.
        RaceArea.Set(QuarryFloor.Loop,450);bool area=RaceArea.Active&&RaceArea.Near(QuarryFloor.School)&&RaceArea.Near(QuarryHills.Square,350)&&!RaceArea.Near(HillcrestRoute.Square,500);RaceArea.Clear();
        Check(area&&!RaceArea.Active,"The quarry race's area holds the floor, the rally school and the town, and leaves Hillcrest out");
        // The Pit Stop's own vehicles (4 Oct): known bodies with measured sizes.
        Check(CarBodies.PitStop.All(id=>CarBodies.IsKnown(id)&&CarBodies.Length(id)>3&&CarBodies.ShoulderWidth(id)>2),"The Pit Stop's recovery truck, parts van and panel truck are known bodies with sizes");
        // The working quarry (4 Oct): every machine spot and route point on the floor, clear of the rallycross and the school.
        Check(QuarryWorks.LayoutPoints().All(p=>QuarryHills.FloorQ(p.x,p.y)<.95f&&HillcrestRoute.Distance(loop,p.x,p.y,out _)>QuarryFloor.LoopWidth*.5f+10&&Vector2.Distance(p,new(QuarryFloor.School.x,QuarryFloor.School.z))>40),
            "The quarry's machines work on the floor, 10 m or more off the circuit and 40 m from the rally school");
        // (Class B's rally tyres also live here, found once 4 stars are won at Tuner races: docs/67.)
        var gravel=EquipmentCatalog.All.parts.Where(p=>p.shop=="quarry"&&p.tier=="C").ToList();
        Check(EquipmentCatalog.All.parts.Where(p=>p.shop=="quarry"&&p.tier=="B").All(p=>p.unlock=="stars:B:4"),"Class B's rally tyres at Grit & Gravel wait for 4 Tuner stars");
        // (24 Oct: the rally school's lessons were retired, so the kit is simply found at the shop.)
        Check(gravel.Count==2&&gravel.All(p=>string.IsNullOrEmpty(p.unlock)),$"Grit & Gravel's own rally kit ({string.Join(", ",gravel.Select(p=>p.name))}) is found at the shop, with no condition");
        var tyres=EquipmentCatalog.Get("tyres_c_gravel");
        Check(tyres.dirt>EquipmentCatalog.All.parts.Where(p=>p.slot=="tyres"&&p.tier=="C"&&p.id!=tyres.id).Max(p=>p.dirt),"The gravel tyres have the best loose-ground grip in class C");
        // The rallycross is loose: gravel tyres out-grip road tyres there, road tyres lose grip (docs/60).
        var kit=EquipmentCatalog.TestLoadout("street");kit["tyres"]="tyres_stock";var stock=EquipmentCatalog.Stats(kit);kit["tyres"]="tyres_rally";var rally=EquipmentCatalog.Stats(kit);
        Check(stock.Loose().roadGrip<stock.roadGrip&&rally.Loose().roadGrip>stock.Loose().roadGrip*1.8f,"On loose slate rally tyres grip about twice as hard as stock road tyres");
        // The quarry's blasting (doc 63): on a long charge the siren wails, falls quiet 7 s before the shot, and the shot lands when asked.
        {
            const float fs=2000;var synth=new BlastSynth(fs,11);synth.Fire(30);float siren=0,hush=0,shot=0;
            for(int i=0;i<fs*42;i++){float t=i/fs,x=Mathf.Abs(synth.Next());if(t>8&&t<20)siren=Mathf.Max(siren,x);else if(t>23.5f&&t<29.9f)hush=Mathf.Max(hush,x);else if(t>30&&t<30.5f)shot=Mathf.Max(shot,x);}
            Check(siren>.1f&&hush<.01f&&shot>.5f,$"A long blast's siren wails ({siren:0.00}), falls quiet ({hush:0.000}), and the shot lands at 30 s ({shot:0.00})");
        }
        return checks;
    }
}
