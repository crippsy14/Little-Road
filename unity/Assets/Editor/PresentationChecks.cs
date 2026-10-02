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
            // Hillcrest's street skyline: the newer terraces share the 5.4 m plot but stand at different heights,
            // and a house's roof can take its own colour.
            Bounds Size(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
            var heights=new Dictionary<string,float>();int x=120;
            foreach(var id in new[]{"terrace_a","terrace_e","terrace_f","terrace_g"}) {
                var t=BayviewKit.Spawn(id,new Vector3(x+=40,0,0),0,host.transform);var b=Size(t);heights[id]=b.max.y;
                Check(b.size.x<=6.4f&&t.GetComponentsInChildren<Transform>().Length>1,$"Terrace {id} fits its 5.4 m frontage ({b.size.x:0.0} m with eaves)");
            }
            Check(heights["terrace_f"]+1<heights["terrace_a"]&&heights["terrace_a"]<heights["terrace_g"]&&heights["terrace_g"]+2<heights["terrace_e"],
                $"Hillcrest terraces step the skyline: cottage {heights["terrace_f"]:0.0} m, plain {heights["terrace_a"]:0.0} m, painted {heights["terrace_g"]:0.0} m, palazzetto {heights["terrace_e"]:0.0} m");
            Material Roof(GameObject go){foreach(var r in go.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)if(m!=null&&m.name.StartsWith("Bayview RoofTile")&&!m.name.Contains("Light"))return m;return null;}
            var plainRoof=Roof(BayviewKit.Spawn("terrace_f",new Vector3(400,0,0),0,host.transform));
            var slate=new BayviewKit.Tint{roof=Toy.Hex(0x69707a)};var slateRoof=Roof(BayviewKit.Spawn("terrace_f",new Vector3(440,0,0),0,host.transform,slate));
            Check(plainRoof!=null&&slateRoof!=null&&plainRoof!=slateRoof&&slateRoof.GetColor("_BaseColor").b>slateRoof.GetColor("_BaseColor").r,"A house's roof tint recolours its tiles (slate roof is bluer than red)");
            // The castle drawbridge (6.7 m out from the gatehouse centre, kit -Y) lies on Via Crest, not across its edge.
            var gate=HillcrestTown.CastleGate();var bridge=gate.at+Quaternion.Euler(0,gate.yaw,0)*Vector3.forward*6.7f;
            HillcrestRoute.Crest.Nearest(bridge,out float off);float skew=Mathf.Abs(Mathf.DeltaAngle(gate.yaw,Quaternion.LookRotation(-HillcrestRoute.Crest.Forward(HillcrestRoute.Crest.Nearest(bridge,out _))).eulerAngles.y));
            Check(Mathf.Abs(off)<1f&&skew<10,$"The castle drawbridge lies on Via Crest ({off:0.00} m off the road's centre, {skew:0} degrees skew)");
            // Road paint round a tight bend (8 m radius, an edge line 3.4 m to the inside): each piece starts where the last ended.
            var bend=new Vector3[41];for(int i=0;i<bend.Length;i++){float a=i*Mathf.PI/80;bend[i]=new Vector3(8*Mathf.Cos(a),0,8*Mathf.Sin(a));}
            var pv=new List<Vector3>();CoastalLandscape.StripeQuads(bend,-3.4f,.14f,0,0,999,pv,new List<int>());float seam=0;
            for(int q=0;q+7<pv.Count;q+=4)seam=Mathf.Max(seam,Vector3.Distance(pv[q+2],pv[q+4]),Vector3.Distance(pv[q+3],pv[q+5]));
            Check(pv.Count==160&&seam<.001f,$"Road edge lines join round tight bends (largest seam {seam*1000:0.0} mm)");
            var camera=new GameObject("Check camera").AddComponent<Camera>();camera.transform.SetParent(host.transform);WorldLook.Camera(camera);
            Check(Mathf.Approximately(camera.layerCullDistances[BayviewKit.DetailLayer],WorldLook.DetailCullDistance*1.25f)&&Mathf.Approximately(camera.layerCullDistances[BayviewKit.MediumLayer],WorldLook.MediumCullDistance*1.2f)&&camera.layerCullSpherical,"The game camera applies the cull distances spherically");
        } finally {UnityEngine.Object.DestroyImmediate(host);}
        return checks;
    }
}
