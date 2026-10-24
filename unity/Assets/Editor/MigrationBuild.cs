using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LittleRoad;

public static class MigrationBuild
{
    [Serializable] public class Evidence {public string engine;public bool developmentBuild;public List<string> checks=new();public List<string> models=new();}
    [MenuItem("Little Road/Build and check Windows trial")]
    public static void Build()
    {
        // Ordinary review builds do not need the development PlayerConnection network listener.
        bool developmentBuild=RaceTrial.Has("--development-build");
        var evidence=new Evidence{engine=Application.unityVersion,developmentBuild=developmentBuild};
        BayviewTransferBake.Prepare();
        foreach(string surfacePath in Directory.GetFiles("Assets/Resources/Surfaces","*",SearchOption.AllDirectories)) {
            if(surfacePath.EndsWith(".meta"))continue;
            var surfaceImporter=AssetImporter.GetAtPath(surfacePath.Replace('\\','/')) as TextureImporter;
            if(surfaceImporter==null)continue;
            bool normal=surfacePath.Contains("_nor_gl_");
            surfaceImporter.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            surfaceImporter.sRGBTexture=surfacePath.Contains("_diff_");surfaceImporter.wrapMode=TextureWrapMode.Repeat;
            surfaceImporter.mipmapEnabled=true;surfaceImporter.anisoLevel=8;surfaceImporter.maxTextureSize=2048;
            surfaceImporter.textureCompression=TextureImporterCompression.CompressedHQ;surfaceImporter.SaveAndReimport();
            Check(true,"Imported coastal surface "+Path.GetFileName(surfacePath),evidence);
        }
        const string skyPath="Assets/Resources/Sky/kloofendal_48d_partly_cloudy_puresky_2k.hdr";
        var skyImporter=AssetImporter.GetAtPath(skyPath) as TextureImporter;
        if(skyImporter==null)throw new InvalidOperationException("Missing licensed coastal sky asset.");
        skyImporter.textureType=TextureImporterType.Default;skyImporter.textureShape=TextureImporterShape.Texture2D;
        skyImporter.sRGBTexture=false;skyImporter.isReadable=true;skyImporter.mipmapEnabled=true;skyImporter.wrapModeU=TextureWrapMode.Repeat;skyImporter.wrapModeV=TextureWrapMode.Clamp;
        skyImporter.textureCompression=TextureImporterCompression.Uncompressed;skyImporter.maxTextureSize=2048;
        var skyPlatform=skyImporter.GetPlatformTextureSettings("Standalone");skyPlatform.overridden=true;skyPlatform.format=TextureImporterFormat.RGBAHalf;skyPlatform.maxTextureSize=2048;skyImporter.SetPlatformTextureSettings(skyPlatform);skyImporter.SaveAndReimport();
        var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        PlayerSettings.companyName="Little Road Project";PlayerSettings.productName="Little Road Unity Trial";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;PlayerSettings.enableFrameTimingStats=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Little Road migration trial").AddComponent<RaceTrial>();
        Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/CoastalTrial.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/CoastalTrial.unity",true)};
        Check(pipeline!=null,"URP pipeline assigned",evidence);
        // Schema 2 (docs/47): every class stocks every slot, and every part id a pre-ladder save can hold survives.
        var savedIds=new[]{"brakes_race","brakes_sport","brakes_stock","chassis_light","chassis_stock","engine_club","engine_stock","engine_workshop","steering_precision","steering_quick","steering_stock","transmission_close","transmission_long","transmission_stock","turbo_club","turbo_stock","turbo_street","tyres_sport","tyres_stock","tyres_touring","tyres_trail","engine_valley","turbo_twin","tyres_grip","tyres_rally","brakes_ceramic","steering_rally","chassis_carbon","transmission_six"};
        Check(EquipmentCatalog.All.slots.Length==7&&savedIds.All(id=>EquipmentCatalog.All.parts.Any(p=>p.id==id)),"Seven slots; all 29 pre-ladder part ids preserved",evidence);
        Check(EquipmentCatalog.All.slots.All(slot=>RaceCalendar.Classes.All(c=>EquipmentCatalog.All.parts.Count(p=>p.slot==slot&&p.tier==c)>=3)),"Every class stocks at least three parts per slot",evidence);
        Check(EquipmentCatalog.All.parts.Count(p=>p.shop=="hillcrest")>=3,"Hillcrest Motors stocks its own valley parts",evidence);
        var stockFit=EquipmentCatalog.StockLoadout();
        foreach(var part in EquipmentCatalog.All.parts) {
            var fit=new Dictionary<string,string>(stockFit);fit[part.slot]=part.id;var stats=EquipmentCatalog.Stats(fit);
            Check(stats.mass>0 && stats.power>0 && stats.gearCeiling>0 && stats.roadGrip>0,"Valid single-part loadout "+part.id,evidence);
        }
        Check(Enumerable.Range(1,23).All(p=>EquipmentCatalog.Reward(p)>=EquipmentCatalog.Reward(p+1))&&EquipmentCatalog.Reward(24)>0&&EquipmentCatalog.Reward(0)==0,"Every finisher is paid, better places pay more, a non-finish pays nothing",evidence);
        Directory.CreateDirectory("Assets/Resources/ShaderReferences");
        foreach(string shaderName in new[]{"Universal Render Pipeline/Lit","Skybox/Procedural","LittleRoad/CoastalSky","LittleRoad/DaySky","LittleRoad/Bunting","LittleRoad/Minimap","LittleRoad/CoastalTerrain","LittleRoad/CoastalFoliage","LittleRoad/CoastalWater","LittleRoad/WorldText","LittleRoad/Distant","LittleRoad/Crops","LittleRoad/Wheat","LittleRoad/Kit","LittleRoad/InlandWater","LittleRoad/Grass","LittleRoad/SkidMark","LittleRoad/LightTrail","LittleRoad/Waterfall","LittleRoad/SoftPuff"}) {
            var shader=Shader.Find(shaderName);Check(shader!=null,"Required shader "+shaderName,evidence);
            // Find succeeds for a shader that failed to compile (it renders pink): fail the build instead.
            Check(!ShaderUtil.ShaderHasError(shader),"Shader compiles without errors "+shaderName,evidence);
            string path="Assets/Resources/ShaderReferences/"+shaderName.Replace('/','_')+".mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)AssetDatabase.CreateAsset(new Material(shader),path);
            // Grass is drawn only with Graphics.RenderMeshInstanced: builds strip instancing variants
            // no included material enables, which left every clump collapsed at the origin.
            if(shaderName=="LittleRoad/Grass"){var reference=AssetDatabase.LoadAssetAtPath<Material>(path);if(!reference.enableInstancing){reference.enableInstancing=true;EditorUtility.SetDirty(reference);AssetDatabase.SaveAssets();}}
        }
        const string lampReference="Assets/Resources/ShaderReferences/VehicleLamp.mat";
        var lampMaterial=AssetDatabase.LoadAssetAtPath<Material>(lampReference);
        if(lampMaterial==null){lampMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(lampMaterial,lampReference);}
        lampMaterial.EnableKeyword("_EMISSION");lampMaterial.SetColor("_EmissionColor",Color.red);EditorUtility.SetDirty(lampMaterial);
        const string glassReference="Assets/Resources/ShaderReferences/VehicleGlass.mat";
        var glassMaterial=AssetDatabase.LoadAssetAtPath<Material>(glassReference);
        if(glassMaterial==null){glassMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(glassMaterial,glassReference);}
        TrialWorld.ConfigureMasterGlass(glassMaterial);EditorUtility.SetDirty(glassMaterial);
        float stock=TimeTo20(CarTuning.Stock),club=TimeTo20(CarTuning.Club);
        // Grounded class C (Richard, 1 Oct: upgrades felt like a Le Mans prototype): a stock car takes 7-11 s to
        // 20 m/s and club kit clearly improves on it (the best class C kit about halves it).
        Check(stock>7 && stock<11,"Stock acceleration is gradual: 0-20 m/s "+stock.ToString("0.00")+" s",evidence);
        Check(club<stock*.7f,"Club equipment improves acceleration: "+club.ToString("0.00")+"s",evidence);
        var longStock=CarTuning.Stock;longStock.gearCeiling=34;
        float engineTop=longStock.drivetrain!=null?longStock.drivetrain.topSpeed:24;
        Check(Mathf.Abs(longStock.AttainableSpeed-engineTop)<.05f,"Long gearing does not invent engine power: "+engineTop.ToString("0.0")+"m/s",evidence);
        DriveModel.Forces(CarTuning.Stock,18,1,0,7,0,out float straight,out _);
        DriveModel.Forces(CarTuning.Stock,18,1,1,3.5f,0,out float corner,out float yaw);
        Check(corner<straight && yaw<1,"Turning and poor grip restrict usable power",evidence);
        var circuit=new Circuit();Check(circuit.length>600 && Circuit.Width==16,"Wide continuous circuit: "+circuit.length.ToString("0.0")+"m x16m",evidence);
        for(float d=0;d<circuit.length;d+=31) {float near=circuit.Nearest(circuit.At(d),out float offset);Check(Mathf.Abs(offset)<.01f&&Mathf.Abs(near-d)<.05f,"Track projection "+d,evidence);}
        // (since 24 Oct the game's only models are the Car Forge cars built to their briefs: CarBodies.Forge)
        var imported=new List<string>();
        foreach(string body in CarBodies.Forge)imported.AddRange(new[]{body,body+"_lod1",body+"_lod2"});
        foreach(string name in imported) {
            var model=Resources.Load<GameObject>("Cars/"+name);Check(model!=null,"Imported "+name,evidence);
            var instance=UnityEngine.Object.Instantiate(model);Bounds bounds=new Bounds();bool first=true;int verts=0;
            foreach(var r in instance.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            foreach(var f in instance.GetComponentsInChildren<MeshFilter>())verts+=f.sharedMesh.vertexCount;
            evidence.models.Add(name+" bounds="+bounds.size+" center="+bounds.center+" verts="+verts);
            string bodyId=name.Replace("_lod1","").Replace("_lod2","");bool longBody=Array.IndexOf(CarBodies.Townsfolk,bodyId)>=0||Array.IndexOf(CarBodies.Prizes,bodyId)>=0;
            // (Teasers of the top classes are true-to-type: the Regale is a 5 m Le Mans prototype, never raced yet.)
            float maxLength=Array.IndexOf(CarBodies.Teasers,bodyId)>=0?5.2f:longBody?4.8f:4;
            Check(bounds.size.x>1.5f&&bounds.size.x<2.5f&&bounds.size.z>2.5f&&bounds.size.z<maxLength,"Metre scale and upright "+name,evidence);
            if(name.StartsWith("compact_v1_lod"))Check(GlassTriangles(model)==GlassTriangles(Resources.Load<GameObject>("Cars/compact_v1")),"Glazing triangles preserved in "+name,evidence);
            var wheels=new WheelVisuals(instance);
            Check(wheels.pivots.Length==4,"Four animated axle pivots in imported "+name,evidence);
            float worstDrift=0,highest=0;bool allWheelsHaveGeometry=true;
            foreach(float steer in new[]{-25f,0,25f})for(int spin=0;spin<360;spin+=30) {
                wheels.Pose(spin,steer);
                foreach(var pivot in wheels.pivots) {
                    Bounds wheelBounds=new Bounds();bool firstWheel=true;
                    foreach(var renderer in pivot.GetComponentsInChildren<Renderer>()) {
                        if(firstWheel){wheelBounds=renderer.bounds;firstWheel=false;}else wheelBounds.Encapsulate(renderer.bounds);
                    }
                    allWheelsHaveGeometry&=!firstWheel;
                    worstDrift=Mathf.Max(worstDrift,Vector3.Distance(wheelBounds.center,pivot.position));
                    highest=Mathf.Max(highest,wheelBounds.max.y);
                }
            }
            Check(allWheelsHaveGeometry && worstDrift<.12f && highest<1.05f,"Wheel geometry stays on axles through spin and steering "+name+" drift="+worstDrift+" height="+highest,evidence);
            UnityEngine.Object.DestroyImmediate(instance);
        }
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../"));
        evidence.checks.AddRange(VehicleLightChecks.Run());
        evidence.checks.AddRange(ForgeLiveryChecks.Run());
        evidence.checks.AddRange(StopLookChecks.Run());
        evidence.checks.AddRange(ForgeRosterChecks.Run());
        evidence.checks.AddRange(PropForgeChecks.Run());
        evidence.checks.AddRange(CheckpointChecks.Run());
        evidence.checks.AddRange(ClassSortChecks.Run());
        evidence.checks.AddRange(RecoveryRejoinChecks.Run());
        evidence.checks.AddRange(WorldLookChecks.Run());
        evidence.checks.AddRange(GradeModelChecks.Run());
        evidence.checks.AddRange(RaceGridChecks.Run());
        evidence.checks.AddRange(ManualRaceStartChecks.Run());
        evidence.checks.AddRange(TechnicalCircuitChecks.Run());
        evidence.checks.AddRange(DriverPolicyChecks.Run());
        evidence.checks.AddRange(RouteProjectionChecks.Run());
        evidence.checks.AddRange(DraftingModelChecks.Run());
        evidence.checks.AddRange(DraftingIntegrationChecks.Run());
        evidence.checks.AddRange(DriverPlanningChecks.Run());
        evidence.checks.AddRange(DriverIdentityChecks.Run());
        evidence.checks.AddRange(RaceRosterChecks.Run());
        evidence.checks.AddRange(FieldRecipeChecks.Run());
        evidence.checks.AddRange(CarLinesChecks.Run());
        evidence.checks.AddRange(WorldStreamingChecks.Run());
        evidence.checks.AddRange(RaceTimingChecks.Run());
        evidence.checks.AddRange(PaceMarginChecks.Run());
        evidence.checks.AddRange(RacingLineChecks.Run());
        evidence.checks.AddRange(BayviewTransferBake.Run());
        evidence.checks.AddRange(RecoverySensorChecks.Run());
        evidence.checks.AddRange(AdventureChecks.Run(Path.Combine(root,"review/unity-04")));
        evidence.checks.AddRange(PitStopChecks.Run(Path.Combine(root,"review/unity-04")));
        evidence.checks.AddRange(ZipLineChecks.Run());
        evidence.checks.AddRange(PresentationChecks.Run());
        evidence.checks.AddRange(ContactChecks.Run());
        evidence.checks.AddRange(OptionsChecks.Run());
        evidence.checks.AddRange(GadgetChecks.Run());
        evidence.checks.AddRange(ScaleTestChecks.Run());
        evidence.checks.AddRange(QuarryHillsChecks.Run());
        evidence.checks.AddRange(PartDiscoveryChecks.Run());
        evidence.checks.AddRange(InterfaceChecks.Run());
        evidence.checks.AddRange(RacerChecks.Run());
        evidence.checks.AddRange(DrivetrainChecks.Run());
        evidence.checks.AddRange(HybridChecks.Run());
        evidence.checks.AddRange(VinelandsChecks.Run());
        evidence.checks.AddRange(RoadNetworkChecks.Run());
        evidence.checks.AddRange(WheelChecks.Run());
        evidence.checks.AddRange(DukeChallengeChecks.Run());
        evidence.checks.AddRange(SoundChecks.Run());
        Directory.CreateDirectory(Path.Combine(root,"review/unity-01"));
        File.WriteAllText(Path.Combine(root,"review/unity-01/editor-checks.json"),JsonUtility.ToJson(evidence,true));
        AssetDatabase.SaveAssets();
        string output=RaceTrial.Arg("--review-export",Path.Combine(root,"../little-road-unity-windows/Little Road Unity.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/CoastalTrial.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=developmentBuild?BuildOptions.Development:BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        File.Copy(Path.Combine(root,"THIRD_PARTY_ASSETS.md"),Path.Combine(Path.GetDirectoryName(output),"THIRD_PARTY_ASSETS.md"),true);
        Debug.Log("LITTLE_ROAD_BUILD_OK "+output+" checks="+evidence.checks.Count+" development="+developmentBuild);
    }
    static int GlassTriangles(GameObject model)
    {
        int count=0;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>()) {
            var materials=filter.GetComponent<Renderer>().sharedMaterials;
            for(int i=0;i<materials.Length;i++)if(materials[i]!=null&&materials[i].name.Contains("Glass"))count+=filter.sharedMesh.GetTriangles(i).Length/3;
        }
        return count;
    }
    // Real gears and shift cuts when the car has a drivetrain (docs/47), as the player feels it.
    static float TimeTo20(CarTuning t){var box=t.drivetrain!=null?new GearState(t.drivetrain):null;float speed=0,time=0;while(speed<20&&time<60){float engine=box!=null?box.Step(speed,1,1f/60):-1;DriveModel.Forces(t,speed,1,0,t.roadGrip,0,out float a,out _,engineAccel:engine);speed+=a/60;time+=1f/60;}return time;}
    static void Check(bool value,string name,Evidence e){if(!value)throw new Exception("Migration check failed: "+name);e.checks.Add(name);}
}
