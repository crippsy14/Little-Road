// Soft puffs (PuffPool: chimney smoke, kicked-up dust): alpha-blended, no depth write, lit softly by the sky and sun.
// Its own shader because builds strip URP Lit's transparent variant when no shipped material uses it, so a runtime
// "transparent" Lit puff drew as a solid ball (street review, 5 Oct). _BaseColor's alpha fades each puff.
Shader "LittleRoad/SoftPuff"
{
    Properties{_BaseColor("Colour",Color)=(1,1,1,1)}
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
            struct A{float4 pos:POSITION;float3 normal:NORMAL;};
            struct V{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
            V vert(A a){V v;v.world=TransformObjectToWorld(a.pos.xyz);v.pos=TransformWorldToHClip(v.world);v.normal=TransformObjectToWorldNormal(a.normal);return v;}
            half4 frag(V v):SV_Target
            {
                half3 n=normalize(v.normal);Light sun=GetMainLight();
                // Wrapped, gentle light: a puff is brighter on its sunny side but never shows a hard terminator.
                half wrap=saturate(dot(n,sun.direction)*.5+.5);
                half3 light=SampleSH(n)*1.2+sun.color*wrap*.45;
                // Thinner at the silhouette, so the sphere reads as a soft blob, not a ball.
                half rim=saturate(dot(n,normalize(GetWorldSpaceViewDir(v.world))));
                return half4(_BaseColor.rgb*light,_BaseColor.a*rim*rim);
            }
            ENDHLSL
        }
    }
}
