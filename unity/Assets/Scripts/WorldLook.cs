using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // The game's look in one place: "toy shapes, real light". Lighting, colour grading, how
    // stylised the ground is, drifting cloud shadows, and the colour limits every material passes
    // through. Scene builders pick hues; this class decides how loud they may be. Change the look
    // here, not in the builders, and keep docs/art-direction.md in step.
    public static class WorldLook
    {
        // ---- Surface colours: every TrialWorld.Material colour (kit, props, roads) is graded.
        // Saturation is scaled and capped, brightness capped below paper white and lifted above
        // black, so nothing glares and shadows keep detail. Car body paint is exempt (player choice).
        // Bright values roll off above ValueKnee (real paint, plaster and stone sit well below paper
        // white), which keeps light surfaces from reading as washed-out pastel.
        public const float SaturationScale=.90f,SaturationCap=.72f,ValueCap=.82f,ValueFloor=.05f,ValueKnee=.55f,ValueRolloff=.6f;
        public static Color Surface(Color c)
        {
            Color.RGBToHSV(c,out float h,out float s,out float v);
            // Over-range colours (HDR tints, >1) keep their energy: only grade the displayable range.
            if(c.maxColorComponent>1.001f)return c;
            s=Mathf.Min(s*SaturationScale,SaturationCap);if(v>ValueKnee)v=ValueKnee+(v-ValueKnee)*ValueRolloff;v=Mathf.Clamp(v,ValueFloor,ValueCap);
            var graded=Color.HSVToRGB(h,s,v);graded.a=c.a;return graded;
        }
        public static bool WithinLimits(Color c)
        {
            Color.RGBToHSV(c,out _,out float s,out float v);return s<=SaturationCap+.001f&&v<=ValueCap+.001f&&v>=ValueFloor-.001f;
        }

        // ---- Kit surfaces (LittleRoad/Kit): what each material is made of decides its pattern.
        public enum Pattern{Plain=0,Plaster=1,Stone=2,Tile=3,Wood=4,Leaf=5,Paint=6,Glass=7,Rock=8,Asphalt=9,Slate=10}
        public static Pattern PatternFor(string material)
        {
            bool Any(params string[] keys){foreach(var k in keys)if(material.StartsWith(k))return true;return false;}
            if(Any("Tint_Wall","PitWhite"))return Pattern.Plaster;
            if(Any("RockDark","Rock"))return Pattern.Rock;
            if(Any("StoneDark","Stone"))return Pattern.Stone;
            if(Any("RoofTile","RoofSlate"))return Pattern.Tile;
            if(Any("WoodDark","Wood","Bark","BirchBark"))return Pattern.Wood;
            if(Any("Leaf","Fir","Poplar","Blossom","Autumn","Moss","Reed"))return Pattern.Leaf;
            if(Any("GlassLit"))return Pattern.Plain;
            if(Any("Glass"))return Pattern.Glass;
            if(Any("WhiteTrim","Tint_","Canvas","Red","Yellow","Blue","Green","Pit"))return Pattern.Paint;
            return Pattern.Plain;
        }
        // A graded kit-shader material. worldSpace for meshes built in code (piers, decks, paving).
        public static Material Kit(string name,Color colour,Pattern pattern,float metal=0,float smooth=.2f,bool worldSpace=false,float wear=1)
        {
            EnsureNoise();var shader=Shader.Find("LittleRoad/Kit");
            if(shader==null||PerfPlainKit||(pattern==Pattern.Leaf&&RaceTrial.Has("--perf-lit-leaves")))return TrialWorld.Material(name,colour,metal,smooth);
            var m=new Material(shader){name=name,enableInstancing=true};m.SetColor("_BaseColor",Surface(colour));m.color=Surface(colour);
            m.SetFloat("_Pattern",(float)pattern);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.SetFloat("_WorldSpace",worldSpace?1:0);m.SetFloat("_Wear",wear);
            return m;
        }
        // Tileable value noise shared by every kit surface (_KitNoise).
        static Texture2D noise;
        public static void EnsureNoise()
        {
            if(noise!=null)return;const int n=256;noise=new Texture2D(n,n,TextureFormat.R8,true,true){name="Kit surface noise",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var px=new Color[n*n];
            float Tile(float x,float y,float f){float a=Mathf.PerlinNoise(x*f,y*f),b=Mathf.PerlinNoise((x-1)*f,y*f),c=Mathf.PerlinNoise(x*f,(y-1)*f),d=Mathf.PerlinNoise((x-1)*f,(y-1)*f);return Mathf.Lerp(Mathf.Lerp(a,b,x),Mathf.Lerp(c,d,x),y);}
            for(int j=0;j<n;j++)for(int i=0;i<n;i++){float x=i/(float)n,y=j/(float)n,v=Tile(x,y,6)*.55f+Tile(x,y,16)*.3f+Tile(x,y,40)*.15f;px[j*n+i]=new Color(v,v,v);}
            noise.SetPixels(px);noise.Apply(true);Shader.SetGlobalTexture("_KitNoise",noise);
        }

        // ---- Terrain: 0 = the photographic grass and rock maps, 1 = flat toy colours.
        public const float TerrainStylise=.35f,RunoffStylise=.55f,MasonryStylise=.3f;
        public static readonly Color ToyGrassLight=new(.31f,.52f,.22f),ToyGrassDeep=new(.19f,.37f,.15f);
        public const float Flowers=.05f;
        public static readonly Color ToyRockLight=new(.68f,.59f,.46f),ToyRockWarm=new(.53f,.44f,.34f);
        public static void Terrain(Material material,float stylise=TerrainStylise)
        {
            material.SetFloat("_Stylise",stylise);
            material.SetColor("_ToyGrassA",ToyGrassLight);material.SetColor("_ToyGrassB",ToyGrassDeep);
            material.SetColor("_ToyRockA",ToyRockLight);material.SetColor("_ToyRockB",ToyRockWarm);material.SetFloat("_Flowers",Flowers);
        }

        // ---- Light: a warm, low late-afternoon sun with real shadow contrast; cooler, dimmer sky
        // fill so forms read. Shadows reach far enough that the valley is not flat-lit.
        public static readonly Color SunColour=new(1f,.95f,.87f);public const float SunIntensity=2.4f,SunShadowStrength=.92f; // a high afternoon sun is nearly white; warmth comes with the low sun (TimeOfDay)
        public static readonly Color SkyAmbient=new(.50f,.62f,.74f),HorizonAmbient=new(.46f,.49f,.46f),GroundAmbient=new(.23f,.23f,.17f);
        public static readonly Color FillColour=new(.66f,.80f,1f);public const float FillIntensity=.26f;
        public const float ShadowDistance=240,ShadowDistanceRacing=140;

        // ---- Grade: a gentle filmic curve, a little less saturation than the source colours, cool
        // shadows and warm highlights for depth, soft vignette and bloom.
        public const float Saturation=-4,Contrast=24,PostExposure=.18f,Temperature=3,Tint=2,VignetteIntensity=.2f,BloomIntensity=.22f,BloomThreshold=1.05f;
        public static readonly Vector4 ShadowTone=new(.90f,.96f,1.10f,0),HighlightTone=new(1.06f,1.0f,.90f,0);

        // ---- Image quality. Temporal AA by default: SMAA alone left rails, fences, bunting, road
        // edges and roof tiles crawling in motion (graphics scorecard item 13). `--aa smaa|msaa|taa`
        // compares; MSAA disables TAA in URP. Sharpening restores the crispness TAA softens.
        public const float TaaSharpening=.4f,TaaBlend=.88f;
        public static string AntiAliasing=>RaceTrial.Arg("--aa",GraphicsOptions.AaMode);
        // ---- Rim and bounce on the kit (Kit.shader): rim strength (times the sky's own light), ground bounce
        // strength and its colour (sunlit meadow and paving). --no-rim-bounce turns both off.
        public const float KitRim=.35f,KitBounce=.14f;public static readonly Color KitBounceColour=new(.55f,.52f,.36f);
        public static void RimAndBounce()
        {
            bool on=!RaceTrial.Has("--no-rim-bounce");
            Shader.SetGlobalFloat("_KitRim",on?KitRim:0);Shader.SetGlobalFloat("_KitBounce",on?KitBounce:0);Shader.SetGlobalColor("_KitBounceColour",KitBounceColour);
        }
        // ---- Distance culling (BayviewKit.CullLayer sorts kit pieces by size). --no-distance-cull turns it off.
        public static bool DistanceCulling=>!RaceTrial.Has("--no-distance-cull");
        public const float DetailCullDistance=280,MediumCullDistance=700;
        public static void Camera(UnityEngine.Camera camera)
        {
            RimAndBounce();
            // Backstop just past the LOD fade-out (BayviewKit.CullLayer), so pieces dissolve rather than pop.
            if(DistanceCulling){var d=new float[32];d[BayviewKit.DetailLayer]=DetailCullDistance*1.25f;d[BayviewKit.MediumLayer]=MediumCullDistance*1.2f;camera.layerCullDistances=d;camera.layerCullSpherical=true;}
            var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
            string mode=AntiAliasing;camera.allowMSAA=mode=="msaa";
            if(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)urp.msaaSampleCount=mode=="msaa"?4:1;
            if(mode=="taa") {
                data.antialiasing=AntialiasingMode.TemporalAntiAliasing;ref var taa=ref data.taaSettings;
                taa.quality=TemporalAAQuality.High;taa.contrastAdaptiveSharpening=TaaSharpening;taa.baseBlendFactor=TaaBlend;
            } else if(mode=="off")data.antialiasing=AntialiasingMode.None;
            else {data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;}
        }
        // ---- Speed (graphics scorecard item 16): the view widens with speed and races add a light
        // camera motion blur, so 34 m/s stops feeling like 15. Free roam gets the FOV only.
        public const float SpeedFovGain=11,SpeedFovFrom=12,SpeedFovTo=38,RaceMotionBlur=.22f;
        public static float SpeedFov(float baseFov,float speed)=>baseFov+SpeedFovGain*GraphicsOptions.SpeedEffectScale*Mathf.SmoothStep(0,1,Mathf.InverseLerp(SpeedFovFrom,SpeedFovTo,Mathf.Abs(speed)));
        // Options > Graphics > Motion blur, applied live: off disables the race blur, on restores it in a race.
        public static void RaceBlurSetting()
        {
            bool on=!GraphicsOptions.Active||GraphicsOptions.MotionBlur.Index==0;
            if(on&&RaceTrial.Racing)RaceBlur();
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.isGlobal&&volume.profile!=null&&volume.profile.TryGet<MotionBlur>(out var blur))blur.active=on;
        }
        public static void RaceBlur()
        {
            if(GraphicsOptions.Active&&GraphicsOptions.MotionBlur.Index==1)return;
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) {
                if(!volume.isGlobal||volume.profile==null||volume.profile.Has<MotionBlur>())continue;
                // Camera and objects: a car moving with the camera (your own) stays sharp; the world streaks past.
                var blur=volume.profile.Add<MotionBlur>();blur.mode.Override(MotionBlurMode.CameraAndObjects);blur.intensity.Override(RaceMotionBlur);blur.clamp.Override(.05f);blur.quality.Override(MotionBlurQuality.Medium);
            }
        }
        // Frame pacing for play: vSync at the display's refresh (no tearing, no 60 fps ceiling on
        // high-refresh screens). Batch trials and frame-cost reviews set their own uncapped rates.
        public static void PlayPacing()=>GraphicsOptions.ApplyPacing();
        // After a teleport or camera cut: drop the temporal history so the old view doesn't smear in.
        public static void Cut(UnityEngine.Camera camera)
        {
            if(camera!=null&&camera.TryGetComponent<UniversalAdditionalCameraData>(out var data))data.resetHistory=true;
            // A 12 m camera jump would smear the whole frame through race motion blur: hold it off briefly.
            foreach(var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))if(volume.isGlobal&&volume.profile!=null&&volume.profile.TryGet<MotionBlur>(out var blur)&&blur.active){blur.active=false;held.Add(blur);}
            if(held.Count>0&&resume==null)resume=new GameObject("Motion blur resume").AddComponent<BlurResume>();
            if(resume!=null)resume.frames=2;
        }
        static readonly System.Collections.Generic.List<MotionBlur> held=new();static BlurResume resume;
        sealed class BlurResume:MonoBehaviour{public int frames;void LateUpdate(){if(--frames>0)return;foreach(var b in held)if(b!=null)b.active=true;held.Clear();resume=null;Destroy(gameObject);}}

        // ---- Clouds: soft shadows drifting over everything (a cookie on the sun).
        public const float CloudCover=.30f,CloudSize=900,CloudDrift=3.2f;

        // Frame-cost study switches (off by default; same build A/B): --perf-no-ssao,
        // --perf-shadows-110, --perf-plain-kit, --perf-no-clouds.
        public static bool PerfNoSsao=>RaceTrial.Has("--perf-no-ssao");
        public static bool PerfShortShadows=>RaceTrial.Has("--perf-shadows-110");
        public static bool PerfPlainKit=>RaceTrial.Has("--perf-plain-kit");
        public static bool PerfNoClouds=>RaceTrial.Has("--perf-no-clouds");
        public static void Lighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=SkyAmbient;
            RenderSettings.ambientEquatorColor=HorizonAmbient;RenderSettings.ambientGroundColor=GroundAmbient;
        }
        public static Light Sun(Vector3 direction,bool racing)
        {
            var sun=new GameObject("Late afternoon sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=SunIntensity;sun.color=SunColour;
            sun.transform.rotation=Quaternion.LookRotation(-direction);sun.shadows=LightShadows.Soft;sun.shadowStrength=SunShadowStrength;RenderSettings.sun=sun;
            if(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp) {
                urp.shadowDistance=PerfShortShadows?110:(racing?ShadowDistanceRacing:ShadowDistance)*GraphicsOptions.ShadowScale;
                urp.mainLightShadowmapResolution=GraphicsOptions.ShadowScale<1?2048:4096;
                if(PerfNoSsao)SetFeature(urp,"ScreenSpaceAmbientOcclusion",false);
            }
            if(CloudCover>0&&!PerfNoClouds){sun.cookie=CloudCookie();var data=sun.GetUniversalAdditionalLightData();data.lightCookieSize=new Vector2(CloudSize,CloudSize);sun.gameObject.AddComponent<CloudDrift>();}
            return sun;
        }
        static void SetFeature(UniversalRenderPipelineAsset urp,string name,bool on)
        {
            var renderer=urp.GetRenderer(0);var prop=typeof(ScriptableRenderer).GetProperty("rendererFeatures",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
            if(renderer==null||prop==null){Debug.LogWarning("LOOK_FEATURE_TOGGLE unavailable");return;}
            foreach(var f in (System.Collections.Generic.List<ScriptableRendererFeature>)prop.GetValue(renderer))if(f!=null&&f.name.Contains(name)){f.SetActive(on);Debug.Log($"LOOK_FEATURE {f.name} active={on}");}
        }
        public static void Fill()
        {
            var fill=new GameObject("Cool open-sky fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=FillIntensity;fill.color=FillColour;
            fill.transform.rotation=Quaternion.Euler(28,145,0);fill.shadows=LightShadows.None;
        }
        public static void Grade(VolumeProfile profile)
        {
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var bloom=profile.Add<Bloom>();bloom.intensity.Override(BloomIntensity);bloom.threshold.Override(BloomThreshold);bloom.scatter.Override(.6f);
            var colour=profile.Add<ColorAdjustments>();colour.postExposure.Override(PostExposure);colour.saturation.Override(Saturation);colour.contrast.Override(Contrast);
            var tones=profile.Add<ShadowsMidtonesHighlights>();tones.shadows.Override(ShadowTone);tones.highlights.Override(HighlightTone);
            var vignette=profile.Add<UnityEngine.Rendering.Universal.Vignette>();vignette.intensity.Override(VignetteIntensity);vignette.smoothness.Override(.5f);
            var balance=profile.Add<WhiteBalance>();balance.temperature.Override(Temperature);balance.tint.Override(Tint);
        }
        // Tileable soft cloud mask: 1 = sunlit, down to 1-CloudCover under a cloud.
        static Texture2D cookie;
        static Texture2D CloudCookie()
        {
            if(cookie!=null)return cookie;const int n=256;cookie=new Texture2D(n,n,TextureFormat.RGBA32,true,true){name="Cloud shadow cookie",wrapMode=TextureWrapMode.Repeat};
            var px=new Color[n*n];
            float Tile(float x,float y,float f){float a=Mathf.PerlinNoise(x*f,y*f),b=Mathf.PerlinNoise((x-1)*f,y*f),c=Mathf.PerlinNoise(x*f,(y-1)*f),d=Mathf.PerlinNoise((x-1)*f,(y-1)*f);return Mathf.Lerp(Mathf.Lerp(a,b,x),Mathf.Lerp(c,d,x),y);}
            for(int j=0;j<n;j++)for(int i=0;i<n;i++) {
                float x=i/(float)n,y=j/(float)n,cloud=Tile(x+13.1f%1,y,4)*.65f+Tile(x,y+7.7f%1,11)*.35f;
                float shade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.50f,.68f,cloud));float lit=1-shade*CloudCover;px[j*n+i]=new Color(lit,lit,lit,1);
            }
            cookie.SetPixels(px);cookie.Apply(true);return cookie;
        }
    }
    // Slides the sun's cloud cookie with the wind.
    public sealed class CloudDrift:MonoBehaviour
    {
        UniversalAdditionalLightData data;
        void Start()=>data=GetComponent<Light>().GetUniversalAdditionalLightData();
        void Update(){if(data!=null)data.lightCookieOffset=new Vector2(Time.time*WorldLook.CloudDrift,Time.time*WorldLook.CloudDrift*.55f);}
    }
}
