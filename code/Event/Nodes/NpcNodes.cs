using Sandbox;
using Sandbox.Utility;
using Sauna;
using static Sandbox.PhysicsContact;

public static partial class NpcNodes
{
	/// <summary>
	/// Punches the NPC, detaches them from the ground and throws them away local to their transform
	/// </summary>
	[ActionGraphNode( "npc.localpunch" )]
	[Title( "Local Punch NPC" ), Group( "NPC" ), Icon( "sports_mma" )]
	public static void LocalPunch( NPC npc, Vector3 launchDirection, float strength = 0f, float extraVerticalStrength = 0f )
	{
		if ( npc == null ) return;

		var punchDirection = launchDirection.Normal;
		var punch = punchDirection * strength + Vector3.Up * extraVerticalStrength;

		npc.LocalPunch( punch );
	}

	/// <summary>
	/// Punches the NPC, detaches them from the ground and throws them away starting from the worldSource
	/// </summary>
	[ActionGraphNode( "npc.worldpunch" )]
	[Title( "World Punch NPC" ), Group( "NPC" ), Icon( "sports_mma" )]
	public static void WorldPunch( NPC npc, Vector3 worldSource, float strength = 0f, float extraVerticalStrength = 0f )
	{
		if ( npc == null ) return;

		npc.WorldPunch( worldSource, strength, extraVerticalStrength );
	}

	/// <summary>
	/// Ragdolls/Unragdolls the NPC, signal will fire once it unragdolls
	/// </summary>
	[ActionGraphNode( "npc.ragdoll" )]
	[Title( "Ragdoll NPC" ), Group( "NPC" ), Icon( "accessibility_new" )]
	public static async Task Ragdoll( NPC npc, bool enabled = true, float duration = 1f, float spin = 0f )
	{
		if ( npc == null ) return;

		npc.SetRagdoll( enabled, duration, spin );

		while ( npc.Ragdoll != null )
			await GameTask.DelaySeconds( Time.Delta );

		return;
	}

	public delegate Task Body();

	/// <summary>
	/// Move to a position
	/// </summary>
	[ActionGraphNode( "npc.moveto" ), Pure]
	[Title( "Move To" ), Group( "NPC" ), Icon( "turn_right" )]
	public static async Task<Task> MoveTo( NPC npc, Vector3 position, Body? reachedDestination, Body? failedToReachDestination )
	{
		if ( npc == null ) return Task.CompletedTask;

		npc.MoveTo( position );
		var currentPosition = npc.TargetPosition;

		while ( npc.IsValid() && !npc.ReachedDestination && currentPosition == npc.TargetPosition )
			await GameTask.DelaySeconds( Time.Delta );

		if ( npc.IsValid() && npc.ReachedDestination && currentPosition == npc.TargetPosition )
			return reachedDestination?.Invoke();
		else
			return failedToReachDestination?.Invoke();
	}

	/// <summary>
	/// Start following a gameobject
	/// </summary>
	[ActionGraphNode( "npc.follow" ), Pure]
	[Title( "Follow" ), Group( "NPC" ), Icon( "follow_the_signs" )]
	public static async Task<Task> Follow( NPC npc, GameObject target, Body? reachedTarget, Body? failedToReachTarget )
	{
		if ( npc == null ) return Task.CompletedTask;

		npc.SetTarget( target );

		while ( npc.IsValid() && target.IsValid() && !npc.IsWithinRange( target, npc.Range + 5f ) && npc.FollowingTargetObject )
			await GameTask.DelaySeconds( Time.Delta );
		Log.Info( npc.FollowingTargetObject );
		if ( npc.IsValid() && target.IsValid() && npc.IsWithinRange( target, npc.Range + 10f ) && npc.FollowingTargetObject )
			return reachedTarget?.Invoke();
		else
			return failedToReachTarget?.Invoke();
	}

	/// <summary>
	/// Stop following whatever
	/// </summary>
	[ActionGraphNode( "npc.stopfollow" ), Pure]
	[Title( "Stop Following" ), Group( "NPC" ), Icon( "dangerous" )]
	public static void StopFollowing( NPC npc )
	{
		if ( npc == null ) return;

		npc.SetTarget( null );
	}
}
