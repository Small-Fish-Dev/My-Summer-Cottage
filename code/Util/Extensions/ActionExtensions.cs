namespace Sauna;

public static class ActionExtensions
{
	public static T InvokeOrDefault<T>( this Func<T> func )
	{
		return func == null 
			? default( T )
			: func.Invoke();
	}
}
