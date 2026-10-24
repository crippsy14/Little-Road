using System;
using System.Collections.Generic;
using System.Linq;
using LittleRoad;
using UnityEngine;

// Paint layers (ForgeLayers; car-forge docs/brief-paint-layers.md, 24 Oct): the recipe leaves clean paint alone and lays
// rust, dirt, fade and tones over it in order; every installed body that names layers has its masks; a car built from
// one wears a composed albedo and a metal and gloss map, shared by every car of that body and paint; a body without
// layers is untouched.
public static class ForgeLayersChecks
{
    public static List<string> Run()
    {
        var checks=new List<string>();
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks.Add(message);}
        var d=new ForgeLayers.Data{stain=new[]{.64f,.31f,.10f},core=new[]{.27f,.12f,.06f},dirt=new[]{.30f,.25f,.19f},mud=new[]{.37f,.28f,.17f},
            fade=new[]{.9f,.87f,.8f},edge=new[]{.56f,.57f,.56f},tone1=new[]{.1f,.2f,.7f},tone2=new[]{1f,1f,1f},stripe=new[]{1f,1f,1f}};
        Color paint=new Color(.8f,.2f,.1f);
        Color T(Color32 wear,Color32 tone,out float metal,out float smooth){metal=.05f;smooth=.56f;return ForgeLayers.Texel(paint,ref metal,ref smooth,wear,tone,d);}
        var none=new Color32(0,0,0,0);
        var c0=T(none,none,out float m0,out float s0);
        Check(Mathf.Abs(c0.r-paint.r)<1e-4f&&Mathf.Abs(c0.g-paint.g)<1e-4f&&Mathf.Abs(c0.b-paint.b)<1e-4f&&m0==.05f&&s0==.56f,"Paint layers: empty masks leave the paint and its gloss as they are");
        var rust=T(new Color32(255,0,0,0),none,out _,out float sr);
        Check(rust.grayscale<paint.grayscale*.6f&&sr<.12f,$"Paint layers: a rust core is dark and matte ({rust.grayscale:0.00}, smoothness {sr:0.00})");
        var bloom=T(new Color32(90,0,0,0),none,out _,out _);
        Check(bloom.r>bloom.b+.2f&&bloom.grayscale>rust.grayscale,"Paint layers: rust's edge is an orange-brown stain, lighter than its core");
        var fade=T(new Color32(0,0,255,0),none,out _,out float sf);
        Check(fade.grayscale>paint.grayscale&&sf<.3f,"Paint layers: fade is paler and chalky");
        var dirt=T(new Color32(0,255,0,0),none,out _,out float sd);
        Check(dirt.grayscale<paint.grayscale&&sd<.2f,"Paint layers: dirt is darker and duller");
        var tone=T(none,new Color32(255,0,0,0),out _,out _);
        Check(Mathf.Abs(tone.b-.7f)<.01f,"Paint layers: a full tone mask is the tone's colour");
        float mw=.05f,sw=.56f;var wet=ForgeLayers.Texel(paint,ref mw,ref sw,none,none,new Color32(255,0,0,0),d);
        Check(wet.grayscale<paint.grayscale&&sw>.85f,"Paint layers: a raindrop is darker and glassy");
        var rustOverTone=T(new Color32(255,0,0,0),new Color32(255,0,0,0),out _,out _);
        Check(Mathf.Abs(rustOverTone.r-rust.r)<.02f,"Paint layers: rust lies over a tone, as the order says (tones first, rust last)");
        // every installed Forge body that names layers has its masks, and a car of it wears them
        foreach(var id in CarBodies.Forge) {
            var data=ForgeLayers.Of(id);
            if(data==null){
                var plain=TrialWorld.Car(id,paint);
                try{Check(plain.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).All(m=>m==null||m.GetTexture("_BaseMap")==null||m.GetTexture("_BaseMap").name!=ForgeLayers.AlbedoName),$"Paint layers: {id} has none, and its car wears none");}
                finally{UnityEngine.Object.DestroyImmediate(plain);}
                continue;
            }
            Check(ForgeLayers.Has(id),$"Paint layers: {id} names layers and its masks are installed (Cars/{id}_layers)");
            var a=TrialWorld.Car(id,paint);var b=TrialWorld.Car(id,paint);
            try {
                var ma=a.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.name.Contains("BodyPaint")).ToList();
                var mb=b.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null&&m.name.Contains("BodyPaint")).ToList();
                Check(ma.Count>0&&ma.All(m=>m.GetTexture("_BaseMap")!=null&&m.GetTexture("_BaseMap").name==ForgeLayers.AlbedoName&&m.GetTexture("_MetallicGlossMap")!=null&&m.IsKeywordEnabled("_METALLICSPECGLOSSMAP")),
                    $"Paint layers: a {id} wears its composed paint and gloss map on every body-paint material ({ma.Count})");
                Check(ma[0].GetTexture("_BaseMap")==mb[0].GetTexture("_BaseMap"),$"Paint layers: two {id}s in one paint share one composed texture");
                var tex=(Texture2D)ma[0].GetTexture("_BaseMap");var px=tex.GetPixels32();
                // (any layer: texels visibly off the plain paint, rust, a tone or a raindrop alike)
                Color32 pc=paint;int off=px.Count(p=>System.Math.Abs(p.r-pc.r)+System.Math.Abs(p.g-pc.g)+System.Math.Abs(p.b-pc.b)>40);
                Check(off>px.Length/5000,$"Paint layers: the {id}'s composed paint holds its layers ({off} texels off the paint of {px.Length})");
            } finally{UnityEngine.Object.DestroyImmediate(a);UnityEngine.Object.DestroyImmediate(b);}
        }
        return checks;
    }
}
