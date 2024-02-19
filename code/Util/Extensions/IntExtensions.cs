namespace Sauna;

public static class IntExtensions
{
	public static float ToKilograms( this int grams )
	{
		return grams / 1000;
	}
}
