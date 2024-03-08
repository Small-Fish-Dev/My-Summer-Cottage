using System;
using System.ComponentModel.Design;

public enum TraceType
{
	[Icon( "crop_portrait" )]
	BBox,
	[Icon( "mouse" )]
	Capsule,
}

[Icon( "nordic_walking" )]
public class MoveHelper : Component
{
	[Property]
	[Category( "Collider" )]
	public bool UseCollider { get; set; } = false;

	[Property]
	[Category( "Collider" )]
	[ShowIf( "UseCollider", true )]
	public Collider Collider { get; set; }

	[Property]
	[Category( "Collider" )]
	[HideIf( "UseCollider", true )]
	public TraceType TraceType { get; set; } = TraceType.BBox;

	/// <summary>
	/// Radius of our trace
	/// </summary>
	[Property]
	[Category( "Collider" )]
	[HideIf( "UseCollider", true )]
	[Range( 0f, 64f, 1f, true, true )]
	public float TraceRadius { get; set; } = 16f;

	/// <summary>
	/// Height of our trace
	/// </summary>
	[Property]
	[Category( "Collider" )]
	[HideIf( "UseCollider", true )]
	[Range( 0f, 200f, 1f, true, true )]
	public float TraceHeight { get; set; } = 72f;

	/// <summary>
	/// How high steps can be for you to climb on
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 64f, 1f, true, true )]
	public float StepHeight { get; set; } = 18f;

	/// <summary>
	/// How steep terrain can be for you to climb on
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 89f, 1f, true, true )]
	public float GroundAngle { get; set; } = 45f;

	/// <summary>
	/// How much you bounce off of walls when colliding against them
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 5f, 0.1f, true, true )]
	public float Bounce { get; set; } = 0.3f;

	/// <summary>
	/// If we want our controller to naturally stick to ground
	/// </summary>
	[Property]
	[Category( "Values" )]
	public bool StickToGround { get; set; } = true;

	/// <summary>
	/// How fast you accelerate on the ground
	/// </summary>
	[Property]
	[Category( "Values" )]
	[ShowIf( "StickToGround", true )]
	[Range( 0f, 3000f, 5f, true, true )]
	public float GroundAcceleration { get; set; } = 2000f;

	/// <summary>
	/// How fast you decelerate on the ground (Exponential)
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 100f, 0.1f, true, true )]
	public float GroundFriction { get; set; } = 30f;

	/// <summary>
	/// How fast you accelerate in the air
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 1500f, 5f, true, true )]
	public float AirAcceleration { get; set; } = 800f;

	/// <summary>
	/// How fast you decelerate in the air (Exponential)
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 100f, 0.1f, true, true )]
	public float AirFriction { get; set; } = 1f;

	/// <summary>
	/// How fast you accelerate based on the speed/wish-speed ratio.
	/// </summary>
	[Property]
	[Category( "Values" )]
	public Curve AccelerationCurve { get; set; }

	/// <summary>
	/// Stops you if you're under this speed
	/// </summary>
	[Property]
	[Category( "Values" )]
	[Range( 0f, 50f, 1f, true, true )]
	public float StopSpeed { get; set; } = 10f;

	/// <summary>
	/// Use the scene's gravity or our own
	/// </summary>
	[Property]
	[Category( "Values" )]
	public bool UseSceneGravity { get; set; } = true;

	[Property]
	[Category( "Values" )]
	[HideIf( "UseSceneGravity", true )]
	public Vector3 Gravity { get; set; } = new Vector3( 0f, 0f, 850f );

	[Property]
	[Category( "Values" )]
	public bool EnableUnstuck { get; set; } = true;

	[Property]
	[Category( "Values" )]
	[ShowIf( "EnableUnstuck", true )]
	[Range( 1, 100, 1, true, true )]
	public int MaxUnstuckTries { get; set; } = 20;

	/// <summary>
	/// Which tags it should ignore
	/// </summary>
	[Property]
	[Category( "Tags" )]
	public TagSet IgnoreTags { get; set; } = new TagSet();


	public BBox CollisionBBox;
	public Capsule CollisionCapsule;

	public Vector3 InitialCameraPosition { get; private set; }
	[Sync] public Angles EyeAngles { get; private set; }
	[Sync] public Vector3 WishVelocity { get; set; }
	[Sync] public Vector3 Velocity { get; set; }
	[Sync] public bool IsOnGround { get; set; }
	public bool IsCapsuleCollider => UseCollider ? Collider is CapsuleCollider : TraceType == TraceType.Capsule;
	private int _stuckTries;

	protected override void DrawGizmos()
	{
		Gizmo.GizmoDraw draw = Gizmo.Draw;

		if ( TraceType == TraceType.Capsule )
			draw.LineCapsule( DefineCapsule() );
		else
			draw.LineBBox( DefineBBox() );
	}

	private SceneTrace BuildTrace( Vector3 from, Vector3 to )
	{
		return BuildTrace( base.Scene.Trace.Ray( in from, in to ) );
	}

	private SceneTrace BuildTrace( SceneTrace source )
	{
		if ( IsCapsuleCollider )
		{
			return source.Capsule( CollisionCapsule )
				.WithoutTags( IgnoreTags )
				.IgnoreGameObjectHierarchy( GameObject );
		}
		else
		{
			BBox hull = CollisionBBox;
			return source.Size( in hull )
				.WithoutTags( IgnoreTags )
				.IgnoreGameObjectHierarchy( GameObject );
		}
	}

	private void Move( bool step )
	{
		if ( step && IsOnGround )
			Velocity = Velocity.WithZ( 0f );

		if ( Velocity.IsNearlyZero( 0.001f ) )
		{
			Velocity = Vector3.Zero;
			return;
		}

		Vector3 position = base.GameObject.Transform.Position;
		CharacterControllerHelper characterControllerHelper = new CharacterControllerHelper( BuildTrace( position, position ), position, Velocity );
		characterControllerHelper.Bounce = Bounce;
		characterControllerHelper.MaxStandableAngle = GroundAngle;

		if ( step && IsOnGround )
			characterControllerHelper.TryMoveWithStep( Time.Delta, StepHeight );
		else
			characterControllerHelper.TryMove( Time.Delta ); // TODO: Maybe this fucks platforming up?

		base.Transform.Position = characterControllerHelper.Position;
		Velocity = characterControllerHelper.Velocity;
	}

	private void CategorizePosition()
	{
		Vector3 position = base.Transform.Position;
		Vector3 to = position + Vector3.Down * 2f;
		Vector3 from = position;
		bool isOnGround = IsOnGround;

		if ( !IsOnGround && Velocity.z > 50f )
		{
			IsOnGround = false;
			return;
		}

		to.z -= (isOnGround ? StepHeight : 0.1f);
		var physicsTraceResult = BuildTrace( from, to ).Run();

		if ( !physicsTraceResult.Hit || Vector3.GetAngle( Vector3.Up, physicsTraceResult.Normal ) > GroundAngle )
		{
			IsOnGround = false;
			return;
		}

		IsOnGround = true;

		if ( StickToGround )
			if ( isOnGround && !physicsTraceResult.StartedSolid && physicsTraceResult.Fraction > 0f && physicsTraceResult.Fraction < 1f )
				base.Transform.Position = physicsTraceResult.EndPosition + physicsTraceResult.Normal * 0.01f;
	}

	//
	// Summary:
	//     Disconnect from ground and punch our velocity. This is useful if you want the
	//     player to jump or something.
	public void Punch( in Vector3 amount )
	{
		IsOnGround = false;
		Velocity += amount;
	}

	//
	// Summary:
	//     Move a character, with this velocity
	public void Move()
	{
		var wishSpeed = WishVelocity.Length;
		var horizontalSpeed = Velocity.WithZ( 0f ).Length;

		if ( wishSpeed >= horizontalSpeed ) // Accelerating
		{
			var wishAcceleration = IsOnGround ? GroundAcceleration : AirAcceleration;
			var acceleration = wishAcceleration * AccelerationCurve.Evaluate( Time.Delta + horizontalSpeed / wishSpeed );

			Velocity += WishVelocity.WithZ( 0 ).Normal * acceleration * Time.Delta;

		}
		else // Decelerating
		{
			var wishDeceleration = IsOnGround ? GroundFriction : AirFriction;
			var newVelocity = Velocity / (1f + wishDeceleration * Time.Delta);

			if ( newVelocity.Length <= StopSpeed )
				Velocity = new Vector3( 0f, 0f, Velocity.z );
			else
				Velocity = newVelocity.WithZ( Velocity.z );
		}

		if ( IsOnGround )
		{
			Velocity = Velocity.WithZ( 0f );
		}
		else
		{
			var gravity = UseSceneGravity ? Scene.PhysicsWorld.Gravity : Gravity;
			Velocity += gravity * Time.Delta; // Apply the scene's gravity to the controller
		}

		if ( !EnableUnstuck || !TryUnstuck() )
		{
			if ( IsOnGround )
				Move( step: true );
			else
				Move( step: false );

			CategorizePosition();
		}
	}

	//
	// Summary:
	//     Move from our current position to this target position, but using tracing an
	//     sliding. This is good for different control modes like ladders and stuff.
	public void MoveTo( Vector3 targetPosition, bool useStep )
	{
		if ( !EnableUnstuck || !TryUnstuck() )
		{
			Vector3 position = base.Transform.Position;
			Vector3 velocity = targetPosition - position;
			CharacterControllerHelper characterControllerHelper = new CharacterControllerHelper( BuildTrace( position, position ), position, velocity );
			characterControllerHelper.MaxStandableAngle = GroundAngle;

			if ( useStep )
				characterControllerHelper.TryMoveWithStep( 1f, StepHeight );
			else
				characterControllerHelper.TryMove( 1f );

			base.Transform.Position = characterControllerHelper.Position;
		}
	}

	private bool TryUnstuck()
	{
		if ( !BuildTrace( base.Transform.Position, base.Transform.Position ).Run().StartedSolid )
		{
			_stuckTries = 0;
			return false;
		}

		int num = MaxUnstuckTries;
		for ( int i = 0; i < num; i++ )
		{
			Vector3 vector = base.Transform.Position + Vector3.Random.Normal * ((float)_stuckTries / 2f);
			if ( i == 0 )
				vector = base.Transform.Position + Vector3.Up * 2f;

			if ( !BuildTrace( vector, vector ).Run().StartedSolid )
			{
				base.Transform.Position = vector;
				return false;
			}
		}

		_stuckTries++;
		return true;
	}

	public BBox DefineBBox()
	{
		if ( !UseCollider || Collider == null || Collider is not BoxCollider box )
			return new BBox( new Vector3( 0f - TraceRadius, 0f - TraceRadius, 0f ), new Vector3( TraceRadius, TraceRadius, TraceHeight ) );
		else
			return new BBox( box.Center - box.Scale / 2f, box.Center + box.Scale / 2f );
	}

	public Capsule DefineCapsule()
	{
		if ( !UseCollider || Collider == null || Collider is not CapsuleCollider capsule )
			return new Capsule( Vector3.Up * TraceRadius, Vector3.Up * (TraceHeight - TraceRadius), TraceRadius );
		else
			return new Capsule( capsule.Start, capsule.End, capsule.Radius );
	}

	protected override void OnStart() // Called as soon as the component gets enabled
	{
		base.OnStart();

		CollisionBBox = DefineBBox();
		CollisionCapsule = DefineCapsule();
	}
}
