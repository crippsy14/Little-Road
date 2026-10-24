using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Background districts (docs/72 phase 2; the default since 12 Oct, --no-background-districts builds everything at load): a district far from where the car starts is built
    // after the town is ready, a few milliseconds a frame, instead of behind the loading card. Its builder is the same code
    // as at load, run as a coroutine that yields between pieces of work, so the world it makes is the same (the world
    // fingerprint checks it). Then its own settle pass (rails trimmed, pieces seated, streets dressed, banks barriered, static
    // batching), and whatever waits for it (WhenBuilt: its residents, shops, activities, signs, townsfolk, parked cars) is set
    // up, one piece a frame (--town-print checks the town comes out the same as one set up at load).
    // Since 23 Oct several districts queue, nearest the start first (from Bayview: Bramblewick, Quarry Hills, Vinelands;
    // --no-far-districts keeps the two far places at load, docs/design.md §13). Each owns the ground it covers: what's placed
    // by what stands there (rails trimmed, fingerposts, speed traps, bank barriers) waits until every district on that ground
    // has built (Built, PendingAt, Completes).
    // Anything that needs the whole world first calls WorldTiles.Complete(), which finishes every district at once: tests,
    // audits, the map bake and the world fingerprint. A race in the kept world finishes the districts its area touches and
    // leaves the rest for the town (PersistentWorld); a warp finishes them all.
    public static class Districts
    {
        public static bool Enabled=>!RaceTrial.Has("--no-background-districts");
        // The far places (Bramblewick, Vinelands) in the background too; off, they build at load as before 23 Oct.
        public static bool FarEnabled=>Enabled&&!RaceTrial.Has("--no-far-districts");
        // Districts nearer the start than this are built at load as before.
        public const float NearStart=1500;
        enum State{Queued,Building,Settling,Done}
        sealed class District{public string name,after;public IEnumerator build;public Func<IEnumerator> settle;public Rect covers;public Func<Vector3,bool> contains;public float distance;public State state;public bool guarded;}
        // Where the player's car is (AdventureGame), for the reach guard (--near-start, docs/brief-near-start.md): a district's
        // budget goes from 3 to 12 ms a frame once the car is within BoostDistance of its ground, and it's finished at once
        // (one hitch, logged) if the car gets within ReachDistance before it has arrived.
        public static Func<Vector3?> Car;
        public const float BoostDistance=400,ReachDistance=150;
        static float Away(Rect r,Vector3 p){float dx=Mathf.Max(r.xMin-p.x,0,p.x-r.xMax),dz=Mathf.Max(r.yMin-p.z,0,p.z-r.yMax);return Mathf.Sqrt(dx*dx+dz*dz);}
        // In build order: nearest the start first.
        static readonly List<District> all=new();
        static readonly Stack<IEnumerator> stack=new();
        static readonly List<(string district,Func<IEnumerator> then)> waiting=new();
        // Still working (building, settling or setting up what waited for it)?
        public static bool Pending=>stack.Count>0||all.Exists(d=>d.state!=State.Done);
        // Is this district still to come? (Once it has arrived, what asks for it gets it at once.)
        public static bool Waiting(string district){var d=Find(district);return d!=null&&d.state!=State.Done;}
        static District Find(string district)=>all.Find(d=>d.name==district);
        // What's under way (load timing: which district a slow frame was in): "hillcrest Building", or "idle".
        public static string Now{get{var d=all.Find(x=>x.state!=State.Done&&x.state!=State.Queued);return d!=null?$"{d.name} {d.state}":Pending?"setting up":"idle";}}
        static bool Pieces(District d)=>d.state>=State.Settling;   // (its pieces all exist)
        // Is everything at p built? False while a district whose ground it is has still to build.
        public static bool Built(Vector3 p){foreach(var d in all)if(!Pieces(d)&&d.contains(p))return false;return true;}
        // The district still to build whose ground p is on (the last in build order where grounds overlap), or null.
        public static string PendingAt(Vector3 p){string last=null;foreach(var d in all)if(!Pieces(d)&&d.contains(p))last=d.name;return last;}
        // The districts still to build, as a cache key part ("x-bramblewick-quarry-hills"), or null when none.
        public static string PendingScope{get{var names=new List<string>();foreach(var d in all)if(!Pieces(d))names.Add(d.name);return names.Count==0?null:"x-"+string.Join("-",names);}}
        // Does settling `district` complete the world at p? Its ground, with no other district still to build there: where
        // grounds overlap the later one does the work for both, so each spot is done once.
        public static bool Completes(string district,Vector3 p){var me=Find(district);if(me==null||!me.contains(p))return false;foreach(var d in all)if(d!=me&&!Pieces(d)&&d.contains(p))return false;return true;}
        static float started;static int frames;static double busiest;static readonly List<(double ms,string what)> slow=new();
        // The ground a district covers: the tile streamer leaves its tiles until it has arrived, as at load (where the
        // town is set up before any tree is streamed), so what's placed on arrival meets the same reserved spots.
        public static bool Holds(Rect tile){foreach(var d in all)if(d.state!=State.Done&&d.covers.Overlaps(tile))return true;return false;}

        // At the start of every world build: nothing carries over from the last.
        public static void Begin(){all.Clear();stack.Clear();waiting.Clear();}
        // Build `district` later: `build` is its builder as a coroutine, `settle` its settle pass (a coroutine too), `covers`
        // its ground for the tile streamer, `contains` its ground for what's placed by what stands there (the rect itself if
        // null), `distance` from where the car starts (the nearest builds first), `after` a district it must follow whatever the
        // distances (one whose ground or data it shares: the build order at load), `guarded`: the reach guard and CompleteAt watch
        // it (--near-start's own districts, whose grounds are tight; the far places' grounds are broad claims, Vinelands' circle
        // reaching Bramblewick's green, and they keep the 1.5 km rule alone).
        public static void Defer(string district,IEnumerator build,Func<IEnumerator> settle,Rect covers,Func<Vector3,bool> contains=null,float distance=0,string after=null,bool guarded=false)
        {
            var rect=covers;contains??=p=>rect.Contains(new Vector2(p.x,p.z));
            var d=new District{name=district,build=build,settle=settle,covers=covers,contains=contains,distance=distance,after=after,guarded=guarded};
            int i=0;while(i<all.Count&&all[i].distance<=distance)i++;
            if(after!=null){int first=all.FindIndex(o=>o.name==after);if(first>=i)i=first+1;}
            all.Insert(i,d);
            Debug.Log($"LITTLE_ROAD_DISTRICT deferred {district} distance={distance:0} order={i+1}");
        }
        // Run `then` once `district` has arrived: now, if it already has (or was never deferred).
        public static void WhenBuilt(string district,Action then)=>WhenBuiltSteps(district,()=>Once(then));
        // The same for set-up that yields between pieces of work (a frame each in the background; straight through otherwise).
        public static void WhenBuiltSteps(string district,Func<IEnumerator> then){if(Waiting(district))waiting.Add((district,then));else TrialWorld.RunNow(then());}
        static IEnumerator Once(Action then){then();yield break;}
        // Start building in the background (once the town is ready; again after a scene that left districts waiting).
        public static void Start(){if(Pending){Ensure();Runner.Ensure();}}
        static void Ensure()
        {
            if(stack.Count>0)return;
            // (A district left building or settling with nothing to run it: its builder threw. It's given up, so the rest go on.)
            foreach(var d in all)if(d.state==State.Building||d.state==State.Settling)GiveUp(d);
            if(all.Exists(d=>d.state!=State.Done))stack.Push(RunAll());
        }
        static void GiveUp(District d){Debug.LogError($"LITTLE_ROAD_DISTRICT failed {d.name}: its build stopped on an exception (see above); what stands is kept, what waited for it runs on it");d.state=State.Done;}
        // Finish now (the whole world is needed), or with `only` just the districts whose ground it says (a race's: the rest
        // carry on in the background when the town comes back). `abandonWaiting`: what waited belonged to a scene that has gone.
        public static void Complete(bool abandonWaiting=false,Func<Rect,bool> only=null)
        {
            if(abandonWaiting)waiting.Clear();
            if(!Pending)return;var clock=System.Diagnostics.Stopwatch.StartNew();var names=new List<string>();
            foreach(var d in all.ToArray()){if(d.state==State.Done||only!=null&&!only(d.covers))continue;names.Add(d.name);Finish(d);}
            if(only==null){Ensure();while(Pending)Step(stack,double.MaxValue);}
            if(names.Count>0)Debug.Log($"LITTLE_ROAD_DISTRICT completed {string.Join(",",names)} at once ms={clock.ElapsedMilliseconds}{(only!=null&&Pending?" (the rest later)":"")}");
        }
        // Where the car really starts: every district whose ground it is on, or within the reach guard of, finished now (a saved
        // spot refused as no longer clear puts the car at Bayview's forecourt, after the world was planned round the spot).
        public static void CompleteAt(Vector3 p)
        {
            foreach(var d in all.ToArray()){if(d.state==State.Done||!d.guarded||!d.contains(p)&&Away(d.covers,p)>=ReachDistance)continue;var clock=System.Diagnostics.Stopwatch.StartNew();Finish(d);Debug.Log($"LITTLE_ROAD_DISTRICT start-on {d.name}: finished at once ms={clock.ElapsedMilliseconds}");}
        }
        // One district to the end of its settle pass: a queued one on its own, the one building now where it stands (one step at
        // a time, so the next in the queue isn't started).
        static void Finish(District d)
        {
            if(d.state==State.Done)return;
            if(d.after!=null&&Find(d.after) is District first&&first.state!=State.Done)Finish(first);   // (what it follows first)
            if(d.state==State.Queued){var own=new Stack<IEnumerator>();own.Push(Run(d));while(own.Count>0)Step(own,double.MaxValue);}
            else{Ensure();while(d.state!=State.Done&&stack.Count>0)Step(stack,0);}
            if(d.state!=State.Done)GiveUp(d);
        }
        static IEnumerator RunAll(){foreach(var d in all.ToArray())if(d.state!=State.Done){yield return Run(d);if(d.state!=State.Done)GiveUp(d);}}
        static IEnumerator Run(District d)
        {
            if(d.state!=State.Queued)yield break;   // (finished on its own while the queue waited for it)
            d.state=State.Building;started=Time.realtimeSinceStartup;frames=0;busiest=0;slow.Clear();
            // (Its runs' meshes queue while it builds and are made on every core before its settle pass, as the world's are at
            // load: Runs.Flush. Drawn one by one as they came they took whole frames.)
            bool batch=!RaceTrial.Has("--no-run-batching");bool was=Runs.Batching;if(batch)Runs.Batching=true;
            yield return d.build;
            Runs.Batching=was;if(batch)yield return Runs.FlushRoutine();
            d.state=State.Settling;var clock=System.Diagnostics.Stopwatch.StartNew();
            yield return d.settle();
            d.state=State.Done;double settleMs=clock.Elapsed.TotalMilliseconds;
            slow.Sort((a,b)=>b.ms.CompareTo(a.ms));var top=new System.Text.StringBuilder();for(int i=0;i<Math.Min(6,slow.Count);i++)top.Append($"{slow[i].what}={slow[i].ms:0} ");
            Debug.Log($"LITTLE_ROAD_DISTRICT built {d.name} seconds={Time.realtimeSinceStartup-started:0.0} frames={frames} busiest_ms={busiest:0} settle_ms={settleMs:0} slowest: {top}");
            // What waited for it, one piece at a time.
            var run=new List<(string,Func<IEnumerator>)>();foreach(var w in waiting)if(w.district==d.name)run.Add(w);waiting.RemoveAll(w=>w.district==d.name);slow.Clear();
            foreach(var (_,then) in run){IEnumerator steps=null;try{steps=then();}catch(Exception e){Debug.LogException(e);}if(steps!=null)yield return steps;yield return null;}
            slow.Sort((a,b)=>b.ms.CompareTo(a.ms));var each=new System.Text.StringBuilder();for(int i=0;i<Math.Min(6,slow.Count);i++)each.Append($"{slow[i].what}={slow[i].ms:0} ");
            if(WorldTiles.Pending>0)WorldTiles.Streamer.Ensure();
            Debug.Log($"LITTLE_ROAD_DISTRICT arrived {d.name} seconds={Time.realtimeSinceStartup-started:0.0} frames={frames} busiest_ms={busiest:0} slowest set-up: {each}");
        }
        static bool Building{get{foreach(var d in all)if(d.state==State.Building)return true;return false;}}
        // Works on `work` for about `budgetMs` (one step when 0); true when it's done.
        static bool Step(Stack<IEnumerator> work,double budgetMs)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            while(work.Count>0) {
                bool more;string lapBefore=CoastalLandscape.LastLap;double t0=clock.Elapsed.TotalMilliseconds;string where=work.Peek().GetType().Name;
                // While a district builds, its decoration queues on the world tiles as at load (the streamer builds it).
                // (A builder that throws is dropped, logged, and its district given up where it stands: RunAll, Finish, Ensure.)
                if(Building)WorldTiles.Resume();try{more=work.Peek().MoveNext();}catch(Exception e){Debug.LogException(e);more=false;}finally{WorldTiles.Pause();}
                // (The slowest pieces of work, named by the load lap they finished and their coroutine: what to split next.)
                double piece=clock.Elapsed.TotalMilliseconds-t0;if(piece>=12)slow.Add((piece,(CoastalLandscape.LastLap!=lapBefore?CoastalLandscape.LastLap:"after "+lapBefore)+" in "+where));
                if(more){if(work.Peek().Current is IEnumerator inner)work.Push(inner);}else work.Pop();
                if(clock.Elapsed.TotalMilliseconds>=budgetMs)break;
            }
            busiest=Math.Max(busiest,clock.Elapsed.TotalMilliseconds);frames++;
            return work.Count==0;
        }
        // ~3 ms a frame until every district is built and set up.
        public sealed class Runner:MonoBehaviour
        {
            static Runner instance;
            public static void Ensure(){if(instance!=null)return;instance=new GameObject("District builder").AddComponent<Runner>();}
            void Update()
            {
                Districts.Ensure();double budget=3;
                if(Car?.Invoke() is Vector3 car)foreach(var d in all.ToArray()) {
                    if(d.state==State.Done||!d.guarded)continue;float away=Away(d.covers,car);
                    if(away<ReachDistance){var clock=System.Diagnostics.Stopwatch.StartNew();Finish(d);Debug.Log($"LITTLE_ROAD_DISTRICT reached {d.name} away={away:0} m: finished at once ms={clock.ElapsedMilliseconds}");}
                    else if(away<BoostDistance)budget=12;
                }
                if(!Pending||Step(stack,budget)){Destroy(gameObject);}
            }
            void OnDestroy(){if(instance==this)instance=null;}
        }
    }
}
