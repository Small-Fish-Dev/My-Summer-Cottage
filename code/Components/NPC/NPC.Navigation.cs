using Sandbox;
using Sauna;

public partial class NPC
{
	static int _totalDirections = 16;
	static Vector3[] _possibleDirections
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
		CheckNewTargetPos();

		var distanceToTarget = Transform.Position.Distance( TargetPosition );

		if ( distanceToTarget >= MoveHelper.TraceRadius )
		{
			var movement3D = false; // TODO: Replace with property if we want flying NPCs
			var positionDifference = TargetPosition - Transform.Position;
			var wishDirection = (movement3D ? positionDifference.WithZ( 0f ) : positionDifference).Normal;

			var interestVector = getInterest( wishDirection );
			var dangerVector = getDanger();
			var preferredDirection = getPreferredDirection( interestVector, dangerVector );
			var wishVelocity = preferredDirection * RunSpeed;
			var steeringForce = wishVelocity - MoveHelper.Velocity;

			MoveHelper.WishVelocity = wishVelocity;
			MoveHelper.WishVelocity += steeringForce;
		}

		MoveHelper.Move();
	}

	float[] getInterest( Vector3 direction )
	{
		return _possibleDirections
			.Select( x => direction.Dot( x ) )
			.ToArray();
	}

	float[] getDanger()
	{
		var totalDirections = _possibleDirections.Length;
		var directionDanger = new float[totalDirections];
		var currentDirection = 0;

		foreach ( var direction in _possibleDirections )
		{
			var startPosition = Transform.Position + Vector3.Up * (MoveHelper.StepHeight + MoveHelper.TraceRadius / 2f);
			var endPosition = startPosition + direction * 100f;
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

	Vector3 getPreferredDirection( float[] interest, float[] danger )
	{
		var totalDirections = _possibleDirections.Length;
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

		return _possibleDirections[currentHighest];
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
	}
}
