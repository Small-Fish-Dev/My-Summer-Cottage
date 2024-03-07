HEADER
{
	Description = "Sauna Terrain Shader";
	Version = 1;
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
	ToolsWireframe( "vr_tools_wireframe.shader" );
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

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 AbsolutePosition : TEXCOORD10;
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		o = FinalizeVertex(o); 
		o.AbsolutePosition = i.vPositionOs.xyz;	// Include vertex data into PixelInput so we can use it for per-triangle grass tinting

		return o;
	}
}

//=========================================================================================================================

//=========================================================================================================================

PS
{ 

	#define CUSTOM_TEXTURE_FILTERING
	SamplerState SamplerPoint < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >; 
	SamplerState SamplerAniso < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >; // Two samplers is probably stupid but hey, "as long as it works"(tm)

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );	// Whatever this means

	#define CUSTOM_MATERIAL_INPUTS

	// ---
	// Splat texture maps. Currently they contain only color, normal, roughness and mask maps.
	// ---
	CreateInputTexture2D( Splat_ColorA, Srgb, 8, "", "_color", "Splat Textures A,30/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_ColorB, Srgb, 8, "", "_color", "Splat Textures B,40/20", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_ColorC, Srgb, 8, "", "_color", "Splat Textures C,50/30", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_ColorD, Srgb, 8, "", "_color", "Splat Textures D,60/40", Default3( 1.0, 1.0, 1.0 ) );

	CreateInputTexture2D( Splat_NormalA, Srgb, 8, "NormalizeNormals", "_normal", "Splat Textures A,30/20", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_NormalB, Srgb, 8, "NormalizeNormals", "_normal", "Splat Textures B,40/20", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_NormalC, Srgb, 8, "NormalizeNormals", "_normal", "Splat Textures C,50/20", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_NormalD, Srgb, 8, "NormalizeNormals", "_normal", "Splat Textures D,60/20", Default3( 1.0, 1.0, 1.0 ) );

	// Global normal map
	CreateInputTexture2D( Splat_GlobalNormal, Srgb, 8, "NormalizeNormals", "_normal", "Splat Map,70/50", Default3( 1.0, 1.0, 1.0 ) );
	CreateInputTexture2D( Splat_GlobalAO, Linear, 8, "", "_ao", "Spalt Map,70/60", Default( 1.0 ) );

	CreateInputTexture2D( Splat_Roughness_A, Linear, 8, "", "_rough", "Splat Textures A,30/30", Default( 0.5 ) );
	CreateInputTexture2D( Splat_Roughness_B, Linear, 8, "", "_rough", "Splat Textures B,40/30", Default( 0.5 ) );
	CreateInputTexture2D( Splat_Roughness_C, Linear, 8, "", "_rough", "Splat Textures C,50/30", Default( 0.5 ) );
	CreateInputTexture2D( Splat_Roughness_D, Linear, 8, "", "_rough", "Splat Textures D,60/30", Default( 0.5 ) );

	CreateInputTexture2D( Splat_BlendMask_A, Linear, 8, "", "_mask", "Splat Textures A,30/40", Default( 0.5 ) );
	CreateInputTexture2D( Splat_BlendMask_B, Linear, 8, "", "_mask", "Splat Textures B,40/40", Default( 0.5 ) );	
	CreateInputTexture2D( Splat_BlendMask_C, Linear, 8, "", "_mask", "Splat Textures C,50/40", Default( 0.5 ) );	
	CreateInputTexture2D( Splat_BlendMask_D, Linear, 8, "", "_mask", "Splat Textures D,60/40", Default( 0.5 ) );

	// Build color map with blend mask included into alpha channel
	CreateTexture2DWithoutSampler( g_tSplatColor_A ) < Channel( RGB, Box( Splat_ColorA ), Srgb ); Channel( A, Box( Splat_BlendMask_A ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatColor_B ) < Channel( RGB, Box( Splat_ColorB ), Srgb ); Channel( A, Box( Splat_BlendMask_B ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatColor_C ) < Channel( RGB, Box( Splat_ColorC ), Srgb ); Channel( A, Box( Splat_BlendMask_C ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatColor_D ) < Channel( RGB, Box( Splat_ColorD ), Srgb ); Channel( A, Box( Splat_BlendMask_D ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;

	// Build normal maps
	CreateTexture2DWithoutSampler( g_tSplatNormal_A ) < Channel( RGB, Box( Splat_NormalA ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatNormal_B ) < Channel( RGB, Box( Splat_NormalB ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatNormal_C ) < Channel( RGB, Box( Splat_NormalC ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tSplatNormal_D ) < Channel( RGB, Box( Splat_NormalD ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	CreateTexture2DWithoutSampler( g_tGlobalNormal ) < Channel( RGB, Box( Splat_GlobalNormal ), Srgb ); Channel( A, Box( Splat_GlobalAO ), Linear ); OutputFormat( DXT5 ); >;

	// Build roughness maps
	CreateTexture2DWithoutSampler( g_tSplatRoughness ) < Channel( R, Box( Splat_Roughness_A ), Linear ); Channel( G, Box( Splat_Roughness_B ), Linear ); Channel( B, Box( Splat_Roughness_C ), Linear ); Channel( A, Box( Splat_Roughness_D ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	
	// --- 
	// Splat assignation, up to 4 layers, RGBA format.
	// Splat maps order is important. 
	// ---
	CreateInputTexture2D( Splat_LayerA, Linear, 8, "", "_layerA", "Splat Map,70/10", Default( 0.0 ) ); // Red channel; "Base" layer
	CreateInputTexture2D( Splat_LayerB, Linear, 8, "", "_layerB", "Splat Map,70/20", Default( 0.0 ) ); // Green channel;
	CreateInputTexture2D( Splat_LayerC, Linear, 8, "", "_layerC", "Splat Map,70/30", Default( 0.0 ) ); // Blue channel;
	CreateInputTexture2D( Splat_LayerD, Linear, 8, "", "_layerD", "Splat Map,70/40", Default( 0.0 ) ); // Alpha channel; Last layer, always drawn on top of everything else.

	CreateTexture2DWithoutSampler( g_tSplatMap ) < Channel( R, Box( Splat_LayerA ), Linear ); Channel( G, Box( Splat_LayerB ), Linear ); Channel( B, Box( Splat_LayerC ), Linear ); Channel( A, Box( Splat_LayerD ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	
	float shoremin < UiType( Slider ); Default( 0.0f ); Range( -500.0f, 5500.0f ); UiGroup("Shoreline Wetness,90/10"); >;
	float shoremax < UiType( Slider ); Default( 25.0f); Range( -500.0f, 5500.0f ); UiGroup("Shoreline Wetness,90/20"); >;

	// ---
	// Triplanar settings
	// ---
	float TextureTiling 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 2048.0f ); UiGroup("Triplanar Settings,80/10"); >;
	float TextureBlending 	< UiType( VectorText ); Default( 1.0f ); Range ( 0.0f,   10.0f ); UiGroup("Triplanar Settings,80/20"); >;
	float TextureScale		< UiType( Slider ); 	Default( 1.0f ); Range ( 0.0f,   64.0f ); UiGroup("Triplanar Settings,80/30"); >;

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
	#include "terrain_utils.hlsl"
	#include "common/utils/triplanar.hlsl" 	
	#include "cliff_utils.hlsl"

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

		float scaleFactor = distance( g_vCameraPositionWs, i.AbsolutePosition.xyz );
		// Prepare splat map data.
		float4 l_tSplatData = Tex2DS( g_tSplatMap, SamplerAniso, UV.xy ).rgba;

		// Fucking mess
		float3 l_tSplatColor_LOD = ApplyWithSplatdata( 
			Tex2DS( g_tSplatColor_A, SamplerPoint, UV.xy * 42 ).rgba,
			Tex2DS( g_tSplatColor_B, SamplerPoint, UV.xy * 42 ).rgba,
			Tex2DS( g_tSplatColor_C, SamplerPoint, UV.xy * 42 ).rgba,
			Tex2DS( g_tSplatColor_D, SamplerPoint, UV.xy * 42 ).rgba, l_tSplatData
		);

		// Prepare global normal
		float4 l_tGlobalNormal = Tex2DS( g_tGlobalNormal, SamplerAniso, UV.xy ).rgba;
		float3 globalnormal = DecodeNormal( l_tGlobalNormal.rgb );

		// Prepare color maps (+ blend masks in alpha channel)
		float4 l_tSplatColor_A = Tex2DTriplanar( g_tSplatColor_A, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_B = Tex2DTriplanar( g_tSplatColor_B, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_C = Tex2DTriplanar( g_tSplatColor_C, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_D = Tex2DTriplanar( g_tSplatColor_D, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;

		// Prepare normal maps
		float3 l_tSplatNormal_A = DecodeNormal( Tex2DTriplanar( g_tSplatNormal_A, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgb );
		float3 l_tSplatNormal_B = DecodeNormal( Tex2DTriplanar( g_tSplatNormal_B, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgb );
		float3 l_tSplatNormal_C = DecodeNormal( Tex2DTriplanar( g_tSplatNormal_C, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgb );
		float3 l_tSplatNormal_D = DecodeNormal( Tex2DTriplanar( g_tSplatNormal_D, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgb );

		// Prepare roughness map.
		float4 l_tSplatRoughness = Tex2DTriplanar( g_tSplatRoughness, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;

        Material m = Material::Init();

		m.Albedo = ApplyWithSplatdata( l_tSplatColor_A, l_tSplatColor_B, l_tSplatColor_C, l_tSplatColor_D, l_tSplatData );	// Pass 1 - apply terrain textures according to splat maps
		m.Albedo = lerp( m.Albedo, l_tSplatColor_LOD, smoothstep(550, 1000, scaleFactor));									// Pass 2 - render LOD texture with smooth transition
		m.Albedo = lerp( m.Albedo, m.Albedo * 0.5, smoothstep( shoremin, shoremax, i.AbsolutePosition.z ));					// Pass 3 - height-based darkening effect

		m.Roughness = ApplyWithSplatdataBNW( 
			float2(l_tSplatRoughness.r, l_tSplatColor_A.a), 
			float2(l_tSplatRoughness.g, l_tSplatColor_B.a), 
			float2(l_tSplatRoughness.b, l_tSplatColor_C.a),
			float2(l_tSplatRoughness.a, l_tSplatColor_D.a), l_tSplatData);
        m.Roughness = lerp( m.Roughness, m.Roughness * 0.2, smoothstep( 2295, 2290, sin(g_flTime) + i.AbsolutePosition.z ) );
        m.Metalness = lerp( 0, 0.65, smoothstep( 2295, 2285, sin(g_flTime) + i.AbsolutePosition.z ) );
        m.AmbientOcclusion = l_tGlobalNormal.a;

		// Apply normals of each splat texture layer by layer, then blend it with terrain's global normal, then transform normal so they display correctly in-game.
		m.Normal = TransformNormal( globalnormal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// Write to shading model 
		float4 result = ShadingModelStandard::Shade( i, m );

		return result;
	}
}