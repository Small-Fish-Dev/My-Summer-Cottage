namespace Sauna;

public static class Vector3Extensions
{
	/// <summary>
	/// Rounds the Vector3.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static Vector3 Round( this Vector3 value )
		=> new Vector3( MathF.Floor( value.x + 0.5f ), MathF.Floor( value.y + 0.5f ), MathF.Floor( value.z + 0.5f ) );
}
