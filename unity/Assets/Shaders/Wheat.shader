// Car-high wheat. Each vertex carries its stalk's root (TEXCOORD1); the stalk rotates about
// that root: a gentle wind sway, or flattened over in the direction a car pushed it, read from
// the field's trample texture (R = how flat, GB = world xz direction). Canopy sheets for the
// far LOD use the same rule, so crop circles show from the hills too.
Shader "LittleRoad/Wheat"
{
    Properties
    {
        _Trample("Trample (R flat, GB direction)",2D)="black"{}
        _FieldOrigin("Field corner xz, size zw",Vector)=(0,0,1,1)
        _FieldAxis("Field x axis xz, z axis zw",Vector)=(1,0,0,1)
        _Wind("Wind sway",Float)=1
    }
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_Trample);SAMPLER(sampler_Trample);
        CBUFFER_START(UnityPerMaterial)
            float4 _Trample_ST;float4 _FieldOrigin;float4 _FieldAxis;float _Wind;
        CBUFFER_END
        // Bent world position of a stalk vertex; `flat` returns how trampled the stalk is.
        float3 BendStalk(float3 p,float3 root,out float flat)
        {
            float3 offset=p-root;float h=max(offset.y,0);float3 side=float3(offset.x,0,offset.z);
            float2 rel=root.xz-_FieldOrigin.xy;
            float2 uv=float2(dot(rel,_FieldAxis.xy)/_FieldOrigin.z,dot(rel,_FieldAxis.zw)/_FieldOrigin.w);
            float4 t=SAMPLE_TEXTURE2D_LOD(_Trample,sampler_Trample,uv,0);
            flat=t.r;float2 pushed=t.gb*2-1;
            float2 windDir=float2(.94,.34);
            float sway=(sin(_Time.y*1.7+root.x*.31+root.z*.23)*.5+.5)*.17+sin(_Time.y*3.3+root.x*.9+root.z*.5)*.035;
            float2 dir=normalize(lerp(windDir,dot(pushed,pushed)>.01?normalize(pushed):windDir,saturate(flat*4)));
            float angle=flat*1.38+sway*_Wind*(1-flat);
            float s=sin(angle),c=cos(angle);
            return root+side*(1+flat*.35)+float3(dir.x*s*h,c*h,dir.y*s*h);
        }
    ENDHLSL
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off
        Pass
        {
            Name "WheatForward"
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            struct Attributes{float4 positionOS:POSITION;half4 color:COLOR;float3 root:TEXCOORD1;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half4 color:COLOR;half flat:TEXCOORD1;half fog:TEXCOORD2;};
            Varyings Vert(Attributes input)
            {
                Varyings o;float flat;
                float3 world=TransformObjectToWorld(input.positionOS.xyz),root=TransformObjectToWorld(input.root);
                o.positionWS=BendStalk(world,root,flat);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.color=input.color;o.flat=flat;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS),i.positionWS,half4(1,1,1,1));
                // Cloud shadows arrive as the cookie-tinted light colour.
                half cloud=saturate(Luminance(sun.color)/max(Luminance(_MainLightColor.rgb),.001h));
                // Vertex colours are authored in sRGB; trampled stalks show their shadier undersides.
                half3 colour=SRGBToLinear(i.color.rgb)*lerp(1,.74h,i.flat);
                half light=.58h+.5h*saturate(sun.direction.y)*lerp(.45h,1,sun.shadowAttenuation)*cloud;
                return half4(ApplyHeightFog(MixFog(colour*light,i.fog),i.positionWS),1);
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
            struct Attributes{float4 positionOS:POSITION;float3 root:TEXCOORD1;};
            float4 Vert(Attributes input):SV_POSITION
            {
                float flat;float3 world=TransformObjectToWorld(input.positionOS.xyz),root=TransformObjectToWorld(input.root);
                return TransformWorldToHClip(BendStalk(world,root,flat));
            }
            half Frag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
