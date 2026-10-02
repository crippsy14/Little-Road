using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Every car sweeps its contacts (study 215): speculative contacts snag road-mesh edges while a car yaws.
public static class ContactChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var car=TrialWorld.Car("compact_v1",Color.white);
        var race=new GameObject("Contact check owner").AddComponent<RaceTrial>();race.track=new Circuit();
        try {
            var driver=car.AddComponent<RoadCar>();driver.Setup(race,0,CarTuning.Stock,false,1,"contact check");
            Check(driver.body.collisionDetectionMode==CollisionDetectionMode.ContinuousDynamic,"AI and player cars use swept contacts by default (--speculative-contacts restores the old mode)");
        } finally {UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(race.gameObject);}
        // Standing cars sit on their four wheels (docs/57, 2.5): one dropped upright 0.4 m above an 8 degree slope, across
        // it at 35 degrees, is lowered and tilted until every wheel touches.
        var slope=GameObject.CreatePrimitive(PrimitiveType.Cube);slope.name="Check slope";slope.transform.position=new Vector3(500,-.5f,500);slope.transform.localScale=new Vector3(40,1,40);slope.transform.rotation=Quaternion.Euler(8,0,0);
        var parked=TrialWorld.Car("beetle",Color.white);
        try {
            parked.transform.SetPositionAndRotation(new Vector3(500,1.2f,500),Quaternion.Euler(0,35,0));Physics.SyncTransforms();
            bool seated=CarContact.Seat(parked);var gaps=CarContact.Gaps(parked);
            Check(seated&&gaps.Count>=4&&gaps.Count%4==0&&gaps.All(g=>g.gap>-.03f&&g.gap<.06f),$"A standing car on an 8 degree slope is seated on all four wheels (gaps {string.Join(" ",gaps.Select(g=>g.gap.ToString("0.00")))} m)");
        } finally {UnityEngine.Object.DestroyImmediate(parked);UnityEngine.Object.DestroyImmediate(slope);}
        // Every body's physics box against its tyres: the box's bottom must sit at the tyres' lowest point (within 2 cm),
        // or the car rides on the box above (or below) the road. Logged per body for the report.
        var lines=new List<string>();int off=0;
        foreach(var asset in new[]{CarBodies.Starter,"compact_v1"}.Concat(CarBodies.Family).Concat(CarBodies.Sporty).Concat(CarBodies.Townsfolk).Distinct()) {
            var probe=TrialWorld.Car(asset,Color.white);
            try {
                var (centre,size)=RoadCar.CollisionBox(probe);float boxBottom=centre.y-size.y*.5f;
                float tyre=float.MaxValue;
                foreach(var t in probe.GetComponentsInChildren<Transform>(true))if(WheelVisuals.IsPivot(t.name))foreach(var f in t.GetComponentsInChildren<MeshFilter>(true)){var m=f.sharedMesh;if(m==null)continue;foreach(var v in m.vertices)tyre=Mathf.Min(tyre,probe.transform.InverseTransformPoint(f.transform.TransformPoint(v)).y);}
                float lowest=float.MaxValue;string lowName="";
                foreach(var f in probe.GetComponentsInChildren<MeshFilter>(true)){var m=f.sharedMesh;if(m==null)continue;foreach(var v in m.vertices){float y=probe.transform.InverseTransformPoint(f.transform.TransformPoint(v)).y;if(y<lowest){lowest=y;lowName=f.name;}}}
                float lift=tyre-boxBottom;if(Mathf.Abs(lift)>.02f)off++;
                lines.Add($"{asset}: box bottom {boxBottom:0.000}, tyres {tyre:0.000}, lowest mesh {lowest:0.000} ({lowName}), car rides {lift*100:0.0} cm up");
            } finally {UnityEngine.Object.DestroyImmediate(probe);}
        }
        Debug.Log("LITTLE_ROAD_BODY_RIDE\n"+string.Join("\n",lines));
        Check(off==0,$"Every body's physics box sits at its tyres, within 2 cm ({lines.Count} bodies): cars drive on their wheels");
        return checks;
    }
}
