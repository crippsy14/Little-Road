using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace LittleRoad
{
    // Per-frame cost while playing (--frame-probe <folder>): frame time, draw batches, SetPass calls,
    // triangles, shadow casters and managed allocation, with where the player's car was. Writes
    // frames.csv and a summary (percentiles, the worst frames and what they had in common) on quit.
    public sealed class FrameProbe:MonoBehaviour
    {
        struct Sample{public float t,ms,carsMs,raceMs,cpuMs,gpuMs,planMs,planMaxMs,physicsMs;public int steps;public long batches,setPass,tris,shadows,gc;public Vector3 at;}
        readonly FrameTiming[] timing=new FrameTiming[1];
        public static int Skip; // set by reviews after a screenshot
        readonly List<Sample> samples=new();string folder;Transform follow;
        ProfilerRecorder batches,setPass,tris,shadows,gc;
        public static void Attach(Transform player)
        {
            string folder=RaceTrial.Arg("--frame-probe");if(string.IsNullOrEmpty(folder))return;
            // A bare name (trial scripts only pass plain tokens) goes beside the race report.
            string report=RaceTrial.Arg("--trial-report");if(!Path.IsPathRooted(folder)&&!string.IsNullOrEmpty(report))folder=Path.Combine(Path.GetDirectoryName(report),folder);
            if(RaceTrial.Has("--time-physics"))PhysicsStepTimer.Attach();
            var probe=new GameObject("Frame probe").AddComponent<FrameProbe>();probe.folder=folder;probe.follow=player;DontDestroyOnLoad(probe.gameObject);
        }
        void OnEnable()
        {
            batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");setPass=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            tris=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");shadows=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Shadow Casters Count");
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
        }
        void Update()
        {
            // Uncapped, so the probe measures cost rather than the display's refresh (vSync at 60 Hz pinned
            // every frame to 16.7 ms in db8).
            // --probe-paced: measure with the player's pacing (VSync) instead, to judge smoothness rather than cost.
            if(RaceTrial.Has("--probe-paced")){if(Time.frameCount%60==0){if(RaceTrial.Has("--probe-race-pacing"))GraphicsOptions.ApplyRacePacing();else GraphicsOptions.ApplyPacing();}}
            else if(QualitySettings.vSyncCount!=0||Application.targetFrameRate!=-1){QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;}
            if(Time.frameCount<30)return; // skip start-up frames
            if(Skip>0){Skip--;return;}   // frames spent taking a review screenshot are not play
            FrameTimingManager.CaptureFrameTimings();bool timed=FrameTimingManager.GetLatestTimings(1,timing)>0;
            var (cars,race,steps)=FrameCost.Take();var (plan,planMax,physics)=FrameCost.TakePlan();
            samples.Add(new Sample{physicsMs=physics,planMs=plan,planMaxMs=planMax,carsMs=cars,raceMs=race,steps=steps,cpuMs=timed?(float)timing[0].cpuMainThreadFrameTime:-1,gpuMs=timed?(float)timing[0].gpuFrameTime:-1,t=Time.realtimeSinceStartup,ms=Time.unscaledDeltaTime*1000,batches=batches.LastValue,setPass=setPass.LastValue,tris=tris.LastValue,shadows=shadows.LastValue,gc=gc.LastValue,at=follow!=null?follow.position:Vector3.zero});
        }
        void OnApplicationQuit()=>Write();
        void OnDisable(){batches.Dispose();setPass.Dispose();tris.Dispose();shadows.Dispose();gc.Dispose();}
        public void Write()
        {
            if(samples.Count==0||string.IsNullOrEmpty(folder))return;Directory.CreateDirectory(folder);
            var csv=new StringBuilder("t,ms,physicsms,planms,planmaxms,carsms,racems,steps,cpums,gpums,batches,setpass,tris,shadowcasters,gcbytes,x,y,z\n");
            foreach(var s in samples)csv.Append($"{s.t:0.000},{s.ms:0.00},{s.physicsMs:0.00},{s.planMs:0.00},{s.planMaxMs:0.00},{s.carsMs:0.00},{s.raceMs:0.00},{s.steps},{s.cpuMs:0.00},{s.gpuMs:0.00},{s.batches},{s.setPass},{s.tris},{s.shadows},{s.gc},{s.at.x:0.0},{s.at.y:0.0},{s.at.z:0.0}\n");
            File.WriteAllText(Path.Combine(folder,"frames.csv"),csv.ToString());
            var ms=samples.Select(s=>s.ms).OrderBy(v=>v).ToList();float P(float q)=>ms[Mathf.Clamp((int)(q*ms.Count),0,ms.Count-1)];
            var sb=new StringBuilder();
            sb.AppendLine($"frames={samples.Count} mean={ms.Average():0.00}ms p50={P(.5f):0.00} p95={P(.95f):0.00} p99={P(.99f):0.00} max={ms[^1]:0.00}");
            sb.AppendLine($"over 16.7ms={samples.Count(s=>s.ms>16.7f)} over 33ms={samples.Count(s=>s.ms>33f)}");
            sb.AppendLine($"batches mean={samples.Average(s=>s.batches):0} max={samples.Max(s=>s.batches)}  setpass mean={samples.Average(s=>s.setPass):0} max={samples.Max(s=>s.setPass)}");
            sb.AppendLine($"triangles mean={samples.Average(s=>s.tris)/1e6:0.00}M max={samples.Max(s=>s.tris)/1e6:0.00}M  shadow casters mean={samples.Average(s=>s.shadows):0} max={samples.Max(s=>s.shadows)}");
            sb.AppendLine($"gc bytes/frame mean={samples.Average(s=>s.gc)/1024:0.0}KB max={samples.Max(s=>s.gc)/1024:0.0}KB  frames allocating >64KB={samples.Count(s=>s.gc>65536)}");
            sb.AppendLine("worst frames (ms, batches, setpass, tris, gcKB, position):");
            foreach(var s in samples.OrderByDescending(s=>s.ms).Take(25))sb.AppendLine($"  {s.ms:0.0} b={s.batches} sp={s.setPass} tri={s.tris/1e6:0.00}M gc={s.gc/1024}KB at=({s.at.x:0},{s.at.y:0},{s.at.z:0}) t={s.t:0.0}");
            // Cost by area: 100 m cells along the route, mean ms and batches.
            sb.AppendLine("by 100 m cell (mean ms, p95 ms, mean batches, frames):");
            foreach(var g in samples.GroupBy(s=>(Mathf.FloorToInt(s.at.x/100),Mathf.FloorToInt(s.at.z/100))).OrderByDescending(g=>g.Average(s=>s.ms)).Take(15)) {
                var v=g.Select(s=>s.ms).OrderBy(x=>x).ToList();
                sb.AppendLine($"  cell ({g.Key.Item1*100},{g.Key.Item2*100}) mean={v.Average():0.00} p95={v[Mathf.Min(v.Count-1,(int)(v.Count*.95f))]:0.00} batches={g.Average(s=>s.batches):0} frames={v.Count}");
            }
            File.WriteAllText(Path.Combine(folder,"frame-probe.txt"),sb.ToString());Debug.Log("FRAME_PROBE "+sb.ToString().Split('\n')[0]);
        }
    }
}
