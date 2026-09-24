using System;
using System.IO;
using UnityEngine;

namespace LittleRoad
{
    // Observation only, pooled during play. Includes all cars' Plan work
    // (recovery, sensing, policy, guard) and the common snapshot separately.
    public sealed class DriverFrameAudit
    {
        public readonly struct Stamp
        {
            public readonly long ticks,bytes;
            public Stamp(long ticks,long bytes){this.ticks=ticks;this.bytes=bytes;}
        }
        [Serializable] public struct Frame
        {
            public int frame,decisionCalls,snapshotCalls,width,height,forecastBatches,forecastJobs;
            public int completedForecastJobs,rankingJobs;
            public float simTime,realTime,frameMs;
            public double decisionMs,snapshotMs,forecastBatchMs,forecastWorkerMs;
            public long decisionBytes,snapshotBytes,forecastWorkerBytes;
        }
        [Serializable] public sealed class Evidence
        {
            public string scope="All RoadCar.Plan calls plus shared snapshot on the main thread, summed per rendered frame. Includes recovery/sensing/follower/guard; excludes physics simulation, rendering, diagnostics and direct fixture commands. Snapshot cost includes forecast preparation, dispatch and joined wait. Forecast worker elapsed/allocations are reported separately; worker elapsed is not CPU utilization and must not be added to joined wall time. Allocation counters cover these same regions plus separately identified workers. First captured frame may include setup/loading; retained explicitly. Hidden/accelerated runs cannot establish real-time performance.";
            public string cpu,gpu;
            public int width,height,frameCap,vsync,droppedFrames;
            public float playbackRate;
            public bool batchMode,developmentBuild;
            public Frame[] frames;
        }
        readonly Frame[] frames=new Frame[65536];
        int count,dropped;Frame current;
        public static Stamp Begin()=>new(System.Diagnostics.Stopwatch.GetTimestamp(),GC.GetAllocatedBytesForCurrentThread());
        public void Record(Stamp start,bool snapshot)
        {
            double ms=(System.Diagnostics.Stopwatch.GetTimestamp()-start.ticks)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            long bytes=GC.GetAllocatedBytesForCurrentThread()-start.bytes;
            if(snapshot){current.snapshotCalls++;current.snapshotMs+=ms;current.snapshotBytes+=bytes;}
            else {current.decisionCalls++;current.decisionMs+=ms;current.decisionBytes+=bytes;}
        }
        public void RecordForecastBatch(ForecastWorkerPool pool,ForecastWork[] jobs,int count)
        {
            current.forecastBatches++;current.forecastJobs+=count;current.forecastBatchMs+=pool.ElapsedMs;
            for(int i=0;i<count;i++){current.forecastWorkerMs+=jobs[i].ElapsedMs;current.forecastWorkerBytes+=jobs[i].AllocatedBytes;if(jobs[i].Completed)current.completedForecastJobs++;if(jobs[i].DynamicPreparation!=null)current.rankingJobs++;}
        }
        public void EndFrame(float elapsed)
        {
            current.width=Screen.width;current.height=Screen.height;
            current.frame=Time.frameCount;current.simTime=elapsed;current.realTime=Time.realtimeSinceStartup;current.frameMs=Time.unscaledDeltaTime*1000;
            if(count<frames.Length)frames[count++]=current;else dropped++;
            current=default;
        }
        public void Save(string report)
        {
            var copy=new Frame[count];Array.Copy(frames,copy,count);
            var evidence=new Evidence{cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,width=Screen.width,height=Screen.height,
                frameCap=Application.targetFrameRate,vsync=QualitySettings.vSyncCount,playbackRate=Time.timeScale,batchMode=Application.isBatchMode,
                developmentBuild=Debug.isDebugBuild,droppedFrames=dropped,frames=copy};
            File.WriteAllText(Path.ChangeExtension(report,"driver-frames.json"),JsonUtility.ToJson(evidence,true));
        }
    }
}
