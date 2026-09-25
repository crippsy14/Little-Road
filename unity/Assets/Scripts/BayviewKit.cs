using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Places Blender-built toy-town assets (source-art/build_bayview_kit.py, Resources/Bayview).
    // Imported colours become URP Lit materials; "Tint_*" slots take a per-instance colour;
    // "Collision*" boxes become static colliders; "SignAnchor_*" empties receive shop text.
    public static class BayviewKit
    {
        public sealed class Tint
        {
            public Color wall=new(.97f,.89f,.72f),shutter=new(.10f,.55f,.62f),awning=new(.92f,.22f,.24f),door=new(.12f,.42f,.62f);
            public Tint(){}
            public Tint(Color wall,Color shutter,Color awning,Color door){this.wall=wall;this.shutter=shutter;this.awning=awning;this.door=door;}
        }
        static readonly Dictionary<string,Material> materials=new();
        [System.Serializable] class PaletteEntry{public string name;public float r,g,b;}
        [System.Serializable] class PaletteFile{public PaletteEntry[] entries;}
        static Dictionary<string,Color> palette;
        static Color Palette(string name,Color fallback)
        {
            if(palette==null){palette=new();var file=Resources.Load<TextAsset>("Bayview/palette");
                if(file!=null)foreach(var e in JsonUtility.FromJson<PaletteFile>(file.text).entries)palette[e.name]=new Color(e.r,e.g,e.b);
                Debug.Log("BAYVIEW_PALETTE entries="+palette.Count);}
            // Longest key first: "GlassLit" must win over "Glass"; imports may add suffixes.
            string best=null;foreach(var key in palette.Keys)if(name.StartsWith(key)&&(best==null||key.Length>best.Length))best=key;
            if(best!=null)return palette[best];
            Debug.LogWarning("BAYVIEW_UNMAPPED_MATERIAL "+name);return fallback;
        }
        public static void Reset()=>materials.Clear();
        public static bool Has(string id)=>Resources.Load<GameObject>("Bayview/"+id)!=null;
        // Moving props live outside the statically batched scenery roots.
        static Transform animated;
        public static Transform AnimatedRoot=>animated!=null?animated:animated=new GameObject("Bayview animated props").transform;
        public static GameObject Spawn(string id,Vector3 position,float yaw,Transform parent,Tint tint=null,string sign=null,bool solid=true,float scale=1)
        {
            var source=Resources.Load<GameObject>("Bayview/"+id);
            if(source==null)throw new System.InvalidOperationException("Missing Bayview kit asset: "+id);
            var go=Object.Instantiate(source,position,Quaternion.Euler(0,yaw,0),parent);go.name=id;tint??=new Tint();if(scale!=1)go.transform.localScale*=scale;
            foreach(var child in go.GetComponentsInChildren<Transform>(true)) {
                if(child.name.StartsWith("Collision")) {
                    var filter=child.GetComponent<MeshFilter>();
                    if(solid&&filter!=null&&filter.sharedMesh!=null){var box=child.gameObject.AddComponent<BoxCollider>();box.center=filter.sharedMesh.bounds.center;box.size=filter.sharedMesh.bounds.size;child.gameObject.tag="Respawn";}
                    Object.Destroy(child.GetComponent<MeshRenderer>());if(filter!=null)Object.Destroy(filter);continue;
                }
                if(child.name.StartsWith("SignAnchor")&&!string.IsNullOrEmpty(sign))Sign(child,sign);
                var renderer=child.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var originals=renderer.sharedMaterials;var converted=new Material[originals.Length];
                for(int i=0;i<originals.Length;i++)converted[i]=Convert(originals[i],tint);
                renderer.sharedMaterials=converted;
                if(child.name.StartsWith("Spin_"))child.gameObject.AddComponent<KitSpin>();
                else child.gameObject.isStatic=parent!=animated;
            }
            return go;
        }
        static Material Convert(Material original,Tint tint)
        {
            string name=original!=null?original.name.Replace(" (Instance)",""):"Unknown";
            Color c=Palette(name,original!=null&&original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):Color.magenta);
            if(name.StartsWith("Tint_Wall"))c=tint.wall;else if(name.StartsWith("Tint_Shutter"))c=tint.shutter;else if(name.StartsWith("Tint_Awning"))c=tint.awning;else if(name.StartsWith("Tint_Door"))c=tint.door;
            c.a=1;string key=name+ColorUtility.ToHtmlStringRGB(c);
            if(materials.TryGetValue(key,out var cached))return cached;
            bool glass=name.StartsWith("Glass"),metal=name.StartsWith("Metal")||name.StartsWith("Gold");
            var m=TrialWorld.Material("Bayview "+name,c,metal?.55f:0,glass?.85f:metal?.55f:name.StartsWith("Water")?.9f:.18f);
            if(name.StartsWith("GlassLit")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*.6f);}
            materials[key]=m;return m;
        }
        static void Sign(Transform anchor,string words)
        {
            var go=new GameObject("Sign / "+words);go.transform.SetParent(anchor,false);go.transform.rotation=anchor.root.rotation*Quaternion.Euler(0,180,0);go.transform.localScale=Vector3.one;
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.font=Toy.Display;text.fontSize=90;text.characterSize=.075f;text.color=Toy.Navy;
            go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }
    }
    // Windmill sails: turn slowly about the axle (the exported object's local forward).
    public sealed class KitSpin:MonoBehaviour{public float degreesPerSecond=28;void Update()=>transform.Rotate(0,0,degreesPerSecond*Time.deltaTime,Space.Self);}
    // Moored boats: gentle bob and roll.
    public sealed class KitBob:MonoBehaviour
    {
        public float phase;Vector3 home;Quaternion rest;bool ready;
        void Update()
        {
            if(!ready){home=transform.position;rest=transform.rotation;ready=true;}
            float t=Time.time*1.3f+phase;
            transform.SetPositionAndRotation(home+Vector3.up*Mathf.Sin(t)*.12f,rest*Quaternion.Euler(Mathf.Sin(t*.8f)*2.5f,0,Mathf.Sin(t*1.1f+1)*3.5f));
        }
    }
}
