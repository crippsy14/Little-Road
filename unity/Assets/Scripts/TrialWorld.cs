using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleRoad
{
    public static class TrialWorld
    {
        // Every colour goes through WorldLook.Surface (the art-direction limits) unless graded=false.
        public static Material Material(string name,Color color,float metal=0,float smooth=.2f,bool graded=true)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=graded?WorldLook.Surface(color):color};
            m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;return m;
        }
        // Shared by runtime assembly and the build's retained shader reference.
        // URP alpha blending with preserved specular keeps sky reflections bright
        // while allowing the cabin to show through the tinted pane.
        // Car Forge lamp lenses (LampGlass): clear glass over a lamp's reflector and bulb, so the lamp reads in depth.
        // Tail and indicator lenses are tinted glass (LampGlassTailLight, LampGlassAmber); a TailLight lens is also one
        // of VehicleLights' brake materials, so the whole lens glows under braking.
        public static Color LampGlassColour(string name)=>name.Contains("TailLight")?new Color(.72f,.03f,.02f,.55f):name.Contains("Amber")?new Color(1,.42f,.04f,.5f):new Color(.9f,.93f,.96f,.14f);
        public static void ConfigureLampGlass(Material material){ConfigureMasterGlass(material);material.color=LampGlassColour(material.name);material.SetFloat("_Smoothness",.95f);}
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
        // Rubber laid down along the racing line: two soft dark tyre tracks and a faint broad band either
        // side of the smoothed line, drawn with the multiply SkidMark shader over the asphalt.
        public static void RubberedLine(Circuit track,Transform parent=null,float lift=.03f)
        {
            var shader=Shader.Find("LittleRoad/SkidMark");if(shader==null||RaceTrial.Has("--no-rubber-line"))return;
            var line=new RacingLine(track,RacingLine.BakeSmooth(track,"rubber-line","bayview"));
            var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var col=new System.Collections.Generic.List<Color>();var t=new System.Collections.Generic.List<int>();
            void Strip(float across,float width,Color tint,float strength)
            {
                int start=v.Count,n=Mathf.CeilToInt(track.length/2);
                for(int i=0;i<=n;i++){
                    float d=i*track.length/n,o=line.Offset(d)+across;var p=track.At(d);var r=track.Right(d);
                    // Rubber builds up where cars brake and turn: stronger on the bends, a hint on the straights.
                    float bend=Mathf.Clamp01(Mathf.Abs(Vector3.SignedAngle(track.Forward(d-6),track.Forward(d+6),Vector3.up))/14f);
                    var c=tint;c.a=strength*Mathf.Lerp(.45f,1,bend);
                    v.Add(p+r*(o-width*.5f)+Vector3.up*lift);v.Add(p+r*(o+width*.5f)+Vector3.up*lift);uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));col.Add(c);col.Add(c);
                    if(i<n){int a=start+i*2;t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
                }
            }
            Strip(0,3.6f,new Color(.82f,.83f,.85f),.55f);
            foreach(float side in new[]{-.78f,.78f})Strip(side,.62f,new Color(.55f,.56f,.58f),.5f);
            var mesh=new Mesh{name="Rubbered racing line"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(col);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
            var go=new GameObject("Rubbered racing line",typeof(MeshFilter),typeof(MeshRenderer));if(parent!=null)go.transform.SetParent(parent);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r2=go.GetComponent<MeshRenderer>();r2.sharedMaterial=new Material(shader){name="Rubbered line"};r2.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        static void Ribbon(Circuit track,string name,float center,float width,float height,Material mat,bool collision=false)
        {
            var o=new GameObject(name);o.AddComponent<MeshFilter>().sharedMesh=track.Ribbon(center,width,height);o.AddComponent<MeshRenderer>().sharedMaterial=mat;
            if(collision)o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;
        }
        // Synchronous build (reviews, fixtures). Play code yields CreateRoutine so the loading card
        // animates its progress between steps instead of freezing on one ~7 s frame.
        public static void Create(Circuit track,bool racing=true)=>RunNow(CreateRoutine(track,racing));
        public static void RunNow(System.Collections.IEnumerator routine)
        {
            var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(routine);
            while(stack.Count>0){var top=stack.Peek();if(top.MoveNext()){if(top.Current is System.Collections.IEnumerator inner)stack.Push(inner);}else stack.Pop();}
        }
        public static System.Collections.IEnumerator CreateRoutine(Circuit track,bool racing=true)
        {
            // Light, grade and clouds all come from WorldLook (see docs/art-direction.md).
            WorldLook.Lighting();
            // The scene is assembled at runtime and has no baked probes. Supply
            // diffuse sky fill explicitly so workshop shadows retain colour.
            var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(new Color(.25f,.29f,.31f));RenderSettings.ambientProbe=ambient;
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.64f,.81f,.86f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0014f;
            Vector3 sunDirection=TimeOfDay.Install(); // procedural day/night sky (TimeOfDay)
            WorldLook.Sun(sunDirection,racing);WorldLook.Fill();
            var volume=new GameObject("World colour grade").AddComponent<Volume>();volume.isGlobal=true;volume.profile=ScriptableObject.CreateInstance<VolumeProfile>();
            WorldLook.Grade(volume.profile);
            // The kit asphalt pattern (aggregate, old repair patches, hairline cracks), as on the valley roads;
            // per-pixel noise here blurred to a flat slab under TAA.
            var asphalt=WorldLook.Kit("Warm aggregate asphalt",new Color(.17f,.19f,.20f),WorldLook.Pattern.Asphalt,0,.22f,true);
            var sand=CoastalSurfaces.Runoff();
            var white=Material("Ivory paint",new Color(.91f,.91f,.80f));var coral=Material("Coral kerb",new Color(.79f,.27f,.18f));
            // One world, kept across races (PersistentWorld, docs/72): a world built earlier in this run is reused, and only
            // the light above and the race's own dressing are made again. A free-roam build is kept for the next load.
            bool reused=PersistentWorld.TryReuse();
            if(reused) {
                if(RaceArea.Active)PersistentWorld.Focus(RaceArea.Centre,RaceArea.Radius);
                else if(racing){var (centre,radius)=PersistentWorld.Circle(track,450);PersistentWorld.Focus(centre,radius);}
                else PersistentWorld.Unfocus();
            }
            if(!reused) {
            var before=PersistentWorld.Roots();
            CoastalLandscape.ValleyDetail=!racing;yield return new CoastalLandscape(track).BuildRoutine();
            // Both road and shoulder have actual matching mesh surfaces across
            // the inlet. The bridge is not a road painted on an invisible island.
            // (The island's loop, its dressing and the village: not in a race far from Bayview, CoastalLandscape.Island.)
            System.Collections.IEnumerator Island()
            {
            // (--new-bayview: two shoulders, not one sheet under the road: on a loop that climbs as it turns, a 24 m sheet's
            //  triangles stood through the 16 m asphalt in the hairpin)
            if(NewBayview.On)foreach(int side in new[]{-1,1})Ribbon(track,"Generous limestone runoff",side*10f,4f,.011f,sand);   // (from the asphalt's edge, not under it: R1-2)
            else Ribbon(track,"Generous limestone runoff",0,24,.011f,sand);
            Ribbon(track,"16 metre race surface",0,Circuit.Width,.025f,asphalt);
            RubberedLine(track);
            new GameObject("Continuous road and shoulder support",typeof(RoadSupport)).AddComponent<MeshCollider>().sharedMesh=track.DrivableSupport();
            foreach(int side in new[]{-1,1})Ribbon(track,"Edge paint",side*7.7f,.16f,.037f,white);
            for(float d=0;d<track.length;d+=12) {
                if(track.Curvature(d)>.012f)continue;var mark=Box("Grid guide",track.At(d)+Vector3.up*.04f,new Vector3(.12f,.015f,3),white);mark.transform.rotation=Quaternion.LookRotation(track.Forward(d));}
            CoastalLandscape.Lap("island circuit");yield return null;
            if(!NewBayview.On)VillageKit.Create();
            else{if(BayviewKit.Has("pit_stop_2"))PitStop.Build("bayview",false,NewBayview.PitStopSite,NewBayview.PitStopYaw,null);CoastalLandscape.Lap("island pit stop");yield return null;yield return NewBayview.BuildTownRoutine(track);}   // (docs/79, stage 2)
            }
            // (--near-start, stage 4: built with the island later, from a start away from it: CoastalLandscape.IslandLater)
            if(CoastalLandscape.Island){if(CoastalLandscape.IslandLater){CoastalLandscape.IslandTyres=TyreBarriers.Laid+TyreBarriers.Refused;CoastalLandscape.IslandFinish=Island;}else RunNow(Island());}
            if(!racing&&!RaceArea.Active)PersistentWorld.Adopt(before);
            }
            // Bayview's race dressing: kerbs, the chequered line and the start gantry, made for the race on top of the
            // world (kept or fresh) and gone with the scene afterwards.
            if(racing&&(reused||CoastalLandscape.Island))RaceDressing(track,white,coral,asphalt);
            new GameObject("Coastal reflected light").AddComponent<CoastalReflections>();
            TimeOfDay.Apply(); // light, sky, fog and valley mist for the chosen time of day
        }
        static void RaceDressing(Circuit track,Material white,Material coral,Material asphalt)
        {
            foreach(int side in new[]{-1,1})for(float d=0;d<track.length;d+=4) {// Coral blocks stand 6 mm prouder: on curves neighbouring blocks overlap and equal tops z-fought.
                bool odd=(int)(d/4)%2!=0;var kerb=Box("Alternating kerb",track.At(d)+track.Right(d)*side*8.25f+Vector3.up*(odd?.058f:.055f),new Vector3(.7f,odd?.106f:.10f,3.8f),odd?coral:white);
                kerb.transform.rotation=Quaternion.LookRotation(track.Forward(d));}
            for(int i=0;i<16;i++)for(int j=0;j<2;j++) {
                var checker=Box("Start finish",track.At(j*.55f)+track.Right(0)*(-7.5f+i)+Vector3.up*.046f,new Vector3(1,.02f,.55f),(i+j)%2==0?white:asphalt);checker.transform.rotation=Quaternion.LookRotation(track.Forward(0));}
            var shutter=Material("Teal joinery",new Color(.12f,.38f,.37f));
            for(int side=-1;side<=1;side+=2)Box("Start gantry post",track.At(0)+track.Right(0)*side*9+Vector3.up*3.5f,new Vector3(.4f,7,.4f),shutter,true);
            var beam=Box("Start gantry",track.At(0)+Vector3.up*6.7f,new Vector3(18,.8f,.4f),shutter);beam.transform.rotation=Quaternion.LookRotation(track.Forward(0));
        }
        // Every material name a car model may use has a rule in Car() below (the build checks each car model against this list;
        // a name with no rule renders the default charcoal).
        public static readonly string[] CarMaterialKeys={"RimGunmetal","RimBronze","RimWhite","RimBlack","PaintedSteel","StripeColour","BodyPaint","Glass","Rubber","Alloy",
            "Headlamp","TailLight","ReverseLight","RearReflector","BumperInsert","Amber","Plate","PanelSeam","BrakeMetal","Cloth","Cream","Livery","Wood",
            "CarbonFibre","GoldTrim","CaliperRed","CanopyGlass","CharcoalTrim","Primer","Rust","EnamelBlue","Rosy","Crust","CrustDark"};
        public static GameObject Car(string asset,Color paint)
        {
            // (an older class C body comes as its Car Forge namesake, and the car is named after the model it shows, so its
            //  collision box is that model's: CarBodies.Look)
            asset=CarBodies.Look(asset);
            var root=new GameObject(asset);
            var materials=new System.Collections.Generic.Dictionary<string,Material>();
            var visual=CarVisual(root,asset,paint,materials);
            // Bodies exported with reduced meshes (compact master, family field) get LOD groups.
            if(Resources.Load<GameObject>("Cars/"+asset+"_lod1")!=null) {
                var lod1=CarVisual(root,asset+"_lod1",paint,materials);var lod2=CarVisual(root,asset+"_lod2",paint,materials);
                var group=root.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(new[]{
                    new LOD(.18f,visual.GetComponentsInChildren<Renderer>()),
                    new LOD(.045f,lod1.GetComponentsInChildren<Renderer>()),
                    new LOD(.008f,lod2.GetComponentsInChildren<Renderer>())});group.RecalculateBounds();
            }
            if(CarBodies.UsesStandardEnvelope(asset))root.AddComponent<StandardEnvelope>();
            // --livery: Car Forge bodies wear a composed livery in their livery space (ForgeLivery).
            if(ForgeLivery.Enabled&&CarBodies.Forged(asset)!=null)ForgeLivery.Apply(root,paint,new Color(.95f,.94f,.9f),ForgeLivery.Number(asset),CarBodies.Forged(asset).livery??"plain");
            // (and its paint layers over the livery: graded rust, dirt, fade, tones; ForgeLayers, 24 Oct)
            ForgeLayers.Apply(root,asset,paint);
            root.AddComponent<VehicleLights>().Initialize();return root;
        }
        // The paint types (car-forge paint.FINISHES, written to the body file): metal and gloss for each, and a faded paint
        // bleached towards a dusty white (a rival's race colour on a faded car looks sun-bleached too). Solid is the default.
        public static readonly string[] PaintFinishes={"solid","metallic","pearl","candy","satin","matte","faded","patina"};
        public static void PaintFinish(string finish,ref Color c,ref float metal,ref float smooth)
        {
            switch(finish) {
                case "metallic":metal=.45f;smooth=.72f;break;
                case "pearl":metal=.3f;smooth=.8f;c=Color.Lerp(c,Color.white,.08f);break;
                case "candy":metal=.2f;smooth=.88f;break;
                case "satin":metal=.05f;smooth=.38f;break;
                case "matte":metal=0;smooth=.12f;break;
                case "faded":metal=0;smooth=.22f;c=Color.Lerp(c,new Color(.86f,.82f,.72f),.3f);break;
                case "patina":metal=0;smooth=.16f;c=Color.Lerp(c,new Color(.42f,.30f,.20f),.18f);break;
            }
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
                    // (Rim finishes, painted steel and stripes had no rule and all came out the default charcoal, so every
                    // gunmetal, bronze or white wheel looked the same in game; colours as build_family_cars.py makes them.
                    // A stripe takes the colour its model was built with, which differs per car.)
                    Color c=name.Contains("RimGunmetal")?new Color(.12f,.13f,.14f):name.Contains("RimBronze")?new Color(.40f,.26f,.09f):
                        name.Contains("RimWhite")?new Color(.86f,.87f,.86f):name.Contains("RimBlack")?new Color(.03f,.035f,.04f):
                        name.Contains("PaintedSteel")?new Color(.05f,.06f,.065f):
                        name.Contains("StripeColour")?(originals[i]!=null&&originals[i].HasProperty("_BaseColor")?originals[i].GetColor("_BaseColor"):originals[i]!=null&&originals[i].HasProperty("_Color")?originals[i].color:new Color(.92f,.92f,.88f)):
                        name.Contains("BodyPaint")?paint:name.Contains("Glass")?new Color(.035f,.09f,.12f):
                        name.Contains("Rubber")?new Color(.035f,.04f,.045f):name.Contains("Alloy")?new Color(.63f,.66f,.65f):
                        name.Contains("Headlamp")?new Color(1,.94f,.74f):name.Contains("TailLight")?new Color(.42f,.015f,.008f):
                        name.Contains("ReverseLight")?new Color(.65f,.70f,.72f):name.Contains("RearReflector")?new Color(.42f,.012f,.008f):
                        name.Contains("BumperInsert")?new Color(.22f,.25f,.26f):
                        name.Contains("Amber")?new Color(1,.38f,.02f):name.Contains("Plate")?new Color(.9f,.89f,.75f):
                        name.Contains("PanelSeam")?new Color(.024f,.054f,.051f):name.Contains("BrakeMetal")?new Color(.08f,.095f,.10f):
                        name.Contains("Cloth")?new Color(.065f,.088f,.094f):name.Contains("Cream")?new Color(.93f,.90f,.80f):name.Contains("Livery")&&!name.Contains("LiveryMap")?new Color(.85f,.10f,.08f):
                        name.Contains("Wood")?new Color(.45f,.27f,.13f):
                        // (Car Forge's wear, 17 Oct: a panel in grey primer or rust, forge/wear.py)
                        name.Contains("Primer")?new Color(.50f,.51f,.50f):name.Contains("Rust")?new Color(.36f,.16f,.07f):
                        // (a blue enamel tap cap: the Tubby, a special, 24 Oct)
                        name.Contains("EnamelBlue")?new Color(.09f,.20f,.62f):
                        // (a face's rosy cheeks: the Toastie, 24 Oct)
                        name.Contains("Rosy")?new Color(.86f,.42f,.42f):
                        // (a baked crust: the loaves on the Toastie's rack)
                        name.Contains("CrustDark")?new Color(.26f,.13f,.05f):name.Contains("Crust")?new Color(.70f,.42f,.17f):
                        // The Regale (Duke Rolo's prototype, docs/53): a painted livery, carbon, gold, red calipers, smoked canopy.
                        name.Contains("LiveryMap")?Color.white:name.Contains("CarbonFibre")?new Color(.03f,.033f,.036f):
                        name.Contains("GoldTrim")?new Color(.83f,.62f,.24f):name.Contains("CaliperRed")?new Color(.72f,.08f,.06f):
                        name.Contains("CanopyGlass")?new Color(.012f,.016f,.022f):new Color(.055f,.073f,.08f);
                    // Plastic seals and trim should not inherit glossy metallic
                    // paint. Preserve readable material separation at race distance.
                    bool paintSurface=name.Contains("BodyPaint"),alloySurface=name.Contains("Alloy");
                    bool chrome=name.Contains("Chrome"),livery=name.Contains("LiveryMap"),gold=name.Contains("GoldTrim");
                    bool rim=name.Contains("RimGunmetal")||name.Contains("RimBronze"),rimPainted=name.Contains("RimWhite")||name.Contains("RimBlack");
                    float metal=rim?.85f:rimPainted?.35f:name.Contains("PaintedSteel")?.3f:chrome?.9f:gold?.85f:alloySurface?.7f:name.Contains("BrakeMetal")?.55f:name.Contains("CarbonFibre")?.25f:paintSurface||livery?.05f:0;
                    float smooth=rim||rimPainted?.32f:name.Contains("PaintedSteel")?.45f:name.Contains("StripeColour")?.35f:chrome?.8f:gold?.66f:livery?.74f:name.Contains("CanopyGlass")?.9f:name.Contains("CarbonFibre")?.55f:alloySurface?.42f:paintSurface?.56f:name.Contains("Rubber")?.15f:
                        name.Contains("Headlamp")||name.Contains("TailLight")||name.Contains("ReverseLight")||name.Contains("Amber")?.6f:
                        name.Contains("Rust")?.1f:name.Contains("Primer")?.18f:.25f;
                    // (a Car Forge body's paint type, from its body file: the car-brief skill's MF section, 24 Oct)
                    if(paintSurface)PaintFinish(CarBodies.Forged(asset.Replace("_lod1","").Replace("_lod2",""))?.finish,ref c,ref metal,ref smooth);
                    converted[i]=Material(name,c,metal,smooth,!paintSurface&&!livery);
                    // The livery texture sits beside the model: Cars/<body>_livery (its colours are graded when painted).
                    if(livery){var art=Resources.Load<Texture2D>("Cars/"+asset.Replace("_lod1","").Replace("_lod2","")+"_livery");if(art!=null)converted[i].SetTexture("_BaseMap",art);}
                    if(name.Contains("TailLight")||name.Contains("ReverseLight")) {
                        converted[i].EnableKeyword("_EMISSION");converted[i].SetColor("_EmissionColor",Color.black);
                    }
                    if(name.Contains("Headlamp"))NightLights.Headlamp(converted[i]); // lit at night
                    // Prototype glazing consists of single-sided polygons. glTF
                    // rendered both sides; preserve that intent in the FBX port.
                    if(name.Contains("Glass"))converted[i].SetFloat("_Cull",0);
                    // (a Car Forge body's own paint is two-sided: where its inside lining leaves a strip beside a window,
                    //  the inside shows painted metal, as a diecast casting's does, not the world behind; Richard's snap, 15 Oct)
                    if(name.Contains("_forge_")&&(name.Contains("BodyPaint")||name.Contains("StripeColour")))converted[i].SetFloat("_Cull",0);
                    if(name.Contains("Cloth")){converted[i].SetFloat("_Smoothness",.1f);converted[i].SetFloat("_Metallic",0);}
                    if(name.Contains("MasterGlass"))ConfigureMasterGlass(converted[i]);
                    if(name.Contains("LampGlass"))ConfigureLampGlass(converted[i]);
                    materials[name]=converted[i];
                }
                renderer.sharedMaterials=converted;
            }
            return visual;
        }
    }
}
