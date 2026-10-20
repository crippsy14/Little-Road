using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Cook once, load forever (docs/72 phase 3): generated world data that's identical every launch for the same build and
    // world flags is saved under persistentDataPath/world-cache and read back instead of regenerated. One file per build,
    // name and world; none in the editor (no build id) or with --no-world-cache.
    // Why a new file per build: a new build may generate a different world, and nothing short of regenerating can tell, so the
    // key is the build id. Only the two builds used most recently keep their files (the play copy and the dev build share this
    // folder; a read counts as use), and within a build the newest four worlds of each kind: older builds' files can never be
    // read again, and used to pile up (~45 MB a build, 12 Oct).
    public static class WorldCache
    {
        public static bool Enabled=>!Application.isEditor&&!string.IsNullOrEmpty(Application.buildGUID)&&!RaceTrial.Has("--no-world-cache");
        // What changes the generated world, and so every world cache's key (the map image's too). Test, review and timing
        // switches don't, so they share caches. A new flag that changes what's built must be listed here.
        // (Every --perf-* switch counts: they change what's built or how it looks.)
        static readonly string[] WorldFlags={"--race-full-world","--no-corner-barriers"};   // (Prop Forge and the new Bayview by their state, below: both on by default since 20 Oct)
        public static string World{get{
            var s=(QuarryHills.Enabled?"qh":"noqh")+(PropForge.Enabled?"-pf":"")+(NewBayview.On?"-nb":"");foreach(var f in WorldFlags)if(RaceTrial.Has(f))s+=f;
            var perf=new List<string>();foreach(var a in Environment.GetCommandLineArgs())if(a.StartsWith("--perf-"))perf.Add(a);perf.Sort();foreach(var f in perf)s+=f;
            return s;}}
        public static string PathFor(string name,string ext="bin"){if(!Enabled)return null;PruneOnce();return System.IO.Path.Combine(Application.persistentDataPath,"world-cache",$"{name}-{Application.buildGUID}-{World}.{ext}");}
        // Once a launch, before the first cache read: older builds' world and map files go (a build that only ever reads, as
        // a played copy does, would otherwise never prune).
        static bool pruned;
        public static void PruneOnce()
        {
            if(pruned||!Enabled)return;pruned=true;
            foreach(var folder in new[]{"world-cache","map-cache"})try{Prune(System.IO.Path.Combine(Application.persistentDataPath,folder));}catch(Exception e){Debug.LogWarning("LITTLE_ROAD_WORLD_CACHE prune failed: "+e.Message);}
        }

        public static bool TryRead(string name,params float[][] arrays)
        {
            string path=PathFor(name);if(path==null||!File.Exists(path))return false;
            try {
                var bytes=File.ReadAllBytes(path);long need=0;foreach(var a in arrays)need+=a.Length*4L;
                if(bytes.Length!=need)return false;
                int at=0;foreach(var a in arrays){Buffer.BlockCopy(bytes,at,a,0,a.Length*4);at+=a.Length*4;}
                Touch(path);return true;
            } catch(Exception e){Debug.LogWarning($"LITTLE_ROAD_WORLD_CACHE {name} unreadable: {e.Message}");return false;}
        }
        public static void Write(string name,params float[][] arrays)
        {
            string path=PathFor(name);if(path==null)return;
            try {
                long size=0;foreach(var a in arrays)size+=a.Length*4L;var bytes=new byte[size];int at=0;
                foreach(var a in arrays){Buffer.BlockCopy(a,0,bytes,at,a.Length*4);at+=a.Length*4;}
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));File.WriteAllBytes(path,bytes);
                var mine=new List<FileInfo>();foreach(var f in new DirectoryInfo(System.IO.Path.GetDirectoryName(path)).GetFiles(name+"-"+Application.buildGUID+"-*"))mine.Add(f);
                mine.Sort((a,b)=>b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));for(int i=4;i<mine.Count;i++)mine[i].Delete();
                Prune(System.IO.Path.GetDirectoryName(path));
            } catch(Exception e){Debug.LogWarning($"LITTLE_ROAD_WORLD_CACHE {name} not saved: {e.Message}");}
        }
        // Shader warm-ups done (docs/72): a build's first launch on this machine warms every view; the graphics driver keeps
        // what it compiled in its own disk cache, so later launches skip it (drive benchmark, 12 Oct: no warm-up, no frame
        // over 33 ms; warmed, one). One marker file per build and warm-up; --always-warm-up warms every time.
        public static bool Warmed(string what)=>Enabled&&!RaceTrial.Has("--always-warm-up")&&File.Exists(PathFor("warmed-"+what,"txt"));
        public static void MarkWarmed(string what){string path=PathFor("warmed-"+what,"txt");if(path==null)return;try{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));File.WriteAllText(path,DateTime.UtcNow.ToString("o"));}catch(Exception){}}
        // A read marks the file as in use, so the build that reads it keeps its files.
        public static void Touch(string path){try{File.SetLastWriteTimeUtc(path,DateTime.UtcNow);}catch(Exception){}}
        // Delete every file in the folder that doesn't belong to one of the two builds used most recently (by their newest
        // file); files with no build id in the name (an older naming) go too. Returns how many went.
        public static int Prune(string folder,int keepBuilds=2)
        {
            if(!Directory.Exists(folder))return 0;
            var files=new DirectoryInfo(folder).GetFiles();var newest=new Dictionary<string,DateTime>();
            foreach(var f in files){var id=BuildOf(f.Name);if(id!=null&&(!newest.TryGetValue(id,out var t)||f.LastWriteTimeUtc>t))newest[id]=f.LastWriteTimeUtc;}
            var builds=new List<string>(newest.Keys);builds.Sort((a,b)=>newest[b].CompareTo(newest[a]));
            var keep=new HashSet<string>(builds.GetRange(0,Math.Min(keepBuilds,builds.Count)));keep.Add(Application.buildGUID);
            int gone=0;foreach(var f in files){var id=BuildOf(f.Name);if(id==null||!keep.Contains(id)){try{f.Delete();gone++;}catch(Exception){}}}
            if(gone>0)Debug.Log($"LITTLE_ROAD_WORLD_CACHE pruned {gone} files from older builds in {System.IO.Path.GetFileName(folder)}");
            return gone;
        }
        // The 32-hex build id in a cache file's name, or null.
        public static string BuildOf(string fileName)
        {
            foreach(var part in fileName.Split('-','.')) {
                if(part.Length!=32)continue;bool hex=true;foreach(char c in part)if(!Uri.IsHexDigit(c)){hex=false;break;}
                if(hex)return part.ToLowerInvariant();
            }
            return null;
        }
    }
}
