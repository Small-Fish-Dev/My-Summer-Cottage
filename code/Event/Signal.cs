namespace Sauna;

public struct Signal : IEquatable<Signal>
{
	public string Identifier { get; set; } = "";

	public Signal( string identifier = "" )
	{
		Identifier = identifier;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine( Identifier );
	}

	public override bool Equals( object obj )
	{
		return obj is Signal other
			&& Equals( other );
	}

	public bool Equals( Signal other )
	{
		return other.Identifier == Identifier;
	}
}
