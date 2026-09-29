// Free-roam maps (Minimap.cs, PauseMenu.cs): the baked top-down world map (WorldMap.Bake).
// Corner map (_Circle 1): centred on the player, rotated so their heading points up, cropped to a circle.
// Flat maps (_Circle 0): the RawImage's own uv, square. Both hide unexplored ground under soft cloud
// (ExploredMap: global _ExploredFog mask, on when _ExploredFogOn is 1). UI shader for overlay canvases.
Shader "LittleRoad/Minimap"
{
    Properties
    {
        [PerRendererData] _MainTex("Map",2D)="white"{}
        _Center("Centre (map uv)",Vector)=(.5,.5,0,0)
        _Angle("Heading (radians)",Float)=0
        _Span("Radius (map uv)",Float)=.06
        _Circle("Circular corner map",Float)=1
        _Edge("Edge colour",Color)=(.11,.17,.39,1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="False"}
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Center;float _Angle,_Span,_Circle;fixed4 _Edge;
            sampler2D _ExploredFog;float _ExploredFogOn;
            struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+float2(1,1)),f.x),f.y);}
            float clouds(float2 p){return noise(p)*.55+noise(p*2.3+7.1)*.3+noise(p*5.1+3.7)*.15;}
            // Explored amount with a soft edge: five taps about one and a half cells apart.
            float explored(float2 uv)
            {
                float d=1.5/256;
                float s=tex2D(_ExploredFog,uv).r*2+tex2D(_ExploredFog,uv+float2(d,0)).r+tex2D(_ExploredFog,uv-float2(d,0)).r
                    +tex2D(_ExploredFog,uv+float2(0,d)).r+tex2D(_ExploredFog,uv-float2(0,d)).r;
                return smoothstep(.25,.85,s/6);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 l=(i.uv-.5)*2;float r=length(l);
                float c=cos(_Angle),s=sin(_Angle);
                float2 w=float2(l.x*c+l.y*s,-l.x*s+l.y*c);
                float2 uv=_Circle>.5?_Center.xy+w*_Span:i.uv;
                fixed4 map=tex2D(_MainTex,uv);
                bool outside=any(uv<0)||any(uv>1);
                // Outside the baked area: sea.
                if(outside)map=fixed4(.05,.20,.44,1);
                else if(_ExploredFogOn>.5) {
                    // Unexplored: opaque drifting cloud; nothing of what's underneath shows.
                    float seen=explored(uv);
                    float n=clouds(uv*38+_Time.x*.6);
                    fixed3 cloud=lerp(fixed3(.66,.73,.81),fixed3(.88,.91,.95),n);
                    map.rgb=lerp(cloud,map.rgb,seen);
                }
                if(_Circle>.5) {
                    // Soft inner vignette towards the rim, then a crisp circular edge.
                    map.rgb*=lerp(1,.8,smoothstep(.75,1,r));
                    float aa=fwidth(r)*1.5;
                    map.a=1-smoothstep(1-aa,1,r);
                } else map.a=1;
                return map*i.color;
            }
            ENDCG
        }
    }
}
