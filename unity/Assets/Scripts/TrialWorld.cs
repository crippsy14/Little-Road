using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    public static class TrialWorld
    {
        public static Material Material(string name,Color color,float metal=0,float smooth=.2f)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
            m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;return m;
        }
        // Shared by runtime assembly and the build's retained shader reference.
        // URP alpha blending with preserved specular keeps sky reflections bright
        // while allowing the cabin to show through the tinted pane.
        public static void ConfigureMasterGlass(Material material)
        {
            material.color=new Color(.055f,.075f,.09f,.72f);
            material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.82f);
            material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);
            material.SetFloat("_BlendModePreserveSpecular",1);material.SetFloat("_Cull",0);
            material.SetFloat("_ZWrite",0);material.SetFloat("_SrcBlend",(float)BlendMode.One);
            material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType","Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetShaderPassEnabled("ShadowCaster",false);material.SetShaderPassEnabled("DepthOnly",false);
            material.renderQueue=(int)RenderQueue.Transparent;
        }
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,bool collision=false)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.position=position;o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.Destroy(o.GetComponent<Collider>());else o.tag="Respawn";return o;
        }
        static void Ribbon(Circuit track,string name,float center,float width,float height,Material mat,bool collision=false)
        {
            var o=new GameObject(name);o.AddComponent<MeshFilter>().sharedMesh=track.Ribbon(center,width,height);o.AddComponent<MeshRenderer>().sharedMaterial=mat;
            if(collision)o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;
        }
        public static void Create(Circuit track,bool racing=true)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.64f,.79f,.91f);
            RenderSettings.ambientEquatorColor=new Color(.56f,.60f,.55f);RenderSettings.ambientGroundColor=new Color(.25f,.30f,.25f);
            // The scene is assembled at runtime and has no baked probes. Supply
            // diffuse sky fill explicitly so workshop shadows retain colour.
            var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.25f,.29f,.31f));RenderSettings.ambientProbe=ambient;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.64f,.81f,.86f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0014f;
            Vector3 sunDirection=CoastalSky.Apply();
            var sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.2f;sun.color=new Color(1,.91f,.77f);
            sun.transform.rotation=Quaternion.LookRotation(-sunDirection);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;
            var fill=new GameObject("Cool open-sky fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.38f;fill.color=new Color(.70f,.84f,1);fill.transform.rotation=Quaternion.Euler(28,145,0);fill.shadows=LightShadows.None;
            var volume=new GameObject("Coastal colour and bloom").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tone=volume.profile.Add<Tonemapping>();tone.mode.Override(TonemappingMode.ACES);
            var bloom=volume.profile.Add<Bloom>();bloom.intensity.Override(.15f);bloom.threshold.Override(1.2f);
            var color=volume.profile.Add<ColorAdjustments>();color.postExposure.Override(.10f);color.saturation.Override(0);color.contrast.Override(3);
            var asphalt=Material("Warm aggregate asphalt",new Color(.23f,.27f,.29f));
            var texture=new Texture2D(128,128);var pixels=new Color[128*128];var rng=new System.Random(83);
            for(int i=0;i<pixels.Length;i++){float n=.84f+(float)rng.NextDouble()*.25f;pixels[i]=new Color(n,n,n);}
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Repeat;asphalt.mainTexture=texture;asphalt.mainTextureScale=new Vector2(8,4);
            var sand=CoastalSurfaces.Runoff();
            var white=Material("Ivory paint",new Color(.91f,.91f,.80f));var coral=Material("Coral kerb",new Color(.79f,.27f,.18f));
            new CoastalLandscape(track).Build();
            // Both road and shoulder have actual matching mesh surfaces across
            // the inlet. The bridge is not a road painted on an invisible island.
            Ribbon(track,"Generous limestone runoff",0,24,.011f,sand);
            Ribbon(track,"16 metre race surface",0,Circuit.Width,.025f,asphalt);
            new GameObject("Continuous road and shoulder support",typeof(RoadSupport)).AddComponent<MeshCollider>().sharedMesh=track.DrivableSupport();
            foreach(int side in new[]{-1,1}) {Ribbon(track,"Edge paint",side*7.7f,.16f,.037f,white);
                if(racing)for(float d=0;d<track.length;d+=4) {var kerb=Box("Alternating kerb",track.At(d)+track.Right(d)*side*8.25f+Vector3.up*.055f,new Vector3(.7f,.10f,3.8f),((int)(d/4)%2==0)?white:coral);
                    kerb.transform.rotation=Quaternion.LookRotation(track.Forward(d));}}
            for(float d=0;d<track.length;d+=12) {
                if(track.Curvature(d)>.012f)continue;var mark=Box("Grid guide",track.At(d)+Vector3.up*.04f,new Vector3(.12f,.015f,3),white);mark.transform.rotation=Quaternion.LookRotation(track.Forward(d));}
            if(racing)for(int i=0;i<16;i++)for(int j=0;j<2;j++) {
                var checker=Box("Start finish",track.At(j*.55f)+track.Right(0)*(-7.5f+i)+Vector3.up*.046f,new Vector3(1,.02f,.55f),(i+j)%2==0?white:asphalt);checker.transform.rotation=Quaternion.LookRotation(track.Forward(0));}
            var shutter=Material("Teal joinery",new Color(.12f,.38f,.37f));
            VillageKit.Create();
            new GameObject("Coastal reflected light").AddComponent<CoastalReflections>();
            if(racing){
                for(int side=-1;side<=1;side+=2)Box("Start gantry post",track.At(0)+track.Right(0)*side*9+Vector3.up*3.5f,new Vector3(.4f,7,.4f),shutter,true);
                var beam=Box("Start gantry",track.At(0)+Vector3.up*6.7f,new Vector3(18,.8f,.4f),shutter);beam.transform.rotation=Quaternion.LookRotation(track.Forward(0));
            }
        }
        public static GameObject Car(string asset,Color paint)
        {
            var root=new GameObject(asset);
            var materials=new System.Collections.Generic.Dictionary<string,Material>();
            var visual=CarVisual(root,asset,paint,materials);
            // Bodies exported with reduced meshes (compact master, family field) get LOD groups.
            if(Resources.Load<GameObject>("Cars/"+asset+"_lod1")!=null) {
                var lod1=CarVisual(root,asset+"_lod1",paint,materials);var lod2=CarVisual(root,asset+"_lod2",paint,materials);
                var group=root.AddComponent<LODGroup>();group.SetLODs(new[]{
                    new LOD(.18f,visual.GetComponentsInChildren<Renderer>()),
                    new LOD(.045f,lod1.GetComponentsInChildren<Renderer>()),
                    new LOD(.008f,lod2.GetComponentsInChildren<Renderer>())});group.RecalculateBounds();
            }
            if(CarBodies.UsesStandardEnvelope(asset))root.AddComponent<StandardEnvelope>();
            root.AddComponent<VehicleLights>().Initialize();return root;
        }
        static GameObject CarVisual(GameObject root,string asset,Color paint,System.Collections.Generic.Dictionary<string,Material> materials)
        {
            var source=Resources.Load<GameObject>("Cars/"+asset);
            if(source==null)throw new System.InvalidOperationException("Missing car resource: "+asset);
            var visual=Object.Instantiate(source,root.transform);visual.name=asset;
            // FBX imports the Blender +Y nose along Unity -Z. The controller
            // uses +Z; keep this correction on the visual only.
            visual.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>()) {
                var originals=renderer.sharedMaterials;var converted=new Material[originals.Length];
                for(int i=0;i<originals.Length;i++) {
                    string name=originals[i]!=null?originals[i].name:"Unknown";
                    if(materials.TryGetValue(name,out var existing)){converted[i]=existing;continue;}
                    Color c=name.Contains("BodyPaint")?paint:name.Contains("Glass")?new Color(.035f,.09f,.12f):
                        name.Contains("Rubber")?new Color(.035f,.04f,.045f):name.Contains("Alloy")?new Color(.63f,.66f,.65f):
                        name.Contains("Headlamp")?new Color(1,.94f,.74f):name.Contains("TailLight")?new Color(.42f,.015f,.008f):
                        name.Contains("ReverseLight")?new Color(.65f,.70f,.72f):name.Contains("RearReflector")?new Color(.42f,.012f,.008f):
                        name.Contains("BumperInsert")?new Color(.22f,.25f,.26f):
                        name.Contains("Amber")?new Color(1,.38f,.02f):name.Contains("Plate")?new Color(.9f,.89f,.75f):
                        name.Contains("PanelSeam")?new Color(.024f,.054f,.051f):name.Contains("BrakeMetal")?new Color(.08f,.095f,.10f):
                        name.Contains("Cloth")?new Color(.065f,.088f,.094f):name.Contains("Cream")?new Color(.93f,.90f,.80f):name.Contains("Livery")?new Color(.85f,.10f,.08f):
                        name.Contains("Wood")?new Color(.45f,.27f,.13f):new Color(.055f,.073f,.08f);
                    // Plastic seals and trim should not inherit glossy metallic
                    // paint. Preserve readable material separation at race distance.
                    bool paintSurface=name.Contains("BodyPaint"),alloySurface=name.Contains("Alloy");
                    bool chrome=name.Contains("Chrome");
                    float metal=chrome?.9f:alloySurface?.7f:name.Contains("BrakeMetal")?.55f:paintSurface?.05f:0;
                    float smooth=chrome?.8f:alloySurface?.42f:paintSurface?.56f:name.Contains("Rubber")?.15f:
                        name.Contains("Headlamp")||name.Contains("TailLight")||name.Contains("ReverseLight")||name.Contains("Amber")?.6f:.25f;
                    converted[i]=Material(name,c,metal,smooth);
                    if(name.Contains("TailLight")||name.Contains("ReverseLight")) {
                        converted[i].EnableKeyword("_EMISSION");converted[i].SetColor("_EmissionColor",Color.black);
                    }
                    // Prototype glazing consists of single-sided polygons. glTF
                    // rendered both sides; preserve that intent in the FBX port.
                    if(name.Contains("Glass"))converted[i].SetFloat("_Cull",0);
                    if(name.Contains("Cloth")){converted[i].SetFloat("_Smoothness",.1f);converted[i].SetFloat("_Metallic",0);}
                    if(name.Contains("MasterGlass"))ConfigureMasterGlass(converted[i]);
                    materials[name]=converted[i];
                }
                renderer.sharedMaterials=converted;
            }
            return visual;
        }
    }
}
