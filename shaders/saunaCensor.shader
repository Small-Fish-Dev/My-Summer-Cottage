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

GS
{
#if D_OUTLINE_PASS == OUTLINE_OUTSIDE

	float _LineSize < UiType(Color); UiGroup("Color"); Attribute("Width"); Default(0.2f); > ;

	#include "common/vertex.hlsl"

	void PositionOffset(inout PixelInput input, float2 vOffsetDir, float flOutlineSize)
	{
		float2 vAspectRatio = normalize(g_vInvViewportSize);
		input.vPositionPs.xy += (vOffsetDir * 2.0) * vAspectRatio * input.vPositionPs.w * flOutlineSize;

		// even though we do -f-vk-invert this is the only shader that does not behave with it
		#if defined( VULKAN )
			input.vPositionPs.y = -input.vPositionPs.y;
		#endif
	}

	//
	// Main
	//
	[maxvertexcount(3 * 7)]
	void MainGs(triangle in PixelInput vertices[3], inout TriangleStream<PixelInput> triStream)
	{
		const float flOutlineSize = _LineSize / 64.0f;
		const float fTwoPi = 6.28318f;
		const int nNumIterations = clamp(_LineSize * 10, 3, 6); // Thin lines don't need many iterations 

		PixelInput v[3];

		[unroll]
		for (float i = 0; i <= nNumIterations; i += 1)
		{
			float fCycle = i / nNumIterations;

			float2 vOffset = float2(
				(sin(fCycle * fTwoPi)),
				(cos(fCycle * fTwoPi))
			);

			for (int i = 0; i < 3; i++)
			{
				v[i] = vertices[i];
				PositionOffset(v[i], vOffset, flOutlineSize);
			}

			triStream.Append(v[2]);
			triStream.Append(v[0]);
			triStream.Append(v[1]);
		}

		// emit the vertices
		triStream.RestartStrip();
	}
#endif
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
    
	CreateTexture2D( _ColorTexture ) < Attribute( "ColorTexture" ); SrgbRead( true ); Filter( POINT ); >;

	//
	// Main
	//
	float4 MainPs( PixelInput i ): SV_Target
	{
		float objectDepth = i.vPositionSs.z;

		float2 screenUv = CalculateViewportUv(i.vPositionSs.xy);

		float worldDepth = Depth::Get(i.vPositionSs.xy - g_vViewportOffset);
		worldDepth = RemapValClamped(worldDepth, g_flViewportMinZ, g_flViewportMaxZ, 0.0, 1.0); // Remap to 0-1 since we are using the full depth range on our depth viewport

		float diff = (objectDepth - worldDepth);

		float2 amount = 128;
		float2 coords = round(screenUv.xy * amount) / amount;
		float4 vColor = Tex2D(_ColorTexture, coords.xy);

		vColor.rgb = lerp(vColor.rgb, Tex2D(_ColorTexture, screenUv).rgb, diff > 0.0001);

		return vColor;
	}
}