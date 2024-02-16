HEADER
{
	Description = "Sauna Blendable";
}

//=========================================================================================================================

MODES
{
	VrForward();
    Default();
    ToolsVis( S_MODE_TOOLS_VIS );
}

//=========================================================================================================================

FEATURES
{
    #include "common/features.hlsl"

    Feature( F_MULTIBLEND, 0..3 ( 0="1 Layers", 1="2 Layers", 2="3 Layers", 3="4 Layers", 4="5 Layers" ), "Number Of Blendable Layers" );
	Feature( F_USE_TINT_MASKS_IN_VERTEX_PAINT, 0..1, "Use Tint Masks In Vertex Paint" );
	
	Feature( F_HIGH_QUALITY_REFLECTIONS, 0..1, "Rendering" );
}

//=========================================================================================================================

COMMON
{
	#define USES_HIGH_QUALITY_REFLECTIONS
	#include "common/shared.hlsl"
}


//=========================================================================================================================

struct VertexInput
{	
	float4 vColorBlendValues : TEXCOORD4 < Semantic( VertexPaintBlendParams ); >;
	float4 vColorPaintValues : TEXCOORD5 < Semantic( VertexPaintTintColor ); >;
	#include "common/vertexinput.hlsl"
};

//=========================================================================================================================

struct PixelInput
{
	float4 vBlendValues		 : TEXCOORD14;
	float4 vPaintValues		 : TEXCOORD15;
	#include "common/pixelinput.hlsl"
};

//=========================================================================================================================

