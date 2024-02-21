using Sandbox;
using Sauna;

public static partial class SaunaActionNodes
{
	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away local to their transform
	/// </summary>
	[ActionGraphNode( "event.localpunchplayer" )]
	[Title( "Local Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
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
	[ActionGraphNode( "event.worldpunchplayer" )]
	[Title( "World Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
	public static void WorldPunch( Player player, Vector3 worldSource, float strength )
	{
		if ( player == null ) return;

		var punchVector = player.Transform.Position + Vector3.Up * 36f - worldSource;
		var punchDirection = punchVector.Normal;
		var punch = punchDirection * strength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

}
