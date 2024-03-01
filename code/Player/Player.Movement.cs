namespace Sauna;

partial class Player
{
	public const float DUCK_HEIGHT = 58f;
	public const float HEIGHT = 72f;

	public static bool HideHead
	{
		get => _hideHead;
		set
		{
			_hideHead = value;
			Local?.UpdateHeadVisibility();
			Local?.Renderer?.SceneModel?.Update( RealTime.Delta );
		}
	}
	private static bool _hideHead;

	[Property]
	[Category( "Movement" )]
	public MoveHelper MoveHelper { get; set; }

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

	/// <summary>
	/// Zoom field of view
	/// </summary>
	[Property, Sync]
	[Category( "Camera" )]
	[Range( 20f, 40f, 30f )]
	public float Zoom { get; set; } = 30f;

	[Sync] public bool Ducking { get; set; }
	[Sync] public Angles EyeAngles { get; set; }
	[Sync] public Vector3 Velocity { get; set; }

	public BBox Bounds => new BBox( Collider.Center - Collider.Scale / 2f, Collider.Center + Collider.Scale / 2f );

	protected void UpdateMovement()
	{
		if ( MoveHelper == null ) return;

		var isWalking = Input.Down( InputAction.Walk );
		var isDucking = Input.Down( InputAction.Duck );

		var wishSpeed = isDucking ? DuckSpeed : isWalking ? WalkSpeed : SprintSpeed;
		var wishVelocity = Input.AnalogMove.Normal * wishSpeed * EyeAngles.WithPitch( 0f );

		MoveHelper.WishVelocity = BlockInputs ? Vector3.Zero : wishVelocity;

		if ( !BlockInputs && Input.Pressed( InputAction.Jump ) && MoveHelper.IsOnGround )
		{
			Renderer?.Set( "jump", true );
			JumpBroadcast();
		}

		// Ducking
		var from = Transform.Position;
		var to = from + Vector3.Up * HEIGHT;

		// If we block inputs let's just keep whatever ducking state you were in
		if ( !BlockInputs )
		{
			Ducking = (Ducking && Scene.Trace.Ray( in from, in to ).Size( Collider.Scale.WithZ( 0f ) ).IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "trigger" ).Run().Hit)
				|| Input.Down( InputAction.Duck ); // Beautiful.
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

		if ( Input.Pressed( "Ragdoll" ) && CanRagdoll )
			SetRagdoll( !IsRagdolled );
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

		var eyes = Renderer.GetAttachment( "eyes" ) ?? Transform.World;
		var rot = Transform.Rotation;
		var oldEyeRot = Camera.Transform.Rotation;
		var newEyeRot = IsRagdolled ? eyes.Rotation : EyeAngles.ToRotation();
		var oldEyePos = Camera.Transform.Position;
		var newEyePos = eyes.Position + (IsRagdolled ? 0f : rot.Forward * 2.4f);

		Camera.Transform.Position = IsRagdolled ? Vector3.Lerp( oldEyePos, newEyePos, Time.Delta * 10f ) : newEyePos;
		Camera.Transform.Rotation = IsRagdolled ? Rotation.Lerp( oldEyeRot, newEyeRot, Time.Delta * 5f ) : newEyeRot;
		Camera.FieldOfView = MathX.LerpTo( Camera.FieldOfView, Input.Down( "view" ) ? Zoom : 90f, 10f * Time.Delta );
		Camera.ZNear = 2.5f;

		UpdateHeadVisibility();
	}

	public void UpdateHeadVisibility()
	{
		var eyes = Renderer.GetAttachment( "eyes" ) ?? Transform.World;
		var rot = Transform.Rotation;

		if ( HideHead )
			Renderer?.SceneModel?.SetBoneWorldTransform( 7, new Transform( eyes.Position + rot.Backward * 10, Rotation.Identity, 0 ) );

		// Hide face and head clothing.
		var face = (Inventory.EquippedItems?.ElementAtOrDefault( (int)EquipSlot.Face ) as ItemEquipment)?.Renderer?.SceneObject;
		if ( face != null ) face.RenderingEnabled = !HideHead;

		var head = (Inventory.EquippedItems?.ElementAtOrDefault( (int)EquipSlot.Head ) as ItemEquipment)?.Renderer?.SceneObject;
		if ( head != null ) head.RenderingEnabled = !HideHead;
	}

	protected void UpdateAnimation()
	{
		if ( Renderer == null || Renderer.SceneModel == null || MoveHelper == null )
			return;

		Renderer.Set( "grounded", MoveHelper.IsOnGround );
		Renderer.Set( "crouching", Ducking );

		var oldX = Renderer.GetFloat( "move_x" );
		var oldY = Renderer.GetFloat( "move_y" );
		var newX = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Forward ) / 140f;
		var newY = Vector3.Dot( MoveHelper.Velocity, Transform.Rotation.Right ) / 140f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Renderer.Set( "move_x", x );
		Renderer.Set( "move_y", y );

		Renderer.SceneModel.Morphs.Set( "fat", Fatness );
		Renderer.Set( "height", Height );

		Renderer.Set( "lookat", EyeAngles.WithYaw( 0 ).Forward );
	}

	private void OnJumpEvent( SceneModel.GenericEvent e )
	{
		if ( e.Type == "jump" )
			MoveHelper.Punch( Vector3.Up * JumpStrength );
	}

	[Broadcast]
	protected void JumpBroadcast()
	{
		Renderer?.Set( "jump", true );
	}
}
