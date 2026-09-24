using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LittleRoad;
using UnityEngine;

// Read the existing follower's owned buffers after replay, without adding
// instrumentation to its hot path or introducing a second control model.
public static class CornerControlReplay
{
    [Serializable] public sealed class Row
    {
        public double timestamp;public float route,speed,pathOffset,actualOffset,pathError,blend,consumed;
        public float envelope0,envelope1,curvature,pursuitLimit,target,feedAcceleration,trackingAcceleration,drag,pedal,steering;
        public bool localSpeed,guide,hasMatchingPedal,commandMatches;
        public float recordedFollower,recordedTarget,recordedLeader,recordedScenery,recordedApplied;
        public ContextPathState path;
    }
    [Serializable] public sealed class Report
    {
        public string scope="Exact opening observations replayed through the unchanged live follower. Owned private geometry/speed buffers are inspected offline. Matching same-tick pedal samples validate commands where sampling phases overlap; unmatched rows are not claimed as exact recorded commands.";
        public int matchedSamples,matchingCommands;public List<Row> rows=new List<Row>();
    }
    static ObservedCar Car(ContextPlanAudit.Car c)=>new ObservedCar(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    static T Buffer<T>(ContextPathFollower follower,string name)=>(T)typeof(ContextPathFollower).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(follower);
    public static void Run()
    {
        string input=RaceTrial.Arg("--corner-control-input"),output=RaceTrial.Arg("--corner-control-output");
        if(input==null||output==null||!RaceTrial.Has("--coast-within-envelope"))throw new ArgumentException("Input/output and matching coasting flag required");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        var course=new TechnicalCircuit();var road=course.track;
        if(source.track!=course.definition.id||source.version!=course.definition.version)throw new ArgumentException("Track mismatch");
        var line=new RacingLine(road,"cedar-smooth-v2",course.definition.id,course.definition.version);var report=new Report();
        foreach(var audit in source.openingPlanAudits)foreach(var e in audit.entries.Where(x=>x.self.route>=course.regionStarts[1]-35&&x.self.route<=course.regionStarts[3]+45)) {
            var self=Car(e.self);var traffic=e.traffic.Select(Car).ToArray();var driver=source.driverPlanning.First(x=>x.id==self.id);
            var profile=new RacingSpeedProfile(line,e.tuning,new DriverPlanningSettings(driver.localBrakeFraction,driver.risk));
            var follower=new ContextPathFollower(profile,false);var path=e.selectedPath;
            var o=new DriverObservation(e.timestamp,e.stepSeconds,self,traffic,road,e.tuning,1,0,e.finished,e.steering,true);
            var command=follower.Follow(in o,in path);
            var speeds=Buffer<float[]>(follower,"speeds");var distances=Buffer<float[]>(follower,"distances");
            var points=Buffer<Vector3[]>(follower,"points");var curves=Buffer<float[]>(follower,"curvatures");
            float look=Mathf.Clamp(5+Mathf.Max(0,self.speed)*.4f,6,19);int aim=0;
            while(aim<39&&distances[aim+1]<look)aim++;
            var delta=Vector3.Lerp(points[aim],points[aim+1],Mathf.InverseLerp(distances[aim],distances[aim+1],look))-self.position;
            var travel=self.velocity;travel.y=0;var heading=travel.sqrMagnitude>9?travel.normalized:self.forward;
            float curve=2*Mathf.Sin(Vector3.SignedAngle(heading,delta,Vector3.up)*Mathf.Deg2Rad)/Mathf.Max(3,delta.magnitude);
            float offset=Vector3.Dot(points[0]-road.At(self.route),road.Right(self.route));
            var row=new Row{timestamp=e.timestamp,route=self.route,speed=self.speed,pathOffset=offset,actualOffset=self.offset,pathError=offset-self.offset,
                blend=path.blend,consumed=path.consumed,localSpeed=path.localSpeed,guide=path.guide,path=path,
                envelope0=speeds[0],envelope1=speeds[1],curvature=curves[1],pursuitLimit=Mathf.Sqrt(e.tuning.roadGrip*1.65f*.85f/Mathf.Max(.00001f,Mathf.Abs(curve))),
                target=command.targetSpeed,feedAcceleration=(speeds[1]*speeds[1]-speeds[0]*speeds[0])/(2*Mathf.Max(.01f,distances[1])),
                trackingAcceleration=(command.targetSpeed-self.speed)*3,drag=DraftingModel.Resistance(self.speed,e.tuning.mass,o.dragReduction),pedal=command.pedal,steering=command.steering};
            var recorded=source.pedalSamples.FirstOrDefault(p=>p.id==self.id&&Math.Abs(p.decisionTimestamp-e.timestamp)<1e-8);
            if(recorded!=null) {
                report.matchedSamples++;row.hasMatchingPedal=true;
                row.recordedFollower=recorded.follower;row.recordedTarget=recorded.followerTarget;row.recordedLeader=recorded.leader;row.recordedScenery=recorded.scenery;row.recordedApplied=recorded.applied;
                row.commandMatches=recorded.follower==command.pedal&&recorded.followerTarget==command.targetSpeed;if(row.commandMatches)report.matchingCommands++;
            }
            report.rows.Add(row);
        }
        File.WriteAllText(output,JsonUtility.ToJson(report,true));
        Debug.Log($"CORNER_CONTROL_REPLAY rows={report.rows.Count} sameTick={report.matchedSamples} exact={report.matchingCommands}");
        if(report.matchedSamples==0||report.matchedSamples!=report.matchingCommands)throw new Exception("Follower command replay mismatch; reject attribution");
    }
}
