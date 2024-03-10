using Sandbox;
using Sauna;
using System.Threading;

public enum WeightType
{
	[Icon( "🐀" )]
	Feather,
	[Icon( "🐇" )]
	Light,
	[Icon( "🚶" )]
	Middle,
	[Icon( "🦌" )]
	Heavy,
	[Icon( "🐘" )]
	Massive
}

[Icon( "smart_toy" )]
public partial class NPC : Component
{
	[Property]
	public string Name { get; set; } = "Default";

	[Property]
	public MoveHelper MoveHelper { get; set; }

	[Property]
	public SkinnedModelRenderer Model { get; set; }

	[Property]
	public HealthComponent Health { get; set; }

	[Property]
	public NavigationType NavigationType { get; set; } = NavigationType.Normal;

	/// <summary>
	/// How much this creature weights (To handle ragdol force amount and duration)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public WeightType Weight { get; set; } = WeightType.Middle;

	/// <summary>
	/// Should the stats scale linearly with the scale of the object
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public bool ScaleStats { get; set; } = true;

	/// <summary>
	/// Doesn't move (Don't add a MoveHelper if this is on)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public bool Static { get; set; } = false;

	/// <summary>
	/// For animations. How many units per second the run animation is tuned to (This is automatically scaled by the scale)
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	public float MaxRunAnimationSpeed { get; set; } = 150f;

	/// <summary>
	/// How fast this NPC can walk
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	[Range( 0f, 600f, 10f )]
	public float WalkSpeed { get; set; } = 90f;

	/// <summary>
	/// How fast this NPC can run
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	[Range( 0f, 600f, 10f )]
	public float RunSpeed { get; set; } = 180f;

	/// <summary>
	/// Should we automatically make the NPC look towards its target
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	public bool FaceTowardsVelocity { get; set; } = true;

