namespace Sauna;

public partial class Player
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
		new Vector3( 16, 16, Ducking ? 32 : 64 ) 
	);

	/// <summary>
	/// A boolean determining if the player is ducked or not.
	/// </summary>
	public bool Ducking { get; private set; } = false;

	// Private fields
	private float stepSize => 8f;
	private float walkSpeed => 150f;
	private float maxStandableAngle => 60f;
	private Vector3 gravity => Vector3.Down * 650f;

	/// <summary>
	/// Simulates the player's movement.
	/// </summary>
	/// <param name="cl"></param>
	protected void MoveSimulate( IClient cl )
	{
		// Handle ducking.
		Ducking = Input.Down( InputButton.Duck );

		// Handle the player's wish velocity.
		var eyeRotation = ViewAngles.WithPitch( 0 ).ToRotation();
		WishVelocity = (InputDirection 
			* eyeRotation).Normal.WithZ( 0 );

		// Calculate velocity.
		var targetVelocity = WishVelocity 
			* walkSpeed 
			* (Ducking ? 0.5f : 1f);

		Velocity = Vector3.Lerp( Velocity, targetVelocity, 10f * Time.Delta )
			.WithZ( Velocity.z );

		if ( GroundEntity == null )
			Velocity += gravity * Time.Delta;

		// Move the player using the MoveHelper struct.
		var helper = new MoveHelper( Position, Velocity ) 
		{ 
			MaxStandableAngle = maxStandableAngle 
		};

		helper.Trace = helper.Trace
			.Size( CollisionBox.Mins, CollisionBox.Maxs )
			.Ignore( this );

		helper.TryUnstuck();
		helper.TryMoveWithStep( Time.Delta, stepSize );

		Position = helper.Position;
		Velocity = helper.Velocity;

		// Check for ground collision.
		if ( Velocity.z <= stepSize )
		{
			var tr = helper.TraceDirection( Vector3.Down );
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
		ViewAngles = (ViewAngles + Input.AnalogLook).Normal;
	}
}
