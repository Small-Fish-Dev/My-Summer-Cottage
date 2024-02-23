namespace Sauna;

partial class Player
{
	public const float DUCK_HEIGHT = 58f;
	public const float HEIGHT = 72f;

	[Property]
	[Category( "Movement" )]
	public MoveHelper MoveHelper { get; set; }

	/// <summary>
	/// How fast you move normally
	/// </summary>
	[Property, Sync]
	[Category( "Movement" )]
	[Range( 0f, 400f, 1f )]
	public float Speed { get; set; } = 140f;

	/// <summary>
	/// How fast you move when holding the sprint button
	/// </summary>
	[Property, Sync]
	[Category( "Movement" )]
	[Range( 0f, 800f, 1f )]
	public float SprintSpeed { get; set; } = 280f;

	/// <summary>
	/// How fast you move when holding the walk button
	/// </summary>
	[Property, Sync]
	[Category( "Movement" )]
	[Range( 0f, 200f, 1f )]
	public float WalkSpeed { get; set; } = 60f;

	/// <summary>
	/// How fast you move when holding the duck button
	/// </summary>
	[Property, Sync]
	[Category( "Movement" )]
	[Range( 0f, 200f, 1f )]
	public float DuckSpeed { get; set; } = 60f;

	/// <summary>
	/// How high you can jump
	/// </summary>
	[Property, Sync]
	[Category( "Movement" )]
	[Range( 0f, 800f, 1f )]
	public float JumpStrength { get; set; } = 200f;

	[Sync] public bool Ducking { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 Velocity { get; set; }

	public BBox Bounds => new BBox( Collider.Center - Collider.Scale / 2f, Collider.Center + Collider.Scale / 2f );

	protected void UpdateMovement()
	{
		if ( MoveHelper == null ) return;

		var isWalking = Input.Down( "Walk" );
		var isSprinting = Input.Down( "Run" );
		var isDucking = Input.Down( "Duck" );

		var wishSpeed = isDucking ? DuckSpeed : (isWalking ? WalkSpeed : (isSprinting ? SprintSpeed : Speed));
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * EyeAngles.WithPitch( 0f );

		MoveHelper.WishVelocity = BlockInputs ? Vector3.Zero : wishVelocity;

		if ( !BlockInputs && Input.Pressed( "Jump" ) && MoveHelper.IsOnGround )
		{
			Model?.Set( "jump", true );
			JumpBroadcast();
		}

		// Ducking
		var from = Transform.Position;
		var to = from + Vector3.Up * HEIGHT;

		// If we block inputs let's just keep whatever ducking state you were in
		if ( !BlockInputs )
		{
			Ducking = (Ducking && Scene.Trace.Ray( in from, in to ).Size( Collider.Scale.WithZ( 0f ) ).IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "trigger" ).Run().Hit)
				|| Input.Down( "duck" ); // Beautiful.
		}

		MoveHelper.Move();

		/*
		// Normal movement
		var rootMotion = Model.RootMotion.Position;
		Velocity = (rootMotion * RootMotionSpeed * Transform.Rotation).WithZ( Velocity.z );

		var tr = Scene.Trace.Box( Bounds, Transform.Position, Transform.Position )
			.IgnoreGameObjectHierarchy( GameObject );
		
		var helper = new CharacterControllerHelper( tr, Transform.Position, Velocity );
		helper.TryMove( Time.Delta );

		Transform.Position = helper.Position;
		Velocity = helper.Velocity;

		// Gravity
		var down = helper.TraceFromTo( Transform.Position, Transform.Position + Vector3.Down * 2f );
		GroundObject = down.GameObject;

		if ( !Grounded )
			Velocity += Gravity * Time.Delta;
		else
			Transform.Position = down.EndPosition;*/

		// Update Collider
		var height = Ducking ? DUCK_HEIGHT : HEIGHT;
		var bbox = MoveHelper.CollisionBBox;

		MoveHelper.CollisionBBox = new BBox( bbox.Mins, bbox.Maxs.WithZ( height ) );
		Collider.Scale = Collider.Scale.WithZ( height );
		Collider.Center = Vector3.Up * height / 2f;
	}

	protected void UpdateAngles()
	{
		if ( BlockMouseAim ) return;

		var ang = EyeAngles;
		ang += Input.AnalogLook;
		ang.pitch = ang.pitch.Clamp( -89, 89 );
		EyeAngles = ang;
	}

	protected void UpdateCamera()
	{
		if ( Camera == null )
			return;

		var eyes = Model.GetAttachment( "eyes" ) ?? Transform.World;
		var rot = Transform.Rotation;
		Camera.Transform.Position = eyes.Position + rot.Forward * 2.4f;
		Camera.Transform.Rotation = EyeAngles.ToRotation();
		Camera.FieldOfView = 90f;
		Camera.ZNear = 2.5f;

		Model?.SceneModel?.SetBoneWorldTransform( 7, new Transform( eyes.Position + rot.Backward * 10, Rotation.Identity, 0 ) );
	}

	protected void UpdateAnimation()
	{
		if ( Model == null || Model.SceneModel == null || MoveHelper == null )
			return;

		Model.Set( "grounded", MoveHelper.IsOnGround );
		Model.Set( "crouching", Ducking );

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Forward ) / 170f;
		var newY = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Right ) / 170f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );

		Model.SceneModel.Morphs.Set( "fat", Fatness );
		Model.Set( "height", Height );

		Model.Set( "lookat", EyeAngles.WithYaw( 0 ).Forward );
	}

	private void OnJumpEvent( SceneModel.GenericEvent e )
	{
		if ( e.Type == "jump" )
			MoveHelper.Punch( Vector3.Up * JumpStrength );
	}

	[Broadcast]
	protected void JumpBroadcast()
	{
		Model?.Set( "jump", true );
	}
}
