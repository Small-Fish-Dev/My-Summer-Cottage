HEADER
{
	Description = "Sauna Censor";
}
FEATURES
{
    #include "common/features.hlsl"
}

COMMON
{
	#define USE_CUSTOM_SHADING
	#include "common/shared.hlsl"
}


struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VertexInput i ) )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

PS
{
    RenderState( DepthWriteEnable, true );
    RenderState( DepthEnable, true );

    CreateTexture2D( g_tDepthTexture ) < Attribute( "DepthTexture" ); SrgbRead( true ); Filter( POINT ); >;
	CreateTexture2D( g_tColorTexture ) < Attribute( "ColorTexture" ); SrgbRead( true ); Filter( POINT ); >;

    float4 MainPs( PS_INPUT i ) : SV_Target0
    { 	
        float objectDepth = i.vPositionSs.z;

		float2 screenUv = CalculateViewportUv( i.vPositionSs.xy ); 

        float worldDepth = Tex2D( g_tDepthTexture, screenUv ).r;
		worldDepth = RemapValClamped( worldDepth, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0 );
		float diff = (objectDepth - worldDepth);
		diff = RemapValClamped( diff, 0.001, 0.002f, 0.0, 1.0);
		
        float2 amount = g_vViewportSize.xy / 16;
        float2 coords = round( i.vPositionSs.xy / g_vViewportSize.xy * amount ) / amount;

        float4 censorColor = Tex2D( g_tColorTexture, coords ).rgba;
        float4 bufferColor = Tex2D( g_tColorTexture, i.vPositionSs.xy / g_vViewportSize.xy ).rgba;

        return lerp( censorColor, bufferColor, diff );
    }
}