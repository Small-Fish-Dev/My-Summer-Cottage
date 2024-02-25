using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Game;

public static partial class GlobalNodes
{
	public delegate Task Body();

	[ActionGraphNode( "if" )]
	public static Task If( bool condition,
		Body? @true = null,
		Body? @false = null )
	{
		return (condition ? @true : @false)?.Invoke() ?? Task.CompletedTask;
	}
	/// <summary>
	/// Is the game being played by a single player
	/// </summary>
	[ActionGraphNode( "event.issingleplayer", DefaultOutputSignal = false )]
	[Title( "Is Single Player" ), Group( "Events" ), Icon( "emoji_people" )]
	public static Task IsSingleplayer( Body? @true = null, Body? @false = null )
	{
		return (Networking.Connections.Count > 1 ? @true : @false)?.Invoke() ?? Task.CompletedTask;
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
	[ActionGraphNode( "event.iseveryplayerincluded", DefaultOutputSignal = false )]
	[Title( "Is Every Player Included" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static Task IsEveryPlayerIncluded( IEnumerable<Player> players, Body? @true = null, Body? @false = null )
	{
		var allPlayers = GameManager.ActiveScene.GetAllComponents<Player>();

		return (allPlayers.All( x => players.Any( player => player == x ) ) ? @true : @false)?.Invoke() ?? Task.CompletedTask;
	}

	/// <summary>
	/// Skips in game seconds, only the host can do this so make sure to check if it's singleplayer or if everyone is included in the event
	/// </summary>
	[ActionGraphNode( "event.skiptime" )]
	[Title( "Skip Time" ), Group( "Events" ), Icon( "update" )]
	public static void SkipTime( int inGameSeconds )
	{
		var timeManager = GameManager.ActiveScene.GetAllComponents<GameTimeManager>()?.First() ?? null;

		if ( timeManager != null )
			timeManager.SkipTimeFromSeconds( inGameSeconds );
	}
}
