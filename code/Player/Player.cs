namespace Sauna;

public partial class Player : AnimatedEntity
{
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

		// Simulate event.
		Event.Run( nameof( SaunaEvent.Simulate ), cl );

		// Pissing
		if ( Game.IsClient || Ragdoll != null ) 
			return;

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
		Event.Run( nameof( SaunaEvent.FrameSimulate ), cl );
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
}
