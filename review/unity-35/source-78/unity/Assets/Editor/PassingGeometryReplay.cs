using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Diagnostic counterfactuals only. No runtime policy or tuning changes.
public static class PassingGeometryReplay
{
    [Serializable] public sealed class Score
    {
        public string name,nominalReason;
        public float blend,destination,routeGain,endSpeed,bound,endOffset;
        public bool nominalAccepted,trafficRoadContained;
    }
    [Serializable] public sealed class Row
    {
        public double timestamp;
        public float speed,offset;
        public bool originalMatches;
        public Score[] alternatives;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Car0 captured retained trajectories during the first15 seconds after its earliest capture. Exact nominal replay precedes fixed geometry comparisons. Frozen observed rival velocity/fading yaw with the live leader controller; no rival future inputs, emergency controller, scenery, repeated replanning or physics contacts. Nominal corridor/side-entry checks and traffic-constrained road containment are necessary evidence, not live collision certification. Do not choose a release parameter from this small selected sample.";
        public int observations,originalMatches,sourceAuditDropped;
        public Row[] rows;
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static Score Evaluate(string name,in DriverObservation o,ContextPathState path,RoadPathFeasibility check,
        ContextPathPrediction prediction,ObservedTrafficRollout traffic,float left,float right)
    {
        bool feasible=check.Check(in o,in path,left,right);string reason=check.LastCandidate.reason;
        var self=o.self;var half=new Vector3(self.halfWidth,.5f,self.halfLength);
        prediction.Predict(in o,path,self.colliderCentre,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
        var last=prediction.samples[prediction.Count-1];((Circuit)o.route).Nearest(last.state.position,out float offset);
        return new Score{name=name,blend=path.blend,destination=path.selectedOffset,nominalAccepted=feasible,nominalReason=reason,
            trafficRoadContained=prediction.envelope.Contained,bound=prediction.envelope.MaximumBound,endOffset=offset,
            routeGain=Mathf.Repeat(last.route-self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f,
            endSpeed=Vector3.Dot(last.state.velocity,last.state.forward)};
    }
    public static void Run()
    {
        string input=RaceTrial.Arg("--passing-geometry-input"),output=RaceTrial.Arg("--passing-geometry-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope")||!RaceTrial.Has("--prepared-path")||!RaceTrial.Has("--traffic-path-check")||!RaceTrial.Has("--continuation-audit"))
            throw new ArgumentException("Replay requires paths and matching coast, prepared-path, traffic-path-check and continuation-audit settings.");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1"||source.reference!="cedar-smooth-v2")throw new ArgumentException("Matching Cedar geometry required.");
        var captures=source.continuationPlanAudits.SelectMany(a=>a.entries).Where(e=>e.self.id==0&&e.captureReason=="retained-trajectory").OrderBy(e=>e.timestamp).ToArray();
        if(captures.Length==0)throw new InvalidOperationException("No owned car0 trajectory captures.");
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,source.reference,course.definition.id,course.definition.version);
        var driver=source.driverPlanning.Single(d=>d.id==0);
        var rows=new List<Row>();var report=new Report{sourceAuditDropped=source.continuationPlanAudits.Sum(a=>a.dropped)};
        foreach(var e in captures.Where(e=>e.timestamp<=captures[0].timestamp+15)) {
            var self=Car(e.self);var rivals=e.traffic.Select(Car).ToArray();
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,rivals,road,e.tuning,1,0,e.finished,e.steering);
            var profile=new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk));
            var half=new Vector3(self.halfWidth,.5f,self.halfLength);
            var prediction=new ContextPathPrediction(profile);
            var check=new RoadPathFeasibility(profile,self.colliderCentre,half,null);
            float edge=Circuit.Width*.5f-self.halfWidth-.55f;
            float left=e.left<=-edge?float.NegativeInfinity:e.left,right=e.right>=edge?float.PositiveInfinity:e.right;
            bool accepted=check.Check(in o,in e.selectedPath,left,right);
            var actual=check.LastCandidate;
            var expected=e.candidates.Last(c=>c.reason=="accepted"&&c.path.Equals(e.selectedPath));
            bool matches=accepted&&actual.reason==expected.reason&&actual.roadBound==expected.roadBound
                &&actual.predictedRouteGain==expected.predictedRouteGain&&actual.predictedEndSpeed==expected.predictedEndSpeed
                &&actual.position.Equals(expected.position)&&actual.velocity.Equals(expected.velocity)&&actual.forward.Equals(expected.forward);
            var traffic=new ObservedTrafficRollout();traffic.Prepare(in o);
            var scores=new List<Score>();
            scores.Add(Evaluate("captured-retained",in o,e.selectedPath,check,prediction,traffic,left,right));
            foreach(float scale in new[]{.5f,.75f,1f,1.25f}) {
                var path=RoadPathFeasibility.FromMotion(in o,e.selectedPath.selectedOffset,false);path.blend*=scale;
                scores.Add(Evaluate("fresh-"+scale.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),in o,path,check,prediction,traffic,left,right));
            }
            scores.Add(Evaluate("hold-offset",in o,RoadPathFeasibility.FromMotion(in o,self.offset,false),check,prediction,traffic,left,right));
            rows.Add(new Row{timestamp=e.timestamp,speed=self.speed,offset=self.offset,originalMatches=matches,alternatives=scores.ToArray()});
            if(matches)report.originalMatches++;
        }
        report.observations=rows.Count;report.rows=rows.ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"PASSING_GEOMETRY_REPLAY observations={report.observations} reproduced={report.originalMatches}");
        if(report.originalMatches!=report.observations)throw new InvalidOperationException("Original forecast mismatch: reject counterfactual conclusions.");
    }
}
