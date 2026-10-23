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
    }
    public sealed partial class CoastalLandscape
    {
        Transform hillcrestHolder;int hillcrestTyres;
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
