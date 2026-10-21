using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Authored height fields, masonry and planted areas for the actual route.
    // The road stays at its established datum; a real mesh deck crosses the inlet.
    public sealed partial class CoastalLandscape
    {
        public const float SeaLevel=-11.8f;
        public static bool ValleyDetail=true;
        // Whether this build includes Bayview island (always, except a race whose area is far from it: RaceArea).
        public static bool Island=>RaceArea.Near(Vector2.zero,250);   // (the raceway sits behind the headland ridge from it)
        public readonly float bridgeDistance;
        readonly Circuit track;
        readonly Transform root;
        readonly Vector3 bridgePoint,bridgeForward,bridgeRight,post;
        readonly Material grass,dryGrass,rock,lightRock,stone,cap,wood,leaf,leafLight;
        readonly CoastalMasonry masonry;
        readonly Material mortar;
        // The island landscape of the current world (the grass layer asks it where grass may grow).
        public static CoastalLandscape Current {get;private set;}
        public CoastalLandscape(Circuit circuit)
        {
            Current=this;track=circuit;bridgeDistance=track.Nearest(new Vector3(-117,0,9),out _);
            bridgePoint=track.At(bridgeDistance);bridgeForward=track.Forward(bridgeDistance);bridgeRight=track.Right(bridgeDistance);
            post=track.At(track.length*.46f)-track.Right(track.length*.46f)*16;
            root=new GameObject("Bayview coast / authored landforms").transform;
            grass=Surface("Coastal meadow",new Color(.38f,.47f,.24f));dryGrass=Surface("Sun-dried meadow",new Color(.48f,.52f,.29f));
            rock=Surface("Layered coastal limestone",new Color(.52f,.48f,.39f));lightRock=Surface("Pale limestone seams",new Color(.66f,.62f,.51f));
            masonry=new CoastalMasonry();
            // Piers and curved arch soffits need world-space mapping: primitive
            // cube UVs stretch a single image over their full twelve-metre height.
            stone=TerrainMaterial("World-mapped bridge limestone",new Color(.35f,.43f,.24f),new Color(.60f,.56f,.46f));
            stone.SetFloat("_RockOnly",1);stone.SetFloat("_RockScale",1.15f);stone.SetFloat("_NormalStrength",.25f);stone.SetColor("_RockColor",new Color(1.18f,1.20f,1.19f));
            WorldLook.Terrain(stone,WorldLook.MasonryStylise);stone.SetFloat("_Bands",0);
            cap=new Material(stone){name="Pale cut bridge voussoirs"};cap.SetColor("_RockColor",new Color(1.28f,1.29f,1.27f));
            wood=Surface("Coast tree bark",new Color(.29f,.23f,.14f));leaf=Surface("Cypress deep foliage",new Color(.16f,.29f,.17f));leafLight=Surface("Olive silver green",new Color(.34f,.43f,.24f));
            mortar=Surface("Recessed warm limestone mortar",new Color(.63f,.60f,.52f));
        }
        static float Blend(float a,float b,float x)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,x));
        static float FlatDistance(Vector3 a,Vector3 b)=>new Vector2(a.x-b.x,a.z-b.z).magnitude;
        static float VillageDistance(Vector3 p)=>NewBayview.On?NewBayview.BuiltDistance(p):new Vector2(Mathf.Max(-96-p.x,0,p.x-37),Mathf.Max(-130-p.z,0,p.z+90)).magnitude;
        // A checksum of the circuit the island is built round (the cliffs' cooked rings depend on it: CoastalCliffs).
        public ulong TrackKey(){ulong h=1469598103934665603;void Mix(float f){h=(h^(uint)System.BitConverter.SingleToInt32Bits(f))*1099511628211;}
            Mix(track.length);for(int k=0;k<64;k++){var p=track.At(track.length*k/64);Mix(p.x);Mix(p.y);Mix(p.z);}Mix(post.x);Mix(post.z);return h;}
        public bool SolidClearance(Vector3 p)
        {
            float d=track.Nearest(new Vector3(p.x,0,p.z),out _);
            return FlatDistance(p,track.At(d))>13.5f&&VillageDistance(p)>5&&FlatDistance(p,post)>22;
        }
        // Where Bayview's meadow grass may grow: up to the verge (9.5 m from the circuit centre, like Hillcrest's
        // 8.5 m from its roads), and in the village only on the green between the shops and the promenade.
        // Open, gentle ground for a crop plot: well off the circuit, the village and the causeway.
        public bool PlotGround(Vector3 p,float half)
        {
            if(RoadDistance(p)<half+24||VillageDistance(p)<half+18||FlatDistance(p,post)<half+22||NearCauseway(p))return false;
            float h=Height(p.x,p.z),low=h,high=h;
            for(int i=0;i<8;i++){float a=i*Mathf.PI*.25f;float s=Height(p.x+Mathf.Cos(a)*half,p.z+Mathf.Sin(a)*half);low=Mathf.Min(low,s);high=Mathf.Max(high,s);}
            return h>1.5f&&high-low<4.5f;
        }
        public float RoadDistance(Vector3 p){float d=track.Nearest(new Vector3(p.x,0,p.z),out _);return FlatDistance(p,track.At(d));}
        public bool GrassClearance(Vector3 p,out bool villageGreen)
        {
            villageGreen=false;float d=track.Nearest(new Vector3(p.x,0,p.z),out _);
            if(FlatDistance(p,track.At(d))<9.5f||FlatDistance(p,post)<22)return false;
            if(VillageDistance(p)>5)return true;
            villageGreen=p.x>-80&&p.x<24&&p.z<-118.5f&&p.z>-128.5f;return villageGreen;
        }
        public bool SurfacePoint(Vector3 p,out RaycastHit surface,bool includeTalus=true)
        {
            surface=default;float nearest=float.MaxValue;
            foreach(var hit in Physics.RaycastAll(new Vector3(p.x,90,p.z),Vector3.down,150)) {
                if(hit.collider.name!="Continuous sculpted island collider"&&!hit.collider.name.StartsWith("Layered coastal outcrop")&&(!includeTalus||!hit.collider.name.StartsWith("Fractured coastal talus")))continue;
                if(hit.distance<nearest){surface=hit;nearest=hit.distance;}
            }
            return nearest<float.MaxValue;
        }
        float farmPadY=float.NaN;
        public float Height(float x,float z)
        {
            if(NewBayview.On)return Quay(x,z,NewBayview.Height(x,z,track));   // (the new Bayview's land, with the same quay and causeway: docs/79)
            var p=new Vector3(x,0,z);float road=track.Nearest(p,out _);float routeDistance=FlatDistance(p,track.At(road));
            float noise=Mathf.PerlinNoise((x+510)*.019f,(z+370)*.019f);
            float radius=Mathf.Sqrt(x*x/(154*154)+z*z/(120*120));
            float land=1-Blend(.91f+noise*.09f,1.15f+noise*.09f,radius);
            float ridge=23*Mathf.Exp(-((x+26)*(x+26)/3200+z*z/1600));
            float height=Mathf.Lerp(-25,5+ridge+noise*8,land);
            float erosion=(Mathf.PerlinNoise((x+72)*.071f,(z-51)*.071f)-.5f)*3.5f;
            float bedding=Mathf.Floor(height/3)*3+Mathf.SmoothStep(0,1,Mathf.Repeat(height,3)/3)*3;
            height=Mathf.Lerp(height,bedding,.65f)+erosion*land;
            // The entire racing surface and recovery shoulder remain supported.
            height=Mathf.Lerp(height,0,1-Blend(14,27,routeDistance));
            // Broken banks beyond the protected shoulder. Broad gullies and
            // shorter ledges interrupt the uniform cut slope beneath outcrops.
            float bank=Blend(14,20,routeDistance)*(1-Blend(31,48,routeDistance))*Blend(1,5,height);
            height+=bank*((Mathf.PerlinNoise(x*.22f+41,z*.22f+93)-.5f)*2.2f+
                (Mathf.PerlinNoise(x*.075f+19,z*.075f+71)-.5f)*3);
            height=Mathf.Lerp(height,0,1-Blend(0,14,VillageDistance(p)));
            height=Mathf.Lerp(height,0,1-Blend(18,26,FlatDistance(p,post)));
            // Farm: level ground for the yard and fields (never inside the race shoulder).
            float farm=(1-Blend(30,46,FlatDistance(p,BayviewCountryside.Farm)))*Blend(27,34,routeDistance);
            if(farm>0&&!float.IsNegativeInfinity(farmPadY)) {
                if(float.IsNaN(farmPadY)){farmPadY=float.NegativeInfinity;farmPadY=Height(BayviewCountryside.Farm.x,BayviewCountryside.Farm.z);}
                height=Mathf.Lerp(height,farmPadY,farm);
            }
            Vector3 delta=p-bridgePoint;float along=Vector3.Dot(delta,bridgeForward),across=Vector3.Dot(delta,bridgeRight);
            float wobble=(Mathf.PerlinNoise(x*.08f+3,z*.08f+8)-.5f)*9;
            float inlet=(1-Blend(15+wobble*.6f,25+wobble*.6f,Mathf.Abs(along)))*Blend(-32+wobble,-18+wobble*.4f,across);
            height=Mathf.Lerp(height,Mathf.Min(height,-15.5f),inlet);
            // Harbour: the village pad ends at a sea wall (face z -134.4) above a shallow basin.
            // Deep sea stays deep; the cut hides inside the wall's 1.2 m core.
            float harbourX=Blend(-92,-80,x)*(1-Blend(22,32,x));
            // Keep the promenade flat right up to the wall (the coast's natural fall leaked in).
            height=Mathf.Lerp(height,0,harbourX*Blend(118,126,-z)*(1-Blend(133.8f,135.2f,-z)));
            height=Mathf.Lerp(height,Mathf.Min(height,-14.5f),harbourX*Blend(133.8f,135.2f,-z));
            return Quay(x,z,height);
        }
        // The Halt quay and the causeway's cut, on whichever island: the mainland's contract.
        float Quay(float x,float z,float height)
        {
            // Bayview Halt quay: flat out to its sea walls (the drop hides in the wall cores), deep
            // water beyond them; its north side runs into the island.
            float quay=Blend(HillcrestRoute.QuayWest-.8f,HillcrestRoute.QuayWest+.6f,x)*(1-Blend(HillcrestRoute.QuayEast-.6f,HillcrestRoute.QuayEast+.8f,x))
                *Blend(HillcrestRoute.QuaySouth-.8f,HillcrestRoute.QuaySouth+.6f,z)*(1-Blend(HillcrestRoute.QuayNorth-4,HillcrestRoute.QuayNorth+8,z));
            float apron=Blend(HillcrestRoute.QuayWest-12,HillcrestRoute.QuayWest-10,x)*(1-Blend(HillcrestRoute.QuayEast+10,HillcrestRoute.QuayEast+12,x))
                *Blend(HillcrestRoute.QuaySouth-12,HillcrestRoute.QuaySouth-10,z)*(1-Blend(HillcrestRoute.QuayNorth-4,HillcrestRoute.QuayNorth+8,z));
            height=Mathf.Lerp(height,Mathf.Min(height,-14.5f),apron*(1-quay)*(1-Blend(-4,-1,height)));
            // The shore round the quay settles to quay level (no knolls towering over the forecourt).
            float shore=Blend(HillcrestRoute.QuayWest-26,HillcrestRoute.QuayWest-4,x)*(1-Blend(HillcrestRoute.QuayEast+4,HillcrestRoute.QuayEast+26,x))
                *Blend(HillcrestRoute.QuaySouth-26,HillcrestRoute.QuaySouth-4,z)*(1-Blend(HillcrestRoute.QuayNorth-2,HillcrestRoute.QuayNorth+10,z));
            height=Mathf.Lerp(height,Mathf.Min(height,0),shore);
            height=Mathf.Lerp(height,0,quay);
            return CausewayCut(x,z,height);
        }
        Material Surface(string name,Color color)
        {
            var material=TrialWorld.Material(name,color,0,.12f);var texture=new Texture2D(128,128,TextureFormat.RGB24,true);texture.name=name+" grain";
            var pixels=new Color[128*128];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
                float n=.93f+.055f*Mathf.PerlinNoise(x*.16f,y*.16f)+.015f*Mathf.PerlinNoise(x*.73f,y*.73f);
                pixels[y*128+x]=new Color(n,n,n);
            }
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;material.mainTexture=texture;return material;
        }
        GameObject MeshObject(string name,Mesh mesh,Material[] materials,bool collision=false)
        {
            foreach(var vertex in mesh.vertices)if(!float.IsFinite(vertex.x)||!float.IsFinite(vertex.y)||!float.IsFinite(vertex.z))throw new System.InvalidOperationException("Non-finite vertex in "+name);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;
            if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
        }
        GameObject Piece(string name,Vector3 p,Vector3 size,Material mat,bool solid=false,PrimitiveType shape=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root);go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
            var collider=go.GetComponent<Collider>();
            if(!solid){collider.enabled=false;Object.Destroy(collider);}else {
                go.tag="Respawn";if(shape!=PrimitiveType.Cube){collider.enabled=false;Object.Destroy(collider);go.AddComponent<MeshCollider>().sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;}
            }
            return go;
        }
        // A hash of the whole built world (docs/72: proving a load-time change builds the same world). Every placed piece
        // (name, position, rotation, scale, its renderers' materials, layer and static flag, its colliders, LODs), and every
        // renderer and collider under the world's root (name, world bounds, materials, layer). Lines are sorted, so the build
        // order doesn't matter, and bounds don't change with static batching. --world-fingerprint [file] logs it straight after
        // the world build, with every streamed tile and district built (AdventureGame), so where the car starts doesn't count.
        public static Transform WorldRoot{get;private set;}
        // Static batching is a drawing optimisation, and batched renderers report their batch's bounds: world fingerprint
        // runs turn it off (--no-static-batching) so they compare the geometry itself, however it was batched.
        public static bool StaticBatching=>!RaceTrial.Has("--no-static-batching");
        public static string Fingerprint()
        {
            int pieces=0,renderers=0,colliders=0;var all=new System.Collections.Generic.List<string>();var line=new System.Text.StringBuilder();
            void Mix(string v)=>line.Append(v).Append(' ');
            // Things that move (sheep, spinning, bobbing and animated props, idling residents' cars, physics bodies) count by what
            // they are, not where they happen to be.
            var animated=BayviewKit.AnimatedRoot;
            bool Moves(Component c)=>c.transform.IsChildOf(animated)||c.GetComponentInParent<Rigidbody>(true)!=null||c.GetComponentInParent<Grazer>(true)!=null||c.GetComponentInParent<KitSpin>(true)!=null||
                c.GetComponentInParent<KitBob>(true)!=null||c.GetComponentInParent<ResidentIdle>(true)!=null||c.GetComponentInParent<BalloonDrift>(true)!=null||c.GetComponentInParent<Spin>(true)!=null;
            string Materials(Renderer r){var sb=new System.Text.StringBuilder();foreach(var m in r.sharedMaterials)sb.Append(m==null?"null":m.name+"|"+m.shader.name+"|"+(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor").ToString("F3"):"-")+"|"+(m.HasProperty("_Pattern")?m.GetFloat("_Pattern").ToString("F2"):"-")).Append(' ');return sb.ToString();}
            foreach(var go in BayviewKit.Placed) {
                if(go==null)continue;pieces++;var t=go.transform;line.Clear();Mix(go.name);if(!Moves(t)){Mix(t.position.ToString("F3"));Mix(t.rotation.ToString("F4"));}Mix(t.lossyScale.ToString("F3"));
                foreach(var r in go.GetComponentsInChildren<Renderer>(true)){renderers++;Mix(r.name);Mix(r.gameObject.layer.ToString());Mix(r.gameObject.isStatic?"s":"d");Mix(Materials(r));}
                foreach(var c in go.GetComponentsInChildren<Collider>(true)){colliders++;Mix(c.GetType().Name);if(c is BoxCollider bx){Mix(bx.center.ToString("F3"));Mix(bx.size.ToString("F3"));}Mix(c.tag);}
                var lod=go.GetComponent<LODGroup>();if(lod!=null)Mix("lod"+lod.GetLODs().Length+lod.fadeMode);all.Add("P "+line);
            }
            int meshes=0,solids=0;
            if(WorldRoot!=null) {
                foreach(var r in WorldRoot.GetComponentsInChildren<Renderer>(true)){meshes++;var b=r.bounds;all.Add(Moves(r)?$"R {r.name} moving {r.gameObject.layer} {Materials(r)}":$"R {r.name} {b.center:F2} {b.size:F2} {r.gameObject.layer} {Materials(r)}");}
                foreach(var c in WorldRoot.GetComponentsInChildren<Collider>(true)){solids++;var b=c.bounds;all.Add(Moves(c)?$"C {c.GetType().Name} {c.name} moving {c.isTrigger} {c.gameObject.layer}":$"C {c.GetType().Name} {c.name} {b.center:F2} {b.size:F2} {c.isTrigger} {c.gameObject.layer}");}
            }
            all.Sort(System.StringComparer.Ordinal);ulong h=1469598103934665603;
            foreach(var l in all){foreach(char ch in l){h^=ch;h*=1099511628211;}h^=10;h*=1099511628211;}
            string file=RaceTrial.Arg("--world-fingerprint");if(!string.IsNullOrEmpty(file)&&!file.StartsWith("--"))System.IO.File.WriteAllText(file,string.Join("\n",all));
            return $"pieces={pieces} renderers={renderers} colliders={colliders} worldRenderers={meshes} worldColliders={solids} hash={h:x16}";
        }
        // Sub-step timings inside the slow load steps (docs/72): LITTLE_ROAD_LOAD_LAP <what> ms since the last lap.
        static System.Diagnostics.Stopwatch lapClock;
        public static string LastLap{get;private set;}
        public static void Lap(string what){LastLap=what;if(lapClock==null){lapClock=System.Diagnostics.Stopwatch.StartNew();return;}Debug.Log($"LITTLE_ROAD_LOAD_LAP {what} ms={lapClock.ElapsedMilliseconds}");lapClock.Restart();}
        void LandMesh()
        {
            Lap("land start");
            int nx=LandNX,nz=LandNZ;const float step=LandStep;
            var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var groups=new List<int>[4];for(int i=0;i<4;i++)groups[i]=new();
            // Rows in parallel (docs/72): Height is pure maths once its one lazy value (the farm pad) is set.
            Height(BayviewCountryside.Farm.x,BayviewCountryside.Farm.z);
            System.Threading.Tasks.Parallel.For(0,nz,z=>{for(int x=0;x<nx;x++){int i=z*nx+x;float wx=(x-(nx-1)*.5f)*step,wz=(z-(nz-1)*.5f)*step;vertices[i]=new Vector3(wx,Height(wx,wz),wz);uv[i]=new Vector2(wx,wz)*.2f;}});
            Lap("land heights");landGrid=vertices;
            void Triangle(int a,int b,int c) {
                Vector3 normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized;
                Vector3 middle=(vertices[a]+vertices[b]+vertices[c])/3;
                int group=normal.y<.75f||middle.y< -1?2+(Mathf.PerlinNoise(middle.x*.13f+20,middle.z*.13f+20)>.55f?1:0):Mathf.PerlinNoise(middle.x*.05f+30,middle.z*.05f+30)>.53f?1:0;
                groups[group].Add(a);groups[group].Add(b);groups[group].Add(c);
            }
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++){int a=z*nx+x;Triangle(a,a+nx,a+1);Triangle(a+1,a+nx,a+nx+1);}
            var mesh=new Mesh{name="Sculpted coastal terrain",indexFormat=IndexFormat.UInt32,vertices=vertices,uv=uv,subMeshCount=4};
            for(int i=0;i<4;i++)mesh.SetTriangles(groups[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var terrain=TerrainMaterial("Slope-blended meadow and limestone",new Color(.35f,.43f,.24f),new Color(.60f,.56f,.46f));
            Lap("land mesh");var island=MeshObject("Continuous sculpted island collider",mesh,new[]{terrain,terrain,terrain,terrain});
            var clip=RoadTerrainCollision.Build(track,mesh);Lap("land collider clip");island.AddComponent<MeshCollider>().sharedMesh=clip;Lap("land collider cook");
        }
        Material TerrainMaterial(string name,Color meadow,Color stoneColour)
        {
            var material=new Material(Shader.Find("LittleRoad/CoastalTerrain")){name=name};
            CoastalSurfaces.Apply(material);
            material.SetTexture("_NoiseTex",grass.mainTexture);
            material.SetColor("_GrassColor",new Color(meadow.r/.35f,meadow.g/.43f*1.18f,meadow.b/.24f*.82f));
            material.SetColor("_RockColor",new Color(stoneColour.r/.60f,stoneColour.g/.56f,stoneColour.b/.46f));
            material.SetColor("_DryColor",new Color(1.16f,1.13f,.86f));WorldLook.Terrain(material);return material;
        }
        void DistantHeadland()
        {
            const int nx=85,nz=49;var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];var tris=new List<int>();
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++) {
                float wx=(x-42)*6,wz=(z-24)*6;float r=Mathf.Sqrt(wx*wx/55000+wz*wz/18000);
                // Two staggered ridge lines create a broken coastal skyline
                // instead of one smooth inflated mound behind the village.
                float n=Mathf.PerlinNoise(wx*.024f+71,wz*.027f+28);
                float crest=.72f+.28f*(1-Mathf.Abs(n*2-1));
                float front=38*Mathf.Max(0,1-Mathf.Abs(wz+34+18*Mathf.Sin(wx*.021f))/53);
                float rear=72*Mathf.Max(0,1-Mathf.Abs(wz-38+22*Mathf.Sin(wx*.014f+1.7f))/66);
                float fracture=(Mathf.PerlinNoise(wx*.065f+91,wz*.062f+12)-.5f)*11;
                float ridge=7+(Mathf.Max(front,rear)*crest+fracture)*.68f;
                float h=Mathf.Lerp(-24,ridge,1-Blend(.55f,1.05f,r));
                int i=z*nx+x;vertices[i]=new Vector3(wx*1.3f+20,h,wz-590);uv[i]=new Vector2(wx,wz)*.15f;
                if(x<nx-1&&z<nz-1){tris.Add(i);tris.Add(i+nx);tris.Add(i+1);tris.Add(i+1);tris.Add(i+nx);tris.Add(i+nx+1);}
            }
            var mesh=new Mesh{name="Southern headland ridges",vertices=vertices,uv=uv,triangles=tris.ToArray()};mesh.RecalculateNormals();
            var distant=TerrainMaterial("Distant sage hills",new Color(.35f,.43f,.24f),new Color(.60f,.56f,.46f));
            distant.SetColor("_RockColor",new Color(.34f,.57f,.85f));
            distant.SetColor("_GrassColor",new Color(.40f,.65f,.60f));
            distant.SetColor("_DryColor",new Color(.51f,.70f,.76f));
            distant.SetColor("_ToyGrassA",new Color(.42f,.66f,.50f));distant.SetColor("_ToyGrassB",new Color(.33f,.56f,.48f));
            distant.SetColor("_ToyRockA",new Color(.55f,.62f,.62f));distant.SetColor("_ToyRockB",new Color(.48f,.56f,.60f));distant.SetFloat("_Flowers",0);distant.SetFloat("_Bands",0);distant.SetFloat("_Stylise",1);
            MeshObject("Distant layered headland",mesh,new[]{distant},true);
        }
        void Bridge()
        {
            // The 24m deck follows the circuit, including its slight curvature.
            CurvedParapet(bridgeDistance-25,bridgeDistance+25,0,25,-.95f,-.01f,stone,false);
            // Three real open arches on both faces, made from wedge voussoirs.
            Quaternion facing=Quaternion.LookRotation(bridgeForward);
            Vector3 Point(float x,float y,float z)=>bridgePoint+bridgeRight*x+Vector3.up*y+bridgeForward*z;
            foreach(int side in new[]{-1,1}) {
                for(int arch=0;arch<3;arch++) {
                    float centre=-12+arch*12;
                    for(int step=0;step<16;step++) {
                        float a=step*Mathf.PI/16,b=(step+1)*Mathf.PI/16;
                        var v=new Vector3[8];int k=0;
                        foreach(float x in new[]{side*12.0f-.55f,side*12.0f+.55f})foreach(float radius in new[]{4.8f,6f})foreach(float angle in new[]{a,b})v[k++]=Point(x,-7+Mathf.Sin(angle)*radius,centre+Mathf.Cos(angle)*radius);
                        int[] t={0,1,3,0,3,2,4,6,7,4,7,5,0,4,5,0,5,1,2,3,7,2,7,6,0,2,6,0,6,4,1,5,7,1,7,3};
                        var mesh=MasonryMesh("Limestone arch block",v,t,true);
                        MeshObject("Arch voussoir",mesh,new[]{step%4==0?cap:stone});
                        // Follow the outer arch exactly up to the deck. A stack
                        // of rectangular columns leaves visible holes here.
                        var infill=new Vector3[8];k=0;
                        foreach(float x in new[]{side*12.0f-.55f,side*12.0f+.55f})foreach(bool upper in new[]{false,true})foreach(float angle in new[]{a,b})infill[k++]=Point(x,upper?-.91f:-7+Mathf.Sin(angle)*6,centre+Mathf.Cos(angle)*6);
                        var fillMesh=MasonryMesh("Curved spandrel",infill,t,true);MeshObject("Arch spandrel",fillMesh,new[]{stone});
                    }
                }
                foreach(float z in new[]{-24f,-18f,-6f,6f,18f,24f}) {
                    var pier=Piece("Bridge masonry pier",Point(side*12,-9,z),new Vector3(1.4f,12,z== -24||z==24?5:2.4f),stone);pier.transform.rotation=facing;
                }
            }
            // Matching visible/collision parapets, outside the generous runoff.
            foreach(int side in new[]{-1,1}) {
                CurvedParapet(bridgeDistance-25,bridgeDistance+25,side*12.65f,.7f,0,1.06f,stone);
                CurvedParapet(bridgeDistance-25,bridgeDistance+25,side*12.65f,.9f,1.06f,1.23f,cap);
            }
        }
        void CurvedParapet(float start,float end,float offset,float width,float bottom,float top,Material material,bool obstacle=true)
        {
            int sections=Mathf.CeilToInt((end-start)/1.5f);var vertices=new Vector3[(sections+1)*4];var triangles=new List<int>();
            for(int i=0;i<=sections;i++) {
                float d=Mathf.Lerp(start,end,i/(float)sections);Vector3 p=track.At(d),r=track.Right(d);
                vertices[i*4]=p+r*(offset-width*.5f)+Vector3.up*bottom;vertices[i*4+1]=p+r*(offset+width*.5f)+Vector3.up*bottom;
                vertices[i*4+2]=p+r*(offset-width*.5f)+Vector3.up*top;vertices[i*4+3]=p+r*(offset+width*.5f)+Vector3.up*top;
                if(i<sections){int a=i*4;int[] t={a,a+4,a+2,a+2,a+4,a+6,a+1,a+3,a+5,a+3,a+7,a+5,a+2,a+6,a+3,a+3,a+6,a+7,a,a+1,a+4,a+1,a+5,a+4};triangles.AddRange(t);}
            }
            triangles.AddRange(new[]{0,2,1,1,2,3,sections*4,sections*4+1,sections*4+2,sections*4+1,sections*4+3,sections*4+2});
            var mesh=MasonryMesh("Continuous curved parapet",vertices,triangles.ToArray(),false);
            // The route support is the deck's single driving collision surface.
            var go=MeshObject(obstacle?"Bridge continuous parapet":"Bridge continuous deck",mesh,new[]{material},obstacle);if(obstacle)go.tag="Respawn";
            if(obstacle) {
                go.GetComponent<Renderer>().enabled=false;
                Vector3 Map(Vector3 point) {
                    float progress=Mathf.Clamp01((point.z+(end-start)*.5f)/(end-start))*sections;
                    int segment=Mathf.Min(Mathf.FloorToInt(progress),sections-1);float t=progress-segment;
                    Vector3 centre=Vector3.Lerp((vertices[segment*4]+vertices[segment*4+1])*.5f,(vertices[(segment+1)*4]+vertices[(segment+1)*4+1])*.5f,t);
                    Vector3 right=Vector3.Lerp((vertices[segment*4+1]-vertices[segment*4])/width,(vertices[(segment+1)*4+1]-vertices[(segment+1)*4])/width,t);
                    centre.y=0;return centre+right*point.x+Vector3.up*point.y;
                }
                if(bottom<.1f)masonry.Body(end-start,width,bottom,top,349+Mathf.RoundToInt(start*19)+(offset>0?1:0),Map);
                else masonry.Coping(end-start,width,bottom,top,451+Mathf.RoundToInt(start*29)+(offset>0?1:0),Map);
                // Recess the original mesh for mortar, retaining the original
                // full-width collider on the invisible parent object.
                var backing=(Vector3[])vertices.Clone();
                for(int i=0;i<=sections;i++)for(int corner=0;corner<4;corner++) {
                    backing[i*4+corner]-=track.Right(Mathf.Lerp(start,end,i/(float)sections))*(corner%2==0?-.032f:.032f);
                    if(corner>=2)backing[i*4+corner]-=Vector3.up*.035f;
                    if(i==0)backing[i*4+corner]+=track.Forward(start)*.05f;
                    if(i==sections)backing[i*4+corner]-=track.Forward(end)*.05f;
                }
                MeshObject("Recessed bridge mortar",MasonryMesh("Bridge mortar backing",backing,triangles.ToArray(),false),new[]{mortar});
            }
        }
        Mesh MasonryMesh(string name,Vector3[] points,int[] indices,bool reverse)
        {
            var vertices=new Vector3[indices.Length];var uv=new Vector2[indices.Length];var triangles=new int[indices.Length];
            for(int i=0;i<indices.Length;i+=3) {
                for(int corner=0;corner<3;corner++){int source=reverse&&corner>0?3-corner:corner;vertices[i+corner]=points[indices[i+source]];triangles[i+corner]=i+corner;}
                Vector3 normal=Vector3.Cross(vertices[i+1]-vertices[i],vertices[i+2]-vertices[i]).normalized;
                for(int corner=0;corner<3;corner++){Vector3 p=vertices[i+corner];uv[i+corner]=Mathf.Abs(normal.y)>.5f?new Vector2(p.x,p.z)*.3f:new Vector2(Vector3.Dot(p,bridgeForward),p.y)*.3f;}
            }
            var mesh=new Mesh{name=name,vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateTangents();return mesh;
        }
        void Wall(float d,int side,float offset,float length)
        {
            // Consecutive sections share the same curve endpoints. Rotated
            // straight boxes left repeated gaps on the outside of bends.
            float half=length*.5f;
            CurvedParapet(d-half,d+half,side*offset,.7f,0,1.06f,stone);
            CurvedParapet(d-half,d+half,side*offset,.9f,1.045f,1.215f,cap);
        }
        void Roadside()
        {
            // (with --prop-forge, Prop Forge's dry-stone wall runs here instead, with the same gaps: PropForgeBayview)
            if(PropForge.Enabled)return;
            for(float d=105;d<track.length-25;d+=3.6f) {
                if(RoadsideGap(d))continue;
                Wall(d,1,13.4f,3.6f);
            }
        }
        // Where the outer coast wall stops: before 105 m and in the last 25, at the bridge, at the Hillcrest mainline's
        // junction (adventure only: races keep the wall), and at open shoulders by the signed turnouts and postcard
        // approaches.
        float roadsideJunction=float.NaN;
        public bool RoadsideGap(float d)
        {
            if(d<105||d>=track.length-25)return true;
            if(float.IsNaN(roadsideJunction))roadsideJunction=ValleyDetail?track.Nearest(HillcrestRoute.Mainline.line[0],out _):-999;
            if(Mathf.Abs(d-bridgeDistance)<29||Mathf.Abs(d-roadsideJunction)<8)return true;
            for(int i=0;i<5;i++)if(Mathf.Abs(d-track.length*(.12f+i*.17f))<10)return true;
            return d>290&&d<345;
        }
        public Circuit Track=>track;
        // The island's drawn ground (LandMesh's grid, 1.5 m, split the way its triangles are), for Prop Forge's runs:
        // exactly what's drawn, where the analytic Height can differ by centimetres between grid points.
        Vector3[] landGrid;const float LandStep=1.5f;
        static int LandNX=>NewBayview.On?361:289;static int LandNZ=>NewBayview.On?337:257;   // (the new island is bigger: ±270 x ±252 m)
        public float Drawn(float x,float z)
        {
            if(landGrid==null)return Height(x,z);
            float gx=x/LandStep+(LandNX-1)*.5f,gz=z/LandStep+(LandNZ-1)*.5f;int ix=Mathf.FloorToInt(gx),iz=Mathf.FloorToInt(gz);
            if(ix<0||iz<0||ix>=LandNX-1||iz>=LandNZ-1)return Height(x,z);
            float fx=gx-ix,fz=gz-iz;int a=iz*LandNX+ix;
            float h00=landGrid[a].y,h10=landGrid[a+1].y,h01=landGrid[a+LandNX].y,h11=landGrid[a+LandNX+1].y;
            return fx+fz<=1?h00+fx*(h10-h00)+fz*(h01-h00):h11+(1-fx)*(h01-h11)+(1-fz)*(h10-h11);
        }
        void Cypress(Vector3 p,float h,int variant)
        {
            Piece("Rooted cypress trunk",p+Vector3.up*h*.28f,new Vector3(.38f,h*.56f,.38f),wood,true);
            const int rings=13,sides=12;var vertices=new Vector3[rings*sides];var triangles=new List<int>();
            for(int ring=0;ring<rings;ring++)for(int side=0;side<sides;side++) {
                float t=ring/(float)(rings-1),angle=side*Mathf.PI*2/sides;
                // Sin(pi) can round slightly negative. Fractional powers of
                // that value would invalidate every combined foliage bound.
                float radius=h*.14f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.65f)*(1-t*.45f);
                radius*=.89f+.17f*Mathf.PerlinNoise(side*.67f+variant,ring*.58f);
                vertices[ring*sides+side]=p+new Vector3(Mathf.Cos(angle)*radius,h*(.28f+t*.85f),Mathf.Sin(angle)*radius);
                if(ring<rings-1){int a=ring*sides+side,b=ring*sides+(side+1)%sides,c=a+sides,d=b+sides;triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);}
            }
            var mesh=new Mesh{name="Irregular cypress crown",vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();MeshObject("Cypress crown",mesh,new[]{leaf});
        }
        void Planting()
        {
            var vegetation=new CoastalVegetation();
            // Shared leaf meshes use GPU instancing and can exceed 65k vertices.
            // Keep them outside the terrain's static-combine hierarchy.
            var vegetationRoot=new GameObject("Bayview shared foliage / unbatched").transform;
            var rng=new System.Random(7341);bool kitTrees=BayviewKit.Has("tree_round");
            for(int i=0;i<640;i++) {
                float x=(float)rng.NextDouble()*310-155,z=(float)rng.NextDouble()*235-118;var p=new Vector3(x,0,z);
                float d=track.Nearest(p,out float lateral),distance=FlatDistance(p,track.At(d));
                if(distance<18||VillageDistance(p)<7||FlatDistance(p,post)<24||BayviewCountryside.Reserved(p)||NearCauseway(p))continue;
                float h=Height(x,z);if(h<-.1f||Mathf.Abs(Height(x+1,z)-h)>1||Mathf.Abs(Height(x,z+1)-h)>1)continue;
                float size=5+(float)rng.NextDouble()*5;
                if(!SurfacePoint(p,out var surface)||surface.normal.y<.62f)continue;p=surface.point;
                int species=i%7<2?0:i%7<5?1:2;
                // Seat the shared root mesh into the lowest point under its
                // footprint. Centre-only placement leaves downhill roots floating.
                float scale=size/8,lowest=p.y;bool supported=true;
                for(int rootSample=0;rootSample<12;rootSample++) {
                    float angle=rootSample*Mathf.PI/6;
                    Vector3 foot=p+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.82f*scale;
                    if(!SurfacePoint(foot,out var ground)){supported=false;break;}
                    lowest=Mathf.Min(lowest,ground.point.y);
                }
                if(!supported)continue;p.y=lowest-.06f*scale;
                if(kitTrees) {
                    // Toy-kit trees, a Mediterranean island mix: umbrella pine and cypress, olive groves (where the
                    // ground noise says so), blossom, round broadleaf and the odd poplar. Keep the windmill hilltop open.
                    if(FlatDistance(p,new Vector3(-24,0,12))<10)continue;
                    bool grove=Mathf.PerlinNoise(x*.018f+5,z*.018f+9)>.58f;int pick=i%10;
                    string id=grove?(pick<7?"olive_tree":"tree_cypress"):pick<2?"tree_cypress":pick<5?"tree_pine":pick<6?"tree_blossom":pick<8?"tree_round":pick<9?"tree_poplar":"olive_tree";
                    BayviewKit.Spawn(id,p+Vector3.down*.1f,i*47,vegetationRoot,scale:((id=="olive_tree"?.6f:.75f)+size/14)*(NewBayview.On?.8f:1));   // (--new-bayview: a fifth smaller, so the buildings read bigger: street review R1)
                }
                else vegetation.Plant(vegetationRoot,p,scale,species,i%3);
            }
            for(float d=15;d<track.length;d+=8) {
                if(Mathf.Abs(d-bridgeDistance)<31)continue;
                foreach(int side in new[]{-1,1}) {
                    Vector3 p=track.At(d)+track.Right(d)*side*(17+(float)rng.NextDouble()*6);
                    if(VillageDistance(p)<8||FlatDistance(p,post)<23||NearCauseway(p))continue;
                    if(!SurfacePoint(p,out var hit)||hit.normal.y<.65f)continue;p=hit.point;if(p.y< -2)continue;
                    float size=.7f+(float)rng.NextDouble()*1.1f;
                    var boulder=Piece("Roadside weathered limestone",p-hit.normal*size*.16f,new Vector3(size*1.6f,size*.8f,size),iMaterial(d),true,PrimitiveType.Sphere);
                    boulder.transform.rotation=Quaternion.FromToRotation(Vector3.up,hit.normal)*Quaternion.Euler(0,d*9,0);
                }
            }
            int shrubs=0;
            for(float d=5;d<track.length;d+=3.4f)foreach(int side in new[]{-1,1})for(int attempt=0;attempt<3;attempt++) {
                float along=d+(float)rng.NextDouble()*4-2;
                float offset=15.7f+(float)rng.NextDouble()*17;
                Vector3 p=track.At(along)+track.Right(along)*side*offset;
                if(!SolidClearance(p)||BayviewCountryside.Reserved(p)||NearCauseway(p)||Mathf.PerlinNoise(p.x*.06f+18,p.z*.06f+53)<.43f)continue;
                bool nearFind=false;
                for(int find=0;find<5;find++) {
                    float fd=track.length*(.12f+find*.17f);Vector3 fp=track.At(fd)+track.Right(fd)*(find%2==0?13:-13);
                    if(FlatDistance(p,fp)<3)nearFind=true;
                }
                if(nearFind||!SurfacePoint(p,out var hit)||hit.normal.y<.69f||hit.point.y<-.2f)continue;
                float scale=.85f+(float)rng.NextDouble()*.65f;p=hit.point;
                float low=p.y;
                for(int foot=0;foot<8;foot++) {
                    float a=foot*Mathf.PI*.25f;
                    if(SurfacePoint(p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.6f*scale,out var ground))low=Mathf.Min(low,ground.point.y);
                }
                p.y=low-.03f;
                if(kitTrees)BayviewKit.Spawn("bush",p+Vector3.down*.05f,(float)rng.NextDouble()*360,vegetationRoot,solid:false,scale:scale*.8f);
                else vegetation.PlantScrub(vegetationRoot,p,scale,shrubs%3,(float)rng.NextDouble()*360);shrubs++;
            }
            Debug.Log("LITTLE_ROAD_SCRUB patches="+shrubs);
        }
        Material iMaterial(float d)=>((int)d%3)==0?rock:lightRock;
        void Sea(){CoastalWater.Create(root);}
        // Load timing (every race entry and return rebuilds the world): LITTLE_ROAD_LOAD_STEP lines.
        static System.Diagnostics.Stopwatch stepClock;
        static long kitMsAt;static int kitAt;
        static void Step(string name)
        {
            LoopProbe.Mark(name);long kitMs=BayviewKit.SpawnClock.ElapsedMilliseconds;int kits=BayviewKit.Spawned;
            if(stepClock!=null)Debug.Log($"LITTLE_ROAD_LOAD_STEP {name} ms={stepClock.ElapsedMilliseconds} kit_ms={kitMs-kitMsAt} kits={kits-kitAt}");
            kitMsAt=kitMs;kitAt=kits;stepClock=System.Diagnostics.Stopwatch.StartNew();
        }
        // Progress for the loading card (fraction of the usual build time, next step's label).
        static void Next(float done,string label)=>LoadingScreen.Progress(done,label);
        public void Build()=>TrialWorld.RunNow(BuildRoutine());
        public System.Collections.IEnumerator BuildRoutine()
        {
            WorldTiles.Begin(ValleyDetail);Districts.Begin();WorldRoot=root;stepClock=null;BayviewKit.Footprints.Clear();BayviewKit.FootprintOwners.Clear();GroundFit.Reset();BayviewKit.Placed.Clear();HillcrestTown.BuntingEnds.Clear();Step("start");Next(0,"Shaping the island");yield return null;
            // (A race far from Bayview builds none of the island: RaceArea. Its heights stay analytic for everything else.)
            bool island=Island;
            if(island)LandMesh();Step("land");Next(.16f,"Raising the mainland");yield return null;
            Mainland();Step("mainland");Next(.33f,"Building the bridge");yield return null;
            if(!NewBayview.On){Bridge();Roadside();}else NewBayview.DrawGravel(root,track);masonry.Finish();Step("bridge-roadside-masonry");Next(.35f,"Carving the cliffs");yield return null;
            var cliffMaterial=TerrainMaterial("Weathered cliff limestone",new Color(.35f,.43f,.24f),new Color(.60f,.56f,.46f));cliffMaterial.SetFloat("_RockOnly",1);
            if(island&&!NewBayview.On)new CoastalCliffs(this,root,cliffMaterial).Build();Step("cliffs");   // (the new island has no CoastalCliffs: docs/79)
            Next(.53f,"Planting Bayview");yield return null;
            if(island){Physics.SyncTransforms();BayviewCountryside.ChoosePlots(this);Planting();}Step("planting");
            if(island&&NewBayview.On&&NewBayview.Windmill(Height))Step("tower-mill");   // (the new Bayview's own tower mill: docs/80)
            else if(island&&BayviewKit.Has("windmill")) {
                var hill=NewBayview.On?new Vector3(NewBayviewPlan.Windmill.x,0,NewBayviewPlan.Windmill.y):new Vector3(-24,0,12);hill.y=Height(hill.x,hill.z)-.4f;
                BayviewKit.Spawn("windmill",hill,200,BayviewKit.AnimatedRoot,new BayviewKit.Tint(Color.white,Toy.Hex(0x3a7bd5),Toy.Hex(0xff4d5e),Toy.Hex(0x3a7bd5)));
            }
            if(island&&!NewBayview.On)new BayviewCountryside(this,track,root).Build();Step("countryside");
            Physics.SyncTransforms();ValleyRoads();ValleyWater();Step("valley-roads-water");Next(.6f,"Laying the railway and Hillcrest");yield return null;
            // The town, railway and woods sit behind the headland ridge, out of sight of the Bayview
            // circuit: Bayview races skip them (ValleyDetail false) and keep their frame cost.
            // (A race leaves Hillcrest's town and railway out unless they're in its area: RaceArea.)
            bool hillcrest=RaceArea.Near(HillcrestRoute.Square,500);
            if(ValleyDetail){if(hillcrest)Railway();Step("railway");Physics.SyncTransforms();new HillcrestTown(this,root).Build();Step("hillcrest-town");Next(.72f,"Growing the woods");yield return null;MainlandTrees();Step("mainland-trees");
                // Quarry Hills far from where the car starts is built after the town is ready (Districts, docs/72).
                if(deferQuarry)Districts.Defer("quarry-hills",QuarryHillsSceneryRoutine(),SettleQuarryHills,Rect.MinMaxRect(QuarryHills.West-30,QuarryHills.South-30,QuarryHills.SeamX+30,1e6f));else QuarryHillsScenery();Step("quarry-hills");
                if(Bramblewick.Enabled){Physics.SyncTransforms();new BramblewickVillage(root).Build();Step("bramblewick");}if(Vinelands.Enabled){Physics.SyncTransforms();new VinelandsTown(root).Build();Step("vinelands");}Next(.93f,"Filling the sea");yield return null;}   // (Bramblewick: docs/82, step 3)
            // (Prop Forge last of the building, so its runs and objects see everything placed: the quay's dressing comes with Hillcrest)
            if(island&&PropForge.Enabled&&!NewBayview.On){Physics.SyncTransforms();PropForgeBayview.Build(this,root);Step("prop-forge");}
            Lap("settle start");Debug.Log("LITTLE_ROAD_RAILS_TRIMMED "+(deferQuarry?TrimRails(p=>!NearQuarryHills(p)):TrimRails()));   // (rails and junction corner walls in front of buildings; every build)
            Lap("settle trim rails");RegisterRoads();Lap("settle register roads");
            // Runtime static batching converts combined geometry to 16-bit
            // chunks. Keep the large grass mesh and displaced sea independent.
            Physics.SyncTransforms();Lap("settle sync");PlacementAudit.Settle();Lap("settle pieces");CarContact.SeatAll();Lap("settle cars");Step("settle-planting");
            if(StaticBatching)StaticBatchingUtility.Combine(root.gameObject);Step("static-batching");
            Sea();if(island)new CoastalGroundCover(this,track,root).Build();Step("sea-ground-cover");
            WorldTiles.EndBuild();
            Debug.Log("LITTLE_ROAD_LANDSCAPE_READY bridgeDistance="+bridgeDistance+" sea="+SeaLevel+" deferred="+WorldTiles.Deferred);
        }
    }
}

