float3 ApplySplatdata( float3 l1, float3 l2, float3 l3, float3 l4, float4 splatmap )
{
    l2 = lerp( l1, l2, splatmap.g );
    l3 = lerp( l2, l3, splatmap.b );
    l4 = lerp( l3, l4, splatmap.a );

    // holy recursion batman
    return  lerp( lerp( lerp( l1, l2, splatmap.g ), l3, splatmap.b ), l4, splatmap.a );
}