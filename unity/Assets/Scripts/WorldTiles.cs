using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // World tiles (doc 58 W1; docs/60 "load time"): the world on a 250 m grid, and decoration streamed onto it.
    // The builders still decide everything at load (the same random draws in the same order, so the same world), but the
    // free-roam woods - the trees, bushes and tufts - are only instantiated at once within NearRadius of where you start.
    // The rest wait in their tiles and are built in the background, nearest tile to the camera first, a few milliseconds
    // a frame, so the whole country fills in within seconds of arriving. Their spots are reserved meanwhile (Blocked), so
    // nothing placed later lands on a tree that isn't there yet. Audits, tests and the map bake call Complete() first.
    // Races build no woods (CoastalLandscape.ValleyDetail), so they never defer. --no-world-tiles builds everything at load.
    public static class WorldTiles
    {
        public const float Size=250,NearRadius=450;
        public static bool Enabled=>!RaceTrial.Has("--no-world-tiles");
        // Where the player will start (set before the world builds); deferral is on only between Begin and the build's end.
        public static Vector3 Focus;static bool deferring,wanted;
        static readonly Dictionary<(int,int),List<Action>> pending=new();
        static readonly Dictionary<(int,int),List<Vector4>> reserved=new();
        public static int Pending{get;private set;}public static int Deferred{get;private set;}
        // True while queued pieces are being built (after the world's own settle pass): they settle themselves.
        public static bool Late{get;private set;}
        public static (int x,int z) Tile(Vector3 p)=>(Mathf.FloorToInt(p.x/Size),Mathf.FloorToInt(p.z/Size));

        // Free roam asks for streaming before the world builds (where the car will start); races never do.
        public static void Plan(Vector3 focus){Focus=focus;wanted=true;}
        // At the start of every world build (each scene load): whatever was queued belonged to the old world.
        public static void Begin(bool woods)
        {
            pending.Clear();reserved.Clear();Pending=Deferred=0;currentList=null;deferring=wanted&&woods&&Enabled;wanted=false;
        }
        public static void EndBuild(){deferring=false;if(Pending>0)Streamer.Ensure();}
        // A piece of decoration at `at`: built now if deferral is off or it's near the start, else queued on its tile.
        // `radius` (0 for things you can drive through) reserves its spot until it's built.
        public static void Place(Vector3 at,float radius,Action build)
        {
            var d=at-Focus;d.y=0;
            if(!deferring||d.sqrMagnitude<NearRadius*NearRadius){build();return;}
            var key=Tile(at);
            if(!pending.TryGetValue(key,out var list))pending[key]=list=new List<Action>();list.Add(build);Pending++;Deferred++;
            if(radius>0){if(!reserved.TryGetValue(key,out var r))reserved[key]=r=new List<Vector4>();r.Add(new Vector4(at.x,at.y,at.z,radius));}
        }
        // Is there a queued (not yet built) solid piece within `radius` of `at`?
        public static bool Blocked(Vector3 at,float radius)
        {
            if(Pending==0)return false;
            var (tx,tz)=Tile(at);
            for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++) {
                if(!reserved.TryGetValue((tx+dx,tz+dz),out var list))continue;
                foreach(var r in list){float x=r.x-at.x,z=r.z-at.z,reach=r.w+radius;if(x*x+z*z<reach*reach&&Mathf.Abs(r.y-at.y)<8)return true;}
            }
            return false;
        }
        // Build everything still queued (audits, tests, the map bake).
        public static void Complete()
        {
            if(Pending==0)return;var clock=System.Diagnostics.Stopwatch.StartNew();int built=Pending;Late=true;
            // (The tile the streamer is part-way through first, from where it got to.)
            if(currentList!=null){for(;currentIndex<currentList.Count;currentIndex++){Pending--;try{currentList[currentIndex]();}catch(Exception e){Debug.LogException(e);}}
                pending.Remove(current);reserved.Remove(current);currentList=null;}
            foreach(var key in new List<(int,int)>(pending.Keys))BuildTile(key);Late=false;
            Debug.Log($"LITTLE_ROAD_WORLD_TILES completed={built} ms={clock.ElapsedMilliseconds} spawned={BayviewKit.Spawned}");
        }
        static void BuildTile((int,int) key)
        {
            if(!pending.TryGetValue(key,out var list))return;
            pending.Remove(key);reserved.Remove(key);
            foreach(var build in list){Pending--;try{build();}catch(Exception e){Debug.LogException(e);}}
        }
        // Builds part of the nearest tile within the frame's budget; returns true when nothing is left.
        static readonly List<(int,int)> order=new();static (int,int) current;static List<Action> currentList;static int currentIndex;
        static bool Step(Vector3 camera,double budgetMs)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();Late=true;
            try{while(clock.Elapsed.TotalMilliseconds<budgetMs) {
                if(currentList==null) {
                    if(pending.Count==0)return true;
                    // The nearest tile to the camera next.
                    float best=float.MaxValue;foreach(var key in pending.Keys){var c=new Vector3((key.Item1+.5f)*Size,0,(key.Item2+.5f)*Size)-camera;c.y=0;float d=c.sqrMagnitude;if(d<best){best=d;current=key;}}
                    currentList=pending[current];currentIndex=0;
                }
                if(currentIndex<currentList.Count){var build=currentList[currentIndex++];Pending--;try{build();}catch(Exception e){Debug.LogException(e);}}
                else{pending.Remove(current);reserved.Remove(current);currentList=null;}
            }}finally{Late=false;}
            return false;
        }
        // The background builder (free roam only): ~2.5 ms a frame until every tile is built.
        public sealed class Streamer:MonoBehaviour
        {
            static Streamer instance;float started;
            public static void Ensure(){if(instance!=null)return;instance=new GameObject("World tiles streamer").AddComponent<Streamer>();}
            void Start(){started=Time.realtimeSinceStartup;currentList=null;}
            void Update()
            {
                var cam=Camera.main;var at=cam!=null?cam.transform.position:Focus;
                if(Step(at,2.5)){Debug.Log($"LITTLE_ROAD_WORLD_TILES streamed={Deferred} seconds={Time.realtimeSinceStartup-started:0.0} spawned={BayviewKit.Spawned}");Destroy(gameObject);}
            }
            void OnDestroy(){if(instance==this)instance=null;}
        }
    }
}
