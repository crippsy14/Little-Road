// Crop fields (bug inbox #20, docs/82): standing crop (CropField's canopies) and rows of ridges, vertex-coloured and lit as
// the Distant shader's, pressed flat where a car has driven through them. A crop vertex carries its own height in uv.x; the
// trample map (CropTrample: one value per metre over the fields' area, 0 standing, 1 flat) lowers it by that height, so
// the crop lies down in the car's tracks and stays down, and per pixel the swath shows flattened stalks with a crisp edge.
Shader "LittleRoad/Crops"
{
    Properties
    {
        _HazeColor("Haze colour",Color)=(.66,.80,.90,1)
        _Haze("Haze per metre",Float)=.00008
        _Trample("Trample map",2D)="black"{}
        _TrampleRect("Trample area (x0, z0, 1/width, 1/depth)",Vector)=(0,0,1,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10"}
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _HazeColor;float _Haze;float4 _TrampleRect;
            CBUFFER_END
            TEXTURE2D(_Trample);SAMPLER(sampler_Trample);
            float Trampled(float3 positionWS)
            {
                float2 uv=(positionWS.xz-_TrampleRect.xy)*_TrampleRect.zw;
                return all(uv>0)&&all(uv<1)?SAMPLE_TEXTURE2D_LOD(_Trample,sampler_Trample,uv,0).r:0;
            }
            // (the wind going over standing crop: a slow swell across the field, gone where it's flattened)
            float3 Pressed(float3 positionOS,float ridge,out float t,out float wave)
            {
                float3 p=TransformObjectToWorld(positionOS);t=Trampled(p);wave=sin(_Time.y*1.3+p.x*.09+p.z*.06);
                p.y-=ridge*t*.92;p.y+=ridge*(1-t)*.07*wave;return p;
            }
        ENDHLSL
        Pass
        {
            Name "CropsForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;half4 color:COLOR;half crushed:TEXCOORD2;half up:TEXCOORD3;float2 local:TEXCOORD4;half wave:TEXCOORD5;};
            Varyings Vert(Attributes input)
            {
                Varyings o;float t,wave;o.positionWS=Pressed(input.positionOS.xyz,input.uv.x,t,wave);o.positionCS=TransformWorldToHClip(o.positionWS);
                // (pressed flat, a ridge faces up: its normal eases towards the sky as it lies down)
                o.normalWS=normalize(lerp(TransformObjectToWorldNormal(input.normalOS),float3(0,1,0),t*step(.001,input.uv.x)));o.color=input.color;o.crushed=t;o.up=input.uv.y;o.local=input.local;o.wave=wave*(1-t)*step(.001,input.uv.x);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                Light sun=GetMainLight();float ndl=saturate(dot(normalize(i.normalWS),sun.direction));
                half cloud=1;
                #if defined(_LIGHT_COOKIES)
                    cloud=SampleMainLightCookie(i.positionWS).r;
                #endif
                half hollow=saturate((i.color.a-.5h)*2);half ridge=saturate((.5h-i.color.a)*2);
                half3 light=SampleSH(half3(0,1,0))*1.75h+sun.color*(ndl*cloud*.33h);
                half3 colour=SRGBToLinear(i.color.rgb)*light*(1-.38h*hollow)*(1+.12h*ridge*ndl);
                // (the swath: the map sampled per pixel, sharply, so its edge is crisp where the mesh can only dip; flattened
                // stalks show green-brown and darker, the soil between them)
                half flat=smoothstep(.2h,.45h,Trampled(i.positionWS));
                colour=lerp(colour,colour*half3(.6h,.66h,.44h),flat);
                // (close up: the crop darkens toward its foot, in its own shade; the drill rows line it every 25 cm and the ears
                // mottle it; the swell's crest catches the light. The fine detail fades out before it would shimmer.)
                float distance=length(i.positionWS-_WorldSpaceCameraPos);float3 q=i.positionWS;
                half drill=.5h+.5h*sin(i.local.x*25.13);half ears=.5h+.5h*sin(q.x*31+sin(q.z*23)*2)*sin(q.z*29+sin(q.x*19)*2);
                colour*=lerp(.74h,1,i.up)*lerp((.93h+.09h*drill)*(.93h+.1h*ears),1,saturate(distance/35))*(1+.06h*i.wave);
                float haze=saturate(1-exp(-distance*_Haze));
                half3 hazeColour=lerp(_HazeColor.rgb*saturate(Luminance(unity_FogColor.rgb)/.58h),unity_FogColor.rgb,.7);
                return half4(ApplyHeightFog(lerp(colour,hazeColour,haze),i.positionWS),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            float4 Vert(Attributes input):SV_POSITION{float t,wave;return TransformWorldToHClip(Pressed(input.positionOS.xyz,input.uv.x,t,wave));}
            half Frag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
