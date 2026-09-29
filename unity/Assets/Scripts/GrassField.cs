using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Meadow grass and wildflower drifts around the camera (graphics scorecard item 22: the
    // meadows read as flat felt close up). The ground within `Radius` is split into 8 m cells;
    // each cell is filled once, deterministically, by raycasting onto the real ground (never
    // roads, buildings, water or reserved plots), cached, and drawn instanced (LittleRoad/Grass).
    // Cells beyond reach are dropped. Off in batch mode (nothing renders) and with --no-grass.
    public sealed class GrassField:MonoBehaviour
    {
        public const float Radius=44,Cell=8;const int PerCell=260;
        // Cells fill a few candidates at a time within this budget per frame. Filling up to four whole cells
        // in one frame (1,000+ rays and placement checks) was a 20-40 ms hitch every ~8 m at driving speed
        // (drive benchmark db4). The random sequence per cell is unchanged, so the grass lands identically.
        const double BudgetMs=1.0;
        readonly System.Diagnostics.Stopwatch clock=new();
        Vector2Int building;bool isBuilding;int candidate,clumps;bool nearRiver;System.Random rng;
        readonly List<Matrix4x4> buildGrass=new(PerCell),buildBloom=new();
        Camera view;Mesh blades,flowers;Material material;
        readonly Dictionary<Vector2Int,(Matrix4x4[] grass,Matrix4x4[] bloom)> cells=new();
        readonly Queue<Vector2Int> pending=new();readonly HashSet<Vector2Int> queued=new();
        readonly Matrix4x4[] batch=new Matrix4x4[1023];
        readonly RaycastHit[] hits=new RaycastHit[8];
        public int Clumps {get;private set;}
        // Diagnostics: why candidates were refused (logged every 5 s as LITTLE_ROAD_GRASS).
        int refusedPatch,refusedOpen,refusedMiss,refusedSurface,refusedLow;float nextLog;
        public static bool Wanted=>!Application.isBatchMode&&!RaceTrial.Has("--no-grass")&&GraphicsOptions.GrassDensity>0;
        public void Setup(Camera camera)
        {
            view=camera;var shader=Shader.Find("LittleRoad/Grass");Debug.Log("LITTLE_ROAD_GRASS setup shader="+(shader!=null));if(shader==null){enabled=false;return;}
            WorldLook.EnsureNoise();
            material=new Material(shader){name="Meadow grass",enableInstancing=true};
            material.SetColor("_Base",WorldLook.Surface(WorldLook.ToyGrassDeep*.72f));material.SetColor("_Tip",WorldLook.Surface(WorldLook.ToyGrassLight*1.12f));
            material.SetColor("_Dry",WorldLook.Surface(new Color(.72f,.66f,.36f)));material.SetFloat("_Radius",Radius);
            blades=Clump(false);flowers=Clump(true);
        }
        // One clump: 7 tapered, bent blades; flowered clumps add four stems with coloured heads.
        static Mesh Clump(bool flowered)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var c=new List<Color>();var t=new List<int>();var rng=new System.Random(flowered?71:17);
            float R()=>(float)rng.NextDouble();
            void Blade(float yaw,float lean,float height,float width)
            {
                var d=Quaternion.Euler(0,yaw,0);var outward=d*Vector3.forward;var side=d*Vector3.right;int b=v.Count;
                for(int k=0;k<3;k++){float h=k/3f,w=width*(1-h*.75f);var mid=outward*lean*h*h*height+Vector3.up*h*height;
                    v.Add(mid-side*w*.5f);v.Add(mid+side*w*.5f);uv.Add(new Vector2(0,h));uv.Add(new Vector2(1,h));c.Add(Color.clear);c.Add(Color.clear);}
                v.Add(outward*lean*height+Vector3.up*height);uv.Add(new Vector2(.5f,1));c.Add(Color.clear);
                for(int k=0;k<2;k++){int a=b+k*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                t.AddRange(new[]{b+4,b+6,b+5});
            }
            // A soft tuft: many short blades leaning out at varied angles (a carpet, not agave rosettes).
            for(int i=0;i<9;i++)Blade(i*40f+R()*30,.35f+R()*.45f,.16f+R()*.2f,.05f+R()*.03f);
            if(flowered) {
                Color[] heads={new(1,.97f,.9f),new(1,.84f,.25f),new(1,.55f,.72f),new(.72f,.62f,1)};
                for(int i=0;i<4;i++) {
                    float yaw=i*90+R()*40,height=.42f+R()*.18f;var d=Quaternion.Euler(0,yaw,0);var top=d*Vector3.forward*.08f+Vector3.up*height;
                    Blade(yaw,.12f,height,.03f);
                    // A small four-petal head facing up.
                    int b=v.Count;var col=WorldLook.Surface(heads[(i+(int)(R()*4))%heads.Length]);col.a=1;float s=.055f;
                    foreach(var o in new[]{Vector3.zero,new Vector3(s,0,0),new Vector3(0,0,s),new Vector3(-s,0,0),new Vector3(0,0,-s)}){v.Add(top+o+Vector3.up*.01f);uv.Add(new Vector2(.5f,1));c.Add(col);}
                    t.AddRange(new[]{b,b+2,b+1,b,b+3,b+2,b,b+4,b+3,b,b+1,b+4});
                }
            }
            var mesh=new Mesh{name=flowered?"Wildflower clump":"Grass clump"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        // Where grass may grow: open meadow, not road, verge paving, buildings, water or reserved plots.
        static bool Open(float x,float z,bool nearRiver)
        {
            if(z<-380) {
                if(ValleyField.RoadDistance(x,z)<8.5f||HillcrestTown.Reserved(x,z))return false;
                if(Vector2.Distance(new(x,z),HillcrestRoute.Lake)<HillcrestRoute.LakeRadius+4)return false;
                if(nearRiver&&HillcrestRoute.Distance(HillcrestRoute.River.line,x,z,out _)<17)return false;
                return true;
            }
            if(HillcrestRoute.OnHaltQuay(new Vector3(x,0,z),4))return false;
            var land=CoastalLandscape.Current;return land!=null&&land.SolidClearance(new Vector3(x,0,z));
        }
        void Begin(Vector2Int key)
        {
            building=key;isBuilding=true;candidate=0;rng=new System.Random(key.x*73856093^key.y*19349663);buildGrass.Clear();buildBloom.Clear();
            float mx=(key.x+.5f)*Cell,mz=(key.y+.5f)*Cell;
            nearRiver=mz<-380&&HillcrestRoute.Distance(HillcrestRoute.River.line,mx,mz,out _)<17+Cell;
            clumps=Mathf.RoundToInt(PerCell*GraphicsOptions.GrassDensity);
        }
        // Places up to `count` more candidates of the cell being built; true when the cell is finished.
        bool Step(int count)
        {
            float R()=>(float)rng.NextDouble();var key=building;var grass=buildGrass;var bloom=buildBloom;
            for(int n=0;n<count&&candidate<clumps;n++,candidate++) {
                float x=(key.x+R())*Cell,z=(key.y+R())*Cell;float keep=R(),yaw=R()*360,scale=.55f+R()*.8f,flower=R();
                // Clumpy meadow: denser in some patches, thin in others.
                if(Mathf.PerlinNoise(x*.09f+11,z*.09f+7)+keep*.6f<.5f){refusedPatch++;continue;}
                if(!Open(x,z,nearRiver)){refusedOpen++;continue;}
                // Mainland rays start 40 m above the valley ground (shorter rays meet fewer colliders).
                float from=z<-380?ValleyField.Height(x,z)+40:400;
                int found=Physics.RaycastNonAlloc(new Vector3(x,from,z),Vector3.down,hits,from+500,~0,QueryTriggerInteraction.Collide);
                RaycastHit? top=null;for(int k=0;k<found;k++)if(top==null||hits[k].point.y>top.Value.point.y)top=hits[k];
                if(top==null){refusedMiss++;continue;}var hit=top.Value;
                if(hit.normal.y<.7f||hit.collider.isTrigger||hit.collider.CompareTag("Respawn")||hit.collider.GetComponent<RoadSurface>()!=null||hit.collider.GetComponent<RoadSupport>()!=null){refusedSurface++;if(refusedSurface<6)Debug.Log($"LITTLE_ROAD_GRASS refused surface {hit.collider.name} trigger={hit.collider.isTrigger} normalY={hit.normal.y:0.00} at={hit.point}");continue;}
                if(hit.point.y<CoastalLandscape.SeaLevel+2.5f){refusedLow++;continue;} // beaches and the tide line
                var m=Matrix4x4.TRS(hit.point-hit.normal*.02f,Quaternion.FromToRotation(Vector3.up,Vector3.Lerp(Vector3.up,hit.normal,.6f))*Quaternion.Euler(0,yaw,0),Vector3.one*scale);
                // Wildflower drifts: long soft patches of flowering clumps.
                bool drift=Mathf.PerlinNoise(x*.021f+3.3f,z*.021f+8.1f)>.55f;
                if(drift&&flower<.45f)bloom.Add(m);else grass.Add(m);
            }
            if(candidate<clumps)return false;
            cells[key]=(grass.ToArray(),bloom.ToArray());isBuilding=false;return true;
        }
        // Update, not LateUpdate: screenshot captures render the camera from coroutines, which run
        // after Update and before LateUpdate, and must include the grass like the frame itself.
        void Update()
        {
            if(view==null||material==null)return;
            Vector3 eye=view.transform.position;var centre=new Vector2Int(Mathf.FloorToInt(eye.x/Cell),Mathf.FloorToInt(eye.z/Cell));int reach=Mathf.CeilToInt(Radius/Cell);
            // Drop cells well out of reach.
            List<Vector2Int> stale=null;foreach(var key in cells.Keys)if(Mathf.Abs(key.x-centre.x)>reach+2||Mathf.Abs(key.y-centre.y)>reach+2)(stale??=new()).Add(key);
            if(stale!=null)foreach(var key in stale)cells.Remove(key);
            // Fill new cells nearest first, a few per frame.
            for(int r=0;r<=reach;r++)for(int dx=-r;dx<=r;dx++)for(int dz=-r;dz<=r;dz++) {
                if(Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dz))!=r)continue;var key=new Vector2Int(centre.x+dx,centre.y+dz);
                if(!cells.ContainsKey(key)&&queued.Add(key))pending.Enqueue(key);
            }
            clock.Restart();
            while(clock.Elapsed.TotalMilliseconds<BudgetMs) {
                if(!isBuilding) {
                    if(pending.Count==0)break;var key=pending.Dequeue();queued.Remove(key);
                    if(Mathf.Abs(key.x-centre.x)>reach+1||Mathf.Abs(key.y-centre.y)>reach+1||cells.ContainsKey(key))continue;Begin(key);
                }
                Step(24);
            }
            var rp=new RenderParams(material){shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,worldBounds=new Bounds(eye,Vector3.one*(Radius*2+40)),camera=null};
            int filled=0;Clumps=0;
            if(Time.unscaledTime>nextLog){nextLog=Time.unscaledTime+5;Debug.Log($"LITTLE_ROAD_GRASS cells={cells.Count} pending={pending.Count} drawn={lastDrawn} refused patch={refusedPatch} open={refusedOpen} miss={refusedMiss} surface={refusedSurface} low={refusedLow} eye={eye}");}
            void Flush(Mesh mesh){if(filled>0){Graphics.RenderMeshInstanced(rp,mesh,0,batch,filled);Clumps+=filled;filled=0;}}
            foreach(var kind in new[]{false,true}) {
                var mesh=kind?flowers:blades;
                foreach(var pair in cells) {
                    var key=pair.Key;float cx=(key.x+.5f)*Cell-eye.x,cz=(key.y+.5f)*Cell-eye.z;if(cx*cx+cz*cz>(Radius+Cell)*(Radius+Cell))continue;
                    var list=kind?pair.Value.bloom:pair.Value.grass;
                    foreach(var m in list){batch[filled++]=m;if(filled==batch.Length)Flush(mesh);}
                }
                Flush(mesh);
            }
            lastDrawn=Clumps;
        }
        int lastDrawn;
        void OnDestroy(){if(material!=null)Destroy(material);if(blades!=null)Destroy(blades);if(flowers!=null)Destroy(flowers);}
    }
}
