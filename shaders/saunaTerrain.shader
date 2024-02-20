HEADER
{
	Description = "Sauna Terrain Shader";
	Version = 1;
	Description = "Testicular torsion";
}

FEATURES 
{
	#include "common/features.hlsl"
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
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{ 
	#define CUSTOM_TEXTURE_FILTERING
	SamplerState Sampler < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >; 
	SamplerState SamplerAniso < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >; 


	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );						// Whatever this means

	#define CUSTOM_MATERIAL_INPUTS
	// Layer 1-4 color maps
	CreateInputTexture2D( LayerA, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( LayerB, Srgb, 8, "", "_color", "Material,10/20", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( LayerC, Srgb, 8, "", "_color", "Material,10/30", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( LayerD, Srgb, 8, "", "_color", "Material,10/40", Default3( 1.0, 1.0, 1.0 ) );

	CreateTexture2DWithoutSampler( g_tColor_L1 ) < Channel( RGB, Box( LayerA ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tColor_L2 ) < Channel( RGB, Box( LayerB ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tColor_L3 ) < Channel( RGB, Box( LayerC ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tColor_L4 ) < Channel( RGB, Box( LayerD ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;

	// Splat map
	CreateInputTexture2D( SplatA, Linear, 8, "", "_splat", "Splatmap,20/10", Default( 1 ) );
	CreateInputTexture2D( SplatB, Linear, 8, "", "_splat", "Splatmap,20/20", Default( 1 ) );
	CreateInputTexture2D( SplatC, Linear, 8, "", "_splat", "Splatmap,20/30", Default( 1 ) );
	CreateInputTexture2D( SplatD, Linear, 8, "", "_splat", "Splatmap,20/40", Default( 1 ) );

	CreateTexture2DWithoutSampler( g_tSplat ) < Channel( R, Box( SplatA ), Linear ); Channel( G, Box( SplatB ), Linear ); Channel( B, Box( SplatC ), Linear ); Channel( A, Box( SplatD ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

	// Triplanar settings
	float TextureTiling 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 2048.0f ); UiGroup("Triplanar Settings,30/10"); >;
	float TextureBlending 	< UiType( VectorText ); Default( 1.0f ); Range ( 0.0f,   10.0f ); UiGroup("Triplanar Settings,30/20"); >;
	float TextureScale		< UiType( Slider ); 	Default( 1.0f ); Range ( 0.0f,   20.0f ); UiGroup("Triplanar Settings,30/30"); >;

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
	#include "common/utils/triplanar.hlsl" 	
	#include "terrain_utils.hlsl"

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	#if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float fac = 8;
		float2 UV = i.vTextureCoords.xy;	// Used for default texture mapping using mesh UV.

        Material m = Material::Init();

		// prepare layer textures with triplanar mapping applied
		float3 l_tLayerA = Tex2DTriplanar( g_tColor_L1, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale).rgb;
		float3 l_tLayerB = Tex2DTriplanar( g_tColor_L2, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale).rgb;
		float3 l_tLayerC = Tex2DTriplanar( g_tColor_L3, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale).rgb;
		float3 l_tLayerD = Tex2DTriplanar( g_tColor_L4, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale).rgb;

		// prepare splat data
		float4 l_tSplatData = Tex2DS( g_tSplat, SamplerAniso, UV.xy ).rgba;

		m.Albedo = ApplySplatdata( l_tLayerA, l_tLayerB, l_tLayerC, l_tLayerD, l_tSplatData );
		
		m.Normal = TransformNormal(DecodeNormal(float3(0.5, 0.5, 1)), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		m.Roughness = 1;		// Temporarily set values
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		// Write to shading model 
		float4 result = ShadingModelStandard::Shade( i, m );

		return result;
	}
}