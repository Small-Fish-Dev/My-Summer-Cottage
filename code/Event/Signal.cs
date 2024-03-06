namespace Sauna;

public class SignalConverter : JsonConverter<Signal>
{
	public override Signal Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
	{
		if ( reader.TokenType == JsonTokenType.String )
		{
			string signalString = reader.GetString();
			return new Signal( signalString );
		}

		if ( reader.TokenType == JsonTokenType.StartObject )
		{
			string signalString = "";
			while ( reader.Read() )
			{
				if ( reader.TokenType == JsonTokenType.EndObject )
				{
					break;
				}
				if ( reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "Identifier" )
				{
					reader.Read();
					signalString = reader.GetString();
				}
			}
			return new Signal( signalString );
		}

		return new Signal();
	}

	public override void Write( Utf8JsonWriter writer, Signal value, JsonSerializerOptions options )
	{
		writer.WriteStringValue( value.Identifier );
	}
}

[JsonConverter( typeof( SignalConverter ) )]
public struct Signal : IEquatable<Signal>
{
	public string Identifier { get; set; }

	public Signal( string identifier )
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
	public override string ToString()
	{
		return Identifier.ToString();
	}

	public static implicit operator string( Signal signal )
	{
		return signal.ToString();
	}

	public static implicit operator Signal( string signal )
	{
		return new Signal( signal );
	}
}
