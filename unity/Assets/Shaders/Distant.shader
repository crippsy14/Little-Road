// Far mountain ranges: vertex-coloured (rock, foothill green, snow), soft sun shading and
// its own aerial-perspective haze so ranges kilometres away still read, pale and blue.
Shader "LittleRoad/Distant"
{
    Properties
    {
        _HazeColor("Haze colour",Color)=(.66,.80,.90,1)
        _Haze("Haze per metre",Float)=.00032
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10"}
        Pass
        {
            Name "DistantForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _HazeColor;float _Haze;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;half4 color:COLOR;};
            Varyings Vert(Attributes input)
            {
                Varyings o;o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.color=input.color;return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                Light sun=GetMainLight();float ndl=saturate(dot(normalize(i.normalWS),sun.direction));
                // Vertex colours are authored in sRGB; the project renders in linear space.
                half cloud=1;
                #if defined(_LIGHT_COOKIES)
                    cloud=SampleMainLightCookie(i.positionWS).r;
                #endif
                // Alpha carries hollowness (0.5 level): gullies fall into shade, ridges catch the sun.
                half hollow=saturate((i.color.a-.5h)*2);half ridge=saturate((.5h-i.color.a)*2);
                // Real light (sky SH + sun), scaled so the afternoon matches the old .62 + .42 N.L; dark at night.
                half3 light=SampleSH(half3(0,1,0))*1.75h+sun.color*(ndl*cloud*.33h);
                half3 colour=SRGBToLinear(i.color.rgb)*light*(1-.38h*hollow)*(1+.12h*ridge*ndl);
                float distance=length(i.positionWS-_WorldSpaceCameraPos);
                float haze=saturate(1-exp(-distance*_Haze));
                // Thinner haze higher up: peaks stand out against the sky.
                haze*=lerp(1,.55,saturate(i.positionWS.y/700));
                // Haze follows the time of day's fog colour (warm at golden hour), keeping some blue.
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _HazeColor;float _Haze;
            CBUFFER_END
            float4 Vert(float4 positionOS:POSITION):SV_POSITION{return TransformObjectToHClip(positionOS.xyz);}
            half Frag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
