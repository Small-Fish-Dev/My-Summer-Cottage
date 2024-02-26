﻿using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Game;

public static partial class GlobalNodes
{
	public delegate Task Body();

	/// <summary>
	/// Is the game being played by a single player
	/// </summary>
	[ActionGraphNode( "event.issingleplayer" ), Pure]
	[Title( "Is Single Player" ), Group( "Events" ), Icon( "emoji_people" )]
	public static bool IsSingleplayer()
	{
		return Networking.Connections.Count == 1;
	}

	/// <summary>
	/// Get every component of type {T} in the game
	/// </summary>
	[ActionGraphNode( "event.geteverycomponent" ), Pure]
	[Title( "Get Every {T} Component" ), Group( "Events" ), Icon( "groups" )]
	public static IEnumerable<T> GetEveryComponent<T>()
	{
		return GameManager.ActiveScene.GetAllComponents<T>();
	}

	/// <summary>
	/// Get every component of type {T} in the provided collection
	/// </summary>
	[ActionGraphNode( "event.geteverycomponentincollection" )]
	[Title( "Get Every {T} Component In Collection" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static IEnumerable<T> GetAllComponentsInCollection<T>( IEnumerable<GameObject> collection )
	{
		return collection.SelectMany( x => x.Components.GetAll<T>() );
	}

	/// <summary>
	/// Is every in the collectionA included inside of collectionB
	/// </summary>
	[ActionGraphNode( "event.iseverycomponentincluded" )]
	[Title( "Is Every Component {T} Included" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static bool IsEveryComponentIncluded<T>( IEnumerable<Component> collectionA, IEnumerable<Component> collectionB ) where T : Component
	{
		return collectionA.All( x => collectionB.Any( y => x == y ) );
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