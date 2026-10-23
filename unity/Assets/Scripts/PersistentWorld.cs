using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleRoad
{
    // One world, kept across races (docs/72 phase 1; Richard, 8 Oct: "loading the whole map every time is not going to be
    // an acceptable route"). Entering a race or returning to town reloads the scene, which used to destroy the world and
    // generate it again from scratch (~11.7 s). Now the free-roam world, once built, is gathered under one root that
    // survives the reload, and every later load in the same process reuses it: only lighting, sky, fog and the race's
    // own dressing are made again. Races run in the free-roam world, as Forza Horizon's do.
    // Studies and tests that start straight in a race build fresh as before (nothing to reuse); --fresh-world turns
    // reuse off for A/B comparison.
    public sealed class PersistentWorld:MonoBehaviour
    {
        public static PersistentWorld Current{get;private set;}
        public static bool Enabled=>!RaceTrial.Has("--fresh-world");
        int footprints,placed;
        public int Reuses{get;private set;}
        // Is this load's world the kept one (a race or the town again), not freshly built? What's placed once per world
        // (the streets' dressing, the bank barriers) checks this: on every return it was placed again, 23 Oct.
        public static bool Reused=>Current!=null&&Current.Reuses>0;

        public static HashSet<GameObject> Roots(){var set=new HashSet<GameObject>();foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())set.Add(go);return set;}

        // A world to reuse? Then the registries the build filled are trimmed back to what the world itself put there
        // (later loads add their own pieces: parked cars, notices), and the woods streamer restarts if trees are queued.
        public static bool TryReuse()
        {
            if(!Enabled||Current==null)return false;
            // A district still building in the background is finished now, behind this load's card, if a race's area touches
            // its ground (a town load, or a race elsewhere, leaves it to carry on once the town is back: Districts.Start); what
            // waited for it belonged to the scene that just went (the new one asks again).
            Districts.Complete(abandonWaiting:true,only:ground=>{if(!RaceArea.Active)return false;var c=RaceArea.Centre;return RaceArea.Near(new Vector2(Mathf.Clamp(c.x,ground.xMin,ground.xMax),Mathf.Clamp(c.y,ground.yMin,ground.yMax)),100);});
            // Pieces that went with the old scene (parked cars, notices) leave the registries; the world's own stay,
            // trees streamed in after the build included.
            for(int i=BayviewKit.Footprints.Count-1;i>=0;i--)if(BayviewKit.FootprintOwners[i]==null){BayviewKit.Footprints.RemoveAt(i);BayviewKit.FootprintOwners.RemoveAt(i);}
            BayviewKit.Placed.RemoveAll(go=>go==null);
            if(WorldTiles.Pending>0)WorldTiles.Streamer.Ensure();
            Current.Reuses++;Debug.Log($"LITTLE_ROAD_WORLD_REUSED reuse={Current.Reuses} footprints={BayviewKit.Footprints.Count} placed={BayviewKit.Placed.Count} pending_trees={WorldTiles.Pending}");
            return true;
        }

        // Racing in the kept world: the world's pieces away from the circuit are switched off, as a race build used to
        // leave them out (RaceArea: 60 m beyond the area), so a 24-car race costs what it did. Terrain, roads, water and
        // the horizon stay. Town brings them all back.
        public static void Focus(Vector2 centre,float radius)
        {
            int off=0;var t=System.Diagnostics.Stopwatch.StartNew();
            foreach(var go in BayviewKit.Placed){if(go==null)continue;var p=go.transform.position;bool keep=Vector2.Distance(new Vector2(p.x,p.z),centre)<radius+60;if(go.activeSelf!=keep)go.SetActive(keep);if(!keep)off++;}
            Debug.Log($"LITTLE_ROAD_WORLD_FOCUS race centre={centre.x:0},{centre.y:0} radius={radius:0} off={off} ms={t.ElapsedMilliseconds}");
        }
        public static void Unfocus(){foreach(var go in BayviewKit.Placed)if(go!=null&&!go.activeSelf)go.SetActive(true);}
        // The bounding circle of a circuit plus a margin (for a race RaceArea doesn't set: Bayview).
        public static (Vector2 centre,float radius) Circle(Circuit track,float margin)
        {
            Vector2 lo=new(float.MaxValue,float.MaxValue),hi=new(float.MinValue,float.MinValue);
            for(float d=0;d<track.length;d+=10){var p=track.At(d);lo=Vector2.Min(lo,new Vector2(p.x,p.z));hi=Vector2.Max(hi,new Vector2(p.x,p.z));}
            return ((lo+hi)*.5f,(hi-lo).magnitude*.5f+margin);
        }

        // After a free-roam build: everything the build made at the top of the scene goes under one root that survives
        // scene loads. (Only the full free-roam world is kept: a race's lighter world, built when nothing was there to
        // reuse, is thrown away as before.)
        public static void Adopt(HashSet<GameObject> before)
        {
            if(!Enabled)return;
            var root=new GameObject("Persistent world");var world=root.AddComponent<PersistentWorld>();int moved=0;
            foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects()){if(go==root||before.Contains(go))continue;go.transform.SetParent(root.transform,true);moved++;}
            world.footprints=BayviewKit.Footprints.Count;world.placed=BayviewKit.Placed.Count;   // (for the log)
            DontDestroyOnLoad(root);Current=world;
            Debug.Log($"LITTLE_ROAD_WORLD_KEPT roots={moved} footprints={world.footprints} placed={world.placed}");
        }
    }
}
