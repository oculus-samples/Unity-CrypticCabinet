// Copyright (c) Meta Platforms, Inc. and affiliates.

#ifndef MY_LIGHTING_INCLUDED
#define MY_LIGHTING_INCLUDED


void CalculateCastShadows_float (float3 WorldPos, half4 Shadowmask, out float ShadowAtten)
{
#if SHADERGRAPH_PREVIEW
    ShadowAtten = 0.5;
#else
    float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
#endif 
}
#endif 