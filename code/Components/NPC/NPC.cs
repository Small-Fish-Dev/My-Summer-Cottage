using Sandbox;
using Sauna;

public enum NPCType
{
	[Icon( "🐇" )]
	[Description( "Killed by low damage" )]
	Small,
	[Icon( "🧍" )]
	[Description( "Stunned by low damage, killed by medium damage" )]
	Medium,
	[Icon( "🦌" )]
	[Description( "Stunned by medium damage, killed by large damage" )]
	Large,
	[Icon( "👼" )]
	[Description( "Can't be killed" )]
	Immortal
}

[Icon( "smart_toy" )]
public class NPC : Component
{
	[Property]
	public MoveHelper MoveHelper { get; set; }
	[Property]
	public SkinnedModelRenderer Model { get; set; }

	[Property]
	[Title( "NPC Type" )]
	public NPCType NPCType { get; set; } = NPCType.Medium;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 600f, 10f )]
	public float WalkSpeed { get; set; } = 60f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 600f, 10f )]
	public float RunSpeed { get; set; } = 120f;

	[Property]
	[Category( "Stats" )]
	public bool FaceTowardsVelocity { get; set; } = true;

	public Vector3 TargetPosition { get; set; }
	public GameObject TargetObject { get; private set; } = null;
	public float DesiredDistance { get; private set; } = 60f;
	public float CurrentSpeed { get; private set; } = 60f;
	public bool IsOnGround { get; private set; } = true;
	public Vector3 Velocity { get; set; }

	protected override void DrawGizmos()
	{
	}

	protected override void OnStart()
	{
		Tags.Set( "npc", true );
		CurrentSpeed = WalkSpeed;

		if ( Model != null )
			Model.OnFootstepEvent += OnFootstep;
	}

	protected override void OnUpdate()
	{
		if ( Model == null ) return;
		if ( MoveHelper == null ) return;

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Forward ) / 100f;
		var newY = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Right ) / 100f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	protected override void OnFixedUpdate()
	{
		if ( MoveHelper == null ) return;

		CheckNewTargetPos();

		if ( Scene.GetAllComponents<Player>().FirstOrDefault() is Player player ) // TODO Remove
			SetTarget( player.GameObject );

		Velocity = MoveHelper.Velocity;

		if ( TargetObject.IsValid() )
		{
			if ( IsWithinRange( TargetObject, DesiredDistance ) )
			{
				var newRotation = Rotation.LookAt( TargetObject.Transform.Position.WithZ( 0f ) - Transform.Position.WithZ( 0f ) );
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, newRotation, Time.Delta * 5f );

				SetRagdoll( true, spin: 100f );
				WorldPunch( TargetObject.Transform.Position, 400f, 300f );
			}
		}

		MoveHelper.Move();

		if ( Ragdoll != null )
			FollowRagdoll();
	}

	void CheckNewTargetPos()
	{
		if ( TargetObject.IsValid() )
		{
			if ( !IsWithinRange( TargetObject, DesiredDistance ) ) // Has our target moved?
				MoveTo( GetPreferredTargetPosition( TargetObject ) );
		}
	}

	/// <summary>
	/// Make the NPC move to the target position
	/// </summary>
	/// <param name="targetPosition"></param>
	public void MoveTo( Vector3 targetPosition )
	{
		TargetPosition = targetPosition;
		//Agent.MoveTo( TargetPosition );
	}

	/// <summary>
	/// Who should the NPC follow, set null to go back to manually setting the target position
	/// </summary>
	/// <param name="target"></param>
	/// <param name="desiredDistance"></param>
	public void SetTarget( GameObject target, float desiredDistance = 60f )
	{
		if ( TargetObject == target )
		{
			SetDesiredDistance( desiredDistance );
			return;
		}

		TargetObject = target;
		SetDesiredDistance( desiredDistance );

		if ( TargetObject != null )
			MoveTo( GetPreferredTargetPosition( TargetObject ) );
	}


	/// <summary>
	/// How close to a target should the NPC get to
	/// </summary>
	/// <param name="desiredDistance"></param>
	public void SetDesiredDistance( float desiredDistance )
	{
		DesiredDistance = desiredDistance;
	}

	/// <summary>
	/// Is the object within the npc's reach
	/// </summary>
	/// <param name="target"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool IsWithinRange( GameObject target, float range = 50f )
	{
		if ( !GameObject.IsValid() ) return false;

		return target.Transform.Position.Distance( Transform.Position ) <= range;
	}

	/// <summary>
	/// Get a random position around the NPC (Horizonal)
	/// </summary>
	/// <param name="minRange"></param>
	/// <param name="maxRange"></param>
	/// <returns></returns>
	public Vector3 GetRandomPositionAround( float minRange = 50f, float maxRange = 300f )
	{
		var position = Transform.Position;
		var tries = 0;

		while ( tries <= 10 && position == Transform.Position )
		{
			var randomDirection = Rotation.FromYaw( Game.Random.Float( 360f ) ).Forward;
			var randomDistance = Game.Random.Float( minRange, maxRange );
			var randomPosition = position + randomDirection * randomDistance;

			var randomPoint = Scene.NavMesh?.GetClosestPoint( position );

			if ( randomPoint != null )
				position = randomPosition;

			tries++;
		}

		return position;
	}

	/// <summary>
	/// Set the speed of the NPC
	/// </summary>
	/// <param name="speed"></param>
	public void SetSpeed( float speed )
	{
		CurrentSpeed = speed;
	}

	/// <summary>
	/// Set the NPC's speed to the RunSpeed
	/// </summary>
	/// <param name="isRunning"></param>
	public void Running( bool isRunning = true )
	{
		SetSpeed( isRunning ? RunSpeed : WalkSpeed );
	}

	/// <summary>
	/// Set the NPC's speed to the WalkSpeed
	/// </summary>
	/// <param name="isWalking"></param>
	public void Walking( bool isWalking = true )
	{
		SetSpeed( isWalking ? WalkSpeed : RunSpeed );
	}

	/// <summary>
	/// Where the NPC should find themselves when near the target
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	public Vector3 GetPreferredTargetPosition( GameObject target )
	{
		if ( !target.IsValid() )
			return TargetPosition;

		var targetPosition = target.Transform.Position;

		var direction = (Transform.Position - targetPosition).Normal;
		var offset = direction * DesiredDistance;
		var position = targetPosition + offset;
		var closestObjectPoint = Scene.NavMesh?.GetClosestPoint( position );

		if ( closestObjectPoint != null )
			return closestObjectPoint.Value;
		else
			return position;
	}

	public ModelPhysics Ragdoll => Model.Components.Get<ModelPhysics>();
	SkinnedModelRenderer _puppet;
	bool _isTransitioning = false;
	[Sync] RealTimeUntil _unragdoll { get; set; }
	Vector3 _lastPosition = Vector3.Zero;
	float _spin = 0f;

	/// <summary>
	/// Set the ragdoll state of the NPC
	/// </summary>
	/// <param name="ragdoll">Ragdoll or Unragdoll</param>
	/// <param name="duration">How long ragdoll state lasts</param>
	/// <param name="spin">How fast it spins towards the given velocity</param>
	[Broadcast( NetPermission.Anyone )]
	public void SetRagdoll( bool ragdoll, float duration = 2f, float spin = 0f )
	{
		if ( ragdoll )
		{
			if ( Ragdoll == null && Model != null )
			{
				var newRagdoll = Model.Components.Create<ModelPhysics>();

				newRagdoll.Model = Model.Model;
				newRagdoll.Renderer = Model;

				newRagdoll.Enabled = false;
				newRagdoll.Enabled = true; // Gotta call OnEnabled for it to update :)

				newRagdoll.PhysicsGroup.Velocity = Velocity;

				_puppet = Model.GameObject.Parent.Components.Create<SkinnedModelRenderer>();
				_puppet.Model = Model.Model;
				_puppet.Enabled = false;
				_puppet.Enabled = true;
				_puppet.SceneModel.RenderingEnabled = false;

				foreach ( var collider in Components.GetAll<Collider>( FindMode.EverythingInSelfAndChildren ) )
					collider.Enabled = false;
			}

			_unragdoll = duration;
			_lastPosition = Ragdoll.Transform.World.Position;
			_spin = spin;
		}
		else
		{
			_unragdoll = 0f;
		}
	}

	/// <summary>
	/// Send the NPC flying like an explosion
	/// </summary>
	/// <param name="force"></param>
	public void LocalPunch( Vector3 force )
	{
		if ( Ragdoll != null )
			Ragdoll.PhysicsGroup.Velocity = force;

		if ( MoveHelper != null && MoveHelper.Enabled )
			MoveHelper.Punch( force );
	}

	/// <summary>
	/// Send the NPC flying towards like an explosion coming from source
	/// </summary>
	/// <param name="source"></param>
	/// <param name="force"></param>
	/// <param name="extraHeight"></param>
	public void WorldPunch( Vector3 source, float force, float extraHeight = 50f )
	{
		var direction = (Transform.Position - source).Normal;
		var appliedForce = direction * force + Vector3.Up * extraHeight;

		LocalPunch( appliedForce );
	}

	void FollowRagdoll()
	{
		if ( Ragdoll == null ) return;

		var rootPosition = Transform.Position;
		Velocity = Ragdoll.PhysicsGroup.Bodies.FirstOrDefault()?.Velocity ?? (MoveHelper?.Velocity ?? Vector3.Zero);

		if ( Model.GetAttachment( "foot_L" ) != null )
		{
			var leftFoot = Model.GetAttachment( "foot_L" ).Value.Position;
			var rightFoot = Model.GetAttachment( "foot_R" ).Value.Position;
			rootPosition = (leftFoot + rightFoot) / 2f;
		}

		if ( !_isTransitioning )
		{
			Transform.Position = rootPosition; // Remember to set before Renderer!
			Model.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset
		}

		var velocity = (Ragdoll.Transform.World.Position - _lastPosition);
		_lastPosition = Ragdoll.Transform.World.Position;

		if ( velocity.Length >= 10f && _unragdoll.Passed <= 0.2f )
		{
			var horizontalDirection = velocity.WithZ( 0f ).Normal;
			var rotatedDirection = horizontalDirection.RotateAround( 0f, Rotation.FromYaw( 90f ) );

			Ragdoll.PhysicsGroup.AngularVelocity = rotatedDirection * _spin;
		}

		if ( _unragdoll )
		{
			if ( !_isTransitioning )
			{
				_unragdoll = 0f;

				var groundTrace = Scene.Trace.Ray( rootPosition, rootPosition + Vector3.Down * 10f )
					.Size( 20f )
					.IgnoreGameObjectHierarchy( GameObject )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				if ( groundTrace.Hit )
					_isTransitioning = true;
			}
			else
			{
				var transition = 0.15f;

				var bones = _puppet.Model.Bones.AllBones;
				Dictionary<PhysicsBody, Transform> bodyTransforms = new();

				foreach ( var bone in bones )
				{
					var body = Ragdoll.PhysicsGroup.GetBody( bone.Index );

					if ( body != null )
						bodyTransforms.Add( body, body.Transform );
				}

				if ( _unragdoll.Passed <= transition )
				{
					if ( Components.TryGet<Character>( out var character, FindMode.EnabledInSelfAndChildren ) )
					{
						_puppet.SceneModel.Morphs.Set( "fat", character.Fatness );
						_puppet.Set( "height", character.Height );
					}

					var time = _unragdoll.Passed / transition;

					foreach ( var bone in bones )
					{
						if ( _puppet.TryGetBoneTransform( in bone, out var transform ) )
						{
							var body = Ragdoll.PhysicsGroup.GetBody( bone.Index );

							if ( body != null )
							{
								body.MotionEnabled = false;
								body.GravityEnabled = false;
								body.EnableSolidCollisions = false;
								body.LinearDrag = 9999f;
								body.AngularDrag = 9999f;

								var oldTransform = bodyTransforms[body];
								var newPos = oldTransform.Position.LerpTo( transform.Position, (float)time );
								var newRot = Rotation.Lerp( oldTransform.Rotation, transform.Rotation, (float)time );

								body.Position = newPos;
								body.Rotation = newRot;
							}
						}
					}
				}
				else
				{
					_puppet.Destroy();
					Ragdoll?.Destroy();

					Model.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Model goes offset

					foreach ( var clothing in Model.GameObject.Children )
						clothing.Transform.Local = new Transform( Vector3.Zero, Rotation.Identity ); // Clothing go offset too

					_isTransitioning = false;
				}
			}
		}
	}


	private TimeSince lastStepped;
	private void OnFootstep( SceneModel.FootstepEvent e )
	{
		if ( lastStepped < 0.2f )
			return;

		var pos = Transform.Position;
		var tr = Scene.Trace.Ray( pos + Vector3.Up * 10, pos + Vector3.Down * 10 )
			.Radius( 1 )
			.WithoutTags( "trigger" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !tr.Hit || tr.Surface == null )
			return;

		lastStepped = 0;

		var path = e.FootId == 0
			? tr.Surface.Sounds.FootLeft
			: tr.Surface.Sounds.FootRight;

		if ( string.IsNullOrEmpty( path ) )
			return;

		var sound = Sound.Play( path, tr.HitPosition + tr.Normal * 5 );
		sound.Volume *= e.Volume;
	}
}
