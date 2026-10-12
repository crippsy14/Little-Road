using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // The reference map (--world-review --world-map): a straight-down orthographic render of the whole
    // world (north up) and every named place, taken from the game's own data, so names in chat and in
    // code match. Writes world-map.jpg and world-map.json (bounds, labels by kind) to the capture folder.
    public static class WorldMap
    {
        // The square the maps cover: Bayview and Hillcrest Valley, and with --quarry-hills Quarry Hills to the west as well.
        public static readonly float X0=QuarryHills.Enabled?-4400:-1150,Z0=QuarryHills.Enabled?-4100:-2100,Size=QuarryHills.Enabled?5900:2500;public const int Pixels=4096;
        public static int BakePixels=>QuarryHills.Enabled?4096:2048;
        [Serializable] public class Label{public string name,kind,note;public float x,z;}
        [Serializable] public class Data{public float x0=X0,z0=Z0,size=Size;public int pixels=Pixels;public string image="world-map.jpg";public List<Label> labels=new();}

        public static IEnumerator Capture(Camera cam,string folder,List<(string name,string role,Vector3 at)> residents,Circuit bayviewCircuit)
        {
            // Clear air, no drifting cloud shadows, full detail, long shadows for depth.
            RenderSettings.fog=false;var clock=UnityEngine.Object.FindAnyObjectByType<DayClock>();if(clock!=null)clock.enabled=false;
            Shader.SetGlobalVector("_HeightFogParams",Vector4.zero);if(RenderSettings.sun!=null)RenderSettings.sun.cookie=null;
            float lodBias=QualitySettings.lodBias;QualitySettings.lodBias=40;
            var urp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;float shadowDistance=urp!=null?urp.shadowDistance:0;if(urp!=null)urp.shadowDistance=2200;
            cam.orthographic=true;cam.orthographicSize=Size/2;cam.nearClipPlane=5;cam.farClipPlane=1600;cam.layerCullDistances=new float[32];cam.aspect=1;
            cam.transform.SetPositionAndRotation(new Vector3(X0+Size/2,900,Z0+Size/2),Quaternion.Euler(90,0,0));
            yield return new WaitForSeconds(2f);
            var rt=new RenderTexture(Pixels,Pixels,24);var old=cam.targetTexture;cam.targetTexture=rt;cam.Render();cam.targetTexture=old;
            var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(Pixels,Pixels,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,Pixels,Pixels),0,0);image.Apply();RenderTexture.active=active;
            Directory.CreateDirectory(folder);File.WriteAllBytes(Path.Combine(folder,"world-map.jpg"),image.EncodeToJPG(86));
            rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);
            QualitySettings.lodBias=lodBias;if(urp!=null)urp.shadowDistance=shadowDistance;
            var data=new Data();Collect(data,residents,bayviewCircuit);
            File.WriteAllText(Path.Combine(folder,"world-map.json"),JsonUtility.ToJson(data,true));
            Debug.Log($"LITTLE_ROAD_WORLD_MAP labels={data.labels.Count} folder={folder}");
        }

        // The in-game map (Minimap): the same straight-down view, rendered once while loading into a texture
        // that stays on the GPU. Lit as a clear afternoon whatever the time of day, without fog or cloud
        // shadows; every global it touches is put back afterwards.
        public static RenderTexture Bake(int pixels=0)
        {
            if(pixels<=0)pixels=BakePixels;
            var clock=System.Diagnostics.Stopwatch.StartNew();
            string cache=CachePath(pixels);
            if(cache!=null&&File.Exists(cache)) {
                try {
                    var bytes=File.ReadAllBytes(cache);var tex=new Texture2D(pixels,pixels,TextureFormat.DXT1,false);tex.LoadRawTextureData(bytes);tex.Apply(false,false);
                    var rt=Target(pixels);Graphics.Blit(tex,rt);UnityEngine.Object.Destroy(tex);
                    WorldCache.Touch(cache);Debug.Log($"LITTLE_ROAD_MAP_CACHE hit ms={clock.ElapsedMilliseconds} file={Path.GetFileName(cache)}");return rt;
                } catch(Exception e){Debug.LogWarning("LITTLE_ROAD_MAP_CACHE unreadable, baking: "+e.Message);}
            }
            WorldTiles.Complete();   // (the map shows every tree)
            var baked=Render(pixels);
            if(cache!=null)try {
                var prev=RenderTexture.active;RenderTexture.active=baked;var read=new Texture2D(pixels,pixels,TextureFormat.RGB24,false);read.ReadPixels(new Rect(0,0,pixels,pixels),0,0,false);RenderTexture.active=prev;
                read.Compress(false);Directory.CreateDirectory(Path.GetDirectoryName(cache));File.WriteAllBytes(cache,read.GetRawTextureData());UnityEngine.Object.Destroy(read);
                // Only the two builds used most recently keep their maps (the play copy and the dev build share the folder).
                WorldCache.Prune(Path.GetDirectoryName(cache));
            } catch(Exception e){Debug.LogWarning("LITTLE_ROAD_MAP_CACHE not saved: "+e.Message);}
            Debug.Log($"LITTLE_ROAD_MAP_CACHE baked ms={clock.ElapsedMilliseconds}");
            return baked;
        }
        static RenderTexture Target(int pixels)=>new RenderTexture(pixels,pixels,24,RenderTextureFormat.ARGB32){name="World map",useMipMap=true,autoGenerateMips=true,filterMode=FilterMode.Trilinear,anisoLevel=4,wrapMode=TextureWrapMode.Clamp};
        // One file per build, map size and world (the flags that change what's built); none in the editor or with --no-map-cache.
        static string CachePath(int pixels)
        {
            if(Application.isEditor||string.IsNullOrEmpty(Application.buildGUID)||RaceTrial.Has("--no-map-cache"))return null;WorldCache.PruneOnce();
            // Keyed on the build, size, quality and what changes the world (WorldCache.World, docs/72): test and review
            // switches used to miss the cache and re-bake the map (+4.8 s) on every run with a new flag.
            uint hash=2166136261;foreach(char ch in Application.buildGUID+"|"+pixels+"|"+QualitySettings.GetQualityLevel()+"|"+WorldCache.World){hash^=ch;hash*=16777619;}
            return Path.Combine(Application.persistentDataPath,"map-cache",$"map-{Application.buildGUID}-{hash:x8}.dxt1");
        }
        static RenderTexture Render(int pixels)
        {
            var go=new GameObject("Map bake camera");var cam=go.AddComponent<Camera>();cam.enabled=false;
            var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.renderShadows=true;
            cam.orthographic=true;cam.orthographicSize=Size/2;cam.nearClipPlane=5;cam.farClipPlane=1600;cam.aspect=1;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.25f,.52f);
            cam.transform.SetPositionAndRotation(new Vector3(X0+Size/2,900,Z0+Size/2),Quaternion.Euler(90,0,0));
            bool fog=RenderSettings.fog;var cookie=RenderSettings.sun!=null?RenderSettings.sun.cookie:null;float lodBias=QualitySettings.lodBias,hour=TimeOfDay.Hour;
            var urp=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;float shadowDistance=urp!=null?urp.shadowDistance:0;
            var rt=Target(pixels);
            try {
                TimeOfDay.Set(14.5f);RenderSettings.fog=false;Shader.SetGlobalVector("_HeightFogParams",Vector4.zero);if(RenderSettings.sun!=null)RenderSettings.sun.cookie=null;
                QualitySettings.lodBias=40;if(urp!=null)urp.shadowDistance=1500;
                cam.targetTexture=rt;cam.Render();cam.targetTexture=null;
                Reef(rt,pixels);
            } finally {
                TimeOfDay.Set(hour);RenderSettings.fog=fog;if(RenderSettings.sun!=null)RenderSettings.sun.cookie=cookie;QualitySettings.lodBias=lodBias;if(urp!=null)urp.shadowDistance=shadowDistance;
                UnityEngine.Object.Destroy(go);
            }
            return rt;
        }
        // The sea on the map: a land mask straight from the world (one downward ray per ~5 m cell: land is
        // anything solid above sea level, sea is seabed or nothing), then colour by distance from the coast -
        // white surf, reef turquoise over the shallows with patchy reef, through blue to deep ocean. Replaces
        // the flat navy clear colour and the lighter sea-mesh rectangle around Bayview.
        const int ReefCells=512;
        static void Reef(RenderTexture rt,int pixels)
        {
            float cell=Size/ReefCells;int n=ReefCells*ReefCells;var land=new float[n];var dist=new float[n];
            var commands=new NativeArray<RaycastCommand>(n,Allocator.TempJob);var hits=new NativeArray<RaycastHit>(n,Allocator.TempJob);
            try {
                var query=new QueryParameters(~0,false,QueryTriggerInteraction.Ignore,false);
                for(int y=0;y<ReefCells;y++)for(int x=0;x<ReefCells;x++)commands[y*ReefCells+x]=new RaycastCommand(new Vector3(X0+(x+.5f)*cell,420,Z0+(y+.5f)*cell),Vector3.down,query,900);
                RaycastCommand.ScheduleBatch(commands,hits,512).Complete();
                for(int i=0;i<n;i++){bool solid=hits[i].collider!=null&&hits[i].point.y>CoastalLandscape.SeaLevel+.25f;land[i]=solid?1:0;dist[i]=solid?0:1e6f;}
            } finally {commands.Dispose();hits.Dispose();}
            // Distance to land in cells (two-pass chamfer).
            const float D=1,Diag=1.4142f;
            for(int y=0;y<ReefCells;y++)for(int x=0;x<ReefCells;x++){int i=y*ReefCells+x;float d=dist[i];
                if(x>0)d=Mathf.Min(d,dist[i-1]+D);if(y>0){d=Mathf.Min(d,dist[i-ReefCells]+D);if(x>0)d=Mathf.Min(d,dist[i-ReefCells-1]+Diag);if(x<ReefCells-1)d=Mathf.Min(d,dist[i-ReefCells+1]+Diag);}dist[i]=d;}
            for(int y=ReefCells-1;y>=0;y--)for(int x=ReefCells-1;x>=0;x--){int i=y*ReefCells+x;float d=dist[i];
                if(x<ReefCells-1)d=Mathf.Min(d,dist[i+1]+D);if(y<ReefCells-1){d=Mathf.Min(d,dist[i+ReefCells]+D);if(x<ReefCells-1)d=Mathf.Min(d,dist[i+ReefCells+1]+Diag);if(x>0)d=Mathf.Min(d,dist[i+ReefCells-1]+Diag);}dist[i]=d;}
            float Sample(float[] grid,float u,float v)
            {
                float fx=Mathf.Clamp(u*ReefCells-.5f,0,ReefCells-1.001f),fy=Mathf.Clamp(v*ReefCells-.5f,0,ReefCells-1.001f);int x0=(int)fx,y0=(int)fy;float tx=fx-x0,ty=fy-y0;int i=y0*ReefCells+x0;
                return Mathf.Lerp(Mathf.Lerp(grid[i],grid[i+1],tx),Mathf.Lerp(grid[i+ReefCells],grid[i+ReefCells+1],tx),ty);
            }
            var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(pixels,pixels,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,pixels,pixels),0,0);RenderTexture.active=active;
            var px=image.GetPixels32();
            Color foam=new(.86f,.95f,.93f),shallow=new(.40f,.85f,.81f),lagoon=new(.21f,.68f,.76f),reef=new(.30f,.71f,.62f),mid=new(.11f,.44f,.66f),deep=new(.05f,.20f,.44f);
            for(int y=0;y<pixels;y++)for(int x=0;x<pixels;x++) {
                float u=(x+.5f)/pixels,v=(y+.5f)/pixels;float l=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.7f,Sample(land,u,v)));if(l>.999f)continue;
                float m=Sample(dist,u,v)*cell,wx=X0+u*Size,wz=Z0+v*Size;
                Color sea=m<2.5f?foam:m<35?Color.Lerp(shallow,lagoon,Mathf.InverseLerp(2.5f,35,m)):m<160?Color.Lerp(lagoon,mid,Mathf.InverseLerp(35,160,m)):Color.Lerp(mid,deep,Mathf.InverseLerp(160,520,m));
                // Patchy reef over the shallows and a gentle swell in the deep water.
                if(m>4&&m<90){float patch=Mathf.PerlinNoise(wx*.035f,wz*.035f)*.7f+Mathf.PerlinNoise(wx*.11f+40,wz*.11f)*.3f;sea=Color.Lerp(sea,reef,Mathf.SmoothStep(.55f,.75f,patch)*Mathf.InverseLerp(90,40,m)*.8f);}
                sea*=.96f+.08f*Mathf.PerlinNoise(wx*.008f+7,wz*.008f+3);
                int i=y*pixels+x;Color32 c=Color.Lerp(sea,(Color)px[i],l);c.a=255;px[i]=c;
            }
            image.SetPixels32(px);image.Apply(false);Graphics.Blit(image,rt);UnityEngine.Object.Destroy(image);
        }
        // Every named place (the atlas's labels) for the full-screen map.
        public static List<Label> Labels(List<(string name,string role,Vector3 at)> residents,Circuit bayviewCircuit){var d=new Data();Collect(d,residents,bayviewCircuit);return d.labels;}
        // World position to map texture uv (0..1).
        public static Vector2 Uv(Vector3 p)=>new((p.x-X0)/Size,(p.z-Z0)/Size);

        static void Add(Data d,string name,string kind,Vector3 at,string note=null){if(at.x<X0||at.x>X0+Size||at.z<Z0||at.z>Z0+Size)return;d.labels.Add(new Label{name=name,kind=kind,note=note,x=at.x,z=at.z});}

        // Kit pieces worth a name on the map; several of one kind close together share one label.
        static readonly Dictionary<string,string> Landmarks=new(){
            {"lighthouse","Lighthouse"},{"windmill","Windmill"},{"pier","Pier"},{"harbour_steps","Harbour steps"},{"tent","Campsite"},{"barn","Farm"},
            {"church","Church"},{"station","Hillcrest Station"},{"town_hall","Town Hall"},{"hotel","Grand Hotel"},{"mill","Watermill"},{"bandstand","Bandstand"},
            {"grandstand","Grandstand"},{"pit_garages","Team garages"},{"wind_turbine","Wind farm"},{"motors","Hillcrest Motors (old)"},{"headland_gate","Headland Gate"},
            {"rail_gate","Railway tunnel"},{"zip_tower","Castle Zip Tower"},{"zip_station","Zipline landing"},{"castle_gatehouse","Castle gatehouse"},{"castle_donjon","Donjon"},
            {"castle_palace","Palazzo"},{"fountain","Fountain"},{"telescope","Telescope"},{"beach_hut","Beach huts"},{"tunnel_portal","Tunnel portal"},{"town_gate","Town gate"}};

        static void Collect(Data d,List<(string name,string role,Vector3 at)> residents,Circuit bayview)
        {
            // Areas (the names on the area banner as you drive in).
            var court=HillcrestRoute.CastleCourt;var main=HillcrestRoute.Mainline;
            Add(d,"Bayview","area",new Vector3(-30,0,-60),"Home island · harbour town");
            Add(d,"Bayview Halt","area",CoastalLandscape.HaltResident);
            Add(d,"The Causeway","area",HillcrestRoute.Causeway[HillcrestRoute.Causeway.Length/2]);
            Add(d,"Headland Tunnel","area",main.At((HillcrestRoute.PortalNorthRun+HillcrestRoute.PortalSouthRun)*.5f));
            Add(d,"Hillcrest","area",HillcrestRoute.Square,"Market town on the hill");
            Add(d,"Hillcrest Castle","area",court);
            Add(d,"Silver Falls","area",HillcrestRoute.Lookout);
            Add(d,"Hillcrest Raceway","area",HillcrestRoute.Paddock+new Vector3(-60,0,-120),"Home of the Valley Cup");
            Add(d,"Valley Viewpoint","area",HillcrestRoute.Vista);
            Add(d,"Golden Acre Farm","area",new Vector3(HillcrestRoute.FarmCentre.x,0,HillcrestRoute.FarmCentre.y));
            Add(d,"Hillcrest Valley","area",new Vector3(250,0,-1650),"The mainland");
            // Places.
            Add(d,"Market Square","place",HillcrestRoute.Square+new Vector3(0,0,-18));
            Add(d,"Raceway paddock","place",HillcrestRoute.Paddock);
            Add(d,"Station yard","place",HillcrestRoute.StationYard);
            Add(d,"The Lake","place",new Vector3(HillcrestRoute.Lake.x,0,HillcrestRoute.Lake.y));
            Add(d,"Balloon field","place",new Vector3(HillcrestRoute.BalloonField.x,0,HillcrestRoute.BalloonField.y));
            Add(d,"Falls head","place",HillcrestRoute.FallsHead);
            foreach(var site in PitStop.Sites)Add(d,"Pit Stop · "+char.ToUpperInvariant(site.Key[0])+site.Key[1..],"service",site.Value);
            if(bayview!=null){Add(d,"Bayview Coast circuit (start)","road",bayview.At(0));}
            foreach(var road in HillcrestRoute.All())Add(d,road.name,"road",road.At(road.Length*.5f));
            Add(d,"Silver river","road",HillcrestRoute.River.At(HillcrestRoute.River.Length*.35f));
            Add(d,"Hillcrest railway","road",HillcrestRoute.Rail.At(HillcrestRoute.Rail.Length*.5f));
            // Landmarks from the placed kit (clusters within 140 m share a label).
            var found=new Dictionary<string,List<Vector3>>();
            foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
                if(t.parent==null||!Landmarks.ContainsKey(t.name))continue;
                if(!found.TryGetValue(t.name,out var list))found[t.name]=list=new List<Vector3>();list.Add(t.position);
            }
            foreach(var (id,points) in found) {
                var clusters=new List<List<Vector3>>();
                float reach=id=="wind_turbine"?700:140;
                foreach(var p in points){var c=clusters.FirstOrDefault(k=>Vector3.Distance(Centre(k),p)<reach);if(c==null)clusters.Add(new List<Vector3>{p});else c.Add(p);}
                foreach(var c in clusters) {
                    // The island's pieces share kit with the mainland: name them for where they are.
                    var at=Centre(c);bool island=at.z>-300;string name=Landmarks[id];
                    if(island&&id=="station")name="Bayview Halt station";else if(island&&id=="barn")name="Bayview farm";
                    Add(d,name,"landmark",at,c.Count>1?c.Count+(id=="wind_turbine"?" turbines":" here"):null);
                }
            }
            // Residents you can talk to (Pip keeps the lighthouse post).
            foreach(var (name,role,at) in residents){Add(d,name,"person",at,role);if(role=="Lighthouse")Add(d,"Lighthouse","landmark",at+new Vector3(-6,0,8));}
        }
        static Vector3 Centre(List<Vector3> p){var s=Vector3.zero;foreach(var v in p)s+=v;return s/p.Count;}
    }
}
