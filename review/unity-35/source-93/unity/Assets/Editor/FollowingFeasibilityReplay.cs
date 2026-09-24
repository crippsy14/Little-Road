using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Counterfactual diagnostic. No live selection or forecast configuration changes.
public static class FollowingFeasibilityReplay
{
    [Serializable] public sealed class Outcome
    {
        public bool accepted;public string reason;
        public float bound,gain,endSpeed;
    }
    [Serializable] public sealed class Row
    {
        public double timestamp;public int candidate;
        public bool originalMatches;public ContextPathState path;
        public Outcome nominal,following;
    }
    [Serializable] public sealed class Report
    {
        public string scope="All candidates from captured unresolved opening decisions. Exact recorded nominal reason, state, road bound, gain and end speed must reproduce. Alternative adds shared moving-leader control with owned constant-velocity/fading-yaw opponents. Same road/corridor/side-entry checks; no scenery, emergency controller, repeated replanning, opponent future inputs or contact impulses. Following acceptance is a hypothesis, not live safety or pace proof.";
        public int decisions,candidates,originalMatches,followingAccepted,decisionsWithAlternative,dropped;
        public Row[] rows;
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static Outcome Evaluate(in DriverObservation o,ContextPathState path,ContextPathPrediction prediction,
        TrafficPathFeasibility traffic,float left,float right,ObservedTrafficRollout following=null)
    {
        var half=new Vector3(o.self.halfWidth,.5f,o.self.halfLength);
        prediction.Predict(in o,path,o.self.colliderCentre,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:following);
        var last=prediction.samples[prediction.Count-1];
        var result=new Outcome{bound=prediction.envelope.MaximumBound,
            gain=Mathf.Repeat(last.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f,
            endSpeed=Vector3.Dot(last.state.velocity,last.state.forward)};
        if(!prediction.envelope.Contained){result.reason="road";return result;}
        if(traffic.Conflict(in o,prediction,o.self.colliderCentre,half,out int other,out _)){result.reason="moving side entry: "+other;return result;}
        float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
        if(left<=-edge)left=float.NegativeInfinity;if(right>=edge)right=float.PositiveInfinity;
        var road=(Circuit)o.route;
        for(int i=0;i<prediction.Count;i+=6) {
            road.Nearest(prediction.samples[i].state.position,out float offset);
            if(offset<left||offset>right){result.reason="corridor";return result;}
        }
        result.accepted=true;result.reason="accepted excluding static scenery";return result;
    }
    public static void Run()
    {
        string input=RaceTrial.Arg("--following-feasibility-input"),output=RaceTrial.Arg("--following-feasibility-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope"))throw new ArgumentException("Replay requires input/output and matching coast-within-envelope.");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1"||source.reference!="cedar-smooth-v2")throw new ArgumentException("Matching Cedar layout and line required.");
        var entries=source.openingPlanAudits.SelectMany(b=>b.entries).Where(e=>e.captureReason=="opening-unresolved").OrderBy(e=>e.timestamp).ToArray();
        if(entries.Length==0)throw new InvalidOperationException("No unresolved opening observations.");
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,source.reference,course.definition.id,course.definition.version);
        var rows=new List<Row>();var report=new Report{decisions=entries.Length,dropped=source.openingPlanAudits.Sum(b=>b.dropped)};
        foreach(var e in entries) {
            var self=Car(e.self);var others=e.traffic.Select(Car).ToArray();
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,others,road,e.tuning,1,0,e.finished,e.steering);
            var driver=source.driverPlanning.Single(d=>d.id==self.id);
            var prediction=new ContextPathPrediction(new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk)));
            var traffic=new TrafficPathFeasibility();var following=new ObservedTrafficRollout();following.Prepare(in o);
            bool any=false;
            for(int i=0;i<e.candidates.Length;i++) {
                var c=e.candidates[i];
                if(c.forecastModel!=null&&c.forecastModel!="nominal")throw new InvalidOperationException("This original-rejection replay requires nominal captured candidates.");
                if(c.reason=="scenery")throw new InvalidOperationException("Static scenery is not present in this replay.");
                var nominal=Evaluate(in o,c.path,prediction,traffic,e.left,e.right);
                var sample=prediction.samples[Mathf.Clamp(Mathf.RoundToInt(c.afterSeconds/e.stepSeconds),0,prediction.Count-1)];
                bool matches=nominal.reason==c.reason&&nominal.bound==c.roadBound&&nominal.gain==c.predictedRouteGain&&nominal.endSpeed==c.predictedEndSpeed
                    &&sample.state.position.Equals(c.position)&&sample.state.velocity.Equals(c.velocity)&&sample.state.forward.Equals(c.forward);
                var alternative=Evaluate(in o,c.path,prediction,traffic,e.left,e.right,following);
                rows.Add(new Row{timestamp=e.timestamp,candidate=i,originalMatches=matches,path=c.path,nominal=nominal,following=alternative});
                if(matches)report.originalMatches++;if(alternative.accepted){report.followingAccepted++;any=true;}
            }
            if(any)report.decisionsWithAlternative++;
        }
        report.candidates=rows.Count;report.rows=rows.ToArray();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"FOLLOWING_FEASIBILITY_REPLAY decisions={report.decisions} candidates={report.candidates} reproduced={report.originalMatches} alternatives={report.followingAccepted} decisionsWithAlternative={report.decisionsWithAlternative}");
        if(report.originalMatches!=report.candidates)throw new InvalidOperationException("Recorded nominal rejection mismatch; reject alternative conclusions.");
    }
}
