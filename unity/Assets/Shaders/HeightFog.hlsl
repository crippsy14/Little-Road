// Low-lying haze (WorldLook.HeightFog): thickest over the valley floor, thinning with height, growing
// with distance. URP's own fog is distance-only; every LittleRoad shader applies this after MixFog so
// hollows and far valley floors sit in air. Globals: colour.a = maximum opacity;
// params: x = density per metre of view distance, y = base height, z = falloff metres.
#ifndef LITTLE_ROAD_HEIGHT_FOG
#define LITTLE_ROAD_HEIGHT_FOG
half4 _HeightFogColor;
float4 _HeightFogParams;
half3 ApplyHeightFog(half3 colour,float3 positionWS)
{
    if(_HeightFogColor.a<=0)return colour;
    float distanceWS=distance(positionWS,_WorldSpaceCameraPos);
    float layer=exp(-max(0,positionWS.y-_HeightFogParams.y)/max(1,_HeightFogParams.z));
    float amount=(1-exp(-distanceWS*_HeightFogParams.x))*layer*_HeightFogColor.a;
    return lerp(colour,_HeightFogColor.rgb,saturate(amount));
}
#endif
