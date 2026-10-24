using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// World loading (docs/72): the footprint grid answers exactly as the old full scan did; the kept world's race focus
// circle; the world cache stays off in the editor (no build id), so checks always build fresh; long audio loops load
// without a main-thread decode.
public static class WorldStreamingChecks
{
    static void Require(bool ok,string why){if(!ok)throw new Exception("World streaming: "+why);}
    public static List<string> Run()
    {
        var checks=new List<string>();
        var saved=new List<Bounds>(BayviewKit.Footprints);
        try {
            BayviewKit.Footprints.Clear();var rng=new System.Random(72);
            float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
            for(int i=0;i<3000;i++)BayviewKit.Footprints.Add(new Bounds(new Vector3(R(-2000,2000),R(0,60),R(-2000,2000)),new Vector3(R(.5f,40),R(.5f,20),R(.5f,40))));
            int mismatches=0,hits=0;
            for(int q=0;q<2000;q++) {
                var area=new Bounds(new Vector3(R(-2000,2000),R(0,60),R(-2000,2000)),new Vector3(R(1,80),R(1,20),R(1,80)));int upTo=q%3==0?rng.Next(0,3000):3000;
                bool brute=false;for(int i=0;i<upTo&&i<BayviewKit.Footprints.Count;i++)if(BayviewKit.Footprints[i].Intersects(area)){brute=true;break;}
                if(brute)hits++;if(BayviewKit.Occupied(area,upTo)!=brute)mismatches++;
            }
            // Cleared and trimmed lists re-index (PersistentWorld trims footprints that went with an old scene).
            BayviewKit.Footprints.RemoveRange(0,1500);var probe=BayviewKit.Footprints[0];
            Require(BayviewKit.Occupied(probe,BayviewKit.Footprints.Count),"grid not rebuilt after the list was trimmed");
            Require(mismatches==0&&hits>50,$"footprint grid disagrees with the full scan in {mismatches} of 2000 queries ({hits} hits)");
            checks.Add($"Footprint grid (BayviewKit.Occupied) matches the full scan on 2000 random queries ({hits} occupied), including first-N queries and a trimmed list");
        } finally {BayviewKit.Footprints.Clear();BayviewKit.Footprints.AddRange(saved);}
        // The indexed polyline distance (HillcrestRoute.Distance on long lines) matches the full scan.
        {
            var rng2=new System.Random(73);int bad=0;
            for(int l=0;l<6;l++) {
                var line=new Vector3[200+l*150];var at=new Vector3((float)rng2.NextDouble()*400,0,(float)rng2.NextDouble()*400);
                for(int i=0;i<line.Length;i++){at+=new Vector3((float)rng2.NextDouble()*16-8,(float)rng2.NextDouble()*2-1,(float)rng2.NextDouble()*16-8);line[i]=at;}
                for(int q=0;q<400;q++) {
                    float x=(float)rng2.NextDouble()*1200-400,z=(float)rng2.NextDouble()*1200-400;
                    float best=float.MaxValue,height=0;var p=new Vector2(x,z);
                    for(int i=0;i<line.Length-1;i++){Vector2 a2=new(line[i].x,line[i].z),b2=new(line[i+1].x,line[i+1].z),ab=b2-a2;float t=Mathf.Clamp01(Vector2.Dot(p-a2,ab)/Mathf.Max(ab.sqrMagnitude,1e-4f));float d=(a2+ab*t-p).sqrMagnitude;if(d<best){best=d;height=Mathf.Lerp(line[i].y,line[i+1].y,t);}}
                    float got=HillcrestRoute.Distance(line,x,z,out float gotHeight);
                    if(got!=Mathf.Sqrt(best)||gotHeight!=height)bad++;
                }
            }
            Require(bad==0,$"indexed polyline distance differs from the full scan in {bad} of 2400 queries");
            checks.Add("Polyline distance index (HillcrestRoute.Distance) matches the full scan exactly on 2400 queries over 6 long lines");
        }
        var (centre,radius)=PersistentWorld.Circle(new Circuit(),450);
        Require(radius>450&&radius<900&&centre.magnitude<200,$"Bayview's race focus circle is wrong: centre {centre}, radius {radius:0}");
        Require(!WorldCache.Enabled,"world cache is on in the editor (checks must build fresh)");
        // Background districts (Districts): the builder runs in order with its nested steps, then the settle pass, then what
        // waited for it, in the order asked; nothing runs before it's started or completed; its ground holds the tile streamer
        // off; and once built, what asks for it runs at once.
        {
            var log=new List<string>();
            System.Collections.IEnumerator Inner(){log.Add("b3");yield break;}
            System.Collections.IEnumerator Build(){log.Add("b1");yield return null;log.Add("b2");yield return Inner();log.Add("b4");}
            System.Collections.IEnumerator Settle(){log.Add("s");yield return null;log.Add("s2");}
            System.Collections.IEnumerator Steps(){log.Add("w2a");yield return null;log.Add("w2b");}
            Districts.Begin();Districts.Defer("check",Build(),Settle,new Rect(0,0,100,100));
            Require(Districts.Waiting("check")&&Districts.Pending&&Districts.Holds(new Rect(50,50,10,10))&&!Districts.Holds(new Rect(500,500,10,10)),"a deferred district isn't waiting, or doesn't hold its ground");
            Districts.WhenBuilt("check",()=>log.Add("w1"));Districts.WhenBuiltSteps("check",Steps);
            Require(log.Count==0,"a deferred district started building before it was asked to");
            Districts.Complete();
            Require(string.Join(",",log)=="b1,b2,b3,b4,s,s2,w1,w2a,w2b",$"district work ran out of order: {string.Join(",",log)}");
            Require(!Districts.Pending&&!Districts.Waiting("check")&&!Districts.Holds(new Rect(50,50,10,10)),"a completed district still pending");
            Districts.WhenBuilt("check",()=>log.Add("late"));Require(log[^1]=="late","asking for a built district didn't run at once");
            Districts.Begin();
            checks.Add("Background districts: builder, settle and waiting set-up run in order on completion; ground held while pending; built districts answer at once");
        }
        // Several districts (23 Oct): they build nearest the start first, whatever order they were deferred in; each owns its
        // ground until it has built; a spot on two grounds is completed by the later one, so it is done once; what waits for
        // each runs when that one arrives.
        {
            var log=new List<string>();
            System.Collections.IEnumerator B(string n){log.Add("b"+n);yield break;}
            Districts.Begin();
            Districts.Defer("far",B("far"),()=>B("sfar"),new Rect(50,0,100,100),null,3000);Districts.Defer("near",B("near"),()=>B("snear"),new Rect(0,0,100,100),null,1000);
            Districts.WhenBuilt("far",()=>log.Add("wfar"));Districts.WhenBuilt("near",()=>log.Add("wnear"));
            var both=new Vector3(75,0,50);var nearOnly=new Vector3(25,0,50);var farOnly=new Vector3(125,0,50);var outside=new Vector3(500,0,500);
            Require(!Districts.Built(both)&&!Districts.Built(nearOnly)&&!Districts.Built(farOnly)&&Districts.Built(outside),"a queued district's ground counts as built, or open ground doesn't");
            Require(Districts.PendingAt(both)=="far"&&Districts.PendingAt(nearOnly)=="near"&&Districts.PendingAt(farOnly)=="far"&&Districts.PendingAt(outside)==null,"the pending district at a spot is wrong");
            Require(Districts.Completes("near",nearOnly)&&!Districts.Completes("near",both)&&!Districts.Completes("near",farOnly)&&!Districts.Completes("far",both),"which district completes a spot is wrong");
            Require(Districts.PendingScope=="x-near-far","the pending scope is "+Districts.PendingScope);
            Require(Districts.Holds(new Rect(120,50,10,10))&&!Districts.Holds(new Rect(300,300,10,10)),"two districts don't hold their ground");
            // (A race in the far one's ground finishes that one alone; the near one carries on later.)
            Districts.Complete(false,ground=>ground.xMin>=50);
            Require(!Districts.Waiting("far")&&Districts.Waiting("near")&&Districts.Pending&&string.Join(",",log)=="bfar,bsfar,wfar",$"finishing one district ran: {string.Join(",",log)}");
            Districts.Complete();
            Require(string.Join(",",log)=="bfar,bsfar,wfar,bnear,bsnear,wnear",$"two districts ran out of order: {string.Join(",",log)}");
            Require(Districts.Built(both)&&Districts.Completes("far",both)&&Districts.PendingAt(both)==null&&Districts.PendingScope==null&&!Districts.Pending,"completed districts still own ground");
            Districts.Begin();
            checks.Add("Several background districts: nearest first, each owning its ground until built, overlapping ground completed once by the later one, waiters run per district");
        }
        // Race entry builds the field's engine sounds on worker threads (CarAudio.Prepare): one built there renders exactly
        // what the same engine built on the main thread does.
        {
            var fit=EquipmentCatalog.StockLoadout();var spec=EngineSoundSpec.From(EquipmentCatalog.Get(fit["engine"]),EquipmentCatalog.Get(fit["turbo"]),EquipmentCatalog.Get(fit["transmission"]));
            var bank=EngineBanks.Get(spec.recording);if(bank!=null){bank.SustainLoop(true);bank.SustainLoop(false);}
            EngineSynth Make(){var e=new EngineSynth(spec,48000,7919*5+13);if(bank!=null)e.UseGrains(new GrainSynth(bank,48000,7919*5+13,spec.idle,spec.redline));return e;}
            var here=Make();var there=System.Threading.Tasks.Task.Run(Make).Result;
            var a=new float[2048];var b=new float[2048];here.Render(a,0,a.Length);there.Render(b,0,b.Length);
            bool same=true;for(int i=0;i<a.Length;i++)if(a[i]!=b[i]){same=false;break;}
            Require(same,"an engine sound built on a worker thread renders differently");
            checks.Add("Engine sounds built on worker threads at race entry (CarAudio.Prepare) render exactly as on the main thread");
        }
        // Traffic courtesy (--traffic-yield: pull over when followed, let go of a jammed car) is off unless asked for.
        Require(!RoadCruiser.Yields,"traffic yield is on without --traffic-yield");
        checks.Add("Traffic courtesy (RoadCruiser.Yields) off by default (--traffic-yield)");
        // Cache pruning: only the two builds used most recently keep files; unnamed (older format) files go.
        {
            string a="0123456789abcdef0123456789abcdef",b="fedcba9876543210fedcba9876543210",c="00000000111111112222222233333333";
            Require(WorldCache.BuildOf($"valley-field-{a}-qh.bin")==a&&WorldCache.BuildOf($"map-{b}-1a2b3c4d.dxt1")==b&&WorldCache.BuildOf("map-1a2b3c4d.dxt1")==null,"cache file build id not read");
            var dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"little-road-prune-check");if(System.IO.Directory.Exists(dir))System.IO.Directory.Delete(dir,true);System.IO.Directory.CreateDirectory(dir);
            var now=DateTime.UtcNow;int k=0;
            foreach(var (name,age) in new[]{($"valley-field-{a}-qh.bin",1),($"map-{a}-00000001.dxt1",2),($"valley-field-{b}-qh.bin",5),($"valley-field-{c}-qh.bin",9),("map-1a2b3c4d.dxt1",0)}) {
                var path=System.IO.Path.Combine(dir,name);System.IO.File.WriteAllText(path,"x"+k++);System.IO.File.SetLastWriteTimeUtc(path,now.AddHours(-age));
            }
            int gone=WorldCache.Prune(dir);var left=new HashSet<string>(Array.ConvertAll(System.IO.Directory.GetFiles(dir),System.IO.Path.GetFileName));
            System.IO.Directory.Delete(dir,true);
            Require(gone==2&&left.Count==3&&!left.Contains($"valley-field-{c}-qh.bin")&&!left.Contains("map-1a2b3c4d.dxt1"),$"cache prune kept the wrong files ({gone} gone: {string.Join(", ",left)})");
            checks.Add("World and map caches keep only the two most recently used builds' files (older builds and unnamed files pruned)");
        }
        checks.Add($"Kept world (PersistentWorld): Bayview race focus circle radius {radius:0} m; world cache off in the editor");
        // Long loops (music, ambience) stay compressed in memory and load in the background: decompressed on load, the
        // first play of each decoded the whole clip on the main thread (~0.43 s as the town came up, docs/72).
        int clips=0;
        foreach(var folder in new[]{"Assets/Resources/Music","Assets/Resources/Ambience"})
            foreach(var guid in UnityEditor.AssetDatabase.FindAssets("t:AudioClip",new[]{folder})) {
                var path=UnityEditor.AssetDatabase.GUIDToAssetPath(guid);var importer=(UnityEditor.AudioImporter)UnityEditor.AssetImporter.GetAtPath(path);
                Require(importer.defaultSampleSettings.loadType==AudioClipLoadType.CompressedInMemory&&importer.loadInBackground,$"{path} must be compressed in memory and load in the background");
                clips++;
            }
        Require(clips>=10,$"expected the music and ambience loops, found {clips}");
        checks.Add($"Music and ambience ({clips} loops) compressed in memory, loaded in the background (no main-thread decode on first play)");
        // Build near the start (--near-start, docs/brief-near-start.md): Hillcrest's valley layer covers all its landmarks; a
        // Bayview start leaves it for the background, a start in the valley builds it at load; and from Bayview's forecourt the
        // district builds on the boosted budget without the reach guard finishing it at once.
        {
            var ground=NearStart.HillcrestGround;bool In(Vector3 p)=>ground.Contains(new Vector2(p.x,p.z));
            var landmarks=new (string,Vector3)[]{("square",HillcrestRoute.Square),("castle",HillcrestRoute.CastleCourt),("paddock",HillcrestRoute.Paddock),("lookout",HillcrestRoute.Lookout),
                ("vista",HillcrestRoute.Vista),("station",HillcrestTown.StationSpot),("farm",new Vector3(HillcrestRoute.FarmCentre.x,0,HillcrestRoute.FarmCentre.y)),
                ("balloon field",new Vector3(HillcrestRoute.BalloonField.x,0,HillcrestRoute.BalloonField.y)),("lake",new Vector3(HillcrestRoute.Lake.x,0,HillcrestRoute.Lake.y))};
            foreach(var (name,at) in landmarks){Require(In(at),$"Hillcrest's {name} {at} is outside its district's ground");Require(!NearStart.OutsideHillcrest(at),$"a start at Hillcrest's {name} would leave the valley for later");}
            var bayview=NewBayview.Spawn;float away=NearStart.Away(ground,bayview);
            Require(NearStart.OutsideHillcrest(bayview),$"a Bayview start ({away:0} m from the valley) builds Hillcrest at load");
            Require(away>Districts.ReachDistance&&away<Districts.BoostDistance,$"Bayview's start is {away:0} m from the valley: the reach guard ({Districts.ReachDistance} m) or the boost ({Districts.BoostDistance} m) is wrong");
            foreach(var (name,c) in new[]{("Quarry Hills",QuarryHills.Square),("Bramblewick",Bramblewick.Centre),("Vinelands",Vinelands.Centre)})
                Require(NearStart.OutsideHillcrest(new Vector3(c.x,0,c.y)),$"{name}'s centre is inside the near ring of Hillcrest's ground");
            checks.Add($"Near start: Hillcrest's ground holds its {landmarks.Length} landmarks; a Bayview start ({away:0} m out) defers it on the boosted budget, a start in the valley doesn't");
            // Stage 2: every road line has a place; Hillcrest's own are drawn at load; each far road's line is on the far roads' ground.
            int far=0;NearStart.FarRoadsGround();
            foreach(var r in HillcrestRoute.All()) {
                Require(HillcrestRoute.PlaceOf.ContainsKey(r.name),$"road {r.name} has no place");
                if(!NearStart.FarRoad(r.name))continue;far++;
                foreach(var p in r.line)Require(NearStart.OnFarRoads(p),$"far road {r.name} at {p} is off the far roads' ground");
            }
            foreach(var near in new[]{HillcrestRoute.Mainline,HillcrestRoute.Quay,HillcrestRoute.Crest,HillcrestRoute.RacewayAccess,HillcrestRoute.Falls,HillcrestRoute.StationSpur,HillcrestRoute.FarmLane})Require(!NearStart.FarRoad(near.name),$"{near.name} would wait for the far roads");
            if(Bramblewick.Enabled)Require(NearStart.FarRoad(Bramblewick.RoadName),"Valley Road South isn't a far road");
            checks.Add($"Near start: {far} far roads (drawn with their district from an island-side start) on their ground; Hillcrest's seven drawn at load");
            // Stage 4: Bayview's island holds what stands on it; starts at the other places leave it for later, one at the tunnel doesn't.
            foreach(var (name,at) in new (string,Vector3)[]{("spawn",NewBayview.Spawn),("Halt",CoastalLandscape.HaltResident),("sea railway loop",new Vector3(HillcrestRoute.BayLoop.x,0,HillcrestRoute.BayLoop.y)),("Pit Stop",NewBayview.PitStopSite)})
                Require(NearStart.OnIsland(at),$"Bayview's {name} {at} is off the island's ground");
            foreach(var (name,c) in new[]{("Hillcrest",new Vector2(HillcrestRoute.Square.x,HillcrestRoute.Square.z)),("Quarry Hills",QuarryHills.Square),("Bramblewick",Bramblewick.Centre),("Vinelands",Vinelands.Centre)})
                Require(NearStart.OutsideIsland(new Vector3(c.x,0,c.y)),$"a start at {name} would build the island at load");
            Require(!NearStart.OutsideIsland(HillcrestRoute.PortalNorth),"a start at the tunnel's north portal would leave the island for later");
            checks.Add("Near start: the island's ground holds Bayview's spawn, Halt, sea railway and Pit Stop; starts at the other four places leave it for later, one at the tunnel doesn't");
        }
        return checks;
    }
}
