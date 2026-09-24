using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

public static class TargetSwitchReplay
{
    [Serializable] public sealed class Alternative
    {
        public string name,reason;public bool feasible;public ContextPathState path;
        public float routeGain,endSpeed,bound,gainOverSelected;
    }
    [Serializable] public sealed class Row
    {
        public int id;public double timestamp;public float route,offset,previousTarget,selectedTarget;
        public bool originalMatches;public int attemptedCandidates;public Alternative selected;public Alternative[] alternatives;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Exact accepted large-target-switch observations. Replays the existing physical road, corridor and moving-side-entry checks; static scenery is excluded. Neither nominal route gain nor this counterfactual establishes live pace, contact blame, passing intent or future opponent inputs. Previous-path alternatives are evaluated, not forced into the race.";
        public int captured,dropped,originalMatches,continuationFeasible,continuationGainOverBodyLength;public Row[] rows;
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static Alternative Evaluate(string name,in DriverObservation o,in ContextPathState path,RoadPathFeasibility check,float left,float right,float selectedGain=0)
    {
        float edge=Circuit.Width*.5f-o.self.halfWidth-.55f;
        if(left<=-edge)left=float.NegativeInfinity;if(right>=edge)right=float.PositiveInfinity;
        bool feasible=check.Check(in o,in path,left,right);var result=check.LastCandidate;
        return new Alternative{name=name,path=path,feasible=feasible,reason=result.reason,routeGain=result.predictedRouteGain,
            endSpeed=result.predictedEndSpeed,bound=result.roadBound,gainOverSelected=result.predictedRouteGain-selectedGain};
    }
    public static void Run()
    {
        string input=RaceTrial.Arg("--target-switch-input"),output=RaceTrial.Arg("--target-switch-output");
        if(input==null||output==null||!RaceTrial.Has("--context-switch-audit")||!RaceTrial.Has("--prepared-path")||!RaceTrial.Has("--traffic-path-check")||!RaceTrial.Has("--coast-within-envelope"))
            throw new ArgumentException("Replay requires input/output and matching switch-audit, prepared-path, traffic-path-check, coast-within-envelope flags.");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1"||source.reference!="cedar-smooth-v2")throw new ArgumentException("Replay requires the matching Cedar layout and line.");
        var course=new TechnicalCircuit();var road=course.track;var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);
        var report=new Report();var rows=new List<Row>();
        foreach(var audit in source.switchPlanAudits) {
            report.dropped+=audit.dropped;
            foreach(var entry in audit.entries) {
                report.captured++;
                if(!entry.selectionSwitch||!entry.hasPreviousPath)throw new InvalidOperationException("Missing accepted switch state.");
                var driver=source.driverPlanning.Single(d=>d.id==entry.self.id);
                var planning=new DriverPlanningSettings(driver.localBrakeFraction,driver.risk);
                var profile=new RacingSpeedProfile(line,entry.tuning,planning);
                var self=Car(entry.self);var others=entry.traffic.Select(Car).ToArray();
                var o=new DriverObservation(entry.timestamp,entry.stepSeconds,self,others,road,entry.tuning,1,0,entry.finished,entry.steering);
                var check=new RoadPathFeasibility(profile,self.colliderCentre,new Vector3(self.halfWidth,.5f,self.halfLength),null);
                var selected=Evaluate("selected",in o,in entry.selectedPath,check,entry.left,entry.right);
                var recorded=entry.candidates.Last(c=>c.reason=="accepted"&&c.path.Equals(entry.selectedPath));var actual=check.LastCandidate;
                bool matches=selected.feasible&&recorded.reason=="accepted"&&actual.roadBound==recorded.roadBound&&actual.predictedRouteGain==recorded.predictedRouteGain&&actual.predictedEndSpeed==recorded.predictedEndSpeed
                    &&actual.position.Equals(recorded.position)&&actual.velocity.Equals(recorded.velocity)&&actual.forward.Equals(recorded.forward);
                if(matches)report.originalMatches++;
                var retained=entry.previousPath;
                var fresh=RoadPathFeasibility.FromMotion(in o,retained.selectedOffset,retained.rejoin);
                var coastRetained=retained;coastRetained.coast=true;var coastFresh=fresh;coastFresh.coast=true;
                var alternatives=new[]{Evaluate("retained",in o,in retained,check,entry.left,entry.right,selected.routeGain),
                    Evaluate("fresh previous target",in o,in fresh,check,entry.left,entry.right,selected.routeGain),
                    Evaluate("coast retained",in o,in coastRetained,check,entry.left,entry.right,selected.routeGain),
                    Evaluate("coast fresh previous target",in o,in coastFresh,check,entry.left,entry.right,selected.routeGain)};
                if(alternatives.Any(a=>a.feasible))report.continuationFeasible++;
                if(alternatives.Any(a=>a.feasible&&a.gainOverSelected>self.halfLength*2))report.continuationGainOverBodyLength++;
                rows.Add(new Row{id=self.id,timestamp=entry.timestamp,route=self.route,offset=self.offset,previousTarget=retained.selectedOffset,selectedTarget=entry.selectedPath.selectedOffset,
                    originalMatches=matches,attemptedCandidates=entry.candidates.Length,selected=selected,alternatives=alternatives});
            }
        }
        report.rows=rows.ToArray();Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"TARGET_SWITCH_REPLAY captured={report.captured} reproduced={report.originalMatches} continuationFeasible={report.continuationFeasible} gainOverBodyLength={report.continuationGainOverBodyLength}");
        if(report.originalMatches!=report.captured)throw new InvalidOperationException("Selected forecast replay mismatch; no counterfactual acceptance.");
    }
}
