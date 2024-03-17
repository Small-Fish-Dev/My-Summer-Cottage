using Sandbox;
using Sandbox.Citizen;
using Sandbox.ModelEditor;
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
	public NavigationType WalkingType { get; set; } = NavigationType.Dumb;

	[Property]
	public NavigationType RunningType { get; set; } = NavigationType.Smart;

	public NavigationType NavigationType => IsRunning ? RunningType : WalkingType;

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
	public float Scale => ScaleStats ? MathF.Max( MathF.Max( GameObject.Transform.Scale.x, GameObject.Transform.Scale.y ), GameObject.Transform.Scale.z ) : 1f;

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
	[Range( 0f, 600f, 10f, false )]
	public float WalkSpeed { get; set; } = 90f;

	/// <summary>
	/// How fast this NPC can run
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	[Range( 0f, 600f, 10f, false )]
	public float RunSpeed { get; set; } = 180f;

	/// <summary>
	/// Should we automatically make the NPC look towards its target
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[HideIf( "Static", true )]
	public bool FaceTowardsVelocity { get; set; } = true;

	/// <summary>
	/// How far the NPC can reach to attack with the target
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 30f, 200f, 10f, false )]
	public float AttackRange { get; private set; } = 80f;

	/// <summary>
	/// How many seconds must pass before the NPC can attack with the target again
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0.5f, 10f, 0.1f, false )]
	public float AttackCooldown { get; private set; } = 5f;

	/// <summary>
	/// If a GameObject has one of these tags it will be considered an enemy
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public TagSet EnemyTags { get; set; }

	/// <summary>
	/// How far away the NPC can detect an enemy
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 1024f, 16f, false )]
	public float DetectRange { get; set; } = 256f;

	/// <summary>
	/// How far away the NPC can see the enemy before losing sight
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[Range( 0f, 2024f, 16f, false )]
	public float VisionRange { get; set; } = 512f;

	/// <summary>
	/// When detecting an enemy, alerts everyone closeby as well
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public bool AlertOthers { get; set; } = true;

	/// <summary>
	/// When the NPC has no target it will sometimes fire off the idle event
	/// </summary>
	[Property]
	[Category( "Stats" )]
	public bool Idle { get; set; } = true;

	/// <summary>
	/// Maximum time in between idle events being called when there is no target
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[ShowIf( "Idle", true )]
	[Range( 0.1f, 10f, 0.1f, false )]
	public float MinimumIdleCooldown { get; set; } = 4;

	/// <summary>
	/// Minimum time in between idle events being called when there is no target
	/// </summary>
	[Property]
	[Category( "Stats" )]
	[ShowIf( "Idle", true )]
	[Range( 0.1f, 20f, 0.1f, false )]
	public float MaximumIdleCooldown { get; set; } = 6;

	/// <summary>
	/// When the NPC is first spawned in
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public Action OnSpawn { get; set; }

	public delegate void NpcTrigger( GameObject enemy );

	/// <summary>
	/// When an enemy enters the detect area or attacks the NPC, or a nearby NPC gets alerted. This won't get called if the NPC has been alerted already.
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnDetect { get; set; }

	/// <summary>
	/// When the enemy is within attack range and our cooldown is up
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnAttack { get; set; }

	/// <summary>
	/// When the enemy gets out of the Vision Range or dies
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnEnemyEscaped { get; set; }

	/// <summary>
	/// When the NPC dies, enemy will be NULL if it died of natural causes (???)
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnKilled { get; set; }

	/// <summary>
	/// When the NPC has no target it will occasionally fire this off
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	[ShowIf( "Idle", true )]
	public Action OnIdle { get; set; }

	[HostSync] public int NpcId { get; set; }
	[HostSync] public Vector3 TargetPosition { get; set; }
	[HostSync] public bool FollowingTargetObject { get; set; } = false;
	[HostSync] public Vector3 SpawnPosition { get; set; }
	[HostSync] public TimeUntil NextIdle { get; set; }
	[HostSync] public TimeUntil NextAttack { get; set; }

	public GameObject TargetObject { get; private set; } = null;

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

		BroadcastOnSpawn();
	}

	[Broadcast]
	private void BroadcastOnSpawn()
	{
		OnSpawn?.Invoke();
	}

	protected override void OnUpdate()
	{
		if ( Model == null ) return;
		if ( MoveHelper == null ) return;
		if ( Static ) return;
		if ( Health != null && !Health.Alive ) return;

		if ( Ragdoll == null && FaceTowardsVelocity )
			if ( !MoveHelper.Velocity.IsNearlyZero( 1f ) )
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, Rotation.LookAt( MoveHelper.Velocity.WithZ( 0f ), Vector3.Up ), Time.Delta * (IsRunning ? 10f : 5f) );

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
		if ( Health != null && Health.Alive ) // If we are still alive
		{
			if ( Ragdoll == null ) // If we are not ragdolled
			{
				if ( TargetObject == null )
				{
					if ( Idle && NextIdle )
					{
						BroadcastOnIdle();
						NextIdle = Game.Random.Float( MinimumIdleCooldown, MaximumIdleCooldown );
					}
				}

				if ( MoveHelper == null ) return;
				{
					if ( !Static )
					{
						ComputeNavigation();
						MoveHelper.Move();
					}
				}

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

	[Broadcast]
	private void BroadcastOnIdle()
	{
		OnIdle?.Invoke();
	}

	protected override void OnDestroy()
	{
	}

	/// <summary>
	/// Get all provokers inside of its detect area
	/// </summary>
	public IEnumerable<GameObject> ProvokersInArea { get; set; }

	public void DetectAround()
	{
		if ( TargetObject != null )
		{
			if ( IsWithinRange( TargetObject ) ) // Is the target within reach
			{
				if ( NextAttack )
				{
					BroadcastOnAttack();
					NextAttack = AttackCooldown;
				}
			}
		}

		var currentTick = (int)(Time.Now / Time.Delta);
		if ( currentTick % 20 != NpcId % 20 ) return; // Check every 20 ticks

		var foundAround = Scene.FindInPhysics( new Sphere( Transform.Position, DetectRange * Scale ) ) // Find gameobjects nearby
			.Where( x => x.Enabled )
			.Where( x => EnemyTags != null && x.Tags.HasAny( EnemyTags ) ) // Do they have any of our enemy tags
			.Where( x => x.Components.Get<HealthComponent>()?.Alive ?? true ); // Are they dead or undead

		if ( TargetObject == null )
		{
			if ( foundAround.Any() )
				Detected( foundAround.First(), true ); // If we don't have any target yet, pick the first one around us
		}
		else
		{
			var targetDead = !TargetObject.Components.Get<HealthComponent>()?.Alive ?? false; // Is our target dead or undead
			var targetEscaped = TargetObject.Transform.Position.Distance( Transform.Position ) > VisionRange * Scale; // Did our target get out of vision range

			if ( targetEscaped || targetDead ) // Did our target die or escape
				Undetected();
		}
	}

	[Broadcast]
	private void BroadcastOnAttack()
	{
		if ( TargetObject is not null )
			OnAttack?.Invoke( TargetObject );
	}

	public void Detected( GameObject target, bool alertOthers = false )
	{
		if ( target == null ) return;
		target = target.Parent == null || target.Parent == Scene ? target : target.Parent;
		if ( target == TargetObject ) return;

		SetTarget( target );
		BroadcastOnDetect();

		if ( alertOthers && AlertOthers )
		{
			var otherNpcs = Scene.GetAllComponents<NPC>()
				.Where( x => x.Transform.Position.Distance( Transform.Position ) <= x.VisionRange * x.Scale ) // Find all nearby NPCs
				.Where( x => x.Health?.Alive ?? true ) // Dead or undead
				.Where( x => x.TargetObject == null ) // They don't have a target already
				.Where( x => x != this ) // Not us
				.Where( x => x.GameObject != null ) // And that don't have a target already
				.Where( x => !x.EnemyTags.HasAny( Tags ) ); // And we are friends

			foreach ( var npc in otherNpcs )
				npc.Detected( target, false );
		}
	}

	[Broadcast]
	private void BroadcastOnDetect()
	{
		if ( TargetObject is not null )
			OnDetect?.Invoke( TargetObject );
	}

	public void Damaged( GameObject target )
	{
		if ( TargetObject == null && TargetObject != target )
			Detected( target, true );
	}

	public void Undetected()
	{
		BroadcastOnEscape();

		TargetObject = null;
		TargetPosition = Transform.Position;
		ReachedDestination = true;
	}

	[Broadcast]
	private void BroadcastOnEscape()
	{
		if ( TargetObject is not null )
			OnEnemyEscaped?.Invoke( TargetObject );
	}

	/// <summary>
	/// Who should the NPC follow, set null to go back to manually setting the target position
	/// </summary>
	/// <param name="target"></param>
	/// <param name="escapeFrom"></param>
	public void SetTarget( GameObject target, bool escapeFrom = false )
	{
		if ( target == null )
		{
			TargetObject = null;
			FollowingTargetObject = false;
			ReachedDestination = true;
			TargetPosition = Transform.Position;
		}
		else
		{
			TargetObject = target;
			FollowingTargetObject = !escapeFrom;
			MoveTo( GetPreferredTargetPosition( TargetObject ) );
			ReachedDestination = false;
		}
	}

	/// <summary>
	/// Is the object within the npc's reach (AttackRange) 
	/// </summary>
	/// <param name="target"></param>
	/// <returns></returns>
	public bool IsWithinRange( GameObject target )
	{
		if ( !GameObject.IsValid() ) return false;

		return IsWithinRange( target, AttackRange * Scale );
	}

	/// <summary>
	/// Is the object within the npc's range (Unscaled)
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

			var groundTrace = Game.ActiveScene.Trace.Ray( randomPoint + Vector3.Up * 64f, randomPoint + Vector3.Down * 64f )
				.Size( 5f )
				.WithoutTags( "player", "npc", "trigger" )
				.Run();

			if ( groundTrace.Hit && !groundTrace.StartedSolid )
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
		var offset = FollowingTargetObject ? direction * AttackRange * Scale / 2f : direction * VisionRange * Scale;
		var wishPos = targetPosition + offset;

		var groundTrace = Scene.Trace.Ray( wishPos + Vector3.Up * 64f, wishPos + Vector3.Down * 64f )
			.Size( 5f )
			.IgnoreGameObjectHierarchy( GameObject )
			.WithoutTags( "player", "npc", "trigger" )
			.Run();
		//Log.Info( target.Transform.Position.Distance( groundTrace.Hit && !groundTrace.StartedSolid ? groundTrace.HitPosition : (FollowingTargetObject ? targetPosition : targetPosition + offset) ) );
		//Log.Info( FollowingTargetObject );
		return groundTrace.Hit && !groundTrace.StartedSolid ? groundTrace.HitPosition : (FollowingTargetObject ? targetPosition : targetPosition + offset);
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
