using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    public sealed class CoastalCliffs
    {
        readonly CoastalLandscape land;readonly Transform parent;readonly Material material;
        public CoastalCliffs(CoastalLandscape surface,Transform root,Material stone){land=surface;parent=root;material=stone;}
        public void Build()
        {
            // Composition anchors follow the exposed inner slopes and coast.
            // Each formation has a primary bluff and smaller adjoining spurs.
            (Vector2 p,Vector3 size)[] forms={
                (new(-94,-30),new(12,11,10)),(new(-86,-7),new(13,10,12)),(new(-92,25),new(10,12,10)),
                (new(-65,15),new(12,8,11)),(new(-46,-47),new(14,10,9)),(new(-6,-44),new(12,8,10)),
                (new(54,-28),new(14,9,10)),(new(85,-12),new(12,12,10)),(new(67,18),new(12,10,10)),
                (new(-138,-23),new(8,14,12)),(new(-143,35),new(11,17,12)),(new(125,58),new(12,16,13)),
                (new(-92,109),new(14,15,12)),(new(0,107),new(15,18,13)),(new(75,89),new(12,17,12))};
            int accepted=0;
            // Every outcrop's shape is worked out on all cores at once (docs/72: 45 outcrops x 2,624 ring points of terrain
            // queries took ~1.2 s on the main thread), then made here in the original order: the same objects as before.
            var specs=new List<(Vector2 p,Vector3 size,int seed)>();
            for(int i=0;i<forms.Length;i++) {
                specs.Add((forms[i].p,forms[i].size,i*19));
                Vector2 offset=new Vector2(Mathf.Cos(i*2.3f),Mathf.Sin(i*2.3f))*forms[i].size.x*.5f;
                specs.Add((forms[i].p+offset,Vector3.Scale(forms[i].size,new Vector3(.65f,.70f,.8f)),i*19+7));
                specs.Add((forms[i].p-offset*.85f,Vector3.Scale(forms[i].size,new Vector3(.72f,.46f,.7f)),i*19+13));
            }
            CoastalLandscape.Lap("cliffs start");var shapes=new Shape[specs.Count];
            // The rings (2,624 terrain and circuit-clearance samples an outcrop) are cooked once per build and circuit (WorldCache,
            // docs/72): ~3 s of work across the cores, read back as the same floats.
            var store=new float[specs.Count*Stride];string cache="cliff-rings-"+land.TrackKey().ToString("x16");bool cooked=WorldCache.TryRead(cache,store);
            System.Threading.Tasks.Parallel.For(0,specs.Count,k=>shapes[k]=Outcrop(specs[k].p,specs[k].size,specs[k].seed,store,k*Stride,cooked));
            if(!cooked)WorldCache.Write(cache,store);
            CoastalLandscape.Lap("cliffs shapes");for(int k=0;k<shapes.Length;k++)if(shapes[k]!=null){Make(shapes[k],specs[k].seed);accepted++;}
            // (The collision shells cooked on all cores at once, then assigned: docs/72.)
            var ids=shells.ConvertAll(c=>c.m.GetEntityId()).ToArray();System.Threading.Tasks.Parallel.For(0,ids.Length,k=>Physics.BakeMesh(ids[k],false));
            foreach(var (c,m) in shells)c.sharedMesh=m;shells.Clear();CoastalLandscape.Lap("cliffs objects");
            Physics.SyncTransforms();
            var talus=new CoastalTalus(land,parent,material);
            for(int i=0;i<forms.Length;i++)talus.Scatter(forms[i].p,forms[i].size,i*19);
            CoastalLandscape.Lap("cliffs talus");talus.Record();Debug.Log("LITTLE_ROAD_CLIFFS formations="+accepted);
        }
        readonly List<(MeshCollider c,Mesh m)> shells=new();
        sealed class Shape{public Vector3[] vertices,faceted,smoothNormals;public int[] tris,faceIndices;}
        const int Levels=41,Sides=64,Stride=Levels*Sides*3+1;   // (per outcrop in the cache: its rings, then 1 if it stands)
        // `store`: the cooked rings (read when `cooked`, written otherwise) at `at`.
        Shape Outcrop(Vector2 centre,Vector3 size,int seed,float[] store,int at,bool cooked)
        {
            var rings=new Vector3[Levels*Sides];
            if(cooked){if(store[at+Stride-1]==0)return null;for(int k=0;k<rings.Length;k++)rings[k]=new Vector3(store[at+k*3],store[at+k*3+1],store[at+k*3+2]);}
            else{if(!Rings(centre,size,seed,rings)){store[at+Stride-1]=0;return null;}for(int k=0;k<rings.Length;k++){store[at+k*3]=rings[k].x;store[at+k*3+1]=rings[k].y;store[at+k*3+2]=rings[k].z;}store[at+Stride-1]=1;}
            return Surface(rings);
        }
        // Uneven bedding and vertical joints shape the silhouette, with
        // smaller relief continuing down the buried-to-exposed transition.
        // Dense samples prevent a whole slope becoming one stretched face. (False: it would stand on the circuit's verge.)
        bool Rings(Vector2 centre,Vector3 size,int seed,Vector3[] rings)
        {
            float[] heights={0,.18f,.36f,.42f,.48f,.63f,.69f,.75f,.88f,.95f,1};
            float[] widths={1.03f,1.04f,.99f,.96f,.87f,.84f,.80f,.68f,.61f,.48f,.28f};
            const int levels=Levels,sides=Sides;
            float baseHeight=land.Height(centre.x,centre.y)-size.y*.42f;
            float rotation=seed*1.319f;
            for(int ring=0;ring<levels;ring++)for(int side=0;side<sides;side++) {
                float t=ring/(float)(levels-1),angle=side*Mathf.PI*2/sides;
                Vector2 edge=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                // Warping the bed height around the perimeter breaks the
                // repeated horizontal tiers without turning the cap into spikes.
                float strata=Mathf.Clamp01(t+Mathf.Sin(angle*2+seed)*.035f*Mathf.Sin(t*Mathf.PI));
                int bed=0;while(bed<heights.Length-2&&heights[bed+1]<strata)bed++;
                float profile=Mathf.Lerp(widths[bed],widths[bed+1],Mathf.InverseLerp(heights[bed],heights[bed+1],strata));
                float broad=Noise(edge.x*1.7f,edge.y*1.7f,t*.7f,seed);
                float joint=Mathf.Pow(Mathf.Max(0,Mathf.Sin(angle*(3+seed%3)+seed*.71f+t*.65f)),12);
                float shelf=.012f*Mathf.Sin(t*37+edge.x*.7f+seed);
                float radius=profile*(.96f+(broad-.5f)*.3f-joint*.065f+shelf);
                float ex=edge.x*size.x*.64f*radius,ez=edge.y*size.z*.64f*radius;
                float x=centre.x+ex*Mathf.Cos(rotation)-ez*Mathf.Sin(rotation)+Mathf.Sin(seed)*t*size.x*.09f;
                float z=centre.y+ex*Mathf.Sin(rotation)+ez*Mathf.Cos(rotation)+Mathf.Cos(seed)*t*size.z*.08f;
                float relief=(Noise(x*.63f,z*.63f,t*size.y*.63f,seed+3)-.5f)*.66f+
                    (Noise(x*1.8f,z*1.8f,t*size.y*1.8f,seed+11)-.5f)*.16f;
                float outward=Mathf.SmoothStep(0,1,t*6)*relief;
                x+=Mathf.Cos(angle+rotation)*outward;z+=Mathf.Sin(angle+rotation)*outward;
                if(!land.SolidClearance(new Vector3(x,0,z)))return false;
                float bedTilt=edge.x*.055f+edge.y*.025f;
                float y=baseHeight+(t*.82f+bedTilt*t+(broad-.5f)*.11f)*size.y+relief*.3f;
                // Every foot follows the local slope, including the downhill
                // side. A buried centre alone leaves the outer mass hovering.
                // (Above 58% of the height the foot blend is 1: skip the terrain query, same result.)
                float footBlend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.58f,t));
                if(footBlend<1){float buried=Mathf.Min(y,land.Height(x,z)-1.2f);y=Mathf.Lerp(buried,y,footBlend);}
                rings[ring*sides+side]=new Vector3(x,y,z);
            }
            return true;
        }
        // The outcrop's surface from its rings: the shell (shared vertices, for the collider) and the render mesh's corners
        // with normals averaged over neighbouring faces within ~30 degrees. Flat arrays (docs/72: per-vertex lists on every
        // core at once spent ~60 ms an outcrop allocating); the same faces, in the same order, so the same sums.
        static Shape Surface(Vector3[] rings)
        {
            const int levels=Levels,sides=Sides;int last=(levels-1)*sides;
            var vertices=new Vector3[rings.Length+2];System.Array.Copy(rings,vertices,rings.Length);
            var tris=new int[(levels-1)*sides*6+sides*6];int ti=0;
            void Face(int a,int b,int c){tris[ti++]=a;tris[ti++]=b;tris[ti++]=c;}
            for(int ring=0;ring<levels-1;ring++)for(int side=0;side<sides;side++) {
                int a=ring*sides+side,b=ring*sides+(side+1)%sides,c=b+sides,d=a+sides;
                Face(a,d,b);Face(b,d,c);
            }
            Vector3 top=Vector3.zero,bottom=Vector3.zero;
            for(int i=0;i<sides;i++){bottom+=rings[i];top+=rings[last+i];}bottom/=sides;top/=sides;
            int bottomIndex=rings.Length,topIndex=rings.Length+1;vertices[bottomIndex]=bottom;vertices[topIndex]=top;
            for(int i=0;i<sides;i++){Face(bottomIndex,i,(i+1)%sides);Face(topIndex,last+(i+1)%sides,last+i);}
            // Average shallow changes but keep broken ledges and deep joints
            // crisp. Flat-shading every little triangle produced noisy stripes.
            var normals=new Vector3[tris.Length/3];var count=new int[vertices.Length+1];
            for(int i=0;i<tris.Length;i+=3) {
                normals[i/3]=Vector3.Cross(vertices[tris[i+1]]-vertices[tris[i]],vertices[tris[i+2]]-vertices[tris[i]]).normalized;
                for(int j=0;j<3;j++)count[tris[i+j]+1]++;
            }
            for(int v=0;v<vertices.Length;v++)count[v+1]+=count[v];
            var adjacent=new Vector3[tris.Length];var fill=new int[vertices.Length];
            for(int i=0;i<tris.Length;i+=3)for(int j=0;j<3;j++){int v=tris[i+j];adjacent[count[v]+fill[v]++]=normals[i/3];}
            var faceted=new Vector3[tris.Length];var faceIndices=new int[tris.Length];var smoothNormals=new Vector3[tris.Length];
            for(int i=0;i<tris.Length;i++) {
                int index=tris[i];Vector3 average=Vector3.zero,normal=normals[i/3];
                for(int k=count[index];k<count[index+1];k++){var neighbour=adjacent[k];if(Vector3.Dot(normal,neighbour)>.86f)average+=neighbour;}
                faceIndices[i]=i;faceted[i]=vertices[index];smoothNormals[i]=average.normalized;
            }
            return new Shape{vertices=vertices,faceted=faceted,smoothNormals=smoothNormals,tris=tris,faceIndices=faceIndices};
        }
        void Make(Shape shape,int seed)
        {
            var vertices=shape.vertices;var faceted=shape.faceted;var smoothNormals=shape.smoothNormals;var tris=shape.tris;var faceIndices=shape.faceIndices;
            var mesh=new Mesh{name="Weathered fractured limestone "+seed,indexFormat=IndexFormat.UInt32};mesh.SetVertices(faceted);mesh.SetTriangles(faceIndices,0);mesh.SetNormals(smoothNormals);mesh.RecalculateBounds();
            var go=new GameObject("Layered coastal outcrop "+seed,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);go.tag="Respawn";
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;// The collider cooks the shared-vertex shell (same surface, ~6x fewer vertices than the faceted render mesh).
            var shell=new Mesh{name="Outcrop collision "+seed,indexFormat=IndexFormat.UInt32};shell.SetVertices(vertices);shell.SetTriangles(tris,0);shell.RecalculateBounds();
            shells.Add((go.GetComponent<MeshCollider>(),shell));
        }
        static float Noise(float x,float z,float y,int seed)
        {
            float phase=seed*.137f;
            return (Mathf.PerlinNoise(x+phase,z+y*.47f+93)+Mathf.PerlinNoise(z-phase,y+x*.31f+27))* .5f;
        }
    }
}
