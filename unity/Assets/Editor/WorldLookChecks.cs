using System;
using System.Collections.Generic;
using LittleRoad;
using UnityEngine;

// Keeps the art direction (docs/art-direction.md) from drifting back to "too toy": colours are
// graded into the WorldLook limits, the ground stays part photographic, and the grade and light
// stay in their agreed ranges.
public static class WorldLookChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool value,string name){if(!value)throw new Exception("World look check failed: "+name);checks.Add(name);}
        Check(WorldLook.WithinLimits(WorldLook.Surface(Color.red))&&WorldLook.WithinLimits(WorldLook.Surface(new Color(.2f,1,.1f))),"Saturated colours are graded into the limits");
        Check(WorldLook.Surface(Color.white).maxColorComponent<=WorldLook.ValueCap+.001f&&WorldLook.Surface(Color.black).maxColorComponent>=WorldLook.ValueFloor-.001f,"No paper white or pure black surfaces");
        var hdr=new Color(2.2f,1.5f,.7f);Check(WorldLook.Surface(hdr)==hdr,"Emissive/HDR tints keep their energy");
        var kit=TrialWorld.Material("Look check",new Color(1,.05f,.05f));Check(WorldLook.WithinLimits(kit.color),"TrialWorld.Material grades every surface colour");
        var paint=TrialWorld.Material("Look check paint",new Color(1,.05f,.05f),0,.5f,false);Check(paint.color==new Color(1,.05f,.05f),"Car body paint is exempt from grading");
        UnityEngine.Object.DestroyImmediate(kit);UnityEngine.Object.DestroyImmediate(paint);
        var palette=Resources.Load<TextAsset>("Bayview/palette");
        if(palette!=null) {
            var entries=JsonUtility.FromJson<Palette>(palette.text).entries;int graded=0;
            foreach(var e in entries)if(WorldLook.WithinLimits(WorldLook.Surface(new Color(e.r,e.g,e.b))))graded++;
            Check(graded==entries.Length,$"All {entries.Length} kit palette colours grade into the limits");
        }
        var stone=WorldLook.Kit("Look check stone",new Color(.9f,.8f,.6f),WorldLook.Pattern.Stone);
        Check(stone.shader.name=="LittleRoad/Kit"&&WorldLook.WithinLimits(stone.color),"Kit materials use the patterned kit shader, graded");UnityEngine.Object.DestroyImmediate(stone);
        Check(WorldLook.PatternFor("Tint_Wall")==WorldLook.Pattern.Plaster&&WorldLook.PatternFor("StoneDark")==WorldLook.Pattern.Stone&&WorldLook.PatternFor("RockDark")==WorldLook.Pattern.Rock&&WorldLook.PatternFor("RoofSlate")==WorldLook.Pattern.Tile&&
            WorldLook.PatternFor("LeafDark")==WorldLook.Pattern.Leaf&&WorldLook.PatternFor("ReedHead")==WorldLook.Pattern.Leaf&&WorldLook.PatternFor("GlassLit")==WorldLook.Pattern.Plain,"Palette materials map to surface patterns");
        Check(WorldLook.TerrainStylise<=.7f&&WorldLook.RunoffStylise<=.7f,"Ground keeps its photographic texture (stylise <= 0.7)");
        Check(WorldLook.Saturation<=0&&WorldLook.Contrast>=0&&WorldLook.Contrast<=30,"Grade desaturates slightly and keeps contrast moderate");
        Check(WorldLook.SunShadowStrength>=.8f&&WorldLook.ShadowDistance>=150,"Shadows are strong and reach across the valley");
        Check(WorldLook.CloudCover>0&&WorldLook.CloudCover<=.45f,"Cloud shadows drift, but never black out the ground");
        return checks;
    }
    [Serializable] class Palette{public Entry[] entries;}
    [Serializable] class Entry{public string name;public float r,g,b;}
}
