using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Original mesh recipes. Shared species/variant meshes keep the leaf-rich
    // near silhouettes affordable; distant crowns use a separate mesh LOD.
    public sealed class CoastalVegetation
    {
        readonly Material bark;
        readonly Material[][] leaves=new Material[3][];
        readonly Dictionary<int,TreeMesh> trees=new();
        readonly Dictionary<int,TreeMesh> shrubs=new();
        readonly Material[] flowers;
        sealed class TreeMesh { public Mesh wood,near,far,bloom; }
        sealed class Geometry
        {
            public readonly List<Vector3> v=new();
            public readonly List<Vector2> uv=new();
            public readonly List<int>[] t={new(),new(),new()};
            public int Vertex(Vector3 p,Vector2 tex){int i=v.Count;v.Add(p);uv.Add(tex);return i;}
            public void Tri(int a,int b,int c,int material=0){t[material].Add(a);t[material].Add(b);t[material].Add(c);}
            public Mesh Finish(string name,int count)
            {
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.subMeshCount=count;
                for(int i=0;i<count;i++)mesh.SetTriangles(t[i],i);
                mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
        }
        public CoastalVegetation()
        {
            bark=TrialWorld.Material("Warm weathered branch bark",new Color(.29f,.25f,.19f),0,.12f);
            flowers=new[]{TrialWorld.Material("Rockrose warm ivory",new Color(.82f,.77f,.61f),0,.15f),
                TrialWorld.Material("Rockrose soft rose",new Color(.64f,.37f,.48f),0,.15f),
                TrialWorld.Material("Rockrose ochre centre",new Color(.70f,.51f,.17f),0,.1f)};
            Color[] baseColours={new(.22f,.34f,.14f),new(.36f,.44f,.25f),new(.29f,.39f,.15f)};
            for(int species=0;species<3;species++) {
                leaves[species]=new Material[3];
                for(int tone=0;tone<3;tone++) {
                    var material=new Material(Shader.Find("LittleRoad/CoastalFoliage")){name="Coast leaf canopy "+species+" tone "+tone,enableInstancing=true};
                    material.SetColor("_BaseColor",baseColours[species]*Mathf.Lerp(.86f,1.14f,tone*.5f));material.SetFloat("_Cull",0);leaves[species][tone]=material;
                }
            }
        }
        static float Range(System.Random rng,float low,float high)=>Mathf.Lerp(low,high,(float)rng.NextDouble());
        static Vector3 Unit(System.Random rng)
        {
            float y=Range(rng,-1,1),angle=Range(rng,0,Mathf.PI*2),radius=Mathf.Sqrt(1-y*y);
            return new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius);
        }
        static void Branch(Geometry g,Vector3 start,Vector3 end,float r0,float r1)
        {
            const int sides=9;Vector3 axis=(end-start).normalized;
            Vector3 right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized,up=Vector3.Cross(axis,right);
            int first=g.v.Count;
            for(int ring=0;ring<2;ring++)for(int side=0;side<=sides;side++) {
                float angle=side*Mathf.PI*2/sides;Vector3 normal=right*Mathf.Cos(angle)+up*Mathf.Sin(angle);
                g.Vertex((ring==0?start:end)+normal*(ring==0?r0:r1),new Vector2(side/(float)sides,ring*Vector3.Distance(start,end)));
            }
            for(int side=0;side<sides;side++) {int a=first+side,b=a+sides+1;g.Tri(a,a+1,b);g.Tri(a+1,b+1,b);}
            int bottom=g.Vertex(start,Vector2.zero),top=g.Vertex(end,Vector2.one);
            for(int side=0;side<sides;side++){g.Tri(bottom,first+side+1,first+side);g.Tri(top,first+sides+1+side,first+sides+2+side);}
        }
        static void Leaf(Geometry g,Vector3 p,Vector3 direction,Vector3 normal,float length,float width,int tone,bool duplicateBack=true)
        {
            direction.Normalize();Vector3 side=Vector3.Cross(normal,direction).normalized;
            if(side.sqrMagnitude<.5f)side=Vector3.Cross(Vector3.right,direction).normalized;
            normal=Vector3.Cross(direction,side).normalized;
            // Lit flower petals retain their back mesh. Foliage uses the same
            // outline once, with face-oriented normals in its two-sided shader.
            Vector3[] points={p-direction*length*.5f,p+side*width*.5f,p+normal*width*.12f,p-side*width*.5f,p+direction*length*.5f};
            foreach(bool reverse in new[]{false,true}) {
                if(reverse&&!duplicateBack)break;
                int start=g.v.Count;foreach(var point in points)g.Vertex(point,Vector2.zero);
                void Face(int a,int b,int c){if(reverse)g.Tri(start+a,start+c,start+b,tone);else g.Tri(start+a,start+b,start+c,tone);}
                Face(0,1,2);Face(1,4,2);Face(4,3,2);Face(3,0,2);
            }
        }
        static void Crown(Geometry far,Vector3 centre,Vector3 size,System.Random rng,int tone)
        {
            const int rings=7,sides=10;int start=far.v.Count;
            for(int ring=0;ring<=rings;ring++)for(int side=0;side<=sides;side++) {
                float phi=ring*Mathf.PI/rings,theta=side*Mathf.PI*2/sides;
                Vector3 d=new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta));
                float irregular=1+.1f*Mathf.Sin(theta*3+centre.x)+.05f*Mathf.Sin(phi*5+centre.z);
                far.Vertex(centre+Vector3.Scale(d,size)*irregular,new Vector2(side/(float)sides,ring/(float)rings));
            }
            for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++) {int a=start+ring*(sides+1)+side,b=a+sides+1;far.Tri(a,a+1,b,tone);far.Tri(a+1,b+1,b,tone);}
        }
        static void Foliage(Geometry near,Geometry far,Vector3 centre,Vector3 size,int species,System.Random rng,int leafCount=0)
        {
            // The old large opaque core dominated the leaves and read as a row
            // of balls. Small interior growth plus dense outer blades gives a
            // porous canopy; the distant mesh retains representative sprays.
            Crown(far,centre,size*.38f,rng,0);
            int count=species==0?230:species==1?620:680;
            if(leafCount>0)count=leafCount;
            for(int i=0;i<count;i++) {
                Vector3 direction=Unit(rng);Vector3 p=centre+Vector3.Scale(direction,size)*Mathf.Pow(Range(rng,.06f,1),1f/3);
                Vector3 blade=species==0?(direction+Vector3.up*.8f).normalized:Unit(rng);
                float length=species==0?Range(rng,.22f,.38f):species==1?Range(rng,.13f,.24f):Range(rng,.22f,.40f);
                float width=species==1?length*.36f:length*.45f;
                Leaf(near,p,blade,(Vector3.up*.8f+direction).normalized,length,width,rng.Next(3),false);
                // Consume the original random draws so branch/near-leaf poses
                // remain identical when changing the distant representation.
                if(i%4==0) {
                    int farTone=rng.Next(3);
                    if(i%8==0)Leaf(far,p,blade,(Vector3.up*.8f+direction).normalized,length*2.5f,width*2.5f,farTone,false);
                }
            }
        }
        TreeMesh Build(int species,int variant)
        {
            var rng=new System.Random(8449+species*377+variant*37);var wood=new Geometry();var near=new Geometry();var far=new Geometry();
            Vector3 lean=new Vector3(Range(rng,-.3f,.3f),0,Range(rng,-.3f,.3f));
            Vector3 fork=Vector3.up*(species==0?5:species==1?2.2f:5.2f)+lean;
            Branch(wood,new Vector3(0,-.15f,0),fork,species==0?.20f:.28f,.12f);
            for(int root=0;root<5;root++){float a=root*Mathf.PI*2/5;Branch(wood,new Vector3(Mathf.Cos(a)*.7f,-.05f,Mathf.Sin(a)*.7f),Vector3.up*.7f,.10f,.16f);}
            if(species==0) {
                Branch(wood,fork,new Vector3(lean.x*.3f,8.8f,lean.z*.3f),.13f,.025f);
                for(int level=0;level<11;level++)for(int side=0;side<5;side++) {
                    float y=1.6f+level*.63f,angle=side*Mathf.PI*.4f+level*.71f+variant;
                    float radius=(1-level/13f)*Range(rng,.65f,.95f);
                    Vector3 start=new Vector3(lean.x*.5f,y,lean.z*.5f),tip=start+new Vector3(Mathf.Cos(angle)*radius,.5f,Mathf.Sin(angle)*radius);
                    Branch(wood,start,tip,.035f,.012f);Foliage(near,far,tip,new Vector3(.42f,.71f,.42f)*(1-level*.035f),species,rng);
                }
            } else {
                int arms=species==1?7:9;
                for(int arm=0;arm<arms;arm++) {
                    float angle=arm*Mathf.PI*2/arms+Range(rng,-.2f,.2f),reach=species==1?Range(rng,1.1f,2.0f):Range(rng,2,3.3f);
                    Vector3 tip=fork+new Vector3(Mathf.Cos(angle)*reach,Range(rng,.5f,2.0f),Mathf.Sin(angle)*reach);
                    Vector3 bend=Vector3.Lerp(fork,tip,.48f)+new Vector3(Range(rng,-.24f,.24f),Range(rng,-.25f,.20f),Range(rng,-.24f,.24f));
                    Branch(wood,fork,bend,.12f,.08f);Branch(wood,bend,tip,.08f,.045f);
                    for(int twig=0;twig<3;twig++) {
                        Vector3 end=tip+new Vector3(Range(rng,-.65f,.65f),Range(rng,.25f,.85f),Range(rng,-.65f,.65f));
                        Branch(wood,tip,end,.045f,.012f);
                        Vector3 size=species==1?new Vector3(1.0f,.95f,1.0f):new Vector3(1.35f,.90f,1.25f);
                        Foliage(near,far,end,size*Range(rng,.85f,1.12f),species,rng);
                    }
                }
            }
            Debug.Log("LITTLE_ROAD_TREE_MESH species="+species+" variant="+variant+" nearVertices="+near.v.Count+" farVertices="+far.v.Count);
            return new TreeMesh{wood=wood.Finish("Branched coast tree "+species+"/"+variant,1),near=near.Finish("Individual folded foliage "+species+"/"+variant,3),far=far.Finish("Distant clustered foliage "+species+"/"+variant,3)};
        }
        static MeshRenderer MeshPart(Transform parent,string name,Mesh mesh,Material[] materials)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;return renderer;
        }
        public void Plant(Transform parent,Vector3 position,float scale,int species,int variant)
        {
            variant%=3;int key=species*3+variant;if(!trees.TryGetValue(key,out var tree)){tree=Build(species,variant);trees.Add(key,tree);}
            var root=new GameObject(species==0?"Mediterranean cypress":species==1?"Coastal olive":"Umbrella pine");root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,variant*107+position.x*11,0));root.transform.localScale=Vector3.one*scale;
            var trunk=MeshPart(root.transform,"Visible branching trunk",tree.wood,new[]{bark});trunk.gameObject.tag="Respawn";trunk.gameObject.AddComponent<MeshCollider>().sharedMesh=tree.wood;
            var high=MeshPart(root.transform,"Near individual leaves",tree.near,leaves[species]);var low=MeshPart(root.transform,"Distant crown clusters",tree.far,leaves[species]);
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,new Renderer[]{trunk,high}),new LOD(.006f,new Renderer[]{trunk,low})});lod.RecalculateBounds();
        }
        TreeMesh BuildScrub(int variant,bool garden)
        {
            var rng=new System.Random(46113+variant*79);var wood=new Geometry();var near=new Geometry();var far=new Geometry();var bloom=new Geometry();
            for(int arm=0;arm<7;arm++) {
                float angle=arm*Mathf.PI*2/7+Range(rng,-.22f,.22f);
                Vector3 tip=new Vector3(Mathf.Cos(angle)*Range(rng,.25f,.55f),Range(rng,.22f,.52f),Mathf.Sin(angle)*Range(rng,.25f,.55f));
                Branch(wood,new Vector3(0,-.12f,0),tip,.035f,.009f);
                Foliage(near,far,tip,new Vector3(.39f,.28f,.38f)*Range(rng,.8f,1.15f),1,rng,70);
                if(variant==0)continue;
                for(int flower=0;flower<3;flower++) {
                    Vector3 centre=tip+new Vector3(Range(rng,-.25f,.25f),Range(rng,.18f,.28f),Range(rng,-.25f,.25f));
                    for(int petal=0;petal<5;petal++) {
                        float a=petal*Mathf.PI*.4f;Vector3 direction=new Vector3(Mathf.Cos(a),.2f,Mathf.Sin(a));
                        Leaf(bloom,centre+direction*(garden?.065f:.04f),direction,Vector3.up,garden?.13f:.08f,garden?.085f:.055f,variant-1);
                    }
                    Leaf(bloom,centre+Vector3.up*.01f,Vector3.right,Vector3.up,.035f,.035f,2);
                }
            }
            return new TreeMesh{wood=wood.Finish("Low scrub branching stems "+variant,1),near=near.Finish("Silver-green scrub leaves "+variant,3),
                far=far.Finish("Low scrub distant clusters "+variant,3),bloom=bloom.Finish("Rockrose petals "+variant,3)};
        }
        public void PlantScrub(Transform parent,Vector3 position,float scale,int variant,float rotation,bool garden=false)
        {
            variant%=3;int key=variant+(garden?3:0);if(!shrubs.TryGetValue(key,out var shrub)){shrub=BuildScrub(variant,garden);shrubs.Add(key,shrub);}
            var root=new GameObject("Coastal scrub / rockrose "+variant);root.transform.SetParent(parent,false);
            root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,rotation,0));root.transform.localScale=Vector3.one*scale;
            // Soft roadside planting, kept outside the drivable/recovery corridor.
            var stems=MeshPart(root.transform,"Scrub stems",shrub.wood,new[]{bark});
            var high=MeshPart(root.transform,"Scrub folded leaves",shrub.near,leaves[1]);var low=MeshPart(root.transform,"Scrub distant canopy",shrub.far,leaves[1]);
            var petals=MeshPart(root.transform,"Rockrose flowers",shrub.bloom,flowers);
            var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.045f,new Renderer[]{stems,high,petals}),new LOD(.009f,new Renderer[]{low,petals})});lod.RecalculateBounds();
        }
    }
}
