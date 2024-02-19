namespace Sauna;

public struct IconSettings : IEquatable<IconSettings>
{
	public string Model { get; set; }
	public Vector3 Position { get; set; }
	public Rotation Rotation { get; set; }

	public override int GetHashCode()
	{
		return HashCode.Combine( Position, Rotation, Model );
	}

	public override bool Equals( object obj )
	{
		return obj is IconSettings other
			&& Equals( other );
	}

	public bool Equals( IconSettings other )
	{
		return other.Position == Position
			&& other.Rotation == Rotation
			&& other.Model == Model;
	}
}