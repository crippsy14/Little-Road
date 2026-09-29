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
        // Distance culling by size (WorldLook.CullDistances on the camera): small props stop drawing, and
        // casting shadows, beyond 280 m, medium ones beyond 700 m; buildings and landmarks always draw.
        // Kits with their own LOD (trees' far stand-ins, KitLod) go back to Default.
        public const int DetailLayer=20,MediumLayer=21;
        // World bounds of every fixed kit piece placed this build (flower beds, stalls and benches have no
        // collider): later placements such as parked cars check them so nothing lands on top of a prop.
        public static readonly List<Bounds> Footprints=new();
        public static readonly List<GameObject> Placed=new(); // every fixed piece (PlacementAudit)
        public static bool Occupied(Bounds area)=>Occupied(area,Footprints.Count);
        // Only the first `upTo` footprints: lets a planting pass ignore its own neighbouring rows.
        public static bool Occupied(Bounds area,int upTo){for(int i=0;i<upTo&&i<Footprints.Count;i++)if(Footprints[i].Intersects(area))return true;return false;}
        static void CullLayer(GameObject go)
        {
            if(!WorldLook.DistanceCulling)return;
            var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return;
            var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            float size=Mathf.Max(b.size.x,b.size.y,b.size.z);int layer=size<3.5f?DetailLayer:size<7f?MediumLayer:0;
            if(layer==0)return;
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;
            // A one-level LOD group that dissolves the piece out (dithered, over half a second) at its cull
            // distance; the layer distance just beyond it is a backstop.
            float cull=layer==DetailLayer?WorldLook.DetailCullDistance:WorldLook.MediumCullDistance;
            var group=go.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;
            group.SetLODs(new[]{new LOD(size/(cull*2*Mathf.Tan(27.5f*Mathf.Deg2Rad)),renderers)});group.RecalculateBounds();
        }
        public static GameObject Spawn(string id,Vector3 position,float yaw,Transform parent,Tint tint=null,string sign=null,bool solid=true,float scale=1)
        {
            var source=Resources.Load<GameObject>("Bayview/"+id);
            if(source==null)throw new System.InvalidOperationException("Missing Bayview kit asset: "+id);
            var go=Object.Instantiate(source,position,Quaternion.Euler(0,yaw,0),parent);go.name=id;tint??=new Tint();if(scale!=1)go.transform.localScale*=scale;
            if(id=="lamp_post")NightLights.Lamp(go.transform);
            foreach(var child in go.GetComponentsInChildren<Transform>(true)) {
                if(child.name.StartsWith("Collision")) {
                    var filter=child.GetComponent<MeshFilter>();
                    if(solid&&filter!=null&&filter.sharedMesh!=null){var box=child.gameObject.AddComponent<BoxCollider>();box.center=filter.sharedMesh.bounds.center;box.size=filter.sharedMesh.bounds.size;child.gameObject.tag="Respawn";}
                    Object.Destroy(child.GetComponent<MeshRenderer>());if(filter!=null)Object.Destroy(filter);continue;
                }
                if(child.name.StartsWith("SignAnchor")&&!string.IsNullOrEmpty(sign)){string words=SignFor(child.name,sign);if(words!=null)Sign(child,words,go.transform);}
                var renderer=child.GetComponent<MeshRenderer>();if(renderer==null)continue;
                var originals=renderer.sharedMaterials;var converted=new Material[originals.Length];
                bool modelled=ModelledMasonry.Contains(id),aged=AgedStone.Contains(id);
                for(int i=0;i<originals.Length;i++)converted[i]=Convert(originals[i],tint,modelled,aged);
                renderer.sharedMaterials=converted;
                if(child.name.StartsWith("Spin_")){var spin=child.gameObject.AddComponent<KitSpin>();spin.axle=child.name.StartsWith("Spin_X")?Vector3.right:Vector3.forward;spin.kit=go.transform;}
                else child.gameObject.isStatic=parent!=animated;
            }
            CullLayer(go);
            if(parent!=animated){var rs=go.GetComponentsInChildren<Renderer>();if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);Footprints.Add(b);}Placed.Add(go);}
            return go;
        }
        // Kits whose stone courses are modelled in the mesh: their stone takes the natural rock
        // surface so the shader's courses don't double up on the modelled ones.
        static readonly HashSet<string> ModelledMasonry=new(){"sea_wall","harbour_steps","headland_gate","rail_gate"};
        // Old stone buildings: greyer, darker stone with heavier weathering (stains, moss at the foot).
        static readonly HashSet<string> AgedStone=new(){"castle_tower","castle_wall","castle_gatehouse","castle_keep","church"};
        static Material Convert(Material original,Tint tint,bool modelledMasonry=false,bool agedStone=false)
        {
            string name=original!=null?original.name.Replace(" (Instance)",""):"Unknown";
            Color c=Palette(name,original!=null&&original.HasProperty("_BaseColor")?original.GetColor("_BaseColor"):Color.magenta);
            if(name.StartsWith("Tint_Wall"))c=tint.wall;else if(name.StartsWith("Tint_Shutter"))c=tint.shutter;else if(name.StartsWith("Tint_Awning"))c=tint.awning;else if(name.StartsWith("Tint_Door"))c=tint.door;
            c.a=1;var pattern=WorldLook.PatternFor(name);if(modelledMasonry&&pattern==WorldLook.Pattern.Stone)pattern=WorldLook.Pattern.Rock;
            bool aged=agedStone&&pattern==WorldLook.Pattern.Stone;if(aged)c=Color.Lerp(c,new Color(.60f,.60f,.57f),.4f)*.86f;
            string key=name+ColorUtility.ToHtmlStringRGB(c)+(int)pattern+(aged?"aged":"");
            if(materials.TryGetValue(key,out var cached))return cached;
            bool glass=name.StartsWith("Glass"),metal=name.StartsWith("Metal")||name.StartsWith("Gold");
            float smooth=glass?.85f:metal?.55f:name.StartsWith("Water")?.9f:.18f;
            // Lit glass and water keep URP Lit (emission, reflections); everything else gets a
            // surface pattern from the kit shader (see WorldLook.PatternFor).
            Material m;
            if(name.StartsWith("GlassLit")||name.StartsWith("Water")){m=TrialWorld.Material("Bayview "+name,c,metal?.55f:0,smooth);if(name.StartsWith("GlassLit")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*.6f);NightLights.Window(m,c);}}
            else m=WorldLook.Kit("Bayview "+name,c,pattern,metal?.55f:0,smooth,false,aged?2.2f:1);
            materials[key]=m;return m;
        }
        // One text for every anchor, or per-anchor text as "kind=TEXT|kind=TEXT" (kind: the anchor
        // name after its last '_'; anchors with no entry stay blank). Used by the Pit Stop bays.
        static string SignFor(string anchor,string sign)
        {
            if(!sign.Contains("="))return sign;
            string kind=anchor.Substring(anchor.LastIndexOf('_')+1);
            foreach(var pair in sign.Split('|')){int eq=pair.IndexOf('=');if(eq>0&&pair.Substring(0,eq)==kind)return pair.Substring(eq+1);}
            return null;
        }
        static void Sign(Transform anchor,string words,Transform building)
        {
            var go=new GameObject("Sign / "+words);go.transform.SetParent(anchor,false);go.transform.rotation=building.rotation*Quaternion.Euler(0,anchor.name.EndsWith("back")?0:180,0); // "...back" anchors face the rear
            // Lettering sized to its board: small viewpoint maps and track boards, big tunnel and stand banners.
            string kind=anchor.name.Substring(anchor.name.LastIndexOf('_')+1);
            go.transform.localScale=Vector3.one*(kind switch{"brand"=>1.1f,"garage"=>.95f,"parts"=>.95f,"body"=>.95f,"map"=>.3f,"board"=>.62f,"portal"=>2.1f,"railway"=>1.35f,"stand"=>1.7f,"standback"=>1.15f,"paddockback"=>.85f,"control"=>.95f,_ when kind.StartsWith("bay")=>.78f,"gate"=>words.Length>12?.9f:1.2f,_=>1f});
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;
            text.font=Toy.Display;text.fontSize=90;text.characterSize=.075f;text.color=kind=="brand"?Toy.White:Toy.Navy;
            go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
        }
    }
    // Windmill sails: turn slowly about the axle (the exported object's local forward).
    // The axle is given in the kit's own space (Blender Y -> Unity Z, X -> X, from the "Spin_<axis>" name):
    // the imported object's local axes don't reliably keep it, and spinning about local Z turned sails
    // and the Pit Stop tyres flat, through their towers.
    public sealed class KitSpin:MonoBehaviour
    {
        public float degreesPerSecond=28;public Vector3 axle=Vector3.forward;public Transform kit;
        void Update()=>Step(Time.deltaTime);
        public void Step(float seconds)=>transform.RotateAround(transform.position,(kit!=null?kit:transform.parent!=null?transform.parent:transform).TransformDirection(axle),degreesPerSecond*seconds);
    }
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
