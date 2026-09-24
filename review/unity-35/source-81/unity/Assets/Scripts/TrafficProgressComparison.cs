using System.Diagnostics;
using UnityEngine;

namespace LittleRoad
{
    // Optional main-thread candidate ranking, separate from feasibility/reuse.
    // Both candidates see the same owned rival rollout and live following rule.
    public sealed class TrafficProgressComparison
    {
        readonly ContextPathPrediction prediction;
        readonly ObservedTrafficRollout traffic=new(RaceTrial.Has("--shared-observed-traffic"));
        public int SharedHits=>traffic.SharedHits;
        public int SharedMisses=>traffic.SharedMisses;
        public int SharedProjectedCars=>traffic.SharedProjectedCars;
        public int Calls {get;private set;}
        public double TotalMs {get;private set;}
        public double RivalPreparationMs {get;private set;}
        public double ProposalMs {get;private set;}
        public double ContinuationMs {get;private set;}
        public double MaximumMs {get;private set;}
        public TrafficProgressComparison(RacingSpeedProfile profile){prediction=new ContextPathPrediction(profile);}
        float Progress(in DriverObservation o,in ContextPathState path,out bool contained)
        {
            prediction.Predict(in o,path,o.self.colliderCentre,new Vector3(o.self.halfWidth,.5f,o.self.halfLength),2,
                controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
            contained=prediction.envelope.Contained;
            var last=prediction.samples[prediction.Count-1];
            return Mathf.Repeat(last.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f;
        }
        public bool PreferContinuation(in DriverObservation o,in ContextPathState proposal,in ContextPathState continuation)
        {
            long started=Stopwatch.GetTimestamp();Calls++;
            traffic.Prepare(in o);
            long prepared=Stopwatch.GetTimestamp();
            float original=Progress(in o,in proposal,out _);
            long proposed=Stopwatch.GetTimestamp();
            float alternative=Progress(in o,in continuation,out bool contained);
            long finished=Stopwatch.GetTimestamp();
            double scale=1000.0/Stopwatch.Frequency,elapsed=(finished-started)*scale;
            RivalPreparationMs+=(prepared-started)*scale;
            ProposalMs+=(proposed-prepared)*scale;
            ContinuationMs+=(finished-proposed)*scale;
            TotalMs+=elapsed;MaximumMs=System.Math.Max(MaximumMs,elapsed);
            return contained&&alternative>original+o.self.halfLength*2;
        }
    }
}
