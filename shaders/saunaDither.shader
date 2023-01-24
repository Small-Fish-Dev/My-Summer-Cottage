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
    
    CreateTexture2D( g_tColorBuffer ) < Attribute( "ColorBuffer" ); SrgbRead( true ); Filter( MIN_MAG_LINEAR_MIP_POINT ); AddressU( MIRROR ); AddressV( MIRROR ); >;

    float4 dither( float4 color, uint x, uint y, int levels = 4)
    {
        const float thresholdMap[8][8] = {
            0/64.0-0.5, 32/64.0-0.5, 8/64.0-0.5, 40/64.0-0.5, 2/64.0-0.5, 34/64.0-0.5, 10/64.0-0.5, 42/64.0-0.5,
            48/64.0-0.5, 16/64.0-0.5, 56/64.0-0.5, 24/64.0-0.5, 50/64.0-0.5, 18/64.0-0.5, 58/64.0-0.5, 26/64.0-0.5,
            12/64.0-0.5, 44/64.0-0.5, 4/64.0-0.5, 36/64.0-0.5, 14/64.0-0.5, 46/64.0-0.5, 6/64.0-0.5, 38/64.0-0.5,
            60/64.0-0.5, 28/64.0-0.5, 52/64.0-0.5, 20/64.0-0.5, 62/64.0-0.5, 30/64.0-0.5, 54/64.0-0.5, 22/64.0-0.5,
            3/64.0-0.5, 35/64.0-0.5, 11/64.0-0.5, 43/64.0-0.5, 1/64.0-0.5, 33/64.0-0.5, 9/64.0-0.5, 41/64.0-0.5,
            51/64.0-0.5, 19/64.0-0.5, 59/64.0-0.5, 27/64.0-0.5, 49/64.0-0.5, 17/64.0-0.5, 57/64.0-0.5, 25/64.0-0.5,
            15/64.0-0.5, 47/64.0-0.5, 7/64.0-0.5, 39/64.0-0.5, 13/64.0-0.5, 45/64.0-0.5, 5/64.0-0.5, 37/64.0-0.5,
            63/64.0-0.5, 31/64.0-0.5, 55/64.0-0.5, 23/64.0-0.5, 61/64.0-0.5, 29/64.0-0.5, 53/64.0-0.5, 21/64.0-0.5
        };

        float4 mask = thresholdMap[x % 8][y % 8];
        return floor( levels * color + mask ) / levels;
    }

    float4 MainPs( PS_INPUT i ) : SV_Target0
    { 	
        float2 amount = g_vViewportSize.xy / 4;
        float2 coords = round( i.vPositionSs.xy / g_vViewportSize.xy * amount ) / amount;
        float4 col = Tex2D( g_tColorBuffer, coords.xy ).rgba;

        return dither( col, i.vPositionSs.x / 2, i.vPositionSs.y / 2, 64 );
    }
}