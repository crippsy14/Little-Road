using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LittleRoad
{
    // The scale test (docs/58 §11, W0): the whole Glasswater continent, generated from the blueprint's data in the game's
    // own coordinates, as rough terrain, graybox roads and blocks for towns, to drive at every class's pace before any
    // region is built properly.
    //   --scale-test                 drive it (Bayview to start); --scale-class C|B|A|S|E picks the car (default C)
    //   in game: 1-5 class C..E (top road build), 0 stock, M max build; Tab / Shift+Tab or D-pad up/down jump to the
    //   next/previous place; C or R3 aerial camera; R or L1 recover to the nearest road; N or View the map; Esc or Start quit
    //   --scale-pacing <file.md>     drive-time report per road and per place for every class build (SpeedProfile)
    //   --scale-autodrive <a>,<b>,.. drive the car along the quickest roads through those places (an autopilot) and log
    //                                each arrival, then quit; --scale-capture <folder> photographs set views and quits
    public sealed class ScaleTest:MonoBehaviour
    {
        ContinentGenerator gen;RaceTrial race;RoadCar car;Camera view;Transform world;
        Material ground;Texture2D biomeMap;
        string cls="C",build="top";Dictionary<string,string> fit;
        bool aerial;Vector3 camVelocity;
        Text hud,help;readonly List<(Transform t,float baseScale)> labels=new();
        static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
        public static readonly Dictionary<string,Color> ClassColours=new(){["C"]=new Color(.25f,.77f,.56f),["B"]=new Color(.27f,.65f,.91f),["A"]=new Color(.95f,.71f,.2f),["S"]=new Color(.94f,.35f,.35f),["E"]=new Color(.61f,.42f,.85f)};
        static readonly Dictionary<string,string> Bodies=new(){["C"]=CarBodies.Starter,["B"]=CarBodies.Sporty[0],["A"]=CarBodies.Sporty[1],["S"]=CarBodies.Sporty[3],["E"]=CarBodies.Prototype};

        IEnumerator Start()
        {
            race=GetComponent<RaceTrial>();race.freeRoam=true;race.track=new Circuit();race.elapsed=1;
            WorldLook.PlayPacing();Time.fixedDeltaTime=1f/120;Application.runInBackground=true;
            LoadingScreen.Begin("Little Road · scale test");LoadingScreen.Progress(.05f,"Generating the continent");yield return null;
            var task=Task.Run(()=>new ContinentGenerator());
            while(!task.IsCompleted)yield return null;
            if(task.Exception!=null){Debug.LogException(task.Exception);Application.Quit(2);yield break;}
            gen=task.Result;
            Debug.Log($"LITTLE_ROAD_SCALE_GENERATED seconds={gen.Seconds:0.0} land={gen.LandKm2:0}km2 roads={gen.roads.Count} km={gen.roads.Sum(r=>r.length)/1000:0.0} lakes={string.Join(",",gen.lakeLevel.Select(l=>l.ToString("0.0",Inv)))}");
            foreach(var r in gen.roads)if(r.maxGrade>r.type.grade+.03f)Debug.Log($"LITTLE_ROAD_SCALE_STEEP road=\"{r.Label}\" grade={r.maxGrade*100:0.0}% limit={r.type.grade*100:0}%");
            foreach(var p in ContinentPlan.Places){float moved=gen.nodeHeight[p.id]-gen.nodeNatural[p.id];if(Mathf.Abs(moved)>25)Debug.Log($"LITTLE_ROAD_SCALE_NODE_MOVED place={p.id} natural={gen.nodeNatural[p.id]:0} used={gen.nodeHeight[p.id]:0} change={moved:0}");}

            if(RaceTrial.Has("--scale-pacing")){LoadingScreen.Progress(.2f,"Timing every road");yield return null;Pacing(RaceTrial.Arg("--scale-pacing"));}
            world=new GameObject("Continent").transform;
            LoadingScreen.Progress(.3f,"Light and water");yield return null;Lighting();Water();
            LoadingScreen.Progress(.4f,"Painting the land");yield return null;Ground();
            LoadingScreen.Progress(.6f,"Laying the roads");yield return null;RoadsBuild();
            LoadingScreen.Progress(.75f,"Building the towns");yield return null;Towns();Trees();
            LoadingScreen.Progress(.9f,"The car");yield return null;
            cls=RaceTrial.Arg("--scale-class","C").ToUpperInvariant();if(RaceCalendar.Rank(cls)<0)cls="C";
            FreeRoamRoads.Clear();foreach(var r in gen.roads)FreeRoamRoads.Register(r.points.Where((_,i)=>i%3==0).ToArray());
            RoadCar.FreeRoamFloor=ContinentGenerator.Sea-90;
            view=new GameObject("Scale test camera").AddComponent<Camera>();view.tag="MainCamera";view.fieldOfView=58;view.nearClipPlane=.25f;view.farClipPlane=32000;
            view.gameObject.AddComponent<AudioListener>();WorldLook.Camera(view);
            var start=ContinentPlan.Find("bayview");PlaceAt(start,Quaternion.identity,true);
            Hud();race.running=true;
            NearAround(car.body.position,true);
            Debug.Log($"LITTLE_ROAD_SCALE_READY class={cls} load={Time.realtimeSinceStartup:0.0}s");LoadingScreen.Ready();
            if(RaceTrial.Has("--scale-capture"))StartCoroutine(Capture(RaceTrial.Arg("--scale-capture")));
            else if(RaceTrial.Has("--scale-autodrive"))StartCoroutine(AutoDrive(RaceTrial.Arg("--scale-autodrive")));
        }
        void OnDestroy(){RoadCar.FreeRoamFloor=float.NaN;RoadCar.ForceAutomaticGears=false;}

        // ---------------------------------------------------------------- builds
        static readonly Dictionary<string,Dictionary<string,string>> builds=new();
        // The class's strongest road build: the best part in every slot at that class or below, by Hillcrest Raceway
        // lap time (SpeedProfile, as the Duke's maximum build is chosen; docs/54).
        public static Dictionary<string,string> ClassBuild(string cls)
        {
            if(builds.TryGetValue(cls,out var cached))return new(cached);
            int rank=RaceCalendar.Rank(cls);var all=EquipmentCatalog.All;var fit=EquipmentCatalog.StockLoadout();
            bool Ok(EquipmentCatalog.Part p)=>RaceCalendar.Rank(p.tier)<=rank&&p.Discipline=="road";
            foreach(var slot in all.slots){var top=all.parts.Where(p=>p.slot==slot&&Ok(p)).OrderByDescending(p=>p.price).FirstOrDefault();if(top!=null)fit[slot]=top.id;}
            var c=new Circuit(HillcrestRoute.Circuit);var lap=new List<Vector3>();for(int n=0;n<2;n++)for(float d=0;d<c.length;d+=2)lap.Add(c.At(d));
            float best=EquipmentCatalog.LapTime(lap,fit);
            for(int round=0;round<4;round++) {
                bool changed=false;
                foreach(var slot in all.slots)foreach(var p in all.parts.Where(p=>p.slot==slot&&Ok(p))) {
                    if(fit[slot]==p.id)continue;var trial=new Dictionary<string,string>(fit){[slot]=p.id};float t=EquipmentCatalog.LapTime(lap,trial);
                    if(t<best-.005f){best=t;fit=trial;changed=true;}
                }
                if(!changed)break;
            }
            builds[cls]=fit;return new(fit);
        }
        Dictionary<string,string> BuildFor(string c,string b)=>b=="stock"?EquipmentCatalog.StockLoadout():b=="max"?EquipmentCatalog.MaxBuild():ClassBuild(c);
        string BodyFor(string c,string b)=>b=="max"?CarBodies.Prototype:b=="stock"?CarBodies.Starter:Bodies[c];
        string BuildLabel=>build=="stock"?"stock car":build=="max"?"maximum build":$"class {cls} top build";

        void PlaceAt(ContinentPlan.Place place,Quaternion fallback,bool fresh)
        {
            var at=ContinentPlan.ToWorld(place.Km);
            if(gen.NearestRoad(at,place.Radius+600,out var road,out int i,out _)) {
                // A little way out along the road, facing into town (not against the beacon).
                int step=i<road.points.Length/2?1:-1;float want=Mathf.Min(place.Radius*.6f,260);
                while(i+step>=0&&i+step<road.points.Length&&new Vector2(road.points[i].x-at.x,road.points[i].z-at.z).magnitude<want)i+=step;
                var p=road.points[i];var f=road.points[Mathf.Clamp(i-step,0,road.points.Length-1)]-road.points[Mathf.Clamp(i+step,0,road.points.Length-1)];f.y=0;
                Spawn(p,Quaternion.LookRotation(f.sqrMagnitude>.01f?f:Vector3.forward),Vector3.zero);
            } else Spawn(at+Vector3.up*gen.HeightAt(at),fallback,Vector3.zero);
        }
        void Spawn(Vector3 position,Quaternion rotation,Vector3 velocity)
        {
            if(car!=null){race.cars.Remove(car);Destroy(car.gameObject);}
            fit=BuildFor(cls,build);var paint=ClassColours[build=="stock"?"C":build=="max"?"E":cls];
            NearAround(position,true);
            var go=TrialWorld.Car(BodyFor(cls,build),paint);go.transform.SetPositionAndRotation(position+Vector3.up*.25f,rotation);
            car=go.AddComponent<RoadCar>();car.Setup(race,0,EquipmentCatalog.Stats(fit),false,1709,BuildLabel);race.cars.Add(car);
            car.body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;car.body.linearVelocity=velocity;
            Debug.Log($"LITTLE_ROAD_SCALE_CAR class={cls} build={build} body={BodyFor(cls,build)} top={car.tuning.AttainableSpeed*3.6f:0}km/h");
        }

        // ---------------------------------------------------------------- light, sky, water
        Material sea;Color fogColour;float fogDensity;
        void Lighting()
        {
            var sunDir=CoastalSky.Apply();
            var sun=new GameObject("Scale test sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.0f;sun.color=new Color(1,.95f,.86f);
            sun.transform.rotation=Quaternion.LookRotation(-sunDir);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            QualitySettings.shadowDistance=600;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.62f,.74f,.88f);RenderSettings.ambientEquatorColor=new Color(.5f,.55f,.55f);RenderSettings.ambientGroundColor=new Color(.3f,.32f,.28f);
            fogColour=new Color(.72f,.8f,.88f);fogDensity=.000045f;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=fogColour;RenderSettings.fogDensity=fogDensity;
        }
        static Material Clear(string name,Color c,float smooth)
        {
            var m=TrialWorld.Material(name,c,0,smooth);m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_Cull",0);m.SetFloat("_ZWrite",0);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetOverrideTag("RenderType","Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;m.SetShaderPassEnabled("ShadowCaster",false);m.SetShaderPassEnabled("DepthOnly",false);
            return m;
        }
        void Water()
        {
            sea=Clear("Continent sea",new Color(.16f,.42f,.58f,.82f),.9f);
            Quad("Sea",new Vector3(0,ContinentGenerator.Sea,-9000),90000,90000,sea,false);
            for(int l=0;l<ContinentPlan.Lakes.Length;l++) {
                var lk=ContinentPlan.Lakes[l];var c=ContinentPlan.ToWorld(lk[0],lk[1],gen.lakeLevel[l]);
                bool frozen=gen.BiomeAt(c)==ContinentGenerator.Biome.Frozen;
                var m=frozen?TrialWorld.Material("Lake ice",new Color(.84f,.91f,.95f),0,.75f):Clear(ContinentPlan.LakeNames[l]+" water",new Color(.18f,.48f,.6f,.8f),.92f);
                var q=Quad(ContinentPlan.LakeNames[l],c,lk[2]*2600,lk[3]*2600,m,frozen);
                Debug.Log($"LITTLE_ROAD_SCALE_LAKE name=\"{ContinentPlan.LakeNames[l]}\" level={gen.lakeLevel[l]:0.0} frozen={frozen}");
            }
        }
        GameObject Quad(string name,Vector3 centre,float w,float d,Material m,bool solid)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;go.transform.SetParent(world);go.transform.SetPositionAndRotation(centre,Quaternion.Euler(90,0,0));go.transform.localScale=new Vector3(w,d,1);
            Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            if(solid){var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,0,.25f);box.size=new Vector3(1,1,.5f);}
            return go;
        }

        // ---------------------------------------------------------------- ground: biome-painted chunks, two detail levels
        const int ChunkCells=100;int chunkCount;Chunk[,] chunks;
        sealed class Chunk {public int cx,cz;public GameObject far,near;public MeshCollider collider;public Bounds bounds;public Matrix4x4[][] trees;}
        void Ground()
        {
            int n=gen.N-1;biomeMap=new Texture2D(n,n,TextureFormat.RGB24,true,false){name="Continent biomes",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear,anisoLevel=4};
            var graded=ContinentGenerator.BiomeColours.Select(c=>(Color)c).Select(c=>WorldLook.Surface(c)).ToArray();var px=new Color32[n*n];
            Parallel.For(0,n,j=>{for(int i=0;i<n;i++){var c=graded[gen.biome[j*gen.N+i]];float v=.94f+.12f*ContinentGenerator.Fbm(i*.21f,j*.21f,404,3);px[j*n+i]=(Color)(c*v);}});
            biomeMap.SetPixels32(px);biomeMap.Apply(true,true);
            ground=TrialWorld.Material("Continent ground",Color.white,0,.06f,false);ground.SetTexture("_BaseMap",biomeMap);ground.mainTexture=biomeMap;
            chunkCount=(gen.N-1)/ChunkCells;chunks=new Chunk[chunkCount,chunkCount];
            for(int cz=0;cz<chunkCount;cz++)for(int cx=0;cx<chunkCount;cx++) {
                var ch=new Chunk{cx=cx,cz=cz};chunks[cx,cz]=ch;
                ch.far=new GameObject($"Ground {cx},{cz} far",typeof(MeshFilter),typeof(MeshRenderer));ch.far.transform.SetParent(world);
                var mesh=ChunkMesh(cx,cz,5,1.6f);ch.far.GetComponent<MeshFilter>().sharedMesh=mesh;ch.far.GetComponent<MeshRenderer>().sharedMaterial=ground;ch.bounds=mesh.bounds;
            }
        }
        Mesh ChunkMesh(int cx,int cz,int step,float sink)
        {
            int side=ChunkCells/step+1,i0=cx*ChunkCells,j0=cz*ChunkCells;var v=new List<Vector3>();var nrm=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
            float span=gen.N-1;
            Vector3 Normal(int i,int j)
            {
                int a=Mathf.Max(i-1,0),b=Mathf.Min(i+1,gen.N-1),c=Mathf.Max(j-1,0),d=Mathf.Min(j+1,gen.N-1);
                float dx=(gen.height[j*gen.N+b]-gen.height[j*gen.N+a])/((b-a)*gen.cellMetres),dz=(gen.height[d*gen.N+i]-gen.height[c*gen.N+i])/((d-c)*gen.cellMetres);
                return new Vector3(-dx,1,-dz).normalized;
            }
            for(int y=0;y<side;y++)for(int x=0;x<side;x++){int i=i0+x*step,j=j0+y*step;var w=gen.WorldOf(i,j);w.y-=sink;v.Add(w);nrm.Add(Normal(i,j));uv.Add(new Vector2((i+.5f)/span,(j+.5f)/span));}
            for(int y=0;y<side-1;y++)for(int x=0;x<side-1;x++){int a=y*side+x;t.AddRange(new[]{a,a+side,a+1,a+1,a+side,a+side+1});}
            // Skirts hang 30 m down from every edge so detail levels never show a crack between them.
            void Skirt(Func<int,int> at)
            {
                int start=v.Count;for(int k=0;k<side;k++){int s=at(k);v.Add(v[s]+Vector3.down*30);nrm.Add(nrm[s]);uv.Add(uv[s]);}
                for(int k=0;k<side-1;k++){int a=at(k),b=at(k+1),c=start+k,d=start+k+1;t.AddRange(new[]{a,c,b,b,c,d,a,b,c,b,d,c});}
            }
            Skirt(k=>k);Skirt(k=>(side-1)*side+k);Skirt(k=>k*side);Skirt(k=>k*side+side-1);
            var m=new Mesh{name=$"Ground {cx},{cz} x{step}",indexFormat=v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(v);m.SetNormals(nrm);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateBounds();return m;
        }
        const float NearDistance=2600,ColliderDistance=1400;
        // Full detail and colliders near the car (and, with `all`, everything in reach now rather than a chunk a frame).
        void NearAround(Vector3 at,bool all)
        {
            if(chunks==null)return;int built=0;
            foreach(var ch in chunks) {
                float d=Mathf.Sqrt(ch.bounds.SqrDistance(new Vector3(at.x,ch.bounds.center.y,at.z)));
                bool wantNear=d<NearDistance,wantCollider=d<ColliderDistance;
                if(wantNear&&ch.near==null&&(all||built<2)) {
                    built++;ch.near=new GameObject($"Ground {ch.cx},{ch.cz} near",typeof(MeshFilter),typeof(MeshRenderer));ch.near.transform.SetParent(world);
                    ch.near.GetComponent<MeshFilter>().sharedMesh=ChunkMesh(ch.cx,ch.cz,1,0);ch.near.GetComponent<MeshRenderer>().sharedMaterial=ground;
                    if(ch.trees==null)ch.trees=TreesFor(ch);
                }
                if(ch.near!=null&&!wantNear&&d>NearDistance+400){Destroy(ch.near.GetComponent<MeshFilter>().sharedMesh);Destroy(ch.near);ch.near=null;ch.collider=null;}
                if(ch.near!=null){ch.near.SetActive(true);ch.far.SetActive(false);}else ch.far.SetActive(true);
                if(ch.near!=null&&wantCollider&&ch.collider==null){ch.collider=ch.near.AddComponent<MeshCollider>();ch.collider.sharedMesh=ch.near.GetComponent<MeshFilter>().sharedMesh;}
                if(ch.collider!=null&&!wantCollider&&d>ColliderDistance+300){Destroy(ch.collider);ch.collider=null;}
            }
        }

        // ---------------------------------------------------------------- roads
        void RoadsBuild()
        {
            var root=new GameObject("Roads").transform;root.SetParent(world);
            var shoulder=TrialWorld.Material("Road shoulder",new Color(.45f,.42f,.34f),0,.1f);
            var white=TrialWorld.Material("Road paint",new Color(.93f,.92f,.86f),0,.3f);var yellow=TrialWorld.Material("Centre paint",new Color(.93f,.76f,.25f),0,.3f);
            var surfaces=new Dictionary<string,Material>{
                ["motorway"]=TrialWorld.Material("Motorway asphalt",new Color(.18f,.19f,.21f),0,.22f),["road"]=TrialWorld.Material("Country asphalt",new Color(.22f,.22f,.23f),0,.2f),
                ["mountain"]=TrialWorld.Material("Mountain asphalt",new Color(.24f,.23f,.23f),0,.2f),["causeway"]=TrialWorld.Material("Causeway asphalt",new Color(.21f,.21f,.22f),0,.22f),
                ["gravel"]=TrialWorld.Material("Gravel",new Color(.58f,.48f,.34f),0,.08f),["ice"]=TrialWorld.Material("Road ice",new Color(.86f,.92f,.96f),0,.7f),
                ["undersea"]=TrialWorld.Material("Sea-floor track",new Color(.64f,.62f,.5f),0,.1f)};
            foreach(var road in gen.roads) {
                var holder=new GameObject(road.Label).transform;holder.SetParent(root);float hw=road.type.width*.5f;var pts=road.points;
                for(int s=0;s<pts.Length-1;s+=125) {
                    int e=Mathf.Min(s+126,pts.Length);
                    Strip(holder,"Carriageway",pts,s,e,-hw,hw,0,0,surfaces[road.plan.type],true,road.type.tarmac);
                    Strip(holder,"Left shoulder",pts,s,e,-hw-6,-hw,-.45f,0,shoulder,true,false);Strip(holder,"Right shoulder",pts,s,e,hw,hw+6,0,-.45f,shoulder,true,false);
                    if(road.plan.type=="motorway") {
                        foreach(float side in new[]{-1f,1f}){Strip(holder,"Edge line",pts,s,e,side*(hw-1.1f)-.12f,side*(hw-1.1f)+.12f,.03f,.03f,white,false,false);Dashes(holder,pts,s,e,side*hw*.5f,white,6,10);}
                        Strip(holder,"Median",pts,s,e,-.45f,-.25f,.03f,.03f,yellow,false,false);Strip(holder,"Median",pts,s,e,.25f,.45f,.03f,.03f,yellow,false,false);
                    } else if(road.type.tarmac)Dashes(holder,pts,s,e,0,white,4,7);
                }
            }
            StaticBatchingUtility.Combine(root.gameObject);
        }
        static Vector3 Side(Vector3[] p,int i){var f=p[Mathf.Min(i+1,p.Length-1)]-p[Mathf.Max(i-1,0)];f.y=0;return Vector3.Cross(Vector3.up,f.normalized);}
        static void Strip(Transform parent,string name,Vector3[] p,int s,int e,float from,float to,float yFrom,float yTo,Material m,bool solid,bool tarmac)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float run=0;
            for(int i=s;i<e;i++){var r=Side(p,i);if(i>s)run+=Vector3.Distance(p[i],p[i-1]);v.Add(p[i]+r*from+Vector3.up*yFrom);v.Add(p[i]+r*to+Vector3.up*yTo);uv.Add(new Vector2(0,run*.1f));uv.Add(new Vector2(1,run*.1f));
                if(i<e-1){int a=(i-s)*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}}
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=m;
            if(!solid)mr.shadowCastingMode=ShadowCastingMode.Off;
            if(solid){go.AddComponent<MeshCollider>().sharedMesh=mesh;if(tarmac)go.AddComponent<RoadSurface>();}
        }
        static void Dashes(Transform parent,Vector3[] p,int s,int e,float offset,Material m,float dash,float gap)
        {
            var v=new List<Vector3>();var t=new List<int>();float run=0;bool on=true;float left=dash;
            for(int i=s+1;i<e;i++) {
                float seg=Vector3.Distance(p[i],p[i-1]);run+=seg;left-=seg;
                if(left<=0){on=!on;left=on?dash:gap;}
                if(!on)continue;var a=p[i-1];var b=p[i];var r0=Side(p,i-1);var r1=Side(p,i);int k=v.Count;
                v.Add(a+r0*(offset-.12f)+Vector3.up*.03f);v.Add(a+r0*(offset+.12f)+Vector3.up*.03f);v.Add(b+r1*(offset-.12f)+Vector3.up*.03f);v.Add(b+r1*(offset+.12f)+Vector3.up*.03f);
                t.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
            }
            if(v.Count==0)return;
            var mesh=new Mesh{name="Dashes"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();
            var go=new GameObject("Lane dashes",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- towns: blocks, beacons and names
        void Towns()
        {
            var palette=new[]{new Color(.93f,.88f,.78f),new Color(.86f,.6f,.48f),new Color(.62f,.74f,.84f),new Color(.95f,.82f,.55f),new Color(.7f,.82f,.66f),new Color(.82f,.78f,.86f),new Color(.6f,.6f,.62f)}
                .Select((c,i)=>TrialWorld.Material("Town block "+i,c,0,.25f)).ToArray();
            var cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            foreach(var place in ContinentPlan.Places) {
                var holder=new GameObject(place.name).transform;holder.SetParent(world);var centre=ContinentPlan.ToWorld(place.Km);float pad=gen.nodeHeight[place.id];
                var rng=new System.Random(place.id.Aggregate(17,(h,ch)=>h*31+ch));float r=place.Radius;
                float density=place.kind=="hub"?.42f:place.kind=="island"?.5f:place.kind=="main"?.45f:place.kind=="secret"?.4f:.32f;
                float step=place.kind=="hub"?30:26;int made=0;
                for(float z=-r;z<=r;z+=step)for(float x=-r;x<=r;x+=step) {
                    float d=Mathf.Sqrt(x*x+z*z);if(d>r)continue;
                    var at=centre+new Vector3(x+(float)(rng.NextDouble()-.5)*step*.5f,0,z+(float)(rng.NextDouble()-.5)*step*.5f);
                    if(rng.NextDouble()>density*Mathf.Pow(1-d/r,.6f))continue;
                    float y=gen.HeightAt(at);if(place.kind!="secret"&&gen.WaterAt(at,out float level,out _)&&y<level+.5f)continue;
                    float t=1-d/r;float h=place.kind=="hub"?Mathf.Lerp(9,70,t*t)*(float)(.55+rng.NextDouble()*.9):place.kind=="island"?Mathf.Lerp(7,30,t)*(float)(.6+rng.NextDouble()*.8):
                        place.kind=="main"?(float)(6+rng.NextDouble()*10):(float)(5+rng.NextDouble()*4);
                    float w=(float)(9+rng.NextDouble()*12)*(place.kind=="hub"?1.3f:1),dep=(float)(9+rng.NextDouble()*12)*(place.kind=="hub"?1.3f:1);
                    // Clear of every road by the block's own half-diagonal plus a pavement.
                    if(gen.Sample(gen.roadGap,at)<new Vector2(w,dep).magnitude*.5f+5)continue;
                    float yaw=gen.NearestRoad(at,90,out var road,out int ri,out _)?Quaternion.LookRotation(road.points[Mathf.Min(ri+1,road.points.Length-1)]-road.points[Mathf.Max(ri-1,0)]).eulerAngles.y:(float)rng.NextDouble()*90;
                    var go=new GameObject("Block",typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider));go.transform.SetParent(holder);
                    go.transform.SetPositionAndRotation(new Vector3(at.x,y-1+h*.5f,at.z),Quaternion.Euler(0,yaw,0));go.transform.localScale=new Vector3(w,h+2,dep);
                    go.GetComponent<MeshFilter>().sharedMesh=cube;go.GetComponent<MeshRenderer>().sharedMaterial=palette[rng.Next(palette.Length)];go.tag="Respawn";made++;
                }
                // The beacon: a class-coloured mast with the name, seen from far off.
                float mast=place.kind=="hub"?230:place.Main?150:90;
                var pole=GameObject.CreatePrimitive(PrimitiveType.Cylinder);pole.name="Beacon";pole.transform.SetParent(holder);Destroy(pole.GetComponent<Collider>());
                // On clear ground beside the roads (the node itself is where the roads meet).
                var spot=centre;for(int ring=1;ring<=12&&gen.Sample(gen.roadGap,spot)<14;ring++)for(int a=0;a<8;a++){var t2=centre+Quaternion.Euler(0,a*45,0)*Vector3.forward*(ring*12);if(gen.Sample(gen.roadGap,t2)>=14){spot=t2;break;}}
                pole.transform.position=new Vector3(spot.x,gen.HeightAt(spot)+mast*.5f,spot.z);pole.transform.localScale=new Vector3(place.Main?4:2.5f,mast*.5f,place.Main?4:2.5f);
                pole.GetComponent<Renderer>().sharedMaterial=TrialWorld.Material(place.cls+" beacon",ClassColours[place.cls],0,.4f);
                Label(holder,place.name+(place.kind=="secret"?" (sunken)":""),pole.transform.position+Vector3.up*(mast*.5f+12),place.Main?1:.7f);
                StaticBatchingUtility.Combine(holder.gameObject);
                if(made>0)Debug.Log($"LITTLE_ROAD_SCALE_TOWN place={place.id} blocks={made} pad={pad:0.0}");
            }
        }
        void Label(Transform parent,string words,Vector3 at,float scale)
        {
            var go=new GameObject("Label "+words);go.transform.SetParent(parent);go.transform.position=at;
            var text=go.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.LowerCenter;text.alignment=TextAlignment.Center;text.fontSize=96;text.characterSize=.5f;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontStyle=FontStyle.Bold;text.color=Color.white;go.GetComponent<Renderer>().sharedMaterial=text.font.material;go.AddComponent<WorldText>();
            labels.Add((go.transform,scale));
        }

        // ---------------------------------------------------------------- trees (instanced, near chunks only)
        Mesh broadleaf,conifer;Material leafMaterial,pineMaterial;readonly List<Matrix4x4> scratch=new();
        void Trees()
        {
            broadleaf=Cone(7,3.6f,10,1.6f,true);conifer=Cone(7,2.8f,15,.6f,false);
            leafMaterial=TrialWorld.Material("Broadleaf",new Color(.27f,.47f,.24f),0,.1f);pineMaterial=TrialWorld.Material("Pine",new Color(.17f,.33f,.25f),0,.1f);
        }
        static Mesh Cone(int sides,float radius,float height,float lift,bool round)
        {
            var v=new List<Vector3>();var t=new List<int>();
            // A low-poly crown: a ring at its widest, a point on top, and (round) a second point below.
            float mid=round?lift+height*.45f:lift;v.Add(new Vector3(0,lift+height,0));v.Add(new Vector3(0,round?lift:lift-.01f,0));
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*radius,mid,Mathf.Sin(a)*radius));}
            for(int i=0;i<sides;i++){int a=2+i,b=2+(i+1)%sides;t.AddRange(new[]{0,b,a,1,a,b});}
            // A trunk.
            int k=v.Count;float tw=.35f;foreach(var (x,z) in new[]{(-tw,-tw),(tw,-tw),(tw,tw),(-tw,tw)}){v.Add(new Vector3(x,0,z));v.Add(new Vector3(x,mid+.5f,z));}
            for(int i=0;i<4;i++){int a=k+i*2,b=k+((i+1)%4)*2;t.AddRange(new[]{a,a+1,b,b,a+1,b+1});}
            var m=new Mesh{name=round?"Broadleaf":"Conifer"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        Matrix4x4[][] TreesFor(Chunk ch)
        {
            var leaf=new List<Matrix4x4>();var pine=new List<Matrix4x4>();var rng=new System.Random(ch.cx*7919+ch.cz);
            for(int j=0;j<ChunkCells;j+=2)for(int i=0;i<ChunkCells;i+=2) {
                int gi=ch.cx*ChunkCells+i,gj=ch.cz*ChunkCells+j,k=gj*gen.N+gi;var b=(ContinentGenerator.Biome)gen.biome[k];
                float p=b switch{ContinentGenerator.Biome.Forest=>.6f,ContinentGenerator.Biome.Pine=>.7f,ContinentGenerator.Biome.Farmland=>.025f,ContinentGenerator.Biome.Steppe=>.012f,ContinentGenerator.Biome.Scrub=>.05f,ContinentGenerator.Biome.Tundra=>.02f,ContinentGenerator.Biome.Rock=>.01f,_=>0};
                if(p<=0||rng.NextDouble()>p||gen.roadGap[k]<9)continue;
                var at=gen.WorldOf(gi,gj)+new Vector3((float)(rng.NextDouble()-.5)*18,0,(float)(rng.NextDouble()-.5)*18);
                bool inTown=false;foreach(var pl in ContinentPlan.Places)if((new Vector2(at.x,at.z)-new Vector2(ContinentPlan.ToWorld(pl.Km).x,ContinentPlan.ToWorld(pl.Km).z)).magnitude<pl.Radius){inTown=true;break;}
                if(inTown)continue;
                at.y=gen.HeightAt(at)-.2f;float s=(float)(.7+rng.NextDouble()*.7);
                bool conif=b==ContinentGenerator.Biome.Pine||b==ContinentGenerator.Biome.Tundra||b==ContinentGenerator.Biome.Rock;
                (conif?pine:leaf).Add(Matrix4x4.TRS(at,Quaternion.Euler(0,(float)rng.NextDouble()*360,0),new Vector3(s,s*(float)(.8+rng.NextDouble()*.5),s)));
            }
            return new[]{leaf.ToArray(),pine.ToArray()};
        }
        void DrawTrees()
        {
            if(chunks==null||view==null)return;
            var leafParams=new RenderParams(leafMaterial){shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};var pineParams=new RenderParams(pineMaterial){shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};
            foreach(var ch in chunks) {
                if(ch.trees==null||ch.near==null)continue;
                Draw(leafParams,broadleaf,ch.trees[0]);Draw(pineParams,conifer,ch.trees[1]);
            }
        }
        static void Draw(RenderParams rp,Mesh mesh,Matrix4x4[] all){for(int s=0;s<all.Length;s+=1000)Graphics.RenderMeshInstanced(rp,mesh,0,all,Mathf.Min(1000,all.Length-s),s);}

        // ---------------------------------------------------------------- HUD and play
        void Hud()
        {
            var canvas=CoastUI.Canvas(transform,"Scale test HUD",40);
            var card=Toy.Card(canvas.transform,"Scale test panel",new Vector2(1180,214),new Vector2(-330,410),Toy.Navy,26,5,6);
            hud=Toy.Label(card.transform,"Scale test readout",string.Empty,24,new Vector2(1130,196),new Vector2(0,0),Toy.White,TextAnchor.UpperLeft,true);
            var bar=Toy.Card(canvas.transform,"Scale test help",new Vector2(1500,56),new Vector2(0,-500),Toy.Navy,22,4,4);
            help=Toy.Label(bar.transform,"Scale test keys","1–5 / D-pad ←→ class C–E  ·  0 stock  ·  M max  ·  Tab / D-pad ↑↓ next town  ·  N / View map  ·  C / R3 aerial  ·  R / L1 recover  ·  Esc quit",22,new Vector2(1460,44),Vector2.zero,Toy.Sun,TextAnchor.MiddleCenter,true);
            MapOverlay(canvas.transform);
        }
        // The map (N / View): the biome painting, every road drawn in by type, the places, and the car.
        GameObject map;RectTransform mapCar;const float MapSize=940;
        void MapOverlay(Transform parent)
        {
            map=new GameObject("Scale test map",typeof(RectTransform));map.transform.SetParent(parent,false);var root=(RectTransform)map.transform;root.sizeDelta=new Vector2(MapSize,MapSize);root.anchoredPosition=new Vector2(0,20);
            var back=Toy.Card(map.transform,"Map frame",new Vector2(MapSize+30,MapSize+30),Vector2.zero,Toy.Navy,20,5,6);back.transform.SetAsFirstSibling();
            var land=new GameObject("Map land",typeof(RectTransform),typeof(RawImage));land.transform.SetParent(map.transform,false);((RectTransform)land.transform).sizeDelta=new Vector2(MapSize,MapSize);land.GetComponent<RawImage>().texture=biomeMap;
            const int R=1024;var roads=new Texture2D(R,R,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var px=new Color32[R*R];
            var colour=new Dictionary<string,Color32>{["motorway"]=new(255,178,63,255),["road"]=new(255,246,227,255),["mountain"]=new(255,246,227,255),["causeway"]=new(255,246,227,255),["gravel"]=new(201,139,74,255),["ice"]=new(232,246,255,255),["undersea"]=new(111,227,210,255)};
            foreach(var road in gen.roads) {
                var c=colour[road.plan.type];int thick=road.plan.type=="motorway"?2:1;
                foreach(var p in road.points){var km=ContinentPlan.ToKm(p);int x=Mathf.RoundToInt(km.x/ContinentPlan.SizeKm*(R-1)),y=Mathf.RoundToInt(km.y/ContinentPlan.SizeKm*(R-1));
                    for(int dy=-thick;dy<=thick;dy++)for(int dx=-thick;dx<=thick;dx++){int xx=x+dx,yy=y+dy;if(xx>=0&&yy>=0&&xx<R&&yy<R)px[yy*R+xx]=c;}}
            }
            roads.SetPixels32(px);roads.Apply(false,true);
            var lines=new GameObject("Map roads",typeof(RectTransform),typeof(RawImage));lines.transform.SetParent(map.transform,false);((RectTransform)lines.transform).sizeDelta=new Vector2(MapSize,MapSize);lines.GetComponent<RawImage>().texture=roads;
            foreach(var place in ContinentPlan.Places) {
                var at=MapPoint(ContinentPlan.ToWorld(place.Km));float dot=place.Main?16:10;
                var mark=Toy.Card(map.transform,"Map "+place.id,new Vector2(dot,dot),at,ClassColours[place.cls],dot*.5f,2,0);
                Toy.Label(map.transform,"Map name "+place.id,place.name,place.Main?17:13,new Vector2(220,24),at+new Vector2(118,0),Toy.White,TextAnchor.MiddleLeft,true);
            }
            var marker=Toy.Card(map.transform,"Map car",new Vector2(22,22),Vector2.zero,Toy.Red,4,3,0);mapCar=(RectTransform)marker.transform;
            map.SetActive(false);
        }
        static Vector2 MapPoint(Vector3 world){var km=ContinentPlan.ToKm(world);return new Vector2((km.x/ContinentPlan.SizeKm-.5f)*MapSize,(km.y/ContinentPlan.SizeKm-.5f)*MapSize);}
        int jumpIndex;string here,tripFrom;float tripStart,tripDistance;Vector3 lastPos;readonly List<string> trips=new();
        void Update()
        {
            if(gen==null||car==null)return;
            var k=Keyboard.current;var pad=Gamepad.current;
            if(k?.escapeKey.wasPressedThisFrame==true||pad?.startButton.wasPressedThisFrame==true){Application.Quit();return;}
            if(map!=null&&(k?.nKey.wasPressedThisFrame==true||pad?.selectButton.wasPressedThisFrame==true))map.SetActive(!map.activeSelf);
            if(map!=null&&map.activeSelf){mapCar.anchoredPosition=MapPoint(car.body.position);mapCar.localRotation=Quaternion.Euler(0,0,-car.transform.eulerAngles.y+45);}
            if(!autoDriving) {
                for(int c=0;c<5;c++)if(k!=null&&k[(Key)((int)Key.Digit1+c)].wasPressedThisFrame)Swap(RaceCalendar.Classes[c],"top");
                if(k?.digit0Key.wasPressedThisFrame==true)Swap("C","stock");if(k?.mKey.wasPressedThisFrame==true)Swap("E","max");
                if(pad?.dpad.right.wasPressedThisFrame==true)Swap(RaceCalendar.Classes[Mathf.Min(4,RaceCalendar.Rank(cls)+1)],"top");
                if(pad?.dpad.left.wasPressedThisFrame==true)Swap(RaceCalendar.Classes[Mathf.Max(0,RaceCalendar.Rank(cls)-1)],"top");
                bool next=k?.tabKey.wasPressedThisFrame==true&&!k.shiftKey.isPressed||pad?.dpad.down.wasPressedThisFrame==true,prev=k?.tabKey.wasPressedThisFrame==true&&k.shiftKey.isPressed||pad?.dpad.up.wasPressedThisFrame==true;
                if(next||prev){jumpIndex=(jumpIndex+(next?1:-1)+ContinentPlan.Places.Length)%ContinentPlan.Places.Length;var p=ContinentPlan.Places[jumpIndex];PlaceAt(p,Quaternion.identity,false);tripFrom=null;Debug.Log("LITTLE_ROAD_SCALE_JUMP place="+p.id);}
                if(k?.rKey.wasPressedThisFrame==true||pad?.leftShoulder.wasPressedThisFrame==true)Recover();
            }
            if(k?.cKey.wasPressedThisFrame==true||pad?.rightStickButton.wasPressedThisFrame==true)aerial=!aerial;
            NearAround(car.body.position,false);DrawTrees();Trip();HudText();
        }
        void Swap(string c,string b)
        {
            cls=c;build=b;var pose=car.body.position;var rot=car.body.rotation;var vel=car.body.linearVelocity;Spawn(pose,Quaternion.Euler(0,rot.eulerAngles.y,0),vel);
        }
        void Recover()
        {
            if(!gen.NearestRoad(car.body.position,5000,out var road,out int i,out _))return;
            var f=road.points[Mathf.Min(i+1,road.points.Length-1)]-road.points[Mathf.Max(i-1,0)];f.y=0;
            car.body.position=road.points[i]+Vector3.up*.3f;car.body.rotation=Quaternion.LookRotation(f);car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;
        }
        void Trip()
        {
            var p=car.body.position;if(lastPos!=default)tripDistance+=new Vector2(p.x-lastPos.x,p.z-lastPos.z).magnitude;lastPos=p;
            string at=null;foreach(var pl in ContinentPlan.Places){var c=ContinentPlan.ToWorld(pl.Km);if(new Vector2(p.x-c.x,p.z-c.z).magnitude<pl.Radius+120){at=pl.id;break;}}
            if(at!=null&&at!=here) {
                if(tripFrom!=null&&tripFrom!=at) {
                    float time=Time.time-tripStart;string line=$"{ContinentPlan.Find(tripFrom).name} → {ContinentPlan.Find(at).name}  {Clock(time)}  ({tripDistance/1000:0.0} km, {BuildLabel})";
                    trips.Insert(0,line);if(trips.Count>3)trips.RemoveAt(3);
                    Debug.Log($"LITTLE_ROAD_SCALE_TRIP from={tripFrom} to={at} seconds={time:0.0} km={tripDistance/1000:0.00} class={cls} build={build}");
                }
                here=at;
            }
            if(at==null&&here!=null){tripFrom=here;tripStart=Time.time;tripDistance=0;here=null;}
        }
        static string Clock(float t){int s=Mathf.RoundToInt(t);return $"{s/60}:{s%60:00}";}
        float frameAverage=.016f,frameLog,frameSum;int frameCount;
        void HudText()
        {
            frameAverage=Mathf.Lerp(frameAverage,Time.unscaledDeltaTime,.05f);frameSum+=Time.unscaledDeltaTime;frameCount++;
            if(Time.unscaledTime>frameLog){if(frameCount>0&&frameLog>0)Debug.Log($"LITTLE_ROAD_SCALE_FRAME ms={frameSum/frameCount*1000:0.0} near={chunks.Cast<Chunk>().Count(c=>c.near!=null)} colliders={chunks.Cast<Chunk>().Count(c=>c.collider!=null)} car={car.body.position} speed={car.speed:0.0} throttle={car.throttle:0.00} steer={car.steering:0.00} gear={car.gears?.Gear} kinematic={car.body.isKinematic} blocked={car.controlsBlocked} path={autoIndex}/{autoPath?.Count}");frameLog=Time.unscaledTime+10;frameSum=0;frameCount=0;}
            var p=car.body.position;string nearest="";float best=float.MaxValue;
            foreach(var pl in ContinentPlan.Places){var c=ContinentPlan.ToWorld(pl.Km);float d=new Vector2(p.x-c.x,p.z-c.z).magnitude;if(d<best){best=d;nearest=pl.name;}}
            string roadName="off road";float grade=0;
            if(gen.NearestRoad(p,30,out var road,out int i,out _)){roadName=$"{road.Label} · {road.plan.type}";int a=Mathf.Max(i-3,0),b=Mathf.Min(i+3,road.points.Length-1);var d=road.points[b]-road.points[a];float run=new Vector2(d.x,d.z).magnitude;grade=run>1?d.y/run*Mathf.Sign(Vector3.Dot(d,car.transform.forward)):0;}
            var sb=new StringBuilder();
            sb.Append($"SCALE TEST  ·  {BuildLabel}  ·  top {Units.Show(car.tuning.AttainableSpeed)}  ·  {1/Mathf.Max(.001f,frameAverage):0} fps\n");
            sb.Append($"{Units.Show(Mathf.Abs(car.speed))}  ·  {ContinentGenerator.BiomeNames[(int)gen.BiomeAt(p)]}  ·  {p.y-ContinentGenerator.Sea:0} m above sea  ·  grade {grade*100:+0.0;-0.0}%\n");
            sb.Append($"Nearest: {nearest} {best/1000:0.0} km  ·  {roadName}\n");
            sb.Append(here!=null?$"At {ContinentPlan.Find(here).name}":tripFrom!=null?$"Driving from {ContinentPlan.Find(tripFrom).name}: {Clock(Time.time-tripStart)}, {tripDistance/1000:0.0} km":"");
            foreach(var t in trips)sb.Append("\n  "+t);
            hud.text=sb.ToString();
        }
        void LateUpdate()
        {
            if(car==null||view==null)return;
            var body=car.body;var fwd=car.transform.forward;fwd.y=0;fwd.Normalize();
            Vector3 want,look;
            if(aerial){want=body.position-fwd*320+Vector3.up*260;look=body.position+fwd*200;}
            else{want=body.position-fwd*7.8f+Vector3.up*2.9f;look=body.position+fwd*5+Vector3.up*1.1f;}
            if(!capturing){view.transform.position=Vector3.SmoothDamp(view.transform.position,want,ref camVelocity,aerial?.35f:.12f);view.transform.rotation=Quaternion.LookRotation(look-view.transform.position);}
            view.fieldOfView=aerial?50:WorldLook.SpeedFov(58,car.speed);
            // Under water: green murk.
            bool under=gen.WaterAt(view.transform.position,out float level,out _)&&view.transform.position.y<level;
            RenderSettings.fogColor=under?new Color(.12f,.32f,.34f):fogColour;RenderSettings.fogDensity=under?.0045f:fogDensity;
            // Names: all of them from the air; on the road, only places within reach (main towns 7 km, villages 3 km).
            foreach(var (t,s) in labels){var to=t.position-view.transform.position;float d=to.magnitude;t.gameObject.SetActive(aerial||capturing||d<(s>=1?7000:3000));t.rotation=Quaternion.LookRotation(to);t.localScale=Vector3.one*s*Mathf.Clamp(d/220,1,40);}
        }

        // ---------------------------------------------------------------- pacing report
        static readonly string[] PaceBuilds={"stock","C","B","A","S","E","max"};
        void Pacing(string path)
        {
            var tunings=PaceBuilds.ToDictionary(b=>b,b=>EquipmentCatalog.Stats(b=="stock"?EquipmentCatalog.StockLoadout():b=="max"?EquipmentCatalog.MaxBuild():ClassBuild(b)));
            var md=new StringBuilder();md.Append("# Scale test pacing (SpeedProfile)\n\n");
            md.Append($"Generated {DateTime.Now:yyyy-MM-dd HH:mm}. Land {gen.LandKm2:0} km², {gen.roads.Count} roads, {gen.roads.Sum(r=>r.length)/1000:0.0} km. Ideal drive from a standstill at each junction, no traffic; gravel, ice and sea floor at dirt grip (ice ×0.6).\n\n");
            md.Append("Top speeds: "+string.Join(" · ",PaceBuilds.Select(b=>$"{b} {tunings[b].AttainableSpeed*3.6f:0} km/h"))+"\n\n");
            md.Append("## Roads\n\n| Road | Type | km | Max grade | Climb m | Tightest turn m | Deepest cut m | Highest fill m | "+string.Join(" | ",PaceBuilds)+" |\n|---|---|---|---|---|---|---|---|"+string.Concat(PaceBuilds.Select(_=>"---|"))+"\n");
            var times=new Dictionary<(ContinentGenerator.BuiltRoad,string,bool),float>();
            foreach(var r in gen.roads) {
                md.Append($"| {r.Label} | {r.plan.type}{(r.hairpins?" (hairpins)":"")} | {r.length/1000:0.00} | {r.maxGrade*100:0.0}% | {r.climb:0} | {(r.minRadius<1e6f?r.minRadius.ToString("0"):"—")} | {r.maxCut:0} | {r.maxFill:0} |");
                foreach(var b in PaceBuilds) {
                    var t=Surface(tunings[b],r.plan.type);float there=SpeedProfile.Time(r.points,t),back=SpeedProfile.Time(r.points.Reverse().ToArray(),t);
                    times[(r,b,true)]=there;times[(r,b,false)]=back;md.Append($" {Clock((there+back)*.5f)} |");
                }
                md.Append('\n');
            }
            md.Append("\n## From Bayview to every place (quickest roads, Dive kit allowed)\n\n| Place | Class | km | "+string.Join(" | ",PaceBuilds)+" |\n|---|---|---|"+string.Concat(PaceBuilds.Select(_=>"---|"))+"\n");
            foreach(var place in ContinentPlan.Places) {
                if(place.id=="bayview")continue;md.Append($"| {place.name} | {place.cls} |");bool first=true;
                foreach(var b in PaceBuilds) {
                    var (secs,km)=Quickest("bayview",place.id,(r,fw)=>times[(r,b,fw)]);
                    if(first){md.Append($" {km:0.0} |");first=false;}
                    md.Append(float.IsInfinity(secs)?" — |":$" {Clock(secs)} |");
                }
                md.Append('\n');
            }
            md.Append("\n## Node heights moved to keep roads within grade\n\n| Node | Natural m | Used m | Change |\n|---|---|---|---|\n");
            foreach(var kv in gen.nodeHeight.OrderByDescending(kv=>Mathf.Abs(kv.Value-gen.nodeNatural[kv.Key])).Take(15))md.Append($"| {kv.Key} | {gen.nodeNatural[kv.Key]-ContinentGenerator.Sea:0} | {kv.Value-ContinentGenerator.Sea:0} | {kv.Value-gen.nodeNatural[kv.Key]:+0;-0} |\n");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));File.WriteAllText(path,md.ToString());
            Debug.Log("LITTLE_ROAD_SCALE_PACING "+Path.GetFullPath(path));
        }
        static CarTuning Surface(CarTuning t,string type){if(type=="gravel"||type=="undersea")t.roadGrip=t.dirtGrip;else if(type=="ice")t.roadGrip=t.dirtGrip*.6f;return t;}
        // Dijkstra over the road graph; cost(road, forwards?) in seconds. Returns seconds and km.
        (float,float) Quickest(string from,string to,Func<ContinentGenerator.BuiltRoad,bool,float> cost)=>Route(from,to,cost,out _);
        (float,float) Route(string from,string to,Func<ContinentGenerator.BuiltRoad,bool,float> cost,out List<(ContinentGenerator.BuiltRoad road,bool forwards)> steps)
        {
            var dist=new Dictionary<string,float>();var km=new Dictionary<string,float>();var prev=new Dictionary<string,(string,ContinentGenerator.BuiltRoad,bool)>();var done=new HashSet<string>();
            dist[from]=0;km[from]=0;steps=new();
            while(true) {
                string u=null;float best=float.PositiveInfinity;foreach(var kv in dist)if(!done.Contains(kv.Key)&&kv.Value<best){best=kv.Value;u=kv.Key;}
                if(u==null||u==to)break;done.Add(u);
                foreach(var r in gen.roads) {
                    string v;bool fw;if(r.plan.a==u){v=r.plan.b;fw=true;}else if(r.plan.b==u){v=r.plan.a;fw=false;}else continue;
                    float nd=best+cost(r,fw);if(!dist.TryGetValue(v,out float old)||nd<old){dist[v]=nd;km[v]=km[u]+r.length/1000;prev[v]=(u,r,fw);}
                }
            }
            if(!dist.ContainsKey(to))return (float.PositiveInfinity,0);
            for(string c=to;c!=from;c=prev[c].Item1)steps.Insert(0,(prev[c].Item2,prev[c].Item3));
            return (dist[to],km[to]);
        }

        // ---------------------------------------------------------------- the autopilot (--scale-autodrive a,b,c)
        bool autoDriving;List<Vector3> autoPath;float[] autoSpeed;int autoIndex;
        IEnumerator AutoDrive(string stops)
        {
            var ids=stops.Split(',').Select(s=>s.Trim()).ToArray();var t=EquipmentCatalog.Stats(fit);
            var first=ContinentPlan.Find(ids[0]);if(first!=null)PlaceAt(first,Quaternion.identity,true);yield return new WaitForSeconds(1);
            var path=new List<Vector3>();
            for(int s=0;s+1<ids.Length;s++) {
                Route(ids[s],ids[s+1],(r,fw)=>r.length,out var steps);
                foreach(var (r,fw) in steps){var p=fw?r.points:r.points.Reverse().ToArray();path.AddRange(path.Count==0?p:p.Skip(1));}
            }
            autoPath=path;autoSpeed=SpeedProfile.Speeds(path,Surface(t,"road"));autoIndex=0;autoDriving=true;
            car.body.position=path[0]+Vector3.up*.3f;var f0=path[3]-path[0];f0.y=0;car.body.rotation=Quaternion.LookRotation(f0);car.body.linearVelocity=Vector3.zero;
            RoadCar.ForceAutomaticGears=true;car.commandSource=AutoCommands;tripFrom=here=null;lastPos=default;float started=Time.time,limit=float.Parse(RaceTrial.Arg("--scale-autodrive-limit","1500"),Inv);int falls=0;
            Debug.Log($"LITTLE_ROAD_SCALE_AUTODRIVE_START stops={stops} km={SpeedProfile.Length(path)/1000:0.0} ideal={SpeedProfile.Time(path,t):0}s");
            float stuckFor=0;int stucks=0;string folder=Path.GetDirectoryName(Path.GetFullPath(Application.consoleLogPath??"x"));
            while(autoIndex<path.Count-8&&Time.time-started<limit) {
                if(car.body.position.y<gen.HeightAt(car.body.position)-4){falls++;Debug.Log($"LITTLE_ROAD_SCALE_FELL at={car.body.position}");var q=path[autoIndex]+Vector3.up*.3f;car.body.position=q;car.body.linearVelocity=Vector3.zero;}
                // Stuck: log what the car is touching, photograph it, and put it back on the road 40 m on.
                stuckFor=Mathf.Abs(car.speed)<.5f?stuckFor+Time.deltaTime:0;
                if(stuckFor>5) {
                    stucks++;stuckFor=0;var pos=car.body.position;
                    var near=Physics.OverlapBox(pos+car.transform.forward*2.5f+Vector3.up*.6f,new Vector3(1.4f,.7f,2.2f),car.transform.rotation).Where(h=>h.attachedRigidbody!=car.body).Select(h=>h.name).Distinct();
                    gen.NearestRoad(pos,60,out var road,out int ri,out float off);
                    Debug.Log($"LITTLE_ROAD_SCALE_STUCK n={stucks} at={pos} road=\"{road?.Label}\" off={off:0.0} roadY={(road!=null?road.points[ri].y:0):0.00} groundY={gen.HeightAt(pos):0.00} touching={string.Join("|",near)} path={autoIndex}");
                    yield return new WaitForEndOfFrame();var grab=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);grab.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);File.WriteAllBytes(Path.Combine(folder,$"stuck-{stucks}.png"),grab.EncodeToPNG());Destroy(grab);
                    autoIndex=Mathf.Min(path.Count-1,autoIndex+10);var f=path[Mathf.Min(path.Count-1,autoIndex+2)]-path[autoIndex];f.y=0;
                    car.body.position=path[autoIndex]+Vector3.up*.4f;car.body.rotation=Quaternion.LookRotation(f);car.body.linearVelocity=Vector3.zero;car.body.angularVelocity=Vector3.zero;
                }
                yield return null;
            }
            float secs=Time.time-started;bool done=autoIndex>=path.Count-8;
            Debug.Log($"LITTLE_ROAD_SCALE_AUTODRIVE_DONE complete={done} seconds={secs:0.0} falls={falls} stuck={stucks} recoveries={car.recoveries}");
            Application.Quit(done?0:3);
        }
        Vector2 AutoCommands(RoadCar c)
        {
            var p=c.body.position;
            // Advance along the path to the nearest point ahead.
            for(int n=0;n<40&&autoIndex<autoPath.Count-1;n++){var a=autoPath[autoIndex];var b=autoPath[autoIndex+1];if(Vector3.Dot(b-a,p-b)>0||(new Vector2(p.x-b.x,p.z-b.z)).magnitude<(new Vector2(p.x-a.x,p.z-a.z)).magnitude)autoIndex++;else break;}
            // A short look-ahead (hairpins are about 30 m across): 7 m at a crawl, about 0.75 s of travel at speed.
            float look=Mathf.Max(7,Mathf.Abs(c.speed)*.75f);int ahead=Mathf.Min(autoPath.Count-1,autoIndex+Mathf.CeilToInt(look/4));
            var delta=autoPath[ahead]-p;delta.y=0;float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(c.transform.forward,Vector3.up),delta,Vector3.up)*Mathf.Deg2Rad;
            float steer=Mathf.Clamp(2*Mathf.Sin(angle)/Mathf.Max(3,delta.magnitude)*Mathf.Max(c.speed,3)/c.tuning.yaw,-1,1);
            // The profile starts from a standstill, so look a few points ahead (and never ask for less than a crawl).
            float target=Mathf.Max(6,autoSpeed[Mathf.Min(autoSpeed.Length-1,autoIndex+3+Mathf.CeilToInt(Mathf.Abs(c.speed)*.6f/4))]*.88f);
            return new Vector2(Mathf.Clamp((target-c.speed)*.6f,-1,1),steer);
        }

        // ---------------------------------------------------------------- photographs (--scale-capture folder)
        bool capturing;
        IEnumerator Capture(string folder)
        {
            Directory.CreateDirectory(folder);capturing=true;
            IEnumerator Shot(string name,Vector3 from,Vector3 at)
            {
                NearAround(from,true);view.transform.SetPositionAndRotation(from,Quaternion.LookRotation(at-from));for(int f=0;f<6;f++)yield return null;
                LateUpdateLabels();yield return new WaitForEndOfFrame();var grab=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);grab.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);File.WriteAllBytes(Path.Combine(folder,name+".png"),grab.EncodeToPNG());Destroy(grab);yield return null;Debug.Log("LITTLE_ROAD_SCALE_SHOT "+name);
            }
            var bay=ContinentPlan.ToWorld(ContinentPlan.Find("bayview").Km,gen.nodeHeight["bayview"]);
            map.SetActive(true);mapCar.anchoredPosition=MapPoint(car.body.position);yield return Shot("00-map",bay+Vector3.up*50,bay+Vector3.forward*100);map.SetActive(false);
            yield return Shot("01-overview",ContinentPlan.ToWorld(10,-2f,9000),ContinentPlan.ToWorld(10,9.5f,0));
            yield return Shot("02-north-coast",bay+new Vector3(-1500,900,1800),bay+new Vector3(1500,0,-4000));
            foreach(var id in new[]{"hillcrest","vinelands","mycity","summit","pinewood","redrock","frostcrown","aurora","coralkey"}) {
                var pl=ContinentPlan.Find(id);PlaceAt(pl,Quaternion.identity,false);for(int f=0;f<10;f++)yield return null;
                var fwd=car.transform.forward;fwd.y=0;fwd.Normalize();var pos=car.body.position;
                yield return Shot($"{id}-road",pos-fwd*8+Vector3.up*3,pos+fwd*30+Vector3.up*2);
                yield return Shot($"{id}-aerial",pos-fwd*600+Vector3.up*380,pos+fwd*600);
            }
            Debug.Log("LITTLE_ROAD_SCALE_CAPTURE_DONE "+Path.GetFullPath(folder));Application.Quit(0);
        }
        void LateUpdateLabels(){foreach(var (t,s) in labels){var to=t.position-view.transform.position;t.rotation=Quaternion.LookRotation(to);t.localScale=Vector3.one*s*Mathf.Clamp(to.magnitude/220,1,40);}}
    }
}
