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
        readonly Vector3 origin=new(0,-10000,0);
        GameObject selected;
        GameObject carModel,platform;float carAngle=35;
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

        public PartPreviewStage(string body,Color paint,IEnumerable<EquipmentCatalog.Part> catalogue,Color? backdrop=null)
        {
            root=new GameObject("Garage preview stage");root.transform.position=origin;
            camera=new GameObject("Single catalogue camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);
            camera.enabled=false;camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=backdrop??Background;
            camera.orthographic=true;camera.nearClipPlane=.02f;camera.farClipPlane=40;camera.allowHDR=false;camera.allowMSAA=true;
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;data.volumeLayerMask=0;
            AddLight(new Vector3(-3,4,3),new Color(.82f,.92f,1),140);
            AddLight(new Vector3(3,2,-2),new Color(1,.82f,.60f),95);
            Selected=Texture(720,406);Car=Texture(560,310);Orbit=Texture(640,390);
            foreach(var part in catalogue) {
                var prefab=Resources.Load<GameObject>("Parts/"+part.id);
                var model=prefab==null?PartDisplayModels.Create(part,root.transform,materials,displayMeshes):UnityEngine.Object.Instantiate(prefab,root.transform);model.name=part.id;
                if(prefab!=null)foreach(var renderer in model.GetComponentsInChildren<Renderer>()) {
                    var converted=new Material[renderer.sharedMaterials.Length];
                    for(int i=0;i<converted.Length;i++) {
                        string name=renderer.sharedMaterials[i].name;
                        Color color=name.Contains("Rubber")?new Color(.04f,.055f,.06f):name.Contains("Alloy")?new Color(.65f,.70f,.72f):
                            name.Contains("Brass")?new Color(.58f,.40f,.16f):name.Contains("Cover")?
                            (part.id=="engine_stock"?new Color(.19f,.27f,.29f):part.id=="engine_workshop"?new Color(.09f,.46f,.39f):new Color(.77f,.22f,.12f)):
                            new Color(.30f,.34f,.35f);
                        converted[i]=TrialWorld.Material(name,color,name.Contains("Rubber")?0:.45f,.38f);materials.Add(converted[i]);
                    }
                    renderer.sharedMaterials=converted;
                }
                Prepare(model);parts.Add(part.id,model);
                orbitIds.Add(part.id);orbitScales.Add(model.transform.localScale);
                var image=Texture(360,302);cards.Add(part.id,image);Render(model,image,145);model.SetActive(false);
            }
            var car=TrialWorld.Car(body,paint);car.transform.SetParent(root.transform,false);
            carModel=car;
            foreach(var renderer in car.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                if(!materials.Contains(material))materials.Add(material);
            var lod=car.GetComponent<LODGroup>();if(lod!=null)lod.ForceLOD(0);
            Prepare(car);
            platform=GameObject.CreatePrimitive(PrimitiveType.Cylinder);platform.name="Display turntable";platform.transform.SetParent(root.transform,false);platform.layer=Layer;
            platform.transform.localScale=new Vector3(3.75f,.06f,3.75f);platform.transform.localPosition=new Vector3(0,-.10f,0);UnityEngine.Object.Destroy(platform.GetComponent<Collider>());
            var deck=TrialWorld.Material("Showroom turntable",new Color(.20f,.40f,.40f),.35f,.4f);materials.Add(deck);platform.GetComponent<Renderer>().sharedMaterial=deck;
            Render(car,Car,35);car.SetActive(false);platform.SetActive(false);
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
            var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);radii[model]=bounds.extents.magnitude;
        }
        void Render(GameObject model,RenderTexture texture,float yaw)
        {
            if(platform!=null)platform.SetActive(model==carModel);
            model.SetActive(true);model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers)if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
            camera.transform.position=bounds.center+new Vector3(0,1.2f,3).normalized*10;camera.transform.LookAt(bounds.center);
            // Fit every transformed corner, including tall intake pipes, in the
            // camera basis. A stable scale across rotation avoids visual pumping.
            camera.orthographicSize=radii[model]*.82f;camera.aspect=(float)texture.width/texture.height;
            camera.targetTexture=texture;camera.Render();camera.targetTexture=null;RenderCount++;model.SetActive(false);if(platform!=null)platform.SetActive(false);
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
        public void RenderOrbit(float degrees)
        {
            if(orbitIds.Count==0)return;if(platform!=null)platform.SetActive(false);carModel.SetActive(false);
            camera.transform.position=origin+new Vector3(0,6,8);camera.transform.LookAt(origin);camera.orthographicSize=2.75f;camera.aspect=(float)Orbit.width/Orbit.height;
            for(int i=0;i<orbitIds.Count;i++){
                var model=parts[orbitIds[i]];float a=(degrees+i*360f/orbitIds.Count)*Mathf.Deg2Rad;
                model.SetActive(true);model.transform.localScale=orbitScales[i]*(.55f/radii[model]);model.transform.localPosition=new Vector3(Mathf.Cos(a)*2.55f,0,Mathf.Sin(a)*2.05f);model.transform.localRotation=Quaternion.Euler(0,degrees+35,0);
                var p=camera.WorldToViewportPoint(model.transform.position);orbitPositions[i]=new Vector2(p.x,p.y);
            }
            camera.targetTexture=Orbit;camera.Render();camera.targetTexture=null;RenderCount++;
            for(int i=0;i<orbitIds.Count;i++){var model=parts[orbitIds[i]];model.SetActive(false);model.transform.localPosition=Vector3.zero;model.transform.localScale=orbitScales[i];}
        }
        public void Dispose()
        {
            if(root!=null)root.SetActive(false);
            if(camera!=null)camera.targetTexture=null;foreach(var texture in textures){if(texture==null)continue;texture.Release();UnityEngine.Object.Destroy(texture);}
            foreach(var material in materials)UnityEngine.Object.Destroy(material);foreach(var mesh in displayMeshes)UnityEngine.Object.Destroy(mesh);UnityEngine.Object.Destroy(root);
        }
    }
}
