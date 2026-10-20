using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleRoad
{
    // Bank barriers (--prop-forge, docs/80 round 7): the area audit's "banks too steep (no barrier)" fixed where they are.
    // Once the world is built, the edge audit finds every stretch of road with a steep drop beside it and nothing to stop a
    // car going over; each run of them gets a Prop Forge dry-stone wall along the top of the drop (slate in Quarry Hills),
    // 3 m past either end. Where the bank's top is too rough for a wall's piers it is tried nearer the road, then as
    // post-and-rail or armco; else it is left out (logged).
    public static class BankBarriers
    {
        public static int Walls,Left;
        // The sites only change with the build and the world's flags, and finding them costs ~1.5 s (the edge audit's
        // raycasts), so a player build keeps them in a small file per build and flag set: only its first launch pays.
        static List<(Vector3 at,Vector3 outward,Vector3 along,float dropAt,string road,float half)> Sites(Circuit bayview)
        {
            string key=Application.isEditor||string.IsNullOrEmpty(Application.buildGUID)?null:$"{Application.buildGUID}-{(NewBayview.On?"nb":"b")}-{(QuarryHills.Enabled?"qh":"")}";
            string file=key==null?null:System.IO.Path.Combine(Application.persistentDataPath,$"bank-sites-{key}.txt");
            var I=System.Globalization.CultureInfo.InvariantCulture;
            if(file!=null&&System.IO.File.Exists(file)) {
                var list=new List<(Vector3,Vector3,Vector3,float,string,float)>();
                try{foreach(var l in System.IO.File.ReadAllLines(file)){var f=l.Split('|');float F(int i)=>float.Parse(f[i],I);
                    list.Add((new Vector3(F(0),F(1),F(2)),new Vector3(F(3),F(4),F(5)),new Vector3(F(6),F(7),F(8)),F(9),f[11],F(10)));}
                    Debug.Log($"LITTLE_ROAD_BANK_SITES cached {list.Count}");return list;}
                catch(System.Exception e){Debug.LogWarning("Bank sites cache unreadable, finding them again: "+e.Message);}
            }
            PlacementAudit.Edges(bayview);var found=new List<(Vector3,Vector3,Vector3,float,string,float)>(PlacementAudit.BankSites);
            if(file!=null)try{System.IO.File.WriteAllLines(file,found.ConvertAll(x=>string.Join("|",new[]{x.Item1.x,x.Item1.y,x.Item1.z,x.Item2.x,x.Item2.y,x.Item2.z,x.Item3.x,x.Item3.y,x.Item3.z,x.Item4,x.Item6}.Select(v=>v.ToString("R",I)))+"|"+x.Item5));}catch(System.Exception e){Debug.LogWarning("Bank sites not cached: "+e.Message);}
            return found;
        }
        public static void Build(Transform parent,Circuit bayview)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();Walls=Left=0;var sites=Sites(bayview);if(sites.Count==0){Debug.Log($"LITTLE_ROAD_BANK_BARRIERS sites=0 ms={clock.ElapsedMilliseconds}");return;}
            var root=new GameObject("Bank barriers").transform;root.SetParent(parent,false);
            var slate=new Dictionary<string,string>{{"drystone","drystone_slate"},{"drystone_pier","drystone_slate_pier"}};
            var group=new List<(Vector3 at,Vector3 outward,Vector3 along,float dropAt,string road,float half)>();
            void Flush()
            {
                if(group.Count==0)return;
                bool quarry=MapRegions.At(group[0].at).name.Contains("Quarry");
                string why="";
                // (dry-stone first, nearer the road where the bank's top is too rough for its piers; then post-and-rail and
                //  armco, whose plumb posts follow a slope)
                foreach(var kit in new[]{"dry_stone","post_rail","armco"})
                foreach(float pull in new[]{.3f,.9f,99f}) {
                    var line=new List<Vector3>();
                    for(int k=0;k<group.Count;k++){var g=group[k];float o=pull>50?g.half+.7f:Mathf.Max(g.dropAt-pull,g.half+.7f);var p=g.at+g.outward*o;if(k==0)line.Add(p-g.along*3);line.Add(p);if(k==group.Count-1)line.Add(p+g.along*3);}
                    var L=Runs.Solve(new RunSpec{name=$"Bank barrier {Walls}",kit=kit,seed=$"bank-{Walls}-{group[0].road}",along=line.ToArray(),restyle=quarry&&kit=="dry_stone"?slate:null});
                    if(L.failures.Count==0&&L.items.Count>0){Runs.Draw(L,root);Runs.Laid.Add(L);Walls++;group.Clear();return;}
                    why=L.failures.Count>0?L.failures[0]:L.notes.Count>0?L.notes[0]:"nothing laid";
                }
                Left++;Debug.Log($"LITTLE_ROAD_BANK_BARRIER left {group[0].road} at {group[0].at} sites={group.Count}: {why}");group.Clear();
            }
            foreach(var s in sites) {
                if(group.Count>0){var last=group[^1];if(last.road!=s.road||Vector3.Dot(last.outward,s.outward)<.5f||Vector3.Distance(last.at,s.at)>8)Flush();}
                group.Add((s.at,s.outward,s.along,s.dropAt,s.road,s.half));
            }
            Flush();Physics.SyncTransforms();
            Debug.Log($"LITTLE_ROAD_BANK_BARRIERS sites={sites.Count} walls={Walls} left={Left} ms={clock.ElapsedMilliseconds}");
        }
    }
}
namespace LittleRoad
{
    // Tyre barriers (--prop-forge, docs/80 round 7; Richard's snaps, 20 Oct: tyre walls dotted about singly "should connect
    // like walls and be around full corners"): a corner's outside as one run of the tyres kit, stacks every 0.8 m with a
    // belt across their fronts. Callers gather a corner's points in order and lay them here; false when the run can't be
    // laid (too short, or its stacks can't stand), so the caller can fall back to its single pieces.
    public static class TyreBarriers
    {
        public static int Laid,Refused;
        public static bool Lay(System.Collections.Generic.List<UnityEngine.Vector3> points,UnityEngine.Transform parent,string name)
        {
            if(points.Count<2){Refused++;return false;}
            var L=Runs.Solve(new RunSpec{name=$"{name} {Laid+Refused}",kit="tyres",seed=$"{name}-{Laid+Refused}",along=points.ToArray()});
            if(L.failures.Count>0||L.items.Count==0){Refused++;if(Refused<=4)Debug.Log($"LITTLE_ROAD_TYRES refused {name} ({points.Count} points, {points[0]}): {(L.failures.Count>0?L.failures[0]:L.notes.Count>0?L.notes[0]:"nothing laid")}");return false;}
            Runs.Draw(L,parent);Runs.Laid.Add(L);Laid++;return true;
        }
    }
}
