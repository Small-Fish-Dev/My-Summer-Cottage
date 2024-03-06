using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Event;

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
		return Game.ActiveScene.GetAllComponents<T>();
	}

	/// <summary>
	/// Get every component of type {T} in the provided collection
	/// </summary>
	[ActionGraphNode( "event.geteverycomponentincollection" ), Pure]
	[Title( "Get Every {T} Component In Collection" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static IEnumerable<T> GetAllComponentsInCollection<T>( IEnumerable<GameObject> collection )
	{
		return collection.SelectMany( x => x.Components.GetAll<T>() );
	}

	/// <summary>
	/// Is every in the collectionA included inside of collectionB
	/// </summary>
	[ActionGraphNode( "event.iseverycomponentincluded" ), Pure]
	[Title( "Is Every Component {T} Included" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static bool IsEveryComponentIncluded<T>( IEnumerable<Component> collectionA, IEnumerable<Component> collectionB ) where T : Component
	{
		return collectionA.All( x => collectionB.Any( y => x == y ) );
	}

	/// <summary>
	/// Get the amount of items in the list
	/// </summary>
	[ActionGraphNode( "event.getitemamount" ), Pure]
	[Title( "Get Item Amount" ), Group( "Events" ), Icon( "reduce_capacity" )]
	public static int GetItemAmount( IEnumerable<ItemComponent> itemList, string name )
	{
		if ( itemList == null ) return 0;
		return itemList.Where( x => x.Name == name ).Count();
	}

	/// <summary>
	/// Skips in game seconds, only the host can do this so make sure to check if it's singleplayer or if everyone is included in the event
	/// </summary>
	[ActionGraphNode( "event.skiptime" )]
	[Title( "Skip Time" ), Group( "Events" ), Icon( "update" )]
	public static void SkipTime( int inGameSeconds )
	{
		var timeManager = Game.ActiveScene.GetAllComponents<GameTimeManager>()?.First() ?? null;

		if ( timeManager != null )
			timeManager.SkipTimeFromSeconds( inGameSeconds );
	}

	/// <summary>
	/// End this session, sets the next story day, opens the recap, unloads all events, and saves all progress
	/// </summary>
	[ActionGraphNode( "event.endsession" )]
	[Title( "End Session" ), Group( "Events" ), Icon( "view_day" )]
	public static void EndSession()
	{
		StoryMaster.EndSession();
	}


	/// <summary>
	/// Start this session, loads the event pool
	/// </summary>
	[ActionGraphNode( "event.startsession" )]
	[Title( "Start Session" ), Group( "Events" ), Icon( "view_day" )]
	public static void StartSession()
	{
		StoryMaster.StartSession();
	}
}
