using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Car-high wheat you can drive through. Stalks bend over under the car and stay flattened
    // (crop circles welcome): a trample texture over the field (R flat, GB push direction)
    // drives the LittleRoad/Wheat shader. Stalk meshes are chunked with a canopy-sheet far LOD.
    public sealed class WheatField:MonoBehaviour
    {
        public static readonly List<WheatField> All=new();
        // The adventure player's car (set by ValleyLife); only it tramples.
        public static RoadCar Player;
        // The Harvest (FarmChallenges, doc 64): extra half-width cut by the harvester header on the player's bumper.
        public static float Header;
        const float Texel=.4f,Chunk=20;
        Vector3 corner,axisX,axisZ;Vector2 size;int w,h;Texture2D trample;Color32[] pixels;bool dirty;int flattened;float chaffTimer;
        Chaff chaff;
        public float Flattened=>flattened/(float)(w*h);
        public bool Contains(Vector3 p,float margin=0){var rel=p-corner;float x=Vector3.Dot(rel,axisX),z=Vector3.Dot(rel,axisZ);return x>-margin&&z>-margin&&x<size.x+margin&&z<size.y+margin;}

        public static WheatField Create(string name,Transform parent,Vector3 centre,Vector2 size,float yaw,System.Func<float,float,float> ground,int seed,float height=1.6f)
        {
            WheatField field=null;TrialWorld.RunNow(CreateRoutine(name,parent,centre,size,yaw,ground,seed,height,f=>field=f));return field;
        }
        // As a coroutine (a background district, --near-start): a frame between chunks once 8 ms have gone; the draws are the same.
        public static System.Collections.IEnumerator CreateRoutine(string name,Transform parent,Vector3 centre,Vector2 size,float yaw,System.Func<float,float,float> ground,int seed,float height=1.6f,System.Action<WheatField> made=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);var field=go.AddComponent<WheatField>();yield return field.Build(centre,size,yaw,ground,seed,height);All.Add(field);made?.Invoke(field);
        }
        void OnDestroy()=>All.Remove(this);
        // Every stalk standing again (a fresh attempt at the Harvest).
        public void Regrow(){for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,128,128,255);flattened=0;dirty=true;}

        System.Collections.IEnumerator Build(Vector3 centre,Vector2 fieldSize,float yaw,System.Func<float,float,float> ground,int seed,float height)
        {
            size=fieldSize;var rot=Quaternion.Euler(0,yaw,0);axisX=rot*Vector3.right;axisZ=rot*Vector3.forward;
            corner=centre-axisX*size.x*.5f-axisZ*size.y*.5f;corner.y=0;
            w=Mathf.CeilToInt(size.x/Texel);h=Mathf.CeilToInt(size.y/Texel);
            trample=new Texture2D(w,h,TextureFormat.RGBA32,false,true){name=name+" trample",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            pixels=new Color32[w*h];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(0,128,128,255);trample.SetPixels32(pixels);trample.Apply(false);
            var material=new Material(Shader.Find("LittleRoad/Wheat")){name="Wheat"};
            material.SetTexture("_Trample",trample);material.SetVector("_FieldOrigin",new Vector4(corner.x,corner.z,size.x,size.y));
            material.SetVector("_FieldAxis",new Vector4(axisX.x,axisX.z,axisZ.x,axisZ.z));
            var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
            Vector3 At(float x,float z){var p=corner+axisX*x+axisZ*z;p.y=ground(p.x,p.z);return p;}
            // Stubble and soil under the crop.
            Sheet("Wheat stubble",At,TrialWorld.Material("Wheat stubble",new Color(.66f,.53f,.30f),0,.08f),.04f,2);
            int chunks=0,tufts=0;
            var slice=System.Diagnostics.Stopwatch.StartNew();
            for(float cx=0;cx<size.x-.01f;cx+=Chunk)for(float cz=0;cz<size.y-.01f;cz+=Chunk) {
                if(slice.ElapsedMilliseconds>=8){yield return null;slice.Restart();}
                float ex=Mathf.Min(size.x,cx+Chunk),ez=Mathf.Min(size.y,cz+Chunk);
                var v=new List<Vector3>();var roots=new List<Vector3>();var col=new List<Color>();var t=new List<int>();
                for(float x=cx+.28f;x<ex;x+=.56f)for(float z=cz+.28f;z<ez;z+=.56f) {
                    var root=At(x+(R()-.5f)*.4f,z+(R()-.5f)*.4f);float tall=height*(.86f+R()*.24f);float a=R()*Mathf.PI;
                    float tone=R();var stem=Color.Lerp(new Color(.80f,.66f,.30f),new Color(.88f,.74f,.34f),tone);var ear=Color.Lerp(new Color(.96f,.78f,.33f),new Color(1f,.87f,.46f),tone);
                    for(int blade=0;blade<2;blade++) {
                        var across=new Vector3(Mathf.Cos(a+blade*1.571f),0,Mathf.Sin(a+blade*1.571f));int s=v.Count;
                        void Level(float f,float half,Color c){v.Add(root+Vector3.up*tall*f-across*half);v.Add(root+Vector3.up*tall*f+across*half);roots.Add(root);roots.Add(root);col.Add(c);col.Add(c);}
                        Level(0,.035f,new Color(.52f,.44f,.20f));Level(.6f,.045f,stem);Level(.8f,.085f,ear);
                        v.Add(root+Vector3.up*tall);roots.Add(root);col.Add(new Color(1f,.92f,.60f));
                        t.AddRange(new[]{s,s+2,s+1,s+1,s+2,s+3,s+2,s+4,s+3,s+3,s+4,s+5,s+4,s+6,s+5});
                    }
                    tufts++;
                }
                var stalks=BuildMesh("Wheat stalks",v,roots,col,t);
                // Far LOD: a wavy canopy sheet at ear height that bends the same way.
                var cv=new List<Vector3>();var cr=new List<Vector3>();var cc=new List<Color>();var ct=new List<int>();int nx=Mathf.CeilToInt((ex-cx)/1f),nz=Mathf.CeilToInt((ez-cz)/1f);
                for(int i=0;i<=nz;i++)for(int j=0;j<=nx;j++) {
                    var root=At(Mathf.Lerp(cx,ex,j/(float)nx),Mathf.Lerp(cz,ez,i/(float)nz));cv.Add(root+Vector3.up*height*.9f);cr.Add(root);
                    cc.Add(Color.Lerp(new Color(.90f,.74f,.34f),new Color(.98f,.84f,.42f),Mathf.PerlinNoise(root.x*.3f,root.z*.3f)));
                    if(i<nz&&j<nx){int q=i*(nx+1)+j;ct.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}
                }
                var canopy=BuildMesh("Wheat canopy",cv,cr,cc,ct);
                var chunk=new GameObject($"Wheat chunk {chunks++}");chunk.transform.SetParent(transform,false);
                var near=Part(chunk.transform,"Stalks",stalks,material);var far=Part(chunk.transform,"Canopy",canopy,material);
                chunk.AddComponent<LODGroup>().SetLODs(new[]{new LOD(.12f,new[]{near}),new LOD(.004f,new[]{far})});
            }
            chaff=new GameObject(name+" chaff").AddComponent<Chaff>();chaff.transform.SetParent(transform,false);chaff.Setup();
            Debug.Log($"LITTLE_ROAD_WHEAT field={name} size={size.x:0}x{size.y:0} tufts={tufts} chunks={chunks}");
        }
        void Sheet(string name,System.Func<float,float,Vector3> at,Material material,float lift,float step)
        {
            var v=new List<Vector3>();var t=new List<int>();int nx=Mathf.CeilToInt(size.x/step),nz=Mathf.CeilToInt(size.y/step);
            for(int i=0;i<=nz;i++)for(int j=0;j<=nx;j++){v.Add(at(size.x*j/nx,size.y*i/nz)+Vector3.up*lift);if(i<nz&&j<nx){int q=i*(nx+1)+j;t.AddRange(new[]{q,q+nx+1,q+1,q+1,q+nx+1,q+nx+2});}}
            var mesh=new Mesh{name=name,indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(transform,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static Mesh BuildMesh(string name,List<Vector3> v,List<Vector3> roots,List<Color> col,List<int> t)
        {
            var mesh=new Mesh{name=name,indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};mesh.SetVertices(v);mesh.SetUVs(1,roots);mesh.SetColors(col);mesh.SetTriangles(t,0);
            var up=new Vector3[v.Count];for(int i=0;i<up.Length;i++)up[i]=Vector3.up;mesh.normals=up;
            mesh.RecalculateBounds();var b=mesh.bounds;b.Expand(new Vector3(3,1,3));mesh.bounds=b;return mesh;
        }
        static Renderer Part(Transform parent,string name,Mesh mesh,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return r;
        }

        // Flattens the stalks under an oriented footprint; returns how many were freshly knocked down.
        public int Press(Vector3 centre,Vector3 forward,Vector3 push,float halfLength,float halfWidth,float soft=.5f)
        {
            forward.y=0;forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);push.y=0;push.Normalize();
            byte g=(byte)Mathf.Clamp(Mathf.RoundToInt((push.x*.5f+.5f)*255),0,255),b=(byte)Mathf.Clamp(Mathf.RoundToInt((push.z*.5f+.5f)*255),0,255);
            var rel=centre-corner;float lx=Vector3.Dot(rel,axisX),lz=Vector3.Dot(rel,axisZ);float reach=Mathf.Sqrt(halfLength*halfLength+halfWidth*halfWidth)+soft;
            int i0=Mathf.Max(0,Mathf.FloorToInt((lx-reach)/Texel)),i1=Mathf.Min(w-1,Mathf.CeilToInt((lx+reach)/Texel)),j0=Mathf.Max(0,Mathf.FloorToInt((lz-reach)/Texel)),j1=Mathf.Min(h-1,Mathf.CeilToInt((lz+reach)/Texel));
            int fresh=0;
            for(int j=j0;j<=j1;j++)for(int i=i0;i<=i1;i++) {
                var p=corner+axisX*((i+.5f)*Texel)+axisZ*((j+.5f)*Texel)-centre;p.y=0;
                float along=Mathf.Abs(Vector3.Dot(p,forward)),across=Mathf.Abs(Vector3.Dot(p,right));
                float amount=along<halfLength&&across<halfWidth?1:along<halfLength+soft&&across<halfWidth+soft?.55f:0;if(amount<=0)continue;
                int k=j*w+i;byte value=(byte)(amount*255);if(value<=pixels[k].r)continue;
                if(pixels[k].r<128&&value>=128){fresh++;flattened++;}
                pixels[k]=new Color32(value,g,b,255);dirty=true;
            }
            return fresh;
        }
        void Update()
        {
            var car=Player;
            if(chaff!=null&&car!=null&&car.body!=null&&Contains(car.transform.position,3)&&Mathf.Abs(car.speed)>.4f) {   // (chaff: made last, the field built over several frames in the background)
                var f=car.transform.forward;int fresh=Press(car.transform.position+f*(Header>0?1.4f:0),f,car.speed>=0?f:-f,2.3f+(Header>0?1:0),1.2f+Header);
                if(fresh>0) {
                    // Pushing through standing wheat: a little drag and a spray of chaff (a header cuts it, so less drag).
                    float drag=Mathf.Clamp01(fresh/(30f*(1+Header)))*.9f*Time.deltaTime;car.body.linearVelocity*=1-drag;
                    chaffTimer-=Time.deltaTime;
                    if(chaffTimer<=0){chaffTimer=.05f;var r=car.transform.right;foreach(int side in new[]{-1,1})chaff.Burst(car.transform.position+f*1.9f+r*side*1.1f+Vector3.up*1.1f,f*Mathf.Abs(car.speed)*.25f+r*side*1.4f);}
                }
            }
            if(dirty){trample.SetPixels32(pixels);trample.Apply(false);dirty=false;}
        }
        // Straw and chaff kicked up from the wheat.
        public sealed class Chaff:PuffPool
        {
            public void Setup(){soft=true;material=SoftMaterial("Wheat chaff",new Color(.97f,.84f,.46f));}   // (soft: opaque, the chaff read as yellow balls)
            public void Burst(Vector3 at,Vector3 velocity)=>Emit(at,velocity+Random.insideUnitSphere*1.2f+Vector3.up*1.6f,Random.Range(.22f,.4f),Random.Range(.7f,1.1f));
            void Update()=>Step(Time.deltaTime);
        }
    }
}
