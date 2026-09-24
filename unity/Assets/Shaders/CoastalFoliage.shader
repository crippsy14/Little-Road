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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
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
                Light sun=GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half facing=dot(normal,sun.direction);
                // A small wrapped term and transmitted backlight keep thin leaf
                // blades readable; opaque wood and terrain keep their own BRDF.
                half diffuse=saturate((facing+.32)/1.32);
                half transmission=saturate(-facing)*.16;
                half3 ambient=SampleSH(normal)*.82+SampleSH(half3(0,1,0))*.18;
                half variation=.94+.06*sin(dot(input.positionWS,half3(2.7,4.1,3.3)));
                half3 colour=_BaseColor.rgb*variation*(ambient+sun.color*(diffuse+transmission)*sun.shadowAttenuation);
                return half4(MixFog(colour,input.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        Pass
        {
            Name "LeafDepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment LeafDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            half4 LeafDepthNormalsFragment(Varyings input,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                input.normalWS*=IS_FRONT_VFACE(face,1,-1);
                half4 result;
                DepthNormalsFragment(input,result);
                return result;
            }
            ENDHLSL
        }
    }
}
