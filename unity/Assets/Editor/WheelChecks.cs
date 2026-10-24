using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Wheels turn (Richard, 1 Oct: "the wheels don't turn when driving so it looks like cars are floating along the
// surface"): with BodyMotion's wheel holder in place, posing the pivots turns every tyre mesh, and each body rolls
// its wheels at its own radius.
public static class WheelChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Wheel check: "+label);checks.Add(label);}
        // (every installed model: the Car Forge cars since 24 Oct, the Regale's 0.345 m wheels before; toy wheels, 0.25-0.5 m)
        foreach(var (body,radius) in CarBodies.Forge.Select(b=>(b,0f))) {
            var car=TrialWorld.Car(body,Color.white);var rb=car.AddComponent<Rigidbody>();rb.isKinematic=true;
            car.AddComponent<BodyMotion>().Collect(rb);var wheels=new WheelVisuals(car);
            var meshes=wheels.pivots.Select(p=>p.GetComponentInChildren<Renderer>()).ToArray();
            Check(wheels.pivots.Length>=4&&meshes.All(m=>m!=null&&m.transform!=null),$"{body}: every wheel pivot still carries its tyre after BodyMotion ({wheels.pivots.Length} pivots)");
            var before=meshes.Select(m=>m.transform.rotation).ToArray();wheels.Pose(90,0);
            float least=meshes.Select((m,i)=>Quaternion.Angle(before[i],m.transform.rotation)).Min();
            Check(least>85&&least<95,$"{body}: a quarter turn of the pivots turns every tyre a quarter turn (least {least:0.0} degrees)");
            Check(radius<=0?wheels.Radius>.25f&&wheels.Radius<.5f:Mathf.Abs(wheels.Radius-radius)<.01f,$"{body}: wheels roll at their own radius ({wheels.Radius:0.000} m)");
            UnityEngine.Object.DestroyImmediate(car);
        }
        return checks;
    }
}
