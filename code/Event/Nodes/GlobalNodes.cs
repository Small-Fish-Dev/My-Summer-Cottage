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
}
