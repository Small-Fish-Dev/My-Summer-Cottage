namespace Sauna;

public partial class Player : AnimatedEntity
{
	private TimeSince lastStepped;
	private Particles peeParticle;
	private Sound peeSound;
	private TimeSince lastPeeSound = 0f;

	public override void Spawn()
	{
		SetModel( "models/guy/guy.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
		EnableShadowCasting = true;

		Position = Entity.All.OfType<SpawnPoint>().FirstOrDefault().Position;

		Event.Run( "onSpawn", this );
	}

	public override void Simulate( IClient cl )
	{
		EyePosition = GetEyePosition();

		// Simulate the player.
		MoveSimulate( cl );
		InteractionSimulate( cl );
		EffectSimulate( cl );

		if ( Game.IsClient ) return;

		// Pissing
		if ( Input.Down( InputButton.PrimaryAttack ) )
		{
			peeParticle ??= Particles.Create( "particles/piss.vpcf" );
			if ( !peeSound.IsPlaying )
			{
				peeSound = Sound.FromEntity( "sounds/water/water_stream.sound", this );

			}

			peeParticle?.SetPosition( 0, Position + Vector3.Up * 40f );
			peeParticle?.SetPosition( 1, Position + Vector3.Up * 40f + Rotation.Forward * 25f );
			peeParticle?.SetPosition( 2, Velocity.WithZ( 0 ) );

			if ( lastPeeSound >= 0.3f )
			{
				// Cheese the piss sounds until I find a way to get them to play when the particle hits
				var pissTrace = Trace.Ray( Position + Vector3.Up * 40f, Position + Rotation.Forward * 50f )
				.Radius( 1 )
				.WithoutTags( "trigger" )
				.Ignore( this )
				.Run();

				Sound.FromWorld( "sounds/water/water_splat.sound", pissTrace.HitPosition );
				lastPeeSound = 0f;
			}

		}
		else
		{
			peeParticle?.Destroy();
			peeParticle = null;
			peeSound.Stop();
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		// Simulate camera.
		CameraSimulate( cl );
	}

	public override void OnAnimEventFootstep( Vector3 pos, int foot, float volume )
	{
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
	/// Avoid using this function, use Subtitles.Send(..) instead.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="col"></param>
	/// <param name="time"></param>
	/// <param name="wrapper"></param>
	[ClientRpc]
	public static void SendSubtitle( string text, Color col, float time = 5f, char wrapper = '"' )
	{
		Subtitles.Show( text, time, col, wrapper );
	}
}
