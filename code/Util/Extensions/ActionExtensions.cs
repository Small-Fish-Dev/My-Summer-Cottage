namespace Sauna;

public static class ActionExtensions
{
	public static bool InvokeOrDefault( this Func<bool> func )
	{
		return func?.Invoke() ?? false;
	}
}
