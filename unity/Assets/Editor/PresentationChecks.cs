using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// Screen UI draws as overlays (never through the game camera's motion blur, TAA or speed FOV), and kit
// pieces sort into distance-cull layers by size, with trees' own LOD left alone.
public static class PresentationChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var host=new GameObject("Presentation check host");
        try {
            var canvas=CoastUI.Canvas(host.transform,"Check canvas",4);
            Check(canvas.renderMode==RenderMode.ScreenSpaceOverlay&&canvas.worldCamera==null,"HUD canvases are screen-space overlays with no camera");
            var bench=BayviewKit.Spawn("bench",Vector3.zero,0,host.transform);var house=BayviewKit.Spawn("terrace_a",new Vector3(40,0,0),0,host.transform);
            var tree=BayviewKit.Spawn("tree_round",new Vector3(80,0,0),0,host.transform);KitLod.Attach(tree,"tree_round");
            Check(bench.layer==BayviewKit.DetailLayer&&bench.GetComponent<LODGroup>()?.fadeMode==LODFadeMode.CrossFade,"Small props dissolve out at "+WorldLook.DetailCullDistance+" m (detail layer, cross-fading LOD group)");
            Check(house.layer==0,"Buildings always draw (default layer)");
            Check(tree.layer==0&&tree.GetComponent<LODGroup>()!=null,"Trees keep their own LOD (far stand-in), not the cull layers");
            var camera=new GameObject("Check camera").AddComponent<Camera>();camera.transform.SetParent(host.transform);WorldLook.Camera(camera);
            Check(Mathf.Approximately(camera.layerCullDistances[BayviewKit.DetailLayer],WorldLook.DetailCullDistance*1.25f)&&Mathf.Approximately(camera.layerCullDistances[BayviewKit.MediumLayer],WorldLook.MediumCullDistance*1.2f)&&camera.layerCullSpherical,"The game camera applies the cull distances spherically");
        } finally {UnityEngine.Object.DestroyImmediate(host);}
        return checks;
    }
}
