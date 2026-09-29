using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    // Day and night. The town world uses a procedural sky (LittleRoad/DaySky, DaySky.hlsl) instead of
    // the photographed panorama, so the sun can be anywhere: a real sun path (latitude 44°, late
    // summer: up at 06:00, down at 20:00) drives the sun, moon, sky, clouds, light, ambient, fog, valley
    // mist and exposure through keys by sun elevation. The 55° key is the WorldLook afternoon.
    // Pause menu > Graphics > Time of day: Cycle (about 20 minutes a day, nights 3x faster) or a fixed
    // time (changes glide there). Reviews default to a fixed afternoon; `--time morning|afternoon|golden|
    // dusk|night|<hour>` picks another, `--time-cycle` runs the clock. Night lighting: NightLights.
    public static class TimeOfDay
    {
        public static Vector3 PhotoSun=Vector3.up;   // set by CoastalSky.Apply (Cedar Hills keeps the photo sky)
        public const float Latitude=44,Declination=15,SecondsPerHour=50,NightSpeedUp=3,CloudHeight=1500,CloudTile=5200; // sky tile (the shadow cookie keeps WorldLook.CloudSize)
        public static readonly (string name,string arg,float hour)[] Fixed={("Morning","morning",8.5f),("Afternoon","afternoon",14.5f),("Golden hour","golden",19.1f),("Dusk","dusk",20.35f),("Night","night",23.5f)};
        public static float Hour=15f;        // static: races and the return to town continue the same day
        public static bool Cycling{get;private set;}
        static float? glideTo;static bool placed;

        sealed class Key
        {
            public float el,sunI,fill,ev,stars,mistDensity;
            public Color sun,sky,equator,ground,fog,zenith,horizon,glow,cloudLit,cloudShade,mist;
        }
        // Colours are sRGB (as authored everywhere else); ev is extra exposure (stops) for dark hours.
        static readonly Key[] Keys={
            new(){el=-30,sunI=0,fill=.08f,ev=1.3f,stars=1,sun=Color.black,sky=new(.16f,.21f,.34f),equator=new(.11f,.14f,.22f),ground=new(.04f,.05f,.07f),fog=new(.07f,.10f,.17f),
                zenith=new(.015f,.028f,.08f),horizon=new(.07f,.10f,.18f),glow=Color.black,cloudLit=new(.11f,.13f,.19f),cloudShade=new(.035f,.045f,.075f),mist=new(.07f,.09f,.15f,.30f),mistDensity=.0009f},
            new(){el=-10,sunI=0,fill=.10f,ev=1.1f,stars=.55f,sun=Color.black,sky=new(.20f,.25f,.40f),equator=new(.15f,.17f,.26f),ground=new(.05f,.055f,.07f),fog=new(.13f,.16f,.27f),
                zenith=new(.03f,.055f,.15f),horizon=new(.14f,.18f,.32f),glow=new(.30f,.18f,.25f),cloudLit=new(.18f,.18f,.26f),cloudShade=new(.06f,.07f,.11f),mist=new(.14f,.17f,.27f,.34f),mistDensity=.0010f},
            new(){el=-4,sunI=0,fill=.15f,ev=1.05f,stars=.12f,sun=new(1,.5f,.3f),sky=new(.28f,.32f,.48f),equator=new(.32f,.28f,.36f),ground=new(.09f,.085f,.09f),fog=new(.38f,.36f,.48f),
                zenith=new(.08f,.13f,.32f),horizon=new(.50f,.42f,.54f),glow=new(.95f,.45f,.28f),cloudLit=new(.62f,.46f,.52f),cloudShade=new(.22f,.21f,.31f),mist=new(.36f,.36f,.48f,.42f),mistDensity=.0011f},
            new(){el=0,sunI=.9f,fill=.16f,ev=.45f,stars=0,sun=new(1,.46f,.22f),sky=new(.33f,.35f,.50f),equator=new(.56f,.40f,.34f),ground=new(.14f,.12f,.10f),fog=new(.82f,.58f,.46f),
                zenith=new(.16f,.25f,.50f),horizon=new(.96f,.58f,.38f),glow=new(1,.42f,.16f),cloudLit=new(1,.55f,.36f),cloudShade=new(.38f,.31f,.42f),mist=new(.80f,.60f,.50f,.46f),mistDensity=.0010f},
            new(){el=6,sunI=1.85f,fill=.18f,ev=.12f,stars=0,sun=new(1,.64f,.36f),sky=new(.40f,.43f,.55f),equator=new(.64f,.48f,.36f),ground=new(.20f,.16f,.12f),fog=new(.92f,.71f,.53f),
                zenith=new(.22f,.36f,.66f),horizon=new(.98f,.72f,.48f),glow=new(1,.55f,.24f),cloudLit=new(1,.74f,.50f),cloudShade=new(.50f,.44f,.52f),mist=new(.94f,.74f,.56f,.44f),mistDensity=.0009f},
            new(){el=14,sunI=2.2f,fill=.22f,ev=0,stars=0,sun=new(1,.78f,.56f),sky=new(.45f,.52f,.64f),equator=new(.56f,.50f,.43f),ground=new(.22f,.19f,.14f),fog=new(.82f,.78f,.70f),
                zenith=new(.26f,.45f,.78f),horizon=new(.88f,.82f,.72f),glow=new(1,.72f,.42f),cloudLit=new(1,.90f,.76f),cloudShade=new(.56f,.55f,.60f),mist=new(.88f,.80f,.68f,.36f),mistDensity=.0008f},
            new(){el=30,sunI=2.35f,fill=.25f,ev=0,stars=0,sun=new(1,.88f,.72f),sky=new(.49f,.60f,.72f),equator=new(.48f,.50f,.47f),ground=new(.23f,.22f,.17f),fog=new(.68f,.81f,.86f),
                zenith=new(.22f,.45f,.82f),horizon=new(.72f,.83f,.90f),glow=new(1,.90f,.74f),cloudLit=new(1,.99f,.96f),cloudShade=new(.60f,.65f,.74f),mist=new(.82f,.87f,.89f,.30f),mistDensity=.0006f},
            new(){el=55,sunI=WorldLook.SunIntensity,fill=WorldLook.FillIntensity,ev=0,stars=0,sun=WorldLook.SunColour,sky=WorldLook.SkyAmbient,equator=WorldLook.HorizonAmbient,ground=WorldLook.GroundAmbient,fog=new(.64f,.81f,.86f),
                zenith=new(.20f,.44f,.84f),horizon=new(.66f,.80f,.90f),glow=new(1,.95f,.85f),cloudLit=new(1,1,1),cloudShade=new(.58f,.64f,.76f),mist=new(.80f,.87f,.90f,.26f),mistDensity=.0005f}};
        static readonly Color MoonLight=new(.62f,.72f,1f);
        static readonly Color AfternoonProbe=new(.25f,.29f,.31f);   // TrialWorld's afternoon SH ambient (linear)

        // ---- sun path
        public static Vector3 Direction(float hour,float declination)
        {
            float lat=Latitude*Mathf.Deg2Rad,dec=declination*Mathf.Deg2Rad,ha=(hour-13)*15*Mathf.Deg2Rad; // solar noon 13:00
            // Local frame: x east, y up, z north.
            var local=new Vector3(-Mathf.Cos(dec)*Mathf.Sin(ha),Mathf.Sin(lat)*Mathf.Sin(dec)+Mathf.Cos(lat)*Mathf.Cos(dec)*Mathf.Cos(ha),
                Mathf.Cos(lat)*Mathf.Sin(dec)-Mathf.Sin(lat)*Mathf.Cos(dec)*Mathf.Cos(ha));
            return Quaternion.Euler(0,WorldYaw,0)*local.normalized;
        }
        // Turn the compass so the 14:30 sun stands where the photographed afternoon sun did (azimuth 45°).
        static float worldYaw=float.NaN;
        static float WorldYaw{get{
            if(float.IsNaN(worldYaw)){worldYaw=0;var at=Direction(14.5f,Declination);at.y=0;worldYaw=Vector3.SignedAngle(at,new Vector3(Mathf.Cos(45*Mathf.Deg2Rad),0,Mathf.Sin(45*Mathf.Deg2Rad)),Vector3.up);}
            return worldYaw;}}
        public static Vector3 Sun(float hour)=>Direction(hour,Declination);
        public static Vector3 Moon(float hour)=>Direction(hour+12,-Declination);
        public static float Elevation(float hour)=>Mathf.Asin(Mathf.Clamp(Sun(hour).y,-1,1))*Mathf.Rad2Deg;

        // ---- choice
        static (bool cycle,float hour) Chosen()
        {
            string arg=RaceTrial.Arg("--time",null);
            if(arg!=null) {
                foreach(var f in Fixed)if(f.arg==arg)return (RaceTrial.Has("--time-cycle"),f.hour);
                if(float.TryParse(arg,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float h))return (RaceTrial.Has("--time-cycle"),Mathf.Repeat(h,24));
            }
            if(!GraphicsOptions.Active)return (RaceTrial.Has("--time-cycle"),14.5f);
            int i=GraphicsOptions.Time.Index;return i==0?(true,Hour):(false,Fixed[i-1].hour);
        }
        // World creation: the procedural sky, cloud texture and the clock. Returns the sun direction.
        public static Vector3 Install()
        {
            var (cycle,hour)=Chosen();Cycling=cycle;if(!cycle||!placed)Hour=hour;placed=true;glideTo=null;
            RenderSettings.skybox=new Material(Shader.Find("LittleRoad/DaySky")){name="Procedural day sky"};
            Shader.SetGlobalTexture("_DayCloudTex",CloudTexture());
            return Sun(Hour);
        }
        // After world creation, and when the option changes: fixed times glide there over a few seconds.
        public static void Apply()
        {
            var (cycle,hour)=Chosen();Cycling=cycle;
            var clock=Object.FindAnyObjectByType<DayClock>();
            if(clock==null){Hour=cycle?Hour:hour;new GameObject("Day clock").AddComponent<DayClock>();}
            else if(!cycle)glideTo=hour;
            Set(Hour);
            Debug.Log($"LITTLE_ROAD_TIME_OF_DAY hour={Hour:0.00} cycle={cycle} elevation={Elevation(Hour):0.0}");
        }
        public static void Tick(float dt)
        {
            if(glideTo is float to) {
                float left=Mathf.Repeat(to-Hour,24);                         // forward through the day, like a time-lapse
                if(left<.01f||left>23.99f){Hour=to;glideTo=null;}else Hour+=Mathf.Min(left,dt*Mathf.Max(2.5f,left/2.2f));
            }
            else if(Cycling)Hour+=dt/SecondsPerHour*(Elevation(Hour)<-6?NightSpeedUp:1);
            Hour=Mathf.Repeat(Hour,24);Set(Hour);
        }

        // ---- apply one moment
        static Light fill;static Volume grade;static ColorAdjustments exposure;
        public static float Night{get;private set;}
        static Key Sample(float el)
        {
            if(el<=Keys[0].el)return Keys[0];if(el>=Keys[^1].el)return Keys[^1];
            int i=1;while(Keys[i].el<el)i++;var a=Keys[i-1];var b=Keys[i];float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(a.el,b.el,el));
            Color C(Color x,Color y)=>Color.Lerp(x,y,t);float F(float x,float y)=>Mathf.Lerp(x,y,t);
            return new Key{el=el,sunI=F(a.sunI,b.sunI),fill=F(a.fill,b.fill),ev=F(a.ev,b.ev),stars=F(a.stars,b.stars),mistDensity=F(a.mistDensity,b.mistDensity),
                sun=C(a.sun,b.sun),sky=C(a.sky,b.sky),equator=C(a.equator,b.equator),ground=C(a.ground,b.ground),fog=C(a.fog,b.fog),zenith=C(a.zenith,b.zenith),horizon=C(a.horizon,b.horizon),
                glow=C(a.glow,b.glow),cloudLit=C(a.cloudLit,b.cloudLit),cloudShade=C(a.cloudShade,b.cloudShade),mist=C(a.mist,b.mist)};
        }
        public static void Set(float hour)
        {
            Vector3 sun=Sun(hour),moon=Moon(hour);float el=Mathf.Asin(Mathf.Clamp(sun.y,-1,1))*Mathf.Rad2Deg;var k=Sample(el);
            Night=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2,-7,el));
            float moonI=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-3,-10,el))*.34f*Mathf.Clamp01(moon.y*4);
            var light=RenderSettings.sun;
            if(light!=null) {
                bool useMoon=moonI>k.sunI;var dir=useMoon?moon:sun;
                light.transform.rotation=Quaternion.LookRotation(-dir);light.color=useMoon?MoonLight:k.sun;light.intensity=useMoon?moonI:k.sunI;
            }
            if(fill==null)foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))if(l.name=="Cool open-sky fill")fill=l;
            if(fill!=null)fill.intensity=k.fill;
            RenderSettings.ambientSkyColor=k.sky;RenderSettings.ambientEquatorColor=k.equator;RenderSettings.ambientGroundColor=k.ground;RenderSettings.fogColor=k.fog;
            // The SH ambient every lit shader reads: the afternoon probe, re-tinted by the sky ambient.
            Color s=k.sky.linear,a=WorldLook.SkyAmbient.linear;
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(new Color(AfternoonProbe.r*s.r/a.r,AfternoonProbe.g*s.g/a.g,AfternoonProbe.b*s.b/a.b));RenderSettings.ambientProbe=probe;
            Shader.SetGlobalVector("_DaySunDir",new Vector4(sun.x,sun.y,sun.z,1));
            Shader.SetGlobalVector("_DayMoonDir",new Vector4(moon.x,moon.y,moon.z,Night*Mathf.Clamp01(moon.y*6)));
            Shader.SetGlobalColor("_DayZenith",k.zenith);Shader.SetGlobalColor("_DayHorizon",k.horizon);Shader.SetGlobalColor("_DayGlow",k.glow);
            Shader.SetGlobalColor("_DayCloudLit",k.cloudLit);Shader.SetGlobalColor("_DayCloudShade",k.cloudShade);
            Color disc=k.sun.linear*(14*Mathf.Clamp01((el+1.5f)/3));Shader.SetGlobalVector("_DaySunDisc",new Vector4(disc.r,disc.g,disc.b,0));
            Shader.SetGlobalVector("_DayCloudParams",new Vector4(.47f,CloudTile,CloudHeight,.94f));
            float t=Time.time*WorldLook.CloudDrift;Shader.SetGlobalVector("_DayCloudOffset",new Vector4(t,t*.55f,k.stars,0));
            Color mist=k.mist.linear;Shader.SetGlobalVector("_HeightFogColor",new Vector4(mist.r,mist.g,mist.b,k.mist.a));
            Shader.SetGlobalVector("_HeightFogParams",new Vector4(k.mistDensity,5,20,0));
            if(grade==null){exposure=null;grade=Object.FindAnyObjectByType<Volume>();if(grade!=null&&grade.profile!=null)grade.profile.TryGet(out exposure);}
            if(exposure!=null)exposure.postExposure.Override(WorldLook.PostExposure+k.ev);
            NightLights.Apply(Night);
            lastElevation=el;
        }
        public static float lastElevation;

        // ---- clouds: one tileable noise for the sky layer (r: shapes, g: detail) and the shadow cookie.
        static Texture2D clouds;
        public static Texture2D CloudTexture()
        {
            if(clouds!=null)return clouds;const int n=256;clouds=new Texture2D(n,n,TextureFormat.RGBA32,true,true){name="Cloud noise",wrapMode=TextureWrapMode.Repeat};
            var px=new Color[n*n];
            float Tile(float x,float y,float f){float a=Mathf.PerlinNoise(x*f,y*f),b=Mathf.PerlinNoise((x-1)*f,y*f),c=Mathf.PerlinNoise(x*f,(y-1)*f),d=Mathf.PerlinNoise((x-1)*f,(y-1)*f);return Mathf.Lerp(Mathf.Lerp(a,b,x),Mathf.Lerp(c,d,x),y);}
            for(int j=0;j<n;j++)for(int i=0;i<n;i++) {
                float x=i/(float)n,y=j/(float)n;
                float cloud=Tile(x+13.1f%1,y,4)*.65f+Tile(x,y+7.7f%1,11)*.35f;           // the cookie's own shapes
                float detail=Tile(x+.37f,y+.61f,24)*.6f+Tile(x+.71f,y+.13f,48)*.4f;
                px[j*n+i]=new Color(cloud,detail,0,1);
            }
            clouds.SetPixels(px);clouds.Apply(true);return clouds;
        }
    }

    // Runs the clock, refreshes reflections as the light changes, and drives NightLights' pools.
    public sealed class DayClock:MonoBehaviour
    {
        float probeElevation,nextPool,nextProbeCheck;bool dirty;readonly Dictionary<ReflectionProbe,float> stamp=new();
        // The sky material replaced the photo: rebuild the default reflection cube once (probes render in CoastalReflections).
        void Start(){probeElevation=TimeOfDay.lastElevation;DynamicGI.UpdateEnvironment();TimeOfDay.Set(TimeOfDay.Hour);}
        void Update()
        {
            TimeOfDay.Tick(Time.deltaTime);
            // Every 4° of sun: refresh the sky cube, and re-capture (one face per frame) only the realtime
            // probes near the camera. Each capture renders the world six times; a probe the player is
            // nowhere near waits until they come back (a 2° all-probe refresh was a hitch every ~7 s).
            if(Mathf.Abs(TimeOfDay.lastElevation-probeElevation)>4f) {
                probeElevation=TimeOfDay.lastElevation;dirty=true;
                DynamicGI.UpdateEnvironment();TimeOfDay.Set(TimeOfDay.Hour);
            }
            if(dirty&&Time.time>=nextProbeCheck) {
                nextProbeCheck=Time.time+1;var eye=Camera.main!=null?Camera.main.transform.position:Vector3.zero;bool waiting=false;
                foreach(var probe in FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None)) {
                    if(probe.mode!=ReflectionProbeMode.Realtime)continue;
                    if(probe.bounds.SqrDistance(eye)>150*150){waiting=true;continue;}
                    if(stamp.TryGetValue(probe,out float at)&&at==probeElevation)continue;
                    stamp[probe]=probeElevation;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.RenderProbe();
                }
                dirty=waiting;
            }
            if(Time.time>=nextPool){nextPool=Time.time+.4f;NightLights.Assign(Camera.main);}
        }
        void LateUpdate()=>NightLights.Follow();
    }

    // Night: lit windows glow, street lamps light the road (a pool of point lights on the lamps nearest
    // the camera), and cars run headlamps (emissive lenses on every car, a spot light on the nearest few).
    public static class NightLights
    {
        static readonly List<(Material m,Color c)> windows=new();
        static readonly List<Material> headlamps=new();
        static readonly List<Transform> lamps=new();
        static readonly List<Light> lampPool=new(),beamPool=new();
        static readonly List<Transform> beamCars=new();
        const int LampLights=18,Beams=8;
        public static float Night{get;private set;}
        static bool dirty=true;
        public static void Window(Material m,Color c){windows.Add((m,c));dirty=true;}
        public static void Headlamp(Material m){headlamps.Add(m);dirty=true;}
        public static void Lamp(Transform t)=>lamps.Add(t);
        public static void Apply(float night)
        {
            if(!dirty&&Mathf.Abs(night-Night)<.002f)return;
            Night=night;dirty=false;Shader.SetGlobalFloat("_KitNight",night);
            windows.RemoveAll(w=>w.m==null);headlamps.RemoveAll(m=>m==null);
            foreach(var (m,c) in windows)m.SetColor("_EmissionColor",c*Mathf.Lerp(.6f,3.2f,night));
            foreach(var m in headlamps){if(night>.01f){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(2.6f,2.45f,2.0f)*night);}else m.SetColor("_EmissionColor",Color.black);}
        }
        static Light Make(string name,LightType type)
        {
            var l=new GameObject(name).AddComponent<Light>();l.type=type;l.shadows=LightShadows.None;l.enabled=false;return l;
        }
        public static void Assign(Camera camera)
        {
            lamps.RemoveAll(t=>t==null);
            if(lampPool.Count>0&&lampPool[0]==null){lampPool.Clear();beamPool.Clear();}
            bool dark=Night>.02f&&camera!=null;
            while(lampPool.Count<LampLights){var l=Make("Street lamp light",LightType.Point);l.color=new Color(1,.76f,.48f);l.range=15;lampPool.Add(l);}
            while(beamPool.Count<Beams){var l=Make("Headlight beam",LightType.Spot);l.color=new Color(1,.94f,.82f);l.range=46;l.spotAngle=78;l.innerSpotAngle=38;beamPool.Add(l);}
            Vector3 eye=dark?camera.transform.position:Vector3.zero;
            if(dark)lamps.Sort((a,b)=>(a.position-eye).sqrMagnitude.CompareTo((b.position-eye).sqrMagnitude));
            for(int i=0;i<lampPool.Count;i++) {
                bool on=dark&&i<lamps.Count&&(lamps[i].position-eye).sqrMagnitude<160*160;lampPool[i].enabled=on;
                if(on){float s=lamps[i].lossyScale.y;lampPool[i].transform.position=lamps[i].position+Vector3.up*4.15f*s;lampPool[i].intensity=5.5f*Night;}
            }
            beamCars.Clear();
            var race=Object.FindAnyObjectByType<RaceTrial>();
            if(dark&&race!=null) {
                var cars=new List<Transform>();foreach(var c in race.cars)if(c!=null)cars.Add(c.transform);
                cars.Sort((a,b)=>(a.position-eye).sqrMagnitude.CompareTo((b.position-eye).sqrMagnitude));
                for(int i=0;i<cars.Count&&i<Beams;i++)beamCars.Add(cars[i]);
            }
            for(int i=0;i<beamPool.Count;i++){beamPool[i].enabled=i<beamCars.Count;beamPool[i].intensity=26*Night;}
            Follow();
        }
        public static void Follow()
        {
            for(int i=0;i<beamCars.Count&&i<beamPool.Count;i++) {
                var car=beamCars[i];if(car==null||beamPool[i]==null)continue;
                beamPool[i].transform.SetPositionAndRotation(car.position+car.forward*2.2f+Vector3.up*.8f,Quaternion.LookRotation(car.forward-car.up*.13f,car.up));
            }
        }
    }
}
