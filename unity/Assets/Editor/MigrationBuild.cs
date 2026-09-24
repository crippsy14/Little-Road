using System;
using System.Collections.Generic;
using System.IO;
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
        CliffStudyImport.Prepare();
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
        PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Little Road migration trial").AddComponent<RaceTrial>();
        Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/Scenes/CoastalTrial.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/CoastalTrial.unity",true)};
        Check(pipeline!=null,"URP pipeline assigned",evidence);
        Check(EquipmentCatalog.All.parts.Length==21 && EquipmentCatalog.All.slots.Length==7,"Preserved 21 parts across seven slots",evidence);
        var stockFit=EquipmentCatalog.StockLoadout();
        foreach(var part in EquipmentCatalog.All.parts) {
            var fit=new Dictionary<string,string>(stockFit);fit[part.slot]=part.id;var stats=EquipmentCatalog.Stats(fit);
            Check(stats.mass>0 && stats.power>0 && stats.gearCeiling>0 && stats.roadGrip>0,"Valid single-part loadout "+part.id,evidence);
        }
        Check(EquipmentCatalog.Reward(24)==100&&EquipmentCatalog.Reward(1)==500&&EquipmentCatalog.Reward(0)==0,"All valid finishers retain C-class reward schedule",evidence);
        Directory.CreateDirectory("Assets/Resources/ShaderReferences");
        foreach(string shaderName in new[]{"Universal Render Pipeline/Lit","Skybox/Procedural","LittleRoad/CoastalSky","LittleRoad/CoastalTerrain","LittleRoad/CoastalFoliage","LittleRoad/CoastalWater","LittleRoad/WorldText"}) {
            var shader=Shader.Find(shaderName);Check(shader!=null,"Required shader "+shaderName,evidence);
            string path="Assets/Resources/ShaderReferences/"+shaderName.Replace('/','_')+".mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)AssetDatabase.CreateAsset(new Material(shader),path);
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
        Check(stock>4 && stock<8,"Stock acceleration is gradual: 0â€“20m/s "+stock.ToString("0.00")+"s",evidence);
        Check(club<stock*.7f,"Club equipment improves acceleration: "+club.ToString("0.00")+"s",evidence);
        var longStock=CarTuning.Stock;longStock.gearCeiling=34;
        Check(Mathf.Abs(longStock.AttainableSpeed-24)<.02f,"Long gearing does not invent engine power",evidence);
        DriveModel.Forces(CarTuning.Stock,18,1,0,7,0,out float straight,out _);
        DriveModel.Forces(CarTuning.Stock,18,1,1,3.5f,0,out float corner,out float yaw);
        Check(corner<straight && yaw<1,"Turning and poor grip restrict usable power",evidence);
        var circuit=new Circuit();Check(circuit.length>600 && Circuit.Width==16,"Wide continuous circuit: "+circuit.length.ToString("0.0")+"m x16m",evidence);
        for(float d=0;d<circuit.length;d+=31) {float near=circuit.Nearest(circuit.At(d),out float offset);Check(Mathf.Abs(offset)<.01f&&Mathf.Abs(near-d)<.05f,"Track projection "+d,evidence);}
        var imported=new List<string>{"little_car","coupe_car","utility_car","formula_car","compact_v1","compact_v1_lod1","compact_v1_lod2"};
        var generated=new List<string>(CarBodies.Family);generated.AddRange(CarBodies.Sporty);generated.Add(CarBodies.Flagship);generated.Add(CarBodies.Starter);
        foreach(string body in generated)imported.AddRange(new[]{body,body+"_lod1",body+"_lod2"});
        foreach(string name in imported) {
            var model=Resources.Load<GameObject>("Cars/"+name);Check(model!=null,"Imported "+name,evidence);
            var instance=UnityEngine.Object.Instantiate(model);Bounds bounds=new Bounds();bool first=true;int verts=0;
            foreach(var r in instance.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
            foreach(var f in instance.GetComponentsInChildren<MeshFilter>())verts+=f.sharedMesh.vertexCount;
            evidence.models.Add(name+" bounds="+bounds.size+" center="+bounds.center+" verts="+verts);
            Check(bounds.size.x>1.5f&&bounds.size.x<2.5f&&bounds.size.z>2.5f&&bounds.size.z<4,"Metre scale and upright "+name,evidence);
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
        CollisionEnvelopeReview.Run(Path.Combine(root,"review/unity-01/collision-envelope.json"));
        evidence.checks.AddRange(VehicleLightChecks.Run());
        evidence.checks.AddRange(CheckpointChecks.Run());
        evidence.checks.AddRange(RaceGridChecks.Run());
        evidence.checks.AddRange(ManualRaceStartChecks.Run());
        evidence.checks.AddRange(OpeningCaptureChecks.Run());
        evidence.checks.AddRange(PassingTargetChecks.Run());
        evidence.checks.AddRange(FollowingFeasibilityChecks.Run());
        evidence.checks.AddRange(FollowingFallbackSelectionChecks.Run());
        evidence.checks.AddRange(SharedTrafficPreparationChecks.Run());
        evidence.checks.AddRange(GradeCourseChecks.Run());
        evidence.checks.AddRange(TechnicalCircuitChecks.Run());
        evidence.checks.AddRange(DriverPolicyChecks.Run());
        evidence.checks.AddRange(ContextDriverChecks.Run());
        evidence.checks.AddRange(RouteProjectionChecks.Run());
        evidence.checks.AddRange(ForecastEnvelopeChecks.Run());
        evidence.checks.AddRange(ForecastPathCacheChecks.Run());
        evidence.checks.AddRange(PreparedRankingChecks.Run());
        evidence.checks.AddRange(ForecastWorkerChecks.Run());
        evidence.checks.AddRange(DynamicForecastWorkerChecks.Run());
        evidence.checks.AddRange(StaggeredPlanClockChecks.Run());
        evidence.checks.AddRange(ForecastReuseChecks.Run());
        evidence.checks.AddRange(ContinuationForecastChecks.Run());
        evidence.checks.AddRange(RearPressureChecks.Run());
        evidence.checks.AddRange(DraftingModelChecks.Run());
        evidence.checks.AddRange(DraftingIntegrationChecks.Run());
        evidence.checks.AddRange(OpenSpacePriorityChecks.Run());
        evidence.checks.AddRange(PassingProgressChecks.Run());
        evidence.checks.AddRange(PathSpeedChecks.Run());
        evidence.checks.AddRange(DynamicRejoinChecks.Run());
        evidence.checks.AddRange(LocalOriginCurvatureChecks.Run());
        evidence.checks.AddRange(DynamicTrafficChecks.Run());
        evidence.checks.AddRange(RouteAdjacencyChecks.Run());
        evidence.checks.AddRange(DynamicRejoinWitness.Run());
        evidence.checks.AddRange(CoastingEnvelopeChecks.Run());
        evidence.checks.AddRange(DriverPlanningChecks.Run());
        evidence.checks.AddRange(DriverIdentityChecks.Run());
        evidence.checks.AddRange(RaceRosterChecks.Run());
        evidence.checks.AddRange(RaceTimingChecks.Run());
        evidence.checks.AddRange(TrafficPathChecks.Run());
        evidence.checks.AddRange(TrafficCoastChecks.Run());
        evidence.checks.AddRange(TrafficProgressChecks.Run());
        evidence.checks.AddRange(PathAwareLeaderChecks.Run());
        evidence.checks.AddRange(UnresolvedHoldChecks.Run());
        evidence.checks.AddRange(PassCommitmentChecks.Run());
        evidence.checks.AddRange(CornerAwarePassChecks.Run());
        evidence.checks.AddRange(GapSeekingChecks.Run());
        evidence.checks.AddRange(PaceMarginChecks.Run());
        if(RaceTrial.Has("--traffic-coast-input"))TrafficCoastReplay.Run();
        evidence.checks.AddRange(RacingLineChecks.Run());
        evidence.checks.AddRange(BayviewTransferBake.Run());
        evidence.checks.AddRange(RecoverySensorChecks.Run());
        evidence.checks.AddRange(TrafficPolicyChecks.Run());
        evidence.checks.AddRange(SweptGeometryReuseChecks.Run());
        evidence.checks.AddRange(AdventureChecks.Run(Path.Combine(root,"review/unity-04")));
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
    static float TimeTo20(CarTuning t){float speed=0,time=0;while(speed<20&&time<60){DriveModel.Forces(t,speed,1,0,t.roadGrip,0,out float a,out _);speed+=a/60;time+=1f/60;}return time;}
    static void Check(bool value,string name,Evidence e){if(!value)throw new Exception("Migration check failed: "+name);e.checks.Add(name);}
}
