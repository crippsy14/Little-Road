using System.IO;
using UnityEditor;
using UnityEngine;

public static class CliffStudyImport
{
    public static void Prepare()
    {
        const string root="Assets/Resources/CoastStudy/";
        for(int lod=0;lod<3;lod++) {
            var model=(ModelImporter)AssetImporter.GetAtPath(root+"cliff_scan_lod"+lod+".fbx");
            model.globalScale=1;model.isReadable=true;model.importAnimation=false;
            model.materialImportMode=ModelImporterMaterialImportMode.None;model.SaveAndReimport();
        }
        foreach(string file in Directory.GetFiles(root)) {
            if(file.EndsWith(".meta"))continue;
            var importer=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(importer==null)continue;
            bool normal=file.Contains("_nor_gl_");bool rough=file.Contains("_rough_");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=file.Contains("_diff_");importer.isReadable=rough;
            importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=8;
            importer.textureCompression=rough?TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        var roughness=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"coastal_cliff_01_rough_2k.exr");
        var packed=new Texture2D(roughness.width,roughness.height,TextureFormat.RGBA32,false,true);
        var pixels=roughness.GetPixels();
        for(int i=0;i<pixels.Length;i++)pixels[i]=new Color(0,0,0,1-Mathf.Clamp01(pixels[i].r));
        packed.SetPixels(pixels);packed.Apply();
        string path=root+"cliff_metal_smoothness.png";File.WriteAllBytes(path,packed.EncodeToPNG());Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        var mask=(TextureImporter)AssetImporter.GetAtPath(path);mask.sRGBTexture=false;mask.alphaSource=TextureImporterAlphaSource.FromInput;
        mask.maxTextureSize=2048;mask.mipmapEnabled=true;mask.anisoLevel=8;mask.textureCompression=TextureImporterCompression.CompressedHQ;mask.SaveAndReimport();
        // A serialized reference preserves the exact Lit keyword combination in
        // the player build. Runtime-only keywords can select a stripped variant.
        string materialPath=root+"CoastalCliff.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetFloat("_WorkflowMode",1);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.55f);
        material.SetColor("_BaseColor",new Color(1.1f,1.08f,1.02f));
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"coastal_cliff_01_diff_2k.jpg"));
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"coastal_cliff_01_nor_gl_2k.exr"));
        material.SetFloat("_BumpScale",.6f);material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.enableInstancing=true;EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
    }
}
