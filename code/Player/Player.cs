namespace Sauna;

public partial class Player : AnimatedEntity
{
	[Net] private int drunkness { get; set; }
	private TimeSince lastTicked;
	public int Drunkness
	{
		get => drunkness;
		set
		{
			Game.AssertServer();

			drunkness = (int)MathX.Clamp( value, 0, 100 );
			lastTicked = 0;
		}
	}

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
	}

	public override void Simulate( IClient cl )
	{
		EyePosition = GetEyePosition();

		// Simulate the player's movement.
		MoveSimulate( cl );
		InteractionSimulate( cl );

		if ( Game.IsServer )
		{
			if ( lastTicked > 5f )
				Drunkness -= 1;
		}
	}

	public override void FrameSimulate( IClient cl )
	{
		// Simulate camera.
		CameraSimulate( cl );
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
