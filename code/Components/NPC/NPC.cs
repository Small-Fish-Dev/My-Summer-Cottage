using Sandbox;
using Sauna;

[Icon( "smart_toy" )]
public class NPC : Component
{
	[Property]
	public SkinnedModelRenderer Model { get; set; }

	[Property]
	[Category( "Stats" )]
	[Range( 8f, 128f, 4f )]
	public float Height { get; set; } = 64f;

	[Property]
	[Category( "Stats" )]
	[Range( 2f, 32f, 2f )]
	public float Radius { get; set; } = 8f;

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
	[Range( 0f, 5000f, 10f )]
	public float Acceleration { get; set; } = 1000f;

	[Property]
	[Category( "Stats" )]
	public bool FaceTowardsVelocity { get; set; } = true;

	public NavMeshAgent Agent { get; set; }
	public CapsuleCollider Collider { get; set; }

	public Vector3 TargetPosition { get; set; }
	public GameObject TargetObject { get; private set; } = null;
	public float DesiredDistance { get; private set; } = 60f;
	public float CurrentSpeed { get; private set; } = 60f;

	protected override void OnStart()
	{
		SetupNavAgent();
		SetupCollider();

		Tags.Set( "npc", true );
		CurrentSpeed = WalkSpeed;
	}

	void SetupNavAgent()
	{
		Agent = Components.Create<NavMeshAgent>();
		Agent.Acceleration = Acceleration;
		Agent.Height = Height;
		Agent.Radius = Radius;
		Agent.MaxSpeed = WalkSpeed;
		Agent.Separation = 0.2f;
		Agent.Acceleration = Acceleration;
		Agent.UpdatePosition = true;
		Agent.UpdateRotation = FaceTowardsVelocity;
	}

	void SetupCollider()
	{
		Collider = Components.Create<CapsuleCollider>();
		Collider.Radius = Radius;
		Collider.Start = Vector3.Up * Radius;
		Collider.End = Vector3.Up * Math.Max( Height - Radius, Radius );
	}

	protected override void OnUpdate()
	{
		if ( Agent == null ) return;
		if ( Model == null ) return;

		var oldX = Model.GetFloat( "move_x" );
		var oldY = Model.GetFloat( "move_y" );
		var newX = Vector3.Dot( Agent.Velocity, Model.Transform.Rotation.Forward ) / 100f;
		var newY = Vector3.Dot( Agent.Velocity, Model.Transform.Rotation.Right ) / 100f;
		var x = MathX.Lerp( oldX, newX, Time.Delta * 5f );
		var y = MathX.Lerp( oldY, newY, Time.Delta * 5f );

		Model.Set( "move_x", x );
		Model.Set( "move_y", y );
	}

	protected override void OnFixedUpdate()
	{
		if ( Agent == null ) return;

		CheckNewTargetPos();

		if ( Scene.GetAllComponents<Player>().FirstOrDefault() is Player player )
			SetTarget( player.GameObject );

		if ( TargetObject.IsValid() )
		{
			if ( IsWithinRange( TargetObject, DesiredDistance ) )
			{
				var newRotation = Rotation.LookAt( TargetObject.Transform.Position.WithZ( 0f ) - Transform.Position.WithZ( 0f ) );
				Transform.Rotation = Rotation.Lerp( Transform.Rotation, newRotation, Time.Delta * 5f );
				Agent.UpdateRotation = false;
			}
			else
				Agent.UpdateRotation = FaceTowardsVelocity;
		}
		else
			Agent.UpdateRotation = FaceTowardsVelocity;
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
		Agent.MoveTo( TargetPosition );
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
}
