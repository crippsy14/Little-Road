using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Builds Hillcrest Valley from ValleyField / HillcrestRoute: LOD'd terrain chunks, the
    // causeway viaduct, Headland Tunnel, valley roads and bridges, river, falls, lake and the
    // snow-capped mountain ring that gives the mainland its sense of scale.
    public sealed partial class CoastalLandscape
    {
        Transform valley;Material mainlandGround;
        public float MainlandHeight(float x,float z)=>ValleyField.Height(x,z);
        static bool NearCauseway(Vector3 p)=>HillcrestRoute.Distance(HillcrestRoute.Causeway,p.x,p.z,out _)<10||HillcrestRoute.OnHaltQuay(p,4);
        // Island side: keep terrain at or below the deck along the causeway's first stretch.
        float CausewayCut(float x,float z,float height)
        {
            float d=HillcrestRoute.Distance(HillcrestRoute.Causeway,x,z,out float deck);
            // (--new-bayview: the mainline starts up on the island's start straight; north of the Halt quay the ground
            //  follows its deck, not sea level: docs/80 round 1)
            float top=NewBayview.On&&z>HillcrestRoute.QuayNorth?deck:Mathf.Min(deck,0);
            return Mathf.Lerp(height,Mathf.Min(height,top-.08f),1-Blend(6,14,d));
        }
        // Valley-scale ground colour, baked once (768x548, ~3.3 m a texel) so the valley reads as
        // countryside instead of one even green: hedged field parcels on the flatter low ground, dark
        // woodland floor round the copses (the same noise MainlandTrees plants) and on steep banks, and
        // drier sun-bleached grass up on the high slopes. RGB multiplies the toy grass, so each crop is
        // written as a target colour divided by the mid grass. Alpha is the distance to the nearest field
        // edge (/8 m, 1 off the fields): the terrain shader draws the hedgerows from it, crisp at any scale.
        const float MacroX=-1180,MacroZ=-2090,MacroW=2500,MacroD=1780;
        static float Hash(int a,int b,int c){unchecked{uint h=(uint)(a*73856093)^(uint)(b*19349663)^(uint)(c*83492791);h^=h>>13;h*=0x5bd1e995;h^=h>>15;return (h&0xffffff)/16777216f;}}
        static Texture2D MacroTint()
        {
            const int nx=768,nz=548;var tex=new Texture2D(nx,nz,TextureFormat.RGBAHalf,true,true){name="Valley macro tint",wrapMode=TextureWrapMode.Clamp};var px=new Color[nx*nz];
            var mid=(WorldLook.ToyGrassLight+WorldLook.ToyGrassDeep)*.5f;
            Color Crop(float r,float g,float b)=>new(r/mid.r,g/mid.g,b/mid.b,1);
            // Fresh pasture, deep meadow, hay stubble, ripe barley, sage ley, lavender, young wheat, fallow.
            Color[] crops={Color.white,Crop(.19f,.35f,.14f),Crop(.52f,.50f,.25f),Crop(.60f,.53f,.24f),Crop(.33f,.46f,.22f),Crop(.45f,.37f,.54f),Crop(.30f,.49f,.16f),Crop(.45f,.39f,.26f)};
            float[] odds={.25f,.16f,.14f,.09f,.13f,.04f,.12f,.07f};
            Color woodland=Crop(.10f,.22f,.10f),upland=Crop(.34f,.42f,.26f);
            const float RegionSize=340;
            // Rows in parallel (docs/72): every pixel is pure maths over the height field.
            System.Threading.Tasks.Parallel.For(0,nz,j=>{for(int i=0;i<nx;i++) {
                float x=MacroX+(i+.5f)/nx*MacroW,z=MacroZ+(j+.5f)/nz*MacroD;
                float h=ValleyField.Height(x,z),slope=1-ValleyField.Normal(x,z).y;
                // Woods: the copse noise, spreading onto steep banks where it is already wooded-ish.
                float woods=Mathf.PerlinNoise(x*.008f+11,z*.008f+23);
                float forest=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.55f,woods));
                forest=Mathf.Max(forest,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.14f,.26f,slope))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.46f,woods)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(HillcrestRoute.LakeY,HillcrestRoute.LakeY+3,h));
                // Farm regions: jittered Voronoi cells, each with its own field orientation; the
                // border between two farms is a hedge too.
                int ri=Mathf.FloorToInt(x/RegionSize),rj=Mathf.FloorToInt(z/RegionSize);float d1=1e9f,d2=1e9f;int bi=0,bj=0;
                for(int dj=-1;dj<=1;dj++)for(int di=-1;di<=1;di++) {
                    int ci=ri+di,cj=rj+dj;float sx=(ci+.15f+.7f*Hash(ci,cj,1))*RegionSize,sz=(cj+.15f+.7f*Hash(ci,cj,2))*RegionSize;
                    float d=(x-sx)*(x-sx)+(z-sz)*(z-sz);if(d<d1){d2=d1;d1=d;bi=ci;bj=cj;}else if(d<d2)d2=d;
                }
                float border=(Mathf.Sqrt(d2)-Mathf.Sqrt(d1))*.5f;
                float angle=(Hash(bi,bj,3)-.5f)*1.4f,ca=Mathf.Cos(angle),sa=Mathf.Sin(angle);
                float u=x*ca+z*sa,v=-x*sa+z*ca;
                float rowH=48+Hash(bi,bj,4)*42;int row=Mathf.FloorToInt(v/rowH);
                float colW=58+Hash(bi,row,5)*80,shift=Hash(bi,row,6)*colW;int col=Mathf.FloorToInt((u+shift)/colW);
                float fu=(u+shift)/colW-col,fv=v/rowH-row;
                float edge=Mathf.Min(Mathf.Min(fu,1-fu)*colW,Mathf.Min(fv,1-fv)*rowH);edge=Mathf.Min(edge,border);
                float roll=Hash(col*7+bi,row*13+bj,7),acc=0;int crop=0;for(int k=0;k<odds.Length;k++){acc+=odds[k];if(roll<acc){crop=k;break;}}
                // Fields keep to the flatter low ground, off the town hill and the lake shore.
                float fields=Mathf.SmoothStep(1,0,Mathf.InverseLerp(.07f,.15f,slope))*Mathf.SmoothStep(1,0,Mathf.InverseLerp(38,58,h))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(250,320,Vector2.Distance(new(x,z),HillcrestRoute.HillCentre)))*
                    Mathf.SmoothStep(0,1,Mathf.InverseLerp(HillcrestRoute.LakeRadius+25,HillcrestRoute.LakeRadius+60,Vector2.Distance(new(x,z),HillcrestRoute.Lake)));
                var c=Color.Lerp(Color.white,crops[crop],fields);
                // Uplands: drier, paler grass as the hills climb, broken by gullies of green.
                float high=Mathf.SmoothStep(0,1,Mathf.InverseLerp(45,115,h))*Mathf.Lerp(.55f,1,Mathf.PerlinNoise(x*.011f+3,z*.011f+5));
                c=Color.Lerp(c,upland,high*.8f*(1-fields));
                c=Color.Lerp(c,woodland,forest*.92f);
                c*=Mathf.Lerp(.9f,1.07f,Mathf.PerlinNoise(x*.035f+1,z*.035f+2));
                float fade=Mathf.Min(Mathf.Min(i,nx-1-i),Mathf.Min(j,nz-1-j))/(nx*.03f);c=Color.Lerp(Color.white,c,Mathf.Clamp01(fade));
                c.a=Mathf.Lerp(1,Mathf.Clamp01(edge/8),fields*(1-forest)*Mathf.Clamp01(fade));
                px[j*nx+i]=c;
            }});
            tex.SetPixels(px);tex.Apply(true);return tex;
        }
        void Mainland()
        {
            Lap("mainland start");ValleyField.Build();Lap("mainland field");
            valley=new GameObject("Hillcrest Valley").transform;
            var chunks=new GameObject("Hillcrest mainland terrain").transform;chunks.SetParent(valley);
            mainlandGround=TerrainMaterial("Mainland meadow and sandstone",new Color(.31f,.43f,.23f),new Color(.60f,.56f,.46f));
            mainlandGround.SetFloat("_ToyVariety",1);
            mainlandGround.SetTexture("_MacroTint",MacroTint());mainlandGround.SetVector("_MacroRect",new Vector4(MacroX,MacroZ,MacroW,MacroD));
            // Mainland rock is grey limestone with faint strata, not Bayview's banded sandstone.
            // Warm weathered limestone, darker than the grass, with clear strata; grass holds on all but the
            // steepest ground. Pale grey rock on moderate slopes read as untextured smears across the hills.
            mainlandGround.SetColor("_ToyRockA",new Color(.56f,.52f,.45f));mainlandGround.SetColor("_ToyRockB",new Color(.40f,.37f,.33f));mainlandGround.SetFloat("_Bands",.55f);
            mainlandGround.SetColor("_RockColor",new Color(.64f,.60f,.53f));mainlandGround.SetFloat("_RockDesat",.4f);mainlandGround.SetFloat("_MeadowBias",.17f);
            Lap("mainland tint");const int size=64;int built=0,triangles=0;
            var quarryGround=QuarryHills.Enabled?QuarryGround():null;
            // Load time (docs/60): the chunks' geometry is worked out on every core at once, then the meshes are made here, and
            // their colliders are cooked in parallel too (Physics.BakeMesh) before they're assigned. The same meshes as before.
            var clock=System.Diagnostics.Stopwatch.StartNew();
            // Which chunks stand above the sea: each chunk's highest cell, found for all chunks at once on every core
            // (docs/72: the one-by-one scan of 3.2 million cells took ~0.25 s), then kept in the same order as before.
            var candidates=new List<(int ci,int cj,int i0,int j0,int i1,int j1,bool quarry)>();
            for(int cj=0;cj*size<ValleyField.NZ-1;cj++)for(int ci=0;ci*size<ValleyField.NX-1;ci++) {
                int i0=ci*size,j0=cj*size,i1=Mathf.Min(i0+size,ValleyField.NX-1),j1=Mathf.Min(j0+size,ValleyField.NZ-1);
                float cx0=ValleyField.X0+i0*ValleyField.Cell,cz1=ValleyField.Z0+j1*ValleyField.Cell;
                if(cx0>=QuarryHills.SeamX-.1f&&cz1<-2000&&!(Bramblewick.Enabled&&cx0+size*ValleyField.Cell>1000))continue; // south of the valley, east of Quarry Hills: neither region (Bramblewick's vale reaches south: docs/82)
                candidates.Add((ci,cj,i0,j0,i1,j1,quarryGround!=null&&cx0<QuarryHills.SeamX-.1f));
            }
            var highs=new float[candidates.Count];
            System.Threading.Tasks.Parallel.For(0,candidates.Count,k=>{var c=candidates[k];float high=-999;for(int j=c.j0;j<=c.j1;j++)for(int i=c.i0;i<=c.i1;i++)high=Mathf.Max(high,ValleyField.Raw(i,j));highs[k]=high;});
            var specs=new List<(int ci,int cj,int i0,int j0,int i1,int j1,bool quarry)>();
            for(int k=0;k<candidates.Count;k++)if(highs[k]>=-22)specs.Add(candidates[k]);
            Lap("mainland chunk choice");
            // LOD 0, 1 and 2 per chunk; the collider (slot 3) is LOD 0's grid without the skirts, cut from it rather than
            // worked out twice (docs/72: the same mesh).
            var data=new ChunkData[specs.Count*4];
            System.Threading.Tasks.Parallel.For(0,specs.Count*3,k=>{var s=specs[k/3];int l=k%3;data[k/3*4+l]=ChunkGeometry(s.i0,s.j0,s.i1,s.j1,l==0?1:l==1?2:4,true);});
            System.Threading.Tasks.Parallel.For(0,specs.Count,k=>data[k*4+3]=data[k*4].WithoutSkirts());
            long geometryMs=clock.ElapsedMilliseconds;
            // Every mesh's buffers filled on all cores and applied in one call (docs/72: made one by one on the main thread,
            // ~2,000 meshes took ~120 ms). The same vertices, normals, UVs, triangles and bounds.
            var made=ChunkData.MakeMeshes(data);
            var colliders=new List<(MeshCollider c,Mesh m)>();
            for(int s=0;s<specs.Count;s++) {
                var spec=specs[s];var go=new GameObject($"Mainland chunk {spec.ci},{spec.cj}");go.transform.SetParent(chunks);
                var lods=new LOD[3];
                for(int l=0;l<3;l++) {
                    var mesh=made[s*4+l];triangles+=l==0?data[s*4+l].t.Length/3:0;
                    var part=new GameObject("LOD"+l,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(go.transform);
                    part.GetComponent<MeshFilter>().sharedMesh=mesh;var r=part.GetComponent<MeshRenderer>();r.sharedMaterial=spec.quarry?quarryGround:mainlandGround;
                    r.shadowCastingMode=l==2?ShadowCastingMode.Off:ShadowCastingMode.On;
                    lods[l]=new LOD(l==0?.26f:l==1?.085f:0,new Renderer[]{r});
                }
                go.AddComponent<LODGroup>().SetLODs(lods);
                var collider=new GameObject("Hillcrest mainland collider",typeof(MeshCollider));collider.transform.SetParent(go.transform);
                colliders.Add((collider.GetComponent<MeshCollider>(),made[s*4+3]));built++;
            }
            long meshMs=clock.ElapsedMilliseconds;
            var ids=colliders.ConvertAll(c=>c.m.GetEntityId()).ToArray();
            System.Threading.Tasks.Parallel.For(0,ids.Length,k=>Physics.BakeMesh(ids[k],false));
            foreach(var (c,m) in colliders)c.sharedMesh=m;
            Debug.Log($"LITTLE_ROAD_MAINLAND_TIMING geometry={geometryMs}ms meshes={meshMs-geometryMs}ms colliders={clock.ElapsedMilliseconds-meshMs}ms");
            Lap("mainland chunks");DistantMountains();Lap("mainland mountains");
            Debug.Log($"LITTLE_ROAD_MAINLAND chunks={built} lod0Triangles={triangles}");
        }
        // Quarry Hills' ground (docs/59): the same terrain shader as the valley, with wetter, deeper western grass, blue-grey
        // slate for the rock, and its own macro tint: grey slate tips and quarry floor, tawny moor, dark wet woods, small walled
        // fields round the town.
        Material QuarryGround()
        {
            var m=TerrainMaterial("Quarry Hills slate country",new Color(.26f,.42f,.22f),new Color(.40f,.43f,.46f));
            m.SetFloat("_ToyVariety",1);
            m.SetColor("_ToyRockA",new Color(.46f,.49f,.53f));m.SetColor("_ToyRockB",new Color(.30f,.33f,.37f));m.SetFloat("_Bands",.85f);
            m.SetColor("_RockColor",new Color(.44f,.47f,.50f));m.SetFloat("_RockDesat",.25f);m.SetFloat("_MeadowBias",.1f);
            m.SetTexture("_MacroTint",QuarryHillsLook.MacroTint());m.SetVector("_MacroRect",QuarryHillsLook.MacroRect);
            return m;
        }
        // Grid patch [i0..i1]x[j0..j1] at `step`; skirts hide cracks between chunk LODs.
        // Triangles crossing the road envelope at the tunnel portals are left out.
        // A chunk's geometry, worked out off the main thread (ValleyField is built by then); ToMesh() runs on the main thread.
        struct ChunkData
        {
            public Vector3[] v,n;public Vector2[] uv;public int[] t;public int gridVertices,gridIndices;public Bounds bounds;
            // (Bounds worked out with the geometry, off the main thread; SetTriangles would otherwise do them, and
            // RecalculateBounds again.)
            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            struct Vertex{public Vector3 p,n;public Vector2 uv;}
            // All the chunks' meshes at once: buffers filled in parallel (Mesh.MeshDataArray), applied in one call.
            public static Mesh[] MakeMeshes(ChunkData[] data)
            {
                var meshes=new Mesh[data.Length];for(int k=0;k<meshes.Length;k++)meshes[k]=new Mesh{name="Mainland patch"};
                var writable=Mesh.AllocateWritableMeshData(data.Length);
                var layout=new[]{new VertexAttributeDescriptor(VertexAttribute.Position,VertexAttributeFormat.Float32,3),
                    new VertexAttributeDescriptor(VertexAttribute.Normal,VertexAttributeFormat.Float32,3),new VertexAttributeDescriptor(VertexAttribute.TexCoord0,VertexAttributeFormat.Float32,2)};
                const MeshUpdateFlags quiet=MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices|MeshUpdateFlags.DontNotifyMeshUsers|MeshUpdateFlags.DontResetBoneBounds;
                System.Threading.Tasks.Parallel.For(0,data.Length,k=>{
                    var d=data[k];var md=writable[k];
                    md.SetVertexBufferParams(d.v.Length,layout);var vb=md.GetVertexData<Vertex>();
                    for(int i=0;i<d.v.Length;i++)vb[i]=new Vertex{p=d.v[i],n=d.n[i],uv=d.uv[i]};
                    bool wide=d.v.Length>65000;md.SetIndexBufferParams(d.t.Length,wide?IndexFormat.UInt32:IndexFormat.UInt16);
                    if(wide)md.GetIndexData<int>().CopyFrom(d.t);else{var ib=md.GetIndexData<ushort>();for(int i=0;i<d.t.Length;i++)ib[i]=(ushort)d.t[i];}
                    md.subMeshCount=1;md.SetSubMesh(0,new SubMeshDescriptor(0,d.t.Length){bounds=d.bounds,vertexCount=d.v.Length},quiet);
                });
                Mesh.ApplyAndDisposeWritableMeshData(writable,meshes,quiet);
                for(int k=0;k<meshes.Length;k++)meshes[k].bounds=data[k].bounds;
                return meshes;
            }
            public ChunkData WithoutSkirts()
            {
                var c=new ChunkData{v=new Vector3[gridVertices],n=new Vector3[gridVertices],uv=new Vector2[gridVertices],t=new int[gridIndices],gridVertices=gridVertices,gridIndices=gridIndices};
                System.Array.Copy(v,c.v,gridVertices);System.Array.Copy(n,c.n,gridVertices);System.Array.Copy(uv,c.uv,gridVertices);System.Array.Copy(t,c.t,gridIndices);c.bounds=BoundsOf(c.v);return c;
            }
            public static Bounds BoundsOf(Vector3[] v){Vector3 lo=v[0],hi=v[0];foreach(var p in v){lo=Vector3.Min(lo,p);hi=Vector3.Max(hi,p);}var b=new Bounds();b.SetMinMax(lo,hi);return b;}
        }
        static ChunkData ChunkGeometry(int i0,int j0,int i1,int j1,int step,bool skirts)
        {
            // (Pre-sized arrays, docs/72: growing lists on every core at once spent most of the time allocating.)
            int cols=(i1-i0)/step+1,rows=(j1-j0)/step+1,ringCount=skirts?2*cols+2*rows-3:0;
            var v=new Vector3[cols*rows+(skirts?2*(ringCount-1):0)];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];
            var t=new int[(rows-1)*(cols-1)*6+(skirts?12*(ringCount-1):0)];int ti=0,vi=0;
            int I(int c)=>Mathf.Min(i0+c*step,i1);int J(int r)=>Mathf.Min(j0+r*step,j1);
            Vector3 P(int i,int j)=>new(ValleyField.X0+i*ValleyField.Cell,ValleyField.Raw(i,j),ValleyField.Z0+j*ValleyField.Cell);
            Vector3 N(int i,int j)=>new Vector3(ValleyField.Raw(i-1,j)-ValleyField.Raw(i+1,j),2*ValleyField.Cell,ValleyField.Raw(i,j-1)-ValleyField.Raw(i,j+1)).normalized;
            for(int r=0;r<rows;r++)for(int c=0;c<cols;c++){var p=P(I(c),J(r));v[vi]=p;n[vi]=N(I(c),J(r));uv[vi]=new Vector2(p.x,p.z)*.2f;vi++;}
            // Only chunks near the tunnel's portals can have holes cut (docs/72: the test ran on all 4.6 million triangles).
            bool nearPortal=NearPortal(v[0],v[vi-1]);
            for(int r=0;r<rows-1;r++)for(int c=0;c<cols-1;c++) {
                int a=r*cols+c;
                if(!nearPortal||!PortalHole(v[a],v[a+cols],v[a+1])){t[ti++]=a;t[ti++]=a+cols;t[ti++]=a+1;}
                if(!nearPortal||!PortalHole(v[a+1],v[a+cols],v[a+cols+1])){t[ti++]=a+1;t[ti++]=a+cols;t[ti++]=a+cols+1;}
            }
            int gridVertices=vi,gridIndices=ti;
            if(skirts) {
                var ring=new int[ringCount];int ri=0;
                for(int c=0;c<cols;c++)ring[ri++]=c;for(int r=1;r<rows;r++)ring[ri++]=r*cols+cols-1;
                for(int c=cols-2;c>=0;c--)ring[ri++]=(rows-1)*cols+c;for(int r=rows-2;r>0;r--)ring[ri++]=r*cols;ring[ri++]=0;
                float drop=2+step*1.5f;
                for(int k=0;k<ringCount-1;k++) {
                    int a=ring[k],b=ring[k+1],s=vi;
                    v[vi]=v[a]+Vector3.down*drop;v[vi+1]=v[b]+Vector3.down*drop;n[vi]=n[a];n[vi+1]=n[b];uv[vi]=uv[a];uv[vi+1]=uv[b];vi+=2;
                    t[ti++]=a;t[ti++]=b;t[ti++]=s+1;t[ti++]=a;t[ti++]=s+1;t[ti++]=s;t[ti++]=a;t[ti++]=s+1;t[ti++]=b;t[ti++]=a;t[ti++]=s;t[ti++]=s+1;
                }
            }
            if(ti<t.Length)System.Array.Resize(ref t,ti);   // (portal holes left some out)
            return new ChunkData{v=v,n=n,uv=uv,t=t,gridVertices=gridVertices,gridIndices=gridIndices,bounds=ChunkData.BoundsOf(v)};
        }
        // Can any triangle in the chunk with these corners be a portal hole? A hole's centre is within 7.5 m of the line
        // between the portals, or 5 m beyond its ends, and a triangle's centre is inside its chunk; 20 m covers both.
        static bool NearPortal(Vector3 corner0,Vector3 corner1)
        {
            Vector3 n0=HillcrestRoute.PortalNorth,s0=HillcrestRoute.PortalSouth;
            float minX=Mathf.Min(corner0.x,corner1.x)-20,maxX=Mathf.Max(corner0.x,corner1.x)+20,minZ=Mathf.Min(corner0.z,corner1.z)-20,maxZ=Mathf.Max(corner0.z,corner1.z)+20;
            return Mathf.Max(n0.x,s0.x)>=minX&&Mathf.Min(n0.x,s0.x)<=maxX&&Mathf.Max(n0.z,s0.z)>=minZ&&Mathf.Min(n0.z,s0.z)<=maxZ;
        }
        static bool PortalHole(Vector3 a,Vector3 b,Vector3 c)
        {
            Vector3 m=(a+b+c)/3,n0=HillcrestRoute.PortalNorth,s0=HillcrestRoute.PortalSouth;
            Vector2 axis=new(s0.x-n0.x,s0.z-n0.z);float length=axis.magnitude;axis/=length;Vector2 rel=new(m.x-n0.x,m.z-n0.z);
            float along=Vector2.Dot(rel,axis),lateral=Mathf.Abs(axis.x*rel.y-axis.y*rel.x);
            // (Up to 1 m past each portal face, not 5: outside the face the pavements are narrower than the bore, and the
            // strips beside them were open holes down to the sea; bug inbox #4.)
            if(lateral>7.5f||along< -1||along>length+1)return false;
            float road=Mathf.Lerp(n0.y,s0.y,Mathf.Clamp01(along/length)),low=Mathf.Min(a.y,Mathf.Min(b.y,c.y)),high=Mathf.Max(a.y,Mathf.Max(b.y,c.y));
            return low<road+9.5f&&high>road-1.5f&&high<road+ValleyField.PortalShelf+1;
        }
        // ---------------------------------------------------------------- roads
        Material asphaltMainland,roadPaint,girderStone,copingStone,quarryGravel;
        void ValleyRoads()
        {
            Lap("roads start");
            asphaltMainland=WorldLook.Kit("Country road asphalt",new Color(.19f,.20f,.21f),WorldLook.Pattern.Asphalt,0,.22f,true);
            roadPaint=TrialWorld.Material("Country road paint",new Color(.97f,.96f,.88f),0,.3f);
            girderStone=WorldLook.Kit("Causeway girder",new Color(.88f,.82f,.68f),WorldLook.Pattern.Stone,0,.2f,true);
            copingStone=WorldLook.Kit("Causeway coping",new Color(.97f,.95f,.9f),WorldLook.Pattern.Stone,0,.2f,true);
            var holder=new GameObject("Hillcrest roads").transform;holder.SetParent(root);
            var main=HillcrestRoute.Mainline;float north=HillcrestRoute.PortalNorthRun,south=HillcrestRoute.PortalSouthRun;
            // Continuous roads are drawn as one ribbon each (no seams where one road carries on as
            // another): the valley road (raceway road + quay + falls road) and the station road
            // (station spur + farm lane). The mainline and Via Crest meet the valley road at
            // T-junctions, the station road branches off it, and the mainline starts at a T off the
            // Bayview loop. Each T gets corner fillets, a gap in the through road's edge line, give-way
            // lines across the approach lane and a give-way sign.
            var valleyRoad=Chain(Reverse(HillcrestRoute.RacewayAccess.line),HillcrestRoute.Quay.line,HillcrestRoute.Falls.line);
            var stationRoad=Chain(HillcrestRoute.StationSpur.line,HillcrestRoute.FarmLane.line);
            var crest=HillcrestRoute.Crest;const float valleyWidth=8,stationWidth=7;
            // The road network (docs/52): junctions are nodes, roads are trimmed back to rounded corners and each
            // junction gets one surface at the roads' heights, so edges, kerbs and lines meet (Richard, 1 Oct: the joins
            // had gaps and markings that didn't line up).
            var net=network=Network=new RoadNetwork{Fillet=FilletRadius};
            var nMain=net.Add(main.line[^1]);var nCrest=net.Add(crest.line[0]);var nStation=net.Add(stationRoad[0]);
            var nIsland=net.Add(main.line[0]);
            { float d=track.Nearest(main.line[0],out _);var at=track.At(d);nIsland.at=new Vector3(at.x,main.line[0].y,at.z);var f=track.Forward(d);
              net.External(nIsland,f,Circuit.Width*.5f,main.line[0].y+.02f);net.External(nIsland,-f,Circuit.Width*.5f,main.line[0].y+.02f); }
            // Quarry Hills (docs/59): Quarry Road leaves the raceway road at a T and runs to the town square, where the High
            // Street, Quay Lane and the haul road meet it; the Kettle Falls road carries on from the High Street.
            RoadNetwork.Node nQuarryTee=null,nSquare=null,nHigh=null;
            if(QuarryHills.Enabled){nQuarryTee=net.Add(QuarryHills.TeeOnRaceway);var qr=HillcrestRoute.Find(QuarryHills.QuarryRoadName);nSquare=net.Add(qr.line[^1]);nHigh=net.Add(HillcrestRoute.Find(QuarryHills.HighStreetName).line[^1]);}
            var valleyPieces=nQuarryTee!=null?net.Through("Valley road",valleyRoad,valleyWidth,.024f,nMain,nCrest,nStation,nQuarryTee):net.Through("Valley road",valleyRoad,valleyWidth,.024f,nMain,nCrest,nStation);
            if(QuarryHills.Enabled) {
                var qr=HillcrestRoute.Find(QuarryHills.QuarryRoadName);net.Connect(qr.name,qr.line,qr.width,.024f,nQuarryTee,nSquare,minorStart:true);
                // The back streets meet the High Street, Quay Lane and the haul road at T-junctions along them.
                var nChapelA=net.Add(QuarryHills.ChapelFrom);var nChapelB=net.Add(QuarryHills.ChapelTo);var nTerraceA=net.Add(QuarryHills.TerraceFrom);var nTerraceB=net.Add(QuarryHills.TerraceTo);
                var hs=HillcrestRoute.Find(QuarryHills.HighStreetName);net.Through(hs.name,hs.line,hs.width,.02f,nSquare,nTerraceB,nChapelB,nHigh);
                var ql=HillcrestRoute.Find(QuarryHills.QuayLaneName);net.Through(ql.name,ql.line,ql.width,.02f,nSquare,nTerraceA);
                var hr=HillcrestRoute.Find(QuarryHills.HaulRoadName);net.Through(hr.name,hr.line,hr.width,.02f,nSquare,nChapelA);
                var cs=HillcrestRoute.Find(QuarryHills.ChapelStreetName);net.Connect(cs.name,cs.line,cs.width,.018f,nChapelA,nChapelB,minorStart:true,minorEnd:true,noCentre:true);
                var tr=HillcrestRoute.Find(QuarryHills.TerraceRowName);net.Connect(tr.name,tr.line,tr.width,.018f,nTerraceA,nTerraceB,minorStart:true,minorEnd:true,noCentre:true);
                var kr=HillcrestRoute.Find(QuarryHills.KettleRoadName);net.Connect(kr.name,kr.line,kr.width,.02f,nHigh,null,noCentre:true);
            }
            var mainRoad=net.Connect(main.name,main.line,main.width,.03f,nIsland,nMain,minorStart:true,minorEnd:true);
            var crestRoad=net.Connect(crest.name,crest.line,crest.width,.018f,nCrest,null,minorStart:true,noCentre:crest.width<7.6f);
            RoadNetwork.Road stationNet;
            if(Bramblewick.Enabled) {   // (Valley Road South leaves the farm lane at a T before Golden Acre's gate: docs/82)
                int tee=0;float bt=float.MaxValue;for(int i=0;i<stationRoad.Length;i++){float d=(stationRoad[i]-Bramblewick.Tee).sqrMagnitude;if(d<bt){bt=d;tee=i;}}
                var nBramble=net.Add(stationRoad[tee]);
                stationNet=net.Connect("Station road",stationRoad[..(tee+1)],stationWidth,.018f,nStation,nBramble,minorStart:true,noCentre:true);
                net.Connect("Station road",stationRoad[tee..],stationWidth,.018f,nBramble,null,minorStart:true,noCentre:true);
                var vr=HillcrestRoute.Find(Bramblewick.RoadName);net.Connect(vr.name,vr.line,vr.width,.022f,nBramble,null,minorStart:true);
            } else stationNet=net.Connect("Station road",stationRoad,stationWidth,.018f,nStation,null,minorStart:true,noCentre:true);
            net.Solve();Lap("roads network");
            foreach(var n in net.nodes)Debug.Log($"LITTLE_ROAD_JUNCTION at=({n.at.x:0},{n.at.z:0}) arms={n.arms.Count} radius={n.Radius:0.0} trims={string.Join("/",n.arms.ConvertAll(a=>a.trim.ToString("0.0")))} y={n.at.y:0.0} ground={ValleyField.Height(n.at.x,n.at.z):0.0} poly={(n.polygon.Count>0?System.Linq.Enumerable.Average(n.polygon.ConvertAll(p=>p.y)):float.NaN):0.0}/{n.polygon.Count} arms=[{string.Join(";",n.arms.ConvertAll(a=>(a.road==null?"external":a.road.name)+(a.atStart?"@s":"@e")+":"+(a.road==null?float.NaN:a.Height(a.trim)).ToString("0.0")))}]");
            Ribbon(main.name,mainRoad.trimmed,main.width,.03f,holder,asphaltMainland,true,false,false,false);
            Stripes(mainRoad.trimmed,main.width,0,0,holder,false);
            Causeway(main,north,holder);
            Tunnel(main,north,south);
            Guardrails(main.line,main.width,holder,System.Array.FindIndex(main.line,p=>p.z<HillcrestRoute.PortalSouth.z-6),true);
            foreach(var r in net.roads) {
                if(r==mainRoad)continue;
                // Free ends (the road just stops) get their gravel ramps and leave 3 m of paint off; junction ends meet the surface.
                // Quarry Hills' haul road and Kettle Falls road are gravel: they grip like dirt (no RoadSurface) and carry no paint.
                if(QuarryHills.Gravel(r.name)){var g=Ribbon(r.name,r.trimmed,r.width,r.lift,holder,quarryGravel??=WorldLook.Kit("Slate gravel road",new Color(.50f,.49f,.47f),WorldLook.Pattern.Rock,0,.12f,true),true,false,r.start==null,r.end==null);Object.DestroyImmediate(g.GetComponent<RoadSurface>());continue;}
                // (Quarry Hills' streets bend and change grade within a vertex or two: 1 m segments keep the ribbon's quads flat,
                // where a 2 m quad across a bend folded into a 10 cm crease - the area audit's Terrace Row seam.)
                Ribbon(r.name,QuarryHills.Owns(r.name)?Subdivide(r.trimmed,1):r.trimmed,r.width,r.lift,holder,asphaltMainland,true,false,r.start==null,r.end==null);
                Stripes(r.trimmed,r.width,r.start==null?3:0,r.end==null?(r==crestRoad?6:3):0,holder,r.noCentre);
            }
            Lap("roads ribbons");
            foreach(var (line,width) in new[]{(valleyRoad,valleyWidth),(stationRoad,stationWidth)})Guardrails(line,width,holder,0,true);
            if(QuarryHills.Enabled)foreach(var name in new[]{QuarryHills.QuarryRoadName,QuarryHills.HighStreetName,QuarryHills.QuayLaneName,QuarryHills.HaulRoadName,QuarryHills.KettleRoadName,QuarryHills.ChapelStreetName,QuarryHills.TerraceRowName}){var q=HillcrestRoute.Find(name);Guardrails(q.line,q.width,holder,0,true,null,name==QuarryHills.QuarryRoadName);}
            Guardrails(crest.line,crest.width,holder,0); // (in town Via Crest has stone parapets instead: HillcrestTown.Parapets)
            Lap("roads guardrails");foreach(var road in HillcrestRoute.All())RiverBridges(road,holder);Lap("roads bridges");
            // Drivable gravel shoulders where the road runs on the ground (not on the causeway, in the tunnel or over the river).
            var gravel=WorldLook.Kit("Road verge gravel",new Color(.58f,.54f,.46f),WorldLook.Pattern.Rock,0,.1f,true);
            Shoulders(main.line,main.width*.5f,.03f,HillcrestRoute.PortalSouthRun+4,false,holder,gravel);
            // The mainline across the Bayview Halt quay (Richard, 30 Sep: onto the station grass, you couldn't get back
            // on). The island ground there sits 11 cm under the road (CausewayCut keeps it below the deck) and the
            // valley field doesn't reach the island, so it had no ramps: the car's nose stopped dead at the edge from
            // every angle (0 of 32 test runs got back on). Ramps to the island's own surface, up to the causeway.
            float quayEnd=0;for(int i=1;i<main.line.Length&&main.line[i].z>HillcrestRoute.QuaySouth+1;i++)quayEnd+=Vector3.Distance(main.line[i],main.line[i-1]);
            // (Its top edge meets the road as built: across the quay the ribbon follows the island down to -0.14 m.)
            Physics.SyncTransforms();
            float RoadTop(Vector3 q){float top=float.NaN;foreach(var hit in Physics.RaycastAll(new Vector3(q.x,20,q.z),Vector3.down,60))if(hit.collider.GetComponent<RoadSurface>()!=null&&!(hit.point.y<=top))top=hit.point.y;return top;}   // (the highest: RaycastAll's order is arbitrary)
            Shoulders(main.line,main.width*.5f,.03f,0,false,holder,gravel,"Road shoulder",quayEnd,q=>SurfacePoint(q,out var hit,false)?hit.point.y:-30,RoadTop);
            Shoulders(valleyRoad,valleyWidth*.5f,.024f,0,false,holder,gravel);
            if(Bramblewick.Enabled){var vr=HillcrestRoute.Find(Bramblewick.RoadName);Shoulders(vr.line,vr.width*.5f,.022f,0,false,holder,gravel);Guardrails(vr.line,vr.width,holder,0,true);}
            if(QuarryHills.Enabled)foreach(var name in new[]{QuarryHills.QuarryRoadName,QuarryHills.HighStreetName,QuarryHills.QuayLaneName,QuarryHills.HaulRoadName,QuarryHills.KettleRoadName,QuarryHills.ChapelStreetName,QuarryHills.TerraceRowName}){var q=HillcrestRoute.Find(name);Shoulders(q.line,q.width*.5f,.02f,0,false,holder,gravel);}Shoulders(stationRoad,stationWidth*.5f,.018f,0,false,holder,gravel);Shoulders(crest.line,crest.width*.5f,.018f,0,false,holder,gravel);
            foreach(var n in net.nodes)if(n.arms.Count>1){JunctionSurface(n,holder);if(!n.HasExternal)KerbShoulders(n,holder,gravel);}
            // Quarry Hills' junctions: shoulders across each mouth (the gap between a road's own shoulder and the kerb ramps
            // left a 16 cm step) and low stone walls where a corner's ground stands well above or below the road.
            if(QuarryHills.Enabled){Physics.SyncTransforms();foreach(var n in net.nodes)if(n.arms.Count>1&&!n.HasExternal&&QuarryHills.Contains(n.at.x,n.at.z))MouthShoulders(n,holder,gravel);}
            Lap("roads shoulders and junctions");RacewayCircuit(holder);Lap("roads raceway");
        }
        // Hillcrest Raceway: 16 m asphalt with runoff, edge paint, corner kerbs, start line and gantry.
        void RacewayCircuit(Transform parent)
        {
            var holder=new GameObject("Hillcrest Raceway").transform;holder.SetParent(parent);var line=HillcrestRoute.Circuit;int n=line.Length;
            var runoff=TrialWorld.Material("Raceway runoff",new Color(.86f,.80f,.62f),0,.1f);
            // Runoff is solid (no drop off the asphalt edge) but grips like dirt.
            // Both carry RoadSupport: race cars on an elevated circuit only drive on road-support geometry.
            var off=Ribbon("Raceway runoff",line,26,.012f,holder,runoff,true,true);Object.DestroyImmediate(off.GetComponent<RoadSurface>());off.AddComponent<RoadSupport>();Lap("raceway runoff");
            Shoulders(line,13,.012f,0,true,holder,WorldLook.Kit("Raceway verge",new Color(.52f,.56f,.36f),WorldLook.Pattern.Rock,0,.08f,true),"Raceway shoulder");Lap("raceway shoulders");
            // Armco along the run-off's outer edge where the ground falls away (the climb to the hairpins): track standards.
            // And on the outside of every bend tighter than 120 m, 30 m either side (docs/70): class B cars left the circuit
            // at ~240 m and ~930 m, crossed the run-off and stuck in the terrain beyond (doc 57 2.4: guarded or drivable).
            // (--no-corner-barriers: judge the AI on staying on the road, docs/71.)
            Guardrails(line,26,holder,0,true,HillcrestRoute.Circuit,false,LittleRoad.RaceTrial.Has("--no-corner-barriers")?null:CatchSides(line,RacewayCatchRadius,RacewayCatchReach));Lap("raceway guardrails");
            Ribbon("Raceway asphalt",line,LittleRoad.Circuit.Width,.03f,holder,asphaltMainland,true,true).AddComponent<RoadSupport>();
            Lap("raceway asphalt");TrialWorld.RubberedLine(new LittleRoad.Circuit(line),holder,.045f);Lap("raceway rubbered line");
            var white=TrialWorld.Material("Raceway paint",new Color(.97f,.97f,.92f),0,.3f);var coral=TrialWorld.Material("Raceway kerb red",new Color(.86f,.2f,.18f),0,.3f);
            foreach(int side in new[]{-1,1}) {
                var edge=new Vector3[n];for(int i=0;i<n;i++){Vector3 f=line[(i+1)%n]-line[(i+n-1)%n];f.y=0;edge[i]=line[i]+Vector3.Cross(Vector3.up,f.normalized)*side*7.7f;}
                Ribbon("Raceway edge paint",edge,.18f,.042f,holder,white,false,true);
            }
            Lap("raceway edge paint");
            // Kerbs where the circuit bends.
            float run=0;int block=0;
            for(int i=0;i<n;i++) {
                Vector3 a=line[i],b=line[(i+1)%n];run+=Vector3.Distance(a,b);if(run<4)continue;run=0;
                Vector3 f0=line[(i+n-6)%n]-line[i],f1=line[(i+6)%n]-line[i];f0.y=f1.y=0;if(Vector3.Angle(-f0,f1)<6)continue;
                var f=(b-a);var right=Vector3.Cross(Vector3.up,new Vector3(f.x,0,f.z).normalized);block++;
                foreach(int side in new[]{-1,1}){bool odd=block%2==0;var k=Piece("Raceway kerb",a+right*side*8.25f+Vector3.up*(odd?.058f:.055f),new Vector3(.7f,odd?.106f:.10f,3.8f),odd?coral:white);k.transform.rotation=Quaternion.LookRotation(f);ForgeRail(k,"kerb");}
            }
            Lap("raceway kerbs");
            // Start line, grid marks, gantry.
            Vector3 s0=line[0],fw=(line[1]-line[n-1]);fw.y=0;fw.Normalize();var r0=Vector3.Cross(Vector3.up,fw);var dark=TrialWorld.Material("Raceway check",new Color(.1f,.1f,.12f),0,.3f);
            for(int i=0;i<16;i++)for(int j=0;j<2;j++){var c=Piece("Start finish",s0+r0*(-7.5f+i)+fw*j*.55f+Vector3.up*.046f,new Vector3(1,.02f,.55f),(i+j)%2==0?white:dark);c.transform.rotation=Quaternion.LookRotation(fw);}
            var post=TrialWorld.Material("Gantry blue",Toy.Hex(0x1d6fa8),0,.3f);
            foreach(int side in new[]{-1,1})Piece("Raceway gantry post",s0+r0*side*9.4f+Vector3.up*3.6f,new Vector3(.5f,7.2f,.5f),post,true);
            var beam=Piece("Raceway gantry",s0+Vector3.up*7.0f,new Vector3(19.4f,1.3f,.5f),post);beam.transform.rotation=Quaternion.LookRotation(fw);
            var banner=Piece("Raceway gantry banner",s0+Vector3.up*7.0f-fw*.28f,new Vector3(12,.9f,.06f),TrialWorld.Material("Gantry banner",Toy.Hex(0xffd23f),0,.3f));banner.transform.rotation=Quaternion.LookRotation(fw);
        }
        // Silver Falls (30 September): water leaves the lip and falls in an arc. Two translucent sheets of
        // streaked water scroll at different speeds (a bluer body behind, white streaks in front), the sheet
        // spreads and its edges wobble as it falls, rocks break both sides and the lip, spray churns at the foot.
        void Cascade(Vector3 top,Vector3 bottom,float width,Transform holder)
        {
            var dir=bottom-top;dir.y=0;float run=Mathf.Max(1,dir.magnitude);dir/=run;var across=Vector3.Cross(Vector3.up,dir);float drop=Mathf.Max(1,top.y-bottom.y);
            Vector3 Sheet(float s,float u,float back,float wobble)=>top+dir*(run*u+back+wobble*u)+across*(s*width*(1+.2f*u))+Vector3.down*(drop*u*u);
            var shader=Shader.Find("LittleRoad/Waterfall");
            for(int layer=0;layer<2;layer++) {
                // A glassy blue body behind, a faster white streak sheet in front (LittleRoad/Waterfall).
                var mat=shader!=null?new Material(shader){name=layer==0?"Falls body":"Falls streaks"}:TrialWorld.Material("Falls",new Color(.9f,.95f,1));
                if(shader!=null){mat.SetColor("_Water",layer==0?new Color(.36f,.60f,.76f,.72f):new Color(.62f,.80f,.90f,.12f));mat.SetColor("_Foam",layer==0?new Color(.86f,.93f,.97f,.85f):new Color(1,1,1,.95f));
                    mat.SetFloat("_Speed",layer==0?.8f:1.35f);mat.SetFloat("_Layer",layer);mat.renderQueue=3000+layer;}
                const int rows=18,cols=12;var v=new Vector3[rows*cols];var uv=new Vector2[rows*cols];var t=new List<int>();
                for(int r=0;r<rows;r++)for(int c=0;c<cols;c++) {
                    float u=r/(rows-1f),s=c/(cols-1f)-.5f;
                    v[r*cols+c]=Sheet(s,u,layer==0?-.4f:0,Mathf.Sin(c*1.9f+layer*2.1f)*.4f);uv[r*cols+c]=new Vector2(s+.5f,u*drop);
                    if(r<rows-1&&c<cols-1){int a=r*cols+c;t.AddRange(new[]{a,a+cols,a+1,a+1,a+cols,a+cols+1});}
                }
                var mesh=new Mesh{name="Silver Falls sheet",vertices=v,uv=uv};mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var go=new GameObject(layer==0?"Silver Falls body":"Silver Falls streaks",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(holder);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=mat;mr.shadowCastingMode=ShadowCastingMode.Off;
            }
            if(!BayviewKit.Has("rock_cluster"))return;
            // Rocks down both edges and along the lip, so the sheet never shows a straight side.
            for(int side=-1;side<=1;side+=2)for(int k=0;k<5;k++) {
                // Beside the sheet, but seated on the gorge side below it: the sheet arcs out from the cliff, so rocks
                // following it hung 1.6-3.3 m in the air (placement audit, 2 Oct).
                float u=k/4f;var p=Sheet(side*.5f,u,0,0)+across*side*(1.2f+u*1.4f);p.y=Mathf.Min(p.y-1.2f,ValleyField.Height(p.x,p.z)-.3f);
                BayviewKit.Spawn(k%2==0?"rock_cluster":"rock_b",p,k*67+side*40,holder,solid:false,scale:2.4f+k*.35f);
            }
            // Lip rocks break the water as it leaves the lip; each is stretched down to stand on the riverbed (the
            // placement audit found them 1.6 m over it), keeping the top that shows above the water where it was.
            foreach(float s in new[]{-.36f,.05f,.4f}) {
                var rock=BayviewKit.Spawn("rock_a",Sheet(s,0,-.8f,0)+Vector3.down*.9f,s*300,holder,solid:false,scale:1.1f);
                var rs=rock.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                float bed=ValleyField.Height(b.center.x,b.center.z)-.1f;if(bed>=b.min.y)continue;
                float k=(b.max.y-bed)/b.size.y;var scale=rock.transform.localScale;scale.y*=k;rock.transform.localScale=scale;
                rs=rock.GetComponentsInChildren<Renderer>();b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);rock.transform.position+=Vector3.down*(b.min.y-bed);
            }
        }
        GameObject Ribbon(string name,Vector3[] line,float width,float lift,Transform parent,Material material,bool drivable,bool closed=false,bool rampStart=true,bool rampEnd=true)
        {
            int n=line.Length,count=closed?n+1:n;var v=new Vector3[count*2];var uv=new Vector2[count*2];var t=new List<int>();float run=0;
            for(int k=0;k<count;k++) {
                int i=k%n;Vector3 f=closed?line[(i+1)%n]-line[(i+n-1)%n]:line[Mathf.Min(n-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);
                if(k>0)run+=Vector3.Distance(line[i],line[(k-1)%n]);
                v[k*2]=line[i]-right*width*.5f+Vector3.up*lift;v[k*2+1]=line[i]+right*width*.5f+Vector3.up*lift;uv[k*2]=new(0,run/5);uv[k*2+1]=new(1,run/5);
                if(k<count-1){int a=k*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh{name=name,vertices=v,uv=uv,indexFormat=v.Length>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            if(drivable){go.AddComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<RoadSurface>();}
            if(drivable&&!closed){if(rampStart)EndRamp(line[0],line[0]-line[1],width,lift,go.transform);if(rampEnd)EndRamp(line[^1],line[^1]-line[^2],width,lift,go.transform);}
            return go;
        }
        // Where a road stops, a 3 m gravel ramp from its end down to the ground: the ~0.17 m step off the
        // end of the ribbon (edge audit: Falls road, Farm lane, Via Crest, the raceway road) caught cars
        // coming back on. Junction ends tuck under the through road, which sits higher.
        Material rampGravel;
        void EndRamp(Vector3 end,Vector3 outward,float width,float lift,Transform parent)
        {
            outward.y=0;if(outward.sqrMagnitude<1e-4f)return;outward.Normalize();var right=Vector3.Cross(Vector3.up,outward);const float length=3.2f;
            // A junction end runs straight onto the through road: no ramp (it would stand in the junction).
            var ahead=end+outward*2.5f;foreach(var road in HillcrestRoute.All())if(HillcrestRoute.Distance(road.line,ahead.x,ahead.z,out _)<road.width*.5f+.5f&&HillcrestRoute.Distance(road.line,end.x,end.z,out _)>.5f)return;
            rampGravel??=WorldLook.Kit("Road end gravel",new Color(.58f,.54f,.46f),WorldLook.Pattern.Rock,0,.1f,true);
            var v=new List<Vector3>();var t=new List<int>();const int steps=4;
            for(int k=0;k<=steps;k++) {
                float d=k*length/steps;foreach(float side in new[]{-.5f,.5f}) {
                    var p=end+outward*d+right*side*(width+.4f);float g=ValleyField.Height(p.x,p.z)-.02f;
                    p.y=k==0?end.y+lift-.004f:Mathf.Lerp(end.y+lift-.004f,g,(float)k/steps);v.Add(p);
                }
                if(k<steps){int a=k*2;t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
            }
            var mesh=new Mesh{name="Road end ramp"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            if(mesh.normals[0].y<0){t.Reverse();mesh.SetTriangles(t,0);mesh.RecalculateNormals();}
            var go=new GameObject("Road end ramp",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=rampGravel;mr.shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<MeshCollider>().sharedMesh=mesh;
        }
        // Drivable shoulders: a gravel ramp from each road edge down (or up) to the terrain, so a
        // car that runs wide can drive straight back on. Road ribbons sit ~0.17 m proud of the
        // ground (the corridor is stamped 0.14 m below the road) and the car's box collider
        // cannot climb that step. Skipped where the ground falls away (guardrails) or over water.
        void Shoulders(Vector3[] line,float halfWidth,float lift,float from,bool closed,Transform parent,Material material,string name="Road shoulder",float to=float.MaxValue,System.Func<Vector3,float> ground=null,System.Func<Vector3,float> roadTop=null)
        {
            ground??=q=>ValleyField.Height(q.x,q.z);
            const float span=1.8f;int n=line.Length;var run=new float[n];for(int i=1;i<n;i++)run[i]=run[i-1]+Vector3.Distance(line[i],line[i-1]);
            foreach(int side in new[]{-1,1}) {
                var v=new List<Vector3>();var t=new List<int>();int prev=-1;
                for(int k=0;k<=(closed?n:n-1);k++) {
                    int i=k%n;if(!closed&&(run[i]<from||run[i]>to)){prev=-1;continue;}
                    Vector3 f=closed?line[(i+1)%n]-line[(i+n-1)%n]:line[Mathf.Min(n-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;f.Normalize();var r=Vector3.Cross(Vector3.up,f)*side;
                    var inner=line[i]+r*halfWidth+Vector3.up*(lift-.004f);var outer=line[i]+r*(halfWidth+span);float g=ground(outer);
                    if(roadTop!=null){float top=roadTop(line[i]+r*(halfWidth-.2f));if(!float.IsNaN(top))inner.y=top-.004f;}
                    // Junctions grow their own ramps round their kerbs (KerbShoulders): stop where this one would run onto
                    // a junction's surface or start on its kerb.
                    bool ok=g>inner.y-.9f&&g<inner.y+1.2f&&!RiverBelow(outer)&&!OnJunction(outer,.6f)&&!OnJunction(inner,.3f);
                    if(!ok){prev=-1;continue;}
                    outer.y=g-.02f;int s0=v.Count;v.Add(inner);v.Add(outer);
                    if(prev>=0){if(side>0)t.AddRange(new[]{prev,s0,prev+1,prev+1,s0,s0+1});else t.AddRange(new[]{prev,prev+1,s0,prev+1,s0+1,s0});}
                    prev=s0;
                }
                if(t.Count==0)continue;
                var mesh=new Mesh{name=name,indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            }
        }
        // Edge lines and centre dashes, left off `trimStart`/`trimEnd` metres at junctions.
        // ---------------------------------------------------------------- junctions
        const float FilletRadius=6;
        // The same polyline with no segment longer than `step` (straight subdivision: the shape doesn't change).
        static Vector3[] Subdivide(Vector3[] line,float step)
        {
            var o=new List<Vector3>{line[0]};
            for(int i=1;i<line.Length;i++){int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(line[i-1],line[i])/step));for(int k=1;k<=n;k++)o.Add(Vector3.Lerp(line[i-1],line[i],k/(float)n));}
            return o.ToArray();
        }
        static Vector3[] Reverse(Vector3[] line){var r=(Vector3[])line.Clone();System.Array.Reverse(r);return r;}
        static Vector3[] Chain(params Vector3[][] parts){var all=new List<Vector3>(parts[0]);for(int k=1;k<parts.Length;k++)for(int i=1;i<parts[k].Length;i++)all.Add(parts[k][i]);return all.ToArray();}
        Material submerged;
        // Inside a junction (shoulders and guardrails stop here); the road network (docs/52) knows where they are.
        RoadNetwork network;
        // The last valley road network built (for the seam audit).
        public static RoadNetwork Network;
        bool NearMouth(Vector3 p,float pad)=>network!=null&&network.NearJunction(p,pad);
        bool OnJunction(Vector3 p,float pad)=>network!=null&&network.OnJunction(p,pad);
        // Gravel ramps round a junction's kerbs (its fillets and the far side of a T), as the road shoulders have
        // along the roads: before these, a car on the grass by a junction met a 16 cm step it couldn't climb
        // (edge audit, 2 Oct: lips 22 -> 60 after the junction rework skipped shoulders within its radius).
        Material cornerStone;
        // Hill junctions (Richard, 3 Oct): six rings from the centre to the rim. Inside, each point takes the blend of its roads'
        // heights (inverse square of the distance to each centreline), so there are no folds between the fan's triangles;
        // toward the rim it eases onto the rim's own heights, which meet the road ribbons exactly.
        static void SmoothJunction(RoadNetwork.Node n,List<Vector3> v,List<int> t)
        {
            var roads=new List<HillcrestRoute.Road>();foreach(var r in HillcrestRoute.All())if(HillcrestRoute.Distance(r.line,n.at.x,n.at.z,out _)<n.Radius+6)roads.Add(r);
            float Blend(Vector3 p){double sw=0,sy=0;foreach(var r in roads){float d=HillcrestRoute.Distance(r.line,p.x,p.z,out float ry);double w=1.0/(d*d+.5);sw+=w;sy+=w*ry;}return sw>0?(float)(sy/sw)+.02f:p.y;}
            var rim=n.polygon;int m=rim.Count;const int rings=6;var c=n.at;c.y=Blend(c);int c0=v.Count;v.Add(c);
            for(int r=1;r<=rings;r++){float f=r/(float)rings,e=f*f*f;
                for(int k=0;k<m;k++){var p=Vector3.Lerp(c,rim[k],f);float inner=Blend(p);p.y=r==rings?rim[k].y:Mathf.Lerp(inner,rim[k].y,e);v.Add(p);}}
            int Idx(int r,int k)=>r==0?c0:c0+1+(r-1)*m+((k%m)+m)%m;
            void Tri(int a,int b,int d){var nrm=Vector3.Cross(v[b]-v[a],v[d]-v[a]);if(nrm.y>0)t.AddRange(new[]{a,b,d});else t.AddRange(new[]{a,d,b});}
            for(int k=0;k<m;k++)Tri(Idx(0,0),Idx(1,k),Idx(1,k+1));
            for(int r=1;r<rings;r++)for(int k=0;k<m;k++){Tri(Idx(r,k),Idx(r+1,k),Idx(r+1,k+1));Tri(Idx(r,k),Idx(r+1,k+1),Idx(r,k+1));}
        }
        // The height at `at` of the plane that best fits `pts` (least squares, z = a x + b z + c about their centroid).
        static float PlaneAt(List<Vector3> pts,Vector3 at)
        {
            double mx=0,my=0,mz=0;foreach(var p in pts){mx+=p.x;my+=p.y;mz+=p.z;}int n=pts.Count;mx/=n;my/=n;mz/=n;
            double sxx=0,sxz=0,szz=0,sxy=0,szy=0;foreach(var p in pts){double dx=p.x-mx,dz=p.z-mz,dy=p.y-my;sxx+=dx*dx;sxz+=dx*dz;szz+=dz*dz;sxy+=dx*dy;szy+=dz*dy;}
            double det=sxx*szz-sxz*sxz;if(System.Math.Abs(det)<1e-6)return (float)my;
            double a=(sxy*szz-szy*sxz)/det,b=(szy*sxx-sxy*sxz)/det;return (float)(my+a*(at.x-mx)+b*(at.z-mz));
        }
        void MouthShoulders(RoadNetwork.Node n,Transform parent,Material material)
        {
            var v=new List<Vector3>();var t=new List<int>();var walls=new List<Matrix4x4>();
            // (The highest road surface: RaycastAll's order is arbitrary, so "the first found" depended on physics internals.)
            float RoadTop(Vector3 p,float fallback){float top=float.NaN;foreach(var h in Physics.RaycastAll(p+Vector3.up*3,Vector3.down,6))if(h.collider.GetComponentInParent<RoadSurface>()!=null&&!(h.point.y<=top))top=h.point.y;return float.IsNaN(top)?fallback:top;}
            foreach(var a in n.arms) {
                var along=a.dir;along.y=0;if(along.sqrMagnitude<1e-4f)continue;along.Normalize();
                foreach(var corner in new[]{a.MouthLeft,a.MouthRight}) {
                    var outward=corner-n.at;outward-=along*Vector3.Dot(outward,along);outward.y=0;if(outward.sqrMagnitude<1e-4f)continue;outward.Normalize();
                    int prev=-1;
                    for(float k=-1.5f;k<=10.01f;k+=.75f) {
                        var edge=corner+along*k;float top=RoadTop(edge-outward*.25f,corner.y);var inner=new Vector3(edge.x,top-.004f,edge.z);var outer=edge+outward*1.8f;float g=ValleyField.Height(outer.x,outer.z);
                        // Beyond the shoulder, ground that climbs or falls away steeply: a stone wall at the shoulder's back.
                        var beyond=edge+outward*6;float g6=ValleyField.Height(beyond.x,beyond.z);
                        if(Mathf.Abs(g6-top)>.9f&&!OnJunction(edge+outward*2.4f,.2f)){var w=edge+outward*2.4f;float lo=Mathf.Min(top,g),hi=Mathf.Max(top,g)+.8f;w.y=(lo+hi)*.5f;walls.Add(Matrix4x4.TRS(w,Quaternion.LookRotation(along),new Vector3(.45f,hi-lo+.4f,.8f)));}
                        if(k>3.01f)continue;
                        if(g<top-.9f||g>top+1.2f||OnJunction(outer,.2f)){prev=-1;
                            // A corner standing well off the road: a low stone wall behind the kerb instead.
                            if(Mathf.Abs(g-top)>.6f){var w=edge+outward*2.1f;w.y=Mathf.Min(top,g)+.45f+Mathf.Abs(g-top)*.5f;walls.Add(Matrix4x4.TRS(w,Quaternion.LookRotation(along),new Vector3(.45f,1+Mathf.Abs(g-top),.8f)));}
                            continue;}
                        outer.y=g-.02f;int s0=v.Count;v.Add(inner);v.Add(outer);
                        if(prev>=0){var nrm=Vector3.Cross(v[s0]-v[prev],v[prev+1]-v[prev]);if(nrm.y>0)t.AddRange(new[]{prev,s0,prev+1,prev+1,s0,s0+1});else t.AddRange(new[]{prev,prev+1,s0,prev+1,s0+1,s0});}
                        prev=s0;
                    }
                }
            }
            // Corners whose ground rises or falls steeply within 6 m of the kerb get a stone wall along the kerb too.
            foreach(var kerb in n.kerbs)for(int k=0;k+1<kerb.Count;k++) {
                var p=(kerb[k]+kerb[k+1])*.5f;var f=kerb[k+1]-kerb[k];f.y=0;if(f.sqrMagnitude<.01f)continue;var r=Vector3.Cross(Vector3.up,f.normalized);var away=p-n.at;away.y=0;if(Vector3.Dot(r,away)<0)r=-r;
                float near=ValleyField.Height(p.x+r.x*2.2f,p.z+r.z*2.2f),far=ValleyField.Height(p.x+r.x*6,p.z+r.z*6),far9=ValleyField.Height(p.x+r.x*9,p.z+r.z*9);
                if(Mathf.Abs(far9-p.y)>Mathf.Abs(far-p.y))far=far9;
                if(Mathf.Abs(far-p.y)>.9f&&Mathf.Abs(far-near)>.4f){var w=p+r*2.4f;float lo=Mathf.Min(p.y,near),hi=Mathf.Max(p.y,near)+.8f;w.y=(lo+hi)*.5f;walls.Add(Matrix4x4.TRS(w,Quaternion.LookRotation(f),new Vector3(.45f,hi-lo+.4f,f.magnitude+.1f)));}
            }
            if(t.Count>0) {
                var mesh=new Mesh{name="Junction mouth shoulder"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
                var go=new GameObject("Junction mouth shoulder",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            }
            cornerStone??=WorldLook.Kit("Quarry Hills corner wall",new Color(.50f,.52f,.55f),WorldLook.Pattern.Stone,0,.12f,true);cornerStoneShared=cornerStone;
            // (Combined once the towns stand: TrimRails drops any piece with a building close behind it, as in front of the
            // doors by Terrace Row's mouth: street review, 4 Oct.)
            cornerWalls.Add((new List<Matrix4x4>(walls),parent));
        }
        void KerbShoulders(RoadNetwork.Node n,Transform parent,Material material)
        {
            const float span=1.8f;var v=new List<Vector3>();var t=new List<int>();
            foreach(var kerb in n.kerbs) {
                var pts=new List<Vector3>();
                for(int k=0;k+1<kerb.Count;k++){int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(kerb[k],kerb[k+1])/1f));for(int j=0;j<steps;j++)pts.Add(Vector3.Lerp(kerb[k],kerb[k+1],j/(float)steps));}
                if(kerb.Count>0)pts.Add(kerb[^1]);
                int prev=-1;
                for(int i=0;i<pts.Count;i++) {
                    var f=pts[Mathf.Min(pts.Count-1,i+1)]-pts[Mathf.Max(0,i-1)];f.y=0;if(f.sqrMagnitude<1e-6f){prev=-1;continue;}
                    var r=Vector3.Cross(Vector3.up,f.normalized);var away=pts[i]-n.at;away.y=0;if(Vector3.Dot(r,away)<0)r=-r;
                    var inner=pts[i]+Vector3.down*.004f;var outer=pts[i]+r*span;float g=ValleyField.Height(outer.x,outer.z);
                    bool ok=g>inner.y-.9f&&g<inner.y+1.2f&&!RiverBelow(outer)&&!OnJunction(outer,.2f);
                    foreach(var road in HillcrestRoute.All())if(HillcrestRoute.Distance(road.line,outer.x,outer.z,out _)<road.width*.5f+.2f){ok=false;break;}
                    if(!ok){prev=-1;continue;}
                    outer.y=g-.02f;int s0=v.Count;v.Add(inner);v.Add(outer);
                    if(prev>=0){var nrm=Vector3.Cross(v[s0]-v[prev],v[prev+1]-v[prev]);if(nrm.y>0)t.AddRange(new[]{prev,s0,prev+1,prev+1,s0,s0+1});else t.AddRange(new[]{prev,prev+1,s0,prev+1,s0+1,s0});}
                    prev=s0;
                }
            }
            if(t.Count==0)return;
            var mesh=new Mesh{name="Junction shoulder"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Junction shoulder",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<MeshCollider>().sharedMesh=mesh;
        }
        // Corner fillets (drivable asphalt), give-way lines across the approach lane, a give-way sign.
        // A junction (docs/52): its surface (for a node with an external road, just the corner pieces), edge lines along
        // its kerbs and round the corners, and give-way lines and a sign on every arm that gives way.
        void JunctionSurface(RoadNetwork.Node n,Transform parent)
        {
            var v=new List<Vector3>();var t=new List<int>();
            void Fan(Vector3 centre,List<Vector3> rim,bool closed)
            {
                int c0=v.Count;v.Add(centre);foreach(var p in rim)v.Add(p);
                int count=rim.Count,last=closed?count:count-1;
                for(int k=0;k<last;k++){int a=c0+1+k,b=c0+1+(k+1)%count;var nrm=Vector3.Cross(v[a]-v[c0],v[b]-v[c0]);if(nrm.y>0)t.AddRange(new[]{c0,a,b});else t.AddRange(new[]{c0,b,a});}
            }
            if(!n.HasExternal){float y=0;foreach(var p in n.polygon)y+=p.y;var centre=n.at;centre.y=y/n.polygon.Count;
                // Quarry Hills' hill junctions (Richard, 3 Oct: joins hard to drive over): the centre on the plane that best fits the
                // rim (least squares), so the fan has no hump or hollow in the middle; the ground under them is kept below by
                // ValleyField (JunctionPoints), so nothing is buried.
                var rim=n.polygon;
                if(QuarryHills.Enabled&&QuarryHills.Contains(n.at.x,n.at.z))SmoothJunction(n,v,t);else Fan(centre,rim,true);}
            else foreach(var piece in n.corners)Fan(piece[0],piece.GetRange(1,piece.Count-1),false);
            if(t.Count>0) {
                // World-space texture coordinates (the ribbons' scale): the asphalt pattern carries on across the junction.
                var mesh=new Mesh{name="Junction"};mesh.SetVertices(v);mesh.SetUVs(0,v.ConvertAll(p=>new Vector2(p.x/8,p.z/5)));mesh.SetTriangles(t,0);mesh.RecalculateNormals();
                var go=new GameObject("Junction",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent);
                // (Street review, 5 Oct: the haul road's gravel stopped in a hard line across the road at Chapel Street's mouth.) A
                // junction where most arms are gravel roads is gravel, so the change falls at the side street's mouth.
                int gravelArms=0;foreach(var a in n.arms)if(a.road!=null&&QuarryHills.Enabled&&QuarryHills.Gravel(a.road.name))gravelArms++;
                var surface=gravelArms*2>n.arms.Count&&quarryGravel!=null?quarryGravel:asphaltMainland;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=surface;go.GetComponent<MeshCollider>().sharedMesh=mesh;if(surface!=quarryGravel)go.AddComponent<RoadSurface>(); // (gravel grips as the gravel road does)
            }
            // Edge lines 0.35 m in from every kerb (only round the corners where an external road keeps its own paint).
            var lv=new List<Vector3>();var lt=new List<int>();
            void Line(List<Vector3> pts)
            {
                for(int k=0;k+1<pts.Count;k++) {
                    Vector3 a=pts[k],b=pts[k+1],f=b-a;f.y=0;if(f.sqrMagnitude<1e-4f)continue;f.Normalize();var r=Vector3.Cross(Vector3.up,f);
                    var mid=(a+b)*.5f;if(Vector3.Dot(n.at-mid,r)<0)r=-r;   // inward, towards the junction's middle
                    int s=lv.Count;foreach(var q in new[]{a,b})foreach(float o in new[]{.28f,.42f})lv.Add(q+r*o+Vector3.up*.047f);
                    var nrm=Vector3.Cross(lv[s+2]-lv[s],lv[s+1]-lv[s]);if(nrm.y>0)lt.AddRange(new[]{s,s+2,s+1,s+1,s+2,s+3});else lt.AddRange(new[]{s,s+1,s+2,s+1,s+3,s+2});
                }
            }
            if(!n.HasExternal)foreach(var kerb in n.kerbs)Line(kerb);
            else foreach(var piece in n.corners)Line(piece.GetRange(1,piece.Count-1));
            // Give-way: two dashed lines across the approach lane (traffic keeps right) just inside each minor mouth.
            foreach(var arm in n.arms) {
                if(!arm.minor||arm.external)continue;var right=Vector3.Cross(Vector3.up,arm.dir);var approach=-right;
                foreach(float b in new[]{arm.trim+.45f,arm.trim+1.0f})
                    for(float l=.25f;l<arm.half-.3f;l+=.9f) {
                        var p=n.at+arm.dir*b+approach*(l+.3f);p.y=arm.Height(b)+.023f;int s0=lv.Count;
                        lv.Add(p-approach*.3f-arm.dir*.1f);lv.Add(p+approach*.3f-arm.dir*.1f);lv.Add(p-approach*.3f+arm.dir*.1f);lv.Add(p+approach*.3f+arm.dir*.1f);
                        var nrm=Vector3.Cross(lv[s0+1]-lv[s0],lv[s0+2]-lv[s0]);if(nrm.y>0)lt.AddRange(new[]{s0,s0+1,s0+2,s0+1,s0+3,s0+2});else lt.AddRange(new[]{s0,s0+2,s0+1,s0+1,s0+2,s0+3});
                    }
                GiveWaySign(n.at+arm.dir*(arm.trim+1.5f)+approach*(arm.half+1.6f),arm.dir,n.HasExternal,parent);
            }
            if(lv.Count>0){var lm=new Mesh{name="Junction paint",indexFormat=lv.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};lm.SetVertices(lv);lm.SetTriangles(lt,0);lm.RecalculateNormals();
                var lg=new GameObject("Junction paint",typeof(MeshFilter),typeof(MeshRenderer));lg.transform.SetParent(parent);lg.GetComponent<MeshFilter>().sharedMesh=lm;lg.GetComponent<MeshRenderer>().sharedMaterial=roadPaint;}
        }
        // The give-way sign beside an approach, facing oncoming traffic.
        void GiveWaySign(Vector3 foot,Vector3 face,bool island,Transform parent)
        {
            foot.y=island?0:ValleyField.Height(foot.x,foot.z);
            var grey=TrialWorld.Material("Sign post",new Color(.55f,.57f,.6f),.4f,.4f);
            var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Object.DestroyImmediate(post.GetComponent<Collider>());post.name="Give way post";post.transform.SetParent(parent);
            post.transform.position=foot+Vector3.up*1.1f;post.transform.localScale=new Vector3(.08f,1.1f,.08f);post.GetComponent<Renderer>().sharedMaterial=grey;
            var sv=new List<Vector3>();var st=new List<int>();var sc=new List<Color>();Vector3 top=foot+Vector3.up*2.55f;var side2=Vector3.Cross(Vector3.up,face);
            void Tri(float size,float lift2,bool front,Color c){int s0=sv.Count;var off=face*lift2;sv.Add(top+off-side2*size*.58f);sv.Add(top+off+side2*size*.58f);sv.Add(top+off+Vector3.down*size);sc.Add(c);sc.Add(c);sc.Add(c);
                var nrm=Vector3.Cross(sv[s0+1]-sv[s0],sv[s0+2]-sv[s0]);bool facesOut=Vector3.Dot(nrm,face)>0;if(facesOut==front)st.AddRange(new[]{s0,s0+1,s0+2});else st.AddRange(new[]{s0,s0+2,s0+1});}
            Tri(.9f,.01f,true,new Color(.78f,.16f,.16f));Tri(.55f,.025f,true,new Color(.93f,.93f,.9f));Tri(.9f,0,false,new Color(.55f,.57f,.6f));
            var sm=new Mesh{name="Give way sign"};sm.SetVertices(sv);sm.SetColors(sc);sm.SetTriangles(st,0);sm.RecalculateNormals();
            var sg=new GameObject("Give way sign",typeof(MeshFilter),typeof(MeshRenderer));sg.transform.SetParent(parent);sg.GetComponent<MeshFilter>().sharedMesh=sm;
            if(signMaterial==null){signMaterial=new Material(Shader.Find("LittleRoad/Distant")){name="Road sign"};signMaterial.SetFloat("_Haze",0);}
            sg.GetComponent<MeshRenderer>().sharedMaterial=signMaterial;
        }
        static Material signMaterial;
        void Stripes(Vector3[] line,float width,float trimStart,float trimEnd,Transform parent,bool noCentre)
        {
            float total=0;for(int i=1;i<line.Length;i++)total+=Vector3.Distance(line[i],line[i-1]);
            foreach(float o in new[]{-width*.5f+.35f,width*.5f-.35f})Stripe("Edge line",line,o,.14f,0,trimStart,total-trimEnd,parent);
            if(!noCentre)Stripe("Centre dashes",line,0,.14f,3,trimStart,total-trimEnd,parent);
        }
        void Stripe(string name,Vector3[] line,float offset,float width,float dash,float from,float to,Transform parent)
        {
            var v=new List<Vector3>();var t=new List<int>();StripeQuads(line,offset,width,dash,from,to,v,t);
            if(v.Count==0)return;
            var mesh=new Mesh{name=name,indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=roadPaint;
        }
        // Road paint: one quad per line segment (4 vertices: start inner/outer, end inner/outer), `offset` metres to the right.
        public static void StripeQuads(Vector3[] line,float offset,float width,float dash,float from,float to,List<Vector3> v,List<int> t)
        {
            float run=0;
            // Each point's sideways direction is the mean of its two segments', so neighbouring pieces share their
            // ends: with a per-segment direction, an edge line 3.4 m out saw-toothed round tight bends.
            Vector3 Side(int i){var f=line[Mathf.Min(line.Length-1,i+1)]-line[Mathf.Max(0,i-1)];f.y=0;return Vector3.Cross(Vector3.up,f.normalized);}
            for(int i=0;i<line.Length-1;i++) {
                run+=Vector3.Distance(line[i],line[i+1]);if(run<from||run>to||(dash>0&&Mathf.Repeat(run,dash*2)>dash))continue;
                Vector3 a=line[i],b=line[i+1],ra=Side(i),rb=Side(i+1);int s=v.Count;
                // (Street review, 5 Oct: Via Crest's edge lines ran on across the market square's paving.)
                if(HillcrestTown.OnMarketPaving((a+b)*.5f))continue;
                v.Add(a+ra*(offset-width*.5f)+Vector3.up*.047f);v.Add(a+ra*(offset+width*.5f)+Vector3.up*.047f);v.Add(b+rb*(offset-width*.5f)+Vector3.up*.047f);v.Add(b+rb*(offset+width*.5f)+Vector3.up*.047f);
                t.AddRange(new[]{s,s+2,s+1,s+1,s+2,s+3});
            }
        }
        // Causeway viaduct from the Halt quay to the north portal: a stone deck on round-arched
        // piers, an open balustrade (an invisible wall keeps cars on) with pilasters, pennants and
        // lamps, a gateway arch where the road leaves the quay and an abutment at the cliff.
        void Causeway(HillcrestRoute.Road main,float end,Transform holder)
        {
            float width=main.width;var line=main.line;
            float start=main.Nearest(new Vector3(73.4f,0,HillcrestRoute.QuaySouth),out _)-.6f;
            var trim=WorldLook.Kit("Causeway string course",new Color(.97f,.95f,.9f),WorldLook.Pattern.Stone,0,.2f,true);
            var boxes=new BoxBatch();float edge=width*.5f+.25f;
            for(int i=0;i<line.Length-1&&main.run[i+1]<=end+1;i++) {
                if(main.run[i+1]<start)continue;
                Vector3 a=line[i],b=line[i+1],mid=(a+b)*.5f,f=b-a;float len=f.magnitude;var flat=new Vector3(f.x,0,f.z).normalized;var right=Vector3.Cross(Vector3.up,flat);
                var rotation=Quaternion.LookRotation(f);
                Piece("Causeway deck",mid+Vector3.down*.6f,new Vector3(width+.8f,1.2f,len+.05f),girderStone).transform.rotation=rotation;
                Piece("Causeway string course",mid+Vector3.down*.1f,new Vector3(width+1.25f,.22f,len+.05f),trim).transform.rotation=rotation;
                foreach(int side in new[]{-1,1}) {
                    var at=mid+right*side*edge;
                    Piece("Balustrade plinth",at+Vector3.up*.18f,new Vector3(.5f,.36f,len+.05f),girderStone).transform.rotation=rotation;
                    var balRail=Piece("Balustrade rail",at+Vector3.up*1.0f,new Vector3(.46f,.16f,len+.05f),copingStone);balRail.transform.rotation=rotation;ForgeRail(balRail,"coping");
                    var wall=new GameObject("Causeway parapet (solid)",typeof(BoxCollider));wall.tag="Respawn";wall.transform.SetParent(holder);
                    wall.transform.SetPositionAndRotation(at+Vector3.up*.6f,rotation);wall.transform.localScale=new Vector3(.5f,1.2f,len+.05f);
                }
            }
            // Balusters every 0.8 m, pilasters every 9 m, pennants on every third pilaster, lamps every 36 m.
            var pennants=System.Array.ConvertAll(new[]{Toy.Red,Toy.Hex(0xffd23f),Toy.Hex(0x1d6fa8),Toy.Hex(0x2bb5a8)},c=>TrialWorld.Material("Causeway pennant",c,0,.3f));int pilaster=0;
            for(float d=start+.4f;d<end-1;d+=.8f) {
                Vector3 p=main.At(d),f=main.Forward(d),right=Vector3.Cross(Vector3.up,f);var rot=Quaternion.LookRotation(f);
                bool post=Mathf.Repeat(d-start,9)<.8f;
                foreach(int side in new[]{-1,1}) {
                    var at=p+right*side*edge;
                    if(!post){boxes.Add(copingStone,at+Vector3.up*.64f,new Vector3(.2f,.56f,.2f),rot,Forge("baluster"));continue;}
                    boxes.Add(girderStone,at+Vector3.up*.62f,new Vector3(.62f,1.24f,.62f),rot,Forge("pilaster"));boxes.Add(copingStone,at+Vector3.up*(PropForge.Enabled?1.33f:1.3f),new Vector3(.74f,PropForge.Enabled?.2f:.12f,.74f),rot,Forge("pilaster_cap"));
                    if(pilaster%3==1) {
                        boxes.Add(copingStone,at+Vector3.up*2.5f,new Vector3(.07f,2.4f,.07f),rot);
                        boxes.Add(pennants[pilaster/3%4],at+Vector3.up*3.25f-f*.5f+right*side*.02f,new Vector3(.03f,.62f,.95f),rot);
                    }
                }
                if(post){
                    if(pilaster%4==2&&BayviewKit.Has("lamp_post"))
                        BayviewKit.Spawn("lamp_post",p+right*(pilaster%8==2?1:-1)*(edge+.05f)+Vector3.up*1.36f,0,holder,new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Color.white,Color.white),solid:false,scale:.85f);
                    pilaster++;
                }
            }
            boxes.Build("Causeway balustrade",holder);
            // Piers every 20 m, round stone arches between neighbours.
            Vector3 lastPier=Vector3.zero;float lastTop=0;bool hasLast=false;
            for(float d=start+14;d<end-6;d+=20) {
                Vector3 p=main.At(d),f=main.Forward(d),right=Vector3.Cross(Vector3.up,f);float ground=Seabed(p,f,right,width);
                if(ground>p.y-2.4f){hasLast=false;continue;}
                float top=p.y-1.2f,h=top-ground+.5f;var rot=Quaternion.LookRotation(f);
                // (Street review, 5 Oct: under the clear water the pale pier read as glass slabs.) Below the sea, wet dark stone.
                float wet=Mathf.Clamp(SeaLevel-.15f-(ground-.25f),0,h);
                Piece("Causeway pier",new Vector3(p.x,top-(h-wet)*.5f+.25f,p.z),new Vector3(width-1,h-wet,2.4f),girderStone,true).transform.rotation=rot;
                if(wet>0){submerged??=WorldLook.Kit("Submerged pier stone",new Color(.13f,.2f,.22f),WorldLook.Pattern.Stone,0,.3f,true);
                    Piece("Causeway pier footing",new Vector3(p.x,ground-.25f+wet*.5f,p.z),new Vector3(width-1,wet,2.4f),submerged,true).transform.rotation=rot;}
                Piece("Pier impost",new Vector3(p.x,top-Mathf.Min(h*.45f,7)+.2f,p.z),new Vector3(width-.6f,.4f,2.8f),trim).transform.rotation=rot;
                if(ground<SeaLevel) {
                    Piece("Pier cutwater",new Vector3(p.x,SeaLevel+.7f,p.z),new Vector3(width,1.6f,3.4f),copingStone).transform.rotation=rot;
                    foreach(int end2 in new[]{-1,1}){var nose=Piece("Cutwater nose",new Vector3(p.x,SeaLevel+.7f,p.z)+right*end2*width*.5f,new Vector3(2.4f,1.6f,2.4f),copingStone);nose.transform.rotation=rot*Quaternion.Euler(0,45,0);}
                }
                if(hasLast)ArchSpan("Causeway arch",lastPier+Flat(p-lastPier).normalized*1.2f,p-Flat(p-lastPier).normalized*1.2f,lastTop,top,Mathf.Max(ground,SeaLevel-1),width-1,girderStone,trim,holder);
                lastPier=p;lastTop=top;hasLast=true;
            }
            // Abutment under the road where it meets the cliff, and the gateway off the quay.
            var mouth=main.At(end-3);var mf=main.Forward(end-3);
            if(!BayviewKit.Has("headland_gate"))Piece("Causeway abutment",new Vector3(mouth.x,(mouth.y-1.2f+SeaLevel-4)*.5f,mouth.z),new Vector3(width+3,mouth.y-1.2f-SeaLevel+4,6),girderStone,true).transform.rotation=Quaternion.LookRotation(mf);
            if(BayviewKit.Has("town_gate")) {
                var gate=main.At(start-4);BayviewKit.Spawn("town_gate",gate,Quaternion.LookRotation(-main.Forward(start-4)).eulerAngles.y,holder,new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Color.white,Color.white),"HILLCREST VALLEY",true,1.05f);
            }
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        // Lowest ground under a pier's footprint (the island collider, or the valley field).
        static float Seabed(Vector3 p,Vector3 f,Vector3 right,float width)
        {
            float low=float.MaxValue;
            foreach(var o in new[]{Vector3.zero,f*1.5f,-f*1.5f,right*width*.4f,-right*width*.4f}) {
                var q=p+o;float g=q.z<-380?ValleyField.Height(q.x,q.z):Physics.Raycast(new Vector3(q.x,p.y-1.4f,q.z),Vector3.down,out var hit,90)?hit.point.y:-26;low=Mathf.Min(low,g);
            }
            return low;
        }
        // A round stone arch between two pier faces: spandrel walls, soffit and a pale arch ring.
        void ArchSpan(string name,Vector3 a,Vector3 b,float topA,float topB,float ground,float width,Material face,Material ring,Transform parent,System.Func<Vector3,float> groundAt=null)
        {
            var along=Flat(b-a);float span=along.magnitude;if(span<3)return;along/=span;var right=Vector3.Cross(Vector3.up,along);
            float crown=Mathf.Min(topA,topB)-.5f,rise=Mathf.Min(span*.5f,crown-ground-1.2f);if(rise<1.2f)return;float spring=crown-rise;
            // No arch where the hillside rises into it between the piers (it would hang or bury).
            if(groundAt!=null)foreach(float u in new[]{.2f,.5f,.8f}){var q=a+(b-a)*u;if(groundAt(q)>spring+rise*Mathf.Sin(Mathf.PI*u)-.4f)return;}
            var v=new List<Vector3>();var t0=new List<int>();var t1=new List<int>();const int N=16;
            void Quad(List<int> t,Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 outward)
            {
                int s=v.Count;v.Add(p0);v.Add(p1);v.Add(p2);v.Add(p3);
                if(Vector3.Dot(Vector3.Cross(p1-p0,p2-p0),outward)>=0)t.AddRange(new[]{s,s+1,s+2,s+2,s+1,s+3});else t.AddRange(new[]{s,s+2,s+1,s+2,s+3,s+1});
            }
            Vector3 P(float u,float y,float side)=>new Vector3(a.x,0,a.z)+along*span*u+right*side+Vector3.up*y;
            float Bottom(float u)=>spring+rise*Mathf.Sin(Mathf.PI*u);float Top(float u)=>Mathf.Lerp(topA,topB,u);
            for(int k=0;k<N;k++) {
                float u0=k/(float)N,u1=(k+1)/(float)N,b0=Bottom(u0),b1=Bottom(u1);
                foreach(int side in new[]{-1,1}) {
                    float o=side*width*.5f;var outward=right*side;
                    Quad(t0,P(u0,b0,o),P(u0,Top(u0),o),P(u1,b1,o),P(u1,Top(u1),o),outward);
                    float r=side*(width*.5f+.06f);
                    Quad(t1,P(u0,b0,r),P(u0,b0+.75f,r),P(u1,b1,r),P(u1,b1+.75f,r),outward);
                }
                var mid=(u0+u1)*.5f;var toward=new Vector3(0,rise*Mathf.PI*Mathf.Cos(Mathf.PI*mid),0)+along*span;var normal=Vector3.Cross(toward,right).normalized;if(normal.y>0)normal=-normal;
                Quad(t1,P(u0,b0,-width*.5f-.06f),P(u0,b0,width*.5f+.06f),P(u1,b1,-width*.5f-.06f),P(u1,b1,width*.5f+.06f),normal);
            }
            var mesh=new Mesh{name=name,subMeshCount=2};mesh.SetVertices(v);mesh.SetTriangles(t0,0);mesh.SetTriangles(t1,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{face,ring};
        }
        // Many small boxes as one mesh per material (balusters, pilasters, pennants).
        sealed class BoxBatch
        {
            readonly Dictionary<Material,(List<Vector3> v,List<Vector3> n,List<int> t)> parts=new();
            static readonly Vector3[] Corners={new(-1,-1,-1),new(1,-1,-1),new(1,1,-1),new(-1,1,-1),new(-1,-1,1),new(1,-1,1),new(1,1,1),new(-1,1,1)};
            static readonly int[][] Faces={new[]{0,3,2,1},new[]{4,5,6,7},new[]{0,1,5,4},new[]{3,7,6,2},new[]{0,4,7,3},new[]{1,2,6,5}};
            public void Add(Material m,Vector3 centre,Vector3 size,Quaternion rot,Mesh unit)
            {
                if(unit==null){Add(m,centre,size,rot);return;}   // (--prop-forge: a Prop Forge part authored in the unit cube, scaled into the box)
                if(!parts.TryGetValue(m,out var p)){p=(new List<Vector3>(),new List<Vector3>(),new List<int>());parts[m]=p;}
                var uv=unit.vertices;var un=unit.normals;int s=p.v.Count;
                for(int i=0;i<uv.Length;i++){p.v.Add(centre+rot*Vector3.Scale(uv[i],size));p.n.Add((rot*new Vector3(un[i].x/size.x,un[i].y/size.y,un[i].z/size.z)).normalized);}
                for(int sm=0;sm<unit.subMeshCount;sm++)foreach(int k in unit.GetTriangles(sm))p.t.Add(s+k);
            }
            public void Add(Material m,Vector3 centre,Vector3 size,Quaternion rot)
            {
                if(!parts.TryGetValue(m,out var p)){p=(new List<Vector3>(),new List<Vector3>(),new List<int>());parts[m]=p;}
                foreach(var face in Faces) {
                    int s=p.v.Count;Vector3 c0=Corners[face[0]],c1=Corners[face[1]],c2=Corners[face[2]];var normal=rot*Vector3.Cross(c1-c0,c2-c0).normalized;
                    foreach(int k in face){p.v.Add(centre+rot*Vector3.Scale(Corners[k],size*.5f));p.n.Add(normal);}
                    p.t.AddRange(new[]{s,s+1,s+2,s,s+2,s+3});
                }
            }
            public void Build(string name,Transform parent)
            {
                foreach(var (m,p) in parts) {
                    var mesh=new Mesh{name=name,indexFormat=p.v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(p.v);mesh.SetNormals(p.n);mesh.SetTriangles(p.t,0);mesh.RecalculateBounds();
                    var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
                }
            }
        }
        // Headland Tunnel: arched bore (inward faces, its own collider), walkways, lamp strip, portal faces.
        void Tunnel(HillcrestRoute.Road main,float from,float to)
        {
            var holder=new GameObject("Headland Tunnel").transform;holder.SetParent(root);
            var wall=WorldLook.Kit("Tunnel lining",new Color(.62f,.60f,.56f),WorldLook.Pattern.Stone,0,.15f,true);
            var lamp=TrialWorld.Material("Tunnel lamps",new Color(1,.78f,.45f),0,.5f);lamp.EnableKeyword("_EMISSION");lamp.SetColor("_EmissionColor",new Color(1,.7f,.35f)*2.2f);
            // Half-profile: floor edge, wall, vault; mirrored for the far side.
            const float half=6.2f,wallTop=4.6f,vault=3.0f;var profile=new List<Vector2>{new(-half,-.3f),new(-half,wallTop)};
            for(int k=1;k<12;k++){float a=Mathf.PI*k/12;profile.Add(new(-Mathf.Cos(a)*half,wallTop+Mathf.Sin(a)*vault));}
            profile.Add(new(half,wallTop));profile.Add(new(half,-.3f));profile.Add(new(-half,-.3f));
            var v=new List<Vector3>();var t=new List<int>();var uv=new List<Vector2>();int rings=0;
            for(float d=from-2;d<=to+2.01f;d+=2) {
                Vector3 p=main.At(d),f=main.Forward(d),right=Vector3.Cross(Vector3.up,f);
                foreach(var q in profile){v.Add(p+right*q.x+Vector3.up*q.y);uv.Add(new Vector2(q.x+q.y,d)*.25f);}rings++;
            }
            int m=profile.Count;
            for(int r=0;r<rings-1;r++)for(int k=0;k<m-1;k++){int a=r*m+k,b=a+m;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            var mesh=new Mesh{name="Tunnel bore"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var bore=new GameObject("Tunnel bore",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));bore.transform.SetParent(holder);
            bore.GetComponent<MeshFilter>().sharedMesh=mesh;bore.GetComponent<MeshRenderer>().sharedMaterial=wall;bore.GetComponent<MeshCollider>().sharedMesh=mesh;bore.tag="Respawn";
            var kerb=WorldLook.Kit("Tunnel walkway",new Color(.78f,.75f,.70f),WorldLook.Pattern.Stone,0,.2f,true);var band=TrialWorld.Material("Tunnel band",Toy.Hex(0xffd23f),0,.4f);
            // Whole segments of about 8 m between the portals (fixed 8 m steps overshot the far portal and
            // left a lamp hanging in the air outside it).
            int segments=Mathf.Max(1,Mathf.RoundToInt((to-from)/8));float step=(to-from)/segments;
            for(int k=0;k<segments;k++) {
                float d=from+k*step;
                Vector3 p=main.At(d+step*.5f),f=main.Forward(d+step*.5f),right=Vector3.Cross(Vector3.up,f);var rot=Quaternion.LookRotation((main.At(d+step)-main.At(d)).normalized);
                foreach(int side in new[]{-1,1}) {
                    Piece("Tunnel walkway",p+right*side*(half-.9f)+Vector3.up*.12f,new Vector3(1.8f,.3f,step+.02f),kerb,true).transform.rotation=rot;
                    Piece("Tunnel wall band",p+right*side*(half-.04f)+Vector3.up*1.1f,new Vector3(.06f,.5f,step+.02f),band).transform.rotation=rot;
                }
                Piece("Tunnel lamp",p+Vector3.up*(wallTop+vault-.2f),new Vector3(.9f,.12f,2.4f),lamp).transform.rotation=rot;
            }
            if(BayviewKit.Has("tunnel_portal")) {
                // Bayview end: the gatehouse on its sea bastion (falls back to the plain portal).
                var gateTint=new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Toy.Hex(0xd94040),Color.white);
                BayviewKit.Spawn(BayviewKit.Has("headland_gate")?"headland_gate":"tunnel_portal",main.At(from)+Vector3.down*.02f,Quaternion.LookRotation(-main.Forward(from)).eulerAngles.y,holder,gateTint,"HILLCREST VALLEY");
                BayviewKit.Spawn("tunnel_portal",main.At(to)+Vector3.down*.02f,Quaternion.LookRotation(main.Forward(to)).eulerAngles.y,holder,null,"BAYVIEW");
            }
        }
        // Red-and-white rails wherever the ground falls away beside a road.
        // steep: also where the ground beyond falls steeply within 8 m (over 1.1 m in any 2 m, 0.4 m in all: the edge
        // audit's "bank you can't drive back up", docs/57), not only a 1.8 m drop 3 m out.
        static bool SteepDrop(Vector3 edge,Vector3 outward)
        {
            // From the road (or run-off) surface itself, so a drop right at its edge counts even where the land beyond is gentle.
            float top=edge.y,worst=0,lowest=top;var hs=new float[17];
            for(int k=0;k<hs.Length;k++){var p=edge+outward*(k*.5f);hs[k]=k==0?top:Mathf.Min(top,ValleyField.Height(p.x,p.z));lowest=Mathf.Min(lowest,hs[k]);}
            for(int k=0;k+4<hs.Length;k++)worst=Mathf.Max(worst,hs[k]-hs[k+4]);
            return worst>1.1f&&top-lowest>.4f;
        }
        // Every rail and post with the way it faces out from the road (TrimRails reads them once the towns stand).
        static readonly List<(GameObject piece,Vector3 edge,Vector3 outward)> railPieces=new();
        static readonly List<(List<Matrix4x4> walls,Transform parent)> cornerWalls=new();static Material cornerStoneShared;
        // (Street review, 4 Oct: on Quay road the rail guarded a drop behind the houses, and ran along the kerb in front of their
        // doors.) After the towns are built: no rail where a building stands within 9 m beyond it.
        // (`now`: only the rails and corner walls where it says so; the rest wait for a later call, after a background
        // district's buildings exist: Districts. Null: all of them.)
        // (Where the last call built corner walls: a background district batches its own.)
        public static readonly List<Transform> TrimmedWallParents=new();
        public static int TrimRails(System.Func<Vector3,bool> now=null)
        {
            int removed=0;
            // Buildings (4.5 x 4.5 x 4 m and up) in a 16 m grid (docs/72): each rail checks the buildings in the cells it
            // looks into, not all ~7,400 footprints (that took ~0.5 s at load). Same tests, same result.
            const float Cell=16;var grid=new Dictionary<(int,int),List<int>>();var feet=BayviewKit.Footprints;
            for(int k=0;k<feet.Count;k++){var b=feet[k];if(b.size.x<4.5f||b.size.z<4.5f||b.size.y<4)continue;
                for(int gx=Mathf.FloorToInt((b.min.x-6)/Cell);gx<=Mathf.FloorToInt((b.max.x+6)/Cell);gx++)for(int gz=Mathf.FloorToInt((b.min.z-6)/Cell);gz<=Mathf.FloorToInt((b.max.z+6)/Cell);gz++){
                    if(!grid.TryGetValue((gx,gz),out var list))grid[(gx,gz)]=list=new List<int>();list.Add(k);}}
            List<int> Near(Vector3 q)=>grid.TryGetValue((Mathf.FloorToInt(q.x/Cell),Mathf.FloorToInt(q.z/Cell)),out var list)?list:null;
            var later=new List<(GameObject,Vector3,Vector3)>();
            foreach(var (piece,edge,outward) in railPieces) {
                if(piece==null)continue;if(now!=null&&!now(edge)){later.Add((piece,edge,outward));continue;}bool house=false;
                for(float o=1;o<=9&&!house;o+=2){var q=edge+outward*o;var near=Near(q);if(near==null)continue;
                    foreach(int k in near){var b=feet[k];if(b.Contains(new Vector3(q.x,b.center.y,q.z))){house=true;break;}}}
                if(house){Object.Destroy(piece);removed++;}
            }
            railPieces.Clear();railPieces.AddRange(later);
            // The junctions' corner walls, less any piece within 3 m of a building.
            var laterWalls=new List<(List<Matrix4x4>,Transform)>();TrimmedWallParents.Clear();
            foreach(var (list,parent) in cornerWalls) {
                if(parent==null)continue;if(now!=null&&list.Exists(m=>!now((Vector3)m.GetColumn(3)))){laterWalls.Add((list,parent));continue;}TrimmedWallParents.Add(parent);var kept=new List<Matrix4x4>();
                foreach(var m in list){var p=(Vector3)m.GetColumn(3);bool house=false;var near=Near(p);
                    if(near!=null)foreach(int k in near){var e=feet[k];e.Expand(new Vector3(6,40,6));if(e.Contains(new Vector3(p.x,feet[k].center.y,p.z))){house=true;break;}}
                    if(house)removed++;else kept.Add(m);}
                QuarryFloor.Combine("Corner walls",kept,cornerStoneShared,parent,true);
            }
            cornerWalls.Clear();cornerWalls.AddRange(laterWalls);return removed;
        }
        public const float RacewayCatchRadius=120;public const int RacewayCatchReach=20;
        // Which side of each point of a closed loop is the outside of a bend tighter than `radius` (heading change over
        // 15 m either way), widened `reach` points each way: [i,0] left, [i,1] right.
        public static bool[,] CatchSides(Vector3[] line,float radius,int reach)
        {
            int n=line.Length,w=10;var tight=new int[n];var flags=new bool[n,2];
            Vector3 Dir(int i){var d=line[(i+1)%n]-line[i%n];d.y=0;return d.normalized;}
            for(int i=0;i<n;i++) {
                float a=Vector3.SignedAngle(Dir(i-w+n),Dir(i+w),Vector3.up)*Mathf.Deg2Rad,len=2*w*Vector3.Distance(line[0],line[1]);
                tight[i]=Mathf.Abs(a)>1e-4f&&len/Mathf.Abs(a)<radius?(a>0?-1:1):0;   // a right-hand bend's outside is the left
            }
            for(int i=0;i<n;i++)if(tight[i]!=0)for(int k=-reach;k<=reach;k++)flags[(i+k+n)%n,tight[i]<0?0:1]=true;
            return flags;
        }
        void Guardrails(Vector3[] line,float width,Transform parent,int start,bool steep=false,Vector3[] own=null,bool riverside=false,bool[,] catchSides=null)
        {
            var white=TrialWorld.Material("Guardrail white",new Color(.97f,.97f,.95f),.2f,.4f);var red=TrialWorld.Material("Guardrail red",Toy.Red,.1f,.4f);
            for(int i=Mathf.Max(0,start);i<line.Length-1;i++) {
                Vector3 a=line[i],b=line[i+1],f=b-a;float len=new Vector2(f.x,f.z).magnitude;var flat=new Vector3(f.x,0,f.z).normalized;var right=Vector3.Cross(Vector3.up,flat);
                foreach(int side in new[]{-1,1}) {
                    Vector3 edge=(a+b)*.5f+right*side*(width*.5f+.55f),beyond=edge+right*side*3;
                    bool drop=ValleyField.Height(beyond.x,beyond.z)<=edge.y-1.8f||steep&&SteepDrop(edge,right*side)||catchSides!=null&&catchSides[i,side<0?0:1];
                    // A riverside road: the bank down to the water can start a few metres out.
                    if(riverside&&!drop)foreach(float o in new[]{6f,9f,12f}){var q=edge+right*side*o;if(ValleyField.Height(q.x,q.z)<=edge.y-1.8f){drop=true;break;}}
                    // (`riverside`: a road running along the river bank keeps its rail; only the bridge itself, with its parapets, has none.)
                    bool river=riverside?HillcrestRoute.Distance(HillcrestRoute.River.line,a.x,a.z,out _)<22.5f:RiverBelow(edge);   // (22 m: where RiverBridges builds a deck)
                    if(!drop||river||NearMouth(edge,2))continue;
                    // Another road here (a side road, a crossing): no rail across it. A stretch of the same chained road
                    // sits as far from the edge as our own centreline, so only a road clearly nearer counts.
                    if(steep){bool crossing=false;float ownDist=width*.5f+.55f;
                        // (A road a metre or more below isn't a crossing: it's the street under the drop, and the drop keeps its rail.)
                        foreach(var road in HillcrestRoute.All()){if(road.line==line||road.line==own||!NearBox(road,edge))continue;float d=HillcrestRoute.Distance(road.line,edge.x,edge.z,out float ry);if(d<Mathf.Min(road.width*.5f+2.5f,ownDist-1)&&ry>edge.y-1){crossing=true;break;}}
                        if(crossing)continue;}
                    var rail=Piece("Guardrail",edge+Vector3.up*.62f,new Vector3(.12f,.32f,len+.08f),i%2==0?white:red,true);rail.transform.rotation=Quaternion.LookRotation(f);railPieces.Add((rail,edge,right*side));ForgeRail(rail,"beam");
                    // Posts stand on the ground (Richard, 3 Oct: barriers floating by the bridge out of Hillcrest, where the
                    // road runs above the bank): from the ground, or the road if that's lower, up to the rail.
                    if(i%2==0){var foot=a+right*side*(width*.5f+.55f);float g=Mathf.Min(foot.y,ValleyField.Height(foot.x,foot.z))-.15f;float top=foot.y+.7f;
                        var post=Piece("Guardrail post",new Vector3(foot.x,(g+top)*.5f,foot.z),new Vector3(.16f,top-g,.16f),white,true);ForgeRail(post,"post",right*side);railPieces.Add((post,foot,right*side));}
                }
            }
        }
        // (--prop-forge: the rail's box drawn as Prop Forge's W-beam or C-channel post, authored in the unit cube, so the piece's
        //  own scale, collider and paint stay as they are; docs/78)
        static Mesh Forge(string part)=>PropForge.Enabled?PropKit.Load("railings").Mesh(part):null;
        static int railLogged;static void ForgeRail(GameObject piece,string part,Vector3? face=null){if(!PropForge.Enabled)return;if(part=="coping"){piece.GetComponent<MeshFilter>().sharedMesh=PropKit.Load("armco").Mesh("coping");return;}if(RaceTrial.Has("--gate-log")&&part=="beam"&&(++railLogged)%40==1)Debug.Log($"LITTLE_ROAD_RAIL {piece.transform.position.x:0.0},{piece.transform.position.y:0.0},{piece.transform.position.z:0.0}");piece.GetComponent<MeshFilter>().sharedMesh=PropKit.Load("armco").Mesh(part);if(face.HasValue)piece.transform.rotation=Quaternion.LookRotation(face.Value);}
        // A road's bounding box padded by its half-width and 3 m (docs/72: the crossing test above only needs the exact
        // distance to roads near the rail, and checking every road's every point for every rail cost ~0.4 s at load).
        static readonly Dictionary<Vector3[],Vector4> roadBoxes=new();
        static bool NearBox(HillcrestRoute.Road road,Vector3 p)
        {
            if(!roadBoxes.TryGetValue(road.line,out var box)){float pad=road.width*.5f+3;box=new Vector4(float.MaxValue,float.MaxValue,float.MinValue,float.MinValue);
                foreach(var q in road.line)box=new Vector4(Mathf.Min(box.x,q.x-pad),Mathf.Min(box.y,q.z-pad),Mathf.Max(box.z,q.x+pad),Mathf.Max(box.w,q.z+pad));roadBoxes[road.line]=box;}
            return p.x>=box.x&&p.x<=box.z&&p.z>=box.y&&p.z<=box.w;
        }
        // (A padded box first, docs/72: most points are nowhere near the river, and the exact distance scans its whole line.)
        static Vector4 riverBox=new(float.NaN,0,0,0);
        static bool RiverBelow(Vector3 p)
        {
            if(float.IsNaN(riverBox.x)){riverBox=new Vector4(float.MaxValue,float.MaxValue,float.MinValue,float.MinValue);foreach(var q in HillcrestRoute.River.line)riverBox=new Vector4(Mathf.Min(riverBox.x,q.x-27),Mathf.Min(riverBox.y,q.z-27),Mathf.Max(riverBox.z,q.x+27),Mathf.Max(riverBox.w,q.z+27));}
            if(p.x<riverBox.x||p.x>riverBox.z||p.z<riverBox.y||p.z>riverBox.w)return false;
            return HillcrestRoute.Distance(HillcrestRoute.River.line,p.x,p.z,out _)<26;
        }
        // Stone arch bridges where a road crosses the river: deck, parapets with colliders, arch ring.
        void RiverBridges(HillcrestRoute.Road road,Transform parent)
        {
            var line=road.line;int start=-1;
            for(int i=0;i<=line.Length;i++) {
                bool over=i<line.Length&&line[i].z<-380&&HillcrestRoute.Distance(HillcrestRoute.River.line,line[i].x,line[i].z,out _)<22;
                if(over&&start<0)start=i;
                if(!over&&start>=0){Bridge(line,Mathf.Max(0,start-2),Mathf.Min(line.Length-1,i+1),road.width,parent);start=-1;}
            }
        }
        void Bridge(Vector3[] line,int from,int to,float width,Transform parent)
        {
            var holder=new GameObject("River bridge").transform;holder.SetParent(parent);
            for(int i=from;i<to;i++) {
                Vector3 a=line[i],b=line[i+1],mid=(a+b)*.5f,f=b-a;float len=f.magnitude;var right=Vector3.Cross(Vector3.up,new Vector3(f.x,0,f.z).normalized);var rot=Quaternion.LookRotation(f);
                Piece("Bridge deck",mid+Vector3.down*.7f,new Vector3(width+1.4f,1.3f,len+.05f),stone).transform.rotation=rot;
                foreach(int side in new[]{-1,1}) {
                    Piece("Bridge parapet",mid+right*side*(width*.5f+.45f)+Vector3.up*.55f,new Vector3(.6f,1.1f,len+.05f),stone,true).transform.rotation=rot;
                    {var bc=Piece("Bridge coping",mid+right*side*(width*.5f+.45f)+Vector3.up*1.15f,new Vector3(.8f,.16f,len+.05f),cap);bc.transform.rotation=rot;ForgeRail(bc,"coping");}
                }
            }
            // Arch ring: voussoirs on a segmental arc between the abutments, springing near the water.
            Vector3 s=line[from],e=line[to],c=(s+e)*.5f,f2=(e-s);f2.y=0;float span=f2.magnitude;f2.Normalize();float top=Mathf.Min(s.y,e.y)-1.4f,spring=Mathf.Min(top-2,1.6f);
            for(int k=0;k<=18;k++) {
                float u=k/18f,x=(u-.5f)*span*.86f,y=spring+(top-spring)*Mathf.Sin(u*Mathf.PI);
                var tangent=f2*span*.86f+Vector3.up*(top-spring)*Mathf.PI*Mathf.Cos(u*Mathf.PI);
                var block=Piece("Arch voussoir",new Vector3(c.x+f2.x*x,y,c.z+f2.z*x),new Vector3(width+1.2f,.9f,span*.86f/18+.1f),k%3==0?cap:stone);
                block.transform.rotation=Quaternion.LookRotation(tangent);
            }
            foreach(int end in new[]{-1,1}) {
                Vector3 p=c+f2*end*span*.47f;float g=ValleyField.Height(p.x,p.z);
                Piece("Bridge pier",new Vector3(p.x,(g+top)*.5f-.5f,p.z),new Vector3(width+1.6f,top-g+1,2.2f),stone).transform.rotation=Quaternion.LookRotation(f2);
            }
        }
        // ---------------------------------------------------------------- railway
        // Ballast, rails and sleepers; stone viaduct piers wherever the line runs above the
        // ground; a tunnel mouth where it disappears into the ridge.
        void Railway(){Railway(HillcrestRoute.Rail,"HILLCREST LINE");Railway(HillcrestRoute.SeaRail,"HILLCREST LINE");BayviewHalt();}
        void Railway(HillcrestRoute.Road rail,string portalName)
        {
            var holder=new GameObject(rail.name).transform;holder.SetParent(root);var line=rail.line;
            var ballast=TrialWorld.Material("Rail ballast",new Color(.55f,.50f,.45f),0,.1f);var steel=TrialWorld.Material("Rail steel",new Color(.62f,.64f,.68f),.6f,.5f);
            var sleeper=WorldLook.Kit("Rail sleepers",new Color(.42f,.28f,.16f),WorldLook.Pattern.Wood,0,.1f,true);var deck=WorldLook.Kit("Viaduct stone",new Color(.86f,.78f,.64f),WorldLook.Pattern.Stone,0,.15f,true);
            Ribbon("Rail ballast",line,3.4f,.05f,holder,ballast,false);
            foreach(float o in new[]{-.72f,.72f}) {
                var offset=new Vector3[line.Length];for(int i=0;i<line.Length;i++)offset[i]=line[i]+rail.Right(rail.run[i])*o;
                Ribbon("Rail",offset,.14f,.28f,holder,steel,false);
            }
            var v=new List<Vector3>();var t=new List<int>();
            for(float d=0;d<rail.Length;d+=.9f) {
                Vector3 p=rail.At(d)+Vector3.up*.17f,f=rail.Forward(d),r=Vector3.Cross(Vector3.up,f);int s=v.Count;
                v.Add(p-r*1.25f-f*.13f);v.Add(p+r*1.25f-f*.13f);v.Add(p-r*1.25f+f*.13f);v.Add(p+r*1.25f+f*.13f);t.AddRange(new[]{s,s+2,s+1,s+1,s+2,s+3});
            }
            var mesh=new Mesh{name="Sleepers",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Sleepers",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(holder);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=sleeper;
            // Deck and piers where the line is raised.
            for(int i=0;i<line.Length-1;i++) {
                Vector3 a=line[i],b=line[i+1],m=(a+b)*.5f;float ground=Mathf.Min(Ground(a),Ground(b));
                if(ground>m.y-1.2f)continue;var rot=Quaternion.LookRotation(b-a);
                Piece("Viaduct deck",m+Vector3.down*.55f,new Vector3(4.4f,1.1f,Vector3.Distance(a,b)+.05f),deck).transform.rotation=rot;
                foreach(int sd in new[]{-1,1})Piece("Viaduct parapet",m+Vector3.Cross(Vector3.up,(b-a).normalized)*sd*2.1f+Vector3.up*.35f,new Vector3(.25f,.7f,Vector3.Distance(a,b)+.05f),copingStone,true).transform.rotation=rot;
            }
            Vector3 lastPier=Vector3.zero,lastForward=Vector3.zero;float lastTop=0;bool hasLast=false;
            for(float d=10;d<rail.Length;d+=16) {
                Vector3 p=rail.At(d),f=rail.Forward(d),side=Vector3.Cross(Vector3.up,f);float ground=Ground(p);if(ground>p.y-3){hasLast=false;continue;}
                // Stand on the lowest ground under the whole footprint (no stubs hanging over slopes).
                foreach(var o in new[]{f*1.4f,-f*1.4f,side*2f,-side*2f})ground=Mathf.Min(ground,Ground(p+o));
                float top=p.y-1.1f,bottom=Mathf.Min(ground,HillcrestRoute.River.At(HillcrestRoute.River.Nearest(p,out _)).y-3)-.5f;
                // (Street review, 5 Oct: under the clear sea the pale piers read as glass columns.) Below the water, wet dark stone.
                float wet=Mathf.Clamp(SeaLevel-.15f-bottom,0,top-bottom);
                var pier=Piece("Viaduct pier",new Vector3(p.x,(top+bottom+wet)*.5f,p.z),new Vector3(3.6f,top-bottom-wet,2.4f),deck,true);pier.transform.rotation=Quaternion.LookRotation(f);
                if(wet>0){submerged??=WorldLook.Kit("Submerged pier stone",new Color(.13f,.2f,.22f),WorldLook.Pattern.Stone,0,.3f,true);
                    Piece("Viaduct pier footing",new Vector3(p.x,bottom+wet*.5f,p.z),new Vector3(3.6f,wet,2.4f),submerged,true).transform.rotation=pier.transform.rotation;}
                Piece("Pier cap",new Vector3(p.x,top-.3f,p.z),new Vector3(4.6f,.6f,3.0f),copingStone).transform.rotation=pier.transform.rotation;
                // Arches only on straight-ish spans (a chord arch would miss the deck on the loops).
                if(hasLast&&Vector3.Angle(lastForward,f)<9)ArchSpan("Viaduct arch",lastPier+Flat(p-lastPier).normalized*1.2f,p-Flat(p-lastPier).normalized*1.2f,lastTop,top,Mathf.Max(ground,SeaLevel-1),3.6f,deck,copingStone,holder,Ground);
                lastPier=p;lastForward=f;lastTop=top;hasLast=true;
            }
            // Tunnel mouth: a small portal and a dark plug just behind its opening (the rock face
            // stands behind the portal, see ValleyField.Face).
            if(rail==HillcrestRoute.SeaRail&&BayviewKit.Has("rail_gate"))
                BayviewKit.Spawn("rail_gate",line[0]+Vector3.down*.02f,Quaternion.LookRotation(rail.Forward(0)).eulerAngles.y,holder,new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Toy.Hex(0xd94040),Color.white),portalName);
            else if(BayviewKit.Has("tunnel_portal")) {
                Vector3 mouth=line[0],f=rail.Forward(0);
                BayviewKit.Spawn("tunnel_portal",mouth+Vector3.down*.02f,Quaternion.LookRotation(f).eulerAngles.y,holder,null,portalName,true,.62f);
                var plug=Piece("Rail tunnel dark",mouth-f*1.3f+Vector3.up*2.5f,new Vector3(7.6f,5.1f,2),TrialWorld.Material("Tunnel dark",new Color(.03f,.03f,.04f),0,0));plug.transform.rotation=Quaternion.LookRotation(f);
            }
        }
        // Island ground under the Bayview end comes from the island collider; the rest from the field.
        static float Ground(Vector3 p)=>p.z<-380?ValleyField.Height(p.x,p.z):Physics.Raycast(new Vector3(p.x,60,p.z),Vector3.down,out var hit,100)&&hit.point.y>-20?hit.point.y:-26;
        // Bayview Halt: a walled quay on the island's south-east shore (the island terrain is
        // flattened to it, see Height). The mainline crosses its west side; a paved forecourt,
        // the station and its platform face the loop's west straight; the causeway gateway
        // stands where the road leaves the quay.
        public static readonly Vector3 HaltResident=new(83.6f,0,-142),HaltArrival=new(82.5f,.02f,-128);
        void BayviewHalt()
        {
            if(!BayviewKit.Has("station"))return;
            var holder=new GameObject("Bayview Halt").transform;holder.SetParent(root);
            float west=HillcrestRoute.QuayWest,east=HillcrestRoute.QuayEast,south=HillcrestRoute.QuaySouth,railX=HillcrestRoute.BayPlatform.x;
            var blue=new BayviewKit.Tint(Color.white,Toy.Hex(0x1d6fa8),Color.white,Color.white);
            // Sea walls wherever the quay meets the sea, promenade rails on top (gaps for road and rail).
            // (Street review, 5 Oct: a wall with sea off one end only stood alone on the lawn at the quay's north end.) Sea along
            // most of the wall's length.
            bool Sea(Vector3 a,Vector3 b){int n=0;for(int k=0;k<=4;k++){var q=Vector3.Lerp(a,b,k/4f);if(Height(q.x,q.z)< -3)n++;}return n>=3;}
            void Wall(Vector3 at,float yaw,Vector3 outward,Vector3 along,System.Func<Vector3,bool> railGap)
            {
                if(!BayviewKit.Has("sea_wall")||!Sea(at+outward*2.5f-along*5,at+outward*2.5f+along*5))return;
                BayviewKit.Spawn("sea_wall",at,yaw,holder);
                if(!BayviewKit.Has("promenade_rail"))return;
                foreach(float o in new[]{-3f,3f}){var p=at+along*o-outward*.25f;if(!railGap(p))BayviewKit.Spawn("promenade_rail",p+Vector3.up*.27f,yaw+180,holder,blue);}
            }
            bool RoadOrRail(Vector3 p)=>HillcrestRoute.Distance(HillcrestRoute.Causeway,p.x,p.z,out _)<7||HillcrestRoute.Distance(HillcrestRoute.SeaRail.line,p.x,p.z,out _)<4;
            for(float z=south+6;z<-90;z+=12)Wall(new Vector3(east,0,z),90,Vector3.right,Vector3.forward,RoadOrRail);
            for(float z=south+6;z<-90;z+=12)Wall(new Vector3(west,0,z),270,Vector3.left,Vector3.forward,RoadOrRail);
            foreach(float x in new[]{west+6,85.7f,95.7f,east-6})Wall(new Vector3(x,0,south),180,Vector3.back,Vector3.right,RoadOrRail);
            Piece("Quay abutment",new Vector3(74.1f,-7.4f,south),new Vector3(11.6f,13.2f,2.4f),girderStone,true);
            // Forecourt: paving between the road and the station, kerb with two openings, lamps.
            var paving=WorldLook.Kit("Halt forecourt paving",new Color(.84f,.80f,.72f),WorldLook.Pattern.Stone,0,.15f,true);var kerbStone=TrialWorld.Material("Halt kerb",new Color(.93f,.91f,.86f),0,.2f);
            float roadEdge=78.4f,building=89.6f;
            Piece("Halt forecourt",new Vector3((roadEdge+building)*.5f,.02f,-134),new Vector3(building-roadEdge,.04f,52),paving);
            for(float z=-108;z>-160;z-=1.5f){
                Piece("Forecourt joint",new Vector3((roadEdge+building)*.5f,.043f,z),new Vector3(building-roadEdge,.004f,.05f),kerbStone);
                bool opening=(z<-114&&z>-127)||(z<-146&&z>-157);if(!opening)Piece("Forecourt kerb",new Vector3(roadEdge+.15f,.08f,z-.75f),new Vector3(.3f,.16f,1.5f),kerbStone);
            }
            BayviewKit.Spawn("station",new Vector3(railX-1.55f-9.36f,0,-142),270,holder,new BayviewKit.Tint(Toy.Hex(0xcfe0f5),Toy.Hex(0x1d6fa8),Toy.Hex(0xff4d5e),Toy.Hex(0x1d6fa8)),"BAYVIEW HALT",true,.9f);
            if(BayviewKit.Has("lamp_post"))foreach(float z in new[]{-110f,-130f,-160f})BayviewKit.Spawn("lamp_post",new Vector3(roadEdge+.9f,0,z),0,holder,blue,solid:false);
            if(BayviewKit.Has("bench"))foreach(float z in new[]{-129f,-155f})BayviewKit.Spawn("bench",new Vector3(building-.9f,0,z),270,holder);
            if(BayviewKit.Has("flower_bed"))foreach(float z in new[]{-111f,-139.5f,-144.5f,-159f})BayviewKit.Spawn("flower_bed",new Vector3(building-1.2f,0,z),z*7,holder);
            if(BayviewKit.Has("map_board"))BayviewKit.Spawn("map_board",new Vector3(roadEdge+1.4f,0,-128.5f),270,holder,null,"HILLCREST LINE",true,1f); // (full size: at .8 its heading was too small to read)
            // The verge west of the road: palms, a bench, lamps along the sea rail.
            if(BayviewKit.Has("palm"))foreach(var (x,z) in new[]{(60.5f,-104f),(62f,-121f),(60.5f,-139f),(62f,-156f),(104.5f,-100f)})BayviewKit.Spawn("palm",new Vector3(x,0,z),x*z,holder);
            if(BayviewKit.Has("bench"))BayviewKit.Spawn("bench",new Vector3(58.3f,0,-130),90,holder);
            if(BayviewKit.Has("lamp_post"))foreach(float z in new[]{-112f,-148f})BayviewKit.Spawn("lamp_post",new Vector3(58f,0,z),0,holder,blue,solid:false);
            // A taxi and a couple of visitors' cars in the forecourt.
            foreach(var (body,paint,z,yaw,who,lines) in new[]{
                ("taxi",Toy.Hex(0xffd23f),-152.5f,0f,"Terry",new[]{"Taxi! Well, I'm the taxi. You're a car. Awkward.","Station to the square? Faster by train, if I'm honest."}),
                ("bubble",Toy.Hex(0xff7ac8),-134f,180f,"Poppy",   /* (not -111: the flower bed there; bug inbox #5) */new[]{"I'm meeting my nan off the 4:10!","The train goes right through the cliff. Through it!"}),
                ("camper",Toy.Hex(0x3ee6a4),-120.5f,180f,"Dune",new[]{"Surf's up on the mainland, apparently.","Just waiting for the ferry. Oh wait, it's a train."})}) {
                var car=TrialWorld.Car(body,paint);car.name="Halt "+body;car.transform.SetParent(holder);car.transform.SetPositionAndRotation(new Vector3(building-2.6f,.02f,z),Quaternion.Euler(0,yaw,0));
                var box=car.AddComponent<BoxCollider>();box.center=CarBodies.Envelope.center;box.size=CarBodies.Envelope.size;car.tag="Respawn";
                var chatter=car.AddComponent<NpcChatter>();chatter.speaker=who;chatter.colour=paint;chatter.lines=lines;chatter.title="Bayview Halt";car.AddComponent<ResidentIdle>();
            }
        }
        // ---------------------------------------------------------------- water
        void ValleyWater()
        {
            var holder=new GameObject("Hillcrest water").transform;holder.SetParent(valley);
            var water=TrialWorld.Material("River water",new Color(.16f,.36f,.42f),0,.93f);
            var ripples=new Texture2D(64,64,TextureFormat.RGB24,true){name="River ripples",wrapMode=TextureWrapMode.Repeat};var px=new Color[64*64];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float s=Mathf.PerlinNoise(x*.13f,y*.05f);px[y*64+x]=Color.Lerp(new Color(.86f,.93f,1),Color.white,Blend(.62f,.7f,s));}
            ripples.SetPixels(px);ripples.Apply();water.mainTexture=ripples;water.mainTextureScale=new Vector2(1,.25f);
            var foam=TrialWorld.Material("Falls foam",new Color(.92f,.97f,1),0,.6f);foam.mainTexture=ripples;
            var river=HillcrestRoute.River;int falls=System.Array.FindIndex(river.line,p=>p.y<.9f);
            var calm=new List<Vector3>();var steep=new List<Vector3>();
            // Foam only over the drop (~16 m); blue water again down the gorge to the sea.
            var gorge=new List<Vector3>();int lip=falls+4;
            // The ground drops into the gorge before the water surface does (the river overhung the cliff by ~10 m):
            // end the calm river at the real edge and fall from there to where the river meets the gorge pool.
            int edge=falls;for(int i=Mathf.Max(1,falls-20);i<=falls;i++)if(ValleyField.Height(river.line[i].x,river.line[i].z)<river.line[i].y-3.5f){edge=i;break;}
            int pool=System.Array.FindIndex(river.line,edge,q=>q.y<river.line[edge].y-10);if(pool<0)pool=lip;
            falls=edge;lip=pool;
            for(int i=0;i<river.line.Length;i++){if(i<=falls)calm.Add(river.line[i]);if(i>=falls-1&&i<=lip)steep.Add(river.line[i]);if(i>=lip)gorge.Add(river.line[i]);}
            // River and lake use the depth-aware inland water (shallows, foam at the banks).
            WorldLook.EnsureNoise();var inland=Shader.Find("LittleRoad/InlandWater");
            Material Inland(string name,float flow,float fade){if(inland==null)return water;var m=new Material(inland){name=name};m.SetFloat("_Flow",flow);m.SetFloat("_DepthFade",fade);return m;}
            var riverWater=Inland("Silver river water",.14f,2.2f);var lakeWater=Inland("Lake water",.015f,2.8f);var gorgeWater=Inland("Gorge water",.35f,1.8f);
            var r1=Ribbon("Silver river",calm.ToArray(),river.width,-.05f,holder,riverWater,false);r1.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // The drop is a curved, layered cascade (it was a flat opaque ribbon down the slope).
            Cascade(river.line[falls],river.line[lip],river.width*1.04f,holder);
            var r3=Ribbon("Gorge water",gorge.ToArray(),river.width*.9f,-.05f,holder,gorgeWater,false);r3.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Foam puffs churning at the foot of the falls.
            var foot=river.line[lip];var foamPuff=TrialWorld.Material("Falls foam puff",new Color(.96f,.99f,1),0,.35f);
            for(int k=0;k<14;k++) {
                var puff=GameObject.CreatePrimitive(PrimitiveType.Sphere);Object.Destroy(puff.GetComponent<Collider>());puff.name="Foam puff";puff.transform.SetParent(holder);
                puff.transform.position=foot+new Vector3(Mathf.Sin(k*2.4f)*6,.25f+(k%3)*.35f,Mathf.Cos(k*2.4f)*4);puff.transform.localScale=Vector3.one*(1.4f+(k%4)*.5f);
                puff.GetComponent<Renderer>().sharedMaterial=foamPuff;puff.AddComponent<FoamPuff>().phase=k*.7f;
            }
            // Lake: a disc at LakeY; the shore hides its rim.
            var lake=new GameObject("Raceway lake",typeof(MeshFilter),typeof(MeshRenderer));lake.transform.SetParent(holder);
            lake.GetComponent<MeshFilter>().sharedMesh=Disc(new Vector3(HillcrestRoute.Lake.x,HillcrestRoute.LakeY,HillcrestRoute.Lake.y),HillcrestRoute.LakeRadius+30,48);
            lake.GetComponent<MeshRenderer>().sharedMaterial=lakeWater;lake.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            LakeShore(holder);
            if(QuarryHills.Enabled) {
                // Glas, the pit lake: turquoise over pale slate silt (Penrhyn's colour), and the river's source tarn.
                var glas=Inland("Glas water",.01f,9);if(glas!=lakeWater||inland!=null){glas.SetColor("_Shallow",new Color(.22f,.62f,.58f));glas.SetColor("_Deep",new Color(.05f,.32f,.36f));}
                var pit=new GameObject("Glas",typeof(MeshFilter),typeof(MeshRenderer));pit.transform.SetParent(holder);
                pit.GetComponent<MeshFilter>().sharedMesh=Ellipse(new Vector3(QuarryHills.Pit.x,QuarryHills.LakeY,QuarryHills.Pit.y),QuarryHills.PitRX,QuarryHills.PitRZ,96);
                pit.GetComponent<MeshRenderer>().sharedMaterial=glas;pit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var tarn=new GameObject("Source tarn",typeof(MeshFilter),typeof(MeshRenderer));tarn.transform.SetParent(holder);
                tarn.GetComponent<MeshFilter>().sharedMesh=Disc(new Vector3(QuarryHills.Source.x,QuarryHills.SourceY,QuarryHills.Source.y),QuarryHills.SourceRadius+10,40);
                tarn.GetComponent<MeshRenderer>().sharedMaterial=lakeWater;tarn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        // A beach where the water meets the bowl (it read as a hard-edged disc on grass): the real
        // waterline is traced round the lake, then a wet-sand strip runs from just under the water to
        // above it and a drier pebbly strip with an uneven outer edge beyond.
        void LakeShore(Transform parent)
        {
            var c=HillcrestRoute.Lake;float y=HillcrestRoute.LakeY;const int n=128;var shore=new float[n];
            for(int k=0;k<n;k++){float a=k*Mathf.PI*2/n;float r=HillcrestRoute.LakeRadius-12;
                while(r<HillcrestRoute.LakeRadius+40&&ValleyField.Height(c.x+Mathf.Sin(a)*r,c.y+Mathf.Cos(a)*r)<y)r+=.5f;shore[k]=r;}
            Mesh Strip(float from,float to,bool ragged)
            {
                var v=new List<Vector3>();var t=new List<int>();
                for(int k=0;k<=n;k++) {
                    float a=k*Mathf.PI*2/n,r0=shore[k%n];var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                    float outer=to+(ragged?(Mathf.PerlinNoise(k*.23f,3.7f)-.35f)*2.6f:0);
                    foreach(float r in new[]{r0+from,r0+(from+outer)*.5f,r0+outer}){var p=new Vector3(c.x,0,c.y)+dir*r;p.y=Mathf.Max(ValleyField.Height(p.x,p.z),y-.35f)+.035f;v.Add(p);}
                    if(k<n){int b=k*3;for(int j=0;j<2;j++){t.AddRange(new[]{b+j,b+j+1,b+j+3,b+j+1,b+j+4,b+j+3});}}
                }
                var mesh=new Mesh{name="Lake shore"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
            }
            void Put(string name,Mesh mesh,Material material){var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
            Put("Lake shore wet sand",Strip(-2.6f,.7f,false),WorldLook.Kit("Wet lake sand",new Color(.52f,.46f,.36f),WorldLook.Pattern.Rock,0,.35f,true));
            Put("Lake shore pebbles",Strip(.7f,3.4f,true),WorldLook.Kit("Lake shore pebbles",new Color(.76f,.70f,.58f),WorldLook.Pattern.Rock,0,.12f,true));
        }
        static Mesh Ellipse(Vector3 centre,float rx,float rz,int sides)
        {
            var v=new List<Vector3>{centre};var uv=new List<Vector2>{new Vector2(centre.x,centre.z)*.05f};var t=new List<int>();
            for(int k=0;k<=sides;k++){float a=k*Mathf.PI*2/sides;var p=centre+new Vector3(Mathf.Sin(a)*rx,0,Mathf.Cos(a)*rz);v.Add(p);uv.Add(new Vector2(p.x,p.z)*.05f);if(k>0){t.Add(0);t.Add(k);t.Add(k+1);}}
            var mesh=new Mesh{name="Ellipse"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        static Mesh Disc(Vector3 centre,float radius,int sides)
        {
            var v=new List<Vector3>{centre};var uv=new List<Vector2>{new Vector2(centre.x,centre.z)*.05f};var t=new List<int>();
            for(int k=0;k<=sides;k++){float a=k*Mathf.PI*2/sides;var p=centre+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius;v.Add(p);uv.Add(new Vector2(p.x,p.z)*.05f);if(k>0){t.Add(0);t.Add(k);t.Add(k+1);}}
            var mesh=new Mesh{name="Disc"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
        }
        // ---------------------------------------------------------------- distant mountains
        void DistantMountains()
        {
            // A ring of ranges 2.3-4.6 km out, east through south to west; the sea stays open to the north.
            // Ridged multi-octave peaks with gullies; coloured afterwards from height and slope:
            // forest at the foot, alpine grass, grey-brown rock faces, snow on the gentler tops.
            // Resolution: rows ~20 m apart radially so the finest ridge octave (~62 m) resolves as
            // gullies instead of aliasing into long vertical streaks (it was 61 m at 40 bands).
            var centre=new Vector2(150,-1100);const int sectors=640,bands=120;
            var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();var detail=new List<float>();
            float Ridged(float x,float z,float f){return 1-Mathf.Abs(Mathf.PerlinNoise(x*f+31,z*f+17)*2-1);}
            for(int s=0;s<=sectors;s++)for(int b=0;b<=bands;b++) {
                float deg=Mathf.Lerp(62,298,s/(float)sectors),theta=deg*Mathf.Deg2Rad,u=b/(float)bands,r=Mathf.Lerp(2250,4700,u);
                // With Quarry Hills (docs/59) the ranges from south-south-west round to the west stand back behind the district.
                if(QuarryHills.Enabled){float back=Mathf.SmoothStep(0,1,Mathf.InverseLerp(185,215,deg));r=Mathf.Lerp(r,Mathf.Lerp(5600,8200,u),back);}
                float x=centre.x+Mathf.Sin(theta)*r,z=centre.y+Mathf.Cos(theta)*r;
                float range=Mathf.Sin(u*Mathf.PI);range*=range;
                float peaks=Mathf.PerlinNoise(x*.0011f+5,z*.0011f+9),ridge=Ridged(x,z,.0024f);
                float rid2=Ridged(x+900,z-400,.0063f),rid3=Ridged(x-300,z+700,.016f);
                float edge=Mathf.SmoothStep(0,1,Mathf.Min(s,sectors-s)/40f);
                float body=(180+peaks*650+ridge*280)*range;
                float height=Mathf.Lerp(-30,body+(rid2*90+rid3*28)*range*Mathf.Clamp01(body/300),edge*Mathf.SmoothStep(0,1,u*3.2f));
                v.Add(new Vector3(x,height,z));detail.Add(Mathf.PerlinNoise(x*.02f,z*.02f));
                if(s<sectors&&b<bands){int a=s*(bands+1)+b;t.AddRange(new[]{a,a+1,a+bands+1,a+1,a+bands+2,a+bands+1});}
            }
            var mesh=new Mesh{name="Distant ranges",indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var normals=mesh.normals;
            // Hollowness: how far a vertex sits below the mean of its ring of neighbours ~60 m out.
            // Gullies and valley floors darken, ridges and spurs catch the light (alpha channel).
            var hollow=new float[v.Count];
            for(int s=0;s<=sectors;s++)for(int b=0;b<=bands;b++) {
                float sum=0;int count=0;
                for(int ds=-3;ds<=3;ds+=3)for(int db=-3;db<=3;db+=3) {
                    int s2=s+ds,b2=b+db;if((ds==0&&db==0)||s2<0||s2>sectors||b2<0||b2>bands)continue;
                    sum+=v[s2*(bands+1)+b2].y;count++;
                }
                int k=s*(bands+1)+b;hollow[k]=count>0?Mathf.Clamp01((sum/count-v[k].y)/45*.5f+.5f):.5f;
            }
            for(int i=0;i<v.Count;i++) {
                float h=v[i].y,up=normals[i].y,n=detail[i];
                var rock=Color.Lerp(new Color(.52f,.46f,.40f),new Color(.40f,.37f,.35f),n);
                var forest=Color.Lerp(new Color(.13f,.25f,.15f),new Color(.19f,.31f,.18f),n);var alpine=Color.Lerp(new Color(.36f,.47f,.27f),new Color(.47f,.52f,.31f),n);
                float gentle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.86f,up));
                // Value pass (docs/visual-quality-audit.md): forest climbs the steep lower slopes to a ragged
                // treeline (~420 m), giving each range a dark base under pale rock and snow, the depth cue a
                // mid-grey mass lacked. Only sheer faces stay bare below it. Alpine grass on gentle ground above.
                float treeline=Blend(420,560,h+(n-.5f)*160),cliff=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.45f,up));
                var ground=Color.Lerp(forest,alpine,treeline);
                float covered=Mathf.Max((1-treeline)*cliff,gentle*(1-Blend(560,780,h+(n-.5f)*140)));
                var col=Color.Lerp(rock,ground,covered);
                // Snow caps only the high, gentler ground; steep faces stay bare rock.
                float snow=Blend(700,860,h+(n-.5f)*180)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.82f,up));
                var shaded=Color.Lerp(col,new Color(.95f,.96f,.98f),snow);shaded.a=hollow[i];c.Add(shaded);
            }
            mesh.SetColors(c);mesh.RecalculateBounds();
            var shader=Shader.Find("LittleRoad/Distant");
            var mat=new Material(shader!=null?shader:Shader.Find("Universal Render Pipeline/Unlit")){name="Distant ranges"};
            // Aerial perspective layers the ring: ~26% haze on the near foothills (2.3 km), ~48% on the
            // back ranges (4.7 km): ranges behind ranges, with rock and snow still reading (m34 at
            // .00022 washed them into a pale icy band).
            mat.SetColor("_HazeColor",new Color(.66f,.80f,.90f));mat.SetFloat("_Haze",.0001f);
            var go=new GameObject("Distant mountain ranges",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(valley);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r2=go.GetComponent<MeshRenderer>();r2.sharedMaterial=mat;r2.shadowCastingMode=ShadowCastingMode.Off;r2.receiveShadows=false;
        }
        // Woods on the hills and ridge, copses and hedgerow trees across the valley floor.
        // Woods by biome (firs up high, birches by the lake, autumn maples for colour), poplar
        // avenues along the country roads, blossom orchards, boulders on the slopes, reeds on the
        // banks, grass tufts and flowering bushes on the verges.
        void MainlandTrees()
        {
            if(!BayviewKit.Has("tree_round"))return;
            var holder=new GameObject("Mainland trees").transform;holder.SetParent(root);var rng=new System.Random(4127);int planted=0;bool nature=BayviewKit.Has("tree_fir");
            float R()=>(float)rng.NextDouble();
            bool Open(float x,float z,float road=13)=>ValleyField.RoadDistance(x,z)>=road&&!HillcrestTown.Reserved(x,z)&&HillcrestRoute.Distance(HillcrestRoute.Rail.line,x,z,out _)>=9;
            // Trees and bushes carry a far stand-in (KitLod), which is what lets the woods be dense.
            // (Decided now, in the same order; built now or streamed in on its tile: WorldTiles.)
            void Plant(string id,float x,float z,float scale,bool solid=true)
            {
                var at=new Vector3(x,ValleyField.Height(x,z)-.1f,z);float yaw=R()*360;if(!RaceArea.Near(x,z,60))return;   // (a race: its area only)
                // Bushes and tufts stay out of the river (street review, 4 Oct: a shrub stood on the water at the quay).
                if((id=="bush"||id=="flower_bush"||id=="grass_tuft")&&HillcrestRoute.Distance(HillcrestRoute.River.line,x,z,out _)<HillcrestRoute.River.width*.5f+1.5f)return;
                WorldTiles.Place(at,solid?1.2f*scale:0,()=>{var g=BayviewKit.Spawn(id,at,yaw,holder,solid:solid,scale:scale);
                    if(id.StartsWith("tree_")||id=="olive_tree"||id=="bush"||id=="flower_bush")KitLod.Attach(g,id);if(WorldTiles.Late)PlacementAudit.SettleOne(g);});
            }
            bool Woodland(float x,float z)=>ValleyField.Height(x,z)>=HillcrestRoute.LakeY+1.2f&&Open(x,z)&&ValleyField.Normal(x,z).y>=.8f&&Vector2.Distance(new(x,z),HillcrestRoute.BalloonField)>=110&&
                HillcrestRoute.Distance(HillcrestRoute.River.line,x,z,out _)>=16;
            string Species(float x,float z,float roll)
            {
                float h=ValleyField.Height(x,z);
                if(!nature)return h>40?"tree_pine":roll<.5f?"tree_round":"tree_cypress";
                if(h>55)return roll<.7f?"tree_fir":roll<.9f?"tree_pine":"tree_cypress";
                if(Vector2.Distance(new(x,z),HillcrestRoute.Lake)<230)return roll<.5f?"tree_birch":roll<.75f?"tree_round":"tree_poplar";
                return roll<.33f?"tree_round":roll<.46f?"tree_autumn":roll<.6f?"tree_pine":roll<.74f?"tree_cypress":roll<.87f?"olive_tree":"tree_birch";
            }
            // Copses: a centre where the woods noise is high, 3-12 trees round it, one main species.
            int copses=0;
            int treeCap=RaceTrial.Has("--perf-sparse-woods")?2200:5400;
            for(int i=0;i<40000&&planted<treeCap;i++) {
                float x=R()*2300-1080,z=R()*1580-1990;float woods=Mathf.PerlinNoise(x*.008f+11,z*.008f+23);
                if(woods<.47f||!Woodland(x,z))continue;
                int n=3+(int)(Mathf.Clamp01((woods-.47f)/.25f)*9+R()*2);float radius=6+woods*16;string dominant=Species(x,z,R());copses++;
                for(int k=0;k<n;k++) {
                    float a=R()*Mathf.PI*2,r=Mathf.Sqrt(R())*radius,px=x+Mathf.Cos(a)*r,pz=z+Mathf.Sin(a)*r;
                    if(!Woodland(px,pz))continue;
                    Plant(R()<.72f?dominant:Species(px,pz,R()),px,pz,.8f+R()*.65f);planted++;
                    if(R()<.3f){float bx=px+R()*4-2,bz=pz+R()*4-2;if(Open(bx,bz,11))Plant("bush",bx,bz,.8f+R()*.5f,false);}
                }
            }
            Debug.Log($"LITTLE_ROAD_COPSES copses={copses}");
            if(!nature){Debug.Log("LITTLE_ROAD_MAINLAND_TREES planted="+planted);return;}
            // Poplar avenues.
            int poplars=0;var main=HillcrestRoute.Mainline;
            foreach(var (road,from,to) in new[]{(main,HillcrestRoute.PortalSouthRun+70,main.Length-60),(HillcrestRoute.RacewayAccess,20f,HillcrestRoute.RacewayAccess.Length-80),(HillcrestRoute.Falls,20f,HillcrestRoute.Falls.Length-40),(HillcrestRoute.FarmLane,30f,HillcrestRoute.FarmLane.Length-60)})
                for(float d=from;d<to;d+=13)foreach(int side in new[]{-1,1}) {
                    var q=road.At(d)+road.Right(d)*side*(road.width*.5f+5.5f);
                    if(ValleyField.RoadDistance(q.x,q.z)<road.width*.5f+4.5f||HillcrestTown.Reserved(q.x,q.z)||ValleyField.Normal(q.x,q.z).y<.85f)continue;
                    if(HillcrestRoute.Distance(HillcrestRoute.River.line,q.x,q.z,out _)<14)continue;
                    Plant("tree_poplar",q.x,q.z,.95f+R()*.2f);poplars++;
                }
            // Blossom orchards.
            int orchard=0;
            foreach(var c in new[]{new Vector2(-40,-1310),new Vector2(430,-1190),new Vector2(-470,-960),new Vector2(-30,-1000)})
                for(float ox=-30;ox<=30;ox+=9)for(float oz=-24;oz<=24;oz+=9) {
                    float x=c.x+ox+R()*1.5f,z=c.y+oz+R()*1.5f;
                    if(!Open(x,z,12)||ValleyField.Normal(x,z).y<.88f||ValleyField.Height(x,z)<HillcrestRoute.LakeY+1.5f)continue;
                    Plant("tree_blossom",x,z,.85f+R()*.3f);orchard++;
                }
            // Boulders on slopes and summits; scree in the gorge.
            int rocks=0;
            for(int i=0;i<9000&&rocks<650;i++) {
                float x=R()*2300-1080,z=R()*1580-1990;float h=ValleyField.Height(x,z),ny=ValleyField.Normal(x,z).y;
                bool slope=ny>.5f&&ny<.86f,summit=h>70&&R()<.35f;
                if((!slope&&!summit)||h<0||!Open(x,z,10))continue;
                float roll=R();Plant(roll<.45f?"rock_a":roll<.75f?"rock_b":"rock_cluster",x,z,.7f+R()*1.3f,roll<.75f);rocks++;
            }
            // Sea stacks and boulders along the foot of the headland's sea cliffs.
            int seaRocks=0;
            for(float x=-640;x<780;x+=7+R()*6) {
                float z=ValleyField.RidgeZ(x);while(z<-386&&ValleyField.Height(x,z)>SeaLevel+.6f)z+=1.5f;
                if(z>=-386||(x>70&&x<164)||HillcrestRoute.Distance(HillcrestRoute.Causeway,x,z,out _)<9||HillcrestRoute.Distance(HillcrestRoute.SeaRail.line,x,z,out _)<7||HillcrestRoute.Distance(HillcrestRoute.River.line,x,z,out _)<34)continue;
                float out1=R()*9-2;var q=new Vector3(x+R()*3,0,z+out1);float roll=R();
                // Stand on the seabed (a fixed height left rocks floating up to 12 m over the drop-off beyond the
                // cliff foot, PlacementAudit), stretched taller where needed so each still breaks the surface.
                float bed=ValleyField.Height(q.x,q.z),sink=R()*.8f;
                var rock=BayviewKit.Spawn(roll<.5f?"rock_cluster":roll<.8f?"rock_b":"rock_a",new Vector3(q.x,Mathf.Min(bed-.3f,SeaLevel-1.2f-sink),q.z),R()*360,holder,solid:false,scale:2.2f+R()*3.2f);
                var rs=rock.GetComponentsInChildren<Renderer>();if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);float want=SeaLevel+.5f+sink;
                    if(b.max.y<want&&b.size.y>.1f){var sc=rock.transform.localScale;sc.y*=Mathf.Min(4,(want-b.min.y)/b.size.y);rock.transform.localScale=sc;}}
                seaRocks++;
            }
            Debug.Log($"LITTLE_ROAD_SEA_ROCKS placed={seaRocks}");
            // Reeds along the river banks and round the lake.
            int reeds=0;var river=HillcrestRoute.River;
            // (From where Hillcrest's own river starts: Quarry Hills plants its own upper reach.)
            for(float d=40+(QuarryHills.Enabled?river.Nearest(new Vector3(-1180,0,-1330),out _):0);d<river.Length;d+=6)foreach(int side in new[]{-1,1}) {
                if(R()<.35f)continue;var c=river.At(d);if(c.y<.5f)break;
                var q=c+river.Right(d)*side*(10.5f+R()*2.5f);float h=ValleyField.Height(q.x,q.z);
                if(h<c.y-.8f||h>c.y+1.4f||ValleyField.RoadDistance(q.x,q.z)<8)continue;
                Plant("reeds",q.x,q.z,.8f+R()*.5f,false);reeds++;
            }
            for(float a=0;a<360;a+=5) {
                var dir=new Vector2(Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad));
                for(float r=HillcrestRoute.LakeRadius-12;r<HillcrestRoute.LakeRadius+34;r+=1.5f){var q=HillcrestRoute.Lake+dir*r;if(ValleyField.Height(q.x,q.y)>HillcrestRoute.LakeY+.2f){if(ValleyField.RoadDistance(q.x,q.y)>10&&R()<.7f){Plant("reeds",q.x,q.y,.8f+R()*.5f,false);reeds++;}break;}}
            }
            // Verges: grass tufts and flowering bushes.
            int tufts=0,bushes=0;
            foreach(var road in HillcrestRoute.All())for(float d=0;d<road.Length;d+=2.2f) {
                if(QuarryHills.Owns(road.name))break; // Quarry Hills plants its own verges
                if(road==main&&d<HillcrestRoute.PortalSouthRun+8)continue;
                int side=R()<.5f?-1:1;var q=road.At(d)+road.Right(d)*side*(road.width*.5f+2.6f+R()*6);
                if(ValleyField.RoadDistance(q.x,q.z)<road.width*.5f+2.3f||ValleyField.Height(q.x,q.z)<road.At(d).y-3||HillcrestTown.Reserved(q.x,q.z)||(q.z>-960&&q.z<-870&&q.x>-110&&q.x<470))continue;
                if(R()<.12f){Plant("flower_bush",q.x,q.z,.7f+R()*.5f,false);bushes++;}else{Plant("grass_tuft",q.x,q.z,.9f+R()*.8f,false);tufts++;}
            }
            for(int i=0;i<5000&&tufts<5200;i++) {
                float x=R()*1500-620,z=R()*900-1500;if(!Open(x,z,9)||ValleyField.Normal(x,z).y<.9f||ValleyField.Height(x,z)<HillcrestRoute.LakeY+.8f)continue;
                for(int k=0;k<5;k++){float px=x+R()*6-3,pz=z+R()*6-3;Plant(k==0&&R()<.3f?"flower_bush":"grass_tuft",px,pz,.8f+R()*.9f,false);tufts++;}
            }
            Debug.Log($"LITTLE_ROAD_MAINLAND_TREES planted={planted} poplars={poplars} orchard={orchard} rocks={rocks} reeds={reeds} tufts={tufts} bushes={bushes}");
        }
        public void RegisterRoads()
        {
            FreeRoamRoads.Clear();FreeRoamRoads.Register(track.points);
            foreach(var road in HillcrestRoute.All())FreeRoamRoads.Register(road.line);
            var loop=new List<Vector3>(HillcrestRoute.Circuit){HillcrestRoute.Circuit[0]};FreeRoamRoads.Register(loop.ToArray());
        }
    }
    // Churning foam: swells, shrinks and drifts a little.
    public sealed class FoamPuff:MonoBehaviour
    {
        public float phase;Vector3 home,size;bool ready;
        void Update()
        {
            if(!ready){home=transform.position;size=transform.localScale;ready=true;}
            float t=Time.time*1.7f+phase;transform.localScale=size*(.8f+.25f*Mathf.Sin(t));transform.position=home+new Vector3(Mathf.Sin(t*.6f),Mathf.Sin(t*1.3f)*.25f,Mathf.Cos(t*.5f))*.6f;
        }
    }
    // Scrolls a water ribbon's texture downstream.
    public sealed class FlowingWater:MonoBehaviour
    {
        public float speed=.1f;Material material;
        void Start()=>material=GetComponent<Renderer>().material;
        void Update(){if(material!=null)material.mainTextureOffset=new Vector2(0,-Time.time*speed);}
    }
}
