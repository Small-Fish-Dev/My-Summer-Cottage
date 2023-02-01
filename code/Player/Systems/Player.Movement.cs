namespace Sauna;

partial class Player
{
	// Client Inputs
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	/// <summary>
	/// Normalized vector of the direction the player wishes to move towards.
	/// </summary>
	public Vector3 WishVelocity { get; set; }

	/// <summary>
	/// The collider used for player's movement collisions.
	/// </summary>
	public BBox CollisionBox => new BBox( 
		new Vector3( -16, -16, 0 ), 
		new Vector3( 16, 16, Ducking ? 40 : 68 ) 
	);

	/// <summary>
	/// A boolean determining if the player is ducked or not.
	/// </summary>
	public bool Ducking { get; private set; } = false;

	/// <summary>
	/// Is the player in a water entity?
	/// </summary>
	public SaunaWater Water { get; set; } = null;

	// Private fields
	private float stepSize => 8f;
	private float walkSpeed => 150f;
	private float maxStandableAngle => 45f;
	private Vector3 gravity => Vector3.Down * 650f;

	/// <summary>
	/// Simulates the player's movement.
	/// </summary>
	/// <param name="cl"></param>
	protected void MoveSimulate( IClient cl )
	{
		// Handle ducking.
		Ducking = Water == null && Input.Down( InputButton.Duck );

		// Handle rotation.
		var yawAngles = new Angles( 0, ViewAngles.yaw, 0 );
		Rotation = Angles.Lerp( Rotation.Angles(), yawAngles, 10f * Time.Delta )
			.ToRotation();

		// Handle the player's wish velocity.
		var eyeRotation = ViewAngles.WithPitch( 0 ).ToRotation();
		WishVelocity = (InputDirection
			* eyeRotation).Normal.WithZ( 0 );

		// Calculate velocity.
		var targetVelocity = WishVelocity
			* (walkSpeed * (Water == null && Input.Down( InputButton.Run ) ? 5 : 1))
			* (Ducking ? 0.5f : 1f);

		Velocity = Vector3.Lerp( Velocity, targetVelocity, 10f * Time.Delta )
			.WithZ( Velocity.z );

		if ( GroundEntity == null )
			Velocity += gravity * Time.Delta;

		// Initialize MoveHelper.
		var helper = new MoveHelper( Position, Velocity )
		{
			MaxStandableAngle = maxStandableAngle
		};

		// Move the player using MoveHelper.
		helper.Trace = helper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.Ignore( this );
		helper.TryUnstuck();
		helper.TryMoveWithStep( Time.Delta, stepSize );

		if ( helper.HitWall )
			helper.ApplyFriction( 5f, Time.Delta );

		Position = helper.Position
			.WithZ( Water != null ? MathF.Max( Water.Position.z + 75 + Water.WaveOffset( Position ), helper.Position.z ) : helper.Position.z );
		Velocity = helper.Velocity
			.WithZ( Water != null ? 0 : Velocity.z );

		// Check for ground collision.
		if ( Velocity.z <= stepSize )
		{
			var tr = helper.TraceDirection( Vector3.Down * 2f );
			GroundEntity = tr.Entity;

			// Move to the ground if there is something.
			if ( GroundEntity != null )
			{
				Position += tr.Distance * Vector3.Down;

				if ( Velocity.z < 0.0f )
					Velocity = Velocity.WithZ( 0 );
			}
		}
		else
			GroundEntity = null;
	}

	public override void BuildInput()
	{
		// Handle the client's inputs.
		InputDirection = Input.AnalogMove;

		var ang = (ViewAngles + Input.AnalogLook);
		ViewAngles = ang
			.WithPitch( MathX.Clamp( ang.pitch, -90, 90 ) )
			.Normal;
	}
}
