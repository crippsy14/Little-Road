// Night light trails (LightTrails.cs): additive, unlit, vertex colour times an HDR tint, no depth write.
Shader "LittleRoad/LightTrail"
{
    Properties{_Tint("Tint",Color)=(1,1,1,1)}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline"}
        Blend One One ZWrite Off Cull Off
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct A{float4 pos:POSITION;half4 col:COLOR;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;half4 col:COLOR;float2 uv:TEXCOORD0;};
            V vert(A a){V v;v.pos=TransformObjectToHClip(a.pos.xyz);v.col=a.col;v.uv=a.uv;return v;}
            half4 frag(V v):SV_Target{half edge=1-abs(v.uv.y*2-1);return _Tint*v.col*v.col.a*edge*edge;}
            ENDHLSL
        }
    }
}
