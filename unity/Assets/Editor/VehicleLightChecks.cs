using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

public static class VehicleLightChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var assets=new List<string>{"compact_v1","little_car",CarBodies.Flagship,CarBodies.Starter};
        assets.AddRange(CarBodies.Family);assets.AddRange(CarBodies.Sporty);
        foreach(string asset in assets) {
            var car=TrialWorld.Car(asset,Color.cyan);var other=TrialWorld.Car(asset,Color.yellow);
            try {
                var lights=car.GetComponent<VehicleLights>();
                Check(lights.BrakeMaterialCount>0&&lights.ReverseMaterialCount>0,"Separate brake/reverse lenses: "+asset);
                lights.ApplyDrive(12,-1);
                Check(lights.Braking&&!lights.Reversing,"Forward braking illuminates red only: "+asset);
                lights.ApplyDrive(-2,-1);
                Check(!lights.Braking&&lights.Reversing,"Backing illuminates white: "+asset);
                foreach(var renderer in car.GetComponentsInChildren<Renderer>(true)) {
                    var materials=renderer.sharedMaterials;var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;
                    for(int slot=0;slot<materials.Length;slot++) {
                        var material=materials[slot];Color emission=material.GetColor("_EmissionColor");
                        if(material.name.Contains("ReverseLight")) {
                            Check(emission.r>1&&Mathf.Abs(emission.r-emission.g)<.01f&&Mathf.Abs(emission.g-emission.b)<.01f,"White reverse material: "+asset+"/"+renderer.name);
                            foreach(int index in mesh.GetTriangles(slot)) {
                                Vector3 point=car.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                                if(point.z>0)throw new InvalidOperationException("Reverse illumination assigned to front geometry: "+asset);
                            }
                        }
                        else if(emission.maxColorComponent>.001f)throw new InvalidOperationException("Reverse state illuminated unrelated material: "+asset+"/"+material.name);
                    }
                }
                foreach(var renderer in other.GetComponentsInChildren<Renderer>(true))foreach(var material in renderer.sharedMaterials)
                    if(material.GetColor("_EmissionColor").maxColorComponent>.001f)throw new InvalidOperationException("Lamp state leaked to another car: "+asset);
                Check(true,"Reverse light limited to rear geometry and owning car: "+asset);
                lights.ApplyDrive(-2,1);Check(lights.Braking&&lights.Reversing,"Braking while backing retains white reverse light: "+asset);
                lights.ApplyDrive(0,0);Check(!lights.Braking&&!lights.Reversing,"Idle clears active lamps: "+asset);
            } finally {UnityEngine.Object.DestroyImmediate(car);UnityEngine.Object.DestroyImmediate(other);}
        }
        return checks;
    }
}
