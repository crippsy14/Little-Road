// Rivers and lakes. Depth under the surface comes from the scene depth texture, so every
// shoreline gets a clear shallow edge, a foam line and a gradual turn to deep teal without any
// authored masks. Two scrolling noise normals give ripples (sun glints and sky reflection come
// from URP's PBR lighting). _Flow moves the ripples along the mesh's UV v (downstream).
Shader "LittleRoad/InlandWater"
{
    Properties
    {
        _Shallow("Shallow",Color)=(.30,.52,.46,1)
        _Deep("Deep",Color)=(.07,.22,.26,1)
        _Foam("Foam",Color)=(.93,.96,.95,1)
        _Flow("Flow speed",Float)=.1
        _DepthFade("Metres to deep colour",Float)=2.2
        _Ripple("Ripple strength",Float)=.35
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Name "InlandWaterForward"
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_KitNoise);SAMPLER(sampler_KitNoise);
            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow,_Deep,_Foam;float _Flow,_DepthFade,_Ripple;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float2 uv:TEXCOORD1;float4 screen:TEXCOORD2;half fog:TEXCOORD3;};
            Varyings Vert(Attributes input)
            {
                Varyings o;VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.uv=input.uv;o.screen=ComputeScreenPos(p.positionCS);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half H(float2 q){return SAMPLE_TEXTURE2D(_KitNoise,sampler_KitNoise,q).r;}
            half4 Frag(Varyings i):SV_Target
            {
                float3 p=i.positionWS;float t=_Time.y;
                // Depth of water along the view ray, turned into a vertical depth estimate.
                float2 suv=i.screen.xy/i.screen.w;float scene=LinearEyeDepth(SampleSceneDepth(suv),_ZBufferParams);
                float surface=LinearEyeDepth(i.positionCS.z,_ZBufferParams);float3 view=GetWorldSpaceNormalizeViewDir(p);
                float depth=max(0,scene-surface)*max(.25,abs(view.y));
                half deep=saturate(depth/_DepthFade);
                // Ripples: two noise layers drifting along the flow, as a derivative normal.
                float2 flow=float2(0,-t*_Flow);
                float2 a=p.xz*.035+flow*.3,b=p.xz*.09+flow*.7+float2(.37,.11);
                float e=.02;
                float hx=(H(a+float2(e,0))-H(a-float2(e,0)))+(H(b+float2(e,0))-H(b-float2(e,0)))*.6;
                float hz=(H(a+float2(0,e))-H(a-float2(0,e)))+(H(b+float2(0,e))-H(b-float2(0,e)))*.6;
                // Gentle ripples, calmer with distance (no glittering mosaic far out).
                float far=saturate(distance(p,_WorldSpaceCameraPos)/120);float amp=_Ripple*4*lerp(1,.25,far);
                half3 n=normalize(half3(-hx*amp,1,-hz*amp));
                // Foam line where the water meets the bank.
                half foam=(1-smoothstep(.05,.35,depth))*smoothstep(.35,.7,H(p.xz*.4+float2(t*.03,0)))+(1-smoothstep(.0,.08,depth))*.6;
                SurfaceData s=(SurfaceData)0;
                s.albedo=lerp(lerp(_Shallow.rgb,_Deep.rgb,deep),_Foam.rgb,saturate(foam));s.smoothness=lerp(.93,.4,saturate(foam));s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
                InputData d=(InputData)0;d.positionWS=p;d.positionCS=i.positionCS;d.normalWS=n;d.viewDirectionWS=view;d.shadowCoord=TransformWorldToShadowCoord(p);
                d.bakedGI=SampleSH(n);d.shadowMask=half4(1,1,1,1);d.normalizedScreenSpaceUV=suv;
                half4 c=UniversalFragmentPBR(d,s);
                // Clear at the very edge, opaque once deep; foam is always opaque.
                c.a=saturate(max(lerp(.35,.96,smoothstep(0,1.2,depth)),saturate(foam)));
                c.rgb=ApplyHeightFog(MixFog(c.rgb,i.fog),p);return c;
            }
            ENDHLSL
        }
    }
}
