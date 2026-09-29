// Near-camera meadow grass and wildflowers (GrassField): instanced clumps that sway in the wind,
// shrink away at the edge of the field radius, and light like leaves (wrapped sun, backlight,
// shadows and drifting cloud cookies). Vertex colour: rgb = flower head colour, a = 1 on heads
// (0 on blades); uv.y = height up the blade (0 root, 1 tip).
Shader "LittleRoad/Grass"
{
    Properties
    {
        _Base("Blade root",Color)=(.18,.30,.12,1)
        _Tip("Blade tip",Color)=(.50,.66,.28,1)
        _Dry("Dry tip",Color)=(.70,.66,.36,1)
        _Radius("Field radius",Float)=44
        _Wind("Wind",Float)=1
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Base,_Tip,_Dry;float _Radius,_Wind;
            CBUFFER_END
            TEXTURE2D(_KitNoise);SAMPLER(sampler_KitNoise);
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            // Sway and fade in world space; returns the world position.
            float3 GrassWorld(Attributes input,out float h)
            {
                h=input.uv.y;float3 root=TransformObjectToWorld(float3(0,0,0));
                float fade=1-smoothstep(_Radius-12,_Radius,distance(root.xz,_WorldSpaceCameraPos.xz));
                float3 os=input.positionOS.xyz;os.y*=fade;os.xz*=lerp(.4,1,fade);
                float3 w=TransformObjectToWorld(os);
                float t=_Time.y;float gust=sin(t*.7+root.x*.013+root.z*.011)*.5+.5;
                float2 sway=float2(sin(t*1.9+root.x*.37+root.z*.21),cos(t*1.6+root.x*.19-root.z*.33))*(.05+.09*gust)*_Wind;
                w.xz+=sway*h*h*fade;
                return w;
            }
        ENDHLSL
        Pass
        {
            Name "GrassForward"
            Tags {"LightMode"="UniversalForward"}
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half4 color:COLOR;half h:TEXCOORD2;half fog:TEXCOORD3;half patch:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
            Varyings Vert(Attributes input)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);
                float h;float3 w=GrassWorld(input,h);
                o.positionWS=w;o.positionCS=TransformWorldToHClip(w);o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.color=input.color;o.h=h;o.fog=ComputeFogFactor(o.positionCS.z);
                float3 root=TransformObjectToWorld(float3(0,0,0));
                // Meadow patches (matches the terrain's 8-30 m breakup): some clumps drier, some lusher.
                o.patch=SAMPLE_TEXTURE2D_LOD(_KitNoise,sampler_KitNoise,root.xz*.021+.37,0).r;
                return o;
            }
            half4 Frag(Varyings i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
                // Blades: dark at the root, lighter at the tip; dry patches turn the tips straw.
                half3 tip=lerp(_Tip.rgb,_Dry.rgb,smoothstep(.55,.8,i.patch));
                half3 albedo=lerp(_Base.rgb,tip,saturate(i.h*1.15))*lerp(.9,1.08,i.patch);
                albedo=lerp(albedo,i.color.rgb,i.color.a);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS),i.positionWS,half4(1,1,1,1));
                half facing=dot(n,sun.direction);
                half diffuse=saturate((facing+.45)/1.45);half transmission=saturate(-facing)*.22*i.h;
                half3 ambient=SampleSH(half3(0,1,0))*lerp(.55,1,i.h);
                half3 colour=albedo*(ambient+sun.color*(diffuse+transmission)*sun.shadowAttenuation);
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
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment Nothing
            #pragma multi_compile_instancing
            float4 DepthVert(Attributes input):SV_POSITION{UNITY_SETUP_INSTANCE_ID(input);float h;return TransformWorldToHClip(GrassWorld(input,h));}
            half4 Nothing():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct Varyings{float4 positionCS:SV_POSITION;half3 normalWS:TEXCOORD0;};
            Varyings NormalsVert(Attributes input){UNITY_SETUP_INSTANCE_ID(input);Varyings o;float h;o.positionCS=TransformWorldToHClip(GrassWorld(input,h));o.normalWS=half3(0,1,0);return o;}
            half4 NormalsFrag(Varyings i):SV_Target
            {
                float3 n=normalize(i.normalWS);
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
