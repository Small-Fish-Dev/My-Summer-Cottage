HEADER
{
	Description = "Sauna Water Shader";
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
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
        vPositionWs.z += 4 * sin( vPositionWs.x * 10 + g_flTime );
        
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
    StaticCombo( S_MODE_DEPTH, 0..1, Sys( ALL ) );

    #define CUSTOM_TEXTURE_FILTERING
    SamplerState Sampler < Filter( POINT ); AddressU( WRAP ); AddressV( WRAP ); >;

    #define BLEND_MODE_ALREADY_SET
    RenderState( BlendEnable, true );
    RenderState( SrcBlend, SRC_ALPHA );
    RenderState( DstBlend, INV_SRC_ALPHA);

    #define CUSTOM_MATERIAL_INPUTS
	CreateInputTexture2D( Color, Srgb, 8, "", "_color", "Material,10/10", Default3( 1.0, 1.0, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tColor ) < Channel( RGB, Box( Color ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); Filter( POINT ); >;

    CreateInputTexture2D( Normal, Linear, 8, "NormalizeNormals", "_normal", "Material,10/20", Default3( 0.5, 0.5, 1.0 ) );
	CreateTexture2DWithoutSampler( g_tNormal ) < Channel( RGB, Box( Normal ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;

    #include "sbox_pixel.fxc"
    #include "common/pixel.hlsl"

    BoolAttribute( translucent, true );

    #if ( S_MODE_DEPTH )
        #define MainPs Disabled
    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
        float time = g_flTime * 0.03;
        float sine = 0.2 * sin( i.vTextureCoords.x + time );
		float2 UV = i.vTextureCoords.xy - float2( time / 2 + sine, sine / 2 );

        Material m = Material::Init();
        m.Albedo = Tex2DS( g_tColor, Sampler, UV.xy ).rgb;
        m.Normal = TransformNormal( DecodeNormal( Tex2DS( g_tNormal, Sampler, UV.xy ).rgb ), i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
        m.Roughness = 0.7;
        m.Metalness = 0;
        m.AmbientOcclusion = 1;
        m.TintMask = 1;
        m.Opacity = 1;
        m.Emission = 0;
        m.Transmission = 1;
	
        float4 result = ShadingModelStandard::Shade( i, m );
        result.a = 0.65;

		return result;
	}
}