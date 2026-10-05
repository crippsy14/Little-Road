Shader "LittleRoad/CoastalWater"
{
    Properties
    {
        _SkyTex("Credited coastal sky",2D)="white"{}
        _AzimuthOffset("Sky rotation",Float)=0
        _ReviewTime("Fixed review time; negative uses live clock",Float)=-1
        _DeepColor("Deep water",Color)=(.02,.19,.36,1)
        _ShallowColor("Shallow water",Color)=(.08,.55,.56,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Name "CoastalWaterForward"
            Tags {"LightMode"="UniversalForward"}
            ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            #include "DaySky.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            TEXTURE2D(_SkyTex);SAMPLER(sampler_SkyTex);
            CBUFFER_START(UnityPerMaterial)
                float _AzimuthOffset,_ReviewTime;
                half4 _DeepColor,_ShallowColor;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;};
            struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half fog:TEXCOORD1;};
            float Clock(){return _ReviewTime>=0?_ReviewTime:_Time.y;}
            float Swell(float2 p,float t)
            {
                return sin(dot(p,float2(.177,.118))+t*.85)*.16+sin(dot(p,float2(-.26,.205))+t*1.12)*.075;
            }
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            float FilteredCos(float phase)
            {
                // Integrate away waves smaller than a pixel instead of
                // allowing them to turn into high-contrast horizon noise.
                float footprint=min(32,max(abs(ddx(phase)),abs(ddy(phase))));
                return cos(phase)*exp2(-.6*footprint*footprint);
            }
            float2 Slope(float2 p,float t)
            {
                // Break up long, perfectly straight reflection bands and let
                // unresolved far-field slopes converge to a calm mean normal.
                float range=distance(p,_WorldSpaceCameraPos.xz);
                float longFade=lerp(1,.12,smoothstep(35,220,range));
                float2 swell=p+float2(Noise(p*.037+t*.014),Noise(p*.043+71-t*.011))*9;
                float2 s=FilteredCos(dot(swell,float2(.177,.118))+t*.85)*.11*float2(.177,.118);
                s+=FilteredCos(dot(swell,float2(-.26,.205))+t*1.12)*.055*float2(-.26,.205);
                s+=FilteredCos(dot(swell,float2(.091,-.153))+t*.68)*.06*float2(.091,-.153);
                s*=longFade;
                p+=float2(Noise(p*.17+t*.025),Noise(p*.14+31-t*.019))*4.3;
                float distanceFade=1-smoothstep(45,240,range);
                s+=FilteredCos(dot(p,float2(2.3,.85))+t*1.65)*.022*float2(2.3,.85)*distanceFade;
                s+=FilteredCos(dot(p,float2(-1.7,2.1))+t*1.43)*.017*float2(-1.7,2.1)*distanceFade;
                s+=FilteredCos(dot(p,float2(5.8,-3.1))+t*2.2)*.005*float2(5.8,-3.1)*distanceFade;
                return s;
            }
            float SceneDepth(float2 uv){float z=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    z=lerp(UNITY_NEAR_CLIP_VALUE,1,z);
                #endif
                return z;
            }
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionWS.y+=Swell(o.positionWS.xz,Clock());
                o.positionCS=TransformWorldToHClip(o.positionWS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float t=Clock();float3 p=input.positionWS;
                float2 slope=Slope(p.xz,t);half3 n=normalize(float3(-slope.x,1,-slope.y));
                half3 v=GetWorldSpaceNormalizeViewDir(p);float2 uv=GetNormalizedScreenSpaceUV(input.positionCS);
                float3 floorPoint=ComputeWorldSpacePosition(uv,SceneDepth(uv),UNITY_MATRIX_I_VP);
                float depth=clamp(p.y-floorPoint.y,0,60);
                // Refraction is constrained to pixels behind the water plane,
                // so a foreground bridge or cliff cannot smear onto the sea.
                float2 refractedUV=saturate(uv+TransformWorldToViewDir(n).xy*.009*saturate(depth));
                float3 refractedFloor=ComputeWorldSpacePosition(refractedUV,SceneDepth(refractedUV),UNITY_MATRIX_I_VP);
                if(refractedFloor.y>p.y)refractedUV=uv;
                half3 bottom=SampleSceneColor(refractedUV);
                // (Street review, 5 Oct: the faces of piers just under the surface took the shallow-beach turquoise, so each pier
                // showed a pale glassy column down into the sea.) Shallow colour is for a shelving bottom: where the surface
                // seen through the water is steep (a pier, a sea wall), it reads as the deep water around it.
                float3 floorNormal=normalize(cross(ddy(floorPoint),ddx(floorPoint)));half steep=smoothstep(.55,.25,abs(floorNormal.y));
                float colourDepth=lerp(depth,max(depth,14),steep);
                half transmission=exp(-colourDepth*.36);
                half3 scatter=lerp(_ShallowColor.rgb,_DeepColor.rgb,1-exp(-colourDepth*.18));
                Light light=GetMainLight(TransformWorldToShadowCoord(p));
                half shade=lerp(.68,1,light.shadowAttenuation);
                // Body colour takes the light (sky SH + sun) so the sea darkens with the day; ~1 at afternoon.
                half3 lit=SampleSH(half3(0,1,0))*1.6h+Luminance(light.color)*saturate(light.direction.y)*.3h;
                half3 base=lerp(scatter*shade*lit,bottom*half3(.50,.83,.78),transmission*.7);
                half3 reflection=reflect(-v,n);reflection.y=max(.001,reflection.y);
                float2 skyUV=float2(.5-(atan2(reflection.z,reflection.x)+_AzimuthOffset)/(2*PI),1-acos(clamp(reflection.y,-1,1))/PI);
                half3 sky=_DaySunDir.w>.5?DaySkyColour(reflection,false)*.85:min(SAMPLE_TEXTURE2D_LOD(_SkyTex,sampler_SkyTex,skyUV,5).rgb*.70,3);
                sky=lerp(unity_FogColor.rgb,sky,smoothstep(0,.2,reflection.y));
                half fresnel=.025+.72*pow(1-saturate(dot(n,v)),5);
                half3 colour=lerp(base,sky,fresnel);
                half3 h=normalize(v+light.direction);
                half glint=pow(saturate(dot(n,h)),220)*.65*light.shadowAttenuation;
                colour+=light.color*glint;
                float noise=Noise(p.xz*1.8+float2(t*.12,-t*.10))*.65+Noise(p.xz*4.1-t*.16)*.35;
                noise=lerp(noise,.5,saturate(length(fwidth(p.xz))*2));
                float wash=.5+.5*sin(depth*5-t*1.8+noise*3);
                half foam=(1-smoothstep(.05,.95,depth))*smoothstep(.50,.91,noise+wash*.28);
                foam*=smoothstep(0,.08,depth);
                // Contact foam where the sea meets anything upright (sea walls, pier legs, hulls, rocks): the gap
                // between the water surface and what sits behind it along the view, broken up by the same noise
                // and faded out with distance, where the depth buffer is too coarse.
                float surfaceEye=LinearEyeDepth(input.positionCS.z,_ZBufferParams),sceneEye=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                float gap=max(0,sceneEye-surfaceEye);
                half contact=((1-smoothstep(.0,1.1,gap))*smoothstep(.26,.70,noise+.18*sin(t*2.1+p.x*.7+p.z*.5))+(1-smoothstep(0,.3,gap))*.75)*(1-smoothstep(60,140,surfaceEye));
                foam=max(foam,saturate(contact));
                colour=lerp(colour,half3(.70,.84,.80)*shade*lit,foam*.88);
                return half4(ApplyHeightFog(MixFog(colour,input.fog),input.positionWS),1);
            }
            ENDHLSL
        }
    }
}
