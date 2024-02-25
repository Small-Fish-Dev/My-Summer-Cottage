using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Game;

public static partial class GlobalNodes
{
	/// <summary>
	/// Is the game being played by a single player
	/// </summary>
	[ActionGraphNode( "event.issingleplayer" ), Pure]
	[Title( "Is Single Player" ), Group( "Events" ), Icon( "emoji_people" )]
	public static bool IsSingleplayer()
	{
		return Networking.Connections.Count > 1; // lol! :-)
	}

	/// <summary>
	/// Get every single player in game
	/// </summary>
	[ActionGraphNode( "event.geteveryplayer" ), Pure]
	[Title( "Get Every Player" ), Group( "Events" ), Icon( "groups" )]
	public static IEnumerable<Player> GetEveryPlayer()
	{
		return GameManager.ActiveScene.GetAllComponents<Player>();
	}

	/// <summary>
	/// Get every player inside of a collection of gameobjects
	/// </summary>
	[ActionGraphNode( "event.getveryplayerincollection" )]
	[Title( "Get Every Player In Collection" ), Group( "Events" ), Icon( "groups" )]
	public static IEnumerable<Player> GetEveryPlayerInCollection( IEnumerable<GameObject> gameObjects )
	{
		return gameObjects.Select( x => x.Components.TryGet<Player>( out var player ) ? player : null ) // Try and get the player from each item
			.Where( x => x != null ) // If it found no player, remove the item
			.Distinct(); // Make sure every player is counted once in the list
	}

	/// <summary>
	/// Is everyone in the game included in the players collection
	/// </summary>
	[ActionGraphNode( "event.iseveryplayerincluded" )]
	[Title( "Is Every Player Included" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static bool IsEveryPlayerIncluded( IEnumerable<Player> players )
	{
		var allPlayers = GameManager.ActiveScene.GetAllComponents<Player>();

		return allPlayers.All( x => players.Any( player => player == x ) );
	}

}
