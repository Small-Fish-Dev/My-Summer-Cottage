namespace Sauna;

public static class GameObjectExtensions
{
	public static IEnumerable<Interaction> GetInteractions( this GameObject obj )
	{
		return obj.Components.Get<Interactions>()?.ObjectInteractions ?? new List<Interaction>();
	}
}
