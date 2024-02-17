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
	[Property]
	[Category( "Movement" )]
	[Range( 0f, 400f, 1f )]
	public float Speed { get; set; } = 140f;

	/// <summary>
	/// How fast you move when holding the sprint button
	/// </summary>
	[Property]
	[Category( "Movement" )]
	[Range( 0f, 800f, 1f )]
	public float SprintSpeed { get; set; } = 280f;

	/// <summary>
	/// How fast you move when holding the walk button
	/// </summary>
	[Property]
	[Category( "Movement" )]
	[Range( 0f, 200f, 1f )]
	public float WalkSpeed { get; set; } = 80f;

	/// <summary>
	/// How fast you move when holding the duck button
	/// </summary>
	[Property]
	[Category( "Movement" )]
	[Range( 0f, 200f, 1f )]
	public float DuckSpeed { get; set; } = 60f;

	/// <summary>
	/// How high you can jump
	/// </summary>
	[Property]
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

		MoveHelper.WishVelocity = wishVelocity;

		if ( Input.Pressed( "Jump" ) && MoveHelper.IsOnGround )
		{
			MoveHelper.Punch( Vector3.Up * JumpStrength );
			JumpBroadcast();
		}

		// Ducking
		var from = Transform.Position;
		var to = from + Vector3.Up * Bounds.Maxs.z;
		Ducking = (Ducking && Scene.Trace.Box( Bounds, in from, in to ).IgnoreGameObjectHierarchy( GameObject ).WithTag( "solid" ).Run().Hit)
			|| Input.Down( "duck" ); // Beautiful.

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
			Transform.Position = down.EndPosition;

		// Update Collider
		var height = Ducking ? DUCK_HEIGHT : HEIGHT;
		Collider.Scale = Collider.Scale.WithZ( height );
		Collider.Center = Vector3.Up * height / 2f;*/
	}

	protected void UpdateAngles()
	{
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

		Model?.SceneModel?.SetBoneWorldTransform( 6, new Transform( eyes.Position + rot.Backward * 10, Rotation.Identity, 0 ) );
	}

	protected void UpdateAnimation()
	{
		if ( Model == null || Model.SceneModel == null || MoveHelper == null )
			return;

		Model.Set( "grounded", MoveHelper.IsOnGround );
		Model.Set( "crouching", Ducking );

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Forward ) / 100f;
		var newY = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Right ) / 100f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );

		Model.SceneModel.Morphs.Set( "fat", Fatness );
		Model.Set( "height", Height );
	}

	[Broadcast]
	protected void JumpBroadcast()
	{
		Model?.Set( "jump", true );
	}
}
