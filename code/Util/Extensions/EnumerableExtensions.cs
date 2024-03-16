namespace Sauna;

public static class EnumerableExtensions
{
	public static int HashCombine<T>( this IEnumerable<T> e, Func<T, decimal> selector )
	{
		var result = 0;

		foreach ( var el in e )
			result = HashCode.Combine( result, selector.Invoke( el ) );

		return result;
	}

	public static byte[] Serialize<T>( this T data ) => JsonSerializer.SerializeToUtf8Bytes( data );
	public static T Deserialize<T>( this byte[] bytes ) => JsonSerializer.Deserialize<T>( bytes );
}
