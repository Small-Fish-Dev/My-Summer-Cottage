namespace Sauna;

public struct IconSettings : IEquatable<IconSettings>
{
	public string Model { get; set; }
	public Vector3 Position { get; set; }
	public Rotation Rotation { get; set; }
	public Guid Guid { get; set; }

	[Hide, JsonIgnore] 
	public string Path => $"ui/icons/{Guid}.png";

	public override int GetHashCode()
	{
		return HashCode.Combine( Guid );
	}

	public override bool Equals( object obj )
	{
		return obj is IconSettings other
			&& Equals( other );
	}

	public bool Equals( IconSettings other )
	{
		return other.Guid == Guid;
	}
}
