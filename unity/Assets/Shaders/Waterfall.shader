// Silver Falls (HillcrestValley.Cascade): a translucent sheet of falling water. Streaks scroll down the
// sheet (uv.y grows downwards), clear glassy water at the lip turns to white foam lower down, the sides
// thin out, and it takes the sun, sky light, fog and height fog like the rest of the world.
Shader "LittleRoad/Waterfall"
{
    Properties
    {
        _Water("Water",Color)=(.42,.66,.80,.55)
        _Foam("Foam",Color)=(.97,.99,1,.95)
        _Speed("Fall speed",Float)=1.1
        _Layer("Layer offset",Float)=0
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Water,_Foam;float _Speed,_Layer; CBUFFER_END
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;half fog:TEXCOORD2;};
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+float2(1,1)),f.x),f.y);}
            V vert(A a){V v;v.world=TransformObjectToWorld(a.pos.xyz);v.pos=TransformWorldToHClip(v.world);v.uv=a.uv;v.fog=ComputeFogFactor(v.pos.z);return v;}
            half4 frag(V v):SV_Target
            {
                // uv.x across (0-1), uv.y metres down the drop.
                float t=_Time.y*_Speed;
                float streak=Noise(float2(v.uv.x*26+_Layer*7,v.uv.y*.35-t))*.65+Noise(float2(v.uv.x*61+_Layer*3,v.uv.y*.9-t*1.6))*.35;
                float drop=saturate(v.uv.y/12);
                half foam=saturate(smoothstep(.45,.8,streak)*(.35+.65*drop)+drop*drop*.35);
                half edge=smoothstep(0,.12,v.uv.x)*smoothstep(0,.12,1-v.uv.x);
                half3 colour=lerp(_Water.rgb,_Foam.rgb,foam);
                half alpha=lerp(_Water.a,_Foam.a,foam)*edge*lerp(.75,1,smoothstep(.2,.7,streak));
                Light light=GetMainLight(TransformWorldToShadowCoord(v.world));
                half3 lit=SampleSH(half3(0,1,0))*.9+light.color*(.45+.55*light.shadowAttenuation)*.55;
                colour*=lit;
                colour=MixFog(colour,v.fog);colour=ApplyHeightFog(colour,v.world);
                return half4(colour,saturate(alpha));
            }
            ENDHLSL
        }
    }
}
