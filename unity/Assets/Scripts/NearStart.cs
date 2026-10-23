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
    }
    public sealed partial class CoastalLandscape
    {
        Transform hillcrestHolder;int hillcrestTyres;
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
