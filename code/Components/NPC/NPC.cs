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
	public float DesiredDistance { get; private set; } = 20f;

	protected override void OnStart()
	{
		SetupNavAgent();
		SetupCollider();

		Tags.Set( "npc", true );
	}

	void SetupNavAgent()
	{
		Agent = Components.Create<NavMeshAgent>();
		Agent.Acceleration = Acceleration;
		Agent.Height = Height;
		Agent.Radius = Radius;
		Agent.MaxSpeed = WalkSpeed;
		Agent.Separation = 0.5f;
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

		CheckNewTarget();

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

	void CheckNewTarget()
	{
		if ( !TargetObject.IsValid() )
		{
			var desiredPosition = GetPreferredTargetPosition( TargetObject );

			if ( TargetPosition.Distance( desiredPosition ) >= DesiredDistance ) // Has our target moved?
				MoveTo( desiredPosition );
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
	/// Is the object within the npc's reach
	/// </summary>
	/// <param name="target"></param>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool IsWithinRange( GameObject target, float range = 50f )
	{
		if ( !GameObject.IsValid() ) return false;

		return target.Transform.Position.Distance( Transform.Position ) - DesiredDistance <= range;
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

	public Vector3 GetPreferredTargetPosition( GameObject target )
	{
		var targetPosition = target.Transform.Position;

		var direction = (targetPosition - Transform.Position).Normal;
		var offset = direction * DesiredDistance;
		var position = targetPosition + offset;
		var closestObjectPoint = Scene.NavMesh?.GetClosestPoint( position );

		if ( closestObjectPoint != null )
			return closestObjectPoint.Value;
		else
			return position;
	}

	protected override void OnFixedUpdate()
	{
		if ( Scene.GetAllComponents<Player>().FirstOrDefault() is Player player )
		{
			var diff = Transform.Position - player.Transform.Position;
			var direction = diff.Normal;
			TargetPosition = player.Transform.Position + direction * 80f;
		}
	}
}
