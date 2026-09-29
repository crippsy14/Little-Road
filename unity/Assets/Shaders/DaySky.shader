Shader "LittleRoad/DaySky"
{
    // Procedural skybox for the day/night cycle; all parameters are globals from TimeOfDay (DaySky.hlsl).
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
            #include "DaySky.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Unused;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.direction=input.positionOS.xyz;return o;
            }
            float4 Frag(Varyings input):SV_Target{return float4(DaySkyColour(normalize(input.direction),true),1);}
            ENDHLSL
        }
    }
}
