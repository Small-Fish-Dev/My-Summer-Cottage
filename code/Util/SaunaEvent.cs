namespace Sauna;

public static class SaunaEvent
{
	/// <summary>
	/// Ran everytime a player spawns. 
	/// Parameters: Player pawn
	/// </summary>
	public class OnSpawn : EventAttribute 
	{ 
		public OnSpawn() : base( nameof( SaunaEvent.OnSpawn ) ) { }
	}
}
