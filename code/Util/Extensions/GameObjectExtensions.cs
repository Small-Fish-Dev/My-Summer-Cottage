namespace Sauna;

public static class GameObjectExtensions
{
	public static IEnumerable<Interaction> GetInteractions( this GameObject obj )
	{
		return obj.Components.GetAll().OfType<Interaction>();
	}
}
