namespace Sauna;

public struct Signal : IEquatable<Signal>
{
	public enum SignalType
	{
		Generic,
		Received,
		Equipped,
		Unequipped,
		Dropped,
		Bought,
		Triggered,
		Completed,
		Placeholder1,
		Placeholder2,
		Placeholder3
	}

	public SignalType Prefix { get; set; } = SignalType.Generic;
	public string Suffix { get; set; } = "";

	public Signal() { }

	[Hide, JsonIgnore]
	public string SignalIdentifier => $"{Prefix}{Suffix}";

	public override int GetHashCode()
	{
		return HashCode.Combine( SignalIdentifier );
	}

	public override bool Equals( object obj )
	{
		return obj is Signal other
			&& Equals( other );
	}

	public bool Equals( Signal other )
	{
		return other.SignalIdentifier == SignalIdentifier;
	}
}
