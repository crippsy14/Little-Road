using UnityEngine;

namespace LittleRoad
{
    // Build near the start, stream the rest (--near-start, docs/brief-near-start.md, stage 1): the world builds behind the
    // loading card only what's within reach of where the car starts (the save's spot, a race's return spot, or Bayview's
    // forecourt: WorldTiles.Focus); every built place further out is a background district (Districts).
    // Stage 1: Hillcrest's valley layer (the town, the castle, the station, the raceway's buildings, the countryside and the
    // farm, then the mainland's trees, rocks and verges, which keep off the town's reserved ground) when the car starts on
    // the island side. It's deferred only when Quarry Hills, Bramblewick and Vinelands are too, and they build after it, so
    // every place is built in the order it is at load (they share ground and data with it: the farm and Bramblewick's
    // ground overlap, Quarry Hills adds to the town's chimneys, and the parked cars and townsfolk draw from shared random
    // sequences in place order).
    public static class NearStart
    {
        public static bool Enabled=>RaceTrial.Has("--near-start");
        // A district builds at load when the start is within this of its ground.
        public const float Ring=300;
        // Hillcrest's valley layer: everything HillcrestTown and the mainland's trees place (the trees' scatter is x -1080..1220,
        // z -1990..-410; the town's countryside and farm are inside it).
        public static readonly Rect HillcrestGround=Rect.MinMaxRect(-1150,-2050,1300,-400);
        public static float Away(Rect r,Vector3 p){float dx=Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),dz=Mathf.Max(r.yMin-p.z,0,p.z-r.yMax);return Mathf.Sqrt(dx*dx+dz*dz);}
        // Stage 1's decision, from where the car starts and whether the places that follow Hillcrest are deferred.
        public static bool DeferHillcrest(Vector3 start,bool othersDeferred)=>Enabled&&othersDeferred&&OutsideHillcrest(start);
        public static bool OutsideHillcrest(Vector3 start)=>Away(HillcrestGround,start)>Ring;
        // Stage 2: a far place's road (Quarry Hills', Bramblewick's, Vinelands'), drawn with the far roads' district when Hillcrest
        // is deferred (CoastalLandscape.ValleyRoads). Its ground: 25 m cells within 40 m of any of their lines.
        public static bool FarRoad(string name)=>HillcrestRoute.PlaceOf.TryGetValue(name,out var place)&&place!="hillcrest";
        const float Cell=25;static readonly System.Collections.Generic.HashSet<(int,int)> farCells=new();
        public static Rect FarRoadsGround()
        {
            farCells.Clear();float x0=float.MaxValue,z0=float.MaxValue,x1=float.MinValue,z1=float.MinValue;
            foreach(var r in HillcrestRoute.All()) {
                if(!FarRoad(r.name))continue;
                foreach(var p in r.line){x0=Mathf.Min(x0,p.x);x1=Mathf.Max(x1,p.x);z0=Mathf.Min(z0,p.z);z1=Mathf.Max(z1,p.z);
                    int cx=Mathf.FloorToInt(p.x/Cell),cz=Mathf.FloorToInt(p.z/Cell);for(int i=-2;i<=2;i++)for(int j=-2;j<=2;j++)farCells.Add((cx+i,cz+j));}
            }
            return x0>x1?new Rect():Rect.MinMaxRect(x0-40,z0-40,x1+40,z1+40);
        }
        public static bool OnFarRoads(Vector3 p)=>farCells.Contains((Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.z/Cell)));
        // Stage 4: Bayview's island (the new island's land, ±270 x ±252 m round the origin, its Halt quay and sea railway), built
        // later when the car starts away from it. Its land and collider stay at load (the causeway's piers and the Halt
        // quay's shoulders stand on them); what's built on it waits.
        public static readonly Rect IslandGround=Rect.MinMaxRect(-280,-262,280,262);
        public static bool OnIsland(Vector3 p)=>IslandGround.Contains(new Vector2(p.x,p.z));
        public static bool OutsideIsland(Vector3 start)=>Away(IslandGround,start)>Ring;
        // Every scene root a step of `work` makes goes under the kept world (PersistentWorld.Adopt took the load's own): built
        // after the load, Bayview's town and circuit would otherwise be thrown away with the scene on the first race.
        public static System.Collections.IEnumerator Kept(System.Collections.IEnumerator work)
        {
            var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(work);
            while(stack.Count>0) {
                var before=PersistentWorld.Roots();bool more=stack.Peek().MoveNext();
                if(PersistentWorld.Current!=null)foreach(var go in PersistentWorld.Roots())if(!before.Contains(go)&&go!=PersistentWorld.Current.gameObject)go.transform.SetParent(PersistentWorld.Current.transform,true);
                if(!more){stack.Pop();continue;}
                if(stack.Peek().Current is System.Collections.IEnumerator inner)stack.Push(inner);else yield return stack.Peek().Current;
            }
        }
    }
    public sealed partial class CoastalLandscape
    {
        Transform hillcrestHolder;int hillcrestTyres;
        // Stage 4: Bayview's island built later (IslandLater): TrialWorld's part (the circuit's surfaces, the Pit Stop, the town)
        // is handed over as IslandFinish, numbering its tyre runs from IslandTyres, where the load stood.
        public static bool IslandLater{get;private set;}
        public static System.Func<System.Collections.IEnumerator> IslandFinish;public static int IslandTyres;
        int islandPlacedFrom;
        // As at load: the planting and the windmill, the sea railway and Bayview Halt, their runs flushed, rails trimmed and
        // pieces settled (the world's settle pass, before the ground cover and the town were made), the ground cover, then
        // TrialWorld's part.
        System.Collections.IEnumerator IslandRoutine()
        {
            islandPlacedFrom=BayviewKit.Placed.Count;
            Physics.SyncTransforms();BayviewCountryside.ChoosePlots(this);yield return PlantingRoutine();Lap("island planting");yield return null;
            // (At load the mill is spawned with no parent before the kit's animated root exists, and a piece whose parent is the
            //  animated root counts as animated (both null): not placed, no footprint. Built later the root is there, so its
            //  entries are taken back out, and it stands in the world as at load.)
            int placedAt=BayviewKit.Placed.Count,footAt=BayviewKit.Footprints.Count;NewBayview.Windmill(Height);
            for(int i=BayviewKit.Placed.Count-1;i>=placedAt;i--)if(BayviewKit.Placed[i]!=null&&BayviewKit.Placed[i].name=="tower_mill")BayviewKit.Placed.RemoveAt(i);
            for(int i=BayviewKit.Footprints.Count-1;i>=footAt;i--)if(BayviewKit.FootprintOwners[i]!=null&&BayviewKit.FootprintOwners[i].name=="tower_mill"){BayviewKit.Footprints.RemoveAt(i);BayviewKit.FootprintOwners.RemoveAt(i);}
            yield return null;
            Railway(HillcrestRoute.SeaRail,"HILLCREST LINE");Lap("island sea railway");yield return null;BayviewHalt();Lap("island halt");yield return null;
            yield return Runs.FlushRoutine();
            TrimRails(NearStart.OnIsland);Physics.SyncTransforms();
            for(int i=islandPlacedFrom;i<BayviewKit.Placed.Count;i++)if(BayviewKit.Placed[i]!=null)PlacementAudit.SettleOne(BayviewKit.Placed[i]);
            Physics.SyncTransforms();Lap("island settle");yield return null;
            yield return new CoastalGroundCover(this,track,root).BuildRoutine();Lap("island ground cover");yield return null;
            // (TrialWorld's part ran after the load's runs were flushed, each run drawn as it was laid: the roadside walls' raycasts see
            //  the walls before them)
            if(IslandFinish!=null){TyreBarriers.Offset=IslandTyres-(TyreBarriers.Laid+TyreBarriers.Refused);bool was=Runs.Batching;Runs.Batching=false;
                try{yield return IslandFinish();}finally{TyreBarriers.Offset=0;IslandFinish=null;Runs.Batching=was;}}
        }
        // The world's settle as AdventureGame did it after the load: its standing cars seated, everything on its ground
        // dressed, its banks barriered; the town's meshes batched.
        System.Collections.IEnumerator SettleIsland()
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();Physics.SyncTransforms();
            if(NewBayview.TownRoot!=null)CarContact.SeatUnder(NewBayview.TownRoot);yield return null;
            int dressed=0,barriers=0;
            if(PropForge.Enabled) {
                Physics.SyncTransforms();yield return PropForgeDressing.DressRoutine(root,null,n=>dressed=n,8,g=>NearStart.OnIsland(g.transform.position));
                bool was=Runs.Batching;Runs.Batching=true;barriers=BankBarriers.Build(root,track,p=>Districts.Completes("island",p),"island");Runs.Batching=was;
                yield return Runs.FlushRoutine();Physics.SyncTransforms();yield return null;
            }
            if(NewBayview.TownRoot!=null&&StaticBatching)yield return BatchInSlices(NewBayview.TownRoot);
            Debug.Log($"LITTLE_ROAD_DISTRICT_SETTLE island dressed={dressed} barriers={barriers} ms={clock.ElapsedMilliseconds}");
        }
        // Stage 2: the far roads' drawing, pass by pass as ValleyRoads would have done it (null: drawn at load). Its pieces: the
        // roads' holder's children and the placed kit pieces from where its build began.
        System.Collections.Generic.List<System.Action> farRoads;int farChildFrom,farPlacedFrom;
        System.Collections.IEnumerator FarRoadsRoutine()
        {
            var holder=root.Find("Hillcrest roads");farChildFrom=holder!=null?holder.childCount:0;farPlacedFrom=BayviewKit.Placed.Count;
            Physics.SyncTransforms();var slice=System.Diagnostics.Stopwatch.StartNew();
            foreach(var draw in farRoads){draw();if(slice.ElapsedMilliseconds>=8){yield return null;slice.Restart();}}
            farRoads=null;
        }
        // As the world's own settle (BuildRoutine, then AdventureGame) did for them at load: their kit pieces settled and
        // dressed, their banks barriered, their meshes batched. (Rails are trimmed by the places' settle passes: TrimRails.)
        System.Collections.IEnumerator SettleFarRoads()
        {
            var mine=new System.Collections.Generic.HashSet<GameObject>();for(int i=farPlacedFrom;i<BayviewKit.Placed.Count;i++)if(BayviewKit.Placed[i]!=null)mine.Add(BayviewKit.Placed[i]);
            Physics.SyncTransforms();foreach(var g in mine)PlacementAudit.SettleOne(g);yield return null;
            if(PropForge.Enabled) {
                Physics.SyncTransforms();yield return PropForgeDressing.DressRoutine(root,null,_=>{},8,mine.Contains);
                bool was=Runs.Batching;Runs.Batching=true;BankBarriers.Build(root,null,p=>Districts.Completes("far-roads",p),"far-roads");Runs.Batching=was;
                yield return Runs.FlushRoutine();Physics.SyncTransforms();yield return null;
            }
            var holder=root.Find("Hillcrest roads");
            if(holder!=null&&StaticBatching) {
                var batch=new System.Collections.Generic.List<GameObject>();
                for(int i=farChildFrom;i<holder.childCount;i++)foreach(var r in holder.GetChild(i).GetComponentsInChildren<MeshRenderer>())if(!r.isPartOfStaticBatch)batch.Add(r.gameObject);
                for(int i=0;i<batch.Count;i+=300){StaticBatchingUtility.Combine(batch.GetRange(i,Mathf.Min(300,batch.Count-i)).ToArray(),holder.gameObject);yield return null;}
            }
            Debug.Log($"LITTLE_ROAD_DISTRICT_SETTLE far-roads pieces={mine.Count}");
        }
        public static bool OnHillcrestGround(Vector3 p)=>NearStart.HillcrestGround.Contains(new Vector2(p.x,p.z));
        // The town then the trees, as at load (BuildRoutine), a frame between their parts. (Its tyre runs numbered from where the
        // load stood when it was deferred, so they're named and seeded as at load: TyreBarriers.Offset. The world at load
        // dressed nothing on its ground: its settle pass dresses all of it, PropForgeDressing.Later.)
        System.Collections.IEnumerator HillcrestRoutine()
        {
            Physics.SyncTransforms();var town=new HillcrestTown(this,root);hillcrestHolder=town.Root;
            TyreBarriers.Offset=hillcrestTyres-(TyreBarriers.Laid+TyreBarriers.Refused);
            try{yield return town.BuildRoutine();}finally{TyreBarriers.Offset=0;}
            yield return null;
            yield return MainlandTreesRoutine();
        }
    }
}
