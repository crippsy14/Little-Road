using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

public static class TrafficCoastReplay
{
    [Serializable] public sealed class Outcome {public string reason;public bool feasible;public float bound,routeGain,endSpeed;}
    [Serializable] public sealed class Row {public int id;public double timestamp;public string recorded;public bool originalReasonMatches,recordedStateMatches;public Outcome original,coast;}
    [Serializable] public sealed class Report
    {
        public string scope="Exact owned pre-controller observations, original final candidate versus coast-capped candidate. Flat Cedar body footprint uses recorded x/z dimensions; height is immaterial to the flat-road envelope. Static scenery is not replayed. Nominal predicted feasibility is not live race acceptance.";
        public int captured,dropped,trafficExhaustions,originalMatches,originalStateMatches,coastFeasible;public Row[] rows;
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static Outcome Evaluate(in DriverObservation o,in ContextPathState path,ContextPathPrediction prediction,TrafficPathFeasibility traffic,float left,float right)
    {
        var half=new Vector3(o.self.halfWidth,.5f,o.self.halfLength);
        prediction.Predict(in o,path,o.self.colliderCentre,half,2,controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true);
        var last=prediction.samples[prediction.Count-1];
        var result=new Outcome{bound=prediction.envelope.MaximumBound,routeGain=Mathf.Repeat(last.route-o.self.route+o.route.Length*.5f,o.route.Length)-o.route.Length*.5f,endSpeed=Vector3.Dot(last.state.velocity,last.state.forward)};
        if(!prediction.envelope.Contained){result.reason="road";return result;}
        if(traffic.Conflict(in o,prediction,o.self.colliderCentre,half,out int other,out _)){result.reason="moving side entry: "+other;return result;}
        var road=(Circuit)o.route;float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
        if(left<=-edge)left=float.NegativeInfinity;if(right>=edge)right=float.PositiveInfinity;
        for(int i=0;i<prediction.Count;i+=6){road.Nearest(prediction.samples[i].state.position,out float offset);if(offset<left||offset>right){result.reason="corridor";return result;}}
        result.reason="clear excluding static scenery";result.feasible=true;return result;
    }
    public static void Run()
    {
        string input=RaceTrial.Arg("--traffic-coast-input"),output=RaceTrial.Arg("--traffic-coast-output");
        if(input==null||output==null)throw new ArgumentException("Traffic coast replay requires input/output paths");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1")throw new ArgumentException("Replay requires matching flat Cedar version");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var report=new Report();var rows=new List<Row>();
        foreach(var audit in source.failedPlanAudits) {
            report.dropped+=audit.dropped;report.captured+=audit.entries.Length;
            ContextPathPrediction prediction=null;var traffic=new TrafficPathFeasibility();
            foreach(var entry in audit.entries) {
                if(!entry.candidates.Any(c=>c.reason.StartsWith("moving side entry")))continue;
                report.trafficExhaustions++;
                var self=Car(entry.self);var others=entry.traffic.Select(Car).ToArray();
                var o=new DriverObservation(entry.timestamp,entry.stepSeconds,self,others,road,entry.tuning,1,0,entry.finished,entry.steering);
                prediction??=new ContextPathPrediction(new RacingSpeedProfile(line,entry.tuning,new DriverPlanningSettings(.75f,5)));
                var candidate=entry.candidates[entry.candidates.Length-1];var coastPath=candidate.path;coastPath.coast=true;
                var normal=Evaluate(in o,in candidate.path,prediction,traffic,entry.left,entry.right);
                var sample=prediction.samples[Mathf.Clamp(Mathf.RoundToInt(candidate.afterSeconds/entry.stepSeconds),0,prediction.Count-1)];
                bool stateMatches=sample.state.position.Equals(candidate.position)&&sample.state.velocity.Equals(candidate.velocity)&&sample.state.forward.Equals(candidate.forward)&&normal.bound==candidate.roadBound;
                var coasting=Evaluate(in o,in coastPath,prediction,traffic,entry.left,entry.right);
                bool matches=normal.reason==candidate.reason;
                if(matches)report.originalMatches++;if(stateMatches)report.originalStateMatches++;if(coasting.feasible)report.coastFeasible++;
                rows.Add(new Row{id=self.id,timestamp=entry.timestamp,recorded=candidate.reason,originalReasonMatches=matches,recordedStateMatches=stateMatches,original=normal,coast=coasting});
            }
        }
        report.rows=rows.ToArray();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"TRAFFIC_COAST_REPLAY captured={report.captured} traffic={report.trafficExhaustions} reproduced={report.originalMatches} stateMatches={report.originalStateMatches} coastFeasible={report.coastFeasible}");
        if(report.originalMatches!=report.trafficExhaustions||report.originalStateMatches!=report.trafficExhaustions)throw new InvalidOperationException("Original failure replay mismatch; no counterfactual acceptance");
    }
}
