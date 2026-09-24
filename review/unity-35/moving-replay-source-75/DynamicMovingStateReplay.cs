using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

public static class DynamicMovingStateReplay
{
    [Serializable] public sealed class Candidate
    {
        public int rank;public ContextPathState path;public float estimatedTime,roadBound,firstUncertainTime;
        public bool geometryValid,shortForecastContained,gateFinished,gateContained;
        public float gateSeconds,gateSpeed,maximumBodyOffset;
    }
    [Serializable] public sealed class Row
    {
        public double timestamp;public float route,speed,horizon;
        public List<Candidate> candidates=new List<Candidate>();
    }
    [Serializable] public sealed class Report
    {
        public string scope="Exact captured candidate forecast replay, followed by every current candidate at seven failed opening observations. Full-gate executions hold one curve with the live follower in the flat-road model; no repeated planning, scenery or Rigidbody contacts. Actual captured collider, tuning, steering and yaw are retained.";
        public int recordedCandidates,exactMatches;public List<Row> rows=new List<Row>();
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static void Forecast(in DriverObservation o,ContextPathState path,ContextPathPrediction prediction)
    {
        prediction.Predict(in o,path,o.self.colliderCentre,new Vector3(o.self.halfWidth,.5f,o.self.halfLength),2,
            controlSteps:3,adaptiveEnvelope:true,roadAllowance:.1f,preparedPath:true);
    }
    static void ExecuteGate(in DriverObservation initial,ContextPathState path,float horizon,RacingSpeedProfile profile,Candidate result)
    {
        var road=(Circuit)initial.route;var self=initial.self;
        var model=new PlanarDrivePrediction(self.position,self.velocity,self.forward,self.yawRate,initial.ownSteeringState);
        var follower=new ContextPathFollower(profile,false);var half=new Vector3(self.halfWidth,.5f,self.halfLength);
        float travelled=0,last=self.route,dt=initial.stepSeconds;
        for(int step=0;step*dt<30;step++) {
            float route=road.Nearest(model.position,out float offset);
            float gain=Mathf.Repeat(route-last+road.Length*.5f,road.Length)-road.Length*.5f;
            travelled+=gain;path.consumed+=Mathf.Max(0,gain);last=route;
            float speed=Vector3.Dot(model.velocity,model.forward);
            result.maximumBodyOffset=Mathf.Max(result.maximumBodyOffset,model.MaxBodyOffset(road,self.colliderCentre,half));
            if(travelled>=horizon){result.gateFinished=true;result.gateSeconds=step*dt;result.gateSpeed=speed;break;}
            var observed=new ObservedCar(self.id,model.position,model.velocity,model.forward,Vector3.Cross(Vector3.up,model.forward),route,offset,speed,self.halfWidth,self.halfLength,model.yawRate,self.colliderCentre);
            var o=new DriverObservation(initial.timestamp+step*dt,dt,observed,Array.Empty<ObservedCar>(),road,initial.tuning,1,0,false,model.steering);
            var command=follower.Follow(in o,in path);model.Step(initial.tuning,command.pedal,command.steering,dt);
        }
        result.gateContained=result.gateFinished&&result.maximumBodyOffset<=Circuit.Width*.5f-.1f;
    }
    public static void Run()
    {
        string input=RaceTrial.Arg("--dynamic-replay-input"),output=RaceTrial.Arg("--dynamic-replay-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope"))throw new ArgumentException("Matching replay paths and coasting flag required");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="cedar-technical"||source.version!="cedar-v1")throw new ArgumentException("Replay track mismatch");
        var course=new TechnicalCircuit();var road=course.track;
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);var report=new Report();
        foreach(var audit in source.failedPlanAudits)foreach(var e in audit.entries) {
            var self=Car(e.self);var traffic=e.traffic.Select(Car).ToArray();var driver=source.driverPlanning.First(x=>x.id==self.id);
            var profile=new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk));
            var prediction=new ContextPathPrediction(profile,false);
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,traffic,road,e.tuning,1,0,e.finished,e.steering,true);
            foreach(var c in e.candidates) {
                Forecast(in o,c.path,prediction);report.recordedCandidates++;
                var sample=prediction.samples[Mathf.Clamp(Mathf.RoundToInt(c.afterSeconds/e.stepSeconds),0,prediction.Count-1)];
                bool match=c.reason=="road"&&!prediction.envelope.Contained&&prediction.envelope.MaximumBound==c.roadBound&&
                    sample.state.position.Equals(c.position)&&sample.state.velocity.Equals(c.velocity)&&sample.state.forward.Equals(c.forward);
                if(match)report.exactMatches++;
            }
        }
        if(report.recordedCandidates==0||report.recordedCandidates!=report.exactMatches)throw new Exception($"Reject counterfactuals: matched {report.exactMatches}/{report.recordedCandidates} original forecasts");
        foreach(var audit in source.openingPlanAudits)foreach(var e in audit.entries.Where(x=>x.captureReason=="opening-unresolved")) {
            var self=Car(e.self);var traffic=e.traffic.Select(Car).ToArray();var driver=source.driverPlanning.First(x=>x.id==self.id);
            var profile=new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk));
            var prediction=new ContextPathPrediction(profile,false);var planner=new DynamicRejoinPlanner(profile);
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,traffic,road,e.tuning,1,0,e.finished,e.steering,true);
            planner.Prepare(in o,in e.previousPath,e.hasPreviousPath);
            var row=new Row{timestamp=e.timestamp,route=self.route,speed=self.speed,horizon=planner.Horizon};
            for(int rank=0;rank<planner.Count;rank++) {
                var path=planner.Path(rank);Forecast(in o,path,prediction);
                var candidate=new Candidate{rank=rank,path=path,geometryValid=float.IsFinite(planner.EstimatedTime(rank)),
                    estimatedTime=float.IsFinite(planner.EstimatedTime(rank))?planner.EstimatedTime(rank):-1,
                    roadBound=prediction.envelope.MaximumBound,firstUncertainTime=prediction.envelope.FirstUncertainTime,shortForecastContained=prediction.envelope.Contained};
                ExecuteGate(in o,path,planner.Horizon,profile,candidate);row.candidates.Add(candidate);
            }
            report.rows.Add(row);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"DYNAMIC_MOVING_REPLAY matches={report.exactMatches}/{report.recordedCandidates} observations={report.rows.Count}");
    }
}
