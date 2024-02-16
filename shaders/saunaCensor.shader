HEADER
{
	Description = "The censoring for Sauna";
}

FEATURES
{
    #include "common/features.hlsl"
    Feature( F_USE_PATTERN, 0..1, "Rendering" );
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

	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );
		return FinalizeVertex( o );
	}
}

//
// Creating geometry outside of the model, for more width.
//
GS
{
	float _Width < UiType( Color ); UiGroup( "Color" ); Attribute( "Width" ); Default( 0.2f ); >;

	#include "common/vertex.hlsl"

	void PositionOffset( inout PixelInput input, float2 vOffsetDir, float flOutlineSize )
	{
		float2 vAspectRatio = normalize(g_vInvViewportSize);
		input.vPositionPs.xy += (vOffsetDir * 2.0) * vAspectRatio * input.vPositionPs.w * flOutlineSize;
	}
	
	//
	// Use this one if you want absolute pixel size
	//
	void PositionOffsetResolutionDependent( inout PixelInput input, float2 vOffsetDir, float flOutlineSize )
	{
		input.vPositionPs.xy += (vOffsetDir * 2.0) * g_vInvViewportSize * input.vPositionPs.w * flOutlineSize;
	}
	
    //
    // Main
    //
    [maxvertexcount(3*7)]
    void MainGs( triangle in PixelInput vertices[3], inout TriangleStream<PixelInput> triStream )
    {
		const float flWidthSize = _Width / 64.0f;
        const float fTwoPi = 6.28318f;
		const int nNumIterations = clamp( _Width * 10, 3, 6 ); // Thin lines don't need many iterations 

        PixelInput v[3];

        [unroll]
        for( float i = 0; i <= nNumIterations; i += 1 )
		{
			float fCycle = i / nNumIterations;

			float2 vOffset = float2( 
				( sin( fCycle * fTwoPi ) ),
				( cos( fCycle * fTwoPi ) )
			);

			for ( int i = 0; i < 3; i++ )
			{
				v[i] = vertices[i];
				PositionOffset( v[i], vOffset, flWidthSize );
			}

			triStream.Append(v[2]);
			triStream.Append(v[0]);
			triStream.Append(v[1]);
		}
		
		// emit the vertices
		triStream.RestartStrip();
    }
}

PS
{
    RenderState( DepthWriteEnable, true );
    RenderState( DepthEnable, true );

    RenderState( StencilEnable, true );
	RenderState( StencilPassOp, REPLACE );
	RenderState( StencilFunc, ALWAYS );
	RenderState( BackStencilFunc, ALWAYS );
	RenderState( StencilRef, 0x01 );
    
	CreateTexture2D( _DepthTexture ) < Attribute( "DepthTexture" ); SrgbRead( true ); Filter( POINT ); >;
	CreateTexture2D( _ColorTexture ) < Attribute( "ColorTexture" ); SrgbRead( true ); Filter( POINT ); >;

	//
	// Main
	//
	float4 MainPs( PixelInput i ): SV_Target
	{
		float objectDepth = i.vPositionSs.z;
		float2 uv = CalculateViewportUv( i.vPositionSs.xy ); 

        float worldDepth = Tex2D( _DepthTexture, uv ).r;
		worldDepth = RemapValClamped( worldDepth, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0 ); // Remap to 0-1 since we are using the full depth range on our depth viewport

		float diff = (objectDepth - worldDepth);
		diff = RemapValClamped( diff, 0.001, 0.002f, 0.0, 1.0 );
		
        float4 input = Tex2D( _ColorTexture, uv.xy );

        float scaling = 128;
        float4 pixelated = Tex2D( _ColorTexture, floor( uv.xy * scaling ) / scaling );
    
		float4 output = lerp( pixelated, input, diff );
		output.rgb = lerp( input.rgb, pixelated.rgb, 1 - diff );

		return output;
	}
}