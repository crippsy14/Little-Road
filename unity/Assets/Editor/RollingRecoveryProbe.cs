using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEditor.SceneManagement;
using UnityEngine;

// Post-build diagnostic: no player configuration or frozen source is rewritten.
public static class RollingRecoveryProbe
{
    [Serializable] public sealed class Row {public double timestamp;public float route,offset,speed;public string geometryRejection,sceneRejection,emptyTrafficRejection;public bool sceneAccepted,emptyTrafficAccepted;public float endOffset;}
    [Serializable] public sealed class Report {public string scope="Captured pre-recovery Bayview car8 failed-plan states and reconstructed Bayview static scene. Horizontal collider dimensions and local centre are captured; vertical height is an approximate1.64m because the audit omits it, so scene results are provisional. Pure horizontal geometry rejection is unaffected by that height. These precede the recovery transition, not an exact first-recovery recording. Empty traffic is a diagnostic counterfactual. No live commands are issued.";public List<Row> rows=new();}
    static ObservedCar Car(ContextPlanAudit.Car c)=>new(c.id,c.position,c.velocity,c.forward,c.right,c.route,c.offset,c.speed,c.halfWidth,c.halfLength,c.yawRate,c.colliderCentre);
    public static void Run()
    {
        string input=RaceTrial.Arg("--rolling-probe-input"),output=RaceTrial.Arg("--rolling-probe-output");
        if(input==null||output==null||File.Exists(output))throw new ArgumentException("Fresh probe paths required");
        var source=JsonUtility.FromJson<TechnicalCircuitReview.Evidence>(File.ReadAllText(input));
        if(source.track!="bayview")throw new ArgumentException("Bayview probe only");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var road=new Circuit();TrialWorld.Create(road);var report=new Report();
        foreach(var entry in source.failedPlanAudits.SelectMany(a=>a.entries).Where(e=>e.self.id==8&&e.timestamp>20&&e.self.route>560&&e.self.route<660)) {
            var self=Car(entry.self);var traffic=entry.traffic.Select(Car).ToArray();
            var root=new GameObject("Probe body");var body=root.AddComponent<Rigidbody>();body.isKinematic=true;
            var collider=root.AddComponent<BoxCollider>();collider.center=self.colliderCentre;collider.size=new Vector3(self.halfWidth*2,1.64f,self.halfLength*2);
            root.transform.SetPositionAndRotation(self.position,Quaternion.LookRotation(self.forward));
            var sensor=new RecoverySensor(body,collider);
            try {
                Physics.SyncTransforms();var controller=new RollingRecovery();
                var o=new DriverObservation(entry.timestamp,entry.stepSeconds,self,traffic,road,entry.tuning,1,0,entry.finished,entry.steering,true);
                controller.TryPlan(in o,self.route,road.length,null);var row=new Row{timestamp=entry.timestamp,route=self.route,offset=self.offset,speed=self.speed,geometryRejection=controller.Rejection};
                row.sceneAccepted=controller.TryPlan(in o,self.route,road.length,sensor);row.sceneRejection=controller.Rejection;row.endOffset=controller.PredictedEndOffset;
                var empty=new DriverObservation(entry.timestamp,entry.stepSeconds,self,Array.Empty<ObservedCar>(),road,entry.tuning,1,0,entry.finished,entry.steering,true,o.dragReduction);
                row.emptyTrafficAccepted=controller.TryPlan(in empty,self.route,road.length,sensor);row.emptyTrafficRejection=controller.Rejection;report.rows.Add(row);
            } finally {sensor.Dispose();UnityEngine.Object.DestroyImmediate(root);}
        }
        if(report.rows.Count==0)throw new Exception("No captured approach states");
        File.WriteAllText(output,JsonUtility.ToJson(report,true));Debug.Log("ROLLING_PROBE_OK rows="+report.rows.Count);
    }
}
