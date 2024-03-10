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

	/// <summary>
	/// Move to a position, signal will fire once it reached the destination
	/// </summary>
	[ActionGraphNode( "npc.moveto" ), Pure]
	[Title( "Move To" ), Group( "NPC" ), Icon( "turn_right" )]
	public static async Task MoveTo( NPC npc, Vector3 position )
	{
		if ( npc == null ) return;

		npc.MoveTo( position );

		while ( !npc.ReachedDestination )
			await GameTask.DelaySeconds( Time.Delta );

		return;
	}
}
