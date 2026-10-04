using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Quarry Hills (docs/59), behind --quarry-hills: the landform's numbers (galleries, the pit lake, the tips, Slate
// Quay's apron) and the quarry floor's layout (the Quarry Sprint loop, the rally school, the Blondin), from the pure
// shape functions, so a change that breaks the design fails the build.
public static class QuarryHillsChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(QuarryHills.Enabled,"Quarry Hills is part of the game (--no-quarry-hills leaves it out)");
        // Galleries: twelve 15 m benches up to the rim.
        Check(QuarryHills.Benches==12&&Mathf.Abs(QuarryHills.BenchHeight-15)<.01f&&Mathf.Abs(QuarryHills.GalleryStep(QuarryHills.Benches*QuarryHills.BenchWidth+1)-QuarryHills.RimY)<.01f,
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
        Check(QuarryHills.FloorQ(QuarryFloor.BlondinFloor.x,QuarryFloor.BlondinFloor.y)<1&&QuarryHills.TipSurface(QuarryFloor.BlondinTip.x,QuarryFloor.BlondinTip.y)>QuarryHills.Tips[0].top-6,
            "The Blondin runs from the quarry floor to the top of the big tip");
        Check(QuarryHills.Owns(QuarryHills.ChapelStreetName)&&QuarryHills.Owns(QuarryHills.TerraceRowName)&&QuarryHills.Gravel(QuarryHills.HaulRoadName)&&!QuarryHills.Gravel(QuarryHills.HighStreetName),
            "Quarry Hills owns its streets (back streets included); the haul road is gravel and the High Street tarmac");
        // Gwen's rally school: six lessons with medal times in order, ids the save accepts, and the rally parts locked behind it.
        Check(RallySchool.Lessons.Length==6&&RallySchool.Lessons.All(l=>l.gold<l.silver&&l.silver<l.bronze&&RallySchool.Known(l.id))&&!RallySchool.Known("lesson_6"),
            "Six rally lessons, each with gold faster than silver faster than bronze");
        var gravel=EquipmentCatalog.All.parts.Where(p=>p.shop=="quarry").ToList();
        Check(gravel.Count==2&&gravel.All(p=>p.unlock==RallySchool.Unlock&&p.tier=="C")&&PartDiscovery.Valid(RallySchool.Unlock),$"Grit & Gravel's own rally kit ({string.Join(", ",gravel.Select(p=>p.name))}) waits for gold in every lesson");
        var school=new AdventureProgress{traps=new List<TrapRecord>()};
        Check(!RallySchool.AllGold(school)&&!PartDiscovery.Met(school,RallySchool.Unlock),"With no lessons done the rally kit stays locked");
        for(int i=0;i<6;i++)school.traps.Add(new TrapRecord{id=RallySchool.Lessons[i].id,best=RallySchool.Lessons[i].gold-1,stars=3});
        Check(RallySchool.AllGold(school)&&PartDiscovery.Met(school,RallySchool.Unlock),"Gold in all six lessons releases it");
        var tyres=EquipmentCatalog.Get("tyres_c_gravel");
        Check(tyres.dirt>EquipmentCatalog.All.parts.Where(p=>p.slot=="tyres"&&p.tier=="C"&&p.id!=tyres.id).Max(p=>p.dirt),"The gravel tyres have the best loose-ground grip in class C");
        // The rallycross is loose: gravel tyres out-grip road tyres there, road tyres lose grip (docs/60).
        var kit=EquipmentCatalog.TestLoadout("street");kit["tyres"]="tyres_stock";var stock=EquipmentCatalog.Stats(kit);kit["tyres"]="tyres_rally";var rally=EquipmentCatalog.Stats(kit);
        Check(stock.Loose().roadGrip<stock.roadGrip&&rally.Loose().roadGrip>stock.Loose().roadGrip*1.8f,"On loose slate rally tyres grip about twice as hard as stock road tyres");
        // Town routines (QuarryFestival): half past seven in the evening is choir night, the Caban's shut and the quarry quiet.
        float hour=TimeOfDay.Hour;TimeOfDay.Hour=19.5f;bool evening=QuarryFestival.ChoirHours&&!QuarryFestival.CabanOpen&&!QuarryFestival.WorkingHours;
        TimeOfDay.Hour=10;bool morning=!QuarryFestival.ChoirHours&&QuarryFestival.CabanOpen&&QuarryFestival.WorkingHours;TimeOfDay.Hour=hour;
        Check(evening&&morning,"Quarry Hills keeps its hours: choir in the evening, Caban and blasting by day");
        return checks;
    }
}
