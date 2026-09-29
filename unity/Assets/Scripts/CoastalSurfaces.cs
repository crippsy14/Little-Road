using UnityEngine;

namespace LittleRoad
{
    public static class CoastalSurfaces
    {
        public static Material Runoff()
        {
            var material=new Material(Shader.Find("LittleRoad/CoastalTerrain")){name="Fine weathered limestone shoulder"};Apply(material);
            material.SetFloat("_RockOnly",1);material.SetFloat("_RockScale",1.4f);material.SetFloat("_NormalStrength",.22f);
            material.SetColor("_RockColor",new Color(1.18f,1.17f,1.10f));
            WorldLook.Terrain(material,WorldLook.RunoffStylise);material.SetFloat("_Bands",0);material.SetColor("_ToyRockA",new Color(.90f,.84f,.70f));return material;
        }
        public static void Apply(Material material)
        {
            Assign(material,"Rock","rock_08");Assign(material,"Grass","grass_ground");
            material.SetFloat("_RockScale",.32f);material.SetFloat("_GrassScale",1/2.51f);material.SetFloat("_NormalStrength",.7f);
        }
        static void Assign(Material material,string slot,string asset)
        {
            foreach(var map in new[]{("Albedo","diff"),("Normal","nor_gl"),("Mask","arm")}) {
                string path="Surfaces/"+asset+"/"+asset+"_"+map.Item2+"_2k";
                var texture=Resources.Load<Texture2D>(path);
                if(texture==null)throw new System.InvalidOperationException("Missing credited surface map "+path);
                material.SetTexture("_"+slot+map.Item1,texture);
            }
        }
    }
}
