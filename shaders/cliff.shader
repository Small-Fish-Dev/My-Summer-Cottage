HEADER
{
	Description = "Cliff Shader";
	Version = 1;
	Description = "Simple triplanar-mapped shader by wheatleymf.";
}

FEATURES 
{
	#include "common/features.hlsl"
	Feature(F_SECOND_LAYER_TEXTURE, 0..1, "Cliff Settings");
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

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );						// Whatever this means
	StaticCombo( S_SECOND_LAYER, F_SECOND_LAYER_TEXTURE, Sys( PC ) );	// Local variable to add support for L2 texture

	#define CUSTOM_MATERIAL_INPUTS
	// Input boxes for Color map, tint mask and tint color.
	CreateInputTexture2D( Color, 	Srgb, 8, "", "_color", 	"Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( ColorTintMask, Linear, 8, "", "_tint", "Material,10/20", Default3( 1.0, 1.0, 1.0 ) );
	float3 g_flColorTint < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material,10/20" ); >;

	// Store color map, include tint mask into alpha channel.
	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); Channel( A, Box( ColorTintMask ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;	

	// Global model normal map.
	CreateInputTexture2D( GlobalNormal, Linear, 8, "NormalizeNormals", "_glnormal", "Material,10/30", Default3( 0.5, 0.5, 1.0) );
	CreateTexture2DWithoutSampler( g_tGlNormal ) < Channel( RGB, Box( GlobalNormal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
	float GlobalNormalStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 50.0 ); UiGroup( "Material,10/30"); >;

	// Store normal map. Normal strength can be adjusted.
    CreateInputTexture2D( DetailNormal, Linear, 8, "NormalizeNormals", "_normal", "Material,10/40", Default3( 0.5, 0.5, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tNormal ) < Channel( RGB, Box( DetailNormal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
	float NormalStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 50.0 ); UiGroup( "Material,10/40"); >; 

	// Roughness, Metalness and Ambient Occlusion - everything gets packed into a signle RGB texture. AO strength can be adjusted.
	CreateInputTexture2D( Roughness, 		Linear, 8, "", "_rough", 	"Material,10/50", Default( 1 ) );
	CreateInputTexture2D( Metalness, 		Linear, 8, "", "_metal", 	"Material,10/60", Default( 1.0 ) );
	CreateInputTexture2D( AmbientOcclusion, Linear, 8, "", "_ao",  		"Material,10/70", Default( 1.0 ) );
	float AmbientOcclusionStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 10.0 ); UiGroup( "Material,10/70" ); >;

	CreateTexture2DWithoutSampler( g_tRmo ) < Channel( R, Box( Roughness ), Linear ); Channel( G, Box( Metalness ), Linear ); Channel( B, Box( AmbientOcclusion ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

	// Cliff mesh details - peaks, direction and dirt masks. Dirt mask's black color indicates which areas should be darkened.
	CreateInputTexture2D( CliffPeaks, 	  Linear, 8, "", "_peaks", 	"Cliff Model Data,20/10", Default( 1 ) );	// R
	CreateInputTexture2D( CliffDirection, Linear, 8, "", "_dir", 	"Cliff Model Data,20/20", Default( 0 ) );	// G
	CreateInputTexture2D( CliffDirt,	  Linear, 8, "", "_dirt", 	"Cliff Model Data,20/30", Default( 1 ) );	// B
	// Free texture input slot for A channel.
	CreateTexture2DWithoutSampler( g_tCmd ) < Channel( R, Box( CliffPeaks ), Linear ); Channel( G, Box( CliffDirection ), Linear ); Channel( B, Box( CliffDirt ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >; 

	// Include sliders to customize influence of CMD maps.
	float PeaksStrength 	< UiType( Slider ); Default( 0.25f ); Range(0, 5); UiGroup("Cliff Model Data,20/30"); >;
	float DirectionStrength < UiType( Slider ); Default( 0.5f  ); Range(0, 5); UiGroup("Cliff Model Data,20/30"); >; 
	float DirtStrength		< UiType( Slider ); Default( 1.0f  ); Range(0, 3); UiGroup("Cliff Model Data,20/30"); >;

	// Second layer that can be applied with the usage of direction map.
	#if (S_SECOND_LAYER)
		CreateInputTexture2D( LayerTwoColor, 			Srgb, 	8, "", 					"_color2", 	"L2 Texture,30/10", Default3( 1.0, 1.0, 1.0 ) );	// Tex1-RGB
		CreateInputTexture2D( LayerTwoNormal, 			Linear, 8, "NormalizeNormals", 	"_normal2", "L2 Texture,30/20", Default3( 1.0, 1.0, 1.0 ) );	// Tex2-RGB
		CreateInputTexture2D( LayerTwoRoughness, 		Linear, 8, "", 					"_rough2", 	"L2 Texture,30/30", Default( 0.5 ) );				// Tex3-R
		CreateInputTexture2D( LayerTwoMetalness, 		Linear, 8, "", 					"_metal2", 	"L2 Texture,30/40", Default( 0 ) );					// Tex3-G
		CreateInputTexture2D( LayerTwoAmbientOcclusion, Linear, 8, "", 					"_ao2", 	"L2 Texture,30/50", Default( 1 ) );					// Tex3-B

		CreateTexture2DWithoutSampler( g_tColor_L2 ) 	< Channel( RGB, Box( LayerTwoColor ), 	Srgb ); 	OutputFormat( BC7 );  SrgbRead( true ); >;
		CreateTexture2DWithoutSampler( g_tNormal_L2 ) 	< Channel( RGB, Box( LayerTwoNormal ), 	Linear ); 	OutputFormat( DXT5 ); SrgbRead( false ); >;
		CreateTexture2DWithoutSampler( g_tRmo_L2 ) 		< Channel( R, 	Box( LayerTwoRoughness ), Linear ); Channel( G, Box( LayerTwoMetalness ), Linear); Channel( B, Box( LayerTwoAmbientOcclusion ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	#endif 

	// Triplanar mapping settings
	float TextureTiling 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 2048.0f ); UiGroup("Triplanar Settings,40/10"); >;
	float TextureBlending 	< UiType( VectorText ); Default( 1.0f ); Range ( 0.0f,   10.0f ); UiGroup("Triplanar Settings,40/20"); >;
	float TextureScale		< UiType( Slider ); 	Default( 1.0f ); Range ( 0.0f,   20.0f ); UiGroup("Triplanar Settings,40/30"); >;

	// Add separate settings for L2 textures
	#if (S_SECOND_LAYER)
		float TextureTilingB 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 2048.0f ); UiGroup("Triplanar Settings,40/10"); >;
		float TextureBlendingB 	< UiType( VectorText ); Default( 1.0f ); Range ( 0.0f,   10.0f ); UiGroup("Triplanar Settings,40/20"); >;
		float TextureScaleB		< UiType( Slider ); 	Default( 1.0f ); Range ( 0.0f,   20.0f ); UiGroup("Triplanar Settings,40/30"); >;
	#endif

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
	#include "cliff_utils.hlsl" 			// Some stuff like normal map blending and linear dodge blend mode
	#include "common/utils/triplanar.hlsl" 	// For triplanar texture mapping

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	#if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 UV = i.vTextureCoords.xy;	// Used for default texture mapping using mesh UV.
		float fac = 8;						// Used in TextureTiling "math" to make scale control feel less clunky. I probably can implement this in a better way. 

		// Preparing cliff mesh data (peaks, distance & dirt masks) and then color map.
		float3 		l_tCmd = Tex2DS( g_tCmd, Sampler, UV.xy ).rgb;	// R = Peaks, G = Direction, B = Dirt
		float4 l_tColorMap = Tex2DTriplanar( g_tColor, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale).rgba;

		// Preparing L2 textures here.
		#if (S_SECOND_LAYER)
			float3 l_tColorMap2 = 	Tex2DTriplanar( g_tColor_L2, Sampler, i, TextureTilingB / fac, TextureBlendingB, TextureScaleB ).rgb;
			float3 l_tNormalMap2 = 	DecodeNormal( Tex2DTriplanar( g_tNormal_L2, Sampler, i, TextureTilingB / fac, TextureBlendingB, TextureScaleB).rgb );
			float3 l_tRmo2 = 		Tex2DTriplanar( g_tRmo_L2, Sampler, i, TextureTilingB / fac, TextureBlendingB, TextureScaleB ).rgb;
		#endif

        Material m = Material::Init();

		// Building final albedo texture: 1) apply tint color according to tint mask onto base color; 2) apply dirt mask; 3) apply peaks mask.
		l_tColorMap.rgb = abs( l_tColorMap.rgb - l_tCmd.b * (DirtStrength / 6) );	// difference
		m.Albedo = BlendLinearDodge( lerp( l_tColorMap.rgb, (l_tColorMap.rgb) * g_flColorTint, l_tColorMap.a), l_tCmd.r, PeaksStrength );	// linear dodge

		// Combine previous albedo texture with 2nd layer color map 
		#if (S_SECOND_LAYER)
			m.Albedo = lerp( m.Albedo, l_tColorMap2, l_tCmd.g );
		#endif

		// Loading up & instantly decoding normal maps. (global & detail)
		float3 l_tGlNormalMap = DecodeNormal( Tex2DS( g_tGlNormal, Sampler, UV.xy).rgb ); 																							// Model's normal map - regular UV mapping
		float3   l_tNormalMap = DecodeNormal( Tex2DTriplanar( g_tNormal, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale ).rgb );	
		// Normal map combined 
		float3 l_tNormalMapBl = BlendNormals( float3( l_tGlNormalMap.rg * GlobalNormalStrength, l_tGlNormalMap.b ), float3( l_tNormalMap.rg * NormalStrength, l_tNormalMap.b) );	// Combined maps

		// Loading up roughness/metalness/ambient occlusion maps. They're used for shading the mesh itself. 
		// Roughness/metalness maps are triplanar mapped and related to detail texture. AO is shading the mesh globally. 
		float2 rm = Tex2DTriplanar( g_tRmo, Sampler, i, TextureTiling / fac, TextureBlending, TextureScale ).rg;
		float  ao = Tex2DS( g_tRmo, Sampler, UV.xy).b;
        m.Roughness = rm.r;	// TODO? Make peaks mask influence the roughness (make it shinier on curves)
        m.Metalness = rm.g;
        m.AmbientOcclusion = ao / AmbientOcclusionStrength;
        m.TintMask = Tex2DS( g_tColor, Sampler, UV.xy ).a;

		// Generate initial normal map blends. If L2 is not enabled, material will be utilizing this map. 
		m.Normal = TransformNormal( l_tNormalMapBl, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// If second layer is enabled, overwrite previous normal map assignation and combine blend maps + L2 normal map.
		// Combine all other maps, too.
		#if (S_SECOND_LAYER)
			m.Normal = TransformNormal( lerp( l_tNormalMapBl, l_tNormalMap2, l_tCmd.g ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
			m.Roughness = lerp( rm.r, l_tRmo2.r, l_tCmd.g );	// There's a chance these maps are blended with main texture incorrectly. Test with other mats first.
			m.Metalness = lerp( rm.g, l_tRmo2.g, l_tCmd.g );
			m.AmbientOcclusion = lerp( m.AmbientOcclusion, l_tRmo2.b / AmbientOcclusionStrength, l_tCmd.g);
		#endif

		// Write to shading model 
		float4 result = ShadingModelStandard::Shade( i, m );

		return result;
	}
}