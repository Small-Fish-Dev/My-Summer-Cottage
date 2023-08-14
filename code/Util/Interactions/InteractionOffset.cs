namespace Sauna;

public class InteractionOffset
{
	public float x;
	public float y;
	public float z;

	public InteractionOffset( float x, float y, float z )
	{
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public static implicit operator InteractionOffset( Vector3 vec ) => new( vec.x, vec.y, vec.z );
	public static implicit operator Vector3( InteractionOffset offset ) => offset == null 
		? default
		: new( offset.x, offset.y, offset.z );
}
