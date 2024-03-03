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
	Feature( F_TRANSPARENCY, 0..1, "Rendering" );
	FeatureRule( Requires1(F_ALPHA_TEST, F_TRANSPARENCY), "You might want to enable Transparency for this material first.");
	Feature( F_EMISSIVE, 0..1, "Rendering" );
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

        float3 vPositionWs = o.vPositionWs.xyz;
		float dist = distance(g_vCameraPositionWs, vPositionWs);

		float scale = RemapValClamped( dist, 1000, 5000, 240, 800 );
        float4 vertex = Position3WsToPs( vPositionWs.xyz );
		vertex.xyz = vertex.xyz / vertex.w;
		vertex.xy = floor( scale * vertex.xy ) / scale;
		vertex.xyz *= vertex.w;

		o.vPositionPs = vertex;

		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{ 
	StaticCombo( S_TRANSPARENCY, F_TRANSPARENCY, Sys( ALL ) );
    StaticCombo( S_ALPHA_TEST, F_ALPHA_TEST, Sys( ALL ) );
	
	#define CUSTOM_TEXTURE_FILTERING
    SamplerState Sampler < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;

	StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );
	StaticCombo( S_EMISSIVE, F_EMISSIVE, Sys( ALL ) );

	#define CUSTOM_MATERIAL_INPUTS
	CreateInputTexture2D( Color, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );

	CreateInputTexture2D( ColorTintMask, Linear, 8, "", "_tint", "Material,10/20", Default3( 1.0, 1.0, 1.0 ) );	// Tint mask, stored in color map's alpha channel
	float3 g_flColorTint < Attribute( "g_flColorTint" ); UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material,10/20" ); >;			// Tint color

	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); Channel( A, Box( ColorTintMask ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); Filter( POINT ); >;
	TextureAttribute(LightSim_DiffuseAlbedoTexture, g_tColor);

	#if S_ALPHA_TEST
		TextureAttribute( LightSim_Opacity_A, g_tColor );
	#endif

    CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", "Material,10/30", Default3( 0.5, 0.5, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tNormal ) < Channel( RGB, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;

	CreateInputTexture2D( Roughness, Linear, 8, "", "_rough", "Material,10/40", Default( 1 ) );
	CreateInputTexture2D( Metalness, Linear, 8, "", "_metal",  "Material,10/50", Default( 1.0 ) );
	CreateTexture2DWithoutSampler( g_tRm ) < Channel( R, Box( Roughness ), Linear ); Channel( G, Box( Metalness ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;

	#if ( S_EMISSIVE )
		float EmissionStrength < UiType( Slider ); Default( 1.0f ); Range( 0, 5.0 ); UiGroup( "Emission,20/10" );  >;

		CreateInputTexture2D( Emission, Linear, 8, "", "", "Emission,20/20", Default3( 0, 0, 0 ) );
		CreateTexture2DWithoutSampler( g_tEmission ) < Channel( RGB, Box( Emission ), Linear ); OutputFormat( BC7 ); SrgbRead( true ); >;
	#endif 

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"
    
	#if ( S_TRANSPARENCY )
		#if( !F_RENDER_BACKFACES )
			#define BLEND_MODE_ALREADY_SET
			RenderState( BlendEnable, true );
			RenderState( SrcBlend, SRC_ALPHA );
			RenderState( DstBlend, INV_SRC_ALPHA);
		#endif

		BoolAttribute( translucent, true );

		CreateInputTexture2D( TransparencyMask, Linear, 8, "", "_trans", "Transparency,10/10", Default( 1 ) );
		CreateTexture2DWithoutSampler( g_tTransparencyMask ) < Channel( R, Box( TransparencyMask ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
	
		float TransparencyRounding< Default( 0.0f ); Range( 0.0f, 1.0f ); UiGroup( "Transparency,10/20" ); >;
	#endif

	RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	#if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		float2 UV = i.vTextureCoords.xy;
		float4 l_tColor = Tex2DS( g_tColor, Sampler, UV.xy ).rgba;

        Material m = Material::Init();
        m.Albedo = lerp(l_tColor.rgb, l_tColor.rgb * g_flColorTint, l_tColor.a );
        m.Normal = TransformNormal( DecodeNormal( Tex2DS( g_tNormal, Sampler, UV.xy ).rgb ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		

		
		float2 rm = Tex2DS( g_tRm, Sampler, UV.xy ).rg;
        m.Roughness = rm.r;
        m.Metalness = rm.g;
        m.AmbientOcclusion = 1;
        m.TintMask = 0;
        m.Opacity = 1;
		m.Emission = 0;
		#if( S_EMISSIVE )
       	 	m.Emission = Tex2DS( g_tEmission, Sampler, UV.xy ).rgb * EmissionStrength;
		#endif
        m.Transmission = 0;

		float4 result = ShadingModelStandard::Shade( i, m );
		#if( S_TRANSPARENCY )
			float alpha = Tex2DS( g_tTransparencyMask, Sampler, UV.xy ).r;
			result.a = max( alpha, floor( alpha + TransparencyRounding ) );
		#endif

		return result;
	}
}