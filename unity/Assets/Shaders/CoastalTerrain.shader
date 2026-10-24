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
        _Stylise("Toy look (0 photo, 1 toy)",Float)=0
        _Bands("Toy rock strata",Float)=1
        _Flowers("Meadow flower density",Float)=.12
        _ToyGrassA("Toy meadow light",Color)=(.52,.74,.27,1)
        _ToyGrassB("Toy meadow deep",Color)=(.35,.59,.22,1)
        _ToyRockA("Toy rock light",Color)=(.93,.80,.58,1)
        _ToyRockB("Toy rock warm",Color)=(.87,.71,.52,1)
        _ToyVariety("Valley-scale meadow variety",Float)=0
        _MacroTint("Valley macro tint (rgb multiplier on grass)",2D)="white"{}
        _MacroRect("Macro tint rect (x0,z0,width,depth)",Vector)=(0,0,1,1)
        _RockDesat("Grey the photo rock",Float)=0
        _MeadowBias("More grass on slopes",Float)=0
        _BaseMap("Shadow pass base",2D)="white"{}
        _BaseColor("Shadow pass colour",Color)=(1,1,1,1)
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _GrassColor,_RockColor,_DryColor;
                float _RockScale,_GrassScale,_NormalStrength,_RockOnly,_Stylise,_Bands,_Flowers,_ToyVariety,_RockDesat,_MeadowBias;
                half4 _ToyGrassA,_ToyGrassB,_ToyRockA,_ToyRockB;
                float4 _MacroRect;
            CBUFFER_END
        ENDHLSL
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
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HeightFog.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            TEXTURE2D(_NoiseTex);SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_MacroTint);SAMPLER(sampler_MacroTint);
            TEXTURE2D(_RockAlbedo);SAMPLER(sampler_RockAlbedo);
            TEXTURE2D(_RockNormal);SAMPLER(sampler_RockNormal);
            TEXTURE2D(_RockMask);SAMPLER(sampler_RockMask);
            TEXTURE2D(_GrassAlbedo);SAMPLER(sampler_GrassAlbedo);
            TEXTURE2D(_GrassNormal);SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_GrassMask);SAMPLER(sampler_GrassMask);
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
                // Mid-scale breakup (8-30 m): rock/grass edges wander, grass tones patch.
                half midA=saturate((SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.041+.47).r-.93)/.07);
                half midB=saturate((SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.013+.19).r-.93)/.07);
                half meadow=smoothstep(.60,.88,normal.y+_MeadowBias+(broad-.95)*.25+(midA-.5)*.16);
                meadow*=smoothstep(-2,1,p.y)*(1-_RockOnly);
                half3 rock=TriSample(TEXTURE2D_ARGS(_RockAlbedo,sampler_RockAlbedo),p*_RockScale,weights,signs).rgb;
                rock=lerp(rock,dot(rock,half3(.3,.59,.11)).xxx,_RockDesat)*_RockColor.rgb;
                half3 turf=TriSample(TEXTURE2D_ARGS(_GrassAlbedo,sampler_GrassAlbedo),p*_GrassScale,weights,signs).rgb;
                turf*=lerp(_GrassColor.rgb,_DryColor.rgb,saturate((broad-.93)*7));
                half2 rockMask=TriSample(TEXTURE2D_ARGS(_RockMask,sampler_RockMask),p*_RockScale,weights,signs).rg;
                half2 grassMask=TriSample(TEXTURE2D_ARGS(_GrassMask,sampler_GrassMask),p*_GrassScale,weights,signs).rg;
                half3 relief=lerp(TriRelief(TEXTURE2D_ARGS(_RockNormal,sampler_RockNormal),p*_RockScale,weights,signs),
                    TriRelief(TEXTURE2D_ARGS(_GrassNormal,sampler_GrassNormal),p*_GrassScale,weights,signs),meadow)*_NormalStrength*lerp(1,.3,_Stylise);
                relief-=normal*dot(relief,normal);normal=normalize(normal+relief);
                half wet=1-smoothstep(-11.8,-8.5,p.y);
                SurfaceData surface=(SurfaceData)0;
                // Toy look: soft meadow patches with wildflower dots, and warm banded
                // sandstone following height. A little photo luminance keeps the crevices.
                half patch=smoothstep(.25,.75,saturate((broad-.93)/.07));
                half fine=saturate((SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.11).r-.93)/.07);
                half3 toyTurf=lerp(_ToyGrassB.rgb,_ToyGrassA.rgb,saturate(patch*.8+fine*.35));
                // Mainland: sun-baked golden meadows, lusher low ground, cooler deeper greens up the hills.
                half dryField=smoothstep(.45,.75,saturate((SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.0031+.21).r-.93)/.07));
                half lushField=smoothstep(.5,.8,saturate((SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,p.xz*.0012+.63).r-.93)/.07));
                half3 variety=lerp(half3(1,1,1),half3(1.16,1.04,.66),dryField*.55);
                variety*=lerp(half3(1,1,1),half3(.64,.90,.70),lushField);
                variety*=lerp(half3(1,1,1),half3(.74,.86,.9),smoothstep(45,130,p.y));
                toyTurf*=lerp(half3(1,1,1),variety,_ToyVariety);
                // Baked valley tint (HillcrestValley.MacroTint): forest floor round the copses, hay meadows.
                half4 macroTex=SAMPLE_TEXTURE2D(_MacroTint,sampler_MacroTint,(p.xz-_MacroRect.xy)/_MacroRect.zw);half3 macro=macroTex.rgb;toyTurf*=macro;turf*=macro;
                // Hedgerows round the field parcels, from the baked edge distance (alpha, 8 m units):
                // ~1.6 m of dark lumpy hedge, antialiased, with the odd gap for a field gate.
                // The distance is read from the top mip (mips would average it away); far off, where a pixel
                // is wider than the hedge, it fades to its covered fraction instead of vanishing or thickening.
                float2 macroUV=(p.xz-_MacroRect.xy)/_MacroRect.zw;
                float hedgeA=SAMPLE_TEXTURE2D_LOD(_MacroTint,sampler_MacroTint,macroUV,0).a;
                float hedgeD=hedgeA*8,hedgeW=1.7+(midB-.5)*.8+(broad-.95)*6,hedgeAA=max(fwidth(hedgeD),.05);
                half hedge=saturate((hedgeW-hedgeD)/(2*hedgeAA)+.5)*saturate(1.6*hedgeW/(hedgeW+hedgeAA))*step(hedgeA,.99)*step(.06,frac(p.x*.013+p.z*.017+midA*.3));
                // (Painted hedgerows are for the patchwork seen from afar: within ~110 m, where a flat band never reads as a
                //  hedge, they fade out, and the planted hedges carry the fields' edges near the road.)
                hedge*=smoothstep(50,110,distance(p,_WorldSpaceCameraPos));
                half3 hedgeColour=half3(.09,.2,.08)*lerp(.8,1.25,fine);
                toyTurf=lerp(toyTurf,hedgeColour,hedge);turf=lerp(turf,hedgeColour,hedge);
                // Pasture patches: lusher darker hollows, paler dry knolls, warmer sunlit faces.
                toyTurf*=lerp(half3(.76,.86,.80),half3(1.08,1.05,.90),midA)*lerp(.84,1.06,midB);
                toyTurf*=lerp(1,.9,smoothstep(.97,.8,normal.y));
                float2 cell=floor(p.xz*1.7);float2 seed=frac(sin(float2(dot(cell,float2(127.1,311.7)),dot(cell,float2(269.5,183.3))))*43758.5453);
                float2 dotAt=(cell+.2+seed*.6)/1.7;half petal=1-smoothstep(.045,.075,distance(p.xz,dotAt));
                // Flower dots (~6 cm) fade once a pixel covers more ground than a dot: no distant sparkle.
                float fp=max(length(ddx(p.xz)),length(ddy(p.xz)));
                half bloom=petal*step(1-_Flowers,frac(seed.x*7.13+seed.y*3.7))*smoothstep(.85,.97,normal.y)*(1-smoothstep(.03,.12,fp));
                half3 flower=seed.y<.35?half3(1,.97,.9):seed.y<.6?half3(1,.84,.25):seed.y<.8?half3(1,.55,.72):half3(.72,.62,1);
                toyTurf=lerp(toyTurf,flower,bloom);
                half strata=frac(p.y*.3+(patch-.5)*1.1+sin(p.x*.05+p.z*.04)*.25);
                half3 toyRock=lerp(_ToyRockA.rgb,_ToyRockB.rgb,lerp(.35,smoothstep(.38,.52,strata)-smoothstep(.88,1,strata),_Bands));
                toyRock*=lerp(1,.93,_Bands*smoothstep(.38,.46,strata)*(1-smoothstep(.46,.56,strata)));
                half rockLuma=dot(rock/max(_RockColor.rgb,.01),half3(.3,.59,.11));toyRock*=lerp(1,saturate(rockLuma*1.6),.55);
                // Weathered outcrops: darker in the cracks and at the foot, lichen on top faces.
                toyRock*=lerp(.78,1.04,midA)*lerp(.9,1,smoothstep(.2,.7,normal.y));
                half3 toy=lerp(toyRock,toyTurf,meadow);
                // Photographic detail shows through more close up and on rock; toy colour holds far away.
                half stylise=_Stylise*lerp(.6,1,saturate(distance(p,_WorldSpaceCameraPos)/90))*lerp(.7,1,meadow);
                surface.albedo=lerp(lerp(rock,turf,meadow)*lerp(.96,1.04,grain),toy,stylise)*lerp(1,.66,wet);
                surface.normalTS=half3(0,0,1);surface.smoothness=lerp(1-lerp(rockMask.y,grassMask.y,meadow),.46,wet);
                surface.occlusion=lerp(.7,1,lerp(rockMask.x,grassMask.x,meadow));surface.alpha=1;
                InputData data=(InputData)0;data.positionWS=p;data.positionCS=input.positionCS;data.normalWS=normal;
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);data.shadowCoord=TransformWorldToShadowCoord(p);
                data.bakedGI=SampleSH(normal);data.shadowMask=half4(1,1,1,1);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(input.positionCS);
                half4 colour=UniversalFragmentPBR(data,surface);colour.rgb=ApplyHeightFog(MixFog(colour.rgb,input.fog),p);return colour;
            }
            ENDHLSL
        }
        // Own shadow/depth passes sharing the UnityPerMaterial layout above (HLSLINCLUDE). Borrowing
        // Lit's passes made the terrain SRP-batcher incompatible, the same fault that cost the kit
        // ~4.7 ms (Kit.shader). Opaque terrain needs no alpha clip.
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment Nothing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;float3 _LightPosition;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                float3 p=TransformObjectToWorld(input.positionOS.xyz);float3 n=TransformObjectToWorldNormal(input.normalOS);
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
            float4 DepthVert(float4 positionOS:POSITION):SV_POSITION{return TransformObjectToHClip(positionOS.xyz);}
            half4 Nothing():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormals"}
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;};
            struct Varyings{float4 positionCS:SV_POSITION;half3 normalWS:TEXCOORD0;};
            Varyings NormalsVert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);return o;}
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
