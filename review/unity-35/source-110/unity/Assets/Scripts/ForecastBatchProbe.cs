using System;
using System.Diagnostics;
using UnityEngine;

namespace LittleRoad
{
    // Diagnostic only: compare 24 independent prospective paths from a declared
    // physical fixture state. No result from this batch controls the real car.
    public static class ForecastBatchProbe
    {
        [Serializable] public sealed class Evidence
        {
            public string scope="Pure 24-request forecast batch. Same physical observation with varied proposed offsets. Serial and concurrent results must agree exactly. Timings exclude output comparison, include preparation and dispatch/join; not live racing or rendered-frame acceptance.";
            public int jobs=24,workers,rounds=4;
            public bool exactParity=true;
            public double initializationMs;
            public double[] serialMs,concurrentMs,workerElapsedMs;
            public long[] serialBytes,mainBytes,workerBytes;
        }
        static long Stamp()=>Stopwatch.GetTimestamp();
        static double Ms(long started)=>(Stamp()-started)*1000.0/Stopwatch.Frequency;
        static bool Equal(ContextPathPrediction a,ContextPathPrediction b)
        {
            if(a.Count!=b.Count||a.PreparedHits!=b.PreparedHits||a.PreparedMisses!=b.PreparedMisses||
                a.envelope.Queries!=b.envelope.Queries||a.envelope.MaximumBound!=b.envelope.MaximumBound||a.envelope.FirstUncertainTime!=b.envelope.FirstUncertainTime)return false;
            for(int i=0;i<a.Count;i++)if(!a.samples[i].Equals(b.samples[i]))return false;
            for(int i=0;i<a.Count-1;i++)if(a.envelope.intervalUpperBounds[i]!=b.envelope.intervalUpperBounds[i])return false;
            return true;
        }
        public static Evidence Run(in DriverObservation o,in ContextPathState path,RacingSpeedProfile profile,Vector3 centre,Vector3 half,int controlSteps,bool prepared,int workers)
        {
            long initialization=Stamp();var evidence=new Evidence{workers=workers};
            var requests=new ForecastRequest[evidence.jobs];var serial=new ContextPathPrediction[evidence.jobs];var work=new ForecastWork[evidence.jobs];
            for(int i=0;i<work.Length;i++) {
                var proposed=path;proposed.endOffset+=((i%7)-3)*.15f;proposed.selectedOffset=proposed.endOffset;
                requests[i]=new ForecastRequest(in o,in proposed,centre,half,controlSteps:controlSteps,allowance:0,prepared:prepared,planning:profile.planning);
                serial[i]=new ContextPathPrediction(profile);work[i]=new ForecastWork(new ContextPathPrediction(profile));work[i].Set(in requests[i]);
            }
            evidence.serialMs=new double[evidence.rounds];evidence.concurrentMs=new double[evidence.rounds];evidence.workerElapsedMs=new double[evidence.rounds];
            evidence.serialBytes=new long[evidence.rounds];evidence.mainBytes=new long[evidence.rounds];evidence.workerBytes=new long[evidence.rounds];
            using(var pool=new ForecastWorkerPool(workers)) {
                evidence.initializationMs=Ms(initialization);
                for(int round=0;round<evidence.rounds;round++) {
                    // Alternate execution order to limit systematic warm-up bias.
                    for(int pass=0;pass<2;pass++) {
                        if((pass+round)%2==0) {
                            long started=Stamp(),bytes=GC.GetAllocatedBytesForCurrentThread();
                            for(int i=0;i<requests.Length;i++)requests[i].Run(serial[i]);
                            evidence.serialMs[round]=Ms(started);evidence.serialBytes[round]=GC.GetAllocatedBytesForCurrentThread()-bytes;
                        }else {
                            pool.Run(work,work.Length);evidence.concurrentMs[round]=pool.ElapsedMs;evidence.mainBytes[round]=pool.MainAllocatedBytes;
                            foreach(var job in work){evidence.workerElapsedMs[round]+=job.ElapsedMs;evidence.workerBytes[round]+=job.AllocatedBytes;}
                        }
                    }
                    for(int i=0;i<work.Length;i++)evidence.exactParity&=work[i].Completed&&work[i].Error==null&&Equal(serial[i],work[i].prediction);
                }
            }
            return evidence;
        }
    }
}
