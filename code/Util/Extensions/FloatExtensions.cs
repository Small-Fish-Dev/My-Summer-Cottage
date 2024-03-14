namespace Sauna;

public static class FloatExtensions
{
	public static string Timer( this float seconds )
	{
		return TimeSpan.FromSeconds( seconds.CeilToInt() ).ToString( @"mm\:ss" );
	}
}
