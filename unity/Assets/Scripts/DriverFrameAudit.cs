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
            public int frame,decisionCalls,snapshotCalls,width,height;
            public float simTime,realTime,frameMs;
            public double decisionMs,snapshotMs;
            public long decisionBytes,snapshotBytes;
        }
        [Serializable] public sealed class Evidence
        {
            public string scope="All RoadCar.Plan calls plus shared snapshot on the main thread, summed per rendered frame. Includes recovery/sensing/follower/guard; excludes physics simulation, rendering, diagnostics and direct fixture commands. Allocation counters cover these same regions. First captured frame may include setup/loading; retained explicitly. Hidden/accelerated runs cannot establish real-time performance.";
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
