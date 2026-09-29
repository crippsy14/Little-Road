// Bunting and other light fabric strung between points: vertex-coloured, lit by the sky and sun,
// double-sided. Vertex alpha is how free a vertex is (0 on the string, 1 at a pennant's tip): free
// vertices sway with a slow breeze and flutter quickly, each pennant out of step with its neighbours.
Shader "LittleRoad/Bunting"
{
    Properties
    {
        _Sway("Sway (m)",Float)=.13
        _Flutter("Flutter (m)",Float)=.05
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off
        Pass
        {
            Name "BuntingForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Sway,_Flutter;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;half4 color:COLOR;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
            float3 Wind(float3 w,float free)
            {
                float phase=w.x*.37+w.z*.53;
                float3 breeze=float3(sin(_Time.y*1.2+phase),0,cos(_Time.y*.9+phase*1.3))*_Sway;
                float3 flutter=float3(sin(_Time.y*7.3+phase*3.1),sin(_Time.y*9.1+phase*2.3)*.4,cos(_Time.y*6.7+phase*2.7))*_Flutter;
                return w+(breeze+flutter)*free+float3(0,.03,0)*free*sin(_Time.y*2.1+phase);
            }
            Varyings Vert(Attributes input)
            {
                Varyings o;float3 w=Wind(TransformObjectToWorld(input.positionOS.xyz),input.color.a);
                o.positionWS=w;o.positionCS=TransformWorldToHClip(w);o.color=input.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings i,bool front:SV_IsFrontFace):SV_Target
            {
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                // Thin fabric: lit by the sky from both sides, and the sun glows through it.
                half3 light=SampleSH(half3(0,1,0))*1.4h+sun.color*(.55h+.25h*sun.shadowAttenuation);
                half3 colour=SRGBToLinear(i.color.rgb)*light;
                return half4(ApplyHeightFog(MixFog(colour,i.fog),i.positionWS),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Sway,_Flutter;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;half4 color:COLOR;};
            float3 Wind(float3 w,float free)
            {
                float phase=w.x*.37+w.z*.53;
                float3 breeze=float3(sin(_Time.y*1.2+phase),0,cos(_Time.y*.9+phase*1.3))*_Sway;
                float3 flutter=float3(sin(_Time.y*7.3+phase*3.1),sin(_Time.y*9.1+phase*2.3)*.4,cos(_Time.y*6.7+phase*2.7))*_Flutter;
                return w+(breeze+flutter)*free+float3(0,.03,0)*free*sin(_Time.y*2.1+phase);
            }
            float4 Vert(Attributes input):SV_POSITION{return TransformWorldToHClip(Wind(TransformObjectToWorld(input.positionOS.xyz),input.color.a));}
            half Frag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
