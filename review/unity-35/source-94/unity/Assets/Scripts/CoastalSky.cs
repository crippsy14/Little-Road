using UnityEngine;

namespace LittleRoad
{
    public static class CoastalSky
    {
        public static Vector3 Apply()
        {
            var texture=Resources.Load<Texture2D>("Sky/kloofendal_48d_partly_cloudy_puresky_2k");
            if(texture==null||!texture.isReadable)throw new System.InvalidOperationException("Coastal HDR sky must be imported with readable linear pixels.");
            var pixels=texture.GetPixels();int brightest=0;float peak=-1;
            for(int i=0;i<pixels.Length;i++) {
                float light=pixels[i].r*.2126f+pixels[i].g*.7152f+pixels[i].b*.0722f;
                if(light>peak){peak=light;brightest=i;}
            }
            float u=((brightest%texture.width)+.5f)/texture.width,v=((brightest/texture.width)+.5f)/texture.height;
            float textureAzimuth=(.5f-u)*Mathf.PI*2,elevation=(v-.5f)*Mathf.PI;
            const float desiredAzimuth=45*Mathf.Deg2Rad;
            var sky=new Material(Shader.Find("LittleRoad/CoastalSky")){name="Poly Haven coastal cloud sky"};
            sky.SetTexture("_SkyTex",texture);sky.SetFloat("_Exposure",.85f);sky.SetFloat("_AzimuthOffset",textureAzimuth-desiredAzimuth);RenderSettings.skybox=sky;
            Vector3 sunDirection=new Vector3(Mathf.Cos(desiredAzimuth)*Mathf.Cos(elevation),Mathf.Sin(elevation),Mathf.Sin(desiredAzimuth)*Mathf.Cos(elevation));
            Debug.Log("LITTLE_ROAD_SKY sunElevation="+elevation*Mathf.Rad2Deg+" peak="+peak+" texture="+texture.width+"x"+texture.height);
            return sunDirection;
        }
    }
}