	/// <summary>
	/// How far the NPC can reach for attacks or navigation
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 30f, 200f, 10f )]
	public float Range { get; private set; } = 60f;
	public float Scale => ScaleStats ? MathF.Max( MathF.Max( GameObject.Transform.Scale.x, GameObject.Transform.Scale.y ), GameObject.Transform.Scale.z ) : 1f;

	/// <summary>
	/// If a GameObject has one of these tags it will be considered a provoker and can trigger alert state
	/// </summary>
	[Property]
	[Category( "TriggerStats" )]
	public TagSet ProvokerTags { get; set; }

	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnSpawn { get; set; }

	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnIdle { get; set; }

	/// <summary>
	/// How close a provoker has to come to alert the NPC
	/// </summary>
	[Property]
	[Category( "TriggerStats" )]
	[Range( 0f, 1024f, 16f )]
	public float AlertRange { get; set; } = 256f;

	/// <summary>
	/// How far a provoker has to go to lose the NPC
	/// </summary>
	[Property]
	[Category( "TriggerStats" )]
	[Range( 0f, 2024f, 16f )]
	public float AlertEscapeRange { get; set; } = 512f;

	public delegate void NpcTrigger( NPC npc, GameObject provoker );

	/// <summary>
	/// When the provoker enters the alert area, or a nearby NPC gets alerted/attacked. This won't get called if the NPC is already alerted.
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnAlerted { get; set; }

	/// <summary>
	/// When the NPC gets directly attacked. This won't get called if the NPC has been attacked already and is in alerted status.
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnAttacked { get; set; }

	/// <summary>
	/// When the provoker gets out of the AlertRange or dies
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnProvokerEscaped { get; set; }

	/// <summary>
	/// When the provoker is within range
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnProvokerWithinRange { get; set; }

	/// <summary>
	/// When the NPC dies, provoker will be NULL if it died of natural causes (???)
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnKilled { get; set; }

	/// <summary>
	/// When the NPC gets destroyed
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public Action OnDestroyed { get; set; }

	public int NpcId { get; set; }
	public Vector3 TargetPosition { get; set; }
	public GameObject TargetObject { get; private set; } = null;
	public bool FollowingTargetObject { get; set; } = false;

	public enum StateType
	{
		Spawn,
		Idle,
		Alerted,
		Attacked,
		ProvokerEscaped,
		ProvokerWithinRange,
		Killed
	}

	public NpcTrigger ToDelegate( StateType type )
	{
		return type switch
		{
			StateType.Spawn => OnSpawn,
			StateType.Idle => OnIdle,
			StateType.Alerted => OnAlerted,
			StateType.Attacked => OnAttacked,
			StateType.ProvokerEscaped => OnProvokerEscaped,
			StateType.ProvokerWithinRange => OnProvokerWithinRange,
			StateType.Killed => OnKilled,
			_ => OnIdle
		};
	}

	public StateType CurrentState { get; set; } = StateType.Spawn;
	public Vector3 SpawnPosition { get; set; }
	public float ForceMultiplier
	{
		get
		{
			return Weight switch
			{
				WeightType.Feather => 2f,
				WeightType.Light => 1.5f,
				WeightType.Middle => 1f,
				WeightType.Heavy => 0.75f,
				WeightType.Massive => 0.5f,
				_ => 1f
			};
		}
	}

	protected override void DrawGizmos()
	{
	}

	protected override void OnStart()
	{
		Tags.Set( "npc", true );

		if ( Model != null )
			Model.OnFootstepEvent += OnFootstep;

		NpcId = Scene.GetAllComponents<NPC>().OrderByDescending( x => x.NpcId ).First().NpcId + 1;

		if ( MoveHelper != null )
			MoveHelper.AirFriction = 100f;
	}

	protected override void OnAwake()
	{
		var spawnTrace = Scene.Trace.Ray( Transform.Position + Vector3.Up * 30f, Transform.Position - Vector3.Up * 200f )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "npc", "trigger" )
			.Run();

		SpawnPosition = spawnTrace.Hit ? spawnTrace.HitPosition : Transform.Position;

		if ( MoveHelper != null )
		{
			MoveHelper.StepHeight *= Scale;
			MoveHelper.TraceRadius *= Scale;
			MoveHelper.TraceHeight *= Scale;
			MoveHelper.StopSpeed *= Scale;
		}

		OnSpawn?.Invoke( this, null );
	}

	protected override void OnUpdate()
	{
		if ( Model == null ) return;
		if ( MoveHelper == null ) return;
		if ( Static ) return;
		if ( Health != null && !Health.Alive ) return;

		if ( Ragdoll == null && FaceTowardsVelocity )
			if ( !MoveHelper.Velocity.IsNearlyZero( 1f ) )
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, Rotation.LookAt( MoveHelper.Velocity.WithZ( 0f ), Vector3.Up ), Time.Delta * 10f );

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var scaledSpeed = MaxRunAnimationSpeed * Scale; // Model is scaled uniformally by the max value on the scale it seems

		var newX = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Forward ) / scaledSpeed;
		var newY = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Right ) / scaledSpeed;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	protected override void OnFixedUpdate()
	{
		if ( MoveHelper == null ) return;

		if ( Health != null && Health.Alive )
		{
			if ( Ragdoll == null )
			{
				ComputeNavigation();
				MoveHelper.Move();

				DetectAround();
			}
		}
		else
		{
			MoveHelper.WishVelocity = 0;
		}

		if ( Ragdoll != null )
			FollowRagdoll();
	}

	protected override void OnDestroy()
	{
		OnDestroyed?.Invoke();
	}

	public void SetState( StateType state, StateType currentState, bool invoke = true )
	{
		if ( CurrentState != currentState ) return;

		if ( invoke )
			ToDelegate( state )?.Invoke( this, TargetObject );

		CurrentState = state;
	}

	/// <summary>
	/// Get all provokers inside of its detect area
	/// </summary>
	public IEnumerable<GameObject> ProvokersInArea { get; set; }

	TimeUntil _nextDetect = 5f;

	public void DetectAround()
	{
		if ( TargetObject != null )
		{
			if ( TargetObject.Transform.Position.Distance( Transform.Position ) <= Range )
			{
				if ( _nextDetect )
				{
					SetState( StateType.ProvokerWithinRange, CurrentState );
					_nextDetect = 5f;
				}
			}
		}

		var currentTick = (int)(Time.Now / Time.Delta);
		if ( currentTick % 30 != NpcId % 30 ) return; // Check every 30 ticks

		var foundAround = Scene.FindInPhysics( new Sphere( Transform.Position, AlertRange ) )
			.Where( x => ProvokerTags != null && x.Tags.HasAny( ProvokerTags ) )
			.Where( x => x.Components.Get<HealthComponent>()?.Alive ?? true );

		if ( TargetObject == null )
		{
			if ( foundAround.Any() )
			{
				Detected( foundAround.First(), true );
			}
		}
		else
		{
			var targetDead = !TargetObject.Components.Get<HealthComponent>()?.Alive ?? false;
			var targetEscaped = TargetObject.Transform.Position.Distance( Transform.Position ) > AlertEscapeRange;
			if ( targetEscaped || targetDead )
				Undetected();
		}
	}

	public void Detected( GameObject target, bool alertOthers = false )
	{
		TargetObject = target;
		SetState( StateType.Alerted, CurrentState );

		if ( alertOthers )
		{
			var otherNpcs = Scene.GetAllComponents<NPC>()
				.Where( x => x.Transform.Position.Distance( Transform.Position ) <= x.AlertEscapeRange )
				.Where( x => x.Health?.Alive ?? false )
				.Where( x => x.TargetObject == null );

			foreach ( var npc in otherNpcs )
				npc.Detected( target, false );
		}
	}

	public void Undetected()
	{
		SetState( StateType.ProvokerEscaped, CurrentState );
		TargetObject = null;
		TargetPosition = Transform.Position;
		ReachedDestination = true;
	}

	/// <summary>
	/// Who should the NPC follow, set null to go back to manually setting the target position
	/// </summary>
	/// <param name="target"></param>
	public void SetTarget( GameObject target )
	{
		if ( target == null )
		{
			TargetObject = null;
			FollowingTargetObject = false;
			ReachedDestination = true;
			TargetPosition = Transform.Position;
		}

		if ( TargetObject != null )
		{
			TargetObject = target;
			MoveTo( GetPreferredTargetPosition( TargetObject ) );
			FollowingTargetObject = true;
			ReachedDestination = false;
		}
	}

	/// <summary>
	/// Is the object within the npc's reach (Range) 
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	public bool IsWithinRange( GameObject target )
	{
		if ( !GameObject.IsValid() ) return false;

		return target.Transform.Position.Distance( Transform.Position ) <= Range;
	}

	/// <summary>
	/// Is the object within the npc's range
	/// </summary>
	/// <param name="target"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool IsWithinRange( GameObject target, float range = 60f )
	{
		if ( !GameObject.IsValid() ) return false;

		return target.Transform.Position.Distance( Transform.Position ) <= range;
	}

	/// <summary>
	/// Get a random position around the position (Horizonal)
	/// </summary>
	/// <param name="position"></param>
	/// <param name="minRange"></param>
	/// <param name="maxRange"></param>
	/// <returns></returns>
	public static Vector3 GetRandomPositionAround( Vector3 position, float minRange = 50f, float maxRange = 300f )
	{
		var tries = 0;
		var hitGround = false;
		var hitPosition = position;

		while ( hitGround == false && tries <= 10f )
		{
			var randomDirection = Rotation.FromYaw( Game.Random.Float( 360f ) ).Forward;
			var randomDistance = Game.Random.Float( minRange, maxRange );
			var randomPoint = position + randomDirection * randomDistance;

			var groundTrace = Game.ActiveScene.Trace.Ray( randomPoint + Vector3.Up * 64f, randomPoint - Vector3.Up * 64f )
				.Size( 5f )
				.WithoutTags( "player", "npc", "trigger" )
				.Run();

			if ( groundTrace.Hit )
			{
				hitGround = true;
				hitPosition = groundTrace.HitPosition;
			}

			tries++;
		}

		return hitPosition;
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
		var offset = direction * Range / 2f;
		var wishPos = targetPosition + offset;

		var groundTrace = Scene.Trace.Ray( wishPos + Vector3.Up * 64f, wishPos - Vector3.Up * 64f )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "npc", "trigger" )
			.Run();

		return groundTrace.Hit ? groundTrace.HitPosition : wishPos;
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
			MoveHelper.Punch( force * ForceMultiplier );
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

	private TimeSince lastStepped;
	private void OnFootstep( SceneModel.FootstepEvent e )
	{
		if ( Health != null && !Health.Alive ) return;

		if ( lastStepped < 0.2f )
			return;

		var pos = Transform.Position;
		var tr = Scene.Trace.Ray( pos + Vector3.Up * 10f, pos + Vector3.Down * 10f )
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
		sound.Volume *= ForceMultiplier * 4f;
		sound.Decibels += ForceMultiplier;
	}
}
