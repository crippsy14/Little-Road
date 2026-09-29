// Tyre marks (SkidMarks): multiply-darkens whatever is beneath, so rubber on tarmac and ruts in
// dirt both sit on their surface without lighting of their own. Vertex colour: rgb = mark tint,
// a = strength (fades with age).
Shader "LittleRoad/SkidMark"
{
    Properties{}
    SubShader
    {
        Tags {"RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20"}
        Pass
        {
            Name "SkidMark"
            Tags {"LightMode"="UniversalForward"}
            Blend DstColor Zero ZWrite Off Cull Off Offset -2,-2
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes{float4 positionOS:POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;half4 color:COLOR;float2 uv:TEXCOORD0;};
            Varyings Vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.color=input.color;o.uv=input.uv;return o;}
            half4 Frag(Varyings i):SV_Target
            {
                // Soft edges across the tread, a faint tread pattern along it.
                half edge=smoothstep(0,.3,i.uv.x)*smoothstep(1,.7,i.uv.x);
                half tread=lerp(.8,1,step(.5,frac(i.uv.y*6)));
                half a=saturate(i.color.a*edge*tread);
                return half4(lerp(half3(1,1,1),i.color.rgb,a),1);
            }
            ENDHLSL
        }
    }
}
