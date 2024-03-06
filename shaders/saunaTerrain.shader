HEADER
{
	Description = "Sauna Terrain Shader";
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
	float2 grassUV : TEXCOORD9 < Semantic( LowPrecisionUv1 ); >;
	float3 AbsolutePosition : TEXCOORD10;
};

struct GeometryInput
{
	#include "common/pixelinput.hlsl"
	float2 grassUV;
	float3 AbsolutePosition;
};



//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		o = FinalizeVertex(o); 

		o.grassUV = o.vTextureCoords.zw;		// Used to add grass billboard onto new triangles. Probably there's a better way to do it.
												// UV2 isn't used at this stage but we initialize all inputs anyway so shader doesn't spam warnings on each compile.
		o.AbsolutePosition = i.vPositionOs.xyz;	// Include vertex data into PixelInput so we can use it for per-triangle grass tinting

		return o;
	}
}

//=========================================================================================================================

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
	SamplerState SamplerPoint < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >; 
	SamplerState SamplerAniso < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >; // Two samplers is probably stupid but hey, "as long as it works"(tm)

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );	// Whatever this means

	#define CUSTOM_MATERIAL_INPUTS

	// ---
	// Color maps input
	// ---
	CreateInputTexture2D( Color, 	Srgb, 8, "", "_color", 	"Material,10/10", Default3( 1.0, 1.0, 1.0 ) );		// Ground color
	CreateInputTexture2D( BillboardColor, Srgb, 8, "", "_color", "Material,10/20", Default3( 1.0, 1.0, 1.0 ) );	// Grass billboard color
	CreateInputTexture2D( Opacity, Linear, 8, "", "_alpha", "Material,10/30", Default( 1.0f ) );				// Grass billboard alpha

	float3 g_flColorVariation < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material,10/40" ); >;		// Color variation

	// Store color map, include tint mask into alpha channel.
	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;	
	CreateTexture2DWithoutSampler( g_tGrass ) < Channel( RGB, Box( BillboardColor ), Srgb ); Channel( A, Box( Opacity ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;

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
	
	// ---
	// Triplanar settings
	// ---
	float TextureTiling 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 2048.0f ); UiGroup("Triplanar Settings,80/10"); >;
	float TextureBlending 	< UiType( VectorText ); Default( 1.0f ); Range ( 0.0f,   10.0f ); UiGroup("Triplanar Settings,80/20"); >;
	float TextureScale		< UiType( Slider ); 	Default( 1.0f ); Range ( 0.0f,   64.0f ); UiGroup("Triplanar Settings,80/30"); >;

	// ---
	// Assign attribute bullshit for VRAD 
	// ---
	TextureAttribute(LightSim_DiffuseAlbedoTexture, g_tColor);
	TextureAttribute(RepresentativeTexture, g_tColor);

	#if S_ALPHA_TEST
		TextureAttribute( LightSim_Opacity_A, g_tGrass );
	#endif

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
	#include "noise3D.hlsl"
	#include "terrain_utils.hlsl"
	#include "common/utils/triplanar.hlsl" 	

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

		float scaleFactor = distance( g_vCameraPositionWs, i.AbsolutePosition.xyz );

		float3 l_tColorMap = Tex2DS( g_tColor, SamplerPoint, UV ).rgb;

		// Prepare splat map data.
		float4 l_tSplatData = Tex2DS( g_tSplatMap, SamplerAniso, UV.xy ).rgba;

		// Fucking mess
		float3 l_tSplatColor_LOD = ApplyWithSplatdata( 
			Tex2DS( g_tSplatColor_A, SamplerPoint, UV.xy * 20 ).rgba,
			Tex2DS( g_tSplatColor_B, SamplerPoint, UV.xy * 20 ).rgba,
			Tex2DS( g_tSplatColor_C, SamplerPoint, UV.xy * 20 ).rgba,
			Tex2DS( g_tSplatColor_D, SamplerPoint, UV.xy * 20 ).rgba, l_tSplatData
		);

		// Prepare color maps (+ blend masks in alpha channel)
		float4 l_tSplatColor_A = Tex2DTriplanar( g_tSplatColor_A, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_B = Tex2DTriplanar( g_tSplatColor_B, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_C = Tex2DTriplanar( g_tSplatColor_C, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;
		float4 l_tSplatColor_D = Tex2DTriplanar( g_tSplatColor_D, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;

		// Prepare normal maps
		float3 l_tSplatNormal_A = DecodeNormal( Tex2DS( g_tSplatNormal_A, SamplerPoint, UV.xy * 64).rgb );
		float3 l_tSplatNormal_B = DecodeNormal( Tex2DS( g_tSplatNormal_B, SamplerPoint, UV.xy * 64).rgb );
		float3 l_tSplatNormal_C = DecodeNormal( Tex2DS( g_tSplatNormal_C, SamplerPoint, UV.xy * 64).rgb );
		float3 l_tSplatNormal_D = DecodeNormal( Tex2DS( g_tSplatNormal_D, SamplerPoint, UV.xy * 64).rgb );

		// Prepare roughness map.
		float4 l_tSplatRoughness = Tex2DTriplanar( g_tSplatRoughness, SamplerPoint, i, TextureTiling / 8, TextureBlending, TextureScale ).rgba;

		float4 l_tGrass = Tex2DS( g_tGrass, SamplerPoint, UV2 ).rgba;
		float  rand = snoise( i.AbsolutePosition.xyz );
			   l_tGrass.rgb = lerp( l_tGrass.rgb, l_tGrass.rgb * g_flColorVariation, saturate( rand ) );

        Material m = Material::Init();

		m.Albedo = ApplyWithSplatdata( l_tSplatColor_A, l_tSplatColor_B, l_tSplatColor_C, l_tSplatColor_D, l_tSplatData );	// Pass 1 - apply terrain textures according to splat maps
		m.Albedo = lerp( m.Albedo, l_tSplatColor_LOD, smoothstep(400, 750, scaleFactor));									// Pass 2 - render LOD texture with smooth transition
		m.Albedo = PaintProceduralGeometry( m.Albedo, l_tGrass.rgb, UV2 ); 													// Pass 3 - apply grass texture+opacity onto generated triangles
		
		m.Opacity = PaintProceduralGeometry( 1, l_tGrass.a, UV2 );	
        m.Roughness = ApplyWithSplatdata( 
			float4(l_tSplatRoughness.r, 0, 0, l_tSplatColor_A.a), 
			float4(l_tSplatRoughness.g, 0, 0, l_tSplatColor_B.a), 
			float4(l_tSplatRoughness.b, 0, 0, l_tSplatColor_C.a),
			float4(l_tSplatRoughness.a, 0, 0, l_tSplatColor_D.a), l_tSplatData);
        m.Metalness = 0;
        m.AmbientOcclusion = 1;

		m.Normal = TransformNormal( ApplyWithSplatdata( 
			float4(l_tSplatNormal_A.rgb, l_tSplatColor_A.a),
			float4(l_tSplatNormal_B.rgb, l_tSplatColor_B.a), 
			float4(l_tSplatNormal_C.rgb, l_tSplatColor_C.a), 
			float4(l_tSplatNormal_D.rgb, l_tSplatColor_D.a), 
			l_tSplatData ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// Write to shading model 
		float4 result = ShadingModelStandard::Shade( i, m );

		#if( S_TRANSPARENCY )
			float alpha = PaintProceduralGeometry( 1, l_tGrass.a, UV2 );
			result.a = max( alpha, floor( alpha + TransparencyRounding ) );
		#endif		

		return result;
	}
}