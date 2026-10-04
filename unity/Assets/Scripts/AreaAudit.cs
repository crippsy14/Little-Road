using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LittleRoad
{
    // Area audit (--area-audit, docs/57): the same measures for every map region (MapRegions), so a new area can be held
    // to the bar the existing ones set. Density and variety of what's built along its roads, its lights, its life,
    // things to do, and its defects (floating pieces, loose bunting, road-edge lips and banks). Writes area-audit.txt
    // (a table, one column per region) and area-audit.json. Thresholds live in docs/57; this only measures.
    public static class AreaAudit
    {
        // Kit pieces by what they are (ids from source-art/build_bayview_kit.py).
        static readonly string[] BuildingIds={"house_","terrace_","cottage_","shop","cafe","post_office","hotel","town_hall","church","mill","motors","station","barn","pit_stop","pit_garages","beach_hut"};
        static readonly string[] FurnitureIds={"lamp_post","bench","fountain","kiosk","flower_bed","flower_bush","picnic_table","market_stall","bandstand","map_board","flag_pole","parasol","telescope","chevron_sign","ad_board","flag_wales","slate_stack"};
        static readonly string[] NatureIds={"tree_","palm","bush","olive_tree","rock_","reeds","grass_tuft","vine_row","lavender_row","sunflower_row","hay_bale"};
        static bool Is(string id,string[] set)=>set.Any(p=>p.EndsWith("_")?id.StartsWith(p):id==p);

        // The standard (docs/57): what every town and every stretch of countryside must reach. The numbers come from
        // Bayview and Hillcrest as built after the 2 Oct round; defects must be zero anywhere.
        public sealed class Standard{public string measure,kind;public bool atLeast;public float bar;public System.Func<Column,float> value;}
        public static readonly Standard[] Standards={
            S("buildings per road km","town",true,15,c=>c.buildingsPerRoadKm),
            S("building designs","town",true,8,c=>c.designs),
            S("most common design share","town",false,.25f,c=>c.topDesignShare),
            S("same design next door","town",false,.25f,c=>c.sameNeighbourShare),
            S("wall colours","town",true,10,c=>c.wallColours),
            S("roof colours","town",true,4,c=>c.roofColours),
            S("lamps per road km","town",true,15,c=>c.lampsPerRoadKm),
            S("street furniture per road km","town",true,50,c=>c.furniturePerRoadKm),
            S("planting per road km","town",true,500,c=>c.naturePerRoadKm),
            S("named residents","town",true,4,c=>c.residents),
            S("life per road km","town",true,10,c=>c.lifePerRoadKm),
            S("Pit Stops","town",true,1,c=>c.pitStops),
            S("things to do (keepsakes, traps, photo sights)","town",true,3,c=>c.keepsakes+c.traps+c.photos),
            S("planting per road km","country",true,1500,c=>c.naturePerRoadKm),
            S("life per road km","country",true,5,c=>c.lifePerRoadKm),
            S("things to do per road km","country",true,3,c=>(c.keepsakes+c.traps+c.photos)/Mathf.Max(.1f,c.roadKm)),
            S("floating pieces","any",false,0,c=>c.floating),
            S("loose bunting ends","any",false,0,c=>c.looseBunting),
            S("road-edge lips","any",false,0,c=>c.lips),
            S("road seams (steps you drive over)","any",false,0,c=>c.seams),
            S("banks too steep (no barrier)","any",false,0,c=>c.banks),
            S("cars not on the ground","any",false,0,c=>c.carsOffGround)};
        public static string CarContactReport="",SeamReport="";
        static Standard S(string m,string k,bool atLeast,float bar,System.Func<Column,float> v)=>new(){measure=m,kind=k,atLeast=atLeast,bar=bar,value=v};
        public static List<string> Failures(Column c)=>Standards.Where(x=>x.kind=="any"||x.kind==c.kind).Where(x=>x.atLeast?x.value(c)<x.bar-1e-4f:x.value(c)>x.bar+1e-4f)
            .Select(x=>$"{x.measure}: {x.value(c):0.##} (needs {(x.atLeast?"at least":"at most")} {x.bar:0.##})").ToList();
        [System.Serializable] public class Column
        {
            public string id,name,kind;public List<string> failures=new();
            public float roadKm;public int pieces,buildings,designs,wallColours,roofColours,furniture,nature,lamps,residents,folk,parked,cruisers,keepsakes,traps,photos,pitStops,spots;
            public float topDesignShare,sameNeighbourShare,piecesPerRoadKm,buildingsPerRoadKm,lampsPerRoadKm,furniturePerRoadKm,naturePerRoadKm,lifePerRoadKm;
            public int floating,looseBunting,lips,banks,seams,carsChecked,carsOffGround;
        }
        [System.Serializable] class Report{public List<Column> regions=new();}

        public static string Run(List<(string name,string role,Vector3 at)> residents,Circuit bayview,out string json)
        {
            // Defects first: these fill PlacementAudit's position lists.
            PlacementAudit.Run();PlacementAudit.Edges(bayview);SeamReport=PlacementAudit.Seams(bayview);
            var regions=MapRegions.All;var cols=regions.Select(r=>new Column{id=r.id,name=r.name,kind=r.kind}).ToList();
            Column Of(Vector3 p){var r=MapRegions.At(p);return cols[regions.ToList().IndexOf(r)];}
            // Drivable road length per region (Bayview's loop, every valley road, the raceway).
            var lines=new List<Vector3[]>();if(bayview!=null)lines.Add(bayview.points);foreach(var road in HillcrestRoute.All())lines.Add(road.line);lines.Add(HillcrestRoute.Circuit);
            foreach(var line in lines)for(int i=1;i<line.Length;i++)Of((line[i]+line[i-1])*.5f).roadKm+=Vector3.Distance(line[i],line[i-1])/1000f;
            // What's built: buildings (design, wall and roof colours, neighbours), street furniture, planting.
            var buildings=new List<(GameObject go,Column col)>();var walls=new Dictionary<Column,HashSet<string>>();var roofs=new Dictionary<Column,HashSet<string>>();
            var designs=new Dictionary<Column,Dictionary<string,int>>();
            foreach(var c in cols){walls[c]=new();roofs[c]=new();designs[c]=new();}
            foreach(var go in BayviewKit.Placed) {
                if(go==null)continue;var col=Of(go.transform.position);col.pieces++;string id=go.name;
                if(id=="lamp_post")col.lamps++;
                if(Is(id,FurnitureIds))col.furniture++;else if(Is(id,NatureIds))col.nature++;
                if(!Is(id,BuildingIds))continue;
                col.buildings++;buildings.Add((go,col));designs[col][id]=designs[col].TryGetValue(id,out int n)?n+1:1;
                foreach(var r in go.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials) {
                    if(m==null||!m.HasProperty("_BaseColor"))continue;string hex=ColorUtility.ToHtmlStringRGB(m.GetColor("_BaseColor"));
                    if(m.name.Contains("Tint_Wall"))walls[col].Add(hex);else if(m.name.Contains("RoofTile")&&!m.name.Contains("Light"))roofs[col].Add(hex);
                }
            }
            // Same design next door: the nearest other building within 15 m has the same design.
            var same=new Dictionary<Column,(int same,int pairs)>();foreach(var c in cols)same[c]=(0,0);
            foreach(var (go,col) in buildings) {
                GameObject near=null;float best=15;
                foreach(var (other,_) in buildings){if(other==go)continue;float d=Vector3.Distance(other.transform.position,go.transform.position);if(d<best){best=d;near=other;}}
                if(near!=null){var s=same[col];same[col]=(s.same+(near.name==go.name?1:0),s.pairs+1);}
            }
            // Life: residents with a name, chatting folk, parked cars, cruising traffic.
            foreach(var r in residents)Of(r.at).residents++;
            foreach(var f in ValleyLife.People)if(f.car!=null)Of(f.car.position).folk++;
            // Standing cars: the valley's "Parked car"s, Bayview's car park ("Parked <body>") and the Halt's ("Halt <body>").
            foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name=="Parked car"||t.name.StartsWith("Parked ")||t.name.StartsWith("Halt "))Of(t.position).parked++;
            // Moving traffic: the valley's cruisers and Bayview's loop traffic ("Traffic <body>").
            foreach(var c in RoadCruiser.All)if(c!=null)Of(c.transform.position).cruisers++;
            foreach(var car in Object.FindObjectsByType<RoadCar>(FindObjectsSortMode.None))if(car.name.StartsWith("Traffic "))Of(car.transform.position).cruisers++;
            // Things to do: keepsakes, speed traps, photo sights, Pit Stops, event and story spots.
            foreach(var k in MapRegions.Keepsakes.Values)Of(k.at).keepsakes++;
            foreach(var t in SpeedTraps.All)Of(t.at).traps++;
            foreach(var p in PhotoRequests.All){var at=p.where();if(at.HasValue)Of(at.Value).photos++;}
            foreach(var s in PitStop.Sites.Values)Of(s).pitStops++;
            foreach(var s in HillcrestTown.Spots.Values)Of(s.at).spots++;
            foreach(var s in QuarryHillsTown.Spots.Values)Of(s.at).spots++;   // (Quarry Hills, --quarry-hills)
            // Defects.
            foreach(var p in PlacementAudit.FloatingAt)Of(p).floating++;
            CarContactReport=CarContact.Audit(out _);foreach(var car in CarContact.Cars())Of(car.transform.position).carsChecked++;foreach(var p in CarContact.Floating)Of(p).carsOffGround++;
            foreach(var p in PlacementAudit.LooseBunting)Of(p).looseBunting++;
            foreach(var (p,lip) in PlacementAudit.EdgeFindings){var c=Of(p);if(lip)c.lips++;else c.banks++;}
            foreach(var p in PlacementAudit.SeamAt)Of(p).seams++;
            foreach(var c in cols) {
                float km=Mathf.Max(.1f,c.roadKm);
                c.designs=designs[c].Count;c.topDesignShare=c.buildings>0?designs[c].Values.Max()/(float)c.buildings:0;
                c.sameNeighbourShare=same[c].pairs>0?same[c].same/(float)same[c].pairs:0;c.wallColours=walls[c].Count;c.roofColours=roofs[c].Count;
                c.piecesPerRoadKm=c.pieces/km;c.buildingsPerRoadKm=c.buildings/km;c.lampsPerRoadKm=c.lamps/km;c.furniturePerRoadKm=c.furniture/km;c.naturePerRoadKm=c.nature/km;
                c.lifePerRoadKm=(c.residents+c.folk+c.parked+c.cruisers)/km;
            }
            foreach(var c in cols)c.failures=Failures(c);
            var report=new Report{regions=cols};json=JsonUtility.ToJson(report,true);
            // The table.
            var sb=new StringBuilder();
            void Row(string label,System.Func<Column,string> f){sb.Append(label.PadRight(34));foreach(var c in cols)sb.Append(f(c).PadLeft(14));sb.AppendLine();}
            Row("measure",c=>c.id);
            sb.AppendLine("-- size");Row("road km",c=>$"{c.roadKm:0.00}");Row("kit pieces",c=>$"{c.pieces}");
            sb.AppendLine("-- what's built along the roads");
            Row("pieces per road km",c=>$"{c.piecesPerRoadKm:0}");Row("buildings per road km",c=>$"{c.buildingsPerRoadKm:0.0}");
            Row("street furniture per road km",c=>$"{c.furniturePerRoadKm:0.0}");Row("planting per road km",c=>$"{c.naturePerRoadKm:0}");Row("lamps per road km",c=>$"{c.lampsPerRoadKm:0.0}");
            sb.AppendLine("-- variety");
            Row("buildings",c=>$"{c.buildings}");Row("building designs",c=>$"{c.designs}");Row("most common design share",c=>$"{c.topDesignShare:P0}");
            Row("same design next door",c=>$"{c.sameNeighbourShare:P0}");Row("wall colours",c=>$"{c.wallColours}");Row("roof colours",c=>$"{c.roofColours}");
            sb.AppendLine("-- life");
            Row("named residents",c=>$"{c.residents}");Row("chatting folk",c=>$"{c.folk}");Row("parked cars",c=>$"{c.parked}");Row("cruising cars",c=>$"{c.cruisers}");Row("life per road km",c=>$"{c.lifePerRoadKm:0.0}");
            sb.AppendLine("-- things to do");
            Row("keepsakes",c=>$"{c.keepsakes}");Row("speed traps",c=>$"{c.traps}");Row("photo sights",c=>$"{c.photos}");Row("Pit Stops",c=>$"{c.pitStops}");Row("event / story spots",c=>$"{c.spots}");
            sb.AppendLine("-- defects (must be 0 in a new area)");
            Row("floating pieces",c=>$"{c.floating}");Row("loose bunting ends",c=>$"{c.looseBunting}");Row("road-edge lips",c=>$"{c.lips}");Row("road seams",c=>$"{c.seams}");Row("banks too steep",c=>$"{c.banks}");Row("cars checked",c=>$"{c.carsChecked}");Row("cars not on the ground",c=>$"{c.carsOffGround}");
            sb.AppendLine("-- against the standard (docs/57)");
            foreach(var c in cols){sb.AppendLine($"{c.name} ({c.kind}): {(c.failures.Count==0?"PASS":"FAIL, "+c.failures.Count+" short")}");foreach(var f in c.failures)sb.AppendLine("    "+f);}
            sb.AppendLine("-- cars: "+CarContactReport.TrimEnd());
            sb.AppendLine("-- road seams: "+SeamReport.TrimEnd());
            return sb.ToString();
        }
    }
}
