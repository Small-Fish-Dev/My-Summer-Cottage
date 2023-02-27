HEADER
{
	Description = "Sauna Simple Shader";
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
FEATURES
{
    #include "common/features.hlsl"
	Feature( F_ALPHA_TEST, 0..1, "Rendering" );
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
MODES
{
	VrForward();
	ToolsVis( S_MODE_TOOLS_VIS );
	Depth( S_MODE_DEPTH );
}

//=========================================================================================================================
COMMON
{
	#include "common/shared.hlsl"
}

//=========================================================================================================================

struct VertexInput
{
	#include "common/vertexinput.hlsl"
};

//=========================================================================================================================

struct PixelInput
{
	#include "common/pixelinput.hlsl"
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"

	//
	// Main
	//
	PixelInput MainVs( INSTANCED_SHADER_PARAMS( VS_INPUT i ) )
	{
		PixelInput o = ProcessVertex( i );

        float3 vPositionWs = o.vPositionWs.xyz;
        float4 vertex = Position3WsToPs( vPositionWs.xyz );
		vertex.xyz = vertex.xyz / vertex.w;
		vertex.xy = floor( 240 * vertex.xy ) / 240;
		vertex.xyz *= vertex.w;

		o.vPositionPs = vertex;

		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{ 
	StaticCombo( S_ALPHA_TEST, F_ALPHA_TEST, Sys( ALL ) );
	   
	#define CUSTOM_TEXTURE_FILTERING
    SamplerState TextureFiltering < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );

    #include "sbox_pixel.fxc"

    #include "common/pixel.config.hlsl"
    #include "common/pixel.material.structs.hlsl"
    #include "common/pixel.lighting.hlsl"
    #include "common/pixel.shading.hlsl"

    #include "common/pixel.material.helpers.hlsl"
    
	CreateInputTexture2D( Color, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); Filter( POINT ); >;

    CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", "Material,10/20", Default3( 0.5, 0.5, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tNormal ) < Channel( RGB, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;

	CreateInputTexture2D( Roughness, Linear, 8, "", "_rough", "Material,10/30", Default( 1 ) );
	CreateTexture2DWithoutSampler( g_tRoughness ) < Channel( R, Box( Roughness ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

	#if ( S_MODE_DEPTH && !S_ALPHA_TEST )
        #define MainPs Disabled
    #endif
	
	#if ( S_ALPHA_TEST )
		CreateInputTexture2D( AlphaMask, Linear, 8, "", "_alpha", "Material,10/30", Default( 1 ) );
		CreateTexture2DWithoutSampler( g_tAlphaMask ) < Channel( R, Box( AlphaMask ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	#endif

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 UV = i.vTextureCoords.xy;

        Material m;
        m.Albedo = Tex2DS( g_tColor, TextureFiltering, UV.xy ).rgb;
        m.Normal = TransformNormal( i, DecodeNormal( Tex2DS( g_tNormal, TextureFiltering, UV.xy ).rgb ) );
        m.Roughness = Tex2DS( g_tRoughness, TextureFiltering, UV.xy ).r;
        m.Metalness = 0;
        m.AmbientOcclusion = 0.1;
        m.TintMask = 0;
        m.Opacity = 1;
        m.Emission = 0;
        m.Transmission = 1;

		ShadingModelValveStandard sm;
		float4 result = FinalizePixelMaterial( i, m, sm );
		#if( S_ALPHA_TEST )
			result.a = Tex2DS( g_tAlphaMask, TextureFiltering, UV.xy ).r;
		#endif

		return result;
	}
}