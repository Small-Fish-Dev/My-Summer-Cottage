namespace Sauna;

partial class Player
{
	/// <summary>
	/// The item the player is currently holding.
	/// </summary>
	public BaseItem Holding => holding;

	[Net] private BaseItem holding { get; set; }

	/// <summary>
	/// Hold a specific item.
	/// </summary>
	/// <param name="item"></param>
	public void Hold( BaseItem item )
	{
		if ( item == null || holding != null || holding.IsValid )
			return;

		holding = item;
	}
}
