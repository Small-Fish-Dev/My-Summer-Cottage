using Sandbox;
using Sauna;

public enum NavigationType
{
	[Icon( "💩" )]
	[Description( "Dumb navigation, High performance" )]
	Dumb,
	[Icon( "🙂" )]
	[Description( "Normal navigation, Medium performance" )]
	Normal,
	[Icon( "🤓" )]
	[Description( "Smart navigation, Low performance" )]
	Smart
}

public partial class NPC
{
	public bool IsRunning { get; set; } = false;
	public float WishSpeed => (IsRunning ? RunSpeed : WalkSpeed) * Scale;

	int _totalDirections
	{
		get
		{
			return NavigationType switch
			{
				NavigationType.Dumb => 5,
				NavigationType.Normal => 10,
				NavigationType.Smart => 16,
				_ => 10,
			};
		}
	}

	int _tickToCheck
	{
		get
		{
			return NavigationType switch
			{
				NavigationType.Dumb => 20,
				NavigationType.Normal => 8,
				NavigationType.Smart => 2,
				_ => 8,
			};
		}
	}

	Vector3[] _possibleDirections
	{
		get
		{
			var finalDirections = new Vector3[_totalDirections];
			var angleSlice = 360 / _totalDirections;

			for ( int direction = 0; direction < _totalDirections; direction++ )
			{
				var rotation = Rotation.FromYaw( direction * angleSlice ).Forward;
				finalDirections[direction] = rotation;
			}

			return finalDirections;
		}
	}


	public virtual void ComputeNavigation()
	{
		if ( MoveHelper == null ) return;
		if ( TargetPosition == Vector3.Zero ) return;

		var currentTick = (int)(Time.Now / Time.Delta);

		if ( currentTick % _tickToCheck != NpcId % _tickToCheck ) return;

		CheckNewTargetPos();

		var distanceToTarget = Transform.Position.Distance( TargetPosition );

		if ( distanceToTarget <= MoveHelper.TraceRadius / 2f )
		{
			MoveHelper.WishVelocity = Vector3.Zero;
			return;
		}
		else
		{
			var movement3D = false; // TODO: Replace with property if we want flying NPCs
			var positionDifference = TargetPosition - Transform.Position;
			var wishDirection = (movement3D ? positionDifference.WithZ( 0f ) : positionDifference).Normal;

			var interestVectors = getInterest( wishDirection );
			var dangerVectors = getDanger();
			var preferredVector = getPreferredDirection( interestVectors, dangerVectors, out var value );
			var isGoingDirectPath = dangerVectors.Max() == 0 || preferredVector == interestVectors.Max();
			var preferredDirection = isGoingDirectPath ? wishDirection : preferredVector;
			var wishVelocity = preferredDirection * WishSpeed;
			var steeringForce = wishVelocity - MoveHelper.Velocity;

			MoveHelper.WishVelocity = wishVelocity;
			MoveHelper.WishVelocity += steeringForce;
		}
	}

	float[] getInterest( Vector3 direction )
	{
		return _possibleDirections
			.Select( x => direction.Dot( x ) )
			.ToArray();
	}

	float[] getDanger()
	{
		var possibleDirections = _possibleDirections;
		var totalDirections = possibleDirections.Length;
		var directionDanger = new float[totalDirections];
		var currentDirection = 0;

		foreach ( var direction in possibleDirections )
		{
			var startPosition = Transform.Position + Vector3.Up * (MoveHelper.StepHeight + MoveHelper.TraceRadius / 2f);
			var endPosition = startPosition + direction * MoveHelper.TraceRadius * 4f;
			var dangerTrace = Scene.Trace.Sphere( MoveHelper.TraceRadius, startPosition, endPosition )
				.IgnoreGameObjectHierarchy( GameObject )
				.Run();

			directionDanger[currentDirection] = dangerTrace.Hit ? 5f : 0f;

			if ( dangerTrace.Hit )
			{
				var directionToLeft = (currentDirection - 1 + totalDirections) % totalDirections;
				var directionToRight = (currentDirection + 1) % totalDirections;

				directionDanger[directionToLeft] += 2f;
				directionDanger[directionToRight] += 2f;
			}

			currentDirection++;
		}

		return directionDanger;
	}

	Vector3 getPreferredDirection( float[] interest, float[] danger, out float value )
	{
		var possibleDirections = _possibleDirections;
		var totalDirections = possibleDirections.Length;
		var finalDirections = new float[totalDirections];
		var currentDirection = 0;
		var currentHighest = 0;
		var currentValue = 0f;

		foreach ( var interestDir in interest )
		{
			var dangerDir = danger[currentDirection];
			var totalValue = interestDir - dangerDir;
			finalDirections[currentDirection] = totalValue;

			if ( totalValue > currentValue )
			{
				currentValue = totalValue;
				currentHighest = currentDirection;
			}

			currentDirection++;
		}

		value = currentHighest;
		return possibleDirections[currentHighest];
	}

	void CheckNewTargetPos()
	{
		if ( TargetObject.IsValid() )
		{
			if ( !IsWithinRange( TargetObject, Range ) ) // Has our target moved?
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
	}
}
