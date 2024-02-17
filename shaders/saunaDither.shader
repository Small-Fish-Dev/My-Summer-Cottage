HEADER
{
	Description = "Sauna Dither";
}

MODES
{
	Default();
    VrForward();
}

COMMON
{
	#include "postprocess/shared.hlsl"
}

struct VS_INPUT
{
    float3 vPositionOs : POSITION < Semantic( PosXyz ); >;
};

struct PS_INPUT
{
	#if ( PROGRAM == VFX_PROGRAM_VS )
		float4 vPositionPs : SV_Position;
	#endif

	#if ( ( PROGRAM == VFX_PROGRAM_PS ) )
		float4 vPositionSs : SV_ScreenPosition;
	#endif
};

VS
{
    PS_INPUT MainVs( VS_INPUT i )
    {
        PS_INPUT o;

        o.vPositionPs = float4( i.vPositionOs.xyz, 1.0f );

        return o;
    }
}

PS
{
    #include "postprocess/common.hlsl"
    
    CreateTexture2D( g_ColorTexture ) < Attribute( "ColorTexture" ); SrgbRead( true ); Filter( POINT ); AddressU( MIRROR ); AddressV( MIRROR ); >;

    static const float bayerMatrix[8][8] = {
        0 / 64.0, 32 / 64.0, 8 / 64.0, 40 / 64.0, 2 / 64.0, 34 / 64.0, 10 / 64.0, 42 / 64.0,
        48 / 64.0, 16 / 64.0, 56 / 64.0, 24 / 64.0, 50 / 64.0, 18 / 64.0, 58 / 64.0, 26 / 64.0,
        12 / 64.0, 44 / 64.0, 4 / 64.0, 36 / 64.0, 14 / 64.0, 46 / 64.0, 6 / 64.0, 38 / 64.0,
        60 / 64.0, 28 / 64.0, 52 / 64.0, 20 / 64.0, 62 / 64.0, 30 / 64.0, 54 / 64.0, 22 / 64.0,
        3 / 64.0, 35 / 64.0, 11 / 64.0, 43 / 64.0, 1 / 64.0, 33 / 64.0, 9 / 64.0, 41 / 64.0,
        51 / 64.0, 19 / 64.0, 59 / 64.0, 27 / 64.0, 49 / 64.0, 17 / 64.0, 57 / 64.0, 25 / 64.0,
        15 / 64.0, 47 / 64.0, 7 / 64.0, 39 / 64.0, 13 / 64.0, 45 / 64.0, 5 / 64.0, 37 / 64.0,
        63 / 64.0, 31 / 64.0, 55 / 64.0, 23 / 64.0, 61 / 64.0, 29 / 64.0, 53 / 64.0, 21 / 64.0
    };

    float4 MainPs( PS_INPUT i ) : SV_Target0
    { 	
        const float levels = 128;

        float4 color = Tex2D( g_ColorTexture, i.vPositionSs.xy / g_vViewportSize.xy ).rgba;
        float2 pos = i.vPositionSs.xy / 2;
        float4 mask = bayerMatrix[pos.x % 8][pos.y % 8] - 0.5;

        return floor( levels * color + mask ) / levels;
    }
}