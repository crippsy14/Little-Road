using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Paint layers (car-forge docs/brief-paint-layers.md; Richard, 24 Oct: "really go AAA in detail", and rust "can't just be
    // colouring component parts in full"). A Car Forge body may carry graded masks in its livery atlas, Cars/<id>_layers
    // (rust, dirt, fade, edge wear; two tones, mud, a stripe), and their colours in its body file. A car's paint is composed
    // from them once per body and paint: tones, stripe, fade, edge wear, dirt, mud, rust, in car-forge
    // forge/paint_layers.py compose()'s order and arithmetic (keep the two in step), into an albedo texture and a metal
    // and gloss map on its body-paint materials. So a rusty car is rusty in any race's colour, with soft edges.
    public static class ForgeLayers
    {
        [System.Serializable] public sealed class Data{public float[] stain,core,dirt,mud,fade,edge,tone1,tone2,stripe;public bool edgeMetal;
            public bool Valid=>stain!=null&&stain.Length==3;}
        sealed class Masks{public int size;public Color32[] wear,tone,extra;}
        static readonly Dictionary<string,Masks> masks=new();
        static readonly Dictionary<string,(Texture2D albedo,Texture2D gloss)> composed=new();
        public const string AlbedoName="ForgeLayers",GlossName="ForgeLayersGloss";

        static Masks Load(string asset)
        {
            if(masks.TryGetValue(asset,out var m))return m;
            var ta=Resources.Load<TextAsset>("Cars/"+asset+"_layers");m=null;
            // (FLY1: wear and tone; FLY2: and extra, the wet layer)
            if(ta!=null&&ta.bytes.Length>12&&ta.bytes[0]=='F'&&ta.bytes[1]=='L'&&ta.bytes[2]=='Y'&&(ta.bytes[3]=='1'||ta.bytes[3]=='2')) {
                var b=ta.bytes;int o=4;int count=ta.bytes[3]=='2'?3:2;var outs=new Color32[3][];int size=0;
                for(int k=0;k<count;k++){int n=System.BitConverter.ToInt32(b,o);o+=4;var png=new byte[n];System.Array.Copy(b,o,png,0,n);o+=n;
                    var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(png);size=t.width;outs[k]=t.GetPixels32();Free(t);}
                m=new Masks{size=size,wear=outs[0],tone=outs[1],extra=outs[2]};
            }
            masks[asset]=m;return m;
        }
        static void Free(Object o){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
        public static Data Of(string asset){var d=CarBodies.Forged(asset)?.layers;return d!=null&&d.Valid?d:null;}
        public static bool Has(string asset)=>Of(asset)!=null&&Load(asset)!=null;

        static Color C(float[] v)=>v!=null&&v.Length==3?new Color(v[0],v[1],v[2]):Color.white;
        static float Smooth(float e0,float e1,float x){float t=Mathf.Clamp01((x-e0)/(e1-e0));return t*t*(3-2*t);}
        // One texel's colour, metal and smoothness: forge/paint_layers.py compose(), line for line.
        public static Color Texel(Color c,ref float metal,ref float smooth,Color32 wear,Color32 tone,Data d)=>Texel(c,ref metal,ref smooth,wear,tone,new Color32(0,0,0,0),d);
        public static Color Texel(Color c,ref float metal,ref float smooth,Color32 wear,Color32 tone,Color32 extra,Data d)
        {
            float r=wear.r/255f,dirt=wear.g/255f,fade=wear.b/255f,edge=wear.a/255f,t1=tone.r/255f,t2=tone.g/255f,mud=tone.b/255f,st=tone.a/255f;
            c=Color.Lerp(c,C(d.tone1),t1);c=Color.Lerp(c,C(d.tone2),t2);c=Color.Lerp(c,C(d.stripe),st);
            c=Color.Lerp(c,.55f*c+.45f*C(d.fade),fade);smooth+=(.2f-smooth)*fade;
            c=Color.Lerp(c,C(d.edge),edge);smooth+=((d.edgeMetal?.45f:.2f)-smooth)*edge;metal+=((d.edgeMetal?.6f:0)-metal)*edge;
            float dd=dirt*.85f;c=Color.Lerp(c,.4f*c+.6f*C(d.dirt),dd);smooth+=(.12f-smooth)*dd;
            c=Color.Lerp(c,C(d.mud),mud);smooth+=(.15f-smooth)*mud;
            float bloom=Smooth(0,.5f,r),core=Smooth(.45f,.85f,r);
            c=Color.Lerp(c,C(d.stain),bloom*.75f);c=Color.Lerp(c,C(d.core),core);
            smooth+=(.08f-smooth)*Mathf.Max(bloom*.6f,core);metal*=1-core;
            float wet=extra.r/255f;c=Color.Lerp(c,.72f*c,wet);smooth+=(.92f-smooth)*wet;   // (raindrops: darker, glassy)
            c.a=1;return c;
        }
        // The composed pair for a body in a paint over a livery (or none), cached: every car of that body and paint shares it.
        public static (Texture2D albedo,Texture2D gloss) Compose(string asset,Color paint,Texture2D livery,string key)
        {
            string id=asset+"|"+key;if(composed.TryGetValue(id,out var pair)&&pair.albedo!=null)return pair;
            var d=Of(asset);var m=Load(asset);if(d==null||m==null)return (null,null);
            // (the paint's own type first: a faded car is faded under its rust; TrialWorld.PaintFinish)
            string finish=CarBodies.Forged(asset)?.finish;   // (read here: the loop below runs off the main thread)
            float metal0=.05f,smooth0=.56f;TrialWorld.PaintFinish(finish,ref paint,ref metal0,ref smooth0);
            int n=m.size;Color32[] lv=null;int ls=0;
            if(livery!=null&&livery.isReadable){lv=livery.GetPixels32();ls=livery.width;}
            var px=new Color32[n*n];var gl=new Color32[n*n];
            System.Threading.Tasks.Parallel.For(0,n,y=>{
                for(int x=0;x<n;x++){int i=y*n+x;
                    Color c=lv!=null?(Color)lv[(y*ls/n)*ls+x*ls/n]:paint;
                    // (the livery was composed in the paint as it was; the finish's colour shift applies over it too)
                    if(lv!=null){float a0=.05f,s0=.56f;TrialWorld.PaintFinish(finish,ref c,ref a0,ref s0);}
                    float mt=metal0,sm=smooth0;c=Texel(c,ref mt,ref sm,m.wear[i],m.tone[i],m.extra!=null?m.extra[i]:new Color32(0,0,0,0),d);
                    px[i]=c;gl[i]=new Color32((byte)(255*Mathf.Clamp01(mt)),0,0,(byte)(255*Mathf.Clamp01(sm)));}});
            var albedo=new Texture2D(n,n,TextureFormat.RGBA32,true){name=AlbedoName,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
            albedo.SetPixels32(px);albedo.Apply(true);
            var gloss=new Texture2D(n,n,TextureFormat.RGBA32,true,true){name=GlossName,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            gloss.SetPixels32(gl);gloss.Apply(true);
            return composed[id]=(albedo,gloss);
        }
        // Puts a body's layers on every body-paint material of a car (over the livery there, if any). Returns how many.
        public static int Apply(GameObject car,string asset,Color paint)
        {
            if(!Has(asset))return 0;int count=0;
            Texture2D livery=null;string key=ColorUtility.ToHtmlStringRGB(paint);
            foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var mat in r.sharedMaterials){
                if(mat==null||!mat.name.Contains("BodyPaint"))continue;
                var t=mat.GetTexture("_BaseMap") as Texture2D;
                if(t!=null&&t.name=="ForgeLivery"){livery=t;break;}}
            // (a livery is the body's own, composed in this paint with the body's number: the same for every car of the body
            //  and paint, so the key needs only whether there is one; the car's own copy is freed once composed over)
            if(livery!=null)key+="|livery";
            var (albedo,gloss)=Compose(asset,paint,livery,key);if(albedo==null)return 0;
            var done=new HashSet<Material>();
            foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var mat in r.sharedMaterials){
                if(mat==null||!mat.name.Contains("BodyPaint")||!done.Add(mat))continue;
                mat.SetTexture("_BaseMap",albedo);mat.color=Color.white;
                mat.SetTexture("_MetallicGlossMap",gloss);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",1);count++;}
            if(livery!=null)Free(livery);
            return count;
        }
    }
}
