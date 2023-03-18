namespace Sauna;

partial class Player : AnimatedEntity
{
	/// <summary>
	/// The ragdoll entity of this player.
	/// </summary>
	[Net] public AnimatedEntity Ragdoll { get; set; }

	/// <summary>
	/// Ragdoll or unragdoll the player.
	/// </summary>
	public void SetRagdoll( bool state, Vector3? force = null, Vector3? forcePosition = null )
	{
		// Only allow for server to handle this.
		if ( !Game.IsServer )
			return;

		// Reset the player.
		EnableDrawing = !state;
		Position = !state
			? Ragdoll?.Position ?? Position
			: Position;
		EnableAllCollisions = !state;

		ResetInterpolation();
		ResetAnimParameters();

		// Remove ragdoll.
		if ( !state )
		{
			Ragdoll?.Delete();
			Ragdoll = null;

			return;
		}

		if ( Ragdoll != null && Ragdoll.IsValid )
			return;

		// Create ragdoll.
		Ragdoll = new AnimatedEntity();
		Ragdoll.UseAnimGraph = false;
		Ragdoll.Tags.Add( "ragdoll", "solid", "debris" );

		Ragdoll.Position = Position;
		Ragdoll.Rotation = Rotation;
		Ragdoll.UsePhysicsCollision = true;
		Ragdoll.EnableAllCollisions = true;

		Ragdoll.SetModel( GetModelName() );
		Ragdoll.CopyBonesFrom( this );
		Ragdoll.CopyFrom( this );
		Ragdoll.CopyMorphs( this );

		Ragdoll.EnableAllCollisions = true;
		Ragdoll.SurroundingBoundsMode = SurroundingBoundsType.Physics;
		Ragdoll.PhysicsGroup.Velocity = Velocity;
		Ragdoll.PhysicsEnabled = true;

		// Copy all children.
		foreach ( var child in Children )
		{
			if ( child is not ModelEntity childEntity )
				continue;

			var copy = new ModelEntity();
			copy.SetModel( childEntity.GetModelName() );
			copy.SetParent( Ragdoll, true );
			copy.CopyBodyGroups( Ragdoll );
			copy.CopyMaterialGroup( Ragdoll );
		}

		// Apply force.
		if ( force != null )
		{
			if ( forcePosition != null )
			{
				var body = Ragdoll.PhysicsBody;

				if ( body != null )
				{
					body.ApplyImpulseAt( forcePosition.Value, force.Value * body.Mass );
					return;
				}
			}

			Ragdoll.PhysicsGroup.ApplyImpulse( force.Value );
		}
	}
	
	[ConCmd.Server( "ragdoll" )]
	public static void RagdollPlayer()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player pawn )
			return;

		var effect = pawn.Effects.Get<Unconscious>();

		if ( (pawn.Ragdoll == null || !pawn.Ragdoll.IsValid) && effect == null )
		{
			effect = pawn.Effects.Apply<Unconscious>( permanent: true );
			effect.SelfApplied = true;
		}

		// Don't allow for the player to unragdoll if it wasn't caused by the player.
		else if ( effect != null && effect.SelfApplied )
			pawn.Effects.Remove( effect );
	}
}
