Shader "LittleRoad/CoastalFoliage"
{
    Properties
    {
        _BaseColor("Leaf colour",Color)=(.3,.42,.18,1)
        _BaseMap("Shadow base",2D)="white"{}
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=0
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "LeafForward"
            Tags {"LightMode"="UniversalForward"}
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=p.positionCS;output.positionWS=p.positionWS;output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.fog=ComputeFogFactor(p.positionCS.z);return output;
            }
            half4 Frag(Varyings input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 normal=normalize(input.normalWS)*IS_FRONT_VFACE(face,1,-1);
                // Position overload: applies the drifting cloud cookie (WorldLook) too.
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS),input.positionWS,half4(1,1,1,1));
                half facing=dot(normal,sun.direction);
                // A small wrapped term and transmitted backlight keep thin leaf
                // blades readable; opaque wood and terrain keep their own BRDF.
                half diffuse=saturate((facing+.32)/1.32);
                half transmission=saturate(-facing)*.16;
                half3 ambient=SampleSH(normal)*.82+SampleSH(half3(0,1,0))*.18;
                half variation=.94+.06*sin(dot(input.positionWS,half3(2.7,4.1,3.3)));
                half3 colour=_BaseColor.rgb*variation*(ambient+sun.color*(diffuse+transmission)*sun.shadowAttenuation);
                return half4(ApplyHeightFog(MixFog(colour,input.fog),input.positionWS),1);
            }
            ENDHLSL
        }
        // Own shadow/depth passes sharing UnityPerMaterial (HLSLINCLUDE): the borrowed Lit passes and
        // LitInput.hlsl declared Lit's material layout, which made foliage SRP-batcher incompatible.
        // Leaves are double-sided (Cull [_Cull], 0 by default), as the Lit passes drew them.
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment Nothing
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;float3 _LightPosition;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);float3 p=TransformObjectToWorld(input.positionOS.xyz);float3 n=TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 l=normalize(_LightPosition-p);
                #else
                    float3 l=_LightDirection;
                #endif
                float4 c=TransformWorldToHClip(ApplyShadowBias(p,n,l));
                #if UNITY_REVERSED_Z
                    c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
                #else
                    c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return c;
            }
            half4 Nothing():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment Nothing
            #pragma multi_compile_instancing
            struct Attributes{float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 DepthVert(Attributes input):SV_POSITION{UNITY_SETUP_INSTANCE_ID(input);return TransformObjectToHClip(input.positionOS.xyz);}
            half4 Nothing():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "LeafDepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;half3 normalWS:TEXCOORD0;};
            Varyings NormalsVert(Attributes input){UNITY_SETUP_INSTANCE_ID(input);Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);return o;}
            half4 NormalsFrag(Varyings i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(n);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                    return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
