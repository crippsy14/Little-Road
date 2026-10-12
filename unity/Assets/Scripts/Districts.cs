using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Background districts (docs/72 phase 2; the default since 12 Oct, --no-background-districts builds everything at load): a district far from where the car starts is built
    // after the town is ready, a few milliseconds a frame, instead of behind the loading card. Its builder is the same code
    // as at load, run as a coroutine that yields between pieces of work, so the world it makes is the same (the world
    // fingerprint checks it). Then its own settle pass (rails trimmed, pieces seated, static batching), and whatever waits
    // for it (WhenBuilt: its residents, shops, activities, signs, townsfolk) is set up, one piece a frame (--town-print
    // checks the town comes out the same as one set up at load).
    // Anything that needs the whole world first calls WorldTiles.Complete(), which finishes the district at once: tests,
    // audits, the map bake and the world fingerprint. A scene load (a race, a warp) finishes it too.
    public static class Districts
    {
        public static bool Enabled=>!RaceTrial.Has("--no-background-districts");
        // Districts nearer the start than this are built at load as before.
        public const float NearStart=1500;
        static readonly Stack<IEnumerator> stack=new();static string building;
        static readonly List<(string district,Func<IEnumerator> then)> waiting=new();
        // Still working (building, settling or setting up what waited for it)?
        public static bool Pending=>stack.Count>0;
        // Is this district still to be built? (Once it's built, what asks for it gets it at once.)
        public static bool Waiting(string district)=>building==district;
        static float started;static int frames;static double busiest;static readonly List<(double ms,string what)> slow=new();
        // The ground the district covers: the tile streamer leaves its tiles until it has arrived, as at load (where the
        // town is set up before any tree is streamed), so what's placed on arrival meets the same reserved spots.
        static Rect area;
        public static bool Holds(Rect tile)=>Pending&&area.Overlaps(tile);

        // At the start of every world build: nothing carries over from the last.
        public static void Begin(){stack.Clear();building=null;waiting.Clear();}
        // Build `district` later: `build` is its builder as a coroutine, `settle` its settle pass (a coroutine too).
        public static void Defer(string district,IEnumerator build,Func<IEnumerator> settle,Rect covers)
        {
            area=covers;stack.Clear();stack.Push(Run(district,build,settle));building=district;started=Time.realtimeSinceStartup;frames=0;busiest=0;slow.Clear();
            Debug.Log($"LITTLE_ROAD_DISTRICT deferred {district}");
        }
        // Run `then` once `district` is built: now, if it already is (or was never deferred).
        public static void WhenBuilt(string district,Action then)=>WhenBuiltSteps(district,()=>Once(then));
        // The same for set-up that yields between pieces of work (a frame each in the background; straight through otherwise).
        public static void WhenBuiltSteps(string district,Func<IEnumerator> then){if(building==district)waiting.Add((district,then));else TrialWorld.RunNow(then());}
        static IEnumerator Once(Action then){then();yield break;}
        // Start building in the background (once the town is ready).
        public static void Start(){if(Pending)Runner.Ensure();}
        // Finish now (the whole world is needed). `abandonWaiting`: what waited belonged to a scene that has gone.
        public static void Complete(bool abandonWaiting=false)
        {
            if(abandonWaiting)waiting.Clear();
            if(!Pending)return;var clock=System.Diagnostics.Stopwatch.StartNew();string name=building;
            while(Pending)Step(double.MaxValue);
            Debug.Log($"LITTLE_ROAD_DISTRICT completed {name} at once ms={clock.ElapsedMilliseconds}");
        }
        static IEnumerator Run(string district,IEnumerator build,Func<IEnumerator> settle)
        {
            yield return build;
            var clock=System.Diagnostics.Stopwatch.StartNew();
            yield return settle();
            building=null;double settleMs=clock.Elapsed.TotalMilliseconds;
            slow.Sort((a,b)=>b.ms.CompareTo(a.ms));var top=new System.Text.StringBuilder();for(int i=0;i<Math.Min(6,slow.Count);i++)top.Append($"{slow[i].what}={slow[i].ms:0} ");
            Debug.Log($"LITTLE_ROAD_DISTRICT built {district} seconds={Time.realtimeSinceStartup-started:0.0} frames={frames} busiest_ms={busiest:0} settle_ms={settleMs:0} slowest: {top}");
            // What waited for it, one piece at a time.
            var run=new List<(string,Func<IEnumerator>)>(waiting);waiting.Clear();slow.Clear();
            foreach(var (_,then) in run){IEnumerator steps=null;try{steps=then();}catch(Exception e){Debug.LogException(e);}if(steps!=null)yield return steps;yield return null;}
            slow.Sort((a,b)=>b.ms.CompareTo(a.ms));var each=new System.Text.StringBuilder();for(int i=0;i<Math.Min(6,slow.Count);i++)each.Append($"{slow[i].what}={slow[i].ms:0} ");
            if(WorldTiles.Pending>0)WorldTiles.Streamer.Ensure();
            Debug.Log($"LITTLE_ROAD_DISTRICT arrived {district} seconds={Time.realtimeSinceStartup-started:0.0} frames={frames} busiest_ms={busiest:0} slowest set-up: {each}");
        }
        // Works on the district for about `budgetMs`; true when it's done.
        static bool Step(double budgetMs)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            while(stack.Count>0) {
                bool more;string lapBefore=CoastalLandscape.LastLap;double t0=clock.Elapsed.TotalMilliseconds;string where=stack.Peek().GetType().Name;
                // While the district builds, its decoration queues on the world tiles as at load (the streamer builds it).
                if(building!=null)WorldTiles.Resume();try{more=stack.Peek().MoveNext();}finally{WorldTiles.Pause();}
                // (The slowest pieces of work, named by the load lap they finished and their coroutine: what to split next.)
                double piece=clock.Elapsed.TotalMilliseconds-t0;if(piece>=12)slow.Add((piece,(CoastalLandscape.LastLap!=lapBefore?CoastalLandscape.LastLap:"after "+lapBefore)+" in "+where));
                if(more){if(stack.Peek().Current is IEnumerator inner)stack.Push(inner);}else stack.Pop();
                if(clock.Elapsed.TotalMilliseconds>=budgetMs)break;
            }
            busiest=Math.Max(busiest,clock.Elapsed.TotalMilliseconds);frames++;
            return stack.Count==0;
        }
        // ~3 ms a frame until the district is built and set up.
        public sealed class Runner:MonoBehaviour
        {
            static Runner instance;
            public static void Ensure(){if(instance!=null)return;instance=new GameObject("District builder").AddComponent<Runner>();}
            void Update(){if(!Pending||Step(3)){Destroy(gameObject);}}
            void OnDestroy(){if(instance==this)instance=null;}
        }
    }
}
