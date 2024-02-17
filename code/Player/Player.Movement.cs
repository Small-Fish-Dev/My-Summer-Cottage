namespace Sauna;

partial class Player
{
	public const float DUCK_HEIGHT = 58f;
	public const float HEIGHT = 72f;

	[Property, Category( "Movement" )] public float RootMotionSpeed { get; set; } = 245.0f;
	[Property, Category( "Movement" )] public float JumpVelocity { get; set; } = 220f;
	[Property, Category( "Movement" )] public Vector3 Gravity { get; set; } = Vector3.Down * 750f;

	[Sync] public bool Ducking { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 Velocity { get; set; }

	public GameObject GroundObject { get; protected set; }
	public bool Grounded => GroundObject.IsValid();
	public BBox Bounds => new BBox( Collider.Center - Collider.Scale / 2f, Collider.Center + Collider.Scale / 2f );

	private float animationSpeed
	{
		get
		{
			if ( Input.Down( "run" ) && !Ducking ) return 1f;
			return 0.5f;
		}
	}

	protected void UpdateMovement()
	{
		// Jump
		if ( Input.Down( "Jump" ) && Grounded )
		{
			Velocity += JumpVelocity * Vector3.Up;
			JumpBroadcast();
		}

		// Ducking
		var from = Transform.Position;
		var to = from + Vector3.Up * Bounds.Maxs.z;
		Ducking = (Ducking && Scene.Trace.Box( Bounds, in from, in to ).IgnoreGameObjectHierarchy( GameObject ).WithTag( "solid" ).Run().Hit)
			|| Input.Down( "duck" ); // Beautiful.

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
		Collider.Center = Vector3.Up * height / 2f;
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
		if ( Camera is null )
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
		if ( Model == null )
			return;

		Model.Set( "grounded", Grounded );
		Model.Set( "crouching", Ducking );

		var x = MathX.LerpTo( Model.GetFloat( "move_x" ), animationSpeed * Input.AnalogMove.x, 5f * Time.Delta );
		var y = MathX.LerpTo( Model.GetFloat( "move_y" ), -animationSpeed * Input.AnalogMove.y, 5f * Time.Delta );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	[Broadcast]
	protected void JumpBroadcast()
	{
		Model?.Set( "jump", true );
	}
}
