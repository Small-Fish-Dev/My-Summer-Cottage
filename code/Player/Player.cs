namespace Sauna;

public partial class Player : AnimatedEntity
{
	/// <summary>
	/// The ragdoll entity of this player.
	/// </summary>
	[Net] public AnimatedEntity Ragdoll { get; set; }

	private TimeSince lastStepped;
	private Particles peeParticle;
	private Sound? peeSound;
	private TimeSince lastPeeSound = 0f;

	public override void Spawn()
	{
		SetModel( "models/guy/guy.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = false;
		EnableShadowCasting = false;

		Position = Entity.All.OfType<SpawnPoint>().FirstOrDefault().Position;
	}

	public override void Simulate( IClient cl )
	{
		// Get the position of the player's eyes.
		EyePosition = GetEyePosition();

		// Simulate player's effects.
		EffectSimulate( cl );

		// Don't do the rest if we're ragdolled.
		if ( Ragdoll != null && Ragdoll.IsValid )
			return;

		// Simulate the player.
		InteractionSimulate( cl );
		MoveSimulate( cl );

		if ( Game.IsClient ) 
			return;

		// Pissing
		if ( Input.Down( InputButton.PrimaryAttack ) )
		{
			peeParticle ??= Particles.Create( "particles/piss.vpcf" );

			if ( peeSound == null )
				peeSound = Sound.FromEntity( "sounds/water/water_stream.sound", this );

			var transform = GetPenoidTransform();
			peeParticle?.SetPosition( 0, transform.Position + transform.Rotation.Backward * 2f );
			peeParticle?.SetPosition( 1, transform.Position + transform.Rotation.Forward * 25f );

			if ( lastPeeSound >= 0.3f )
			{
				// Cheese the piss sounds until I find a way to get them to play when the particle hits
				var pissTrace = Trace.Ray( transform.Position, transform.Position + transform.Rotation.Forward * 50f )
					.Radius( 1 )
					.WithoutTags( "trigger" )
					.Ignore( this )
					.Run();

				Sound.FromWorld( "sounds/water/water_splat.sound", pissTrace.EndPosition );
				lastPeeSound = 0f;
			}
		}
		else
		{
			peeParticle?.Destroy();
			peeParticle = null;
			
			peeSound?.Stop();
			peeSound = null;
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		// Simulate camera.
		CameraSimulate( cl );
	}

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
		if ( Ragdoll != null && Ragdoll.IsValid )
			return;

		if ( !Game.IsClient || lastStepped < 0.2f )
			return;

		volume *= Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f );
		lastStepped = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
			.WithoutTags( "trigger" )
			.Ignore( this )
			.Run();

		if ( !tr.Hit ) 
			return;

		tr.Surface.DoFootstep( this, tr, foot, volume );
	}

	/// <summary>
	/// Please do not use this. 
	/// Use Eventlogger.Send(...) instead.
	/// </summary>
	/// <param name="data"></param>
	[ClientRpc]
	public static void _sendEventlog( byte[] data )
	{
		Eventlogger.FromBytes( data );
	}

	/// <summary>
	/// Ragdoll or unragdoll the player.
	/// </summary>
	public void ToggleRagdoll( Vector3? force = null, Vector3? forcePosition = null )
	{
		Game.AssertServer();

		// Reset the player.
		EnableDrawing = Ragdoll != null;
		Position = Ragdoll != null
			? Ragdoll.Position
			: Position;
		EnableAllCollisions = EnableDrawing;

		ResetInterpolation();
		ResetAnimParameters();

		// Remove ragdoll.
		if ( Ragdoll != null && Ragdoll.IsValid )
		{
			Ragdoll.Delete();
			return;
		}

		// Create ragdoll.
		Ragdoll = new AnimatedEntity();
		Ragdoll.UseAnimGraph = false;
		Ragdoll.Tags.Add( "ragdoll", "solid", "debris" );

		Ragdoll.Position = Position;
		Ragdoll.Rotation = Rotation;
		Ragdoll.UsePhysicsCollision = true;
		Ragdoll.EnableAllCollisions = true;
		Ragdoll.Transmit = TransmitType.Always;

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

	[ConCmd.Server("ragdol")]
	public static void Test()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player pawn )
			return;

		pawn.ToggleRagdoll();
	}
}
