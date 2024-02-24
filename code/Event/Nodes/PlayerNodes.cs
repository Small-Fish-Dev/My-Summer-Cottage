﻿using Sandbox;
using Sandbox.Utility;
using Sauna;
using static Sandbox.PhysicsContact;

public static partial class PlayerNodes
{
	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away local to their transform
	/// </summary>
	[ActionGraphNode( "event.localpunchplayer" )]
	[Title( "Local Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
	public static void LocalPunch( Player player, Vector3 launchDirection, float strength = 0f, float extraVerticalStrength = 0f )
	{
		if ( player == null ) return;

		var punchDirection = launchDirection.Normal;
		var punch = punchDirection * strength + Vector3.Up * extraVerticalStrength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

	/// <summary>
	/// Punches the player, detaches them from the ground and throws them away starting from the worldSource
	/// </summary>
	[ActionGraphNode( "event.worldpunchplayer" )]
	[Title( "World Punch Player" ), Group( "Events" ), Icon( "sports_mma" )]
	public static void WorldPunch( Player player, Vector3 worldSource, float strength = 0f, float extraVerticalStrength = 0f )
	{
		if ( player == null ) return;

		var punchVector = player.Transform.Position + Vector3.Up * 36f - worldSource;
		var punchDirection = punchVector.Normal;
		var punch = punchDirection * strength + Vector3.Up * extraVerticalStrength;

		if ( player.Components.TryGet<MoveHelper>( out MoveHelper moveHelper ) )
			moveHelper.Punch( punch );
	}

	/// <summary>
	/// Ragdolls/Unragdolls the player
	/// </summary>
	[ActionGraphNode( "event.ragdollplayer" )]
	[Title( "Ragdoll Player" ), Group( "Events" ), Icon( "accessibility_new" )]
	public static async Task Ragdoll( Player player, bool enabled = true, bool forced = true, float duration = 1f, float spin = 0f )
	{
		if ( player == null ) return;

		player.SetRagdoll( enabled, forced, duration, spin );

		while ( player.IsRagdolled )
		{
			await Task.Delay( (int)(Time.Delta * 1000) );
		}
	}

	/// <summary>
	/// Fades to black
	/// </summary>
	[ActionGraphNode( "event.blackscreen" ), Pure]
	[Title( "Black Screen" ), Group( "Events" ), Icon( "blinds" )]
	public static async Task BlackScreen( Player player, float startingTransition = 2f, float blackTransition = 2f, float endingTransition = 1f )
	{
		if ( player == null ) return;

		player.BlackScreen( startingTransition, blackTransition, endingTransition );
		Log.Info( startingTransition );
		Log.Info( blackTransition );
		Log.Info( endingTransition );

		await Task.Delay( (int)(startingTransition + blackTransition + endingTransition) * 1000 );
	}
}
