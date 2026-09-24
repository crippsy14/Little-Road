Shader "LittleRoad/CoastalTerrain"
{
    Properties
    {
        _NoiseTex("Original surface grain",2D)="white"{}
        _GrassColor("Meadow",Color)=(.38,.47,.24,1)
        _RockColor("Limestone",Color)=(.61,.57,.47,1)
        _DryColor("Dry grass",Color)=(.48,.49,.29,1)
        _RockAlbedo("Rock colour",2D)="white"{}
        _RockNormal("Rock normal",2D)="bump"{}
        _RockMask("Rock AO roughness metal",2D)="white"{}
        _GrassAlbedo("Grass colour",2D)="white"{}
        _GrassNormal("Grass normal",2D)="bump"{}
        _GrassMask("Grass AO roughness metal",2D)="white"{}
        _RockScale("Rock repeats per metre",Float)=.32
        _GrassScale("Grass repeats per metre",Float)=.3984
        _NormalStrength("Surface relief",Float)=.7
        _RockOnly("Expose rock on every face",Float)=0
        _BaseMap("Shadow pass base",2D)="white"{}
        _BaseColor("Shadow pass colour",Color)=(1,1,1,1)
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        Pass
        {
            Name "CoastalForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_NoiseTex);SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_RockAlbedo);SAMPLER(sampler_RockAlbedo);
            TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
            TEXTURE2D(_RockMask);SAMPLER(sampler_RockMask);
            TEXTURE2D(_GrassAlbedo);SAMPLER(sampler_GrassAlbedo);
            TEXTURE2D(_GrassNormal);SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_GrassMask);SAMPLER(sampler_GrassMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _GrassColor,_RockColor,_DryColor;
                float _RockScale,_GrassScale,_NormalStrength,_RockOnly;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes input)
            {
                Varyings output;VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS=p.positionCS;output.positionWS=p.positionWS;output.normalWS=TransformObjectToWorldNormal(input.normalOS);
                output.fog=ComputeFogFactor(p.positionCS.z);return output;
            }
            half4 TriSample(TEXTURE2D_PARAM(map,sampler_map),float3 p,half3 weights,half3 signs)
            {
                return SAMPLE_TEXTURE2D(map,sampler_map,float2(-p.z*signs.x,p.y))*weights.x+
                    SAMPLE_TEXTURE2D(map,sampler_map,float2(p.x,-p.z*signs.y))*weights.y+
                    SAMPLE_TEXTURE2D(map,sampler_map,float2(p.x*signs.z,p.y))*weights.z;
            }
            half3 TriRelief(TEXTURE2D_PARAM(map,sampler_map),float3 p,half3 weights,half3 signs)
            {
                half3 x=UnpackNormal(SAMPLE_TEXTURE2D(map,sampler_map,float2(-p.z*signs.x,p.y)));
                half3 y=UnpackNormal(SAMPLE_TEXTURE2D(map,sampler_map,float2(p.x,-p.z*signs.y)));
                half3 z=UnpackNormal(SAMPLE_TEXTURE2D(map,sampler_map,float2(p.x*signs.z,p.y)));
                x.xy/=max(x.z,.3);y.xy/=max(y.z,.3);z.xy/=max(z.z,.3);
                return half3(0,x.y,-x.x*signs.x)*weights.x+half3(y.x,0,-y.y*signs.y)*weights.y+half3(z.x*signs.z,z.y,0)*weights.z;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 normal=normalize(input.normalWS);half3 weights=pow(abs(normal),4);weights/=max(dot(weights,1),.0001);
                half3 signs=half3(normal.x<0?-1:1,normal.y<0?-1:1,normal.z<0?-1:1);
                float3 p=input.positionWS;
                half grain=SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.zy*.18).r*weights.x+
                    SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.18).r*weights.y+
                    SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xy*.18).r*weights.z;
                half broad=SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.023).r;
                half meadow=smoothstep(.60,.88,normal.y+(broad-.95)*.25);
                meadow*=smoothstep(-2,1,p.y)*(1-_RockOnly);
                half3 rock=TriSample(TEXTURE2D_ARGS(_RockAlbedo,sampler_RockAlbedo),p*_RockScale,weights,signs).rgb*_RockColor.rgb;
                half3 turf=TriSample(TEXTURE2D_ARGS(_GrassAlbedo,sampler_GrassAlbedo),p*_GrassScale,weights,signs).rgb;
                turf*=lerp(_GrassColor.rgb,_DryColor.rgb,saturate((broad-.93)*7));
                half2 rockMask=TriSample(TEXTURE2D_ARGS(_RockMask,sampler_RockMask),p*_RockScale,weights,signs).rg;
                half2 grassMask=TriSample(TEXTURE2D_ARGS(_GrassMask,sampler_GrassMask),p*_GrassScale,weights,signs).rg;
                half3 relief=lerp(TriRelief(TEXTURE2D_ARGS(_RockNormal,sampler_RockNormal),p*_RockScale,weights,signs),
                    TriRelief(TEXTURE2D_ARGS(_GrassNormal,sampler_GrassNormal),p*_GrassScale,weights,signs),meadow)*_NormalStrength;
                relief-=normal*dot(relief,normal);normal=normalize(normal+relief);
                half wet=1-smoothstep(-11.8,-8.5,p.y);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=lerp(rock,turf,meadow)*lerp(.96,1.04,grain)*lerp(1,.66,wet);
                surface.normalTS=half3(0,0,1);surface.smoothness=lerp(1-lerp(rockMask.y,grassMask.y,meadow),.46,wet);
                surface.occlusion=lerp(.7,1,lerp(rockMask.x,grassMask.x,meadow));surface.alpha=1;
                InputData data=(InputData)0;data.positionWS=p;data.positionCS=input.positionCS;data.normalWS=normal;
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);data.shadowCoord=TransformWorldToShadowCoord(p);
                data.bakedGI=SampleSH(normal);data.shadowMask=half4(1,1,1,1);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                half4 colour=UniversalFragmentPBR(data,surface);colour.rgb=MixFog(colour.rgb,input.fog);return colour;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
