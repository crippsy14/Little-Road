using System.Diagnostics;
using UnityEngine;

namespace LittleRoad
{
    // Optional main-thread candidate ranking, separate from feasibility/reuse.
    // Both candidates see the same owned rival rollout and live following rule.
    public sealed class TrafficProgressComparison
    {
        readonly ContextPathPrediction prediction;
        readonly ObservedTrafficRollout traffic=new();
        public int Calls {get;private set;}
        public double TotalMs {get;private set;}
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
            float original=Progress(in o,in proposal,out _);
            float alternative=Progress(in o,in continuation,out bool contained);
            TotalMs+=(Stopwatch.GetTimestamp()-started)*1000.0/Stopwatch.Frequency;
            return contained&&alternative>original+o.self.halfLength*2;
        }
    }
}
