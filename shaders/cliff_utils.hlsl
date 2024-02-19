#ifndef CLIFF_UTILS_H
#define CLIFF_UTILS_H

// Old normal map blending method, not used due to being shite and really really close to linear implementation.
float3 BlendNormalsLinear(float3 nrmA, float3 nrmB) 
{
    float3 c = float3(2, 1, 0);
    float3 r;
    r = nrmB * c.yyz + nrmA.xyz;
    r =    r * c.xxx -    c.xxy;

    return normalize(r);
}

// Keep in mind that passed normal maps here must be unpacked already. (hence why it returns normal map without normalize() )
float3 BlendNormals(float3 NrmA, float3 NrmB) 
{
    NrmA += float3(  0,  0, 1 );
    NrmB *= float3( -1, -1, 1 );

    return NrmA * dot( NrmA, NrmB ) / NrmA.z - NrmB;
}

// Linear Dodge blend mode
float3 BlendLinearDodge(float3 clr, float mask, float strength)
{
    return min( 1, clr + (mask * strength) );
}

#endif