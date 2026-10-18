using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleRoad
{
    // Prop Forge (docs/76, docs/77): the world's run kits (walls, fences) and objects (bales, jerry cans) are built in
    // car-forge (forge/world) and synced as build output into Resources/Props: <kit>.json, the contract (schema
    // prop-forge-kit/0), and <kit>_mesh.bytes, the mesh data (8 floats a vertex: position, normal, uv; then uint32
    // indices), already in Unity's axes. Off by default: --prop-forge (and the --run-range fixture).
    public static class PropForge
    {
        public static bool Enabled=>RaceTrial.Has("--prop-forge")||RunRange.Requested;
    }
    [Serializable] public sealed class PropVec{public float x,y,z;public Vector3 V=>new(x,y,z);}
    [Serializable] public sealed class PropSub{public string material;public int start,count;}
    [Serializable] public sealed class PropLod{public int vertexStart,vertexCount,triangles;public PropSub[] submeshes;}
    [Serializable] public sealed class PropCollision{public string type;public PropVec centre,size;}
    [Serializable] public sealed class PropSocket{public string name;public PropVec at,forward;}
    [Serializable] public sealed class PropPiece{public string name,role,ground;public int variant;public float length,skirt;public float[] stretch,lengths,sink;
        public PropVec[] contact;public PropCollision collision;public PropSocket[] sockets;public PropLod[] lods;}
    // (a deforming kit adds: deform, the slice spacing it bends at; halfBase, half its foot's width; copeFrom and height,
    //  where its cope starts and its crown; copeJitter, how far the cope's top wobbles; sink, how far its base goes in)
    // (an object family adds: sameGap and sameYaw, how near and how alike in yaw the same variant may stand; rollSlope and
    //  rolls, the slope above which the pieces that roll turn their axis downhill)
    [Serializable] public sealed class PropRun{public float spacing,minRadius,maxTurn,sagitta,deform,halfBase,copeFrom,copeJitter,height,sink,sameGap,sameYaw,rollSlope;
        public float[] railHeights,clearance,opening;public string[] rolls;}
    [Serializable] public sealed class PropKitFile{public string schema,kit,hash,kind,doc;public int version,vertexCount,indexByteOffset,indexCount;
        public string[] materials;public PropRun run;public PropPiece[] pieces;}

    // One kit, read once: its contract, and its pieces' meshes built on demand (per piece and LOD, shared).
    public sealed class PropKit
    {
        public const string Schema="prop-forge-kit/0";
        public readonly PropKitFile file;
        readonly float[] vertices;readonly uint[] indices;
        readonly Dictionary<(string,int),Mesh> meshes=new();
        static readonly Dictionary<string,PropKit> loaded=new();
        static readonly Dictionary<string,Material> materials=new();

        PropKit(PropKitFile f,byte[] blob)
        {
            file=f;vertices=new float[f.vertexCount*8];Buffer.BlockCopy(blob,0,vertices,0,vertices.Length*4);
            indices=new uint[f.indexCount];Buffer.BlockCopy(blob,f.indexByteOffset,indices,0,indices.Length*4);
        }
        // The kit, or an exception naming what's wrong (a missing kit fails loudly: the sync writes them all).
        public static PropKit Load(string id)
        {
            if(loaded.TryGetValue(id,out var k))return k;
            var json=Resources.Load<TextAsset>("Props/"+id);var blob=Resources.Load<TextAsset>("Props/"+id+"_mesh");
            if(json==null||blob==null)throw new InvalidOperationException($"Prop Forge kit '{id}' is not installed (car-forge tools/sync_props.py)");
            var f=JsonUtility.FromJson<PropKitFile>(json.text);
            if(f.schema!=Schema)throw new InvalidOperationException($"Prop Forge kit '{id}': schema {f.schema}, this game reads {Schema}");
            if(blob.bytes.Length!=f.indexByteOffset+f.indexCount*4)throw new InvalidOperationException($"Prop Forge kit '{id}': mesh data is {blob.bytes.Length} bytes, the contract says {f.indexByteOffset+f.indexCount*4}");
            return loaded[id]=new PropKit(f,blob.bytes);
        }
        public static void Forget()=>loaded.Clear();
        public PropPiece Piece(string name)
        {
            foreach(var p in file.pieces)if(p.name==name)return p;
            throw new KeyNotFoundException($"Prop Forge kit '{file.kit}' has no piece '{name}'");
        }
        // A piece's mesh at a LOD: its own vertices, one submesh per material class (in the order MaterialsOf gives).
        public Mesh Mesh(string piece,int lod=0)
        {
            if(meshes.TryGetValue((piece,lod),out var m))return m;
            var p=Piece(piece);var l=p.lods[Mathf.Clamp(lod,0,p.lods.Length-1)];
            var pos=new Vector3[l.vertexCount];var nrm=new Vector3[l.vertexCount];var uv=new Vector2[l.vertexCount];
            for(int i=0;i<l.vertexCount;i++){int o=(l.vertexStart+i)*8;pos[i]=new(vertices[o],vertices[o+1],vertices[o+2]);nrm[i]=new(vertices[o+3],vertices[o+4],vertices[o+5]);uv[i]=new(vertices[o+6],vertices[o+7]);}
            // (uv1 and uv2 are the kit shader's surface coordinate and height above the base, for its non-world-space
            //  patterns: the kit's own metres and the height up the piece)
            var height=new Vector2[l.vertexCount];for(int i=0;i<l.vertexCount;i++)height[i]=new Vector2(pos[i].y,0);
            m=new Mesh{name=$"{file.kit}.{piece}.lod{lod}"};m.SetVertices(pos);m.SetNormals(nrm);m.SetUVs(0,uv);m.SetUVs(1,uv);m.SetUVs(2,height);m.subMeshCount=l.submeshes.Length;
            for(int s=0;s<l.submeshes.Length;s++){var sub=l.submeshes[s];var tri=new int[sub.count];for(int i=0;i<sub.count;i++)tri[i]=(int)indices[sub.start+i]-l.vertexStart;m.SetTriangles(tri,s,false);}
            m.RecalculateBounds();return meshes[(piece,lod)]=m;
        }
        public Material[] MaterialsOf(string piece,int lod=0)
        {
            var subs=Piece(piece).lods[Mathf.Clamp(lod,0,Piece(piece).lods.Length-1)].submeshes;var mats=new Material[subs.Length];
            for(int s=0;s<subs.Length;s++)mats[s]=MaterialFor(subs[s].material);return mats;
        }
        // A material class to the game's kit look (WorldLook.Kit: graded colour and pattern; docs/art-direction.md). An
        // unknown class fails loudly, as the car materials do.
        public static Material MaterialFor(string cls)
        {
            if(materials.TryGetValue(cls,out var m))return m;
            m=cls switch {
                "timber"=>WorldLook.Kit("PF Timber",new Color(.55f,.4f,.26f),WorldLook.Pattern.Wood,0,.2f,true),
                "paint"=>WorldLook.Kit("PF Paint",new Color(.92f,.91f,.86f),WorldLook.Pattern.Paint,0,.3f),
                "stone"=>WorldLook.Kit("PF Stone",new Color(.62f,.6f,.55f),WorldLook.Pattern.Stone,0,.15f,true),
                "red_paint"=>WorldLook.Kit("PF Red Paint",new Color(.72f,.13f,.09f),WorldLook.Pattern.Paint,.1f,.4f),
                "drystone"=>WorldLook.Kit("PF DryStone",new Color(.6f,.58f,.53f),WorldLook.Pattern.DryStone,0,.08f),
                "silage"=>WorldLook.Kit("PF Silage",new Color(.14f,.2f,.15f),WorldLook.Pattern.Plain,0,.55f),
                "metal"=>WorldLook.Kit("PF Metal",new Color(.6f,.62f,.65f),WorldLook.Pattern.Plain,.6f,.45f),
                "rubber"=>WorldLook.Kit("PF Rubber",new Color(.12f,.12f,.13f),WorldLook.Pattern.Plain,0,.15f),
                "straw"=>WorldLook.Kit("PF Straw",new Color(.86f,.74f,.42f),WorldLook.Pattern.Straw,0,.08f),
                "plastic"=>WorldLook.Kit("PF Plastic",new Color(.78f,.16f,.12f),WorldLook.Pattern.Plain,0,.4f),
                "tint_wall"=>WorldLook.Kit("PF Tint_Wall",new Color(.9f,.88f,.82f),WorldLook.Pattern.Plaster,0,.2f),
                "glass"=>WorldLook.Kit("PF Glass",new Color(.5f,.6f,.66f),WorldLook.Pattern.Glass,0,.8f),
                _=>throw new InvalidOperationException($"Prop Forge material class '{cls}' has no look (PropKit.MaterialFor)")};
            return materials[cls]=m;
        }
    }

    // One ground for the run solver and the audits (docs/77; the architecture review 6.2): a provider when one is set
    // (the --run-range fixture sets its own), else the island's field north of z=-380 and the valley field south of it.
    // (Road surfaces come first in a provider that knows them; the live world's provider gains them at the proving
    // street, step 6.)
    public static class Ground
    {
        public interface IProvider{float Height(float x,float z);}
        static IProvider provider;
        public static void Use(IProvider p)=>provider=p;
        public static void Reset()=>provider=null;
        public static bool Custom=>provider!=null;
        public static float Surface(float x,float z)
        {
            if(provider!=null)return provider.Height(x,z);
            return z>-380&&CoastalLandscape.Current!=null?CoastalLandscape.Current.Height(x,z):ValleyField.Height(x,z);
        }
        public static Vector3 Normal(float x,float z,float d=.25f)
        {
            float hx=Surface(x+d,z)-Surface(x-d,z),hz=Surface(x,z+d)-Surface(x,z-d);
            return new Vector3(-hx,2*d,-hz).normalized;
        }
    }
}
