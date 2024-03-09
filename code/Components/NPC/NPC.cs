﻿using Sandbox;
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
public partial class NPC : Component
{
	[Property]
	public MoveHelper MoveHelper { get; set; }
	[Property]
	public SkinnedModelRenderer Model { get; set; }

	[Property]
	[Title( "NPC Type" )]
	public NPCType NPCType { get; set; } = NPCType.Medium;

	[Property]
	public NavigationType NavigationType { get; set; } = NavigationType.Normal;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 600f, 10f )]
	public float WalkSpeed { get; set; } = 90f;

	[Property]
	[Category( "Stats" )]
	[Range( 0f, 600f, 10f )]
	public float RunSpeed { get; set; } = 180f;

	[Property]
	[Category( "Stats" )]
	public bool FaceTowardsVelocity { get; set; } = true;

	[Property]
	[Category( "Triggers" )]
	public Action OnSpawn { get; set; }

	public delegate void NpcTrigger( GameObject provoker );

	/// <summary>
	/// If a GameObject has one of these tags it will be considered a provoker and can trigger alert state
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public TagSet ProvokerTags { get; set; }

	/// <summary>
	/// How close a provoker has to come to alert the NPC
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	[Range( 0f, 1024f, 16f )]
	public float AlertRange { get; set; } = 256f;

	/// <summary>
	/// When the provoker enters the alert area, or attacks the NPC, or a nearby NPC gets alerted
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnAlerted { get; set; }

	/// <summary>
	/// When the NPC gets directly attacked
	/// </summary>
	[Property]
	[Category( "Triggers" )]
	public NpcTrigger OnAttacked { get; set; }

	/// <summary>
	/// When the provoker gets out of the AlertRange or dies
	/// </summary>
	[Property]
	[Category( "Trigger" )]
	public NpcTrigger OnProvokerEscaped { get; set; }

	public int NpcId { get; set; }
	public Vector3 TargetPosition { get; set; }
	public GameObject TargetObject { get; private set; } = null;
	public float DesiredDistance { get; private set; } = 60f;

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

	protected override void OnUpdate()
	{
		if ( Model == null ) return;
		if ( MoveHelper == null ) return;

		if ( FaceTowardsVelocity )
			if ( !MoveHelper.Velocity.IsNearlyZero( 1f ) )
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, Rotation.LookAt( MoveHelper.Velocity.WithZ( 0f ), Vector3.Up ), Time.Delta * 10f );

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Forward ) / 150f;
		var newY = Vector3.Dot( MoveHelper.Velocity, Model.Transform.Rotation.Right ) / 150f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	protected override void OnFixedUpdate()
	{
		if ( MoveHelper == null ) return;

		if ( TargetObject.IsValid() )
		{
			if ( IsWithinRange( TargetObject, DesiredDistance ) )
			{
				var newRotation = Rotation.LookAt( TargetObject.Transform.Position.WithZ( 0f ) - Transform.Position.WithZ( 0f ), Vector3.Up );
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, newRotation, Time.Delta * 5f );

				SetRagdoll( true, spin: 100f );
				WorldPunch( TargetObject.Transform.Position, 400f, 300f );
			}
		}

		if ( Ragdoll != null )
			FollowRagdoll();

		ComputeNavigation();
		MoveHelper.Move();
	}

	/// <summary>
	/// Who should the NPC follow, set null to go back to manually setting the target position
	/// </summary>
	/// <param name="target"></param>
	public void SetTarget( GameObject target )
	{
		if ( TargetObject == target )
			return;

		TargetObject = target;

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

		var randomDirection = Rotation.FromYaw( Game.Random.Float( 360f ) ).Forward;
		var randomDistance = Game.Random.Float( minRange, maxRange );
		return position + randomDirection * randomDistance;
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
		return targetPosition + offset;
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
