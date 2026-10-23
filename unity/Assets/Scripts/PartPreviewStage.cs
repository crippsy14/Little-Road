using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // One isolated camera, cached catalogue images, and one view rendered only
    // when rotated. Presentation contains no equipment or physics parameters.
    public sealed class PartPreviewStage : IDisposable
    {
        const int Layer=31;
        readonly GameObject root;
        readonly Camera camera;
        readonly List<GameObject> models=new();
        readonly List<Material> materials=new();
        readonly List<Mesh> displayMeshes=new();
        readonly List<RenderTexture> textures=new();
        readonly Dictionary<string,GameObject> parts=new();
        readonly Dictionary<string,RenderTexture> cards=new();
        readonly Dictionary<GameObject,float> radii=new();
        // Each stage gets its own hidden spot (60 m apart, cameras see 40 m): a stage still alive from another
        // screen, e.g. the retry preview's showroom, once hid a podium car rendered at the shared spot.
        static int nextSpot;
        readonly Vector3 origin=new((nextSpot++%16)*60f,-10000,0);
        GameObject selected;
        GameObject carModel,platform,showroom;float carAngle=35;
        readonly Color partBackground;readonly bool useShowroom;
        readonly Light[] worldLights;readonly int[] worldLightMasks;
        readonly List<string> orbitIds=new();readonly List<Vector3> orbitScales=new();readonly Vector2[] orbitPositions=new Vector2[7];
        float angle=145;
        public float Angle=>angle;
        public int RenderCount {get;private set;}
        public RenderTexture Selected {get;}
        public RenderTexture Car {get;}
        public RenderTexture Orbit {get;}
        public float CarAngle=>carAngle;
        public Vector2 OrbitPosition(int index)=>orbitPositions[index];
        static readonly Color Background=new(.13f,.23f,.25f,1);

        public PartPreviewStage(string body,Color paint,IEnumerable<EquipmentCatalog.Part> catalogue,Color? backdrop=null,bool showroom=false,float lightScale=1)
        {
            useShowroom=showroom;partBackground=backdrop??Background;
            worldLights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);worldLightMasks=new int[worldLights.Length];
            root=new GameObject("Garage preview stage");root.transform.position=origin;
            camera=new GameObject("Single catalogue camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
            camera.enabled=false;camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=backdrop??Background;
            camera.orthographic=true;camera.nearClipPlane=.02f;camera.farClipPlane=40;camera.allowHDR=false;camera.allowMSAA=true;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.volumeLayerMask=0;
            AddLight(new Vector3(-3,4,3),new Color(.82f,.92f,1),140*lightScale);
            AddLight(new Vector3(3,2,-2),new Color(1,.82f,.60f),95*lightScale);
            // (a car-only stage, the body shop's and the garage's, needs no part views: two 4x MSAA textures fewer)
            var list=new List<EquipmentCatalog.Part>(catalogue);
            Selected=list.Count>0?Texture(720,406):Texture(8,8);Car=Texture(900,500);Orbit=list.Count>0?Texture(744,392):Texture(8,8);
            foreach(var part in list) {
                var prefab=Resources.Load<GameObject>("Parts/"+part.id);if(prefab==null&&part.slot=="engine")prefab=Resources.Load<GameObject>("Parts/engine_club");
                var model=prefab==null?PartDisplayModels.Create(part,root.transform,materials,displayMeshes):UnityEngine.Object.Instantiate(prefab,root.transform);model.name=part.id;
                if(prefab!=null)foreach(var renderer in model.GetComponentsInChildren<Renderer>()) {
                    var converted=new Material[renderer.sharedMaterials.Length];
                    for(int i=0;i<converted.Length;i++) {
                        string name=renderer.sharedMaterials[i].name;
                        // Finish from the Blender material name (source-art/build_*_previews.py).
                        bool chrome=name.Contains("Chrome"),matte=name.Contains("Rubber")||name.Contains("White")||name.Contains("Carbon");
                        Color color=name.Contains("Rubber")?new Color(.04f,.055f,.06f):chrome?new Color(.86f,.87f,.88f):name.Contains("Alloy")?new Color(.65f,.70f,.72f):
                            name.Contains("Brass")?new Color(.58f,.40f,.16f):name.Contains("Carbon")?new Color(.07f,.075f,.08f):name.Contains("White")?new Color(.92f,.92f,.9f):
                            name.Contains("Cover")?Accent(part):new Color(.30f,.34f,.35f);
                        converted[i]=TrialWorld.Material(name,color,name.Contains("Rubber")||name.Contains("White")?0:chrome?.9f:matte?.2f:.45f,chrome?.7f:.38f);materials.Add(converted[i]);
                    }
                    renderer.sharedMaterials=converted;
                }
                Prepare(model);parts.Add(part.id,model);
                orbitIds.Add(part.id);orbitScales.Add(model.transform.localScale);
                var image=Texture(360,302);cards.Add(part.id,image);Render(model,image,145);model.SetActive(false);
            }
            var car=carModel=MakeCar(body,paint);
            platform=GameObject.CreatePrimitive(PrimitiveType.Cylinder);platform.name="Display turntable";platform.transform.SetParent(root.transform,false);platform.layer=Layer;
            platform.transform.localScale=new Vector3(3.75f,.06f,3.75f);platform.transform.localPosition=new Vector3(0,-.10f,0);UnityEngine.Object.Destroy(platform.GetComponent<Collider>());
            var deck=TrialWorld.Material("Showroom turntable",useShowroom?new Color(.045f,.065f,.074f):new Color(.20f,.40f,.40f),.35f,.4f);materials.Add(deck);platform.GetComponent<Renderer>().sharedMaterial=deck;
            if(useShowroom)this.showroom=CarShowroom.Create(root.transform,materials,displayMeshes);
            Render(car,Car,35);car.SetActive(false);platform.SetActive(false);
        }
        GameObject MakeCar(string body,Color paint)
        {
            var car=TrialWorld.Car(body,paint);car.transform.SetParent(root.transform,false);
            foreach(var renderer in car.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                if(!materials.Contains(material))materials.Add(material);
            // Full detail, chosen directly: the LOD group sometimes left the car hidden in a one-off render
            // (a podium car missing on the result screen), and cross-fades need frames the stage never runs.
            var lod=car.GetComponent<LODGroup>();
            if(lod!=null){var levels=lod.GetLODs();lod.enabled=false;
                for(int l=0;l<levels.Length;l++)foreach(var r in levels[l].renderers)if(r!=null)r.enabled=l==0;}
            Prepare(car);return car;
        }
        // A car leaves the stage with its own materials and its livery texture.
        void DropCar(GameObject car)
        {
            var own=new HashSet<Material>();foreach(var r in car.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m!=null)own.Add(m);
            foreach(var m in own){var tex=m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):null;if(tex!=null&&tex.name=="ForgeLivery")liveries.Add(tex);materials.Remove(m);UnityEngine.Object.Destroy(m);}
            foreach(var t in liveries)UnityEngine.Object.Destroy(t);liveries.Clear();
            models.Remove(car);radii.Remove(car);renderersOf.Remove(car);car.SetActive(false);UnityEngine.Object.Destroy(car);
        }
        readonly HashSet<Texture> liveries=new();
        // The body shop's and the garage's browsing: another body on the same turntable (the stage and its textures stay;
        // a new stage per car made three 4x MSAA textures every time the focus moved).
        public void SetCar(string body,Color paint,float yaw=35)
        {
            DropCar(carModel);carModel=MakeCar(body,paint);carModel.SetActive(false);carAngle=yaw;Render(carModel,Car,carAngle);
        }
        // A still of a body on the turntable, for a card: rendered once into its own small texture (no MSAA or depth of its
        // own: it resolves from one shared 4x target), freed with the stage.
        RenderTexture still;
        public RenderTexture Still(string body,Color paint,int width,int height,float yaw=35)
        {
            if(still==null||still.width!=width||still.height!=height){if(still!=null){textures.Remove(still);still.Release();UnityEngine.Object.Destroy(still);}still=Texture(width,height);}
            var car=MakeCar(body,paint);Render(car,still,yaw,.66f);   // (framed closer: a card is small)
            var image=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32){name="Garage card"};image.Create();textures.Add(image);Graphics.Blit(still,image);
            DropCar(car);return image;
        }
        // Tier colour for a part's painted pieces: stock grey-teal, then teal, then red within a Bayview
        // slot by price; Hillcrest's valley parts in amber.
        public static Color Accent(EquipmentCatalog.Part part)
        {
            if(part.shop=="hillcrest")return new Color(.86f,.62f,.12f);
            int rank=0;foreach(var p in EquipmentCatalog.All.parts)if(p.slot==part.slot&&p.shop!="hillcrest"&&p.price<part.price)rank++;
            return rank==0?new Color(.19f,.27f,.29f):rank==1?new Color(.09f,.46f,.39f):new Color(.77f,.22f,.12f);
        }
        void AddLight(Vector3 position,Color color,float intensity)
        {
            var light=new GameObject("Preview softbox").AddComponent<Light>();light.transform.SetParent(root.transform,false);
            light.transform.localPosition=position;light.type=LightType.Point;light.range=14;light.intensity=intensity;
            light.color=color;light.cullingMask=1<<Layer;light.shadows=LightShadows.None;
        }
        RenderTexture Texture(int width,int height)
        {
            var texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=4,name="Garage preview"};texture.Create();textures.Add(texture);return texture;
        }
        void Prepare(GameObject model)
        {
            models.Add(model);model.transform.localPosition=Vector3.zero;
            foreach(var child in model.GetComponentsInChildren<Transform>(true))child.gameObject.layer=Layer;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true)){collider.enabled=false;UnityEngine.Object.Destroy(collider);}
            var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;renderersOf[model]=renderers;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);radii[model]=bounds.extents.magnitude;
        }
        readonly Dictionary<GameObject,Renderer[]> renderersOf=new();
        void Render(GameObject model,RenderTexture texture,float yaw,float fit=.82f)
        {
            bool reveal=useShowroom&&model==carModel;
            if(showroom!=null)showroom.SetActive(reveal);
            camera.backgroundColor=reveal?new Color(.015f,.023f,.03f,1):partBackground;
            if(platform!=null)platform.SetActive(model==carModel);
            model.SetActive(true);model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            // (the renderers found once, in Prepare: the turntable renders every frame now)
            var renderers=renderersOf.TryGetValue(model,out var known)?known:model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers)if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
            camera.transform.position=bounds.center+new Vector3(0,1.2f,3).normalized*10;camera.transform.LookAt(bounds.center);
            // Fit every transformed corner, including tall intake pipes, in the
            // camera basis. A stable scale across rotation avoids visual pumping.
            camera.orthographicSize=radii[model]*fit;camera.aspect=(float)texture.width/texture.height;
            RenderCamera(texture);model.SetActive(false);if(platform!=null)platform.SetActive(false);if(showroom!=null)showroom.SetActive(false);
        }
        void RenderCamera(RenderTexture texture)
        {
            // Daylight from the race must not wash out the studio. Exclude only
            // the preview layer during this synchronous render and restore it.
            if(useShowroom)for(int i=0;i<worldLights.Length;i++)if(worldLights[i]!=null){worldLightMasks[i]=worldLights[i].cullingMask;worldLights[i].cullingMask&=~(1<<Layer);}
            try{camera.targetTexture=texture;camera.Render();RenderCount++;}
            finally{camera.targetTexture=null;if(useShowroom)for(int i=0;i<worldLights.Length;i++)if(worldLights[i]!=null)worldLights[i].cullingMask=worldLightMasks[i];}
        }
        public Texture Card(string id)=>cards[id];
        public void Select(string id)
        {
            if(selected!=null)selected.SetActive(false);selected=parts[id];angle=145;Render(selected,Selected,angle);
        }
        public void Rotate(float degrees)
        {
            if(selected==null||Mathf.Abs(degrees)<.01f)return;angle=Mathf.Repeat(angle+degrees,360);Render(selected,Selected,angle);
        }
        public void RotateCar(float degrees){carAngle=Mathf.Repeat(carAngle+degrees,360);Render(carModel,Car,carAngle);}
        // Paint shop preview: recolour the showroom car's body panels and re-render it.
        public void Repaint(Color paint)
        {
            ForgeLivery.Repaint(carModel,carModel.name,paint);
            Render(carModel,Car,carAngle);
        }
        public void RenderOrbit(float degrees)
        {
            if(orbitIds.Count==0)return;if(platform!=null)platform.SetActive(false);carModel.SetActive(false);
            if(showroom!=null)showroom.SetActive(false);camera.backgroundColor=partBackground;
            camera.transform.position=origin+new Vector3(0,6,8);camera.transform.LookAt(origin);camera.orthographicSize=2.75f;camera.aspect=(float)Orbit.width/Orbit.height;
            for(int i=0;i<orbitIds.Count;i++){
                var model=parts[orbitIds[i]];float a=(degrees+i*360f/orbitIds.Count)*Mathf.Deg2Rad;
                model.SetActive(true);model.transform.localScale=orbitScales[i]*(.55f/radii[model]);model.transform.localPosition=new Vector3(Mathf.Cos(a)*2.55f,0,Mathf.Sin(a)*2.05f);model.transform.localRotation=Quaternion.Euler(0,degrees+35,0);
                var p=camera.WorldToViewportPoint(model.transform.position);orbitPositions[i]=new Vector2(p.x,p.y);
            }
            RenderCamera(Orbit);
            for(int i=0;i<orbitIds.Count;i++){var model=parts[orbitIds[i]];model.SetActive(false);model.transform.localPosition=Vector3.zero;model.transform.localScale=orbitScales[i];}
        }
        public void Dispose()
        {
            if(root!=null)root.SetActive(false);
            if(camera!=null)camera.targetTexture=null;foreach(var texture in textures){if(texture==null)continue;texture.Release();UnityEngine.Object.Destroy(texture);}
            foreach(var material in materials)if(material!=null&&material.HasProperty("_BaseMap")&&material.GetTexture("_BaseMap") is Texture t&&t.name=="ForgeLivery")liveries.Add(t);
            foreach(var t in liveries)UnityEngine.Object.Destroy(t);
            foreach(var material in materials)UnityEngine.Object.Destroy(material);foreach(var mesh in displayMeshes)UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(root);
        }
    }
}