VS
{
	StaticCombo( S_MULTIBLEND, F_MULTIBLEND, Sys( PC ) );
	
	#include "common/vertex.hlsl"

	BoolAttribute( VertexPaintUI2Layer, F_MULTIBLEND == 1 );
	BoolAttribute( VertexPaintUI3Layer, F_MULTIBLEND == 2 );
	BoolAttribute( VertexPaintUI4Layer, F_MULTIBLEND == 3 );
	BoolAttribute( VertexPaintUI5Layer, F_MULTIBLEND == 4 );
	BoolAttribute( VertexPaintUIPickColor, true );

	BoolAttribute( UsesHighQualityReflections, ( F_HIGH_QUALITY_REFLECTIONS > 0 ) );

	//
	// Main
	//
	PS_INPUT MainVs( VertexInput i )
	{
		PS_INPUT o = ProcessVertex( i );

        float3 vPositionWs = o.vPositionWs.xyz;
        float4 vertex = Position3WsToPs( vPositionWs.xyz );
		vertex.xyz = vertex.xyz / vertex.w;
		vertex.xy = floor( 240 * vertex.xy ) / 240;
		vertex.xyz *= vertex.w;

		o.vPositionPs = vertex;

		o.vBlendValues = i.vColorBlendValues;
        o.vPaintValues = i.vColorPaintValues;

		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{
	//
	// Combos
	//
	StaticCombo( S_MULTIBLEND, F_MULTIBLEND, Sys( PC ) );
    StaticCombo( S_USE_TINT_MASKS_IN_VERTEX_PAINT, F_USE_TINT_MASKS_IN_VERTEX_PAINT, Sys( PC ) );

	#ifndef COMMON_PIXEL_BLEND_H
    #define COMMON_PIXEL_BLEND_H

    #include "common/pixel.hlsl"
    #include "texture_blending.fxc"

    Material MaterialParametersMultiblend( Material a, Material b, float fBlendValue, float fBlendMaskB, float fSoftness = 0.5 )
    {
        Material o;
        float fBlendfactor = ComputeBlendWeight( fBlendValue, fSoftness, fBlendMaskB );
        o = Material::lerp( a, b, fBlendfactor ); 
        return o;
    }

    #if S_MULTIBLEND >= 0
        //
        // Material A
        //
        CreateInputTexture2D( TextureColorA,            Srgb,   8, "",                 "_color",  "Material A,10/10", Default3( 1.0, 1.0, 1.0 ) );
        CreateInputTexture2D( TextureNormalA,           Linear, 8, "NormalizeNormals", "_normal", "Material A,10/20", Default3( 0.5, 0.5, 1.0 ) );
        CreateInputTexture2D( TextureBlendMaskA,        Linear, 8, "",                 "_blend",  "Material E,10/60", Default( 1.0 ) );
        float3 g_flTintColorA < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material A,10/80" ); >;
        float g_flBlendSoftnessA < Default( 0.5 ); Range( 0.1, 1.0 ); UiGroup( "Material A,10/90" ); >;

        CreateTexture2DWithoutSampler( g_tColorA )  < Channel( RGB,  Box( TextureColorA ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
        CreateTexture2DWithoutSampler( g_tNormalA ) < Channel( RGB, Box( TextureNormalA ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
        CreateTexture2DWithoutSampler( g_tBlendMaskA )  < Channel( A, Box( TextureBlendMaskA ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    #if S_MULTIBLEND >= 1
        //
        // Material B
        //
        CreateInputTexture2D( TextureColorB,            Srgb,   8, "",                 "_color",  "Material B,10/10", Default3( 1.0, 1.0, 1.0 ) );
        CreateInputTexture2D( TextureNormalB,           Linear, 8, "NormalizeNormals", "_normal", "Material B,10/20", Default3( 0.5, 0.5, 1.0 ) );
        CreateInputTexture2D( TextureBlendMaskB,        Linear, 8, "",                 "_blend",  "Material E,10/60", Default( 1.0 ) );
        float3 g_flTintColorB < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material A,10/80" ); >;
        float g_flBlendSoftnessB < Default( 0.5 ); Range( 0.1, 1.0 ); UiGroup( "Material B,10/90" ); >;
        float2 g_vTexCoordScale2 < Default2( 1.0, 1.0 ); Range2( 0.0, 0.0, 100.0, 100.0 ); UiGroup( "Material B,10/100" ); >;

        CreateTexture2DWithoutSampler( g_tColorB )  < Channel( RGB,  Box( TextureColorB ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
        CreateTexture2DWithoutSampler( g_tNormalB ) < Channel( RGB, Box( TextureNormalB ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
        CreateTexture2DWithoutSampler( g_tBlendMaskB )  < Channel( A, Box( TextureBlendMaskB ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    #if S_MULTIBLEND >= 2
        //
        // Material C
        //
        CreateInputTexture2D( TextureColorC,            Srgb,   8, "",                 "_color",  "Material C,10/10", Default3( 1.0, 1.0, 1.0 ) );
        CreateInputTexture2D( TextureNormalC,           Linear, 8, "NormalizeNormals", "_normal", "Material C,10/20", Default3( 0.5, 0.5, 1.0 ) );
        CreateInputTexture2D( TextureBlendMaskC,        Linear, 8, "",                 "_blend",  "Material E,10/60", Default( 1.0 ) );
        float3 g_flTintColorC < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material A,10/80" ); >;
        float g_flBlendSoftnessC < Default( 0.5 ); Range( 0.1, 1.0 ); UiGroup( "Material C,10/90" ); >;
        float2 g_vTexCoordScale3 < Default2( 1.0, 1.0 ); Range2( 0.0, 0.0, 100.0, 100.0 ); UiGroup( "Material C,10/100" ); >;

        CreateTexture2DWithoutSampler( g_tColorC )  < Channel( RGB,  Box( TextureColorC ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
        CreateTexture2DWithoutSampler( g_tNormalC ) < Channel( RGB, Box( TextureNormalC ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
        CreateTexture2DWithoutSampler( g_tBlendMaskC )  < Channel( A, Box( TextureBlendMaskC ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    #if S_MULTIBLEND >= 3
        //
        // Material D
        //
        CreateInputTexture2D( TextureColorD,            Srgb,   8, "",                 "_color",  "Material D,10/10", Default3( 1.0, 1.0, 1.0 ) );
        CreateInputTexture2D( TextureNormalD,           Linear, 8, "NormalizeNormals", "_normal", "Material D,10/20", Default3( 0.5, 0.5, 1.0 ) );
        CreateInputTexture2D( TextureBlendMaskD,        Linear, 8, "",                 "_blend",  "Material E,10/60", Default( 1.0 ) );
        float3 g_flTintColorD < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material A,10/80" ); >;
        float g_flBlendSoftnessD < Default( 0.5 ); Range( 0.1, 1.0 ); UiGroup( "Material D,10/90" ); >;
        float2 g_vTexCoordScale4 < Default2( 1.0, 1.0 ); Range2( 0.0, 0.0, 100.0, 100.0 ); UiGroup( "Material D,10/100" ); >;

        CreateTexture2DWithoutSampler( g_tColorD )  < Channel( RGB,  Box( TextureColorD ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
        CreateTexture2DWithoutSampler( g_tNormalD ) < Channel( RGB, Box( TextureNormalD ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
        CreateTexture2DWithoutSampler( g_tBlendMaskD )  < Channel( A, Box( TextureBlendMaskD ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    #if S_MULTIBLEND >= 4
        //
        // Material E
        //
        CreateInputTexture2D( TextureColorE,            Srgb,   8, "",                 "_color",  "Material E,10/10", Default3( 1.0, 1.0, 1.0 ) );
        CreateInputTexture2D( TextureNormalE,           Linear, 8, "NormalizeNormals", "_normal", "Material E,10/20", Default3( 0.5, 0.5, 1.0 ) );
        CreateInputTexture2D( TextureBlendMaskE,        Linear, 8, "",                 "_blend",  "Material E,10/60", Default( 1.0 ) );
        float3 g_flTintColorE < UiType( Color ); Default3( 1.0, 1.0, 1.0 ); UiGroup( "Material A,10/80" ); >;
        float g_flBlendSoftnessE < Default( 0.5 ); Range( 0.1, 1.0 ); UiGroup( "Material E,10/90" ); >;
        float2 g_vTexCoordScale5 < Default2( 1.0, 1.0 ); Range2( 0.0, 0.0, 100.0, 100.0 ); UiGroup( "Material E,10/100" ); >;

        CreateTexture2DWithoutSampler( g_tColorE )  < Channel( RGB,  Box( TextureColorE ), Srgb ); OutputFormat( BC7 ); SrgbRead( true ); >;
        CreateTexture2DWithoutSampler( g_tNormalE ) < Channel( RGB, Box( TextureNormalE ), Linear ); OutputFormat( DXT5 ); SrgbRead( false ); >;
        CreateTexture2DWithoutSampler( g_tBlendMaskE )  < Channel( A, Box( TextureBlendMaskE ), Linear ); OutputFormat( BC7 ); SrgbRead( false ); >;
    #endif // 4
    #endif // 3
    #endif // 2
    #endif // 1
    #endif // 0


    //-----------------------------------------------------------------------------
    //
    // ToMaterial but for multiple material channels
    //
    //-----------------------------------------------------------------------------
    Material ToMaterialMultiblend( float2 vUV, PixelInput i )
    {
        #if S_MULTIBLEND >= 0
            Material material = Material::From( i, 
                    Tex2DS( g_tColorA, TextureFiltering, vUV ), 
                    Tex2DS( g_tNormalA, TextureFiltering, vUV ), 
                    float4( 1, 1, 1, 1 ),
                    g_flTintColorA
                );
        #if S_MULTIBLEND >= 1
            Material materialB = Material::From( i,
                Tex2DS( g_tColorB, TextureFiltering, vUV * g_vTexCoordScale2.xy ), 
                Tex2DS( g_tNormalB, TextureFiltering, vUV * g_vTexCoordScale2.xy ), 
                float4( 1, 1, 1, 1 ),
                g_flTintColorB
            );
            const float fBlendMaskB = Tex2DS( g_tBlendMaskB, TextureFiltering, vUV ).a;
            material = MaterialParametersMultiblend( material, materialB, i.vBlendValues.r, fBlendMaskB, g_flBlendSoftnessB );
        #if S_MULTIBLEND >= 2
            Material materialC = Material::From( i,
                Tex2DS( g_tColorC, TextureFiltering, vUV * g_vTexCoordScale3.xy ), 
                Tex2DS( g_tNormalC, TextureFiltering, vUV * g_vTexCoordScale3.xy ), 
                float4( 1, 1, 1, 1 ),
                g_flTintColorC
            );
            const float fBlendMaskC = Tex2DS( g_tBlendMaskC, TextureFiltering, vUV ).a;
            material = MaterialParametersMultiblend( material, materialC, i.vBlendValues.g, fBlendMaskC, g_flBlendSoftnessC );
        #if S_MULTIBLEND >= 3
            Material materialD = Material::From( i,
                Tex2DS( g_tColorD, TextureFiltering, vUV * g_vTexCoordScale4.xy ), 
                Tex2DS( g_tNormalD, TextureFiltering, vUV * g_vTexCoordScale4.xy ), 
                float4( 1, 1, 1, 1 ),
                g_flTintColorD
            );
            const float fBlendMaskD = Tex2DS( g_tBlendMaskD, TextureFiltering, vUV ).a;
            material = MaterialParametersMultiblend( material, materialD, i.vBlendValues.b, fBlendMaskD, g_flBlendSoftnessD );
        #if S_MULTIBLEND >= 4
            Material materialE = Material::From( i,
                Tex2DS( g_tColorE, TextureFiltering, vUV * g_vTexCoordScale5.xy ), 
                Tex2DS( g_tNormalE, TextureFiltering, vUV * g_vTexCoordScale5.xy ), 
                float4( 1, 1, 1, 1 ),
                g_flTintColorE
            );
            const float fBlendMaskB = Tex2DS( g_tBlendMaskE, TextureFiltering, vUV ).a;
            material = MaterialParametersMultiblend( material, materialE, i.vBlendValues.a, fBlendMaskE, g_flBlendSoftnessE );
        #endif // 4
        #endif // 3
        #endif // 2
        #endif // 1
        #endif // 0

        return material;
    }

    #endif

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		//
		// Set up materials
		//
		float2 vUV = i.vTextureCoords.xy;

		Material material = ToMaterialMultiblend( vUV, i );
        
		//
		// Vertex Painting
		//
		#if( S_USE_TINT_MASKS_IN_VERTEX_PAINT )
		{
			material.Albedo = lerp( material.Albedo.xyz, material.Albedo.xyz * i.vPaintValues.xyz, material.TintMask.x );
		}
		#else
		{
			material.Albedo = material.Albedo.xyz * i.vPaintValues.xyz;
		}
		#endif

        //
		// Write to final combiner
		//
		return ShadingModelStandard::Shade( i, material );
	}
}