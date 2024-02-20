using Sandbox;
using Sauna;

public static partial class SaunaActionNodes
{

	/// <summary>
	/// Returns the player component from the GameObject if it has one
	/// </summary>
	[ActionGraphNode( "sauna.getplayer" ), Pure]
	[Title( "Get Player" ), Group( "Sauna" ), Icon( "group_add" )]
	public static Player GetPlayer( GameObject gameObject )
	{
		if ( gameObject == null ) return null;

		if ( gameObject.Components.TryGet<Player>( out Player player ) )
			return player;

		return null;
	}

	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away local to their transform
	/// </summary>
	[ActionGraphNode( "sauna.localpunchplayer" ), Pure]
	[Title( "Local Punch Player" ), Group( "Sauna" ), Icon( "sports_mma" )]
	public static void LocalPunch( Player player, Vector3 launchDirection, float strength )
	{
		if ( player == null ) return;

		var punchDirection = launchDirection.Normal;
		var punch = punchDirection * strength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away starting from the worldSource
	/// </summary>
	[ActionGraphNode( "sauna.worldpunchplayer" ), Pure]
	[Title( "World Punch Player" ), Group( "Sauna" ), Icon( "sports_mma" )]
	public static void WorldPunch( Player player, Vector3 worldSource, float strength )
	{
		if ( player == null ) return;

		var punchVector = player.Transform.Position - worldSource;
		var punchDirection = punchVector.Normal;
		var punch = punchDirection * strength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

}
