#if ( PROGRAM == VFX_PROGRAM_PS )
float3 ApplyWithSplatdata( float4 l1, float4 l2, float4 l3, float4 l4, float4 splatmap )
{
    float l2blend = ComputeBlendWeight( splatmap.g, 0.05, l2.a );
    float l3blend = ComputeBlendWeight( splatmap.b, 0.05, l3.a );
    float l4blend = ComputeBlendWeight( splatmap.a, 0.05, l4.a );

    l2.rgb = lerp( l1.rgb, l2.rgb, l2blend );
    l3.rgb = lerp( l2.rgb, l3.rgb, l3blend );
    l4.rgb = lerp( l3.rgb, l4.rgb, l4blend );

    return l4.rgb;
}

float ApplyWithSplatdataBNW( float2 l1, float2 l2, float2 l3, float2 l4, float4 splatmap )
{
    float l2blend = ComputeBlendWeight( splatmap.g, 0.05, l2.g );
    float l3blend = ComputeBlendWeight( splatmap.b, 0.05, l3.g );
    float l4blend = ComputeBlendWeight( splatmap.a, 0.05, l4.g );

    l2.r = lerp( l1.r, l2.r, l2blend );
    l3.r = lerp( l2.r, l3.r, l3blend );
    l4.r = lerp( l3.r, l4.r, l4blend );

    return l4.r;
}

// Apply textures onto geometry with assigned UV. This is probably a horrible hack and there's a better way to do this. 
float3 PaintProceduralGeometry( float3 baseColor, float3 geometryTexture, float2 uv )
{
	return lerp( baseColor, geometryTexture, ( uv.x > 0 || uv.y > 0 ) ? 1 : 0 );    // <-- this is definitely can be improved, right? 
}
#endif

#if ( PROGRAM == VFX_PROGRAM_DS || PROGRAM == VFX_PROGRAM_HS )
struct TessellationFactors
{
	float edge[3] 	: SV_TessFactor;
	float inside 	: SV_InsideTessFactor;
};
#endif