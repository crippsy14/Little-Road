using UnityEngine;

namespace LittleRoad
{
    // The far places as background districts (Districts; docs/design.md §13, 23 Oct): Bramblewick and Vinelands build after
    // the town is ready when the car starts far from them, as Quarry Hills has since 12 Oct. Each builder is the same code as
    // at load, as a coroutine (BramblewickVillage.BuildRoutine, VinelandsTown.BuildRoutine), and every district shares one
    // settle pass (SettleDistrict). --no-far-districts builds both at load, as before.
    public sealed partial class CoastalLandscape
    {
        // The districts' holders (their settle passes seat, dress and batch what's under them).
        Transform bramblewickHolder,vinelandsHolder;
        static float StartDistance(Vector2 centre)=>Vector2.Distance(new Vector2(WorldTiles.Focus.x,WorldTiles.Focus.z),centre);
        // Build a far place in the background: a free-roam build with the car starting far from it.
        bool DeferFar(Vector2 centre)=>Districts.FarEnabled&&WorldTiles.Streams&&!RaceArea.Active&&StartDistance(centre)>Districts.NearStart;
        // The ground each place's pieces can stand on (30 m round what it claims): rails, fingerposts, traps and barriers there
        // wait for it.
        public static bool NearBramblewick(Vector3 p)=>Bramblewick.Contains(p.x,p.z)||Bramblewick.Contains(p.x+30,p.z)||Bramblewick.Contains(p.x-30,p.z)||Bramblewick.Contains(p.x,p.z+30)||Bramblewick.Contains(p.x,p.z-30);
        public static bool NearVinelands(Vector3 p)=>Vector2.Distance(new Vector2(p.x,p.z),Vinelands.Centre)<Vinelands.Radius+30;
        System.Collections.IEnumerator BramblewickRoutine(){Physics.SyncTransforms();var village=new BramblewickVillage(root);bramblewickHolder=village.Root;yield return village.BuildRoutine();}
        System.Collections.IEnumerator VinelandsRoutine(){Physics.SyncTransforms();var town=new VinelandsTown(root);vinelandsHolder=town.Root;yield return town.BuildRoutine();}
        // A district's own settle pass, as the world's (BuildRoutine, then AdventureGame) did for it at load: its rails trimmed,
        // its planting settled on the ground, standing cars seated, its streets dressed and its banks barriered (both read
        // what's built, so they come with it), its geometry static-batched. (In pieces, a frame each in the background.)
        // `holderOf`: the district's holder, made by its builder.
        System.Collections.IEnumerator SettleDistrict(string district,System.Func<Transform> holderOf)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();var holder=holderOf();
            int trimmed=TrimRails(Districts.Built);Physics.SyncTransforms();int settled=0;
            if(holder!=null)foreach(var go in BayviewKit.Placed)if(go!=null&&go.transform.IsChildOf(holder)&&PlacementAudit.SettleOne(go))settled++;
            // (Its own standing cars only: seating the whole world's again moved every parked car and resident a little, 23 Oct.)
            yield return null;if(holder!=null)CarContact.SeatUnder(holder);yield return null;
            int dressed=0,barriers=0;
            if(PropForge.Enabled) {
                Physics.SyncTransforms();if(holder!=null)yield return PropForgeDressing.DressRoutine(root,holder,n=>dressed=n);
                // (The edge audit of its roads, raycasts: a build's first launch pays, the bank sites are cached after: BankBarriers.
                //  Its walls' meshes are queued and made on every core, as the district's own: fifteen drawn as they came took a 280 ms frame.)
                bool was=Runs.Batching;Runs.Batching=true;barriers=BankBarriers.Build(root,null,p=>Districts.Completes(district,p),district);Runs.Batching=was;
                yield return Runs.FlushRoutine();Physics.SyncTransforms();yield return null;
            }
            // (Static batching in slices of a few hundred renderers a frame, each slice one batch per material; the world's is one
            // call at load, too long a frame for a district: 200 ms and more, 23 Oct.)
            if(holder!=null&&StaticBatching)yield return BatchInSlices(holder);
            var walls=new System.Collections.Generic.List<GameObject>();foreach(var parent in TrimmedWallParents)if(parent!=null)foreach(var r in parent.GetComponentsInChildren<MeshRenderer>())if(!r.isPartOfStaticBatch&&r.name.StartsWith("Corner walls"))walls.Add(r.gameObject);
            if(walls.Count>0&&StaticBatching)StaticBatchingUtility.Combine(walls.ToArray(),root.gameObject);
            Debug.Log($"LITTLE_ROAD_DISTRICT_SETTLE {district} trimmed={trimmed} settled={settled} dressed={dressed} barriers={barriers} walls={walls.Count} ms={clock.ElapsedMilliseconds}");
        }
        static System.Collections.IEnumerator BatchInSlices(Transform holder,int slice=300)
        {
            var group=new System.Collections.Generic.List<GameObject>();
            foreach(var r in holder.GetComponentsInChildren<MeshRenderer>()) {
                if(r.isPartOfStaticBatch)continue;group.Add(r.gameObject);
                if(group.Count>=slice){StaticBatchingUtility.Combine(group.ToArray(),holder.gameObject);group.Clear();yield return null;}
            }
            if(group.Count>0)StaticBatchingUtility.Combine(group.ToArray(),holder.gameObject);
        }
    }
}
