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
        readonly List<RenderTexture> textures=new();
        readonly Dictionary<string,GameObject> parts=new();
        readonly Dictionary<string,RenderTexture> cards=new();
        readonly Dictionary<GameObject,float> radii=new();
        readonly Vector3 origin=new(0,-10000,0);
        GameObject selected;
        float angle=145;
        public float Angle=>angle;
        public int RenderCount {get;private set;}
        public RenderTexture Selected {get;}
        public RenderTexture Car {get;}
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
            Selected=Texture(720,406);Car=Texture(560,310);
            foreach(var part in catalogue) {
                var prefab=Resources.Load<GameObject>("Parts/"+part.id);
                if(prefab==null)throw new InvalidOperationException("Missing garage model: "+part.id);
                var model=UnityEngine.Object.Instantiate(prefab,root.transform);model.name=part.id;
                foreach(var renderer in model.GetComponentsInChildren<Renderer>()) {
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
                var image=Texture(360,302);cards.Add(part.id,image);Render(model,image,145);model.SetActive(false);
            }
            var car=TrialWorld.Car(body,paint);car.transform.SetParent(root.transform,false);
            foreach(var renderer in car.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)
                if(!materials.Contains(material))materials.Add(material);
            var lod=car.GetComponent<LODGroup>();if(lod!=null)lod.ForceLOD(0);
            Prepare(car);Render(car,Car,35);car.SetActive(false);
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
            model.SetActive(true);model.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var renderers=model.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers)if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
            camera.transform.position=bounds.center+new Vector3(0,1.2f,3).normalized*10;camera.transform.LookAt(bounds.center);
            // Fit every transformed corner, including tall intake pipes, in the
            // camera basis. A stable scale across rotation avoids visual pumping.
            camera.orthographicSize=radii[model]*.82f;camera.aspect=(float)texture.width/texture.height;
            camera.targetTexture=texture;camera.Render();camera.targetTexture=null;RenderCount++;
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
        public void Dispose()
        {
            if(root!=null)root.SetActive(false);
            if(camera!=null)camera.targetTexture=null;foreach(var texture in textures){if(texture==null)continue;texture.Release();UnityEngine.Object.Destroy(texture);}
            foreach(var material in materials)UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(root);
        }
    }
}
