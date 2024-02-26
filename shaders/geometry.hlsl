#define PI 			3.141592
#define PI_DOUBLE 	6.283185

#include "noise3D.hlsl"

// Options
float GrassSize 	< UiType( VectorText ); Default( 2.0f ); Range ( 1.0f, 20.0f ); UiGroup("Grass Settings,10/30"); >;

// Rotation matrix bullshit: taken from https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33 
// Usage: float3x3 rotationMatrix = AngleAxis3x3( 0.4f, float3( 0, 0, 1 ) ) - create rotation matrix with angle 0.4 on Z axis. 
float3x3 AngleAxis3x3( float angle, float3 axis )
{
	float c, s;
	sincos( angle, s, c );

	float t = 1 - c;
	float x = axis.x;
	float y = axis.y;
	float z = axis.z;

	return float3x3(
		t * x * x + c,      t * x * y - s * z,  t * x * z + s * y,
		t * x * y + s * z,  t * y * y + c,      t * y * z - s * x,
		t * x * z - s * y,  t * y * z + s * x,  t * z * z + c
	);
}

// Random function, returns the value in 0..1 range.
float ClampedRandom( float3 input )
{	
	return ( snoise(input) ); 
}

// This matrix is used to convert grass triangles to tangent space
float3x3 convertTangetToLocal( float3 tangent, float3 bitangent, float3 normal )
{
	return float3x3
	(
		tangent.x, bitangent.x, normal.x,
		tangent.y, bitangent.y, normal.y,
		tangent.z, bitangent.z, normal.z		
	);
}

// Emit vertex in world with *actual* world space. Used for placing down original mesh geometry. 
void EmitVertex( inout TriangleStream<PS_INPUT> triStream, triangle PS_INPUT i )
{
	float3 vWorldPosition = i.vPositionWs + g_vHighPrecisionLightingOffsetWs.xyz; // Evil motherfucking dark magic trickery to set the vertex in REAL world space 

	i.vPositionWs = vWorldPosition - g_vHighPrecisionLightingOffsetWs.xyz; 		  // ???????
	i.vPositionPs = Position3WsToPs( vWorldPosition );							  // meow :3
	GSAppendVertex( triStream, i );
}

// Emits one of the grass vertices. Called 3 times in a row, each call describes 1 vertex position, UV and size. 
void EmitGrass( inout TriangleStream<PS_INPUT> triStream, triangle PS_INPUT i, float3 shape, float2 uv, float size )
{
	float3 position  = g_vCameraPositionWs + i.vPositionWs.xyz; // In today's episode of "how the fuck are you supposed to know that". Prevents grass jitter.
	float3 normal 	 = normalize( i.vNormalWs.xyz );
	float3 tangent 	 = normalize( i.vTangentUWs.xyz );
	float3 bitangent = cross( normal, tangent.xyz );

	float3x3 tangentToLocal = convertTangetToLocal( tangent.xyz, bitangent.xyz, normal.xyz );

	float3x3 rotationMatrix = AngleAxis3x3( ClampedRandom( position * 2 ) * PI_DOUBLE, float3( 0, 0, 1) );	// Rotates vertex on given XYZ axis. (in this case Z)
	float3x3 transformationMatrix = mul( tangentToLocal, rotationMatrix );									// Set given vertex to tangent space, then apply rotation.

	i.vPositionWs += mul( transformationMatrix, shape * size );						// Apply "tangent + rotation" matrix to current world space position
	float3 vWorldPosition = i.vPositionWs + g_vHighPrecisionLightingOffsetWs.xyz;	// Same weird tricks to get real world space position

	i.vPositionWs = vWorldPosition - g_vHighPrecisionLightingOffsetWs.xyz;			 
	i.vPositionPs = Position3WsToPs( vWorldPosition );								 

	i.grassUV.xy = uv;

	GSAppendVertex( triStream, i );
}

// Main geometry shader function 
[maxvertexcount(6)]
void MainGs( triangle in PixelInput input[3], inout TriangleStream<PixelInput> triStream )
{
	PS_INPUT o = (PS_INPUT)0;

	EmitVertex( triStream, input[0] );
	EmitVertex( triStream, input[1] );
	EmitVertex( triStream, input[2] );

	GSRestartStrip( triStream );

	// Cycle through each mesh vertex position and create a triangle with set width and UV.
	for (float k = 0; k < 3; k++)
	{
		PS_INPUT o = input[k];

		EmitGrass( triStream, o, float3( GrassSize, 0, 0 ), 	 float2(   1, 1 ), 7 );
		EmitGrass( triStream, o, float3(-GrassSize, 0, 0 ), 	 float2(   0, 1 ), 7 );
		EmitGrass( triStream, o, float3( 0, 0, 1 + GrassSize ),  float2( 0.5, 0 ), 7 );	// UV X axis here can be used for sway animation
	}

	GSRestartStrip( triStream );
}