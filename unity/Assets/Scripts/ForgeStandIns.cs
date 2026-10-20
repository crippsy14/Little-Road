using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge stand-ins (stage two, docs/78), behind --prop-forge: a Forge kit that declares `replaces` stands in for
    // game kit pieces by id. BayviewKit.Spawn asks here first; a stand-in comes back as a source prefab built the way the
    // Blender kit's are (a LOD0 and a LOD1 renderer under a LOD group, materials named by the game's palette, a Collision
    // part), so the kit's own machinery does the rest unchanged: tints, roof colours, patterns, night windows, colliders,
    // footprints, culling and static batching. Every existing placement in every town takes the new model; no layout code
    // changes, and with the flag off nothing here runs.
    public static class ForgeStandIns
    {
        sealed class Entry{public string kit;public string[] pieces;}
        static Dictionary<string,Entry> map;
        static readonly Dictionary<string,GameObject> sources=new();static Transform holder;
        static readonly Dictionary<string,Material> placeholders=new();

        static void Load()
        {
            if(map!=null)return;map=new();
            foreach(var t in Resources.LoadAll<TextAsset>("Props")) {
                if(t.name.EndsWith("_mesh")||t.name=="prop_manifest")continue;
                var kit=PropKit.Load(t.name);if(kit.file.replaces==null)continue;
                foreach(var r in kit.file.replaces)map[r.id]=new Entry{kit=t.name,pieces=r.pieces};
            }
        }
        // (--new-bayview: its landmarks (the landmarks kit) are Forge models with no Blender original, so they stand in
        //  with or without --prop-forge)
        public static bool Has(string id){if(!PropForge.Enabled&&!NewBayview.On)return false;Load();return map.TryGetValue(id,out var e)&&(PropForge.Enabled||e.kit=="landmarks");}
        public static IEnumerable<string> Ids{get{Load();return map.Keys;}}
        // Where a stand-in's chimney smokes (its Chimney socket, docs/80): the variant standing at `at`, turned by `yaw`.
        // Otherwise `kit` (the Blender kit's chimney, from the caller's table), turned the same way.
        public static Vector3 ChimneyOr(string id,Vector3 at,float yaw,Vector3 kit,bool shop=false)
        {
            var rot=Quaternion.Euler(0,yaw,0);
            if(Has(id)) {
                var e=map[id];string piece=Variant(id,e,at,shop);
                var p=PropKit.Load(e.kit).Piece(piece);
                if(p.sockets!=null)foreach(var s in p.sockets)if(s.name=="Chimney"){var v=s.at.V;return at+rot*new Vector3(-v.x,v.y,-v.z);}   // (turned half round, as the model is)
            }
            return at+rot*kit;
        }
        // The variant for a placement: a hash of where it stands (the same piece every build), and its source prefab. key
        // names the template (one per variant).
        // (terrace_b is two shops and two houses, docs/80 round 4: a placement with a shop's name takes a shop, one without a
        //  house, so no name hangs under a balcony and no fascia stands empty: street review QH2-02, QH2-06)
        static string Variant(string id,Entry e,Vector3 at,bool shop)
        {
            int h=(Mathf.RoundToInt(at.x*10)*73856093)^(Mathf.RoundToInt(at.z*10)*19349663)^id.GetHashCode();h&=0x7fffffff;
            if(id=="terrace_b"&&e.pieces.Length==4)return e.pieces[(shop?0:2)+h%2];
            return e.pieces[h%e.pieces.Length];
        }
        public static GameObject Source(string id,Vector3 at,out string key,bool shop=false)
        {
            Load();var e=map[id];string piece=Variant(id,e,at,shop);key=id+"@"+piece;
            if(sources.TryGetValue(key,out var go)&&go!=null)return go;
            return sources[key]=Build(PropKit.Load(e.kit),piece,id);
        }
        static GameObject Build(PropKit kit,string piece,string id)
        {
            if(holder==null){var h=new GameObject("Forge stand-ins");h.SetActive(false);Object.DontDestroyOnLoad(h);holder=h.transform;}
            var root=new GameObject(id);root.transform.SetParent(holder,false);var p=kit.Piece(piece);var lods=new List<LOD>();
            // (turned half round: the Blender kit's fronts face -y as the Forge's do, and Unity's FBX import turns them to +z;
            //  the Forge's mesh data comes in unturned. Until 19 Oct every stand-in faced backwards: houses showed their
            //  backs to the road.)
            var turned=new GameObject("Turned").transform;turned.SetParent(root.transform,false);turned.localRotation=Quaternion.Euler(0,180,0);
            int count=Mathf.Min(2,p.lods.Length);
            for(int lod=0;lod<count;lod++) {
                var go=new GameObject("LOD"+lod,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(turned,false);
                go.GetComponent<MeshFilter>().sharedMesh=kit.Mesh(piece,lod);var subs=p.lods[lod].submeshes;var mats=new Material[subs.Length];
                for(int s=0;s<subs.Length;s++)mats[s]=Placeholder(subs[s].material);
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterials=mats;lods.Add(new LOD(lod==0?.09f:.008f,new Renderer[]{r}));
            }
            root.AddComponent<LODGroup>().SetLODs(lods.ToArray());
            // (sign sockets as the Blender kit's SignAnchor empties: BayviewKit letters a shop's name onto them; docs/80)
            if(p.sockets!=null)foreach(var s in p.sockets)if(s.name!=null&&s.name.StartsWith("SignAnchor")) {
                var a=new GameObject(s.name).transform;a.SetParent(turned,false);a.localPosition=s.at.V;
                if(piece.StartsWith("terrace_"))a.localScale=Vector3.one*.8f;   // (a terrace's name fits its fascia, under the floor above: QH2-02)
            }
            if(p.collision!=null&&p.collision.size!=null&&p.collision.size.V!=Vector3.zero) {   // (the kit's Collision part: a box BayviewKit reads by its bounds)
                var c=new GameObject("Collision",typeof(MeshFilter),typeof(MeshRenderer));c.transform.SetParent(turned,false);
                c.GetComponent<MeshFilter>().sharedMesh=Box(p.collision.centre.V,p.collision.size.V);
                if(p.collision.more!=null)for(int k=0;k<p.collision.more.Length;k++) {   // (a gatehouse: each tower and the block over the arch, the road clear through it)
                    var m=new GameObject("Collision "+(k+2),typeof(MeshFilter),typeof(MeshRenderer));m.transform.SetParent(turned,false);
                    m.GetComponent<MeshFilter>().sharedMesh=Box(p.collision.more[k].centre.V,p.collision.more[k].size.V);
                }
            }
            return root;
        }
        // (a material by palette name only: BayviewKit.Convert reads the name and gives it the game's look and tint)
        static Material Placeholder(string name)
        {
            if(placeholders.TryGetValue(name,out var m))return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};return placeholders[name]=m;
        }
        static Mesh Box(Vector3 c,Vector3 s)
        {
            var h=s*.5f;var v=new List<Vector3>();foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})v.Add(c+Vector3.Scale(h,new Vector3(x,y,z)));
            var m=new Mesh{name="Forge collision box"};m.SetVertices(v);m.SetTriangles(new[]{0,1,2,1,3,2,4,6,5,5,6,7,0,4,1,1,4,5,2,3,6,3,7,6,0,2,4,2,6,4,1,5,3,3,5,7},0);m.RecalculateBounds();return m;
        }
    }
}
