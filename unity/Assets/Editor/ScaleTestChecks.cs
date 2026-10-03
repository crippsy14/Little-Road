using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The continent and its scale test (ContinentPlan, ContinentGenerator, ScaleTest; docs/58 §11). Generated at 50 m here
// (the game uses 10 m) so the check stays quick; the climate grid is the same.
public static class ScaleTestChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        Check(ContinentPlan.ToWorld(ContinentPlan.BayKm)==Vector3.zero&&Mathf.Abs(ContinentPlan.ToWorld(ContinentPlan.Find("hillcrest").Km).z+1150)<1,"The continent uses the game's own coordinates: Bayview island at the origin, Hillcrest 1.15 km south");
        var ids=new HashSet<string>(ContinentPlan.Places.Select(p=>p.id));
        Check(ids.Count==ContinentPlan.Places.Length&&ContinentPlan.Roads.All(r=>(ids.Contains(r.a)||ContinentPlan.Junctions.ContainsKey(r.a))&&(ids.Contains(r.b)||ContinentPlan.Junctions.ContainsKey(r.b))&&ContinentPlan.Types.ContainsKey(r.type)),
            $"{ContinentPlan.Places.Length} places with unique ids; every road joins known places or junctions and has a known type");
        // Every place can be reached from Bayview by road.
        var reached=new HashSet<string>{"bayview"};bool grew=true;
        while(grew){grew=false;foreach(var r in ContinentPlan.Roads){if(reached.Contains(r.a)&&reached.Add(r.b))grew=true;if(reached.Contains(r.b)&&reached.Add(r.a))grew=true;}}
        Check(ids.All(reached.Contains),"Every place can be reached from Bayview by road");

        var gen=new ContinentGenerator(50);
        Check(gen.LandKm2>250&&gen.LandKm2<360,$"The continent has about 300 km² of land ({gen.LandKm2:0} km²), as in the blueprint");
        foreach(var id in new[]{"bayview","hillcrest","mycity","aurora"})Check(gen.nodeHeight[id]>ContinentGenerator.Sea+1,$"{ContinentPlan.Find(id).name} stands above the sea");
        float glass=gen.lakeLevel[0];
        Check(glass>ContinentGenerator.Sea+3&&gen.nodeHeight["lowtown"]<glass-8,$"Glasswater sits above the sea ({glass-ContinentGenerator.Sea:0} m) and Lowtown lies well below its surface");
        var undersea=gen.roads.First(r=>r.plan.name=="Undersea Road");var mid=undersea.points[undersea.points.Length/2];
        Check(gen.WaterAt(mid,out float level,out _)&&mid.y<level-5,"The Undersea Road runs along the sea floor between Bayview and Coral Key");
        string B(string id)=>ContinentGenerator.BiomeNames[(int)gen.BiomeAt(ContinentPlan.ToWorld(ContinentPlan.Find(id).Km))];
        Check(new[]{"Sand desert","Red canyons","Scrub","Dry grassland"}.Contains(B("redrock"))&&new[]{"Rainforest","Pine forest"}.Contains(B("pinewood"))&&new[]{"Snow & glacier","Alpine rock","Tundra"}.Contains(B("frostcrown"))&&B("hillcrest")=="Farmland & meadow",
            $"The climate puts the land where the design says: Redrock {B("redrock")}, Pinewood {B("pinewood")}, Frostcrown {B("frostcrown")}, Hillcrest {B("hillcrest")}");
        var steep=gen.roads.Where(r=>r.maxGrade>r.type.grade*1.6f+.02f).Select(r=>$"{r.Label} {r.maxGrade*100:0}%").ToList();
        Check(steep.Count==0,"Every road keeps near its type's grade limit"+(steep.Count>0?": "+string.Join(", ",steep):""));
        var tight=gen.roads.Where(r=>r.hairpins&&r.minRadius<ContinentGenerator.HairpinRadius*.75f).Select(r=>$"{r.Label} {r.minRadius:0} m").ToList();
        Check(tight.Count==0,$"Every switchback turns on at least {ContinentGenerator.HairpinRadius*.75f:0} m (hairpins are {ContinentGenerator.HairpinRadius:0} m arcs)"+(tight.Count>0?": "+string.Join(", ",tight):""));
        Check(gen.roads.All(r=>r.points.Length>2&&Vector3.Distance(r.points[0],ContinentPlan.ToWorld(ContinentPlan.NodeKm(r.plan.a),r.points[0].y))<1&&Vector3.Distance(r.points[^1],ContinentPlan.ToWorld(ContinentPlan.NodeKm(r.plan.b),r.points[^1].y))<1),"Every road starts and ends at its places");
        var tops=RaceCalendar.Classes.Select(c=>EquipmentCatalog.Stats(ScaleTest.ClassBuild(c)).AttainableSpeed).ToArray();
        Check(tops.Zip(tops.Skip(1),(a,b)=>b>=a).All(x=>x)&&tops[0]>EquipmentCatalog.Stats(EquipmentCatalog.StockLoadout()).AttainableSpeed-.1f,
            "Each class's top road build is at least as quick as the one below: "+string.Join(" < ",tops.Select(t=>$"{t*3.6f:0}")));
        return checks;
    }
}
