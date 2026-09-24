Shader "LittleRoad/CoastalSky"
{
    Properties
    {
        _SkyTex("Linear HDR panorama",2D)="white"{}
        _Exposure("Exposure",Float)=1
        _AzimuthOffset("Sampling azimuth offset in radians",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_SkyTex); SAMPLER(sampler_SkyTex);
            CBUFFER_START(UnityPerMaterial)
            float _Exposure, _AzimuthOffset;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.direction=input.positionOS.xyz;return o;
            }
            float4 Frag(Varyings input):SV_Target
            {
                float3 d=normalize(input.direction);
                float2 uv=float2(.5-(atan2(d.z,d.x)+_AzimuthOffset)/(2*PI),1-acos(clamp(d.y,-1,1))/PI);
                // Longitude wraps, but its sampling footprint must not jump
                // across the panorama and choose the coarsest mip at the seam.
                float2 dx=ddx(uv),dy=ddy(uv);dx.x-=round(dx.x);dy.x-=round(dy.x);
                float3 sky=SAMPLE_TEXTURE2D_GRAD(_SkyTex,sampler_SkyTex,uv,dx,dy).rgb*_Exposure;
                sky=lerp(unity_FogColor.rgb,sky,smoothstep(0,.20,d.y));
                return float4(sky,1);
            }
            ENDHLSL
        }
    }
}
