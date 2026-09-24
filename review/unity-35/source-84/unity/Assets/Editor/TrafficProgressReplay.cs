using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

public static class TrafficProgressReplay
{
    [Serializable] public sealed class Score {public bool roadContained;public float routeGain,endSpeed,bound;}
    [Serializable] public sealed class Row
    {
        public int id;public double timestamp;public float route,speed,proposalTarget,continuationTarget;
        public bool nominalMatches,trafficKeepsContinuation;
        public Score nominalProposal,nominalContinuation,trafficProposal,trafficContinuation;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Captured chosen continuation overrides. Original nominal forecasts are verified before counterfactual scoring. Traffic scores use the live following controller with fixed observed rival velocity and fading observed yaw. No future rival inputs, repeated replanning, scenery or emergency controller is replayed. Scores are a comparison hypothesis, not guaranteed live progress or collision/road certification.";
        public int captured,dropped,nominalMatches,trafficKeepsContinuation;public Row[] rows;
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static Score Evaluate(in DriverObservation o,in ContextPathState path,ContextPathPrediction prediction,ObservedTrafficRollout traffic=null)
    {
        prediction.Predict(in o,path,o.self.colliderCentre,new Vector3(o.self.halfWidth,.5f,o.self.halfLength),2,
            controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true,traffic:traffic);
        var last=prediction.samples[prediction.Count-1];
        return new Score{roadContained=prediction.envelope.Contained,bound=prediction.envelope.MaximumBound,
            routeGain=Mathf.Repeat(last.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f,
            endSpeed=Vector3.Dot(last.state.velocity,last.state.forward)};
    }
    static bool Matches(Score score,ContextPlanAudit.Candidate candidate)=>score.bound==candidate.roadBound&&score.routeGain==candidate.predictedRouteGain&&score.endSpeed==candidate.predictedEndSpeed;
    public static void Run()
    {
        string input=RaceTrial.Arg("--traffic-progress-input"),output=RaceTrial.Arg("--traffic-progress-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope"))throw new ArgumentException("Replay requires input/output and matching coast-within-envelope flag.");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1"||source.reference!="cedar-smooth-v2")throw new ArgumentException("Replay requires matching Cedar geometry/line.");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var report=new Report();var rows=new List<Row>();var traffic=new ObservedTrafficRollout();
        foreach(var audit in source.continuationPlanAudits) {
            report.dropped+=audit.dropped;
            foreach(var e in audit.entries) {
                if(e.captureReason!="chosen-continuation"||!e.hasPreviousPath)throw new InvalidOperationException("Missing continuation comparison state.");
                report.captured++;
                var driver=source.driverPlanning.Single(d=>d.id==e.self.id);
                var prediction=new ContextPathPrediction(new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk)));
                var self=Car(e.self);var rivals=e.traffic.Select(Car).ToArray();
                var o=new DriverObservation(e.timestamp,e.stepSeconds,self,rivals,road,e.tuning,1,0,e.finished,e.steering);
                var proposal=Evaluate(in o,in e.proposalPath,prediction);var continuation=Evaluate(in o,in e.selectedPath,prediction);
                var recordedProposal=e.candidates.Last(c=>c.reason=="accepted"&&c.path.Equals(e.proposalPath));
                var recordedContinuation=e.candidates.Last(c=>c.reason=="accepted"&&c.path.Equals(e.selectedPath));
                bool matches=Matches(proposal,recordedProposal)&&Matches(continuation,recordedContinuation);
                if(matches)report.nominalMatches++;
                traffic.Prepare(in o);
                var trafficProposal=Evaluate(in o,in e.proposalPath,prediction,traffic);
                var trafficContinuation=Evaluate(in o,in e.selectedPath,prediction,traffic);
                bool keep=trafficContinuation.roadContained&&trafficContinuation.routeGain>trafficProposal.routeGain+self.halfLength*2;
                if(keep)report.trafficKeepsContinuation++;
                rows.Add(new Row{id=self.id,timestamp=e.timestamp,route=self.route,speed=self.speed,proposalTarget=e.proposalPath.selectedOffset,continuationTarget=e.selectedPath.selectedOffset,
                    nominalMatches=matches,trafficKeepsContinuation=keep,nominalProposal=proposal,nominalContinuation=continuation,trafficProposal=trafficProposal,trafficContinuation=trafficContinuation});
            }
        }
        report.rows=rows.ToArray();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"TRAFFIC_PROGRESS_REPLAY captured={report.captured} reproduced={report.nominalMatches} trafficKeeps={report.trafficKeepsContinuation}");
        if(report.nominalMatches!=report.captured)throw new InvalidOperationException("Original nominal replay mismatch.");
    }
}
