using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Post-build diagnosis only. Never changes the already frozen player inputs.
public static class PaceCandidateReplay
{
    [Serializable] public sealed class Candidate
    {
        public int rank;public bool finite,accepted,curveConflict;public float estimate,clearance,curveConflictAfter;
        public ContextPlanAudit.Candidate check;
    }
    [Serializable] public sealed class Row
    {
        public int id,fallbackRank;public double timestamp;public float route,speed;public bool hasPrevious;
        public List<Candidate> candidates=new();
    }
    [Serializable] public sealed class Report
    {
        public string scope="Replay failed selections with captured bodies, tuning, steering, traffic and corridor bounds. Exact recorded nominal checks required before candidate counterfactuals. Each row states whether the capture includes its previous path; missing paths mean fresh candidates only, not the original full candidate set. No scenery replay; any recorded scenery rejection aborts attribution. Counterfactual acceptance is a short forecast, not a completed manoeuvre.";
        public int recorded,matched;public List<Row> rows=new();
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    public static void Run()
    {
        string input=RaceTrial.Arg("--pace-replay-input"),output=RaceTrial.Arg("--pace-replay-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope")||!RaceTrial.Has("--local-origin-curvature")||!RaceTrial.Has("--traffic-path-check")||!RaceTrial.Has("--prepared-path")||!RaceTrial.Has("--opening-decision-audit"))throw new ArgumentException("Matching control/traffic/audit flags and paths required");
        if(File.Exists(output))throw new IOException("Do not overwrite replay evidence");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        var course=source.track=="bayview"?new TechnicalCircuit(new Circuit()):new TechnicalCircuit();var road=course.track;
        if(source.track!=course.definition.id||source.version!=course.definition.version)throw new Exception("Track mismatch");
        var line=new RacingLine(road,source.reference,course.definition.id,course.definition.version);var report=new Report();
        bool routeLocal=RaceTrial.Has("--route-local-adjacency");
        foreach(var audit in source.failedPlanAudits)foreach(var e in audit.entries) {
            var self=Car(e.self);var traffic=e.traffic.Select(Car).ToArray();var driver=source.driverPlanning.First(x=>x.id==self.id);
            var profile=new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk,true),true);
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,traffic,road,e.tuning,1,0,e.finished,e.steering,true);
            var checker=new RoadPathFeasibility(profile,self.colliderCentre,new Vector3(self.halfWidth,.5f,self.halfLength),null);
            float edge=Circuit.Width*.5f-self.halfWidth-.55f;
            float left=e.left>-edge?e.left:float.NegativeInfinity,right=e.right<edge?e.right:float.PositiveInfinity;
            foreach(var c in e.candidates) {
                if(c.reason=="scenery")throw new Exception("Scenery fixture requires its own scene replay");
                if(c.forecastModel!="nominal")throw new Exception("Unexpected following-model candidate in nominal replay");
                checker.Check(in o,in c.path,left,right);var actual=checker.LastCandidate;report.recorded++;
                bool match=actual.reason==c.reason&&actual.afterSeconds==c.afterSeconds&&actual.roadBound==c.roadBound&&actual.predictedRouteGain==c.predictedRouteGain&&actual.predictedEndSpeed==c.predictedEndSpeed&&actual.position.Equals(c.position)&&actual.velocity.Equals(c.velocity)&&actual.forward.Equals(c.forward);
                if(!match)throw new Exception($"Captured check mismatch id={self.id} time={e.timestamp} reason={c.reason}/{actual.reason}");
                report.matched++;
            }
            var map=new ContextPreferenceMap(profile,false,true,false,true,true,false,routeLocal);
            map.Fill(in o,e.hasPreviousPath,e.previousPath.endOffset,true,true);
            var planner=new DynamicRejoinPlanner(profile,routeLocal,RaceTrial.Has("--dynamic-blocking-target"));
            if(map.RelevantCars>0)planner.PrepareTraffic(in o,in e.previousPath,e.hasPreviousPath);
            else planner.Prepare(in o,in e.previousPath,e.hasPreviousPath);
            string firstReason=e.candidates.Length>0?e.candidates[0].reason:"";
            var row=new Row{id=self.id,timestamp=e.timestamp,route=self.route,speed=self.speed,hasPrevious=e.hasPreviousPath,fallbackRank=planner.FallbackRank(firstReason=="scenery",firstReason=="corridor")};
            for(int rank=0;rank<planner.Count;rank++) {
                var path=planner.Path(rank);bool accepted=checker.Check(in o,in path,left,right);
                row.candidates.Add(new Candidate{rank=rank,finite=float.IsFinite(planner.EstimatedTime(rank)),estimate=float.IsFinite(planner.EstimatedTime(rank))?planner.EstimatedTime(rank):-1,clearance=planner.Clearance(rank),accepted=accepted,check=checker.LastCandidate});
            }
            // All geometric alternatives use one and the same primary physical
            // schedule. Rechecking it is diagnostic work, not live budget use.
            var primary=planner.Path(0);checker.Check(in o,in primary,left,right);
            foreach(var candidate in row.candidates) {
                var path=planner.Path(candidate.rank);
                candidate.curveConflict=checker.CurveTrafficConflict(in o,in path,out float when);candidate.curveConflictAfter=when;
            }
            report.rows.Add(row);
        }
        if(report.recorded==0||report.recorded!=report.matched)throw new Exception("No complete original check replay");
        File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"PACE_CANDIDATE_REPLAY matches={report.matched}/{report.recorded} observations={report.rows.Count}");
    }
}

