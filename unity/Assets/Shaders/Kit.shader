// Surfaces for the Blender kit (buildings, props, trees) and the built masonry. The shape stays
// toy-simple; the surface carries the detail: coursed stone, plaster mottling, tile joints, wood
// grain, leaf clumps, weathering at the base and runs of grime, per-building tint drift, and a
// derivative bump from the same patterns so they catch the light.
// Coordinates: kit meshes carry KitSurface (TEXCOORD1: along-face u, v) and KitHeight (TEXCOORD2:
// height above the kit's base), baked by source-art/build_bayview_kit.py. _WorldSpace=1 derives
// the same from world position for meshes built in code (no base weathering there).
Shader "LittleRoad/Kit"
{
    Properties
    {
        _BaseColor("Colour",Color)=(1,1,1,1)
        _Smoothness("Smoothness",Float)=.2
        _Metallic("Metallic",Float)=0
        _Pattern("Pattern (0 plain,1 plaster,2 stone,3 tile,4 wood,5 leaf,6 paint,7 glass,8 rock,9 asphalt,10 slate,11 dry stone,12 straw)",Float)=0
        _WorldSpace("World-space coordinates",Float)=0
        _Wear("Weathering",Float)=1
        _BaseMap("Shadow base",2D)="white"{}
        _Cutoff("Cutoff",Float)=.5
        _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        // Leaves (pattern 5) sway in the wind, more with height above the kit's base (KitHeight), each
        // tree phased by position, with a light flutter. Every pass applies it so shadows move too.
        HLSLINCLUDE
            #define KIT_LEAF(pattern) ((pattern)>4.5&&(pattern)<5.5)
            #define KIT_WIND(w,h) ((w)+(float3(sin(_Time.y*1.3+(w).x*.11+(w).z*.07),0,cos(_Time.y*1.1+(w).x*.07-(w).z*.12))*.075+float3(sin(_Time.y*5.3+(w).y*2.1+(w).x),0,cos(_Time.y*4.7+(w).y*1.7+(w).z))*.014)*saturate(((h)-1.2)/6))
            // Flags and banners (KitHeight.y = freedom: 0 where held, 1 at the free edge) wave along the kit's
            // thin axis (Blender Y = object Z after import): a slow billow plus a quicker ripple running outward.
            #define KIT_FLAG(w,f) ((w)+normalize(TransformObjectToWorldDir(float3(0,0,1)))*(f)*(sin(_Time.y*2.6+(w).x*.31+(w).z*.23-(f)*3.2)*.2+sin(_Time.y*8.3-(f)*7.1+(w).y*1.7)*.05))
        ENDHLSL
        Pass
        {
            Name "KitForward"
            Tags {"LightMode"="UniversalForward"}
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #include "HeightFog.hlsl"
            TEXTURE2D(_KitNoise);SAMPLER(sampler_KitNoise);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;float _Smoothness,_Metallic,_Pattern,_WorldSpace,_Wear,_Cutoff,_Cull;float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 surface:TEXCOORD1;float2 height:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float3 kit:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
            Varyings Vert(Attributes input)
            {
                Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_TRANSFER_INSTANCE_ID(input,o);
                float3 w=TransformObjectToWorld(input.positionOS.xyz);if(KIT_LEAF(_Pattern))w=KIT_WIND(w,input.height.x);if(_WorldSpace<.5&&input.height.y>0&&input.height.y<=1.001)w=KIT_FLAG(w,input.height.y);
                VertexPositionInputs p=(VertexPositionInputs)0;p.positionWS=w;p.positionCS=TransformWorldToHClip(w);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(input.normalOS);
                o.kit=float3(input.surface,input.height.x);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half N(float2 q){return SAMPLE_TEXTURE2D(_KitNoise,sampler_KitNoise,q).r;}
            float Hash(float2 c){return frac(sin(dot(c,float2(127.1,311.7)))*43758.5453);}
            // Pattern anti-aliasing: a feature `width` metres across keeps full contrast while a pixel
            // covers less than half of it and is gone once a pixel covers twice it (fp: metres of
            // surface per pixel). Joints, courses and per-cell colour steps otherwise crawl at 15-40 m.
            half Resolve(float width,float fp){return 1-smoothstep(width*.5,width*2,fp);}
            float _KitNight; // NightLights (global)
            float _KitRim,_KitBounce;half4 _KitBounceColour; // WorldLook.RimAndBounce (global)
            half4 Frag(Varyings i):SV_Target
            {
                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(i.positionCS); // dissolve at LOD and distance-cull switches instead of popping
                #endif
                UNITY_SETUP_INSTANCE_ID(i);
                half3 n=normalize(i.normalWS);float3 p=i.positionWS;half up=abs(n.y);int pattern=(int)round(_Pattern);
                // Surface coordinates (u along the face, v up it) and height above the base.
                float2 s=i.kit.xy;float height=i.kit.z;
                if(_WorldSpace>.5) {
                    float2 t=normalize(float2(-n.z,n.x)+float2(1e-4,0));
                    s=up>.7?p.xz:float2(dot(p.xz,t),p.y);height=10;
                }
                half mottle=N(s*.07+.31),fine=N(s*1.7),grain=N(s*4.3+.7);
                // Fine pattern fades with distance (no shimmer), leaving the broad colour drift.
                half near=1-smoothstep(35,150,length(p-_WorldSpaceCameraPos));
                float fp=max(length(ddx(s)),length(ddy(s)));
                half m=1,bump=0,occlusion=1,smooth=_Smoothness;half3 tint=1;
                // Beyond 150 m the fine pattern has faded out: take the cheap path (broad drift only).
                // (Split slate keeps its beds and stains at any range: the quarry's galleries are seen from 200-600 m away, and
                // the cheap path turned them into one flat dark band.)
                if(near<=0&&pattern!=7&&pattern!=10){pattern=0;m=1+(mottle-.5)*.08;}
                else if(pattern==1) { // plaster: soft mottling, sandy grain, faint vertical runs
                    m=1+(mottle-.5)*.16+(fine-.5)*.07;bump=fine*.5+grain*.25;
                    half runs=N(float2(s.x*2.3,s.y*.12+.4));m-=smoothstep(.55,.8,runs)*.05*_Wear;
                } else if(pattern==2) { // coursed stone, running bond; flags on flat tops
                    float2 cellSize=up>.7?float2(.8,.8):float2(.86,.42);
                    // Rows of varying block length (hash per course) so big walls aren't wallpaper.
                    float row=floor(s.y/cellSize.y);float rowLength=up>.7?1:.72+.62*Hash(float2(row,3.1));
                    float2 q=float2(s.x/(cellSize.x*rowLength)+Hash(float2(row,7.3)),s.y/cellSize.y);
                    float2 cell=floor(q),f=frac(q);float2 edge=min(f,1-f)*cellSize;half mortar=(1-smoothstep(.012,.03,min(edge.x,edge.y)))*Resolve(.03,fp);
                    half block=lerp(.5,Hash(cell),Resolve(.3,fp)),worn=N(cell*.37+s*.02);m=1+(block-.5)*.16+(fine-.5)*.1+(mottle-.5)*.12-mortar*.1*near;
                    // Rain streaks down from ledges.
                    m-=smoothstep(.58,.82,N(float2(s.x*1.4,s.y*.07+.2)))*.07*_Wear*(1-up);
                    // Old walls: broad weather staining and lichen patches.
                    half aged=saturate(_Wear-1);m-=smoothstep(.45,.75,N(s*.035+.6))*.14*aged;
                    tint=lerp(tint,half3(.93,.97,.84),smoothstep(.66,.8,N(s*.11+.3))*.6*aged);
                    bump=((1-mortar)*.6+worn*.3)*near+grain*.2;occlusion=1-mortar*.18*near;tint=lerp(1,half3(1.03,1,.95),block);
                } else if(pattern==3) { // roof tiles: joints and per-tile colour, lichen specks
                    float2 q=float2(s.x/.3,s.y/.21);float2 cell=floor(float2(q.x+(fmod(floor(q.y),2)<1?0:.5),q.y));float2 f=frac(float2(q.x+(fmod(floor(q.y),2)<1?0:.5),q.y));
                    half joint=(1-smoothstep(.02,.07,min(f.x,1-f.x)))*Resolve(.02,fp);half course=smoothstep(.6,1,f.y)*Resolve(.06,fp);
                    m=1+(Hash(cell)-.5)*.22*Resolve(.16,fp)-joint*.2-course*.08;bump=1-joint-course*.5;occlusion=1-joint*.3;
                    half lichen=smoothstep(.72,.8,N(s*2.1+.5))*_Wear;tint=lerp(1,half3(1.05,1.08,.82),lichen*.5);
                } else if(pattern==4) { // wood: grain and board joints
                    half g=N(float2(s.x*.35,s.y*7));float board=frac(s.y/.19);half seam=(1-smoothstep(.0,.06,min(board,1-board)))*Resolve(.015,fp);
                    m=1+(g-.5)*.28-seam*.22;bump=g*.6-seam*.3;
                } else if(pattern==5) { // leaves: clumps, sunlit tips
                    half clump=N(s*.75+p.xz*.02),small=N(s*2.6);m=.76+clump*.34+(small-.5)*.14;bump=clump*.7+small*.4;
                    tint=lerp(half3(.94,1,.9),half3(1.05,1.03,.9),saturate(n.y*.5+.5));
                } else if(pattern==6) { // painted wood and trim: slight wear
                    m=1+(mottle-.5)*.06+(fine-.5)*.03;bump=fine*.15;
                } else if(pattern==11) { // dry stone (Prop Forge walls, on the run's own arc and up the section, so courses run on
                    // round bends and across pieces). The face: rough, wavering courses of flat stones of every length, thin
                    // stones leaving dark open gaps above them (no mortar), each stone its own shade, matte; the cope (above
                    // 0.87 m): stones set on edge, narrow upright slabs. Lichen in patches, darker towards the foot. (The first
                    // cut read as pillowy bricks in neat rows, 18 Oct: no bevel now, courses waver and stones vary more.)
                    half joint,shade;float2 cell;
                    if(height>.87) {
                        float xx=s.x/.12+(N(float2(s.x*.6,.8))-.5)*1.4;cell=float2(floor(xx),71);float fx=frac(xx);
                        joint=(1-smoothstep(.03,.16,min(fx,1-fx)))*Resolve(.03,fp);shade=lerp(.5,Hash(cell),Resolve(.12,fp));
                    } else {
                        float wob=(N(float2(s.x*.35,.3))-.5)*.22+(N(float2(s.x*1.3,.7))-.5)*.05;float rowH=.13;float yy=s.y+wob;float row=floor(yy/rowH);
                        float len=.16+.42*Hash(float2(row,1.7));float xx=s.x/len+Hash(float2(row,5.1))+(N(float2(s.x*.8,row*.37))-.5)*.9;
                        cell=float2(floor(xx),row);float fx=frac(xx),fy=frac(yy/rowH);
                        float thick=rowH*(.62+.6*Hash(cell+2.3));float2 edge=float2(min(fx,1-fx)*len,min(fy*rowH,max(thick-fy*rowH,0)));
                        joint=(1-smoothstep(.005,.02,min(edge.x,edge.y)))*Resolve(.03,fp);shade=lerp(.5,Hash(cell),Resolve(.2,fp));
                    }
                    m=.76+shade*.38+(fine-.5)*.14+(grain-.5)*.08+(mottle-.5)*.16-joint*.62*near;
                    tint=lerp(half3(1.06,1,.92),half3(.92,.98,1.07),lerp(.5,Hash(cell+7.7),Resolve(.25,fp)));
                    half lichen=smoothstep(.6,.78,N(s*.8+.2))*smoothstep(.2,.8,s.y);tint=lerp(tint,half3(1.1,1.08,.8),lichen*.4);m+=lichen*.06;
                    m*=lerp(.84,1,smoothstep(0,.45,height));
                    bump=fine*.35+grain*.2+shade*.15-joint*.6;occlusion=1-joint*.5*near;smooth=min(_Smoothness,.05);
                } else if(pattern==12) { // straw (Prop Forge bales): fibres along u (round a round bale, in rings on its ends),
                    // packed in layers of slightly different tone, with dark gaps between bundles; matte
                    half fibre=N(float2(s.x*.9,s.y*24))*.6+N(float2(s.x*.35+.3,s.y*8))*.4;
                    half gap=smoothstep(.62,.86,N(float2(s.x*1.8,s.y*52)))*Resolve(.015,fp);
                    half layer=N(float2(s.x*.05,s.y*1.6+.2));
                    m=.8+fibre*.32+(layer-.5)*.16-gap*.22+(mottle-.5)*.12;
                    tint=lerp(half3(1.05,1,.84),half3(.97,.95,.96),layer);
                    bump=fibre*.55-gap*.45;smooth=min(_Smoothness,.06);
                } else if(pattern==8) { // natural rock: weathered, cracked, no courses
                    half crack=(1-smoothstep(.0,.09,abs(N(s*.33+.9)-.5)))*Resolve(.12,fp);m=1+(mottle-.5)*.18+(fine-.5)*.09-crack*.08*near;
                    // Low-frequency relief only: the derivative bump is evaluated per 2x2 pixel quad, so
                    // fine grain in it turned the road verges into blocky speckle (m39 v04).
                    bump=fine*.35+mottle*.5-crack*.25;occlusion=1-crack*.25*near;tint=lerp(1,half3(.96,1.02,.94),smoothstep(.6,.8,N(s*.5))*up);
                } else if(pattern==9) { // asphalt: aggregate speckle, old repair patches, hairline cracks
                    half agg=N(s*2.7+.3);m=1+(agg-.5)*.1*near+(fine-.5)*.05;
                    // Repair patches: fresher, darker tar with wobbly edges (squares read as floating tiles).
                    half patch=smoothstep(.62,.66,N(floor(s*.35+(N(s*.06+.2)-.5)*1.4)*.13+.4))*lerp(.5,1,Resolve(.5,fp));m*=lerp(1,.84,patch);
                    // Hairline cracks only in a few worn stretches.
                    half crack=(1-smoothstep(.0,.006,abs(N(s*.09+.77)-.5)))*smoothstep(.62,.72,N(s*.02+.2))*Resolve(.06,fp);m-=crack*.15*near*(1-patch);
                    m*=lerp(.94,1.04,mottle);bump=agg*.3*near;smooth=_Smoothness*(1+patch*.1);
                } else if(pattern==10) { // split slate (Quarry Hills): world-anchored (no seams where a curved face's columns turn):
                    // horizontal beds of purple, blue and grey, near-vertical cleavage lines, cracks and rust runs on faces; flat tops
                    // are slate rubble (chips of every shade). Always matte.
                    if(up>.7) {
                        // Rubble, not tiles (Richard, 4 Oct: the floor looked like a kitchen floor's repeating squares): irregular
                        // chips from two Voronoi scales (broken slabs and the small stuff between), drifts of pale dust, damp
                        // dark patches; _Wear >= 2 is loose gravel (chips that much finer) with two polished wheel ruts along
                        // the strip's own across-coordinate (kit.x, 0..1) and loose stones on the berms.
                        bool gravel=_Wear>=2;float scale=gravel?_Wear*1.3:1.1;
                        float2 wob=float2(N(p.xz*.11+.2),N(p.xz*.11+.7))-.5;
                        float2 q=p.xz*scale+wob*1.2;float2 cell=floor(q),f=frac(q);float d1=8,d2=8;float2 id=0;
                        [unroll]for(int j=-1;j<=1;j++)[unroll]for(int k=-1;k<=1;k++){float2 g=float2(k,j);float2 o=float2(Hash(cell+g),Hash(cell+g+17.3))*.85+.075;float2 r=g+o-f;float dd=dot(r,r);if(dd<d1){d2=d1;d1=dd;id=cell+g;}else if(dd<d2)d2=dd;}
                        half gap=(1-smoothstep(.0,.16,sqrt(d2)-sqrt(d1)))*Resolve(.08/scale,fp);
                        float2 q2=p.xz*scale*3.1+wob*2;float2 c2=floor(q2),f2=frac(q2);float e1=8,e2=8;float2 id2=0;
                        [unroll]for(int j2=-1;j2<=1;j2++)[unroll]for(int k2=-1;k2<=1;k2++){float2 g=float2(k2,j2);float2 o=float2(Hash(c2+g+5.1),Hash(c2+g+9.7))*.85+.075;float2 r=g+o-f2;float dd=dot(r,r);if(dd<e1){e2=e1;e1=dd;id2=c2+g;}else if(dd<e2)e2=dd;}
                        half gap2=(1-smoothstep(.0,.14,sqrt(e2)-sqrt(e1)))*Resolve(.03/scale,fp);
                        // (Per-chip colour fades out once a chip is under a few pixels, or it shimmers at range.)
                        half big=lerp(.5,Hash(id),Resolve(.45/scale,fp)),small=lerp(.5,Hash(id2),Resolve(.15/scale,fp));
                        // Mostly compacted gravel (speckle at two sizes) with a scatter of loose stones; the broken-slab
                        // outlines only where a few bedrock slabs show through (a patch mask), so no cell grid reads.
                        half slabs=smoothstep(.58,.72,N(p.xz*.05+.4));gap*=slabs;
                        half stone=(1-smoothstep(.08,.16,sqrt(e1)))*step(.72,Hash(id2+4.4))*Resolve(.06/scale,fp);gap2*=stone*.6;
                        if(gravel)gap=0;
                        half speck=(N(p.xz*6.5*scale/1.1+.3)-.5)*.16+(N(p.xz*15*scale/1.1+.8)-.5)*.12*Resolve(.04,fp);
                        if(gravel)slabs=0;
                        half chip=lerp(.5+(big-.5)*.2,big,slabs);
                        half dust=smoothstep(.5,.75,N(p.xz*.035+.15))*(1-gap*.5);   // pale dust drifts
                        half damp=smoothstep(.62,.8,N(p.xz*.021+.55));                 // dark damp hollows
                        m=.8+chip*.18+speck+stone*(small*.22+.04)-(gap*.18+gap2*.1)*near;
                        tint=lerp(half3(1.05,.97,1.08),half3(.93,1,1.05),lerp(.5,Hash(id+3.7),Resolve(.45/scale,fp)));tint=lerp(tint,half3(.95,.98,1.04),Hash(id2+1.3)*.4*Resolve(.15/scale,fp));
                        m=lerp(m,1.18+(small-.5)*.08,dust*.55);tint=lerp(tint,half3(1.06,1.03,.97),dust*.6);
                        m*=lerp(1,.78,damp);smooth=lerp(min(_Smoothness,.08),.32,damp*damp);
                        if(gravel) {
                            float u=i.kit.x+(N(float2(i.kit.y*.02,.3))-.5)*.08;half rut=0;
                            rut=max(1-smoothstep(.03,.075,abs(u-.34)),1-smoothstep(.03,.075,abs(u-.66)));
                            half berm=smoothstep(.1,.02,u)+smoothstep(.9,.98,u);
                            m=lerp(m,.84+(small-.5)*.05,rut*.7);smooth=lerp(smooth,.2,rut*.6);tint=lerp(tint,half3(.97,.98,1.02),rut*.5);
                            m=lerp(m,.9+big*.36,berm*.6);
                            gap*=1-rut*.8;
                        }
                        bump=chip*.3+speck*1.5+stone*.7-gap*.5-gap2*.3+dust*.1;occlusion=1-(gap*.25+gap2*.2)*near;
                    } else {
                        half wz=abs(n.z)/(abs(n.x)+abs(n.z)+1e-4);
                        // (Triplanar: leaning faces, like the galleries' curved ends, also take a top-down sample, which the two
                        // side projections stretched into vertical streaks: street review, 4 Oct.)
                        half wy=smoothstep(.35,.7,up);
                        #define SN(sc,off) lerp(lerp(N(p.zy*(sc)+(off)),N(p.xy*(sc)+(off)),wz),N(p.xz*(sc)+(off)),wy)
                        half wob=SN(.05,.2);float bed=floor((p.y+wob*3)/1.7);half tone=Hash(float2(bed,4.7));
                        float cl=dot(p.xz,float2(.8,.6))*2.6+SN(.4,.6)*.7;half cleave=(1-smoothstep(.0,.09,abs(frac(cl)-.5)))*Resolve(.05,fp);
                        half crack=(1-smoothstep(.0,.07,abs(SN(.21,.9)-.5)))*Resolve(.12,fp);
                        half rust=smoothstep(.7,.85,N(float2(cl*.11,p.y*.03+.5)))*smoothstep(.4,.7,SN(.09,.3));
                        half fine2=SN(1.7,.1);
                        m=1+(tone-.5)*.3+(fine2-.5)*.1+(wob-.5)*.12-cleave*.08*near-crack*.12*near;
                        // Broad weathering: paler worked faces, darker wet streaks down from each bench, at gallery scale.
                        m*=lerp(.84,1.14,N(float2(dot(p.xz,float2(.8,.6))*.006,p.y*.012)+.37));
                        m-=smoothstep(.55,.8,N(float2(dot(p.xz,float2(.8,.6))*.09,p.y*.008+.2)))*.12;
                        tint=lerp(half3(1.07,.95,1.08),half3(.93,1,1.06),tone);tint=lerp(tint,half3(1.12,.97,.84),rust*.55);
                        bump=tone*.4+fine2*.3-cleave*.3-crack*.4;occlusion=1-crack*.3*near;
                        #undef SN
                        smooth=min(_Smoothness,.08);
                    }
                } else if(pattern==7) { // glass: darker at the bottom, a sky sheen at the top
                    m=1;smooth=.92;
                } else m=1+(fine-.5)*.04;
                m=lerp(lerp(1,m,.35),m,near);
                // Weathering near the base of walls, trims and stone (rising damp, splash-back).
                if(pattern==1||pattern==2||pattern==4||pattern==6) {
                    half base=(1-smoothstep(.0,1.2,height+(mottle-.5)*.6))*(1-up)*_Wear;
                    m*=1-base*.2;tint=lerp(tint,half3(.9,.95,.86),base*.5);
                }
                // Neighbouring buildings drift a little in warmth and value.
                half drift=N(p.xz*.0023+.17);tint*=lerp(half3(1.03,1,.96),half3(.97,.99,1.03),drift)*(1+(drift-.5)*.06);
                // Derivative bump from the pattern height (no tangents needed).
                float3 dpdx=ddx(p),dpdy=ddy(p);float dhx=ddx(bump),dhy=ddy(bump);
                float3 r1=cross(dpdy,n),r2=cross(n,dpdx);float det=dot(dpdx,r1);
                float strength=pattern==2?.04:pattern==11?.03:pattern==12?.03:pattern==3?.035:pattern==5?.06:pattern==8||pattern==10?.045:.02;
                float3 grad=sign(det)*(dhx*r1+dhy*r2)*strength*(.35+.65*near);half3 bumped=normalize(abs(det)*n-grad);
                if(abs(det)>1e-8&&near>0)n=bumped;
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=saturate(_BaseColor.rgb*m*tint);surface.metallic=_Metallic;surface.smoothness=saturate(smooth*(.85+fine*.3));
                surface.occlusion=occlusion;surface.alpha=1;surface.normalTS=half3(0,0,1);
                InputData data=(InputData)0;data.positionWS=p;data.positionCS=i.positionCS;data.normalWS=n;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(p);
                data.shadowCoord=TransformWorldToShadowCoord(p);data.bakedGI=SampleSH(n);data.shadowMask=half4(1,1,1,1);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                half4 colour=UniversalFragmentPBR(data,surface);
                if(pattern==7&&_KitNight>0) { // night: about half the windows lit, warm, per storey and bay
                    float3 cell=floor(p/float3(3.2,2.9,3.2));float h=frac(sin(dot(cell,float3(12.9898,78.233,37.719)))*43758.5453);
                    if(h<.55)colour.rgb+=lerp(half3(1,.62,.30),half3(1,.82,.55),frac(h*7.3))*(1.6+h*1.4)*_KitNight;
                }
                // Rim and bounce (docs/visual-quality-audit.md). A sky-tinted rim on silhouettes separates
                // shapes from what's behind them; warm light bounced off the sunlit ground lifts the lower,
                // side- and down-facing faces of walls, as it does outdoors. Both follow the real sky and sun.
                {
                    half3 sky=SampleSH(half3(0,1,0));half facing=saturate(dot(n,data.viewDirectionWS));
                    half rim=pow(1-facing,4)*saturate(n.y*.5+.6);
                    Light key=GetMainLight();half sunUp=saturate(key.direction.y*1.5);
                    half fromGround=saturate(-n.y*.5+.5)*saturate(1-height/9);
                    colour.rgb+=sky*rim*_KitRim+surface.albedo*_KitBounceColour.rgb*key.color*sunUp*fromGround*_KitBounce;
                }
                if(pattern==5&&near>0) { // light through the leaves
                    Light sun=GetMainLight(data.shadowCoord,p,half4(1,1,1,1));
                    colour.rgb+=surface.albedo*sun.color*saturate(-dot(n,sun.direction)+.2)*.22*sun.shadowAttenuation;
                }
                colour.rgb=ApplyHeightFog(MixFog(colour.rgb,i.fog),p);return colour;
            }
            ENDHLSL
        }
        // Own shadow/depth passes sharing this shader's UnityPerMaterial layout: borrowing Lit's
        // passes broke SRP-batcher compatibility and cost ~4.7 ms (review/map-sweep p1 study).
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment Nothing
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;float _Smoothness,_Metallic,_Pattern,_WorldSpace,_Wear,_Cutoff,_Cull;float4 _BaseMap_ST;
            CBUFFER_END
            float3 _LightDirection;float3 _LightPosition;
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 height:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 ShadowVert(Attributes input):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);float3 p=TransformObjectToWorld(input.positionOS.xyz);if(KIT_LEAF(_Pattern))p=KIT_WIND(p,input.height.x);if(_WorldSpace<.5&&input.height.y>0&&input.height.y<=1.001)p=KIT_FLAG(p,input.height.y);float3 n=TransformObjectToWorldNormal(input.normalOS);
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
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;float _Smoothness,_Metallic,_Pattern,_WorldSpace,_Wear,_Cutoff,_Cull;float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float2 height:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 DepthVert(Attributes input):SV_POSITION{UNITY_SETUP_INSTANCE_ID(input);float3 w=TransformObjectToWorld(input.positionOS.xyz);if(KIT_LEAF(_Pattern))w=KIT_WIND(w,input.height.x);if(_WorldSpace<.5&&input.height.y>0&&input.height.y<=1.001)w=KIT_FLAG(w,input.height.y);return TransformWorldToHClip(w);}
            // Depth fades with the colour pass, or a dissolving piece would leave holes in what's behind it.
            half4 DepthFrag(float4 positionCS:SV_POSITION):SV_Target
            {
                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(positionCS);
                #endif
                return 0;
            }
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
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;float _Smoothness,_Metallic,_Pattern,_WorldSpace,_Wear,_Cutoff,_Cull;float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 height:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;half3 normalWS:TEXCOORD0;};
            Varyings NormalsVert(Attributes input){UNITY_SETUP_INSTANCE_ID(input);Varyings o;float3 w=TransformObjectToWorld(input.positionOS.xyz);if(KIT_LEAF(_Pattern))w=KIT_WIND(w,input.height.x);if(_WorldSpace<.5&&input.height.y>0&&input.height.y<=1.001)w=KIT_FLAG(w,input.height.y);o.positionCS=TransformWorldToHClip(w);o.normalWS=TransformObjectToWorldNormal(input.normalOS);return o;}
            half4 NormalsFrag(Varyings i):SV_Target
            {
                #ifdef LOD_FADE_CROSSFADE
                    LODFadeCrossFade(i.positionCS);
                #endif
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
