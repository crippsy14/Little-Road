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
        return checks;
    }
}
