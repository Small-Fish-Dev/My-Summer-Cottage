HEADER
{
	Description = "Ground Test";
	Version = 1;
}

FEATURES 
{
	#include "common/features.hlsl"
	Feature( F_ALPHA_TEST, 0..1, "Rendering" );
	Feature( F_TRANSPARENCY, 0..1, "Rendering" );
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
	#define S_TRANSLUCENT 0
	#define S_UV2 1
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
	float2 grassUV : TEXCOORD9 < Semantic( LowPrecisionUv1 ); >;
};

struct GeometryInput
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
	PixelInput MainVs( VertexInput i )		// I don't know why but game complains about incomplete MainVs initialization
	{
		PixelInput o = ProcessVertex( i );
		o = FinalizeVertex(o); 
		o.grassUV = o.vTextureCoords.zw;

		return o;
	}
}

GS
{	
	#include "geometry.hlsl"
}

//=========================================================================================================================

PS
{ 
	StaticCombo( S_TRANSPARENCY, F_TRANSPARENCY, Sys( ALL ) );
    StaticCombo( S_ALPHA_TEST, F_ALPHA_TEST, Sys( ALL ) );

	#define CUSTOM_TEXTURE_FILTERING
	SamplerState Sampler < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >; 

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );						// Whatever this means

	#define CUSTOM_MATERIAL_INPUTS

	CreateInputTexture2D( Color, 	Srgb, 8, "", "_color", 	"Material,10/10", Default3( 1.0, 1.0, 1.0 ) );		// Ground color
	CreateInputTexture2D( BillboardColor, Srgb, 8, "", "_color", "Material,20/10", Default3( 1.0, 1.0, 1.0 ) );	// Grass billboard color
	CreateInputTexture2D( Opacity, Linear, 8, "", "_alpha", "Material,30/10", Default( 1.0f ) );				// Grass billboard alpha


	// Store color map, include tint mask into alpha channel.
	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;	
	CreateTexture2DWithoutSampler( g_tGrass ) < Channel( RGB, Box( BillboardColor ), Srgb ); Channel( A, Box( Opacity ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;

	TextureAttribute(LightSim_DiffuseAlbedoTexture, g_tColor);
	TextureAttribute(RepresentativeTexture, g_tColor);

	#if S_ALPHA_TEST
		TextureAttribute( LightSim_Opacity_A, g_tGrass );
	#endif

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	#if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	#if ( S_TRANSPARENCY )
		#if( !F_RENDER_BACKFACES )
			#define BLEND_MODE_ALREADY_SET
			RenderState( BlendEnable, true );
			RenderState( SrcBlend, SRC_ALPHA );
			RenderState( DstBlend, INV_SRC_ALPHA);
		#endif

		BoolAttribute( translucent, true );
		float TransparencyRounding< Default( 0.0f ); Range( 0.0f, 1.0f ); UiGroup( "Transparency,10/20" ); >;
	#endif	

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 UV = i.vTextureCoords.xy;
		float2 UV2 = i.grassUV.xy; 

		float3 l_tColorMap = Tex2DS( g_tColor, Sampler, UV ).rgb;
		float4 l_tGrass = Tex2DS( g_tGrass, Sampler, UV2 ).rgba;

        Material m = Material::Init();

		m.Albedo = lerp( l_tColorMap.rgb, l_tGrass.rgb, ( UV2.x >= 0.01 || UV2.y >= 0.01 ) ? 1 : 0 );
		
		m.Opacity = 1;
        m.Roughness = 1;
        m.Metalness = 0;
        m.AmbientOcclusion = 1;

		m.Normal = TransformNormal( 1, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// Write to shading model 
		float4 result = ShadingModelStandard::Shade( i, m );

		#if( S_TRANSPARENCY )
			float alpha = lerp( 1, l_tGrass.a, ( UV2.x >= 0.01 || UV2.y >= 0.01 ) ? 1 : 0 );
			result.a = max( alpha, floor( alpha + TransparencyRounding ) );
		#endif		

		return result;
	}
}