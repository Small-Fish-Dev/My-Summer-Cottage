namespace Sauna;

public partial class Player : AnimatedEntity
{
	private TimeSince lastStepped;
	Particles PeeParticle;

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

		PeeParticle = Particles.Create( "particles/piss.vpcf" );
	}

	public override void Simulate( IClient cl )
	{
		EyePosition = GetEyePosition();

		// Simulate the player.
		MoveSimulate( cl );
		InteractionSimulate( cl );
		EffectSimulate( cl );

		PeeParticle?.SetPosition( 0, Position + Vector3.Up * 40f );
		PeeParticle?.SetPosition( 1, Position + Vector3.Up * 40f + Rotation.Forward * 25 );
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

		volume *= Velocity.WithZ( 0 ).Length.LerpInverse( 0.0f, 200.0f ) * 2f;
		lastStepped = 0;

		var tr = Trace.Ray( pos, pos + Vector3.Down * 20 )
			.Radius( 1 )
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
