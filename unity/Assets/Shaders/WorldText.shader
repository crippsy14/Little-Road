Shader "LittleRoad/WorldText"
{
    Properties {_MainTex("Font atlas",2D)="white"{} }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="SRPDefaultUnlit"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 colour:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 colour:COLOR;};
            Varyings Vert(Attributes input){Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);output.uv=input.uv;output.colour=input.colour;return output;}
            half4 Frag(Varyings input):SV_Target {half4 colour=input.colour;colour.a*=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;return colour;}
            ENDHLSL
        }
    }
}
