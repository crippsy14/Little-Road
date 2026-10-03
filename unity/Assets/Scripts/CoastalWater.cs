using UnityEngine;
using UnityEngine.Rendering;

namespace LittleRoad
{
    // Visual sea only. Vehicle buoyancy and water traversal are separate,
    // unimplemented equipment/gameplay requirements.
    public sealed class CoastalWater:MonoBehaviour
    {
        public Material Surface {get;private set;}
        public static void Create(Transform parent)
        {
            var go=new GameObject("Animated coastal sea",typeof(MeshFilter),typeof(MeshRenderer),typeof(CoastalWater));
            go.transform.SetParent(parent,false);
            go.GetComponent<CoastalWater>().Build();
        }
        void Build()
        {
            const int count=257;var vertices=new Vector3[count*count];var normals=new Vector3[vertices.Length];
            var triangles=new int[(count-1)*(count-1)*6];
            // With Quarry Hills (docs/59) the sea reaches past its coast, 4.5 km west of Bayview.
            float reach=QuarryHills.Enabled?4800:1750;float Axis(int i){float t=(i-128)/128f;return Mathf.Sign(t)*Mathf.Pow(Mathf.Abs(t),1.65f)*reach;}
            for(int z=0;z<count;z++)for(int x=0;x<count;x++) {
                int i=z*count+x;vertices[i]=new Vector3(Axis(x),CoastalLandscape.SeaLevel,Axis(z));normals[i]=Vector3.up;
                if(x<count-1&&z<count-1){int k=(z*(count-1)+x)*6;triangles[k]=i;triangles[k+1]=i+count;triangles[k+2]=i+1;triangles[k+3]=i+1;triangles[k+4]=i+count;triangles[k+5]=i+count+1;}
            }
            var mesh=new Mesh{name="Coastal sea grid",indexFormat=IndexFormat.UInt32,vertices=vertices,normals=normals,triangles=triangles};
            mesh.RecalculateBounds();var bounds=mesh.bounds;bounds.Expand(new Vector3(0,1,0));mesh.bounds=bounds;
            GetComponent<MeshFilter>().sharedMesh=mesh;
            Surface=new Material(Shader.Find("LittleRoad/CoastalWater")){name="Moving coastal water"};
            var sky=RenderSettings.skybox;
            // Photo sky (Cedar): reflect the panorama. The town's procedural sky is reflected from DaySky.hlsl.
            if(sky!=null&&sky.HasProperty("_SkyTex")){Surface.SetTexture("_SkyTex",sky.GetTexture("_SkyTex"));Surface.SetFloat("_AzimuthOffset",sky.GetFloat("_AzimuthOffset"));}
            var renderer=GetComponent<MeshRenderer>();renderer.sharedMaterial=Surface;renderer.shadowCastingMode=ShadowCastingMode.Off;
            Debug.Log("LITTLE_ROAD_WATER vertices="+vertices.Length+" triangles="+triangles.Length/3+" collider=False shaderSupported="+Surface.shader.isSupported);
        }
    }
}
