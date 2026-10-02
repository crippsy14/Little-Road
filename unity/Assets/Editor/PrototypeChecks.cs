using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LittleRoad;
using UnityEngine;

// The Regale, Duke Rolo's prototype (docs/53): the model and its livery load, the livery material carries the
// texture, every liveried vertex maps inside it, and the painted colours obey WorldLook's limits.
public static class PrototypeChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();void Check(bool pass,string label){if(!pass)throw new Exception("Prototype check: "+label);checks.Add(label);}
        string id=CarBodies.Prototype;
        Check(CarBodies.IsKnown(id)&&!CarBodies.Sellable(id)&&CarBodies.Length(id)>4&&CarBodies.ShoulderWidth(id)>2,"The Regale is a known body, not for sale, with its measured length and width");
        var art=Resources.Load<Texture2D>("Cars/"+id+"_livery");
        Check(art!=null&&art.width==2048&&art.height==2048,$"Livery texture loads at 2048 px ({art?.width}x{art?.height})");
        var car=TrialWorld.Car(id,Color.white);
        var livery=car.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.name.Contains("LiveryMap")).ToList();
        Check(livery.Count>0&&livery.All(m=>m.GetTexture("_BaseMap")==art&&m.color==Color.white),$"The livery material carries the texture, untinted ({livery.Count} materials; {string.Join(", ",livery.Select(m=>m.name+" "+(m.GetTexture("_BaseMap")!=null?m.GetTexture("_BaseMap").name:"no map")+" "+m.color))})");
        foreach(var key in new[]{"CarbonFibre","GoldTrim","CaliperRed","CanopyGlass"}) {
            var m=car.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).FirstOrDefault(x=>x!=null&&x.name.Contains(key));
            Check(m!=null&&m.color!=new Color(.055f,.073f,.08f),$"{key} has its own finish");
        }
        UnityEngine.Object.DestroyImmediate(car);
        var model=Resources.Load<GameObject>("Cars/"+id);int outside=0,mapped=0;
        foreach(var f in model.GetComponentsInChildren<MeshFilter>()) {
            var mesh=f.sharedMesh;var mats=f.GetComponent<Renderer>().sharedMaterials;var uv=new List<Vector2>();mesh.GetUVs(0,uv);
            for(int s=0;s<mesh.subMeshCount&&s<mats.Length;s++) {
                if(mats[s]==null||!mats[s].name.Contains("LiveryMap"))continue;
                foreach(int v in mesh.GetTriangles(s)){mapped++;var p=uv[v];if(p.x<-.001f||p.x>1.001f||p.y<-.001f||p.y>1.001f)outside++;}
            }
        }
        Check(mapped>10000&&outside==0,$"Every liveried vertex maps inside the texture ({mapped} corners, {outside} outside)");
        // The painted colours: a 64 px grid of samples from the PNG itself (the imported texture isn't readable).
        var path=Path.Combine(Application.dataPath,"Resources/Cars/"+id+"_livery.png");var raw=new Texture2D(2,2);raw.LoadImage(File.ReadAllBytes(path));
        int bad=0,seen=0;
        for(int y=16;y<raw.height;y+=32)for(int x=16;x<raw.width;x+=32){seen++;var c=raw.GetPixel(x,y);if(!WorldLook.WithinLimits(c))bad++;}
        UnityEngine.Object.DestroyImmediate(raw);
        Check(bad<=seen/100,$"Livery colours sit within WorldLook's limits ({bad} of {seen} samples outside, anti-aliased edges allowed)");
        return checks;
    }
}
