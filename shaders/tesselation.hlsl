
#if ( PROGRAM == VFX_PROGRAM_HS )
	#define sMaxTesselation 100
	#ifdef DISTANCE_BASED_TESS
		#define fTesselationFalloff 1500
	#endif

	PatchSize( 3 );
	HullPatchConstants TessellationFunc(InputPatch<HullInput, 3> patch)
	{
		HullPatchConstants o;
	
		#ifdef DISTANCE_BASED_TESS
			float fTessMax = 1.0f;
			float4 vTess = DistanceBasedTess( patch[0].vPositionWs, patch[1].vPositionWs, patch[2].vPositionWs, 1.0, fTesselationFalloff, sMaxTesselation);
			
			o.Edge[0] = vTess.x;
			o.Edge[1] = vTess.y;
			o.Edge[2] = vTess.z;
			
			o.Inside = vTess.w;
		#endif
		return o;
	}

	TessellationDomain( "tri" )
	TessellationOutputControlPoints( 3 )
	TessellationOutputTopology( "triangle_cw" )
	TessellationPartitioning( "fractional_odd" )
	TessellationPatchConstantFunc( "TessellationFunc" )
	HullOutput MainHs( InputPatch<HullInput, 3> patch, uint id : SV_OutputControlPointID )
	{
		HullInput i = patch[id];
		HullOutput o;
		
		o.vPositionPs = i.vPositionPs;
		o.vPositionWs = i.vPositionWs;
		o.vNormalWs = i.vNormalWs;
		o.vTextureCoords = i.vTextureCoords;
		o.vVertexColor = i.vVertexColor;
		
		#if ( S_DETAIL_TEXTURE )
			o.vDetailTextureCoords = i.vDetailTextureCoords;
		#endif

		#if ( D_BAKED_LIGHTING_FROM_LIGHTMAP )
			o.vLightmapUV = i.vLightmapUV;
		#endif

		#if ( PS_INPUT_HAS_PER_VERTEX_LIGHTING )
			o.vPerVertexLighting = i.vPerVertexLighting;
		#endif

		#if ( S_SPECULAR )
			o.vCentroidNormalWs = i.vCentroidNormalWs;
		#endif

		#ifdef PS_INPUT_HAS_TANGENT_BASIS
			o.vTangentUWs = i.vTangentUWs;
			o.vTangentVWs = i.vTangentVWs;
		#endif

		#if ( S_USE_PER_VERTEX_CURVATURE )
			o.flSSSCurvature = i.flSSSCurvature;
		#endif

		return o;
	}
#endif //( PROGRAM == VFX_PROGRAM_HS )



#if ( PROGRAM == VFX_PROGRAM_DS )
	#include "common/domain.hlsl"
	TessellationDomain( "tri" )
	PixelInput MainDs(HullPatchConstants i, float3 barycentricCoordinates : SV_DomainLocation, const OutputPatch<DomainInput, 3> patch)
	{
		PixelInput o;
		
		//Barycentric3Interpolate( vPositionPs, barycentricCoordinates );
		Barycentric3Interpolate( vPositionWs, barycentricCoordinates );
		Barycentric3Interpolate( vNormalWs, barycentricCoordinates );
		Barycentric3Interpolate( vTextureCoords, barycentricCoordinates );
		Barycentric3Interpolate( vVertexColor, barycentricCoordinates );

		o.vPositionPs = Position3WsToPs( o.vPositionWs );

		//---------------------------------------
		
		#if ( S_DETAIL_TEXTURE )
			Barycentric3Interpolate( vDetailTextureCoords, barycentricCoordinates  );
		#endif

		#if ( D_BAKED_LIGHTING_FROM_LIGHTMAP )
			Barycentric3Interpolate( vLightmapUV, barycentricCoordinates  );
		#endif

		#if ( PS_INPUT_HAS_PER_VERTEX_LIGHTING )
			Barycentric3Interpolate( vPerVertexLighting, barycentricCoordinates  );
		#endif

		#if ( S_SPECULAR )
			Barycentric3Interpolate( vCentroidNormalWs, barycentricCoordinates  );
		#endif

		#ifdef PS_INPUT_HAS_TANGENT_BASIS
			Barycentric3Interpolate( vTangentUWs, barycentricCoordinates  );
			Barycentric3Interpolate( vTangentVWs, barycentricCoordinates  );
		#endif

		#if ( S_USE_PER_VERTEX_CURVATURE )
			Barycentric3Interpolate( flSSSCurvature, barycentricCoordinates  );
		#endif

		return o;
	}
#endif //( PROGRAM == VFX_PROGRAM_DS )