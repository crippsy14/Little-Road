using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Far-away stand-ins for kit trees and bushes: one low canopy (lumpy ellipsoid, or a cone for
    // conifers) and a trunk, in the kit's own leaf and bark materials, behind an LODGroup. Lets the
    // valley carry dense woods and hedgerows without paying for full models at a distance.
    public static class KitLod
    {
        static readonly Dictionary<string,(Mesh canopy,Mesh trunk,Material leaf,Material bark)> proxies=new();
        public const float NearScreen=.035f,FarScreen=.0015f;
        public static void Attach(GameObject go,string id)
        {
            var renderers=go.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)return;
            if(!proxies.TryGetValue(id,out var p)){p=Build(go,renderers,id);proxies[id]=p;}
            if(p.canopy==null)return;
            // The LOD group handles distance itself: undo BayviewKit's size-based cull layer and fade group.
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
            var sizeGroup=go.GetComponent<LODGroup>();if(sizeGroup!=null)UnityEngine.Object.DestroyImmediate(sizeGroup);
            var lod=new GameObject("Far stand-in");lod.transform.SetParent(go.transform,false);
            var parts=new List<Renderer>();
            void Part(Mesh mesh,Material mat){if(mesh==null||mat==null)return;var o=new GameObject("Stand-in",typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(lod.transform,false);o.GetComponent<MeshFilter>().sharedMesh=mesh;var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;o.isStatic=true;parts.Add(r);}
            Part(p.canopy,p.leaf);Part(p.trunk,p.bark);
            var treeGroup=go.AddComponent<LODGroup>();treeGroup.fadeMode=LODFadeMode.CrossFade;treeGroup.animateCrossFading=true;// (a Prop Forge stand-in brings its own LOD0 and LOD1: near, middle, then the far stand-in; both at once would draw twice)
            var l0=go.transform.Find("LOD0");var l1=go.transform.Find("LOD1");
            if(l0!=null&&l1!=null)treeGroup.SetLODs(new[]{new LOD(.09f,new Renderer[]{l0.GetComponent<MeshRenderer>()}),new LOD(NearScreen,new Renderer[]{l1.GetComponent<MeshRenderer>()}),new LOD(FarScreen,parts.ToArray())});
            else treeGroup.SetLODs(new[]{new LOD(NearScreen,renderers),new LOD(FarScreen,parts.ToArray())});
        }
        static bool Leafy(string n)=>n.Contains("Leaf")||n.Contains("Fir")||n.Contains("Poplar")||n.Contains("Blossom")||n.Contains("Autumn")||n.Contains("Moss");
        static (Mesh,Mesh,Material,Material) Build(GameObject go,MeshRenderer[] renderers,string id)
        {
            Material leaf=null,bark=null;var inv=go.transform.worldToLocalMatrix;Bounds b=default;bool any=false;
            foreach(var r in renderers) {
                foreach(var m in r.sharedMaterials){if(m==null)continue;if(leaf==null&&Leafy(m.name))leaf=m;if(bark==null&&(m.name.Contains("Bark")||m.name.Contains("Wood")))bark=m;}
                var mf=r.GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null)continue;
                var mb=mf.sharedMesh.bounds;var m2=inv*r.transform.localToWorldMatrix;
                foreach(var c in new[]{new Vector3(-1,-1,-1),new Vector3(1,1,1),new Vector3(-1,1,-1),new Vector3(1,-1,1)}){var q=m2.MultiplyPoint3x4(mb.center+Vector3.Scale(mb.extents,c));if(!any){b=new Bounds(q,Vector3.zero);any=true;}else b.Encapsulate(q);}
            }
            if(leaf==null||!any)return (null,null,null,null);
            float height=b.max.y,crownBase=height*(id.Contains("pine")?.72f:id.Contains("cypress")||id.Contains("poplar")?.12f:id.Contains("fir")?.18f:.38f);
            var centre=new Vector3(b.center.x,(crownBase+height)*.5f,b.center.z);var radii=new Vector3(b.extents.x*.9f,(height-crownBase)*.5f,b.extents.z*.9f);
            var canopy=id.Contains("fir")?Cone(new Vector3(b.center.x,crownBase,b.center.z),Mathf.Max(radii.x,radii.z),height-crownBase):Blob(centre,radii,id.GetHashCode());
            Mesh trunk=bark!=null&&crownBase>.4f?Cone(new Vector3(b.center.x,0,b.center.z),Mathf.Max(.12f,height*.03f),crownBase+.3f,6,true):null;
            return (canopy,trunk,leaf,bark);
        }
        // Icosphere (80 faces) pushed about by noise so the silhouette isn't a clean ball.
        static Mesh Blob(Vector3 centre,Vector3 radii,int seed)
        {
            float t=(1+Mathf.Sqrt(5))/2;var v=new List<Vector3>{new(-1,t,0),new(1,t,0),new(-1,-t,0),new(1,-t,0),new(0,-1,t),new(0,1,t),new(0,-1,-t),new(0,1,-t),new(t,0,-1),new(t,0,1),new(-t,0,-1),new(-t,0,1)};
            var f=new List<int>{0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            var mid=new Dictionary<long,int>();
            int Mid(int a,int b2){long k=a<b2?((long)a<<32)|(uint)b2:((long)b2<<32)|(uint)a;if(mid.TryGetValue(k,out int i))return i;v.Add((v[a]+v[b2])*.5f);mid[k]=v.Count-1;return v.Count-1;}
            var g=new List<int>();for(int i=0;i<f.Count;i+=3){int a=f[i],b2=f[i+1],c=f[i+2],ab=Mid(a,b2),bc=Mid(b2,c),ca=Mid(c,a);g.AddRange(new[]{a,ab,ca,b2,bc,ab,c,ca,bc,ab,bc,ca});}
            float o=(seed&255)*.37f;
            for(int i=0;i<v.Count;i++){var n=v[i].normalized;float bump=.86f+.28f*Mathf.PerlinNoise(n.x*2.1f+o,n.z*2.1f+n.y*1.3f+o);v[i]=centre+Vector3.Scale(n*bump,radii);}
            var mesh=new Mesh{name="Canopy stand-in"};mesh.SetVertices(v);mesh.SetTriangles(g,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Mesh Cone(Vector3 baseCentre,float radius,float height,int sides=10,bool open=false)
        {
            var v=new List<Vector3>{baseCentre+Vector3.up*height};var t=new List<int>();
            for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides;v.Add(baseCentre+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius);}
            for(int k=0;k<sides;k++){t.Add(0);t.Add(1+(k+1)%sides);t.Add(1+k);}
            if(!open){v.Add(baseCentre);for(int k=0;k<sides;k++){t.Add(v.Count-1);t.Add(1+k);t.Add(1+(k+1)%sides);}}
            var mesh=new Mesh{name="Cone stand-in"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
