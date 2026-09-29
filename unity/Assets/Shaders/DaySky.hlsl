// Procedural day/night sky (TimeOfDay sets every global each frame). Shared by the LittleRoad/DaySky
// skybox and the sea's reflection so the water always mirrors the sky above it.
// Sky gradient + Henyey-Greenstein sun glow, sun and moon discs, a star field, and one drifting cloud
// layer read from the same noise as the sun's cloud-shadow cookie (same size, same drift).
#ifndef LITTLE_ROAD_DAY_SKY
#define LITTLE_ROAD_DAY_SKY
float4 _DaySunDir;       // xyz: sun direction; w: 1 while the day sky is active
float4 _DayMoonDir;      // xyz: moon direction; w: moon brightness (0 by day)
half4 _DayZenith,_DayHorizon,_DayGlow,_DaySunDisc,_DayCloudLit,_DayCloudShade;
float4 _DayCloudParams;  // x: cover threshold, y: tile size (m), z: layer height (m), w: opacity
float4 _DayCloudOffset;  // xy: drift (m), z: star brightness, w: unused
TEXTURE2D(_DayCloudTex);SAMPLER(sampler_DayCloudTex);

float DaySkyHash(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}

// Cloud density (0..1) where the view ray meets the cloud layer; uv is returned for lighting.
float DaySkyClouds(float3 d,out float2 uv)
{
    float t=_DayCloudParams.z/max(d.y,.03);
    uv=(d.xz*t+_DayCloudOffset.xy)/_DayCloudParams.y;
    // Three octaves: big cumulus shapes, their lumps, and fine billows at the edges.
    float base=SAMPLE_TEXTURE2D(_DayCloudTex,sampler_DayCloudTex,uv).r;
    float lumps=SAMPLE_TEXTURE2D(_DayCloudTex,sampler_DayCloudTex,uv*2.7+float2(.37,.19)).r;
    float detail=SAMPLE_TEXTURE2D(_DayCloudTex,sampler_DayCloudTex,uv*7.3+_DayCloudOffset.xy*.00009).g;
    float c=_DayCloudParams.x;
    return smoothstep(c,c+.16,base*.72+lumps*.28+(detail-.5)*.16);
}

half3 DaySkyColour(float3 d,bool detail)
{
    float up=d.y,h=saturate(up);
    float3 sun=_DaySunDir.xyz;float cosS=dot(d,sun);
    half3 sky=lerp(_DayHorizon.rgb,_DayZenith.rgb,pow(h,.45));
    // Forward scattering round the sun (strongest near the horizon), and the warm band on the sun's side.
    float g=.74,mie=(1-g*g)/pow(max(1e-3,1+g*g-2*g*cosS),1.5);
    float side=saturate(cosS*.5+.5);
    sky+=_DayGlow.rgb*(mie*.03*(.3+.7*(1-h))+pow(side,5)*.45*pow(1-h,4));
    // Sun disc (soft limb) and moon disc with a faint halo.
    sky+=_DaySunDisc.rgb*smoothstep(.99962,.99986,cosS);
    float cosM=dot(d,_DayMoonDir.xyz);
    sky+=half3(.86,.9,1)*_DayMoonDir.w*(smoothstep(.99952,.99968,cosM)*2.4+pow(saturate(cosM),350)*.18);
    if(detail&&_DayCloudOffset.z>0&&up>0)
    {
        float2 p=float2(atan2(d.z,d.x)*100,asin(saturate(up))*190);float2 cell=floor(p);
        float r=DaySkyHash(cell);float2 at=float2(DaySkyHash(cell+7.1),DaySkyHash(cell+3.3))*.7+.15;
        float star=r>.94?smoothstep(.15,0,length(frac(p)-at))*pow((r-.94)*16.7,2):0;
        star*=.65+.35*sin(_Time.y*(1.5+r*4)+r*60);
        sky+=star*_DayCloudOffset.z*smoothstep(0,.2,up)*half3(.92,.95,1)*1.6;
    }
    if(up>0)
    {
        float2 uv;float density=DaySkyClouds(d,uv);
        // Lit edges face the sun: compare with the density a little way toward it.
        float2 toward=normalize(sun.xz+float2(1e-4,0))*.02;
        float sunward=smoothstep(_DayCloudParams.x,_DayCloudParams.x+.16,SAMPLE_TEXTURE2D(_DayCloudTex,sampler_DayCloudTex,uv+toward).r*.72+SAMPLE_TEXTURE2D(_DayCloudTex,sampler_DayCloudTex,(uv+toward)*2.7+float2(.37,.19)).r*.28);
        float lit=saturate(.72+(density-sunward)*1.6-density*.3);
        half3 cloud=lerp(_DayCloudShade.rgb,_DayCloudLit.rgb,lit);
        cloud+=_DayGlow.rgb*pow(saturate(cosS),6)*(1-density)*1.4;           // silver lining
        float fade=smoothstep(0,.12,up)*_DayCloudParams.w;
        sky=lerp(sky,cloud,density*fade);
    }
    // Into the haze at the horizon (fog colour), so terrain and sky meet without a seam.
    return lerp(unity_FogColor.rgb,sky,smoothstep(-.02,.14,up));
}
#endif
